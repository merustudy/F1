using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// A state the breakdown at the fatigue threshold brings (Docs/Design/04_Lobby_100Day_Economy.md §3): an affliction (a "mental breakdown" on the
    /// screens since stage 48) or a virtue, and what it does to the unit's battle numbers. Darkest Dungeon's afflictions refuse orders; an auto battle has
    /// no orders, so a state moves the numbers instead: how fast the unit's items cycle, how much healing it takes, how likely a hit at death's door kills
    /// it and, stage 48, its hit, dodge and critical chances, the damage it deals, the fatigue it gains and the fatigue its allies take when a battle starts.
    /// </summary>
    public sealed class FatigueStateData
    {
        public const string DefinitionName = "FatigueState";

        /// <param name="cooldownPercent">Change to the unit's item cooldowns, in percent: 25 is a quarter slower, -20 a fifth faster. 0 in every shipped state since stage 48 (the user's "일단 동작 안되도록").</param>
        /// <param name="healTakenPercent">Change to the healing the unit receives, in percent.</param>
        /// <param name="deathChanceDelta">Added to the death chance of a hit at death's door, in percentage points.</param>
        /// <param name="hitPercent">Stage 48: added to the unit's hit chance, in percentage points.</param>
        /// <param name="dodgePercent">Stage 48: added to the unit's dodge chance, in percentage points.</param>
        /// <param name="critPercent">Stage 48: added to the critical chance of the unit's weapons, in percentage points.</param>
        /// <param name="damagePercent">Stage 48: change to the damage the unit's items deal to the other side, in percent.</param>
        /// <param name="fatigueGainPercent">Stage 48: change to the fatigue the unit gains, in percent, added to its endurance's and traits'.</param>
        /// <param name="allyFatigueAtStart">Stage 48: the fatigue every living ally gains (or, negative, sheds) when a battle starts.</param>
        [JsonConstructor]
        public FatigueStateData(
            string id, LocalizedText name, FatigueStateKind kind, int cooldownPercent, int healTakenPercent, int deathChanceDelta, LocalizedText description,
            int hitPercent = 0, int dodgePercent = 0, int critPercent = 0, int damagePercent = 0, int fatigueGainPercent = 0, int allyFatigueAtStart = 0)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            Description = description ?? throw new DataException($"{DefinitionName} '{id}': Description is missing.");
            Within(id, nameof(CooldownPercent), cooldownPercent, -50, 100);
            Within(id, nameof(HealTakenPercent), healTakenPercent, -100, 200);
            Within(id, nameof(DeathChanceDelta), deathChanceDelta, -100, 100);
            Within(id, nameof(HitPercent), hitPercent, -100, 100);
            Within(id, nameof(DodgePercent), dodgePercent, -100, 100);
            Within(id, nameof(CritPercent), critPercent, -100, 100);
            Within(id, nameof(DamagePercent), damagePercent, -100, 200);
            Within(id, nameof(FatigueGainPercent), fatigueGainPercent, -100, 200);
            Within(id, nameof(AllyFatigueAtStart), allyFatigueAtStart, -100, 100);
            if (cooldownPercent == 0 && healTakenPercent == 0 && deathChanceDelta == 0 && hitPercent == 0 && dodgePercent == 0 && critPercent == 0
                && damagePercent == 0 && fatigueGainPercent == 0 && allyFatigueAtStart == 0)
            {
                throw new DataException($"{DefinitionName} '{id}': a state must change something.");
            }

            Kind = kind;
            CooldownPercent = cooldownPercent;
            HealTakenPercent = healTakenPercent;
            DeathChanceDelta = deathChanceDelta;
            HitPercent = hitPercent;
            DodgePercent = dodgePercent;
            CritPercent = critPercent;
            DamagePercent = damagePercent;
            FatigueGainPercent = fatigueGainPercent;
            AllyFatigueAtStart = allyFatigueAtStart;
        }

        static void Within(string id, string column, int value, int min, int max)
        {
            if (value < min || value > max)
            {
                throw new DataException($"{DefinitionName} '{id}': {column} must be within {min}..{max}.");
            }
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public FatigueStateKind Kind { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int CooldownPercent { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int HealTakenPercent { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int DeathChanceDelta { get; }

        [JsonProperty(Order = 7, Required = Required.Always)]
        public int HitPercent { get; }

        [JsonProperty(Order = 8, Required = Required.Always)]
        public int DodgePercent { get; }

        [JsonProperty(Order = 9, Required = Required.Always)]
        public int CritPercent { get; }

        [JsonProperty(Order = 10, Required = Required.Always)]
        public int DamagePercent { get; }

        [JsonProperty(Order = 11, Required = Required.Always)]
        public int FatigueGainPercent { get; }

        [JsonProperty(Order = 12, Required = Required.Always)]
        public int AllyFatigueAtStart { get; }

        /// <summary>What the state does, in words for the screens.</summary>
        [JsonProperty(Order = 13, Required = Required.Always)]
        public LocalizedText Description { get; }
    }
}
