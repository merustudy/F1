using System;
using System.Collections.Generic;
using System.Linq;

namespace F1.Data
{
    /// <summary>The definition lists that make up <see cref="StaticData"/>.</summary>
    public sealed class StaticDataParts
    {
        public IEnumerable<BalanceEntry> Balance;
        public IEnumerable<JobData> Jobs;
        public IEnumerable<ItemData> Items;
        public IEnumerable<BagData> Bags;
        public IEnumerable<PotionData> Potions;
        public IEnumerable<EnemyData> Enemies;
        public IEnumerable<EnemyGroupData> EnemyGroups;
        public IEnumerable<AffinityData> Affinities;
        public IEnumerable<DungeonData> Dungeons;
        public IEnumerable<MercenaryData> Mercenaries;
        public IEnumerable<FatigueStateData> FatigueStates;
        public IEnumerable<RecipeData> Recipes;
        public IEnumerable<LevelData> Levels;
        public IEnumerable<BuildingData> Buildings;

        /// <summary>The mercenaries' traits (Slice D stage 37). Null is none (test data).</summary>
        public IEnumerable<TraitData> Traits;
    }

    /// <summary>
    /// A definition table: lookup by id, and iteration in id order. Rules always iterate in id order
    /// so that results never depend on dictionary ordering.
    /// </summary>
    public sealed class DataTable<T>
    {
        readonly Dictionary<string, T> _byId;
        readonly string _definition;

        internal DataTable(string definition, Dictionary<string, T> byId, List<T> ordered)
        {
            _definition = definition;
            _byId = byId;
            Ordered = ordered;
        }

        /// <summary>Every definition, sorted by id (ordinal).</summary>
        public IReadOnlyList<T> Ordered { get; }

        public int Count => Ordered.Count;

        public bool Contains(string id)
        {
            return id != null && _byId.ContainsKey(id);
        }

        /// <summary>Throws when the id is unknown. There is no silent default.</summary>
        public T Get(string id)
        {
            if (id != null && _byId.TryGetValue(id, out T value))
            {
                return value;
            }

            throw new KeyNotFoundException($"{_definition} '{id}' does not exist.");
        }
    }

    /// <summary>
    /// All static data, validated as a whole. Both the game and the simulator build it from the same
    /// generated JSON through <see cref="StaticDataLoader"/>.
    /// </summary>
    public sealed class StaticData
    {
        public StaticData(StaticDataParts parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            var problems = new List<string>();

            try
            {
                Balance = new BalanceData(parts.Balance);
            }
            catch (DataException exception)
            {
                problems.Add(exception.Message);
            }

            Jobs = Index(parts.Jobs, x => x.Id, JobData.DefinitionName, problems);
            Items = Index(parts.Items, x => x.Id, ItemData.DefinitionName, problems);
            Bags = Index(parts.Bags, x => x.Id, BagData.DefinitionName, problems);
            Potions = Index(parts.Potions, x => x.Id, PotionData.DefinitionName, problems);
            Enemies = Index(parts.Enemies, x => x.Id, EnemyData.DefinitionName, problems);
            EnemyGroups = Index(parts.EnemyGroups, x => x.Id, EnemyGroupData.DefinitionName, problems);
            Affinities = Index(parts.Affinities, x => x.Id, AffinityData.DefinitionName, problems);
            Dungeons = Index(parts.Dungeons, x => x.Id, DungeonData.DefinitionName, problems);
            Mercenaries = Index(parts.Mercenaries, x => x.Id, MercenaryData.DefinitionName, problems);
            FatigueStates = Index(parts.FatigueStates, x => x.Id, FatigueStateData.DefinitionName, problems);
            Recipes = Index(parts.Recipes ?? new List<RecipeData>(), x => x.Id, RecipeData.DefinitionName, problems);
            Levels = LevelLadder(parts.Levels, problems);
            Buildings = BuildingLadders(parts.Buildings, problems);
            Traits = Index(parts.Traits ?? new List<TraitData>(), x => x.Id, TraitData.DefinitionName, problems);

            if (Balance != null)
            {
                CheckReferences(problems);
            }

            if (problems.Count > 0)
            {
                throw new DataValidationException(problems);
            }
        }

