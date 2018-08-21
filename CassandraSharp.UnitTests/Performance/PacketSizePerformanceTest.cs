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
using CassandraSharp.Config;
using CassandraSharp.CQLPoco;
using NUnit.Framework;

namespace CassandraSharp.UnitTests.Performance
{
    [TestFixture]
    public class PacketSizePerformanceTest
    {
        private static long InsertData(string data, IPreparedQuery<NonQuery> preparedQuery)
        {
            Console.WriteLine("Buffer size {0}", data.Length);
            var totalwatch = Stopwatch.StartNew();

            // warmup
            for (var i = 0; i < 10; ++i) preparedQuery.Execute(new {x = "abc", y = data}).AsFuture().Wait();

            const long nbQueries = 5000;
            for (var i = 0; i < nbQueries; ++i)
            {
                var stopwatch = Stopwatch.StartNew();
                preparedQuery.Execute(new {x = "abc", y = data}).AsFuture().Wait();
                stopwatch.Stop();
                //Console.WriteLine("Insert: {0}", stopwatch.ElapsedMilliseconds);
            }

            totalwatch.Stop();
            Console.WriteLine("Total inserts time ms: {0}", totalwatch.ElapsedMilliseconds);
            Console.WriteLine("Total inserts/s: {0}", 1000.0 * nbQueries / totalwatch.ElapsedMilliseconds);

            return totalwatch.ElapsedMilliseconds;
        }

        [Test]
        public void PacketSizeTest()
        {
            long time1423;
            long time1424;

            //run Write Performance Test using cassandra-sharp driver
            var cassandraSharpConfig = new CassandraSharpConfig();
            using (var clusterManager = new ClusterManager(cassandraSharpConfig))
            {
                var clusterConfig = new ClusterConfig
                                    {
                                        Endpoints = new EndpointsConfig
                                                    {
                                                        Servers = new[] {"cassandra1"}
                                                    }
                                    };

                using (var cluster = clusterManager.GetCluster(clusterConfig))
                {
                    var cmd = cluster.CreatePocoCommand();

                    const string dropFoo = "drop keyspace Tests";
                    try
                    {
                        cmd.Execute(dropFoo).AsFuture().Wait();
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch
                        // ReSharper restore EmptyGeneralCatchClause
                    {
                    }

                    const string createFoo = "CREATE KEYSPACE Tests WITH replication = {'class': 'SimpleStrategy', 'replication_factor' : 1}";
                    Console.WriteLine("============================================================");
                    Console.WriteLine(createFoo);
                    Console.WriteLine("============================================================");

                    var resCount = cmd.Execute(createFoo).AsFuture();
                    resCount.Wait();
                    Console.WriteLine();
                    Console.WriteLine();

                    const string createBar = "CREATE TABLE Tests.tbl ( x varchar primary key, y varchar )";
                    Console.WriteLine("============================================================");
                    Console.WriteLine(createBar);
                    Console.WriteLine("============================================================");
                    resCount = cmd.Execute(createBar).AsFuture();
                    resCount.Wait();
                    Console.WriteLine();
                    Console.WriteLine();

                    using (var preparedQuery = cmd.Prepare("insert into Tests.tbl (x, y) values (?, ?)"))
                    {
                        time1423 = InsertData(new string('x', 1423), preparedQuery);
                        Console.WriteLine();

                        time1424 = InsertData(new string('x', 1424), preparedQuery);
                        Console.WriteLine();
                    }

                    Console.WriteLine("============================================================");
                    Console.WriteLine(dropFoo);
                    Console.WriteLine("============================================================");

                    resCount = cmd.Execute(dropFoo).AsFuture();
                    resCount.Wait();
                }

                var delta = Math.Abs(time1424 - time1423);
                var min = Math.Max(time1423, time1424);
                var percent = delta / (double)min;
                Assert.IsTrue(percent < 1.0);
            }
        }
    }
}