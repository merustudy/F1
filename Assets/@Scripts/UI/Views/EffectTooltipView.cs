using System.Globalization;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EffectChip = F1.UI.BattleUnitView.EffectChip;

namespace F1.UI
{
    /// <summary>
    /// Round 104 (2026-10-24, the user's "효과 아이콘에 마우스 오른쪽 클릭시 설명 툴팁", 판정 3 A): the card of an effect under a unit in battle. A right
    /// click on a chip of the state line or on the badge opens it above that chip — the trait card's look (Round 84): the dark panel in a brass
    /// rim, its notch pointing at the chip — with the effect's icon, its name and number in its colour, what kind it is, what it does now and its
    /// rule. It follows the numbers while it is open (a state wears off every tick) and closes at the next press anywhere, or when the chip goes.
    /// One card per screen; it takes no clicks. The numbers are the unit's and BalanceData's: the screen only multiplies what the data says.
    /// </summary>
    public sealed class EffectTooltipView : MonoBehaviour
    {
        public const float Padding = 20f;
        public const float MinWidth = 300f;
        public const float Gap = 10f;

        /// <summary>One card's words and colour.</summary>
        public struct Card
        {
            public string Title;
            public string Kind;
            public string Effect;
            public string Rule;
            public Color Color;
        }

        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _kind;
        [SerializeField] TMP_Text _effect;
        [SerializeField] TMP_Text _rule;
        [SerializeField] RectTransform _notch;
        [SerializeField] Sprite[] _icons;
        [SerializeField] float _titleLeft;

        BattleUnitView _owner;
        EffectChip _chip;
        BalanceData _balance;
        Card _shown;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>The frame the card was opened in: a press after it closes the card.</summary>
        public int ShownFrame { get; private set; } = -1;

        public BattleUnitView Owner => _owner;
        public EffectChip Chip => _chip;
        public string Title => _title.text;
        public string Kind => _kind.text;
        public string Effect => _effect.text;
        public string Rule => _rule.text;

        public void Show(BattleUnitView owner, EffectChip chip, BalanceData balance)
        {
            _owner = owner;
            _chip = chip;
            _balance = balance;
            ShownFrame = Time.frameCount;
            _icon.sprite = _icons[(int)chip];
            _shown = default;
            gameObject.SetActive(true);
            ((RectTransform)transform).SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            _owner = null;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (_owner == null || !_owner.ChipShown(_chip))
            {
                Hide();
                return;
            }

            PointerPress.Poll();
            if (PointerPress.PressedSince(ShownFrame))
            {
                Hide();
                return;
            }

            Refresh();
        }

        /// <summary>Writes the card again when its words changed, and puts it over the chip (the chip moves when its line changes).</summary>
        void Refresh()
        {
            Card card = CardOf(_chip, _owner.Unit, _owner.ShieldShown, _owner.ThornsShown, _balance);
            if (card.Title != _shown.Title || card.Effect != _shown.Effect || card.Kind != _shown.Kind || card.Rule != _shown.Rule)
            {
                _shown = card;
                _title.text = card.Title;
                _title.color = card.Color;
                _kind.text = card.Kind;
                _effect.text = card.Effect;
                _effect.color = card.Color;
                _rule.text = card.Rule;
            }

            float width = Mathf.Ceil(Mathf.Max(MinWidth, _titleLeft + TextWidth.Of(_title, _title.text) + Padding,
                2f * Padding + TextWidth.Of(_kind, _kind.text), 2f * Padding + TextWidth.Of(_effect, _effect.text),
                2f * Padding + TextWidth.Of(_rule, _rule.text)));
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
            _title.rectTransform.sizeDelta = new Vector2(width - _titleLeft - Padding, _title.rectTransform.sizeDelta.y);
            _kind.rectTransform.sizeDelta = new Vector2(width - 2f * Padding, _kind.rectTransform.sizeDelta.y);
            _effect.rectTransform.sizeDelta = new Vector2(width - 2f * Padding, _effect.rectTransform.sizeDelta.y);
            _rule.rectTransform.sizeDelta = new Vector2(width - 2f * Padding, _rule.rectTransform.sizeDelta.y);

            // Above the chip, centred on it, kept inside the screen; the notch points at the chip's middle wherever the card is.
            var corners = new Vector3[4];
            _owner.ChipRect(_chip).GetWorldCorners(corners);
            var parent = (RectTransform)rect.parent;
            Vector3 topMiddle = parent.InverseTransformPoint((corners[1] + corners[2]) / 2f);
            Rect bounds = parent.rect;
            float x = Mathf.Clamp(topMiddle.x, bounds.xMin + width / 2f, bounds.xMax - width / 2f);
            rect.localPosition = new Vector3(x, topMiddle.y + Gap, 0f);
            _notch.anchoredPosition = new Vector2(topMiddle.x - x, _notch.anchoredPosition.y);
        }

