using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One square of a board's frame (Slice B stage 19): it takes the board's clicks, right clicks and the pointer passing over it, and
    /// draws what the square is (an empty bag square in the dark of the old outside squares, nothing under an item or outside every bag,
    /// the frame while a bag is held: a dashed line round the square, as round 48's mockups drew it). The pieces of the items lie
    /// over the squares and take no pointer, so a click on an item is a click on one of its squares. Tests call <see cref="Click"/>,
    /// <see cref="RightClickNow"/> and <see cref="Hover"/>.
    /// </summary>
    public sealed class GridSquareView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image _image;
        [SerializeField] Outline _line;
        [SerializeField] DashedFrame _dashes;

        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>What the square shows now.</summary>
        public GridSquareLook Look { get; private set; }

        /// <summary>Whether the square shows the frame's dashed line (while a bag is held, outside every bag).</summary>
        public bool ShowsDashes => _dashes != null && _dashes.enabled;

        public event Action<int, int> Clicked;
        public event Action<int, int> RightClicked;
        public event Action<int, int> Entered;
        public event Action<int, int> Exited;

        public void Show(GridSquareLook look)
        {
            Look = look;
            switch (look)
            {
                case GridSquareLook.Empty:
                    // Diablo's black square; the grey between squares is the bag's (round 49).
                    _image.color = UiPalette.GridSquare;
                    _line.enabled = false;
                    break;
                case GridSquareLook.Frame:
                    _image.color = UiPalette.GridFrameFill;
                    _line.enabled = false;
                    break;
                default:
                    // Under an item or outside every bag: nothing to see, but the square still takes the pointer.
                    _image.color = Color.clear;
                    _line.enabled = false;
                    break;
            }

            if (_dashes != null)
            {
                _dashes.enabled = look == GridSquareLook.Frame;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Click();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                RightClickNow();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Exited?.Invoke(X, Y);
        }

        /// <summary>As if the square had been clicked.</summary>
        public void Click()
        {
            Clicked?.Invoke(X, Y);
        }

        /// <summary>As if the square had been right-clicked.</summary>
        public void RightClickNow()
        {
            RightClicked?.Invoke(X, Y);
        }

        /// <summary>As if the pointer had come over the square.</summary>
        public void Hover()
        {
            Entered?.Invoke(X, Y);
        }
    }

    public enum GridSquareLook
    {
        /// <summary>Nothing shows: under an item, or outside every bag while no bag is held.</summary>
        Hidden,

        /// <summary>An empty square of a bag.</summary>
        Empty,

        /// <summary>A square of the frame outside every bag, shown while a bag is held: where a bag can go.</summary>
        Frame,
    }
}
