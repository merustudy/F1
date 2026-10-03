using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// A trailing fill behind a bar's fill that lingers where the value just was: when the value
    /// drops, the ghost stays for a moment and then shrinks after the fill, so a hit reads as a
    /// red tail that is eaten away. When the value rises, the ghost snaps up with it. It is only a
    /// picture; the value is the bar's. The HP bars have one.
    /// </summary>
    public sealed class UiBarGhost : MonoBehaviour
    {
        const float Delay = 0.2f;
        const float Speed = 1.3f;

        [SerializeField] UiBar _bar;
        [SerializeField] RectTransform _ghost;

        float _shown;
        float _last;
        float _wait;

        /// <summary>The ratio the ghost shows.</summary>
        public float Ratio => _shown;

        void Update()
        {
            float ratio = _bar.Ratio;
            if (ratio < _last)
            {
                _wait = Delay;
            }

            _last = ratio;

            if (ratio >= _shown)
            {
                _shown = ratio;
            }
            else if (_wait > 0f)
            {
                _wait -= Time.deltaTime;
            }
            else
            {
                _shown = Mathf.MoveTowards(_shown, ratio, Speed * Time.deltaTime);
            }

            UiBar.Fill(_ghost, _shown);
        }
    }
}
