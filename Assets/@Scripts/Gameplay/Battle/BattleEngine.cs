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
    /// behind it advance one row at once, before anything else is processed.
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
                _party.Add(CreateUnit(BattleSide.Party, i, setup.Party[i], 0));
            }

            for (int i = 0; i < setup.Enemies.Count; i++)
            {
                _enemies.Add(CreateUnit(BattleSide.Enemy, i, setup.Enemies[i], setup.EnemyCooldownPermille));
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
            _nextBurnTickMs = _balance.BurnTickMs;
            _nextStormTickMs = _balance.StormStartMs;

            Log(BattleEventKind.BattleStarted, UnitRef.None, UnitRef.None, null, 0, 0, 0);
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

        BattleUnit CreateUnit(BattleSide side, int index, BattleUnitSetup setup, int cooldownPermille)
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
                        continue;
                    }

                    bool active = equipped.Item.UsableIn(setup.Row);
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

                    item.NextFireMs += item.CooldownMs;
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
                int magnitude = effect.MagnitudeAt(item.Equipped.Grade);
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
                    int rearmost = RearmostRow(foes);
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

        /// <summary>The rearmost row a living unit stands in, or 0 when nobody is alive.</summary>
        static int RearmostRow(List<BattleUnit> units)
        {
            int rearmost = 0;
            foreach (BattleUnit unit in units)
            {
                if (unit.Alive && unit.Row > rearmost)
                {
                    rearmost = unit.Row;
                }
            }

            return rearmost;
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

        /// <summary>Whether the condition of a unit's passive holds right now. Rows are judged by where the unit stands at this moment.</summary>
        static bool ConditionHolds(BattleUnit unit, PassiveSpec passive)
        {
            switch (passive.Condition)
            {
                case PassiveCondition.None: return true;
                case PassiveCondition.InRows: return BattleRows.Contains(passive.Rows, unit.Row);
                case PassiveCondition.SelfInDog: return unit.InDog;
                default: throw new InvalidOperationException($"Passive condition {passive.Condition} is not implemented.");
            }
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

        /// <summary>Shield absorbs first. Damage that gets through is a hit, which matters at 0 HP.</summary>
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
                }

                return;
            }

            if (wasInDog)
            {
                DogHit(target);
            }
            else if (target.Hp == 0)
            {
                target.InDog = true;
                target.GraceHits = 0;
                target.GraceEndMs = TimeMs + _balance.DogGraceMs;
                Log(BattleEventKind.DogEntered, UnitRef.None, target.Ref, null, target.GraceEndMs, 0, 0);
            }
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

            int roll = _battleRng.NextInt(100);
            bool died = roll < _balance.DogDeathChancePercent;
            Log(BattleEventKind.DeathRolled, UnitRef.None, target.Ref, null, _balance.DogDeathChancePercent, roll, died ? 1 : 0);
            if (died)
            {
                Kill(target);
            }
        }

        void ApplyHeal(BattleUnit target, int amount, UnitRef source, string cause)
        {
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
        }

        /// <summary>
        /// The advance rule: when a unit has died, every unit behind it moves one row forward.
        /// It happens with the death itself, before anything else is processed.
        /// </summary>
        void AdvanceBehind(BattleUnit fallen)
        {
            List<BattleUnit> units = fallen.Side == BattleSide.Party ? _party : _enemies;
            bool moved = false;
            foreach (BattleUnit unit in units)
            {
                if (unit.Alive && unit.Row > fallen.Row)
                {
                    unit.Row--;
                    RefreshItems(unit);
                    moved = true;
                }
            }

            if (moved)
            {
                Log(BattleEventKind.RowsAdvanced, UnitRef.None, UnitRef.None, null, (int)fallen.Side, fallen.Row, 0);
            }
        }

        /// <summary>
        /// After the owner changed rows: an item that can be used in the new row but could not in the
        /// old one starts a fresh cooldown now; one that no longer can be used stops and loses its
        /// progress. An item usable in both rows is not touched.
        /// </summary>
        void RefreshItems(BattleUnit unit)
        {
            foreach (BattleItemState item in unit.Items)
            {
                bool usable = item.Equipped.Item.UsableIn(unit.Row);
                if (usable && !item.Active)
                {
                    item.NextFireMs = TimeMs + item.CooldownMs;
                }

                item.Active = usable;
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
