using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Orders;

public class AmbiguityScorerTests
{
    [Fact]
    public void FullOrder_ZeroScore()
    {
        var order = Complete(OrderIntent.Seize);

        var actual = AmbiguityScorer.Breakdown(order);

        Assert.Equal(0.0, actual.Total);
        Assert.Equal(0.0, actual.MissingTarget);
        Assert.Equal(0.0, actual.Vague);
        Assert.Equal(0.0, actual.MissingDeadline);
        Assert.Equal(0.0, actual.MissingReportBy);
        Assert.Equal(0.0, actual.Branches);
    }

    [Fact]
    public void MissingTarget_HasExactWeight()
    {
        var order = Complete(OrderIntent.Seize, target: null);

        var actual = AmbiguityScorer.Breakdown(order);

        Assert.Equal(0.7, actual.MissingTarget, 6);
        Assert.Equal(0.7, actual.Total, 6);
    }

    [Fact]
    public void Vague_Deadlines_AndBranches_HaveExactWeights_AndClamp()
    {
        var order = Complete(OrderIntent.Seize, target: null, deadline: null, reportBy: null, vague: ["по обстановке"]);
        order.AddBranch(new OrderBranch(BranchCondition.BattleEnRoute, BranchAction.Bypass));
        order.AddBranch(new OrderBranch(BranchCondition.BridgeDestroyed, BranchAction.AlternateTarget));

        var actual = AmbiguityScorer.Breakdown(order);

        Assert.Equal(0.7, actual.MissingTarget, 6);
        Assert.Equal(0.25, actual.Vague, 6);
        Assert.Equal(0.1, actual.MissingDeadline, 6);
        Assert.Equal(0.1, actual.MissingReportBy, 6);
        Assert.Equal(0.1, actual.Branches, 6);
        Assert.Equal(1.0, actual.Total, 6);
    }

    [Fact]
    public void Clamp_ToUnit()
    {
        var order = Complete(OrderIntent.Seize, target: null, deadline: null, reportBy: null, vague: ["maybe"]);
        for (var i = 0; i < 3; i++)
            order.AddBranch(new OrderBranch(BranchCondition.CommsLost, BranchAction.Hold));

        Assert.Equal(1.0, AmbiguityScorer.Score(order), 6);
    }

    [Fact]
    public void Hold_EmptyTarget_DoesNotAddMissingTargetWeight()
    {
        var order = Complete(OrderIntent.Hold, target: null);

        Assert.Equal(0.0, AmbiguityScorer.Score(order), 6);
        Assert.Equal(0.0, AmbiguityScorer.Breakdown(order).MissingTarget, 6);
    }

    [Fact]
    public void Reserve_UsesReserveZoneFromContext()
    {
        var order = Complete(OrderIntent.Reserve, target: null);

        Assert.Equal(0.7, AmbiguityScorer.Score(order), 6);
        Assert.Equal(0.0, AmbiguityScorer.Score(order, new OrderScoringContext(ReserveZoneId: "R")), 6);
    }

    [Fact]
    public void Guard_UsesContextObjectOrPosition()
    {
        var order = Complete(OrderIntent.Guard, target: null);

        Assert.Equal(0.7, AmbiguityScorer.Score(order), 6);
        Assert.Equal(0.0, AmbiguityScorer.Score(order, new OrderScoringContext(GuardObjectId: "OBJ")), 6);
        Assert.Equal(0.0, AmbiguityScorer.Score(order, new OrderScoringContext(CurrentZoneId: "A")), 6);
    }

    [Fact]
    public void CustomFrictionWeights_AreUsed()
    {
        var friction = new FrictionTable
        {
            Orders = new FrictionTable.OrderParameters
            {
                Ambiguity = new FrictionTable.AmbiguityWeights
                {
                    MissingTarget = 0.4,
                    VagueFormulation = 0.2,
                    MissingDeadline = 0.05,
                    MissingReportTime = 0.05,
                    PerBranch = 0.01
                }
            }
        };
        var order = Complete(OrderIntent.Seize, target: null, vague: ["на усмотрение"]);

        Assert.Equal(0.6, AmbiguityScorer.Score(order, friction), 6);
    }

    private static Order Complete(
        OrderIntent intent,
        string? target = "B",
        int? deadline = 450,
        int? reportBy = 445,
        IReadOnlyList<string>? vague = null) => new()
    {
        ToUnitId = "U1",
        Intent = intent,
        TargetZoneId = target,
        NotLaterThan = deadline,
        ReportBy = reportBy,
        VaguePatterns = vague is null ? [] : new List<string>(vague)
    };
}
