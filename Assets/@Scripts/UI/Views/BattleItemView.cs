using F1.Gameplay;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>One item of a unit in battle, with its cooldown drawn as a filling bar.</summary>
    public sealed class BattleItemView : MonoBehaviour
    {
        [SerializeField] UiBar _cooldown;
        [SerializeField] TMP_Text _name;

        BattleItemState _item;

        public void Bind(BattleItemState item)
        {
            _item = item;
            _name.text = UiText.Name(item.Equipped.Item.Name);
            _name.color = item.Active ? UiPalette.Text : UiPalette.TextDim;
        }

        public void Render(int timeMs, bool ownerAlive)
        {
            if (!_item.Active || !ownerAlive)
            {
                _cooldown.SetRatio(0f);
                return;
            }

            // The bar is empty right after the item fired and full at the moment it fires again.
            int remaining = _item.NextFireMs - timeMs;
            _cooldown.SetRatio(1f - (float)remaining / _item.CooldownMs);
        }
    }
}
