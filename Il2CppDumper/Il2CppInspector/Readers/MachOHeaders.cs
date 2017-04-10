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

    internal class MachosSegmentCommand
    {
        [String(IsNullTerminated=false, FixedSize=16)]
        public string segname;  /* segment name */
        public uint vmaddr;     /* memory address of this segment */
        public uint vmsize;     /* memory size of this segment */
        public uint fileoff;    /* file offset of this segment */
        public uint filesize;   /* amount to map from the file */
        public int maxprot;     /* maximum VM protection */
        public int initprot;    /* initial VM protection */
        public uint nsects;     /* number of sections in segment */
        public uint flags;		/* flags */
    }

    internal class MachosSegmentCommand64
    {
        [String(IsNullTerminated = false, FixedSize = 16)]
        public string segname;  /* segment name */
        public ulong vmaddr;     /* memory address of this segment */
        public ulong vmsize;     /* memory size of this segment */
        public ulong fileoff;    /* file offset of this segment */
        public ulong filesize;   /* amount to map from the file */
        public int maxprot;     /* maximum VM protection */
        public int initprot;    /* initial VM protection */
        public uint nsects;     /* number of sections in segment */
        public uint flags;		/* flags */
    }

    internal class MachoSection
    {
        [String(IsNullTerminated = false, FixedSize = 16)]
        public string sectname;  /* name of this section */
        [String(IsNullTerminated = false, FixedSize = 16)]
        public string segname;   /* segment this section goes in */
        public uint addr;        /* memory address of this section */
        public uint size;        /* size in bytes of this section */
        public uint offset;      /* file offset of this section */
        public uint align;       /* section alignment (power of 2) */
        public uint reloff;      /* file offset of relocation entries */
        public uint nreloc;      /* number of relocation entries */
        public uint flags;       /* flags (section type and attributes)*/
        public uint reserved1;   /* reserved */
        public uint reserved2;   /* reserved */
    }

    internal class MachoSection64
    {
        [String(IsNullTerminated = false, FixedSize = 16)]
        public string sectname;  /* name of this section */
        [String(IsNullTerminated = false, FixedSize = 16)]
        public string segname;   /* segment this section goes in */
        public ulong addr;        /* memory address of this section */
        public ulong size;        /* size in bytes of this section */
        public uint offset;      /* file offset of this section */
        public uint align;       /* section alignment (power of 2) */
        public uint reloff;      /* file offset of relocation entries */
        public uint nreloc;      /* number of relocation entries */
        public uint flags;       /* flags (section type and attributes)*/
        public uint reserved1;   /* reserved */
        public uint reserved2;   /* reserved */
        public uint reserved3;   /* reserved */
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
    internal class MyMachoSection
    {
        public string name;
        public long address;
        public long size;
        public long offset;
        public long end;
    }
}
