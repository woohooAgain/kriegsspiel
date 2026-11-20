namespace Kriegsspiel.Core.Ai;

public sealed record AiReplayEntry(
    int Seed,
    string Prompt,
    string Response,
    bool Valid,
    string? Error,
    string Source,
    string Output);

public sealed class AiReplayLog
{
    private readonly List<AiReplayEntry> _entries = new();

    public IReadOnlyList<AiReplayEntry> Entries => _entries;

    public string Digest => AiStableId.Create(_entries.Select(entry =>
        $"{entry.Seed}\u001f{entry.Prompt}\u001f{entry.Response}\u001f{entry.Valid}\u001f{entry.Error}\u001f{entry.Source}\u001f{entry.Output}"));

    public void Append(AiReplayEntry entry) => _entries.Add(entry);
}
