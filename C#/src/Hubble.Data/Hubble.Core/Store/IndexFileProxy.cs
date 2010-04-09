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
using System.Diagnostics;
using Hubble.Framework.IO;
using Hubble.Framework.Threading;
using Hubble.Core.Data;

namespace Hubble.Core.Store
{
    public class IndexFileProxy : /*MessageQueue,*/ IIndexFile
    {
        const int Timeout = 5 * 60 * 1000;

        enum Event
        {
            Add = 1,
            Collect = 2,
            Get = 3,
            GetFilePositionList = 4,
            MergeAck = 5,
        }

        public class GetInfo
        {
            string _Word;
            int _TotalDocs;
            int _MaxReturnCount = -1;
            private Data.DBProvider _DBProvider;
            //private int _TabIndex;

            public string Word
            {
                get
                {
                    return _Word;
                }
            }

            public int TotalDocs
            {
                get
                {
                    return _TotalDocs;
                }
            }

            public int MaxReturnCount
            {
                get
                {
                    return _MaxReturnCount;
                }
            }

            public Data.DBProvider DBProvider
            {
                get
                {
                    return _DBProvider;
                }
            }

            //public int TabIndex
            //{
            //    get
            //    {
            //        return _TabIndex;
            //    }
            //}

            public GetInfo(string word, int totalDocs, Data.DBProvider dbProvider, int maxReturnCount)
            {
                _Word = word;
                _TotalDocs = totalDocs;
                _DBProvider = dbProvider;
                _MaxReturnCount = maxReturnCount;
                //_TabIndex = tabIndex;
            }
        }

        class WordDocList
        {
            string _Word;

            public string Word
            {
                get
                {
                    return _Word;
                }
            }

            List<Entity.DocumentPositionList> _DocList;

            public List<Entity.DocumentPositionList> DocList
            {
                get
                {
                    return _DocList;
                }
            }

            public WordDocList(string word, List<Entity.DocumentPositionList> docList)
            {
                _Word = word;
                _DocList = docList;
            }
        }

        public class MergeAck
        {
            public class MergeFilePosition
            {
                internal IndexFile.FilePosition MergedFilePostion;

                internal string Word;

                //internal WordFilePositionList FilePostionList;

                internal MergeFilePosition(IndexFile.FilePosition filePostion, string word)
                {
                    MergedFilePostion = filePostion;
                    //FilePostionList = pList;
                    this.Word = word;
                }

            }

            private int _BeginSerial; // Begin file serial;

            public int BeginSerial
            {
                get
                {
                    return _BeginSerial;
                }
            }

            private int _EndSerial; // End file serial;

            public int EndSerial
            {
                get
                {
                    return _EndSerial;
                }
            }

            private int _MergedSerial; // File serial merged;

            public int MergedSerial
            {
                get
                {
                    return _MergedSerial;
                }
            }

            string _MergeHeadFileName;

            public string MergeHeadFileName
            {
                get
                {
                    return _MergeHeadFileName;
                }
            }

            string _MergeIndexFileName;

            public string MergeIndexFileName
            {
                get
                {
                    return _MergeIndexFileName;
                }
            }

            public List<MergeFilePosition> MergeFilePositionList = new List<MergeFilePosition>();

            public MergeAck(int begin, int end, string mergeHead, string mergeIndex, int mergedSerial)
            {
                _BeginSerial = begin;
                _EndSerial = end;
                _MergeHeadFileName = mergeHead;
                _MergeIndexFileName = mergeIndex;
                _MergedSerial = mergedSerial;
            }
        }

        public class MergeInfos
        {
            private int _BeginSerial; // Begin file serial;

            public int BeginSerial
            {
                get
                {
                    return _BeginSerial;
                }
            }

            private int _EndSerial; // End file serial;

            public int EndSerial
            {
                get
                {
                    return _EndSerial;
                }
            }


            private int _MergedSerial; // File serial merged;

            public int MergedSerial
            {
                get
                {
                    return _MergedSerial;
                }
            }

            string _MergeHeadFileName;

            public string MergeHeadFileName
            {
                get
                {
                    return _MergeHeadFileName;
                }
            }

            string _MergeIndexFileName;

            public string MergeIndexFileName
            {
                get
                {
                    return _MergeIndexFileName;
                }
            }

            List<MergedWordFilePostionList> _MergedWordFilePostionList;

            public List<MergedWordFilePostionList> MergedWordFilePostionList
            {
                get
                {
                    return _MergedWordFilePostionList;
                }
            }

