using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One living unit on the stage of the battle: the figure (its art, or a placeholder when it
    /// has none) and the marks under its feet (2026-10-05 round 29, Slay the Spire's way: no plate).
    /// The marks are its HP as a bar with the numbers on it, a badge at the bar's left end (a shield
    /// and its number, or the skull at death's door), a rim around the bar in the colour of its state
    /// (<see cref="Rim"/>) and a line of states under the bar: the burn as an icon with a number, or
    /// the death's door state. While a potion can be used on the unit, a light lies on the floor at
    /// its feet. Both sides' marks look the same: where the unit stands says its side. Its row and
    /// name, and its items, are its board in the panel column under the stage
    /// (<see cref="BattleBoardView"/>). It shows what the engine says; texts are rebuilt only when
    /// their numbers change. The battle screen takes a dead unit off the stage.
    ///
    /// The presenter moves it for a moment when something happens: it lunges when its weapon
    /// fires, recoils and flashes when it is hit, and walks into its new column when it advances.
    /// A mercenary has an attack pose and a hit pose drawn after its figure (Docs/Design/10 §5):
    /// the attack pose shows while a weapon's lunge plays, the hit pose while a blow's recoil plays,
    /// each 0.05 s more, then the figure again; the later of the two wins.
    /// A lunge and a recoil move the figure (and the shadow under it) only: the marks stay in the
    /// column so that they can be read while the figure acts (2026-10-04, round 19). A walk moves
    /// both: the unit has moved to another column. No motion moves the column itself.
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

        /// <summary>How long a pose stays after its lunge or recoil has ended.</summary>
        const float PoseExtra = 0.05f;

        /// <summary>What the rim around the HP bar says, the first that applies: death's door, a potion's target, a shield, or nothing.</summary>
        public enum Rim
        {
            None,
            Shield,
            Target,
            Danger,
        }

        [SerializeField] Button _button;
        [SerializeField] FigureView _figureView;
        [SerializeField] RectTransform _marks;
        [SerializeField] Image _rim;
        [SerializeField] Image _targetLight;
        [SerializeField] Image _badge;
        [SerializeField] TMP_Text _badgeNumber;
        [SerializeField] Sprite _shieldBadge;
        [SerializeField] Sprite _dangerBadge;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] GameObject _burnChip;
        [SerializeField] TMP_Text _burn;
        [SerializeField] GameObject _statusChip;
        [SerializeField] TMP_Text _status;

        BattleUnit _unit;
        int _shownHp = -1;
        int _shownShield = -1;
        int _shownBurn = -1;
        long _shownStatus = -1;
        Rim _shownRim = Rim.None;

        Vector2 _marksBase;
        bool _marksBaseKnown;
        float _lungeAge = -1f;
        float _lungeDirection;
        float _recoilAge = -1f;
        float _recoilDirection;
        float _walkAge = -1f;
        float _walkFrom;
        Sprite _attackPose;
        Sprite _hitPose;
        float _poseLeft = -1f;
        float _attackLeft = -1f;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The unit's art, or its placeholder.</summary>
        public FigureView Figure => _figureView;

        /// <summary>The place of the figure: where numbers rise from.</summary>
        public RectTransform FigureRect => (RectTransform)_figureView.transform;

        /// <summary>The marks under the figure: the unit's HP, its badge and rim, and its states.</summary>
        public RectTransform MarksRect => _marks;

        /// <summary>The image of the art, for the ghost of a fallen enemy or the grave of a fallen mercenary.</summary>
        public Image FigureArt => _figureView.ArtImage;

        /// <summary>What the rim around the HP bar says now.</summary>
        public Rim RimShown => _shownRim;

        /// <summary>The badge at the bar's left end on show (the shield's, the skull of death's door), or null when there is none.</summary>
        public Sprite BadgeShown => _badge.gameObject.activeSelf ? _badge.sprite : null;

        /// <summary>True while the light of a potion's target lies at the unit's feet.</summary>
        public bool TargetLit => _targetLight.enabled;

        /// <summary>True while a lunge, a recoil or a walk moves the unit.</summary>
        public bool Moving => _lungeAge >= 0f || _recoilAge >= 0f || _walkAge >= 0f;

        /// <summary>True when the unit has a hit pose: its red flash is at half strength (Docs/Design/10 §5).</summary>
        public bool HasHitPose => _hitPose != null;

        /// <summary>
        /// True while the unit attacks: its lunge and a moment more (as long as an attack pose stays). The screen then draws
        /// its column over everyone, so that its weapon is not hidden behind the unit in front of it (Docs/Design/10 §5).
        /// </summary>
        public bool Attacking => _attackLeft > 0f;

        /// <param name="figure">The unit's art, or null when it has none.</param>
        /// <param name="figureScale">How many times the common size the art is drawn (a boss is larger).</param>
        /// <param name="attackPose">The unit's attack pose, or null when it has none.</param>
        /// <param name="hitPose">The unit's hit pose, or null.</param>
        public void Bind(BattleUnit unit, Sprite figure, float figureScale = 1f, Sprite attackPose = null, Sprite hitPose = null)
        {
            _unit = unit;
            _figureView.Show(figure);
            _figureView.SetScale(figureScale);
            _attackPose = attackPose;
            _hitPose = hitPose;
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
            _shownHp = -1;
            _shownShield = -1;
            _shownBurn = -1;
            _shownStatus = -1;
        }

        /// <summary>A step towards the other side and back: an item that strikes or guards fired.</summary>
        /// <param name="direction">1 to the right, -1 to the left.</param>
        /// <param name="withPose">True for a weapon: the attack pose shows while the lunge plays.</param>
        public void Lunge(float direction, bool withPose = false)
        {
            _lungeAge = 0f;
            _lungeDirection = direction;
            _attackLeft = LungeOut + LungeBack + PoseExtra;
            if (withPose)
            {
                ShowPose(_attackPose, LungeOut + LungeBack);
            }
        }

        /// <summary>A knock away from the blow that dies down; the hit pose shows meanwhile.</summary>
        public void Recoil(float direction)
        {
            _recoilAge = 0f;
            _recoilDirection = direction;
            ShowPose(_hitPose, RecoilDuration);
        }

        /// <summary>A support item fired: the figure swells and a warm light behind it fades.</summary>
        public void Pulse()
        {
            _figureView.Pulse();
        }

        void ShowPose(Sprite pose, float motion)
        {
            if (pose == null)
            {
                return;
            }

            _figureView.ShowPose(pose);
            _poseLeft = motion + PoseExtra;
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

        /// <summary>What the rim of a unit says in a state: at death's door, as the target of a potion, with a shield, or nothing.</summary>
        public static Rim RimFor(BattleUnit unit, bool targetable)
        {
            if (unit.InDog)
            {
                return Rim.Danger;
            }

            if (targetable)
            {
                return Rim.Target;
            }

            return unit.Shield > 0 ? Rim.Shield : Rim.None;
        }

        static Color ColorOf(Rim rim)
        {
            switch (rim)
            {
                case Rim.Shield:
                    return UiPalette.RimShield;
                case Rim.Target:
                    return UiPalette.RimTarget;
                default:
                    return UiPalette.RimDanger;
            }
        }

        /// <param name="targetable">True while a potion is waiting for this unit to be clicked.</param>
        public void Render(BattleEngine engine, bool targetable)
        {
            BalanceData balance = engine.Setup.Balance;
            int timeMs = engine.TimeMs;

            if (_unit.Hp != _shownHp)
            {
                _shownHp = _unit.Hp;
                _hp.text = UiStrings.Get(UiKeys.Battle.Hp, _unit.Hp, _unit.MaxHp);
                _hpBar.Set(_unit.Hp, _unit.MaxHp);
            }

            if (_unit.Shield != _shownShield)
            {
                _shownShield = _unit.Shield;
                _badgeNumber.text = UiStrings.Get(UiKeys.Battle.Shield, _unit.Shield);
            }

            if (_unit.Burn != _shownBurn)
            {
                _shownBurn = _unit.Burn;
                _burn.text = UiStrings.Get(UiKeys.Battle.Burn, _unit.Burn);
            }

            RenderStatus(balance, timeMs);

            // The state line holds either the death's door state or what the unit carries.
            SetShown(_statusChip, _unit.InDog);
            SetShown(_burnChip, !_unit.InDog && _unit.Burn > 0);

            // The badge at the bar's end: the skull at death's door, the shield with its number while there is one.
            Sprite badge = _unit.InDog ? _dangerBadge : _unit.Shield > 0 ? _shieldBadge : null;
            SetShown(_badge.gameObject, badge != null);
            if (badge != null && _badge.sprite != badge)
            {
                _badge.sprite = badge;
            }

            SetShown(_badgeNumber.gameObject, !_unit.InDog);

            _button.interactable = targetable && _unit.Alive;
            Rim rim = RimFor(_unit, targetable);
            if (rim != _shownRim)
            {
                _shownRim = rim;
                _rim.enabled = rim != Rim.None;
                _rim.color = ColorOf(rim);
            }

            bool lit = targetable && _unit.Alive;
            if (_targetLight.enabled != lit)
            {
                _targetLight.enabled = lit;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float x = 0f;
            float walk = 0f;

            if (_attackLeft > 0f)
            {
                _attackLeft -= dt;
            }

            if (_poseLeft >= 0f)
            {
                _poseLeft -= dt;
                if (_poseLeft < 0f)
                {
                    _figureView.ShowFigure();
                }
            }

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
                    walk = _walkFrom * (1f - t) * (1f - t);
                }
                else
                {
                    _walkAge = -1f;
                }
            }

            // The figure takes every motion; the marks only the walk into a new column.
            _figureView.SetMotion(new Vector2(x + walk, 0f));
            if (!_marksBaseKnown)
            {
                _marksBase = _marks.anchoredPosition;
                _marksBaseKnown = true;
            }

            _marks.anchoredPosition = _marksBase + new Vector2(walk, 0f);

            // At death's door the rim pulses.
            if (_shownRim == Rim.Danger)
            {
                Color danger = UiPalette.RimDanger;
                danger.a = 0.8f + 0.2f * (0.5f + 0.5f * Mathf.Sin(Time.time * DangerPulse));
                _rim.color = danger;
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
