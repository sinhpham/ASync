using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASyncLib
{
    public static class DataGen
    {
        public static IEnumerable<KeyValuePair<string, string>> Gen(int size, int changedPercent)
        {
            var hFunc = new MurmurHash3_x64_128();
            var modifiedPercent = changedPercent / 2;
            var addedPercent = changedPercent / 2;
            var modifiedCount = modifiedPercent * size / 100;
            var addedCount = addedPercent * size / 100;

            // Modified
            for (var i = 0; i < modifiedCount; ++i)
            {
                var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(-i - 1)));
                var valStr = str + str;
                yield return new KeyValuePair<string, string>(i.ToString(), valStr);
            }

            // Original + added
            for (var i = modifiedCount; i < size + addedCount; ++i)
            {
                var str = BitConverter.ToString(hFunc.ComputeHash(BitConverter.GetBytes(i)));
                var valStr = str + str;
                yield return new KeyValuePair<string, string>(i.ToString(), valStr);
            }
        }
    }
}
