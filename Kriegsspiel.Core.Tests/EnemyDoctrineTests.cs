using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class EnemyDoctrineTests
{
    [Fact]
    public void Author_FixedSeedAndInput_ProducesIdenticalOrders()
    {
        var state = BuildState();
        var input = BuildInput(state);
        var first = new EnemyDoctrine(new RulesStub(), new SeededRandom(1234)).Author(input);
        var second = new EnemyDoctrine(new RulesStub(), new SeededRandom(1234)).Author(input);

        Assert.Equal(first.Select(Shape), second.Select(Shape));
    }

    [Fact]
    public void Author_ProducesOrdersWithinClosedSlotSchema()
    {
        var state = BuildState();
        var input = BuildInput(state);
        var doctrine = new EnemyDoctrine(new RulesStub(), new SeededRandom(77));
        var orders = doctrine.Author(input);

        Assert.Equal(3, orders.Count);
        foreach (var order in orders)
        {
            Assert.True(doctrine.IsValidOrder(order, input));
            Assert.True(Enum.IsDefined(typeof(OrderIntent), order.Intent));
            Assert.True(Enum.IsDefined(typeof(MovementMode), order.MovementMode));
            Assert.All(order.Constraints, c => Assert.True(Enum.IsDefined(typeof(OrderConstraint), c)));
            Assert.All(order.Branches, b =>
            {
                Assert.True(Enum.IsDefined(typeof(BranchCondition), b.Condition));
                Assert.True(Enum.IsDefined(typeof(BranchAction), b.Then));
            });
            Assert.True(order.Branches.Count <= input.Friction.Orders.MaxBranches);
            Assert.NotNull(order.TargetZoneId);
            Assert.NotNull(order.NotLaterThan);
            Assert.NotNull(order.ReportBy);
            Assert.True(order.ReportBy <= order.NotLaterThan);
            Assert.InRange(order.AmbiguityScore, 0, 1);
            Assert.True(order.HasValidStructure);
        }
    }

    [Fact]
    public void Resolve_LlmDisabled_UsesDoctrineWithoutCallingCompiler()
    {
        var state = BuildState();
        var friction = new FrictionTable { Llm = new FrictionTable.LlmParameters { Enabled = false } };
        var input = BuildInput(state, friction);
        var compiler = new ThrowingCompiler();
        var doctrine = new EnemyDoctrine(new RulesStub(), new SeededRandom(31));

        Assert.True(doctrine.UsesDoctrine(friction, true));
        var resolved = doctrine.Resolve(input, true, compiler);
        var expected = new EnemyDoctrine(new RulesStub(), new SeededRandom(31)).Author(input);

        Assert.False(compiler.Called);
        Assert.Equal(expected.Select(Shape), resolved.Select(Shape));
    }

    [Fact]
    public void Resolve_CompilerThrows_FallsBackToDoctrine()
    {
        var state = BuildState();
        var friction = new FrictionTable { Llm = new FrictionTable.LlmParameters { Enabled = true } };
        var input = BuildInput(state, friction);
        var compiler = new ThrowingCompiler();
        var doctrine = new EnemyDoctrine(new RulesStub(), new SeededRandom(19));

        Assert.False(doctrine.UsesDoctrine(friction, true));
        var resolved = doctrine.Resolve(input, true, compiler);
        var expected = new EnemyDoctrine(new RulesStub(), new SeededRandom(19)).Author(input);

        Assert.True(compiler.Called);
        Assert.Equal(expected.Select(Shape), resolved.Select(Shape));
    }

    [Fact]
    public void Resolve_InvalidCompiledOrder_FallsBackToDoctrine()
    {
        var state = BuildState();
        var friction = new FrictionTable { Llm = new FrictionTable.LlmParameters { Enabled = true } };
        var input = BuildInput(state, friction);
        var compiler = new FixedCompiler([
            new Order { ToUnitId = "UNKNOWN", Intent = OrderIntent.Advance, IssuedAt = input.Minute }
        ]);
        var doctrine = new EnemyDoctrine(new RulesStub(), new SeededRandom(5));

        var resolved = doctrine.Resolve(input, true, compiler);
        var expected = new EnemyDoctrine(new RulesStub(), new SeededRandom(5)).Author(input);

        Assert.Equal(expected.Select(Shape), resolved.Select(Shape));
    }

    [Fact]
    public void Resolve_ValidCompiledOrder_PassesThroughUnchanged()
    {
        var state = BuildState();
        var friction = new FrictionTable { Llm = new FrictionTable.LlmParameters { Enabled = true } };
        var input = BuildInput(state, friction);
        var compiled = new EnemyDoctrine(new RulesStub(), new SeededRandom(44)).Author(input);
        var compiler = new FixedCompiler(compiled);
        var random = new SeededRandom(8);
        var doctrine = new EnemyDoctrine(new RulesStub(), random);
        var checkpoint = random.State;

        var resolved = doctrine.Resolve(input, true, compiler);

        Assert.Same(compiled, resolved);
        Assert.Equal(checkpoint, random.State);
    }

    [Fact]
    public void Input_DoesNotExposeHiddenPlayerTruth()
    {
        var noTruthLeakProperties = typeof(EnemyDoctrineInput).GetProperties().Where(p =>
            p.PropertyType == typeof(GameState) ||
            p.PropertyType == typeof(Unit) ||
            p.PropertyType == typeof(Zone) ||
            typeof(GameState).IsAssignableFrom(p.PropertyType));
        Assert.Empty(noTruthLeakProperties);

        var firstState = BuildState();
        var secondState = BuildState();
        secondState.Units["BLUE_1"].LocationZoneId = "A";
        secondState.Zones["C"].PresentUnitIds.Clear();
        secondState.Zones["A"].PresentUnitIds.Add("BLUE_1");

        var firstOrders = new EnemyDoctrine(new RulesStub(), new SeededRandom(61)).Author(BuildInput(firstState));
        var secondOrders = new EnemyDoctrine(new RulesStub(), new SeededRandom(61)).Author(BuildInput(secondState));

        Assert.Equal(firstOrders.Select(Shape), secondOrders.Select(Shape));
    }

    [Fact]
    public void Orders_PassThroughPipelineContractsWithoutEnemyExceptions()
    {
        var state = BuildState();
        var input = BuildInput(state);
        var orders = new EnemyDoctrine(new RulesStub(), new SeededRandom(101)).Author(input);
        state.Orders.AddRange(orders);
        var order = orders[0];

        var transmission = new TransmissionResolver().Transmit(order, state, input.Friction, new Random(7));
        Assert.True(transmission.DelayMinutes > 0);

        var deliveryMinute = state.Clock.Now + transmission.DelayMinutes;
        var understanding = new UnderstandingResolver()
            .Resolve(new OrderDelivered(deliveryMinute, order.Id), state, input.Friction, new Random(7));
        var execution = new ExecutionResolver();
        var reports = understanding
            .Concat(execution.Start(new OrderAccepted(deliveryMinute, order.Id), state, input.Friction, new Random(7)))
            .Concat(execution.GenerateReport(
                new ReportDue(deliveryMinute + 30, order.ToUnitId, ReportKind.Status),
                state,
                input.Friction,
                new Random(7)))
            .ToList();

        Assert.NotEmpty(reports);
        Assert.All(reports, r => Assert.True(r.HasRequiredFields));
        Assert.All(reports, r => Assert.Equal(order.ToUnitId, r.SourceId));
    }

    private static GameState BuildState()
    {
        var zones = new[]
        {
            new Zone
            {
                Id = "A",
                Name = "A",
                Terrain = TerrainType.Open,
                TravelMinutes = { ["B"] = 30, ["C"] = 45 },
                EdgeTypes = { ["B"] = EdgeType.Road, ["C"] = EdgeType.Road }
            },
            new Zone
            {
                Id = "B",
                Name = "B",
                Terrain = TerrainType.Forest,
                DefenseBonus = 2,
                TravelMinutes = { ["A"] = 30, ["C"] = 30 },
                EdgeTypes = { ["A"] = EdgeType.Road, ["C"] = EdgeType.Crossing },
                Objects = [new ZoneObject { Id = "BR1", Kind = ZoneObjectKind.Bridge }]
            },
            new Zone
            {
                Id = "C",
                Name = "C",
                Terrain = TerrainType.Village,
                DefenseBonus = 1,
                TravelMinutes = { ["A"] = 45, ["B"] = 30 },
                EdgeTypes = { ["A"] = EdgeType.Road, ["B"] = EdgeType.Crossing }
            }
        };

        var units = new[]
        {
            new Unit
            {
                Id = "E1",
                Name = "E1",
                Type = UnitType.Infantry,
                Faction = Faction.Red,
                Strength = 100,
                InitialStrengthSnapshot = 100,
                Readiness = 80,
                Morale = 80,
                Ammo = 50,
                LocationZoneId = "A",
                Traits = new CommanderTraits(1, 1, 1, 1)
            },
            new Unit
            {
                Id = "E2",
                Name = "E2",
                Type = UnitType.Recon,
                Faction = Faction.Red,
                Concealed = true,
                Strength = 40,
                InitialStrengthSnapshot = 40,
                Readiness = 80,
                Morale = 80,
                Ammo = 40,
                LocationZoneId = "B",
                Traits = new CommanderTraits(2, 0, 0, 2)
            },
            new Unit
            {
                Id = "E3",
                Name = "E3",
                Type = UnitType.HQ,
                Faction = Faction.Red,
                Strength = 20,
                InitialStrengthSnapshot = 20,
                Readiness = 80,
                Morale = 80,
                Ammo = 20,
                LocationZoneId = "A",
                Traits = new CommanderTraits(0, 2, 1, 2)
            },
            new Unit
            {
                Id = "BLUE_1",
                Name = "BLUE_1",
                Type = UnitType.Infantry,
                Faction = Faction.Blue,
                Strength = 120,
                InitialStrengthSnapshot = 120,
                Readiness = 90,
                Morale = 90,
                Ammo = 60,
                LocationZoneId = "C",
                Traits = new CommanderTraits(2, 1, 0, 3)
            }
        };

        var state = new GameState
        {
            ScenarioId = "enemy-doctrine-tests",
            Clock = new Clock(360),
            Zones = zones.ToDictionary(z => z.Id),
            Units = units.ToDictionary(u => u.Id),
            Knowledge = new PlayerKnowledge(),
            EndMinute = 480
        };
        foreach (var unit in state.Units.Values)
            state.Zones[unit.LocationZoneId].PresentUnitIds.Add(unit.Id);
        return state;
    }

    private static EnemyDoctrineInput BuildInput(GameState state, FrictionTable? friction = null)
    {
        var rules = new RulesStub();
        return new EnemyDoctrineInput
        {
            Faction = Faction.Red,
            Minute = state.Clock.Now,
            EndMinute = state.EndMinute,
            Friction = friction ?? new FrictionTable(),
            Units = state.Units.Values
                .Where(u => u.Faction == Faction.Red)
                .OrderBy(u => u.Id, StringComparer.Ordinal)
                .Select(u => EnemyUnitView.From(u, rules))
                .ToArray(),
            Zones = state.Zones.Values
                .OrderBy(z => z.Id, StringComparer.Ordinal)
                .Select(EnemyZoneView.From)
                .ToArray(),
            Observations = [new EnemyObservation("B", Faction.Blue, "BLUE_1", 60, 350, 2, "E2")],
            Objectives = [new EnemyObjectiveView(ObjectiveType.HoldZone, "C", null, "Blue", 480, 1.0)]
        };
    }

    private static OrderShape Shape(Order order) => new(
        order.ToUnitId,
        order.Intent,
        order.TargetZoneId,
        order.TargetObjectId,
        order.TargetUnitId,
        order.TargetEdgeId,
        order.NotLaterThan,
        order.ReportBy,
        order.MovementMode,
        string.Join(",", order.Constraints),
        string.Join(",", order.Branches.Select(b => $"{b.Condition}:{b.Then}:{b.Else}")),
        order.AmbiguityScore);

    private sealed record OrderShape(
        string ToUnitId,
        OrderIntent Intent,
        string? TargetZoneId,
        string? TargetObjectId,
        string? TargetUnitId,
        string? TargetEdgeId,
        int? Deadline,
        int? ReportBy,
        MovementMode MovementMode,
        string Constraints,
        string Branches,
        double Ambiguity);

    private sealed class RulesStub : IGameRules
    {
        public bool IsCombatEffective(Unit unit) => unit.IsCombatEffective;
    }

    private sealed class ThrowingCompiler : IEnemyOrderCompiler
    {
        public bool Called { get; private set; }

        public IReadOnlyList<Order>? TryCompile(EnemyDoctrineInput input)
        {
            Called = true;
            throw new InvalidOperationException("LLM unavailable.");
        }
    }

    private sealed class FixedCompiler(IReadOnlyList<Order> orders) : IEnemyOrderCompiler
    {
        public IReadOnlyList<Order>? TryCompile(EnemyDoctrineInput input) => orders;
    }

    private sealed class SeededRandom : IRandomSource
    {
        private const long Multiplier = 6364136223846793005L;
        private const long Increment = 1442695040888963407L;
        private readonly long _initial;

        public SeededRandom(int seed)
        {
            Seed = seed;
            _initial = unchecked(seed * Multiplier + Increment);
            State = _initial;
        }

        public int Seed { get; }
        public long State { get; private set; }

        public double NextDouble()
        {
            Step();
            return (State & long.MaxValue) / (double)long.MaxValue;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));
        }

        public void Restore(long state) => State = state;

        public void Reset() => State = _initial;

        private void Step() => State = unchecked(State * Multiplier + Increment);
    }
}
