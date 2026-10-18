using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A building of the town (Round 71, Darkest Dungeon's hamlet; Round 78, stage 30: a picture of its own at its level, Clash of Clans'
    /// growing buildings): the picture stands on the far town background at a place and a width the screen gives it (the level's size),
    /// the hotspot over it is a button, the plate under it names it with its level, and while the pointer is over it a warm wash with a
    /// brass rim lights the building and the plate's line turns brass. Under construction the scaffold stands over the picture and a badge
    /// under the plate says the days left over a bar. The lobby listens for the pointer to show its bubble. Tests call <see cref="Hover"/>
    /// and <see cref="Leave"/>.
    /// </summary>
    public sealed class BuildingView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float PlateWidth = 280f;
        public const float PlateHeight = 50f;
        public const float PlateHeightWithSub = 78f;
        public const float BadgeWidth = 300f;
        public const float BadgeHeight = 62f;

        /// <summary>How far under the picture's foot the plate's centre stands, and the badge under the plate.</summary>
        const float PlateBelow = 34f;
        const float BadgeGap = 8f;

        [SerializeField] Button _button;
        [SerializeField] Image _picture;
        [SerializeField] Image _scaffold;
        [SerializeField] GameObject _wash;
        [SerializeField] RectTransform _plate;
        [SerializeField] Outline _plateLine;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _sub;
        [SerializeField] RectTransform _badge;
        [SerializeField] TMP_Text _badgeWords;
        [SerializeField] UiBar _badgeBar;
        [SerializeField] RectTransform _count;
        [SerializeField] TMP_Text _countWords;

        public Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;
        public RectTransform Plate => _plate;
        public bool IsLit => _wash.activeSelf;
        public string Name => _name.text;
        public string Sub => _sub.text;
        public Sprite Picture => _picture.sprite;
        public bool ScaffoldShown => _scaffold.gameObject.activeSelf;
        public bool BadgeShown => _badge.gameObject.activeSelf;
        public string BadgeWords => _badgeWords.text;

        /// <summary>The small gold count at the plate's corner (Slice D stage 35: the barracks' mercenaries who can level up).</summary>
        public bool CountShown => _count.gameObject.activeSelf;
        public string CountWords => _countWords.text;

        public event Action<BuildingView> Entered;
        public event Action<BuildingView> Exited;

        /// <summary>
        /// Stands the building's picture on the town: its foot at (footX, footY) of the frame (UI coordinates, y down), at the given width,
        /// as tall as the picture's shape makes it. The hotspot is the picture; the plate is centred under the foot. With no picture (the gate,
        /// 2026-10-18: the arch painted on the town is the gate) the box is a transparent hotspot of the given height, lit by its plate only.
        /// </summary>
        public void Place(Sprite picture, float footX, float footY, float width, float height = 0f)
        {
            _picture.sprite = picture;
            _picture.color = picture == null ? Color.clear : Color.white;
            if (picture != null && picture.rect.width > 0f)
            {
                height = width * picture.rect.height / picture.rect.width;
            }
            else if (height <= 0f)
            {
                height = width * 0.66f;
            }

            Rect.anchoredPosition = new Vector2(footX - width / 2f, -(footY - height));
            Rect.sizeDelta = new Vector2(width, height);
            // The scaffold is laid over the picture's box: a little wider than the walls and as tall as the roof.
            _scaffold.rectTransform.anchoredPosition = new Vector2(-width * 0.05f, height * 0.04f);
            _scaffold.rectTransform.sizeDelta = new Vector2(width * 1.1f, height * 1.08f);
            _plate.anchoredPosition = new Vector2(width / 2f - PlateWidth / 2f, -(height + PlateBelow - _plate.sizeDelta.y / 2f));
            _badge.anchoredPosition = new Vector2(width / 2f - BadgeWidth / 2f, -(height + PlateBelow + _plate.sizeDelta.y / 2f + BadgeGap));
        }

        /// <summary>The scaffold's picture, shared by every building (ArtAddress.Scaffold); the lobby gives it once it is loaded.</summary>
        public void SetScaffold(Sprite scaffold)
        {
            _scaffold.sprite = scaffold;
        }

        /// <summary>The plate's words: the name, a second line or null, and whether the name is dimmed (the building cannot be used now).</summary>
        public void ShowPlate(string name, string sub, bool dim)
        {
            _name.text = name;
            _name.color = dim ? UiPalette.TextDim : UiPalette.Text;
            _sub.text = sub ?? string.Empty;
            _sub.gameObject.SetActive(sub != null);
            _plate.sizeDelta = new Vector2(PlateWidth, sub != null ? PlateHeightWithSub : PlateHeight);
        }

        /// <summary>The count at the plate's corner (Slice D stage 35), or null for none.</summary>
        public void ShowCount(string words)
        {
            _count.gameObject.SetActive(words != null);
            _countWords.text = words ?? string.Empty;
        }

        /// <summary>Under construction: the scaffold over the picture and the badge with the words and how far along the work is (0..1).</summary>
        public void ShowConstruction(string words, float progress)
        {
            _scaffold.gameObject.SetActive(true);
            _badge.gameObject.SetActive(true);
            _badgeWords.text = words;
            _badgeBar.SetRatio(Mathf.Clamp01(progress));
        }

        public void HideConstruction()
        {
            _scaffold.gameObject.SetActive(false);
            _badge.gameObject.SetActive(false);
        }

        public void SetLit(bool lit)
        {
            _wash.SetActive(lit && _picture.sprite != null);
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