            public MergeInfos(string headFileName, string indexFileName,
                List<MergedWordFilePostionList> list, int begin, int end, int mergedSerial)
            {
                _MergeHeadFileName = headFileName;
                _MergeIndexFileName = indexFileName;
                _MergedWordFilePostionList = list;
                _BeginSerial = begin;
                _EndSerial = end;
                _MergedSerial = mergedSerial;
            }
        }

        public class MergedWordFilePostionList : IComparable<MergedWordFilePostionList>
        {
            private string _Word;

            public string Word
            {
                get
                {
                    return _Word;
                }
            }

            private WordFilePositionList _FilePositionList;

            internal WordFilePositionList FilePositionList
            {
                get
                {
                    return _FilePositionList;
                }
            }

            //private WordFilePositionList _OrginalFilePositionList;

            //internal WordFilePositionList OrginalFilePositionList
            //{
            //    get
            //    {
            //        return _OrginalFilePositionList;
            //    }
            //}

            internal MergedWordFilePostionList(string word)
            {
                _Word = word;
                //_OrginalFilePositionList = orginal;
                _FilePositionList = new WordFilePositionList();
            }

            #region IComparable<WordFilePostionList> Members

            public int CompareTo(MergedWordFilePostionList other)
            {
                if (other == null)
                {
                    return 1;
                }

                if (this.FilePositionList.Count == 0 && other.FilePositionList.Count == 0)
                {
                    return 0;
                }

                if (other.FilePositionList.Count == 0)
                {
                    return 1;
                }

                if (this.FilePositionList.Count == 0)
                {
                    return -1;
                }

                if (this.FilePositionList[0].Serial > other.FilePositionList[0].Serial)
                {
                    return 1;
                }
                else if (this.FilePositionList[0].Serial < other.FilePositionList[0].Serial)
                {
                    return -1;
                }
                else
                {
                    long myPosition = this.FilePositionList[0].Position;
                    long otherPosition = other.FilePositionList[0].Position;

                    if (myPosition > otherPosition)
                    {
                        return 1;
                    }
                    else if (myPosition < otherPosition)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }

            }

            #endregion
        }


        private object _MergeLockObj = new object();

        private bool _CanMerge = true;

        private IndexFile _IndexFile;

        private WordFilePositionProvider _WordFilePositionTable = new WordFilePositionProvider();

        private int _WordCount = 0;

        private bool _NeedClose = false;

        private bool _CanClose = true;

        private object _LockObj = new object();

        private Hubble.Core.Data.Field.IndexMode _IndexMode;

        private int InnerWordTableSize
        {
            get
            {
                lock (this)
                {
                    return _WordCount;
                }
            }

            set
            {
                lock (this)
                {
                    _WordCount = value;
                }
            }
        }

        //private Index.DelegateWordUpdate _WordUpdateDelegate;

        #region Public properties

        //public Index.DelegateWordUpdate WordUpdateDelegate
        //{
        //    get
        //    {
        //        return _WordUpdateDelegate;
        //    }

        //    set
        //    {
        //        _WordUpdateDelegate = value;
        //    }
        //}

        public int WordTableSize
        {
            get
            {
                return InnerWordTableSize;
            }
        }

        internal bool CanMerge
        {
            get
            {
                lock (_MergeLockObj)
                {
                    return _CanMerge;
                }
            }

            set
            {
                lock (_MergeLockObj)
                {
                    _CanMerge = value ;
                }
            }
        }

        internal bool CanClose
        {
            get
            {
                lock (_LockObj)
                {
                    return _CanClose;
                }

            }
        }

        internal string LastHeadFilePath
        {
            get
            {
                return _IndexFile.LastHeadFilePath;
            }
        }

        internal string LastIndexFilePath
        {
            get
            {
                return _IndexFile.LastIndexFilePath;
            }
        }

        #endregion

        private WordFilePositionList GetFilePositionListByWord(string word)
        {
            WordFilePositionList pList;

            if (_WordFilePositionTable.TryGetValue(word, out pList))
            {
                return pList;
            }
            else
            {
                return null;
            }
        }

        private void PatchWordFilePositionTable(List<IndexFile.WordFilePosition> wordFilePostionList)
        {
            foreach (IndexFile.WordFilePosition p in wordFilePostionList)
            {
                WordFilePositionList pList;

                if (_WordFilePositionTable.TryGetValue(p.Word, out pList))
                {
                    pList.Add(p.Position);
                }
                else
                {
                    pList = new WordFilePositionList();
                    pList.AddOnly(p.Position);

                    string internedWord = string.IsInterned(p.Word);

                    if (internedWord == null)
                    {
                        internedWord = p.Word;
                    }

                    _WordFilePositionTable.Add(internedWord, pList);
                }
            }

            InnerWordTableSize = _WordFilePositionTable.Count;

            GC.Collect();
            GC.Collect();
            GC.Collect();

        }

