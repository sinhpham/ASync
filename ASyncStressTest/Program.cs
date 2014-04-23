using ASyncLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASyncStressTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var sArr = new int[] { 1000000, 500000, 100000};
            var changedArr = new int[] {  50, 20 , 4 };

            foreach (var size in sArr)
            {
                foreach (var changedPer in changedArr)
                {
                    Console.WriteLine("Testing ibf sync with {0} size and {1} percent change", size, changedPer);
                    var sw = new Stopwatch();
                    sw.Start();
                    var diff = StressTestStrataEstimator(size, changedPer);
                    sw.Stop();
                    Console.WriteLine("Done in {0}, diff = {1}", sw.Elapsed, diff);
                }
            }
        }

        static void StressTestIBFSync(int size, int changedPer)
        {
            var clientDic = DataGen.Gen(size, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var serverDic = DataGen.Gen(size, changedPer).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

            var _bffile = new MemoryStream();
            var _p1file = new MemoryStream();
            var _ibffile = new MemoryStream();
            var _p2file = new MemoryStream();

            KeyValSync.ClientGenBfFile(clientDic, clientDic.Count, _bffile);
            _bffile.Position = 0;
            KeyValSync.ServerGenPatch1File(serverDic, serverDic.Count, _bffile, _p1file);
            _p1file.Position = 0;
            using (var sr = new StreamReader(_p1file))
            {
                var d0 = int.Parse(sr.ReadLine());
                var patchItems = Helper.ReadLinesFromTextStream(sr).Select(str =>
                {
                    var strArr = str.Split(' ');
                    return new KeyValuePair<string, string>(strArr[0], strArr[1]);
                });
                KeyValSync.ClientPatchAndGenIBFFile(clientDic, currItem => clientDic[currItem.Key] = currItem.Value, patchItems, d0, _ibffile);
            }
            _ibffile.Position = 0;
            KeyValSync.ServerGenPatch2FromIBF(serverDic, key => serverDic[key], _ibffile, _p2file);
            _p2file.Position = 0;
            var patchDic = Serializer.Deserialize<Dictionary<string, string>>(_p2file);
            KeyValSync.ClientApplyPatch<string, string>(currItem => clientDic[currItem.Key] = currItem.Value, patchDic);

            if (!KeyValSync.AreTheSame(clientDic, serverDic))
            {
                throw new InvalidDataException();
            }
        }

        static void StressTestIBFWithoutEstimator(int size, int changedPer)
        {
            var clientDic = DataGen.Gen(size, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var serverDic = DataGen.Gen(size, changedPer).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

            var d0 = (int)(size / 100 * changedPer * 2);

            var _ibffile = new MemoryStream();
            var _p2file = new MemoryStream();

            KeyValSync.ClientGenIBF(clientDic, d0, _ibffile);
            _ibffile.Position = 0;
            KeyValSync.ServerGenPatch2FromIBF(serverDic, key => serverDic[key], _ibffile, _p2file);
            _p2file.Position = 0;
            var patchDic = Serializer.Deserialize<Dictionary<string, string>>(_p2file);
            KeyValSync.ClientApplyPatch<string, string>(currItem => clientDic[currItem.Key] = currItem.Value, patchDic);

            if (!KeyValSync.AreTheSame(clientDic, serverDic))
            {
                throw new InvalidDataException();
            }
        }

        static double StressTestBFEstimator(int size, int changedPer)
        {
            var clientDic = DataGen.Gen(size, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var serverDic = DataGen.Gen(size, changedPer).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

            var _bffile = new MemoryStream();
            

            KeyValSync.ClientGenBfFile(clientDic, clientDic.Count, _bffile);
            _bffile.Position = 0;

            var bf = Serializer.Deserialize<BloomFilter>(_bffile);

            bf.SetHashFunctions(BloomFilter.DefaultHashFuncs());

            var hitNum = 0;
            var hFunc = new MurmurHash3_x86_32();
            
            foreach (var item in serverDic)
            {
                var block = item.Key + "-" + item.Value;
                var bBlock = Helper.GetBytes(block);

                var hv = hFunc.ComputeHash(bBlock);
                if (!bf.Contains(hv))
                {
                    
                }
                else
                {
                    hitNum++;
                }
            }

            var estimatedDo = Helper.EstimateD0(bf.Count, serverDic.Count, hitNum, bf) + 20;
            var realD0 = changedPer * size / 100 * 1.5;
            var diff = (double)estimatedDo / realD0;
            return diff;
        }

        static double StressTestStrataEstimator(int size, int changedPer)
        {
            var clientDic = DataGen.Gen(size, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var serverDic = DataGen.Gen(size, changedPer).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

            var clientEst = new StrataEstimator();
            clientEst.Encode(clientDic);
            var serverEst = new StrataEstimator();
            serverEst.Encode(serverDic);

            var diffEst = serverEst - clientEst;


            var diff = diffEst.Estimate() ;
            return diff;
        }
    }
}
