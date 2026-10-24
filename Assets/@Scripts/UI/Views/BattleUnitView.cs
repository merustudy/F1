using System.Collections.Generic;
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
    /// (<see cref="Rim"/>), under the bar a party unit's fatigue as pips (2026-10-06 round 36, C; an
    /// enemy has none) and a line of states: the burn as an icon with a number, or the death's door
    /// state, and after them the name of the unit's affliction or virtue while it is in one. While a potion can be used on the unit, a light lies on the floor at
    /// its feet. Both sides' marks look the same: where the unit stands says its side. Its row and
    /// name, and its items, are its board in the panel column under the stage
    /// (<see cref="BattleBoardView"/>). It shows what the engine says; texts are rebuilt only when
    /// their numbers change. The battle screen takes a dead unit off the stage.
    ///
    /// The presenter moves it for a moment when something happens: it lunges when its weapon
    /// fires, recoils and flashes when it is hit, and walks into its new column when it advances.
    /// A unit has an attack pose and a hit pose drawn after its figure (Docs/Design/10 §5; a monster's since round 28):
    /// the attack pose shows while a weapon's lunge plays, the hit pose while a blow's recoil plays,
    /// each 0.05 s more, then the figure again; the later of the two wins. A mercenary may hold a state pose through the
    /// moment of a breakdown (<see cref="HoldPose"/>, round 38): the lunge and the recoil leave it alone meanwhile, and it
    /// shivers (<see cref="Shiver"/>) for an affliction.
    /// A lunge and a recoil move the figure (and the shadow under it) only: the marks stay in the
    /// column so that they can be read while the figure acts (2026-10-04, round 19). A walk moves
    /// both: the unit has moved to another column. No motion moves the column itself.
    ///
    /// Round 104 (2026-10-24): stars turn over the head of a unit a stagger pushed back (<see cref="Stagger"/>), the shield and thorns of the
    /// battle's opening still to come are held back from its marks (<see cref="HoldBack"/>), and a right click on a chip of its state line or
    /// on its badge asks for that effect's card (<see cref="EffectRightClicked"/>).
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

        /// <summary>Round 104: how long the stagger's stars fade out at the end, in battle milliseconds, and how far over the head they turn.</summary>
        const int StarsFadeMs = 150;
        const float StarsOverHead = 16f;

        /// <summary>Round 104 (판정 3-2): what a right click on the marks opens a card of — the badge's shield or death's door, or a chip of the state line.</summary>
        public enum EffectChip
        {
            Shield,
            DeathsDoor,
            Burn,
            Bleed,
            Regen,
            Thorns,
            Freeze,
            Blind,
            Aim,
            Keen,
            Fury,
            Haste,
            Vulnerable,
        }

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
        [SerializeField] GameObject _bleedChip;
        [SerializeField] TMP_Text _bleed;
        [SerializeField] GameObject _regenChip;
        [SerializeField] TMP_Text _regen;
        [SerializeField] GameObject _thornsChip;
        [SerializeField] TMP_Text _thorns;
        [SerializeField] GameObject _freezeChip;
        [SerializeField] TMP_Text _freeze;
        [SerializeField] GameObject _blindChip;
        [SerializeField] TMP_Text _blind;
        [SerializeField] GameObject _aimChip;
        [SerializeField] TMP_Text _aim;
        [SerializeField] GameObject _keenChip;
        [SerializeField] TMP_Text _keen;
        [SerializeField] GameObject _furyChip;
        [SerializeField] TMP_Text _fury;
        [SerializeField] GameObject _hasteChip;
        [SerializeField] TMP_Text _haste;
        [SerializeField] GameObject _vulnerableChip;
        [SerializeField] TMP_Text _vulnerable;
        [SerializeField] GameObject _statusChip;
        [SerializeField] TMP_Text _status;
        [SerializeField] GameObject _pipsRow;
        [SerializeField] UiBar[] _fatiguePips;
        [SerializeField] TMP_Text _fatigueState;
        [SerializeField] StunStarsView _stars;

        BattleUnit _unit;
        int _shownHp = -1;
        int _shownShield = -1;
        int _shownBurn = -1;
        int _shownBleed = -1;
        int _shownRegen = -1;
        int _shownThorns = -1;
        int _shownFreeze = -1;
        int _shownBlind = -1;
        int _shownAim = -1;
        int _shownKeen = -1;
        int _shownFury = -1;
        int _shownHaste = -1;
        int _shownVulnerable = -1;
        long _shownStatus = -1;
        int _shownFatigue = -1;
        string _shownState;
        bool _shownOverwork;
        bool _stateShown;
        Rim _shownRim = Rim.None;
        int _heldShield;
        int _heldThorns;
        int _stunEndMs = -1;

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
        float _holdLeft = -1f;
        float _shiverLeft = -1f;
        float _shiverFor = 1f;
        float _shiverAmplitude;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>Round 104: a right click on the badge or a chip of the state line, with the effect it shows.</summary>
        public event System.Action<BattleUnitView, EffectChip> EffectRightClicked;

        /// <summary>The stagger's stars over the head (Round 104).</summary>
        public StunStarsView Stars => _stars;

        /// <summary>The badge's shield as the marks show it: the unit's, less what the opening still holds back.</summary>
        public int ShieldShown => System.Math.Max(0, _unit.Shield - _heldShield);

        /// <summary>The thorns as the marks show them, less what the opening still holds back.</summary>
        public int ThornsShown => System.Math.Max(0, _unit.Thorns - _heldThorns);

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

        /// <summary>True while the row of fatigue pips is shown: a party unit's. An enemy's is hidden.</summary>
        public bool PipsShown => _pipsRow.activeSelf;

        /// <summary>The fatigue pips under the HP bar, left to right: each a tenth of the most fatigue (round 36, C).</summary>
        public IReadOnlyList<UiBar> FatiguePips => _fatiguePips;

        /// <summary>The name of the affliction or virtue on the state line, or an empty string while the unit is in none.</summary>
        public string FatigueStateShown => _fatigueState.gameObject.activeSelf ? _fatigueState.text : string.Empty;

        /// <summary>True while a lunge, a recoil or a walk moves the unit.</summary>
        public bool Moving => _lungeAge >= 0f || _recoilAge >= 0f || _walkAge >= 0f;

        /// <summary>True while a state pose is held through the moment of a breakdown.</summary>
        public bool HoldsPose => _holdLeft > 0f;


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

            // Only a mercenary has fatigue (Docs/Design/04_Lobby_100Day_Economy.md §3).
            _pipsRow.SetActive(unit.Side == BattleSide.Party);

            // Round 104: the stars stand over the head, whatever the figure's height.
            _stars.Hide();
            var stars = (RectTransform)_stars.transform;
            stars.anchoredPosition = new Vector2(stars.anchoredPosition.x, _figureView.HeadHeight + StarsOverHead);
            _stunEndMs = -1;
            _heldShield = 0;
            _heldThorns = 0;
        }

        void Awake()
        {
            // Round 104 (판정 3-2·3-3): the badge and every chip of the state line open the card of what they show on a right click.
            Listen(_badge.gameObject, () => _unit.InDog ? EffectChip.DeathsDoor : EffectChip.Shield);
            Listen(_statusChip, () => EffectChip.DeathsDoor);
            Listen(_burnChip, () => EffectChip.Burn);
            Listen(_bleedChip, () => EffectChip.Bleed);
            Listen(_regenChip, () => EffectChip.Regen);
            Listen(_thornsChip, () => EffectChip.Thorns);
            Listen(_freezeChip, () => EffectChip.Freeze);
            Listen(_blindChip, () => EffectChip.Blind);
            Listen(_aimChip, () => EffectChip.Aim);
            Listen(_keenChip, () => EffectChip.Keen);
            Listen(_furyChip, () => EffectChip.Fury);
            Listen(_hasteChip, () => EffectChip.Haste);
            Listen(_vulnerableChip, () => EffectChip.Vulnerable);
        }

        void Listen(GameObject chip, System.Func<EffectChip> effect)
        {
            chip.GetComponent<RightClick>().Clicked += () => EffectRightClicked?.Invoke(this, effect());
        }

        /// <summary>The badge or the chip that shows an effect: where its card points.</summary>
        public RectTransform ChipRect(EffectChip chip)
        {
            return (RectTransform)ChipObject(chip).transform;
        }

        /// <summary>Whether the marks show an effect now: its card closes when it goes (판정 3-4).</summary>
        public bool ChipShown(EffectChip chip)
        {
            if (_unit == null || !_unit.Alive)
            {
                return false;
            }

            switch (chip)
            {
                case EffectChip.Shield:
                    return !_unit.InDog && ShieldShown > 0;
                case EffectChip.DeathsDoor:
                    return _unit.InDog;
                default:
                    return ChipObject(chip).activeInHierarchy;
            }
        }

        GameObject ChipObject(EffectChip chip)
        {
            switch (chip)
            {
                case EffectChip.Shield: return _badge.gameObject;
                case EffectChip.DeathsDoor: return _statusChip;
                case EffectChip.Burn: return _burnChip;
                case EffectChip.Bleed: return _bleedChip;
                case EffectChip.Regen: return _regenChip;
                case EffectChip.Thorns: return _thornsChip;
                case EffectChip.Freeze: return _freezeChip;
                case EffectChip.Blind: return _blindChip;
                case EffectChip.Aim: return _aimChip;
                case EffectChip.Keen: return _keenChip;
                case EffectChip.Fury: return _furyChip;
                case EffectChip.Haste: return _hasteChip;
                case EffectChip.Vulnerable: return _vulnerableChip;
                default: throw new System.ArgumentOutOfRangeException(nameof(chip), chip, null);
            }
        }

        /// <summary>Tests: as if the chip (or the badge) had been right-clicked.</summary>
        internal void RightClickChip(EffectChip chip)
        {
            ChipObject(chip).GetComponent<RightClick>().Simulate();
        }

        /// <summary>
        /// Round 104 (판정 1): a stagger pushed this unit's items back by <paramref name="ms"/> at <paramref name="atMs"/>: the stars turn until the
        /// battle reaches the end of the push. A stagger while they turn adds its time to their end, as the engine adds it to the items' (판정 1-2).
        /// </summary>
        public void Stagger(int atMs, int ms)
        {
            _stunEndMs = System.Math.Max(_stunEndMs, atMs) + ms;
        }

        /// <summary>The battle time the stagger's stars turn until, or -1 when none has come.</summary>
        public int StunEndMs => _stunEndMs;

        /// <summary>
        /// Round 104 (판정 2-6): how much of the unit's shield and thorns (hundredths) belongs to the battle's opening steps still to come. The
        /// marks show the rest; the presenter lowers it to 0 as the steps play.
        /// </summary>
        public void HoldBack(int shield, int thorns)
        {
            _heldShield = shield;
            _heldThorns = thorns;
        }

        /// <summary>Writes a state's stacks into its chip's text when they changed since the last render.</summary>
        static void RenderStacks(int stacks, ref int shown, TMP_Text text, string key)
        {
            if (stacks != shown)
            {
                shown = stacks;
                text.text = UiStrings.Get(key, stacks);
            }
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
            _shownHp = -1;
            _shownShield = -1;
            _shownBurn = -1;
            _shownBleed = -1;
            _shownRegen = -1;
            _shownThorns = -1;
            _shownFreeze = -1;
            _shownBlind = -1;
            _shownAim = -1;
            _shownKeen = -1;
            _shownFury = -1;
            _shownHaste = -1;
            _shownVulnerable = -1;
            _shownStatus = -1;
            _shownFatigue = -1;
            _stateShown = false;
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
            // A held state pose is not interrupted by the poses of a lunge or a recoil.
            if (pose == null || _holdLeft > 0f)
            {
                return;
            }

            _figureView.ShowPose(pose);
            _poseLeft = motion + PoseExtra;
        }

        /// <summary>
        /// Holds a pose in place of the figure for this long (the moment of a breakdown, round 38: the broken pose or the resolute pose),
        /// then shows the figure again. Nothing happens without a pose (a job whose state poses are not drawn yet keeps its figure).
        /// </summary>
        public void HoldPose(Sprite pose, float seconds)
        {
            if (pose == null)
            {
                return;
            }

            _figureView.ShowPose(pose);
            _holdLeft = Mathf.Max(0.01f, seconds);
            _poseLeft = -1f;
        }

        /// <summary>A shiver that dies down: the figure trembles sideways by up to the amplitude (the moment of an affliction, round 38).</summary>
        public void Shiver(float seconds, float amplitude)
        {
            _shiverFor = Mathf.Max(0.01f, seconds);
            _shiverLeft = _shiverFor;
            _shiverAmplitude = amplitude;
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

        /// <summary>The rim as this view shows it: a shield the opening still holds back does not light it.</summary>
        Rim RimShownFor(bool targetable)
        {
            Rim rim = RimFor(_unit, targetable);
            return rim == Rim.Shield && ShieldShown <= 0 ? Rim.None : rim;
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
                _hp.text = UiStrings.Get(UiKeys.Battle.Hp, UiText.Hp(_unit.Hp), UiText.Hp(_unit.MaxHp));
                _hpBar.Set(_unit.Hp, _unit.MaxHp);
            }

            int shield = ShieldShown;
            if (shield != _shownShield)
            {
                _shownShield = shield;
                _badgeNumber.text = UiStrings.Get(UiKeys.Battle.Shield, UiText.Amount(shield));
            }

            if (_unit.Burn != _shownBurn)
            {
                _shownBurn = _unit.Burn;
                _burn.text = UiStrings.Get(UiKeys.Battle.Burn, _unit.Burn);
            }

            // The effect vocabulary's states (Slice D stage 44, Round 90): stacks of bleed and regen, thorns as a whole number (hundredths, like a shield).
            if (_unit.Bleed != _shownBleed)
            {
                _shownBleed = _unit.Bleed;
                _bleed.text = UiStrings.Get(UiKeys.Battle.Bleed, _unit.Bleed);
            }

            if (_unit.Regen != _shownRegen)
            {
                _shownRegen = _unit.Regen;
                _regen.text = UiStrings.Get(UiKeys.Battle.Regen, _unit.Regen);
            }

            int thorns = ThornsShown;
            if (thorns != _shownThorns)
            {
                _shownThorns = thorns;
                _thorns.text = UiStrings.Get(UiKeys.Battle.Thorns, UiText.Amount(thorns));
            }

            // The seven states (stage 45, Round 91): each its stacks.
            RenderStacks(_unit.Freeze, ref _shownFreeze, _freeze, UiKeys.Battle.Freeze);
            RenderStacks(_unit.Blind, ref _shownBlind, _blind, UiKeys.Battle.Blind);
            RenderStacks(_unit.Aim, ref _shownAim, _aim, UiKeys.Battle.Aim);
            RenderStacks(_unit.Keen, ref _shownKeen, _keen, UiKeys.Battle.Keen);
            RenderStacks(_unit.Fury, ref _shownFury, _fury, UiKeys.Battle.Fury);
            RenderStacks(_unit.Haste, ref _shownHaste, _haste, UiKeys.Battle.Haste);
            RenderStacks(_unit.Vulnerable, ref _shownVulnerable, _vulnerable, UiKeys.Battle.Vulnerable);

            RenderStatus(balance, timeMs);
            RenderFatigue(balance);

            // The state line holds either the death's door state or what the unit carries, and after it the state of the breakdown.
            SetShown(_statusChip, _unit.InDog);
            SetShown(_burnChip, !_unit.InDog && _unit.Burn > 0);
            SetShown(_bleedChip, !_unit.InDog && _unit.Bleed > 0);
            SetShown(_regenChip, !_unit.InDog && _unit.Regen > 0);
            SetShown(_thornsChip, !_unit.InDog && thorns > 0);
            SetShown(_freezeChip, !_unit.InDog && _unit.Freeze > 0);
            SetShown(_blindChip, !_unit.InDog && _unit.Blind > 0);
            SetShown(_aimChip, !_unit.InDog && _unit.Aim > 0);
            SetShown(_keenChip, !_unit.InDog && _unit.Keen > 0);
            SetShown(_furyChip, !_unit.InDog && _unit.Fury > 0);
            SetShown(_hasteChip, !_unit.InDog && _unit.Haste > 0);
            SetShown(_vulnerableChip, !_unit.InDog && _unit.Vulnerable > 0);

            // The badge at the bar's end: the skull at death's door, the shield with its number while there is one.
            Sprite badge = _unit.InDog ? _dangerBadge : shield > 0 ? _shieldBadge : null;
            SetShown(_badge.gameObject, badge != null);
            if (badge != null && _badge.sprite != badge)
            {
                _badge.sprite = badge;
            }

            SetShown(_badgeNumber.gameObject, !_unit.InDog);

            _button.interactable = targetable && _unit.Alive;
            Rim rim = RimShownFor(targetable);
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

            // Round 104: the stagger's stars, fading over the last moment of the push.
            if (_unit.Alive && timeMs < _stunEndMs)
            {
                _stars.Show(Mathf.Min(1f, (float)(_stunEndMs - timeMs) / StarsFadeMs));
            }
            else
            {
                _stars.Hide();
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

            // The held pose and the shiver belong to the moment of a breakdown, which runs on real time (BattleScreen): they keep pace
            // with its dark and zoom while the battle and the motions run slow.
            if (_holdLeft > 0f)
            {
                _holdLeft -= Time.unscaledDeltaTime;
                if (_holdLeft <= 0f)
                {
                    _holdLeft = -1f;
                    _figureView.ShowFigure();
                }
            }

            if (_shiverLeft > 0f)
            {
                x += Random.Range(-1f, 1f) * _shiverAmplitude * (_shiverLeft / _shiverFor);
                _shiverLeft -= Time.unscaledDeltaTime;
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

        /// <summary>A party unit's fatigue as pips, and its affliction or virtue by name in its colour (round 36). An enemy has neither.</summary>
        void RenderFatigue(BalanceData balance)
        {
            if (_unit.Side != BattleSide.Party)
            {
                return;
            }

            if (_unit.Fatigue != _shownFatigue)
            {
                _shownFatigue = _unit.Fatigue;
                UI.FatiguePips.Show(_fatiguePips, _unit.Fatigue, balance.MaxFatigue, balance.FatigueBreakdown);
            }

            // Stage 48: "· 과로" follows the state's name while the unit is overworked (Docs/Architecture/12_UI.md "피로도와 붕괴의 상태").
            string stateId = _unit.State?.Id;
            bool overworked = FatigueRules.IsOverworked(balance, _unit.State, _unit.Fatigue);
            if (_stateShown && stateId == _shownState && overworked == _shownOverwork)
            {
                return;
            }

            _stateShown = true;
            _shownState = stateId;
            _shownOverwork = overworked;
            SetShown(_fatigueState.gameObject, _unit.State != null);
            if (_unit.State != null)
            {
                _fatigueState.text = UiText.FatigueStateLabel(_unit.State, overworked);
                _fatigueState.color = UiPalette.FatigueState(_unit.State.Kind);
            }
        }

        void RenderStatus(BalanceData balance, int timeMs)
        {
            // One number that changes exactly when the status text has to change. The chance is in it: a state can come or go at death's door.
            long status;
            int graceTenths = 0;
            int chance = 0;
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
                chance = FatigueRules.DogDeathChancePercent(balance, _unit.State, _unit.Setup.Stats, _unit.Setup.Traits);
                status = -2L - chance;
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
            else if (status < 0)
            {
                _status.text = UiStrings.Get(UiKeys.Battle.DogRolling, chance);
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
