/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using Il2CppInspector.Readers;
using Il2CppInspector.Structures;
using NLog;
using System.Linq;

namespace Il2CppInspector
{
    public abstract class Il2CppReader
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public IFileFormatReader Image { get; }

        protected Il2CppReader(IFileFormatReader stream) {
            Image = stream;
        }

        protected Il2CppReader(IFileFormatReader stream, uint codeRegistration, uint metadataRegistration) {
            Image = stream;
            Configure(codeRegistration, metadataRegistration);
        }

        public Il2CppCodeRegistration PtrCodeRegistration { get; protected set; }
        public Il2CppMetadataRegistration PtrMetadataRegistration { get; protected set; }

        // Architecture-specific search function
        protected abstract (long, long) Search(long loc, long globalOffset);

        // Check all search locations
        public virtual bool Load() {
            var addrs = Image.GetSearchLocations();
            if (addrs != null)
            {
                foreach (var loc in addrs)
                {
                    if (loc != 0)
                    {
                        var (code, metadata) = Search(loc, Image.GlobalOffset);
                        if (code != 0)
                        {
                            Configure(code, metadata);
                            return true;
                        }
                    }
                }
            }
            logger.Error("Unable to find registrations");
            return false;
        }

        internal virtual void Configure(long codeRegistration, long metadataRegistration) {
            PtrCodeRegistration = Image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            PtrMetadataRegistration = Image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);
            PtrCodeRegistration.methodPointers = Image.ReadMappedArray<uint>(PtrCodeRegistration.pmethodPointers,
                (int) PtrCodeRegistration.methodPointersCount);
            PtrMetadataRegistration.fieldOffsets = Image.ReadMappedArray<int>(PtrMetadataRegistration.pfieldOffsets,
                PtrMetadataRegistration.fieldOffsetsCount);

            long[] types;
            if (Image.Is64bits)
            {
                var ptrs = Image.ReadMappedArray<ulong>(PtrMetadataRegistration.ptypes, PtrMetadataRegistration.typesCount);
                types = ptrs.Select(p => (long)p).ToArray();
            }
            else
            {
                var ptrs = Image.ReadMappedArray<uint>(PtrMetadataRegistration.ptypes, PtrMetadataRegistration.typesCount);
                types = ptrs.Select(p => (long)p).ToArray();
            }

            PtrMetadataRegistration.types = new Il2CppType[PtrMetadataRegistration.typesCount];
            for (int i = 0; i < PtrMetadataRegistration.typesCount; ++i) {
                PtrMetadataRegistration.types[i] = Image.ReadMappedObject<Il2CppType>(types[i]);
                PtrMetadataRegistration.types[i].Init();
            }
        }

        public Il2CppType GetTypeFromTypeIndex(int idx) {
            return PtrMetadataRegistration.types[idx];
        }

        public int GetFieldOffsetFromIndex(int typeIndex, int fieldIndexInType) {
            var ptr = PtrMetadataRegistration.fieldOffsets[typeIndex];
            Image.Stream.Position = Image.MapVATR((uint) ptr) + 4 * fieldIndexInType;
            return Image.Stream.ReadInt32();
        }
    }
}
