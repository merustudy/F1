using System.Collections.Generic;
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
    /// Each side has one column per row, and a row holds one unit. A unit's card stands in the
    /// column of the row the engine says it is in, so it moves when the unit advances. The dead
    /// leave the field. The event log is not shown while the battle runs; the result panel opens
    /// a viewer with the whole log.
    /// </summary>
    public sealed class BattleScreen : UIScreen
    {
        /// <summary>Lines per text of the log viewer: a long log is split over several texts.</summary>
        const int LogLinesPerChunk = 40;

        static readonly int[] Speeds = { 100, 200, 400 };

        [SerializeField] TMP_Text _time;
        [SerializeField] TMP_Text _storm;
        [SerializeField] Button _pause;
        [SerializeField] Image _pauseFrame;
        [SerializeField] Button[] _speedButtons;
        [SerializeField] Image[] _speedFrames;
        [SerializeField] TMP_Text[] _speedLabels;
        [SerializeField] BattleUnitView _unitTemplate;
        [SerializeField] RectTransform _field;
        [SerializeField] RectTransform[] _partyRows;
        [SerializeField] RectTransform[] _partyRowLabels;
        [SerializeField] RectTransform[] _enemyRows;
        [SerializeField] RectTransform[] _enemyRowLabels;
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
        readonly List<PotionSlotView> _potionViews = new List<PotionSlotView>();
        readonly List<TMP_Text> _logChunks = new List<TMP_Text>();
        BattleSession _battle;
        int _armedPotion = -1;
        int _shownTimeTenths = -1;
        bool _resultShown;

        /// <summary>The clock that paces this battle. Tests speed it up.</summary>
        public BattleClock Clock => _clock;

        protected override void OnOpen()
        {
            _battle = Managers.Expedition.Battle;
            _clock.SpeedPercent = _preferredSpeedPercent;
            BattleEngine engine = _battle.Engine;

            // A battle continued from a save is already under way: let the player look before it moves on.
            _clock.Paused = engine.TimeMs > 0;

            LayoutColumns(engine.Setup.Balance.PartySize);

            foreach (BattleUnit unit in engine.Party)
            {
                BattleUnitView view = CreateUnit(unit, _partyRows[unit.Row - 1]);
                _partyViews.Add(view);
                int index = unit.Index;
                view.Button.onClick.AddListener(() => OnPartyUnitClicked(index));
            }

            foreach (BattleUnit unit in engine.Enemies)
            {
                _enemyViews.Add(CreateUnit(unit, _enemyRows[unit.Row - 1]));
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

            _shownTimeTenths = -1;
            _resultShown = false;
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
                _partyRowLabels[i].gameObject.SetActive(used);
                if (used)
                {
                    PlaceColumn(_partyRows[i], _partyRowLabels[i], FieldLayout.PartyColumnX(width, partyRows, i), width);
                }
            }

            for (int i = 0; i < _enemyRows.Length; i++)
            {
                PlaceColumn(_enemyRows[i], _enemyRowLabels[i], FieldLayout.EnemyColumnX(width, partyRows, i), width);
            }
        }

        static void PlaceColumn(RectTransform column, RectTransform label, float x, float width)
        {
            column.anchoredPosition = new Vector2(x, column.anchoredPosition.y);
            column.sizeDelta = new Vector2(width, column.sizeDelta.y);
            label.anchoredPosition = new Vector2(x, label.anchoredPosition.y);
            label.sizeDelta = new Vector2(width, label.sizeDelta.y);
        }

        BattleUnitView CreateUnit(BattleUnit unit, Transform parent)
        {
            BattleUnitView view = Instantiate(_unitTemplate, parent);
            view.gameObject.SetActive(true);
            view.Bind(unit);
            return view;
        }

        void Render()
        {
            BattleEngine engine = _battle.Engine;
            BalanceData balance = engine.Setup.Balance;
            bool ongoing = !_battle.IsFinished;

            int tenths = engine.TimeMs / 100;
            if (tenths != _shownTimeTenths)
            {
                _shownTimeTenths = tenths;
                _time.text = UiStrings.Get(UiKeys.Battle.Time, UiText.Seconds(engine.TimeMs));
                _storm.text = engine.TimeMs < balance.StormStartMs
                    ? UiStrings.Get(UiKeys.Battle.StormIn, UiText.Seconds(balance.StormStartMs - engine.TimeMs))
                    : UiStrings.Get(UiKeys.Battle.StormActive, engine.NextStormDamage);
                _storm.color = engine.TimeMs < balance.StormStartMs ? UiPalette.TextDim : UiPalette.Burn;
                RenderControls(engine, balance, ongoing);
            }

            PlaceUnits(_partyViews, _partyRows);
            PlaceUnits(_enemyViews, _enemyRows);
            for (int i = 0; i < _partyViews.Count; i++)
            {
                _partyViews[i].Render(engine, ongoing && _armedPotion >= 0);
            }

            foreach (BattleUnitView view in _enemyViews)
            {
                view.Render(engine, false);
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
        /// Puts each card in the column of the row its unit stands in now and takes the cards of the
        /// dead off the field. Cards are visited in unit order, so those that advance into a column
        /// together keep their order.
        /// </summary>
        static void PlaceUnits(List<BattleUnitView> views, RectTransform[] columns)
        {
            foreach (BattleUnitView view in views)
            {
                BattleUnit unit = view.Unit;
                if (view.gameObject.activeSelf != unit.Alive)
                {
                    view.gameObject.SetActive(unit.Alive);
                }

                Transform column = columns[unit.Row - 1];
                if (unit.Alive && view.transform.parent != column)
                {
                    view.transform.SetParent(column, false);
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
