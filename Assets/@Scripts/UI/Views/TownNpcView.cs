using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A speaker standing in the town (Slice D stage 55, Round 101 안 B): the figure on the road by the gate with a plate under it; the lobby shows
    /// its bubble under the pointer, and a click plays the last dialogue heard again. Tests call <see cref="Hover"/> and <see cref="Leave"/>.
    /// </summary>
    public sealed class TownNpcView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>Where the figure's foot stands and how tall the figure's canvas is drawn (Round 101 mock: left of the gate's arch, on the road).</summary>
        public const float FootX = 1585f;
        public const float FootY = 700f;
        public const float Height = 280f;

        /// <summary>The figure's canvas leaves 16/896 under the feet (the art pipeline's floor line).</summary>
        const float FloorShare = 16f / 896f;

        [SerializeField] Button _button;
        [SerializeField] Image _figure;
        [SerializeField] TMP_Text _name;

        public Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;
        public string Name => _name.text;
        public Sprite Figure => _figure.sprite;

        public event Action<TownNpcView> Entered;
        public event Action<TownNpcView> Exited;

        /// <summary>Stands the figure at its place with its name; null hides it.</summary>
        public void Show(Sprite figure, string name)
        {
            gameObject.SetActive(figure != null);
            if (figure == null)
            {
                return;
            }

            _figure.sprite = figure;
            _name.text = name;
            float width = Height * figure.rect.width / figure.rect.height;
            Rect.anchoredPosition = new Vector2(FootX - width / 2f, -(FootY - Height + Height * FloorShare));
            Rect.sizeDelta = new Vector2(width, Height);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Leave();
        }

        public void Hover()
        {
            Entered?.Invoke(this);
        }

        public void Leave()
        {
            Exited?.Invoke(this);
        }
    }
}
