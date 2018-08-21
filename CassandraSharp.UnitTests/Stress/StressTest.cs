﻿// cassandra-sharp - high performance .NET driver for Apache Cassandra
// Copyright (c) 2011-2018 Pierre Chalamet
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CassandraSharp.Config;
using CassandraSharp.CQLPoco;
using CassandraSharp.Extensibility;
using NUnit.Framework;

namespace CassandraSharp.UnitTests.Stress
{
    public class DisconnectingProxy
    {
        private readonly int _source;

        private readonly int _target;

        private volatile bool _enableKiller;

        private volatile bool _stop;

        public DisconnectingProxy(int source, int target)
        {
            _source = source;
            _target = target;
        }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(_ => Worker());
            Thread.Sleep(3000);
            Console.WriteLine("Proxy is started");
        }

        public void EnableKiller()
        {
            _enableKiller = true;
        }

        public void Stop()
        {
            _stop = true;
        }

        private void Worker()
        {
            var ipHostInfo = Dns.GetHostEntry("cassandra1");
            var ipAddress = ipHostInfo.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            EndPoint listenEndpoint = new IPEndPoint(ipAddress, _source);

            while (!_stop)
            {
                EndPoint targetEndpoint = new IPEndPoint(ipAddress, _target);
                var targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                targetSocket.Connect(targetEndpoint);

                var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(listenEndpoint);
                listenSocket.Listen(10);

                var clientSocket = listenSocket.Accept();
                ThreadPool.QueueUserWorkItem(_ => Transmit(clientSocket, targetSocket));
                ThreadPool.QueueUserWorkItem(_ => Transmit(targetSocket, clientSocket));
                Killer(targetSocket, clientSocket, listenSocket);
                Thread.Sleep(3000);
            }
        }

        private void Killer(params Socket[] sockets)
        {
            var rnd = new Random();
            while (!_stop)
            {
                Thread.Sleep(rnd.Next(500));

                var proba = rnd.Next(1000);
                if (_enableKiller && 900 < proba) break;
            }

            Console.WriteLine("Killing connection");

            foreach (var socket in sockets)
                try
                {
                    socket.Dispose();
                }
                catch (Exception)
                {
                }
        }

        private static void Transmit(Socket source, Socket target)
        {
            try
            {
                var buffer = new byte[1024];
                while (true)
                {
                    var count = source.Receive(buffer);
                    var write = 0;
                    while (write != count) write += target.Send(buffer, write, count - write, SocketFlags.None);
                }
            }
            catch
            {
            }
        }
    }

    [TestFixture]
    public class ResilienceTest
    {
        public class ResilienceLogger : ILogger
        {
            public void Debug(string format, params object[] prms)
            {
                Console.WriteLine(format, prms);
            }

            public void Info(string format, params object[] prms)
            {
                Console.WriteLine(format, prms);
            }

            public void Warn(string format, params object[] prms)
            {
                Console.WriteLine(format, prms);
            }

            public void Error(string format, params object[] prms)
            {
                Console.WriteLine(format, prms);
            }

            public void Fatal(string format, params object[] prms)
            {
                Console.WriteLine(format, prms);
            }

            public void Dispose()
            {
            }

            public bool IsDebugEnabled()
            {
                return true;
            }
        }

        [Test]
        public void RecoveryTest()
        {
            var cassandraSharpConfig = new CassandraSharpConfig
                                       {
                                           Logger = new LoggerConfig {Type = typeof(ResilienceLogger).AssemblyQualifiedName},
                                           Recovery = new RecoveryConfig {Interval = 2}
                                       };
            using (var clusterManager = new ClusterManager(cassandraSharpConfig))
            {
                var clusterConfig = new ClusterConfig
                                    {
                                        Endpoints = new EndpointsConfig
                                                    {
                                                        Servers = new[] {"cassandra1"}
                                                    },
                                        Transport = new TransportConfig
                                                    {
                                                        Port = 666,
                                                        ReceiveTimeout = 10 * 1000
                                                    }
                                    };

                var proxy = new DisconnectingProxy(666, 9042);
                proxy.Start();

                using (var cluster = clusterManager.GetCluster(clusterConfig))
                {
                    var cmd = cluster.CreatePocoCommand();

                    const string dropFoo = "drop keyspace data";
                    try
                    {
                        cmd.Execute(dropFoo).AsFuture().Wait();
                    }
                    catch
                    {
                    }

                    const string createFoo = "CREATE KEYSPACE data WITH replication = {'class': 'SimpleStrategy', 'replication_factor' : 1}";
                    cmd.Execute(createFoo).AsFuture().Wait();

                    const string createBar = "CREATE TABLE data.test (time text PRIMARY KEY)";
                    cmd.Execute(createBar).AsFuture().Wait();

                    proxy.EnableKiller();

                    for (var i = 0; i < 10; ++i)
                    {
                        var attempt = 0;
                        while (true)
                        {
                            var now = DateTime.Now;
                            var insert = string.Format("insert into data.test(time) values ('{0}');", now);
                            Console.WriteLine("{0}.{1}) {2}", i, ++attempt, insert);

                            try
                            {
                                cmd.Execute(insert).AsFuture().Wait();
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Failed with error {0}", ex.Message);
                                Thread.Sleep(1000);
                            }
                        }
                    }
                }

                proxy.Stop();
            }
        }
    }
}