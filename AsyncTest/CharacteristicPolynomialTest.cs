using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASync;
using System.Collections.Generic;

namespace AsyncTest
{
    [TestClass]
    public class CharacteristicPolynomialTest
    {
        CharacteristicPolynomial _cp;

        [TestInitialize]
        public void Setup()
        {
            _cp = new CharacteristicPolynomial();
        }

        [TestMethod]
        public void CPTestCorrect1()
        {
            var sa = new List<int> { 1, 2, 9, 12, 33 };
            var sb = new List<int> { 1, 2, 9, 10, 12, 28 };
            var xVal = new List<int> { -1, -2, -3, -4, -5 };
            var f = 97;

            var cpa = _cp.Calc(sa, xVal, f);
            var cpb = _cp.Calc(sb, xVal, f);
            var cpaocpb = _cp.Div(cpa, cpb, f);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                f, sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p, f);
            var qFactors = _cp.Factoring(q, f);

            CollectionAssert.AreEqual(new List<int> { 58, 19, 89, 77, 4 }, cpa);
            CollectionAssert.AreEqual(new List<int> { 15, 54, 68, 77, 50 }, cpb);
            CollectionAssert.AreEqual(new List<int> { 75, 74, 17, 1, 35 }, cpaocpb);
            // TODO: assert p, q, factors
        }

        [TestMethod]
        public void CPTestCorrect2()
        {
            var sa = new List<int> { 1, 2, 5, 11, 19 };
            var sb = new List<int> { 1, 5, 7, 11 };
            var xVal = new List<int> { -1, -2, -3, -4 };
            var f = 67;

            var cpa = _cp.Calc(sa, xVal, f);
            var cpb = _cp.Calc(sb, xVal, f);
            var cpaocpb = _cp.Div(cpa, cpb, f);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                f, sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p, f);
            var qFactors = _cp.Factoring(q, f);

            CollectionAssert.AreEqual(new List<int> { 3, 49, 32, 47 }, cpa);
            CollectionAssert.AreEqual(new List<int> { 13, 45, 58, 55 }, cpb);
            CollectionAssert.AreEqual(new List<int> { 26, 13, 56, 24 }, cpaocpb);
            // TODO: assert p, q, factors
        }
    }
}
