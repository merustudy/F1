using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The battle. It moves battle time forward from frame time, draws what the engine says and
    /// passes potion and retreat clicks on. Speed and pause change only how fast the battle is
    /// shown; the engine decides every outcome.
    ///
    /// Each side has one column of the stage and, right under it, one column of the board panel
    /// per row, and a row holds one unit. A unit's figure and marks stand in the stage column of
    /// the row the engine says it is in, and its board (the head with its row and name, its item
    /// cells) in the panel column under it, so both move when the unit advances. The dead leave the stage and the panel. A fallen mercenary turns
    /// into a grave for a moment, and the party keeps the places it is drawn in until the grave has
    /// gone, then walks into the ones the engine has already given it: only the picture waits
    /// (2026-10-04, round 21). The event log is not
    /// shown while the battle runs; the result panel opens a viewer with the whole log. What
    /// happens is played as it happens (<see cref="BattlePresenter"/>): numbers rise, units lunge
    /// and recoil, cells flash, the storm candle between the two sides of the panel burns down
    /// while its light on the stage goes down with it and pulls in, and goes out with the storm,
    /// and the last few events read as captions in the header.
    /// </summary>
    public sealed class BattleScreen : UIScreen
    {
        /// <summary>Lines per text of the log viewer: a long log is split over several texts.</summary>
        const int LogLinesPerChunk = 40;

        /// <summary>The candle's flame gutters and its light pulls in over this long before the storm, until it is here.</summary>
        const int StormDuskMs = 10000;

        /// <summary>
        /// Seconds a fallen mercenary's grave stands at x1 and then takes to fade out (2026-10-04, round 21 mockup 2B). At a
        /// higher speed both are shorter by as much, so that the battle does not run far ahead of what is shown.
        /// </summary>
        internal const float GraveHold = 0.7f;
        internal const float GraveVanish = 0.15f;

        /// <summary>
        /// The kill moment (2026-10-05 round 30, Docs/Design/10 §5): when an enemy falls to a unit's item, for KillSlowFor
        /// seconds the battle and every motion run at KillSlowPercent (the clock is slowed, and the engine's time scale for the
        /// motions), the rest of the stage darkens under the one who struck and the one who fell, and the stage draws in
        /// towards them; then the fallen goes as any fallen enemy does, and the dark and the zoom go back. The times are real
        /// seconds at x1, shorter by as much at x2; at a speed above KillMomentTopSpeed there is none. The curves are the
        /// mockup's (ArtPipeline/Archive/30-kill-moment): the dark comes in over KillDarkIn and goes from KillDarkOutFrom over
        /// KillDarkOut, the zoom comes in over KillZoomIn and goes back from KillZoomOutFrom over KillZoomOut.
        /// </summary>
        internal const float KillSlowFor = 0.5f;
        internal const int KillSlowPercent = 25;
        internal const int KillMomentTopSpeed = 200;
        internal const float KillZoom = 1.1f;

        /// <summary>
        /// The dark's alpha. The approved 60% is the mockup's: the rest of the stage at 40% of its brightness on the screen. The
        /// UI blends in linear light, where black at 60% leaves a bright colour at about two thirds, so the same look takes
        /// 1 - 0.4^2.2 = 0.87.
        /// </summary>
        internal const float KillDark = 0.87f;
        internal const float KillLength = KillZoomOutFrom + KillZoomOut;
        const float KillDarkIn = 0.08f;
        const float KillDarkOutFrom = 0.452f;
        const float KillDarkOut = 0.12f;
        const float KillZoomIn = 0.12f;
        const float KillZoomOutFrom = 0.45f;
        const float KillZoomOut = 0.2f;

        /// <summary>
        /// The moment of a breakdown (2026-10-06 round 38, "B"; Docs/Design/04 §3): the kill moment's grammar for a mercenary whose fatigue
        /// reached the threshold (an affliction or a virtue) or its maximum (the collapse). For BreakdownSlowFor seconds the battle and the
        /// motions run at KillSlowPercent, the rest of the stage darkens (KillDark) under the unit, the stage draws in BreakdownZoom times
        /// towards it, and the dark and the zoom go back over BreakdownOut from then. Meanwhile the unit holds its state pose (the figure
        /// swapped as for an attack, where its job has the pose), shivers (an affliction) or swells (a virtue) and flashes in the state's
        /// colour, the glow and the effect burst stand behind it and the state's word over its head (BattleFxLayer). At a speed above
        /// KillMomentTopSpeed there is no slow, dark or zoom; the rest plays. The mockup: ArtPipeline/Archive/38-breakdown-fx.
        /// </summary>
        internal const float BreakdownSlowFor = 0.8f;
        internal const float BreakdownZoom = 1.2f;
        internal const float BreakdownOut = 0.25f;
        internal const float BreakdownLength = BreakdownSlowFor + BreakdownOut;
        const float ShiverFor = 0.5f;
        const float ShiverAmplitude = 4f;
        const float InkBurstSize = 416f;
        const float LightBurstSize = 352f;
        static readonly Color AfflictionTint = new Color(0.62f, 0.52f, 0.72f);
        static readonly Color VirtueTint = new Color(1f, 0.92f, 0.62f);

        static readonly int[] Speeds = { 100, 200, 400 };

        [SerializeField] Image _background;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _clockTime;
        [SerializeField] TMP_Text _clockLabel;
        [SerializeField] CandleView _candle;
        [SerializeField] TMP_Text[] _captions;
        [SerializeField] BattleFxLayer _fx;
        [SerializeField] Sprite _grave;
        [SerializeField] Button _pause;
        [SerializeField] Image _pauseFrame;
        [SerializeField] Button[] _speedButtons;
        [SerializeField] Image[] _speedFrames;
        [SerializeField] TMP_Text[] _speedLabels;
        [SerializeField] BattleUnitView _unitTemplate;
        [SerializeField] RectTransform _stageBack;
        [SerializeField] RectTransform _stageFront;
        [SerializeField] Image _killDark;
        [SerializeField] RectTransform _momentFx;
        [SerializeField] Sprite _inkBurst;
        [SerializeField] Sprite _lightBurst;
        [SerializeField] RectTransform _field;
        [SerializeField] RectTransform[] _partyRows;
        [SerializeField] RectTransform[] _enemyRows;
        [SerializeField] BattleBoardView _boardTemplate;
        [SerializeField] RectTransform[] _partyBoardColumns;
        [SerializeField] RectTransform[] _enemyBoardColumns;
        [SerializeField] PotionSlotView _potionTemplate;
        [SerializeField] Transform _potionParent;
        [SerializeField] TMP_Text _potionHint;
        [SerializeField] Button _retreat;
        [SerializeField] TMP_Text _retreatLabel;
        [SerializeField] GameObject _resultPanel;
        [SerializeField] TMP_Text _resultTitle;
        [SerializeField] TMP_Text _resultDetail;
        [SerializeField] Button _showLog;
        [SerializeField] Button _continue;
        [SerializeField] GameObject _logPanel;
        [SerializeField] ScrollRect _logScroll;
        [SerializeField] RectTransform _logContent;
        [SerializeField] TMP_Text _logChunkTemplate;
        [SerializeField] TMP_Text _logPartyFallen;
        [SerializeField] TMP_Text _logEnemyFallen;
        [SerializeField] Button _logClose;

        /// <summary>The speed the player last chose. Kept for the session so every battle starts at it.</summary>
        static int _preferredSpeedPercent = Speeds[0];

        /// <summary>The speed the player last chose, for the play log (AppRoot).</summary>
        internal static int PreferredSpeedPercent => _preferredSpeedPercent;
        readonly BattleClock _clock = new BattleClock();
        readonly List<BattleUnitView> _partyViews = new List<BattleUnitView>();
        readonly List<BattleUnitView> _enemyViews = new List<BattleUnitView>();
        readonly List<RectTransform> _columnOrder = new List<RectTransform>();
        int _firstColumn;
        readonly List<BattleBoardView> _partyBoards = new List<BattleBoardView>();
        readonly List<BattleBoardView> _enemyBoards = new List<BattleBoardView>();
        readonly List<PotionSlotView> _potionViews = new List<PotionSlotView>();
        readonly List<TMP_Text> _logChunks = new List<TMP_Text>();
        readonly List<string> _captionLines = new List<string>();
        BattleSession _battle;
        ExpeditionArt _art;
        Sprite _backgroundArt;
        BattlePresenter _presenter;
        int _armedPotion = -1;
        int _shownTimeTenths = -1;
        bool _resultShown;

        /// <summary>True once the clock has been drawn: a candle already out when the screen opens (a battle continued in the storm) makes no sound.</summary>
        bool _clockShown;
        StageMoment _moment;

        /// <summary>Where the kill moment's dark lies when nothing is drawn in: its place in the field, and its corner in the frame (the stage's box).</summary>
        Vector2 _killDarkPlace;
        Vector3 _killDarkHome;

        /// <summary>In the frame: the point the stage draws in about, and the stage's layers' own pivot at rest (both stretch over the frame).</summary>
        Vector2 _killCentre;
        Vector2 _stageRest;

        /// <summary>
        /// A moment on the stage: a kill moment, or the moment of a breakdown (round 38). Whom it lights (its fallen, whom it holds on the
        /// stage, and those who stand out of the dark: the strikers, or the one who broke down), how long it has run (real seconds), how
        /// fast, whether its slow time is over, and its own times and zoom.
        /// </summary>
        sealed class StageMoment
        {
            public readonly List<BattleUnitView> Fallen = new List<BattleUnitView>();
            public readonly List<BattleUnitView> Strikers = new List<BattleUnitView>();
            public float Age;

            /// <summary>The speed it was struck at, as a factor: its times are divided by it.</summary>
            public float Pace = 1f;

            /// <summary>True once the slow time is over (and a kill moment's fallen have gone): the dark and the zoom are going back.</summary>
            public bool Released;

            /// <summary>True for a kill moment: its fallen go, with their words and ghosts, when the slow time is over.</summary>
            public bool Kill;

            public float SlowFor;
            public float Zoom;
            public float DarkOutFrom;
            public float DarkOut;
            public float ZoomOutFrom;
            public float ZoomOut;
            public float Length;

            public static StageMoment KillMoment(float pace)
            {
                return new StageMoment
                {
                    Kill = true, Pace = pace, SlowFor = KillSlowFor, Zoom = KillZoom, DarkOutFrom = KillDarkOutFrom, DarkOut = KillDarkOut,
                    ZoomOutFrom = KillZoomOutFrom, ZoomOut = KillZoomOut, Length = KillLength,
                };
            }

            public static StageMoment Breakdown(float pace)
            {
                return new StageMoment
                {
                    Pace = pace, SlowFor = BreakdownSlowFor, Zoom = BreakdownZoom, DarkOutFrom = BreakdownSlowFor, DarkOut = BreakdownOut,
                    ZoomOutFrom = BreakdownSlowFor, ZoomOut = BreakdownOut, Length = BreakdownLength,
                };
            }
        }

        /// <summary>The clock that paces this battle. Tests speed it up.</summary>
        public BattleClock Clock => _clock;

        /// <summary>What the battle shows when something happens. For tests.</summary>
        public BattleFxLayer Fx => _fx;

        /// <summary>How many events of the log the presenter has passed.</summary>
        public int PlayedEvents => _presenter == null ? 0 : _presenter.Played;

        /// <summary>How far the storm has come, 0..1: the share of the storm candle that has burnt.</summary>
        public float StormProgress => _candle.Progress;

        /// <summary>The storm candle. For tests.</summary>
        public CandleView Candle => _candle;

        /// <summary>True while a kill moment is shown, until its dark and zoom are back. For tests.</summary>
        public bool KillMomentShown => _moment != null && _moment.Kill;

        /// <summary>True while the moment of a breakdown is shown (round 38). For tests.</summary>
        public bool BreakdownMomentShown => _moment != null && !_moment.Kill;

        /// <summary>True while any moment is shown.</summary>
        public bool MomentShown => _moment != null;

        /// <summary>True while a moment holds the battle slow (a kill moment holds its fallen meanwhile). For tests.</summary>
        public bool MomentSlows => _moment != null && !_moment.Released;

        /// <summary>How far the stage is drawn in: 1 when it is not.</summary>
        public float StageZoom => _stageFront.localScale.x;

        /// <summary>How dark a kill moment has made the rest of the stage, 0..1.</summary>
        public float KillDarkness => _killDark.enabled ? _killDark.color.a : 0f;

        /// <summary>The kill moment's dark, among the stage's columns. For tests.</summary>
        public Transform KillDarkLayer => _killDark.transform;

        /// <summary>The captions in the header, oldest first.</summary>
        public IReadOnlyList<string> Captions => _captionLines;

        /// <summary>The dungeon's background behind the battle, or null when the dungeon has none.</summary>
        public Sprite Background => _background.enabled ? _background.sprite : null;

        /// <summary>
        /// The art is loaded before the screen opens, so nothing waits for it afterwards: the
        /// figures of the units, the icons of the items and the background of the dungeon. A dungeon whose data names no
        /// background is fought on the plain background color.
        /// </summary>
        public override async Task PrepareAsync()
        {
            StaticData data = Managers.Data.Data;
            _art = await ExpeditionArt.LoadAsync(Managers.Resource, data);

            string background = data.Dungeons.Get(Managers.Expedition.Expedition.DungeonId).Background;
            _backgroundArt = background == null
                ? null
                : await Managers.Resource.LoadAsync<Sprite>(background, ResourceScope.Expedition);
        }

        protected override void OnOpen()
        {
            _battle = Managers.Expedition.Battle;
            _clock.SpeedPercent = _preferredSpeedPercent;
            BattleEngine engine = _battle.Engine;

            // A battle continued from a save is already under way: let the player look before it moves on.
            _clock.Paused = engine.TimeMs > 0;

            _background.sprite = _backgroundArt;
            _background.enabled = _backgroundArt != null;
            LayoutColumns(engine.Setup.Balance.PartySize);
            RenderTitle();

            // A unit has a view on the stage and a board in the panel; for an ally, both take the click a potion is aimed with.
            foreach (BattleUnit unit in engine.Party)
            {
                BattleUnitView view = CreateUnit(unit, _partyRows[unit.Row - 1]);
                BattleBoardView board = CreateBoard(unit, _partyBoardColumns[unit.Row - 1]);
                _partyViews.Add(view);
                _partyBoards.Add(board);
                int index = unit.Index;
                view.Button.onClick.AddListener(() => OnPartyUnitClicked(index));
                board.Button.onClick.AddListener(() => OnPartyUnitClicked(index));
            }

            foreach (BattleUnit unit in engine.Enemies)
            {
                _enemyViews.Add(CreateUnit(unit, _enemyRows[unit.Row - 1]));
                _enemyBoards.Add(CreateBoard(unit, _enemyBoardColumns[unit.Row - 1]));
            }

            for (int i = 0; i < engine.Potions.Count; i++)
            {
                PotionSlotView view = Instantiate(_potionTemplate, _potionParent);
                view.gameObject.SetActive(true);
                _potionViews.Add(view);
                int slot = i;
                view.Button.onClick.AddListener(() => OnPotionClicked(slot));
            }

            _pause.onClick.AddListener(OnPause);
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                int speed = Speeds[i];
                _speedButtons[i].onClick.AddListener(() => OnSpeed(speed));
            }

            _retreat.onClick.AddListener(OnRetreat);
            _showLog.onClick.AddListener(OnShowLog);
            _continue.onClick.AddListener(OnContinue);
            _logClose.onClick.AddListener(OnCloseLog);
            _resultPanel.SetActive(false);
            _logPanel.SetActive(false);

            // From the layout, not from positions (a fresh screen's are not laid out yet): the stage's front stretches over the
            // frame, the field hangs from its top-left corner and the dark from the field's.
            var frame = (RectTransform)_stageFront.parent;
            Rect frameRect = frame.rect;
            _killDarkPlace = _killDark.rectTransform.anchoredPosition;
            _killDarkHome = new Vector3(frameRect.xMin + _field.anchoredPosition.x + _killDarkPlace.x, frameRect.yMax + _field.anchoredPosition.y + _killDarkPlace.y, 0f);
            _stageRest = frameRect.center;
            _presenter = new BattlePresenter(engine, Managers.Data.Data, _fx, UnitViewOf, BoardViewOf, _candle.Rect, PushCaption, OnPartyFell,
                KillMomentsOn, OnKillingBlow, Managers.Sound.PlayEffect, OnBreakdown);
            RenderCaptions();
            Managers.Sound.PlayMusic(IsBossBattle() ? MusicTrack.Boss : MusicTrack.Dungeon);
        }

        /// <summary>True when the battle is the dungeon's boss node: the boss's music plays (Docs/Design/12 §2).</summary>
        static bool IsBossBattle()
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            return expedition.CurrentNodeId >= 0 && expedition.Map.Get(expedition.CurrentNodeId).Kind == MapNodeKind.Boss;
        }

        /// <summary>The dungeon and the floor the battle is fought on, in the header.</summary>
        void RenderTitle()
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            string dungeon = UiText.Name(Managers.Data.Data.Dungeons.Get(expedition.DungeonId).Name);
            int floor = expedition.CurrentNodeId < 0 ? 0 : expedition.Map.Get(expedition.CurrentNodeId).Floor;
            _title.text = UiStrings.Get(UiKeys.Battle.Title, dungeon, floor);
        }

        BattleUnitView UnitViewOf(UnitRef unit)
        {
            List<BattleUnitView> views = unit.Side == BattleSide.Party ? _partyViews : _enemyViews;
            return unit.Index >= 0 && unit.Index < views.Count ? views[unit.Index] : null;
        }

        BattleBoardView BoardViewOf(UnitRef unit)
        {
            List<BattleBoardView> boards = unit.Side == BattleSide.Party ? _partyBoards : _enemyBoards;
            return unit.Index >= 0 && unit.Index < boards.Count ? boards[unit.Index] : null;
        }

        /// <summary>A mercenary fell: it turns into a grave where it stood. The party keeps its places while the grave stands (Render).</summary>
        void OnPartyFell(UnitRef unit)
        {
            float speed = _clock.SpeedPercent / 100f;
            _fx.Grave(UnitViewOf(unit)?.FigureArt, _grave, GraveHold / speed, GraveVanish / speed);
        }

        /// <summary>A kill moment is played at the speeds where it does not hold the battle up for long: x1 and x2.</summary>
        bool KillMomentsOn()
        {
            return _clock.SpeedPercent <= KillMomentTopSpeed;
        }

        /// <summary>
        /// An enemy fell to a unit's item: a kill moment begins, or one already slowing takes this one in too (it lasts no
        /// longer). The stage draws in about the middle of the two; a moment begun while the last one draws back keeps its point.
        /// </summary>
        void OnKillingBlow(UnitRef striker, UnitRef fallen)
        {
            BattleUnitView fallenView = UnitViewOf(fallen);
            if (fallenView == null)
            {
                return;
            }

            BattleUnitView strikerView = UnitViewOf(striker);
            if (_moment == null || _moment.Released)
            {
                if (_moment == null)
                {
                    Vector3 centre = FigureCentre(fallenView);
                    if (strikerView != null)
                    {
                        centre = (centre + FigureCentre(strikerView)) * 0.5f;
                    }

                    _killCentre = _stageFront.parent.InverseTransformPoint(centre);
                }

                _moment = StageMoment.KillMoment(_clock.SpeedPercent / 100f);
            }

            if (!_moment.Fallen.Contains(fallenView))
            {
                _moment.Fallen.Add(fallenView);
            }

            if (strikerView != null && !_moment.Strikers.Contains(strikerView))
            {
                _moment.Strikers.Add(strikerView);
            }

            SlowFor(_moment);
        }

        static Vector3 FigureCentre(BattleUnitView view)
        {
            RectTransform figure = view.FigureRect;
            return figure.TransformPoint(figure.rect.center);
        }

        /// <summary>
        /// A mercenary broke down into a state, or (with a null state) collapsed at the most fatigue (round 38, "B"): the moment of a
        /// breakdown. At the speeds that play kill moments the battle slows, the stage darkens but the unit and draws in on it (a moment
        /// already on takes the unit in instead); at every speed the unit holds its state pose (where its job has one), shivers or swells
        /// and flashes in the state's colour, the glow and the burst stand behind it and the state's word over its head.
        /// </summary>
        void OnBreakdown(UnitRef unit, FatigueStateData state)
        {
            BattleUnitView view = UnitViewOf(unit);
            if (view == null)
            {
                return;
            }

            bool virtue = state != null && state.Kind == FatigueStateKind.Virtue;
            Color color = state == null ? UiPalette.FatigueDanger : UiPalette.FatigueState(state.Kind);
            float pace = _clock.SpeedPercent / 100f;
            if (KillMomentsOn())
            {
                if (_moment == null || _moment.Released)
                {
                    if (_moment == null)
                    {
                        _killCentre = _stageFront.parent.InverseTransformPoint(FigureCentre(view));
                    }

                    _moment = StageMoment.Breakdown(pace);
                }

                if (!_moment.Strikers.Contains(view))
                {
                    _moment.Strikers.Add(view);
                }

                SlowFor(_moment);
            }

            string mercenaryId = view.Unit.Setup.SourceId;
            view.HoldPose(virtue ? _art.ResolutePoseOfMercenary(mercenaryId) : _art.BrokenPoseOfMercenary(mercenaryId), BreakdownLength / pace);
            if (virtue)
            {
                view.Pulse();
            }
            else
            {
                view.Shiver(ShiverFor / pace, ShiverAmplitude);
            }

            view.Flash(virtue ? VirtueTint : AfflictionTint);
            _fx.Burst(view.FigureRect, virtue ? _lightBurst : _inkBurst, color, virtue ? LightBurstSize : InkBurstSize, virtue, BreakdownSlowFor / pace, BreakdownOut / pace);
            string word = state == null ? UiStrings.Get(UiKeys.Fx.Collapsed) : UiText.FatigueStateName(state);
            _fx.Word(view.FigureRect, word, UiText.Name(view.Unit.Setup.Name), color, pace);
        }

        /// <summary>The battle and the motions run slow while the moment holds its fallen, at the speed chosen otherwise.</summary>
        void SlowFor(StageMoment moment)
        {
            bool slow = moment != null && !moment.Released;
            _clock.SlowPercent = slow ? KillSlowPercent : 100;
            float scale = slow ? KillSlowPercent / 100f : 1f;
            if (!Mathf.Approximately(Time.timeScale, scale))
            {
                Time.timeScale = scale;
            }
        }

        /// <summary>The moment runs on real time, paused or not: when its slow time is over a kill moment's fallen go, as any fallen enemy goes.</summary>
        void AdvanceKillMoment(float realSeconds)
        {
            if (_moment == null)
            {
                return;
            }

            _moment.Age += realSeconds;
            if (!_moment.Released && _moment.Age >= _moment.SlowFor / _moment.Pace)
            {
                _moment.Released = true;
                foreach (BattleUnitView fallen in _moment.Fallen)
                {
                    _fx.Ghost(fallen.FigureArt);
                    _fx.Float(fallen.FigureRect, UiStrings.Get(UiKeys.Fx.Died), UiPalette.Danger, true);
                }
            }

            if (_moment.Age >= _moment.Length / _moment.Pace)
            {
                _moment = null;
            }

            SlowFor(_moment);
        }

        /// <summary>The dark over the stage and how far the stage is drawn in, by the moment's curves; the dark stays on the stage's box.</summary>
        void RenderKillMoment()
        {
            float dark = 0f;
            float zoom = 1f;
            if (_moment != null)
            {
                float t = _moment.Age * _moment.Pace;
                dark = KillDark * Mathf.Min(Smooth(t / KillDarkIn), 1f - Smooth((t - _moment.DarkOutFrom) / _moment.DarkOut));
                zoom = 1f + (_moment.Zoom - 1f) * Mathf.Min(Smooth(t / KillZoomIn), 1f - Smooth((t - _moment.ZoomOutFrom) / _moment.ZoomOut));
            }

            // Scaled about their own pivot and moved by as much as keeps the moment's point where it is: drawn in about that point.
            var scale = new Vector3(zoom, zoom, 1f);
            Vector2 offset = (1f - zoom) * (_killCentre - _stageRest);
            _stageBack.localScale = scale;
            _stageFront.localScale = scale;
            _stageBack.anchoredPosition = offset;
            _stageFront.anchoredPosition = offset;
            _killDark.enabled = dark > 0.001f;
            _killDark.color = new Color(0f, 0f, 0f, dark);
            RectTransform rect = _killDark.rectTransform;
            rect.localScale = new Vector3(1f / zoom, 1f / zoom, 1f);
            if (_moment != null)
            {
                rect.position = _stageFront.parent.TransformPoint(_killDarkHome);
            }
            else
            {
                rect.anchoredPosition = _killDarkPlace;
            }
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>The time scale is the engine's, not the screen's: whatever closes the screen, the motions run at their speed again.</summary>
        void OnDisable()
        {
            _moment = null;
            _clock.SlowPercent = 100;
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>A new caption in the header; the oldest goes when there are more than fit.</summary>
        void PushCaption(string line)
        {
            _captionLines.Add(line);
            while (_captionLines.Count > _captions.Length)
            {
                _captionLines.RemoveAt(0);
            }

            RenderCaptions();
        }

        /// <summary>The captions, newest at the bottom and brightest; the older ones fade.</summary>
        void RenderCaptions()
        {
            int first = _captions.Length - _captionLines.Count;
            for (int i = 0; i < _captions.Length; i++)
            {
                int line = i - first;
                _captions[i].text = line >= 0 ? _captionLines[line] : string.Empty;
                float age = _captions.Length - 1 - i;
                _captions[i].color = age == 0 ? UiPalette.Text : new Color(UiPalette.TextDim.r, UiPalette.TextDim.g, UiPalette.TextDim.b, 1f - 0.25f * age);
            }
        }

        public override void Refresh()
        {
            // Everything that holds text is rebuilt, so a locale change shows at once.
            for (int i = 0; i < _speedLabels.Length; i++)
            {
                _speedLabels[i].text = UiStrings.Get(UiKeys.Battle.Speed, Speeds[i] / 100);
            }

            foreach (BattleUnitView view in _partyViews)
            {
                view.Invalidate();
            }

            foreach (BattleUnitView view in _enemyViews)
            {
                view.Invalidate();
            }

            foreach (BattleBoardView board in _partyBoards)
            {
                board.Invalidate();
            }

            foreach (BattleBoardView board in _enemyBoards)
            {
                board.Invalidate();
            }

            _shownTimeTenths = -1;
            _resultShown = false;
            RenderTitle();
            _captionLines.Clear();
            RenderCaptions();
            if (_logPanel.activeSelf)
            {
                BuildLog();
            }

            Render();
        }

        void Update()
        {
            if (_battle == null)
            {
                return;
            }

            AdvanceKillMoment(Time.unscaledDeltaTime);
            if (!_battle.IsFinished)
            {
                int stepMs = _clock.Step(Time.unscaledDeltaTime);
                if (stepMs > 0)
                {
                    Managers.Expedition.AdvanceBattle(stepMs);
                }
            }

            Render();
        }

        /// <summary>
        /// Spreads the columns over the field (<see cref="FieldLayout"/>): the rows the party can
        /// stand in on the left (row 1 next to the middle), every row of the enemy on the right. The
        /// party is smaller than a full line, so its columns beyond the party size are not shown.
        /// The board panel's columns stand right under the stage's; the panel spans the frame, so
        /// they are offset by the field's left edge.
        /// </summary>
        void LayoutColumns(int partyRows)
        {
            partyRows = Mathf.Min(partyRows, _partyRows.Length);
            float width = FieldLayout.ColumnWidth(_field.rect.width, partyRows, _enemyRows.Length);
            float panelLeft = _field.anchoredPosition.x;

            for (int i = 0; i < _partyRows.Length; i++)
            {
                bool used = i < partyRows;
                _partyRows[i].gameObject.SetActive(used);
                _partyBoardColumns[i].gameObject.SetActive(used);
                if (used)
                {
                    float x = FieldLayout.PartyColumnX(width, partyRows, i);
                    PlaceColumn(_partyRows[i], x, width);
                    PlaceColumn(_partyBoardColumns[i], panelLeft + x, width);
                }
            }

            for (int i = 0; i < _enemyRows.Length; i++)
            {
                float x = FieldLayout.EnemyColumnX(width, partyRows, i);
                PlaceColumn(_enemyRows[i], x, width);
                PlaceColumn(_enemyBoardColumns[i], panelLeft + x, width);
            }
        }

        static void PlaceColumn(RectTransform column, float x, float width)
        {
            column.anchoredPosition = new Vector2(x, column.anchoredPosition.y);
            column.sizeDelta = new Vector2(width, column.sizeDelta.y);
        }

        /// <summary>
        /// Draws the stage's columns in the field's order (built from the rearmost row to row 1, the party's before the
        /// enemy's: a row is drawn over the rows behind it, and the enemy's row 1 over the party's), except that the column of
        /// a unit that is attacking is drawn over every other while it attacks, so that its weapon is not hidden behind the
        /// unit in front of it (Docs/Design/10 §5). The order comes back when the attack is over. During a kill moment its
        /// dark comes next, and over it the columns of its fallen and then of those who struck them.
        /// </summary>
        void ArrangeColumns()
        {
            if (_columnOrder.Count == 0)
            {
                _columnOrder.AddRange(_partyRows);
                _columnOrder.AddRange(_enemyRows);
                _columnOrder.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
                _firstColumn = _columnOrder[0].GetSiblingIndex();
            }

            int index = _firstColumn;
            for (int pass = 0; pass < 4; pass++)
            {
                foreach (RectTransform column in _columnOrder)
                {
                    int lit = KillLight(column);
                    bool inPass = pass == 0 ? lit == 0 && !AttackingIn(column)
                        : pass == 1 ? lit == 0 && AttackingIn(column)
                        : pass == 2 ? lit == 1
                        : lit == 2;
                    if (inPass)
                    {
                        PutAt(column, ref index);
                    }
                }

                if (pass == 1)
                {
                    PutAt(_killDark.transform, ref index);
                    PutAt(_momentFx, ref index);
                }
            }
        }

        static void PutAt(Transform child, ref int index)
        {
            if (child.GetSiblingIndex() != index)
            {
                child.SetSiblingIndex(index);
            }

            index++;
        }

        /// <summary>How a kill moment lights a column: 2 for one who struck, 1 for one who fell, 0 for the rest.</summary>
        int KillLight(Transform column)
        {
            if (_moment == null)
            {
                return 0;
            }

            foreach (BattleUnitView view in _moment.Strikers)
            {
                if (view.gameObject.activeSelf && view.transform.parent == column)
                {
                    return 2;
                }
            }

            foreach (BattleUnitView view in _moment.Fallen)
            {
                if (view.gameObject.activeSelf && view.transform.parent == column)
                {
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>True when a living unit in the column is attacking (a fallen one's view is hidden and stops its clock).</summary>
        bool AttackingIn(Transform column)
        {
            foreach (BattleUnitView view in _partyViews)
            {
                if (view.Attacking && view.gameObject.activeSelf && view.transform.parent == column)
                {
                    return true;
                }
            }

            foreach (BattleUnitView view in _enemyViews)
            {
                if (view.Attacking && view.gameObject.activeSelf && view.transform.parent == column)
                {
                    return true;
                }
            }

            return false;
        }

        BattleUnitView CreateUnit(BattleUnit unit, Transform parent)
        {
            BattleUnitView view = Instantiate(_unitTemplate, parent);
            view.gameObject.SetActive(true);

            // A party unit is a mercenary and is shown as its job; an enemy has its own figure, drawn at its own
            // scale (a boss stands larger than its place, its poses too). Both show their attack and hit poses.
            string id = unit.Setup.SourceId;
            if (unit.Side == BattleSide.Party)
            {
                view.Bind(unit, _art.OfMercenary(id), 1f, _art.AttackPoseOfMercenary(id), _art.HitPoseOfMercenary(id));
            }
            else
            {
                view.Bind(unit, _art.OfEnemy(id), _art.ScaleOfEnemy(id), _art.AttackPoseOfEnemy(id), _art.HitPoseOfEnemy(id));
            }
            return view;
        }

        /// <summary>The unit's board of the board panel: its cells, in the panel column of its row.</summary>
        BattleBoardView CreateBoard(BattleUnit unit, Transform column)
        {
            BattleBoardView board = Instantiate(_boardTemplate, column);
            board.gameObject.SetActive(true);
            board.Bind(unit, _art);
            return board;
        }

        void Render()
        {
            BattleEngine engine = _battle.Engine;
            BalanceData balance = engine.Setup.Balance;
            bool ongoing = !_battle.IsFinished;

            // What happened since the last frame is played before the dead leave the stage, so that a
            // fallen enemy's ghost and a fallen mercenary's grave start where it stood.
            _presenter.Play();

            int tenths = engine.TimeMs / 100;
            if (tenths != _shownTimeTenths)
            {
                _shownTimeTenths = tenths;
                RenderClock(engine, balance);
                RenderControls(engine, balance, ongoing);
            }

            bool danger = false;
            foreach (BattleUnit unit in engine.Party)
            {
                danger |= unit.Alive && unit.InDog;
            }

            _fx.SetDanger(ongoing && danger);

            // While a fallen mercenary's grave stands, the living of the party keep their places on the stage and in the
            // panel (with the rows on their boards' heads); they walk on when the last grave has gone. While a kill moment holds
            // its fallen, they stay on the stage and the enemy keeps its places the same way.
            bool graves = _fx.GravesLeft > 0f;
            bool killHold = MomentSlows;
            Place(_partyViews, _partyRows, view => view.Unit, graves, WalkIn);
            Place(_enemyViews, _enemyRows, view => view.Unit, killHold, WalkIn, view => killHold && _moment.Fallen.Contains(view));
            ArrangeColumns();
            Place(_partyBoards, _partyBoardColumns, board => board.Unit, graves, StandIn);
            Place(_enemyBoards, _enemyBoardColumns, board => board.Unit, killHold, StandIn);
            RenderKillMoment();
            bool targeting = ongoing && _armedPotion >= 0;
            foreach (BattleUnitView view in _partyViews)
            {
                view.Render(engine, targeting);
            }

            foreach (BattleUnitView view in _enemyViews)
            {
                view.Render(engine, false);
            }

            foreach (BattleBoardView board in _partyBoards)
            {
                board.Render(engine, targeting);
            }

            foreach (BattleBoardView board in _enemyBoards)
            {
                board.Render(engine, false);
            }

            _pauseFrame.color = _clock.Paused ? UiPalette.Selected : UiPalette.ButtonQuiet;
            for (int i = 0; i < _speedFrames.Length; i++)
            {
                _speedFrames[i].color = !_clock.Paused && _clock.SpeedPercent == Speeds[i] ? UiPalette.Selected : UiPalette.ButtonQuiet;
            }

            // The last enemy's kill moment is seen before the result covers the stage.
            if (!ongoing && !_resultShown && _moment == null)
            {
                ShowResult(engine);
            }
        }

        /// <summary>
        /// The battle time under the candle, and the storm: the candle burns down until the storm is
        /// here, when it goes out and the label says what the next tick takes. The flame gutters and
        /// its light on the stage pulls in over the last seconds before the storm.
        /// </summary>
        void RenderClock(BattleEngine engine, BalanceData balance)
        {
            _clockTime.text = UiStrings.Get(UiKeys.Battle.Time, UiText.Seconds(engine.TimeMs));
            bool storm = engine.TimeMs >= balance.StormStartMs;
            float closeness = storm ? 1f : Mathf.Clamp01((float)(engine.TimeMs - (balance.StormStartMs - StormDuskMs)) / StormDuskMs);
            bool wasLit = _candle.Lit;
            _candle.Show(storm ? 1f : Mathf.Clamp01((float)engine.TimeMs / balance.StormStartMs), closeness, storm);
            if (wasLit && !_candle.Lit && _clockShown)
            {
                Managers.Sound.PlayEffect(SoundEffect.CandleOut);
            }

            _clockShown = true;
            _clockLabel.text = storm
                ? UiStrings.Get(UiKeys.Battle.StormActive, engine.NextStormDamage)
                : UiStrings.Get(UiKeys.Battle.StormIn, UiText.Seconds(balance.StormStartMs - engine.TimeMs));
            _clockLabel.color = storm ? UiPalette.Burn : UiPalette.TextDim;
        }

        /// <summary>
        /// Puts each unit's view (on the stage, or in the panel) in the place of the row the unit
        /// stands in now and takes the dead away. Views are visited in unit order, so those that
        /// advance into a place together keep their order. A view that changes place is told how
        /// far it came from, so that it can walk in instead of appearing. While `hold` is true the
        /// living keep the places they are in; the dead go all the same, except those `kept` (a kill moment's fallen).
        /// </summary>
        static void Place<T>(List<T> views, RectTransform[] places, Func<T, BattleUnit> unitOf, bool hold, Action<T, float> moved, Func<T, bool> kept = null)
            where T : Component
        {
            foreach (T view in views)
            {
                BattleUnit unit = unitOf(view);
                bool shown = unit.Alive || (kept != null && kept(view));
                if (view.gameObject.activeSelf != shown)
                {
                    view.gameObject.SetActive(shown);
                }

                RectTransform place = places[unit.Row - 1];
                if (unit.Alive && !hold && view.transform.parent != place)
                {
                    var from = (RectTransform)view.transform.parent;
                    view.transform.SetParent(place, false);
                    moved?.Invoke(view, from.anchoredPosition.x - place.anchoredPosition.x);
                }
            }
        }

        /// <summary>A unit's view was put in the column of the unit's row: it walks in from where it stood.</summary>
        static void WalkIn(BattleUnitView view, float fromX)
        {
            view.Walk(fromX);
        }

        /// <summary>A unit's board was put in the column of the unit's row: its head says so.</summary>
        static void StandIn(BattleBoardView board, float fromX)
        {
            board.StandIn(board.Unit.Row);
        }

        /// <summary>Potions, their hint and the retreat button. They change with battle time (cooldowns) and with clicks.</summary>
        void RenderControls(BattleEngine engine, BalanceData balance, bool ongoing)
        {
            bool potionReady = engine.TimeMs >= engine.PotionReadyMs;
            for (int i = 0; i < _potionViews.Count; i++)
            {
                PotionData potion = engine.Potions[i];
                _potionViews[i].Show(potion, potion == null ? null : _art.OfPotion(potion.Id), i == _armedPotion, ongoing && potion != null && potionReady);
            }

            // The words of the chosen potion (its name, its effect, what to do next) show only while one is chosen:
            // they go away when it is used or put down (2026-10-04).
            bool armed = _armedPotion >= 0 && engine.Potions[_armedPotion] != null;
            if (armed)
            {
                PotionData chosen = engine.Potions[_armedPotion];
                _potionHint.text = UiStrings.Get(UiKeys.Battle.PotionArmed, UiText.Name(chosen.Name), UiText.PotionDetails(chosen));
            }

            _potionHint.transform.parent.gameObject.SetActive(armed);

            _retreat.interactable = engine.CanRetreat;
            _retreatLabel.text = ongoing && !engine.CanRetreat
                ? UiStrings.Get(UiKeys.Battle.RetreatWait, UiText.Seconds(engine.RetreatReadyMs - engine.TimeMs))
                : UiStrings.Get(UiKeys.Battle.Retreat, balance.RetreatChancePercent);
        }

        /// <summary>
        /// Fills the log viewer: who fell on each side, then every line of the log in the current
        /// locale. A long log is split over several texts so that no single text grows too large.
        /// </summary>
        void BuildLog()
        {
            BattleEngine engine = _battle.Engine;
            _logPartyFallen.text = FallenLine(engine.Party, UiKeys.Battle.PartyFallen);
            _logEnemyFallen.text = FallenLine(engine.Enemies, UiKeys.Battle.EnemyFallen);

            var lines = new List<string>();
            foreach (BattleEvent e in engine.Events)
            {
                string line = BattleLogText.Line(e, engine);
                if (line != null)
                {
                    lines.Add(line);
                }
            }

            int chunks = (lines.Count + LogLinesPerChunk - 1) / LogLinesPerChunk;
            while (_logChunks.Count < chunks)
            {
                _logChunks.Add(Instantiate(_logChunkTemplate, _logContent));
            }

            for (int i = 0; i < _logChunks.Count; i++)
            {
                bool used = i < chunks;
                _logChunks[i].gameObject.SetActive(used);
                if (used)
                {
                    int start = i * LogLinesPerChunk;
                    _logChunks[i].text = string.Join("\n", lines.GetRange(start, Mathf.Min(LogLinesPerChunk, lines.Count - start)));
                }
            }
        }

        /// <summary>The dead of one side by name, or nothing when nobody died.</summary>
        static string FallenLine(IReadOnlyList<BattleUnit> units, string key)
        {
            var names = new List<string>();
            foreach (BattleUnit unit in units)
            {
                if (!unit.Alive)
                {
                    names.Add(UiText.Name(unit.Setup.Name));
                }
            }

            return names.Count == 0 ? string.Empty : UiStrings.Get(key, string.Join(", ", names));
        }

        void ShowResult(BattleEngine engine)
        {
            _resultShown = true;
            _armedPotion = -1;
            _resultPanel.SetActive(true);

            switch (engine.Result)
            {
                case BattleResult.Victory:
                    _resultTitle.text = UiStrings.Get(UiKeys.Battle.Victory);
                    _resultTitle.color = UiPalette.Good;
                    Managers.Sound.PlayEffect(SoundEffect.Victory);
                    break;
                case BattleResult.Retreated:
                    _resultTitle.text = UiStrings.Get(UiKeys.Battle.Retreated);
                    _resultTitle.color = UiPalette.Text;
                    break;
                default:
                    _resultTitle.text = UiStrings.Get(UiKeys.Battle.Defeat);
                    _resultTitle.color = UiPalette.Danger;
                    Managers.Sound.PlayEffect(SoundEffect.Defeat);
                    break;
            }

            // Every death is explained from the log: when the mercenary fell and what the last hit and roll were.
            List<DeathCause> deaths = BattleLog.PartyDeaths(engine.Events);
            if (deaths.Count == 0)
            {
                _resultDetail.text = UiStrings.Get(UiKeys.Battle.NoDeaths);
            }
            else
            {
                var lines = new List<string>();
                foreach (DeathCause death in deaths)
                {
                    lines.Add(BattleLogText.Death(death, engine));
                }

                _resultDetail.text = string.Join("\n", lines);
            }

            RenderControls(engine, engine.Setup.Balance, false);
        }

        void OnPotionClicked(int slot)
        {
            _armedPotion = _armedPotion == slot ? -1 : slot;
            RenderControls(_battle.Engine, _battle.Engine.Setup.Balance, !_battle.IsFinished);
        }

        void OnPartyUnitClicked(int partyIndex)
        {
            if (_armedPotion < 0 || _battle.IsFinished)
            {
                return;
            }

            Managers.Expedition.TryUsePotion(_armedPotion, partyIndex);
            _armedPotion = -1;
            RenderControls(_battle.Engine, _battle.Engine.Setup.Balance, !_battle.IsFinished);
        }

        void OnRetreat()
        {
            if (_battle.IsFinished)
            {
                return;
            }

            Managers.Expedition.TryRetreat();
            RenderControls(_battle.Engine, _battle.Engine.Setup.Balance, !_battle.IsFinished);
        }

        void OnPause()
        {
            _clock.Paused = !_clock.Paused;
        }

        void OnSpeed(int speedPercent)
        {
            _preferredSpeedPercent = speedPercent;
            _clock.SpeedPercent = speedPercent;
            _clock.Paused = false;
        }

        void OnShowLog()
        {
            if (!_battle.IsFinished)
            {
                return;
            }

            BuildLog();
            _logPanel.SetActive(true);

            // Start at the top. The layout has to run once before the scroll position means anything.
            Canvas.ForceUpdateCanvases();
            _logScroll.verticalNormalizedPosition = 1f;
        }

        void OnCloseLog()
        {
            _logPanel.SetActive(false);
        }

        void OnContinue()
        {
            if (!_battle.IsFinished)
            {
                return;
            }

            Managers.Expedition.CloseBattle();
            GoToCurrentPhase();
        }
    }
}
