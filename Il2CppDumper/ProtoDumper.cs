using Il2CppInspector;
using System.IO;
using System.Text;
using System.Linq;

namespace Il2CppDumper
{
    public class ProtoDumper : BaseDumper
    {
        public ProtoDumper(Il2CppProcessor proc) : base(proc) { }
        
        public void DumpToFile(string outFile) {
            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create))) {
                this.WriteHeaders(writer);

                // Find the enum type index
                var enumIdx = FindTypeIndex("Enum");

                var holo = metadata.Types.Where(t => metadata.GetString(t.namespaceIndex).StartsWith("Holoholo.Rpc")).Select(t => t);

                var enums = holo.Where(t => t.parentIndex == enumIdx);
                foreach (var enumObject in enums)
                {
                    this.WriteEnum(writer, enumObject);
                }

                var messages = holo.Where(t => t.parentIndex != enumIdx);
                foreach (var typeDef in messages)
                {
                    this.WriteType(writer, typeDef);
                }
            }
        }

        internal void WriteHeaders(StreamWriter writer)
        {
            writer.Write("syntax = \"proto3\";\n");
            writer.Write("package Holoholo.Rpc;\n\n");
        }

        internal void WriteEnum(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            writer.Write($"enum {metadata.GetTypeName(typeDef)}\n{{\n");
            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart + 1; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var defaultValue = this.GetDefaultValue(i);
                writer.Write($"\t{metadata.GetString(pField.nameIndex)} = {defaultValue};\n");
            }
            writer.Write("}\n\n");
        }

        internal void WriteType(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0) return;

            writer.Write($"message {metadata.GetTypeName(typeDef)}\n{{\n");
            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var fieldName = metadata.GetString(pField.nameIndex);
                if (fieldName.EndsWith("FieldNumber"))
                {
                    var realName = fieldName.Substring(0, fieldName.Length - "FieldNumber".Length);
                    var defaultValue = this.GetDefaultValue(i);

                    var realField = metadata.Fields[++i];
                    var pType = il2cpp.Code.GetTypeFromTypeIndex(realField.typeIndex);
                    var realType = this.GetProtoType(il2cpp.GetTypeName(pType));

                    writer.Write($"\t{realType} {this.ToSnakeCase(realName)} = {defaultValue};\n");
                }
            }
            writer.Write("}\n\n");
        }

        internal string GetProtoType(string typeName)
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
                typeName = "uint64";
            }
            else if (typeName.StartsWith("FieldCodec`1"))
            {
                typeName = typeName.Substring("FieldCodec`1".Length + 1, typeName.Length - "FieldCodec`1".Length - 2);
                typeName = "repeated " + this.GetProtoType(typeName);
            }
            return typeName;
        }
    }
}
