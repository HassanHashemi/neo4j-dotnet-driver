﻿// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Collections.Generic;
using System;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.Result
{
    internal abstract class ResultBuilderBase : IMessageResponseCollector
    {
        private bool _statementProcessed = false;
        protected List<string> _keys = new List<string>();
        protected SummaryCollector SummaryCollector { get; }

        protected ResultBuilderBase(Statement statement, IServerInfo server)
        {
            SummaryCollector = new SummaryCollector(statement, server);
        }

        protected List<string> Keys
        {
            get
            {
                if (!_statementProcessed)
                {
                    EnsureStatementProcessed();
                }

                return _keys;
            }
        }

        public void CollectFields(IDictionary<string, object> meta)
        {
            if (meta == null)
            {
                return;
            }

            CollectKeys(meta, "fields", _keys);
            SummaryCollector.CollectWithFields(meta);
        }

        public void CollectBookmark(IDictionary<string, object> meta)
        {
            throw new NotSupportedException(
                $"Should not get a bookmark on a result. bookmark = {meta[Bookmark.BookmarkKey].As<string>()}");
        }

        public void CollectRecord(object[] fields)
        {
            var record = new Record(_keys, fields);
            EnqueueRecord(record);
        }

        public void CollectSummary(IDictionary<string, object> meta)
        {
            NoMoreRecords();
            if (meta == null)
            {
                return;
            }
            SummaryCollector.Collect(meta);
        }

        public void DoneSuccess()
        {
            // do nothing
            _statementProcessed = true;
        }

        public void DoneFailure()
        {
            NoMoreRecords();// an error received, so the result is broken
            _statementProcessed = true;
        }

        public void DoneIgnored()
        {
            NoMoreRecords();// the result is ignored
            _statementProcessed = true;
        }

        protected abstract void EnsureStatementProcessed();
        protected abstract void NoMoreRecords();
        protected abstract void EnqueueRecord(Record record);

        private static void CollectKeys(IDictionary<string, object> meta, string name, List<string> keys)
        {
            if (meta.ContainsKey(name))
            {
                keys.AddRange(meta[name].As<List<string>>());
            }
        }
    }
}