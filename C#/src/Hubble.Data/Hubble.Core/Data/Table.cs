﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Hubble.Framework.IO;
using Hubble.Framework.Serialization;

namespace Hubble.Core.Data
{
    
    [Serializable, System.Xml.Serialization.XmlRoot(Namespace = "http://www.hubble.net")]
    public class Table
    {
        #region Private field
        
        string _Name;

        string _ConnectionString = null;

        string _DBTableName;

        List<Field> _Fields = new List<Field>();

        string _DBAdapterTypeName = null; //eg. SqlServer2005Adapter 

        string _SQLForCreate;

        int _ForceCollectCount = 5000;

        int _MaxReturnCount = 2 * 1024 * 1024;

        int _GroupByLimit = 40000;

        bool _IndexOnly = false;

        bool _Debug = false;

        private string _DocIdReplaceField = null;

        private bool _IgnoreReduplicateDocIdReplaceFieldAtInsert = true;

        bool _QueryCacheEnabled = true;

        int _QueryCacheTimeout = 0; //In seconds

        bool _StoreQueryCacheInFile = true;

        int _CleanupQueryCacheFileInDays = 7;

        bool _InitImmediatelyAfterStartup = Global.Setting.Config.InitTablesStartup;

        int _IndexThread = 1;

        bool _TableSynchronization = false;

        string _TriggerTableName = null;

        bool _IsBigTable = false;

        private BigTable.BigTable _BigTable = new Hubble.Core.BigTable.BigTable();

        #endregion

        #region Public properties

        /// <summary>
        /// This table is index only or not
        /// </summary>
        public bool IndexOnly
        {
            get
            {
                lock (this)
                {
                    return _IndexOnly;
                }
            }

            set
            {
                lock (this)
                {
                    _IndexOnly = value;
                }
            }
        }

        /// <summary>
        /// This field replace docid field connecting with database. 
        /// It is only actively in index only mode.
        /// </summary>
        public string DocIdReplaceField
        {
            get
            {
                lock (this)
                {
                    return _DocIdReplaceField;
                }
            }

            set
            {
                lock (this)
                {
                    _DocIdReplaceField = value;
                }
            }
        }

        /// <summary>
        /// if set to true. It will ignore the reduplicate id value when insert.
        /// if set to false. It will raise a exception when insert reduplicate id value.
        /// </summary>
        public bool IgnoreReduplicateDocIdReplaceFieldAtInsert
        {
            get
            {
                lock (this)
                {
                    return _IgnoreReduplicateDocIdReplaceFieldAtInsert;
                }
            }

            set
            {
                lock (this)
                {
                    _IgnoreReduplicateDocIdReplaceFieldAtInsert = value;
                }
            }
        }

        /// <summary>
        /// Table name
        /// </summary>
        public string Name
        {
            get
            {
                lock (this)
                {
                    return _Name;
                }
            }

            set
            {
                lock (this)
                {
                    _Name = value;
                }
            }
        }

        /// <summary>
        /// ConnectionString of database (eg. SQLSERVER)
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }

