using UnityEngine.InputSystem;

namespace F1.UI
{
    /// <summary>
    /// The turning of a held thing (Slice B stage 19; Backpack Battles' controls, researched 2026-10-08: the mouse wheel or R turns what
    /// is held): R turns it a quarter clockwise, the wheel turns it down a quarter clockwise and up a quarter anticlockwise. Escape and the
    /// right button let go (2026-10-18, the user: a right click puts it back where it was; the right button turned it before). A screen with
    /// a hand polls it every frame while something is held; tests turn the hand and let go directly.
    /// </summary>
    public static class TurnInput
    {
        /// <summary>The quarter turns asked for this frame (positive clockwise), and whether letting go was asked for.</summary>
        public static int Poll(out bool cancel)
        {
            cancel = false;
            int step = 0;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    step++;
                }

                cancel = keyboard.escapeKey.wasPressedThisFrame;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    cancel = true;
                }

                float wheel = mouse.scroll.ReadValue().y;
                if (wheel < 0f)
                {
                    step++;
                }
                else if (wheel > 0f)
                {
                    step--;
                }
            }

            return step;
        }
    }
}
