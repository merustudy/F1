using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    public enum MapNodeState
    {
        /// <summary>Can be chosen now.</summary>
        Available,
        /// <summary>The node the party stands on.</summary>
        Current,
        /// <summary>On a floor the party has already passed.</summary>
        Passed,
        /// <summary>Ahead, but not reachable from here yet.</summary>
        Ahead,
    }

    /// <summary>One node of the expedition map.</summary>
    public sealed class MapNodeView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Image _icon;
        [SerializeField] Sprite _battleIcon;
        [SerializeField] Sprite _bossIcon;
        [SerializeField] Sprite _eliteIcon;
        [SerializeField] Sprite _campIcon;
        [SerializeField] TMP_Text _label;

        public Button Button => _button;
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>The marker on show: the swords of a battle, the swords on a red diamond of an elite, the fire of a camp or the skull of the boss.</summary>
        public Sprite Icon => _icon.sprite;

        /// <summary>The name under the node.</summary>
        public string Label => _label.text;

        public void Show(string label, MapNodeKind kind, MapNodeState state, bool selected)
        {
            _label.text = label;
            _icon.sprite = IconOf(kind);
            _button.interactable = state == MapNodeState.Available;
            _label.color = state == MapNodeState.Passed ? UiPalette.TextDim : UiPalette.Text;

            if (selected)
            {
                _frame.color = UiPalette.Selected;
            }
            else
            {
                switch (state)
                {
                    case MapNodeState.Available: _frame.color = UiPalette.Button; break;
                    case MapNodeState.Current: _frame.color = UiPalette.Good; break;
                    case MapNodeState.Passed: _frame.color = UiPalette.Dead; break;
                    default: _frame.color = UiPalette.ButtonQuiet; break;
                }
            }
        }

        /// <summary>The marker of a kind of node.</summary>
        public Sprite IconOf(MapNodeKind kind)
        {
            switch (kind)
            {
                case MapNodeKind.Boss: return _bossIcon;
                case MapNodeKind.Elite: return _eliteIcon;
                case MapNodeKind.Camp: return _campIcon;
                default: return _battleIcon;
            }
        }
    }
}
