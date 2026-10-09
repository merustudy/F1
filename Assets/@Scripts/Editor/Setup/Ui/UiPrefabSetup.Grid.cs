using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        // ---- The grid boards (Slice B stage 19; Docs/Architecture/12_UI.md "격자 보드") --------------------------------------------
        //
        // A board is the frame's 3 x 8 squares of 50 with gaps of 2 (GridGeometry) at the top of its panel column, BoardCellsTop under the
        // column's top. The look is Diablo II's inventory (round 49): each bag a well in the stone (a sunk rim tinted by its leather, grey
        // lines between black squares), an item's squares one piece of dark blue (none in battle), nothing where no bag is.

        /// <summary>The ghost's words: a dark plate with the words in the ghost's colour.</summary>
        const float GhostLabelFontSize = 15f;

        /// <summary>The line round a ghost.</summary>
        const float GhostEdgeWidth = 2f;

        /// <summary>The sunk rim's lines: dark at the top and left, light at the bottom and right.</summary>
        const float BevelWidth = 2f;

        /// <summary>A frame square's dashed line while a bag is held (round 48's mockups: dashes of 4 every 7).</summary>
        const float FrameDash = 4f;
        const float FrameGap = 3f;

        /// <summary>
        /// A member's board between battles as a grid (GridBoardView): the grid's rect at the top of the column, and in it, in the order
        /// they are drawn, the bags, the squares (which take the pointer), the pieces and the ghost. The view makes the bags, the squares
        /// and the pieces from the templates at runtime. Every object is named after the prefix.
        /// </summary>
        static GridBoardView BuildGridBoard(Transform column, string p)
        {
            RectTransform grid = UiBuild.Rect(p + "Grid", column);
            UiBuild.Place(grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -BoardCellsTop), new Vector2(GridGeometry.Width, GridGeometry.Height));

            RectTransform bags = Layer(grid, p + "Bags");
            RectTransform squares = Layer(grid, p + "Squares");
            RectTransform pieces = Layer(grid, p + "Pieces");
            RectTransform ghosts = Layer(grid, p + "Ghost");

            Image bagTemplate = BuildBagLeather(bags, p + "BagTemplate");
            GridSquareView squareTemplate = BuildGridSquare(squares, p + "SquareTemplate");
            ItemSlotView pieceTemplate = BuildItemSlot(pieces, p + "PieceTemplate");

            // The ghost's squares are see-through fills without a line (an outline copies the whole square, which would fill it); one edge
            // of four thin lines goes round the whole shape.
            Image ghostSquare = UiBuild.Image(p + "GhostSquareTemplate", ghosts, Color.white);
            ghostSquare.gameObject.SetActive(false);

            Image ghostBag = KitFrame(p + "GhostBag", ghosts, UiArt.Slot);
            ghostBag.gameObject.SetActive(false);

            RectTransform ghostEdge = UiBuild.Rect(p + "GhostEdge", ghosts);
            Image[] edgeLines =
            {
                EdgeLine(ghostEdge, p + "GhostEdgeTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, GhostEdgeWidth)),
                EdgeLine(ghostEdge, p + "GhostEdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, GhostEdgeWidth)),
                EdgeLine(ghostEdge, p + "GhostEdgeLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(GhostEdgeWidth, 0f)),
                EdgeLine(ghostEdge, p + "GhostEdgeRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(GhostEdgeWidth, 0f)),
            };
            ghostEdge.gameObject.SetActive(false);

            RectTransform ghostArt = UiBuild.Rect(p + "GhostArt", ghosts);
            Image ghostIcon = UiBuild.Image(p + "GhostIcon", ghostArt, Color.white);
            ghostIcon.preserveAspect = true;
            UiBuild.Stretch(ghostIcon.rectTransform);
            ghostArt.gameObject.SetActive(false);

            Image plate = UiBuild.Image(p + "GhostLabel", ghosts, UiPalette.LabelBox);
            AddLine(plate, UiPalette.LabelLine, 1f);
            TextMeshProUGUI label = UiBuild.SingleLine(UiBuild.Label(p + "GhostLabelText", plate.transform, GhostLabelFontSize, UiPalette.Text, TextAlignmentOptions.Center));
            label.overflowMode = TextOverflowModes.Overflow;
            UiBuild.Stretch(label.rectTransform);
            plate.gameObject.SetActive(false);

            var view = grid.gameObject.AddComponent<GridBoardView>();
            UiBuild.SetReference(view, "_grid", grid);
            UiBuild.SetReference(view, "_bagLayer", bags);
            UiBuild.SetReference(view, "_squareLayer", squares);
            UiBuild.SetReference(view, "_pieceLayer", pieces);
            UiBuild.SetReference(view, "_ghostLayer", ghosts);
            UiBuild.SetReference(view, "_bagTemplate", bagTemplate);
            UiBuild.SetReference(view, "_squareTemplate", squareTemplate);
            UiBuild.SetReference(view, "_pieceTemplate", pieceTemplate);
            UiBuild.SetReference(view, "_ghostSquareTemplate", ghostSquare);
            UiBuild.SetReference(view, "_ghostBag", ghostBag);
            UiBuild.SetReference(view, "_ghostArt", ghostArt);
            UiBuild.SetReference(view, "_ghostIcon", ghostIcon);
            UiBuild.SetReference(view, "_ghostLabelPlate", plate.rectTransform);
            UiBuild.SetReference(view, "_ghostLabel", label);
            UiBuild.SetReference(view, "_ghostEdge", ghostEdge);
            UiBuild.SetReferences(view, "_ghostEdgeLines", edgeLines);
            return view;
        }

        /// <summary>One side of the ghost's edge: a thin line stretched along a side of the edge's rect, inside it. Takes no pointer.</summary>
        static Image EdgeLine(RectTransform edge, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 thickness)
        {
            Image line = UiBuild.Image(name, edge, Color.white);
            line.raycastTarget = false;
            RectTransform rect = line.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = thickness;
            return line;
        }

        /// <summary>A dashed line stretched over a parent's rect (`DashedFrame`). Takes no pointer.</summary>
        static DashedFrame AddDashes(Transform parent, string name, float thickness, float dash, float gap, float inset, Color color)
        {
            RectTransform rect = UiBuild.Rect(name, parent);
            UiBuild.Stretch(rect);
            var dashes = rect.gameObject.AddComponent<DashedFrame>();
            dashes.Shape(thickness, dash, gap, inset);
            dashes.color = color;
            return dashes;
        }

        /// <summary>A rect over the whole grid that only holds what is drawn in one layer.</summary>
        static RectTransform Layer(RectTransform grid, string name)
        {
            RectTransform layer = UiBuild.Rect(name, grid);
            UiBuild.Stretch(layer);
            return layer;
        }

        /// <summary>A thin line round a graphic (UGUI's outline effect), in a colour the view may change.</summary>
        static Outline AddLine(Graphic graphic, Color color, float width)
        {
            var line = graphic.gameObject.AddComponent<Outline>();
            line.effectColor = color;
            line.effectDistance = new Vector2(width, -width);
            line.useGraphicAlpha = false;
            return line;
        }

        /// <summary>
        /// A bag as Diablo's well (round 49, "처음 디아블로 안의 돌 테"): a plain rim the view tints (the stone with the bag's leather in it)
        /// reaching GridGeometry.BagRim past the squares, sunk; inside it the grey that the gaps between the black squares show; a line round
        /// it the view makes gold while the bag is held. Takes no pointer.
        /// </summary>
        static Image BuildBagLeather(Transform parent, string name)
        {
            Image rim = UiBuild.Image(name, parent, Color.white);
            AddLine(rim, UiPalette.Ink, 1f);
            Image well = UiBuild.Image(name + "Well", rim.transform, UiPalette.GridSquareLine);
            UiBuild.Stretch(well.rectTransform, GridGeometry.BagRim, GridGeometry.BagRim, GridGeometry.BagRim, GridGeometry.BagRim);
            AddSunkBevel(rim.rectTransform, name);
            rim.gameObject.SetActive(false);
            return rim;
        }

        /// <summary>A sunk edge round a rect: a dark line at its top and left, a light one at its bottom and right, inside it.</summary>
        static void AddSunkBevel(RectTransform rect, string name)
        {
            EdgeLine(rect, name + "BevelTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, BevelWidth)).color = UiPalette.BevelDark;
            EdgeLine(rect, name + "BevelLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(BevelWidth, 0f)).color = UiPalette.BevelDark;
            EdgeLine(rect, name + "BevelBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, BevelWidth)).color = UiPalette.BevelLight;
            EdgeLine(rect, name + "BevelRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(BevelWidth, 0f)).color = UiPalette.BevelLight;
        }

        /// <summary>One square of a board's frame: a plain square with a thin line, which takes the pointer even while the view shows nothing.</summary>
        static GridSquareView BuildGridSquare(Transform parent, string name)
        {
            Image image = UiBuild.Image(name, parent, Color.clear, raycastTarget: true);
            UiBuild.Size(image, GridGeometry.Square, GridGeometry.Square);
            Outline line = AddLine(image, UiPalette.GridSquareLine, 1f);

            // The frame's square while a bag is held (round 48's mockups): a dashed line round it.
            DashedFrame dashes = AddDashes(image.transform, name + "Dashes", 1f, FrameDash, FrameGap, 0f, UiPalette.GridFrameLine);
            dashes.enabled = false;
            var view = image.gameObject.AddComponent<GridSquareView>();
            UiBuild.SetReference(view, "_image", image);
            UiBuild.SetReference(view, "_line", line);
            UiBuild.SetReference(view, "_dashes", dashes);
            image.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// An item's piece (ItemSlotView), the board's and the tiles' alike: a plain piece the view colours (Diablo's dark blue, round 49);
        /// the icon's box (turned at runtime) with the silhouette of a tier's outline behind the icon (off in Diablo's look); the name for an
        /// item without an icon; the fatigue tag at the top-right, the merge mark and the tier's stars at the bottom-left; and for a bag
        /// shown as a thing, the grey and the black squares inside its rim. Takes no pointer.
        /// </summary>
        static ItemSlotView BuildItemSlot(Transform parent, string name)
        {
            Image frame = UiBuild.Image(name, parent, UiPalette.GridPiece);
            UiBuild.Size(frame, GridGeometry.Square, GridGeometry.Square);
            Outline line = AddLine(frame, UiPalette.LabelLine, 1f);

            Image picked = UiBuild.Image(name + "Picked", frame.transform, UiPalette.GridPicked);
            UiBuild.Stretch(picked.rectTransform);
            picked.enabled = false;

            RectTransform art = UiBuild.Rect(name + "Art", frame.transform);
            UiBuild.Size(art, GridGeometry.Square - 2f * ItemSlotView.ArtMargin, GridGeometry.Square - 2f * ItemSlotView.ArtMargin);
            Image outline = BuildOutline(art, name + "Outline", out SilhouetteOutline outlineEffect);
            UiBuild.Stretch(outline.rectTransform);
            Image icon = UiBuild.Image(name + "Icon", art, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform);

            TextMeshProUGUI text = UiBuild.Label(name + "Text", frame.transform, 15f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Stretch(text.rectTransform, 4f, 2f, 4f, 2f);
            UiBuild.ShrinkToFit(text, 10f);

            // The fatigue tag at the top-right corner: "+1" on equipment that costs fatigue when a battle starts (round 32, B1).
            TextMeshProUGUI fatigueText = BuildFatigueTag(frame.transform, name + "Fatigue", Vector2.one, new Vector2(-FatigueTagInset, -FatigueTagInset), out GameObject fatigue);

            // The mark of a piece the held item would merge into (round 35): a veil with the words of the tier the merge makes.
            RectTransform merge = UiBuild.Rect(name + "Merge", frame.transform);
            UiBuild.Stretch(merge);
            Image veil = UiBuild.Image(name + "MergeVeil", merge, new Color(0.08f, 0.09f, 0.12f, MergeVeilAlpha));
            UiBuild.Stretch(veil.rectTransform, MergeVeilInset, MergeVeilInset, MergeVeilInset, MergeVeilInset);
            TextMeshProUGUI mergeText = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label(name + "MergeText", merge, 16f, UiPalette.Text, TextAlignmentOptions.Center)), 10f);
            UiBuild.Stretch(mergeText.rectTransform, 2f, 0f, 2f, 0f);
            merge.gameObject.SetActive(false);

            // A bag shown as a thing (round 49): the grey inside its rim and the black squares on it.
            Image bagWell = UiBuild.Image(name + "BagWell", frame.transform, UiPalette.GridSquareLine);
            UiBuild.Stretch(bagWell.rectTransform, GridGeometry.BagRim, GridGeometry.BagRim, GridGeometry.BagRim, GridGeometry.BagRim);
            RectTransform bagSquaresRect = UiBuild.Rect(name + "BagSquares", bagWell.transform);
            UiBuild.Stretch(bagSquaresRect);
            var bagSquares = bagSquaresRect.gameObject.AddComponent<SquareGrid>();
            bagSquares.color = UiPalette.GridSquare;
            bagSquares.Shape(1, 1, GridGeometry.Gap);
            bagWell.gameObject.SetActive(false);

            // Over everything, the tier tag with the stars (round 41).
            Image tierTag = BuildTierTag(frame.transform, name + "Tier", out Image[] stars);

            var view = frame.gameObject.AddComponent<ItemSlotView>();
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_frameLine", line);
            UiBuild.SetReference(view, "_picked", picked);
            UiBuild.SetReference(view, "_art", art);
            UiBuild.SetReference(view, "_outline", outline);
            UiBuild.SetReference(view, "_outlineEffect", outlineEffect);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_text", text);
            UiBuild.SetReference(view, "_fatigue", fatigue);
            UiBuild.SetReference(view, "_fatigueText", fatigueText);
            UiBuild.SetReference(view, "_tierTag", tierTag);
            UiBuild.SetReferences(view, "_stars", stars);
            UiBuild.SetReference(view, "_merge", merge.gameObject);
            UiBuild.SetReference(view, "_mergeText", mergeText);
            UiBuild.SetReference(view, "_bagWell", bagWell.gameObject);
            UiBuild.SetReference(view, "_bagSquares", bagSquares);

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// One item in battle (BattleItemView): its dark piece, which takes the right click (the card; round 47), and in it two copies of
        /// the icon's box (turned at runtime): the dark one, and the lit one inside a mask that grows from the piece's left as the item
        /// charges, with the white of the brief brightening over its icon; the name for an item without an icon; the tier tag.
        /// </summary>
        static BattleItemView BuildBattleItem(Transform parent)
        {
            Image piece = UiBuild.Image("ItemTemplate", parent, Color.clear, raycastTarget: true);
            UiBuild.Size(piece, GridGeometry.Square, GridGeometry.Square);
            Outline line = AddLine(piece, UiPalette.LabelLine, 1f);

            // Round 42: the piece takes the click (a right click opens the item's card, round 47). Silent and without tints: a piece
            // that may not be clicked (a potion waits for its board) gives up its raycast instead of dimming.
            Button button = UiBuild.MakeButton(piece);
            UiBuild.Silence(button);
            button.transition = Selectable.Transition.None;

            RectTransform darkArt = UiBuild.Rect("ItemDarkArt", piece.transform);
            Image darkOutline = BuildOutline(darkArt, "ItemDarkOutline", out SilhouetteOutline darkOutlineEffect);
            UiBuild.Stretch(darkOutline.rectTransform);
            Image darkIcon = UiBuild.Image("ItemDarkIcon", darkArt, Color.white);
            darkIcon.preserveAspect = true;
            UiBuild.Stretch(darkIcon.rectTransform);

            RectTransform mask = UiBuild.Rect("ItemLit", piece.transform);
            mask.gameObject.AddComponent<RectMask2D>();
            RectTransform litArt = UiBuild.Rect("ItemLitArt", mask);
            Image outline = BuildOutline(litArt, "ItemOutline", out SilhouetteOutline outlineEffect);
            UiBuild.Stretch(outline.rectTransform);
            Image icon = UiBuild.Image("ItemIcon", litArt, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform);
            Image flash = UiBuild.Image("ItemFlash", litArt, new Color(1f, 1f, 1f, 0f));
            flash.preserveAspect = true;
            flash.material = SilhouetteMaterial();
            UiBuild.Stretch(flash.rectTransform);
            flash.enabled = false;

            TextMeshProUGUI name = UiBuild.ShrinkToFit(UiBuild.Label("ItemName", piece.transform, 15f, UiPalette.Text, TextAlignmentOptions.Center), 10f);
            UiBuild.Stretch(name.rectTransform, 4f, 2f, 4f, 2f);

            Image tierTag = BuildTierTag(piece.transform, "ItemTier", out Image[] stars);

            var view = piece.gameObject.AddComponent<BattleItemView>();
            UiBuild.SetReference(view, "_frame", piece);
            UiBuild.SetReference(view, "_frameLine", line);
            UiBuild.SetReference(view, "_darkArt", darkArt);
            UiBuild.SetReference(view, "_darkOutline", darkOutline);
            UiBuild.SetReference(view, "_darkOutlineEffect", darkOutlineEffect);
            UiBuild.SetReference(view, "_darkIcon", darkIcon);
            UiBuild.SetReference(view, "_litMask", mask);
            UiBuild.SetReference(view, "_litArt", litArt);
            UiBuild.SetReference(view, "_outline", outline);
            UiBuild.SetReference(view, "_outlineEffect", outlineEffect);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_flash", flash);
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_rightClick", piece.gameObject.AddComponent<RightClick>());
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_tierTag", tierTag);
            UiBuild.SetReferences(view, "_stars", stars);

            piece.gameObject.SetActive(false);
            return view;
        }

        /// <summary>An empty square of a bag in battle: Diablo's black (the grey of the gaps is the bag's). Takes no pointer.</summary>
        static Image BuildBattleSquare(Transform parent)
        {
            Image square = UiBuild.Image("SquareTemplate", parent, UiPalette.GridSquare);
            UiBuild.Size(square, GridGeometry.Square, GridGeometry.Square);
            square.gameObject.SetActive(false);
            return square;
        }
    }
}
