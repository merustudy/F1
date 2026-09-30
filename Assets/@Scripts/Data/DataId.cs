using System.Text.RegularExpressions;

namespace F1.Data
{
    /// <summary>Static data identifiers are English snake_case strings, for example "knight".</summary>
    public static class DataId
    {
        static readonly Regex Pattern = new Regex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$", RegexOptions.CultureInvariant);

        public static bool IsValid(string id)
        {
            return !string.IsNullOrEmpty(id) && Pattern.IsMatch(id);
        }

        public static string Require(string id, string what)
        {
            if (!IsValid(id))
            {
                throw new DataException($"{what}: '{id}' is not a valid id (English snake_case).");
            }

            return id;
        }
    }
}
