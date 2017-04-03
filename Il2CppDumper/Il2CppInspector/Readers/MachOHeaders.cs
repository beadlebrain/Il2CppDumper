#pragma warning disable 0649

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector.Readers
{
    internal class MachOConstants
    {
        internal const uint MH_MAGIC = 0xFEEDFACE;
        internal const uint MH_CIGAM = 0xCEFAEDFE;

        internal const uint MH_MAGIC_64 = 0xFEEDFACF;
        internal const uint MH_CIGAM_64 = 0xCFFAEDFE;
        
        internal const uint FAT_MAGIC = 0xCAFEBABE;
        internal const uint FAT_CIGAM = 0xBEBAFECA;

        internal const uint CPU_TYPE_ARM = 12;
        internal const uint CPU_ARCH_ABI64 = 0x1000000;
        internal const uint CPU_TYPE_ARM64 = CPU_ARCH_ABI64 | CPU_TYPE_ARM;
    }

    internal class MachoSection
    {
        public string section_name;
        public uint address;
        public uint size;
        public uint offset;
        public uint end;
    }

    internal class FatArch
    {
        public uint cputype;
        public uint cpusubtype;
        public uint offset;
        public uint size;
        public uint align;
    }
}
