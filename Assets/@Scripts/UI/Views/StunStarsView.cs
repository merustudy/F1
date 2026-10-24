using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// Round 104 (2026-10-24, the user's "경직 스턴 이펙트(머리위 노란별 여러개가 빙글빙글 돌아가는)", 판정 1 A): the stars over a staggered unit's head.
    /// Five yellow stars turn on a flat ellipse, a turn every <see cref="Period"/> seconds; a star at the back is smaller and fainter than one at the
    /// front, which is drawn over it. Each star is the item star's art twice (a dark rim under a yellow fill, as <see cref="GridStarView"/> draws it).
    /// The unit's view says how long they turn and how faint they are (they fade at the end); they take no clicks.
    /// </summary>
    public sealed class StunStarsView : MonoBehaviour
    {
        public const float Period = 0.7f;
        public const float RadiusX = 46f;
        public const float RadiusY = 12f;

        /// <summary>The size of a star at the back and at the front, against its built size.</summary>
        const float BackScale = 0.75f;
        const float FrontScale = 1.1f;

        [SerializeField] RectTransform[] _stars;
        [SerializeField] Image[] _edges;
        [SerializeField] Image[] _fills;

        float _age;
        float _alpha;

        public bool Shown => gameObject.activeSelf;

        public int Count => _stars.Length;

        /// <summary>Turns the stars, this opaque (0 to 1).</summary>
        public void Show(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            if (!gameObject.activeSelf)
            {
                _age = 0f;
                gameObject.SetActive(true);
                Turn();
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        void Update()
        {
            _age += Time.deltaTime;
            Turn();
        }

        void Turn()
        {
            int count = _stars.Length;
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * (_age / Period + (float)i / count);
                // 0 at the back of the ellipse (up on the screen), 1 at the front.
                float depth = 0.5f - 0.5f * Mathf.Sin(angle);
                _stars[i].anchoredPosition = new Vector2(RadiusX * Mathf.Cos(angle), RadiusY * Mathf.Sin(angle));
                float scale = Mathf.Lerp(BackScale, FrontScale, depth);
                _stars[i].localScale = new Vector3(scale, scale, 1f);
                _stars[i].localRotation = Quaternion.Euler(0f, 0f, depth * 34f);
                float alpha = _alpha * Mathf.Lerp(0.55f, 1f, depth);
                SetAlpha(_edges[i], alpha);
                SetAlpha(_fills[i], alpha);
            }

            // The front stars over the back ones: the higher on the screen (the further back), the earlier drawn.
            if (_order == null || _order.Length != count)
            {
                _order = new int[count];
            }

            for (int i = 0; i < count; i++)
            {
                int k = i;
                while (k > 0 && _stars[_order[k - 1]].anchoredPosition.y < _stars[i].anchoredPosition.y)
                {
                    _order[k] = _order[k - 1];
                    k--;
                }

                _order[k] = i;
            }

            for (int k = 0; k < count; k++)
            {
                if (_stars[_order[k]].GetSiblingIndex() != k)
                {
                    _stars[_order[k]].SetSiblingIndex(k);
                }
            }
        }

        int[] _order;

        static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                image.color = new Color(c.r, c.g, c.b, alpha);
            }
        }
    }
}
