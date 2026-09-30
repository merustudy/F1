using F1.Core;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// A full screen. It shows state and sends commands to the managers; it computes no rule.
    /// UIManager creates it, calls <see cref="Open"/> once and destroys it when another screen is shown.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        bool _opened;

        /// <summary>Called once, right after the screen was created.</summary>
        public void Open()
        {
            _opened = true;
            Managers.Setting.LocaleChanged += HandleLocaleChanged;
            OnOpen();
            Refresh();
        }

        /// <summary>
        /// Lets the screen take clicks or not. UIManager turns clicks off while it replaces the screen,
        /// so a second click cannot send a command twice.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            var group = GetComponent<CanvasGroup>();
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        /// <summary>Redraws everything from the current state and locale.</summary>
        public abstract void Refresh();

        /// <summary>Wire buttons here. Called once before the first <see cref="Refresh"/>.</summary>
        protected abstract void OnOpen();

        protected virtual void OnDestroy()
        {
            if (_opened && Managers.IsConfigured)
            {
                Managers.Setting.LocaleChanged -= HandleLocaleChanged;
            }
        }

        /// <summary>Replaces this screen. Further clicks on this screen are ignored from now on.</summary>
        protected async void GoTo(ScreenId id)
        {
            if (Managers.UI.IsBusy)
            {
                return;
            }

            await Managers.UI.ShowAsync(id);
        }

        /// <summary>Goes to the screen of the current game phase. Call it after a command that may change the phase.</summary>
        protected void GoToCurrentPhase()
        {
            GoTo(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
        }

        void HandleLocaleChanged(string localeCode)
        {
            Refresh();
        }
    }
}
