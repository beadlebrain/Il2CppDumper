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

        private (bool, long, long) SearchARM(long loc, long globalOffset)
        {
            var bytes = new byte[] { 0x1c, 0x0, 0x9f, 0xe5, 0x1c, 0x10, 0x9f, 0xe5, 0x1c, 0x20, 0x9f, 0xe5 };
            Image.Position = loc;
            var buff = Image.ReadBytes(12);
            if (bytes.SequenceEqual(buff))
            {
                Image.Position = loc + 0x2c;
                var subaddr = Image.ReadUInt32() + globalOffset;
                Image.Position = subaddr + 0x28;
                var codeRegistration = Image.ReadUInt32() + globalOffset;
                Image.Position = subaddr + 0x2C;
                var ptr = Image.ReadUInt32() + globalOffset;
                Image.Position = Image.MapVATR(ptr);
                var metadataRegistration = Image.ReadUInt32();
                return (true, codeRegistration, metadataRegistration);
            }

            return (false, 0, 0);
        }

        private (bool, long, long) SearchARM7Thumb(long loc, long globalOffset)
        {
            // ARMv7 Thumb (T1)
            // http://liris.cnrs.fr/~mmrissa/lib/exe/fetch.php?media=armv7-a-r-manual.pdf - A8.8.106
            // http://armconverter.com/hextoarm/

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
                    var metadataRegistration = decodeMovImm32(Image.ReadBytes(8));
                    var codeRegistration = decodeMovImm32(Image.ReadBytes(8));
                    return (true, codeRegistration, metadataRegistration);
                }
            }

            return (false, 0, 0);
        }

        private (bool, long, long) SearchAltARM7(long loc, long globalOffset)
        {
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
                    logger.Debug($"Pos: 0x{locfix.ToString("X")}");
                    Image.Position = Image.MapVATR(locfix) + 10;
                    logger.Debug($"Pos: 0x{Image.Position.ToString("X")}");
                    bytes = Image.ReadBytes(8);
                    logger.Debug(System.BitConverter.ToString(bytes));
                    var subaddr = decodeMovImm32(bytes) + locfix + 24u - 1u;
                    logger.Debug($"subaddr: 0x{subaddr.ToString("X")}");
                    var rsubaddr = Image.MapVATR(subaddr);
                    Image.Position = rsubaddr;
                    var ptr = decodeMovImm32(Image.ReadBytes(8)) + subaddr + 16u;
                    Image.Position = Image.MapVATR(ptr);
                    var metadataRegistration = Image.ReadUInt32();
                    Image.Position = rsubaddr + 8;
                    buff = Image.ReadBytes(4);
                    Image.Position = rsubaddr + 14;
                    buff = buff.Concat(Image.ReadBytes(4)).ToArray();
                    var codeRegistration = decodeMovImm32(buff) + subaddr + 26u;
                    return (true, codeRegistration, metadataRegistration);
                }
            }

            return (false, 0, 0);
        }

        private (bool, long, long) SearchAltARM64(long loc, long globalOffset)
        {


            return (false, 0, 0);
        }

        protected override (long, long) Search(long loc, long globalOffset) {
            // Assembly bytes to search for at start of each function
            bool found = false;
            long codeRegistration, metadataRegistration;

            logger.Debug($"Loc 0x{loc.ToString("X")}");

            if (Image.Is64bits)
            {
                // iOS ARM64 ?

            }
            else
            {
                // ARM (should work on elf)
                logger.Debug("Search using SearchARM at 0x{0}...", loc.ToString("X"));
                (found, codeRegistration, metadataRegistration) = SearchARM(loc, globalOffset);
                if (found) return (codeRegistration, metadataRegistration);

                // ARMv7 thumb (should work on elf Arm7 thumb)
                logger.Debug("Search using SearchARM7 at 0x{0}...", loc.ToString("X"));
                (found, codeRegistration, metadataRegistration) = SearchARM7Thumb(loc, globalOffset);
                if (found) return (codeRegistration, metadataRegistration);

                // Not found, try alternate method that should work on iOS arm7
                logger.Debug("Search using SearchAltARM7 at 0x{0}...", loc.ToString("X"));
                (found, codeRegistration, metadataRegistration) = SearchAltARM7(loc, globalOffset);
                if (found) return (codeRegistration, metadataRegistration);
            }

            return (0, 0);
        }

        private uint decodeMovImm32(byte[] asm) {
            ushort low = (ushort) (asm[2] + ((asm[3] & 0x70) << 4) + ((asm[1] & 0x04) << 9) + ((asm[0] & 0x0f) << 12));
            ushort high = (ushort) (asm[6] + ((asm[7] & 0x70) << 4) + ((asm[5] & 0x04) << 9) + ((asm[4] & 0x0f) << 12));
            return (uint) ((high << 16) + low);
        }
    }
}
