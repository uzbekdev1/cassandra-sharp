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
using System.Net;

namespace CassandraSharp.Extensibility
{
    public sealed class TracingEvent
    {
        public string Activity { get; internal set; }

        public Guid EventId { get; internal set; }

        public IPAddress Source { get; internal set; }

        public int SourceElapsed { get; internal set; }

        public string Stage { get; internal set; }

        public string Thread { get; internal set; }
    }
}