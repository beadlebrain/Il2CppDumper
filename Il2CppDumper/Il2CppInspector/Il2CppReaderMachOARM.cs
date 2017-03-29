/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppReaderMachOARM : Il2CppReader
    {
        private static byte[] FeatureBytes1 = { 0x0, 0x22 }; //MOVS R2, #0
        private static byte[] FeatureBytes2 = { 0x78, 0x44, 0x79, 0x44 }; //ADD R0, PC and ADD R1, PC

        public Il2CppReaderMachOARM(IFileFormatReader stream) : base(stream) { }

        public Il2CppReaderMachOARM(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        protected override (uint, uint) Search(uint loc, uint globalOffset) {
            var i = loc - 1;
            Image.Position = MapVATR(i);
            Image.Position += 4;
            var buff = Image.ReadBytes(2);
            if (FeatureBytes1.SequenceEqual(buff))
            {
                Image.Position += 12;
                buff = Image.ReadBytes(4);
                if (FeatureBytes2.SequenceEqual(buff))
                {
                    Image.Position = MapVATR(i) + 10;
                    var subaddr = decodeMov(Image.ReadBytes(8)) + i + 24u - 1u;
                    var rsubaddr = MapVATR(subaddr);
                    Image.Position = rsubaddr;
                    var ptr = decodeMov(Image.ReadBytes(8)) + subaddr + 16u;
                    Image.Position = MapVATR(ptr);
                    var metadataRegistration = Image.ReadUInt32();
                    Image.Position = rsubaddr + 8;
                    buff = Image.ReadBytes(4);
                    Image.Position = rsubaddr + 14;
                    buff = buff.Concat(Image.ReadBytes(4)).ToArray();
                    var codeRegistration = decodeMov(buff) + subaddr + 26u;
                    return (codeRegistration, metadataRegistration);
                }
            }
            return (0, 0);
        }

        protected uint MapVATR(uint uiAddr)
        {
            var sections = ((Readers.MachOReader)Image).sections;
            var section = sections.First(x => uiAddr >= x.address && uiAddr <= x.end);
            return uiAddr - (section.address - section.offset);
        }

        private uint decodeMov(byte[] asm) {
            var low = (ushort)(asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 0x04) << 9) + ((asm[0] & 0x0f) << 12));
            var high = (ushort)(asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 0x04) << 9) + ((asm[4] & 0x0f) << 12));
            return (uint)((high << 16) + low);
        }
    }
}
