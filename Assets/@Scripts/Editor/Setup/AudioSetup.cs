using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Core;
using UnityEditor;
using UnityEngine;

namespace F1.Editor.Setup
{
    /// <summary>
    /// The sounds the game plays. SoundCatalog lists the approved ones and a sound's key says where its file is
    /// ("sound/sfx/mercenary-death" is "Assets/@Audio/Sfx/mercenary-death.wav"). Every file is imported by the policy of its
    /// kind, which lives here. A sound is wired by copying the approved file to its place and listing it in the catalog; no
    /// entry and no import setting is made by hand (Docs/Architecture/14_SOUND.md).
    /// </summary>
    public static class AudioSetup
    {
        public const string DoneToken = "F1_AUDIO_SYNC_DONE";
        public const string AudioDirectory = "Assets/@Audio";
        public const string AudioGroup = AddressablesSetup.GroupPrefix + "Audio";

        /// <summary>Vorbis quality of the music, 0..1. Unity makes the only compressed copy: the files are wav.</summary>
        const float MusicQuality = 0.7f;

        static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg", ".aif", ".aiff" };

        readonly struct AudioFile
        {
            public AudioFile(string address, string assetPath, bool music)
            {
                Address = address;
                AssetPath = assetPath;
                Music = music;
            }

            public string Address { get; }
            public string AssetPath { get; }

            /// <summary>Music streams and keeps its stereo; an effect is mono and decompressed when it loads.</summary>
            public bool Music { get; }
        }

        public static string AssetPath(SoundEffect effect)
        {
            return $"{AudioDirectory}/Sfx/{SoundCatalog.Key(effect)}.wav";
        }

        public static string AssetPath(MusicTrack track)
        {
            return $"{AudioDirectory}/Bgm/{SoundCatalog.Key(track)}.wav";
        }

        [MenuItem("F1/Setup/Sync Audio")]
        public static void SyncMenu()
        {
            Sync();
            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        /// <summary>One entry per wired sound, all in the app scope: the effects are small and the music streams.</summary>
        public static List<AddressEntry> Entries()
        {
            return Files().Select(f => new AddressEntry(f.AssetPath, f.Address, ResourceScope.App, AudioGroup)).ToList();
        }

        static List<AudioFile> Files()
        {
            return SoundCatalog.Effects.Select(e => new AudioFile(SoundCatalog.Address(e), AssetPath(e), false))
                .Concat(SoundCatalog.Tracks.Select(t => new AudioFile(SoundCatalog.Address(t), AssetPath(t), true)))
                .ToList();
        }

        /// <summary>Brings every sound file to the import policy of its kind. Files already there are not touched.</summary>
        public static void Sync()
        {
            foreach (AudioFile file in Files())
            {
                var importer = AssetImporter.GetAtPath(file.AssetPath) as AudioImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"Sound '{file.Address}' has no audio file at {file.AssetPath}.");
                }

                if (PolicyProblems(importer, file.Music).Count > 0)
                {
                    ApplyPolicy(importer, file.Music);
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>Returns a description of everything that does not match: missing files, import settings, files nothing names.</summary>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (AudioFile file in Files())
            {
                listed.Add(file.AssetPath);
                if (!LogicalAddress.IsValid(file.Address))
                {
                    problems.Add($"{file.Address}: not a valid logical address.");
                    continue;
                }

                if (!File.Exists(file.AssetPath))
                {
                    problems.Add($"{file.Address}: audio file is missing ({file.AssetPath}).");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(file.AssetPath) as AudioImporter;
                if (importer == null)
                {
                    problems.Add($"{file.Address}: {file.AssetPath} is not imported as audio.");
                    continue;
                }

                foreach (string problem in PolicyProblems(importer, file.Music))
                {
                    problems.Add($"{file.Address}: {problem}. Run F1/Setup/Sync Audio.");
                }
            }

            if (Directory.Exists(AudioDirectory))
            {
                foreach (string file in Directory.GetFiles(AudioDirectory, "*", SearchOption.AllDirectories))
                {
                    string path = file.Replace('\\', '/');
                    if (AudioExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()) && !listed.Contains(path))
                    {
                        problems.Add($"{path}: nothing names this sound: it is not in SoundCatalog.");
                    }
                }
            }

            return problems;
        }

        static void ApplyPolicy(AudioImporter importer, bool music)
        {
            importer.forceToMono = !music;
            importer.loadInBackground = music;
            importer.ambisonic = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = music ? MusicQuality : 1f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !music;
            importer.defaultSampleSettings = settings;
            importer.ClearSampleSettingOverride("Standalone");
        }

        static List<string> PolicyProblems(AudioImporter importer, bool music)
        {
            var problems = new List<string>();
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Check(problems, importer.forceToMono == !music, music ? "music must keep its stereo" : "an effect must be forced to mono");
            Check(problems, importer.loadInBackground == music, music ? "music must load in the background" : "an effect must not load in the background");
            Check(problems, !importer.ambisonic, "must not be ambisonic");
            Check(problems, settings.loadType == (music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad),
                music ? "music must stream" : "an effect must be decompressed on load");
            Check(problems, settings.compressionFormat == (music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM),
                music ? "music must be Vorbis" : "an effect must be ADPCM");
            Check(problems, !music || Mathf.Approximately(settings.quality, MusicQuality), $"music quality must be {MusicQuality}");
            Check(problems, settings.sampleRateSetting == AudioSampleRateSetting.PreserveSampleRate, "the sample rate must be kept");
            Check(problems, settings.preloadAudioData == !music, music ? "music must not preload" : "an effect must preload");
            Check(problems, !importer.ContainsSampleSettingsOverride("Standalone"), "must have no Standalone override");
            return problems;
        }

        static void Check(List<string> problems, bool ok, string rule)
        {
            if (!ok)
            {
                problems.Add(rule);
            }
        }
    }
}
