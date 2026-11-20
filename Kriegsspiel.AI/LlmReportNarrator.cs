using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Reporting;

namespace Kriegsspiel.AI;

public sealed class LlmReportNarrator : IReportNarrator
{
    private readonly FrictionTable _friction;
    private readonly AiNarrationPipeline _pipeline;

    public LlmReportNarrator(
        FrictionTable friction,
        ILlmClient client,
        AiReplayLog log,
        ReportTemplates templates)
    {
        ArgumentNullException.ThrowIfNull(friction);
        _friction = friction;
        _pipeline = new AiNarrationPipeline(client, log, templates);
    }

    public Report Narrate(Report fact, int seed) =>
        _pipeline.Narrate(
            fact,
            StateQueryTools.BuildNarrationPrompt(fact, _friction.Llm.SchemaVersion),
            seed,
            _friction.Llm.Enabled,
            _friction.Llm.SchemaVersion);

    public Report Narrate(ReportContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return Narrate(ctx.Fact, AiStableId.Int(ctx.Fact.Id));
    }
}
