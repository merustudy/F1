using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A building of the town (Round 71, Darkest Dungeon's hamlet): the hotspot over the painting is a button, the plate under it names it
    /// always (with a second line when its state says so: the gate past the last day, the inn closed to resting), and while the pointer is
    /// over it a warm wash with a brass rim lights the building and the plate's line turns brass. The lobby listens for the pointer to show
    /// its bubble. Tests call <see cref="Hover"/> and <see cref="Leave"/>.
    /// </summary>
    public sealed class BuildingView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float PlateWidth = 280f;
        public const float PlateHeight = 50f;
        public const float PlateHeightWithSub = 78f;

        [SerializeField] Button _button;
        [SerializeField] GameObject _wash;
        [SerializeField] RectTransform _plate;
        [SerializeField] Outline _plateLine;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _sub;

        public Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;
        public RectTransform Plate => _plate;
        public bool IsLit => _wash.activeSelf;
        public string Name => _name.text;
        public string Sub => _sub.text;

        public event Action<BuildingView> Entered;
        public event Action<BuildingView> Exited;

        /// <summary>The plate's words: the name, a second line or null, and whether the name is dimmed (the building cannot be used now).</summary>
        public void ShowPlate(string name, string sub, bool dim)
        {
            _name.text = name;
            _name.color = dim ? UiPalette.TextDim : UiPalette.Text;
            _sub.text = sub ?? string.Empty;
            _sub.gameObject.SetActive(sub != null);
            _plate.sizeDelta = new Vector2(PlateWidth, sub != null ? PlateHeightWithSub : PlateHeight);
        }

        public void SetLit(bool lit)
        {
            _wash.SetActive(lit);
            _plateLine.effectColor = lit ? UiPalette.Brass : UiPalette.Line;
        }

        public void Hover()
        {
            SetLit(true);
            Entered?.Invoke(this);
        }

        public void Leave()
        {
            SetLit(false);
            Exited?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Leave();
        }
    }
}
