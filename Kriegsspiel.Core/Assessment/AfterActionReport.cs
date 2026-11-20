using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Assessment;

public sealed class AfterActionReport
{
    public required ScenarioObjectiveEvaluation Objectives { get; init; }
    public required MetricSnapshot Metrics { get; init; }
    public required int EndMinute { get; init; }

    public double Score => Objectives.Score;
    public bool IsVictory => Objectives.IsVictory;
    public MetricSnapshot StaffWork => Metrics;

    public static AfterActionReport Build(
        Scenario scenario,
        GameState state,
        MetricOptions? options = null,
        ObjectiveEvaluator? evaluator = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(state);

        var objectiveEvaluator = evaluator ?? new ObjectiveEvaluator();
        var metrics = new MetricCalculator(scenario, options, objectiveEvaluator).Calculate(state);
        return new AfterActionReport
        {
            Objectives = objectiveEvaluator.Evaluate(scenario, state),
            Metrics = metrics,
            EndMinute = scenario.EndMinute > 0
                ? scenario.EndMinute
                : state.EndMinute ?? state.Clock.Now
        };
    }
}
