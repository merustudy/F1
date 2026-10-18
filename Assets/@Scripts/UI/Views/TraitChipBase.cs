using UnityEngine;
using UnityEngine.EventSystems;

namespace F1.UI
{
    /// <summary>
    /// A place on a trait line (Slice D stage 37, Round 84): a trait with its name (<see cref="TraitChipView"/>), a trait's icon alone
    /// (<see cref="TraitIconView"/>, the party popup's stage cards) or the "+N" pill (<see cref="TraitMoreView"/>). The line lays them out by their
    /// widths; a right click on one opens the window's trait card above it (2026-10-19 사용자 "특성은 마우스 우클릭시 툴팁으로 설명 나오게": the
    /// item cards' rule — the next press closes it).
    /// </summary>
    public abstract class TraitChipBase : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] TraitTooltipView _tooltip;

        public TraitChip Chip { get; private set; }

        /// <summary>The words the chip shows (the trait's name, the pill's "+N"); null for an icon alone.</summary>
        public abstract string Name { get; }

        /// <summary>The icon's letter; null for the pill.</summary>
        public abstract string Glyph { get; }

        /// <summary>The colour the icon's rim (or the pill) is drawn in.</summary>
        public abstract Color RimColor { get; }

        /// <summary>How wide the chip is with that trait.</summary>
        public abstract float WidthOf(TraitChip chip);

        /// <summary>Writes the trait into the chip at its width.</summary>
        protected abstract void Fill(TraitChip chip, float width);

        /// <summary>Shows a trait at a distance from the line's left; returns the chip's width.</summary>
        public float Show(TraitChip chip, float x)
        {
            Chip = chip;
            float width = WidthOf(chip);
            Fill(chip, width);
            var rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            gameObject.SetActive(true);
            return width;
        }

        public void Hide()
        {
            _tooltip.Hide(this);
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RightClickNow();
            }
        }

        /// <summary>Opens the window's card for this chip, as a right click does (tests call it after a simulated press).</summary>
        public void RightClickNow()
        {
            _tooltip.Show(this, Chip);
        }
    }
}
