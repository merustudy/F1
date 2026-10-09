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
    /// Order at one timestamp: burn tick, item activations (party first, unit order, slot order),
    /// storm tick, then player inputs made at that time.
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
        public int NextStormTickMs => _nextStormTickMs;

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
            if (setup.Items != null)
            {
                for (int slot = 0; slot < setup.Items.Count; slot++)
                {
                    EquippedItem equipped = setup.Items[slot];
                    if (equipped == null)
                    {
                        throw new ArgumentException($"Unit '{setup.SourceId}' has an empty entry on its board.");
                    }

                    bool active = equipped.Item.UsableIn(setup.Row, lineLength);
                    items.Add(new BattleItemState(slot, equipped, active, EffectiveCooldown(equipped.Item.CooldownMs, cooldownPermille)));
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

        /// <summary>base x (1000 + permille) / 1000, rounded to the nearest millisecond, never below MinCooldownMs.</summary>
        int EffectiveCooldown(int baseMs, int permille)
        {
            long scaled = ((long)baseMs * (1000 + permille) + 500) / 1000;
            return scaled < _balance.MinCooldownMs ? _balance.MinCooldownMs : (int)scaled;
        }

        /// <summary>The cooldown an item of a unit cycles with now: its own, changed by the unit's fatigue state (an affliction slows, a virtue quickens).</summary>
        int CooldownOf(BattleUnit owner, BattleItemState item)
        {
            return owner.State == null ? item.CooldownMs : EffectiveCooldown(item.CooldownMs, owner.State.CooldownPercent * 10);
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
                    if (item.Active && item.NextFireMs < next)
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
                BurnTick();
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

                    if (!item.Active || item.NextFireMs != now)
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

            foreach (ItemEffect effect in data.Effects)
            {
                int magnitude = item.Equipped.Magnitude(_balance, effect);
                bool weaponDamage = effect.Kind == EffectKind.Damage && data.Category == ItemCategory.Weapon;
                if (weaponDamage)
                {
                    magnitude = WithWeaponPower(owner, magnitude);
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
                            ApplyDamage(target, magnitude, owner.Ref, data.Id);
                            if (weaponDamage && target.Alive)
                            {
                                ApplyTriggeredPassive(owner, PassiveTrigger.WeaponHit, target);
                            }

                            break;
                        case EffectKind.Heal:
                            ApplyHeal(target, magnitude, owner.Ref, data.Id);
                            ApplyTriggeredPassive(owner, PassiveTrigger.Heal, target);
                            break;
                        case EffectKind.Shield:
                            AddShield(target, magnitude, owner.Ref, data.Id);
                            break;
                        case EffectKind.Burn:
                            AddBurn(target, magnitude, owner.Ref, data.Id);
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
        void ApplyDamage(BattleUnit target, int amount, UnitRef source, string cause)
        {
            int absorbed = Math.Min(target.Shield, amount);
            target.Shield -= absorbed;
            int through = amount - absorbed;
            bool wasInDog = target.InDog;
            if (through > 0 && !wasInDog)
            {
                target.Hp = Math.Max(0, target.Hp - through);
            }

            Log(BattleEventKind.Damaged, source, target.Ref, cause, amount, absorbed, target.Hp);
            if (through <= 0)
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

            // The hit tires the one hit (if it lives), and death's door tires it and its allies. A burn tick is a hit
            // (death's door counts it) but tires nobody; a storm tick does (Docs/Design/04_Lobby_100Day_Economy.md §3).
            if (cause != BattleEvent.CauseBurn)
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

            // A fatigue state moves the chance (a reckless mercenary dies more easily, a stalwart one less).
            int chance = _balance.DogDeathChancePercent + (target.State?.DeathChanceDelta ?? 0);
            chance = Math.Max(0, Math.Min(100, chance));
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

        void Kill(BattleUnit target)
        {
            target.Alive = false;
            target.InDog = false;
            target.Shield = 0;
            target.Burn = 0;
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
            int roll = _fatigueRng.NextInt(100);
            bool virtue = roll < _balance.VirtueChancePercent;
            List<FatigueStateData> pool = StatesOf(virtue ? FatigueStateKind.Virtue : FatigueStateKind.Affliction);
            if (pool.Count == 0)
            {
                throw new InvalidOperationException($"The battle setup names no {(virtue ? "virtue" : "affliction")} to break down into.");
            }

            unit.State = pool[_fatigueRng.NextInt(pool.Count)];
            Log(BattleEventKind.BrokeDown, UnitRef.None, unit.Ref, unit.State.Id, roll, _balance.VirtueChancePercent, virtue ? 1 : 0);
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
                    bool usable = item.Equipped.Item.UsableIn(unit.Row, lineLength);
                    if (usable && !item.Active)
                    {
                        item.NextFireMs = TimeMs + CooldownOf(unit, item);
                    }

                    item.Active = usable;
                }
            }
        }

        void BurnTick()
        {
            if (!TickUnits(_party, isStorm: false, 0))
            {
                TickUnits(_enemies, isStorm: false, 0);
            }
        }

        void StormTick()
        {
            int damage = NextStormDamage;
            _stormTicks++;
            Log(BattleEventKind.StormTicked, UnitRef.None, UnitRef.None, null, damage, 0, 0);
            if (!TickUnits(_party, isStorm: true, damage))
            {
                TickUnits(_enemies, isStorm: true, damage);
            }
        }

        /// <summary>Applies a burn or storm tick to each living unit. Returns true when the battle ended.</summary>
        bool TickUnits(List<BattleUnit> units, bool isStorm, int stormDamage)
        {
            foreach (BattleUnit unit in units)
            {
                if (!unit.Alive)
                {
                    continue;
                }

                if (isStorm)
                {
                    ApplyDamage(unit, stormDamage, UnitRef.None, BattleEvent.CauseStorm);
                }
                else if (unit.Burn > 0)
                {
                    ApplyDamage(unit, unit.Burn, UnitRef.None, BattleEvent.CauseBurn);
                    if (unit.Alive)
                    {
                        unit.Burn--;
                    }
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
