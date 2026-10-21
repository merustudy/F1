using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the static data that ships: sources convert, generated files are current and load.</summary>
    public sealed class ShippedDataTests
    {
        [Test]
        public void GeneratedJson_IsUpToDateWithSources()
        {
            List<string> stale = DataTransformMenu.FindStaleFiles();

            Assert.IsEmpty(stale, "Stale generated files. Run F1/Data/Transform Static Data: " + string.Join(", ", stale));
        }

        [Test]
        public void GeneratedJson_LoadsThroughTheRuntimeLoader()
        {
            StaticData data = LoadShipped();

            Assert.IsNotEmpty(data.Jobs.Ordered);
            Assert.IsNotEmpty(data.Dungeons.Ordered);
        }

        [Test]
        public void EveryShippedJob_NamesItsSixStats_AndEveryBaseWeaponGrowsWithOne()
        {
            // Slice D stage 32 (Docs/Design/05_Mercenary_Growth.md §2): the CSV requires the six; the base weapons are the jobs' axes (02 §4).
            StaticData data = LoadShipped();
            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.IsNotNull(job.Stats, job.Id);
                Assert.IsNull(job.Stats.FirstOutside(data.Balance.StatMin, data.Balance.StatMax), job.Id);
                Assert.IsNotNull(data.Items.Get(job.WeaponItemId).ScalesWith, job.WeaponItemId);
            }
        }

        /// <summary>
        /// Slice D stage 42 (Docs/Design/02 §4 "장비 요구 스탯", 05 §2; the user's "모든 무기 장비와 방어 장비", "기본 장비 … 스탯(5)"): every weapon and every piece of
        /// defensive gear asks its owner for a stat, nothing else does, and a job's base weapon asks 5 of the job's main stat (the one its growth favours),
        /// which the job's base values meet — so every mercenary of the job wields it.
        /// </summary>
        [Test]
        public void EveryShippedPieceOfEquipment_AsksForAStat_AndABaseWeaponAsksFiveOfItsJobsMainStat()
        {
            StaticData data = LoadShipped();
            foreach (ItemData item in data.Items.Ordered)
            {
                bool equipment = item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Armor;
                Assert.AreEqual(equipment, item.HasRequirement, item.Id + (equipment ? ": equipment asks for a stat." : ": only equipment asks."));
            }

            foreach (JobData job in data.Jobs.Ordered)
            {
                ItemData weapon = data.Items.Get(job.WeaponItemId);
                Stat main = StatBlock.All.OrderByDescending(s => job.Growth[s]).First();
                Assert.AreEqual((Stat?)main, weapon.ReqStat, $"{job.Id}: its base weapon '{weapon.Id}' asks for the job's main stat.");
                Assert.AreEqual(5, weapon.ReqValue, weapon.Id);
                Assert.IsTrue(StatRules.MeetsRequirement(job.Stats, weapon), $"{job.Id}: {main} {job.Stats[main]} meets its base weapon.");
            }

            Assert.AreEqual((Stat?)Stat.Str, data.Items.Get("iron_armor").ReqStat, "Plate asks for strength.");
            Assert.AreEqual(data.Items.Get("longsword").ReqValue, data.Items.Get("gladius").ReqValue, "The forge's result asks what the longsword asks.");
        }

        [Test]
        public void TheShippedJobs_AreTheEight_AndTheStartingRosterIsFive()
        {
            // Slice D stage 36 (Docs/Design/05_Mercenary_Growth.md §1, Design/15 판정): 전사·바이킹·방랑자·마법사·사제 on their base weapons;
            // 세드릭 is gone and 카이 is a warrior (the user's decision). Stage 47 (Design/20 판정): two jobs an axis — the ranger is the archer, the rogue
            // (dagger), the occultist (cursed tome) and the minstrel (lute) are new; they all come from the stagecoach.
            StaticData data = LoadShipped();
            CollectionAssert.AreEquivalent(new[] { "warrior", "viking", "rogue", "archer", "mage", "occultist", "priest", "minstrel" }, data.Jobs.Ordered.Select(j => j.Id));
            Assert.IsFalse(data.Jobs.Contains("ranger"));

            // Rounds 93 and 94 (2026-10-22, the user's "연결 및 구현"): the four have their own figures, and the four poses follow from them (ArtSetup);
            // the spellblade's picture, their stand-in until then, is named by no job any more (its files left Assets with it).
            foreach (string job in new[] { "rogue", "archer", "occultist", "minstrel" })
            {
                Assert.AreEqual("unit/job/" + job, data.Jobs.Get(job).Figure, job);
            }

            Assert.IsFalse(data.Jobs.Ordered.Any(j => j.Figure == "unit/job/spellblade"), "The spellblade's picture was the stand-in until Round 93.");
            Assert.AreEqual("longsword", data.Jobs.Get("warrior").WeaponItemId);
            Assert.AreEqual("greataxe", data.Jobs.Get("viking").WeaponItemId);
            Assert.AreEqual("dagger", data.Jobs.Get("rogue").WeaponItemId);
            Assert.AreEqual("longbow", data.Jobs.Get("archer").WeaponItemId);
            Assert.AreEqual("fire_staff", data.Jobs.Get("mage").WeaponItemId);
            Assert.AreEqual("cursed_tome", data.Jobs.Get("occultist").WeaponItemId);
            Assert.AreEqual("healing_staff", data.Jobs.Get("priest").WeaponItemId);
            Assert.AreEqual("lute", data.Jobs.Get("minstrel").WeaponItemId);
            Assert.AreEqual(8, data.Jobs.Ordered.Select(j => j.WeaponItemId).Distinct().Count(), "Every job's base weapon is its own (the user's decision).");
            Assert.AreEqual(PassiveEffect.CritChancePercent, data.Jobs.Get("archer").Passive.Effect);
            Assert.AreEqual(10, data.Jobs.Get("archer").Passive.Magnitude);
            Assert.AreEqual((PassiveTrigger.Always, PassiveEffect.DodgePercent, 10), (data.Jobs.Get("rogue").Passive.Trigger, data.Jobs.Get("rogue").Passive.Effect, data.Jobs.Get("rogue").Passive.Magnitude));
            Assert.AreEqual((PassiveTrigger.BattleStart, PassiveEffect.Shield, PassiveTarget.Self, 273), (data.Jobs.Get("occultist").Passive.Trigger, data.Jobs.Get("occultist").Passive.Effect, data.Jobs.Get("occultist").Passive.Target, data.Jobs.Get("occultist").Passive.Magnitude));
            Assert.AreEqual((PassiveTrigger.BattleStart, PassiveEffect.Shield, PassiveTarget.AllyAll, 164), (data.Jobs.Get("minstrel").Passive.Trigger, data.Jobs.Get("minstrel").Passive.Effect, data.Jobs.Get("minstrel").Passive.Target, data.Jobs.Get("minstrel").Passive.Magnitude));
            Assert.AreEqual(3, data.Items.Get("longbow").ShopWeight, "A base weapon's shop weight.");
            Assert.AreEqual((3, 0), (data.Items.Get("dagger").ShopWeight, data.Items.Get("dagger").DropWeight), "Stage 47: the dagger is the rogue's base weapon — shops only, like the longbow.");
            Assert.AreEqual((EffectKind.Charge, TargetMode.AllyAll, 1000, 1000), (data.Items.Get("lute").Effects[0].Kind, data.Items.Get("lute").Effects[0].Target, data.Items.Get("lute").Effects[0].Min, data.Items.Get("lute").Effects[0].Max), "The lute pulls the party's cooldowns.");
            Assert.AreEqual(EffectKind.Vulnerable, data.Items.Get("cursed_tome").Effects[1].Kind, "The tome curses.");

            Dictionary<string, string> roster = data.StartingMercenaries.ToDictionary(m => m.Id, m => m.JobId);
            CollectionAssert.AreEquivalent(new[] { "rowan", "astrid", "ella", "mira", "kai" }, roster.Keys);
            Assert.AreEqual("warrior", roster["kai"]);
            Assert.AreEqual("warrior", roster["rowan"]);
            Assert.IsFalse(data.Mercenaries.Contains("cedric"));
        }

        [Test]
        public void EveryDefinition_HasSourceAndGeneratedFile()
        {
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.SourceDirectory, file.SourceFileName)), file.SourceFileName);
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.GeneratedDirectory, file.GeneratedFileName)), file.GeneratedFileName);
            }
        }

        /// <summary>The first mercenaries of the roster, lined up from row 1 back.</summary>
        static List<PartyMember> FirstParty(StaticData data)
        {
            return data.StartingMercenaries
                .Take(data.Balance.PartySize)
                .Select((m, index) => new PartyMember(m.Id, m.JobId, BattleRows.Front + index))
                .ToList();
        }

        [Test]
        public void EveryShippedDungeon_ProducesAMapAndBattleSetupsForEveryFoughtNode()
        {
            StaticData data = LoadShipped();
            List<PartyMember> party = FirstParty(data);

            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                for (ulong seed = 1; seed <= 20; seed++)
                {
                    ExpeditionState state = ExpeditionRules.Create(data, dungeon.Id, seed, party);
                    foreach (MapNode node in state.Map.Nodes)
                    {
                        if (!node.IsFought)
                        {
                            Assert.IsNull(node.EnemyGroupId, $"{dungeon.Id}: nobody waits at camp {node.Id}");
                            continue;
                        }

                        BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, state, node.EnemyGroupId, seed, node.Floor);
                        Assert.DoesNotThrow(() => new BattleEngine(setup), $"{dungeon.Id} {node.EnemyGroupId} on floor {node.Floor}");
                    }
                }
            }
        }

        [Test]
        public void ShippedBattles_AlwaysEnd()
        {
            StaticData data = LoadShipped();
            List<PartyMember> party = FirstParty(data);

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                ExpeditionState state = ExpeditionRules.Create(data, group.DungeonId, 1, party);
                var battle = new BattleEngine(ExpeditionRules.BuildBattleSetup(data, state, group.Id, 1));

                Assert.DoesNotThrow(() => battle.RunToEnd(), group.Id);
                Assert.AreNotEqual(BattleResult.Ongoing, battle.Result);
            }
        }

        [Test]
        public void EveryShippedJob_CanUseItsWeaponInItsRecommendedRow_OfAFullParty()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.IsTrue(
                    data.Items.Get(job.WeaponItemId).UsableIn(job.RecommendedRow, data.Balance.PartySize),
                    $"{job.Id}: {job.WeaponItemId} in row {job.RecommendedRow} of {data.Balance.PartySize}");
            }
        }

        [Test]
        public void EveryShippedEnemyGroup_ThreatensFromTheStart()
        {
            // Where an item works is counted on the living line, so nobody stays dead weight for good: the
            // line only shortens. What matters is that the group acts before anyone has died.
            StaticData data = LoadShipped();

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                bool someoneActsAtTheStart = false;
                for (int i = 0; i < group.Enemies.Count; i++)
                {
                    int row = BattleRows.Front + i;
                    EnemyData enemy = data.Enemies.Get(group.Enemies[i]);
                    someoneActsAtTheStart |= enemy.Items.Any(grant => data.Items.Get(grant.ItemId).UsableIn(row, group.Enemies.Count));
                }

                Assert.IsTrue(someoneActsAtTheStart, $"{group.Id}: nobody can use an item where they stand at the start");
            }
        }

        /// <summary>
        /// The items are classed as the design says (Docs/Design/02_Combat_System.md §4, 2026-10-04): every job's own weapon is a
        /// weapon (the bishop's healing staff too), the buckler is defensive gear, the flask and the spit are attacks that are not
        /// weapons, and the charm, the pouch and the chant are support items.
        /// </summary>
        [Test]
        public void ShippedItems_AreClassedAsDecided()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.AreEqual(ItemCategory.Weapon, data.Items.Get(job.WeaponItemId).Category, job.Id);
            }

            Assert.AreEqual(ItemCategory.Weapon, data.Items.Get("healing_staff").Category);
            Assert.AreEqual(ItemCategory.Armor, data.Items.Get("buckler").Category);
            Assert.AreEqual(ItemCategory.Attack, data.Items.Get("ember_flask").Category);
            Assert.AreEqual(ItemCategory.Weapon, data.Items.Get("lantern_staff").Category);
            foreach (string support in new[] { "ward_charm", "herb_pouch", "mending_chant" })
            {
                Assert.AreEqual(ItemCategory.Support, data.Items.Get(support).Category, support);
            }
        }

        /// <summary>
        /// Slice D stage 39 (Docs/Design/02_Combat_System.md §4 "방어구", the user's names): the iron armour and the iron helm, defensive gear that
        /// activates once when a battle starts, everywhere, with no stat; the helm takes points off the critical chance. Sold and dropped; no enemy wears them.
        /// </summary>
        [Test]
        public void TheShippedArmour_IsTheIronArmourAndTheIronHelm()
        {
            StaticData data = LoadShipped();
            ItemData armour = data.Items.Get("iron_armor");
            ItemData helm = data.Items.Get("iron_helm");
            Assert.AreEqual("철 갑옷", armour.Name.Values["ko-KR"]);
            Assert.AreEqual("철 투구", helm.Name.Values["ko-KR"]);
            foreach (ItemData piece in new[] { armour, helm })
            {
                Assert.AreEqual(ItemCategory.Armor, piece.Category, piece.Id);
                Assert.AreEqual(ItemTrigger.BattleStart, piece.Trigger, piece.Id);
                Assert.AreEqual(0, piece.CooldownMs, piece.Id);
                Assert.IsTrue(piece.Rows.IsEveryRow, piece.Id);
                Assert.IsNull(piece.ScalesWith, piece.Id + ": defensive gear is outside the stats.");
                Assert.AreEqual(EffectKind.Shield, piece.Effects.Single().Kind, piece.Id);
                Assert.AreEqual(TargetMode.Self, piece.Effects.Single().Target, piece.Id);
                Assert.IsTrue(piece.ShopWeight > 0 && piece.Price > 0 && piece.DropWeight > 0, piece.Id + ": sold and dropped.");
                Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Items.Any(i => i.ItemId == piece.Id) || e.Phases.Any(p => p.Items.Any(i => i.ItemId == piece.Id))),
                    piece.Id + ": no enemy wears it.");
            }

            Assert.AreEqual((2, 2), (armour.Width, armour.Height));
            Assert.AreEqual((2, 1), (helm.Width, helm.Height));
            Assert.AreEqual(0, armour.CritTakenPercent);
            Assert.IsTrue(helm.CritTakenPercent > 0);
            Assert.IsTrue(helm.Effects[0].Min < armour.Effects[0].Min, "The helm's shield is the smaller (\"소폭\").");
        }

        /// <summary>Slice B stage 20 and round 51: the whetstone and its stars, the melee weapons, the valkyrie's battle axe.</summary>
        [Test]
        public void ShippedStarsAndShapes_AreAsDecided()
        {
            StaticData data = LoadShipped();

            ItemData whetstone = data.Items.Get("whetstone");
            Assert.AreEqual(ItemCategory.Other, whetstone.Category);
            Assert.IsTrue(whetstone.IsPassive, "It never activates.");
            Assert.AreEqual((1, 1), (whetstone.Width, whetstone.Height));
            CollectionAssert.AreEqual(new[] { (0, -1), (0, 1) }, whetstone.Stars.Select(s => (s.X, s.Y)), "A star above and one below.");
            Assert.AreEqual(100, whetstone.StarDamage, "+1, in hundredths (stage 22).");
            Assert.Greater(whetstone.ShopWeight, 0, "Sold in shops.");
            Assert.Greater(whetstone.Price, 0);
            Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Items.Any(g => g.ItemId == "whetstone")), "No enemy carries it: shops only.");

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "longsword", "greataxe", "mace", "dagger", "spear", "halberd", "rat_bite", "rusty_blade", "overseer_maul", "gladius", "abyss_cleaver",
                    // Stage 46 (Docs/Design/02 §4 "아이템 목록"): the new hand weapons and the viking's unique.
                    "serrated_dagger", "war_hammer", "pickaxe", "berserker_axe", "goblin_sawblade", "bloodgale",
                },
                data.Items.Ordered.Where(i => i.Melee).Select(i => i.Id));

            ItemData axe = data.Items.Get("greataxe");
            Assert.AreEqual("전투도끼", axe.Name.Resolve("ko-KR"));
            Assert.AreEqual((3, 2), (axe.Width, axe.Height), "The viking's battle axe is a base weapon of 3x2.");
            Assert.AreEqual((2, 2), (data.Items.Get("longbow").Width, data.Items.Get("longbow").Height), "The archer's longbow (Slice D stage 36) keeps its 2x2.");
            Assert.AreEqual((2, 1), (data.Items.Get("dagger").Width, data.Items.Get("dagger").Height), "The rogue's dagger (stage 47) keeps its 2x1: the third exception to 3x1.");
            foreach (JobData job in data.Jobs.Ordered.Where(j => j.WeaponItemId != "greataxe" && j.WeaponItemId != "longbow" && j.WeaponItemId != "dagger"))
            {
                ItemData weapon = data.Items.Get(job.WeaponItemId);
                Assert.AreEqual((3, 1), (weapon.Width, weapon.Height), job.Id);
            }

            Assert.AreEqual("쥐 발톱", data.Items.Get("rat_bite").Name.Resolve("ko-KR"));
        }

        [Test]
        public void TheGladius_IsTheMinesUnique_ForgedFromTheLongswordAndAWhetstone()
        {
            // Stage 23 (round 70): the first unique item and its recipe.
            StaticData data = LoadShipped();
            ItemData gladius = data.Items.Get("gladius");
            Assert.IsTrue(gladius.Unique);
            Assert.AreEqual(0, gladius.ShopWeight, "Never in a shop.");
            Assert.AreEqual(0, gladius.Price);
            Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Items.Any(g => g.ItemId == "gladius")), "No enemy carries it: forged only.");
            Assert.AreEqual((3, 1), (gladius.Width, gladius.Height), "The longsword's place.");
            Assert.IsTrue(gladius.Melee);
            Assert.AreEqual(2, gladius.Effects[0].Reach, "Hits one enemy more than the longsword.");
            Assert.AreEqual(10, gladius.CritChancePercent);
            CollectionAssert.AreEqual(new[] { (1, -1), (1, 1) }, gladius.Stars.Select(s => (s.X, s.Y)), "A star above and below its centre square.");
            Assert.AreEqual(400, gladius.StarDamage, "A Tier3 whetstone's +4, flat: a unique has no tier (round 70 follow-up 3).");
            Assert.AreEqual((804, 1340), (gladius.Effects[0].Min, gladius.Effects[0].Max), "8.04-13.4 in hundredths: 12-20 (the longsword 2-4 + a whetstone, x4, flat) scaled by 0.67 with every damage value (2026-10-17).");
            Assert.AreEqual("item/gladius-glow", gladius.GlowIcon, "The gold glow behind its icon, made from the icon.");
            Assert.IsNull(data.Items.Get("longsword").GlowIcon, "Only a unique item glows.");

            RecipeData recipe = data.Recipes.Get("gladius");
            Assert.AreEqual(("gladius", "longsword", ItemTier.Tier3, "whetstone", ItemTier.Tier3), (recipe.Result, recipe.BaseItem, recipe.BaseTier, recipe.Catalyst, recipe.CatalystTier));
            Assert.AreEqual(5, data.Recipes.Count, "Stage 46: one unique per job's base weapon.");
            Assert.AreEqual(5, data.Items.Ordered.Count(i => i.Unique));
        }

        [Test]
        public void TheStage46Uniques_AreForgedFromTheOtherBaseWeapons_AtTheirTier3Values_PlusOneWordOfTheVocabulary()
        {
            // Stage 46 (Docs/Design/02 §4 "아이템 목록", Design/19 판정 8): bloodgale, windcutter, cinder and dawnlight — the base's Tier3 values with a
            // second effect, no stars (the gladius' star was the user's decision), the base's size, place, cooldown and requirement.
            StaticData data = LoadShipped();
            foreach ((string unique, string baseItem, EffectKind word) in new[]
            {
                ("bloodgale", "greataxe", EffectKind.Bleed), ("windcutter", "longbow", EffectKind.Vulnerable), ("cinder", "fire_staff", EffectKind.Burn), ("dawnlight", "healing_staff", EffectKind.Regen),
            })
            {
                ItemData forged = data.Items.Get(unique);
                ItemData from = data.Items.Get(baseItem);
                Assert.IsTrue(forged.Unique, unique);
                Assert.AreEqual((0, 0, 0), (forged.ShopWeight, forged.Price, forged.DropWeight), unique + ": forged only.");
                Assert.IsEmpty(forged.Stars, unique + ": no star of its own.");
                Assert.AreEqual((from.Width, from.Height, from.CooldownMs, from.ReqStat, from.ReqValue), (forged.Width, forged.Height, forged.CooldownMs, forged.ReqStat, forged.ReqValue), unique + ": the base's place.");
                for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
                {
                    Assert.AreEqual(from.UsableIn(row, BattleRows.Count), forged.UsableIn(row, BattleRows.Count), $"{unique}: usable where the {baseItem} is, row {row}.");
                }
                Assert.AreEqual(from.Effects[0].Kind, forged.Effects[0].Kind, unique);
                Assert.AreEqual((from.Effects[0].Min * 4, from.Effects[0].Max * 4), (forged.Effects[0].Min, forged.Effects[0].Max), unique + ": the base at Tier3 (x4).");
                Assert.AreEqual(word, forged.Effects[1].Kind, unique + ": one word of the vocabulary as its second effect.");
                RecipeData recipe = data.Recipes.Get(unique);
                Assert.AreEqual((unique, baseItem, ItemTier.Tier3, "whetstone", ItemTier.Tier3), (recipe.Result, recipe.BaseItem, recipe.BaseTier, recipe.Catalyst, recipe.CatalystTier));
            }
        }

        [Test]
        public void TheStage46Catalog_CarriesEveryWordOfTheVocabulary_OnTheShelves_AndArmourForEveryAxis()
        {
            // Stage 46 (Docs/Design/19, 02 §4 "아이템 목록", 03 §4·§5, 02 §10): 56 items, every one of the thirteen words on at least one item, armour
            // asking for each of the four axes, the two enemy-only items, the four deep bundles swapped, four potions and five bags.
            StaticData data = LoadShipped();
            Assert.AreEqual(58, data.Items.Ordered.Count, "56 of stage 46 and the two base weapons of stage 47 (the occultist's tome, the minstrel's lute).");
            foreach (EffectKind word in new[]
            {
                EffectKind.Bleed, EffectKind.Regen, EffectKind.Thorns, EffectKind.Cleanse, EffectKind.Stagger, EffectKind.Charge,
                EffectKind.Freeze, EffectKind.Blind, EffectKind.Aim, EffectKind.Keen, EffectKind.Fury, EffectKind.Haste, EffectKind.Vulnerable,
            })
            {
                Assert.IsTrue(data.Items.Ordered.Any(i => !i.Unique && i.ShopWeight > 0 && i.Effects.Any(e => e.Kind == word)), word + ": on a shop's shelf.");
            }

            CollectionAssert.AreEquivalent(new[] { Stat.Str, Stat.Dex, Stat.Int, Stat.Wis },
                data.Items.Ordered.Where(i => i.Category == ItemCategory.Armor && i.ShopWeight > 0).Select(i => i.ReqStat.Value).Distinct().ToList(), "Armour on every axis (08 §49's gap).");
            Assert.AreEqual((1, 3), (data.Items.Get("banner").Width, data.Items.Get("banner").Height), "The banner is the first 1x3 shape.");
            Assert.IsTrue(data.Items.Get("spiked_shield").Cycles && data.Items.Get("studded_armor").Trigger == ItemTrigger.BattleStart, "Thorns that stack by the cooldown, and once at the start.");
            foreach (string id in new[] { "goblin_sawblade", "bristle_hide" })
            {
                Assert.AreEqual((0, 0), (data.Items.Get(id).ShopWeight, data.Items.Get(id).DropWeight), id + ": the enemy's own, loot only.");
                Assert.Greater(data.Items.Get(id).Price, 0, id + ": sells for a little.");
            }

            CollectionAssert.AreEqual(new[] { "rat_bite", "bristle_hide" }, data.Enemies.Get("cave_rat").ItemsAt(11).Select(g => g.ItemId), "The bristly rat.");
            CollectionAssert.AreEqual(new[] { "crude_bow", "crude_bow", "ash_pouch" }, data.Enemies.Get("goblin_archer").ItemsAt(21).Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "lantern_staff", "mending_chant", "hex_bone" }, data.Enemies.Get("goblin_hexer").ItemsAt(21).Select(g => g.ItemId));

            CollectionAssert.AreEquivalent(new[] { "healing_potion", "barrier_potion", "cleansing_potion", "regen_potion" }, data.Potions.Ordered.Select(p => p.Id).ToList());
            Assert.AreEqual((PotionEffect.Cleanse, 3), (data.Potions.Get("cleansing_potion").Effect, data.Potions.Get("cleansing_potion").Magnitude));
            Assert.AreEqual((PotionEffect.Regen, 8), (data.Potions.Get("regen_potion").Effect, data.Potions.Get("regen_potion").Magnitude), "Stacks: not hundredths.");
            Assert.AreEqual(5, data.Bags.Ordered.Count);
            Assert.AreEqual((2, 2, 16), (data.Bags.Get("knapsack").Width, data.Bags.Get("knapsack").Height, data.Bags.Get("knapsack").Price));
            Assert.AreEqual((3, 2, 24), (data.Bags.Get("big_pack").Width, data.Bags.Get("big_pack").Height, data.Bags.Get("big_pack").Price), "Four coins a square, like the others.");

            // Round 92: every shipped item, potion and bag has its picture (item/<kebab-id>, potion/<kebab-id>, bag/<kebab-id>), the uniques a glow too.
            // Round 93 (2026-10-22, the user's "연결 및 구현"): the occultist's tome and the minstrel's lute have theirs as well.
            foreach (ItemData item in data.Items.Ordered)
            {
                Assert.AreEqual("item/" + item.Id.Replace('_', '-'), item.Icon, item.Id);
                Assert.AreEqual(item.Unique ? item.Icon + "-glow" : null, item.GlowIcon, item.Id);
            }

            foreach (PotionData potion in data.Potions.Ordered)
            {
                Assert.AreEqual("potion/" + potion.Id.Replace('_', '-'), potion.Icon, potion.Id);
            }
        }

        [Test]
        public void ShippedInventory_IsTheSizeItsWindowIsLaidOutFor()
        {
            BalanceData balance = LoadShipped().Balance;

            // The data allows a larger inventory than the window holds: a size of its own would lay the grid past the window's edge.
            Assert.AreEqual(
                (InventoryGridView.LaidOutWidth, InventoryGridView.LaidOutHeight),
                (balance.InventoryWidth, balance.InventoryHeight),
                "The inventory window is laid out for another size: change InventoryGridView.LaidOutWidth/Height and the window with the data.");
        }

        [Test]
        public void ShippedMineEnemies_CarryOneMoreFromTheEleventhFloor_AndAnotherFromTheTwentyFirst_AtTheItemsOwnValues()
        {
            // 2026-10-20 (Docs/Design/03_Dungeon_Structure.md §4, the user's "11층 +1, 21층+2, 보통 세기"): the mine's common enemies carry a bundle from
            // floor 11 and another from floor 21, each at the item's own values (100); the elites only the second; the bosses, the escort and the abyss none.
            StaticData data = LoadShipped();
            foreach (string id in new[] { "goblin_raider", "goblin_archer", "goblin_shaman" })
            {
                CollectionAssert.AreEqual(new[] { 11, 21 }, data.Enemies.Get(id).DeepItems.Select(bundle => bundle.Floor), id);
            }

            CollectionAssert.AreEqual(new[] { 11 }, data.Enemies.Get("cave_rat").DeepItems.Select(bundle => bundle.Floor), "Rats roam to floor 18.");
            foreach (string id in new[] { "goblin_veteran", "goblin_hexer" })
            {
                CollectionAssert.AreEqual(new[] { 21 }, data.Enemies.Get(id).DeepItems.Select(bundle => bundle.Floor), id);
            }

            foreach (string id in new[] { "mine_overseer", "skeleton_guard", "abyss_lord" })
            {
                Assert.IsEmpty(data.Enemies.Get(id).DeepItems, id);
            }

            foreach (EnemyData enemy in data.Enemies.Ordered)
            {
                foreach (EnemyDepthItems bundle in enemy.DeepItems)
                {
                    Assert.AreEqual(1, bundle.Items.Count, enemy.Id + ": one item per bundle");
                    Assert.AreEqual(100, bundle.Items[0].PowerPercent, enemy.Id + ": at the item's own values");
                }
            }

            // Stage 46: the raider's second bundle is the saw-blade (the ember flask before).
            CollectionAssert.AreEqual(new[] { "rusty_blade", "rusty_blade", "goblin_sawblade" }, data.Enemies.Get("goblin_raider").ItemsAt(21).Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "lantern_staff", "mending_chant", "ward_charm", "lantern_staff" }, data.Enemies.Get("goblin_shaman").ItemsAt(21).Select(g => g.ItemId));
            Assert.AreEqual(1, data.Enemies.Get("goblin_raider").ItemsAt(10).Count, "The tenth floor is the base alone.");
        }

        [Test]
        public void ShippedAbyssLord_CarriesThreeFallingPhases_IntoTheBattleSetup()
        {
            // Slice C stage 28 (Docs/Design/02_Combat_System.md §9): the final boss starts with its cleaver and changes its items at 75, 50 and 25%.
            StaticData data = LoadShipped();
            EnemyData lord = data.Enemies.Get("abyss_lord");
            Assert.AreEqual(data.Balance.FinalBossLevel, lord.Level);
            CollectionAssert.AreEqual(new[] { 75, 50, 25 }, lord.Phases.Select(p => p.HpPercent));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver" }, lord.Items.Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver", "abyss_howl" }, lord.Phases[0].Items.Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver", "abyss_howl", "abyss_grasp" }, lord.Phases[2].Items.Select(g => g.ItemId));
            Assert.IsTrue(lord.Phases[2].Items.All(g => g.PowerPercent > lord.Phases[0].Items[0].PowerPercent), "The last phase hits harder.");
            Assert.IsTrue(data.Dungeons.Ordered.Single(d => d.IsFinal).Id == "abyss");
            Assert.IsTrue(data.EnemyGroups.Ordered.Any(g => g.IsBoss && g.DungeonId == "abyss" && g.Enemies[0] == "abyss_lord"), "The lord leads the final dungeon's boss group.");
            Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Id != "abyss_lord" && e.Level >= data.Balance.FinalBossLevel), "The level is the lord's alone.");

            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, F1.Tests.FlowTestKit.Party(data.StartingMercenaries.Take(data.Balance.MinPartySize).Select((m, i) => (m.Id, i + 1)).ToArray()));
            run.ClearedDungeons["abandoned_mine"] = data.Dungeons.Get("abyss").UnlockClears;
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "abyss");
            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, expedition, "abyss_lord_court", 1);
            BattleUnitSetup boss = setup.Enemies[0];
            Assert.AreEqual("abyss_lord", boss.SourceId);
            Assert.AreEqual(3, boss.Phases.Count);
            CollectionAssert.AreEqual(new[] { 75, 50, 25 }, boss.Phases.Select(p => p.HpPercent));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver", "abyss_howl", "abyss_grasp" }, boss.Phases[1].Items.Select(i => i.Item.Id));
            Assert.IsNotNull(boss.Phases[1].Layout, "The screen draws the phase's board from its layout.");
            Assert.IsEmpty(setup.Enemies[1].Phases, "The guards have none.");
        }

        [Test]
        public void ShippedEnemyItems_HaveAPriceToSellFor_ButNoShopWeight_AndSellForAQuarter()
        {
            // Round 74 (2026-10-17): the enemies' items got a price so that they sell at a shop; their weight 0 keeps them off the shop's grid.
            // 2026-10-18 (the user's play feedback): the shop pays a quarter, half of what it paid.
            StaticData data = LoadShipped();
            Assert.AreEqual(25, data.Balance.SellPercent);
            foreach (string id in new[] { "rat_bite", "rusty_blade", "crude_bow", "lantern_staff", "mending_chant", "overseer_roar", "overseer_maul", "abyss_howl", "abyss_grasp", "abyss_cleaver" })
            {
                ItemData item = data.Items.Get(id);
                Assert.Greater(item.Price, 0, id);
                Assert.AreEqual(0, item.ShopWeight, id + " never on a shop's grid");
            }

            Assert.AreEqual(1, ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("rusty_blade"))), "6 at Common, a quarter rounded down.");
            Assert.AreEqual(2, ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("whetstone"))), "8 at Common (restored 2026-10-17), a quarter.");
            Assert.AreEqual(1, ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("rat_bite"))), "2 at Common: a quarter is 0, so the least, 1.");
            Assert.AreEqual(0, data.Items.Get("gladius").Price, "A unique has no price (and cannot be sold).");
        }

        [Test]
        public void ShippedBags_EachHaveAPicture_AtTheirSquares()
        {
            // Round 79: every bag has its picture (bag/<kebab-id>), shown where the bag is a thing (stage 46's two since round 92).
            StaticData data = LoadShipped();
            foreach (BagData bag in data.Bags.Ordered)
            {
                Assert.AreEqual("bag/" + bag.Id.Replace('_', '-'), bag.Icon, bag.Id);
            }
        }

        [Test]
        public void ShippedBuildings_HaveThreeLevelsEach_ForFortyAndEightyGold_OverThreeAndFiveDays()
        {
            // Stage 30 (Round 78): three buildings (the gate has no level since 2026-10-18), level 2 for 40 gold and 3 days, level 3 for 80 gold and 5 days; the bonuses grow.
            StaticData data = LoadShipped();
            foreach (string id in BuildingId.All)
            {
                Assert.AreEqual(3, data.MaxBuildingLevel(id), id);
                Assert.AreEqual(0, data.BuildingAt(id, 1).Bonus, id);
                Assert.AreEqual((40, 3), (data.BuildingAt(id, 2).Price, data.BuildingAt(id, 2).Days), id);
                Assert.AreEqual((80, 5), (data.BuildingAt(id, 3).Price, data.BuildingAt(id, 3).Days), id);
                Assert.Greater(data.BuildingAt(id, 3).Bonus, data.BuildingAt(id, 2).Bonus, id);
            }

            Assert.AreEqual(4, data.BuildingAt(BuildingId.Barracks, 3).Bonus, "Roster limit 8 -> 12.");
            Assert.AreEqual(2, data.BuildingAt(BuildingId.Coach, 3).Bonus, "Candidates 3 -> 5.");
            Assert.AreEqual(40, data.BuildingAt(BuildingId.Inn, 3).Bonus, "Relief 40 -> 80.");
            Assert.AreEqual(3, data.Buildings.Count(row => row.Level == 1), "No gate among the buildings.");
        }

        [Test]
        public void ShippedTraits_AreTheFirstBatchsSixInnate_InClashingPairs_AndTheStartingRosterHasTheDatas()
        {
            // Slice D stage 37 (Design/16 판정 14, Docs/Design/05 §6): the six innate traits of the first batch, strengths and weaknesses three each,
            // eagle eye against clumsy and steady against timid; the starting roster's set in the data.
            StaticData data = LoadShipped();
            CollectionAssert.AreEquivalent(new[] { "eagle_eye", "clumsy", "iron_bones", "frail", "steady", "timid" }, data.InnateTraits.Select(t => t.Id).ToList());
            Assert.IsTrue(data.InnateTraits.All(t => t.Weight > 0 && t.Source == null));
            Assert.AreEqual(3, data.InnateTraits.Count(t => t.Polarity == TraitPolarity.Positive));
            Assert.IsTrue(data.Traits.Get("eagle_eye").Clashes(data.Traits.Get("clumsy")));
            Assert.IsTrue(data.Traits.Get("steady").Clashes(data.Traits.Get("timid")));
            Assert.AreEqual((5, -5, -5, -10, -10, 10), (data.Traits.Get("eagle_eye").HitPercent, data.Traits.Get("clumsy").HitPercent, data.Traits.Get("iron_bones").DogDeathPercent,
                data.Traits.Get("frail").MaxHpPercent, data.Traits.Get("steady").FatiguePercent, data.Traits.Get("timid").FatiguePercent));
            CollectionAssert.AreEquivalent(new[] { "rowan:steady", "astrid:iron_bones+clumsy", "ella:frail", "mira:eagle_eye+timid", "kai:" },
                data.StartingMercenaries.Select(m => m.Id + ":" + string.Join("+", m.Traits)).ToList());
            Assert.IsTrue(data.RecruitPool.All(m => m.Traits.Count == 0));
            Assert.AreEqual((30, 50, 20), (data.Balance.RecruitTraitWeight0, data.Balance.RecruitTraitWeight1, data.Balance.RecruitTraitWeight2));
        }

        [Test]
        public void ShippedTraits_TheAcquiredSeven_ComeFromTheirSources_AtTheStartingChances()
        {
            // Slice D stage 38 (Docs/Design/05 §6 and the stage's 판정 1~3, 2026-10-19 "권장안 대로"): seasoned from a clear, shaken from a retreat,
            // survivor, scarred and limp from death's door (one strength in three), nightmares from an affliction, hardened from a virtue; no new
            // clashing pairs; the chances 15, 15, 25, 40, 40.
            StaticData data = LoadShipped();
            List<TraitData> acquired = data.Traits.Ordered.Where(t => t.Kind == TraitKind.Acquired).ToList();
            CollectionAssert.AreEquivalent(new[] { "seasoned:Clear:Positive", "shaken:Retreat:Negative", "survivor:DeathsDoor:Positive", "scarred:DeathsDoor:Negative",
                "limp:DeathsDoor:Negative", "nightmares:Affliction:Negative", "hardened:Virtue:Positive" }, acquired.Select(t => $"{t.Id}:{t.Source}:{t.Polarity}").ToList());
            Assert.IsTrue(acquired.All(t => t.Weight == 10 && t.Excludes == null));
            Assert.IsFalse(acquired.Any(t => data.Traits.Ordered.Any(other => other.Excludes == t.Id)));
            Assert.AreEqual((2, -10, -10, -5, -5, 10, 10), (data.Traits.Get("seasoned").CritPercent, data.Traits.Get("shaken").VirtuePercent, data.Traits.Get("survivor").DogDeathPercent,
                data.Traits.Get("scarred").MaxHpPercent, data.Traits.Get("limp").DodgePercent, data.Traits.Get("nightmares").FatiguePercent, data.Traits.Get("hardened").VirtuePercent));
            BalanceData balance = data.Balance;
            Assert.AreEqual((15, 15, 25, 40, 40), (balance.TraitChanceClearPercent, balance.TraitChanceRetreatPercent, balance.TraitChanceDeathsDoorPercent,
                balance.TraitChanceAfflictionPercent, balance.TraitChanceVirtuePercent));
        }

        [Test]
        public void EveryShippedJobsPassive_HasItsShortNameAsATrait()
        {
            // Round 84 판정 4: 선봉·광기·급소·후방 영창·축복.
            StaticData data = LoadShipped();
            var names = data.Jobs.Ordered.ToDictionary(job => job.Id, job => job.PassiveName?.Resolve("ko-KR"));
            Assert.AreEqual(("선봉", "광기", "급소", "후방 영창", "축복"), (names["warrior"], names["viking"], names["archer"], names["mage"], names["priest"]));
            // Stage 47 (Design/20): 그림자 걸음·어둠의 장막·서곡.
            Assert.AreEqual(("그림자 걸음", "어둠의 장막", "서곡"), (names["rogue"], names["occultist"], names["minstrel"]));
            Assert.IsTrue(data.Jobs.Ordered.All(job => job.Passive != null && job.PassiveName.Resolve("en-US").Length > 0));
        }

        [Test]
        public void ShippedBaseWeapons_AreOnSale_AndTheGeneralItems_DropByWeight()
        {
            // 2026-10-17: the jobs' base weapons priced and weighted for the shop (the party's own only: ExpeditionRules), the shop's general
            // items with a drop weight, and nothing else dropping as a general item (the whetstone and the base weapons come from shops).
            StaticData data = LoadShipped();
            Assert.AreEqual(30, data.Dungeons.Get("abandoned_mine").GeneralDropChancePercent);
            Assert.AreEqual(0, data.Dungeons.Get("abyss").GeneralDropChancePercent, "The pit drops only bones: a general item there made it twice as easy (Design/08 §32).");
            foreach (JobData job in data.Jobs.Ordered)
            {
                ItemData weapon = data.Items.Get(job.WeaponItemId);
                Assert.Greater(weapon.Price, 0, job.Id);
                Assert.Greater(weapon.ShopWeight, 0, job.Id);
                Assert.AreEqual(0, weapon.DropWeight, job.Id + ": a base weapon comes from a shop or a camp, never a drop.");
            }

            // Slice D stage 36: the longbow is the ranger's base weapon now — shops only, like the other base weapons. Stage 47: the dagger is the rogue's.
            foreach (string id in new[] { "spear", "halberd", "buckler", "ward_charm", "herb_pouch", "ember_flask", "serrated_dagger" })
            {
                Assert.Greater(data.Items.Get(id).DropWeight, 0, id);
            }

            Assert.AreEqual(0, data.Items.Get("dagger").DropWeight, "Stage 47: a base weapon.");

            Assert.AreEqual(0, data.Items.Get("whetstone").DropWeight, "Shops only (round 51).");
            Assert.AreEqual(0, data.Items.Get("gladius").DropWeight);
            Assert.IsTrue(data.Items.Ordered.All(i => i.DropWeight == 0 || i.ShopWeight > 0), "A general drop is one of the shop's general items.");
            Assert.IsTrue(data.Enemies.Ordered.SelectMany(e => e.Items).Select(g => data.Items.Get(g.ItemId)).All(i => i.ShopWeight > 0 || i.DropWeight == 0),
                "An enemy's own item (one no shop sells) drops only as the enemy's; the buckler is both a shop's general item and the veteran's.");
        }

        static StaticData LoadShipped()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            return StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));
        }
    }
}
