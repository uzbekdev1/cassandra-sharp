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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using CassandraSharp.Extensibility;
using CassandraSharp.Utils.Stream;

namespace CassandraSharp.CQLBinaryProtocol.Queries
{
    internal sealed class CreateKeyspaceQuery : Query<bool>
    {
        private readonly bool _durableWrites;
        private readonly string _name;

        private readonly IDictionary<string, string> _replicationOptions;

        public CreateKeyspaceQuery(IConnection connection, string name, IDictionary<string, string> replicationOptions, bool durableWrites)
            : base(connection, ConsistencyLevel.ONE, ExecutionFlags.None)
        {
            _name = name;
            _replicationOptions = replicationOptions;
            _durableWrites = durableWrites;
        }

        protected override IEnumerable<bool> ReadFrame(IFrameReader frameReader)
        {
            var stream = frameReader.ReadOnlyStream;
            var resultOpcode = (ResultOpcode)stream.ReadInt();
            yield return resultOpcode == ResultOpcode.SchemaChange;
        }

        protected override void WriteFrame(IFrameWriter fw)
        {
            var sb = new StringBuilder("CREATE KEYSPACE ");
            sb.Append(_name);
            sb.Append(" WITH replication = {");
            sb.Append(string.Join(",", _replicationOptions.Select(x => string.Format("'{0}': '{1}'", x.Key, x.Value))));
            sb.Append("} AND durable_writes = ");
            sb.Append(_durableWrites);

            var stream = fw.WriteOnlyStream;
            stream.WriteLongString(sb.ToString());
            stream.WriteUShort((ushort)ConsistencyLevel);
            stream.WriteByte(0);
            fw.SetMessageType(MessageOpcodes.Query);
        }

        protected override InstrumentationToken CreateInstrumentationToken()
        {
            var token = InstrumentationToken.Create(RequestType.Ready, ExecutionFlags.None);
            return token;
        }
    }
}