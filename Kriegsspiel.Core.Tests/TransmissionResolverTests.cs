using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Kriegsspiel.Core.Tests.Fixtures;

public class TransmissionResolverTests
{
    [Fact]
    public void Transmit_ReturnsPositiveDelay()
    {
        var b = TestContent.Bundle();
        var s = GameStateFactory.FromBundle(b);
        var o = new Order { ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };
        var r = new TransmissionResolver().Transmit(o, s, b.Friction, new Random(42));
        Assert.True(r.DelayMinutes > 0);
    }

    [Fact]
    public void Transmit_Delivers_06_15_Order_At_06_45_With_DeliveryTime30_And_Returns_By_Eta()
    {
        var b = BundleWithTiming();
        var s = GameStateFactory.FromBundle(b);
        SetClock(s, 375);
        var resolver = new TransmissionResolver();
        var order = new Order { Id = "ord_0615", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A", IssuedAt = 375 };

        var result = resolver.Transmit(order, s, b.Friction, new FixedSequenceRandom(0.9, 0.9, 0.9, 0.9));

        Assert.False(result.Lost);
        Assert.Equal(30, result.DelayMinutes);
        Assert.Single(s.Couriers);
        var courier = s.Couriers[0];
        Assert.Equal(375, courier.OutboundDepartureMinute);
        Assert.Equal(30, courier.OutboundDeliveryTime);
        Assert.Equal(25, courier.ReturnTime);
        Assert.Equal(405, courier.ExpectedDeliveryMinute);
        Assert.Equal(430, courier.ExpectedReturnMinute);
        Assert.Equal(490, courier.ReplacementMinute);
        Assert.Equal(CourierState.Delivering, courier.State);
        Assert.Contains(s.Clock.PendingKeys, k => k.Minute == 430);

        resolver.UpdateTo(s, 430);
        Assert.Equal(CourierState.Returned, courier.State);
        Assert.True(courier.DeliveryConfirmed);
    }

    [Fact]
    public void QueuedOrders_DispatchFifo_OnCourierFree_WithActualDispatchEta()
    {
        var b = BundleWithTiming(couriers: 1);
        var s = GameStateFactory.FromBundle(b);
        SetClock(s, 375);
        var resolver = new TransmissionResolver();
        var o1 = new Order { Id = "o1", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };
        var o2 = new Order { Id = "o2", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };
        var o3 = new Order { Id = "o3", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };

        resolver.Transmit(o1, s, b.Friction, new FixedSequenceRandom(0.9, 0.9));
        var r2 = resolver.Transmit(o2, s, b.Friction, new FixedSequenceRandom(0.9, 0.9));
        var r3 = resolver.Transmit(o3, s, b.Friction, new FixedSequenceRandom(0.9, 0.9));
        Assert.False(r2.Lost);
        Assert.False(r3.Lost);
        Assert.Equal(85, r2.DelayMinutes);
        Assert.Equal(140, r3.DelayMinutes);
        var queue = resolver.Pool.GetQueue(s);
        Assert.Equal(2, queue.Count);
        Assert.Equal("o2", queue[0].MessageId);
        Assert.Equal("o3", queue[1].MessageId);

        resolver.UpdateTo(s, 430, new FixedSequenceRandom(0.9, 0.9));
        var c2 = s.Couriers.First(c => c.AssignedOrderId == "o2");
        Assert.Equal(430, c2.OutboundDepartureMinute);
        Assert.Equal(460, c2.ExpectedDeliveryMinute);
        Assert.Equal(485, c2.ExpectedReturnMinute);

        resolver.UpdateTo(s, 485, new FixedSequenceRandom(0.9, 0.9));
        var c3 = s.Couriers.First(c => c.AssignedOrderId == "o3");
        Assert.Equal(485, c3.OutboundDepartureMinute);
        Assert.Equal(515, c3.ExpectedDeliveryMinute);
        Assert.Equal(540, c3.ExpectedReturnMinute);
    }

    [Fact]
    public void Query_HasPriority_Over_Order_InQueue_And_DispatchesFirst()
    {
        var b = BundleWithTiming(couriers: 1);
        var s = GameStateFactory.FromBundle(b);
        SetClock(s, 375);
        var resolver = new TransmissionResolver();

        resolver.Transmit(new Order { Id = "ord1", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" }, s, b.Friction, new FixedSequenceRandom(0.9, 0.9));
        resolver.Transmit(new Order { Id = "ord2", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" }, s, b.Friction, new FixedSequenceRandom(0.9, 0.9));
        var q = resolver.TransmitQuery("q1", "U1", "ENEMY_1", s, b.Friction, new FixedSequenceRandom(0.9, 0.9));
        Assert.Null(q);

        var queue = resolver.Pool.GetQueue(s);
        Assert.Equal(CourierMessageKind.Query, queue[0].Kind);
        Assert.Equal("q1", queue[0].MessageId);
        Assert.Equal("ord2", queue[1].MessageId);
        Assert.Contains(s.Clock.PendingKeys, k => k.Minute == 375 && k.Phase == EventPhase.Scheduled);

        resolver.UpdateTo(s, 430, new FixedSequenceRandom(0.9, 0.9));
        var courier = s.Couriers.First(c => c.AssignedQueryId == "q1");
        Assert.Equal("q1", courier.AssignedQueryId);
        Assert.Null(courier.AssignedOrderId);
    }

    [Fact]
    public void OutboundLoss_MarksOrderLost_AndCourierLost()
    {
        var b = BundleWithTiming(couriers: 1, outboundLossBase: 1.0);
        var s = GameStateFactory.FromBundle(b);
        SetClock(s, 375);
        var resolver = new TransmissionResolver();
        var order = new Order { Id = "ord_lost", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };

        var result = resolver.Transmit(order, s, b.Friction, new FixedSequenceRandom(0.0, 0.9));

        Assert.True(result.Lost);
        Assert.Equal(30, result.DelayMinutes);
        var courier = s.Couriers.Single();
        Assert.True(courier.LostOnOutbound);
        Assert.Equal(CourierState.Lost, courier.State);
        Assert.False(courier.DeliveryConfirmed);
        Assert.Contains(s.Clock.PendingKeys, k => k.Minute == 405);
    }

    [Fact]
    public void ReturnLoss_DeliversOrder_ButDeliveryNotConfirmed_AndCourierReplacedAfter60MinFromCalculatedReturn()
    {
        var b = BundleWithTiming(couriers: 1, outboundLossBase: 0.5);
        var s = GameStateFactory.FromBundle(b);
        SetClock(s, 375);
        var resolver = new TransmissionResolver();
        var order = new Order { Id = "ord_ret_lost", ToUnitId = "ENEMY_1", Intent = OrderIntent.Move, TargetZoneId = "A" };

        var result = resolver.Transmit(order, s, b.Friction, new FixedSequenceRandom(0.9, 0.0));

        Assert.False(result.Lost);
        Assert.Equal(30, result.DelayMinutes);
        var courier = s.Couriers.Single();
        Assert.True(courier.LostOnReturn);
        Assert.Equal(430, courier.ExpectedReturnMinute);
        Assert.Equal(490, courier.ReplacementMinute);
        Assert.Contains(s.Clock.PendingKeys, k => k.Minute == 430);

        resolver.UpdateTo(s, 430);
        Assert.Equal(CourierState.Lost, courier.State);
        Assert.False(courier.DeliveryConfirmed);

        resolver.UpdateTo(s, 489);
        Assert.DoesNotContain(s.Couriers, c => c.IsAvailable);
        resolver.UpdateTo(s, 490);
        Assert.Contains(s.Couriers, c => c.IsAvailable && c.State == CourierState.Idle);
    }

    [Fact]
    public void Query_Dispatch_SchedulesQuerySent_AndUsesCourierMessage()
    {
        var b = BundleWithTiming(couriers: 1);
        var s = GameStateFactory.FromBundle(b);
        SetClock(s, 375);
        var resolver = new TransmissionResolver();

        var dispatch = resolver.TransmitQuery("q-1", "U1", "ENEMY_1", s, b.Friction, new FixedSequenceRandom(0.9, 0.9));

        Assert.NotNull(dispatch);
        Assert.False(dispatch!.OutboundLost);
        Assert.Equal(375, dispatch.DispatchMinute);
        Assert.Equal(405, dispatch.DeliveryMinute);
        Assert.Equal(430, dispatch.ReturnMinute);
        Assert.Contains(s.Clock.PendingKeys, k => k.Minute == 375 && k.Phase == EventPhase.Scheduled);
        var courier = s.Couriers.Single();
        Assert.Equal("q-1", courier.AssignedQueryId);
        Assert.True(courier.IsQuery);
    }

    private static ScenarioBundle BundleWithTiming(int couriers = 1, double outboundLossBase = 0.0, double returnLossBase = 0.0)
    {
        var b = TestContent.Bundle();
        return new ScenarioBundle
        {
            Scenario = b.Scenario,
            Friction = new FrictionTable
            {
                Hq = new FrictionTable.HqParameters
                {
                    Couriers = couriers,
                    CourierReplacementMinutes = b.Friction.Hq.CourierReplacementMinutes,
                    DefaultDeliveryTimeMinutes = 30,
                    DefaultReturnTimeMinutes = 25,
                    CourierLossBase = outboundLossBase,
                    CourierLossPerKm = 0.0,
                    CourierLossHostileBonus = 0.0,
                    ReportLossBase = b.Friction.Hq.ReportLossBase
                },
                Orders = b.Friction.Orders,
                Movement = b.Friction.Movement,
                Combat = b.Friction.Combat,
                Recon = b.Friction.Recon,
                Reports = b.Friction.Reports,
                Supply = b.Friction.Supply,
                Capture = b.Friction.Capture,
                Civilians = b.Friction.Civilians,
                Llm = b.Friction.Llm,
                DifficultyProfiles = b.Friction.DifficultyProfiles
            },
            Reports = b.Reports,
            ScenarioRaw = b.ScenarioRaw,
            FrictionRaw = b.FrictionRaw,
            ReportsRaw = b.ReportsRaw
        };
    }

    private static ScenarioBundle BundleWithReturnLoss(int couriers = 1)
    {
        var b = BundleWithTiming(couriers, 0.0, 0.0);
        return b;
    }

    private static void SetClock(GameState state, int minute)
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var prop = typeof(GameState).GetProperty("Clock", flags);
        var field = typeof(GameState).GetField("<Clock>k__BackingField", flags);
        if (field is not null)
            field.SetValue(state, new Clock(minute));
        else if (prop?.CanWrite == true)
            prop.SetValue(state, new Clock(minute));
        else
            throw new InvalidOperationException("Cannot set Clock for test");
    }

    private sealed class FixedSequenceRandom : Random
    {
        private readonly Queue<double> _values;

        public FixedSequenceRandom(params double[] values)
        {
            _values = new Queue<double>(values.Length == 0 ? new[] { 0.5 } : values);
        }

        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next() => 0;
        protected override double Sample() => _values.Count > 0 ? _values.Dequeue() : 0.5;
    }
}
