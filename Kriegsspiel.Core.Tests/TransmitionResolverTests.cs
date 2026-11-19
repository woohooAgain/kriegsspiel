using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Kriegsspiel.Core.Tests.Fixtures;
using System.Reflection.Metadata;
public class TransmissionResolverTests
{
    [Fact]
    public void Transmit_ReturnsPositiveDelay()
    {
        var b = TestContent.Bundle();
        var s = Kriegsspiel.Core.Content.GameStateFactory.FromBundle(b);
        var o = new Order { ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };
        var r = new TransmissionResolver().Transmit(o, s, b.Friction, new Random(42));
        Assert.True(r.DelayMinutes > 0);
    }
}