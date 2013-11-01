﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASync;
using System.Collections.Generic;

namespace AsyncTest
{
    [TestClass]
    public class CharacteristicPolynomialTest
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void CPTestCorrect1()
        {
            var _cp = new CharacteristicPolynomial(97);

            var sa = new List<int> { 1, 2, 9, 12, 33 };
            var sb = new List<int> { 1, 2, 9, 10, 12, 28 };
            var xVal = new List<int> { -1, -2, -3, -4, -5 };

            var cpa = _cp.Calc(sa, xVal);
            var cpb = _cp.Calc(sb, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p);
            var qFactors = _cp.Factoring(q);

            CollectionAssert.AreEqual(new List<int> { 58, 19, 89, 77, 4 }, cpa);
            CollectionAssert.AreEqual(new List<int> { 15, 54, 68, 77, 50 }, cpb);
            CollectionAssert.AreEqual(new List<int> { 75, 74, 17, 1, 35 }, cpaocpb);
            CollectionAssert.AreEqual(new List<int> { 64, 1 }, p);
            CollectionAssert.AreEqual(new List<int> { 86, 59, 1 }, q);
            CollectionAssert.AreEqual(new List<int> { 33 }, pFactors);
            CollectionAssert.AreEqual(new List<int> { 10, 28 }, qFactors);
        }

        [TestMethod]
        public void CPTestCorrect2()
        {
            var _cp = new CharacteristicPolynomial(67);

            var sa = new List<int> { 1, 2, 5, 11, 19 };
            var sb = new List<int> { 1, 5, 7, 11 };
            var xVal = new List<int> { -1, -2, -3, -4 };

            var cpa = _cp.Calc(sa, xVal);
            var cpb = _cp.Calc(sb, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p);
            var qFactors = _cp.Factoring(q);

            CollectionAssert.AreEqual(new List<int> { 3, 49, 32, 47 }, cpa);
            CollectionAssert.AreEqual(new List<int> { 13, 45, 58, 55 }, cpb);
            CollectionAssert.AreEqual(new List<int> { 26, 13, 56, 24 }, cpaocpb);
            CollectionAssert.AreEqual(new List<int> { 38, 46, 1 }, p);
            CollectionAssert.AreEqual(new List<int> { 60, 1 }, q);
            CollectionAssert.AreEqual(new List<int> { 2, 19 }, pFactors);
            CollectionAssert.AreEqual(new List<int> { 7 }, qFactors);
        }

        [TestMethod]
        public void CPTestCorrect3()
        {
            var _cp = new CharacteristicPolynomial(7);

            var sa = new List<int> { 1, 2 };
            var sb = new List<int> { 1, 3 };
            var xVal = new List<int> { -1, -2 };

            var cpa = _cp.Calc(sa, xVal);
            var cpb = _cp.Calc(sb, xVal);
            var cpaocpb = _cp.Div(cpa, cpb);

            List<int> p;
            List<int> q;
            _cp.Interpolate(cpaocpb, xVal,
                sa.Count - sb.Count,
                out p, out q);

            var pFactors = _cp.Factoring(p);
            var qFactors = _cp.Factoring(q);

            CollectionAssert.AreEqual(new List<int> { 6, 5 }, cpa);
            CollectionAssert.AreEqual(new List<int> { 1, 1 }, cpb);
            CollectionAssert.AreEqual(new List<int> { 6, 5 }, cpaocpb);
            //CollectionAssert.AreEqual(new List<int> { 2 }, p);
            //CollectionAssert.AreEqual(new List<int> { 60, 1 }, q);
            CollectionAssert.AreEqual(new List<int> { 2 }, pFactors);
            CollectionAssert.AreEqual(new List<int> { 3 }, qFactors);
        }
    }
}
