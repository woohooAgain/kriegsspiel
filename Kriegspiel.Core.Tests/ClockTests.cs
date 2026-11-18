using Kriegsspiel.Core.Domain;
public class ClockTests
{
    [Fact]
    public void AdvanceTo_YieldsInOrder()
    {
        var clock = new Clock(360);
        clock.Schedule(new ReportDue(390, "u1", ReportKind.Status), 390);
        clock.Schedule(new ReportDue(375, "u2", ReportKind.Status), 375);

        var evts = clock.AdvanceTo(400).ToList();

        Assert.Equal(2, evts.Count);
        Assert.Equal(375, evts[0].Minute);
        Assert.Equal("u2", ((ReportDue)evts[0]).UnitId);
        Assert.Equal(400, clock.Now);
    }
}