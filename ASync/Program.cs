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
            Sync("testdata/50000-clientDic.dat", "testdata/50000-4changed-serverDic.dat");
        }

        static void Sync(string clientFile, string serverFile)
        {
            var clientDic = new Dictionary<string, string>();
            var serverDic = new Dictionary<string, string>();

            using (var f = File.OpenRead(clientFile))
            {
                clientDic = Serializer.Deserialize<Dictionary<string, string>>(f);
            }

            using (var f = File.OpenRead(serverFile))
            {
                serverDic = Serializer.Deserialize<Dictionary<string, string>>(f);
            }

            var bffile = "bfibf-bf.dat";
            var p1file = "bfibf-p1.dat";
            var ibfFile = "bfibf-ibf.dat";
            var p2file = "bfibf-p2.dat";
            KeyValSync.ClientGenBfFile(clientDic, bffile);
            KeyValSync.ServerGenPatch1File(serverDic, bffile, p1file);
            KeyValSync.ClientPatchAndGenIBFFile(clientDic, p1file, ibfFile);
            KeyValSync.ServerGenPatch2FromIBF(serverDic, ibfFile, p2file);
            KeyValSync.ClientPatch(clientDic, p2file);


            if (!KeyValSync.AreTheSame(clientDic, serverDic))
            {
                throw new InvalidDataException();
            }
        }
    }
}