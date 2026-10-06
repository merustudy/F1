namespace F1.Data
{
    /// <summary>
    /// The value of a definition's art column: the Figure of a job or an enemy, the Background of
    /// a dungeon, the Icon of an item. Data carries it as written; whether it is an address of an
    /// asset that exists is checked where the assets are (the Editor setup).
    /// </summary>
    public static class ArtAddress
    {
        /// <summary>The pose a mercenary's figure is swapped for while its weapon's lunge plays (Docs/Design/10 §5).</summary>
        public const string Attack = "attack";

        /// <summary>The pose a mercenary's figure is swapped for while a blow from a unit knocks it back.</summary>
        public const string Hit = "hit";

        /// <summary>
        /// The pose a mercenary's figure holds through the moment of a breakdown at the fatigue threshold that brought an affliction,
        /// and of the collapse at the most fatigue (2026-10-06 round 38; Docs/Design/04 §3). Drawn job by job: a job without the file
        /// keeps its figure in that moment (ExpeditionArt).
        /// </summary>
        public const string Broken = "broken";

        /// <summary>The pose for the moment of a virtue, as <see cref="Broken"/>.</summary>
        public const string Resolute = "resolute";

        /// <summary>Null for "no art". Anything else must be text without spaces.</summary>
        /// <param name="owner">The definition the value belongs to, for the error.</param>
        /// <param name="column">The name of the column, for the error.</param>
        public static string Optional(string value, string owner, string column)
        {
            if (value == null)
            {
                return null;
            }

            if (value.Length == 0 || value.IndexOf(' ') >= 0 || value != value.Trim())
            {
                throw new DataException($"{owner}: {column} '{value}' must be an address without spaces, or left out.");
            }

            return value;
        }

        /// <summary>
        /// The address of a pose of a unit: the figure's address under "pose", with the pose after its last segment
        /// ("unit/job/knight" is "pose/job/knight-attack"). A unit's attack and hit poses are drawn after its figure is
        /// approved (Docs/Design/10 §2, §5: the mercenaries' and the monsters'), so a job or an enemy that has a figure has
        /// both and the data names only the figure.
        /// Null for a unit without a figure.
        /// </summary>
        /// <param name="pose"><see cref="Attack"/> or <see cref="Hit"/>.</param>
        public static string PoseOf(string figure, string pose)
        {
            if (figure == null)
            {
                return null;
            }

            int slash = figure.IndexOf('/');
            return "pose" + (slash < 0 ? "/" + figure : figure.Substring(slash)) + "-" + pose;
        }
    }
}
