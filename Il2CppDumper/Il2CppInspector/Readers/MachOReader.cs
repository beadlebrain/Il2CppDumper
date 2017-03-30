/*
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
        private static byte[] FeatureBytes1 = { 0x0, 0x22 }; //MOVS R2, #0
        private static byte[] FeatureBytes2 = { 0x78, 0x44, 0x79, 0x44 }; //ADD R0, PC and ADD R1, PC

        private static Logger logger = LogManager.GetCurrentClassLogger();
        internal List<MachoSection> sections = new List<MachoSection>();

        public MachOReader(Stream stream) : base(stream) { }

        public override string Arch {
            get {
                return "ARM";
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

        public override (uint, uint) Search(uint loc, uint globalOffset)
        {
            var i = loc - 1;
            Position = MapVATR(i);
            Position += 4;
            var buff = ReadBytes(2);
            if (FeatureBytes1.SequenceEqual(buff))
            {
                Position += 12;
                buff = ReadBytes(4);
                if (FeatureBytes2.SequenceEqual(buff))
                {
                    Position = MapVATR(i) + 10;
                    var subaddr = decodeMov(ReadBytes(8)) + i + 24u - 1u;
                    var rsubaddr = MapVATR(subaddr);
                    Position = rsubaddr;
                    var ptr = decodeMov(ReadBytes(8)) + subaddr + 16u;
                    Position = MapVATR(ptr);
                    var metadataRegistration = ReadUInt32();
                    Position = rsubaddr + 8;
                    buff = ReadBytes(4);
                    Position = rsubaddr + 14;
                    buff = buff.Concat(ReadBytes(4)).ToArray();
                    var codeRegistration = decodeMov(buff) + subaddr + 26u;
                    return (codeRegistration, metadataRegistration);
                }
            }
            return (0, 0);
        }

        public override uint MapVATR(uint uiAddr)
        {
            var section = sections.First(x => uiAddr >= x.address && uiAddr <= x.end);
            return uiAddr - (section.address - section.offset);
        }

        private uint decodeMov(byte[] asm)
        {
            var low = (ushort)(asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 0x04) << 9) + ((asm[0] & 0x0f) << 12));
            var high = (ushort)(asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 0x04) << 9) + ((asm[4] & 0x0f) << 12));
            return (uint)((high << 16) + low);
        }
    }
}
