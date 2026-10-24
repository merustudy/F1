using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Text that screens build from game data: names in the current locale and descriptions of
    /// items and potions. For display only.
    /// </summary>
    public static class UiText
    {
        /// <summary>A game data name in the current locale.</summary>
        public static string Name(LocalizedText text)
        {
            return text.Resolve(Managers.Setting.LocaleCode);
        }

        public static string Mercenary(string mercenaryId)
        {
            return Name(Managers.Data.Data.Mercenaries.Get(mercenaryId).Name);
        }

        public static string Job(string jobId)
        {
            return Name(Managers.Data.Data.Jobs.Get(jobId).Name);
        }

        /// <summary>What the job's passive does, or an empty string when it has none. A shield is shown whole (stage 22).</summary>
        public static string Passive(JobData job)
        {
            if (job.Passive == null)
            {
                return string.Empty;
            }

            int magnitude = job.Passive.Effect == PassiveEffect.Shield ? Amount(job.Passive.Magnitude) : job.Passive.Magnitude;
            return string.Format(CultureInfo.InvariantCulture, Name(job.PassiveText), magnitude);
        }

        /// <summary>
        /// A mercenary's traits as the trait line shows them (Slice D stage 37, Round 84): the job's trait first (its passive, by its short name, the
        /// gold rim), then the mercenary's own in their order (a strength's rim green, a weakness's red).
        /// </summary>
        public static List<TraitChip> TraitChips(StaticData data, string jobId, IReadOnlyList<string> traitIds)
        {
            var chips = new List<TraitChip>();
            JobData job = data.Jobs.Get(jobId);
            if (job.Passive != null)
            {
                string jobName = Name(job.Name);
                string name = job.PassiveName == null ? jobName : Name(job.PassiveName);
                chips.Add(new TraitChip(name, Glyph(name), UiPalette.TraitJob, UiStrings.Get(UiKeys.Trait.KindJob, jobName), Passive(job)));
            }

            chips.AddRange(TraitChips(data, traitIds));
            return chips;
        }

        /// <summary>
        /// A mercenary's own traits, without the job's (the settlement's "new traits" line, Slice D stage 38): a strength's rim green, a weakness's
        /// red; an acquired trait's card says what left it ("얻은 특성 · 약점 — 빈사에서 살아 돌아와").
        /// </summary>
        public static List<TraitChip> TraitChips(StaticData data, IReadOnlyList<string> traitIds)
        {
            var chips = new List<TraitChip>();
            foreach (string id in traitIds ?? new List<string>())
            {
                TraitData trait = data.Traits.Get(id);
                string name = Name(trait.Name);
                bool strength = trait.Polarity == TraitPolarity.Positive;
                string kind = UiStrings.Get(trait.Kind == TraitKind.Innate ? UiKeys.Trait.KindInnate : UiKeys.Trait.KindAcquired);
                string polarity = UiStrings.Get(strength ? UiKeys.Trait.Strength : UiKeys.Trait.Weakness);
                string sub = UiStrings.Get(UiKeys.Trait.Sub, kind, polarity);
                if (trait.Source != null)
                {
                    sub = UiStrings.Get(UiKeys.Trait.SubFrom, sub, UiStrings.Get(SourceKey(trait.Source.Value)));
                }

                chips.Add(new TraitChip(name, Glyph(name), strength ? UiPalette.TraitStrength : UiPalette.TraitWeakness, sub, TraitEffect(trait)));
            }

            return chips;
        }

        static string SourceKey(TraitSource source)
        {
            switch (source)
            {
                case TraitSource.Clear: return UiKeys.Trait.FromClear;
                case TraitSource.Retreat: return UiKeys.Trait.FromRetreat;
                case TraitSource.DeathsDoor: return UiKeys.Trait.FromDeathsDoor;
                case TraitSource.Affliction: return UiKeys.Trait.FromAffliction;
                default: return UiKeys.Trait.FromVirtue;
            }
        }

        /// <summary>The more pill of a trait line (Round 84 판정 3): "+N", and on its card the names it stands for.</summary>
        public static TraitChip MoreChip(IReadOnlyList<TraitChip> hidden)
        {
            string names = string.Empty;
            foreach (TraitChip chip in hidden)
            {
                names = names.Length == 0 ? chip.Name : UiStrings.Get(UiKeys.Trait.Sub, names, chip.Name);
            }

            return MoreChip(hidden.Count, names);
        }

        /// <summary>A more pill for a count, with what its card names (empty where only its width matters).</summary>
        public static TraitChip MoreChip(int count, string names = "")
        {
            return new TraitChip(UiStrings.Get(UiKeys.Trait.More, count), null, UiPalette.TraitMore, names, string.Empty, count);
        }

        /// <summary>What a trait does, each term it moves in turn ("명중 +5%p", "최대 HP -10%").</summary>
        public static string TraitEffect(TraitData trait)
        {
            string line = string.Empty;
            foreach ((string key, int value) in new[]
            {
                (UiKeys.Trait.Hit, trait.HitPercent), (UiKeys.Trait.Dodge, trait.DodgePercent), (UiKeys.Trait.Crit, trait.CritPercent),
                (UiKeys.Trait.DogDeath, trait.DogDeathPercent), (UiKeys.Trait.Virtue, trait.VirtuePercent), (UiKeys.Trait.Fatigue, trait.FatiguePercent),
                (UiKeys.Trait.MaxHp, trait.MaxHpPercent),
            })
            {
                if (value == 0)
                {
                    continue;
                }

                string term = UiStrings.Get(key, Signed(value));
                line = line.Length == 0 ? term : UiStrings.Get(UiKeys.Trait.Sub, line, term);
            }

            return line;
        }

        /// <summary>A whole number with its sign: "+5", "-10".</summary>
        public static string Signed(int value)
        {
            return (value > 0 ? "+" : string.Empty) + value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The placeholder icon's letter: the name's first.</summary>
        static string Glyph(string name)
        {
            return string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1);
        }

        /// <summary>
        /// A damage, a heal or a shield in hundredths as the screen shows it (Slice B stage 22, Docs/Architecture/12_UI.md "수의 표시"): a whole
        /// number, rounded half up.
        /// </summary>
        public static int Amount(int hundredths)
        {
            return Centi.Round(hundredths);
        }

        /// <summary>HP in hundredths as the screen shows it: rounded half up, but at least 1 while there is any (0 is death's door or death).</summary>
        public static int Hp(int hundredths)
        {
            return hundredths > 0 ? Math.Max(1, Centi.Round(hundredths)) : 0;
        }

        /// <summary>A damage range in hundredths as the screen shows it: "2-4", or one number when both ends round alike ("1").</summary>
        public static string Range(int min, int max)
        {
            int least = Amount(min);
            int most = Amount(max);
            return least == most
                ? least.ToString(CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, "{0}-{1}", least, most);
        }

        /// <summary>Names of mercenaries separated by commas, or "None" for an empty list.</summary>
        public static string MercenaryList(IReadOnlyList<string> mercenaryIds)
        {
            if (mercenaryIds.Count == 0)
            {
                return UiStrings.Get(UiKeys.Common.None);
            }

            var names = new List<string>();
            foreach (string id in mercenaryIds)
            {
                names.Add(Mercenary(id));
            }

            return string.Join(", ", names);
        }

        /// <summary>The key of a row's name ("Row 1"). Prefab builders use it for fixed labels.</summary>
        public static string RowKey(int row)
        {
            switch (row)
            {
                case 1: return UiKeys.Common.Row1;
                case 2: return UiKeys.Common.Row2;
                case 3: return UiKeys.Common.Row3;
                case 4: return UiKeys.Common.Row4;
                default: throw new ArgumentOutOfRangeException(nameof(row), row, "No text for this row.");
            }
        }

        /// <summary>Milliseconds as seconds with one decimal, for example "2.9" (the battle's clock and log).</summary>
        public static string Seconds(int milliseconds)
        {
            int tenths = milliseconds / 100;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", tenths / 10, tenths % 10);
        }

        /// <summary>
        /// Milliseconds as seconds with one decimal, rounded half up at the second: 2940 is "2.9", 1455 "1.5", 2960 "3.0" (2026-10-20 Round 88 4차, the user:
        /// "소수점 둘째 자리에서 반올림" for the card's numbers; <see cref="Seconds"/> truncates, for the clock and the log).
        /// </summary>
        public static string RoundedSeconds(int milliseconds)
        {
            int tenths = (milliseconds + 50) / 100;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", tenths / 10, tenths % 10);
        }

        /// <summary>
        /// "Name N성", the tier in its colour (round 35; round 69 the star word); a Common item is its name alone (round 41). Items have no grade
        /// any more (Slice B stage 22; round 57 had taken it out of the tooltips already).
        /// </summary>
        public static string ItemTitle(EquippedItem item)
        {
            return item.Tier == ItemTier.Common
                ? Name(item.Item.Name)
                : UiStrings.Get(UiKeys.Item.TooltipTitle, Name(item.Item.Name), TierWord(item.Tier));
        }

        /// <summary>The name of a tier (Docs/Design/02_Combat_System.md §4).</summary>
        public static string TierName(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Common: return UiStrings.Get(UiKeys.Item.Common);
                case ItemTier.Tier1: return UiStrings.Get(UiKeys.Item.Tier1);
                case ItemTier.Tier2: return UiStrings.Get(UiKeys.Item.Tier2);
                case ItemTier.Tier3: return UiStrings.Get(UiKeys.Item.Tier3);
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        /// <summary>The name of a tier in the tier's colour (the one palette of round 69: the pips' colours, Diablo's blue and yellow round silver).</summary>
        public static string TierWord(ItemTier tier)
        {
            return Colored(TierName(tier), UiPalette.Rarity(tier));
        }

        /// <summary>Under a chosen item's facts: what putting it on the same item at the same tier does (round 35). A Common item names no tier of its own.</summary>
        public static string MergeHint(EquippedItem item)
        {
            return item.Tier == ItemTier.Common
                ? UiStrings.Get(UiKeys.Item.MergeHintPlain, Name(item.Item.Name), TierWord(item.Tier + 1))
                : UiStrings.Get(UiKeys.Item.MergeHint, Name(item.Item.Name), TierWord(item.Tier), TierWord(item.Tier + 1));
        }

        /// <summary>The name of a state of the breakdown: an affliction or a virtue (Docs/Design/04_Lobby_100Day_Economy.md §3).</summary>
        public static string FatigueStateName(FatigueStateData state)
        {
            return Name(state.Name);
        }

        /// <summary>The name of a state in its colour: red for an affliction, gold for a virtue (round 36).</summary>
        public static string FatigueStateWord(FatigueStateData state)
        {
            return Colored(Name(state.Name), UiPalette.FatigueState(state.Kind));
        }

        /// <summary>"공포: 아이템이 25% 느리게 돈다": the state and what it does, for the party side's detail line.</summary>
        public static string FatigueStateDetail(FatigueStateData state)
        {
            return UiStrings.Get(UiKeys.Board.StateDetail, FatigueStateWord(state), Name(state.Description));
        }

        /// <summary>
        /// The job on the state line under a party member's feet, with the member's state after it while it is in one: "마검사 · 공포", and (stage 48) the
        /// overwork after that while it is overworked: "마검사 · 공포 · 과로".
        /// </summary>
        public static string JobLine(string jobId, FatigueStateData state, bool overworked = false)
        {
            if (state == null)
            {
                return Job(jobId);
            }

            string line = UiStrings.Get(UiKeys.Board.JobState, Job(jobId), FatigueStateWord(state));
            return overworked ? UiStrings.Get(UiKeys.Board.JobState, line, Colored(UiStrings.Get(UiKeys.Board.Overwork), UiPalette.FatigueDanger)) : line;
        }

        /// <summary>A state's name for the battle's state line, and (stage 48) "· 과로" after it while the unit is overworked.</summary>
        public static string FatigueStateLabel(FatigueStateData state, bool overworked)
        {
            return overworked ? UiStrings.Get(UiKeys.Board.JobState, FatigueStateName(state), UiStrings.Get(UiKeys.Board.Overwork)) : FatigueStateName(state);
        }

        /// <summary>A value before and after a change: "11 → 22".</summary>
        public static string Change(string before, string after)
        {
            return UiStrings.Get(UiKeys.Item.Change, before, after);
        }

        const string FactSeparator = " / ";

        /// <summary>
        /// An item's card (round 42): the facts on one line, the effects one per line, and the fatigue line, which is null when
        /// the item neither costs fatigue nor is a base weapon.
        /// </summary>
        /// <param name="starDamage">What the stars on the item's board add to a weapon's damage (stage 20): its damage line counts it.</param>
        /// <param name="ownerStats">The stats of the mercenary whose board the item is on (Slice D stage 32): its values count them. Null for an item without an owner, or an enemy's.</param>
        /// <param name="ownerAccuracy">An enemy owner's Accuracy (Slice D stage 33), which its card's hit chance counts; 0 otherwise.</param>
        /// <param name="ownerIsEnemy">True for an enemy's item (Slice D stage 42): its requirement line is plain, the enemy being never asked.</param>
        public static void ItemCard(EquippedItem item, out string facts, out string effects, out string fatigue, int starDamage = 0, StatBlock ownerStats = null, int ownerAccuracy = 0, PassiveSpec ownerPassive = null,
            TraitBonus ownerTraits = null, bool ownerIsEnemy = false)
        {
            // Each fact keeps together (stage 22: "치명타 5%" split as "치|명타" on a Korean card); the line breaks between facts only.
            facts = string.Join(FactSeparator, ItemFacts(item, ownerStats, ownerAccuracy, ownerPassive, ownerTraits, ownerIsEnemy).ConvertAll(fact => "<nobr>" + fact + "</nobr>"));
            effects = string.Join("\n", ItemEffects(item, starDamage, ownerStats));
            var parts = new List<string>();
            AddFatigue(parts, item);
            fatigue = parts.Count == 0 ? null : parts[0];
        }

        /// <summary>
        /// An item's lines in Diablo II's tooltip (round 49, the inventory popup's box): the title in its rarity colour, each fact on a
        /// line, the effects in the magic blue, the fatigue last in its own colour. Whoever shows them centres them.
        /// </summary>
        public static string TooltipLines(EquippedItem item)
        {
            var lines = new List<string> { Colored(ItemTitle(item), TitleColor(item)) };
            lines.AddRange(ItemFacts(item));
            foreach (string effect in ItemEffects(item))
            {
                lines.Add(Colored(effect, UiPalette.TooltipEffect));
            }

            AddFatigue(lines, item);
            if (item.Item.Unique)
            {
                lines.Add(Colored(UiStrings.Get(UiKeys.Item.UniqueLine), UiPalette.Unique));
            }

            string recipe = RecipeHint(item);
            if (recipe != null)
            {
                lines.Add(Colored(recipe, UiPalette.TextDim));
            }

            return string.Join("\n", lines);
        }

        /// <summary>The colour of an item's name: Diablo's unique gold for a unique item (stage 23), the tier's colour otherwise.</summary>
        public static Color TitleColor(EquippedItem item)
        {
            return item.Item.Unique ? UiPalette.Unique : UiPalette.Rarity(item.Tier);
        }

        /// <summary>
        /// On a recipe's base or catalyst (stage 23; stage 50: a won battle combines them side by side): what it becomes, with what, or null for an
        /// item in no recipe. The recipe is never hidden (Docs/Design/02_Combat_System.md §4 "유니크의 조합").
        /// </summary>
        public static string RecipeHint(EquippedItem item)
        {
            StaticData data = Managers.Data.Data;
            foreach (RecipeData recipe in data.Recipes.Ordered)
            {
                if (recipe.BaseItem != item.Item.Id && recipe.Catalyst != item.Item.Id)
                {
                    continue;
                }

                string baseTitle = ItemTitle(new EquippedItem(data.Items.Get(recipe.BaseItem), tier: recipe.BaseTier));
                string catalystTitle = ItemTitle(new EquippedItem(data.Items.Get(recipe.Catalyst), tier: recipe.CatalystTier));
                return UiStrings.Get(UiKeys.Item.Recipe, baseTitle, catalystTitle, Name(data.Items.Get(recipe.Result).Name));
            }

            return null;
        }

        /// <summary>An item's category, size, cooldown, where it works and its effects on one line, the fatigue last.</summary>
        public static string ItemSummary(EquippedItem item)
        {
            List<string> parts = ItemFacts(item);
            parts.AddRange(ItemEffects(item));
            AddFatigue(parts, item);
            return string.Join(FactSeparator, parts);
        }

        /// <summary>
        /// What the item costs in fatigue when a battle starts (round 32): in the fatigue's violet for equipment that
        /// costs, "no fatigue" dimmed for a base weapon or (2026-10-19) a unique item, nothing for the rest (Docs/Design/04_Lobby_100Day_Economy.md §3).
        /// </summary>
        static void AddFatigue(List<string> parts, EquippedItem item)
        {
            int cost = FatigueRules.ItemCost(Managers.Data.Data.Balance, item);
            if (cost > 0)
            {
                parts.Add(Colored(UiStrings.Get(UiKeys.Item.FatigueCost, cost), UiPalette.Fatigue));
            }
            else if (item.IsBase)
            {
                parts.Add(Colored(UiStrings.Get(UiKeys.Item.BaseWeapon), UiPalette.TextDim));
            }
            else if (item.Item.Unique)
            {
                parts.Add(Colored(UiStrings.Get(UiKeys.Item.UniqueNoFatigue), UiPalette.TextDim));
            }
        }

        public static string Colored(string text, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }

        /// <summary>The words of an item's category (Docs/Design/02_Combat_System.md §4).</summary>
        public static string CategoryKey(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return UiKeys.Item.Weapon;
                case ItemCategory.Armor: return UiKeys.Item.Armor;
                case ItemCategory.Attack: return UiKeys.Item.Attack;
                case ItemCategory.Support: return UiKeys.Item.Support;
                case ItemCategory.Other: return UiKeys.Item.Other;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        /// <summary>A bag's line (Slice B stage 19): its name, its squares, and what it adds.</summary>
        public static string BagDetail(BagData bag)
        {
            return Name(bag.Name) + " — " + UiStrings.Get(UiKeys.Map.ShopBagSub, bag.Width, bag.Height) + " / " + UiStrings.Get(UiKeys.Map.BagFacts, bag.Area);
        }

        /// <param name="ownerStats">The owner's stats (Slice D stage 33): the critical and hit chances count them. Null without an owner.</param>
        /// <param name="ownerAccuracy">An enemy owner's Accuracy, which the hit chance counts.</param>
        static List<string> ItemFacts(EquippedItem item, StatBlock ownerStats = null, int ownerAccuracy = 0, PassiveSpec ownerPassive = null, TraitBonus ownerTraits = null, bool ownerIsEnemy = false)
        {
            ItemData data = item.Item;
            var facts = new List<string>
            {
                UiStrings.Get(CategoryKey(data.Category)),
                UiStrings.Get(UiKeys.Item.Size, data.Width, data.Height),
            };

            // A melee weapon says so: the stars of a whetstone strengthen only those (stage 20).
            if (data.Unique)
            {
                facts.Add(UiStrings.Get(UiKeys.Item.Unique));
            }

            if (data.Melee)
            {
                facts.Add(UiStrings.Get(UiKeys.Item.Melee));
            }

            facts.Add(CooldownFact(data, ownerStats));

            // A weapon's chance of a critical hit (stage 22), with the owner's dexterity (Slice D stage 33), and the hit chance of what can miss against a
            // target at the baseline: the number green over the data's, red under it (Round 81). Both count the owner's traits (stage 37, Round 84 판정 9).
            BalanceData balance = Managers.Data.Data.Balance;
            if (data.CritChancePercent > 0)
            {
                facts.Add(UiStrings.Get(UiKeys.Item.Crit, AgainstData(StatRules.CritChance(balance, ownerStats, data, StatRules.CritBonusPercent(ownerPassive, data), ownerTraits), data.CritChancePercent * 100)));
            }

            if (CanMiss(data))
            {
                int hit = StatRules.HitChance(balance, StatRules.Accuracy(balance, ownerStats, ownerAccuracy, ownerTraits), 0);
                facts.Add(UiStrings.Get(UiKeys.Item.Hit, AgainstData(hit, balance.HitBasePercent * 100)));
            }
            // A magic blow never misses (2026-10-20 스탯 개정): no hit line at all (Round 88 2차, the user: "마법 장비는 언제나 명중 항목 빼기").

            // An item that works anywhere in the line needs no fact about where.
            if (!data.Rows.IsEveryRow)
            {
                facts.Add(Rows(data.Rows));
            }

            // What the owner needs to wield it (Slice D stage 42, Round 89 A안): the last fact, as Diablo II writes its requirements.
            if (data.HasRequirement)
            {
                facts.Add(RequirementFact(data, ownerStats, ownerIsEnemy));
            }

            return facts;
        }

        /// <summary>
        /// "요구 힘 5", the stat an owner needs and the least of it (Slice D stage 42, Round 89): plain when it is met — by the owner, or, on a card without an
        /// owner (the floor, the inventory, the shop's box), by some living member of the party — and in the blood red when it is not. An enemy's card is
        /// plain: the enemy is never asked. The rule is the domain's (<see cref="StatRules.MeetsRequirement"/>); the card only colours by it.
        /// </summary>
        static string RequirementFact(ItemData data, StatBlock ownerStats, bool ownerIsEnemy)
        {
            string fact = UiStrings.Get(UiKeys.Item.Requires, StatName(data.ReqStat.Value), data.ReqValue);
            bool met = ownerIsEnemy || (ownerStats != null ? StatRules.MeetsRequirement(ownerStats, data) : PartyWields(data));
            return met ? fact : Colored(fact, UiPalette.BloodLight);
        }

        /// <summary>Whether some living member of the expedition going on can wield an item; true with no expedition (nobody to ask).</summary>
        static bool PartyWields(ItemData item)
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            if (expedition == null)
            {
                return true;
            }

            foreach (ExpeditionMember member in expedition.Members)
            {
                if (member.Alive && StatRules.MeetsRequirement(member.Stats, item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The cooldown, "never activates" for an item without effects (stage 20), or "at the start of battle" for the armour (Slice D stage 39, Round 87).
        /// With an owner whose dexterity or wisdom quickens the item (2026-10-20 스탯 개정), the seconds are the owner's: green when quicker than the data's,
        /// red when slower (as the hit and the critical chance are coloured).
        /// </summary>
        static string CooldownFact(ItemData data, StatBlock ownerStats = null)
        {
            if (data.IsPassive)
            {
                return UiStrings.Get(UiKeys.Item.NoActivation);
            }

            if (data.Trigger == ItemTrigger.BattleStart)
            {
                // Slice D stage 56 (Design/38 판정 4): an item that only raises the maximum HP shows it on the board at once.
                bool onlyMaxHp = true;
                foreach (ItemEffect effect in data.Effects)
                {
                    onlyMaxHp &= effect.Kind == EffectKind.MaxHp;
                }

                return UiStrings.Get(onlyMaxHp ? UiKeys.Item.WhileEquipped : UiKeys.Item.BattleStart);
            }

            BalanceData balance = Managers.Data.Data.Balance;
            int percent = StatRules.CooldownPercent(balance, ownerStats, data);
            // Round 88 4차 (2026-10-20, the user: "소수점 둘째 자리에서 반올림 — 보여주는 항목에서"): the card's seconds keep one decimal, rounded.
            if (percent == StatRules.Full)
            {
                return UiStrings.Get(UiKeys.Item.Cooldown, RoundedSeconds(data.CooldownMs));
            }

            string seconds = RoundedSeconds(StatRules.CooldownMs(balance, data.CooldownMs, percent));
            return UiStrings.Get(UiKeys.Item.Cooldown, Colored(seconds, percent < StatRules.Full ? UiPalette.Good : UiPalette.BloodLight));
        }



        /// <param name="starDamage">What stars add to the item's weapon damage on its board (stage 20, as the battle adds it), or 0.</param>
        static List<string> ItemEffects(EquippedItem item, int starDamage = 0, StatBlock ownerStats = null)
        {
            var effects = new List<string>();
            BalanceData balance = Managers.Data.Data.Balance;
            foreach (ItemEffect effect in item.Item.Effects)
            {
                effects.Add(Effect(effect, item, balance, starDamage, ownerStats));
            }

            // A helm's work (Slice D stage 39, Round 87): the points it takes off the critical chance of the weapons that hit its owner, whatever its tier.
            if (item.Item.CritTakenPercent > 0)
            {
                effects.Add(UiStrings.Get(UiKeys.Item.CritTaken, item.Item.CritTakenPercent));
            }

            // A star item's work (stage 20): what a melee weapon on one of its stars deals more, at its tier.
            if (item.Item.Stars.Count > 0)
            {
                effects.Add(UiStrings.Get(UiKeys.Item.StarDamage, Amount(item.StarDamage)));
            }

            return effects;
        }

        /// <summary>
        /// What the stars on a board say of an item there (Slice B stage 20; Docs/Architecture/12_UI.md "격자 보드"), for its card: on a melee
        /// weapon, the star items that strengthen it and by how much in gold ("★ 숫돌 +1"); on a star item, the stars a melee weapon lies on
        /// and which ("걸린 ★ 1/2 — 롱소드"). Null for anything else, and for a weapon no star reaches.
        /// </summary>
        public static string StarLine(ItemBoard board, BoardItem placed)
        {
            if (board == null || placed == null)
            {
                return null;
            }

            if (placed.Item.Item.Stars.Count > 0)
            {
                List<BoardItem> lit = StarRules.Lit(board, placed);
                int stars = placed.Item.Item.Stars.Count;
                return lit.Count == 0
                    ? UiStrings.Get(UiKeys.Item.StarLitNone, stars)
                    : UiStrings.Get(UiKeys.Item.StarLit, lit.Count, stars, string.Join(", ", lit.ConvertAll(weapon => Name(weapon.Item.Item.Name))));
            }

            List<BoardItem> sources = StarRules.Sources(board, placed);
            if (sources.Count == 0)
            {
                return null;
            }

            var names = new List<string>();
            foreach (BoardItem source in sources)
            {
                string name = Name(source.Item.Item.Name);
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }

            return Colored(UiStrings.Get(UiKeys.Item.StarBonus, string.Join("·", names), Amount(StarRules.DamageOn(board, placed))), UiPalette.StarLit);
        }

        /// <summary>Where in its line the owner must stand: "only in the front row", "only within the rear 3 rows".</summary>
        public static string Rows(RowSpan rows)
        {
            if (rows.From == RowEnd.Front)
            {
                return rows.Reach == 1 ? UiStrings.Get(UiKeys.Item.RowsFrontOne) : UiStrings.Get(UiKeys.Item.RowsFront, rows.Reach);
            }

            return rows.Reach == 1 ? UiStrings.Get(UiKeys.Item.RowsBackOne) : UiStrings.Get(UiKeys.Item.RowsBack, rows.Reach);
        }

        /// <summary>One effect of an item at its tier and power, as its card says it: "맨 앞 적에게 피해 2-4", "자신에게 보호막 2", "화상 2".</summary>
        /// <param name="starDamage">What stars add to a weapon's damage on its board (stage 20, as the battle adds it), in hundredths, or 0.</param>
        /// <param name="ownerStats">The owner's stats (Slice D stage 32), counted as the battle counts them; null for none.</param>
        public static string Effect(ItemEffect effect, EquippedItem item, BalanceData balance, int starDamage = 0, StatBlock ownerStats = null)
        {
            return UiStrings.Get(EffectKey(effect.Kind), Target(effect), EffectValue(effect, item, balance, starDamage, ownerStats));
        }

        /// <summary>
        /// The value of one effect of an item at its tier and power (stage 22): damage as a range with the stars of its board on both ends
        /// ("2-4"), a heal or a shield whole ("3"), a burn its stacks.
        /// </summary>
        public static string EffectValue(ItemEffect effect, EquippedItem item, BalanceData balance, int starDamage = 0, StatBlock ownerStats = null)
        {
            // The owner's stat grows the value as it does in battle (Slice D stage 32): the damage after the stars, a burn rounded.
            int percent = StatRules.PowerPercent(balance, ownerStats, item.Item);
            switch (effect.Kind)
            {
                case EffectKind.Damage:
                {
                    int stars = item.Item.Category == ItemCategory.Weapon ? starDamage : 0;
                    return Range(StatRules.Scale(item.Min(balance, effect) + stars, percent), StatRules.Scale(item.Max(balance, effect) + stars, percent));
                }

                case EffectKind.Burn:
                case EffectKind.Bleed:
                case EffectKind.Regen:
                case EffectKind.Cleanse:
                case EffectKind.Freeze:
                case EffectKind.Blind:
                case EffectKind.Aim:
                case EffectKind.Keen:
                case EffectKind.Fury:
                case EffectKind.Haste:
                case EffectKind.Vulnerable:
                    // Stacks (stage 44 added the bleed, the regen and the cleanse to the burn; stage 45 the seven states): a whole number, rounded half up by the stat.
                    return StatRules.ScaleStacks(item.Magnitude(balance, effect), percent).ToString(CultureInfo.InvariantCulture);
                case EffectKind.Stagger:
                case EffectKind.Charge:
                    // Milliseconds, shown as seconds (stage 44).
                    return Seconds(StatRules.Scale(item.Magnitude(balance, effect), percent));
                default:
                    return Amount(StatRules.Scale(item.Magnitude(balance, effect), percent)).ToString(CultureInfo.InvariantCulture);
            }
        }

        // ---- The stats (Slice D stage 32, Round 81; Docs/Architecture/12_UI.md "스탯의 표시") ----

        public static string StatName(Stat stat)
        {
            switch (stat)
            {
                case Stat.Str: return UiStrings.Get(UiKeys.Stat.Str);
                case Stat.Dex: return UiStrings.Get(UiKeys.Stat.Dex);
                case Stat.Con: return UiStrings.Get(UiKeys.Stat.Con);
                case Stat.Int: return UiStrings.Get(UiKeys.Stat.Int);
                case Stat.Wis: return UiStrings.Get(UiKeys.Stat.Wis);
                case Stat.End: return UiStrings.Get(UiKeys.Stat.End);
                default: throw new ArgumentOutOfRangeException(nameof(stat), stat, "No name for this stat.");
            }
        }

        /// <summary>
        /// A stat's value against the power baseline (Round 81; 2026-10-20 스탯 개정: PowerBaseline 5, the middle of a job's 1..8 — the rolls' baseline 10 would
        /// paint every stat red): green above it, a soft red below it, white at it.
        /// </summary>
        public static Color StatColor(int value)
        {
            int baseline = Managers.Data.Data.Balance.PowerBaseline;
            return value > baseline ? UiPalette.Good : value < baseline ? UiPalette.BloodLight : UiPalette.Text;
        }

        /// <summary>"힘 12": the name dimmed and the value in its colour.</summary>
        public static string StatPair(Stat stat, int value)
        {
            return Colored(StatName(stat), UiPalette.TextDim) + " " + Colored(value.ToString(CultureInfo.InvariantCulture), StatColor(value));
        }

        /// <summary>The six as pairs on one line in columns this wide (the stagecoach's candidates, Round 81): TMP's position tags keep them aligned.</summary>
        /// <param name="against">The job's base values (Slice D stage 34, Round 81 판정 3): each stat that differs gets a small ▲N/▼N after it; null for none.</param>
        public static string StatColumns(StatBlock stats, float column, StatBlock against = null)
        {
            var line = new System.Text.StringBuilder();
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                Stat stat = StatBlock.All[i];
                line.Append("<pos=").Append((i * column).ToString("0", CultureInfo.InvariantCulture)).Append('>');
                line.Append(StatPair(stat, stats[stat]));
                int delta = against == null ? 0 : stats[stat] - against[stat];
                if (delta != 0)
                {
                    string mark = UiStrings.Get(delta > 0 ? UiKeys.Stat.Up : UiKeys.Stat.Down, Math.Abs(delta));
                    line.Append("<size=70%><voffset=0.45em>").Append(Colored(mark, delta > 0 ? UiPalette.Good : UiPalette.BloodLight)).Append("</voffset></size>");
                }
            }

            return line.ToString();
        }

        /// <summary>
        /// A stat's value on a barracks line (Slice D stage 35, Round 82 안 A): the value, and when the level-up just grew it, a small green "+N" raised
        /// after it — the roll stays where the stats are read until the window closes. An invisible copy of the "+N" before the value keeps the value
        /// in the middle of its cell, under the others.
        /// </summary>
        public static string StatValue(int value, int gain)
        {
            string words = value.ToString(CultureInfo.InvariantCulture);
            if (gain <= 0)
            {
                return words;
            }

            string plus = UiStrings.Get(UiKeys.Lobby.StatGain, gain);
            // TMP's alpha tag has no closing form: the opaque one ends the invisible copy.
            return "<size=55%><alpha=#00>" + plus + "<alpha=#FF></size>" + words + "<size=55%><voffset=0.9em>" + Colored(plus, UiPalette.Good) + "</voffset></size>";
        }

        /// <summary>A strip chip's level (Slice D stage 35): "Lv 2", or in gold with a ▲ before it when the barracks can level the mercenary up.</summary>
        public static string ChipLevel(int level, bool ready)
        {
            string words = UiStrings.Get(UiKeys.Lobby.Level, level);
            return ready ? Colored("<size=85%>" + UiStrings.Get(UiKeys.Lobby.LevelUpMark) + "</size>" + words, UiPalette.StarLit) : words;
        }

        /// <summary>The six as pairs on one line, a gap of spaces apart.</summary>
        public static string StatLine(StatBlock stats, string gap)
        {
            var pairs = new List<string>();
            foreach (Stat stat in StatBlock.All)
            {
                pairs.Add(StatPair(stat, stats[stat]));
            }

            return string.Join(gap, pairs);
        }

        /// <summary>Whether any effect of the item can miss (Slice D stage 33): a weapon's or an attack item's damage or burn.</summary>
        static bool CanMiss(ItemData item)
        {
            foreach (ItemEffect effect in item.Effects)
            {
                if (StatRules.CanMiss(item, effect))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A chance in ten-thousandths as percent words with at most one decimal, rounded half up at the second: 450 is "4.5", 7667 "76.7", 8200 "82"
        /// (Slice D stage 33; 2026-10-20 Round 88 4차, the user: "소수점 둘째 자리에서 반올림 — 보여지는 화면에서": the cards, the battle's log and subtitles alike).
        /// </summary>
        public static string Percent(int tenThousandths)
        {
            int tenths = (tenThousandths + 5) / 10;
            return (tenths / 10m).ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>A chance's words coloured against the data's: green above it, red under it, plain when the same (Round 81).</summary>
        static string AgainstData(int tenThousandths, int data)
        {
            string words = Percent(tenThousandths);
            return tenThousandths == data ? words : Colored(words, tenThousandths > data ? UiPalette.Good : UiPalette.BloodLight);
        }

        /// <summary>A percent as a multiplier: 120 is "1.2", 125 "1.25", 90 "0.9".</summary>
        public static string Multiplier(int percent)
        {
            return (percent / 100m).ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string PotionDetails(PotionData potion)
        {
            switch (potion.Effect)
            {
                case PotionEffect.Heal: return UiStrings.Get(UiKeys.Potion.Heal, Amount(potion.Magnitude));
                case PotionEffect.Shield: return UiStrings.Get(UiKeys.Potion.Shield, Amount(potion.Magnitude));
                case PotionEffect.Cleanse: return UiStrings.Get(UiKeys.Potion.Cleanse, potion.Magnitude);
                case PotionEffect.Regen: return UiStrings.Get(UiKeys.Potion.Regen, potion.Magnitude);
                default: throw new ArgumentOutOfRangeException(nameof(potion), potion.Effect, "No text for this potion effect.");
            }
        }

        static string EffectKey(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Damage: return UiKeys.Effect.Damage;
                case EffectKind.Heal: return UiKeys.Effect.Heal;
                case EffectKind.Shield: return UiKeys.Effect.Shield;
                case EffectKind.Burn: return UiKeys.Effect.Burn;
                case EffectKind.Bleed: return UiKeys.Effect.Bleed;
                case EffectKind.Regen: return UiKeys.Effect.Regen;
                case EffectKind.Thorns: return UiKeys.Effect.Thorns;
                case EffectKind.Cleanse: return UiKeys.Effect.Cleanse;
                case EffectKind.Stagger: return UiKeys.Effect.Stagger;
                case EffectKind.Charge: return UiKeys.Effect.Charge;
                case EffectKind.Freeze: return UiKeys.Effect.Freeze;
                case EffectKind.Blind: return UiKeys.Effect.Blind;
                case EffectKind.Aim: return UiKeys.Effect.Aim;
                case EffectKind.Keen: return UiKeys.Effect.Keen;
                case EffectKind.Fury: return UiKeys.Effect.Fury;
                case EffectKind.Haste: return UiKeys.Effect.Haste;
                case EffectKind.Vulnerable: return UiKeys.Effect.Vulnerable;
                case EffectKind.MaxHp: return UiKeys.Effect.MaxHp;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "No text for this effect.");
            }
        }

        /// <summary>The name of one of the seven states of stage 45 (빙결·암흑·조준·예리·격분·가속·취약), for the battle log.</summary>
        public static string StateName(EffectKind state)
        {
            switch (state)
            {
                case EffectKind.Freeze: return UiStrings.Get(UiKeys.State.Freeze);
                case EffectKind.Blind: return UiStrings.Get(UiKeys.State.Blind);
                case EffectKind.Aim: return UiStrings.Get(UiKeys.State.Aim);
                case EffectKind.Keen: return UiStrings.Get(UiKeys.State.Keen);
                case EffectKind.Fury: return UiStrings.Get(UiKeys.State.Fury);
                case EffectKind.Haste: return UiStrings.Get(UiKeys.State.Haste);
                case EffectKind.Vulnerable: return UiStrings.Get(UiKeys.State.Vulnerable);
                default: throw new ArgumentOutOfRangeException(nameof(state), state, "No name for this state.");
            }
        }

        /// <summary>Who an effect hits: "the front enemy", "the rear 2 enemies", "self" and so on.</summary>
        static string Target(ItemEffect effect)
        {
            switch (effect.Target)
            {
                case TargetMode.EnemyFront:
                    return effect.Reach == 1 ? UiStrings.Get(UiKeys.Target.EnemyFront) : UiStrings.Get(UiKeys.Target.EnemyFrontMany, effect.Reach);
                case TargetMode.EnemyBack:
                    return effect.Reach == 1 ? UiStrings.Get(UiKeys.Target.EnemyBack) : UiStrings.Get(UiKeys.Target.EnemyBackMany, effect.Reach);
                case TargetMode.EnemyAll: return UiStrings.Get(UiKeys.Target.EnemyAll);
                case TargetMode.Self: return UiStrings.Get(UiKeys.Target.Self);
                case TargetMode.AllyLowestHp: return UiStrings.Get(UiKeys.Target.AllyLowestHp);
                case TargetMode.AllyAll: return UiStrings.Get(UiKeys.Target.AllyAll);
                default: throw new ArgumentOutOfRangeException(nameof(effect), effect.Target, "No text for this target.");
            }
        }
    }
}
