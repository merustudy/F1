using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A drop of loot lying on the stage's floor where an enemy fell (2026-10-08 round 47, C; Docs/Architecture/12_UI.md "전투 화면"의
    /// "이긴 뒤"): a soft light on the floor, the item's icon on its side (a bag as Diablo's well, round 49), and Diablo's ground label
    /// over it (a black box, the title in the item's rarity colour or a bag's gold); picked, the label's line turns to gold and a word
    /// over it asks for the cell. The whole drop is the button (silent: the screen sounds the click);
    /// a right click opens the item's card.
    /// </summary>
    public sealed class LootDropView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] RightClick _rightClick;
        [SerializeField] Image _glow;
        [SerializeField] Image _icon;
        [SerializeField] Image _plateLine;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _pickedWord;
        [SerializeField] GameObject _bagWell;
        [SerializeField] SquareGrid _bagSquares;

        Vector2 _iconSize;

        const float GlowAlpha = 0.28f;
        const float GlowPickedAlpha = 0.5f;

        public Button Button => _button;
        public RightClick RightClick => _rightClick;

        /// <summary>The slot of the loot the drop shows; the screen sets it when it makes the drop.</summary>
        public int Slot { get; set; }

        /// <summary>The item lying here, or null for a bag.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The bag lying here (an elite's, Slice B stage 19), or null for an item.</summary>
        public BagData Bag { get; private set; }

        public bool IsPicked { get; private set; }

        /// <summary>The words on the plate.</summary>
        public string Title => _title.text;

        /// <summary>The icon's box: what a card is placed against.</summary>
        public RectTransform IconRect => _icon.rectTransform;

        public void Show(EquippedItem item, Sprite icon, bool picked)
        {
            gameObject.SetActive(true);
            Item = item;
            Bag = null;
            IsPicked = picked;
            RestoreIcon();
            _icon.sprite = icon;
            _icon.color = Color.white;
            _icon.enabled = icon != null;
            _title.text = UiText.ItemTitle(item);
            _title.color = UiPalette.Rarity(item.Tier);
            ShowPicked(picked);
        }

        /// <summary>
        /// A bag lying here (Slice B stage 19): Diablo's well in the icon's place (round 49: its stone rim tinted by its leather round its
        /// black squares, at a board's size or smaller) and its name on the plate in gold.
        /// </summary>
        public void ShowBag(BagData bag, bool picked)
        {
            gameObject.SetActive(true);
            Item = null;
            Bag = bag;
            IsPicked = picked;
            RestoreIcon();
            Vector2 well = new Vector2(GridGeometry.Span(bag.Width), GridGeometry.Span(bag.Height)) + 2f * GridGeometry.BagRim * Vector2.one;
            float fit = Mathf.Min(1f, _iconSize.x / well.x, _iconSize.y / well.y);
            _icon.rectTransform.sizeDelta = well * fit;
            _icon.sprite = null;
            _icon.preserveAspect = false;
            _icon.color = UiPalette.BagRim(bag.Id);
            _icon.enabled = true;
            _bagWell.SetActive(true);
            _bagSquares.Shape(bag.Width, bag.Height, GridGeometry.Gap);
            _title.text = UiText.Name(bag.Name);
            _title.color = UiPalette.BagName;
            ShowPicked(picked);
        }

        /// <summary>The icon's box as built (a bag changes it), with no bag in it.</summary>
        void RestoreIcon()
        {
            if (_iconSize == Vector2.zero)
            {
                _iconSize = _icon.rectTransform.sizeDelta;
            }

            _icon.rectTransform.sizeDelta = _iconSize;
            _icon.preserveAspect = true;
            _bagWell.SetActive(false);
        }

        void ShowPicked(bool picked)
        {
            _plateLine.color = Tinted(picked ? UiPalette.Virtue : UiPalette.LabelLine, _plateLine.color.a);
            _glow.color = Tinted(_glow.color, picked ? GlowPickedAlpha : GlowAlpha);
            _pickedWord.gameObject.SetActive(picked);
        }

        public void Hide()
        {
            Item = null;
            Bag = null;
            IsPicked = false;
            gameObject.SetActive(false);
        }

        static Color Tinted(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
