using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace ASync
{
    struct FileChunk
    {
        public int Offset { get; set; }
        public int Length { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var inputList = new BlockingCollection<int>();
            var outList = new BlockingCollection<int>();

            Task.Run(() =>
            {
                while (true)
                {
                    var str = Console.ReadLine();
                    var num = 0;
                    if (int.TryParse(str, out num))
                    {
                        inputList.Add(num);
                    }
                    else
                    {
                        inputList.CompleteAdding();
                    }
                }
            });

            Task.Run(() =>
            {
                var lm = new LocalMaxima(2);
                lm.CalcUsingBlockAlgo(inputList, outList);
            });

            foreach (var i in outList.GetConsumingEnumerable())
            {
                Console.WriteLine("Local max pos: {0}", i);
            }
        }
    }
}