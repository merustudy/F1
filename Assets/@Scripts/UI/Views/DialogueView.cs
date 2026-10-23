using System;
using System.Collections.Generic;
using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A dialogue of the story over the lobby (Slice D stage 55, Round 101 안 A): the town darkened, the speaker's dialogue picture at the left (it
    /// slides in and comes up), the box along the bottom with the speaker's name, the line and "1/3 · 클릭: 다음 · Esc: 건너뛰기". Each line shows
    /// its speaker's face (a short crossing when it changes). A click or space goes to the next line, the last line or Esc closes it. The words,
    /// speakers and faces are data (DialogueLineData, NpcData); the screen gives the pictures (loaded in the lobby's scope).
    /// </summary>
    public sealed class DialogueView : MonoBehaviour
    {
        const float AppearMs = 400f;
        const float FaceMs = 200f;
        const float SlideIn = 40f;

        [SerializeField] Image _shade;
        [SerializeField] Image _portrait;
        [SerializeField] Image _portraitBefore;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _text;
        [SerializeField] TMP_Text _hint;
        [SerializeField] Button _catcher;
        [SerializeField] CanvasGroup _group;

        readonly List<DialogueLineData> _lines = new List<DialogueLineData>();
        Func<string, string, Sprite> _face;
        Func<string, string> _speakerName;
        Action _closed;
        Vector2 _portraitHome;
        int _index;
        float _sinceOpen;
        float _sinceFace;

        public bool IsOpen => gameObject.activeSelf;
        public int Index => _index;
        public string Text => _text.text;
        public string Speaker => _name.text;
        public Sprite Face => _portrait.sprite;

        void Awake()
        {
            _catcher.onClick.AddListener(Next);
            _portraitHome = _portrait.rectTransform.anchoredPosition;
        }

        /// <summary>
        /// Plays a dialogue's lines. <paramref name="face"/> gives a speaker's picture for an expression, <paramref name="speakerName"/> a speaker's
        /// name in the locale; <paramref name="closed"/> is called once when it closes (to its end or skipped).
        /// </summary>
        public void Play(IReadOnlyList<DialogueLineData> lines, Func<string, string, Sprite> face, Func<string, string> speakerName, Action closed)
        {
            _lines.Clear();
            _lines.AddRange(lines);
            _face = face;
            _speakerName = speakerName;
            _closed = closed;
            _index = 0;
            _sinceOpen = 0f;
            _sinceFace = FaceMs;
            gameObject.SetActive(true);
            _portraitBefore.gameObject.SetActive(false);
            ShowLine();
            Animate();
        }

        void Update()
        {
            if (StoryKeys.Skip())
            {
                Close();
                return;
            }

            if (StoryKeys.Next())
            {
                Next();
                return;
            }

            float ms = Time.unscaledDeltaTime * 1000f;
            _sinceOpen += ms;
            _sinceFace += ms;
            Animate();
        }

        /// <summary>The next line, or the end after the last.</summary>
        public void Next()
        {
            if (_index + 1 >= _lines.Count)
            {
                Close();
                return;
            }

            Sprite before = _portrait.sprite;
            _index++;
            ShowLine();
            if (before != _portrait.sprite)
            {
                _portraitBefore.sprite = before;
                _portraitBefore.gameObject.SetActive(before != null);
                _sinceFace = 0f;
            }

            Animate();
        }

        /// <summary>Closes it (Esc, or after the last line).</summary>
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

        public DialogueLineData CurrentLine => _lines[_index];

        void ShowLine()
        {
            DialogueLineData line = _lines[_index];
            _text.text = UiText.Name(line.Text);
            _name.text = _speakerName(line.Speaker);
            Sprite face = _face(line.Speaker, line.Expression);
            _portrait.sprite = face;
            _portrait.color = face == null ? Color.clear : Color.white;
            _hint.text = UiStrings.Get(UiKeys.Story.DialogueHint, _index + 1, _lines.Count);
        }

        void Animate()
        {
            float appear = Mathf.Clamp01(_sinceOpen / AppearMs);
            _group.alpha = appear;
            _portrait.rectTransform.anchoredPosition = _portraitHome + new Vector2(-SlideIn * (1f - appear), 0f);
            _portraitBefore.rectTransform.anchoredPosition = _portrait.rectTransform.anchoredPosition;
            if (_portraitBefore.gameObject.activeSelf)
            {
                float fade = 1f - Mathf.Clamp01(_sinceFace / FaceMs);
                _portraitBefore.color = new Color(1f, 1f, 1f, fade);
                if (fade <= 0f)
                {
                    _portraitBefore.gameObject.SetActive(false);
                }
            }
        }
    }
}
