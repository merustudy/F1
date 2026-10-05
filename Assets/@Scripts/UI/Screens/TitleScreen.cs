using F1.Core;
using F1.Data;
using F1.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>The first screen: new run, continue and quit, and a row of settings: language, music and effects.</summary>
    public sealed class TitleScreen : UIScreen
    {
        [SerializeField] Button _newRun;
        [SerializeField] Button _continue;
        [SerializeField] Button _language;
        [SerializeField] TMP_Text _languageLabel;
        [SerializeField] Button _music;
        [SerializeField] TMP_Text _musicLabel;
        [SerializeField] Button _effects;
        [SerializeField] TMP_Text _effectsLabel;
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
            _music.onClick.AddListener(OnMusic);
            _effects.onClick.AddListener(OnEffects);
            _quit.onClick.AddListener(Application.Quit);
            _confirmYes.onClick.AddListener(StartNewRun);
            _confirmNo.onClick.AddListener(() => _confirmPanel.SetActive(false));
            _confirmPanel.SetActive(false);
            Managers.Sound.PlayMusic(MusicTrack.Lobby);
        }

        public override void Refresh()
        {
            _continue.gameObject.SetActive(Managers.Run.HasRun);
            _languageLabel.text = UiStrings.Get(UiKeys.Title.Language, UiStrings.Get(UiKeys.Title.LanguageName));
            _musicLabel.text = UiStrings.Get(UiKeys.Title.Music, VolumeText(Managers.Setting.MusicVolume));
            _effectsLabel.text = UiStrings.Get(UiKeys.Title.Effects, VolumeText(Managers.Setting.EffectVolume));
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

        /// <summary>On, low or off: the step of the title's volume buttons nearest the volume (Docs/Design/12 §3).</summary>
        static string VolumeText(int volume)
        {
            switch (VolumeLevels.Nearest(volume))
            {
                case VolumeLevels.Full: return UiStrings.Get(UiKeys.Title.VolumeOn);
                case VolumeLevels.Low: return UiStrings.Get(UiKeys.Title.VolumeLow);
                default: return UiStrings.Get(UiKeys.Title.VolumeOff);
            }
        }

        /// <summary>Steps the music's volume: on, low, off, on. If the setting cannot be saved nothing changes and the notice says so.</summary>
        void OnMusic()
        {
            ChangeVolume(() => Managers.Setting.ChangeMusicVolume(VolumeLevels.Next(Managers.Setting.MusicVolume)));
        }

        /// <summary>Steps the effects' volume: on, low, off, on.</summary>
        void OnEffects()
        {
            ChangeVolume(() => Managers.Setting.ChangeEffectVolume(VolumeLevels.Next(Managers.Setting.EffectVolume)));
        }

        void ChangeVolume(System.Action change)
        {
            try
            {
                change();
                Refresh();
            }
            catch (SaveWriteException)
            {
                _notice.text = UiStrings.Get(UiKeys.Title.VolumeNotSaved);
            }
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
            catch (SaveWriteException)
            {
                // SettingManager put the language back; the player is told why nothing changed.
                _notice.text = UiStrings.Get(UiKeys.Title.SettingsNotSaved);
            }
            finally
            {
                _changingLocale = false;
            }
        }
    }
}
