using F1.Data;
using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>
        /// The party side of the node map and the reward screen: the battle screen's party columns
        /// in the battle screen's places (row 1 next to the middle), each with the figure, the info
        /// card and the board stacked under it; the potions in the strip under the header; the
        /// inventory as a popup over the right half. Built after the right half so the popup draws
        /// over it.
        /// </summary>
        const float PartyTop = 186f;
        const float PartyLabelHeight = 28f;
        const float PartyCardMargin = 12f;
        const float PartyCardCellsTop = 162f;
        const float PartyLeft = 20f;
        const float PartyWidth = 930f;
        const float PartyDetailWidth = 760f;
        const float PartyToInventoryWidth = 150f;

        /// <summary>The popup over the right half: it must not cover the buttons under the panels there.</summary>
        const float InventoryPopupTop = 100f;
        const float InventoryPopupHeight = 820f;

        static float PartyFigureTop => PartyTop + PartyLabelHeight - 2f;
        static float PartyCardTop => PartyFigureTop + BattleFigureHeight + 14f;
        static float PartyCardHeight => PartyCardCellsTop + JobData.MaxItemSlots * PartyColumnView.CellHeight + (JobData.MaxItemSlots - 1) * PartyColumnView.CellGap + PartyCardMargin;
        static float PartyBottom => PartyCardTop + PartyCardHeight;

        /// <summary>The width of a column as the battle screen lays its columns out for a full party.</summary>
        static float PartyColumnWidth => (BattleFieldWidth - BattleScreen.SideGap - BattleScreen.ColumnGap * (BattleRows.Count * 2 - 2)) / (BattleRows.Count * 2);

        static float PartyColumnX(int row)
        {
            return PartyLeft + (PartyColumnWidth + BattleScreen.ColumnGap) * (BattleRows.Count - row);
        }

        static PartySideView BuildPartySide(RectTransform frame)
        {
            // Potions where the battle screen has them.
            Image potionsPanel = UiBuild.Panel("PotionsPanel", frame, UiPalette.Panel);
            UiBuild.Box(potionsPanel, 30f, 90f, 610f, 76f);
            RectTransform potions = UiBuild.Box(UiBuild.Rect("Potions", frame), 40f, 96f, 590f, 64f);
            UiBuild.Horizontal(potions, 10f);
            PotionSlotView potionTemplate = BuildPotionSlot(potions, 190f);

            var columns = new PartyColumnView[BattleRows.Count];
            var roots = new GameObject[BattleRows.Count];
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                columns[row - 1] = BuildPartyColumn(frame, row);
                roots[row - 1] = columns[row - 1].gameObject;
            }

            Image floor = UiBuild.Image("PartyFloor", frame, UiPalette.Line);
            UiBuild.Box(floor, PartyLeft, PartyFigureTop + BattleFigureHeight + 2f, PartyWidth, 2f);

            // Under the columns: the selected item's facts (or how to use the boards), and the button that takes it off its board.
            TextMeshProUGUI detail = UiBuild.Label("PartyDetail", frame, 19f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(detail, PartyLeft, PartyBottom + 8f, PartyDetailWidth, 54f);
            ButtonParts toInventory = UiBuild.LocalizedButton("ToInventory", frame, UiKeys.Board.ToInventory, UiPalette.ButtonQuiet, 22f);
            UiBuild.Box(toInventory.Rect, PartyLeft + PartyWidth - PartyToInventoryWidth, PartyBottom + 8f, PartyToInventoryWidth, 50f);

            GameObject popup = BuildInventoryPopup(frame, out TMP_Text inventoryTitle, out GameObject inventoryEmpty, out InventoryEntryView entryTemplate, out RectTransform entryParent);

            var view = frame.gameObject.AddComponent<PartySideView>();
            UiBuild.SetReferences(view, "_columns", columns);
            UiBuild.SetReferences(view, "_columnRoots", roots);
            UiBuild.SetReference(view, "_potionTemplate", potionTemplate);
            UiBuild.SetReference(view, "_potionParent", potions);
            UiBuild.SetReference(view, "_detail", detail);
            UiBuild.SetReference(view, "_toInventory", toInventory.Button);
            UiBuild.SetReference(view, "_inventoryPanel", popup);
            UiBuild.SetReference(view, "_inventoryTitle", inventoryTitle);
            UiBuild.SetReference(view, "_inventoryEmpty", inventoryEmpty);
            UiBuild.SetReference(view, "_entryTemplate", entryTemplate);
            UiBuild.SetReference(view, "_entryParent", entryParent);
            return view;
        }

        /// <summary>
        /// One row's column: the row's name, the figure placeholder, then the info card with the
        /// board stacked under it. Every object is named after the row, because a prefab must not
        /// repeat a name.
        /// </summary>
        static PartyColumnView BuildPartyColumn(Transform frame, int row)
        {
            string p = "Party" + row;
            float x = PartyColumnX(row);
            float w = PartyColumnWidth;
            RectTransform column = UiBuild.Box(UiBuild.Rect(p + "Column", frame), x, PartyTop, w, PartyBottom - PartyTop);
            UiBuild.Box(UiBuild.LocalizedLabel(p + "Label", column, UiText.RowKey(row), 20f, UiPalette.TextDim, TextAlignmentOptions.Center), 0f, 0f, w, PartyLabelHeight);

            Image figure = BuildFigure(column, p + "Figure", BattleUnitView.FigureTint(UiPalette.Party), raycastTarget: false);
            UiBuild.Box(figure, 0f, PartyFigureTop - PartyTop, w, BattleFigureHeight);

            Image card = UiBuild.Image(p + "Card", column, UiPalette.Party);
            UiBuild.Box(card, 0f, PartyCardTop - PartyTop, w, PartyCardHeight);
            const float m = PartyCardMargin;
            float inner = w - 2f * m;
            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label(p + "Name", card.transform, 28f, UiPalette.Text), m, 10f, inner, 38f));
            TextMeshProUGUI job = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label(p + "Job", card.transform, 20f, UiPalette.TextDim), m, 44f, inner, 24f));
            UiBar hpBar = UiBuild.Bar(p + "HpBar", card.transform, UiPalette.Good);
            UiBuild.Box(hpBar, m, 76f, inner, 24f);
            TextMeshProUGUI hp = UiBuild.Box(UiBuild.Label(p + "Hp", card.transform, 18f, UiPalette.Text, TextAlignmentOptions.Center), m, 76f, inner, 24f);

            // Forward goes towards row 1 (to the right), back the other way.
            float buttonWidth = (inner - 8f) / 2f;
            ButtonParts forward = UiBuild.LocalizedButton(p + "Forward", card.transform, UiKeys.Board.Forward, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(forward.Rect, m, 110f, buttonWidth, 40f);
            ButtonParts back = UiBuild.LocalizedButton(p + "Back", card.transform, UiKeys.Board.Back, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(back.Rect, m + buttonWidth + 8f, 110f, buttonWidth, 40f);

            // The board: cells stacked top to bottom, as many as a board can have at most. The view sizes them at runtime.
            RectTransform cells = UiBuild.Box(UiBuild.Rect(p + "Cells", card.transform), m, PartyCardCellsTop, inner, PartyCardHeight - PartyCardCellsTop - m);
            VerticalLayoutGroup stack = UiBuild.Vertical(cells, PartyColumnView.CellGap);
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            ItemSlotView slotTemplate = BuildItemSlot(cells, PartyColumnView.CellHeight, p + "CellTemplate", inner);

            var view = column.gameObject.AddComponent<PartyColumnView>();
            UiBuild.SetReference(view, "_figure", figure.gameObject);
            UiBuild.SetReference(view, "_card", card.gameObject);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_hp", hp);
            UiBuild.SetReference(view, "_hpBar", hpBar);
            UiBuild.SetReference(view, "_forward", forward.Button);
            UiBuild.SetReference(view, "_back", back.Button);
            UiBuild.SetReference(view, "_slotTemplate", slotTemplate);
            UiBuild.SetReference(view, "_slotParent", cells);
            return view;
        }

        /// <summary>One cell of a board: the name on the left, the grade on the right, in one line. Its height is set at runtime.</summary>
        static ItemSlotView BuildItemSlot(Transform parent, float height, string name, float width)
        {
            Image frame = UiBuild.Image(name, parent, UiPalette.Slot);
            UiBuild.Size(frame, width, height);
            Button button = UiBuild.MakeButton(frame);

            TextMeshProUGUI itemName = UiBuild.SingleLine(UiBuild.Label(name + "Name", frame.transform, 19f, UiPalette.Text));
            UiBuild.Stretch(itemName.rectTransform, 8f, 0f, 76f, 0f);
            TextMeshProUGUI grade = UiBuild.SingleLine(UiBuild.Label(name + "Grade", frame.transform, 16f, UiPalette.TextDim, TextAlignmentOptions.Right));
            UiBuild.Stretch(grade.rectTransform, 8f, 0f, 8f, 0f);

            var view = frame.gameObject.AddComponent<ItemSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_name", itemName);
            UiBuild.SetReference(view, "_grade", grade);

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// The inventory popup: a dimmed cover over the right half with a box that lists the items
        /// top to bottom. The screen's toggle button opens and closes it; the view fills it.
        /// </summary>
        static GameObject BuildInventoryPopup(Transform frame, out TMP_Text title, out GameObject empty, out InventoryEntryView entryTemplate, out RectTransform entryParent)
        {
            Image cover = UiBuild.Image("InventoryPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Box(cover, 970f, InventoryPopupTop, 940f, InventoryPopupHeight);
            Image box = UiBuild.Panel("InventoryBox", cover.transform, UiPalette.Panel);
            UiBuild.Box(box, 10f, 10f, 920f, InventoryPopupHeight - 20f);
            title = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("InventoryTitle", box.transform, 32f, UiPalette.Text), 24f, 20f, 600f, 44f));
            UiBuild.Box(UiBuild.LocalizedLabel("InventoryHint", box.transform, UiKeys.Board.InventoryHint, 20f, UiPalette.TextDim), 24f, 70f, 872f, 30f);
            ScrollRect scroll = UiBuild.VerticalScroll("InventoryList", box.transform, 16f, out entryParent);
            UiBuild.Box((RectTransform)scroll.transform, 24f, 110f, 872f, InventoryPopupHeight - 20f - 130f);
            entryTemplate = BuildInventoryEntry(entryParent);
            empty = UiBuild.Box(UiBuild.LocalizedLabel("InventoryEmpty", box.transform, UiKeys.Board.InventoryEmpty, 22f, UiPalette.TextDim), 40f, 122f, 400f, 32f).gameObject;

            cover.gameObject.SetActive(false);
            return cover.gameObject;
        }

        /// <summary>One line of the inventory popup: the item's title over a summary of its facts.</summary>
        static InventoryEntryView BuildInventoryEntry(Transform parent)
        {
            Image frame = UiBuild.Image("InventoryEntryTemplate", parent, UiPalette.Slot);
            UiBuild.Size(frame, 800f, 76f);
            var element = frame.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 76f;
            element.preferredHeight = 76f;
            Button button = UiBuild.MakeButton(frame);

            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Label("InventoryEntryTitle", frame.transform, 24f, UiPalette.Text));
            UiBuild.Stretch(title.rectTransform, 16f, 6f, 16f, 36f);
            TextMeshProUGUI facts = UiBuild.SingleLine(UiBuild.Label("InventoryEntryFacts", frame.transform, 18f, UiPalette.TextDim));
            UiBuild.Stretch(facts.rectTransform, 16f, 40f, 16f, 8f);

            var view = frame.gameObject.AddComponent<InventoryEntryView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_title", title);
            UiBuild.SetReference(view, "_facts", facts);

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>One potion slot: the name over the effect.</summary>
        static PotionSlotView BuildPotionSlot(Transform parent, float width)
        {
            Image frame = UiBuild.Image("PotionTemplate", parent, UiPalette.Slot);
            UiBuild.Size(frame, width, 64f);
            Button button = UiBuild.MakeButton(frame);

            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Label("PotionName", frame.transform, 22f, UiPalette.Text));
            UiBuild.Stretch(name.rectTransform, 10f, 4f, 10f, 32f);
            TextMeshProUGUI effect = UiBuild.SingleLine(UiBuild.Label("PotionEffect", frame.transform, 18f, UiPalette.TextDim));
            UiBuild.Stretch(effect.rectTransform, 10f, 34f, 10f, 4f);

            var view = frame.gameObject.AddComponent<PotionSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_effect", effect);

            frame.gameObject.SetActive(false);
            return view;
        }
    }
}