        private object ProcessGetFilePositionList(int evt, MessageQueue.MessageFlag flag, object data)
        {
            OptimizationOption option = (OptimizationOption)data;

            List<MergedWordFilePostionList> result = new List<MergedWordFilePostionList>();

            if (_IndexFile.IndexFileList.Count <= 2)
            {
                if (_IndexFile.IndexFileList.Count <= 1)
                {
                    return null;
                }
                else if (option != OptimizationOption.Minimum)
                {
                    return null;
                }
            }

            int i = 0;
            long fstFileSize = 0;
            long secFileSize = 0;
            long otherFileSize = 0;

            foreach (IndexFile.IndexFileInfo ifi in _IndexFile.IndexFileList)
            {
                if (i == 0)
                {
                    fstFileSize = ifi.Size;
                }
                else if (i == 1)
                {
                    secFileSize = ifi.Size;
                }
                else
                {
                    otherFileSize += ifi.Size;
                }

                i++;
            }

            int begin;
            int end = _IndexFile.IndexFileList[_IndexFile.IndexFileList.Count - 1].Serial;

            switch (option)
            {
                case OptimizationOption.Minimum:
                    begin = _IndexFile.IndexFileList[0].Serial;
                    break;
                case OptimizationOption.Middle:
                    if (fstFileSize < otherFileSize + secFileSize)
                    {
                        begin = _IndexFile.IndexFileList[0].Serial;
                    }
                    else
                    {
                        begin = _IndexFile.IndexFileList[1].Serial;
                    }
                    break;
                case OptimizationOption.Speedy:
                    if (fstFileSize < otherFileSize + secFileSize)
                    {
                        begin = _IndexFile.IndexFileList[0].Serial;
                    }
                    else
                    {
                        begin = _IndexFile.IndexFileList[1].Serial;

                        if (secFileSize > otherFileSize * 10 && _IndexFile.IndexFileList.Count < 32)
                        {
                            //If the index file count < 32 and all other files is small file
                            //Does not need optimize
                            return null;
                        }
                    }
                    break;
                default:
                    return null;
            }

            foreach (string word in _WordFilePositionTable.Keys)
            {
                WordFilePositionList pList = _WordFilePositionTable[word];
                MergedWordFilePostionList wfpl = new MergedWordFilePostionList(word);

                foreach (IndexFile.FilePosition fp in pList.Values)
                {
                    if (fp.Serial >= begin && fp.Serial <= end)
                    {
                        wfpl.FilePositionList.AddOnly(new IndexFile.FilePosition(fp.Serial, fp.Position, fp.Length));
                    }
                }

                result.Add(wfpl);
            }

            int serial;

            if (begin == _IndexFile.IndexFileList[0].Serial)
            {
                serial = 0;
            }
            else
            {
                serial = 1;
            }



            return new MergeInfos(_IndexFile.GetHeadFileName(serial),
                _IndexFile.GetIndexFileName(serial), result, begin, end, serial);
        }

