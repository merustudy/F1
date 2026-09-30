using System.Collections.Generic;
using F1.Core;
using F1.UI;

namespace F1.Editor.Setup
{
    public static partial class AddressablesSetup
    {
        /// <summary>One entry per screen prefab, taken from ScreenCatalog, and the save error overlay.</summary>
        static partial void AddUiEntries(List<AddressEntry> entries)
        {
            entries.Add(new AddressEntry(UiPrefabSetup.SaveErrorOverlayPath, SaveErrorOverlay.Address, ResourceScope.App, UiGroup));

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
