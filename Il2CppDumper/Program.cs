﻿using Il2CppDumper.Dumpers;
using Il2CppInspector;
using NLog;
using System;
using System.IO;

namespace Il2CppDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            NLog.LayoutRenderers.LayoutRenderer.Register("current-dir", (logEvent) => Directory.GetCurrentDirectory());
            Logger logger = LogManager.GetCurrentClassLogger();

            logger.Info("Starting Il2CppDumper...");
            logger.Info("Current directory: {0}", Directory.GetCurrentDirectory());

            var options = new Options();
            CommandLine.Parser.Default.ParseArguments(args, options);

            // Check files
            if (!File.Exists(options.BinaryFile))
            {
                logger.Error($"File {options.BinaryFile} does not exist.");
            }
            else if (!File.Exists(options.MetadataFile))
            {
                logger.Error($"File {options.MetadataFile} does not exist.");
            }
            else
            {
                logger.Info("Load data from files...");
                var il2cpp = Il2CppProcessor.LoadFromFile(options.BinaryFile, options.MetadataFile);
                if (il2cpp == null)
                {
                    logger.Error("Unable to load data from files.");
                }
                else
                {
                    logger.Info("Writing pseudo code...");
                    var dumper = new PseudoCodeDumper(il2cpp);
                    dumper.DumpStrings("strings.txt");
                    dumper.DumpToFile("pseudo.cs");

                    logger.Info("Writing structs...");
                    var structOffsets = new StructDumper(il2cpp);
                    structOffsets.DumpToFile("structs.h");

                    logger.Info("Writing methods offsets...");
                    var methodsOffsets = new MethodsOffsetsDumper(il2cpp);
                    methodsOffsets.DumpToFile("methods.txt");

                    logger.Info("Writing extracted protos...");
                    var protoDumper = new ProtoDumper(il2cpp);
                    protoDumper.DumpToFile("generated.proto");
                }
            }
            logger.Info("Done.");

            Console.WriteLine("Press a key to terminate.");
            Console.ReadKey();
        }
    }
}
