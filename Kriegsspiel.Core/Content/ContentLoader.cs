using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Content;

public sealed class ContentValidationException : Exception
{
    public ContentValidationException(string message) : base(message)
    {
    }
}

public sealed class ContentLoader
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
    };

    private static readonly HashSet<string> EdgeTypes = new(StringComparer.OrdinalIgnoreCase) { "Road", "Offroad" };
    private static readonly HashSet<string> Factions = new(StringComparer.OrdinalIgnoreCase) { "Blue", "Red" };
    private static readonly HashSet<string> Postures = new(StringComparer.OrdinalIgnoreCase) { "March", "Defend", "Attack" };
    private static readonly HashSet<string> ControlStates = new(StringComparer.OrdinalIgnoreCase) { "Achieved", "Contested", "Failed" };
    private static readonly HashSet<string> PMinModifierTypes = new(StringComparer.OrdinalIgnoreCase) { "Additive", "Multiplicative" };

    public ScenarioBundle Load(string scenarioPath, string frictionPath, string reportsPath)
    {
        var scenarioText = ReadRequiredFile(scenarioPath, "scenario");
        var frictionText = ReadRequiredFile(frictionPath, "friction");
        var reportsText = ReadRequiredFile(reportsPath, "reports");

        var normalizedScenario = NormalizeScenarioJson(scenarioPath, scenarioText);
        var scenarioRaw = ParseJson(scenarioPath, normalizedScenario);
        var frictionRaw = ParseJson(frictionPath, frictionText);
        var reportsRaw = ParseJson(reportsPath, reportsText);

        var scenario = Deserialize<Scenario>(scenarioPath, normalizedScenario, "scenario");
        var friction = Deserialize<FrictionTable>(frictionPath, frictionText, "friction");
        var reports = Deserialize<ReportTemplates>(reportsPath, reportsText, "reports");

        ApplyScenarioDefaults(scenario);
        ValidateScenario(scenario);
        ValidateFriction(friction);
        ValidateReports(reports);

        return new ScenarioBundle
        {
            Scenario = scenario,
            Friction = friction,
            Reports = reports,
            ScenarioRaw = scenarioRaw,
            FrictionRaw = frictionRaw,
            ReportsRaw = reportsRaw
        };
    }

    private static string ReadRequiredFile(string path, string kind)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ContentValidationException($"{kind} path is empty.");
        if (!File.Exists(path))
            throw new ContentValidationException($"{kind} file not found: '{path}'.");
        var text = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(text))
            throw new ContentValidationException($"{kind} file is empty: '{path}'.");
        return text;
    }

    private static JsonElement ParseJson(string path, string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            return doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new ContentValidationException($"Invalid JSON in '{path}': {ex.Message}");
        }
    }

    private static T Deserialize<T>(string path, string text, string kind) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, Opt)
                   ?? throw new ContentValidationException($"{kind} is null in '{path}'.");
        }
        catch (JsonException ex)
        {
            throw new ContentValidationException($"Failed to deserialize {kind} '{path}': {ex.Message}");
        }
    }

    private static string NormalizeScenarioJson(string path, string text)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
        }
        catch (JsonException ex)
        {
            throw new ContentValidationException($"Invalid JSON in '{path}': {ex.Message}");
        }

        if (node is not JsonObject root)
            return text;

        if (root["courierPool"] is JsonObject pool)
        {
            MoveIfMissing(pool, "count", root, "courierCount");
            MoveIfMissing(pool, "courierLossProbability", root, "courierLossProbability");
            MoveIfMissing(pool, "reportLossProbability", root, "reportLossProbability");
        }

        if (root["zones"] is JsonArray zones)
        {
            foreach (var zone in zones.OfType<JsonObject>())
            {
                Move(zone, "observability", "observation");
                Move(zone, "courierTimes", "courierMinutes");
            }
        }

        if (root["units"] is JsonArray units)
        {
            foreach (var unit in units.OfType<JsonObject>())
            {
                Move(unit, "zoneId", "locationZoneId");
                Move(unit, "initialStrength", "initialStrengthSnapshot");
                if (unit["posture"] is JsonValue postureValue && postureValue.TryGetValue<string>(out var posture))
                    unit["posture"] = NormalizePosture(posture);
                if (unit["traits"] is JsonObject traits)
                {
                    Move(traits, "init", "initiative");
                    Move(traits, "pedantry", "literalism");
                    Move(traits, "honesty", "reportHonesty");
                }
            }
        }

        return root.ToJsonString();
    }

    private static void MoveIfMissing(JsonObject source, string from, JsonObject target, string to)
    {
        if (!target.ContainsKey(to) && source[from] is { } value)
            target[to] = value.DeepClone();
    }

    private static void Move(JsonObject source, string from, string to)
    {
        if (!source.ContainsKey(to) && source[from] is { } value)
            source[to] = value.DeepClone();
        source.Remove(from);
    }

    private static string NormalizePosture(string posture) => posture.ToLowerInvariant() switch
    {
        "defense" or "defend" => "Defend",
        "march" => "March",
        "attack" => "Attack",
        _ => posture
    };

    private static void ApplyScenarioDefaults(Scenario scenario)
    {
        scenario.VictoryThreshold = scenario.WinThreshold;

        var hq = scenario.Units.FirstOrDefault(u => u.Type == UnitType.HQ && u.Faction == Faction.Blue);
        if (hq is not null)
        {
            scenario.HqZoneId ??= hq.LocationZoneId;
            scenario.Units.RemoveAll(u => u.Type == UnitType.HQ);
        }
    }

    private static void ValidateScenario(Scenario scenario)
    {
        if (string.IsNullOrWhiteSpace(scenario.Id))
            throw new ContentValidationException("Scenario id is required.");
        if (string.IsNullOrWhiteSpace(scenario.Name))
            throw new ContentValidationException($"Scenario '{scenario.Id}': name is required.");
        if (scenario.StartMinute < 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': startMinute must be >= 0.");
        if (scenario.EndMinute <= scenario.StartMinute)
            throw new ContentValidationException($"Scenario '{scenario.Id}': endMinute must be greater than startMinute.");
        if (scenario.EndMinute % 30 != 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': endMinute must be a multiple of 30.");
        if (!scenario.Seed.HasValue)
            throw new ContentValidationException($"Scenario '{scenario.Id}': seed is required.");
        if (scenario.Seed.Value < 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': seed must be >= 0.");
        if (!IsProbability(scenario.WinThreshold) || scenario.WinThreshold <= 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': winThreshold must be in (0,1].");
        if (scenario.CourierCount < 1)
            throw new ContentValidationException($"Scenario '{scenario.Id}': courierPool.count must be >= 1.");
        if (!IsProbability(scenario.CourierLossProbability))
            throw new ContentValidationException($"Scenario '{scenario.Id}': courierPool.courierLossProbability must be in [0,1] (actual {FormatDouble(scenario.CourierLossProbability)}).");
        if (!IsProbability(scenario.ReportLossProbability))
            throw new ContentValidationException($"Scenario '{scenario.Id}': courierPool.reportLossProbability must be in [0,1] (actual {FormatDouble(scenario.ReportLossProbability)}).");
        if (string.IsNullOrWhiteSpace(scenario.HqZoneId))
            throw new ContentValidationException($"Scenario '{scenario.Id}': hqZone is required.");
        if (scenario.Zones.Count == 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': at least one zone is required.");
        if (scenario.Units.Count == 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': at least one unit is required.");
        if (scenario.Objectives.Count == 0)
            throw new ContentValidationException($"Scenario '{scenario.Id}': at least one objective is required.");

        var zoneIds = EnsureUniqueIds(scenario.Zones.Select(z => z.Id), "zone", $"scenario '{scenario.Id}'");
        var unitIds = EnsureUniqueIds(scenario.Units.Select(u => u.Id), "unit", $"scenario '{scenario.Id}'");

        if (!zoneIds.Contains(scenario.HqZoneId))
            throw new ContentValidationException($"Scenario '{scenario.Id}': hqZone references unknown zone '{scenario.HqZoneId}'.");

        ValidateZones(scenario, zoneIds);
        ValidateUnits(scenario, zoneIds);
        ValidateObjectives(scenario, zoneIds, unitIds);
        ValidateConnectivity(scenario, zoneIds);
    }

    private static void ValidateZones(Scenario scenario, HashSet<string> zoneIds)
    {
        var zoneMap = scenario.Zones.ToDictionary(z => z.Id, StringComparer.Ordinal);

        foreach (var zone in scenario.Zones)
        {
            if (string.IsNullOrWhiteSpace(zone.Id))
                throw new ContentValidationException($"Scenario '{scenario.Id}': zone id is required.");
            if (string.IsNullOrWhiteSpace(zone.Name))
                throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' name is required.");
            if (zone.DefenseBonus is < 0 or > 2)
                throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' defenseBonus must be in [0,2].");
            if (zone.Observation is < 1 or > 3)
                throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' observability must be in [1,3].");

            foreach (var (neighbor, minutes) in zone.TravelMinutes)
            {
                if (neighbor == zone.Id)
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' cannot reference itself in travelMinutes.");
                if (!zoneIds.Contains(neighbor))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' references unknown neighbor '{neighbor}'.");
                if (minutes <= 0)
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' travelTime to '{neighbor}' must be > 0.");
                if (!zone.CourierMinutes.TryGetValue(neighbor, out var courierMinutes))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' courierTime to '{neighbor}' is required.");
                if (courierMinutes <= 0)
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' courierTime to '{neighbor}' must be > 0.");
                if (!zone.EdgeTypes.TryGetValue(neighbor, out var edgeType))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' edge type to '{neighbor}' is required.");
                if (edgeType is not (EdgeType.Road or EdgeType.Offroad))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' edge type to '{neighbor}' must be road or offroad.");
            }
        }

        foreach (var zone in scenario.Zones)
        {
            foreach (var (neighbor, minutes) in zone.TravelMinutes)
            {
                if (!zoneMap.TryGetValue(neighbor, out var other))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': zone '{zone.Id}' references unknown neighbor '{neighbor}'.");
                if (!other.TravelMinutes.TryGetValue(zone.Id, out var backMinutes))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': edge '{zone.Id}' -> '{neighbor}' is not symmetric (missing reverse edge).");
                if (backMinutes != minutes)
                    throw new ContentValidationException($"Scenario '{scenario.Id}': edge '{zone.Id}' <-> '{neighbor}' has asymmetric travelTime ({minutes} != {backMinutes}).");
                if (!other.CourierMinutes.TryGetValue(zone.Id, out var backCourier))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': edge '{zone.Id}' -> '{neighbor}' is not symmetric (missing reverse courierTime).");
                if (backCourier != zone.CourierMinutes[neighbor])
                    throw new ContentValidationException($"Scenario '{scenario.Id}': edge '{zone.Id}' <-> '{neighbor}' has asymmetric courierTime.");
                if (!other.EdgeTypes.TryGetValue(zone.Id, out var backType))
                    throw new ContentValidationException($"Scenario '{scenario.Id}': edge '{zone.Id}' -> '{neighbor}' is not symmetric (missing reverse edge type).");
                if (backType != zone.EdgeTypes[neighbor])
                    throw new ContentValidationException($"Scenario '{scenario.Id}': edge '{zone.Id}' <-> '{neighbor}' has asymmetric edge type.");
            }
        }
    }

    private static void ValidateUnits(Scenario scenario, HashSet<string> zoneIds)
    {
        foreach (var unit in scenario.Units)
        {
            if (string.IsNullOrWhiteSpace(unit.Id))
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit id is required.");
            if (string.IsNullOrWhiteSpace(unit.Name))
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' name is required.");
            if (unit.Faction is not (Faction.Blue or Faction.Red))
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' faction must be Blue or Red.");
            if (string.IsNullOrWhiteSpace(unit.LocationZoneId) || !zoneIds.Contains(unit.LocationZoneId))
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' has unknown zoneId '{unit.LocationZoneId}'.");
            if (unit.InitialStrengthSnapshot <= 0)
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' initialStrength must be > 0.");
            if (unit.Strength <= 0)
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' initialStrength must be > 0.");
            if (unit.Readiness is < 0 or > 100)
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' readiness must be in [0,100].");
            if (unit.Ammo < 0)
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' ammo must be >= 0.");
            if (!Postures.Contains(unit.Posture.ToString()))
                throw new ContentValidationException($"Scenario '{scenario.Id}': unit '{unit.Id}' posture must be defense, march or attack.");
            ValidateTraits(scenario.Id, unit.Id, unit.Traits);
        }
    }

    private static void ValidateTraits(string scenarioId, string unitId, CommanderTraits traits)
    {
        if (traits is null)
            throw new ContentValidationException($"Scenario '{scenarioId}': unit '{unitId}' traits are required.");
        ValidateTraitRange(traits.Init, "init", scenarioId, unitId);
        ValidateTraitRange(traits.Pedantry, "pedantry", scenarioId, unitId);
        ValidateTraitRange(traits.Honesty, "honesty", scenarioId, unitId);
    }

    private static void ValidateTraitRange(int value, string name, string scenarioId, string unitId)
    {
        if (value < 0 || value > 3)
            throw new ContentValidationException($"Scenario '{scenarioId}': unit '{unitId}' trait '{name}' must be in [0,3].");
    }

    private static void ValidateObjectives(Scenario scenario, HashSet<string> zoneIds, HashSet<string> unitIds)
    {
        var weightSum = 0.0;
        foreach (var objective in scenario.Objectives)
        {
            if (objective.Weight <= 0)
                throw new ContentValidationException($"Scenario '{scenario.Id}': objective weight must be > 0.");
            weightSum += objective.Weight;
            ValidateObjectiveTargets(scenario.Id, objective, zoneIds, unitIds);
            foreach (var minute in objective.ControlMinutes)
            {
                if (minute < scenario.StartMinute || minute > scenario.EndMinute)
                    throw new ContentValidationException($"Scenario '{scenario.Id}': objective control minute {minute} must be within scenario bounds.");
            }
            if (!string.IsNullOrWhiteSpace(objective.TargetFaction))
                ValidateEnumString(objective.TargetFaction, Factions, $"Scenario '{scenario.Id}': objective targetFaction");
            if (objective.ControlStateWeights.Count > 0)
            {
                foreach (var (state, w) in objective.ControlStateWeights)
                {
                    ValidateEnumString(state, ControlStates, $"Scenario '{scenario.Id}': objective control state");
                    if (w < 0 || w > 1)
                        throw new ContentValidationException($"Scenario '{scenario.Id}': objective control state weight for '{state}' must be in [0,1].");
                }
            }
        }

        if (!AreClose(weightSum, 1.0))
            throw new ContentValidationException($"Scenario '{scenario.Id}': objectives weight sum must be 1 (actual {FormatDouble(weightSum)}).");
    }

    private static void ValidateObjectiveTargets(string scenarioId, Objective objective, HashSet<string> zoneIds, HashSet<string> unitIds)
    {
        switch (objective.Type)
        {
            case ObjectiveType.HoldZone:
            case ObjectiveType.NoEnemyCombatEffectiveInZone:
                RequireZoneRef(scenarioId, objective.Zone, zoneIds, objective.Type.ToString());
                break;
            case ObjectiveType.PreserveStrength:
                RequireUnitRef(scenarioId, objective.Unit, unitIds, objective.Type.ToString());
                break;
            case ObjectiveType.DelayEnemy:
                if (!string.IsNullOrWhiteSpace(objective.Zone))
                    RequireZoneRef(scenarioId, objective.Zone, zoneIds, objective.Type.ToString());
                break;
            case ObjectiveType.DisableEnemyStrength:
                break;
            case ObjectiveType.EvacuateCivilians:
            case ObjectiveType.BlockEvacuation:
                RequireZoneRef(scenarioId, objective.Zone, zoneIds, objective.Type.ToString());
                if (!string.IsNullOrWhiteSpace(objective.SafeZone))
                    RequireZoneRef(scenarioId, objective.SafeZone, zoneIds, objective.Type.ToString() + ".safeZone");
                break;
        }
    }

    private static void ValidateConnectivity(Scenario scenario, HashSet<string> zoneIds)
    {
        if (scenario.Zones.Count <= 1)
            return;

        var adjacency = scenario.Zones.ToDictionary(
            z => z.Id,
            z => z.TravelMinutes.Keys.Where(zoneIds.Contains).ToList(),
            StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        var start = scenario.Zones[0].Id;
        visited.Add(start);
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        if (visited.Count != scenario.Zones.Count)
            throw new ContentValidationException($"Scenario '{scenario.Id}': graph must be connected.");
    }

    private static void RequireZoneRef(string scenarioId, string? zoneId, HashSet<string> zoneIds, string context)
    {
        if (string.IsNullOrWhiteSpace(zoneId) || !zoneIds.Contains(zoneId))
            throw new ContentValidationException($"Scenario '{scenarioId}': {context} references unknown zone '{zoneId}'.");
    }

    private static void RequireUnitRef(string scenarioId, string? unitId, HashSet<string> unitIds, string context)
    {
        if (string.IsNullOrWhiteSpace(unitId) || !unitIds.Contains(unitId))
            throw new ContentValidationException($"Scenario '{scenarioId}': {context} references unknown unit '{unitId}'.");
    }

    private static void ValidateFriction(FrictionTable f)
    {
        if (f.Hq.Couriers < 1)
            throw new ContentValidationException("Friction hq.couriers must be >= 1.");
        ValidatePositive(f.Hq.CourierReplacementMinutes, "friction.hq.courierReplacementMinutes");
        ValidatePositive(f.Hq.DefaultDeliveryTimeMinutes, "friction.hq.defaultDeliveryTimeMinutes");
        ValidatePositive(f.Hq.DefaultReturnTimeMinutes, "friction.hq.defaultReturnTimeMinutes");
        ValidateProbability(f.Hq.CourierLossBase, "friction.hq.courierLossBase");
        ValidateNonNegative(f.Hq.CourierLossPerKm, "friction.hq.courierLossPerKm");
        ValidateProbability(f.Hq.CourierLossHostileBonus, "friction.hq.courierLossHostileBonus");
        ValidateProbability(f.Hq.ReportLossBase, "friction.hq.reportLossBase");

        ValidateProbability(f.Orders.P0, "friction.orders.p0");
        ValidateProbability(f.Orders.PMin, "friction.orders.pMin");
        ValidateProbability(f.Orders.PMax, "friction.orders.pMax");
        if (f.Orders.PMin > f.Orders.PMax)
            throw new ContentValidationException("friction.orders.pMin must be <= pMax.");
        if (f.Orders.P0 < f.Orders.PMin)
            throw new ContentValidationException("friction.orders.p0 must be >= pMin (consistent with Pmin floor).");
        ValidateEnumString(f.Orders.PMinModifierType, PMinModifierTypes, "friction.orders.pMinModifierType");
        if (f.Orders.MaxBranches < 0)
            throw new ContentValidationException("friction.orders.maxBranches must be >= 0.");

        ValidateNonNegative(f.Orders.Ambiguity.MissingTarget, "friction.orders.ambiguity.missingTarget");
        ValidateNonNegative(f.Orders.Ambiguity.VagueFormulation, "friction.orders.ambiguity.vagueFormulation");
        ValidateNonNegative(f.Orders.Ambiguity.MissingDeadline, "friction.orders.ambiguity.missingDeadline");
        ValidateNonNegative(f.Orders.Ambiguity.MissingReportTime, "friction.orders.ambiguity.missingReportTime");
        ValidateNonNegative(f.Orders.Ambiguity.PerBranch, "friction.orders.ambiguity.perBranch");
        ValidateNonNegative(f.Orders.Traits.Literalism, "friction.orders.traits.literalism");
        ValidateNonNegative(f.Orders.Traits.Caution, "friction.orders.traits.caution");
        ValidateNonNegative(f.Orders.Traits.Initiative, "friction.orders.traits.initiative");

        ValidateMovementMode(f.Movement.March, "friction.movement.march");
        ValidateMovementMode(f.Movement.Stealth, "friction.movement.stealth");
        ValidateMovementMode(f.Movement.ContactReady, "friction.movement.contactReady");

        ValidateNonNegative(f.Combat.LethalitySelf, "friction.combat.lethalitySelf");
        ValidateNonNegative(f.Combat.LethalityEnemy, "friction.combat.lethalityEnemy");
        ValidatePositive(f.Combat.Position.March, "friction.combat.position.march");
        ValidatePositive(f.Combat.Position.Attack, "friction.combat.position.attack");
        ValidatePositive(f.Combat.Position.Defense, "friction.combat.position.defense");
        ValidatePositive(f.Combat.Collision.Front, "friction.combat.collision.front");
        ValidatePositive(f.Combat.Collision.Flank, "friction.combat.collision.flank");
        ValidatePositive(f.Combat.Collision.Rear, "friction.combat.collision.rear");
        ValidateNonNegative(f.Combat.TerrainDefenseFactor, "friction.combat.terrainDefenseFactor");
        ValidateNonNegative(f.Combat.MoraleLossPerTenPercentStrength, "friction.combat.moraleLossPerTenPercentStrength");
        ValidateProbability(f.Combat.CombatEffective.StrengthRatio, "friction.combat.combatEffective.strengthRatio");
        ValidateNonNegative(f.Combat.CombatEffective.Morale, "friction.combat.combatEffective.morale");
        ValidateNonNegative(f.Combat.CombatEffective.Readiness, "friction.combat.combatEffective.readiness");
        ValidateNonNegative(f.Combat.CombatEffective.Ammo, "friction.combat.combatEffective.ammo");
        ValidateNonNegative(f.Combat.CombatEffective.PanicMorale, "friction.combat.combatEffective.panicMorale");
        ValidateProbability(f.Combat.CaptureBaseChance, "friction.combat.captureBaseChance");
        ValidateProbability(f.Combat.CaptureEncircledBonus, "friction.combat.captureEncircledBonus");
        ValidateNonNegative(f.Combat.CaptureInitiativePenalty, "friction.combat.captureInitiativePenalty");
        ValidateNonNegative(f.Combat.EntrenchmentRatePerStep, "friction.combat.entrenchmentRatePerStep");
        ValidateNonNegative(f.Combat.EntrenchmentCap, "friction.combat.entrenchmentCap");
        ValidatePositive(f.Combat.PoorControl.PositionMultiplier, "friction.combat.poorControl.positionMultiplier");
        ValidatePositive(f.Combat.PoorControl.CollisionMultiplier, "friction.combat.poorControl.collisionMultiplier");
        ValidatePositive(f.Combat.PoorControl.MoraleStabilityMultiplier, "friction.combat.poorControl.moraleStabilityMultiplier");
        ValidatePositive(f.Combat.PoorControl.LossMultiplier, "friction.combat.poorControl.lossMultiplier");
        ValidateNonNegative(f.Combat.AmmoConsumptionPerTick, "friction.combat.ammoConsumptionPerTick");

        ValidateProbability(f.Recon.DetectionBase, "friction.recon.detectionBase");
        ValidateNonNegative(f.Recon.DetectionObservabilityWeight, "friction.recon.detectionObservabilityWeight");
        ValidateNonNegative(f.Recon.OpDetectionBonus, "friction.recon.opDetectionBonus");
        ValidateNonNegative(f.Recon.CamouflageStealthBonus, "friction.recon.camouflageStealthBonus");
        ValidateProbability(f.Recon.DetectionMin, "friction.recon.detectionMin");
        ValidateProbability(f.Recon.DetectionMax, "friction.recon.detectionMax");
        if (f.Recon.DetectionMin > f.Recon.DetectionMax)
            throw new ContentValidationException("friction.recon.detectionMin must be <= detectionMax.");
        ValidateProbability(f.Recon.SuccessBase, "friction.recon.successBase");
        ValidateProbability(f.Recon.UnconsciousFailBase, "friction.recon.unconsciousFailBase");
        ValidateProbability(f.Recon.UnconsciousFailCap, "friction.recon.unconsciousFailCap");
        ValidatePositive(f.Recon.FactKeyWindowMinutes, "friction.recon.factKeyWindowMinutes");

        ValidatePositive(f.Reports.BaseDelayMinutes, "friction.reports.baseDelayMinutes");
        ValidateNonNegative(f.Reports.JitterMinutes, "friction.reports.jitterMinutes");
        ValidateNonNegative(f.Reports.StrengthNoise, "friction.reports.strengthNoise");
        ValidateNonNegative(f.Reports.HonestyDistortionPerLevel, "friction.reports.honestyDistortionPerLevel");
        ValidateNonNegative(f.Reports.ObserverError, "friction.reports.observerError");
        ValidatePositive(f.Reports.QuerySilenceMinutes, "friction.reports.querySilenceMinutes");
        ValidateNonNegative(f.Reports.ImpossibleReactionPenaltyMinutes, "friction.reports.impossibleReactionPenaltyMinutes");

        ValidateNonNegative(f.Supply.CargoToAmmoCoefficient, "friction.supply.cargoToAmmoCoefficient");
        ValidatePositive(f.Supply.ConsumersPerResupply, "friction.supply.consumersPerResupply");
        ValidateNonNegative(f.Supply.AmmoCeiling, "friction.supply.ammoCeiling");

        ValidatePositive(f.Capture.ConvoySpeedMultiplier, "friction.capture.convoySpeedMultiplier");
        ValidatePositive(f.Capture.ConvoyOffroadMultiplier, "friction.capture.convoyOffroadMultiplier");
        ValidateProbability(f.Capture.EscapeChanceBase, "friction.capture.escapeChanceBase");
        ValidateNonNegative(f.Capture.EscapeChancePerGuardDeficit, "friction.capture.escapeChancePerGuardDeficit");
        ValidateProbability(f.Capture.NoPrisonersCaptureReduction, "friction.capture.noPrisonersCaptureReduction");
        ValidateNonNegative(f.Capture.NoPrisonersLoyaltyPenalty, "friction.capture.noPrisonersLoyaltyPenalty");
        ValidatePositive(f.Capture.InterrogationMinMinutes, "friction.capture.interrogationMinMinutes");
        ValidatePositive(f.Capture.InterrogationMaxMinutes, "friction.capture.interrogationMaxMinutes");
        if (f.Capture.InterrogationMinMinutes > f.Capture.InterrogationMaxMinutes)
            throw new ContentValidationException("friction.capture.interrogationMinMinutes must be <= interrogationMaxMinutes.");

        ValidateNonNegative(f.Civilians.RumorCredibilityMax, "friction.civilians.rumorCredibilityMax");
        ValidateProbability(f.Civilians.FalseReportBase, "friction.civilians.falseReportBase");
        ValidatePositive(f.Civilians.PanicRoadMultiplierBase, "friction.civilians.panicRoadMultiplierBase");
        ValidateNonNegative(f.Civilians.PanicRoadMultiplierPerLevel, "friction.civilians.panicRoadMultiplierPerLevel");
        ValidateNonNegative(f.Civilians.CoercionLoyaltyPenalty, "friction.civilians.coercionLoyaltyPenalty");
        ValidateNonNegative(f.Civilians.ProtectionLoyaltyBonus, "friction.civilians.protectionLoyaltyBonus");
        ValidateNonNegative(f.Civilians.LootingLoyaltyPenalty, "friction.civilians.lootingLoyaltyPenalty");
        ValidateNonNegative(f.Civilians.PanicIncreaseOnCombat, "friction.civilians.panicIncreaseOnCombat");
        foreach (var t in f.Civilians.PanicThresholds)
            ValidateProbability(t, "friction.civilians.panicThresholds");
        foreach (var t in f.Civilians.LoyaltyThresholds)
            ValidateProbability(t, "friction.civilians.loyaltyThresholds");

        ValidateEnumString(f.Llm.FallbackMode, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DoctrineTemplate", "Template", "Deterministic" }, "friction.llm.fallbackMode", allowOther: true);

        foreach (var profile in f.DifficultyProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
                throw new ContentValidationException("friction.difficultyProfiles id is required.");
        }
        EnsureUniqueIds(f.DifficultyProfiles.Select(p => p.Id), "difficulty profile", "friction");
    }

    private static void ValidateMovementMode(FrictionTable.MovementMode mode, string path)
    {
        ValidatePositive(mode.TimeMultiplier, path + ".timeMultiplier");
        ValidateNonNegative(mode.FatigueDelta, path + ".fatigueDelta");
        if (string.IsNullOrWhiteSpace(mode.Position))
            throw new ContentValidationException(path + ".position is required.");
    }

    private static void ValidateReports(ReportTemplates reports)
    {
        if (reports.ByKind.Count == 0)
            throw new ContentValidationException("reports.byKind must contain at least one kind.");
        foreach (var (kind, templates) in reports.ByKind)
        {
            if (string.IsNullOrWhiteSpace(kind))
                throw new ContentValidationException("reports.byKind contains empty kind key.");
            if (templates is null || templates.Count == 0)
                throw new ContentValidationException($"reports.byKind['{kind}'] must contain at least one template.");
            foreach (var t in templates)
            {
                if (string.IsNullOrWhiteSpace(t))
                    throw new ContentValidationException($"reports.byKind['{kind}'] contains empty template.");
            }
        }
    }

    private static HashSet<string> EnsureUniqueIds(IEnumerable<string> ids, string kind, string scope)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ContentValidationException($"{scope}: {kind} id is required.");
            if (!set.Add(id))
                throw new ContentValidationException($"{scope}: duplicate {kind} id '{id}'.");
        }
        return set;
    }

    private static void ValidateEnumString(string? value, HashSet<string> allowed, string path, bool allowOther = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ContentValidationException($"{path} is required.");
        if (!allowed.Contains(value) && !allowOther)
            throw new ContentValidationException($"{path} has invalid value '{value}'.");
    }

    private static void ValidateProbability(double value, string path)
    {
        if (!IsProbability(value))
            throw new ContentValidationException($"{path} must be in [0,1] (actual {FormatDouble(value)}).");
    }

    private static void ValidateNonNegative(double value, string path)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            throw new ContentValidationException($"{path} must be >= 0 (actual {FormatDouble(value)}).");
    }

    private static void ValidatePositive(double value, string path)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            throw new ContentValidationException($"{path} must be > 0 (actual {FormatDouble(value)}).");
    }

    private static void ValidatePositive(int value, string path)
    {
        if (value <= 0)
            throw new ContentValidationException($"{path} must be > 0 (actual {value}).");
    }

    private static bool IsProbability(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 1;

    private static bool AreClose(double a, double b) => Math.Abs(a - b) <= 1e-6;

    private static string FormatDouble(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
}
