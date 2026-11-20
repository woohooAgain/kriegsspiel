namespace Kriegsspiel.Core.Contracts;

using Kriegsspiel.Core.Domain;

public interface IKnowledgeUpdater
{
    void Apply(PlayerKnowledge knowledge, Report report);

    void Apply(PlayerKnowledge knowledge, IEnumerable<Report> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        foreach (var report in reports)
            Apply(knowledge, report);
    }
}
