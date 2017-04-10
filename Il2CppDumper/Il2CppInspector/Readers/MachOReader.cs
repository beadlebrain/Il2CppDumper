/*
 *  Copyright 2017 niico - https://github.com/pogosandbox/Il2CppDumper 
 *
 *  All rights reserved.
*/

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppInspector.Readers
{
    internal class MachOReader : FileFormatReader<MachOReader>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        internal List<MyMachoSection> sections = new List<MyMachoSection>();
        
        public MachOReader(Stream stream) : base(stream) { }
        
        public override string Arch {
            get {
                return "ARM";
            }
        }

        private bool InitFat()
        {
            var options = Il2CppDumper.Options.GetOptions();
            var nArch = ReadUInt32();
            for (var i = 0; i < nArch; i++)
            {
                var fatArch = ReadObject<FatArch>();
                if (options.Arm7 && fatArch.cputype == MachOConstants.CPU_TYPE_ARM)
                {
                    // reinit using the ARM macho segment
                    Position = GlobalOffset = fatArch.offset;
                    return Init();
                }
                else if (fatArch.cputype == MachOConstants.CPU_TYPE_ARM64)
                {
                    // reinit using the ARM64 macho segment
                    Position = GlobalOffset = fatArch.offset;
                    return Init();
                }
            }
            return false;
        }

        private bool InitARM7()
        {
            Is64bits = false;

            var header = ReadObject<MachoHeader>();
            for (int i = 0; i < header.ncmds; i++)
            {
                var offset = Position;
                var loadCommand = ReadObject<MachoLoadCommand>();
                if (loadCommand.cmd == MachOConstants.LC_SEGMENT)
                {
                    var segment = ReadObject<MachosSegmentCommand>();
                    if (segment.segname == "__TEXT" || segment.segname == "__DATA")
                    {
                        for (int j = 0; j < segment.nsects; j++)
                        {
                            var section = ReadObject<MachoSection>();
                            sections.Add(new MyMachoSection() {
                                name = section.sectname,
                                address = section.addr + GlobalOffset,
                                size = section.size,
                                offset = section.offset + GlobalOffset,
                                end = section.addr + GlobalOffset + section.size
                            });

                        }
                    }
                }
                Position = offset + loadCommand.cmdsize; //skip
            }
            return true;
        }

        private bool InitARM64()
        {
            Is64bits = true;

            var header = ReadObject<MachoHeader64>();
            for (int i = 0; i < header.ncmds; i++)
            {
                var offset = Position;
                var loadCommand = ReadObject<MachoLoadCommand>();
                if (loadCommand.cmd == MachOConstants.LC_SEGMENT_64)
                {
                    var segment = ReadObject<MachosSegmentCommand64>();
                    if (segment.segname == "__TEXT" || segment.segname == "__DATA")
                    {
                        for (int j = 0; j < segment.nsects; j++)
                        {
                            var section = ReadObject<MachoSection64>();
                            sections.Add(new MyMachoSection()
                            {
                                name = section.sectname,
                                address = (long)section.addr + GlobalOffset,
                                size = (long)section.size,
                                offset = section.offset + GlobalOffset,
                                end = (long)section.addr + GlobalOffset + (long)section.size
                            });
                        }
                    }
                }
                Position = offset + loadCommand.cmdsize; //skip
            }
            return true;
        }

        protected override bool Init() {
            Endianness = NoisyCowStudios.Bin2Object.Endianness.Little;
            var magic =  ReadUInt32();

            if (magic == MachOConstants.MH_CIGAM || magic == MachOConstants.MH_CIGAM_64 || magic == MachOConstants.FAT_CIGAM)
            {
                Endianness = NoisyCowStudios.Bin2Object.Endianness.Big;
            }

            if (magic == MachOConstants.MH_MAGIC || magic == MachOConstants.MH_CIGAM)
            {
                // MachO ARM7
                logger.Debug("Opening ARM7 binary");
                return InitARM7();
            }

            if (magic == MachOConstants.MH_MAGIC_64 || magic == MachOConstants.MH_CIGAM_64)
            {
                // Macho O ARM64
                logger.Debug("Opening ARM64 binary");
                return InitARM64();
            }

            if (magic == MachOConstants.FAT_MAGIC || magic == MachOConstants.FAT_CIGAM)
            {
                // Fat MachO
                logger.Debug("Opening Fat binary");
                return InitFat();
            }
            
            logger.Debug("Invalid magic detected in MachO files: 0x{0}", magic.ToString("X"));

            return false;
        }

        public override long[] GetSearchLocations() {
            if (!Is64bits)
            {
                var __mod_init_func = sections.First(x => x.name == "__mod_init_func");
                var locations = ReadArray<uint>(__mod_init_func.offset, (int)(__mod_init_func.size / 4));
                return locations.Select(l => (long)l).ToArray();
            }
            else
            {
                var __mod_init_func = sections.First(x => x.name == "__mod_init_func");
                var locations = ReadArray<ulong>(__mod_init_func.offset, (int)(__mod_init_func.size / 8));
                return locations.Select(l => (long)l).ToArray();
            }
        }
        
        public override long MapVATR(long uiAddr)
        {
            var section = sections.First(x => (uiAddr >= x.address && uiAddr <= x.end));
            return GlobalOffset + uiAddr - (section.address - section.offset);
        }
    }
}
