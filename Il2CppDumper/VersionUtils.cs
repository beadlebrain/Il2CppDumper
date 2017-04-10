using Il2CppInspector.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Il2CppDumper
{
    public class VersionUtils
    {
        public byte[] BinaryContent { get; set; }
        public uint BinaryMagic { get; set; }

        public byte[] MetadataContent { get; set; }
        public uint MetadataMagic { get; set; }
        public uint MetadataVersion { get; set; }

        public bool IsValid { get; set; }
        public bool IsElf { get; set; }
        public bool IsMachO { get; set; }
        public bool IsFatMachO { get; set; }
        public bool Is64bits { get; set; }

        public VersionUtils(string binaryFile, string metadataFile)
        {
            BinaryContent = File.ReadAllBytes(binaryFile);
            MetadataContent = File.ReadAllBytes(metadataFile);

            ReLoad();
        }

        public void ReLoad()
        {
            MetadataMagic = BitConverter.ToUInt32(MetadataContent, 0);
            MetadataVersion = BitConverter.ToUInt32(MetadataContent, 4);

            BinaryMagic = BitConverter.ToUInt32(BinaryContent, 0);

            IsValid = MetadataMagic == 0xFAB11BAF;

            if (BinaryMagic == 0x464c457f)
            {
                IsElf = true;
                Is64bits = false;
            }
            else if (BinaryMagic == MachOConstants.FAT_MAGIC || BinaryMagic == MachOConstants.FAT_CIGAM)
            {
                IsMachO = true;
                IsFatMachO = true;
                // TODO Fazt Macho
            }
            else if (BinaryMagic == MachOConstants.MH_MAGIC || BinaryMagic == MachOConstants.MH_CIGAM)
            {
                IsMachO = true;
                Is64bits = false;
            }
            else if (BinaryMagic == MachOConstants.MH_MAGIC_64 || BinaryMagic == MachOConstants.MH_CIGAM_64)
            {
                IsMachO = true;
                Is64bits = true;
            }
        }

        public Stream GetBinaryReader()
        {
            var stream = new MemoryStream(BinaryContent);
            if (BinaryMagic == MachOConstants.FAT_CIGAM || BinaryMagic == MachOConstants.MH_CIGAM_64 || BinaryMagic == MachOConstants.MH_CIGAM)
            {
                //stream.
            }

            if (!IsFatMachO)
            {
                
            }
            else
            {
                MetadataVersion = BitConverter.ToUInt32(MetadataContent, 4);
                if (Options.GetOptions().Arm7)
                {

                }
                else
                {

                }
            }
            return null;
        }

        public Stream GetMetadataStream()
        {
            return null;
        }
    }
}
