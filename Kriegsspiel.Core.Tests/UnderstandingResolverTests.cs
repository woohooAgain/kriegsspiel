using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

public class UnderstandingResolverTests
{
    [Fact]
    public void Probability_FloorAndTraitModifiers()
    {
        var friction = new FrictionTable();
        var neutral = new CommanderTraits(0, 0, 0, 3);

        Assert.Equal(0.03, UnderstandingResolver.Probability(0, neutral, friction), 6);
        Assert.Equal(0.73, UnderstandingResolver.Probability(0.7, neutral, friction), 6);
        Assert.True(UnderstandingResolver.Probability(0.7, new CommanderTraits(3, 0, 0, 3), friction) < 0.73);
        Assert.Equal(0.9, UnderstandingResolver.Probability(0.7, new CommanderTraits(0, 3, 3, 3), friction), 6);
    }

    [Fact]
    public void Probability_PMinModifier_AdditiveAndMultiplicative()
    {
        var additive = new FrictionTable();
        var multiplicative = new FrictionTable
        {
            Orders = new FrictionTable.OrderParameters { PMinModifierType = "Multiplicative" }
        };
        var neutral = new CommanderTraits(0, 0, 0, 3);

        Assert.Equal(0.08, UnderstandingResolver.EffectivePMin(additive.Orders, 0.05), 6);
        Assert.Equal(0.08, UnderstandingResolver.Probability(0, neutral, additive, 0.05), 6);
        Assert.Equal(0.045, UnderstandingResolver.EffectivePMin(multiplicative.Orders, 0.5), 6);
        Assert.Equal(0.9, UnderstandingResolver.EffectivePMin(multiplicative.Orders, 29), 6);
    }

    [Fact]
    public void Probability_ProfilePMinModifier_IsAppliedToRoll()
    {
        var friction = new FrictionTable
        {
            DifficultyProfiles =
            [
                new FrictionTable.DifficultyProfile { Id = "t", Modifiers = { ["pMinModifier"] = 0.1 } }
            ]
        };
        var (state, order, _) = Scenario(new CommanderTraits(0, 0, 0, 3));

        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, friction, new CountingRandom(0.05))
            .ToList();

