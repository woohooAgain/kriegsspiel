using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public sealed class AIAdapterTests
{
    [Fact]
    public void ValidateOrder_AcceptsCompleteSchemaAndRejectsInvalidValues()
    {
        var view = View();
        var valid = AiSchemas.ValidateOrder(ValidOrderJson(), view);

        Assert.True(valid.IsValid);
        var draft = valid.Value!;
        Assert.Equal(OrderIntent.Occupy, draft.Intent);
        Assert.Equal(MovementMode.Covert, draft.MovementMode);
        Assert.Single(draft.Branches);
        Assert.Equal(OrderConstraint.NoZoneEntry, draft.Constraints[0]);
        Assert.Equal("R1", draft.ToUnitId);

        var badIntent = AiSchemas.ValidateOrder(
            ValidOrderJson().Replace("\"Occupy\"", "\"Teleport\"", StringComparison.Ordinal), view);
        Assert.False(badIntent.IsValid);
        Assert.Contains("intent", badIntent.Error, StringComparison.Ordinal);

        var extraField = AiSchemas.ValidateOrder(
            ValidOrderJson().Replace("\"originalText\"", "\"secret\": true, \"originalText\"", StringComparison.Ordinal), view);
        Assert.False(extraField.IsValid);
        Assert.Contains("unknown property", extraField.Error, StringComparison.Ordinal);

        var unknownZone = AiSchemas.ValidateOrder(
            ValidOrderJson().Replace("\"targetZoneId\": \"B\"", "\"targetZoneId\": \"C\"", StringComparison.Ordinal), view);
        Assert.False(unknownZone.IsValid);
        Assert.False(AiSchemas.ValidateOrder(FourBranchesOrderJson(), view).IsValid);
    }

    [Fact]
    public void Compile_ValidResponse_UsesLlmDraft()
    {
        var view = View();
        var log = new AiReplayLog();
        var pipeline = new AiOrderPipeline(new DelegateLlmClient(_ => ValidOrderJson()), log);

        var order = pipeline.Compile(view, view.Minute, 42, Prompt(view), true, AiSchemas.Version);

        Assert.Equal(OrderIntent.Occupy, order.Intent);
        Assert.Equal(MovementMode.Covert, order.MovementMode);
        Assert.Equal(435, order.NotLaterThan);
        Assert.Equal(450, order.ReportBy);
        Assert.Single(order.Branches);
        Assert.Single(log.Entries);
        Assert.True(log.Entries[0].Valid);
        Assert.Equal("Llm", log.Entries[0].Source);
    }

    [Fact]
    public void Compile_InvalidResponse_UsesDoctrineFallbackAndLogsIt()
    {
        var view = View();
        var log = new AiReplayLog();
        var pipeline = new AiOrderPipeline(new DelegateLlmClient(_ => "not json"), log);

        var order = pipeline.Compile(view, view.Minute, 42, Prompt(view), true, AiSchemas.Version);
        var expected = new TemplateDoctrineOrderFallback().Build(view, view.Minute, 42);

        Assert.Equal(expected.Id, order.Id);
        Assert.Equal(expected.ToUnitId, order.ToUnitId);
        Assert.True(order.HasValidStructure);
        Assert.Single(log.Entries);
        Assert.False(log.Entries[0].Valid);
        Assert.Equal("Fallback", log.Entries[0].Source);
        Assert.Equal(42, log.Entries[0].Seed);
    }

    [Fact]
    public void Compile_DisabledLlm_DoesNotCallClientAndUsesFallback()
    {
        var view = View();
        var log = new AiReplayLog();
        var client = new DelegateLlmClient(_ => throw new InvalidOperationException("LLM must not be called."));

        var order = new AiOrderPipeline(client, log).Compile(view, view.Minute, 7, Prompt(view), false, AiSchemas.Version);

        Assert.True(order.HasValidStructure);
        Assert.Equal("Fallback", log.Entries[0].Source);
        Assert.Equal("LLM disabled", log.Entries[0].Error);
    }

    [Fact]
    public void Narrate_ValidResponse_OnlyReplacesText()
    {
        var fact = Fact();
        var log = new AiReplayLog();
        var pipeline = new AiNarrationPipeline(
            new DelegateLlmClient(_ => """{"schemaVersion":"1.0","text":"Подразделение держит позицию."}"""),
            log,
            Templates());

        var narrated = pipeline.Narrate(fact, NarrationPrompt(fact), 42, true, AiSchemas.Version);

        Assert.Equal("Подразделение держит позицию.", narrated.Text);
        AssertSameFacts(fact, narrated);
        Assert.True(log.Entries[0].Valid);
        Assert.Equal("Llm", log.Entries[0].Source);
    }

    [Fact]
    public void Narrate_InjectedFieldsOrTime_FallsBackWithoutChangingFacts()
    {
        var fact = Fact();
        var responses = new[]
        {
            """{"schemaVersion":"1.0","text":"Донесение получено.","confidence":3}""",
            """{"schemaVersion":"1.0","text":"Прибытие в 07:30."}"""
        };

        foreach (var response in responses)
        {
            var log = new AiReplayLog();
            var pipeline = new AiNarrationPipeline(new DelegateLlmClient(_ => response), log, Templates());

            var narrated = pipeline.Narrate(fact, NarrationPrompt(fact), 42, true, AiSchemas.Version);

            Assert.False(log.Entries[0].Valid);
            Assert.Equal("Fallback", log.Entries[0].Source);
            Assert.NotEqual(response, narrated.Text);
            AssertSameFacts(fact, narrated);
        }
    }

    [Fact]
    public void OrderPrompt_ContainsOnlyAllowedEnemyKnowledge()
    {
        var state = new GameState
        {
            ScenarioId = "s",
            Clock = new Clock(375),
            Knowledge = new PlayerKnowledge(),
            Zones = new Dictionary<string, Zone>
            {
                ["B"] = new Zone
                {
                    Id = "B",
                    Name = "B",
                    PresentUnitIds = { "TOP_SECRET_ZONE_MARKER" },
                    ControllingFaction = Faction.Blue
                }
            },
            Units = new Dictionary<string, Unit>
            {
                ["R1"] = new Unit
                {
                    Id = "R1",
                    Name = "R1",
                    LocationZoneId = "B",
                    Faction = Faction.Red,
                    Traits = new CommanderTraits(1, 1, 1, 1)
                },
                ["U1"] = new Unit
                {
                    Id = "U1",
                    Name = "TOP_SECRET_PLAYER_UNIT",
                    LocationZoneId = "B",
                    Faction = Faction.Blue,
                    Strength = 999,
                    Ammo = 777,
                    Experience = 3,
                    Concealed = true,
                    Traits = new CommanderTraits(1, 1, 1, 1)
                }
            }
        };

        var view = AiWorldView.FromState(state, Faction.Red);
        var prompt = Prompt(view);

        Assert.DoesNotContain("TOP_SECRET_PLAYER_UNIT", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("TOP_SECRET_ZONE_MARKER", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("\"U1\"", prompt, StringComparison.Ordinal);
        var context = prompt.Split("\nSCHEMA:\n", 2)[0];
        foreach (var field in AiWorldView.ForbiddenFields)
            Assert.DoesNotContain(field, context, StringComparison.OrdinalIgnoreCase);
        Assert.Single(view.Units);
        Assert.Equal("R1", view.Units[0].Id);
    }

    [Fact]
    public void FromState_LegacyEnemyPrefixIsTreatedAsRedWithoutHiddenFields()
    {
        var state = new GameState
        {
            ScenarioId = "s",
            Clock = new Clock(375),
            Knowledge = new PlayerKnowledge(),
            Zones = new Dictionary<string, Zone>(),
            Units = new Dictionary<string, Unit>
            {
                ["ENEMY_1"] = new Unit
                {
                    Id = "ENEMY_1",
                    Name = "LegacyEnemy",
                    LocationZoneId = "B",
                    Faction = Faction.Blue,
                    Strength = 999,
                    Ammo = 777,
                    Traits = new CommanderTraits(1, 1, 1, 1)
                }
            }
        };

        var view = AiWorldView.FromState(state, Faction.Red);
        var unit = Assert.Single(view.Units);
        var prompt = Prompt(view);
        var context = prompt.Split("\nSCHEMA:\n", 2)[0];

        Assert.Equal("ENEMY_1", unit.Id);
        Assert.Equal(Faction.Red, unit.Faction);
        Assert.DoesNotContain("999", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("777", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("strength", context, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FixedSeedAndLog_AreReproducible()
    {
        var view = View();
        var prompt = Prompt(view);
        var firstLog = new AiReplayLog();
        var first = Run(view, prompt, firstLog, ValidOrderJson());
        var secondLog = new AiReplayLog();
        var second = Run(view, prompt, secondLog, ValidOrderJson());
        var changedLog = new AiReplayLog();
        Run(view, prompt, changedLog, ValidOrderJson().Replace("Covert", "Contact", StringComparison.Ordinal));

        Assert.Equal(firstLog.Digest, secondLog.Digest);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Intent, second.Intent);
        Assert.Equal(first.MovementMode, second.MovementMode);
        Assert.Equal(first.NotLaterThan, second.NotLaterThan);
        Assert.NotEqual(firstLog.Digest, changedLog.Digest);
        Assert.Single(firstLog.Entries);
        Assert.Single(secondLog.Entries);
        Assert.Equal(firstLog.Entries[0].Prompt, secondLog.Entries[0].Prompt);
    }

    [Fact]
    public void AmbiguityInterpreter_IgnoresLlmResponse()
    {
        var order = new Order
        {
            ToUnitId = "R1",
            Intent = OrderIntent.Hold,
            NotLaterThan = 435,
            ReportBy = 450
        };
        var interpreter = new DeterministicAmbiguityInterpreter();

        Assert.Equal(interpreter.Interpret(order), interpreter.Interpret(order, """{"intent":"wrong"}"""));
        Assert.Equal(AiAmbiguityGuard.Score(order), interpreter.Interpret(order));
    }

    private static Order Run(AiWorldView view, string prompt, AiReplayLog log, string response) =>
        new AiOrderPipeline(new DelegateLlmClient(_ => response), log)
            .Compile(view, view.Minute, 42, prompt, true, AiSchemas.Version);

    private static AiWorldView View() => new()
    {
        ViewerFaction = Faction.Red,
        Minute = 375,
        Units = [new AiUnitView("R1", "R1", UnitType.Infantry, "B", Faction.Red)],
        Zones = [new AiZoneView("B", KnowledgePresence.Occupied, "Blue", 1, 360, 380)]
    };

    private static string Prompt(AiWorldView view) =>
        AiPromptBuilder.BuildOrderPrompt(view, AiSchemas.OrderInstructionText, AiSchemas.OrderSchemaText);

    private static string NarrationPrompt(Report fact) =>
        AiPromptBuilder.BuildNarrationPrompt(fact, AiSchemas.NarrationInstructionText, AiSchemas.NarrationSchemaText);

    private static Report Fact() => Report.Create(
        360,
        380,
        "U1",
        2,
        ReportKind.Status,
        "original",
        "zone:B",
        Faction.Blue,
        null,
        new Dictionary<string, string> { ["zone"] = "B", ["strength"] = "80" });

    private static ReportTemplates Templates() => new()
    {
        ByKind = new Dictionary<string, List<string>>
        {
            ["Status"] = ["{unit} в {zone}: силы {strength}."]
        }
    };

    private static void AssertSameFacts(Report expected, Report actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.EventMinute, actual.EventMinute);
        Assert.Equal(expected.DeliveryMinute, actual.DeliveryMinute);
        Assert.Equal(expected.SourceId, actual.SourceId);
        Assert.Equal(expected.Confidence, actual.Confidence);
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.FactKey, actual.FactKey);
        Assert.Equal(expected.SourceFaction, actual.SourceFaction);
        Assert.Equal(expected.Facts.Count, actual.Facts.Count);
        foreach (var (key, value) in expected.Facts)
            Assert.Equal(value, actual.Facts[key]);
    }

    private static string FourBranchesOrderJson() => """
        {
          "schemaVersion": "1.0",
          "toUnitId": "R1",
          "intent": "Occupy",
          "targetZoneId": "B",
          "targetObjectId": null,
          "targetUnitId": null,
          "notLaterThan": 435,
          "reportBy": 450,
          "movementMode": "Covert",
          "constraints": [],
          "branches": [
            {"condition":"BridgeDestroyed","then":"Bypass","else":null,"droppedConstraintName":null},
            {"condition":"CommsLost","then":"ReportImmediately","else":null,"droppedConstraintName":null},
            {"condition":"BattleEnRoute","then":"Attack","else":null,"droppedConstraintName":null},
            {"condition":"DeadlineExpired","then":"Withdraw","else":null,"droppedConstraintName":null}
          ],
          "originalText": "Занять B"
        }
        """;

    private static string ValidOrderJson() => """
        {
          "schemaVersion": "1.0",
          "toUnitId": "R1",
          "intent": "Occupy",
          "targetZoneId": "B",
          "targetObjectId": null,
          "targetUnitId": null,
          "notLaterThan": 435,
          "reportBy": 450,
          "movementMode": "Covert",
          "constraints": ["NoZoneEntry"],
          "branches": [
            {
              "condition": "BridgeDestroyed",
              "then": "Bypass",
              "else": null,
              "droppedConstraintName": null
            }
          ],
          "originalText": "Занять B к 07:15"
        }
        """;
}
