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

        public GenericIl2CppType[] Types { get; set; }
        public long[] MethodPointers { get; set; }

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
                            logger.Debug("Offset found: 0x{0:x} 0x{1:x}", code, metadata);
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
            var PtrCodeRegistration = Image.ReadMappedObject<Il2CppCodeRegistration>(codeRegistration);
            var PtrMetadataRegistration = Image.ReadMappedObject<Il2CppMetadataRegistration>(metadataRegistration);

            var methodPointers = Image.ReadMappedArray<uint>(PtrCodeRegistration.pmethodPointers, (int) PtrCodeRegistration.methodPointersCount);
            MethodPointers = methodPointers.Select(p => (long)p).ToArray();

            //PtrMetadataRegistration.fieldOffsets = Image.ReadMappedArray<int>(PtrMetadataRegistration.pfieldOffsets,
            //    PtrMetadataRegistration.fieldOffsetsCount);

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

            Types = new GenericIl2CppType[PtrMetadataRegistration.typesCount];
            for (int i = 0; i < PtrMetadataRegistration.typesCount; ++i) {
                var pType = Image.ReadMappedObject<Il2CppType>(types[i]);
                pType.Init();
                Types[i] = new GenericIl2CppType(pType);
            }
        }

        public GenericIl2CppType GetTypeFromTypeIndex(int idx) {
            return Types[idx];
        }

        //public int GetFieldOffsetFromIndex(int typeIndex, int fieldIndexInType) {
        //    var ptr = PtrMetadataRegistration.fieldOffsets[typeIndex];
        //    Image.Stream.Position = Image.MapVATR(ptr) + 4 * fieldIndexInType;
        //    return Image.Stream.ReadInt32();
        //}
    }
}
