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
            ProcessFile("abc.txt");
        }

        static void ProcessFile(string filename)
        {
            var rollingHash = new BlockingCollection<uint>();
            var localMaximaPos = new BlockingCollection<int>();
            var partitionHash = new BlockingCollection<uint>();

            Task.Run(() =>
            {
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fh = new FileHash(1024);
                    fh.StreamToHashValues(fs, rollingHash);
                }
            });

            Task.Run(() =>
            {
                var lm = new LocalMaxima(1024);
                lm.CalcUsingBlockAlgo(rollingHash, localMaximaPos);
            });

            Task.Run(() =>
            {
                var mmh = new MurmurHash3_x86_32();
                var fph = new FileParitionHash(mmh);
                using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fph.ProcessStream(fs, localMaximaPos, partitionHash);
                }
            });

            foreach (var i in partitionHash.GetConsumingEnumerable())
            {
                Console.WriteLine("File par hash: {0}", i);
            }
        }

        private static void TestLM()
        {
            var inputList = new BlockingCollection<uint>();
            var outList = new BlockingCollection<int>();

            Task.Run(() =>
            {
                while (true)
                {
                    var str = Console.ReadLine();
                    var num = 0;
                    if (int.TryParse(str, out num))
                    {
                        inputList.Add((uint)num);
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