        public BalanceData Balance { get; }
        public DataTable<JobData> Jobs { get; }
        public DataTable<ItemData> Items { get; }

        /// <summary>The bags (Slice B stage 19). Exactly one is the start bag (<see cref="StartBag"/>).</summary>
        public DataTable<BagData> Bags { get; }

        /// <summary>The bag every mercenary leaves on an expedition with.</summary>
        public BagData StartBag => Bags.Ordered.First(bag => bag.Start);
        public DataTable<PotionData> Potions { get; }
        public DataTable<EnemyData> Enemies { get; }
        public DataTable<EnemyGroupData> EnemyGroups { get; }
        public DataTable<AffinityData> Affinities { get; }
        public DataTable<DungeonData> Dungeons { get; }
        public DataTable<MercenaryData> Mercenaries { get; }

        /// <summary>The starting roster, in data order: the rows of MercenaryData.csv that are Starting.</summary>
        public List<MercenaryData> StartingMercenaries => Mercenaries.Ordered.Where(m => m.Starting).ToList();

        /// <summary>The names the stagecoach draws candidates from (Slice C stage 26): the rows that are not Starting.</summary>
        public List<MercenaryData> RecruitPool => Mercenaries.Ordered.Where(m => !m.Starting).ToList();

        /// <summary>The forge recipes (Slice B stage 23); empty until a dungeon has one.</summary>
        public DataTable<RecipeData> Recipes { get; }

        /// <summary>The levels in order from 1 (Slice C stage 27): what each takes to leave and what it adds to the maximum HP.</summary>
        public IReadOnlyList<LevelData> Levels { get; }

        /// <summary>The last level a mercenary can reach.</summary>
        public int MaxLevel => Levels.Count;

        /// <summary>The levels of the town's buildings (Slice C stage 30), every building of <see cref="BuildingId.All"/> from level 1 up, in id then level order.</summary>
        public IReadOnlyList<BuildingData> Buildings { get; }

        /// <summary>The last level a building can reach.</summary>
        public int MaxBuildingLevel(string buildingId)
        {
            int max = 0;
            foreach (BuildingData row in Buildings)
            {
                if (row.Id == buildingId)
                {
                    max = Math.Max(max, row.Level);
                }
            }

            if (max == 0)
            {
                throw new KeyNotFoundException($"{BuildingData.DefinitionName} '{buildingId}' does not exist.");
            }

            return max;
        }

