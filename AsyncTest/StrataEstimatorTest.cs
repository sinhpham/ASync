using System;
using ASyncLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace AsyncTest
{
    [TestClass]
    public class StrataEstimatorTest
    {
        [TestMethod]
        public void StrataTest1()
        {
            var clientEst = new StrataEstimator(true);

            var _clientDic = DataGen.Gen(1000, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var _serverDic = DataGen.Gen(1000, 4).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            

            clientEst.Encode(_clientDic);

            var serverEst = new StrataEstimator(true);
            serverEst.Encode(_serverDic);

            var diff = serverEst - clientEst;

            var n = diff.Estimate();
            Assert.IsTrue(n > 0);
        }
    }
}
