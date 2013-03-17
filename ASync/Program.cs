﻿using System;
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
    public class DataChunk<T>
    {
        const int BufferSize = 2;

        public T[] Data = new T[BufferSize];
        public int DataSize { get; private set; }

        public bool IsFull { get { return DataSize == BufferSize; } }

        public void Add(T item)
        {
            Data[DataSize] = item;
            ++DataSize;
        }
    }

    public class BlockingCollectionDataChunk<T>
    {
        public BlockingCollectionDataChunk()
        {
            BlockingCollection = new BlockingCollection<DataChunk<T>>();
            _currChunk = new DataChunk<T>();
        }

        public void Add(T item)
        {
            if (!_currChunk.IsFull)
            {
                _currChunk.Add(item);
                return;
            }
            // CurrChunk is full, need to create new one and add the current one to blocking collection
            BlockingCollection.Add(_currChunk);
            _currChunk = new DataChunk<T>();
            _currChunk.Add(item);
        }

        public void CompleteAdding()
        {
            BlockingCollection.Add(_currChunk);
            BlockingCollection.CompleteAdding();
            _currChunk = null;
        }

        public BlockingCollection<DataChunk<T>> BlockingCollection { get; private set; }
        private DataChunk<T> _currChunk;
    }

    class Program
    {
        static void Main(string[] args)
        {
            //ProcessFile("test.dat");

            LocalMaxima.StressTest();
        }

        static void ProcessFile(string filename)
        {
            var rollingHash = new BlockingCollectionDataChunk<uint>();
            var localMaximaPos = new BlockingCollectionDataChunk<int>();
            var partitionHash = new BlockingCollectionDataChunk<uint>();

            var sw = new Stopwatch();
            sw.Start();

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
                var lm = new LocalMaxima(512 * 1024);
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

            var count = 0;
            foreach (var i in partitionHash.BlockingCollection.GetConsumingEnumerable())
            {
                count += i.DataSize;
                //Console.WriteLine("File par hash: {0}", i);
            }
            sw.Stop();

            Console.WriteLine("Number of partitions: {0}", count);
            Console.WriteLine("Time: {0} ms", sw.ElapsedMilliseconds);
        }
    }
}