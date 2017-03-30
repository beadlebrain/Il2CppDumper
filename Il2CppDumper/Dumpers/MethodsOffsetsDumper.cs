using Il2CppInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        this.WriteType(writer, typeDef);
                    }
                }
                writer.Write("}\n\n");
            }

        private void WriteType(StreamWriter writer, Il2CppTypeDefinition typeDef)
        {
            
        }
    }
    }
}
