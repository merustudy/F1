using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace F1.Editor.Setup
{
    /// <summary>One addressable asset: where the file is, its logical address, scope and group.</summary>
    public readonly struct AddressEntry
    {
        public AddressEntry(string assetPath, string address, ResourceScope scope, string group)
        {
            AssetPath = assetPath;
            Address = address;
            Scope = scope;
            Group = group;
        }

        public string AssetPath { get; }
        public string Address { get; }
        public ResourceScope Scope { get; }
        public string Group { get; }
    }

    /// <summary>
    /// Keeps Addressables settings equal to the entry lists in code. Entries are never registered
    /// through the Inspector: add a line to a list and run the sync.
    /// </summary>
    public static partial class AddressablesSetup
    {
        public const string DoneToken = "F1_ADDRESSABLES_SYNC_DONE";
        public const string GroupPrefix = "F1-";
        public const string DataGroup = "F1-Data";
        public const string UiGroup = "F1-UI";

        static readonly ResourceScope[] Scopes = (ResourceScope[])Enum.GetValues(typeof(ResourceScope));

        /// <summary>Every addressable asset of the project. Category lists live in the other partial files.</summary>
        public static IReadOnlyList<AddressEntry> AllEntries()
        {
            var entries = new List<AddressEntry>();
            AddDataEntries(entries);
            AddUiEntries(entries);
            AddArtEntries(entries);
            return entries;
        }

        static partial void AddDataEntries(List<AddressEntry> entries);

        static partial void AddUiEntries(List<AddressEntry> entries);

        static partial void AddArtEntries(List<AddressEntry> entries);

        [MenuItem("F1/Setup/Sync Addressables")]
        public static void SyncMenu()
        {
            Sync();
            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        public static void Sync()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                throw new InvalidOperationException("Could not create Addressables settings.");
            }

            bool changed = false;

            // Editor play mode reads assets straight from the AssetDatabase; no content build is needed.
            if (settings.ActivePlayModeDataBuilderIndex != 0)
            {
                settings.ActivePlayModeDataBuilderIndex = 0;
                changed = true;
            }

            List<string> existingLabels = settings.GetLabels();
            foreach (ResourceScope scope in Scopes)
            {
                string label = LogicalAddress.ScopeLabel(scope);
                if (!existingLabels.Contains(label))
                {
                    settings.AddLabel(label, false);
                    changed = true;
                }
            }

            IReadOnlyList<AddressEntry> expected = AllEntries();
            var expectedGuids = new HashSet<string>();
            foreach (AddressEntry entry in expected)
            {
                string guid = AssetDatabase.AssetPathToGUID(entry.AssetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    throw new InvalidOperationException($"Addressable asset not found: {entry.AssetPath}");
                }

                expectedGuids.Add(guid);
                AddressableAssetGroup group = FindOrCreateGroup(settings, entry.Group, ref changed);

                AddressableAssetEntry assetEntry = settings.FindAssetEntry(guid);
                if (assetEntry == null || assetEntry.parentGroup != group)
                {
                    assetEntry = settings.CreateOrMoveEntry(guid, group, false, false);
                    changed = true;
                }

                if (assetEntry.address != entry.Address)
                {
                    assetEntry.SetAddress(entry.Address, false);
                    changed = true;
                }

                string expectedLabel = LogicalAddress.ScopeLabel(entry.Scope);
                foreach (ResourceScope scope in Scopes)
                {
                    string label = LogicalAddress.ScopeLabel(scope);
                    bool shouldHave = label == expectedLabel;
                    if (assetEntry.labels.Contains(label) != shouldHave)
                    {
                        assetEntry.SetLabel(label, shouldHave, false, false);
                        changed = true;
                    }
                }
            }

            foreach (AddressableAssetGroup group in settings.groups.Where(IsProjectGroup).ToList())
            {
                foreach (AddressableAssetEntry stale in group.entries.Where(e => !expectedGuids.Contains(e.guid)).ToList())
                {
                    settings.RemoveAssetEntry(stale.guid, false);
                    changed = true;
                }
            }

            if (changed)
            {
                foreach (AddressableAssetGroup group in settings.groups.Where(IsProjectGroup))
                {
                    EditorUtility.SetDirty(group);
                }

                EditorUtility.SetDirty(settings);
            }
        }

        /// <summary>Returns a description of every mismatch between the entry lists and the settings.</summary>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                problems.Add("Addressables settings do not exist. Run F1/Setup/Sync Addressables.");
                return problems;
            }

            IReadOnlyList<AddressEntry> expected = AllEntries();
            var addresses = new HashSet<string>();
            var expectedGuids = new HashSet<string>();
            foreach (AddressEntry entry in expected)
            {
                if (!File.Exists(entry.AssetPath))
                {
                    problems.Add($"{entry.Address}: asset file is missing ({entry.AssetPath}).");
                    continue;
                }

                if (!LogicalAddress.IsValid(entry.Address))
                {
                    problems.Add($"{entry.Address}: not a valid logical address.");
                }

                if (!addresses.Add(entry.Address))
                {
                    problems.Add($"{entry.Address}: address is listed more than once.");
                }

                string[] segments = entry.Address.Split('/');
                if ((segments[0] == "data" || segments[0] == "ui") && segments[1] != LogicalAddress.ScopeSegment(entry.Scope))
                {
                    problems.Add($"{entry.Address}: second segment must be '{LogicalAddress.ScopeSegment(entry.Scope)}' for scope {entry.Scope}.");
                }

                if (!entry.Group.StartsWith(GroupPrefix, StringComparison.Ordinal))
                {
                    problems.Add($"{entry.Address}: group '{entry.Group}' must start with '{GroupPrefix}'.");
                }

                string guid = AssetDatabase.AssetPathToGUID(entry.AssetPath);
                expectedGuids.Add(guid);
                AddressableAssetEntry assetEntry = settings.FindAssetEntry(guid);
                if (assetEntry == null)
                {
                    problems.Add($"{entry.Address}: not registered. Run F1/Setup/Sync Addressables.");
                    continue;
                }

                if (assetEntry.address != entry.Address)
                {
                    problems.Add($"{entry.Address}: registered with address '{assetEntry.address}'.");
                }

                if (assetEntry.parentGroup == null || assetEntry.parentGroup.Name != entry.Group)
                {
                    problems.Add($"{entry.Address}: registered in the wrong group.");
                }

                List<string> scopeLabels = Scopes.Select(LogicalAddress.ScopeLabel).Where(assetEntry.labels.Contains).ToList();
                if (scopeLabels.Count != 1 || scopeLabels[0] != LogicalAddress.ScopeLabel(entry.Scope))
                {
                    problems.Add($"{entry.Address}: must have exactly the label '{LogicalAddress.ScopeLabel(entry.Scope)}'.");
                }
            }

            foreach (AddressableAssetGroup group in settings.groups.Where(IsProjectGroup))
            {
                foreach (AddressableAssetEntry unlisted in group.entries.Where(e => !expectedGuids.Contains(e.guid)))
                {
                    problems.Add($"{unlisted.address}: registered in {group.Name} but not listed in AddressablesSetup.");
                }
            }

            return problems;
        }

        static bool IsProjectGroup(AddressableAssetGroup group)
        {
            return group != null && group.Name.StartsWith(GroupPrefix, StringComparison.Ordinal);
        }

        static AddressableAssetGroup FindOrCreateGroup(AddressableAssetSettings settings, string name, ref bool changed)
        {
            AddressableAssetGroup group = settings.FindGroup(name);
            if (group != null)
            {
                return group;
            }

            changed = true;
            return settings.CreateGroup(
                name,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }
    }
}
