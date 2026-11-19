namespace Kriegsspiel.Core.Content;

public sealed class ReportTemplates
{
    public Dictionary<string, List<string>> ByKind { get; init; } = new();

    public string Pick(string kind, IReadOnlyDictionary<string, string> vars, Random rng)
    {
        if (!ByKind.TryGetValue(kind, out var list) || list.Count == 0)
            return "[нет шаблона]";
        var t = list[rng.Next(list.Count)];
        foreach (var (k, v) in vars)
            t = t.Replace("{" + k + "}", v);
        return t;
    }
}