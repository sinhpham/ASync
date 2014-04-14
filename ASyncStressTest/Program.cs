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
            var sArr = new int[] { 2000000, 200000, 20000 };
            var changedArr = new int[] {  50, 20, 4 };

            foreach (var size in sArr)
            {
                foreach (var changedPer in changedArr)
                {
                    Console.WriteLine("Testing ibf sync with {0} size and {1} percent change", size, changedPer);
                    var sw = new Stopwatch();
                    sw.Start();
                    StressTestIBFWithoutEstimator(size, changedPer);
                    sw.Stop();
                    Console.WriteLine("Done in {0}", sw.Elapsed);
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
    }
}