        /// <summary>The row of a building's level, 1 being the start.</summary>
        public BuildingData BuildingAt(string buildingId, int level)
        {
            foreach (BuildingData row in Buildings)
            {
                if (row.Id == buildingId && row.Level == level)
                {
                    return row;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(level), level, $"{BuildingData.DefinitionName} '{buildingId}' has no level {level}.");
        }

        /// <summary>The row of a level, 1 being the first.</summary>
        public LevelData LevelAt(int level)
        {
            if (level < 1 || level > Levels.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Levels run from 1 to {Levels.Count}.");
            }

            return Levels[level - 1];
        }

        /// <summary>
        /// The buildings' levels (stage 30), checked as a ladder per building: every building of the town has its rows, they run from 1 without
        /// a gap or a repeat, and the bonus never falls from one level to the next (the row's constructor checked the first level's zeros).
        /// </summary>
        static IReadOnlyList<BuildingData> BuildingLadders(IEnumerable<BuildingData> rows, List<string> problems)
        {
            var all = new List<BuildingData>();
            if (rows == null)
            {
                problems.Add($"{BuildingData.DefinitionName}: no data.");
                return all;
            }

            foreach (BuildingData row in rows)
            {
                if (row == null)
                {
                    problems.Add($"{BuildingData.DefinitionName}: a row is missing.");
                    return all;
                }

                all.Add(row);
            }

            all.Sort((a, b) => a.Id != b.Id ? string.CompareOrdinal(a.Id, b.Id) : a.Level.CompareTo(b.Level));
            foreach (string id in BuildingId.All)
            {
                List<BuildingData> ladder = all.FindAll(row => row.Id == id);
                if (ladder.Count == 0)
                {
                    problems.Add($"{BuildingData.DefinitionName}: '{id}' has no level (every building of the town needs its level 1).");
                    continue;
                }

                for (int i = 0; i < ladder.Count; i++)
                {
                    if (ladder[i].Level != i + 1)
                    {
                        problems.Add($"{BuildingData.DefinitionName} {id}: the levels must run from 1 without a gap or a repeat (found {ladder[i].Level} where {i + 1} was expected).");
                        break;
                    }

                    if (i > 0 && ladder[i].Bonus < ladder[i - 1].Bonus)
                    {
                        problems.Add($"{BuildingData.DefinitionName} {id} {ladder[i].Level}: Bonus cannot be under the level below it.");
                    }
                }
            }

            return all;
        }

        /// <summary>
        /// The levels in order, checked as a ladder: at least one, running from 1 without a gap, each but the last taking some experience to leave
        /// and the last none, the first adding nothing to the HP and no level adding less than the one under it.
        /// </summary>
        static IReadOnlyList<LevelData> LevelLadder(IEnumerable<LevelData> rows, List<string> problems)
        {
            var levels = new List<LevelData>();
            if (rows == null)
            {
                problems.Add($"{LevelData.DefinitionName}: no data.");
                return levels;
            }

            foreach (LevelData row in rows)
            {
                if (row == null)
                {
                    problems.Add($"{LevelData.DefinitionName}: a row is missing.");
                    return levels;
                }

                levels.Add(row);
            }

            levels.Sort((a, b) => a.Level.CompareTo(b.Level));
            if (levels.Count == 0)
            {
                problems.Add($"{LevelData.DefinitionName}: at least one level is required.");
                return levels;
            }

            for (int i = 0; i < levels.Count; i++)
            {
                LevelData level = levels[i];
                bool last = i == levels.Count - 1;
                if (level.Level != i + 1)
                {
                    problems.Add($"{LevelData.DefinitionName}: the levels must run from 1 without a gap or a repeat (found {level.Level} where {i + 1} was expected).");
                    return levels;
                }

                if (last != level.IsLast)
                {
                    problems.Add($"{LevelData.DefinitionName} {level.Level}: ExpToNext must be 0 at the last level and above 0 before it.");
                }

            }

            return levels;
        }
        public DataTable<FatigueStateData> FatigueStates { get; }

        /// <summary>The mercenaries' traits (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6), in id order.</summary>
        public DataTable<TraitData> Traits { get; }

        /// <summary>The traits a candidate of the stagecoach can be born with: the innate ones of a weight above 0, in id order.</summary>
        public List<TraitData> InnateTraits => Traits.Ordered.Where(trait => trait.Kind == TraitKind.Innate && trait.Weight > 0).ToList();

        /// <summary>The states of a kind, in id order: what the breakdown at the fatigue threshold picks from.</summary>
        public List<FatigueStateData> FatigueStatesOf(FatigueStateKind kind)
        {
            return FatigueStates.Ordered.Where(state => state.Kind == kind).ToList();
        }

        /// <summary>The groups a battle node of a floor (1-based) of a dungeon can hold, in id order: neither a boss nor an elite group.</summary>
        public List<EnemyGroupData> GroupsFor(string dungeonId, int floor)
        {
            return EnemyGroups.Ordered
                .Where(group => group.DungeonId == dungeonId && !group.IsBoss && !group.IsElite && group.MinFloor <= floor && floor <= group.MaxFloor)
                .ToList();
        }

        /// <summary>The groups an elite node of a floor can hold.</summary>
        public List<EnemyGroupData> ElitesFor(string dungeonId, int floor)
        {
            return EnemyGroups.Ordered
                .Where(group => group.DungeonId == dungeonId && group.IsElite && group.MinFloor <= floor && floor <= group.MaxFloor)
                .ToList();
        }

        /// <summary>The dungeon that ends the run (Slice C stage 25), or null when the data has none.</summary>
        public DungeonData FinalDungeon => Dungeons.Ordered.FirstOrDefault(dungeon => dungeon.IsFinal);

        public EnemyGroupData BossGroupOf(string dungeonId)
        {
            return EnemyGroups.Ordered.First(group => group.DungeonId == dungeonId && group.IsBoss);
        }

        /// <summary>Whether an enemy's items fit its board: the start bag, as many rows taller as they need, within the frame (Round 86).</summary>
        void CheckEnemyBag(IReadOnlyList<ItemGrant> grants, string what, bool hasStartBag, List<string> problems)
        {
            if (!hasStartBag)
            {
                return;
            }

            var sizes = new List<(int Width, int Height)>();
            foreach (ItemGrant grant in grants)
            {
                if (Items.Contains(grant.ItemId))
                {
                    ItemData item = Items.Get(grant.ItemId);
                    sizes.Add((item.Width, item.Height));
                }
            }

            int rows = BoardFrame.Pack(sizes, StartBag.Width, StartBag.Height, out _);
            if (rows < 0 || rows > BoardFrame.Height)
            {
                problems.Add($"{what} need {(rows < 0 ? "a wider bag than" : rows + " rows of")} the start bag '{StartBag.Id}' ({StartBag.Width} wide), more than a board frame ({BoardFrame.Height}).");
            }
        }

        void CheckReferences(List<string> problems)
        {
            int startBags = Bags.Ordered.Count(bag => bag.Start);
            if (startBags != 1)
            {
                problems.Add($"{BagData.DefinitionName}: exactly one bag is the start bag, not {startBags}.");
            }

            foreach (JobData job in Jobs.Ordered)
            {
                string what = $"{JobData.DefinitionName} '{job.Id}'";
                Require(Items, job.WeaponItemId, what + " WeaponItemId", problems);
                Stat? outside = job.Stats?.FirstOutside(Balance.StatMin, Balance.StatMax);
                if (outside != null)
                {
                    problems.Add($"{what}: {outside} = {job.Stats[outside.Value]} is outside StatMin..StatMax ({Balance.StatMin}..{Balance.StatMax}).");
                }

                // The weapon lies unturned at the top-left of the start bag when the expedition leaves.
                if (startBags == 1 && Items.Contains(job.WeaponItemId))
                {
                    ItemData weapon = Items.Get(job.WeaponItemId);
                    if (weapon.Width > StartBag.Width || weapon.Height > StartBag.Height)
                    {
                        problems.Add($"{what}: the weapon '{weapon.Id}' ({weapon.Width}x{weapon.Height}) does not fit the start bag '{StartBag.Id}' ({StartBag.Width}x{StartBag.Height}).");
                    }
                }
            }

            foreach (RecipeData recipe in Recipes.Ordered)
            {
                string what = $"{RecipeData.DefinitionName} '{recipe.Id}'";
                Require(Items, recipe.Result, what + " Result", problems);
                Require(Items, recipe.BaseItem, what + " BaseItem", problems);
                Require(Items, recipe.Catalyst, what + " Catalyst", problems);
                if (Items.Contains(recipe.Result) && !Items.Get(recipe.Result).Unique)
                {
                    problems.Add($"{what}: the result '{recipe.Result}' is not a unique item.");
                }

                if (Items.Contains(recipe.Catalyst) && Items.Get(recipe.Catalyst).Stars.Count == 0)
                {
                    problems.Add($"{what}: the catalyst '{recipe.Catalyst}' has no stars to be lit on the base.");
                }
            }

            // The enemies of the final dungeon's boss group (stage 28), which may carry the final boss's level.
            var finalBosses = new HashSet<string>(StringComparer.Ordinal);
            foreach (EnemyGroupData group in EnemyGroups.Ordered)
            {
                if (group.IsBoss && Dungeons.Contains(group.DungeonId) && Dungeons.Get(group.DungeonId).IsFinal)
                {
                    finalBosses.UnionWith(group.Enemies);
                }
            }

            foreach (EnemyData enemy in Enemies.Ordered)
            {
                string what = $"{EnemyData.DefinitionName} '{enemy.Id}'";
                foreach (ItemGrant grant in enemy.Items)
                {
                    Require(Items, grant.ItemId, what + " Items", problems);
                }

                // An enemy's board is the start bag, a row taller whenever its items need it (2026-10-19 Round 86), within the frame.
                CheckEnemyBag(enemy.Items, what + ": its items", startBags == 1, problems);

                // FinalBossLevel is the final boss's alone (stage 28): an enemy may carry it only in the final dungeon's boss group, and nothing is above it.
                if (enemy.Level > Balance.FinalBossLevel || (enemy.Level == Balance.FinalBossLevel && !finalBosses.Contains(enemy.Id)))
                {
                    problems.Add($"{what}: level {enemy.Level} is reserved for the final boss (FinalBossLevel {Balance.FinalBossLevel}).");
                }

                foreach (EnemyPhase phase in enemy.Phases)
                {
                    foreach (ItemGrant grant in phase.Items)
                    {
                        Require(Items, grant.ItemId, $"{what} Phases", problems);
                    }

                    CheckEnemyBag(phase.Items, $"{what}: the items of the phase at {phase.HpPercent}%", startBags == 1, problems);
                }

                // The deep item bundles (2026-10-20, Design/03 §4): their items exist, and what the enemy carries from each bundle's floor — the base
                // items, and each phase's, with the bundles down to that floor — still fits the frame.
                foreach (EnemyDepthItems bundle in enemy.DeepItems)
                {
                    foreach (ItemGrant grant in bundle.Items)
                    {
                        Require(Items, grant.ItemId, $"{what} DeepItems", problems);
                    }

                    CheckEnemyBag(enemy.ItemsAt(bundle.Floor), $"{what}: its items from floor {bundle.Floor}", startBags == 1, problems);
                    foreach (EnemyPhase phase in enemy.Phases)
                    {
                        CheckEnemyBag(enemy.ItemsAt(phase, bundle.Floor), $"{what}: the items of the phase at {phase.HpPercent}% from floor {bundle.Floor}", startBags == 1, problems);
                    }
                }
            }

            foreach (EnemyGroupData group in EnemyGroups.Ordered)
            {
                string what = $"{EnemyGroupData.DefinitionName} '{group.Id}'";
                Require(Dungeons, group.DungeonId, what + " DungeonId", problems);
                foreach (string enemyId in group.Enemies)
                {
                    Require(Enemies, enemyId, what + " enemies", problems);
                }
            }

            foreach (DungeonData dungeon in Dungeons.Ordered)
            {
                string what = $"{DungeonData.DefinitionName} '{dungeon.Id}'";
                Require(Affinities, dungeon.AffinityId, what + " AffinityId", problems);
                foreach (string potionId in dungeon.StartingPotions)
                {
                    Require(Potions, potionId, what + " StartingPotions", problems);
                }

                if (dungeon.StartingPotions.Count > Balance.PotionSlots)
                {
                    problems.Add($"{what}: more starting potions than PotionSlots ({Balance.PotionSlots}).");
                }

                int bossGroups = EnemyGroups.Ordered.Count(group => group.DungeonId == dungeon.Id && group.IsBoss);
                if (bossGroups != 1)
                {
                    problems.Add($"{what}: needs exactly one boss group, found {bossGroups}.");
                }

                if (dungeon.UnlockDungeonId != null)
                {
                    Require(Dungeons, dungeon.UnlockDungeonId, $"{what} UnlockDungeonId", problems);
                }

                // Every floor that can have a battle node needs a group for it, and every floor that can have an elite node an elite group.
                for (int floor = 1; floor <= dungeon.Floors; floor++)
                {
                    if (floor != dungeon.CampFloor && floor != dungeon.ShopFloor && GroupsFor(dungeon.Id, floor).Count == 0)
                    {
                        problems.Add($"{what}: no enemy group can appear on floor {floor}.");
                    }

                    if (dungeon.EliteCanStandOn(floor) && ElitesFor(dungeon.Id, floor).Count == 0)
                    {
                        problems.Add($"{what}: an elite node can stand on floor {floor} but no elite group can appear there.");
                    }
                }
            }

            if (Dungeons.Ordered.Count(dungeon => dungeon.IsFinal) > 1)
            {
                problems.Add($"{DungeonData.DefinitionName}: at most one dungeon is final.");
            }

            foreach (TraitData trait in Traits.Ordered)
            {
                if (trait.Excludes != null)
                {
                    Require(Traits, trait.Excludes, $"{TraitData.DefinitionName} '{trait.Id}' Excludes", problems);
                }
            }

            foreach (MercenaryData mercenary in StartingMercenaries)
            {
                Require(Jobs, mercenary.JobId, $"{MercenaryData.DefinitionName} '{mercenary.Id}' JobId", problems);
                // The starting roster's traits (stage 37): each exists, none twice, no two that clash.
                var held = new List<TraitData>();
                foreach (string traitId in mercenary.Traits)
                {
                    string what = $"{MercenaryData.DefinitionName} '{mercenary.Id}' Traits";
                    if (!Traits.Contains(traitId))
                    {
                        problems.Add($"{what}: '{traitId}' does not exist.");
                        continue;
                    }

                    TraitData trait = Traits.Get(traitId);
                    if (held.Contains(trait))
                    {
                        problems.Add($"{what}: '{traitId}' is there twice.");
                    }
                    else if (held.Any(other => other.Clashes(trait)))
                    {
                        problems.Add($"{what}: '{traitId}' clashes with another of its traits.");
                    }

                    held.Add(trait);
                }
            }

            if (StartingMercenaries.Count < Balance.MinPartySize)
            {
                problems.Add($"{MercenaryData.DefinitionName}: the starting roster is smaller than MinPartySize.");
            }

            if (StartingMercenaries.Count > Balance.RosterLimit)
            {
                problems.Add($"{MercenaryData.DefinitionName}: the starting roster is larger than RosterLimit.");
            }

            // The stagecoach draws RecruitCount names at a time (Slice C stage 26): the pool must hold at least that many.
            if (RecruitPool.Count < Balance.RecruitCount)
            {
                problems.Add($"{MercenaryData.DefinitionName}: the recruit pool ({RecruitPool.Count} names) is smaller than RecruitCount.");
            }

            if (Dungeons.Count == 0)
            {
                problems.Add($"{DungeonData.DefinitionName}: at least one dungeon is required.");
            }

            // The breakdown picks an affliction or a virtue: there must be one of each to pick.
            if (FatigueStatesOf(FatigueStateKind.Affliction).Count == 0 || FatigueStatesOf(FatigueStateKind.Virtue).Count == 0)
            {
                problems.Add($"{FatigueStateData.DefinitionName}: at least one affliction and one virtue are required.");
            }

            if (!Items.Ordered.Any(item => item.ShopWeight > 0) && !Potions.Ordered.Any(potion => potion.ShopWeight > 0))
            {
                problems.Add("No item or potion has a ShopWeight above 0, so no shop could stock anything.");
            }
        }

        static void Require<T>(DataTable<T> table, string id, string what, List<string> problems)
        {
            if (!table.Contains(id))
            {
                problems.Add($"{what}: '{id}' does not exist.");
            }
        }

        static DataTable<T> Index<T>(IEnumerable<T> items, Func<T, string> idOf, string definition, List<string> problems)
        {
            var byId = new Dictionary<string, T>(StringComparer.Ordinal);
            if (items == null)
            {
                problems.Add($"{definition}: no data.");
            }
            else
            {
                foreach (T item in items)
                {
                    string id = idOf(item);
                    if (byId.ContainsKey(id))
                    {
                        problems.Add($"{definition}: duplicate id '{id}'.");
                        continue;
                    }

                    byId.Add(id, item);
                }
            }

            List<T> ordered = byId.Values.OrderBy(idOf, StringComparer.Ordinal).ToList();
            return new DataTable<T>(definition, byId, ordered);
        }
    }
}
