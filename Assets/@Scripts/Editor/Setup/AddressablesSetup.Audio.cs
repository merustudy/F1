using System.Collections.Generic;

namespace F1.Editor.Setup
{
    public static partial class AddressablesSetup
    {
        /// <summary>One entry per wired sound, taken from AudioSetup (and so from SoundCatalog).</summary>
        static partial void AddAudioEntries(List<AddressEntry> entries)
        {
            entries.AddRange(AudioSetup.Entries());
        }
    }
}
