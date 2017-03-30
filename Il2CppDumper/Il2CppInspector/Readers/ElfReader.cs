/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;

namespace Il2CppInspector.Readers
{
    internal class ElfReader : FileFormatReader<ElfReader>
    {
        private program_header_table[] program_table_element;
        private elf_header elf_header;

        public ElfReader(Stream stream) : base(stream) { }

        public override string Arch {
            get {
                switch (elf_header.e_machine) {
                    case 0x03:
                        return "x86";
                    case 0x28:
                        return "ARM";
                    default:
                        return "Unsupported";
                }
            }
        }

        protected override bool Init() {
            elf_header = ReadObject<elf_header>();

            if (elf_header.m_dwFormat != 0x464c457f) {
                // Not an ELF file
                return false;
            }
            if (elf_header.m_arch == 2)//64
            {
                // 64-bit not supported
                return false;
            }
            program_table_element = ReadArray<program_header_table>(elf_header.e_phoff, elf_header.e_phnum);
            return true;
        }

        public override uint[] GetSearchLocations() {
            // Find dynamic section
            var dynamic = new elf_32_shdr();
            var PT_DYNAMIC = program_table_element.First(x => x.p_type == 2u);
            dynamic.sh_offset = PT_DYNAMIC.p_offset;
            dynamic.sh_size = PT_DYNAMIC.p_filesz;

            // We need GOT, INIT_ARRAY and INIT_ARRAYSZ
            uint _GLOBAL_OFFSET_TABLE_ = 0;
            var init_array = new elf_32_shdr();
            Position = dynamic.sh_offset;
            var dynamicend = dynamic.sh_offset + dynamic.sh_size;
            while (Position < dynamicend) {
                var tag = ReadInt32();
                if (tag == 3) //DT_PLTGOT
                {
                    _GLOBAL_OFFSET_TABLE_ = ReadUInt32();
                    continue;
                }
                else if (tag == 25) //DT_INIT_ARRAY
                {
                    init_array.sh_offset = MapVATR(ReadUInt32());
                    continue;
                }
                else if (tag == 27) //DT_INIT_ARRAYSZ
                {
                    init_array.sh_size = ReadUInt32();
                    continue;
                }
                Position += 4;
            }
            if (_GLOBAL_OFFSET_TABLE_ == 0)
                throw new InvalidOperationException("Unable to get GLOBAL_OFFSET_TABLE from PT_DYNAMIC");
            GlobalOffset = _GLOBAL_OFFSET_TABLE_;
            return ReadArray<uint>(init_array.sh_offset, (int) init_array.sh_size / 4);
        }

        public override (uint, uint) Search(uint loc, uint globalOffset)
        {
            // Assembly bytes to search for at start of each function
            uint metadataRegistration, codeRegistration;

            // ARM
            var bytes = new byte[] { 0x1c, 0x0, 0x9f, 0xe5, 0x1c, 0x10, 0x9f, 0xe5, 0x1c, 0x20, 0x9f, 0xe5 };
            Position = loc;
            var buff = ReadBytes(12);
            if (bytes.SequenceEqual(buff))
            {
                Position = loc + 0x2c;
                var subaddr = ReadUInt32() + globalOffset;
                Position = subaddr + 0x28;
                codeRegistration = ReadUInt32() + globalOffset;
                Position = subaddr + 0x2C;
                var ptr = ReadUInt32() + globalOffset;
                Position = MapVATR(ptr);
                metadataRegistration = ReadUInt32();
                return (codeRegistration, metadataRegistration);
            }

            // ARMv7 Thumb (T1)
            // http://liris.cnrs.fr/~mmrissa/lib/exe/fetch.php?media=armv7-a-r-manual.pdf - A8.8.106
            // http://armconverter.com/hextoarm/
            bytes = new byte[] { 0x2d, 0xe9, 0x00, 0x48, 0xeb, 0x46 };
            Position = loc;
            buff = ReadBytes(6);
            if (!bytes.SequenceEqual(buff))
                return (0, 0);
            bytes = new byte[] { 0x00, 0x23, 0x00, 0x22, 0xbd, 0xe8, 0x00, 0x48 };
            Position += 0x10;
            buff = ReadBytes(8);
            if (!bytes.SequenceEqual(buff))
                return (0, 0);
            Position = loc + 6;
            Position = (MapVATR(decodeMovImm32(ReadBytes(8))) & 0xfffffffc) + 0x0e;
            metadataRegistration = decodeMovImm32(ReadBytes(8));
            codeRegistration = decodeMovImm32(ReadBytes(8));
            return (codeRegistration, metadataRegistration);
        }

        public override uint MapVATR(uint uiAddr)
        {
            var program_header_table = program_table_element.First(x => uiAddr >= x.p_vaddr && uiAddr <= (x.p_vaddr + x.p_memsz));
            return uiAddr - (program_header_table.p_vaddr - program_header_table.p_offset);
        }

        private uint decodeMovImm32(byte[] asm)
        {
            ushort low = (ushort)(asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 0x04) << 9) + ((asm[0] & 0x0f) << 12));
            ushort high = (ushort)(asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 0x04) << 9) + ((asm[4] & 0x0f) << 12));
            return (uint)((high << 16) + low);
        }
    }
}
