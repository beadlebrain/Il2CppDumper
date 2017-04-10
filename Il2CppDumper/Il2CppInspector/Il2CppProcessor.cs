/*
 *  Copyright 2017 niico - https://github.com/pogosandbox/Il2CppDumper 
 *  Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
 *
 *  All rights reserved.
*/

using Il2CppInspector.Readers;
using Il2CppInspector.Structures;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppInspector
{
    public class Il2CppProcessor
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Il2CppReader Code { get; }
        public Metadata Metadata { get; }

        public Il2CppProcessor(Il2CppReader code, Metadata metadata)
        {
            Code = code;
            Metadata = metadata;
        }

        public static Il2CppProcessor LoadFromFile(string codeFile, string metadataFile)
        {
            // Load the metadata file
            var metadata = new Metadata(new MemoryStream(File.ReadAllBytes(metadataFile)));

            // Load the il2cpp code file (try ELF, PE and MachO)
            var memoryStream = new MemoryStream(File.ReadAllBytes(codeFile));

            IFileFormatReader stream = null;
            if (codeFile.ToLower().EndsWith(".so"))
            {
                logger.Debug("Using ELF reader.");
                stream = ElfReader.Load(memoryStream);
            }
            else if (codeFile.ToLower().EndsWith(".dll"))
            {
                logger.Debug("Using PE reader.");
                stream = PEReader.Load(memoryStream);
            }
            else
            {
                logger.Debug("Using MachO reader.");
                stream = MachOReader.Load(memoryStream);
            }
            
            if (stream == null) {
                logger.Error("Unsupported executable file format.");
                return null;
            }

            Il2CppReader il2cpp;

            // We are currently supporting x86 and ARM architectures
            switch (stream.Arch) {
                case "x86":
                    il2cpp = new Il2CppReaderX86(stream);
                    break;
                case "ARM":
                    if (stream.Is64bits) il2cpp = new Il2CppReaderARM64(stream);
                    else il2cpp = new Il2CppReaderARM(stream);
                    break;
                default:
                    logger.Error("Unsupported architecture: {0}", stream.Arch);
                    return null;
            }

            // Find code and metadata regions
            if (!il2cpp.Load()) {
                logger.Error("Could not process IL2CPP image");
                return null;
            }

            // fix method pointer in mach-o (always +1, don't know why)
            if (stream is MachOReader)
            {
                il2cpp.MethodPointers = il2cpp.MethodPointers.Select(ptr => ptr - 1).ToArray();
            }

            return new Il2CppProcessor(il2cpp, metadata);
        }

        public GenericIl2CppType GetTypeFromGeneric(GenericIl2CppType pType)
        {
            if (Code.Image.Is64bits)
            {
                var generic = Code.Image.ReadMappedObject<Il2CppGenericClass64>((long)pType.generic_class);
                var pInst = Code.Image.ReadMappedObject<Il2CppGenericInst64>((long)generic.context.class_inst);
                var pointers = Code.Image.ReadMappedArray<ulong>((long)pInst.type_argv, (int)pInst.type_argc);
                var realType = Code.Image.ReadMappedObject<Il2CppType64>((long)pointers[0]);
                realType.Init();
                return new GenericIl2CppType(realType);
            }
            else
            {
                var generic = Code.Image.ReadMappedObject<Il2CppGenericClass>((long)pType.generic_class);
                var pInst = Code.Image.ReadMappedObject<Il2CppGenericInst>(generic.context.class_inst);
                var pointers = Code.Image.ReadMappedArray<uint>(pInst.type_argv, (int)pInst.type_argc);
                var realType = Code.Image.ReadMappedObject<Il2CppType>(pointers[0]);
                realType.Init();
                return new GenericIl2CppType(realType);
            }
        }

        public string GetTypeName(GenericIl2CppType pType)
        {
            string ret;

            if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE)
            {
                Il2CppTypeDefinition klass = Metadata.Types[pType.klassIndex];
                ret = Metadata.GetString(klass.nameIndex);
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
            {
                if (Code.Image.Is64bits)
                {
                    Il2CppGenericClass64 generic_class = Code.Image.ReadMappedObject<Il2CppGenericClass64>((long)pType.generic_class);
                    Il2CppTypeDefinition pMainDef = Metadata.Types[generic_class.typeDefinitionIndex];
                    ret = Metadata.GetString(pMainDef.nameIndex);
                    var typeNames = new List<string>();
                    Il2CppGenericInst64 pInst = Code.Image.ReadMappedObject<Il2CppGenericInst64>((long)generic_class.context.class_inst);
                    var pointers = Code.Image.ReadMappedArray<ulong>((long)pInst.type_argv, (int)pInst.type_argc);
                    for (ulong i = 0; i < pInst.type_argc; ++i)
                    {
                        var pOriType = Code.Image.ReadMappedObject<Il2CppType64>((long)pointers[i]);
                        pOriType.Init();
                        typeNames.Add(GetTypeName(new GenericIl2CppType(pOriType)));
                    }
                    ret += $"<{string.Join(", ", typeNames)}>";
                }
                else
                {
                    Il2CppGenericClass generic_class = Code.Image.ReadMappedObject<Il2CppGenericClass>((long)pType.generic_class);
                    Il2CppTypeDefinition pMainDef = Metadata.Types[generic_class.typeDefinitionIndex];
                    ret = Metadata.GetString(pMainDef.nameIndex);
                    var typeNames = new List<string>();
                    Il2CppGenericInst pInst = Code.Image.ReadMappedObject<Il2CppGenericInst>(generic_class.context.class_inst);
                    var pointers = Code.Image.ReadMappedArray<uint>(pInst.type_argv, (int)pInst.type_argc);
                    for (int i = 0; i < pInst.type_argc; ++i)
                    {
                        var pOriType = Code.Image.ReadMappedObject<Il2CppType>(pointers[i]);
                        pOriType.Init();
                        typeNames.Add(GetTypeName(new GenericIl2CppType(pOriType)));
                    }
                    ret += $"<{string.Join(", ", typeNames)}>";
                }
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY)
            {
                if (Code.Image.Is64bits)
                {
                    var arrayType = Code.Image.ReadMappedObject<Il2CppArrayType64>((long)pType.dataArray);
                    var type = Code.Image.ReadMappedObject<Il2CppType64>((long)arrayType.etype);
                    type.Init();
                    ret = $"{GetTypeName(new GenericIl2CppType(type))}[]";
                }
                else
                {
                    var arrayType = Code.Image.ReadMappedObject<Il2CppArrayType>((long)pType.dataArray);
                    var type = Code.Image.ReadMappedObject<Il2CppType>(arrayType.etype);
                    type.Init();
                    ret = $"{GetTypeName(new GenericIl2CppType(type))}[]";
                }
            }
            else if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY)
            {
                if (Code.Image.Is64bits)
                {
                    var type = Code.Image.ReadMappedObject<Il2CppType64>((long)pType.dataType);
                    type.Init();
                    ret = $"{GetTypeName(new GenericIl2CppType(type))}[]";
                }
                else
                {
                    var type = Code.Image.ReadMappedObject<Il2CppType>((long)pType.dataType);
                    type.Init();
                    ret = $"{GetTypeName(new GenericIl2CppType(type))}[]";
                }
            }
            else
            {
                if ((int)pType.type >= szTypeString.Length)
                    ret = "unknow";
                else
                    ret = szTypeString[(int)pType.type];
            }
            return ret;
        }

        private readonly string[] szTypeString =
        {
            "END",
            "void",
            "bool",
            "char",
            "sbyte",
            "byte",
            "short",
            "ushort",
            "int",
            "uint",
            "long",
            "ulong",
            "float",
            "double",
            "string",
            "PTR",//eg. void*
            "BYREF",
            "VALUETYPE",
            "CLASS",
            "T",
            "ARRAY",
            "GENERICINST",
            "TYPEDBYREF",
            "None",
            "IntPtr",
            "UIntPtr",
            "None",
            "FNPTR",
            "object",
            "SZARRAY",
            "T",
            "CMOD_REQD",
            "CMOD_OPT",
            "INTERNAL",
        };
    }
}
