using System;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// The six stats (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2): a job's base values (<see cref="JobData.Stats"/>) and a
    /// mercenary's own. Whole numbers; whether they lie within the balance's StatMin..StatMax is checked where the balance is known (the
    /// dataset, the save). Immutable.
    /// </summary>
    public sealed class StatBlock : IEquatable<StatBlock>
    {
        /// <summary>Every stat, in the order the screens list them.</summary>
        public static readonly Stat[] All = { Stat.Str, Stat.Dex, Stat.Con, Stat.Int, Stat.Wis, Stat.End };

        [JsonConstructor]
        public StatBlock(int str, int dex, int con, int @int, int wis, int end)
        {
            Str = str;
            Dex = dex;
            Con = con;
            Int = @int;
            Wis = wis;
            End = end;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public int Str { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Dex { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Con { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Int { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int Wis { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int End { get; }

        public int this[Stat stat]
        {
            get
            {
                switch (stat)
                {
                    case Stat.Str: return Str;
                    case Stat.Dex: return Dex;
                    case Stat.Con: return Con;
                    case Stat.Int: return Int;
                    case Stat.Wis: return Wis;
                    case Stat.End: return End;
                    default: throw new ArgumentOutOfRangeException(nameof(stat), stat, "Not a stat.");
                }
            }
        }

        /// <summary>Every stat at one value: the baseline of a job that names none.</summary>
        public static StatBlock Uniform(int value)
        {
            return new StatBlock(value, value, value, value, value, value);
        }

        /// <summary>The same stats with one of them at another value.</summary>
        public StatBlock With(Stat stat, int value)
        {
            return new StatBlock(
                stat == Stat.Str ? value : Str,
                stat == Stat.Dex ? value : Dex,
                stat == Stat.Con ? value : Con,
                stat == Stat.Int ? value : Int,
                stat == Stat.Wis ? value : Wis,
                stat == Stat.End ? value : End);
        }

        /// <summary>Each stat added to the other's (Slice D stage 35: two level-ups' gains shown together).</summary>
        public StatBlock Plus(StatBlock other)
        {
            return new StatBlock(Str + other.Str, Dex + other.Dex, Con + other.Con, Int + other.Int, Wis + other.Wis, End + other.End);
        }

        /// <summary>The first stat outside min..max, or null when all lie within.</summary>
        public Stat? FirstOutside(int min, int max)
        {
            foreach (Stat stat in All)
            {
                if (this[stat] < min || this[stat] > max)
                {
                    return stat;
                }
            }

            return null;
        }

        public bool Equals(StatBlock other)
        {
            return other != null && Str == other.Str && Dex == other.Dex && Con == other.Con && Int == other.Int && Wis == other.Wis && End == other.End;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StatBlock);
        }

        public override int GetHashCode()
        {
            return ((((Str * 31 + Dex) * 31 + Con) * 31 + Int) * 31 + Wis) * 31 + End;
        }

        public override string ToString()
        {
            return $"{Str}/{Dex}/{Con}/{Int}/{Wis}/{End}";
        }
    }
}
