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
            var positions = new BlockingCollection<int>();
            var outHashValues = new BlockingCollection<uint>();

            Task.Run(() =>
            {
                while (true)
                {
                    var str = Console.ReadLine();
                    var num = 0;
                    if (int.TryParse(str, out num))
                    {
                        positions.Add(num);
                    }
                    else
                    {
                        positions.CompleteAdding();
                    }
                }
            });
            Task.Run(() =>
            {
                foreach (var i in outHashValues.GetConsumingEnumerable())
                {
                    Console.WriteLine("Hash values: {0}", i);
                }
            });

            var mmh = new MurmurHash3_x86_32();
            var fph = new FileParitionHash(mmh);
            

            var strStream = "abcdef";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(strStream)))
            {
                fph.ProcessStream(ms, positions, outHashValues);
            }

            var h1 = BitConverter.ToUInt32(mmh.ComputeHash(Encoding.UTF8.GetBytes("abc"), 0, 3), 0);
            var h2 = BitConverter.ToUInt32(mmh.ComputeHash(Encoding.UTF8.GetBytes("def"), 0, 3), 0);
        }

        private static void TestLM()
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