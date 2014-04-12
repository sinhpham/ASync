using System;
using ASyncLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace AsyncTest
{
    [TestClass]
    public class StrataEstimatorTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var est = new StrataEstimator();

            var _clientDic = new Dictionary<string, string>();
            var _serverDic = new Dictionary<string, string>();
            for (var i = 0; i < 2000; ++i)
            {
                _clientDic.Add(i.ToString(), i.ToString());
            }
            for (var i = 0; i < 2005; ++i)
            {
                _serverDic.Add(i.ToString(), i.ToString());
            }

            est.Encode(_clientDic);

            var serverEst = new StrataEstimator();
            serverEst.Encode(_serverDic);

            var diff = est - serverEst;

            var n = diff.Estimate();
            Assert.IsTrue(true);
        }
    }
}
