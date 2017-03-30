using CommandLine;
using Il2CppInspector;
using NLog;
using System;
using System.IO;

namespace Il2CppDumper
{
    internal class Options
    {
        [Option('m', "metadata", DefaultValue = "global-metadata.dat", HelpText = "Metadata file.")]
        public string MetadataFile { get; set; }

        [Option('b', "binary", DefaultValue = "libil2cpp.so", HelpText = "Binary file.")]
        public string BinaryFile { get; set; }
    }

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
                logger.Error($"File {options.BinaryFile} does not exist. Exiting.");
                Environment.Exit(1);
            }
            if (!File.Exists(options.MetadataFile))
            {
                logger.Error($"File {options.MetadataFile} does not exist. Exiting.");
                Environment.Exit(1);
            }

            try
            {
                logger.Info("Load data from files...");
                var il2cpp = Il2CppProcessor.LoadFromFile(options.BinaryFile, options.MetadataFile);
                if (il2cpp == null)
                {
                    logger.Error("Unable to load data from files, exiting.");
                    Environment.Exit(1);
                }

                logger.Info("Writing pseudo code...");
                var dumper = new PseudoCodeDumper(il2cpp);
                dumper.DumpStrings("strings.txt");
                dumper.DumpToFile("pseudo.cs");

                logger.Info("Writing extracted protos...");
                var protoDumper = new ProtoDumper(il2cpp);
                protoDumper.DumpToFile("generated.proto");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            logger.Info("Done.");
        }
    }
}
