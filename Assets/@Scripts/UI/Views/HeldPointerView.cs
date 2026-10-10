using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>An item lying in a held bag, for drawing it on the pointer: where it lies in the bag's squares and how it is turned.</summary>
    public readonly struct HeldBagItem
    {
        public HeldBagItem(Sprite icon, int x, int y, int width, int height, int turns)
        {
            Icon = icon;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Turns = turns;
        }

        public Sprite Icon { get; }

        /// <summary>Its top-left square in the bag's squares as the bag lies on its board.</summary>
        public int X { get; }
        public int Y { get; }

        /// <summary>Its squares across and down as it lies (turned).</summary>
        public int Width { get; }
        public int Height { get; }
        public int Turns { get; }
    }

    /// <summary>
    /// What is held, on the pointer (round 54, Diablo II's inventory; Docs/Architecture/12_UI.md "격자 보드"). While the hand holds
    /// something, a clear layer over the whole screen takes every press first and says where (<see cref="Pressed"/>): the screen then
    /// puts what is held there, lets a button that works on it through, or lets go so that it is back where it was. The held thing is
    /// drawn in place of the pointer, which is hidden: an item's icon at its squares' size (no ground), a bag's well with the items in it,
    /// turned as held, its centre on the pointer. The screen shows and hides it with the hand; it moves with the mouse by itself.
    /// </summary>
    public sealed class HeldPointerView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] RectTransform _shape;
        [SerializeField] RectTransform _art;
        [SerializeField] Image _icon;
        [SerializeField] GameObject _bagRim;
        [SerializeField] Image[] _bagRimEdges;
        [SerializeField] SquareGrid _squares;
        [SerializeField] Image _bagItemTemplate;

        /// <summary>A bag on the pointer is see-through (its rim a frame, its squares dim) so that the colour where it would land shows under it; its items are not.</summary>
        const float BagRimAlpha = 0.85f;
        const float BagSquaresAlpha = 0.45f;

        readonly List<Image> _bagItems = new List<Image>();
        Vector2 _lastPointer;
        bool _primed;
        Vector2 _lastDrawn;
        bool _following;
        Camera _camera;
        bool _cameraKnown;

        /// <summary>A left press anywhere while something is held, at this screen position.</summary>
        public event Action<Vector2> Pressed;

        /// <summary>Whether something is on the pointer.</summary>
        public bool Shown => gameObject.activeSelf;

        /// <summary>The held item's icon on the pointer, or null (nothing held, a bag, or an item without one).</summary>
        public Sprite Icon => Shown && _art.gameObject.activeSelf ? _icon.sprite : null;

        /// <summary>The held bag on the pointer, or null.</summary>
        public BagData Bag { get; private set; }

        /// <summary>How the held thing is drawn turned, in quarters clockwise.</summary>
        public int Turns => Mathf.RoundToInt(-_shape.localEulerAngles.z / 90f + 4f) % 4;

        /// <summary>The held thing's unturned size as drawn (its squares and the gaps between them).</summary>
        public Vector2 Size => _shape.sizeDelta;

        /// <summary>The camera the screen's canvas is seen through (null for an overlay canvas): what screen positions are read with.</summary>
        public Camera EventCamera
        {
            get
            {
                if (!_cameraKnown)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    Canvas root = canvas != null ? canvas.rootCanvas : null;
                    _camera = root == null || root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
                    _cameraKnown = true;
                }

                return _camera;
            }
        }

        /// <summary>What a hand holds, on the pointer; nothing held hides it (and the pointer comes back).</summary>
        public void ShowHand(BoardHand hand, ExpeditionState expedition, ExpeditionArt art)
        {
            if (hand == null || expedition == null || !hand.Holding)
            {
                if (Shown)
                {
                    Hide();
                }

                return;
            }

            BoardBag boardBag = hand.HeldBag(expedition);
            if (boardBag != null)
            {
                var items = new List<HeldBagItem>();
                foreach (BoardItem item in expedition.Members[hand.BagMember].Board.ItemsIn(boardBag))
                {
                    items.Add(new HeldBagItem(art?.OfItem(item.Item.Item.Id), item.At.X - boardBag.At.X, item.At.Y - boardBag.At.Y, item.Width, item.Height, item.At.Turns));
                }

                ShowBag(boardBag.Bag, boardBag.At.Turns, hand.Turns, items);
                return;
            }

            if (hand.Outside?.Bag != null)
            {
                ShowBag(hand.Outside.Bag, 0, hand.Turns, null);
                return;
            }

            EquippedItem held = hand.HeldItem(expedition);
            if (held == null)
            {
                Hide();
                return;
            }

            ShowItem(art?.OfItem(held.Item.Id), held.Item.Width, held.Item.Height, hand.Turns);
        }

        /// <summary>An item on the pointer: its icon over its squares (unturned across and down), turned so many quarters.</summary>
        public void ShowItem(Sprite icon, int width, int height, int turns)
        {
            Open();
            Bag = null;
            Vector2 size = new Vector2(GridGeometry.Span(width), GridGeometry.Span(height));
            _shape.sizeDelta = size;
            _shape.localRotation = GridGeometry.ArtTurn(turns);
            _bagRim.SetActive(false);
            HideBagItems();

            // An item without an icon shows its squares in the held blue instead.
            bool pictured = icon != null;
            _art.gameObject.SetActive(pictured);
            _squares.gameObject.SetActive(!pictured);
            if (pictured)
            {
                _art.sizeDelta = GridGeometry.ArtBox(size, 0, ItemSlotView.ArtMargin);
                _icon.sprite = icon;
            }
            else
            {
                _squares.color = UiPalette.GridPiecePicked;
                _squares.Shape(width, height, GridGeometry.Gap);
            }
        }

        /// <summary>
        /// A bag on the pointer: its well (the stone rim tinted by its leather round its black squares) with the items in it, drawn as it
        /// lay on its board (<paramref name="baseTurns"/>; across and down as it lay) and turned on to <paramref name="turns"/>.
        /// </summary>
        public void ShowBag(BagData bag, int baseTurns, int turns, IReadOnlyList<HeldBagItem> items)
        {
            Open();
            Bag = bag;
            var lay = new Placement(0, 0, baseTurns);
            int width = lay.WidthOf(bag.Width, bag.Height);
            int height = lay.HeightOf(bag.Width, bag.Height);
            _shape.sizeDelta = new Vector2(GridGeometry.Span(width), GridGeometry.Span(height));
            _shape.localRotation = GridGeometry.ArtTurn(turns - baseTurns);
            _art.gameObject.SetActive(false);
            _bagRim.SetActive(true);
            Color rim = UiPalette.BagRim(bag.Id);
            foreach (Image edge in _bagRimEdges)
            {
                edge.color = new Color(rim.r, rim.g, rim.b, BagRimAlpha);
            }
            _squares.gameObject.SetActive(true);
            _squares.color = new Color(UiPalette.GridSquare.r, UiPalette.GridSquare.g, UiPalette.GridSquare.b, BagSquaresAlpha);
            _squares.Shape(width, height, GridGeometry.Gap);

            int shown = 0;
            foreach (HeldBagItem item in items ?? Array.Empty<HeldBagItem>())
            {
                if (item.Icon == null)
                {
                    continue;
                }

                if (shown == _bagItems.Count)
                {
                    _bagItems.Add(Instantiate(_bagItemTemplate, _shape));
                }

                Image image = _bagItems[shown++];
                image.gameObject.SetActive(true);
                Vector2 piece = new Vector2(GridGeometry.Span(item.Width), GridGeometry.Span(item.Height));
                Vector2 corner = GridGeometry.Corner(item.X, item.Y);
                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(corner.x + piece.x / 2f, corner.y - piece.y / 2f);
                rect.sizeDelta = GridGeometry.ArtBox(piece, item.Turns, ItemSlotView.ArtMargin);
                rect.localRotation = GridGeometry.ArtTurn(item.Turns);
                image.sprite = item.Icon;
            }

            for (int i = shown; i < _bagItems.Count; i++)
            {
                _bagItems[i].gameObject.SetActive(false);
            }
        }

        /// <summary>Nothing held: the layer and the drawing go, and the pointer comes back.</summary>
        public void Hide()
        {
            Bag = null;
            _primed = false;
            _following = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Draws the held thing with its centre at a screen position (tests, where no mouse moves: the pointer is where they press or
        /// point). The mouse as it is now counts as followed, so the drawing stays here until the mouse moves.
        /// </summary>
        public void PlaceAt(Vector2 screen)
        {
            Place(screen);
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                _following = true;
                _lastDrawn = mouse.position.ReadValue();
            }
        }

        /// <summary>The screen position of the held thing's centre as drawn.</summary>
        public Vector2 DrawnAt => RectTransformUtility.WorldToScreenPoint(EventCamera, _shape.TransformPoint(_shape.rect.center));

        void Place(Vector2 screen)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, EventCamera, out Vector2 local))
            {
                _shape.anchoredPosition = local;
            }
        }

        /// <summary>
        /// Whether the mouse moved since the last ask while something is held, and where it is: the screen aims the hand again then. The
        /// first ask after the hand took hold only notes where the mouse is (the press that took hold aimed already).
        /// </summary>
        public bool PointerMoved(out Vector2 screen)
        {
            screen = default;
            Mouse mouse = Mouse.current;
            if (!Shown || mouse == null)
            {
                return false;
            }

            screen = mouse.position.ReadValue();
            if (!_primed)
            {
                _primed = true;
                _lastPointer = screen;
                return false;
            }

            if (screen == _lastPointer)
            {
                return false;
            }

            _lastPointer = screen;
            return true;
        }

        /// <summary>Tests: a left press at a screen position, as the layer takes it.</summary>
        public void PressAt(Vector2 screen)
        {
            Pressed?.Invoke(screen);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Pressed?.Invoke(eventData.position);
            }
        }

        void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
            }
        }

        void HideBagItems()
        {
            foreach (Image image in _bagItems)
            {
                image.gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.visible = true;
        }

        /// <summary>The held thing follows the mouse: its centre where the pointer is (from the first frame it is held, then whenever the mouse moves).</summary>
        void LateUpdate()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 now = mouse.position.ReadValue();
            if (_following && now == _lastDrawn)
            {
                return;
            }

            _following = true;
            _lastDrawn = now;
            Place(now);
        }
    }
}
