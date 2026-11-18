using Kriegsspiel.Core.Orders;
public class AmbiguityScorerTests
{
    [Fact]
    public void FullOrder_LowScore()
    {
        var o = new Order
        {
            ToUnitId = "U1",
            Intent = OrderIntent.Seize,
            TargetZoneId = "B",
            NotLaterThan = 450,
            ReportBy = 450
        };
        Assert.True(AmbiguityScorer.Score(o) < 0.1);
    }
    [Fact]
    public void NoTarget_HighScore()
    {
        var o = new Order { ToUnitId = "U1", Intent = OrderIntent.Seize };
        Assert.True(AmbiguityScorer.Score(o) > 0.3);
    }
}