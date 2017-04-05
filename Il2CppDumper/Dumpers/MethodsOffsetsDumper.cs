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
                var enumIdx = this.FindTypeIndex("Enum");
                foreach (var typeDef in metadata.Types)
                {
                    if (typeDef.parentIndex != enumIdx)
                    {
                        this.WriteType(writer, typeDef);
                    }
                }
                writer.Write("}\n\n");
            }
        }

        private void WriteType(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            
        }
    }
}
