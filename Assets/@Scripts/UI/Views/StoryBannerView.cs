using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The intro's last scene over the run's first lobby (Slice D stage 55, the Round 98 mockup): it comes up out of the intro's black, a band over
    /// the header says "일식까지 N주" at 0.8 s, a box at the bottom says the scene's guide line at 1.6 s, and it goes after the scene's time. A click
    /// closes it at once. The words are the scene's line (data) and the countdown's UI text.
    /// </summary>
    public sealed class StoryBannerView : MonoBehaviour
    {
        [SerializeField] Image _cover;
        [SerializeField] CanvasGroup _countdownGroup;
        [SerializeField] TMP_Text _countdown;
        [SerializeField] CanvasGroup _guideGroup;
        [SerializeField] RectTransform _guideBox;
        [SerializeField] TMP_Text _guide;
        [SerializeField] Button _catcher;

        float _local;
        float _duration;
        Action _closed;

        public bool IsOpen => gameObject.activeSelf;
        public string Countdown => _countdown.text;
        public string Guide => _guide.text;
        public bool CountdownShown => _countdown.gameObject.activeSelf;

        void Awake()
        {
            _catcher.onClick.AddListener(Close);
        }

        /// <param name="countdown">"일식까지 N주", or null when the scene has none.</param>
        /// <param name="guide">The scene's guide line, or null.</param>
        public void Play(string countdown, string guide, float durationMs, Action closed)
        {
            gameObject.SetActive(true);
            _countdown.text = countdown ?? string.Empty;
            _countdownGroup.gameObject.SetActive(countdown != null);
            _guide.text = guide ?? string.Empty;
            _guideGroup.gameObject.SetActive(guide != null);
            if (guide != null)
            {
                _guideBox.sizeDelta = new Vector2(_guide.GetPreferredValues(guide).x + 72f, _guideBox.sizeDelta.y);
            }

            _duration = durationMs;
            _local = 0f;
            _closed = closed;
            Draw();
        }

        void Update()
        {
            if (StoryKeys.Skip() || StoryKeys.Next())
            {
                Close();
                return;
            }

            Advance(Time.unscaledDeltaTime * 1000f);
        }

        /// <summary>Moves it on (tests step it).</summary>
        public void Advance(float ms)
        {
            _local += ms;
            if (_local >= _duration)
            {
                Close();
                return;
            }

            Draw();
        }

        public void Close()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(false);
            Action closed = _closed;
            _closed = null;
            closed?.Invoke();
        }

        void Draw()
        {
            (float cover, float countdown, float guide) = IntroTimeline.BannerAt(_local, _duration);
            _cover.color = new Color(0f, 0f, 0f, cover);
            _countdownGroup.alpha = countdown;
            _guideGroup.alpha = guide;
        }
    }
}
