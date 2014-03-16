using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASyncLib;

namespace AsyncTest
{
    [TestClass]
    public class MurmurHashTest
    {
        [TestMethod]
        public void VerifyMurmurHash()
        {
            var hFunc = new MurmurHash3_x86_32();
            var x = VerificationTest(hFunc, 32, 0xB0F57EE3, true); // For MurmurHash3_x86_32
            Assert.IsTrue(x);

            var hFunc128 = new MurmurHash3_x64_128();
            var y = VerificationTest(hFunc128, 128, 0x6384BA69, true); // For MurmurHash3_x64_128
            Assert.IsTrue(y);
        }

        private static bool VerificationTest(dynamic hash, int hashbits, UInt32 expected, bool verbose)
        {
            int hashbytes = hashbits / 8;

            byte[] key = new byte[256];
            byte[] hashes = new byte[hashbytes * 256];

            // Hash keys of the form {0}, {0,1}, {0,1,2}... up to N=255,using 256-N as
            // the seed
            for (int i = 0; i < 256; i++)
            {
                key[i] = (byte)i;
                hash.Seed = (uint)(256 - i);
                var ret = hash.ComputeHash(key, 0, i);
                Array.Copy(ret, 0, hashes, i * hashbytes, hashbytes);
            }

            // Then hash the result array
            hash.Seed = 0;
            var final = hash.ComputeHash(hashes, 0, hashbytes * 256);

            // The first four bytes of that hash, interpreted as a little-endian integer, is our
            // verification value

            UInt32 verification = (UInt32)((final[0] << 0) | (final[1] << 8) | (final[2] << 16) | (final[3] << 24));

            //----------

            if (expected != verification)
            {
                if (verbose)
                {
                    Console.WriteLine("Verification value {0} : Failed! (Expected {1})", verification, expected);
                }
                return false;
            }
            else
            {
                if (verbose)
                {
                    Console.WriteLine("Verification value {0} : Passed!", verification);
                }
                return true;
            }
        }
    }
}
