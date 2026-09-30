using F1.Core;
using F1.Data;
using F1.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>The first screen: new run, continue, language and quit.</summary>
    public sealed class TitleScreen : UIScreen
    {
        [SerializeField] Button _newRun;
        [SerializeField] Button _continue;
        [SerializeField] Button _language;
        [SerializeField] TMP_Text _languageLabel;
        [SerializeField] Button _quit;
        [SerializeField] TMP_Text _notice;
        [SerializeField] GameObject _confirmPanel;
        [SerializeField] Button _confirmYes;
        [SerializeField] Button _confirmNo;

        bool _changingLocale;

        protected override void OnOpen()
        {
            _newRun.onClick.AddListener(OnNewRun);
            _continue.onClick.AddListener(OnContinue);
            _language.onClick.AddListener(OnLanguage);
            _quit.onClick.AddListener(Application.Quit);
            _confirmYes.onClick.AddListener(StartNewRun);
            _confirmNo.onClick.AddListener(() => _confirmPanel.SetActive(false));
            _confirmPanel.SetActive(false);
        }

        public override void Refresh()
        {
            _continue.gameObject.SetActive(Managers.Run.HasRun);
            _languageLabel.text = UiStrings.Get(UiKeys.Title.Language, UiStrings.Get(UiKeys.Title.LanguageName));
            _notice.text = SaveNotice();
        }

        /// <summary>What the player should know about the save file that was found at startup.</summary>
        static string SaveNotice()
        {
            switch (Managers.Run.LoadStatus)
            {
                case SaveLoadStatus.Corrupt when !Managers.Run.HasRun:
                    return UiStrings.Get(UiKeys.Title.SaveUnreadable);
                case SaveLoadStatus.RestoredFromBackup:
                    return UiStrings.Get(UiKeys.Title.SaveRestored);
                default:
                    return string.Empty;
            }
        }

        void OnNewRun()
        {
            // A new run replaces the one in progress, so ask first.
            if (Managers.Run.HasRun)
            {
                _confirmPanel.SetActive(true);
            }
            else
            {
                StartNewRun();
            }
        }

        void StartNewRun()
        {
            Managers.Run.StartNewRun();
            GoToCurrentPhase();
        }

        void OnContinue()
        {
            GoToCurrentPhase();
        }

        /// <summary>Switches to the next supported language. The screen redraws when LocaleChanged arrives.</summary>
        async void OnLanguage()
        {
            if (_changingLocale)
            {
                return;
            }

            _changingLocale = true;
            try
            {
                int index = 0;
                for (int i = 0; i < LocalePolicy.SupportedCodes.Count; i++)
                {
                    if (LocalePolicy.SupportedCodes[i] == Managers.Setting.LocaleCode)
                    {
                        index = i;
                    }
                }

                string next = LocalePolicy.SupportedCodes[(index + 1) % LocalePolicy.SupportedCodes.Count];
                await Managers.Setting.ChangeLocaleAsync(next);
            }
            finally
            {
                _changingLocale = false;
            }
        }
    }
}
