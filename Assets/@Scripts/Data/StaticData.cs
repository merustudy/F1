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
        public IEnumerable<PotionData> Potions;
        public IEnumerable<EnemyData> Enemies;
        public IEnumerable<EnemyGroupData> EnemyGroups;
        public IEnumerable<AffinityData> Affinities;
        public IEnumerable<DungeonData> Dungeons;
        public IEnumerable<MercenaryData> Mercenaries;
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
            Potions = Index(parts.Potions, x => x.Id, PotionData.DefinitionName, problems);
            Enemies = Index(parts.Enemies, x => x.Id, EnemyData.DefinitionName, problems);
            EnemyGroups = Index(parts.EnemyGroups, x => x.Id, EnemyGroupData.DefinitionName, problems);
            Affinities = Index(parts.Affinities, x => x.Id, AffinityData.DefinitionName, problems);
            Dungeons = Index(parts.Dungeons, x => x.Id, DungeonData.DefinitionName, problems);
            Mercenaries = Index(parts.Mercenaries, x => x.Id, MercenaryData.DefinitionName, problems);

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
        public DataTable<PotionData> Potions { get; }
        public DataTable<EnemyData> Enemies { get; }
        public DataTable<EnemyGroupData> EnemyGroups { get; }
        public DataTable<AffinityData> Affinities { get; }
        public DataTable<DungeonData> Dungeons { get; }
        public DataTable<MercenaryData> Mercenaries { get; }

        /// <summary>Non-boss groups that can appear on a battle floor (1-based) of a dungeon, in id order.</summary>
        public List<EnemyGroupData> GroupsFor(string dungeonId, int floor)
        {
            return EnemyGroups.Ordered
                .Where(group => group.DungeonId == dungeonId && !group.IsBoss && group.MinFloor <= floor && floor <= group.MaxFloor)
                .ToList();
        }

        public EnemyGroupData BossGroupOf(string dungeonId)
        {
            return EnemyGroups.Ordered.First(group => group.DungeonId == dungeonId && group.IsBoss);
        }

        void CheckReferences(List<string> problems)
        {
            foreach (JobData job in Jobs.Ordered)
            {
                string what = $"{JobData.DefinitionName} '{job.Id}'";
                Require(Items, job.WeaponItemId, what + " WeaponItemId", problems);
                if (Items.Contains(job.WeaponItemId) && Items.Get(job.WeaponItemId).Size > job.ItemSlots)
                {
                    problems.Add($"{what}: the weapon '{job.WeaponItemId}' takes {Items.Get(job.WeaponItemId).Size} cells but the board has {job.ItemSlots}.");
                }
            }

            foreach (EnemyData enemy in Enemies.Ordered)
            {
                string what = $"{EnemyData.DefinitionName} '{enemy.Id}'";
                int cells = 0;
                foreach (ItemGrant grant in enemy.Items)
                {
                    Require(Items, grant.ItemId, what + " Items", problems);
                    if (Items.Contains(grant.ItemId))
                    {
                        cells += Items.Get(grant.ItemId).Size;
                    }
                }

                // The battle screen draws every board at most MaxItemSlots cells tall.
                if (cells > JobData.MaxItemSlots)
                {
                    problems.Add($"{what}: its items take {cells} cells, more than a board can have ({JobData.MaxItemSlots}).");
                }

                if (enemy.Level >= Balance.FinalBossLevel)
                {
                    problems.Add($"{what}: level {enemy.Level} is reserved for the final boss (FinalBossLevel {Balance.FinalBossLevel}).");
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

                for (int floor = 1; floor <= dungeon.Floors; floor++)
                {
                    if (GroupsFor(dungeon.Id, floor).Count == 0)
                    {
                        problems.Add($"{what}: no enemy group can appear on floor {floor}.");
                    }
                }
            }

            foreach (MercenaryData mercenary in Mercenaries.Ordered)
            {
                Require(Jobs, mercenary.JobId, $"{MercenaryData.DefinitionName} '{mercenary.Id}' JobId", problems);
            }

            if (Mercenaries.Count < Balance.MinPartySize)
            {
                problems.Add($"{MercenaryData.DefinitionName}: the starting roster is smaller than MinPartySize.");
            }

            if (Dungeons.Count == 0)
            {
                problems.Add($"{DungeonData.DefinitionName}: at least one dungeon is required.");
            }

            if (!Items.Ordered.Any(item => item.RewardWeight > 0) && !Potions.Ordered.Any(potion => potion.RewardWeight > 0))
            {
                problems.Add("No item or potion has a RewardWeight above 0, so battles could offer no reward.");
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
