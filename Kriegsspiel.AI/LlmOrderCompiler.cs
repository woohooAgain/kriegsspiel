using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.AI;

public sealed class LlmOrderCompiler
{
    private readonly FrictionTable _friction;
    private readonly AiOrderPipeline _pipeline;

    public LlmOrderCompiler(
        FrictionTable friction,
        ILlmClient client,
        AiReplayLog log,
        IDoctrineOrderFallback? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(friction);
        _friction = friction;
        _pipeline = new AiOrderPipeline(client, log, fallback);
    }

    public Order Compile(AiWorldView view, int minute, int seed, string? difficultyProfileId = null)
    {
        if (view.ViewerFaction != Faction.Red)
            throw new ArgumentException("LLM order authoring is only available for the enemy HQ.", nameof(view));

        return _pipeline.Compile(
            view,
            minute,
            seed,
            StateQueryTools.BuildOrderPrompt(view, _friction.Llm.SchemaVersion),
            _friction.Llm.Enabled,
            _friction.Llm.SchemaVersion,
            difficultyProfileId);
    }
}
