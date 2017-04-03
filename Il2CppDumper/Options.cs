using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppDumper
{
    public class Options
    {
        private static Options instance = null;

        public Options()
        {
            instance = this;
        }

        public static Options GetOptions() { return instance; }

        [Option('m', "metadata", DefaultValue = "global-metadata.dat", HelpText = "Metadata file.")]
        public string MetadataFile { get; set; }

        [Option('b', "binary", DefaultValue = "libil2cpp.so", HelpText = "Binary file.")]
        public string BinaryFile { get; set; }

        [Option("arm7", DefaultValue = false, HelpText = "Use arm7 for fat binary (default to arm64)")]
        public bool Arm7 { get; set; }
    }
}