        /// <summary>
        /// What the card of an effect says for a unit (판정 3-7, the words of Round 104's README): its name and number, its kind, what it does now,
        /// its rule. <paramref name="shield"/> and <paramref name="thorns"/> are as the marks show them (the battle's opening may hold some back).
        /// </summary>
        public static Card CardOf(EffectChip chip, BattleUnit unit, int shield, int thorns, BalanceData balance)
        {
            string tick = Seconds(balance.BurnTickMs);
            string harmful = UiStrings.Get(UiKeys.EffectCard.KindHarmful);
            string helpful = UiStrings.Get(UiKeys.EffectCard.KindHelpful);
            string decay = UiStrings.Get(UiKeys.EffectCard.Decay, tick);
            switch (chip)
            {
                case EffectChip.Shield:
                    return Make(UiKeys.EffectCard.NameShield, UiText.Amount(shield), UiStrings.Get(UiKeys.EffectCard.KindDefence),
                        UiStrings.Get(UiKeys.EffectCard.ShieldEffect, UiText.Amount(shield)), UiStrings.Get(UiKeys.EffectCard.ShieldRule), UiPalette.Shield);
                case EffectChip.DeathsDoor:
                {
                    int chance = FatigueRules.DogDeathChancePercent(balance, unit.State, unit.Setup.Stats, unit.Setup.Traits);
                    return new Card
                    {
                        Title = UiStrings.Get(UiKeys.EffectCard.NameDeathsDoor), Kind = UiStrings.Get(UiKeys.EffectCard.KindDanger),
                        Effect = UiStrings.Get(UiKeys.EffectCard.DeathsDoorEffect, chance), Rule = UiStrings.Get(UiKeys.EffectCard.DeathsDoorRule),
                        Color = UiPalette.RimDanger,
                    };
                }

                case EffectChip.Burn:
                    return Make(UiKeys.EffectCard.NameBurn, unit.Burn, harmful,
                        UiStrings.Get(UiKeys.EffectCard.BurnEffect, tick, UiText.Amount(unit.Burn * balance.BurnDamagePerStack)),
                        UiStrings.Get(UiKeys.EffectCard.BurnRule, tick), UiPalette.Burn);
                case EffectChip.Bleed:
                    return Make(UiKeys.EffectCard.NameBleed, unit.Bleed, harmful,
                        UiStrings.Get(UiKeys.EffectCard.BleedEffect, tick, UiText.Amount(unit.Bleed * balance.BleedDamagePerStack)),
                        UiStrings.Get(UiKeys.EffectCard.BleedRule, tick), UiPalette.Bleed);
                case EffectChip.Regen:
                    return Make(UiKeys.EffectCard.NameRegen, unit.Regen, helpful,
                        UiStrings.Get(UiKeys.EffectCard.RegenEffect, tick, UiText.Amount(unit.Regen * balance.RegenHealPerStack)), decay, UiPalette.Regen);
                case EffectChip.Thorns:
                    return Make(UiKeys.EffectCard.NameThorns, UiText.Amount(thorns), helpful,
                        UiStrings.Get(UiKeys.EffectCard.ThornsEffect, UiText.Amount(thorns)), UiStrings.Get(UiKeys.EffectCard.ThornsRule), UiPalette.Thorns);
                case EffectChip.Freeze:
                    return State(EffectKind.Freeze, unit.Freeze, harmful,
                        UiStrings.Get(UiKeys.EffectCard.FreezeEffect, unit.Freeze * balance.FreezePercentPerStack),
                        UiStrings.Get(UiKeys.EffectCard.CooldownRule, balance.FreezePercentPerStack, tick), UiPalette.Freeze);
                case EffectChip.Blind:
                    return State(EffectKind.Blind, unit.Blind, harmful, UiStrings.Get(UiKeys.EffectCard.BlindEffect, unit.Blind * balance.BlindPercentPerStack),
                        UiStrings.Get(UiKeys.EffectCard.BlindRule, tick), UiPalette.Blind);
                case EffectChip.Aim:
                    return State(EffectKind.Aim, unit.Aim, helpful, UiStrings.Get(UiKeys.EffectCard.AimEffect, unit.Aim * balance.AimPercentPerStack), decay,
                        UiPalette.Aim);
                case EffectChip.Keen:
                    return State(EffectKind.Keen, unit.Keen, helpful, UiStrings.Get(UiKeys.EffectCard.KeenEffect, unit.Keen * balance.KeenPercentPerStack), decay,
                        UiPalette.Keen);
                case EffectChip.Fury:
                    return State(EffectKind.Fury, unit.Fury, helpful, UiStrings.Get(UiKeys.EffectCard.FuryEffect, unit.Fury * balance.FuryPercentPerStack),
                        UiStrings.Get(UiKeys.EffectCard.FuryRule, balance.FuryPercentPerStack, tick), UiPalette.Fury);
                case EffectChip.Haste:
                    return State(EffectKind.Haste, unit.Haste, helpful,
                        UiStrings.Get(UiKeys.EffectCard.HasteEffect, unit.Haste * balance.HastePercentPerStack),
                        UiStrings.Get(UiKeys.EffectCard.CooldownRule, balance.HastePercentPerStack, tick), UiPalette.Haste);
                case EffectChip.Vulnerable:
                    return State(EffectKind.Vulnerable, unit.Vulnerable, harmful,
                        UiStrings.Get(UiKeys.EffectCard.VulnerableEffect, unit.Vulnerable * balance.VulnerablePercentPerStack),
                        UiStrings.Get(UiKeys.EffectCard.VulnerableRule, balance.VulnerablePercentPerStack, tick), UiPalette.Vulnerable);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(chip), chip, null);
            }
        }

        static Card Make(string nameKey, int number, string kind, string effect, string rule, Color color)
        {
            return new Card
            {
                Title = UiStrings.Get(UiKeys.EffectCard.Title, UiStrings.Get(nameKey), number), Kind = kind, Effect = effect, Rule = rule, Color = color,
            };
        }

        static Card State(EffectKind state, int stacks, string kind, string effect, string rule, Color color)
        {
            return new Card { Title = UiStrings.Get(UiKeys.EffectCard.Title, UiText.StateName(state), stacks), Kind = kind, Effect = effect, Rule = rule, Color = color };
        }

        /// <summary>A tick's seconds: whole when they are ("1초마다"), to the tenth otherwise.</summary>
        static string Seconds(int milliseconds)
        {
            return milliseconds % 1000 == 0 ? (milliseconds / 1000).ToString(CultureInfo.InvariantCulture) : UiText.Seconds(milliseconds);
        }
    }
}
