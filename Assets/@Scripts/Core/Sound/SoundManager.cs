using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace F1.Core
{
    /// <summary>
    /// Plays the game's effects and music through <see cref="SoundOutput"/> (Docs/Architecture/14_SOUND.md). The screens call
    /// it as what they show happens; the game's other managers and its domain never do, and nothing waits for a sound. What
    /// can play is in <see cref="SoundCatalog"/>: a sound that is not wired yet plays nothing. The volumes are the settings'.
    /// </summary>
    public sealed class SoundManager
    {
        /// <summary>The same effect asked for again within this long plays once: a burst at x4 makes one sound, not a roar.</summary>
        public const float SameEffectGapSeconds = 0.06f;

        /// <summary>Each effect plays up to this much higher or lower, so that a repeated sound is not mechanical.</summary>
        public const float PitchSpread = 0.03f;

        /// <summary>The music sits under the effects: at full volume in the settings it plays at this share.</summary>
        public const float MusicLevel = 0.6f;

        const int RememberedLimit = 256;

        readonly ResourceManager _resource;
        readonly SettingManager _setting;
        readonly SoundOutput _output;
        readonly Dictionary<SoundEffect, AudioClip> _effects = new Dictionary<SoundEffect, AudioClip>();
        readonly Dictionary<MusicTrack, AudioClip> _tracks = new Dictionary<MusicTrack, AudioClip>();
        readonly Dictionary<SoundEffect, float> _lastPlayed = new Dictionary<SoundEffect, float>();
        readonly List<SoundEffect> _asked = new List<SoundEffect>();
        readonly List<SoundEffect> _played = new List<SoundEffect>();
        MusicTrack? _track;

        public SoundManager(ResourceManager resource, SettingManager setting, SoundOutput output)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _setting = setting ?? throw new ArgumentNullException(nameof(setting));
            _output = output != null ? output : throw new ArgumentNullException(nameof(output));
            _setting.VolumeChanged += ApplyVolumes;
        }

        public bool IsLoaded { get; private set; }

        /// <summary>The track the screens asked for last, or null before the first. It may be one that has no music yet.</summary>
        public MusicTrack? Track => _track;

        /// <summary>The effects the screens asked for, wired or not, oldest first (the latest 256). For tests.</summary>
        internal IReadOnlyList<SoundEffect> Asked => _asked;

        /// <summary>The effects that played, oldest first (the latest 256). For tests.</summary>
        internal IReadOnlyList<SoundEffect> Played => _played;

        internal SoundOutput Output => _output;

        /// <summary>Loads every wired sound into the app scope. Boot calls it once, after the settings are loaded.</summary>
        public async Task LoadAsync()
        {
            foreach (SoundEffect effect in SoundCatalog.Effects)
            {
                _effects[effect] = await _resource.LoadAsync<AudioClip>(SoundCatalog.Address(effect), ResourceScope.App);
            }

            foreach (MusicTrack track in SoundCatalog.Tracks)
            {
                _tracks[track] = await _resource.LoadAsync<AudioClip>(SoundCatalog.Address(track), ResourceScope.App);
            }

            IsLoaded = true;
            ApplyVolumes();
        }

        public void PlayEffect(SoundEffect effect)
        {
            Remember(_asked, effect);
            if (!_effects.TryGetValue(effect, out AudioClip clip))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(effect, out float last) && now - last < SameEffectGapSeconds)
            {
                return;
            }

            _lastPlayed[effect] = now;
            Remember(_played, effect);
            _output.PlayEffect(clip, 1f + UnityEngine.Random.Range(-PitchSpread, PitchSpread));
        }

        /// <summary>Changes the music. The same track again does nothing, so a screen can ask each time it opens.</summary>
        public void PlayMusic(MusicTrack track)
        {
            if (_track == track)
            {
                return;
            }

            _track = track;
            _output.PlayMusic(_tracks.TryGetValue(track, out AudioClip clip) ? clip : null);
        }

        internal void ForgetEffects()
        {
            _asked.Clear();
            _played.Clear();
            _lastPlayed.Clear();
        }

        static void Remember(List<SoundEffect> list, SoundEffect effect)
        {
            if (list.Count >= RememberedLimit)
            {
                list.RemoveAt(0);
            }

            list.Add(effect);
        }

        void ApplyVolumes()
        {
            _output.SetVolumes(MusicLevel * _setting.MusicVolume / 100f, _setting.EffectVolume / 100f);
        }
    }
}
