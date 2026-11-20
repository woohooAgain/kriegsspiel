using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.AI;

public static class StateQueryTools
{
    public static string AmbiguityPrompt => Load("AmbiguityPrompt.txt");

    public static string BuildOrderPrompt(AiWorldView view, string schemaVersion) =>
        AiPromptBuilder.BuildOrderPrompt(
            view,
            Load("OrderPrompt.txt"),
            Load("OrderSchema.json").Replace("{version}", schemaVersion, StringComparison.Ordinal));

    public static string BuildNarrationPrompt(Report fact, string schemaVersion) =>
        AiPromptBuilder.BuildNarrationPrompt(
            fact,
            Load("NarrationPrompt.txt"),
            Load("NarrationSchema.json").Replace("{version}", schemaVersion, StringComparison.Ordinal));

    public static AiWorldView Project(
        GameState state,
        Faction viewerFaction,
        IEnumerable<AiZoneView>? scenarioAllowedKnowledge = null) =>
        AiWorldView.FromState(state, viewerFaction, scenarioAllowedKnowledge);

    private static string Load(string name)
    {
        var resourceName = "Kriegsspiel.AI.Prompts." + name;
        using var stream = typeof(StateQueryTools).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource {resourceName}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
