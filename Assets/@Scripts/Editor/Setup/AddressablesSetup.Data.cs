using System.Collections.Generic;
using F1.Core;
using F1.Data;

namespace F1.Editor.Setup
{
    public static partial class AddressablesSetup
    {
        /// <summary>One entry per static data definition, taken from StaticDataFiles.</summary>
        static partial void AddDataEntries(List<AddressEntry> entries)
        {
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                entries.Add(new AddressEntry(
                    StaticDataFiles.GeneratedDirectory + "/" + file.GeneratedFileName,
                    file.Address,
                    ResourceScope.App,
                    DataGroup));
            }
        }
    }
}
