//  leveldb-sharp
// 
//  Copyright (c) 2012-2013, Mirco Bauer <meebey@meebey.net>
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without
//  modification, are permitted provided that the following conditions are
//  met:
// 
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following disclaimer
//       in the documentation and/or other materials provided with the
//       distribution.
//     * Neither the name of Google Inc. nor the names of its
//       contributors may be used to endorse or promote products derived from
//       this software without specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
//  OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
//  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
//  LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
//  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
//  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
//  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using LevelDB;

namespace ETModel
{
    public class DBTests
    {
        DB Database { get; set; }
        string DatabasePath { get; set; }

        public void SetUp()
        {
            var tempPath = System.IO.Directory.GetCurrentDirectory();
            var randName = "LevelDB";
            DatabasePath = Path.Combine(tempPath, randName);
            var options = new Options() {
                CreateIfMissing = true
            };
            Database = new DB(options, DatabasePath);
        }

        public void TearDown()
        {
            // some test-cases tear-down them self
            if (Database != null) {
                Database.Dispose();
            }
            if (Directory.Exists(DatabasePath)) {
                Directory.Delete(DatabasePath, true);
            }
        }

        public void Constructor()
        {
            // NOOP, SetUp calls ctor for us
        }

        public void Close()
        {
            // test double close
            Database.Dispose();
        }

        public void DisposeChecks()
        {
            Database.Dispose();
            Database.Get("key1");
        }

        public void Error()
        {
            var options = new Options() {
                CreateIfMissing = false
            };
            var db = new DB(options, "non-existent");
            db.Get("key1");
        }

        public void Put()
        {
            Database.Put( "key1", "value1");
            Database.Put( "key2", "value2");
            Database.Put( "key3", "value3");

            var options = new WriteOptions() {
                Sync = true
            };
            Database.Put( "key4", "value4", options);
        }

        public void Get()
        {
            Database.Put("key1", "value1");
            var value1 = Database.Get("key1");
            Trace.Assert("value1"==value1);

            Database.Put("key2", "value2");
            var value2 = Database.Get( "key2");
            Trace.Assert("value2"==value2);

            Database.Put( "key3", "value3");
            var value3 = Database.Get("key3");
            Trace.Assert("value3"==value3);

            // verify checksum
            var options = new ReadOptions() {
                VerifyCheckSums = true
            };
            value1 = Database.Get("key1", options);
            Trace.Assert("value1"==value1);

            // no fill cache
            options = new ReadOptions() {
                FillCache = false
            };
            value2 = Database.Get("key2", options);
            Trace.Assert("value2"==value2);
        }

        //[Test]
        public void Delete()
        {
            Database.Put("key1", "value1");
            var value1 = Database.Get("key1");
            //Assert.AreEqual("value1", value1);
            Database.Delete("key1");
            value1 = Database.Get("key1");
            //Assert.IsNull(value1);
        }

        //[Test]
        public void WriteBatch()
        {
            Database.Put("key1", "value1");

            var writeBatch = new WriteBatch().
                Delete("key1").
                Put("key2", "value2");
            Database.Write(writeBatch);

            var value1 = Database.Get("key1");
            //Assert.IsNull(value1);
            var value2 = Database.Get("key2");
            //Assert.AreEqual("value2", value2);

            writeBatch.Delete("key2").Clear();
            Database.Write(writeBatch);
            value2 = Database.Get("key2");
            //Assert.AreEqual("value2", value2);
        }

        //[Test]
        public void IsValid()
        {
            Database.Put("key1", "value1");

            var iter = Database.CreateIterator();
            iter.SeekToLast();
            //Assert.IsTrue(iter.IsValid);
            
            iter.Next();
            //Assert.IsFalse(iter.IsValid);
        }

        //[Test]
        public void Enumerator()
        {
            Database.Put( "key1", "value1");
            Database.Put( "key2", "value2");
            Database.Put( "key3", "value3");

            var entries = new List<KeyValuePair<byte[], byte[]>>();
            foreach (var entry in Database) {
                entries.Add( entry );
            }

            //Assert.AreEqual(3, entries.Count);
            //Assert.AreEqual("key1", entries[0].Key);
            //Assert.AreEqual("value1", entries[0].Value);
            //Assert.AreEqual("key2", entries[1].Key);
            //Assert.AreEqual("value2", entries[1].Value);
            //Assert.AreEqual("key3", entries[2].Key);
            //Assert.AreEqual("value3", entries[2].Value);
        }


        public void Snapshot()
        {
            // modify db
            Database.Put("key1", "value1");

            // create snapshot
            var snapshot = Database.CreateSnapshot();

            // modify db again
            Database.Put("key2", "value2");

            // read from snapshot
            var readOptions = new ReadOptions() {
                Snapshot = snapshot
            };
            var val1 = Database.Get( "key1", readOptions);
            //Assert.AreEqual("value1", val1);
            var val2 = Database.Get( "key2", readOptions);
            //Assert.IsNull(val2);

            // read from non-snapshot
            val1 = Database.Get("key1");
            //Assert.AreEqual("value1", val1);
            val2 = Database.Get("key2");
            //Assert.AreEqual("value2", val2);

            // release snapshot
            // GC calls ~Snapshot() for us


        }


        public void Destroy()
        {
            Database.Dispose();
            Database = null;

            DB.Destroy(null, DatabasePath);
        }

        public void Repair()
        {
            Database.Dispose();
            Database = null;

            DB.Repair(null, DatabasePath);
        }


    }
}
