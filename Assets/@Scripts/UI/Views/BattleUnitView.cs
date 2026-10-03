using System.Globalization;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One living unit on the stage of the battle: the figure (its art, or a placeholder when it
    /// has none) and the plate under its feet. The plate holds the row the unit stands in, its
    /// name, its HP, and a line of states: shield and burn as an icon with a number, or the
    /// death's door state. The plate's color says whose side the unit is on, that it is at death's
    /// door or that a potion can be used on it. The unit's items are not here: they are its board
    /// in the panel column under the stage (<see cref="BattleBoardView"/>). It shows what the
    /// engine says; texts are rebuilt only when their numbers change. The battle screen takes a
    /// dead unit off the stage.
    ///
    /// The presenter moves it for a moment when something happens: it lunges when its weapon
    /// fires, recoils and flashes when it is hit, and walks into its new column when it advances.
    /// The motion moves the figure and the plate together and never the column they are in.
    /// </summary>
    public sealed class BattleUnitView : MonoBehaviour
    {
        const float LungeOut = 0.1f;
        const float LungeBack = 0.2f;
        const float LungeDistance = 26f;
        const float RecoilDuration = 0.26f;
        const float RecoilDistance = 12f;
        const float WalkDuration = 0.35f;
        const float DangerPulse = 5f;

        [SerializeField] Button _button;
        [SerializeField] FigureView _figureView;
        [SerializeField] Image _plate;
        [SerializeField] Sprite _plateParty;
        [SerializeField] Sprite _plateEnemy;
        [SerializeField] Sprite _plateDanger;
        [SerializeField] Sprite _plateTarget;
        [SerializeField] TMP_Text _row;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] GameObject _shieldChip;
        [SerializeField] TMP_Text _shield;
        [SerializeField] GameObject _burnChip;
        [SerializeField] TMP_Text _burn;
        [SerializeField] GameObject _statusChip;
        [SerializeField] TMP_Text _status;

        BattleUnit _unit;
        int _shownRow = -1;
        int _shownHp = -1;
        int _shownShield = -1;
        int _shownBurn = -1;
        long _shownStatus = -1;

        Vector2 _plateBase;
        bool _plateBaseKnown;
        float _lungeAge = -1f;
        float _lungeDirection;
        float _recoilAge = -1f;
        float _recoilDirection;
        float _walkAge = -1f;
        float _walkFrom;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The unit's art, or its placeholder.</summary>
        public FigureView Figure => _figureView;

        /// <summary>The place of the figure: where numbers rise from.</summary>
        public RectTransform FigureRect => (RectTransform)_figureView.transform;

        /// <summary>The image of the art, for a ghost of a fallen unit.</summary>
        public Image FigureArt => _figureView.ArtImage;

        /// <summary>The plate on show: the one of the unit's side or state.</summary>
        public Sprite Plate => _plate.sprite;

        /// <summary>True while a lunge, a recoil or a walk moves the unit.</summary>
        public bool Moving => _lungeAge >= 0f || _recoilAge >= 0f || _walkAge >= 0f;

        /// <param name="figure">The unit's art, or null when it has none.</param>
        public void Bind(BattleUnit unit, Sprite figure)
        {
            _unit = unit;
            _name.text = UiText.Name(unit.Setup.Name);
            _figureView.Show(figure);
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
            _shownHp = -1;
            _shownShield = -1;
            _shownBurn = -1;
            _shownStatus = -1;
            _name.text = UiText.Name(_unit.Setup.Name);
        }

        /// <summary>A step towards the other side and back: the unit's weapon fired.</summary>
        /// <param name="direction">1 to the right, -1 to the left.</param>
        public void Lunge(float direction)
        {
            _lungeAge = 0f;
            _lungeDirection = direction;
        }

        /// <summary>A knock away from the blow that dies down.</summary>
        public void Recoil(float direction)
        {
            _recoilAge = 0f;
            _recoilDirection = direction;
        }

        /// <summary>The unit was put in a new column: it starts this far from it (where it was) and walks in.</summary>
        public void Walk(float fromDeltaX)
        {
            _walkAge = 0f;
            _walkFrom = fromDeltaX;
        }

        public void Flash(Color tint)
        {
            _figureView.Flash(tint);
        }

        /// <summary>The plate a unit shows in a state: at death's door, as the target of a potion, or just its side's.</summary>
        public static Sprite PlateFor(BattleUnit unit, bool targetable, Sprite party, Sprite enemy, Sprite danger, Sprite target)
        {
            if (unit.InDog)
            {
                return danger;
            }

            if (targetable)
            {
                return target;
            }

            return unit.Side == BattleSide.Party ? party : enemy;
        }

        /// <param name="targetable">True while a potion is waiting for this unit to be clicked.</param>
        public void Render(BattleEngine engine, bool targetable)
        {
            BalanceData balance = engine.Setup.Balance;
            int timeMs = engine.TimeMs;

            // The unit moves to another column when it advances; its badge follows.
            if (_unit.Row != _shownRow)
            {
                _shownRow = _unit.Row;
                _row.text = _unit.Row.ToString(CultureInfo.InvariantCulture);
            }

            if (_unit.Hp != _shownHp)
            {
                _shownHp = _unit.Hp;
                _hp.text = UiStrings.Get(UiKeys.Battle.Hp, _unit.Hp, _unit.MaxHp);
                _hpBar.Set(_unit.Hp, _unit.MaxHp);
            }

            if (_unit.Shield != _shownShield)
            {
                _shownShield = _unit.Shield;
                _shield.text = UiStrings.Get(UiKeys.Battle.Shield, _unit.Shield);
            }

            if (_unit.Burn != _shownBurn)
            {
                _shownBurn = _unit.Burn;
                _burn.text = UiStrings.Get(UiKeys.Battle.Burn, _unit.Burn);
            }

            RenderStatus(balance, timeMs);

            // The state line holds either the death's door state or what the unit carries.
            SetShown(_statusChip, _unit.InDog);
            SetShown(_shieldChip, !_unit.InDog && _unit.Shield > 0);
            SetShown(_burnChip, !_unit.InDog && _unit.Burn > 0);

            _button.interactable = targetable && _unit.Alive;
            Sprite plate = PlateFor(_unit, targetable, _plateParty, _plateEnemy, _plateDanger, _plateTarget);
            if (_plate.sprite != plate)
            {
                _plate.sprite = plate;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float x = 0f;

            if (_lungeAge >= 0f)
            {
                _lungeAge += dt;
                if (_lungeAge < LungeOut)
                {
                    x += _lungeDirection * LungeDistance * (_lungeAge / LungeOut);
                }
                else if (_lungeAge < LungeOut + LungeBack)
                {
                    x += _lungeDirection * LungeDistance * (1f - (_lungeAge - LungeOut) / LungeBack);
                }
                else
                {
                    _lungeAge = -1f;
                }
            }

            if (_recoilAge >= 0f)
            {
                _recoilAge += dt;
                float t = _recoilAge / RecoilDuration;
                if (t < 1f)
                {
                    x += _recoilDirection * RecoilDistance * (1f - t) * (1f - t);
                }
                else
                {
                    _recoilAge = -1f;
                }
            }

            if (_walkAge >= 0f)
            {
                _walkAge += dt;
                float t = _walkAge / WalkDuration;
                if (t < 1f)
                {
                    x += _walkFrom * (1f - t) * (1f - t);
                }
                else
                {
                    _walkAge = -1f;
                }
            }

            var offset = new Vector2(x, 0f);
            _figureView.SetMotion(offset);
            if (!_plateBaseKnown)
            {
                _plateBase = _plate.rectTransform.anchoredPosition;
                _plateBaseKnown = true;
            }

            _plate.rectTransform.anchoredPosition = _plateBase + offset;

            // At death's door the plate pulses.
            if (_unit != null && _unit.InDog)
            {
                float alpha = 0.8f + 0.2f * (0.5f + 0.5f * Mathf.Sin(Time.time * DangerPulse));
                _plate.color = new Color(1f, 1f, 1f, alpha);
            }
            else if (_plate.color.a < 1f)
            {
                _plate.color = Color.white;
            }
        }

        static void SetShown(GameObject chip, bool shown)
        {
            if (chip.activeSelf != shown)
            {
                chip.SetActive(shown);
            }
        }

        void RenderStatus(BalanceData balance, int timeMs)
        {
            // One number that changes exactly when the status text has to change.
            long status;
            int graceTenths = 0;
            if (!_unit.InDog)
            {
                status = 0;
            }
            else if (timeMs < _unit.GraceEndMs)
            {
                graceTenths = (_unit.GraceEndMs - timeMs + 99) / 100;
                status = 1000L + graceTenths * 100L + _unit.GraceHits;
            }
            else
            {
                status = 2;
            }

            if (status == _shownStatus)
            {
                return;
            }

            _shownStatus = status;
            if (status == 0)
            {
                _status.text = string.Empty;
            }
            else if (status == 2)
            {
                _status.text = UiStrings.Get(UiKeys.Battle.DogRolling, balance.DogDeathChancePercent);
            }
            else
            {
                _status.text = UiStrings.Get(
                    UiKeys.Battle.DogGrace,
                    UiText.Seconds(graceTenths * 100),
                    _unit.GraceHits,
                    balance.DogGraceBreakHits);
            }
        }
    }
}
