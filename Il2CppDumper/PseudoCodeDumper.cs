using Il2CppInspector;
using System.IO;
using System.Text;
using System.Linq;

namespace Il2CppDumper
{
    public class PseudoCodeDumper : BaseDumper
    {
        public PseudoCodeDumper(Il2CppProcessor proc) : base(proc) { }

        public void DumpStrings(string outFile)
        {
            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create)))
            {
                foreach (var str in il2cpp.Metadata.Strings)
                {
                    writer.WriteLine(str);
                }
            }
        }

        public void DumpToFile(string outFile) {
            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create))) {
                var enumIdx = this.FindTypeIndex("Enum");

                for (int imageIndex = 0; imageIndex < metadata.Images.Length; imageIndex++) {
                    var imageDef = metadata.Images[imageIndex];
                    writer.Write($"// Image {imageIndex}: {metadata.GetImageName(imageDef)} ({imageDef.typeCount})\n");
                }
                writer.Write("\n");

                var typesByNameSpace = metadata.Types.GroupBy(t => t.namespaceIndex).Select(t => t);
                foreach (var nameSpaceIdx in typesByNameSpace)
                {
                    var nameSpaceName = metadata.GetString(nameSpaceIdx.Key);
                    writer.Write($"namespace {nameSpaceName} {{\n");
                    foreach (var typeDef in nameSpaceIdx)
                    {
                        if (typeDef.parentIndex == enumIdx)
                        {
                            this.WriteEnum(writer, typeDef);
                        }
                        else
                        {
                            this.WriteType(writer, typeDef);
                        }
                    }
                    writer.Write("}\n\n");
                }
            }
        }

        internal void WriteEnum(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            writer.Write("\t");
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == DefineConstants.TYPE_ATTRIBUTE_PUBLIC) writer.Write("public ");
            writer.Write("enum {\n");
            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart + 1; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var defaultValue = this.GetDefaultValue(i);
                writer.Write($"\t\t{metadata.GetString(pField.nameIndex)} = {defaultValue}\n");
            }
            writer.Write("\t}\n\n");
        }

        internal void WriteType(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_SERIALIZABLE) != 0) writer.Write("\t[Serializable]\n");
            writer.Write("\t");
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_VISIBILITY_MASK) == DefineConstants.TYPE_ATTRIBUTE_PUBLIC) writer.Write("public ");
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_ABSTRACT) != 0) writer.Write("abstract ");
            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_SEALED) != 0) writer.Write("sealed ");

            if ((typeDef.flags & DefineConstants.TYPE_ATTRIBUTE_INTERFACE) != 0) writer.Write("interface ");
            else writer.Write("class ");

            var nameSpace = metadata.GetTypeNamespace(typeDef);
            if (nameSpace.Length > 0) nameSpace += ".";

            writer.Write($"{nameSpace}{metadata.GetTypeName(typeDef)}");
            if (typeDef.parentIndex >= 0)
            {
                var pType = il2cpp.Code.GetTypeFromTypeIndex(typeDef.parentIndex);
                var name = il2cpp.GetTypeName(pType);
                if (name != "object")
                {
                    writer.Write($" extends {name}");
                }
            }
            writer.Write("\n\t{\n");

            this.WriteFields(writer, typeDef);
            this.WriteMethods(writer, typeDef);
            
            writer.Write("\t}\n\n");
        }

        internal void WriteFields(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            writer.Write("\t\t// Fields\n");
            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
            for (int i = typeDef.fieldStart; i < fieldEnd; ++i)
            {
                var pField = metadata.Fields[i];
                var pType = il2cpp.Code.GetTypeFromTypeIndex(pField.typeIndex);
                var defaultValue = this.GetDefaultValue(i);
            
                writer.Write("\t\t");
                if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_PRIVATE) == DefineConstants.FIELD_ATTRIBUTE_PRIVATE) writer.Write("private ");
                if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_PUBLIC) == DefineConstants.FIELD_ATTRIBUTE_PUBLIC) writer.Write("public ");
                if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_STATIC) != 0) writer.Write("static ");
                if ((pType.attrs & DefineConstants.FIELD_ATTRIBUTE_INIT_ONLY) != 0) writer.Write("readonly ");

                writer.Write($"{il2cpp.GetTypeName(pType)} {metadata.GetString(pField.nameIndex)}");
                if (defaultValue != null) writer.Write($" = {defaultValue}");
                writer.Write(";\n");
            }
        }

        internal void WriteMethods(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            writer.Write("\t\t// Methods\n");
            var methodEnd = typeDef.methodStart + typeDef.method_count;
            for (int i = typeDef.methodStart; i < methodEnd; ++i)
            {
                var methodDef = metadata.Methods[i];
                writer.Write("\t\t");
                Il2CppType pReturnType = il2cpp.Code.GetTypeFromTypeIndex(methodDef.returnType);
                if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) ==
                    DefineConstants.METHOD_ATTRIBUTE_PRIVATE)
                    writer.Write("private ");
                if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) ==
                    DefineConstants.METHOD_ATTRIBUTE_PUBLIC)
                    writer.Write("public ");
                if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_VIRTUAL) != 0)
                    writer.Write("virtual ");
                if ((methodDef.flags & DefineConstants.METHOD_ATTRIBUTE_STATIC) != 0)
                    writer.Write("static ");

                writer.Write($"{il2cpp.GetTypeName(pReturnType)} {metadata.GetString(methodDef.nameIndex)}(");
                for (int j = 0; j < methodDef.parameterCount; ++j)
                {
                    Il2CppParameterDefinition pParam = metadata.parameterDefs[methodDef.parameterStart + j];
                    string szParamName = metadata.GetString(pParam.nameIndex);
                    Il2CppType pType = il2cpp.Code.GetTypeFromTypeIndex(pParam.typeIndex);
                    string szTypeName = il2cpp.GetTypeName(pType);
                    if ((pType.attrs & DefineConstants.PARAM_ATTRIBUTE_OPTIONAL) != 0)
                        writer.Write("optional ");
                    if ((pType.attrs & DefineConstants.PARAM_ATTRIBUTE_OUT) != 0)
                        writer.Write("out ");
                    if (j != methodDef.parameterCount - 1)
                    {
                        writer.Write($"{szTypeName} {szParamName}, ");
                    }
                    else
                    {
                        writer.Write($"{szTypeName} {szParamName}");
                    }
                }
                writer.Write(");\n");
            }
        }
    }
}