            set
            {
                _ConnectionString = value;
            }
        }

        /// <summary>
        /// Table name of database (eg. SQLSERVER)
        /// </summary>
        public string DBTableName
        {
            get
            {
                return _DBTableName;
            }

            set
            {
                _DBTableName = value;
            }
        }

        /// <summary>
        /// Fields of this table
        /// </summary>
        public List<Field> Fields
        {
            get
            {
                return _Fields;
            }
        }

        public string DBAdapterTypeName
        {
            get
            {
                return _DBAdapterTypeName;
            }

            set
            {
                _DBAdapterTypeName = value;
            }
        }

        public string SQLForCreate
        {
            get
            {
                lock (this)
                {
                    return _SQLForCreate;
                }
            }

            set
            {
                lock (this)
                {
                    _SQLForCreate = value;
                }
            }
        }

        public bool Debug
        {
            get
            {
                lock (this)
                {
                    return _Debug;
                }
            }

            set
            {
                lock (this)
                {
                    _Debug = value;
                }
            }

        }

        public int ForceCollectCount
        {
            get
            {
                return _ForceCollectCount;
            }

            set
            {
                if (value <= 0)
                {
                    _ForceCollectCount = 1;
                }
                else
                {
                    _ForceCollectCount = value;
                }
            }
        }

        /// <summary>
        /// max count when the doc id list is too many to list.
        /// </summary>
        public int MaxReturnCount
        {
            get
            {
                lock (this)
                {
                    return _MaxReturnCount;
                }
            }

            set
            {
                lock (this)
                {
                    if (value <= 0)
                    {
                        _MaxReturnCount = 2 * 1024 * 1024;
                    }
                    else
                    {
                        _MaxReturnCount = value;
                    }
                }
            }
        }

        /// <summary>
        /// limit number for group by.
        /// Only group by limit number of records from the query result.
        /// </summary>
        public int GroupByLimit
        {
            get
            {
                lock (this)
                {
                    return _GroupByLimit;
                }
            }

            set
            {
                lock (this)
                {
                    if (value <= 0)
                    {
                        _GroupByLimit = 40000;
                    }
                    else
                    {
                        _GroupByLimit = value;
                    }
                }
            }
        }

        public bool QueryCacheEnabled
        {
            get
            {
                lock (this)
                {
                    return _QueryCacheEnabled;
                }
            }

            set
            {
                lock (this)
                {
                    _QueryCacheEnabled = value;
                }
            }
        }

        public int QueryCacheTimeout
        {
            get
            {
                lock (this)
                {
                    return _QueryCacheTimeout;
                }
            }

            set
            {
                lock (this)
                {
                    _QueryCacheTimeout = value;
                }
            }
        }


        public bool StoreQueryCacheInFile
        {
            get
            {
                lock (this)
                {
                    return _StoreQueryCacheInFile;
                }
            }

            set
            {
                lock (this)
                {
                    _StoreQueryCacheInFile = value;
                }
            }
        }

        public int CleanupQueryCacheFileInDays
        {
            get
            {
                lock (this)
                {
                    return _CleanupQueryCacheFileInDays;
                }
            }

            set
            {
                lock (this)
                {
                    if (value < 0)
                    {
                        _CleanupQueryCacheFileInDays = 0;
                    }
                    else
                    {
                        _CleanupQueryCacheFileInDays = value;
                    }
                }
            }
        }

        /// <summary>
        /// Initinalize immediately after hubble.net startup.
        /// </summary>
        public bool InitImmediatelyAfterStartup
        {
            get
            {
                lock (this)
                {
                    return _InitImmediatelyAfterStartup;
                }
            }

            set
            {
                lock (this)
                {
                    _InitImmediatelyAfterStartup = value;
                }
            } 
        }

        /// <summary>
        /// How many threads used for index
        /// </summary>
        public int IndexThread
        {
            get
            {
                lock (this)
                {
                    return _IndexThread;
                }
            }

            set
            {
                lock (this)
                {
                    _IndexThread = value;
                }
            } 
        }

        /// <summary>
        /// Table synchronization.
        /// If set it to true, it will be 
        /// synchronized from database and
        /// can't be used insert, update or delete.
        /// Only indexonly mode can be available.
        /// </summary>
        public bool TableSynchronization
        {
            get
            {
                lock (this)
                {
                    return _TableSynchronization;
                }
            }

            set
            {
                lock (this)
                {
                    _TableSynchronization = value;
                }
            }
        }

        /// <summary>
        /// Trigger table in database.
        /// Available when DocIdReplaceField != null
        /// </summary>
        public string TriggerTableName
        {
            get
            {
                lock (this)
                {
                    return _TriggerTableName;
                }
            }

            set
            {
                lock (this)
                {
                    _TriggerTableName = value;
                }
            }

        }

        /// <summary>
        /// Is it big table
        /// </summary>
        public bool IsBigTable
        {
            get
            {
                lock (this)
                {
                    return _IsBigTable;
                }
            }

            set
            {
                lock (this)
                {
                    _IsBigTable = value;
                }
            }

        }

        /// <summary>
        /// Configuration of bigtable
        /// </summary>
        public BigTable.BigTable BigTable
        {
            get
            {
                lock (this)
                {
                    return _BigTable;
                }
            }

            set
            {
                lock (this)
                {
                    _BigTable = value;
                }
            }
        }


        private static void TableCheck(Table table)
        {
            foreach (Field field in table.Fields)
            {
                if (field.IndexType == Field.Index.Untokenized)
                {
                    switch (field.DataType)
                    {
                        case DataType.TinyInt:
                        case DataType.SmallInt:
                            if (field.SubTabIndex < 0)
                            {
                                throw new DataException(string.Format("SubTabIndex of field:{0} less then zero, the payload file is for old version, please truncate table and rebuild it!",
                                    field.Name));
                            }
                            break;
                    }
                }
            }
        }
        

        public void Save(string dir)
        {
            dir = Path.AppendDivision(dir, '\\');

            string fileName = dir + "tableinfo.xml";

            using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create,
                 System.IO.FileAccess.ReadWrite))
            {
                XmlSerialization<Table>.Serialize(this, Encoding.UTF8, fs);
            }            
        }

        public static Table Load(string dir)
        {
            dir = Path.AppendDivision(dir, '\\');

            string fileName = dir + "tableinfo.xml";

            Table table;

            using (System.IO.FileStream fs = new System.IO.FileStream(fileName, System.IO.FileMode.Open,
                 System.IO.FileAccess.Read))
            {
                table = XmlSerialization<Table>.Deserialize(fs);
            }

            TableCheck(table);

            return table;
        }

        #endregion

    }
}
