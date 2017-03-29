﻿/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
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
                return "MachOARM";
            }
        }

        internal bool InitARM7()
        {
            Position = 0;
            Position += 16; //skip
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
                            var address = ReadUInt32();
                            var size = ReadUInt32();
                            var offset2 = ReadUInt32();
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

        protected override bool Init() {
            var magic = ReadUInt32();

            if (magic == MachOConstants.MH_MAGIC)
            {
                // MachO ARM7
                return InitARM7();
            }

            if (magic == MachOConstants.MH_MAGIC ||
                magic == MachOConstants.MH_MAGIC_64 ||
                magic == MachOConstants.MH_CIGAM_64)
            {
                // Macho O
                logger.Debug("ARM64 not supported.");
                return false;
            }

            if (magic == MachOConstants.FAT_MAGIC || magic == MachOConstants.FAT_CIGAM)
            {
                // Fat MachO
                logger.Debug("MachO Fat Binary not supported");
                return false;
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
            return uiAddr - (section.address - section.offset);
        }
    }
}
