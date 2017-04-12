using Il2CppInspector;
using Il2CppInspector.Structures;
using System.IO;

namespace Il2CppDumper.Dumpers
{
    public class MethodsOffsetsDumper : BaseDumper
    {
        public MethodsOffsetsDumper(Il2CppProcessor proc) : base(proc) { }

        public override void DumpToFile(string filename)
        {
            using (var writer = new StreamWriter(new FileStream(filename, FileMode.Create)))
            {
                foreach(var typeDef in metadata.Types)
                {
                    var nameSpace = metadata.GetTypeNamespace(typeDef);
                    if (nameSpace.Length > 0) nameSpace += ".";
                    var typeName = nameSpace + metadata.GetTypeName(typeDef);

                    var methodEnd = typeDef.methodStart + typeDef.method_count;
                    for (int i = typeDef.methodStart; i < methodEnd; ++i)
                    {
                        var methodDef = metadata.Methods[i];
                        var methodName = metadata.GetString(methodDef.nameIndex);
                        if (methodDef.methodIndex >= 0)
                        {
                            var ptr = il2cpp.Code.MethodPointers[methodDef.methodIndex];
                            writer.Write("{0}{1} 0x{2:x}\n", typeName, methodName, ptr);
                        }
                    }
                    writer.Write("\n");
                }
            }
        }


    }
}
