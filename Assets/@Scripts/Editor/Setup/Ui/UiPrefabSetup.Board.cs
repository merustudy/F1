using F1.Data;
using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>Outer size of the party board panel.</summary>
        const float BoardPanelWidth = 1060f;
        const float BoardPanelHeight = 620f;
        const float BoardPaddingX = 16f;
        const float BoardPaddingY = 12f;
        const float BoardWidth = BoardPanelWidth - BoardPaddingX * 2f;
        const float BoardCardHeight = 98f;
        const float BoardCardGap = 8f;

        /// <summary>The party board shared by the node map and the reward screen: a 1060 x 620 panel.</summary>
        static PartyBoardView BuildBoard(Transform parent, float x, float y)
        {
            Image panel = UiBuild.Panel("PartyBoard", parent, UiPalette.Panel);
            UiBuild.Box(panel, x, y, BoardPanelWidth, BoardPanelHeight);
            RectTransform board = UiBuild.Box(
                UiBuild.Rect("BoardContent", panel.transform),
                BoardPaddingX,
                BoardPaddingY,
                BoardWidth,
                BoardPanelHeight - BoardPaddingY * 2f);

            // One card per member, one under the other: the box holds as many cards as a side has rows.
            RectTransform members = UiBuild.Box(UiBuild.Rect("Members", board), 0f, 0f, BoardWidth, BattleRows.Count * BoardCardHeight + (BattleRows.Count - 1) * BoardCardGap);
            UiBuild.Vertical(members, BoardCardGap);
            BoardMemberView memberTemplate = BuildBoardMember(members);

            UiBuild.Box(UiBuild.LocalizedLabel("PotionsHeader", board, UiKeys.Board.Potions, 24f, UiPalette.TextDim), 0f, 440f, 120f, 40f);
            RectTransform potions = UiBuild.Box(UiBuild.Rect("Potions", board), 130f, 428f, BoardWidth - 130f, 64f);
            UiBuild.Horizontal(potions, 8f);
            PotionSlotView potionTemplate = BuildPotionSlot(potions, 220f);

            TextMeshProUGUI detail = UiBuild.Label("BoardDetail", board, 22f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(detail, 0f, 500f, BoardWidth, 60f);
            UiBuild.Box(UiBuild.LocalizedLabel("BoardHint", board, UiKeys.Board.Hint, 20f, UiPalette.TextDim), 0f, 566f, BoardWidth, 30f);

            var view = panel.gameObject.AddComponent<PartyBoardView>();
            UiBuild.SetReference(view, "_memberTemplate", memberTemplate);
            UiBuild.SetReference(view, "_memberParent", members);
            UiBuild.SetReference(view, "_potionTemplate", potionTemplate);
            UiBuild.SetReference(view, "_potionParent", potions);
            UiBuild.SetReference(view, "_detail", detail);
            return view;
        }

        /// <summary>
        /// One member card: name, job and HP on the left, the row and the two move buttons in the
        /// middle, the item slots on the right. It is low enough for a card per row to fit the board.
        /// </summary>
        static BoardMemberView BuildBoardMember(Transform parent)
        {
            Image background = UiBuild.Panel("MemberTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, BoardWidth, BoardCardHeight);
            Transform member = background.transform;

            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("MemberName", member, 26f, UiPalette.Text), 16f, 4f, 176f, 32f));
            TextMeshProUGUI job = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("MemberJob", member, 20f, UiPalette.TextDim), 16f, 36f, 176f, 24f));
            TextMeshProUGUI hp = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("MemberHp", member, 20f, UiPalette.Text), 16f, 60f, 176f, 24f));
            UiBar hpBar = UiBuild.Bar("MemberHpBar", member, UiPalette.Good);
            UiBuild.Box(hpBar, 16f, 86f, 176f, 8f);

            // The row the member stands in, and the buttons that trade places with the next row.
            TextMeshProUGUI row = UiBuild.Label("MemberRow", member, 26f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(row, 200f, 4f, 208f, 34f);
            ButtonParts forward = UiBuild.LocalizedButton("MemberForward", member, UiKeys.Board.Forward, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(forward.Rect, 200f, 46f, 100f, 44f);
            ButtonParts back = UiBuild.LocalizedButton("MemberBack", member, UiKeys.Board.Back, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(back.Rect, 308f, 46f, 100f, 44f);

            RectTransform slots = UiBuild.Box(UiBuild.Rect("MemberSlots", member), 420f, 8f, BoardWidth - 428f, BoardCardHeight - 16f);
            UiBuild.Horizontal(slots, 8f);
            ItemSlotView slotTemplate = BuildItemSlot(slots, BoardCardHeight - 16f);

            var view = background.gameObject.AddComponent<BoardMemberView>();
            UiBuild.SetReference(view, "_background", background);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_hp", hp);
            UiBuild.SetReference(view, "_hpBar", hpBar);
            UiBuild.SetReference(view, "_rowLabel", row);
            UiBuild.SetReference(view, "_forward", forward.Button);
            UiBuild.SetReference(view, "_back", back.Button);
            UiBuild.SetReference(view, "_slotTemplate", slotTemplate);
            UiBuild.SetReference(view, "_slotParent", slots);

            background.gameObject.SetActive(false);
            return view;
        }

        /// <summary>One item slot. Its width is set at runtime from the number of slots, so its texts stretch with it.</summary>
        static ItemSlotView BuildItemSlot(Transform parent, float height)
        {
            Image frame = UiBuild.Image("SlotTemplate", parent, UiPalette.Slot);
            UiBuild.Size(frame, 220f, height);
            Button button = UiBuild.MakeButton(frame);

            TextMeshProUGUI name = UiBuild.Label("SlotName", frame.transform, 20f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Stretch(name.rectTransform, 8f, 6f, 8f, 30f);
            TextMeshProUGUI grade = UiBuild.Label("SlotGrade", frame.transform, 18f, UiPalette.TextDim, TextAlignmentOptions.BottomLeft);
            UiBuild.Stretch(grade.rectTransform, 8f, height - 30f, 8f, 6f);

            var view = frame.gameObject.AddComponent<ItemSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_grade", grade);

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
