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

            RectTransform members = UiBuild.Box(UiBuild.Rect("Members", board), 0f, 0f, BoardWidth, 418f);
            UiBuild.Vertical(members, 8f);
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

        static BoardMemberView BuildBoardMember(Transform parent)
        {
            Image background = UiBuild.Panel("MemberTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, BoardWidth, 134f);
            Transform member = background.transform;

            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("MemberName", member, 30f, UiPalette.Text), 16f, 8f, 220f, 40f));
            TextMeshProUGUI job = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("MemberJob", member, 22f, UiPalette.TextDim), 16f, 48f, 220f, 30f));
            TextMeshProUGUI hp = UiBuild.Box(UiBuild.Label("MemberHp", member, 22f, UiPalette.Text), 16f, 80f, 220f, 28f);
            UiBar hpBar = UiBuild.Bar("MemberHpBar", member, UiPalette.Good);
            UiBuild.Box(hpBar, 16f, 112f, 220f, 12f);

            ButtonParts row = UiBuild.Button("MemberRow", member, UiPalette.ButtonQuiet, 26f);
            UiBuild.Box(row.Rect, 250f, 37f, 110f, 60f);

            RectTransform slots = UiBuild.Box(UiBuild.Rect("MemberSlots", member), 372f, 10f, BoardWidth - 380f, 114f);
            UiBuild.Horizontal(slots, 8f);
            ItemSlotView slotTemplate = BuildItemSlot(slots);

            var view = background.gameObject.AddComponent<BoardMemberView>();
            UiBuild.SetReference(view, "_background", background);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_hp", hp);
            UiBuild.SetReference(view, "_hpBar", hpBar);
            UiBuild.SetReference(view, "_row", row.Button);
            UiBuild.SetReference(view, "_rowLabel", row.Label);
            UiBuild.SetReference(view, "_slotTemplate", slotTemplate);
            UiBuild.SetReference(view, "_slotParent", slots);

            background.gameObject.SetActive(false);
            return view;
        }

        /// <summary>One item slot. Its width is set at runtime from the number of slots, so its texts stretch with it.</summary>
        static ItemSlotView BuildItemSlot(Transform parent)
        {
            Image frame = UiBuild.Image("SlotTemplate", parent, UiPalette.Slot);
            UiBuild.Size(frame, 220f, 114f);
            Button button = UiBuild.MakeButton(frame);

            TextMeshProUGUI name = UiBuild.Label("SlotName", frame.transform, 22f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Stretch(name.rectTransform, 10f, 8f, 10f, 42f);
            TextMeshProUGUI grade = UiBuild.Label("SlotGrade", frame.transform, 20f, UiPalette.TextDim, TextAlignmentOptions.BottomLeft);
            UiBuild.Stretch(grade.rectTransform, 10f, 76f, 10f, 8f);

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
