using Il2CppInspector;
using Il2CppInspector.Structures;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace Il2CppDumper.Dumpers
{
    internal class Il2CppArrayOf
    {
        public string Name { get; set; }
        public string ItemType { get; set; }
    }

    public class StructDumper : BaseDumper
    {
        private IEnumerable<Il2CppTypeDefinition> holoTypes;
        private int enumIdx = -1;
        private List<GenericIl2CppType> typesToDump = new List<GenericIl2CppType>();
        private List<Il2CppArrayOf> arrayTypesToDump = new List<Il2CppArrayOf>();

        public StructDumper(Il2CppProcessor proc) : base(proc) { }
        
        public override void DumpToFile(string outFile) {
            enumIdx = FindTypeIndex("Enum");
            holoTypes = metadata.Types.Where(t => metadata.GetString(t.namespaceIndex).StartsWith("Holo" + "holo.Rpc")).Select(t => t);
            if (holoTypes.Count() == 0) return;

            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create))) {
                this.WriteHeaders(writer);

                // dump holo types
                var types = holoTypes.Where(t => t.parentIndex != enumIdx);
                foreach (var typeDef in types)
                {
                    this.WriteType(writer, typeDef);
                }

                // dump subtypes
                for (var i = 0; i < typesToDump.Count(); i++)
                {
                    var realType = typesToDump[i];
                    if (realType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                    {
                        realType = il2cpp.GetTypeFromGeneric(realType);
                    }
                    var subtypeDef = metadata.Types[realType.klassIndex];
                    if (realType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE || realType.type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS)
                    {
                        if (!holoTypes.Any(t => t.nameIndex == subtypeDef.nameIndex))
                        {
                            if (subtypeDef.parentIndex != enumIdx)
                            {
                                this.WriteType(writer, subtypeDef);
                            }
                        }
                    }
                }

                // dump array types
                foreach (var pType in arrayTypesToDump)
                {
                    writer.Write($"struct {pType.Name} : public Il2CppArray\n");
                    writer.Write("{\n");
                    writer.Write($"\tALIGN_FIELD(8) {pType.ItemType} items;\n");
                    writer.Write("}\n\n");
                }
            }
        }

        internal void WriteHeaders(StreamWriter writer)
        {
            writer.Write("struct Il2CppObject\n");
            writer.Write("{\n");
            writer.Write("\tIl2CppClass *klass;\n");
            writer.Write("\tMonitorData *monitor;\n");
            writer.Write("}\n\n");

            writer.Write("struct Il2CppArray : public Il2CppObject\n");
            writer.Write("{\n");
            writer.Write("\tvoid *bounds;\n");
            writer.Write("\tint max_length;\n");
            writer.Write("}\n\n");

            writer.Write("struct Il2CppString\n");
            writer.Write("{\n");
            writer.Write("\tIl2CppObject object;\n");
            writer.Write("\tint length;\n");
            writer.Write("\tchar16_t *chars;\n");
            writer.Write("}\n\n");
    }

        internal void WriteType(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_INTERFACE) != 0) return;
           
            var nameSpace = metadata.GetTypeNamespace(typeDef);
            if (nameSpace.Length > 0) nameSpace += ".";

            writer.Write($"struct {metadata.GetTypeName(typeDef)}");
            
            if (typeDef.parentIndex >= 0)
            {
                var pType = il2cpp.Code.GetTypeFromTypeIndex(typeDef.parentIndex);
                var name = il2cpp.GetTypeName(pType);
                if (name != "object")
                {
                    writer.Write($" : public {name}");
                }
                else
                {
                    writer.Write($" : public Il2CppObject");
                }
            }

            writer.Write("\n{\n");

            this.WriteFields(writer, typeDef);

            writer.Write("}\n\n");
        }

        internal void WriteFields(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            if (typeDef.field_count <= 0) return;

            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var pType = il2cpp.Code.GetTypeFromTypeIndex(pField.typeIndex);

                if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_STATIC) == 0)
                {
                    var fieldname = metadata.GetString(pField.nameIndex);
                    var typename = this.GetStructType(il2cpp.GetTypeName(pType), fieldname);
                    writer.Write($"\t{typename} {fieldname};\n");

                    if (pType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE || pType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                    {
                        this.AddTypeToDump(pType);
                    }
                }
            }
        }

        private void AddTypeToDump(GenericIl2CppType pType)
        {
            if (!typesToDump.Any(t => t.klassIndex == pType.klassIndex))
            {
                typesToDump.Add(pType);
            }
        }

        private void AddArrayTypeToDump(string name, string itemType)
        {
            if (!arrayTypesToDump.Any(t => t.ItemType == itemType))
            {
                arrayTypesToDump.Add(new Il2CppArrayOf()
                {
                    Name = name,
                    ItemType = itemType,
                });
            }
        }

        internal string GetStructType(string typeName, string fieldName)
        {
            string[] types = { "int", "uint", "long", "ulong" };
            if (typeName == "int" || typeName == "long")
            {
                //
            }
            else if (typeName == "uint" || typeName == "ulong")
            {
                typeName = "unsigned " + typeName.Substring(1);
            }
            else if (typeName == "string")
            {
                typeName = "Il2CppString *";
            }
            else if (typeName.StartsWith("FieldCodec`1"))
            {
                typeName = typeName.Substring("FieldCodec`1".Length + 1, typeName.Length - "FieldCodec`1".Length - 2);
                var itemType = this.GetStructType(typeName, fieldName);
                typeName = "Il2CppArrayOf" + itemType;
                AddArrayTypeToDump(typeName, itemType);
            }
            else if (typeName.StartsWith("RepeatedField`1"))
            {
                typeName = typeName.Substring("RepeatedField`1".Length + 1, typeName.Length - "RepeatedField`1".Length - 2);
                var itemType = this.GetStructType(typeName, fieldName);
                typeName = "Il2CppArrayOf" + itemType;
                AddArrayTypeToDump(typeName, itemType);
            }
            else
            {
                typeName = typeName + " *";
            }
            
            return typeName;
        }
    }
}
