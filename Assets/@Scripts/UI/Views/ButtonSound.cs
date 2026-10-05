using F1.Core;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The click of a button (Docs/Architecture/14_SOUND.md "소리를 내는 자리"). The screen builder puts one on every button.
    /// A button whose command has its own sound (setting out, entering a node, putting an item away) is built silent, and
    /// its screen plays that sound when the command is done.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSound : MonoBehaviour
    {
        [SerializeField] bool _silent;

        public bool Silent => _silent;

        void Awake()
        {
            if (!_silent)
            {
                GetComponent<Button>().onClick.AddListener(Play);
            }
        }

        static void Play()
        {
            if (Managers.IsConfigured)
            {
                Managers.Sound.PlayEffect(SoundEffect.Button);
            }
        }
    }
}
