/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NLog;
using System;
using System.IO;
using System.Linq;

namespace Il2CppInspector.Readers
{
    internal class MachOReader : FileFormatReader<MachOReader>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public MachOReader(Stream stream) : base(stream) { }

        public override string Arch {
            get {
                return "ARM";
            }
        }

        protected override bool Init() {
            var magic = ReadUInt32();

            if (magic == MachOConstants.MH_MAGIC ||
                magic == MachOConstants.MH_CIGAM ||
                magic == MachOConstants.MH_MAGIC_64 ||
                magic == MachOConstants.MH_CIGAM_64)
            {
                // Macho O
                return true;
            }

            if (magic == MachOConstants.FAT_MAGIC || magic == MachOConstants.FAT_CIGAM)
            {
                // Fat MachO
                return true;
            }

            logger.Debug("Invalid magic detected in MachO files: 0x{0}", magic.ToString("X"));

            return false;
        }

        public override uint[] GetSearchLocations() {
            return null;
        }

        public override uint MapVATR(uint uiAddr)
        {
            return 0;            
        }
    }
}
