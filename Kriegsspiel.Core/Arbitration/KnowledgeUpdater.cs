using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed class KnowledgeUpdater : IKnowledgeUpdater
{
    public void Apply(PlayerKnowledge knowledge, Report report)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(report);

        if (!report.HasRequiredFields)
            throw new ArgumentException("A report needs event minute, delivery minute, source and confidence.", nameof(report));

        knowledge.Apply(report);
    }
}
