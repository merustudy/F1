using UnityEngine.InputSystem;

namespace F1.UI
{
    /// <summary>
    /// The key that takes every drop left on the floor straight into the inventory (2026-10-17, the user's request): A, after a
    /// win while the loot lies there. The battle screen polls it every frame; tests press the screen's key method directly.
    /// </summary>
    public static class TakeAllKey
    {
        public static bool Pressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.aKey.wasPressedThisFrame;
        }
    }
}
