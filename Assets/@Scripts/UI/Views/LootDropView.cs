using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A drop of loot lying on the stage's floor where an enemy fell (2026-10-08 round 47, C; Docs/Architecture/12_UI.md "전투 화면"의
    /// "이긴 뒤"): a soft light on the floor, the item's icon on its side, and a plate over it with the item's title; picked, the plate's
    /// line turns to gold and a word over it asks for the cell. The whole drop is the button (silent: the screen sounds the click);
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

        const float GlowAlpha = 0.28f;
        const float GlowPickedAlpha = 0.5f;

        public Button Button => _button;
        public RightClick RightClick => _rightClick;

        /// <summary>The slot of the loot the drop shows; the screen sets it when it makes the drop.</summary>
        public int Slot { get; set; }

        /// <summary>The item lying here.</summary>
        public EquippedItem Item { get; private set; }

        public bool IsPicked { get; private set; }

        /// <summary>The words on the plate.</summary>
        public string Title => _title.text;

        /// <summary>The icon's box: what a card is placed against.</summary>
        public RectTransform IconRect => _icon.rectTransform;

        public void Show(EquippedItem item, Sprite icon, bool picked)
        {
            gameObject.SetActive(true);
            Item = item;
            IsPicked = picked;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _title.text = UiText.ItemTitle(item);
            _plateLine.color = Tinted(picked ? UiPalette.Virtue : UiPalette.Brass, _plateLine.color.a);
            _glow.color = Tinted(_glow.color, picked ? GlowPickedAlpha : GlowAlpha);
            _pickedWord.gameObject.SetActive(picked);
        }

        public void Hide()
        {
            Item = null;
            IsPicked = false;
            gameObject.SetActive(false);
        }

        static Color Tinted(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
