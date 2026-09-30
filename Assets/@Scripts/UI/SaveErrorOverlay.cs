using F1.Core;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// Covers every screen while the last change could not be saved. Nothing else can be clicked
    /// until the save is retried successfully, so the game never moves past an unconfirmed state.
    /// </summary>
    public sealed class SaveErrorOverlay : MonoBehaviour
    {
        public const string Address = "ui/app/save-error-overlay";

        [SerializeField] GameObject _panel;
        [SerializeField] Button _retry;
        [SerializeField] Button _quit;

        bool _opened;

        /// <summary>True while the overlay is covering the screen.</summary>
        public bool IsShown => _panel.activeSelf;

        /// <summary>Called once by UIManager right after the overlay was created.</summary>
        public void Open()
        {
            _opened = true;
            _retry.onClick.AddListener(OnRetry);
            _quit.onClick.AddListener(Application.Quit);
            Managers.Run.SaveBlockedChanged += Show;
            Show(Managers.Run.IsSaveBlocked);
        }

        void OnDestroy()
        {
            if (_opened && Managers.IsConfigured)
            {
                Managers.Run.SaveBlockedChanged -= Show;
            }
        }

        void Show(bool blocked)
        {
            _panel.SetActive(blocked);
        }

        void OnRetry()
        {
            // On success RunManager raises SaveBlockedChanged(false) and the overlay goes away.
            Managers.Run.RetrySave();
        }
    }
}
