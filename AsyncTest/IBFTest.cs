using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.Cryptography;
using ASync;
using System.Linq;

namespace AsyncTest
{
    [TestClass]
    public class IBFTest
    {
        [TestMethod]
        public void TestContains()
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

        [TestMethod]
        public void TestDecode()
        {
            var hFuncs = new List<HashAlgorithm>
            {
                new MurmurHash3_x86_32() {Seed = 0},
                new MurmurHash3_x86_32() {Seed = 1},
                new MurmurHash3_x86_32() {Seed = 2},
                new MurmurHash3_x86_32() {Seed = 3},
                new MurmurHash3_x86_32() {Seed = 4}
            };

            var l1 = new List<int>(Enumerable.Range(1, 1000));
            var l2 = new List<int>(Enumerable.Range(1, 1010));

            var ibf1 = new IBF(30, hFuncs);
            var ibf2 = new IBF(30, hFuncs);

            foreach (var i in l1)
            {
                var bArr = BitConverter.GetBytes(i);
                ibf1.Add(i, bArr);
            }
            foreach (var i in l2)
            {
                var bArr = BitConverter.GetBytes(i);
                ibf2.Add(i, bArr);
            }

            var sub = ibf1.Substract(ibf2);

            var l1ml2 = new List<int>();
            var l2ml1 = new List<int>();
            var ret = sub.Decode(l1ml2, l2ml1);

            var expected = l2.Except(l1).ToList();
            expected.Sort();
            var actual = l2ml1;
            actual.Sort();

            CollectionAssert.AreEqual(expected, actual);
            Assert.IsTrue(ret);
        }

        [TestMethod]
        public void TestDecode2()
        {
            var hFuncs = new List<HashAlgorithm>
            {
                new MurmurHash3_x86_32() {Seed = 0},
                new MurmurHash3_x86_32() {Seed = 1},
                new MurmurHash3_x86_32() {Seed = 2},
                new MurmurHash3_x86_32() {Seed = 3},
                new MurmurHash3_x86_32() {Seed = 4}
            };

            var intList = new List<int>(Enumerable.Range(1, 1000000));

            var ibf = new IBF(3 * intList.Count, hFuncs);

            foreach (var i in intList)
            {
                var bArr = BitConverter.GetBytes(i);
                ibf.Add(i, bArr);
            }

            var l1ml2 = new List<int>();
            var l2ml1 = new List<int>();
            var ret = ibf.Decode(l1ml2, l2ml1);

            var expected = intList;
            expected.Sort();
            var actual = l1ml2;
            actual.Sort();

            Assert.IsTrue(ret);
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
