﻿// cassandra-sharp - high performance .NET driver for Apache Cassandra
// Copyright (c) 2011-2013 Pierre Chalamet
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

namespace CassandraSharp.CQLPropertyBag
{
    internal sealed class PropertyBagCommand : IPropertyBagCommand
    {
        private readonly ICqlCommand _command;

        public PropertyBagCommand(ICqlCommand command)
        {
            _command = command;
        }

        public ICqlQuery<T> Execute<T>(string cql, object dataSource)
        {
            return _command.Execute<T>(cql);
        }

        public IPreparedQuery<T> Prepare<T>(string cql, ExecutionFlags executionFlags)
        {
            return _command.Prepare<T>(cql, executionFlags);
        }

        public ICqlQuery<PropertyBag> Execute(string cql, object dataSource)
        {
            return _command.Execute<PropertyBag>(cql);
        }

        public IPreparedQuery<PropertyBag> Prepare(string cql, ExecutionFlags executionFlags)
        {
            return _command.Prepare<PropertyBag>(cql, executionFlags);
        }
    }
}