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
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using ProtoBuf;
using NLog;

namespace ASync
{

    [ProtoContract]
    public class PatchData
    {
        [ProtoMember(1)]
        public int HashValue { get; set; }
        [ProtoMember(2)]
        public byte[] Data { get; set; }
    }

    public class FileChunkInfo
    {
        public int Pos { get; set; }
        public int Length { get; set; }
    }

    class Options
    {
        [VerbOption("genbf", HelpText = "Generate Bloom Filter file")]
        public BloomFilterSubOptions GenBFVerb { get; set; }

        [VerbOption("gencp", HelpText = "Generate characteristic polynomial file")]
        public CharacteristicPolynomialSubOptions GenCPVerb { get; set; }

        [VerbOption("gend", HelpText = "Generate delta file")]
        public DeltaFileSubOptions GenDVerb { get; set; }

        [VerbOption("patch", HelpText = "Patch file")]
        public PatchFileSubOptions PatchVerb { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }

        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }

    class BloomFilterSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - new file", Required = true)]
        public string Input { get; set; }
        [Option('o', "bffile", HelpText = "Output bloom filter file name", Required = true)]
        public string BFFile { get; set; }
    }

    class CharacteristicPolynomialSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - old file", Required = true)]
        public string Input { get; set; }
        [Option('b', "bffile", HelpText = "Bloom filter file name", Required = true)]
        public string BFFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
        [Option('a', "addx", HelpText = "Additional x values", Required = false)]
        public int AdditionalXVal { get; set; }
    }

    class DeltaFileSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - new file", Required = true)]
        public string Input { get; set; }
        [Option('c', "cpfile", HelpText = "Characteristic Polynomial file name", Required = true)]
        public string CPFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }

    class PatchFileSubOptions
    {
        [Option('i', "input", HelpText = "Input file name - old file", Required = true)]
        public string Input { get; set; }
        [Option('d', "deltafile", HelpText = "Delta file name", Required = true)]
        public string DeltaFile { get; set; }
        [Option('o', "ouputFile", HelpText = "Output file name", Required = true)]
        public string Ouput { get; set; }
    }

    [ProtoContract]
    class CPData
    {
        [ProtoMember(1)]
        public int SetCount { get; set; }
        [ProtoMember(2)]
        public List<int> CPValues { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var clientDic = new Dictionary<string, string>();
            for (var i = 0; i < 10000; ++i)
            {
                clientDic.Add(i.ToString(), i.ToString());
            }

            var serverDic = new Dictionary<string, string>();
            for (var i = 0; i < 15000; ++i)
            {
                serverDic.Add(i.ToString(), (i).ToString());
            }


            using (var f = File.Create("clientDic.dat"))
            {
                Serializer.Serialize(f, clientDic);
            }

            using (var f = File.Create("serverDic.dat"))
            {
                Serializer.Serialize(f, serverDic);
            }

            //var chfn = "clienthashvalues.dat";
            //var pfn = "patch.dat";

            //KeyValSyncNaive.ClientGenHashFile(clientDic, chfn);
            //KeyValSyncNaive.ServerGenPatchFile(serverDic, chfn, pfn);
            //KeyValSyncNaive.ClientPatch(clientDic, pfn);

            var bffile = "bfsr-bf.dat";
            var p1file = "bfsr-p1.dat";
            var cpfile = "bfsr-cp.dat";
            var p2file = "bfsr-p2.dat";
            KeyValSync.ClientGenBfFile(clientDic, bffile);
            KeyValSync.ServerGenPatch1File(serverDic, bffile, p1file);
            KeyValSync.ClientPatchAndGenCPFile(clientDic, p1file, cpfile);
            KeyValSync.ServerGenPatch2(serverDic, cpfile, p2file);
            KeyValSync.ClientPatch(clientDic, p2file);

            //KeyValSync.SyncDic(clientDic, serverDic);

            if (!KeyValSync.AreTheSame(clientDic, serverDic))
            {
                throw new InvalidDataException();
            }

        }
    }
}