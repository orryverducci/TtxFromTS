using System;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// The character subsets available for a teletext page to be displayed in.
    /// </summary>
    public enum CharacterSubset
    {
        English = 0x00,
        German = 0x01,
        SwedishFinishHugarian = 0x02,
        Italian = 0x03,
        French = 0x04,
        PortugeseSpanish = 0x05,
        CzechSlovak = 0x06
    }
}
