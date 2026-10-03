using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The place of a unit's full-body art: the art when the unit has one, the placeholder
    /// silhouette when it has none. Every figure is drawn on the same canvas with the same floor
    /// line, so the art stands at the bottom centre of the place at one size, whatever the width
    /// of the place is.
    /// </summary>
    public sealed class FigureView : MonoBehaviour
    {
        [SerializeField] Image _art;
        [SerializeField] GameObject _placeholder;

        /// <summary>The art on show, or null while the placeholder stands in.</summary>
        public Sprite Art => _art.enabled ? _art.sprite : null;

        public bool ShowsPlaceholder => _placeholder.activeSelf;

        /// <param name="art">Null for a unit without art.</param>
        public void Show(Sprite art)
        {
            _art.sprite = art;
            _art.enabled = art != null;
            _placeholder.SetActive(art == null);
        }
    }
}
