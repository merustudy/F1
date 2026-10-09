using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One unit's board of the board panel under the stage: its head, with the row the unit stands
    /// in on a gold stud and its name (a monster's in a pale red, 2026-10-05 round 29), and under it
    /// its board as a grid (Slice B stage 19): a mercenary's bags in leather, the empty squares of the bags dark, and its items as pieces
    /// where they lie, each showing its cooldown (<see cref="BattleItemView"/>); an enemy has no bags, so its board is only its items'
    /// pieces one under another. The board sits in the panel's column of the row the unit stands in, right under its figure and marks
    /// (2026-10-03 mockup V), so it moves when the unit advances and leaves the panel with the dead, as the figure leaves the stage. The
    /// row on the head is the row of the column the screen has put the board in, which can be behind the engine for a moment: while a
    /// fallen mercenary's grave stands, the party keeps its places (2026-10-04, round 21). The whole board is the button a potion is aimed at.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        const float BagPad = GridGeometry.BagRim;

        [SerializeField] Button _button;
        [SerializeField] TMP_Text _row;
        [SerializeField] TMP_Text _name;
        [SerializeField] RectTransform _cells;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] Image _bagTemplate;
        [SerializeField] Image _squareTemplate;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        ExpeditionArt _art;
        int _standRow;
        int _shownRow = -1;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The pieces of the items in the unit's item order (the battle's order: reading order).</summary>
        public IReadOnlyList<BattleItemView> Items => _items;

        /// <summary>How many bags the board shows (none for an enemy).</summary>
        public int BagsShown { get; private set; }

        /// <summary>How many empty squares the board shows.</summary>
        public int EmptySquaresShown { get; private set; }

        /// <param name="art">Where the icons of the items come from.</param>
        public void Bind(BattleUnit unit, ExpeditionArt art)
        {
            _unit = unit;
            _art = art;
            _standRow = unit.Row;
            _name.text = UiText.Name(unit.Setup.Name);
            _name.color = unit.Side == BattleSide.Enemy ? UiPalette.EnemyName : UiPalette.Text;

            // Where the items lie: the setup's layout, or one under another (a setup made without one, as an enemy's board is).
            BoardLayout layout = unit.Setup.Layout ?? BoardLayout.Stacked(unit.Setup.Items ?? new List<EquippedItem>());
            var covered = new HashSet<int>();
            for (int i = 0; i < unit.Items.Count; i++)
            {
                ItemData data = unit.Items[i].Equipped.Item;
                Placement at = layout.Items[i];
                for (int dy = 0; dy < at.HeightOf(data.Width, data.Height); dy++)
                {
                    for (int dx = 0; dx < at.WidthOf(data.Width, data.Height); dx++)
                    {
                        covered.Add((at.Y + dy) * BoardFrame.Width + at.X + dx);
                    }
                }
            }

            // The bags as Diablo's wells (round 49) with all their squares black, under the pieces (an item lies on its black squares: it
            // has no ground in battle). An enemy has no bags: each of its items lies in a well of its own, the stone a little red.
            BagsShown = 0;
            EmptySquaresShown = 0;
            foreach (BoardBag bag in layout.Bags)
            {
                Well(bag.At.X, bag.At.Y, bag.Width, bag.Height, UiPalette.BagRim(bag.Bag.Id));
                BagsShown++;
                for (int dy = 0; dy < bag.Height; dy++)
                {
                    for (int dx = 0; dx < bag.Width; dx++)
                    {
                        int x = bag.At.X + dx;
                        int y = bag.At.Y + dy;
                        Square(x, y);
                        if (!covered.Contains(y * BoardFrame.Width + x))
                        {
                            EmptySquaresShown++;
                        }
                    }
                }
            }

            if (layout.Bags.Count == 0)
            {
                for (int i = 0; i < unit.Items.Count; i++)
                {
                    ItemData data = unit.Items[i].Equipped.Item;
                    Placement at = layout.Items[i];
                    int width = at.WidthOf(data.Width, data.Height);
                    int height = at.HeightOf(data.Width, data.Height);
                    Well(at.X, at.Y, width, height, UiPalette.EnemyRim);
                    for (int dy = 0; dy < height; dy++)
                    {
                        for (int dx = 0; dx < width; dx++)
                        {
                            Square(at.X + dx, at.Y + dy);
                        }
                    }
                }
            }

            for (int i = 0; i < unit.Items.Count; i++)
            {
                BattleItemState item = unit.Items[i];
                ItemData data = item.Equipped.Item;
                Placement at = layout.Items[i];
                BattleItemView view = Instantiate(_itemTemplate, _cells);
                view.gameObject.SetActive(true);
                GridGeometry.Lay(view.Rect, at.X, at.Y, at.WidthOf(data.Width, data.Height), at.HeightOf(data.Width, data.Height));
                BindItem(view, item, at);
                BattleItemView clicked = view;
                BattleItemState state = item;
                view.RightClick.Clicked += () => ItemRightClicked?.Invoke(clicked, state);
                _items.Add(view);
            }
        }

        /// <summary>A right click on an item's piece (round 42, right-click since round 47): the screen opens the item's card.</summary>
        public event Action<BattleItemView, BattleItemState> ItemRightClicked;

        /// <summary>Round 42: whether the pieces take clicks for their cards. Off while a potion waits for this board to be clicked.</summary>
        public void SetItemsClickable(bool clickable)
        {
            foreach (BattleItemView item in _items)
            {
                item.SetClickable(clickable);
            }
        }

        /// <summary>The piece of the item in this slot of the board, or null when the unit has no such item.</summary>
        public BattleItemView ItemOfSlot(int slotIndex)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_unit.Items[i].SlotIndex == slotIndex)
                {
                    return _items[i];
                }
            }

            return null;
        }

        /// <summary>The screen has put the board in the column of this row: its head shows the row.</summary>
        public void StandIn(int row)
        {
            _standRow = row;
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
            _name.text = UiText.Name(_unit.Setup.Name);
            for (int i = 0; i < _items.Count; i++)
            {
                BindItem(_items[i], _unit.Items[i], _items[i].At);
            }
        }

        void BindItem(BattleItemView view, BattleItemState item, Placement at)
        {
            view.Bind(item, at, _art.OfItem(item.Equipped.Item.Id), mirrored: _unit.Side == BattleSide.Enemy);
        }

        /// <param name="targetable">True while a potion is waiting for this unit to be clicked.</param>
        public void Render(BattleEngine engine, bool targetable)
        {
            if (_standRow != _shownRow)
            {
                _shownRow = _standRow;
                _row.text = _standRow.ToString(CultureInfo.InvariantCulture);
            }

            _button.interactable = targetable && _unit.Alive;
            foreach (BattleItemView item in _items)
            {
                item.Render(engine.TimeMs, _unit.Alive);
            }
        }

        /// <summary>A well's rim and grey under squares of the board (round 49), behind everything drawn before it.</summary>
        void Well(int x, int y, int width, int height, Color rim)
        {
            Image well = Instantiate(_bagTemplate, _cells);
            well.gameObject.SetActive(true);
            well.transform.SetAsFirstSibling();
            GridGeometry.Lay(well.rectTransform, x, y, width, height);
            well.rectTransform.anchoredPosition += new Vector2(-BagPad, BagPad);
            well.rectTransform.sizeDelta += new Vector2(2f * BagPad, 2f * BagPad);
            well.color = rim;
        }

        /// <summary>A black square of the board, under the pieces.</summary>
        void Square(int x, int y)
        {
            Image square = Instantiate(_squareTemplate, _cells);
            square.gameObject.SetActive(true);
            GridGeometry.Lay(square.rectTransform, x, y, 1, 1);
        }
    }
}
