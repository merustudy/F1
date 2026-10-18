using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>The trait card (TraitTooltipView): its height, and the icon's size and place.</summary>
        const float TraitTooltipHeight = 132f;
        const float TraitTooltipIcon = 40f;
        const float TraitTooltipIconTop = 16f;

        /// <summary>
        /// A mercenary's trait line (Slice D stage 37, Round 84 안 A): <paramref name="chips"/> chips side by side from the left — each the
        /// placeholder icon (a square, its rim in the trait's colour, the name's first letter) and, when <paramref name="names"/>, the name — and
        /// the "+N" pill. The view lays them out by their widths; the chips open the window's trait card.
        /// </summary>
        static TraitLineView BuildTraitLine(string name, Transform parent, float x, float y, float height, float iconSize, float fontSize, int chips, bool names,
            TraitTooltipView tooltip)
        {
            RectTransform line = UiBuild.Box(UiBuild.Rect(name, parent), x, y, 10f, height);
            var views = new TraitChipBase[chips];
            for (int i = 0; i < chips; i++)
            {
                views[i] = BuildTraitChip(name + "Chip" + i, line, height, iconSize, fontSize, names, tooltip);
            }

            TraitMoreView more = BuildTraitMore(name + "More", line, height, iconSize, fontSize, tooltip);
            var view = line.gameObject.AddComponent<TraitLineView>();
            UiBuild.SetReferences(view, "_chips", views);
            UiBuild.SetReference(view, "_more", more);
            UiBuild.SetFloat(view, "_gap", names ? 14f : 4f);
            return view;
        }

        /// <summary>A trait's chip: the icon and, when <paramref name="withName"/>, the name (<see cref="TraitChipView"/>), else the icon alone (<see cref="TraitIconView"/>).</summary>
        static TraitChipBase BuildTraitChip(string name, RectTransform line, float height, float iconSize, float fontSize, bool withName, TraitTooltipView tooltip)
        {
            RectTransform chip = UiBuild.Box(UiBuild.Rect(name, line), 0f, 0f, iconSize, height);
            // The whole chip, icon and name, takes the pointer.
            UiBuild.Stretch(UiBuild.Image(name + "Hit", chip, Color.clear, raycastTarget: true).rectTransform);
            float top = (height - iconSize) / 2f;
            Image rim = UiBuild.Image(name + "Rim", chip, UiPalette.TraitStrength);
            UiBuild.Box(rim.rectTransform, 0f, top, iconSize, iconSize);
            UiBuild.Box(UiBuild.Image(name + "Fill", chip, UiPalette.TraitIconFill).rectTransform, 2f, top + 2f, iconSize - 4f, iconSize - 4f);
            TextMeshProUGUI glyph = UiBuild.SingleLine(UiBuild.Label(name + "Glyph", chip, Mathf.Round(iconSize * 0.55f), UiPalette.TraitStrength, TextAlignmentOptions.Center));
            UiBuild.Box(glyph.rectTransform, 0f, top, iconSize, iconSize);
            TraitChipBase view;
            if (withName)
            {
                TextMeshProUGUI label = UiBuild.SingleLine(UiBuild.Label(name + "Name", chip, fontSize, UiPalette.Text));
                UiBuild.Box(label.rectTransform, iconSize + TraitChipView.NameGap, 0f, 200f, height);
                view = chip.gameObject.AddComponent<TraitChipView>();
                UiBuild.SetReference(view, "_name", label);
            }
            else
            {
                view = chip.gameObject.AddComponent<TraitIconView>();
            }

            UiBuild.SetReference(view, "_rim", rim);
            UiBuild.SetReference(view, "_glyph", glyph);
            UiBuild.SetFloat(view, "_iconSize", iconSize);
            UiBuild.SetReference(view, "_tooltip", tooltip);
            chip.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The "+N" pill at the end of a line that cannot hold every trait (Round 84 판정 3): a quiet rounded pill with the count.</summary>
        static TraitMoreView BuildTraitMore(string name, RectTransform line, float height, float iconSize, float fontSize, TraitTooltipView tooltip)
        {
            RectTransform chip = UiBuild.Box(UiBuild.Rect(name, line), 0f, 0f, 40f, height);
            float top = (height - iconSize) / 2f;
            Image pill = Rounded(name + "Pill", chip, UiPalette.TraitMore, iconSize / 2f);
            pill.raycastTarget = true;
            UiBuild.Stretch(pill.rectTransform, 0f, top, 0f, top);
            TextMeshProUGUI label = UiBuild.SingleLine(UiBuild.Label(name + "Label", chip, fontSize, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Box(label.rectTransform, TraitMoreView.Padding, 0f, 24f, height);
            var view = chip.gameObject.AddComponent<TraitMoreView>();
            UiBuild.SetReference(view, "_pill", pill);
            UiBuild.SetReference(view, "_label", label);
            UiBuild.SetReference(view, "_tooltip", tooltip);
            chip.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// The trait card of a window (Slice D stage 37, Round 84 판정 6): the item card's black and grey, its notch under it; the icon and the
        /// name on top, the kind under them, what the trait does at the bottom. It stands over everything in the window and takes no clicks.
        /// </summary>
        /// <param name="prefix">Names the card's objects apart from another window's card in the same screen.</param>
        static TraitTooltipView BuildTraitTooltip(Transform window, string prefix)
        {
            RectTransform card = UiBuild.Rect(prefix + "TraitTooltip", window);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0f);
            card.sizeDelta = new Vector2(TraitTooltipView.MinWidth, TraitTooltipHeight);
            // Round 84's mock: the window's dark panel inside a 2px brass rim, and a brass notch under it pointing at the chip.
            UiBuild.Stretch(UiBuild.Image(prefix + "TraitTooltipLine", card, UiPalette.Brass).rectTransform);
            UiBuild.Stretch(UiBuild.Image(prefix + "TraitTooltipFill", card, UiPalette.Panel).rectTransform, 2f, 2f, 2f, 2f);
            RectTransform notch = OutOfLayout(UiBuild.Rect(prefix + "TraitTooltipNotch", card));
            notch.gameObject.AddComponent<RectMask2D>();
            notch.anchorMin = new Vector2(0.5f, 0f);
            notch.anchorMax = new Vector2(0.5f, 0f);
            notch.pivot = new Vector2(0.5f, 1f);
            notch.sizeDelta = new Vector2(TooltipNotch * 2f, TooltipNotch);
            notch.anchoredPosition = Vector2.zero;
            Diamond(prefix + "TraitTooltipNotchFill", notch, UiPalette.Brass, TooltipNotch, new Vector2(0.5f, 1f));

            RectTransform icon = UiBuild.Box(UiBuild.Rect(prefix + "TraitTooltipIcon", card), TraitTooltipView.Padding, TraitTooltipIconTop, TraitTooltipIcon, TraitTooltipIcon);
            Image rim = UiBuild.Image(prefix + "TraitTooltipIconRim", icon, UiPalette.TraitStrength);
            UiBuild.Stretch(rim.rectTransform);
            UiBuild.Stretch(UiBuild.Image(prefix + "TraitTooltipIconFill", icon, UiPalette.TraitIconFill).rectTransform, 3f, 3f, 3f, 3f);
            TextMeshProUGUI glyph = UiBuild.SingleLine(UiBuild.Label(prefix + "TraitTooltipGlyph", icon, 22f, UiPalette.TraitStrength, TextAlignmentOptions.Center));
            UiBuild.Stretch(glyph.rectTransform);

            float nameLeft = TraitTooltipView.Padding + TraitTooltipIcon + 14f;
            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Label(prefix + "TraitTooltipName", card, 26f, UiPalette.Text));
            UiBuild.Box(title.rectTransform, nameLeft, TraitTooltipIconTop, 200f, TraitTooltipIcon);
            TextMeshProUGUI kind = UiBuild.SingleLine(UiBuild.Label(prefix + "TraitTooltipKind", card, 18f, UiPalette.TextDim));
            UiBuild.Box(kind.rectTransform, TraitTooltipView.Padding, 66f, 260f, 26f);
            TextMeshProUGUI effect = UiBuild.SingleLine(UiBuild.Label(prefix + "TraitTooltipEffect", card, 22f, UiPalette.Text));
            UiBuild.Box(effect.rectTransform, TraitTooltipView.Padding, 94f, 260f, 30f);

            foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            var view = card.gameObject.AddComponent<TraitTooltipView>();
            UiBuild.SetReference(view, "_icon", icon.gameObject);
            UiBuild.SetReference(view, "_iconRim", rim);
            UiBuild.SetReference(view, "_glyph", glyph);
            UiBuild.SetReference(view, "_name", title);
            UiBuild.SetReference(view, "_kind", kind);
            UiBuild.SetReference(view, "_effect", effect);
            UiBuild.SetReference(view, "_notch", notch);
            UiBuild.SetFloat(view, "_nameLeft", nameLeft);
            card.gameObject.SetActive(false);
            return view;
        }
    }
}
