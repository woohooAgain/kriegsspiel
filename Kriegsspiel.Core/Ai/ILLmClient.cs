namespace Kriegsspiel.Core.Ai;

public interface ILlmClient
{
    string? Complete(string prompt);
}

public sealed class DelegateLlmClient : ILlmClient
{
    private readonly Func<string, string?> _complete;

    public DelegateLlmClient(Func<string, string?> complete) =>
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));

    public string? Complete(string prompt) => _complete(prompt);
}
