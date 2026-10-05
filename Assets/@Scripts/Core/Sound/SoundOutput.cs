using UnityEngine;

namespace F1.Core
{
    /// <summary>
    /// The audio sources the sound manager plays through (Docs/Architecture/14_SOUND.md): two for music, which hand over to
    /// each other when the track changes, and eight for effects, so that each effect keeps its own pitch and a burst of
    /// effects is capped. AppRoot makes it under itself, so it lives as long as the app. The music fades on real time: a kill
    /// moment slows the battle, not the sound.
    /// </summary>
    public sealed class SoundOutput : MonoBehaviour
    {
        public const int EffectVoices = 8;
        public const float MusicFadeSeconds = 0.5f;

        AudioSource[] _music;
        float[] _musicGain;
        float[] _musicTarget;
        int _front;
        AudioSource[] _effects;
        float[] _effectStarted;
        float _musicVolume = 1f;
        float _effectVolume = 1f;

        public static SoundOutput Create(Transform parent)
        {
            var host = new GameObject("Sound");
            host.transform.SetParent(parent, false);
            var output = host.AddComponent<SoundOutput>();
            output.Build();
            return output;
        }

        /// <summary>The clip of the track playing or coming in, or null when the music is silent or fading out.</summary>
        public AudioClip MusicClip => _music[_front].clip;

        public float MusicVolume => _musicVolume;

        public float EffectVolume => _effectVolume;

        void Build()
        {
            _music = new[] { NewSource(true), NewSource(true) };
            _musicGain = new float[2];
            _musicTarget = new float[2];
            _effects = new AudioSource[EffectVoices];
            _effectStarted = new float[EffectVoices];
            for (int i = 0; i < EffectVoices; i++)
            {
                _effects[i] = NewSource(false);
            }
        }

        AudioSource NewSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        /// <param name="music">The music's volume, 0..1.</param>
        /// <param name="effects">The effects' volume, 0..1.</param>
        public void SetVolumes(float music, float effects)
        {
            _musicVolume = Mathf.Clamp01(music);
            _effectVolume = Mathf.Clamp01(effects);
            ApplyMusicVolumes();
            foreach (AudioSource source in _effects)
            {
                source.volume = _effectVolume;
            }
        }

        /// <summary>Plays an effect once: on a free source, or on the one that started first when all are busy.</summary>
        public void PlayEffect(AudioClip clip, float pitch)
        {
            int chosen = -1;
            for (int i = 0; i < EffectVoices && chosen < 0; i++)
            {
                if (!_effects[i].isPlaying)
                {
                    chosen = i;
                }
            }

            if (chosen < 0)
            {
                chosen = 0;
                for (int i = 1; i < EffectVoices; i++)
                {
                    if (_effectStarted[i] < _effectStarted[chosen])
                    {
                        chosen = i;
                    }
                }
            }

            AudioSource source = _effects[chosen];
            source.Stop();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = _effectVolume;
            source.Play();
            _effectStarted[chosen] = Time.unscaledTime;
        }

        /// <summary>Hands the music over to a clip, looping, or fades it out when the clip is null.</summary>
        public void PlayMusic(AudioClip clip)
        {
            int back = 1 - _front;
            AudioSource incoming = _music[back];
            incoming.Stop();
            incoming.clip = clip;
            _musicGain[back] = 0f;
            _musicTarget[back] = clip != null ? 1f : 0f;
            if (clip != null)
            {
                incoming.Play();
            }

            _musicTarget[_front] = 0f;
            _front = back;
            ApplyMusicVolumes();
        }

        void Update()
        {
            float step = Time.unscaledDeltaTime / MusicFadeSeconds;
            for (int i = 0; i < _music.Length; i++)
            {
                _musicGain[i] = Mathf.MoveTowards(_musicGain[i], _musicTarget[i], step);
                if (_musicGain[i] <= 0f && _musicTarget[i] <= 0f && _music[i].clip != null)
                {
                    _music[i].Stop();
                    _music[i].clip = null;
                }
            }

            ApplyMusicVolumes();
        }

        void ApplyMusicVolumes()
        {
            for (int i = 0; i < _music.Length; i++)
            {
                _music[i].volume = _musicVolume * _musicGain[i];
            }
        }
    }
}
