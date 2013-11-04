using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.Cryptography;
using ASync;

namespace AsyncTest
{
    [TestClass]
    public class IBFTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var hFuncs = new List<HashAlgorithm>
            {
                new MurmurHash3_x86_32() {Seed = 0},
                new MurmurHash3_x86_32() {Seed = 1},
                new MurmurHash3_x86_32() {Seed = 2},
                new MurmurHash3_x86_32() {Seed = 3},
                new MurmurHash3_x86_32() {Seed = 4}
            };

            var filter = new IBF(1000000, hFuncs);
            var numberOfValuesToAdd = 5000;
            for (int i = 0; i < numberOfValuesToAdd; i++)
            {
                var str = string.Format("Test {0}", i);
                var ba = System.Text.Encoding.UTF8.GetBytes(str);
                Assert.IsFalse(filter.Contains(i, ba, 0, ba.Length));
                filter.Add(i, ba, 0, ba.Length);
                Assert.IsTrue(filter.Contains(i, ba, 0, ba.Length));
            }
        }
    }
}
