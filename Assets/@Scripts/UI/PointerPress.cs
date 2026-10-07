using UnityEngine;
using UnityEngine.InputSystem;

namespace F1.UI
{
    /// <summary>
    /// The frame the pointer was last pressed in (2026-10-07 round 42): the press that closes an item's card. A screen polls
    /// it every frame and closes its card when a press came after the card was shown; the press's own click still does what
    /// it does. Tests simulate a press, since a batch run has no device.
    /// </summary>
    public static class PointerPress
    {
        /// <summary>The frame of the latest press, or -1.</summary>
        public static int LatestFrame { get; private set; } = -1;

        /// <summary>Reads the mouse and the touchscreen. Called from Update; polling twice in a frame is harmless.</summary>
        public static void Poll()
        {
            Mouse mouse = Mouse.current;
            Touchscreen touch = Touchscreen.current;
            if ((mouse != null && mouse.leftButton.wasPressedThisFrame) || (touch != null && touch.primaryTouch.press.wasPressedThisFrame))
            {
                LatestFrame = Time.frameCount;
            }
        }

        /// <summary>True when a press came after the given frame: what closes a card shown in that frame.</summary>
        public static bool PressedSince(int frame)
        {
            return LatestFrame > frame;
        }

        /// <summary>Tests: as if the pointer had been pressed in this frame.</summary>
        internal static void Simulate()
        {
            LatestFrame = Time.frameCount;
        }
    }
}
