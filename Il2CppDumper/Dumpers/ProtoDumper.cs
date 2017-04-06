using Il2CppInspector;
using Il2CppInspector.Structures;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppDumper.Dumpers
{
    public class ProtoDumper : BaseDumper
    {
        private IEnumerable<Il2CppTypeDefinition> holoTypes;
        private int enumIdx = -1;

        public ProtoDumper(Il2CppProcessor proc) : base(proc) { }
        
        public override void DumpToFile(string outFile) {
            // Find the enum type index
            enumIdx = FindTypeIndex("Enum");
            holoTypes = metadata.Types.Where(t => metadata.GetString(t.namespaceIndex).StartsWith("Holo" + "holo.Rpc")).Select(t => t);
            if (holoTypes.Count() > 0) return;

            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create))) {
                this.WriteHeaders(writer);

                var enums = holoTypes.Where(t => t.parentIndex == enumIdx);
                foreach (var enumObject in enums)
                {
                    this.WriteEnum(writer, enumObject);
                }

                writer.Write("\n");
                
                var messages = holoTypes.Where(t => t.parentIndex != enumIdx);
                foreach (var typeDef in messages)
                {
                    this.WriteType(writer, typeDef);
                }
            }
        }

        internal void WriteHeaders(StreamWriter writer)
        {
            writer.Write("syntax = \"proto3\";\n");
            writer.Write("package Holoholo.Rpc;\n");
        }

        internal void WriteEnum(StreamWriter writer, Il2CppTypeDefinition typeDef, string pad = "")
        {
            writer.Write("\n");
            writer.Write(pad + $"enum {metadata.GetTypeName(typeDef)}\n");
            writer.Write(pad + "{\n");
            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart + 1; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var defaultValue = this.GetDefaultValue(i);
                writer.Write(pad + $"\t{metadata.GetString(pField.nameIndex)} = {defaultValue};\n");
            }
            writer.Write(pad + "}\n");
        }

        internal void WriteType(StreamWriter writer, Il2CppTypeDefinition typeDef, string pad = "")
        {
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0) return;
            var typesToDump = new List<Il2CppType>();
            writer.Write("\n");
            writer.Write(pad + $"message {metadata.GetTypeName(typeDef)}\n");
            writer.Write(pad + "{\n");

            var methodsReturn = new Dictionary<string, Il2CppType>();
            var methodEnd = typeDef.methodStart + typeDef.method_count;
            for (int i = typeDef.methodStart; i < methodEnd; ++i)
            {
                var methodDef = metadata.Methods[i];
                var name = metadata.GetString(methodDef.nameIndex);
                var pReturnType = il2cpp.Code.GetTypeFromTypeIndex(methodDef.returnType);
                methodsReturn[name] = pReturnType;
            }

            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var fieldName = metadata.GetString(pField.nameIndex);
                if (fieldName.EndsWith("FieldNumber"))
                {
                    var realName = fieldName.Substring(0, fieldName.Length - "FieldNumber".Length);
                    var protoIndex = this.GetDefaultValue(i);

                    var getter = "get_" + realName;
                    var pType = methodsReturn[getter];

                    realName = this.ToSnakeCase(realName);
                    var realType = this.GetProtoType(il2cpp.GetTypeName(methodsReturn[getter]), realName);

                    writer.Write(pad + $"\t{realType} {realName} = {protoIndex};\n");

                    if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                    {
                        typesToDump.Add(pType);
                    }
                }
            }
            
            foreach (var pType in typesToDump)
            {
                var realType = pType;
                if (realType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                {
                    var generic = il2cpp.Code.Image.ReadMappedObject<Il2CppGenericClass>(realType.data.generic_class);
                    var pInst = il2cpp.Code.Image.ReadMappedObject<Il2CppGenericInst>(generic.context.class_inst);
                    var pointers = il2cpp.Code.Image.ReadMappedArray<uint>(pInst.type_argv, (int)pInst.type_argc);
                    realType = il2cpp.Code.Image.ReadMappedObject<Il2CppType>(pointers[0]);
                    realType.Init();
                }
                var subtypeDef = metadata.Types[realType.data.klassIndex];
                if (realType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE || realType.type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS)
                {
                    if (!holoTypes.Any(t => t.nameIndex == subtypeDef.nameIndex))
                    {
                        if (subtypeDef.parentIndex == enumIdx)
                        {
                            this.WriteEnum(writer, subtypeDef, pad + "\t");
                        }
                        else
                        {
                            this.WriteType(writer, subtypeDef, pad + "\t");
                        }
                    }
                }
            }

            writer.Write(pad + "}\n");
        }

        internal string GetProtoType(string typeName, string fieldName)
        {
            if (typeName == "int")
            {
                typeName = "int32";
            }
            else if (typeName == "long")
            {
                typeName = "int64";
            }
            else if (typeName == "uint")
            {
                typeName = "uint32";
            }
            else if (typeName == "ByteString")
            {
                typeName = "bytes";
            }
            else if (typeName == "ulong")
            {
                typeName = "fixed64"; // "uint64";
            }
            else if (typeName.StartsWith("FieldCodec`1"))
            {
                typeName = typeName.Substring("FieldCodec`1".Length + 1, typeName.Length - "FieldCodec`1".Length - 2);
                typeName = "repeated " + this.GetProtoType(typeName, fieldName);
            }
            else if (typeName.StartsWith("RepeatedField`1"))
            {
                typeName = typeName.Substring("RepeatedField`1".Length + 1, typeName.Length - "RepeatedField`1".Length - 2);
                typeName = "repeated " + this.GetProtoType(typeName, fieldName);
            }

            // depending on field name, adjust type

            string[] uint64names = { "timeStamp", "timeStamp", "page_timestamp", "game_master_timestamp", "asset_digest_timestamp", "cell_id", "s2_cell_id" };
            if (typeName == "fixed64" && uint64names.Contains(fieldName)) 
            {
                typeName = "uint64";
            }

            string[] fixed32names = { "checksum" };
            if (typeName == "uint32" && fixed32names.Contains(fieldName))
            {
                typeName = "fixed32";
            }

            return typeName;
        }
    }
}
