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
    /// The lobby as the company's town (Round 71, Docs/Design/04_Lobby_100Day_Economy.md §7): the painted town with a building per quarter,
    /// each named by a plate and lit under the pointer with a bubble that sums it up: the barracks (the roster, the party and its rows,
    /// dismissing), the inn (resting and the treatment), the stagecoach (hiring) and the gate (to the expedition screen). Under them a thin
    /// roster strip with [원정 화면으로]. A building opens its window over the darkened town. Past the last day the gate is the final stand and
    /// the inn is closed to resting (stage 25). Once the run is over the result window (Round 72) stands over the town with [새 런].
    /// Round 78 (stage 30): the town is seen from farther away with empty plots, and each building is a picture of its own that stands on its
    /// plot at its level's size, with a scaffold and a badge while it is built; the windows carry the building line with [업그레이드]. The gate
    /// has no level (2026-10-18, the user: "성문은 업그레이드 없음(원정)" and "이전 배경 성문 이용"): it is the stone arch painted on the town, a
    /// transparent hotspot with a plate, and clicking it goes to the expedition screen.
    /// The screen shows the state and sends commands; what can be done is asked of the run manager.
    /// </summary>
    public sealed class LobbyScreen : UIScreen
    {
        /// <summary>The painting (2304x1536) scaled to the screen's width, 1920x1280, from the top: its upper 1080 is the screen (Round 71: the floor line stays as painted).</summary>
        public const float TownWidth = 1920f;
        public const float TownHeight = 1280f;

        /// <summary>
        /// Where a building stands on the far town (Round 78): the foot of its picture (the middle of the plot's front edge, in the 1920-wide
        /// painting from the top) and the width of its picture at level 2; a level scales it (<see cref="LevelScale"/>): level 1 smaller, level 3 bigger.
        /// </summary>
        public readonly struct Plot
        {
            public Plot(float footX, float footY, float width)
            {
                FootX = footX;
                FootY = footY;
                Width = width;
            }

            public float FootX { get; }
            public float FootY { get; }
            public float Width { get; }
        }

        public static readonly IReadOnlyDictionary<string, Plot> Plots = new Dictionary<string, Plot>(System.StringComparer.Ordinal)
        {
            { BuildingId.Barracks, new Plot(420f, 690f, 520f) },
            { BuildingId.Inn, new Plot(963f, 508f, 380f) },
            { BuildingId.Coach, new Plot(1330f, 728f, 360f) },
        };

        /// <summary>
        /// The gate's hotspot (2026-10-18, the user: the arch painted on the town is the gate — no picture of its own): the box over the arch at the
        /// town's right edge, its foot on the road under the arch, as wide as the plate so that the plate stays on the screen.
        /// </summary>
        public static readonly Plot GatePlot = new Plot(1780f, 505f, 280f);
        public const float GateHeight = 170f;

        /// <summary>How much of the plot's width a building's picture takes at each level (the mock of Round 78: 82%, 100%, 122%).</summary>
        public static float LevelScale(int level)
        {
            return level <= 1 ? 0.82f : level == 2 ? 1f : 1.22f;
        }

        /// <summary>How far over a building's top the bubble's centre stands.</summary>
        const float BubbleAbove = 40f;

        public enum Quarter
        {
            None,
            Barracks,
            Inn,
            Coach,
        }

        [SerializeField] Image _town;
        [SerializeField] TMP_Text _day;
        [SerializeField] TMP_Text _gold;
        [SerializeField] Button _toTitle;
        [SerializeField] BuildingView _barracks;
        [SerializeField] BuildingView _inn;
        [SerializeField] BuildingView _coach;
        [SerializeField] BuildingView _gate;
        [SerializeField] RectTransform _bubble;
        [SerializeField] TMP_Text _bubbleName;
        [SerializeField] TMP_Text _bubbleDetail;
        [SerializeField] RosterChipView _chipTemplate;
        [SerializeField] Transform _chipParent;
        [SerializeField] TMP_Text _stripStatus;
        [SerializeField] Button _depart;
        [SerializeField] TMP_Text _departLabel;
        [SerializeField] GameObject _shade;
        [SerializeField] GameObject _barracksWindow;
        [SerializeField] Button _barracksClose;
        [SerializeField] RosterEntryView _entryTemplate;
        [SerializeField] Transform _entryParent;
        [SerializeField] TMP_Text _barracksSummary;
        [SerializeField] GameObject _innWindow;
        [SerializeField] Button _innClose;
        [SerializeField] Button _rest;
        [SerializeField] TMP_Text _restLabel;
        [SerializeField] PatientView _patientTemplate;
        [SerializeField] Transform _patientParent;
        [SerializeField] TMP_Text _innNote;
        [SerializeField] GameObject _coachWindow;
        [SerializeField] Button _coachClose;
        [SerializeField] TMP_Text _coachSummary;
        [SerializeField] CandidateView _candidateTemplate;
        [SerializeField] Transform _candidateParent;
        [SerializeField] TMP_Text _noCandidates;
        [SerializeField] BuildingLineView _barracksLine;
        [SerializeField] BuildingLineView _innLine;
        [SerializeField] BuildingLineView _coachLine;
        [SerializeField] GameObject _runOverWindow;
        [SerializeField] TMP_Text _runOverTitle;
        [SerializeField] TMP_Text _runOverDay;
        [SerializeField] TMP_Text _runOverExpeditions;
        [SerializeField] TMP_Text _runOverFallen;
        [SerializeField] TMP_Text _runOverSurvivors;
        [SerializeField] Button _runOverNewRun;

        readonly Dictionary<string, RosterEntryView> _entries = new Dictionary<string, RosterEntryView>();
        readonly Dictionary<string, RosterChipView> _chips = new Dictionary<string, RosterChipView>();
        readonly Dictionary<string, PatientView> _patients = new Dictionary<string, PatientView>();
        // What the barracks' level-ups grew since the window opened (Slice D stage 35, Round 82 안 A): shown beside the values until it closes.
        readonly Dictionary<string, StatBlock> _grown = new Dictionary<string, StatBlock>();
        readonly List<CandidateView> _candidates = new List<CandidateView>();
        readonly Dictionary<string, Sprite> _pictures = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
        Sprite _townArt;
        Sprite _scaffoldArt;
        Quarter _open;

        /// <summary>The quarter whose window is open, or None.</summary>
        public Quarter OpenQuarter => _open;

        public bool BubbleShown => _bubble.gameObject.activeSelf;
        public string BubbleName => _bubbleName.text;
        public string BubbleDetail => _bubbleDetail.text;
        public BuildingView Barracks => _barracks;
        public BuildingView Inn => _inn;
        public BuildingView Coach => _coach;
        public BuildingView Gate => _gate;

        /// <summary>The candidates' views, in the order of the run's Recruits (rebuilt on every refresh).</summary>
        public IReadOnlyList<CandidateView> Candidates => _candidates;

        /// <summary>
        /// The dungeon the strip's button and the gate stand for: the final one once the final stand is due, else the first that is not final.
        /// Which dungeon to go to is chosen on the expedition screen; this only decides whether the way is open.
        /// </summary>
        static DungeonData Dungeon
        {
            get
            {
                StaticData data = Managers.Data.Data;
                if (Managers.Run.IsFinalStandDue && data.FinalDungeon != null)
                {
                    return data.FinalDungeon;
                }

                foreach (DungeonData dungeon in data.Dungeons.Ordered)
                {
                    if (!dungeon.IsFinal)
                    {
                        return dungeon;
                    }
                }

                return data.Dungeons.Ordered[0];
            }
        }

        /// <summary>The town, the buildings at every level and the scaffold are loaded before the screen opens, into the lobby's scope: the expedition screen shares the scope.</summary>
        public override async Task PrepareAsync()
        {
            _townArt = await Managers.Resource.LoadAsync<Sprite>(ArtAddress.LobbyTown, ResourceScope.Lobby);
            _scaffoldArt = await Managers.Resource.LoadAsync<Sprite>(ArtAddress.Scaffold, ResourceScope.Lobby);
            StaticData data = Managers.Data.Data;
            foreach (string buildingId in BuildingId.All)
            {
                for (int level = 1; level <= data.MaxBuildingLevel(buildingId); level++)
                {
                    string address = ArtAddress.Building(buildingId, level);
                    _pictures[address] = await Managers.Resource.LoadAsync<Sprite>(address, ResourceScope.Lobby);
                }
            }
        }

        /// <summary>The building view of a building id (one of <see cref="BuildingId.All"/>).</summary>
        public BuildingView Building(string buildingId)
        {
            switch (buildingId)
            {
                case BuildingId.Barracks: return _barracks;
                case BuildingId.Inn: return _inn;
                default: return _coach;
            }
        }

        BuildingLineView Line(string buildingId)
        {
            switch (buildingId)
            {
                case BuildingId.Barracks: return _barracksLine;
                case BuildingId.Inn: return _innLine;
                default: return _coachLine;
            }
        }

        protected override void OnOpen()
        {
            _town.sprite = _townArt;
            _toTitle.onClick.AddListener(() => GoTo(ScreenId.Title));
            _barracks.Button.onClick.AddListener(() => Open(Quarter.Barracks));
            _inn.Button.onClick.AddListener(() => Open(Quarter.Inn));
            _coach.Button.onClick.AddListener(() => Open(Quarter.Coach));
            // The gate goes straight to the expedition screen (as before Round 78; 2026-10-18: no gate window, nothing to upgrade there).
            _gate.Button.onClick.AddListener(OnDepart);
            _depart.onClick.AddListener(OnDepart);
            _barracksClose.onClick.AddListener(() => Open(Quarter.None));
            _innClose.onClick.AddListener(() => Open(Quarter.None));
            _coachClose.onClick.AddListener(() => Open(Quarter.None));
            _rest.onClick.AddListener(OnRest);
            _runOverNewRun.onClick.AddListener(OnNewRun);
            foreach (string buildingId in BuildingId.All)
            {
                string id = buildingId;
                Line(id).Upgrade.onClick.AddListener(() => Upgrade(id));
            }

            foreach (BuildingView building in new[] { _barracks, _inn, _coach, _gate })
            {
                building.SetScaffold(_scaffoldArt);
                building.Entered += ShowBubble;
                building.Exited += HideBubble;
            }

            // The gate is the arch painted on the town: a transparent hotspot over it, placed once; it never grows and is never built.
            _gate.Place(null, GatePlot.FootX, GatePlot.FootY, GatePlot.Width, GateHeight);
            _gate.HideConstruction();

            _bubble.gameObject.SetActive(false);
            Managers.Sound.PlayMusic(MusicTrack.Lobby);
        }

        /// <summary>Opens a quarter's window over the town, or closes it (None). The bubble goes with the pointer leaving the building.</summary>
        public void Open(Quarter quarter)
        {
            if (quarter != _open)
            {
                _grown.Clear();
            }

            _open = quarter;
            _bubble.gameObject.SetActive(false);
            Refresh();
        }

        public override void Refresh()
        {
            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            RunState run = Managers.Run.Run;
            bool due = Managers.Run.IsFinalStandDue;
            if (run.IsOver)
            {
                _open = Quarter.None;
            }

            _day.text = due ? UiStrings.Get(UiKeys.Lobby.DayFinalStand, run.Day, balance.TotalDays) : UiStrings.Get(UiKeys.Lobby.Day, run.Day, balance.TotalDays);
            _day.color = due ? UiPalette.Virtue : UiPalette.Text;
            _gold.text = UiStrings.Get(UiKeys.Lobby.Gold, run.Gold);

            RefreshTown(data, run, due);
            RefreshStrip(data, run, due);
            RefreshBarracks(data, run);
            RefreshInn(data, run, due);
            RefreshCoach(data, run);
            RefreshRunOver(data, run);

            _shade.SetActive(_open != Quarter.None || run.IsOver);
            _barracksWindow.SetActive(_open == Quarter.Barracks);
            _innWindow.SetActive(_open == Quarter.Inn);
            _coachWindow.SetActive(_open == Quarter.Coach);
            _runOverWindow.SetActive(run.IsOver);
        }

        // ---- The town ------------------------------------------------------------------------

        /// <summary>
        /// The buildings on their plots at their levels (Round 78), the plates with the levels (the gate has none; it reads "final stand" past the
        /// last day and says whose boss; the inn says no day is left to rest), the scaffold and the badge of a building under construction (its plate dimmed).
        /// </summary>
        void RefreshTown(StaticData data, RunState run, bool due)
        {
            foreach (string buildingId in BuildingId.All)
            {
                BuildingView view = Building(buildingId);
                int level = Managers.Run.BuildingLevel(buildingId);
                Plot plot = Plots[buildingId];
                _pictures.TryGetValue(ArtAddress.Building(buildingId, level), out Sprite picture);
                view.Place(picture, plot.FootX, plot.FootY, plot.Width * LevelScale(level));

                ConstructionState construction = Managers.Run.ConstructionOf(buildingId);
                if (construction != null)
                {
                    int left = BuildingRules.DaysLeft(run, construction);
                    int days = data.BuildingAt(buildingId, construction.ToLevel).Days;
                    view.ShowConstruction(UiStrings.Get(UiKeys.Lobby.Construction, left), days <= 0 ? 0f : 1f - (float)left / days);
                }
                else
                {
                    view.HideConstruction();
                }
            }

            string Plate(string key, string buildingId)
            {
                return UiStrings.Get(UiKeys.Lobby.BuildingPlate, UiStrings.Get(key), Managers.Run.BuildingLevel(buildingId));
            }

            _barracks.ShowPlate(Plate(UiKeys.Lobby.Barracks, BuildingId.Barracks), null, dim: Managers.Run.IsBuildingLocked(BuildingId.Barracks));
            // How many the barracks can level up (Slice D stage 35): a small gold count at the plate's corner, none at zero.
            int ready = 0;
            foreach (MercenaryState mercenary in run.Roster)
            {
                ready += Managers.Run.CanLevelUp(mercenary.Id) ? 1 : 0;
            }

            _barracks.ShowCount(ready > 0 ? UiStrings.Get(UiKeys.Lobby.LevelUpCount, ready) : null);
            _inn.ShowPlate(Plate(UiKeys.Lobby.Inn, BuildingId.Inn), due && !run.IsOver ? UiStrings.Get(UiKeys.Lobby.InnClosed) : null, dim: due || Managers.Run.IsBuildingLocked(BuildingId.Inn));
            _coach.ShowPlate(Plate(UiKeys.Lobby.Coach, BuildingId.Coach), null, dim: Managers.Run.IsBuildingLocked(BuildingId.Coach));
            if (due && data.FinalDungeon != null)
            {
                _gate.ShowPlate(UiStrings.Get(UiKeys.Lobby.FinalStand), UiStrings.Get(UiKeys.Lobby.FinalStandSub, UiText.Name(data.FinalDungeon.Name)), dim: false);
            }
            else
            {
                _gate.ShowPlate(UiStrings.Get(UiKeys.Lobby.Gate), null, dim: false);
            }

            // Every building opens its window while the run goes on (a locked one shows its construction there); the gate is the way out.
            _gate.Button.interactable = !run.IsOver;
            _barracks.Button.interactable = !run.IsOver;
            _inn.Button.interactable = !run.IsOver;
            _coach.Button.interactable = !run.IsOver;
        }

        /// <summary>
        /// The building line of a window (stage 30): the level, what the next level gives with its price and days (or the top level, or the
        /// construction going on), the [업그레이드] button, and the note that the building is closed while it is built.
        /// </summary>
        void RefreshLine(StaticData data, RunState run, string buildingId)
        {
            int level = Managers.Run.BuildingLevel(buildingId);
            BuildingData next = BuildingRules.NextLevel(data, run, buildingId);
            ConstructionState construction = Managers.Run.ConstructionOf(buildingId);
            string levelWords = UiStrings.Get(UiKeys.Lobby.Level, level);
            string effect;
            string button = null;
            if (construction != null)
            {
                effect = UiStrings.Get(UiKeys.Lobby.ConstructionLine, level, construction.ToLevel, BuildingRules.DaysLeft(run, construction));
            }
            else if (next == null)
            {
                effect = UiStrings.Get(UiKeys.Lobby.MaxLevelLine, level, Effect(data, run, buildingId, level, level));
            }
            else
            {
                effect = UiStrings.Get(UiKeys.Lobby.UpgradeLine, level, next.Level, Effect(data, run, buildingId, level, next.Level), next.Price, next.Days);
                button = UiStrings.Get(UiKeys.Lobby.UpgradeButton, next.Price, next.Days);
            }

            Line(buildingId).Show(levelWords, effect, button, Managers.Run.CanUpgrade(buildingId) == UpgradeCheck.Ok, Managers.Run.IsBuildingLocked(buildingId));
        }

        /// <summary>What a building gives at a level and at another: "로스터 상한 8 → 10" and the like, from the balance base and the levels' bonuses.</summary>
        static string Effect(StaticData data, RunState run, string buildingId, int level, int otherLevel)
        {
            BalanceData balance = data.Balance;
            int bonus = data.BuildingAt(buildingId, level).Bonus;
            int other = data.BuildingAt(buildingId, otherLevel).Bonus;
            switch (buildingId)
            {
                case BuildingId.Barracks: return UiStrings.Get(UiKeys.Lobby.EffectBarracks, balance.RosterLimit + bonus, balance.RosterLimit + other);
                case BuildingId.Inn: return UiStrings.Get(UiKeys.Lobby.EffectInn, balance.TreatmentRelief + bonus, balance.TreatmentRelief + other);
                default: return UiStrings.Get(UiKeys.Lobby.EffectCoach, balance.RecruitCount + bonus, balance.RecruitCount + other);
            }
        }

        /// <summary>The dungeons, the locked ones saying so; past the last day, whose boss the final stand is against.</summary>
        string GateWords(StaticData data)
        {
            if (Managers.Run.IsFinalStandDue && data.FinalDungeon != null)
            {
                return UiStrings.Get(UiKeys.Lobby.FinalStandSub, UiText.Name(data.FinalDungeon.Name));
            }

            var parts = new List<string>();
            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                string name = UiText.Name(dungeon.Name);
                parts.Add(Managers.Run.IsUnlocked(dungeon.Id) ? name : UiStrings.Get(UiKeys.Lobby.DungeonLocked, name));
            }

            return string.Join(" · ", parts);
        }

        /// <summary>The bubble over a building the pointer is on: its name and what it holds now.</summary>
        void ShowBubble(BuildingView building)
        {
            if (_open != Quarter.None || Managers.Run.Run.IsOver)
            {
                return;
            }

            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            RunState run = Managers.Run.Run;
            _bubbleName.text = building.Name;
            _bubbleDetail.text = BubbleDetailOf(building, data, balance, run);

            // Centred over the plate, a little above the building's top (the mock: the stagecoach's bubble at its top), and never under the header.
            RectTransform rect = building.Rect;
            float x = rect.anchoredPosition.x + building.Plate.anchoredPosition.x + building.Plate.sizeDelta.x / 2f;
            float top = -rect.anchoredPosition.y;
            float y = Mathf.Max(100f + _bubble.sizeDelta.y / 2f + 8f, top - BubbleAbove);
            _bubble.anchoredPosition = new Vector2(x - _bubble.sizeDelta.x / 2f, -(y - _bubble.sizeDelta.y / 2f));
            _bubble.gameObject.SetActive(true);
        }

        void HideBubble(BuildingView building)
        {
            _bubble.gameObject.SetActive(false);
        }

        /// <summary>What the bubble says under the name: the construction going on, or what the building holds now and, if it can be upgraded, the price. The gate names the dungeons.</summary>
        string BubbleDetailOf(BuildingView building, StaticData data, BalanceData balance, RunState run)
        {
            if (building == _gate)
            {
                return GateWords(data);
            }

            string buildingId = building == _barracks ? BuildingId.Barracks : building == _inn ? BuildingId.Inn : BuildingId.Coach;
            ConstructionState construction = Managers.Run.ConstructionOf(buildingId);
            if (construction != null)
            {
                return UiStrings.Get(UiKeys.Lobby.Construction, BuildingRules.DaysLeft(run, construction));
            }

            string detail;
            if (building == _barracks)
            {
                detail = UiStrings.Get(UiKeys.Lobby.BarracksHint, run.Roster.Count, BuildingRules.RosterLimit(data, run), run.Party.Count);
            }
            else if (building == _inn)
            {
                int tired = 0;
                foreach (MercenaryState mercenary in run.Roster)
                {
                    tired += mercenary.Fatigue >= balance.FatigueBreakdown ? 1 : 0;
                }

                detail = UiStrings.Get(UiKeys.Lobby.InnHint, balance.FatigueBreakdown, tired, balance.TreatmentPrice);
            }
            else
            {
                detail = UiStrings.Get(UiKeys.Lobby.CoachHint, run.Recruits.Count, balance.HirePrice);
            }

            BuildingData next = BuildingRules.NextLevel(data, run, buildingId);
            return next != null && Managers.Run.CanUpgrade(buildingId) == UpgradeCheck.Ok ? detail + " · " + UiStrings.Get(UiKeys.Lobby.UpgradeHint, next.Price) : detail;
        }

        // ---- The strip -----------------------------------------------------------------------

        /// <summary>A chip per mercenary of the roster, the strip's summary, and the button to the expedition screen (the final stand past the last day).</summary>
        void RefreshStrip(StaticData data, RunState run, bool due)
        {
            BalanceData balance = data.Balance;
            var alive = new HashSet<string>();
            foreach (MercenaryState mercenary in run.Roster)
            {
                alive.Add(mercenary.Id);
                if (!_chips.TryGetValue(mercenary.Id, out RosterChipView chip))
                {
                    chip = Instantiate(_chipTemplate, _chipParent);
                    chip.gameObject.SetActive(true);
                    chip.name = "Chip_" + mercenary.Id;
                    _chips.Add(mercenary.Id, chip);
                }

                int row = RowOf(run, mercenary.Id);
                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                chip.Show(
                    UiText.Mercenary(mercenary.Id),
                    UiText.ChipLevel(mercenary.Level, Managers.Run.CanLevelUp(mercenary.Id)),
                    row == 0 ? null : UiStrings.Get(UiText.RowKey(row)),
                    UiText.JobLine(mercenary.JobId, affliction),
                    affliction != null,
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown);
                chip.transform.SetAsLastSibling();
            }

            DropGone(_chips, alive);

            _stripStatus.text = UiStrings.Get(UiKeys.Lobby.RosterCount, run.Roster.Count, BuildingRules.RosterLimit(data, run)) + " · " + UiStrings.Get(UiKeys.Lobby.Deaths, run.Fallen.Count, balance.DeathLimit);
            DepartCheck check = Managers.Run.CanDepart(Dungeon.Id);
            _depart.interactable = check == DepartCheck.Ok || check == DepartCheck.PartyTooSmall;
            _departLabel.text = UiStrings.Get(due ? UiKeys.Lobby.ToFinalStand : UiKeys.Lobby.Depart);
        }

        // ---- The barracks --------------------------------------------------------------------

        void RefreshBarracks(StaticData data, RunState run)
        {
            BalanceData balance = data.Balance;
            RefreshLine(data, run, BuildingId.Barracks);
            var alive = new HashSet<string>();
            foreach (MercenaryState mercenary in run.Roster)
            {
                alive.Add(mercenary.Id);
                if (!_entries.TryGetValue(mercenary.Id, out RosterEntryView entry))
                {
                    entry = Instantiate(_entryTemplate, _entryParent);
                    entry.gameObject.SetActive(true);
                    entry.name = "Entry_" + mercenary.Id;
                    _entries.Add(mercenary.Id, entry);

                    string id = mercenary.Id;
                    entry.Dismiss.onClick.AddListener(() => Dismiss(id));
                    entry.LevelUp.onClick.AddListener(() => LevelUp(id));
                }

                // An affliction came home with the mercenary (a virtue never does: Docs/Design/04_Lobby_100Day_Economy.md §3).
                JobData job = data.Jobs.Get(mercenary.JobId);
                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                entry.Show(
                    UiText.Mercenary(mercenary.Id),
                    UiText.Name(job.Name),
                    UiText.Passive(job),
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown,
                    affliction == null ? null : UiText.FatigueStateName(affliction),
                    affliction == null ? null : UiText.Name(affliction.Description),
                    Managers.Run.CanDismiss(mercenary.Id));
                entry.ShowLevel(mercenary.Level, mercenary.Exp, LevelRules.ExpToNext(data, mercenary), Managers.Run.CanLevelUp(mercenary.Id));
                entry.ShowStats(mercenary.Stats ?? StatRules.BaseOf(data, mercenary.JobId), _grown.TryGetValue(mercenary.Id, out StatBlock gains) ? gains : null);
                entry.transform.SetAsLastSibling();
            }

            DropGone(_entries, alive);

            var parts = new List<string>
            {
                UiStrings.Get(UiKeys.Lobby.Gold, run.Gold),
                UiStrings.Get(UiKeys.Lobby.RosterCount, run.Roster.Count, BuildingRules.RosterLimit(data, run)),
                UiStrings.Get(UiKeys.Lobby.Deaths, run.Fallen.Count, balance.DeathLimit),
            };
            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                string name = UiText.Name(dungeon.Name);
                if (Managers.Run.IsUnlocked(dungeon.Id))
                {
                    run.ClearedDungeons.TryGetValue(dungeon.Id, out int cleared);
                    parts.Add(UiStrings.Get(UiKeys.Lobby.ClearedOf, name, cleared));
                }
                else
                {
                    parts.Add(UiStrings.Get(UiKeys.Lobby.LockedOf, name, UiText.Name(data.Dungeons.Get(dungeon.UnlockDungeonId).Name), dungeon.UnlockClears));
                }
            }

            _barracksSummary.text = string.Join(" · ", parts);
        }

        // ---- The inn -------------------------------------------------------------------------

        void RefreshInn(StaticData data, RunState run, bool due)
        {
            BalanceData balance = data.Balance;
            RefreshLine(data, run, BuildingId.Inn);
            _rest.interactable = Managers.Run.CanRest;
            _restLabel.text = UiStrings.Get(UiKeys.Lobby.Rest, RunRules.FatigueRecovery(balance, balance.RestDays), balance.RestDays);
            _innNote.text = due && !run.IsOver ? UiStrings.Get(UiKeys.Lobby.InnClosed) : UiStrings.Get(UiKeys.Lobby.InnNote);

            var alive = new HashSet<string>();
            foreach (MercenaryState mercenary in run.Roster)
            {
                alive.Add(mercenary.Id);
                if (!_patients.TryGetValue(mercenary.Id, out PatientView patient))
                {
                    patient = Instantiate(_patientTemplate, _patientParent);
                    patient.gameObject.SetActive(true);
                    patient.name = "Patient_" + mercenary.Id;
                    _patients.Add(mercenary.Id, patient);
                    string id = mercenary.Id;
                    patient.Treat.onClick.AddListener(() => Treat(id));
                }

                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                string words = UiStrings.Get(UiKeys.Lobby.Fatigue, mercenary.Fatigue, balance.MaxFatigue);
                patient.Show(
                    UiText.Mercenary(mercenary.Id),
                    UiText.JobLine(mercenary.JobId, affliction),
                    affliction != null,
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown,
                    affliction == null ? words : UiStrings.Get(UiKeys.Lobby.FatigueState, words, UiText.FatigueStateName(affliction)),
                    UiStrings.Get(UiKeys.Lobby.Treat, balance.TreatmentPrice, BuildingRules.TreatmentRelief(data, run)),
                    Managers.Run.CanTreat(mercenary.Id) == TreatCheck.Ok);
                patient.transform.SetAsLastSibling();
            }

            DropGone(_patients, alive);
        }

        // ---- The stagecoach ------------------------------------------------------------------

        /// <summary>How far apart the six pairs of a candidate's stat line stand (Round 81: 106).</summary>
        const float CandidateStatColumn = 106f;

        /// <summary>The candidates are few and change at every settlement and hire: their views are rebuilt each time.</summary>
        void RefreshCoach(StaticData data, RunState run)
        {
            BalanceData balance = data.Balance;
            RefreshLine(data, run, BuildingId.Coach);
            _coachSummary.text = UiStrings.Get(UiKeys.Lobby.Gold, run.Gold) + " · " + UiStrings.Get(UiKeys.Lobby.RosterCount, run.Roster.Count, BuildingRules.RosterLimit(data, run));
            foreach (CandidateView candidate in _candidates)
            {
                Destroy(candidate.gameObject);
            }

            _candidates.Clear();
            for (int i = 0; i < run.Recruits.Count; i++)
            {
                RecruitState recruit = run.Recruits[i];
                CandidateView view = Instantiate(_candidateTemplate, _candidateParent);
                view.gameObject.SetActive(true);
                view.name = "Candidate_" + recruit.Id;
                JobData job = data.Jobs.Get(recruit.JobId);
                view.Show(
                    UiText.Mercenary(recruit.Id),
                    UiText.Name(job.Name),
                    UiStrings.Get(UiKeys.Lobby.Level, LevelRules.FirstLevel) + " · " + UiStrings.Get(UiKeys.Lobby.Fatigue, 0, balance.MaxFatigue),
                    UiText.Passive(job),
                    UiText.StatColumns(recruit.Stats ?? StatRules.BaseOf(data, recruit.JobId), CandidateStatColumn, StatRules.BaseOf(data, recruit.JobId)),
                    UiStrings.Get(UiKeys.Lobby.Hire, balance.HirePrice),
                    Managers.Run.CanHire(i) == HireCheck.Ok);
                int index = i;
                view.Hire.onClick.AddListener(() => Hire(index));
                _candidates.Add(view);
            }

            _noCandidates.gameObject.SetActive(run.Recruits.Count == 0);
        }

        // ---- The result window (Round 72) -----------------------------------------------------

        void RefreshRunOver(StaticData data, RunState run)
        {
            if (!run.IsOver)
            {
                return;
            }

            BalanceData balance = data.Balance;
            _runOverTitle.text = RunOverTitle(run, balance);
            _runOverTitle.color = run.Outcome == RunOutcome.Victory ? UiPalette.Virtue : UiPalette.Danger;
            _runOverDay.text = UiStrings.Get(UiKeys.Lobby.DayValue, run.Day);

            var byDungeon = new List<string>();
            foreach (KeyValuePair<string, int> visits in run.ExpeditionsByDungeon)
            {
                byDungeon.Add(UiStrings.Get(UiKeys.Lobby.ExpeditionsOf, UiText.Name(data.Dungeons.Get(visits.Key).Name), visits.Value));
            }

            _runOverExpeditions.text = byDungeon.Count == 0
                ? UiStrings.Get(UiKeys.Lobby.Times, run.ExpeditionCount)
                : UiStrings.Get(UiKeys.Lobby.ExpeditionsValue, run.ExpeditionCount, string.Join(", ", byDungeon));
            _runOverFallen.text = UiStrings.Get(UiKeys.Lobby.FallenValue, run.Fallen.Count);
            var survivors = new List<string>();
            foreach (MercenaryState mercenary in run.Roster)
            {
                survivors.Add(mercenary.Id);
            }

            _runOverSurvivors.text = UiText.MercenaryList(survivors);
        }

        /// <summary>How the run ended (Docs/Design/04_Lobby_100Day_Economy.md §8): the final boss fell, or what lost it.</summary>
        static string RunOverTitle(RunState run, BalanceData balance)
        {
            if (run.Outcome == RunOutcome.Victory)
            {
                return UiStrings.Get(UiKeys.Lobby.Victory);
            }

            switch (run.DefeatCause)
            {
                case DefeatCause.DeathLimit: return UiStrings.Get(UiKeys.Lobby.DefeatDeathLimit, balance.DeathLimit);
                case DefeatCause.FinalStandLost: return UiStrings.Get(UiKeys.Lobby.DefeatFinalStand);
                default: return UiStrings.Get(UiKeys.Lobby.RunOver);
            }
        }

        // ---- Helpers and commands ------------------------------------------------------------

        /// <summary>The row the mercenary holds in the party, or 0 when it is not in the party.</summary>
        static int RowOf(RunState run, string mercenaryId)
        {
            foreach (PartySlot slot in run.Party)
            {
                if (slot.MercenaryId == mercenaryId)
                {
                    return slot.Row;
                }
            }

            return 0;
        }

        /// <summary>Removes the views of mercenaries no longer in the roster (dead or dismissed).</summary>
        static void DropGone<T>(Dictionary<string, T> views, HashSet<string> alive)
            where T : Component
        {
            var gone = new List<string>();
            foreach (string id in views.Keys)
            {
                if (!alive.Contains(id))
                {
                    gone.Add(id);
                }
            }

            foreach (string id in gone)
            {
                Destroy(views[id].gameObject);
                views.Remove(id);
            }
        }

        void Dismiss(string mercenaryId)
        {
            if (Managers.Run.CanDismiss(mercenaryId))
            {
                Managers.Run.Dismiss(mercenaryId);
                Refresh();
            }
        }

        /// <summary>
        /// Levels a mercenary up by one in the barracks (Slice D stage 35): the roll is saved with it; what it grew joins what this window already
        /// showed for the mercenary (two levels in a row add up), and the small bell of a heal rings.
        /// </summary>
        void LevelUp(string mercenaryId)
        {
            if (!Managers.Run.CanLevelUp(mercenaryId))
            {
                return;
            }

            StatBlock gains = Managers.Run.LevelUp(mercenaryId);
            _grown[mercenaryId] = _grown.TryGetValue(mercenaryId, out StatBlock before) ? before.Plus(gains) : gains;
            Managers.Sound.PlayEffect(SoundEffect.Heal);
            Refresh();
        }

        void Hire(int index)
        {
            if (Managers.Run.CanHire(index) == HireCheck.Ok)
            {
                Managers.Run.Hire(index);
                Refresh();
            }
        }

        void Treat(string mercenaryId)
        {
            if (Managers.Run.CanTreat(mercenaryId) == TreatCheck.Ok)
            {
                Managers.Run.Treat(mercenaryId);
                Refresh();
            }
        }

        void OnRest()
        {
            Managers.Run.Rest();
            Refresh();
        }

        /// <summary>Starts a building's next level (stage 30): the price leaves the gold and the scaffold goes up; the days pass with the run's days.</summary>
        void Upgrade(string buildingId)
        {
            if (Managers.Run.CanUpgrade(buildingId) == UpgradeCheck.Ok)
            {
                Managers.Run.Upgrade(buildingId);
                Refresh();
            }
        }

        /// <summary>Round 71 E2: the lobby forms the party; where to go is chosen on the expedition screen, which departs.</summary>
        void OnDepart()
        {
            DepartCheck check = Managers.Run.CanDepart(Dungeon.Id);
            if (check != DepartCheck.Ok && check != DepartCheck.PartyTooSmall)
            {
                return;
            }

            GoTo(ScreenId.Expedition);
        }

        void OnNewRun()
        {
            Managers.Run.StartNewRun();

            // The roster of the old run is gone: start the lists over.
            foreach (RosterEntryView entry in _entries.Values)
            {
                Destroy(entry.gameObject);
            }

            foreach (RosterChipView chip in _chips.Values)
            {
                Destroy(chip.gameObject);
            }

            foreach (PatientView patient in _patients.Values)
            {
                Destroy(patient.gameObject);
            }

            _entries.Clear();
            _chips.Clear();
            _patients.Clear();
            _grown.Clear();
            _open = Quarter.None;
            Refresh();
        }
    }
}
