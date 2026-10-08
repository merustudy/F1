using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The body of a tile of the shop or the loot (2026-10-08 round 46, S1 and L2; Docs/Architecture/12_UI.md "상점", "전리품"): the
    /// item as a board cell at its own size in the room of two cells (a longer item drawn smaller), its name, its kind and cells, a
    /// brass rule and its facts under it; the tile's own view adds the foot (the price, or the take). The whole tile is the button;
    /// the cell inside takes no click. The rim is brass while the tile is picked.
    /// </summary>
    public abstract class ItemTileView : MonoBehaviour
    {
        /// <summary>The room the cell has: two cells of a board, stacked.</summary>
        public const float CellRoom = 2f * BattleItemView.CellHeight + BattleItemView.CellGapY;

        [SerializeField] Button _button;
        [SerializeField] RightClick _rightClick;
        [SerializeField] Image _rim;
        [SerializeField] CanvasGroup _group;
        [SerializeField] ItemSlotView _cell;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _sub;
        [SerializeField] Image _rule;
        [SerializeField] TMP_Text _facts;

        public Button Button => _button;

        /// <summary>The right click that opens the item's card (round 47).</summary>
        public RightClick RightClick => _rightClick;

        /// <summary>The cell the item is shown in: its icon and tier marks, as on a board.</summary>
        public ItemSlotView Cell => _cell;

        /// <summary>The facts under the rule, or an empty string while they are hidden.</summary>
        public string Facts => _facts.gameObject.activeSelf ? _facts.text : string.Empty;

        protected string NameText => _name.text;

        /// <summary>The tile's look: the brass rim while picked, how see-through it is, whether it takes a click.</summary>
        protected void Look(bool picked, float alpha, bool interactable)
        {
            _rim.color = picked ? UiPalette.Selected : UiPalette.Line;
            _group.alpha = alpha;
            _button.interactable = interactable;
        }

        /// <summary>The item in the cell, as tall as the cells it takes, and drawn smaller when that is more than the room.</summary>
        protected void ShowCell(EquippedItem item, Sprite icon)
        {
            _cell.gameObject.SetActive(true);
            _cell.Show(item, icon, selected: false, interactable: true, fatigueCost: 0, merges: false);
            float height = BattleItemView.BoardHeight(item.Item.Size);
            _cell.SetHeight(height);
            float scale = Mathf.Min(1f, CellRoom / height);
            _cell.transform.localScale = new Vector3(scale, scale, 1f);
        }

        protected void HideCell()
        {
            _cell.gameObject.SetActive(false);
        }

        /// <summary>The words: the name, the kind and cells, the rule and the facts.</summary>
        protected void ShowWords(string name, string sub, string facts)
        {
            _name.gameObject.SetActive(true);
            _sub.gameObject.SetActive(true);
            _rule.gameObject.SetActive(true);
            _facts.gameObject.SetActive(true);
            _name.text = name;
            _sub.text = sub;
            _facts.text = facts;
        }

        /// <summary>Hides the cell and the words; the name stays when one is given (a drop already taken says what it was).</summary>
        protected void HideBody(string name = null)
        {
            HideCell();
            _name.gameObject.SetActive(name != null);
            _name.text = name ?? string.Empty;
            _sub.gameObject.SetActive(false);
            _rule.gameObject.SetActive(false);
            _facts.gameObject.SetActive(false);
        }
    }
}
