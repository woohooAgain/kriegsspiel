using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Reporting;

public sealed class ReportContext
{
    public required Report Fact { get; init; }
    public required Unit Unit { get; init; }
    public required ReportTemplates Templates { get; init; }
    public required Random Rng { get; init; }
}

public interface IReportNarrator { Report Narrate(ReportContext ctx); }

public sealed class TemplateReportNarrator : IReportNarrator
{
    public Report Narrate(ReportContext ctx)
    {
        var vars = new Dictionary<string, string>
        {
            ["zone"] = ctx.Unit.LocationZoneId,
            ["unit"] = ctx.Unit.Name,
            ["strength"] = ctx.Unit.Strength.ToString()
        };
        var text = ctx.Templates.Pick(ctx.Fact.Kind.ToString(), vars, ctx.Rng);
        return new Report
        {
            Id = ctx.Fact.Id,
            Minute = ctx.Fact.Minute,
            ArrivesAt = ctx.Fact.ArrivesAt,
            FromUnitId = ctx.Fact.FromUnitId,
            Kind = ctx.Fact.Kind,
            Confidence = ctx.Fact.Confidence,
            Text = text
        };
    }
}