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
            var est = new StrataEstimator();

            var _clientDic = DataGen.Gen(1000, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var _serverDic = DataGen.Gen(1000, 4).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            

            est.Encode(_clientDic);

            var serverEst = new StrataEstimator();
            serverEst.Encode(_serverDic);

            var diff = serverEst - est;

            var n = diff.Estimate();
            Assert.IsTrue(n > 0);
        }
    }
}
