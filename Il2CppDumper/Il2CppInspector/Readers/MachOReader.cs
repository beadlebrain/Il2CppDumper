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
        internal List<MachoSection> sections = new List<MachoSection>();

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
            Position += 12; //skip
            var ncmds = ReadUInt32();
            Position += 8; //skip
            for (int i = 0; i < ncmds; i++)
            {
                var offset = Position;
                var loadCommandType = ReadUInt32();
                var command_size = ReadUInt32();
                if (loadCommandType == 1) //SEGMENT
                {
                    var segment_name = System.Text.Encoding.UTF8.GetString(ReadBytes(16)).TrimEnd('\0');
                    if (segment_name == "__TEXT" || segment_name == "__DATA")
                    {
                        Position += 24; //skip
                        var number_of_sections = ReadUInt32();
                        Position += 4; //skip
                        for (int j = 0; j < number_of_sections; j++)
                        {
                            var section_name = System.Text.Encoding.UTF8.GetString(ReadBytes(16)).TrimEnd('\0');
                            Position += 16;
                            var address = ReadUInt32() + GlobalOffset;
                            var size = ReadUInt32();
                            var offset2 = ReadUInt32() + GlobalOffset;
                            var end = address + size;
                            sections.Add(new MachoSection() { section_name = section_name, address = address, size = size, offset = offset2, end = end });
                            Position += 24;
                        }
                    }
                }
                Position = offset + command_size; //skip
            }
            return true;
        }

        private bool InitARM64()
        {
            logger.Warn("ARM64 not supported.");
            return false;
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

        public override uint[] GetSearchLocations() {
            var __mod_init_func = sections.First(x => x.section_name == "__mod_init_func");
            return ReadArray<uint>(__mod_init_func.offset, (int)__mod_init_func.size / 4);
        }
        
        public override uint MapVATR(uint uiAddr)
        {
            var section = sections.First(x => uiAddr >= x.address && uiAddr <= x.end);
            return GlobalOffset + uiAddr - (section.address - section.offset);
        }

        private uint decodeMov(byte[] asm)
        {
            var low = (ushort)(asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 0x04) << 9) + ((asm[0] & 0x0f) << 12));
            var high = (ushort)(asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 0x04) << 9) + ((asm[4] & 0x0f) << 12));
            return (uint)((high << 16) + low);
        }
    }
}
