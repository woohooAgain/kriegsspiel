using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed class CombatResolver : ICombatResolver
{
    private readonly ContactGeometryResolver _contactGeometry;
    private readonly IGameRules _rules;

    public CombatResolver()
        : this(new ContactGeometryResolver(), new DefaultGameRules())
    {
    }

    public CombatResolver(ContactGeometryResolver contactGeometry)
        : this(contactGeometry, new DefaultGameRules())
    {
    }

    public CombatResolver(ContactGeometryResolver contactGeometry, IGameRules rules)
    {
        _contactGeometry = contactGeometry ?? throw new ArgumentNullException(nameof(contactGeometry));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public IEnumerable<Report> Resolve(CombatTick e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Zones.TryGetValue(e.ZoneId, out var zone)) yield break;

        var present = zone.PresentUnitIds
            .Where(s.Units.ContainsKey)
            .Select(id => s.Units[id])
            .Where(u => !u.Captured && u.Strength > 0)
            .ToList();

        var blue = present.Where(u => u.Faction == Faction.Blue).ToList();
        var red = present.Where(u => u.Faction == Faction.Red).ToList();
        if (blue.Count == 0 || red.Count == 0) yield break;

        var attackerSide = DetermineAttackerSide(blue, red);
        var attackers = attackerSide == Faction.Blue ? blue : red;
        var defenders = attackerSide == Faction.Blue ? red : blue;

        var geometryByDefender = defenders.ToDictionary(
            d => d.Id,
            d => _contactGeometry.Resolve(SelectPrimaryAttacker(attackers), d, zone, s.Zones));
        var geometryByAttacker = attackers.ToDictionary(
            a => a.Id,
            _ => geometryByDefender.Count == 1
                ? geometryByDefender.Values.First()
                : geometryByDefender.Values.OrderByDescending(g => g.CollisionMultiplier).First());

        ApplyContactMoralePenalties(defenders, geometryByDefender);

        double attackersPower = attackers.Sum(u => ComputePower(u, zone, geometryByAttacker[u.Id], true, f));
        double defendersPower = defenders.Sum(u => ComputePower(u, zone, geometryByDefender[u.Id], false, f));
        double total = attackersPower + defendersPower;
        if (total <= 0) yield break;

        int attackerLossTotal = ComputeLossTotal(attackers.Sum(u => u.Strength), defendersPower, total, attackers.Count, f.Combat.LethalitySelf);
        int defenderLossTotal = ComputeLossTotal(defenders.Sum(u => u.Strength), attackersPower, total, defenders.Count, f.Combat.LethalityEnemy);

        ApplyCombatEffects(attackers, attackerLossTotal, f, e.Minute, e.ZoneId, s);
        ApplyCombatEffects(defenders, defenderLossTotal, f, e.Minute, e.ZoneId, s);

        EvaluateOutcomes(attackers, defenders, zone, s, f);

        foreach (var report in BuildReports(blue, red, zone, attackerSide == Faction.Blue ? attackerLossTotal : defenderLossTotal, geometryByDefender.Values, e.Minute, f))
            yield return report;
    }

    private static Unit SelectPrimaryAttacker(IReadOnlyList<Unit> attackers) => attackers[0];

    private static Faction DetermineAttackerSide(IReadOnlyList<Unit> blue, IReadOnlyList<Unit> red)
    {
        bool blueAttacking = blue.Any(u => u.MoveEdgeId != null && u.Posture != Posture.Defend);
        bool redAttacking = red.Any(u => u.MoveEdgeId != null && u.Posture != Posture.Defend);

        if (blueAttacking == redAttacking)
            return blue[0].Traits.Initiative >= red[0].Traits.Initiative ? Faction.Blue : Faction.Red;
        return blueAttacking ? Faction.Blue : Faction.Red;
    }

    private static double ComputePower(Unit unit, Zone zone, ContactGeometry geometry, bool isAttacker, FrictionTable f)
    {
        if (unit.Strength <= 0) return 0;

        double position = unit.Posture switch
        {
            Posture.March => f.Combat.Position.March,
            Posture.Defend => f.Combat.Position.Defense,
            _ => f.Combat.Position.Attack
        };

        double terrain = isAttacker ? 1.0 : 1.0 + f.Combat.TerrainDefenseFactor * zone.DefenseBonus;
        double collision = isAttacker ? geometry.CollisionMultiplier : 1.0;

        if (unit.PoorControl)
        {
            position *= f.Combat.PoorControl.PositionMultiplier;
            if (isAttacker)
                collision *= f.Combat.PoorControl.CollisionMultiplier;
        }

        double ammoMultiplier = unit.Ammo <= 0 ? 0.5 : 1.0;

        return unit.Strength
            * (unit.Readiness / 100.0)
            * (unit.Morale / 100.0)
            * position
            * terrain
            * collision
            * ammoMultiplier;
    }

    private static void ApplyContactMoralePenalties(IReadOnlyList<Unit> defenders, IReadOnlyDictionary<string, ContactGeometry> geometryByDefender)
    {
        foreach (var defender in defenders)
        {
            if (!geometryByDefender.TryGetValue(defender.Id, out var geometry)) continue;
            if (geometry.MoralePenalty > 0)
                defender.Morale = Math.Max(0, defender.Morale - geometry.MoralePenalty);
        }
    }

    private static int ComputeLossTotal(int selfStrength, double opposingPower, double totalPower, int unitCount, double lethality)
    {
        if (unitCount <= 0 || selfStrength <= 0 || totalPower <= 0) return 0;
        double raw = lethality * (opposingPower / totalPower) * selfStrength;
        return Math.Max(0, (int)Math.Round(raw, MidpointRounding.AwayFromZero));
    }

    private void ApplyCombatEffects(
        IReadOnlyList<Unit> units,
        int totalLoss,
        FrictionTable f,
        int minute,
        string zoneId,
        GameState state)
    {
        if (units.Count == 0) return;

        var losses = Distribute(totalLoss, units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            int unitLoss = losses[i];
            if (unit.PoorControl)
                unitLoss = (int)Math.Round(unitLoss * f.Combat.PoorControl.LossMultiplier, MidpointRounding.AwayFromZero);

            int deferred = unitLoss / 2;
            int immediate = unitLoss - deferred;

            unit.Strength = Math.Max(0, unit.Strength - immediate);

            int moraleDelta = (int)Math.Round((unitLoss / 10.0) * f.Combat.MoraleLossPerTenPercentStrength, MidpointRounding.AwayFromZero);
            if (unit.PoorControl)
                moraleDelta = (int)Math.Round(moraleDelta / Math.Max(0.01, f.Combat.PoorControl.MoraleStabilityMultiplier), MidpointRounding.AwayFromZero);
            unit.Morale = Math.Max(0, unit.Morale - moraleDelta);

            unit.Fatigue = Math.Min(100, unit.Fatigue + 5);
            unit.Readiness = Math.Max(0, unit.Readiness - 5);
            if (unit.Ammo > 0)
                unit.Ammo = Math.Max(0, unit.Ammo - (int)Math.Ceiling(f.Combat.AmmoConsumptionPerTick));

            if (deferred > 0)
            {
                state.PendingCasualties.Add(new PendingCasualty(minute + 30, unit.Id, deferred)
                {
                    ZoneId = zoneId,
                    CombatTickMinute = minute
                });
                state.Clock.Schedule(new Domain.PendingCasualties(minute + 30, unit.Id, deferred), minute + 30, EventPhase.Combat);
            }

            unit.PoorControl = unit.PoorControl || ShouldEnterPoorControl(unit);
        }
    }

    private static bool ShouldEnterPoorControl(Unit unit)
    {
        return unit.CommanderLost || unit.EffectiveOrder is { PoorControlInducing: true };
    }

    private void EvaluateOutcomes(
        IReadOnlyList<Unit> attackers,
        IReadOnlyList<Unit> defenders,
        Zone zone,
        GameState state,
        FrictionTable f)
    {
        bool enemyCapablePresent = defenders.Any(IsCapable) || attackers.Any(IsCapable);
        EvaluateSide(attackers, defenders, enemyCapablePresent, zone, state, f);
        EvaluateSide(defenders, attackers, enemyCapablePresent, zone, state, f);
    }

    private void EvaluateSide(
        IReadOnlyList<Unit> side,
        IReadOnlyList<Unit> opposing,
        bool enemyCapablePresent,
        Zone zone,
        GameState state,
        FrictionTable f)
    {
        foreach (var unit in side)
        {
            bool canWithdraw = CanWithdraw(unit, zone, state.Zones);
            bool combatEffective = _rules.IsCombatEffective(unit);
            bool panic = unit.Morale < f.Combat.CombatEffective.PanicMorale;

            if (panic && canWithdraw)
            {
                ApplyAutoWithdraw(unit, zone, state.Zones);
                continue;
            }

            if (!panic && unit.Morale >= f.Combat.CombatEffective.PanicMorale && HasHeavyLoss(unit) && combatEffective && unit.Traits.Initiative >= 2)
            {
                if (canWithdraw)
                {
                    ApplyAutoWithdraw(unit, zone, state.Zones);
                    continue;
                }
            }

            if (!canWithdraw && !combatEffective && opposing.Any(IsCapable) && enemyCapablePresent)
            {
                unit.Captured = true;
                unit.Posture = Posture.Reserve;
            }
        }
    }

    private static bool HasHeavyLoss(Unit unit)
    {
        int snapshot = Math.Max(1, unit.StrengthSnapshot);
        return unit.Strength < snapshot * (2.0 / 3.0);
    }

    private static bool IsCapable(Unit unit) => !unit.Captured && unit.Strength > 0;

    private static bool CanWithdraw(Unit unit, Zone zone, IReadOnlyDictionary<string, Zone> zones)
    {
        if (unit.Captured) return false;
        foreach (var neighborId in zone.TravelMinutes.Keys)
        {
            if (!zones.TryGetValue(neighborId, out _)) continue;
            return true;
        }
        return false;
    }

    private static void ApplyAutoWithdraw(Unit unit, Zone zone, IReadOnlyDictionary<string, Zone> zones)
    {
        var target = zone.TravelMinutes.Keys.FirstOrDefault(id => zones.ContainsKey(id));
        if (target is null) return;
        unit.MoveEdgeId = target;
        unit.AttentionEdgeId = target;
        unit.Posture = Posture.Withdraw;
        unit.LocationZoneId = target;
        if (zones.TryGetValue(target, out var z) && !z.PresentUnitIds.Contains(unit.Id))
            z.PresentUnitIds.Add(unit.Id);
        if (zone.PresentUnitIds.Contains(unit.Id))
            zone.PresentUnitIds.Remove(unit.Id);
    }

    private static int[] Distribute(int total, int count)
    {
        var result = new int[count];
        if (count <= 0 || total <= 0) return result;
        int per = total / count;
        int rem = total % count;
        for (int i = 0; i < count; i++)
            result[i] = per + (i < rem ? 1 : 0);
        return result;
    }

    private IEnumerable<Report> BuildReports(
        IReadOnlyList<Unit> blue,
        IReadOnlyList<Unit> red,
        Zone zone,
        int blueLossTotal,
        IEnumerable<ContactGeometry> geometries,
        int minute,
        FrictionTable f)
    {
        var geometry = geometries.OrderByDescending(g => g.CollisionMultiplier).FirstOrDefault();
        string geometryText = geometry is null ? "front" : geometry.Classification.ToString().ToLowerInvariant();

        foreach (var unit in blue)
        {
            int share = blue.Count == 0 ? 0 : blueLossTotal / blue.Count;
            yield return new Report
            {
                Minute = minute,
                ArrivesAt = minute + f.ReportBaseDelayMinutes,
                FromUnitId = unit.Id,
                Kind = ReportKind.CombatResult,
                Confidence = 2,
                Text = $"{unit.Name}: бой в {zone.Id} ({geometryText}), потери ~{share}."
            };
        }

        _ = red;
    }

    private sealed class DefaultGameRules : IGameRules
    {
        public bool IsCombatEffective(Unit unit)
        {
            var snapshot = unit.InitialStrengthSnapshot > 0 ? unit.InitialStrengthSnapshot : unit.Strength;
            return unit.Strength >= 0.4 * snapshot &&
                   unit.Morale >= 30 &&
                   unit.Readiness >= 20 &&
                   unit.Ammo > 0;
        }
    }
}
