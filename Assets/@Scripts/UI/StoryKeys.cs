using UnityEngine.InputSystem;

namespace F1.UI
{
    /// <summary>
    /// The keys of the story (Slice D stage 55, Round 101 판정 3): space goes on (the next scene, the next line), Esc skips to the end.
    /// A click goes on as well (the screens' catchers). Tests call the screens' methods directly.
    /// </summary>
    public static class StoryKeys
    {
        public static bool Next()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
        }

        public static bool Skip()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }
    }
}