        private void ProcessMergeAck(int evt, MessageQueue.MessageFlag flag, object data)
        {
            MergeAck mergeAck = (MergeAck)data;

            int begin = mergeAck.BeginSerial;
            int end = mergeAck.EndSerial;
            int time = 0;

            for (int serial = begin; serial <= end; serial++)
            {
                string fileName;

                fileName = _IndexFile.IndexDir + GetHeadFileName(serial);

                while (true)
                {
                    try
                    {
                        if (System.IO.File.Exists(fileName))
                        {
                            System.IO.File.Delete(fileName);
                        }

                        break;
                    }
                    catch
                    {
                        if (time < 40)
                        {
                            System.Threading.Thread.Sleep(20);
                            time++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }


                fileName = _IndexFile.IndexDir + GetIndexFileName(serial);
                time = 0;

                while (true)
                {
                    try
                    {
                        if (System.IO.File.Exists(fileName))
                        {
                            System.IO.File.Delete(fileName);
                        }

                        break;
                    }
                    catch
                    {
                        if (time < 40)
                        {
                            System.Threading.Thread.Sleep(20);
                            time++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            System.Threading.Thread.Sleep(20);

            try
            {
                System.IO.File.Move(_IndexFile.IndexDir + @"Optimize\" + mergeAck.MergeHeadFileName,
                    _IndexFile.IndexDir + mergeAck.MergeHeadFileName);
            }
            catch (Exception e)
            {
                Global.Report.WriteErrorLog(string.Format("ProcessMergeAck begin = {0} end = {1} dest file name:{2}",
                    begin, end, _IndexFile.IndexDir + mergeAck.MergeHeadFileName),
                    e);
                throw e;
            }

            try
            {
                System.IO.File.Move(_IndexFile.IndexDir + @"Optimize\" + mergeAck.MergeIndexFileName,
                    _IndexFile.IndexDir + mergeAck.MergeIndexFileName);
            }
            catch (Exception e)
            {
                Global.Report.WriteErrorLog(string.Format("ProcessMergeAck begin = {0} end = {1} dest file name:{2}",
                    begin, end, _IndexFile.IndexDir + mergeAck.MergeIndexFileName),
                    e);
                throw e;
            }

            foreach (MergeAck.MergeFilePosition mfp in mergeAck.MergeFilePositionList)
            {
                //WordFilePositionList pList = mfp.FilePostionList;

                WordFilePositionList pList = _WordFilePositionTable[mfp.Word];

                if (pList == null)
                {
                    continue;
                }
                
                int i = 0;
                bool fst = true;

                while (i < pList.Count)
                {
                    if (pList[i].Serial >= begin && pList[i].Serial <= end)
                    {
                        if (fst)
                        {
                            pList[i] = mfp.MergedFilePostion;
                            fst = false;
                            i++;
                        }
                        else
                        {
                            pList.RemoveAt(i);
                        }
                    }
                    else
                    {
                        i++;
                    }
                }

                _WordFilePositionTable.Reset(pList.Word, pList.FPList);
            }

            _IndexFile.AfterMerge(begin, end, mergeAck.MergedSerial);
        }

        private object ProcessMessage(int evt, MessageQueue.MessageFlag flag, object data)
        {
            try
            {
                switch ((Event)evt)
                {
                    case Event.Add:
                        WordDocList wl = (WordDocList)data;
                        _IndexFile.AddWordAndDocList(wl.Word, wl.DocList);
                        //if (WordUpdateDelegate != null)
                        //{
                        //    WordUpdateDelegate(wl.Word, wl.DocList);
                        //}

                        break;
                    case Event.Collect:

                        lock (_LockObj)
                        {
                            if (_NeedClose)
                            {
                                break;
                            }

                            _CanClose = false;
                        }

                        _IndexFile.Collect();

                        PatchWordFilePositionTable(_IndexFile.WordFilePositionList);
                        _IndexFile.ClearWordFilePositionList();

                        lock (_LockObj)
                        {
                            _CanClose = true;
                        }

                        break;
                    case Event.Get:
                        {
                            GetInfo getInfo = data as GetInfo;
                            WordFilePositionList pList = GetFilePositionListByWord(getInfo.Word);
                            return _IndexFile.GetWordIndex(getInfo.Word, pList, getInfo.TotalDocs,
                                getInfo.DBProvider, getInfo.MaxReturnCount);
                        }
                    case Event.GetFilePositionList:
                        return ProcessGetFilePositionList(evt, flag, data);

                    case Event.MergeAck:
                        ProcessMergeAck(evt, flag, data);
                        break;

                }

            }
            catch (Exception e)
            {
                Global.Report.WriteErrorLog(string.Format("Index File Proxy Fail! Event={0}", ((Event)evt).ToString()), e);

                throw e;
            }

            return null;
        }


        public IndexFileProxy(string path, string fieldName, Hubble.Core.Data.Field.IndexMode indexMode)
            : this(path, fieldName, false, indexMode)
        {

        }

        public IndexFileProxy(string path, string fieldName, bool rebuild, Hubble.Core.Data.Field.IndexMode indexMode)
            : base()
        {
            _IndexMode = indexMode;
            //OnMessageEvent = ProcessMessage;
            _IndexFile = new IndexFile(path, this);
            _IndexFile.Create(fieldName, rebuild, indexMode);

            //this.Start();
        }

        //public void AddDocInfos(List<IndexFile.DocInfo> docInfos)
        //{
        //}

        public MergeInfos GetMergeInfos(Data.OptimizationOption option)
        {
            if (!System.Threading.Monitor.TryEnter(_LockObj, Timeout))
            {
                if (_NeedClose)
                {
                    return null;
                }

                throw new TimeoutException();
            }

            try
            {
                return (MergeInfos)ProcessGetFilePositionList((int)Event.GetFilePositionList, MessageQueue.MessageFlag.None, option);
            }
            finally
            {
                System.Threading.Monitor.Exit(_LockObj);
            }



            //return SSendMessage((int)Event.GetFilePositionList,
            //    option, 30 * 1000) as MergeInfos;
        }

        public void DoMergeAck(MergeAck mergeAck)
        {
            if (!System.Threading.Monitor.TryEnter(_LockObj, Timeout))
            {
                if (_NeedClose)
                {
                    return;
                }

                throw new TimeoutException();
            }

            try
            {
                ProcessMergeAck((int)Event.MergeAck, MessageQueue.MessageFlag.None, mergeAck);
            }
            finally
            {
                System.Threading.Monitor.Exit(_LockObj);
            }

            //SSendMessage((int)Event.MergeAck, mergeAck, 300 * 1000); //time out 5 min
        }

        public void AddWordPositionAndDocumentPositionList(string word,
            List<Entity.DocumentPositionList> docList)
        {
            if (!System.Threading.Monitor.TryEnter(_LockObj, Timeout))
            {
                if (_NeedClose)
                {
                    return;
                }

                throw new TimeoutException();
            }

            try
            {
                _IndexFile.AddWordAndDocList(word, docList);
            }
            finally
            {
                System.Threading.Monitor.Exit(_LockObj);
            }

            //ASendMessage((int)Event.Add, new WordDocList(word, docList));
        }


        public Hubble.Core.Index.InvertedIndex.WordIndexReader GetWordIndex(GetInfo getInfo)
        {
            if (!System.Threading.Monitor.TryEnter(_LockObj, Timeout))
            {
                if (_NeedClose)
                {
                    return null;
                }

                throw new TimeoutException();
            }

            try
            {
                WordFilePositionList pList = GetFilePositionListByWord(getInfo.Word);

                if (pList == null)
                {
                    return null;
                }

                return _IndexFile.GetWordIndex(getInfo.Word, pList, getInfo.TotalDocs,
                    getInfo.DBProvider, getInfo.MaxReturnCount);
            }
            finally
            {
                System.Threading.Monitor.Exit(_LockObj);
            }

            //return SSendMessage((int)Event.Get, getInfo, 30 * 1000) as
            //    Hubble.Core.Index.InvertedIndex.WordIndexReader;

            //lock (_LockObj)
            //{
            //    WordFilePositionList pList = GetFilePositionListByWord(getInfo.Word);
            //    return _IndexFile.GetWordIndex(getInfo.Word, pList, getInfo.TotalDocs,
            //        getInfo.DBProvider, getInfo.TabIndex);

            //}
        }

        public void Collect()
        {
            if (!System.Threading.Monitor.TryEnter(_LockObj, Timeout))
            {
                if (_NeedClose)
                {
                    return;
                }

                throw new TimeoutException();
            }

            try
            {
                _IndexFile.Collect();

                PatchWordFilePositionTable(_IndexFile.WordFilePositionList);
                _IndexFile.ClearWordFilePositionList();
            }
            finally
            {
                System.Threading.Monitor.Exit(_LockObj);
            }

            //ASendMessage((int)Event.Collect, null);
        }

        internal void SafelyClose()
        {
            System.Threading.Monitor.Enter(_LockObj);

            _NeedClose = true;

            System.Threading.Monitor.Exit(_LockObj);
        }

               
        public List<string> InnerLike(string str, InnerLikeType type)
        {
            System.Threading.Monitor.Enter(_LockObj);
            try
            {
                return _WordFilePositionTable.InnerLike(str, type);
            }
            finally
            {
                System.Threading.Monitor.Exit(_LockObj);
            }
        }


        internal void Close(int millisecondsTimeout)
        {
            //base.Close(millisecondsTimeout);

            _WordFilePositionTable.Clear();
            _IndexFile.Close();
            _IndexFile = null;
            GC.Collect();
        }

        public string GetHeadFileName(int serialNo)
        {
            return _IndexFile.GetHeadFileName(serialNo);
        }

        public string GetIndexFileName(int serialNo)
        {
            return _IndexFile.GetIndexFileName(serialNo);
        }

        #region IndexFileInit Members

        public void ImportWordFilePositionList(List<IndexFile.WordFilePosition> wordFilePositionList)
        {
            PatchWordFilePositionTable(wordFilePositionList);
        }

        public void CollectWordFilePositionList()
        {
            _WordFilePositionTable.Collect();
        }

        #endregion

    }

}
