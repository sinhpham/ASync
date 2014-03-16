using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.Cryptography;
using ASyncLib;
using System.Linq;

namespace AsyncTest
{
    [TestClass]
    public class IBFTest
    {
        [TestMethod]
        public void TestContains()
        {
            var hFuncs = new List<IHashFunc>
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
                Assert.IsFalse(filter.Contains(i));
                filter.Add(i);
                Assert.IsTrue(filter.Contains(i));
            }
        }

        [TestMethod]
        public void TestDecode()
        {
            var hFuncs = new List<IHashFunc>
            {
                new MurmurHash3_x86_32() {Seed = 0},
                new MurmurHash3_x86_32() {Seed = 1},
                new MurmurHash3_x86_32() {Seed = 2},
                new MurmurHash3_x86_32() {Seed = 3},
                new MurmurHash3_x86_32() {Seed = 4}
            };
            
            var l1 = new List<long>();
            var l2 = new List<long>();
            for (var i = 0; i < 1000; ++i)
            {
                l1.Add((long)i);
            }
            for (var i = 0; i < 1010; ++i)
            {
                l2.Add((long)i);
            }

            var ibf1 = new IBF(30, hFuncs);
            var ibf2 = new IBF(30, hFuncs);

            foreach (var i in l1)
            {
                ibf1.Add(i);
            }
            foreach (var i in l2)
            {
                ibf2.Add(i);
            }

            var sub = ibf1 - ibf2;

            var l1ml2 = new List<long>();
            var l2ml1 = new List<long>();
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
            var hFuncs = new List<IHashFunc>
            {
                new MurmurHash3_x86_32() {Seed = 0},
                new MurmurHash3_x86_32() {Seed = 1},
                new MurmurHash3_x86_32() {Seed = 2},
                new MurmurHash3_x86_32() {Seed = 3},
                new MurmurHash3_x86_32() {Seed = 4}
            };

            var intList = new List<long>();
            for (var i = 0; i < 100000; ++i)
            {
                intList.Add((long)i);
            }

            var ibf = new IBF(3 * intList.Count, hFuncs);

            foreach (var i in intList)
            {
                ibf.Add(i);
            }

            var l1ml2 = new List<long>();
            var l2ml1 = new List<long>();
            var ret = ibf.Decode(l1ml2, l2ml1);

            var expected = intList;
            expected.Sort();
            var actual = l1ml2;
            actual.Sort();

            Assert.IsTrue(ret);
            CollectionAssert.AreEqual(expected.Cast<long>().ToList(), actual);
        }
    }
}
