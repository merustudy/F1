using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The battle rules (Docs/Design/02_Combat_System.md). Time is integer milliseconds and the
    /// engine jumps from one event to the next, so the outcome depends only on the setup and the
    /// recorded player inputs: never on frame rate, wall clock time or presentation.
    ///
    /// Order at one timestamp: the ticks (every unit's burn, then every unit's bleed, then every unit's regen: Slice D stage 44),
    /// item activations (party first, unit order, slot order), storm tick, then player inputs made at that time.
    ///
    /// Rows: each side is a line, one unit per row from row 1 back. When a unit dies, the ones
    /// behind it advance one row at once, before anything else is processed, and every item of
    /// that side is judged again: where an item works is counted on the living line.
    /// </summary>
    public sealed class BattleEngine
    {
        /// <summary>A safety net only. The storm ends every battle long before this.</summary>
        const int MaxBattleMs = 30 * 60 * 1000;

        readonly BalanceData _balance;
        readonly List<BattleUnit> _party = new List<BattleUnit>();
        readonly List<BattleUnit> _enemies = new List<BattleUnit>();
        readonly PotionData[] _potions;
        readonly Pcg32 _battleRng;
        readonly Pcg32 _inputRng;
        readonly Pcg32 _fatigueRng;
        readonly Pcg32 _damageRng;
        readonly List<BattleEvent> _events = new List<BattleEvent>();
        readonly List<BattleInput> _inputs = new List<BattleInput>();

        int _nextBurnTickMs;
        int _nextStormTickMs;
        int _stormTicks;

        public BattleEngine(BattleSetup setup)
        {
            Setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _balance = setup.Balance ?? throw new ArgumentException("BattleSetup.Balance is missing.", nameof(setup));
            if (setup.Party == null || setup.Party.Count == 0 || setup.Enemies == null || setup.Enemies.Count == 0)
            {
                throw new ArgumentException("A battle needs at least one unit on each side.", nameof(setup));
            }

            RequireFormation(setup.Party, "Party");
            RequireFormation(setup.Enemies, "Enemies");

            for (int i = 0; i < setup.Party.Count; i++)
            {
                _party.Add(CreateUnit(BattleSide.Party, i, setup.Party[i], 0, setup.Party.Count));
            }

            for (int i = 0; i < setup.Enemies.Count; i++)
            {
                _enemies.Add(CreateUnit(BattleSide.Enemy, i, setup.Enemies[i], setup.EnemyCooldownPermille, setup.Enemies.Count));
            }

            _potions = new PotionData[_balance.PotionSlots];
            if (setup.Potions != null)
            {
                if (setup.Potions.Count > _potions.Length)
                {
                    throw new ArgumentException("More potions than potion slots.", nameof(setup));
                }

                for (int i = 0; i < setup.Potions.Count; i++)
                {
                    _potions[i] = setup.Potions[i];
                }
            }

            _battleRng = new Pcg32(setup.Seed, RngStream.Battle);
            _inputRng = new Pcg32(setup.Seed, RngStream.Input);
            _fatigueRng = new Pcg32(setup.Seed, RngStream.Fatigue);
            _damageRng = new Pcg32(setup.Seed, RngStream.Damage);
            _nextBurnTickMs = _balance.BurnTickMs;
            _nextStormTickMs = _balance.StormStartMs;

            Log(BattleEventKind.BattleStarted, UnitRef.None, UnitRef.None, null, 0, 0, 0);

            // The fatigue a mercenary brings in is judged at once (Docs/Design/04_Lobby_100Day_Economy.md §3): at the maximum it
            // collapses, at the threshold without a state it breaks down. A state it brings in sets its items' first cooldown.
            foreach (BattleUnit unit in _party)
            {
                if (unit.State != null)
                {
                    foreach (BattleItemState item in unit.Items)
                    {
                        item.NextFireMs = CooldownOf(unit, item);
                    }
                }

                ResolveFatigue(unit);
            }

            CheckEnd();
            if (Result != BattleResult.Ongoing)
            {
                return;
            }

            ApplyBattleStartPassives(_party);
            ApplyBattleStartPassives(_enemies);
            // Then the items that activate when a battle starts (Slice D stage 39, the armour): the party first, unit order, slot order.
            ApplyBattleStartItems(_party);
            if (Result == BattleResult.Ongoing)
            {
                ApplyBattleStartItems(_enemies);
            }
        }

        public BattleSetup Setup { get; }
        public int TimeMs { get; private set; }
        public BattleResult Result { get; private set; }
        public IReadOnlyList<BattleUnit> Party => _party;
        public IReadOnlyList<BattleUnit> Enemies => _enemies;

        /// <summary>Potion slots. A null entry is empty.</summary>
        public IReadOnlyList<PotionData> Potions => _potions;

        public int PotionReadyMs { get; private set; }
        public int RetreatReadyMs { get; private set; }

        /// <summary>Damage the next storm tick will deal to every unit.</summary>
        public int NextStormDamage => _balance.StormBaseDamage + _balance.StormGrowth * _stormTicks;
        public IReadOnlyList<BattleEvent> Events => _events;

        /// <summary>The accepted player inputs so far, in order. Replaying them reproduces the battle.</summary>
        public IReadOnlyList<BattleInput> Inputs => _inputs;

        /// <summary>Rebuilds a battle from its setup and recorded inputs, up to a battle time.</summary>
        public static BattleEngine Replay(BattleSetup setup, IReadOnlyList<BattleInput> inputs, int untilMs)
        {
            var engine = new BattleEngine(setup);
            if (inputs != null)
            {
                foreach (BattleInput input in inputs)
                {
                    engine.ApplyRecordedInput(input);
                }
            }

            if (engine.Result == BattleResult.Ongoing && untilMs > engine.TimeMs)
            {
                engine.AdvanceTo(untilMs);
            }

            return engine;
        }

        public BattleUnit Unit(UnitRef unit)
        {
            return unit.Side == BattleSide.Party ? _party[unit.Index] : _enemies[unit.Index];
        }

        /// <summary>
        /// Processes every automatic event up to and including <paramref name="targetMs"/>.
        /// Advancing in many small steps or one large step gives the same result.
        /// </summary>
        public void AdvanceTo(int targetMs)
        {
            if (targetMs < TimeMs)
            {
                throw new ArgumentOutOfRangeException(nameof(targetMs), targetMs, "Battle time cannot go backwards.");
            }

            while (Result == BattleResult.Ongoing)
            {
                int next = NextAutomaticEventMs();
                if (next > targetMs)
                {
                    break;
                }

                TimeMs = next;
                ProcessAutomaticEvents(next);
            }

            if (Result == BattleResult.Ongoing)
            {
                TimeMs = targetMs;
            }
        }

        /// <summary>Runs with no further input until the battle ends.</summary>
        public void RunToEnd()
        {
            while (Result == BattleResult.Ongoing)
            {
                int next = NextAutomaticEventMs();
                if (next > MaxBattleMs)
                {
                    throw new InvalidOperationException("The battle did not end. Check the storm constants.");
                }

                AdvanceTo(next);
            }
        }

        public bool CanUsePotion(int potionSlot, int partyIndex)
        {
            return Result == BattleResult.Ongoing
                && potionSlot >= 0 && potionSlot < _potions.Length && _potions[potionSlot] != null
                && TimeMs >= PotionReadyMs
                && partyIndex >= 0 && partyIndex < _party.Count && _party[partyIndex].Alive;
        }

        /// <summary>Uses a potion at the current battle time. Returns false, and records nothing, when it cannot be used.</summary>
        public bool TryUsePotion(int potionSlot, int partyIndex)
        {
            if (!CanUsePotion(potionSlot, partyIndex))
            {
                return false;
            }

            _inputs.Add(new BattleInput(TimeMs, BattleInputKind.UsePotion, potionSlot, partyIndex));

            PotionData potion = _potions[potionSlot];
            BattleUnit target = _party[partyIndex];
            _potions[potionSlot] = null;
            PotionReadyMs = TimeMs + _balance.PotionCooldownMs;
            Log(BattleEventKind.PotionUsed, UnitRef.None, target.Ref, potion.Id, potionSlot, 0, 0);

            switch (potion.Effect)
            {
                case PotionEffect.Heal:
                    ApplyHeal(target, potion.Magnitude, UnitRef.None, potion.Id);
                    break;
                case PotionEffect.Shield:
                    AddShield(target, potion.Magnitude, UnitRef.None, potion.Id);
                    break;
                case PotionEffect.Cleanse:
                    Cleanse(target, potion.Magnitude, UnitRef.None, potion.Id);
                    break;
                case PotionEffect.Regen:
                    // Stage 46: regen stacks like an item's (02 §5), logged by the potion's id; the ticks heal from the next second.
                    AddRegen(target, potion.Magnitude, UnitRef.None, potion.Id);
                    break;
                default:
                    throw new InvalidOperationException($"Potion effect {potion.Effect} is not implemented.");
            }

            return true;
        }

        public bool CanRetreat => Result == BattleResult.Ongoing && TimeMs >= RetreatReadyMs;

        /// <summary>Attempts to retreat at the current battle time. Returns false when no attempt can be made now.</summary>
        public bool TryRetreat()
        {
            if (!CanRetreat)
            {
                return false;
            }

            _inputs.Add(new BattleInput(TimeMs, BattleInputKind.Retreat, 0, 0));

            int roll = _inputRng.NextInt(100);
            bool success = roll < _balance.RetreatChancePercent;
            Log(BattleEventKind.RetreatAttempted, UnitRef.None, UnitRef.None, null, _balance.RetreatChancePercent, roll, success ? 1 : 0);
            if (success)
            {
                End(BattleResult.Retreated);
            }
            else
            {
                RetreatReadyMs = TimeMs + _balance.RetreatCooldownMs;
            }

            return true;
        }

        /// <summary>Re-applies an input recorded earlier. It must still be valid at its time.</summary>
        public void ApplyRecordedInput(BattleInput input)
        {
            AdvanceTo(input.TimeMs);

            bool accepted = input.Kind == BattleInputKind.UsePotion
                ? TryUsePotion(input.PotionSlot, input.PartyIndex)
                : TryRetreat();
            if (!accepted)
            {
                throw new InvalidOperationException(
                    $"Recorded input {input.Kind} at {input.TimeMs} ms is not valid for this battle.");
            }
        }

        /// <summary>Units come in unit order and stand one per row from row 1 back: the first in row 1, the next in row 2.</summary>
        static void RequireFormation(IReadOnlyList<BattleUnitSetup> units, string what)
        {
            if (units.Count > BattleRows.Count)
            {
                throw new ArgumentException($"{what}: a side has at most {BattleRows.Count} units, one per row.");
            }

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Row != BattleRows.Front + i)
                {
                    throw new ArgumentException(
                        $"{what}: unit {i} stands in row {units[i].Row}, but units are listed one per row from row {BattleRows.Front} back.");
                }
            }
        }

        /// <param name="lineLength">How many units the side starts with; they stand in rows 1..lineLength.</param>
        BattleUnit CreateUnit(BattleSide side, int index, BattleUnitSetup setup, int cooldownPermille, int lineLength)
        {
            if (setup.MaxHp < 1 || setup.Hp < 0 || setup.Hp > setup.MaxHp)
            {
                throw new ArgumentException($"Unit '{setup.SourceId}' has invalid HP {setup.Hp}/{setup.MaxHp}.");
            }

            var items = new List<BattleItemState>();
            if (setup.StarDamage != null && (setup.Items == null || setup.StarDamage.Count != setup.Items.Count))
            {
                throw new ArgumentException($"Unit '{setup.SourceId}': the star damage does not match its items one for one.");
            }

            if (setup.Items != null)
            {
                for (int slot = 0; slot < setup.Items.Count; slot++)
                {
                    EquippedItem equipped = setup.Items[slot];
                    if (equipped == null)
                    {
                        throw new ArgumentException($"Unit '{setup.SourceId}' has an empty entry on its board.");
                    }

                    bool active = Works(equipped.Item, setup.Row, lineLength);
                    int star = setup.StarDamage == null ? 0 : setup.StarDamage[slot];
                    // 2026-10-20 스탯 개정: the owner's dexterity or wisdom quickens its equipment (an enemy has no stats: 100%).
                    items.Add(new BattleItemState(slot, equipped, active, EffectiveCooldown(equipped.Item.CooldownMs, cooldownPermille, StatRules.CooldownPercent(_balance, setup.Stats, equipped.Item)), star));
                }
            }

            var unit = new BattleUnit(side, index, setup, items);
            if (setup.HasDog && setup.Hp == 0)
            {
                // A mercenary who starts at 0 HP is already at death's door.
                unit.InDog = true;
                unit.GraceEndMs = _balance.DogGraceMs;
            }

            return unit;
        }

        /// <summary>
        /// Enters every phase the unit's HP has reached (Slice C stage 28): the items of the lowest phase reached replace the board, judged where the unit
        /// stands as at the start, with every cooldown starting over from now. One event per phase entered, so the log says which were passed.
        /// </summary>
        void EnterPhasesReached(BattleUnit unit)
        {
            IReadOnlyList<BattlePhaseSetup> phases = unit.Setup.Phases;
            if (phases == null)
            {
                return;
            }

            int permille = unit.Side == BattleSide.Enemy ? Setup.EnemyCooldownPermille : 0;
            while (unit.Phase < phases.Count && (long)unit.Hp * 100 <= (long)unit.MaxHp * phases[unit.Phase].HpPercent)
            {
                BattlePhaseSetup phase = phases[unit.Phase];
                int lineLength = LineLength(unit.Side == BattleSide.Party ? _party : _enemies);
                var items = new List<BattleItemState>();
                for (int slot = 0; slot < phase.Items.Count; slot++)
                {
                    EquippedItem equipped = phase.Items[slot];
                    var state = new BattleItemState(slot, equipped, Works(equipped.Item, unit.Row, lineLength), EffectiveCooldown(equipped.Item.CooldownMs, permille, StatRules.CooldownPercent(_balance, unit.Setup.Stats, equipped.Item)), 0);
                    state.NextFireMs = TimeMs + CooldownOf(unit, state);
                    items.Add(state);
                }

                unit.EnterPhase(unit.Phase + 1, items);
                Log(BattleEventKind.PhaseChanged, UnitRef.None, unit.Ref, null, unit.Phase, phase.HpPercent, unit.Hp);
            }
        }

        /// <summary>Whether an item activates where its owner stands: never for an item without effects (Slice B stage 20), else where its rows say.</summary>
        static bool Works(ItemData item, int row, int lineLength)
        {
            return !item.IsPassive && item.UsableIn(row, lineLength);
        }

        /// <summary>base x (1000 + permille) / 1000, rounded to the nearest millisecond, never below MinCooldownMs.</summary>
        int EffectiveCooldown(int baseMs, int permille)
        {
            return EffectiveCooldown(baseMs, permille, StatRules.Full);
        }

        /// <summary>
        /// base x (1000 + permille) / 1000 x statPercent / 100 (2026-10-20 스탯 개정: the owner's dexterity or wisdom, <see cref="StatRules.CooldownPercent"/>),
        /// rounded to the nearest millisecond, never below MinCooldownMs. With no affinity it is <see cref="StatRules.CooldownMs"/>, which the cards show.
        /// </summary>
        int EffectiveCooldown(int baseMs, int permille, int statPercent)
        {
            long scaled = ((long)baseMs * (1000 + permille) * statPercent + 50000) / 100000;
            return scaled < _balance.MinCooldownMs ? _balance.MinCooldownMs : (int)scaled;
        }

        /// <summary>The cooldown an item of a unit cycles with now: its own, changed by the unit's fatigue state (an affliction slows, a virtue quickens).</summary>
        /// <summary>
        /// The cooldown an item of this owner fills next: its own with the fatigue state's percent and, stage 45, the owner's freeze stacks slowing it and
        /// its haste stacks quickening it (percent points added together), never under MinCooldownMs. Read when the next activation is scheduled, so a
        /// stack applied mid-cooldown tells from the next one.
        /// </summary>
        int CooldownOf(BattleUnit owner, BattleItemState item)
        {
            int percent = (owner.State?.CooldownPercent ?? 0) + owner.Freeze * _balance.FreezePercentPerStack - owner.Haste * _balance.HastePercentPerStack;
            return percent == 0 ? item.CooldownMs : EffectiveCooldown(item.CooldownMs, percent * 10);
        }

        /// <summary>Battle time of the next burn tick, item activation or storm tick.</summary>
        public int NextAutomaticEventMs()
        {
            int next = Math.Min(_nextBurnTickMs, _nextStormTickMs);
            next = Math.Min(next, NextItemMs(_party));
            return Math.Min(next, NextItemMs(_enemies));
        }

        static int NextItemMs(List<BattleUnit> units)
        {
            int next = int.MaxValue;
            foreach (BattleUnit unit in units)
            {
                if (!unit.Alive)
                {
                    continue;
                }

                foreach (BattleItemState item in unit.Items)
                {
                    if (item.Active && item.Cycles && item.NextFireMs < next)
                    {
                        next = item.NextFireMs;
                    }
                }
            }

            return next;
        }

        void ProcessAutomaticEvents(int now)
        {
            if (now == _nextBurnTickMs)
            {
                _nextBurnTickMs += _balance.BurnTickMs;
                Ticks();
                if (Result != BattleResult.Ongoing)
                {
                    return;
                }
            }

            FireItems(_party, now);
            if (Result != BattleResult.Ongoing)
            {
                return;
            }

            FireItems(_enemies, now);
            if (Result != BattleResult.Ongoing)
            {
                return;
            }

            if (now == _nextStormTickMs)
            {
                _nextStormTickMs += _balance.StormTickMs;
                StormTick();
            }
        }

        void FireItems(List<BattleUnit> units, int now)
        {
            foreach (BattleUnit unit in units)
            {
                foreach (BattleItemState item in unit.Items)
                {
                    if (!unit.Alive)
                    {
                        break;
                    }

                    if (!item.Active || !item.Cycles || item.NextFireMs != now)
                    {
                        continue;
                    }

                    item.NextFireMs += CooldownOf(unit, item);
                    Activate(unit, item);
                    if (Result != BattleResult.Ongoing)
                    {
                        return;
                    }
                }
            }
        }

        void Activate(BattleUnit owner, BattleItemState item)
        {
            ItemData data = item.Equipped.Item;
            Log(BattleEventKind.ItemActivated, owner.Ref, UnitRef.None, data.Id, item.SlotIndex, 0, 0);

            // The owner's stat grows the item's power (Slice D stage 32): an enemy has no stats, an item may grow with none.
            int statPercent = StatRules.PowerPercent(_balance, owner.Setup.Stats, data);
            foreach (ItemEffect effect in data.Effects)
            {
                int magnitude = item.Equipped.Magnitude(_balance, effect);
                bool weaponDamage = effect.Kind == EffectKind.Damage && data.Category == ItemCategory.Weapon;
                if (effect.Kind == EffectKind.Damage)
                {
                    // Damage is rolled once for the attack (stage 22): every unit it hits takes the same roll. The stars on a weapon add
                    // next (stage 20); a critical hit is rolled for each unit hit, then the weapon-power passive grows it all.
                    magnitude = _damageRng.NextInt(item.Equipped.Min(_balance, effect), item.Equipped.Max(_balance, effect));
                    if (weaponDamage)
                    {
                        magnitude += item.StarDamage;
                    }
                }
                else if (ItemEffect.CountsStacks(effect.Kind))
                {
                    magnitude = StatRules.ScaleStacks(magnitude, statPercent);
                }
                else
                {
                    // Hundredths, or a stagger's and a charge's milliseconds (stage 44): rounded down, at least 1.
                    magnitude = StatRules.Scale(magnitude, statPercent);
                }

                foreach (BattleUnit target in ResolveTargets(owner, effect))
                {
                    if (!target.Alive)
                    {
                        continue;
                    }

                    switch (effect.Kind)
                    {
                        case EffectKind.Damage:
                        {
                            // Each unit hit rolls its own hit first (Slice D stage 33): a miss deals nothing and triggers nothing.
                            if (!Lands(owner, target, data, effect))
                            {
                                continue;
                            }

                            int dealt = magnitude;
                            // A blow on one's own side (2026-10-21: any effect may go either way) never crits, triggers no passive and meets no thorns.
                            bool foe = target.Side != owner.Side;
                            bool critical = weaponDamage && foe && RollCritical(owner, target, data, ref dealt);
                            // Roll, stars, critical hit, then the owner's stat, then the weapon-power passive (Docs/Design/02_Combat_System.md §4),
                            // then what the target's vulnerable stacks add (stage 45).
                            dealt = StatRules.Scale(dealt, statPercent);
                            if (weaponDamage)
                            {
                                dealt = WithWeaponPower(owner, dealt);
                            }

                            if (target.Vulnerable > 0)
                            {
                                dealt = (int)((long)dealt * (100 + (long)target.Vulnerable * _balance.VulnerablePercentPerStack) / 100);
                            }

                            if (critical)
                            {
                                Log(BattleEventKind.CriticalHit, owner.Ref, target.Ref, data.Id, dealt, _lastCritRoll, _lastCritChance);
                            }

                            ApplyDamage(target, dealt, owner.Ref, data.Id);
                            if (critical)
                            {
                                CriticalFatigue(owner, target);
                            }

                            if (weaponDamage && foe && target.Alive)
                            {
                                ApplyTriggeredPassive(owner, PassiveTrigger.WeaponHit, target);
                            }

                            // Thorns (stage 44): a melee weapon's blow is dealt back by the target's thorns while it stands (a fallen unit's are gone).
                            // The attacker it fells stops attacking.
                            if (weaponDamage && foe && data.Melee && target.Alive && target.Thorns > 0 && owner.Alive)
                            {
                                ApplyDamage(owner, target.Thorns, UnitRef.None, BattleEvent.CauseThorns);
                                if (!owner.Alive)
                                {
                                    CheckEnd();
                                    return;
                                }
                            }

                            break;
                        }
                        case EffectKind.Heal:
                            ApplyHeal(target, magnitude, owner.Ref, data.Id);
                            ApplyTriggeredPassive(owner, PassiveTrigger.Heal, target);
                            break;
                        case EffectKind.Shield:
                            AddShield(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Burn:
                            if (!Lands(owner, target, data, effect))
                            {
                                continue;
                            }

                            AddBurn(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Bleed:
                            // A physical bleed rolls its hit as a burn does (stage 44).
                            if (!Lands(owner, target, data, effect))
                            {
                                continue;
                            }

                            AddBleed(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Regen:
                            AddRegen(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Thorns:
                            AddThorns(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Cleanse:
                            Cleanse(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Stagger:
                            if (!Lands(owner, target, data, effect))
                            {
                                continue;
                            }

                            Stagger(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Charge:
                            Charge(target, magnitude, owner.Ref, data.Id, item);
                            break;
                        case EffectKind.Freeze:
                        case EffectKind.Blind:
                        case EffectKind.Aim:
                        case EffectKind.Keen:
                        case EffectKind.Fury:
                        case EffectKind.Haste:
                        case EffectKind.Vulnerable:
                            // The seven states of stage 45: stacks on the unit. One that hurts rolls its hit on the other side, as a burn does.
                            if (!Lands(owner, target, data, effect))
                            {
                                continue;
                            }

                            AddState(target, effect.Kind, magnitude, owner.Ref, data.Id);
                            break;
                        default:
                            throw new InvalidOperationException($"Effect {effect.Kind} is not implemented.");
                    }

                    CheckEnd();
                    if (Result != BattleResult.Ongoing)
                    {
                        return;
                    }
                }
            }
        }

        int _lastCritRoll;
        int _lastCritChance;

        /// <summary>
        /// Rolls one unit's hit (Slice D stage 33, Docs/Design/02_Combat_System.md §5) on the damage stream, before its critical hit: the base chance with
        /// the attacker's accuracy (its dexterity, or an enemy's Accuracy; 2026-10-20 스탯 개정: only a physical blow rolls) and less the target's dodge (its dexterity, or an enemy's Dodge),
        /// within the bounds. A miss is logged; nothing is dealt to that unit.
        /// </summary>
        /// <summary>
        /// Whether an effect lands on one unit: everything lands on one's own side; on the other side a physical blow that hurts rolls its hit
        /// (<see cref="StatRules.CanMiss"/>, stage 33; 2026-10-21: the data may send any effect either way).
        /// </summary>
        bool Lands(BattleUnit owner, BattleUnit target, ItemData item, ItemEffect effect)
        {
            return target.Side == owner.Side || !StatRules.CanMiss(item, effect) || RollHit(owner, target, item);
        }

        bool RollHit(BattleUnit owner, BattleUnit target, ItemData item)
        {
            // The owner's aim and blind stacks (stage 45) move its accuracy by their points, in ten-thousandths like the rest.
            int accuracy = StatRules.Accuracy(_balance, owner.Setup.Stats, owner.Setup.Accuracy, owner.Setup.Traits)
                + (owner.Aim * _balance.AimPercentPerStack - owner.Blind * _balance.BlindPercentPerStack) * 100;
            // The target's traits (stage 37) and its passive (stage 47, the rogue) move its dodge by their points.
            int chance = StatRules.HitChance(
                _balance,
                accuracy,
                StatRules.Dodge(_balance, target.Setup.Stats, target.Setup.Dodge, target.Setup.Traits, target.Setup.Passive));
            // A certain hit rolls nothing (HitMaxPercent 100: the test data), so the damage stream stays as it was without misses.
            if (chance >= StatRules.Certain)
            {
                return true;
            }

            int roll = _damageRng.NextInt(StatRules.Certain);
            if (roll < chance)
            {
                return true;
            }

            Log(BattleEventKind.Missed, owner.Ref, target.Ref, item.Id, roll, chance, 0);
            return false;
        }

        /// <summary>
        /// Rolls the weapon's critical hit on one unit (stage 22, Docs/Design/02_Combat_System.md §4): with the weapon's chance, the damage
        /// becomes <see cref="StatRules.CritDamagePercent"/>% of the attack's roll (stars included; 2026-10-20 스탯 개정: a physical weapon's multiplier
        /// grows with the weapon and the owner's traits, a magic one's is the balance's). A weapon without a chance rolls nothing.
        /// </summary>
        bool RollCritical(BattleUnit owner, BattleUnit target, ItemData weapon, ref int damage)
        {
            if (weapon.CritChancePercent <= 0)
            {
                return false;
            }

            // In ten-thousandths, with the owner's dexterity (Slice D stage 33), the ranger's passive (stage 36), the owner's traits (stage 37) and
            // less what the target's helm takes off (stage 39). The roll is drawn all the same: the stream does not depend on the helm.
            // The owner's keen stacks add their points to the chance and its fury stacks to the multiplier (stage 45).
            _lastCritChance = StatRules.CritChance(_balance, owner.Setup.Stats, weapon,
                StatRules.CritBonusPercent(owner.Setup.Passive, weapon) + owner.Keen * _balance.KeenPercentPerStack, owner.Setup.Traits,
                CritTakenPercent(target));
            _lastCritRoll = _damageRng.NextInt(StatRules.Certain);
            if (_lastCritRoll >= _lastCritChance)
            {
                return false;
            }

            int percent = StatRules.CritDamagePercent(_balance, weapon, owner.Setup.Traits) + owner.Fury * _balance.FuryPercentPerStack;
            damage = (int)((long)damage * percent / 100);
            return true;
        }

        /// <summary>
        /// The points a unit's items take off the critical chance of the weapons that hit it (Slice D stage 39, Docs/Design/02_Combat_System.md §4
        /// "방어구"): those working where it stands now. The tier does not change them.
        /// </summary>
        static int CritTakenPercent(BattleUnit target)
        {
            int points = 0;
            foreach (BattleItemState item in target.Items)
            {
                if (item.Active)
                {
                    points += item.Equipped.Item.CritTakenPercent;
                }
            }

            return points;
        }

        /// <summary>
        /// The fatigue of a critical hit (stage 22, Docs/Design/04_Lobby_100Day_Economy.md §3, after Darkest Dungeon's stress): a mercenary hit
        /// by one is tired and so are its allies; a mercenary who lands one is relieved and so are its allies. Enemies have no fatigue.
        /// </summary>
        void CriticalFatigue(BattleUnit owner, BattleUnit target)
        {
            if (owner.Side == BattleSide.Party)
            {
                ChangeFatigue(owner, -_balance.CritFatigueDealt, BattleEvent.FatigueCritDealt);
                ChangeAlliesFatigue(owner, -_balance.CritFatigueDealtAllies, BattleEvent.FatigueAllyCritDealt);
            }
            else if (target.Side == BattleSide.Party)
            {
                ChangeFatigue(target, _balance.CritFatigueTaken, BattleEvent.FatigueCritTaken);
                ChangeAlliesFatigue(target, _balance.CritFatigueTakenAllies, BattleEvent.FatigueAllyCritTaken);
            }
        }

        /// <summary>
        /// The living targets of an effect, in the order it is applied to them. "Enemy" and "ally"
        /// are relative to the owner. They are chosen once, before the effect is applied: an advance
        /// caused by the effect does not change who it hits.
        ///
        /// The living always stand in rows 1..n with no gap, so "the first N from the front" are
        /// rows 1..N and "the first N from the back" are rows n-N+1..n. With fewer than N alive,
        /// everyone alive is hit: an attack never lands on an empty row.
        /// </summary>
        List<BattleUnit> ResolveTargets(BattleUnit owner, ItemEffect effect)
        {
            List<BattleUnit> allies = owner.Side == BattleSide.Party ? _party : _enemies;
            List<BattleUnit> foes = owner.Side == BattleSide.Party ? _enemies : _party;
            var targets = new List<BattleUnit>();

            switch (effect.Target)
            {
                case TargetMode.EnemyFront:
                    foreach (BattleUnit foe in foes)
                    {
                        if (foe.Alive && foe.Row <= effect.Reach)
                        {
                            targets.Add(foe);
                        }
                    }

                    break;
                case TargetMode.EnemyBack:
                    // The rearmost living unit stands in row n: the length of the living line.
                    int rearmost = LineLength(foes);
                    for (int i = foes.Count - 1; i >= 0; i--)
                    {
                        if (foes[i].Alive && foes[i].Row > rearmost - effect.Reach)
                        {
                            targets.Add(foes[i]);
                        }
                    }

                    break;
                case TargetMode.EnemyAll:
                    AddAlive(targets, foes);
                    break;
                case TargetMode.Self:
                    targets.Add(owner);
                    break;
                case TargetMode.AllyLowestHp:
                    AddIfNotNull(targets, LowestHp(allies));
                    break;
                case TargetMode.AllyAll:
                    AddAlive(targets, allies);
                    break;
                default:
                    throw new InvalidOperationException($"Target mode {effect.Target} is not implemented.");
            }

            return targets;
        }

        static void AddIfNotNull(List<BattleUnit> targets, BattleUnit unit)
        {
            if (unit != null)
            {
                targets.Add(unit);
            }
        }

        static void AddAlive(List<BattleUnit> targets, List<BattleUnit> units)
        {
            foreach (BattleUnit unit in units)
            {
                if (unit.Alive)
                {
                    targets.Add(unit);
                }
            }
        }

        /// <summary>Lowest HP ratio; ties go to the earlier unit.</summary>
        static BattleUnit LowestHp(List<BattleUnit> units)
        {
            BattleUnit lowest = null;
            long lowestRatio = long.MaxValue;
            foreach (BattleUnit unit in units)
            {
                if (!unit.Alive)
                {
                    continue;
                }

                long ratio = (long)unit.Hp * 1000000 / unit.MaxHp;
                if (ratio < lowestRatio)
                {
                    lowest = unit;
                    lowestRatio = ratio;
                }
            }

            return lowest;
        }

        /// <summary>
        /// Whether the condition of a unit's passive holds right now. A span of the line is judged by
        /// where the unit stands at this moment and how many of its side are alive.
        /// </summary>
        bool ConditionHolds(BattleUnit unit, PassiveSpec passive)
        {
            switch (passive.Condition)
            {
                case PassiveCondition.None: return true;
                case PassiveCondition.InRows: return passive.Rows.Contains(unit.Row, LineLength(SideOf(unit)));
                case PassiveCondition.SelfInDog: return unit.InDog;
                default: throw new InvalidOperationException($"Passive condition {passive.Condition} is not implemented.");
            }
        }

        List<BattleUnit> SideOf(BattleUnit unit)
        {
            return unit.Side == BattleSide.Party ? _party : _enemies;
        }

        /// <summary>How many units of a side are alive. They always stand in rows 1..n.</summary>
        static int LineLength(List<BattleUnit> units)
        {
            int alive = 0;
            foreach (BattleUnit unit in units)
            {
                if (unit.Alive)
                {
                    alive++;
                }
            }

            return alive;
        }

        int WithWeaponPower(BattleUnit owner, int magnitude)
        {
            PassiveSpec passive = owner.Setup.Passive;
            if (passive == null
                || passive.Trigger != PassiveTrigger.Always
                || passive.Effect != PassiveEffect.WeaponPowerPercent
                || !ConditionHolds(owner, passive))
            {
                return magnitude;
            }

            return magnitude * (100 + passive.Magnitude) / 100;
        }

        void ApplyBattleStartPassives(List<BattleUnit> units)
        {
            foreach (BattleUnit unit in units)
            {
                PassiveSpec passive = unit.Setup.Passive;
                if (passive == null || passive.Trigger != PassiveTrigger.BattleStart || !ConditionHolds(unit, passive))
                {
                    continue;
                }

                if (passive.Target == PassiveTarget.Self)
                {
                    AddShield(unit, passive.Magnitude, unit.Ref, BattleEvent.CausePassive);
                }
                else
                {
                    foreach (BattleUnit ally in units)
                    {
                        if (ally.Alive)
                        {
                            AddShield(ally, passive.Magnitude, unit.Ref, BattleEvent.CausePassive);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The items that activate when a battle starts (Slice D stage 39, Docs/Design/02_Combat_System.md §4 "방어구"): each fires once now, where it
        /// works, in unit and slot order, and never again (it does not cycle). A fatigue state's cooldown has nothing to change on them.
        /// </summary>
        void ApplyBattleStartItems(List<BattleUnit> units)
        {
            foreach (BattleUnit unit in units)
            {
                foreach (BattleItemState item in unit.Items)
                {
                    if (!unit.Alive)
                    {
                        break;
                    }

                    if (!item.Active || item.Equipped.Item.Trigger != ItemTrigger.BattleStart)
                    {
                        continue;
                    }

                    Activate(unit, item);
                    if (Result != BattleResult.Ongoing)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>Passives that react to one of the owner's effects landing on a target.</summary>
        void ApplyTriggeredPassive(BattleUnit owner, PassiveTrigger trigger, BattleUnit eventTarget)
        {
            PassiveSpec passive = owner.Setup.Passive;
            if (passive == null || passive.Trigger != trigger || !ConditionHolds(owner, passive) || !eventTarget.Alive)
            {
                return;
            }

            switch (passive.Effect)
            {
                case PassiveEffect.Shield:
                    AddShield(eventTarget, passive.Magnitude, owner.Ref, BattleEvent.CausePassive);
                    break;
                case PassiveEffect.Burn:
                    AddBurn(eventTarget, passive.Magnitude, owner.Ref, BattleEvent.CausePassive);
                    break;
                default:
                    throw new InvalidOperationException($"Passive effect {passive.Effect} cannot be triggered by {trigger}.");
            }
        }

        /// <summary>
        /// Shield absorbs first. Damage that gets through is a hit, which matters at 0 HP, and which tires a mercenary
        /// (Docs/Design/04_Lobby_100Day_Economy.md §3): the hit itself, and more for reaching death's door, which tires the
        /// allies too. An enemy killed by a mercenary's item relieves that mercenary a little.
        /// </summary>
        /// <param name="throughShield">True for a bleed tick (stage 44): the wound is inside, the shield absorbs none of it.</param>
        void ApplyDamage(BattleUnit target, int amount, UnitRef source, string cause, bool throughShield = false)
        {
            int absorbed = throughShield ? 0 : Math.Min(target.Shield, amount);
            target.Shield -= absorbed;
            int through = amount - absorbed;
            bool wasInDog = target.InDog;
            if (through > 0 && !wasInDog)
            {
                target.Hp = Math.Max(0, target.Hp - through);
            }

            Log(BattleEventKind.Damaged, source, target.Ref, cause, amount, absorbed, target.Hp);

            // A boss whose HP fell through a phase's percent carries that phase's items from now on (stage 28), whether or not the damage was a hit.
            if (!target.Setup.HasDog && target.Hp > 0)
            {
                EnterPhasesReached(target);
            }

            // Damage that gets through under HitMinDamage is no hit (stage 22): it costs HP but tires nobody and rolls no death, unless it
            // takes the HP to 0. In hundredths a shield lets slivers through that whole numbers never had (Docs/Design/02_Combat_System.md §5).
            if (through <= 0 || (through < _balance.HitMinDamage && (wasInDog || target.Hp > 0)))
            {
                return;
            }

            if (!target.Setup.HasDog)
            {
                if (target.Hp == 0)
                {
                    Kill(target);
                    if (!source.IsNone && source.Side == BattleSide.Party)
                    {
                        ChangeFatigue(Unit(source), -_balance.FatigueOnKill, BattleEvent.FatigueKill);
                    }
                }

                return;
            }

            bool enteredDog = false;
            if (wasInDog)
            {
                DogHit(target);
            }
            else if (target.Hp == 0)
            {
                EnterDog(target);
                enteredDog = true;
            }

            // The hit tires the one hit (if it lives) by FatigueOnHit, and death's door tires it and its allies. A burn tick is a hit
            // (death's door counts it) but tires nobody, and so are a bleed tick and thorns dealt back (stage 44); a storm tick tires as a hit
            // does (Docs/Design/04_Lobby_100Day_Economy.md §3).
            // 2026-10-17: FatigueOnHit is 0 in the shipped data, so only a critical hit tires (CriticalFatigue); the rule stays data.
            if (!BattleEvent.IsTickCause(cause))
            {
                ChangeFatigue(target, _balance.FatigueOnHit, BattleEvent.FatigueHit);
            }

            if (enteredDog)
            {
                ChangeFatigue(target, _balance.FatigueOnDog, BattleEvent.FatigueDog);
                ChangeAlliesFatigue(target, _balance.FatigueOnAllyDog, BattleEvent.FatigueAllyDog);
            }
        }

        void EnterDog(BattleUnit target)
        {
            target.InDog = true;
            target.GraceHits = 0;
            target.GraceEndMs = TimeMs + _balance.DogGraceMs;
            Log(BattleEventKind.DogEntered, UnitRef.None, target.Ref, null, target.GraceEndMs, 0, 0);
        }

        /// <summary>
        /// A hit on a mercenary at 0 HP. During grace, hits are counted and the hit that reaches
        /// DogGraceBreakHits breaks the grace and rolls. After grace, every hit rolls.
        /// </summary>
        void DogHit(BattleUnit target)
        {
            if (TimeMs < target.GraceEndMs)
            {
                target.GraceHits++;
                if (target.GraceHits < _balance.DogGraceBreakHits)
                {
                    return;
                }

                target.GraceEndMs = TimeMs;
                Log(BattleEventKind.GraceBroken, UnitRef.None, target.Ref, null, target.GraceHits, 0, 0);
            }

            int chance = FatigueRules.DogDeathChancePercent(_balance, target.State, target.Setup.Stats, target.Setup.Traits);
            int roll = _battleRng.NextInt(100);
            bool died = roll < chance;
            Log(BattleEventKind.DeathRolled, UnitRef.None, target.Ref, null, chance, roll, died ? 1 : 0);
            if (died)
            {
                Kill(target);
            }
        }

        /// <summary>Heals the target, by less or more under a fatigue state (hopeless takes half, stalwart half again).</summary>
        void ApplyHeal(BattleUnit target, int amount, UnitRef source, string cause)
        {
            if (target.State != null)
            {
                amount = FatigueRules.Scaled(amount, target.State.HealTakenPercent);
            }

            int before = target.Hp;
            target.Hp = Math.Min(target.MaxHp, target.Hp + amount);
            Log(BattleEventKind.Healed, source, target.Ref, cause, amount, target.Hp - before, target.Hp);

            // A heal binds the wound (stage 44): every stack of bleed stops, logged as a cleanse by the heal's cause.
            if (amount > 0 && target.Bleed > 0)
            {
                int stopped = target.Bleed;
                target.Bleed = 0;
                Log(BattleEventKind.Cleansed, source, target.Ref, cause, 0, stopped, 0);
            }

            if (target.InDog && target.Hp > 0)
            {
                target.InDog = false;
                target.GraceHits = 0;
                Log(BattleEventKind.DogExited, UnitRef.None, target.Ref, null, 0, 0, 0);
            }
        }

        void AddShield(BattleUnit target, int amount, UnitRef source, string cause)
        {
            target.Shield += amount;
            Log(BattleEventKind.ShieldGained, source, target.Ref, cause, amount, 0, target.Shield);
        }

        void AddBurn(BattleUnit target, int stacks, UnitRef source, string cause)
        {
            target.Burn += stacks;
            Log(BattleEventKind.BurnApplied, source, target.Ref, cause, stacks, 0, target.Burn);
        }

        // ---- The effect vocabulary (Slice D stage 44, Docs/Design/02_Combat_System.md §4·§5) ----

        void AddBleed(BattleUnit target, int stacks, UnitRef source, string cause)
        {
            target.Bleed += stacks;
            Log(BattleEventKind.BleedApplied, source, target.Ref, cause, stacks, 0, target.Bleed);
        }

        void AddRegen(BattleUnit target, int stacks, UnitRef source, string cause)
        {
            target.Regen += stacks;
            Log(BattleEventKind.RegenApplied, source, target.Ref, cause, stacks, 0, target.Regen);
        }

        void AddThorns(BattleUnit target, int amount, UnitRef source, string cause)
        {
            target.Thorns += amount;
            Log(BattleEventKind.ThornsGained, source, target.Ref, cause, amount, 0, target.Thorns);
        }

        /// <summary>
        /// Takes that many stacks of burn, of bleed and (stage 45) of each of freeze, blind and vulnerable off the target: what hurts. Logged with what
        /// came off the burn (A), the bleed (B) and the three together (C), even when nothing did.
        /// </summary>
        void Cleanse(BattleUnit target, int stacks, UnitRef source, string cause)
        {
            int burn = Math.Min(stacks, target.Burn);
            int bleed = Math.Min(stacks, target.Bleed);
            target.Burn -= burn;
            target.Bleed -= bleed;
            int states = 0;
            foreach (EffectKind state in HarmfulStates)
            {
                int off = Math.Min(stacks, target.StacksOf(state));
                target.SetStacks(state, target.StacksOf(state) - off);
                states += off;
            }

            Log(BattleEventKind.Cleansed, source, target.Ref, cause, burn, bleed, states);
        }

        /// <summary>The seven states of stage 45, in the order their stacks decay at a tick; the three that hurt are what a cleanse takes off.</summary>
        static readonly EffectKind[] States = { EffectKind.Freeze, EffectKind.Blind, EffectKind.Aim, EffectKind.Keen, EffectKind.Fury, EffectKind.Haste, EffectKind.Vulnerable };
        static readonly EffectKind[] HarmfulStates = { EffectKind.Freeze, EffectKind.Blind, EffectKind.Vulnerable };

        /// <summary>Stacks one of the seven states on the target (stage 45). It tells from the next thing the stacks touch: the next hit roll, crit roll or cooldown.</summary>
        void AddState(BattleUnit target, EffectKind state, int stacks, UnitRef source, string cause)
        {
            target.SetStacks(state, target.StacksOf(state) + stacks);
            Log(BattleEventKind.StateApplied, source, target.Ref, cause, stacks, (int)state, target.StacksOf(state));
        }

        /// <summary>
        /// Pushes the next activation of every cycling item the target can use back by the milliseconds. It is over at once: nothing is tracked
        /// and nothing expires, so the log and a replay agree. An item that does not work where its owner stands keeps its reset for when it does.
        /// </summary>
        void Stagger(BattleUnit target, int ms, UnitRef source, string cause)
        {
            int pushed = 0;
            foreach (BattleItemState item in target.Items)
            {
                if (item.Active && item.Cycles)
                {
                    item.NextFireMs += ms;
                    pushed++;
                }
            }

            Log(BattleEventKind.Staggered, source, target.Ref, cause, ms, pushed, 0);
        }

        /// <summary>
        /// Pulls the next activation of every cycling item the target can use forward by the milliseconds, never before the next millisecond:
        /// an item never fires twice at one time, and one pulled to now fires at the next event, in the usual order. The item doing the charging
        /// never pulls itself (<paramref name="except"/>), or it would fire again every millisecond.
        /// </summary>
        void Charge(BattleUnit target, int ms, UnitRef source, string cause, BattleItemState except)
        {
            int pulled = 0;
            foreach (BattleItemState item in target.Items)
            {
                if (item.Active && item.Cycles && item != except)
                {
                    item.NextFireMs = Math.Max(TimeMs + 1, item.NextFireMs - ms);
                    pulled++;
                }
            }

            Log(BattleEventKind.Charged, source, target.Ref, cause, ms, pulled, 0);
        }

        void Kill(BattleUnit target)
        {
            target.Alive = false;
            target.InDog = false;
            target.Shield = 0;
            target.Burn = 0;
            target.Bleed = 0;
            target.Regen = 0;
            target.Thorns = 0;
            foreach (EffectKind state in States)
            {
                target.SetStacks(state, 0);
            }
            Log(BattleEventKind.Died, UnitRef.None, target.Ref, null, 0, 0, 0);
            AdvanceBehind(target);
            RefreshSide(SideOf(target));

            // A mercenary's death tires the others.
            if (target.Side == BattleSide.Party)
            {
                ChangeAlliesFatigue(target, _balance.FatigueOnAllyDeath, BattleEvent.FatigueAllyDeath);
            }
        }

        // ---- Fatigue (Docs/Design/04_Lobby_100Day_Economy.md §3) --------------------------------

        /// <summary>
        /// Moves a mercenary's fatigue (an enemy's never moves) and judges it: at the maximum it collapses; at the threshold without
        /// a state it breaks down; under the threshold an affliction ends. Logged only when it moved.
        /// </summary>
        void ChangeFatigue(BattleUnit unit, int amount, string cause)
        {
            if (unit.Side != BattleSide.Party || !unit.Alive || amount == 0)
            {
                return;
            }

            // A gain is softened (or hardened) by the unit's endurance (Slice D stage 33) and its traits (stage 37); what takes fatigue off stays as it is.
            amount = StatRules.FatigueGain(_balance, unit.Setup.Stats, amount, unit.Setup.Traits);
            if (amount == 0)
            {
                return;
            }

            int before = unit.Fatigue;
            unit.Fatigue = FatigueRules.Add(_balance, before, amount);
            if (unit.Fatigue != before)
            {
                Log(BattleEventKind.FatigueChanged, UnitRef.None, unit.Ref, cause, unit.Fatigue - before, 0, unit.Fatigue);
            }
            else if (amount < 0 || before < _balance.MaxFatigue)
            {
                return;
            }

            // Unchanged at the maximum by more fatigue: it collapses again (Docs/Design/04_Lobby_100Day_Economy.md §3).
            ResolveFatigue(unit);
        }

        /// <summary>Tires every other living mercenary.</summary>
        void ChangeAlliesFatigue(BattleUnit of, int amount, string cause)
        {
            foreach (BattleUnit ally in _party)
            {
                if (ally != of)
                {
                    ChangeFatigue(ally, amount, cause);
                }
            }
        }

        void ResolveFatigue(BattleUnit unit)
        {
            if (!unit.Alive)
            {
                return;
            }

            if (unit.Fatigue >= _balance.MaxFatigue)
            {
                Collapse(unit);
                return;
            }

            if (unit.State != null && FatigueRules.AfflictionEnds(_balance, unit.State, unit.Fatigue))
            {
                Log(BattleEventKind.FatigueStateEnded, UnitRef.None, unit.Ref, unit.State.Id, 0, 0, 0);
                unit.State = null;
                return;
            }

            if (unit.State == null && unit.Fatigue >= _balance.FatigueBreakdown)
            {
                BreakDown(unit);
            }
        }

        /// <summary>
        /// The breakdown at the threshold: by VirtueChancePercent a virtue, which also brings the fatigue down to VirtueFatigue,
        /// otherwise an affliction; which one of its kind is drawn from the setup's states. Both rolls come from the fatigue
        /// stream, so death rolls never move.
        /// </summary>
        void BreakDown(BattleUnit unit)
        {
            // Spirit helps (Slice D stage 33), and so may the unit's traits (stage 37).
            int chance = StatRules.VirtueChancePercent(_balance, unit.Setup.Stats, unit.Setup.Traits);
            int roll = _fatigueRng.NextInt(100);
            bool virtue = roll < chance;
            List<FatigueStateData> pool = StatesOf(virtue ? FatigueStateKind.Virtue : FatigueStateKind.Affliction);
            if (pool.Count == 0)
            {
                throw new InvalidOperationException($"The battle setup names no {(virtue ? "virtue" : "affliction")} to break down into.");
            }

            unit.State = pool[_fatigueRng.NextInt(pool.Count)];
            Log(BattleEventKind.BrokeDown, UnitRef.None, unit.Ref, unit.State.Id, roll, chance, virtue ? 1 : 0);
            if (virtue && unit.Fatigue > _balance.VirtueFatigue)
            {
                int before = unit.Fatigue;
                unit.Fatigue = _balance.VirtueFatigue;
                Log(BattleEventKind.FatigueChanged, UnitRef.None, unit.Ref, BattleEvent.FatigueVirtue, unit.Fatigue - before, 0, unit.Fatigue);
            }
        }

        List<FatigueStateData> StatesOf(FatigueStateKind kind)
        {
            var states = new List<FatigueStateData>();
            if (Setup.FatigueStates != null)
            {
                foreach (FatigueStateData state in Setup.FatigueStates)
                {
                    if (state.Kind == kind)
                    {
                        states.Add(state);
                    }
                }
            }

            return states;
        }

        /// <summary>The collapse at the maximum: the unit goes to death's door (which tires its allies), or dies if it was there already.</summary>
        void Collapse(BattleUnit unit)
        {
            bool dies = unit.InDog;
            Log(BattleEventKind.Collapsed, UnitRef.None, unit.Ref, null, 0, 0, dies ? 1 : 0);
            if (dies)
            {
                Kill(unit);
                return;
            }

            unit.Hp = 0;
            EnterDog(unit);
            ChangeAlliesFatigue(unit, _balance.FatigueOnAllyDog, BattleEvent.FatigueAllyDog);
        }

        /// <summary>
        /// The advance rule: when a unit has died, every unit behind it moves one row forward.
        /// It happens with the death itself, before anything else is processed.
        /// </summary>
        void AdvanceBehind(BattleUnit fallen)
        {
            bool moved = false;
            foreach (BattleUnit unit in SideOf(fallen))
            {
                if (unit.Alive && unit.Row > fallen.Row)
                {
                    unit.Row--;
                    moved = true;
                }
            }

            if (moved)
            {
                Log(BattleEventKind.RowsAdvanced, UnitRef.None, UnitRef.None, null, (int)fallen.Side, fallen.Row, 0);
            }
        }

        /// <summary>
        /// After a death on a side, every living unit of that side judges its items again: the ones
        /// behind the dead moved, and the line got shorter for everyone. An item that works now but
        /// did not before starts a fresh cooldown; one that no longer works would stop and lose its
        /// progress, but the line only ever shortens, so an item never stops during a battle.
        /// </summary>
        void RefreshSide(List<BattleUnit> units)
        {
            int lineLength = LineLength(units);
            foreach (BattleUnit unit in units)
            {
                if (!unit.Alive)
                {
                    continue;
                }

                foreach (BattleItemState item in unit.Items)
                {
                    bool usable = Works(item.Equipped.Item, unit.Row, lineLength);
                    if (usable && !item.Active)
                    {
                        item.NextFireMs = TimeMs + CooldownOf(unit, item);
                    }

                    item.Active = usable;
                }
            }
        }

        enum Tick
        {
            Storm,
            Burn,
            Bleed,
            Regen,
            States,
        }

        static readonly Tick[] TimedTicks = { Tick.Burn, Tick.Bleed, Tick.Regen, Tick.States };

        /// <summary>
        /// The ticks at a burn tick's time (Docs/Design/02_Combat_System.md §3): every unit's burn, then every unit's bleed, then every unit's regen
        /// (Slice D stage 44 added the two after the burn, so a battle without them logs as it did), then (stage 45) every unit's seven states lose a
        /// stack, each the party first.
        /// </summary>
        void Ticks()
        {
            foreach (Tick tick in TimedTicks)
            {
                if (TickUnits(_party, tick, 0) || TickUnits(_enemies, tick, 0))
                {
                    return;
                }
            }
        }

        void StormTick()
        {
            int damage = NextStormDamage;
            _stormTicks++;
            Log(BattleEventKind.StormTicked, UnitRef.None, UnitRef.None, null, damage, 0, 0);
            if (!TickUnits(_party, Tick.Storm, damage))
            {
                TickUnits(_enemies, Tick.Storm, damage);
            }
        }

        /// <summary>Applies one kind of tick to each living unit. Returns true when the battle ended.</summary>
        bool TickUnits(List<BattleUnit> units, Tick tick, int stormDamage)
        {
            foreach (BattleUnit unit in units)
            {
                if (!unit.Alive)
                {
                    continue;
                }

                switch (tick)
                {
                    case Tick.Storm:
                        ApplyDamage(unit, stormDamage, UnitRef.None, BattleEvent.CauseStorm);
                        break;
                    case Tick.Burn:
                        if (unit.Burn > 0)
                        {
                            ApplyDamage(unit, unit.Burn * _balance.BurnDamagePerStack, UnitRef.None, BattleEvent.CauseBurn);
                            if (unit.Alive)
                            {
                                unit.Burn--;
                            }
                        }

                        break;
                    case Tick.Bleed:
                        // Through the shield: the wound is inside (stage 44).
                        if (unit.Bleed > 0)
                        {
                            ApplyDamage(unit, unit.Bleed * _balance.BleedDamagePerStack, UnitRef.None, BattleEvent.CauseBleed, throughShield: true);
                            if (unit.Alive)
                            {
                                unit.Bleed--;
                            }
                        }

                        break;
                    case Tick.Regen:
                        // A heal like any other (stage 44): the fatigue state scales it, it binds a bleed, it brings a unit back from death's door.
                        if (unit.Regen > 0)
                        {
                            ApplyHeal(unit, unit.Regen * _balance.RegenHealPerStack, UnitRef.None, BattleEvent.CauseRegen);
                            unit.Regen--;
                        }

                        break;
                    case Tick.States:
                        // The seven states of stage 45 lose a stack; nothing happens, so nothing is logged.
                        foreach (EffectKind state in States)
                        {
                            int stacks = unit.StacksOf(state);
                            if (stacks > 0)
                            {
                                unit.SetStacks(state, stacks - 1);
                            }
                        }

                        break;
                }

                CheckEnd();
                if (Result != BattleResult.Ongoing)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The party is checked first: if both sides fall at the same moment, it is a defeat.</summary>
        void CheckEnd()
        {
            if (Result != BattleResult.Ongoing)
            {
                return;
            }

            if (!AnyAlive(_party))
            {
                End(BattleResult.Defeat);
            }
            else if (!AnyAlive(_enemies))
            {
                End(BattleResult.Victory);
            }
        }

        static bool AnyAlive(List<BattleUnit> units)
        {
            foreach (BattleUnit unit in units)
            {
                if (unit.Alive)
                {
                    return true;
                }
            }

            return false;
        }

        void End(BattleResult result)
        {
            Result = result;
            Log(BattleEventKind.BattleEnded, UnitRef.None, UnitRef.None, null, (int)result, 0, 0);
        }

        void Log(BattleEventKind kind, UnitRef source, UnitRef target, string id, int a, int b, int c)
        {
            _events.Add(new BattleEvent(TimeMs, kind, source, target, id, a, b, c));
        }
    }
}
