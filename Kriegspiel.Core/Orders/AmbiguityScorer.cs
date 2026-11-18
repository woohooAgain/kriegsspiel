namespace Kriegsspiel.Core.Orders;

public static class AmbiguityScorer
{
    public static double Score(Order o)
    {
        double s = 0;
        if ((o.Intent is OrderIntent.Seize or OrderIntent.Move or OrderIntent.Recon)
            && string.IsNullOrWhiteSpace(o.TargetZoneId)) s += 0.35;
        if (o.NotLaterThan is null) s += 0.20;
        if (o.ReportBy is null) s += 0.15;
        if (o.Constraints.Count > 1) s += 0.10;
        return Math.Min(1.0, s);
    }
}