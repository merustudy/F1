using System.Collections.Generic;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// The art the screens of an expedition show: the full-body figures of the units, the faces
    /// cut out of them and the icons of the items. A screen loads it before it opens, so its views
    /// ask for a picture without waiting. A unit or an item whose data names no art has none, and
    /// its view shows the placeholder (the silhouette, the item's name); art that is named but
    /// does not load fails the screen.
    /// </summary>
    public sealed class ExpeditionArt
    {
        readonly StaticData _data;
        readonly Dictionary<string, Sprite> _sprites;

        ExpeditionArt(StaticData data, Dictionary<string, Sprite> sprites)
        {
            _data = data;
            _sprites = sprites;
        }

        /// <summary>
        /// Loads every picture the data names into the expedition scope. The scope keeps what it
        /// has loaded, so the next screen of the expedition gets the same sprites at once.
        /// </summary>
        public static async Task<ExpeditionArt> LoadAsync(ResourceManager resource, StaticData data)
        {
            var sprites = new Dictionary<string, Sprite>();
            foreach (string address in Addresses(data))
            {
                if (!sprites.ContainsKey(address))
                {
                    sprites.Add(address, await resource.LoadAsync<Sprite>(address, ResourceScope.Expedition));
                }
            }

            return new ExpeditionArt(data, sprites);
        }

        /// <summary>The address of every picture the data names: the jobs and the enemies (each figure with its face), then the items, each in id order.</summary>
        public static IEnumerable<string> Addresses(StaticData data)
        {
            foreach (JobData job in data.Jobs.Ordered)
            {
                if (job.Figure != null)
                {
                    yield return job.Figure;
                    yield return job.Face;
                }
            }

            foreach (EnemyData enemy in data.Enemies.Ordered)
            {
                if (enemy.Figure != null)
                {
                    yield return enemy.Figure;
                    yield return enemy.Face;
                }
            }

            foreach (ItemData item in data.Items.Ordered)
            {
                if (item.Icon != null)
                {
                    yield return item.Icon;
                }
            }
        }

        /// <summary>The figure of a job, or null when the job has none.</summary>
        public Sprite OfJob(string jobId)
        {
            return Of(_data.Jobs.Get(jobId).Figure);
        }

        /// <summary>A mercenary is shown as its job.</summary>
        public Sprite OfMercenary(string mercenaryId)
        {
            return OfJob(_data.Mercenaries.Get(mercenaryId).JobId);
        }

        /// <summary>The figure of an enemy, or null when the enemy has none.</summary>
        public Sprite OfEnemy(string enemyId)
        {
            return Of(_data.Enemies.Get(enemyId).Figure);
        }

        /// <summary>The face cut out of a job's figure, or null when the job has no figure.</summary>
        public Sprite FaceOfJob(string jobId)
        {
            return Of(_data.Jobs.Get(jobId).Face);
        }

        /// <summary>A mercenary's face is its job's.</summary>
        public Sprite FaceOfMercenary(string mercenaryId)
        {
            return FaceOfJob(_data.Mercenaries.Get(mercenaryId).JobId);
        }

        /// <summary>The face cut out of an enemy's figure, or null when the enemy has no figure.</summary>
        public Sprite FaceOfEnemy(string enemyId)
        {
            return Of(_data.Enemies.Get(enemyId).Face);
        }

        /// <summary>The icon of an item, or null when the item has none.</summary>
        public Sprite OfItem(string itemId)
        {
            return Of(_data.Items.Get(itemId).Icon);
        }

        Sprite Of(string address)
        {
            return address == null ? null : _sprites[address];
        }
    }
}
