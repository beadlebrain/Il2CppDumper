/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NoisyCowStudios.Bin2Object;
using Il2CppInspector.Structures;
using System.Collections.Generic;

namespace Il2CppInspector
{
    public class Metadata : BinaryObjectReader
    {
        private Il2CppGlobalMetadataHeader pMetadataHdr;

        public Il2CppImageDefinition[] Images { get; }
        public Il2CppTypeDefinition[] Types { get; }
        public Il2CppMethodDefinition[] Methods { get; }
        public Il2CppParameterDefinition[] parameterDefs;
        public Il2CppFieldDefinition[] Fields { get; }
        public Il2CppFieldDefaultValue[] fieldDefaultValues;
        public string[] Strings { get; }
        public Dictionary<int, Il2CppTypeDefinition> Interfaces { get; }
        public uint[] EncodedMethods { get; }

        public string GetImageName(Il2CppImageDefinition image) => GetString(image.nameIndex);
        public string GetTypeNamespace(Il2CppTypeDefinition type) => GetString(type.namespaceIndex);
        public string GetTypeName(Il2CppTypeDefinition type) => GetString(type.nameIndex);
        
        public Metadata(Stream stream) : base(stream)
        {
            pMetadataHdr = ReadObject<Il2CppGlobalMetadataHeader>();
            if (pMetadataHdr.sanity != 0xFAB11BAF)
            {
                throw new Exception("ERROR: Metadata file supplied is not valid metadata file.");
            }
            if (pMetadataHdr.version != 21 && pMetadataHdr.version != 22)
            {
                throw new Exception($"ERROR: Metadata file supplied is not a supported version[{pMetadataHdr.version}].");
            }

            // Strings literals
            var uiStringLiteralCount = pMetadataHdr.stringLiteralCount / MySizeOf(typeof(Il2CppStringLiteral));
            var stringDefs = ReadArray<Il2CppStringLiteral>(pMetadataHdr.stringLiteralOffset, uiStringLiteralCount);
            Strings = new string[stringDefs.Length];
            for (var idx = 0; idx < stringDefs.Length; idx++)
            {
                var raw = ReadArray<byte>(pMetadataHdr.stringLiteralDataOffset + stringDefs[idx].dataIndex, (int)stringDefs[idx].length);
                Strings[idx] = System.Text.Encoding.UTF8.GetString(raw);
            }

            // Images (.dll)
            var uiImageCount = pMetadataHdr.imagesCount / MySizeOf(typeof(Il2CppImageDefinition));
            Images = ReadArray<Il2CppImageDefinition>(pMetadataHdr.imagesOffset, uiImageCount);

            // Interfaces
            Interfaces = new Dictionary<int, Il2CppTypeDefinition>();
            //var uiInterfacePairCount = pMetadataHdr.interfaceOffsetsCount / MySizeOf(typeof(Il2CppInterfaceOffsetPair));
            //var interfacePairs = ReadArray<Il2CppInterfaceOffsetPair>(pMetadataHdr.interfaceOffsetsOffset, uiInterfacePairCount);
            //var uiInterfaceCount = pMetadataHdr.interfacesCount / MySizeOf(typeof(Il2CppTypeDefinition));
            //var interfaceDefs = ReadArray<Il2CppTypeDefinition>(pMetadataHdr.interfacesOffset, uiInterfaceCount);
            //for (var i = 0; i < interfacePairs.Count(); i++)
            //{
            //    Interfaces[i] = interfaceDefs[interfacePairs[i].interfaceTypeIndex];
            //}

            // EncodedMethods
            //EncodedMethods = ReadArray<uint>(pMetadataHdr.vtableMethodsOffset, pMetadataHdr.vtableMethodsCount);

            // GetTypeDefFromIndex
            var uiNumTypes = pMetadataHdr.typeDefinitionsCount / MySizeOf(typeof(Il2CppTypeDefinition));
            Types = ReadArray<Il2CppTypeDefinition>(pMetadataHdr.typeDefinitionsOffset, uiNumTypes);
            
            // GetMethodDefinition
            Methods = ReadArray<Il2CppMethodDefinition>(pMetadataHdr.methodsOffset, pMetadataHdr.methodsCount / MySizeOf(typeof(Il2CppMethodDefinition)));
            
            // GetParameterFromIndex
            parameterDefs = ReadArray<Il2CppParameterDefinition>(pMetadataHdr.parametersOffset, pMetadataHdr.parametersCount / MySizeOf(typeof(Il2CppParameterDefinition)));
            
            // GetFieldDefFromIndex
            Fields = ReadArray<Il2CppFieldDefinition>(pMetadataHdr.fieldsOffset, pMetadataHdr.fieldsCount / MySizeOf(typeof(Il2CppFieldDefinition)));

            // GetFieldDefaultFromIndex
            fieldDefaultValues = ReadArray<Il2CppFieldDefaultValue>(pMetadataHdr.fieldDefaultValuesOffset, pMetadataHdr.fieldDefaultValuesCount / MySizeOf(typeof(Il2CppFieldDefaultValue)));
        }

        public Il2CppFieldDefaultValue GetFieldDefaultFromIndex(int idx)
        {
            return fieldDefaultValues.FirstOrDefault(x => x.fieldIndex == idx);
        }

        public int GetDefaultValueFromIndex(int idx)
        {
            return pMetadataHdr.fieldAndParameterDefaultValueDataOffset + idx;
        }

        public string GetString(int idx)
        {
            return ReadNullTerminatedString(pMetadataHdr.stringOffset + idx);
        }

        private int MySizeOf(Type type)
        {
            int size = 0;
            foreach (var i in type.GetTypeInfo().GetFields())
            {
                if (i.FieldType == typeof(int))
                {
                    size += 4;
                }
                else if (i.FieldType == typeof(uint))
                {
                    size += 4;
                }
                else if (i.FieldType == typeof(short))
                {
                    size += 2;
                }
                else if (i.FieldType == typeof(ushort))
                {
                    size += 2;
                }
                else
                {
                    throw new Exception("unimplemented");
                }
            }
            return size;
        }
    }
}
