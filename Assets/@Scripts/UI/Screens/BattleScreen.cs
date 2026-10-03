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
    /// Each side has one column of the stage and one line of the board panel per row, and a row
    /// holds one unit. A unit's figure and plate stand in the column of the row the engine says it
    /// is in, and its face and item cells lie in the panel's line of that row, so both move when
    /// the unit advances. The dead leave the stage and the panel. The event log is not shown
    /// while the battle runs; the result panel opens a viewer with the whole log. What happens
    /// is played as it happens (<see cref="BattlePresenter"/>): numbers rise, units lunge and
    /// recoil, cells flash, the storm's clock in the middle of the panel fills and darkens the
    /// stage, and the last few events read as captions under the clock.
    /// </summary>
    public sealed class BattleScreen : UIScreen
    {
        /// <summary>Lines per text of the log viewer: a long log is split over several texts.</summary>
        const int LogLinesPerChunk = 40;

        /// <summary>The stage darkens over this long before the storm, until it is here.</summary>
        const int StormDuskMs = 10000;

        static readonly int[] Speeds = { 100, 200, 400 };

        [SerializeField] Image _background;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _clockTime;
        [SerializeField] TMP_Text _clockLabel;
        [SerializeField] Image _clockRing;
        [SerializeField] TMP_Text[] _captions;
        [SerializeField] BattleFxLayer _fx;
        [SerializeField] Button _pause;
        [SerializeField] Image _pauseFrame;
        [SerializeField] Button[] _speedButtons;
        [SerializeField] Image[] _speedFrames;
        [SerializeField] TMP_Text[] _speedLabels;
        [SerializeField] BattleUnitView _unitTemplate;
        [SerializeField] RectTransform _field;
        [SerializeField] RectTransform[] _partyRows;
        [SerializeField] RectTransform[] _enemyRows;
        [SerializeField] BattleBoardView _boardTemplate;
        [SerializeField] RectTransform[] _partyLines;
        [SerializeField] RectTransform[] _enemyLines;
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

        readonly BattleClock _clock = new BattleClock();
        readonly List<BattleUnitView> _partyViews = new List<BattleUnitView>();
        readonly List<BattleUnitView> _enemyViews = new List<BattleUnitView>();
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

        /// <summary>The clock that paces this battle. Tests speed it up.</summary>
        public BattleClock Clock => _clock;

        /// <summary>What the battle shows when something happens. For tests.</summary>
        public BattleFxLayer Fx => _fx;

        /// <summary>How many events of the log the presenter has passed.</summary>
        public int PlayedEvents => _presenter == null ? 0 : _presenter.Played;

        /// <summary>How far the storm's ring has filled: 0 at the start, 1 when the storm is here.</summary>
        public float StormRingFill => _clockRing.fillAmount;

        /// <summary>The captions under the clock, oldest first.</summary>
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

            // A unit has a view on the stage and a line in the panel; for an ally, both take the click a potion is aimed with.
            foreach (BattleUnit unit in engine.Party)
            {
                BattleUnitView view = CreateUnit(unit, _partyRows[unit.Row - 1]);
                BattleBoardView board = CreateBoard(unit, _partyLines[unit.Row - 1]);
                _partyViews.Add(view);
                _partyBoards.Add(board);
                int index = unit.Index;
                view.Button.onClick.AddListener(() => OnPartyUnitClicked(index));
                board.Button.onClick.AddListener(() => OnPartyUnitClicked(index));
            }

            foreach (BattleUnit unit in engine.Enemies)
            {
                _enemyViews.Add(CreateUnit(unit, _enemyRows[unit.Row - 1]));
                _enemyBoards.Add(CreateBoard(unit, _enemyLines[unit.Row - 1]));
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

            _presenter = new BattlePresenter(engine, Managers.Data.Data, _fx, UnitViewOf, BoardViewOf, _clockRing.rectTransform, PushCaption);
            RenderCaptions();
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

        /// <summary>A new line under the clock; the oldest line goes when there are more than fit.</summary>
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
        /// </summary>
        void LayoutColumns(int partyRows)
        {
            partyRows = Mathf.Min(partyRows, _partyRows.Length);
            float width = FieldLayout.ColumnWidth(_field.rect.width, partyRows, _enemyRows.Length);

            for (int i = 0; i < _partyRows.Length; i++)
            {
                bool used = i < partyRows;
                _partyRows[i].gameObject.SetActive(used);
                if (used)
                {
                    PlaceColumn(_partyRows[i], FieldLayout.PartyColumnX(width, partyRows, i), width);
                }
            }

            for (int i = 0; i < _enemyRows.Length; i++)
            {
                PlaceColumn(_enemyRows[i], FieldLayout.EnemyColumnX(width, partyRows, i), width);
            }
        }

        static void PlaceColumn(RectTransform column, float x, float width)
        {
            column.anchoredPosition = new Vector2(x, column.anchoredPosition.y);
            column.sizeDelta = new Vector2(width, column.sizeDelta.y);
        }

        BattleUnitView CreateUnit(BattleUnit unit, Transform parent)
        {
            BattleUnitView view = Instantiate(_unitTemplate, parent);
            view.gameObject.SetActive(true);

            // A party unit is a mercenary and is shown as its job; an enemy has its own figure.
            string id = unit.Setup.SourceId;
            view.Bind(unit, unit.Side == BattleSide.Party ? _art.OfMercenary(id) : _art.OfEnemy(id));
            return view;
        }

        /// <summary>The unit's line of the board panel: the face cut out of the figure the unit is shown as, and its cells.</summary>
        BattleBoardView CreateBoard(BattleUnit unit, Transform line)
        {
            BattleBoardView board = Instantiate(_boardTemplate, line);
            board.gameObject.SetActive(true);
            string id = unit.Setup.SourceId;
            board.Bind(unit, unit.Side == BattleSide.Party ? _art.FaceOfMercenary(id) : _art.FaceOfEnemy(id), _art);
            return board;
        }

        void Render()
        {
            BattleEngine engine = _battle.Engine;
            BalanceData balance = engine.Setup.Balance;
            bool ongoing = !_battle.IsFinished;

            // What happened since the last frame is played before the dead leave the stage, so that a
            // fallen unit's ghost starts where it stood.
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

            Place(_partyViews, _partyRows, view => view.Unit, (view, fromX) => view.Walk(fromX));
            Place(_enemyViews, _enemyRows, view => view.Unit, (view, fromX) => view.Walk(fromX));
            Place(_partyBoards, _partyLines, board => board.Unit, null);
            Place(_enemyBoards, _enemyLines, board => board.Unit, null);
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

            if (!ongoing && !_resultShown)
            {
                ShowResult(engine);
            }
        }

        /// <summary>
        /// The clock in the middle of the panel: the battle time, and the storm as a ring that
        /// fills until the storm is here, when it turns the storm's color and the label says what
        /// the next tick takes. The stage darkens over the last seconds before the storm.
        /// </summary>
        void RenderClock(BattleEngine engine, BalanceData balance)
        {
            _clockTime.text = UiStrings.Get(UiKeys.Battle.Time, UiText.Seconds(engine.TimeMs));
            bool storm = engine.TimeMs >= balance.StormStartMs;
            _clockRing.fillAmount = storm ? 1f : Mathf.Clamp01((float)engine.TimeMs / balance.StormStartMs);
            _clockRing.color = storm ? UiPalette.Burn : UiPalette.Text;
            _clockLabel.text = storm
                ? UiStrings.Get(UiKeys.Battle.StormActive, engine.NextStormDamage)
                : UiStrings.Get(UiKeys.Battle.StormIn, UiText.Seconds(balance.StormStartMs - engine.TimeMs));
            _clockLabel.color = storm ? UiPalette.Burn : UiPalette.TextDim;
            _fx.SetStorm(storm ? 1f : Mathf.Clamp01((float)(engine.TimeMs - (balance.StormStartMs - StormDuskMs)) / StormDuskMs));
        }

        /// <summary>
        /// Puts each unit's view (on the stage, or in the panel) in the place of the row the unit
        /// stands in now and takes the dead away. Views are visited in unit order, so those that
        /// advance into a place together keep their order. A view that changes place is told how
        /// far it came from, so that it can walk in instead of appearing.
        /// </summary>
        static void Place<T>(List<T> views, RectTransform[] places, Func<T, BattleUnit> unitOf, Action<T, float> moved)
            where T : Component
        {
            foreach (T view in views)
            {
                BattleUnit unit = unitOf(view);
                if (view.gameObject.activeSelf != unit.Alive)
                {
                    view.gameObject.SetActive(unit.Alive);
                }

                RectTransform place = places[unit.Row - 1];
                if (unit.Alive && view.transform.parent != place)
                {
                    var from = (RectTransform)view.transform.parent;
                    view.transform.SetParent(place, false);
                    moved?.Invoke(view, from.anchoredPosition.x - place.anchoredPosition.x);
                }
            }
        }

        /// <summary>Potions, their hint and the retreat button. They change with battle time (cooldowns) and with clicks.</summary>
        void RenderControls(BattleEngine engine, BalanceData balance, bool ongoing)
        {
            bool potionReady = engine.TimeMs >= engine.PotionReadyMs;
            for (int i = 0; i < _potionViews.Count; i++)
            {
                PotionData potion = engine.Potions[i];
                _potionViews[i].Show(potion, i == _armedPotion, ongoing && potion != null && potionReady);
            }

            if (_armedPotion >= 0 && engine.Potions[_armedPotion] != null)
            {
                _potionHint.text = UiStrings.Get(UiKeys.Battle.PotionArmed, UiText.Name(engine.Potions[_armedPotion].Name));
            }
            else if (!potionReady)
            {
                _potionHint.text = UiStrings.Get(UiKeys.Battle.PotionWait, UiText.Seconds(engine.PotionReadyMs - engine.TimeMs));
            }
            else
            {
                _potionHint.text = UiStrings.Get(UiKeys.Battle.PotionHint);
            }

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
                    break;
                case BattleResult.Retreated:
                    _resultTitle.text = UiStrings.Get(UiKeys.Battle.Retreated);
                    _resultTitle.color = UiPalette.Text;
                    break;
                default:
                    _resultTitle.text = UiStrings.Get(UiKeys.Battle.Defeat);
                    _resultTitle.color = UiPalette.Danger;
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
