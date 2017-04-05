/*
 *  Copyright 2017 niico - https://github.com/pogosandbox/Il2CppDumper 
 *
 *  All rights reserved.
*/

#pragma warning disable 0649

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector.Readers
{
    // internal MachO classes
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

        internal const uint LC_SEGMENT = 1;
        internal const uint LC_SEGMENT_64 = 0x19;
    }

    internal class MachoHeader
    {
        // public uint magic;      /* mach magic number identifier */
        public int cputype;        /* cpu specifier */
        public int cpusubtype;     /* machine specifier */
        public uint filetype;      /* type of file */
        public uint ncmds;         /* number of load commands */
        public uint sizeofcmds;    /* the size of all the load commands */
        public uint flags;		   /* flags */
    }

    internal class MachoHeader64
    {
        // public uint magic;      /* mach magic number identifier */
        public int cputype;        /* cpu specifier */
        public int cpusubtype;     /* machine specifier */
        public uint filetype;      /* type of file */
        public uint ncmds;         /* number of load commands */
        public uint sizeofcmds;    /* the size of all the load commands */
        public uint flags;		   /* flags */
        public uint reserved;
    }

    internal class MachoLoadCommand
    {
        public uint cmd;       /* type of load command */
        public uint cmdsize;   /* total size of command in bytes */
    }

    internal class FatArch
    {
        public uint cputype;
        public uint cpusubtype;
        public uint offset;
        public uint size;
        public uint align;
    }

    // Our types
    internal class MachoSection
    {
        public string section_name;
        public uint address;
        public uint size;
        public uint offset;
        public uint end;
    }
}
