/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector.Readers
{
    internal class MachOConstants
    {
        internal const uint MH_MAGIC = 0xFEEDFACE;
        internal const uint MH_CIGAM = 0xCEFAEDFE;

        internal const uint MH_MAGIC_64 = 0xFEEDFACF;
        internal const uint MH_CIGAM_64 = 0xCFFAEDFE;
        
        internal const uint FAT_MAGIC = 0xCAFEBABE;
        internal const uint FAT_CIGAM = 0xBEBAFECA;
    }
}
