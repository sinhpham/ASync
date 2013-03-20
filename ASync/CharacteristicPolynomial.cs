﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASync
{
    public class CharacteristicPolynomial
    {
        public CharacteristicPolynomial(int fieldOrder)
        {
            _fieldOrder = fieldOrder;
        }

        readonly int _fieldOrder;
        public int FieldOrder { get { return _fieldOrder; } }

        public List<int> Calc(IEnumerable<int> set, IEnumerable<int> xValues)
        {
            var ret = new List<int>();
            foreach (var xVal in xValues)
            {
                var currValue = 1;
                foreach (var item in set)
                {
                    currValue = (currValue * (xVal - item)) % FieldOrder;
                }
                currValue += currValue < 0 ? FieldOrder : 0;
                ret.Add(currValue);
            }
            return ret;
        }

        // Division of two polynomials over finite field
        public List<int> Div(List<int> cpa, List<int> cpb)
        {
            // Calculate CPa/CPb.
            var ret = new List<int>();
            for (var i = 0; i < cpa.Count; ++i)
            {
                var currRet = DivGF(cpa[i], cpb[i]);
                currRet += currRet < 0 ? FieldOrder : 0;
                ret.Add(currRet);
            }
            return ret;
        }

        // Factoring polynomial over finite field
        // coeff[0] is the constant
        public List<int> Factoring(List<int> coeff)
        {
            var arr = coeff.ToArray();

            var rSize = 0;
            var rPointer = Factoring(arr, coeff.Count, FieldOrder, out rSize);

            var retArray = new int[rSize];
            Marshal.Copy(rPointer, retArray, 0, rSize);
            DeleteArrPtr(rPointer);

            var ret = new List<int>(retArray);
            return ret;
        }

        // Interpolating rational polynomial over finite field
        public void Interpolate(List<int> rf, List<int> xValues, int d,
            out List<int> P, out List<int> Q)
        {
            var pSize = 0;
            var qSize = 0;

            var retPtr = Interpolate(rf.ToArray(), rf.Count, xValues.ToArray(), xValues.Count, FieldOrder, d, out pSize, out qSize);

            var pArr = new int[pSize];
            var qArr = new int[qSize];
            Marshal.Copy(retPtr, pArr, 0, pSize);
            Marshal.Copy(retPtr + pSize * 4, qArr, 0, qSize);

            P = new List<int>(pArr);
            Q = new List<int>(qArr);

            DeleteArrPtr(retPtr);
        }

        private int DivGF(int a, int b)
        {
            // Division over finite field 
            // p should be a prime number
            if (b == 0)
            {
                // TODO: handle div 0
                return 0;
            }

            var ib = InversionGF(b);
            return (a * ib) % FieldOrder;
        }

        private int InversionGF(int a)
        {
            // Inversion over finite field 
            // p should be a prime number
            var y1 = 1;
            var y2 = 0;
            var currP = FieldOrder;

            while (a != 1)
            {
                var q = currP / a;

                var nextA = currP - q * a;
                var nextP = a;
                var nextY2 = y1;
                var nextY1 = y2 - ((q * y1) % FieldOrder);
                nextY1 += nextY1 < 0 ? FieldOrder : 0;

                a = nextA;
                currP = nextP;
                y1 = nextY1;
                y2 = nextY2;
            }
            return y1;
        }

        [DllImport("libs/NTLLib.dll")]
        private static extern IntPtr Factoring(int[] array, int size, int maxFieldValue, out int returnArrSize);

        [DllImport("libs/NTLLib.dll")]
        private static extern IntPtr Interpolate(int[] rfArr, int rfArrSize, int[] sampleArr, int sampleArrSize,
            int maxFieldValue, int d,
            out int retPSize, out int retQSize);

        [DllImport("libs/NTLLib.dll")]
        private static extern void DeleteArrPtr(IntPtr ptr);
    }
}
