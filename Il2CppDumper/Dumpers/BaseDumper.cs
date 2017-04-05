using Il2CppInspector;
using Il2CppInspector.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Il2CppDumper.Dumpers
{
    public abstract class BaseDumper
    {
        internal readonly Il2CppProcessor il2cpp;
        internal readonly Metadata metadata;

        public BaseDumper(Il2CppProcessor proc)
        {
            il2cpp = proc;
            metadata = il2cpp.Metadata;
        }

        internal int FindTypeIndex(string name)
        {
            for (var idx = 0; idx < il2cpp.Code.PtrMetadataRegistration.types.Length; idx++)
            {
                var typename = il2cpp.GetTypeName(il2cpp.Code.PtrMetadataRegistration.types[idx]);
                if (typename == name) return idx;
            }
            return -1;
        }

        internal string GetDefaultValue(int fieldIdx)
        {
            var pDefault = metadata.GetFieldDefaultFromIndex(fieldIdx);
            if (pDefault != null && pDefault.dataIndex != -1)
            {
                var pointer = metadata.GetDefaultValueFromIndex(pDefault.dataIndex);
                Il2CppType pTypeToUse = il2cpp.Code.GetTypeFromTypeIndex(pDefault.typeIndex);
                if (pointer > 0)
                {
                    metadata.Position = pointer;
                    object multi = null;
                    switch (pTypeToUse.type)
                    {
                        case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
                            multi = metadata.ReadBoolean();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_U1:
                        case Il2CppTypeEnum.IL2CPP_TYPE_I1:
                            multi = metadata.ReadByte();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
                            multi = metadata.ReadChar();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_U2:
                            multi = metadata.ReadUInt16();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_I2:
                            multi = metadata.ReadInt16();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_U4:
                            multi = metadata.ReadUInt32();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                            multi = metadata.ReadInt32();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_U8:
                            multi = metadata.ReadUInt64();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_I8:
                            multi = metadata.ReadInt64();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_R4:
                            multi = metadata.ReadSingle();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_R8:
                            multi = metadata.ReadDouble();
                            break;
                        case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                            var uiLen = metadata.ReadInt32();
                            multi = Encoding.UTF8.GetString(metadata.ReadBytes(uiLen));
                            break;
                    }
                    if (multi is string) return "\"{multi}\"";
                    else if (multi != null) return multi.ToString();
                }
            }
            return null;
        }

        internal string ToSnakeCase(string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        public abstract void DumpToFile(string filename);
    }
}
