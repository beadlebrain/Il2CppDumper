/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using Il2CppInspector.Readers;
using Il2CppInspector.Structures;
using NLog;
using System;
using System.Linq;

namespace Il2CppInspector
{
    internal class Il2CppReaderARM64 : Il2CppReader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Il2CppReaderARM64(IFileFormatReader stream) : base(stream) { }
        
        protected override (long, long) Search(long loc, long globalOffset)
        {
            // iOS ARM64
            var bytes = new byte[] { 0x2, 0x0, 0x80, 0xD2 }; // MOV X2, #0
            Image.Position = Image.MapVATR(loc);
            var buff = Image.ReadBytes(4);
            if (bytes.SequenceEqual(buff))
            {
                bytes = new byte[] { 0x3, 0x0, 0x80, 0x52 }; // MOV W3, #0
                buff = Image.ReadBytes(4);
                if (bytes.SequenceEqual(buff))
                {
                    Image.Position += 8;
                    var subaddr = decodeAdr(loc + 16, Image.ReadBytes(4));
                    var rsubaddr = Image.MapVATR(subaddr);
                    Image.Position = rsubaddr;
                    var codeRegistration = decodeAdrp(subaddr, Image.ReadBytes(4));
                    codeRegistration += decodeAdd(Image.ReadBytes(4));
                    Image.Position = rsubaddr + 8;
                    var metadataRegistration = decodeAdrp(subaddr + 8, Image.ReadBytes(4));
                    metadataRegistration += decodeAdd(Image.ReadBytes(4));
                    return (codeRegistration, metadataRegistration);
                }
            }

            return (0, 0);
        }
        
        internal override void Configure(long codeRegistration, long metadataRegistration)
        {
            var ptrCodeReg = Image.ReadMappedObject<Il2CppCodeRegistration64>(codeRegistration);
            var ptrMetadataReg = Image.ReadMappedObject<Il2CppMetadataRegistration64>(metadataRegistration);

            var methodPointers = Image.ReadMappedArray<ulong>((long)ptrCodeReg.pmethodPointers, (int)ptrCodeReg.methodPointersCount);
            MethodPointers = methodPointers.Select(p => (long)p).ToArray();

            //ptrMetadataReg.fieldOffsets = Image.ReadMappedArray<long>((long)ptrMetadataReg.pfieldOffsets, (int)ptrMetadataReg.fieldOffsetsCount);

            var ptrs = Image.ReadMappedArray<ulong>((long)ptrMetadataReg.ptypes, (int)ptrMetadataReg.typesCount);
            var types = ptrs.Select(p => (long)p).ToArray();

            Types = new GenericIl2CppType[ptrMetadataReg.typesCount];
            for (var i = 0; i < ptrMetadataReg.typesCount; ++i)
            {
                var pType = Image.ReadMappedObject<Il2CppType64>(types[i]);
                pType.Init();
                Types[i] = new GenericIl2CppType(pType);
            }
        }

        private long decodeAdr(long pc, byte[] label)
        {
            var bin = "";
            foreach (var b in label)
            {
                var str = Convert.ToString(b, 2);
                if (str.Length < 8)
                {
                    str = new string(Enumerable.Repeat('0', 8 - str.Length).Concat(str.ToCharArray()).ToArray());
                }
                bin += str;
            }
            var uint64 = new string(Enumerable.Repeat(bin[16], 44).ToArray())
                         + bin.Substring(17, 7) + bin.Substring(8, 8) + bin.Substring(0, 3) + bin.Substring(25, 2);
            return pc + (long)Convert.ToUInt64(uint64, 2);
        }

        private long decodeAdrp(long pc, byte[] label)
        {
            var pcbin = Convert.ToString((long)pc, 2);
            if (pcbin.Length < 64)
            {
                pcbin = new string(Enumerable.Repeat('0', 64 - pcbin.Length).Concat(pcbin.ToCharArray()).ToArray());
            }
            pcbin = pcbin.Substring(0, 52) + new string(Enumerable.Repeat('0', 12).ToArray());
            var bin = "";
            foreach (var b in label)
            {
                var str = Convert.ToString(b, 2);
                if (str.Length < 8)
                {
                    str = new string(Enumerable.Repeat('0', 8 - str.Length).Concat(str.ToCharArray()).ToArray());
                }
                bin += str;
            }
            var uint64 = new string(Enumerable.Repeat(bin[16], 32).ToArray())
                         + bin.Substring(17, 7) + bin.Substring(8, 8) + bin.Substring(0, 3) + bin.Substring(25, 2)
                         + new string(Enumerable.Repeat('0', 12).ToArray());
            return (long)Convert.ToUInt64(pcbin, 2) + (long)Convert.ToUInt64(uint64, 2);
        }

        private long decodeAdd(byte[] ins)
        {
            var bin = "";
            foreach (var b in ins)
            {
                var str = Convert.ToString(b, 2);
                if (str.Length < 8)
                {
                    str = new string(Enumerable.Repeat('0', 8 - str.Length).Concat(str.ToCharArray()).ToArray());
                }
                bin += str;
           }
            var uint64 = Convert.ToUInt64(bin.Substring(18, 6) + bin.Substring(8, 6), 2);
            if (bin[17] == '1')
                uint64 <<= 12;
            return (long)uint64;
        }
    }
}
