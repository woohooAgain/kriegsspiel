using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.AI;

public sealed class LlmAmbiguityInterpreter : IAmbiguityInterpreter
{
    private readonly AiReplayLog _log;
    private readonly IAmbiguityInterpreter _deterministic = new DeterministicAmbiguityInterpreter();

    public LlmAmbiguityInterpreter(AiReplayLog log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    public double Interpret(Order order, string? llmResponse = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (llmResponse is not null)
        {
            _log.Append(new AiReplayEntry(
                AiStableId.Int(order.Id),
                StateQueryTools.AmbiguityPrompt,
                llmResponse,
                false,
                "Understanding must remain deterministic.",
                "Fallback",
                "AmbiguityScorer"));
        }
        return _deterministic.Interpret(order, llmResponse);
    }
}
