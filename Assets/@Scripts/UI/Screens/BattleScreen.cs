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
    /// </summary>
    public sealed class BattleScreen : UIScreen
    {
        const int MaxLogLines = 12;
        static readonly int[] Speeds = { 100, 200, 400 };

        [SerializeField] TMP_Text _node;
        [SerializeField] TMP_Text _time;
        [SerializeField] TMP_Text _storm;
        [SerializeField] Button _pause;
        [SerializeField] Image _pauseFrame;
        [SerializeField] Button[] _speedButtons;
        [SerializeField] Image[] _speedFrames;
        [SerializeField] TMP_Text[] _speedLabels;
        [SerializeField] BattleUnitView _unitTemplate;
        [SerializeField] Transform _partyFront;
        [SerializeField] Transform _partyRear;
        [SerializeField] Transform _enemyFront;
        [SerializeField] Transform _enemyRear;
        [SerializeField] PotionSlotView _potionTemplate;
        [SerializeField] Transform _potionParent;
        [SerializeField] TMP_Text _potionHint;
        [SerializeField] Button _retreat;
        [SerializeField] TMP_Text _retreatLabel;
        [SerializeField] TMP_Text _log;
        [SerializeField] GameObject _resultPanel;
        [SerializeField] TMP_Text _resultTitle;
        [SerializeField] TMP_Text _resultDetail;
        [SerializeField] Button _continue;

        /// <summary>The speed the player last chose. Kept for the session so every battle starts at it.</summary>
        static int _preferredSpeedPercent = Speeds[0];

        readonly BattleClock _clock = new BattleClock();
        readonly List<BattleUnitView> _partyViews = new List<BattleUnitView>();
        readonly List<BattleUnitView> _enemyViews = new List<BattleUnitView>();
        readonly List<PotionSlotView> _potionViews = new List<PotionSlotView>();
        readonly List<string> _logLines = new List<string>();
        BattleSession _battle;
        int _loggedEvents;
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

            foreach (BattleUnit unit in engine.Party)
            {
                BattleUnitView view = CreateUnit(unit, unit.Row == BattleRow.Front ? _partyFront : _partyRear);
                _partyViews.Add(view);
                int index = unit.Index;
                view.Button.onClick.AddListener(() => OnPartyUnitClicked(index));
            }

            foreach (BattleUnit unit in engine.Enemies)
            {
                _enemyViews.Add(CreateUnit(unit, unit.Row == BattleRow.Front ? _enemyFront : _enemyRear));
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
            _continue.onClick.AddListener(OnContinue);
            _resultPanel.SetActive(false);
        }

        public override void Refresh()
        {
            // Everything that holds text is rebuilt, so a locale change shows at once.
            MapNode node = _battle.Node;
            _node.text = node.Kind == MapNodeKind.Boss
                ? UiStrings.Get(UiKeys.Battle.NodeBoss)
                : UiStrings.Get(UiKeys.Battle.NodeBattle, node.Floor);
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

            _logLines.Clear();
            _loggedEvents = 0;
            _shownTimeTenths = -1;
            _resultShown = false;
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

            for (int i = 0; i < _partyViews.Count; i++)
            {
                _partyViews[i].Render(engine, ongoing && _armedPotion >= 0);
            }

            foreach (BattleUnitView view in _enemyViews)
            {
                view.Render(engine, false);
            }

            RenderLog(engine);

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

        void RenderLog(BattleEngine engine)
        {
            IReadOnlyList<BattleEvent> events = engine.Events;
            if (_loggedEvents == events.Count)
            {
                return;
            }

            for (; _loggedEvents < events.Count; _loggedEvents++)
            {
                string line = BattleLogText.Line(events[_loggedEvents], engine);
                if (line != null)
                {
                    _logLines.Add(line);
                }
            }

            if (_logLines.Count > MaxLogLines)
            {
                _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
            }

            _log.text = string.Join("\n", _logLines);
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
