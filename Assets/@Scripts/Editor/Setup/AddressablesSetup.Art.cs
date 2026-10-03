using System.Collections.Generic;

namespace F1.Editor.Setup
{
    public static partial class AddressablesSetup
    {
        /// <summary>One entry per piece of art the static data names (figures and backgrounds), taken from ArtSetup.</summary>
        static partial void AddArtEntries(List<AddressEntry> entries)
        {
            entries.AddRange(ArtSetup.Entries());
        }
    }
}
