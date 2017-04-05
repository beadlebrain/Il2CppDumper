/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using Il2CppInspector.Readers;
using NLog;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppReaderARM : Il2CppReader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Il2CppReaderARM(IFileFormatReader stream) : base(stream) { }

        public Il2CppReaderARM(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) : base(stream, codeRegistration, metadataRegistration) { }

        private (bool, uint, uint) SearchARM64(uint loc, uint globalOffset)
        {
            uint metadataRegistration, codeRegistration;

            var bytes = new byte[] { 0x1c, 0x0, 0x9f, 0xe5, 0x1c, 0x10, 0x9f, 0xe5, 0x1c, 0x20, 0x9f, 0xe5 };
            Image.Position = loc;
            var buff = Image.ReadBytes(12);
            if (bytes.SequenceEqual(buff))
            {
                Image.Position = loc + 0x2c;
                var subaddr = Image.ReadUInt32() + globalOffset;
                Image.Position = subaddr + 0x28;
                codeRegistration = Image.ReadUInt32() + globalOffset;
                Image.Position = subaddr + 0x2C;
                var ptr = Image.ReadUInt32() + globalOffset;
                Image.Position = Image.MapVATR(ptr);
                metadataRegistration = Image.ReadUInt32();
                return (true, codeRegistration, metadataRegistration);
            }

            return (false, 0, 0);
        }

        private (bool, uint, uint) SearchARM7(uint loc, uint globalOffset)
        {
            // ARMv7 Thumb (T1)
            // http://liris.cnrs.fr/~mmrissa/lib/exe/fetch.php?media=armv7-a-r-manual.pdf - A8.8.106
            // http://armconverter.com/hextoarm/

            uint metadataRegistration, codeRegistration;

            var bytes = new byte[] { 0x2d, 0xe9, 0x00, 0x48, 0xeb, 0x46 };
            Image.Position = loc;
            var buff = Image.ReadBytes(6);
            if (bytes.SequenceEqual(buff))
            {
                bytes = new byte[] { 0x00, 0x23, 0x00, 0x22, 0xbd, 0xe8, 0x00, 0x48 };
                Image.Position += 0x10;
                buff = Image.ReadBytes(8);
                if (bytes.SequenceEqual(buff))
                {
                    Image.Position = loc + 6;
                    Image.Position = (Image.MapVATR(decodeMovImm32(Image.ReadBytes(8))) & 0xfffffffc) + 0x0e;
                    metadataRegistration = decodeMovImm32(Image.ReadBytes(8));
                    codeRegistration = decodeMovImm32(Image.ReadBytes(8));
                    return (true, codeRegistration, metadataRegistration);
                }
            }

            return (false, 0, 0);
        }

        private (bool, uint, uint) SearchAltARM7(uint loc, uint globalOffset)
        {
            uint metadataRegistration, codeRegistration;

            var locfix = loc - 1;
            var bytes = new byte[] { 0x0, 0x22 }; //MOVS R2, #0
            Image.Position = Image.MapVATR(locfix);
            Image.Position += 4;
            var buff = Image.ReadBytes(2);
            if (bytes.SequenceEqual(buff))
            {
                bytes = new byte[] { 0x78, 0x44, 0x79, 0x44 }; //ADD R0, PC and ADD R1, PC
                Image.Position += 12;
                buff = Image.ReadBytes(4);
                if (bytes.SequenceEqual(buff))
                {
                    Image.Position = Image.MapVATR(locfix) + 10;
                    var subaddr = decodeMovImm32(Image.ReadBytes(8)) + locfix + 24u - 1u;
                    var rsubaddr = Image.MapVATR(subaddr);
                    Image.Position = rsubaddr;
                    var ptr = decodeMovImm32(Image.ReadBytes(8)) + subaddr + 16u;
                    Image.Position = Image.MapVATR(ptr);
                    metadataRegistration = Image.ReadUInt32();
                    Image.Position = rsubaddr + 8;
                    buff = Image.ReadBytes(4);
                    Image.Position = rsubaddr + 14;
                    buff = buff.Concat(Image.ReadBytes(4)).ToArray();
                    codeRegistration = decodeMovImm32(buff) + subaddr + 26u;
                    return (true, codeRegistration, metadataRegistration);
                }
            }

            return (false, 0, 0);
        }

        private (bool, uint, uint) SearchAltARM64(uint loc, uint globalOffset)
        {
            return (false, 0, 0);
        }

        protected override (uint, uint) Search(uint loc, uint globalOffset) {
            // Assembly bytes to search for at start of each function
            bool found = false;
            uint metadataRegistration, codeRegistration;

            // ARM64 (should work on elf 64 bits)
            (found, codeRegistration, metadataRegistration) = SearchARM64(loc, globalOffset);
            if (found) return (metadataRegistration, codeRegistration);

            // ARMv7 (should work on elf Arm7)
            (found, codeRegistration, metadataRegistration) = SearchARM7(loc, globalOffset);
            if (found) return (metadataRegistration, codeRegistration);

            // Not found, try alternate method that should work on iOS arm7
            (found, codeRegistration, metadataRegistration) = SearchAltARM7(loc, globalOffset); 
            if (found) return (metadataRegistration, codeRegistration);

            // iOS ARM64 ?

            return (0, 0);
        }

        private uint decodeMovImm32(byte[] asm) {
            ushort low = (ushort) (asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 0x04) << 9) + ((asm[0] & 0x0f) << 12));
            ushort high = (ushort) (asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 0x04) << 9) + ((asm[4] & 0x0f) << 12));
            return (uint) ((high << 16) + low);
        }
    }
}
