using System.Globalization;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>The end of a side's line that a span is counted from.</summary>
    public enum RowEnd
    {
        Front,
        Back,
    }

    /// <summary>
    /// A stretch of a side's line counted from one end: "the front N rows" or "the rear N rows"
    /// (Docs/Design/02_Combat_System.md §4). It says where an item can be used and where a passive
    /// applies. The living always stand in rows 1..n with no gap, so the front N are rows 1..N and
    /// the rear N are rows n-N+1..n; a line shorter than N is covered whole.
    /// In data it is written "front:N", "back:N" or "all".
    /// </summary>
    public sealed class RowSpan
    {
        public const string Everywhere = "all";
        const string FrontWord = "front";
        const string BackWord = "back";

        [JsonConstructor]
        public RowSpan(RowEnd from, int reach)
        {
            if (reach < 1 || reach > BattleRows.Count)
            {
                throw new DataException($"A row span reaches 1..{BattleRows.Count} rows, not {reach}.");
            }

            From = from;
            Reach = reach;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public RowEnd From { get; }

        /// <summary>How many rows are covered, counted from <see cref="From"/>.</summary>
        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Reach { get; }

        /// <summary>Every row of a side, however long its line is.</summary>
        public static RowSpan All => new RowSpan(RowEnd.Front, BattleRows.Count);

        public static RowSpan Front(int reach)
        {
            return new RowSpan(RowEnd.Front, reach);
        }

        public static RowSpan Back(int reach)
        {
            return new RowSpan(RowEnd.Back, reach);
        }

        /// <summary>True when the span covers a whole line of any length.</summary>
        [JsonIgnore]
        public bool IsEveryRow => Reach >= BattleRows.Count;

        /// <param name="row">The row a unit stands in.</param>
        /// <param name="lineLength">How many units of that side are alive; they stand in rows 1..lineLength.</param>
        public bool Contains(int row, int lineLength)
        {
            return From == RowEnd.Front ? row <= Reach : row > lineLength - Reach;
        }

        /// <summary>Reads "front:N", "back:N" or "all". False for anything else.</summary>
        public static bool TryParse(string text, out RowSpan span)
        {
            span = null;
            if (text == Everywhere)
            {
                span = All;
                return true;
            }

            string[] parts = text.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            RowEnd from;
            switch (parts[0])
            {
                case FrontWord: from = RowEnd.Front; break;
                case BackWord: from = RowEnd.Back; break;
                default: return false;
            }

            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int reach) || reach < 1 || reach > BattleRows.Count)
            {
                return false;
            }

            span = new RowSpan(from, reach);
            return true;
        }

        public override string ToString()
        {
            if (From == RowEnd.Front && IsEveryRow)
            {
                return Everywhere;
            }

            return (From == RowEnd.Front ? FrontWord : BackWord) + ":" + Reach.ToString(CultureInfo.InvariantCulture);
        }
    }
}
