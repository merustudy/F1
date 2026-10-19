using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace F1.UI
{
    /// <summary>
    /// The pointer coming onto and leaving a thing (2026-10-19 Round 85: a star item's stars while the pointer is on it in battle; Docs/Architecture/12_UI.md "★").
    /// It lives beside the thing's button and raycast graphic. Tests simulate the pointer.
    /// </summary>
    public sealed class PointerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public event Action Entered;
        public event Action Exited;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Entered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Exited?.Invoke();
        }

        /// <summary>Tests: as if the pointer had come onto the thing.</summary>
        internal void SimulateEnter()
        {
            Entered?.Invoke();
        }

        /// <summary>Tests: as if the pointer had left the thing.</summary>
        internal void SimulateExit()
        {
            Exited?.Invoke();
        }
    }
}
