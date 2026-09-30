using System.Collections.Generic;
using F1.UI;

namespace F1.Editor.Setup
{
    public static partial class AddressablesSetup
    {
        /// <summary>One entry per screen prefab, taken from ScreenCatalog.</summary>
        static partial void AddUiEntries(List<AddressEntry> entries)
        {
            foreach (ScreenId id in ScreenCatalog.All)
            {
                entries.Add(new AddressEntry(
                    UiPrefabSetup.PrefabPath(id),
                    ScreenCatalog.Address(id),
                    ScreenCatalog.Scope(id),
                    UiGroup));
            }
        }
    }
}
