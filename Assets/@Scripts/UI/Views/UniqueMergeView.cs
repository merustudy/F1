using F1.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The merge of a recipe's pieces after a won battle (Slice D stage 50, Round 96 V1 "금빛 번짐", Docs/Architecture/12_UI.md "유니크의 조합"), over a
    /// board whose state is already the unique: the base and the catalyst as they were are drawn over it (copies of the board's own pieces), the line
    /// between them shows a moment, the catalyst is drawn into the base's middle, gold spreads over the base from there, the base underneath turns out
    /// the unique, and the gold lifts. Nothing before the merge glows (the user's Round 96 3). Presentation only: the merge is the state already.
    /// </summary>
    public sealed class UniqueMergeView : MonoBehaviour
    {
        public const float LineUntil = 0.15f;
        public const float DrawnIn = 0.45f;
        public const float Covered = 0.75f;
        public const float Done = 1.05f;
        const float CatalystEndScale = 0.3f;
        const float CatalystEndAlpha = 0.4f;
        const float GoldAlpha = 0.92f;

        ItemSlotView _base;
        ItemSlotView _catalyst;
        CanvasGroup _catalystGroup;
        LinkLines _line;
        RectTransform _goldMask;
        DiscGraphic _gold;
        Vector2 _catalystFrom;
        Vector2 _catalystTo;
        float _reach;

        /// <summary>Whether the gold is still on: the effect has not ended.</summary>
        public bool Playing { get; private set; }

        /// <summary>Lays the merge over a board's grid, the pieces as they were, at t = 0.</summary>
        public static UniqueMergeView Begin(GridBoardView board, UniqueForged forged, ExpeditionArt art)
        {
            var root = new GameObject("UniqueMerge", typeof(RectTransform), typeof(CanvasGroup), typeof(UniqueMergeView));
            var rect = (RectTransform)root.transform;
            rect.SetParent(board.PieceLayer, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(GridGeometry.Width, GridGeometry.Height);
            rect.SetAsLastSibling();
            var group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var view = root.GetComponent<UniqueMergeView>();
            var baseItem = new BoardItem(forged.BaseBefore, forged.BaseAt);
            var catalystItem = new BoardItem(forged.Catalyst, forged.CatalystAt);
            view._base = Piece(board, rect, baseItem, art);
            view._catalyst = Piece(board, rect, catalystItem, art);
            view._catalystGroup = view._catalyst.gameObject.AddComponent<CanvasGroup>();
            view._catalystFrom = view._catalyst.Rect.anchoredPosition;
            Vector2 baseMiddle = LinkLines.Middle(baseItem);
            Vector2 catalystMiddle = LinkLines.Middle(catalystItem);
            view._catalystTo = view._catalystFrom + (baseMiddle - catalystMiddle);

            view._line = LinkLines.Under(rect);
            view._line.ShowOne(baseMiddle, catalystMiddle);

            // The gold is a disc from the base's middle, clipped to the base's squares.
            var mask = new GameObject("Gold", typeof(RectTransform), typeof(RectMask2D));
            view._goldMask = (RectTransform)mask.transform;
            view._goldMask.SetParent(rect, false);
            GridGeometry.Lay(view._goldMask, baseItem.At.X, baseItem.At.Y, baseItem.Width, baseItem.Height);
            var disc = new GameObject("Disc", typeof(RectTransform), typeof(CanvasRenderer), typeof(DiscGraphic));
            view._gold = disc.GetComponent<DiscGraphic>();
            view._gold.raycastTarget = false;
            var discRect = (RectTransform)disc.transform;
            discRect.SetParent(view._goldMask, false);
            discRect.anchorMin = discRect.anchorMax = new Vector2(0.5f, 0.5f);
            discRect.pivot = new Vector2(0.5f, 0.5f);
            discRect.anchoredPosition = Vector2.zero;
            view._reach = view._goldMask.sizeDelta.magnitude / 2f + 2f;
            view.Playing = true;
            view.Apply(0f);
            return view;
        }

        static ItemSlotView Piece(GridBoardView board, RectTransform parent, BoardItem item, ExpeditionArt art)
        {
            ItemSlotView piece = Instantiate(board.PieceTemplate, parent);
            piece.gameObject.SetActive(true);
            GridGeometry.Lay(piece.Rect, item.At.X, item.At.Y, item.Width, item.Height);
            piece.ShowItem(item.Item, art?.OfItem(item.Item.Item.Id), item.At, false, 0, false, false, art?.GlowOfItem(item.Item.Item.Id));
            return piece;
        }

        /// <summary>The merge t seconds in. Returns whether it still plays.</summary>
        public bool Apply(float t)
        {
            _line.gameObject.SetActive(t < LineUntil);

            float drawn = Smooth((t - LineUntil) / (DrawnIn - LineUntil));
            _catalyst.gameObject.SetActive(t < DrawnIn);
            _catalyst.Rect.anchoredPosition = Vector2.Lerp(_catalystFrom, _catalystTo, drawn);
            _catalyst.Rect.localScale = Vector3.one * Mathf.Lerp(1f, CatalystEndScale, drawn);
            _catalystGroup.alpha = Mathf.Lerp(1f, CatalystEndAlpha, drawn);

            _base.gameObject.SetActive(t < Covered);
            float grow = Smooth((t - DrawnIn) / (Covered - DrawnIn));
            float fade = 1f - Smooth((t - Covered) / (Done - Covered));
            _gold.gameObject.SetActive(t >= DrawnIn && t < Done);
            _gold.rectTransform.sizeDelta = Vector2.one * (2f * _reach * grow);
            _gold.color = new Color(UiPalette.MergeGold.r, UiPalette.MergeGold.g, UiPalette.MergeGold.b, GoldAlpha * fade);

            Playing = t < Done;
            return Playing;
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