        Assert.Equal(Misinterpretation.DelayExecution, state.EffectiveOrders[order.Id].Misinterpretation);
    }

    [Fact]
    public void Conflict_LiteralismDominant_ResolvedBeforeRoll()
    {
        var traits = new CommanderTraits(0, 0, 3, 3);
        foreach (var roll in new[] { 0.0, 1.0 })
        {
            var (state, order, _) = Scenario(traits, constraint: OrderConstraint.NoZoneEntry);

            var reports = new UnderstandingResolver()
                .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(roll))
                .ToList();

            Assert.Empty(reports);
            Assert.Equal(Misinterpretation.LiteralExecution, state.EffectiveOrders[order.Id].Misinterpretation);
            var events = state.Clock.AdvanceTo(381).ToList();
            Assert.Single(events, e => e is OrderMisinterpreted mis && mis.Type == Misinterpretation.LiteralExecution);
        }
    }

    [Fact]
    public void Conflict_CautionDominant_BecomesQueryWithoutMisinterpretation()
    {
        var (state, order, _) = Scenario(new CommanderTraits(0, 3, 0, 3), constraint: OrderConstraint.NoZoneEntry);

        var reports = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(0.0))
            .ToList();

        var effective = state.EffectiveOrders[order.Id];
        Assert.True(effective.IsClarification);
        Assert.Equal(Misinterpretation.None, effective.Misinterpretation);
        Assert.Single(reports, r => r.Kind == ReportKind.Request);
        var events = state.Clock.AdvanceTo(381).ToList();
        Assert.Single(events, e => e is QuerySent);
        Assert.DoesNotContain(events, e => e is OrderMisinterpreted);
    }

    [Fact]
    public void Conflict_InitiativeDominant_DropsConstraintAndConcealsWhenDishonest()
    {
        var (state, order, _) = Scenario(new CommanderTraits(3, 0, 0, 1), constraint: OrderConstraint.NoZoneEntry);

        var reports = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(1.0))
            .ToList();

        var effective = state.EffectiveOrders[order.Id];
        Assert.Equal(Misinterpretation.DropConstraint, effective.Misinterpretation);
        Assert.DoesNotContain(OrderConstraint.NoZoneEntry, effective.Constraints);
        Assert.Contains("скрыто", effective.Note, StringComparison.Ordinal);
        Assert.Single(reports, r => r.Kind == ReportKind.Status);
    }

    [Fact]
    public void Conflict_TraitTie_UsesQueryFirst()
    {
        var resolution = ConflictResolver.Resolve(
            new Order { ToUnitId = "U1", Intent = OrderIntent.Seize, TargetZoneId = "B", Constraints = [OrderConstraint.NoZoneEntry] },
            new CommanderTraits(2, 2, 2, 3));

        Assert.Equal(ConflictOutcome.Query, resolution.Outcome);
    }

    [Fact]
    public void Resolve_UsesExactlyOneRollPerOrder()
    {
        var (state, order, _) = Scenario(new CommanderTraits(1, 1, 1, 3));
        var rng = new CountingRandom(1.0);

        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), rng)
            .ToList();

        Assert.Equal(1, rng.Calls);
    }

    [Fact]
    public void MissingTarget_WithoutKnownNeighbor_ClarifiesInsteadOfMisinterpreting()
    {
        var (state, order, _) = Scenario(new CommanderTraits(0, 0, 0, 3), target: null);

        var reports = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(0.0))
            .ToList();

        var effective = state.EffectiveOrders[order.Id];
        Assert.True(effective.IsClarification);
        Assert.Equal(Misinterpretation.None, effective.Misinterpretation);
        Assert.Single(reports, r => r.Kind == ReportKind.Request);
        var events = state.Clock.AdvanceTo(381).ToList();
        Assert.Single(events, e => e is QuerySent);
        Assert.DoesNotContain(events, e => e is OrderMisinterpreted);
    }

    [Fact]
    public void MissingTarget_ShiftsOnlyToKnownNeighbor()
    {
        var (state, order, _) = Scenario(new CommanderTraits(0, 0, 0, 3), target: null);
        MarkKnown(state, "B");

        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(0.0))
            .ToList();

        var effective = state.EffectiveOrders[order.Id];
        Assert.Equal("B", effective.TargetZoneId);
        Assert.Equal(Misinterpretation.ShiftZone, effective.Misinterpretation);
        Assert.Equal("B", state.Orders.Single(o => o.Id == effective.Id).TargetZoneId);
        var events = state.Clock.AdvanceTo(381).ToList();
        Assert.Single(events, e => e is OrderMisinterpreted mis && mis.Type == Misinterpretation.ShiftZone);
    }

    [Fact]
    public void HoldAndReserve_EmptyTarget_AreCorrectedFromContext()
    {
        var (holdState, holdOrder, _) = Scenario(new CommanderTraits(0, 0, 0, 3), OrderIntent.Hold, null);
        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, holdOrder.Id), holdState, new FrictionTable(), new CountingRandom(1.0))
            .ToList();
        Assert.Equal("A", holdState.EffectiveOrders[holdOrder.Id].TargetZoneId);
        Assert.Equal(Misinterpretation.None, holdState.EffectiveOrders[holdOrder.Id].Misinterpretation);

        var (reserveState, reserveOrder, _) = Scenario(new CommanderTraits(0, 0, 0, 3), OrderIntent.Reserve, null);
        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, reserveOrder.Id), reserveState, new FrictionTable(),
                new CountingRandom(1.0), new UnderstandingOptions(ReserveZoneId: "B"))
            .ToList();
        Assert.Equal("B", reserveState.EffectiveOrders[reserveOrder.Id].TargetZoneId);
        Assert.Equal(Misinterpretation.None, reserveState.EffectiveOrders[reserveOrder.Id].Misinterpretation);
    }

    [Fact]
    public void Branches_AreSelectedByCommanderKnowledge()
    {
        var branch = new OrderBranch(BranchCondition.TargetHeldByEnemy, BranchAction.Attack, BranchAction.Bypass);
        var (heldState, heldOrder, _) = Scenario(new CommanderTraits(1, 1, 1, 3), target: "B", branches: [branch]);
        MarkKnown(heldState, "B", Faction.Red);
        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, heldOrder.Id), heldState, new FrictionTable(), new CountingRandom(1.0))
            .ToList();
        Assert.Equal(BranchAction.Attack, heldState.EffectiveOrders[heldOrder.Id].SelectedBranch?.Then);

        var (emptyState, emptyOrder, _) = Scenario(new CommanderTraits(1, 1, 1, 3), target: "B", branches: [branch]);
        MarkKnown(emptyState, "B");
        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, emptyOrder.Id), emptyState, new FrictionTable(), new CountingRandom(1.0))
            .ToList();
        Assert.Equal(BranchAction.Bypass, emptyState.EffectiveOrders[emptyOrder.Id].SelectedBranch?.Then);
    }

    [Fact]
    public void ContradictoryOrderBranch_RequiresAlreadyExecutingOrder()
    {
        var branch = new OrderBranch(BranchCondition.ContradictoryOrderReceived, BranchAction.ReportImmediately);
        var (state, order, unit) = Scenario(new CommanderTraits(1, 1, 1, 3), target: "B", branches: [branch]);

        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(1.0))
            .ToList();
        Assert.Null(state.EffectiveOrders[order.Id].SelectedBranch);

        var (runningState, runningOrder, runningUnit) = Scenario(new CommanderTraits(1, 1, 1, 3), target: "B", branches: [branch]);
        runningState.Orders.Add(new Order { Id = "OLD", ToUnitId = runningUnit.Id, DeliveredMinute = 370 });
        runningUnit.CurrentOrderId = "OLD";
        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, runningOrder.Id), runningState, new FrictionTable(), new CountingRandom(1.0))
            .ToList();
        Assert.Equal(BranchAction.ReportImmediately, runningState.EffectiveOrders[runningOrder.Id].SelectedBranch?.Then);
    }

    [Fact]
    public void BranchStructure_LimitedToThree()
    {
        var four = Enumerable.Range(0, 4)
            .Select(_ => new OrderBranch(BranchCondition.CommsLost, BranchAction.Hold))
            .ToList();
        Assert.False(OrderBranchRules.IsValidStructure(four));
        Assert.True(OrderBranchRules.IsValidStructure(four.Take(3).ToList()));

        var order = new Order { ToUnitId = "U1" };
        Assert.True(order.AddBranch(four[0]));
        Assert.True(order.AddBranch(four[1]));
        Assert.True(order.AddBranch(four[2]));
        Assert.False(order.AddBranch(four[3]));
    }

    [Fact]
    public void ChangeIntent_RecordsBranchErrorSeparately_AndOriginalOrderStaysIntact()
    {
        var branch = new OrderBranch(BranchCondition.BattleEnRoute, BranchAction.Bypass, BranchAction.Hold);
        var (state, order, _) = Scenario(
            new CommanderTraits(0, 0, 3, 3),
            target: "B",
            branches: [branch],
            vague: ["по обстановке"]);

        _ = new UnderstandingResolver()
            .Resolve(new OrderDelivered(380, order.Id), state, new FrictionTable(), new CountingRandom(0.0))
            .ToList();

        var effective = state.EffectiveOrders[order.Id];
        Assert.Equal(Misinterpretation.ChangeIntent, effective.Misinterpretation);
        Assert.Equal(OrderIntent.Hold, effective.Intent);
        Assert.Equal(OrderIntent.Seize, order.Intent);
        Assert.Equal("B", order.TargetZoneId);
        var events = state.Clock.AdvanceTo(381).ToList();
        Assert.Contains(events, e => e is OrderMisinterpreted mis && mis.Type == Misinterpretation.ChangeIntent);
        Assert.Contains(events, e => e is OrderMisinterpreted mis && mis.Type == Misinterpretation.BranchError);
    }

    private static (GameState State, Order Order, Unit Unit) Scenario(
        CommanderTraits traits,
        OrderIntent intent = OrderIntent.Seize,
        string? target = "B",
        OrderConstraint? constraint = null,
        IReadOnlyList<OrderBranch>? branches = null,
        IReadOnlyList<string>? vague = null)
    {
        var unit = new Unit
        {
            Id = "U1",
            Name = "U1",
            Traits = traits,
            LocationZoneId = "A",
            Faction = Faction.Blue,
            Strength = 100,
            InitialStrengthSnapshot = 100,
            Readiness = 80,
            Morale = 80,
            Ammo = 100
        };
        var state = new GameState
        {
            ScenarioId = "t",
            Clock = new Clock(360),
            Zones = new()
            {
                ["A"] = new Zone { Id = "A", Name = "A", TravelMinutes = { ["B"] = 30, ["C"] = 30 } },
                ["B"] = new Zone { Id = "B", Name = "B", TravelMinutes = { ["A"] = 30 } },
                ["C"] = new Zone { Id = "C", Name = "C", TravelMinutes = { ["A"] = 30 } }
            },
            Units = new() { [unit.Id] = unit },
            Knowledge = new PlayerKnowledge()
        };
        var order = new Order
        {
            ToUnitId = unit.Id,
            Intent = intent,
            TargetZoneId = target,
            NotLaterThan = 450,
            ReportBy = 445,
            IssuedAt = 360,
            DeliveredMinute = 380,
            Constraints = constraint is null ? [] : [constraint.Value],
            Branches = branches is null ? [] : new List<OrderBranch>(branches),
            VaguePatterns = vague is null ? [] : new List<string>(vague)
        };
        state.Orders.Add(order);
        return (state, order, unit);
    }

    private static void MarkKnown(GameState state, string zoneId, Faction? faction = null)
    {
        var zone = state.Knowledge.GetOrCreate(zoneId);
        zone.Presence = faction is null ? KnowledgePresence.Empty : KnowledgePresence.Occupied;
        zone.ReportedFaction = faction;
        zone.DeliveryMinute = 360;
    }

    private sealed class CountingRandom(double value) : Random
    {
        public int Calls { get; private set; }

        public override double NextDouble()
        {
            Calls++;
            return value;
        }
    }
}
