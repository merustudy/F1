using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1.Core
{
    /// <summary>A sound effect the screens can ask for (Docs/Design/12_Sound_Direction.md §2). Whether it has a sound yet is the catalog's to say.</summary>
    public enum SoundEffect
    {
        ItemWeapon,
        ItemArmor,
        ItemAttack,
        ItemSupport,
        Hit,
        HitHeavy,
        Blocked,
        Heal,
        Shield,
        Burn,
        DeathsDoor,
        Survived,
        EnemyDown,
        KillMoment,
        MercenaryDeath,
        Potion,
        Retreat,
        RetreatFailed,
        CandleOut,
        Thunder,
        Victory,
        Defeat,
        Button,
        ItemPlace,
        NodeMove,
        Depart,
    }

    /// <summary>A piece of music (Docs/Design/12_Sound_Direction.md §2).</summary>
    public enum MusicTrack
    {
        Lobby,
        Dungeon,
        Boss,
    }

    /// <summary>
    /// The sounds wired into the game. Only approved sounds are listed (Docs/Architecture/14_SOUND.md): a sound that is not
    /// listed plays nothing, the way an empty Figure leaves a unit without a picture. A listed sound must have its file; the
    /// setup check finds a missing one and the boot fails on it. A sound's key is its name in kebab case ("mercenary-death"),
    /// the key of its row in SoundPipeline/Rosters.
    /// </summary>
    public static class SoundCatalog
    {
        public const string EffectPrefix = "sound/sfx/";
        public const string MusicPrefix = "sound/bgm/";

        /// <summary>
        /// The effects approved so far: all of them (2026-10-05). Hit, the mercenary's death and the button were the user's picks
        /// (round 02); the rest were picked by the recommendation the user left them to (round 03, SoundPipeline/Archive/03-rest).
        /// </summary>
        static readonly SoundEffect[] WiredEffects =
        {
            SoundEffect.ItemWeapon,
            SoundEffect.ItemArmor,
            SoundEffect.ItemAttack,
            SoundEffect.ItemSupport,
            SoundEffect.Hit,
            SoundEffect.HitHeavy,
            SoundEffect.Blocked,
            SoundEffect.Heal,
            SoundEffect.Shield,
            SoundEffect.Burn,
            SoundEffect.DeathsDoor,
            SoundEffect.Survived,
            SoundEffect.EnemyDown,
            SoundEffect.KillMoment,
            SoundEffect.MercenaryDeath,
            SoundEffect.Potion,
            SoundEffect.Retreat,
            SoundEffect.RetreatFailed,
            SoundEffect.CandleOut,
            SoundEffect.Thunder,
            SoundEffect.Victory,
            SoundEffect.Defeat,
            SoundEffect.Button,
            SoundEffect.ItemPlace,
            SoundEffect.NodeMove,
            SoundEffect.Depart,
        };

        /// <summary>The music approved so far: all three, each an approved candidate cut to a loop of eight bars (2026-10-05).</summary>
        static readonly MusicTrack[] WiredTracks =
        {
            MusicTrack.Lobby,
            MusicTrack.Dungeon,
            MusicTrack.Boss,
        };

        public static IReadOnlyList<SoundEffect> Effects => WiredEffects;

        public static IReadOnlyList<MusicTrack> Tracks => WiredTracks;

        public static bool Has(SoundEffect effect)
        {
            return Array.IndexOf(WiredEffects, effect) >= 0;
        }

        public static bool Has(MusicTrack track)
        {
            return Array.IndexOf(WiredTracks, track) >= 0;
        }

        public static string Key(SoundEffect effect)
        {
            return Kebab(effect.ToString());
        }

        public static string Key(MusicTrack track)
        {
            return Kebab(track.ToString());
        }

        public static string Address(SoundEffect effect)
        {
            return EffectPrefix + Key(effect);
        }

        public static string Address(MusicTrack track)
        {
            return MusicPrefix + Key(track);
        }

        /// <summary>Every effect the screens can ask for, wired or not, in declaration order.</summary>
        public static IEnumerable<SoundEffect> AllEffects()
        {
            return Enum.GetValues(typeof(SoundEffect)).Cast<SoundEffect>();
        }

        public static IEnumerable<MusicTrack> AllTracks()
        {
            return Enum.GetValues(typeof(MusicTrack)).Cast<MusicTrack>();
        }

        static string Kebab(string name)
        {
            var key = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0)
                {
                    key.Append('-');
                }

                key.Append(char.ToLowerInvariant(c));
            }

            return key.ToString();
        }
    }
}
