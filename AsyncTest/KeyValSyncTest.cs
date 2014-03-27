﻿using System;
using ASyncLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace AsyncTest
{
    [TestClass]
    public class KeyValSyncTest
    {
        [TestInitialize]
        public void Init()
        {
            _clientDic = new Dictionary<string, string>();
            _serverDic = new Dictionary<string, string>();
            _bffile = new MemoryStream();
            _p1file = new MemoryStream();
            _ibffile = new MemoryStream();
            _p2file = new MemoryStream();
        }
        Dictionary<string, string> _clientDic;
        Dictionary<string, string> _serverDic;
        MemoryStream _bffile;
        MemoryStream _p1file;
        MemoryStream _ibffile;
        MemoryStream _p2file;

        [TestMethod]
        public void KeyValSyncTest1()
        {
            for (var i = 0; i < 20; ++i)
            {
                _clientDic.Add(i.ToString(), i.ToString());
            }
            for (var i = 0; i < 25; ++i)
            {
                _serverDic.Add(i.ToString(), i.ToString());
            }

            KeyValSync.ClientGenBfFile(_clientDic, _clientDic.Count, _bffile);
            _bffile.Position = 0;
            KeyValSync.ServerGenPatch1File(_serverDic, _serverDic.Count, _bffile, _p1file);
            _p1file.Position = 0;
            KeyValSync.ClientPatchAndGenIBFFile(_clientDic, _p1file, _ibffile);
            _ibffile.Position = 0;
            KeyValSync.ServerGenPatch2FromIBF(_serverDic, _ibffile, _p2file);
            _p2file.Position = 0;
            KeyValSync.ClientPatch(_clientDic, _p2file);

            CollectionAssert.AreEquivalent(_clientDic, _serverDic);
        }

        [TestMethod]
        public void KeyValSyncTest2()
        {
            for (var i = 0; i < 200; ++i)
            {
                _clientDic.Add(i.ToString(), i.ToString());
            }
            for (var i = 0; i < 250; ++i)
            {
                _serverDic.Add(i.ToString(), i.ToString());
            }

            KeyValSync.ClientGenBfFile(_clientDic, _clientDic.Count, _bffile);
            _bffile.Position = 0;
            KeyValSync.ServerGenPatch1File(_serverDic, _serverDic.Count, _bffile, _p1file);
            _p1file.Position = 0;
            KeyValSync.ClientPatchAndGenIBFFile(_clientDic, _p1file, _ibffile);
            _ibffile.Position = 0;
            KeyValSync.ServerGenPatch2FromIBF(_serverDic, _ibffile, _p2file);
            _p2file.Position = 0;
            KeyValSync.ClientPatch(_clientDic, _p2file);

            CollectionAssert.AreEquivalent(_clientDic, _serverDic);
        }
    }
}
