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
        /// The address of the face of a unit: the figure's address with its first segment replaced
        /// ("unit/job/knight" is "face/job/knight"). A unit that has a figure has a face, because the
        /// face is cut out of the figure (ArtPipeline, cutface.py), so the data names only the figure.
        /// Null for a unit without a figure.
        /// </summary>
        public static string FaceOf(string figure)
        {
            if (figure == null)
            {
                return null;
            }

            int slash = figure.IndexOf('/');
            return "face" + (slash < 0 ? "/" + figure : figure.Substring(slash));
        }

        /// <summary>
        /// The address of a pose of a unit: the figure's address under "pose", with the pose after its last segment
        /// ("unit/job/knight" is "pose/job/knight-attack"). A mercenary's attack and hit poses are drawn after its figure
        /// is approved (Docs/Design/10 §2), so a job that has a figure has both and the data names only the figure.
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
