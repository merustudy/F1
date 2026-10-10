using UnityEngine.InputSystem;

namespace F1.UI
{
    /// <summary>
    /// The inventory's key (round 52): I opens and closes the inventory window wherever its button is (the node map, the battle screen after a
    /// win). A screen polls it every frame; tests press the screen's key method directly.
    /// </summary>
    public static class InventoryKey
    {
        public static bool Pressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.iKey.wasPressedThisFrame;
        }
    }
}
