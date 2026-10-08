using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace F1.UI
{
    /// <summary>
    /// A right click on a thing that shows an item (2026-10-08 round 47; Docs/Architecture/12_UI.md "툴팁"): the screen opens the
    /// item's card. It lives beside the thing's button, which keeps the left click; a disabled button does not stop it, since the
    /// facts of an item can always be read. Tests simulate the click.
    /// </summary>
    public sealed class RightClick : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Clicked?.Invoke();
            }
        }

        /// <summary>Tests: as if the thing had been right-clicked.</summary>
        internal void Simulate()
        {
            Clicked?.Invoke();
        }
    }
}
