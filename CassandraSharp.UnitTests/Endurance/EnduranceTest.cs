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
using System.Diagnostics;
using System.Threading;
using CassandraSharp.Config;
using CassandraSharp.CQLPoco;
using NUnit.Framework;

namespace CassandraSharp.UnitTests.Endurance
{
    [TestFixture]
    public class EnduranceTest
    {
        private void BinaryProtocolRunWritePerformanceParallel(string transportType)
        {
            //run Write Performance Test using cassandra-sharp driver
            var cassandraSharpConfig = new CassandraSharpConfig();
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
                                                        Type = transportType
                                                    }
                                    };

                using (var cluster = clusterManager.GetCluster(clusterConfig))
                {
                    var cmd = cluster.CreatePocoCommand();

                    const string dropFoo = "drop keyspace Endurance";
                    try
                    {
                        cmd.Execute(dropFoo).AsFuture().Wait();
                    }
                    catch
                    {
                    }

                    const string createFoo = "CREATE KEYSPACE Endurance WITH replication = {'class': 'SimpleStrategy', 'replication_factor' : 1}";
                    Console.WriteLine("============================================================");
                    Console.WriteLine(createFoo);
                    Console.WriteLine("============================================================");

                    var resCount = cmd.Execute(createFoo);
                    resCount.AsFuture().Wait();
                    Console.WriteLine();
                    Console.WriteLine();

                    const string createBar = "CREATE TABLE Endurance.stresstest (strid varchar,intid int,PRIMARY KEY (strid))";
                    Console.WriteLine("============================================================");
                    Console.WriteLine(createBar);
                    Console.WriteLine("============================================================");
                    resCount = cmd.Execute(createBar);
                    resCount.AsFuture().Wait();
                    Console.WriteLine();
                    Console.WriteLine();

                    const string insertPerf = "UPDATE Endurance.stresstest SET intid = ? WHERE strid = ?";
                    Console.WriteLine("============================================================");
                    Console.WriteLine(" Cassandra-Sharp Driver write performance test single thread ");
                    Console.WriteLine("============================================================");
                    var prepared = cmd.Prepare(insertPerf);

                    var timer = Stopwatch.StartNew();

                    var running = 0;
                    for (var i = 0; i < 100000; i++)
                    {
                        if (0 == i % 1000) Console.WriteLine("Sent {0} requests - pending requests {1}", i, Interlocked.CompareExchange(ref running, 0, 0));

                        Interlocked.Increment(ref running);
                        prepared.Execute(new {intid = i, strid = i.ToString("X")}).AsFuture().ContinueWith(_ => Interlocked.Decrement(ref running));
                    }

                    while (0 != Interlocked.CompareExchange(ref running, 0, 0))
                    {
                        Console.WriteLine("{0} requests still running", running);
                        Thread.Sleep(1 * 1000);
                    }

                    timer.Stop();
                    Console.WriteLine("Endurance ran in {0} ms", timer.ElapsedMilliseconds);

                    Console.WriteLine("============================================================");
                    Console.WriteLine(dropFoo);
                    Console.WriteLine("============================================================");

                    cmd.Execute(dropFoo).AsFuture().Wait();
                }
            }
        }

        [Test]
        public void BinaryProtocolRunWritePerformanceParallelLongRunning()
        {
            BinaryProtocolRunWritePerformanceParallel("LongRunning");
        }
    }
}