using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Assessment;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class ObjectiveEvaluatorTests
{
    private sealed record TimeAdvanced(int Minute) : GameEvent(Minute);
    private sealed record CombatLoss(int Minute, string ZoneId, string UnitId, int Losses) : GameEvent(Minute);
    private sealed record InitiativeTaken(int Minute, string UnitId, string ZoneId, double ProgressAtStart) : GameEvent(Minute);
    private sealed record LiteralExecution(int Minute, string OrderId) : GameEvent(Minute);
    private sealed record CivilianCasualties(int Minute, string CivilianGroupId, int Casualties) : GameEvent(Minute);

    [Fact]
    public void ControlStates_UseWeightedScores()
    {
        var objective = new Objective
        {
            Type = ObjectiveType.HoldZone,
            Zone = "A",
            ControlMinutes = [420, 480],
            Weight = 1
        };
        var scenario = ScenarioWith([objective]);
        var friendly = Unit("U1", Faction.Blue, "A", 100);
        var enemy = Unit("ENEMY_1", Faction.Red, "A", 100);
        var state = NewState(scenario, [Zone("A")], [friendly, enemy]);
        var evaluator = new ObjectiveEvaluator();

        var contested = evaluator.EvaluateObjective(objective, state, scenario);
        Assert.Equal(0.5, contested.Fraction, 6);
        Assert.All(contested.Checks, check => Assert.Equal(ControlState.Contested, check.Control));

        enemy.Strength = 10;
        var achieved = evaluator.EvaluateObjective(objective, state, scenario);
        Assert.Equal(1, achieved.Fraction, 6);
        Assert.All(achieved.Checks, check => Assert.Equal(ControlState.Achieved, check.Control));

        friendly.Strength = 10;
        enemy.Strength = 100;
        var failed = evaluator.EvaluateObjective(objective, state, scenario);
        Assert.Equal(0, failed.Fraction, 6);
        Assert.All(failed.Checks, check => Assert.Equal(ControlState.Failed, check.Control));
    }

    [Fact]
    public void ControlMinutes_GiveShareOfSuccessfulChecks()
    {
        var objective = new Objective
        {
            Type = ObjectiveType.DelayEnemy,
            Zone = "A",
            Minutes = 60,
            ControlMinutes = [390, 420, 450],
            Weight = 1
        };
        var scenario = ScenarioWith([objective]);
        var friendly = Unit("U1", Faction.Blue, "A", 100);
        var enemy = Unit("ENEMY_1", Faction.Red, "B", 100);
        var state = NewState(scenario, [Zone("A"), Zone("B")], [friendly, enemy]);
        state.EventLog.Add(new UnitArrived(420, "ENEMY_1", "A"));

        var result = new ObjectiveEvaluator().EvaluateObjective(objective, state, scenario);

        Assert.Equal(3, result.Checks.Count);
        Assert.Equal(0.5, result.Checks[0].Score, 6);
        Assert.Equal(1, result.Checks[1].Score, 6);
        Assert.Equal(1, result.Checks[2].Score, 6);
        Assert.Equal(5.0 / 6.0, result.Fraction, 6);
    }

    [Fact]
    public void DisableEnemyStrength_IsLinearUpToRequiredPercent()
    {
        var objective = new Objective
        {
            Type = ObjectiveType.DisableEnemyStrength,
            MinPercent = 80,
            TargetFaction = "Red",
            Weight = 1
        };
        var scenario = ScenarioWith([objective]);
        var first = Unit("ENEMY_1", Faction.Red, "B", 20, snapshot: 100);
        var second = Unit("ENEMY_2", Faction.Red, "B", 100, snapshot: 100);
        var state = NewState(scenario, [Zone("B")], [first, second]);

        var partial = new ObjectiveEvaluator().EvaluateObjective(objective, state, scenario);
        Assert.Equal(0.625, partial.Fraction, 6);

        var fullObjective = new Objective
        {
            Type = ObjectiveType.DisableEnemyStrength,
            MinPercent = 40,
            TargetFaction = "Red",
            Weight = 1
        };
        var full = new ObjectiveEvaluator().EvaluateObjective(fullObjective, state, scenario);
        Assert.Equal(1, full.Fraction, 6);

        second.Strength = 100;
        first.Strength = 100;
        var none = new ObjectiveEvaluator().EvaluateObjective(objective, state, scenario);
        Assert.Equal(0, none.Fraction, 6);
    }

    [Fact]
    public void ScenarioScore_UsesWeightsAndVictoryThreshold()
    {
        var hold = new Objective
        {
            Type = ObjectiveType.HoldZone,
            Zone = "A",
            ControlMinutes = [480],
            Weight = 0.5
        };
        var contest = new Objective
        {
            Type = ObjectiveType.HoldZone,
            Zone = "B",
            ControlMinutes = [480],
            Weight = 0.5
        };
        var scenario = ScenarioWith([hold, contest]);
        var state = NewState(
            scenario,
            [Zone("A"), Zone("B")],
            [
                Unit("U1", Faction.Blue, "A", 100),
                Unit("U2", Faction.Blue, "B", 100),
                Unit("ENEMY_1", Faction.Red, "B", 100)
            ]);

        var result = new ObjectiveEvaluator().Evaluate(scenario, state);

        Assert.Equal(1, result.WeightSum, 6);
        Assert.True(result.WeightsValid);
        Assert.Equal(0.75, result.Score, 6);
        Assert.Equal(0.7, result.VictoryThreshold, 6);
        Assert.True(result.IsVictory);

        var strictScenario = ScenarioWith([hold, contest], 0.8);
        Assert.False(new ObjectiveEvaluator().Evaluate(strictScenario, state).IsVictory);
    }

    [Fact]
    public void AllSevenObjectiveTypes_AreEvaluated()
    {
        var objectives = new[]
        {
            new Objective { Type = ObjectiveType.HoldZone, Zone = "A", Weight = 1.0 / 7 },
            new Objective { Type = ObjectiveType.PreserveStrength, Unit = "U1", MinPercent = 50, Weight = 1.0 / 7 },
            new Objective { Type = ObjectiveType.DelayEnemy, Zone = "A", Minutes = 60, Weight = 1.0 / 7 },
            new Objective { Type = ObjectiveType.DisableEnemyStrength, MinPercent = 40, Weight = 1.0 / 7 },
            new Objective { Type = ObjectiveType.NoEnemyCombatEffectiveInZone, Zone = "B", Weight = 1.0 / 7 },
            new Objective { Type = ObjectiveType.EvacuateCivilians, Zone = "C", CivilianGroupId = "C1", SafeZone = "SAFE", Weight = 1.0 / 7 },
            new Objective { Type = ObjectiveType.BlockEvacuation, Zone = "C", CivilianGroupId = "C1", Weight = 1.0 / 7 }
        };
        var scenario = ScenarioWith(objectives);
        var civilians = new CivilianGroup
        {
            Id = "C1",
            Population = 20,
            LocationZoneId = "SAFE",
            Evacuated = true,
            EvacuationDeadline = 470
        };
        var zoneC = Zone("C");
        zoneC.Civilians.Add(civilians);
        var state = NewState(
            scenario,
            [Zone("A"), Zone("B"), zoneC],
            [Unit("U1", Faction.Blue, "A", 100), Unit("ENEMY_1", Faction.Red, "B", 100)]);

        var result = new ObjectiveEvaluator().Evaluate(scenario, state);

        Assert.Equal(7, result.Objectives.Count);
        Assert.Equal(7, result.Objectives.Select(o => o.Objective.Type).Distinct().Count());
        Assert.Equal(1, result.WeightSum, 6);
        Assert.All(result.Objectives, o => Assert.NotEmpty(o.Checks));
    }

    [Fact]
    public void Metrics_UseLostOrderRuleReactionTimeAndPendingLosses()
    {
        var (scenario, state) = MetricFixture();
        var withPenalty = new MetricCalculator(
            scenario,
            new MetricOptions { LostOrderPenaltyMinutes = 90 }).Calculate(state);

        Assert.Equal((25.0 + 30 + 90) / 3, withPenalty.OrderDelay.AverageMinutes, 6);
        Assert.Equal(1, withPenalty.OrderDelay.LostOrders);
        Assert.False(withPenalty.OrderDelay.LostOrdersUseInfiniteDelay);
        Assert.Equal(90, withPenalty.OrderDelay.PenaltyMinutes);
        Assert.Equal(1.0 / 3.0, withPenalty.Ambiguity.Share, 6);

        Assert.Equal(1, withPenalty.Reaction.Contradictions);
        Assert.Equal(460, withPenalty.Reaction.Details[0].ReactionMinute);
        Assert.Equal(60, withPenalty.Reaction.Details[0].DelayMinutes);
        Assert.Equal(4, withPenalty.Reaction.Details[0].Segment);

        Assert.Equal(10, withPenalty.PoorControl.ImmediateLosses);
        Assert.Equal(6, withPenalty.PoorControl.PendingLosses);
        Assert.Equal(16, withPenalty.PoorControl.TotalLosses);

        Assert.Equal(1, withPenalty.Initiative.Count);
        Assert.True(withPenalty.Initiative.Details[0].ToBetter);
        Assert.Equal(420, withPenalty.Initiative.Details[0].EvaluationMinute);
        Assert.Equal(1, withPenalty.Literalism.Count);
        Assert.Contains("O2", withPenalty.Literalism.OrderIds);

        Assert.Equal(1, withPenalty.Discrepancies.Count);
        Assert.Contains("Intent", withPenalty.Discrepancies.Details[0].Changes);
        Assert.Equal(2, withPenalty.Discrepancies.Details[0].Confidence);
        Assert.Equal("U1", withPenalty.Discrepancies.Details[0].SourceId);

        Assert.Equal(2, withPenalty.Civilians.Casualties);
        Assert.Equal(1, withPenalty.Civilians.Tasks);
        Assert.Equal(1, withPenalty.Civilians.CompletedTasks);

        var infinite = new MetricCalculator(scenario).Calculate(state);
        Assert.Equal(27.5, infinite.OrderDelay.AverageMinutes, 6);
        Assert.True(infinite.OrderDelay.LostOrdersUseInfiniteDelay);
        Assert.Equal(1, infinite.OrderDelay.LostOrders);
    }

    [Fact]
    public void Reaction_WithoutActionUsesMaximumPenalty()
    {
        var (scenario, state) = MetricFixture();
        state.EventLog.RemoveAll(e => e is TimeAdvanced or OrderIssued);

        var result = new MetricCalculator(scenario).Calculate(state);

        Assert.Equal(0, result.Reaction.Reacted);
        Assert.Equal(80, result.Reaction.Details[0].DelayMinutes);
        Assert.Equal(0, result.Reaction.Share, 6);
    }

    [Fact]
    public void AfterActionReport_ExposesTruthWithoutChangingKnowledge()
    {
        var (scenario, state) = MetricFixture();
        var knowledgeFacts = state.Knowledge.Facts.Count;
        var knowledgeZones = state.Knowledge.Zones.Count;

        var report = AfterActionReport.Build(
            scenario,
            state,
            new MetricOptions { LostOrderPenaltyMinutes = 90 });

        Assert.Equal(report.Objectives.Score, report.Score, 6);
        Assert.Equal(report.Objectives.IsVictory, report.IsVictory);
        Assert.Same(report.Metrics, report.StaffWork);
        Assert.Equal(2, report.Metrics.OrderDelay.DeliveredOrders);
        Assert.Equal(knowledgeFacts, state.Knowledge.Facts.Count);
        Assert.Equal(knowledgeZones, state.Knowledge.Zones.Count);
        Assert.Equal(480, report.EndMinute);
    }

    private static (Scenario Scenario, GameState State) MetricFixture()
    {
        var hold = new Objective
        {
            Type = ObjectiveType.HoldZone,
            Zone = "A",
            ControlMinutes = [420, 480],
            Weight = 0.5
        };
        var block = new Objective
        {
            Type = ObjectiveType.BlockEvacuation,
            Zone = "A",
            CivilianGroupId = "C1",
            ControlMinutes = [420, 480],
            Weight = 0.5
        };
        var scenario = ScenarioWith([hold, block]);
        var friendly = Unit("U1", Faction.Blue, "A", 100);
        friendly.PoorControl = true;
        var enemy = Unit("ENEMY_1", Faction.Red, "A", 100);
        var zone = Zone("A");
        var civilians = new CivilianGroup { Id = "C1", Population = 30, Casualties = 3 };
        zone.Civilians.Add(civilians);
        var state = NewState(scenario, [zone], [friendly, enemy]);

        state.Orders.Add(new Order { Id = "O1", ToUnitId = "U1", IssuedAt = 360, DeliveredMinute = 385 });
        state.Orders.Add(new Order
        {
            Id = "O2",
            ToUnitId = "U1",
            Intent = OrderIntent.Occupy,
            IssuedAt = 360,
            DeliveredMinute = 390,
            AmbiguityScore = 0.4
        });
        state.Orders.Add(new Order
        {
            Id = "O3",
            ToUnitId = "U1",
            IssuedAt = 360,
            Lost = true
        });

        var contradiction = Report.Create(
            390, 400, "S1", 2, ReportKind.Contradiction, "противоречие", "fact:bridge");
        state.EventLog.Add(new ReportDelivered(400, "R1", "U1", contradiction));
        state.EventLog.Add(new TimeAdvanced(430));
        state.EventLog.Add(new OrderIssued(460, "O4"));
        state.EventLog.Add(new PoorControlEntered(410, "U1", "потеря управления"));
        state.EventLog.Add(new CombatLoss(420, "A", "U1", 10));
        state.EventLog.Add(new PendingCasualties(450, "U1", 6));
        state.EventLog.Add(new InitiativeTaken(415, "U1", "A", 0.4));
        state.EventLog.Add(new LiteralExecution(425, "O2"));
        state.EventLog.Add(new CivilianCasualties(430, "C1", 2));
        state.PendingCasualties.Add(new PendingCasualty(450, "U1", 6));

        var effective = EffectiveOrder.From(state.Orders[1]);
        effective.Intent = OrderIntent.Hold;
        effective.Misinterpretation = Misinterpretation.ChangeIntent;
        state.EffectiveOrders[effective.Id] = effective;

        var evidence = Report.Create(
            395, 405, "U1", 2, ReportKind.Status, "подтверждение",
            facts: new Dictionary<string, string> { ["orderId"] = "O2" });
        state.DeliveredReports.Add(evidence);

        return (scenario, state);
    }

    private static Scenario ScenarioWith(
        IEnumerable<Objective> objectives,
        double victoryThreshold = 0.7) => new()
    {
        Id = "s",
        Name = "s",
        StartMinute = 360,
        EndMinute = 480,
        Seed = 42,
        VictoryThreshold = victoryThreshold,
        Objectives = objectives.ToList()
    };

    private static GameState NewState(Scenario scenario, Zone[] zones, Unit[] units)
    {
        var state = new GameState
        {
            ScenarioId = scenario.Id,
            Clock = new Clock(scenario.StartMinute),
            Zones = zones.ToDictionary(z => z.Id),
            Units = units.ToDictionary(u => u.Id),
            Knowledge = new PlayerKnowledge(),
            EndMinute = scenario.EndMinute
        };
        foreach (var zone in zones)
        {
            foreach (var unit in units.Where(u => u.LocationZoneId == zone.Id))
                zone.PresentUnitIds.Add(unit.Id);
        }
        return state;
    }

    private static Zone Zone(string id) => new()
    {
        Id = id,
        Name = id,
        Terrain = TerrainType.Open
    };

    private static Unit Unit(
        string id,
        Faction faction,
        string zoneId,
        int strength,
        int snapshot = 100) => new()
    {
        Id = id,
        Name = id,
        Faction = faction,
        LocationZoneId = zoneId,
        Strength = strength,
        InitialStrengthSnapshot = snapshot,
        Morale = 80,
        Readiness = 80,
        Ammo = 10,
        Traits = new CommanderTraits(1, 1, 1, 1)
    };
}
