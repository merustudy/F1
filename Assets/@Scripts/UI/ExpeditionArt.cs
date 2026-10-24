using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// The art the screens of an expedition show: the full-body figures of the units, the units'
    /// attack and hit poses, the icons of the items and the bottles of the potions. A screen loads it before it opens, so its views
    /// ask for a picture without waiting. A unit or an item whose data names no art has none, and
    /// its view shows the placeholder (the silhouette, the item's name); art that is named but
    /// does not load fails the screen. The poses of a breakdown and of a virtue (round 38) are the one
    /// exception: drawn job by job, they are loaded where they are registered and are null where they are not.
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

            foreach (string address in OptionalAddresses(data))
            {
                if (!sprites.ContainsKey(address) && await resource.ExistsAsync(address))
                {
                    sprites.Add(address, await resource.LoadAsync<Sprite>(address, ResourceScope.Expedition));
                }
            }

            return new ExpeditionArt(data, sprites);
        }

        /// <summary>
        /// The addresses the data names but a job may not have yet: the poses of a breakdown and of a virtue (round 38), drawn job by job
        /// and loaded where they are registered.
        /// </summary>
        public static IEnumerable<string> OptionalAddresses(StaticData data)
        {
            foreach (JobData job in data.Jobs.Ordered)
            {
                foreach (string figure in FiguresOf(job))
                {
                    yield return ArtAddress.PoseOf(figure, ArtAddress.Broken);
                    yield return ArtAddress.PoseOf(figure, ArtAddress.Resolute);
                }
            }

            // Stage 57 (Docs/Design/39 §2-4): a job drawn in a build, with its poses, where that build is drawn for the job.
            foreach (string address in BuildAddresses(data))
            {
                yield return address;
            }
        }

        /// <summary>A job's figures (stage 57): its own, then those of each gender, each once.</summary>
        public static IEnumerable<string> FiguresOf(JobData job)
        {
            var seen = new HashSet<string>();
            foreach (string figure in new[] { job.Figure }.Concat(job.FiguresMale).Concat(job.FiguresFemale))
            {
                if (figure != null && seen.Add(figure))
                {
                    yield return figure;
                }
            }
        }

        /// <summary>The figure of every job's figure in every build that names a variant, each followed by its four poses (stage 57). Drawn figure by figure: loaded where registered.</summary>
        public static IEnumerable<string> BuildAddresses(StaticData data)
        {
            foreach (JobData job in data.Jobs.Ordered)
            {
                foreach (string baseFigure in FiguresOf(job))
                {
                    foreach (TraitData build in data.Traits.Ordered)
                    {
                        string figure = build.Build ? ArtAddress.BuildFigure(baseFigure, build.Figure) : null;
                        if (figure != null)
                        {
                            yield return figure;
                            yield return ArtAddress.PoseOf(figure, ArtAddress.Attack);
                            yield return ArtAddress.PoseOf(figure, ArtAddress.Hit);
                            yield return ArtAddress.PoseOf(figure, ArtAddress.Broken);
                            yield return ArtAddress.PoseOf(figure, ArtAddress.Resolute);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The figure a mercenary stands in before its build (stage 57): its own figure where it is one of its job's and drawn, its job's otherwise.
        /// </summary>
        public static string BaseFigureOf(StaticData data, string jobId, string figure, System.Func<string, bool> drawn)
        {
            JobData job = data.Jobs.Get(jobId);
            return figure != null && job.HasFigure(figure) && drawn(figure) ? figure : job.Figure;
        }

        /// <summary>
        /// The figure a mercenary is shown in (stage 57): its figure (or its job's) drawn in its build where that is drawn, that figure otherwise. Static,
        /// so a screen that loads the figures itself (the expedition's party) asks the same.
        /// </summary>
        public static string FigureAddressOf(StaticData data, string jobId, string figure, IReadOnlyList<string> traitIds, System.Func<string, bool> drawn)
        {
            string baseFigure = BaseFigureOf(data, jobId, figure, drawn);
            TraitData build = TraitRules.BuildOf(data, traitIds);
            string variant = build == null ? null : ArtAddress.BuildFigure(baseFigure, build.Figure);
            return variant != null && drawn(variant) ? variant : baseFigure;
        }

        /// <summary>How large a mercenary's figure stands (stage 57): its build's FigureScale where its build is drawn for its figure, 1 otherwise.</summary>
        public static float ScaleOf(StaticData data, string jobId, string figure, IReadOnlyList<string> traitIds, System.Func<string, bool> drawn)
        {
            TraitData build = TraitRules.BuildOf(data, traitIds);
            return build != null && FigureAddressOf(data, jobId, figure, traitIds, drawn) != BaseFigureOf(data, jobId, figure, drawn) ? build.FigureScale / 100f : 1f;
        }

        /// <summary>The address of every picture the data names: the jobs and the enemies (each figure with its two poses), then the items (a unique item's icon with its glow), then the potions, each in id order.</summary>
        public static IEnumerable<string> Addresses(StaticData data)
        {
            foreach (JobData job in data.Jobs.Ordered)
            {
                // Stage 57: a job's figures by gender as well, each with its attack and hit poses.
                foreach (string figure in FiguresOf(job))
                {
                    yield return figure;
                    yield return ArtAddress.PoseOf(figure, ArtAddress.Attack);
                    yield return ArtAddress.PoseOf(figure, ArtAddress.Hit);
                }
            }

            foreach (EnemyData enemy in data.Enemies.Ordered)
            {
                if (enemy.Figure != null)
                {
                    yield return enemy.Figure;
                    yield return enemy.AttackPose;
                    yield return enemy.HitPose;
                }
            }

            foreach (ItemData item in data.Items.Ordered)
            {
                if (item.Icon != null)
                {
                    yield return item.Icon;
                }

                if (item.GlowIcon != null)
                {
                    yield return item.GlowIcon;
                }
            }

            foreach (PotionData potion in data.Potions.Ordered)
            {
                if (potion.Icon != null)
                {
                    yield return potion.Icon;
                }
            }

            // The bags' pictures (round 79): shown where a bag is a thing (the floor, the merchant's tile, the hand).
            foreach (BagData bag in data.Bags.Ordered)
            {
                if (bag.Icon != null)
                {
                    yield return bag.Icon;
                }
            }
        }

        /// <summary>The picture of a bag (round 79), or null when it has none: the well stands in.</summary>
        public Sprite OfBag(string bagId)
        {
            return Of(_data.Bags.Get(bagId).Icon);
        }

        /// <summary>The figure of a job, or null when the job has none.</summary>
        public Sprite OfJob(string jobId)
        {
            return Of(_data.Jobs.Get(jobId).Figure);
        }

        /// <summary>The figure of a mercenary (stage 57): its job's in its build where drawn, its job's otherwise; null when the job has none.</summary>
        public Sprite OfMercenary(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            return Of(MercenaryFigure(jobId, figure, traitIds));
        }

        /// <summary>How large a mercenary's figure stands (stage 57): its build's FigureScale where its build is drawn, 1 otherwise.</summary>
        public float ScaleOfMercenary(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            return ScaleOf(_data, jobId, figure, traitIds, _sprites.ContainsKey);
        }

        /// <summary>A mercenary's attack pose: its figure's (stage 57: its build's where drawn). Null when the job has no figure.</summary>
        public Sprite AttackPoseOfMercenary(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            return Of(ArtAddress.PoseOf(MercenaryFigure(jobId, figure, traitIds), ArtAddress.Attack));
        }

        /// <summary>A mercenary's hit pose, as <see cref="AttackPoseOfMercenary"/>.</summary>
        public Sprite HitPoseOfMercenary(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            return Of(ArtAddress.PoseOf(MercenaryFigure(jobId, figure, traitIds), ArtAddress.Hit));
        }

        /// <summary>A mercenary's breakdown pose, as <see cref="BrokenPoseOfJob"/> for its figure (stage 57: its build's where drawn).</summary>
        public Sprite BrokenPoseOfMercenary(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            return OfOptional(ArtAddress.PoseOf(MercenaryFigure(jobId, figure, traitIds), ArtAddress.Broken));
        }

        /// <summary>A mercenary's virtue pose, as <see cref="BrokenPoseOfMercenary"/>.</summary>
        public Sprite ResolutePoseOfMercenary(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            return OfOptional(ArtAddress.PoseOf(MercenaryFigure(jobId, figure, traitIds), ArtAddress.Resolute));
        }

        string MercenaryFigure(string jobId, string figure, IReadOnlyList<string> traitIds)
        {
            // A figure (and a build drawn for it) is loaded with its attack and hit poses (ArtSetup registers them with the figure).
            return FigureAddressOf(_data, jobId, figure, traitIds, _sprites.ContainsKey);
        }


        /// <summary>The figure of an enemy, or null when the enemy has none.</summary>
        public Sprite OfEnemy(string enemyId)
        {
            return Of(_data.Enemies.Get(enemyId).Figure);
        }

        /// <summary>How much larger than the common figure place an enemy's figure is drawn (1 for most; a boss is larger). Data: <see cref="EnemyData.FigureScale"/>.</summary>
        public float ScaleOfEnemy(string enemyId)
        {
            return _data.Enemies.Get(enemyId).FigureScale / 100f;
        }

        /// <summary>A mercenary's attack pose: its job's (Docs/Design/10 §5). A mercenary is shown as its job, the roster's (a hired one has no job in the data). Null when the job has no figure.</summary>
        public Sprite AttackPoseOfJob(string jobId)
        {
            return Of(_data.Jobs.Get(jobId).AttackPose);
        }

        /// <summary>A mercenary's hit pose: its job's. Null when the job has no figure.</summary>
        public Sprite HitPoseOfJob(string jobId)
        {
            return Of(_data.Jobs.Get(jobId).HitPose);
        }

        /// <summary>
        /// A mercenary's pose for the moment of a breakdown (an affliction, or the collapse; round 38): its job's. Null when the job has
        /// no figure or the pose is not drawn yet, and the figure stands through the moment.
        /// </summary>
        public Sprite BrokenPoseOfJob(string jobId)
        {
            return OfOptional(_data.Jobs.Get(jobId).BrokenPose);
        }

        /// <summary>A mercenary's pose for the moment of a virtue, as <see cref="BrokenPoseOfJob"/>.</summary>
        public Sprite ResolutePoseOfJob(string jobId)
        {
            return OfOptional(_data.Jobs.Get(jobId).ResolutePose);
        }

        /// <summary>An enemy's attack pose (Docs/Design/10 §5). Null when the enemy has no figure.</summary>
        public Sprite AttackPoseOfEnemy(string enemyId)
        {
            return Of(_data.Enemies.Get(enemyId).AttackPose);
        }

        /// <summary>An enemy's hit pose. Null when the enemy has no figure.</summary>
        public Sprite HitPoseOfEnemy(string enemyId)
        {
            return Of(_data.Enemies.Get(enemyId).HitPose);
        }

        /// <summary>The icon of an item, or null when the item has none.</summary>
        public Sprite OfItem(string itemId)
        {
            return Of(_data.Items.Get(itemId).Icon);
        }

        /// <summary>The gold glow behind a unique item's icon (stage 23), or null for an item that is not unique or has no icon.</summary>
        public Sprite GlowOfItem(string itemId)
        {
            return Of(_data.Items.Get(itemId).GlowIcon);
        }

        /// <summary>The bottle of a potion, or null when the potion has none.</summary>
        public Sprite OfPotion(string potionId)
        {
            return Of(_data.Potions.Get(potionId).Icon);
        }

        Sprite Of(string address)
        {
            return address == null ? null : _sprites[address];
        }

        /// <summary>A picture that may not be there: null for an address that was not registered.</summary>
        Sprite OfOptional(string address)
        {
            return address != null && _sprites.TryGetValue(address, out Sprite sprite) ? sprite : null;
        }
    }
}
