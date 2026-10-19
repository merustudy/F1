using UnityEngine.InputSystem;

namespace F1.UI
{
    /// <summary>
    /// The turning of a held thing (Slice B stage 19; Backpack Battles' controls, researched 2026-10-08: right click, the mouse wheel or
    /// R turns what is held): R and the right button turn it a quarter clockwise, the wheel turns it down a quarter clockwise and up a
    /// quarter anticlockwise. Escape lets go: it is back where it was. (2026-10-18 the right button let go too; 2026-10-19 the user wanted it
    /// to turn again.) A screen with a hand polls it every frame while something is held; tests turn the hand and let go directly.
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
                    step++;
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
