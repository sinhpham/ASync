using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using ASync;

namespace AsyncTest
{
    [TestClass]
    public class LocalMaximaTest
    {
        [TestMethod]
        public void LocalMaximaTestCorrectness1()
        {
            var list = new List<int> {
                9, 21, 19,
                13, 27, 18,
                26, 25, 24,
                16, 35, 35,
                37, 20, 40,
                45, 10, 10,
                10, 10, 20,
                10, 20, 30,
                10, 35, 40,
                10, 10, 10,
                10, 20
            };
            var lm = new LocalMaxima(2);
            var ret = lm.LocalMaxima2(list);
            var pos = ret.Select(kvp => kvp.Key);

            CollectionAssert.AreEqual(new List<int> { 1, 4, 15, 26, 31 }, pos.ToList());
        }

        [TestMethod]
        public void LocalMaximaTestCorrectness2()
        {
            var list = new List<int> { 1, 1, 10, 1, 1, 1, 1, 10, 10 };
            var lm = new LocalMaxima(5);
            var ret = lm.LocalMaxima2(list);
            var pos = ret.Select(kvp => kvp.Key);

            CollectionAssert.AreEqual(new List<int> {  }, pos.ToList());
        }
    }
}
