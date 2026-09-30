using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using F1.Data;
using UnityEngine;

namespace F1.Core
{
    /// <summary>
    /// Loads generated JSON through ResourceManager and exposes validated static data.
    /// It never parses CSV and owns no runtime or save state.
    /// </summary>
    public sealed class DataManager
    {
        readonly ResourceManager _resource;
        StaticData _data;

        public DataManager(ResourceManager resource)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        public bool IsLoaded => _data != null;

        public StaticData Data => _data ?? throw new InvalidOperationException("Static data is not loaded.");

        /// <summary>Loads every definition into the App scope and validates the whole data set.</summary>
        public async Task LoadAsync()
        {
            if (_data != null)
            {
                throw new InvalidOperationException("Static data is already loaded.");
            }

            var texts = new Dictionary<StaticDataFiles.Entry, string>();
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                TextAsset asset = await _resource.LoadAsync<TextAsset>(file.Address, ResourceScope.App);
                texts.Add(file, asset.text);
            }

            _data = StaticDataLoader.Load(file => texts[file]);
        }
    }
}
