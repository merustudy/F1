using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A dungeon's marker on the expedition map (Round 71 E2): a disc where the painting put the place, ringed when it is the chosen
    /// one, dimmed while it is locked, with a plate under it naming the dungeon and its state. The screen clones the template per dungeon.
    /// </summary>
    public sealed class DungeonMarkerView : MonoBehaviour
    {
        /// <summary>The marker's rect: the disc's centre is <see cref="DiscCenter"/> from its top-left corner.</summary>
        public const float Width = 320f;
        public const float Height = 140f;
        public static readonly Vector2 DiscCenter = new Vector2(160f, 26f);

        [SerializeField] Button _button;
        [SerializeField] Image _selection;
        [SerializeField] Image _ring;
        [SerializeField] Image _disc;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _detail;

        public string DungeonId { get; set; }
        public Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;
        public string Name => _name.text;
        public bool Locked { get; private set; }
        public bool Selected => _selection.gameObject.activeSelf;

        /// <summary>Puts the disc's centre at a point of the map, measured from the map's top-left corner.</summary>
        public void PlaceAt(Vector2 point)
        {
            Rect.anchoredPosition = new Vector2(point.x - DiscCenter.x, -(point.y - DiscCenter.y));
        }

        public void Show(string name, string detail, bool locked, bool selected)
        {
            Locked = locked;
            _name.text = name;
            _detail.text = detail;
            _name.color = locked ? UiPalette.TextDim : UiPalette.Text;
            _disc.color = locked ? UiPalette.Slot : UiPalette.PanelLight;
            _ring.color = locked ? UiPalette.Line : UiPalette.Brass;
            _selection.gameObject.SetActive(selected);
            _button.interactable = !locked;
        }
    }
}
