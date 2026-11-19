using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed class CombatResolver : ICombatResolver
{
    public IEnumerable<Report> Resolve(CombatTick e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Zones.TryGetValue(e.ZoneId, out var zone)) yield break;
        var present = zone.PresentUnitIds.Where(s.Units.ContainsKey).Select(id => s.Units[id]).ToList();
        var friend = present.Where(u => !u.Id.StartsWith("ENEMY_")).ToList();
        var enemy = present.Where(u => u.Id.StartsWith("ENEMY_")).ToList();
        if (friend.Count == 0 || enemy.Count == 0) yield break;

        double pa = friend.Sum(u => Power(u, zone)) * (1 + rng.NextDouble() * 0.1);
        double pb = enemy.Sum(u => Power(u, zone)) * (1 + rng.NextDouble() * 0.1);
        double ratio = pa / (pa + pb);

        int lossF = (int)(enemy.Sum(u => u.Strength) * (1 - ratio) * f.CombatLethality);
        int lossE = (int)(friend.Sum(u => u.Strength) * ratio * f.CombatLethality);

        foreach (var u in friend) { u.Strength = Math.Max(0, u.Strength - lossF / friend.Count); u.Morale -= 5; }
        foreach (var u in enemy) { u.Strength = Math.Max(0, u.Strength - lossE / enemy.Count); }

        foreach (var u in friend)
            yield return new Report
            {
                Minute = e.Minute,
                ArrivesAt = e.Minute + 20,
                FromUnitId = u.Id,
                Kind = ReportKind.CombatResult,
                Confidence = 1,
                Text = $"{u.Name}: бой в {zone.Id}, потери ~{lossF / friend.Count}%, отошли/держимся."
            };
    }

    private static double Power(Unit u, Zone z) =>
        u.Strength * (u.Readiness / 100.0) * (u.Morale / 100.0)
        * (1 + (u.Posture == Posture.Defend ? z.DefenseBonus * 0.15 : 0));
}