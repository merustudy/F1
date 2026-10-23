using System;
using System.Text.RegularExpressions;

namespace F1.Core
{
    /// <summary>Lifetime of loaded resources. Every Addressables entry belongs to exactly one scope.</summary>
    public enum ResourceScope
    {
        App,
        Lobby,
        Expedition,

        /// <summary>The story's screen (Slice D stage 55): the intro and its scenes' pictures, released when it closes.</summary>
        Story,
    }

    /// <summary>
    /// Logical addresses are lowercase kebab paths such as "data/app/job" or "ui/lobby/lobby-screen".
    /// They are independent of where the asset file lives.
    /// </summary>
    public static class LogicalAddress
    {
        static readonly Regex Pattern = new Regex(
            "^[a-z0-9]+(-[a-z0-9]+)*(/[a-z0-9]+(-[a-z0-9]+)*)+$",
            RegexOptions.CultureInvariant);

        public static bool IsValid(string address)
        {
            return !string.IsNullOrEmpty(address) && Pattern.IsMatch(address);
        }

        public static void Require(string address)
        {
            if (!IsValid(address))
            {
                throw new ArgumentException($"'{address}' is not a valid logical address.", nameof(address));
            }
        }

        public static string ScopeLabel(ResourceScope scope)
        {
            switch (scope)
            {
                case ResourceScope.App: return "scope-app";
                case ResourceScope.Lobby: return "scope-lobby";
                case ResourceScope.Expedition: return "scope-expedition";
                case ResourceScope.Story: return "scope-story";
                default: throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        /// <summary>The scope segment used in "data/&lt;scope&gt;/..." and "ui/&lt;scope&gt;/..." addresses.</summary>
        public static string ScopeSegment(ResourceScope scope)
        {
            switch (scope)
            {
                case ResourceScope.App: return "app";
                case ResourceScope.Lobby: return "lobby";
                case ResourceScope.Expedition: return "expedition";
                case ResourceScope.Story: return "story";
                default: throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }
    }
}
