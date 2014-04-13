using ASyncLib;
using System;
using System.Collections.Generic;
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
            StressTestIBFSync(20000);
        }

        static void StressTestIBFSync(int size)
        {
            var clientDic = DataGen.Gen(size, 0).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);
            var serverDic = DataGen.Gen(size, 50).ToDictionary(currItem => currItem.Key, currItem => currItem.Value);

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
            KeyValSync.ClientPatch<string, string>(currItem => clientDic[currItem.Key] = currItem.Value, _p2file);

            if (!KeyValSync.AreTheSame(clientDic, serverDic))
            {
                throw new InvalidDataException();
            }
        }
    }
}
