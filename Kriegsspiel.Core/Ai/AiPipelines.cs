using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Ai;

public sealed record AiValidation<T>(bool IsValid, T? Value, string? Error)
{
    public static AiValidation<T> Ok(T value) => new(true, value, null);

    public static AiValidation<T> Fail(string error) => new(false, default, error);
}

public sealed record AiInvocationResult<T>(T Value, bool UsedFallback, string? Error);

public interface IAmbiguityInterpreter
{
    double Interpret(Order order, string? llmResponse = null);
}

public sealed class DeterministicAmbiguityInterpreter : IAmbiguityInterpreter
{
    public double Interpret(Order order, string? llmResponse = null) => AiAmbiguityGuard.Score(order);
}

public static class AiAmbiguityGuard
{
    public static double Score(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return AmbiguityScorer.Score(order);
    }
}

public static class AiPipeline
{
    public static AiInvocationResult<T> Invoke<T>(
        int seed,
        string prompt,
        ILlmClient client,
        AiReplayLog log,
        bool llmEnabled,
        Func<string, AiValidation<T>> validate,
        Func<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(fallback);

        string response;
        AiValidation<T> validation;
        if (llmEnabled)
        {
            response = client.Complete(prompt) ?? "";
            validation = response.Length == 0
                ? AiValidation<T>.Fail("empty response")
                : validate(response);
        }
        else
        {
            response = "";
            validation = AiValidation<T>.Fail("LLM disabled");
        }

        var usedFallback = !validation.IsValid;
        var value = usedFallback ? fallback() : validation.Value!;
        log.Append(new AiReplayEntry(
            seed,
            prompt,
            response,
            validation.IsValid,
            validation.Error,
            usedFallback ? "Fallback" : "Llm",
            Describe(value)));
        return new AiInvocationResult<T>(value, usedFallback, validation.Error);
    }

    private static string Describe<T>(T value) => value switch
    {
        Order order => $"{order.Id}|{order.ToUnitId}|{order.Intent}|{order.TargetZoneId}|{order.NotLaterThan}|{order.ReportBy}|{order.MovementMode}|{string.Join(",", order.Constraints)}|{order.Branches.Count}",
        Report report => $"{report.Id}|{report.Minute}|{report.ArrivesAt}|{report.FromUnitId}|{report.Kind}|{report.Confidence}|{report.FactKey}|{report.Text}",
        string text => text,
        _ => value?.ToString() ?? ""
    };
}

public sealed class AiOrderPipeline
{
    private readonly ILlmClient _client;
    private readonly AiReplayLog _log;
    private readonly IDoctrineOrderFallback _fallback;

    public AiOrderPipeline(ILlmClient client, AiReplayLog log, IDoctrineOrderFallback? fallback = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _fallback = fallback ?? new TemplateDoctrineOrderFallback();
    }

    public Order Compile(
        AiWorldView view,
        int minute,
        int seed,
        string prompt,
        bool llmEnabled,
        string? schemaVersion = null,
        string? difficultyProfileId = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!view.IsValid)
            throw new ArgumentException("World view contains data outside allowed knowledge.", nameof(view));

        var issuerId = $"HQ_{view.ViewerFaction}";
        var result = AiPipeline.Invoke<Order>(
            seed,
            prompt,
            _client,
            _log,
            llmEnabled,
            response =>
            {
                var validation = AiSchemas.ValidateOrder(response, view, schemaVersion);
                return validation.IsValid
                    ? AiValidation<Order>.Ok(validation.Value!.ToOrder(issuerId, minute, seed))
                    : AiValidation<Order>.Fail(validation.Error!);
            },
            () => _fallback.Build(view, minute, seed, difficultyProfileId));
        return result.Value;
    }
}

public sealed class AiNarrationPipeline
{
    private readonly ILlmClient _client;
    private readonly AiReplayLog _log;
    private readonly ReportTemplates _templates;

    public AiNarrationPipeline(ILlmClient client, AiReplayLog log, ReportTemplates templates)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    public Report Narrate(
        Report fact,
        string prompt,
        int seed,
        bool llmEnabled,
        string? schemaVersion = null)
    {
        ArgumentNullException.ThrowIfNull(fact);

        var result = AiPipeline.Invoke<Report>(
            seed,
            prompt,
            _client,
            _log,
            llmEnabled,
            response =>
            {
                var validation = AiSchemas.ValidateNarration(response, fact, schemaVersion);
                return validation.IsValid
                    ? AiValidation<Report>.Ok(NarrationFallback.WithText(fact, validation.Value!))
                    : AiValidation<Report>.Fail(validation.Error!);
            },
            () => NarrationFallback.Build(fact, _templates, seed));
        return result.Value;
    }
}
