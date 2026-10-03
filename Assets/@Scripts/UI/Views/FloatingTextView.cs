using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// A number or a word that rises from a unit and fades: the moment a hit lands, a heal
    /// restores, a shield goes up, a mercenary reaches death's door. The battle's fx layer plays
    /// it from the event log and takes it back when it is done. It is two texts: a dark one a
    /// little behind, so that the colored one in front reads over any background.
    /// </summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        const float Rise = 84f;
        const float PopDuration = 0.14f;
        const float PopScale = 1.35f;
        const float FadeFrom = 0.55f;

        [SerializeField] TMP_Text _text;
        [SerializeField] TMP_Text _shadow;

        RectTransform _rect;
        Vector2 _start;
        float _age;
        float _life;
        float _scale;
        Color _color;

        /// <summary>True while it is on its way up.</summary>
        public bool Playing => gameObject.activeSelf;

        public string Text => _text.text;

        /// <param name="position">Where it starts, in the fx layer's coordinates.</param>
        /// <param name="scale">1 for an ordinary number, more for a heavy one.</param>
        /// <param name="life">Seconds until it is gone.</param>
        public void Play(Vector2 position, string text, Color color, float scale, float life)
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }

            _start = position;
            _age = 0f;
            _life = life;
            _scale = scale;
            _color = color;
            _text.text = text;
            _shadow.text = text;
            gameObject.SetActive(true);
            Step(0f);
        }

        void Update()
        {
            Step(Time.deltaTime);
        }

        void Step(float deltaSeconds)
        {
            _age += deltaSeconds;
            if (_age >= _life)
            {
                gameObject.SetActive(false);
                return;
            }

            // It shoots up and slows, pops in a little large, and fades over the last part of its life.
            float t = _age / _life;
            float rise = 1f - (1f - t) * (1f - t);
            _rect.anchoredPosition = _start + new Vector2(0f, Rise * rise);
            float pop = _age < PopDuration ? Mathf.Lerp(PopScale, 1f, _age / PopDuration) : 1f;
            _rect.localScale = Vector3.one * (_scale * pop);
            float alpha = t < FadeFrom ? 1f : 1f - (t - FadeFrom) / (1f - FadeFrom);
            _text.color = new Color(_color.r, _color.g, _color.b, alpha);
            _shadow.color = new Color(UiPalette.Ink.r, UiPalette.Ink.g, UiPalette.Ink.b, alpha);
        }
    }
}
