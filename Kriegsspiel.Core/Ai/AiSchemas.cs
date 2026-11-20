using System.Text.Json;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Ai;

public sealed record AiOrderDraft(
    string ToUnitId,
    OrderIntent Intent,
    string? TargetZoneId,
    string? TargetObjectId,
    string? TargetUnitId,
    int NotLaterThan,
    int ReportBy,
    MovementMode MovementMode,
    IReadOnlyList<OrderConstraint> Constraints,
    IReadOnlyList<OrderBranch> Branches,
    string OriginalText)
{
    public Order ToOrder(string issuerId, int issuedAt, int seed)
    {
        var probe = new Order
        {
            ToUnitId = ToUnitId,
            Intent = Intent,
            TargetZoneId = TargetZoneId,
            NotLaterThan = NotLaterThan,
            ReportBy = ReportBy,
            Constraints = Constraints.ToList()
        };

        return new Order
        {
            Id = AiStableId.Create("order", seed, issuedAt, issuerId, ToUnitId, OriginalText),
            ToUnitId = ToUnitId,
            Intent = Intent,
            TargetZoneId = TargetZoneId,
            TargetObjectId = TargetObjectId,
            TargetUnitId = TargetUnitId,
            NotLaterThan = NotLaterThan,
            ReportBy = ReportBy,
            IssuedAt = issuedAt,
            IssuerId = issuerId,
            AmbiguityScore = AmbiguityScorer.Score(probe),
            OrderVersion = 1,
            MovementMode = MovementMode,
            Constraints = Constraints.ToList(),
            Branches = Branches.ToList(),
            OriginalText = OriginalText
        };
    }
}

public static class AiSchemas
{
    public const string Version = "1.0";

    public const string OrderInstructionText =
        "Ты автор приказов вражеского штаба. Верни только JSON по схеме. " +
        "Используй только ID юнитов и зон из контекста, перечисления строго из схемы. " +
        "Не знай истинное состояние игрока и не используй скрытые поля.";

    public const string NarrationInstructionText =
        "Только переформулируй уже вычисленные структурированные факты. " +
        "Верни JSON с полями schemaVersion и text. Не добавляй числа, время, источники, уверенность или новые факты.";

    public const string OrderSchemaText = """
        {
          "schemaVersion": "1.0",
          "type": "object",
          "additionalProperties": false,
          "required": ["schemaVersion", "toUnitId", "intent", "targetZoneId", "targetObjectId", "targetUnitId", "notLaterThan", "reportBy", "movementMode", "constraints", "branches", "originalText"],
          "properties": {
            "schemaVersion": { "const": "1.0" },
            "toUnitId": { "type": "string" },
            "intent": { "enum": ["Seize", "Hold", "Move", "Recon", "Support", "Withdraw", "Reserve", "Occupy", "Advance", "Delay", "Escort", "Destroy", "Block", "Check", "Guard"] },
            "targetZoneId": { "type": ["string", "null"] },
            "targetObjectId": { "type": ["string", "null"] },
            "targetUnitId": { "type": ["string", "null"] },
            "notLaterThan": { "type": "integer" },
            "reportBy": { "type": "integer" },
            "movementMode": { "enum": ["March", "Covert", "Contact"] },
            "constraints": { "type": "array", "maxItems": 5, "items": { "enum": ["NoZoneEntry", "NoEngagement", "NoRetreatBeyondZone", "NoAmmoUse", "NoWeaponDisplay"] } },
            "branches": { "type": "array", "maxItems": 3, "items": { "type": "object", "additionalProperties": false } },
            "originalText": { "type": "string" }
          }
        }
        """;

    public const string NarrationSchemaText = """
        {
          "schemaVersion": "1.0",
          "type": "object",
          "additionalProperties": false,
          "required": ["schemaVersion", "text"],
          "properties": {
            "schemaVersion": { "const": "1.0" },
            "text": { "type": "string" }
          }
        }
        """;

    private static readonly string[] OrderProperties =
    {
        "schemaVersion", "toUnitId", "intent", "targetZoneId", "targetObjectId", "targetUnitId",
        "notLaterThan", "reportBy", "movementMode", "constraints", "branches", "originalText"
    };

    private static readonly string[] BranchProperties =
    {
        "condition", "then", "else", "droppedConstraintName"
    };

    private static readonly string[] NarrationProperties = { "schemaVersion", "text" };

    public static AiValidation<AiOrderDraft> ValidateOrder(string json, AiWorldView view, string? schemaVersion = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(view);

        using var document = Parse(json, OrderProperties, out var error);
        if (document is null) return AiValidation<AiOrderDraft>.Fail(error);
        var root = document.RootElement;
        var expectedVersion = schemaVersion ?? Version;

        if (!TryString(root, "schemaVersion", out var version) || version != expectedVersion)
            return AiValidation<AiOrderDraft>.Fail("schemaVersion mismatch");
        if (!TryString(root, "toUnitId", out var toUnitId) || !view.Units.Any(u => u.Id == toUnitId))
            return AiValidation<AiOrderDraft>.Fail("unknown toUnitId");
        if (!TryString(root, "intent", out var intentText) || !TryParseEnum(intentText, out OrderIntent intent))
            return AiValidation<AiOrderDraft>.Fail("invalid intent");
        if (!TryNullableString(root, "targetZoneId", out var targetZoneId))
            return AiValidation<AiOrderDraft>.Fail("invalid targetZoneId");
        if (targetZoneId is not null &&
            (targetZoneId.Length == 0 || targetZoneId.Length > 64 || !view.Zones.Any(z => z.Id == targetZoneId)))
            return AiValidation<AiOrderDraft>.Fail("targetZoneId is outside allowed knowledge");
        if (!TryNullableString(root, "targetObjectId", out var targetObjectId) || !IsValidOptionalId(targetObjectId))
            return AiValidation<AiOrderDraft>.Fail("invalid targetObjectId");
        if (!TryNullableString(root, "targetUnitId", out var targetUnitId) || !IsValidOptionalId(targetUnitId))
            return AiValidation<AiOrderDraft>.Fail("invalid targetUnitId");
        if (!TryInt(root, "notLaterThan", out var notLaterThan) || !TryInt(root, "reportBy", out var reportBy))
            return AiValidation<AiOrderDraft>.Fail("invalid deadlines");
        if (notLaterThan < view.Minute || notLaterThan > view.Minute + 1440 ||
            reportBy < notLaterThan || reportBy > view.Minute + 1440)
            return AiValidation<AiOrderDraft>.Fail("deadlines outside window");
        if (!TryString(root, "movementMode", out var movementText) || !TryParseEnum(movementText, out MovementMode movementMode))
            return AiValidation<AiOrderDraft>.Fail("invalid movementMode");

        if (!root.TryGetProperty("constraints", out var constraintsElement) || constraintsElement.ValueKind != JsonValueKind.Array)
            return AiValidation<AiOrderDraft>.Fail("invalid constraints");
        var constraints = new List<OrderConstraint>();
        foreach (var item in constraintsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                !TryParseEnum(item.GetString() ?? "", out OrderConstraint constraint) ||
                constraints.Contains(constraint))
                return AiValidation<AiOrderDraft>.Fail("invalid constraint");
            constraints.Add(constraint);
        }
        if (constraints.Count > 5)
            return AiValidation<AiOrderDraft>.Fail("too many constraints");

        if (!root.TryGetProperty("branches", out var branchesElement) || branchesElement.ValueKind != JsonValueKind.Array)
            return AiValidation<AiOrderDraft>.Fail("invalid branches");
        var branches = new List<OrderBranch>();
        foreach (var item in branchesElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !HasOnlyProperties(item, BranchProperties, out error))
                return AiValidation<AiOrderDraft>.Fail(error);
            if (!TryString(item, "condition", out var conditionText) || !TryParseEnum(conditionText, out BranchCondition condition))
                return AiValidation<AiOrderDraft>.Fail("invalid branch condition");
            if (!TryString(item, "then", out var thenText) || !TryParseEnum(thenText, out BranchAction then))
                return AiValidation<AiOrderDraft>.Fail("invalid branch action");
            if (!TryNullableString(item, "else", out var elseText) || !TryNullableString(item, "droppedConstraintName", out var droppedConstraintName))
                return AiValidation<AiOrderDraft>.Fail("invalid branch slots");
            if (!IsValidOptionalId(droppedConstraintName))
                return AiValidation<AiOrderDraft>.Fail("invalid branch constraint name");
            BranchAction? elseAction = null;
            if (elseText is not null)
            {
                if (!TryParseEnum(elseText, out BranchAction parsedElse))
                    return AiValidation<AiOrderDraft>.Fail("invalid branch else action");
                elseAction = parsedElse;
            }
            branches.Add(new OrderBranch(condition, then, elseAction, droppedConstraintName));
        }
        if (branches.Count > 3)
            return AiValidation<AiOrderDraft>.Fail("too many branches");

        if (!TryString(root, "originalText", out var originalText) || originalText.Length is 0 or > 500)
            return AiValidation<AiOrderDraft>.Fail("invalid originalText");

        return AiValidation<AiOrderDraft>.Ok(new AiOrderDraft(
            toUnitId,
            intent,
            targetZoneId,
            targetObjectId,
            targetUnitId,
            notLaterThan,
            reportBy,
            movementMode,
            constraints,
            branches,
            originalText));
    }

    public static AiValidation<string> ValidateNarration(string json, Report fact, string? schemaVersion = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(fact);

        using var document = Parse(json, NarrationProperties, out var error);
        if (document is null) return AiValidation<string>.Fail(error);
        var root = document.RootElement;
        var expectedVersion = schemaVersion ?? Version;

        if (!TryString(root, "schemaVersion", out var version) || version != expectedVersion)
            return AiValidation<string>.Fail("schemaVersion mismatch");
        if (!TryString(root, "text", out var text) || !AiNarrationGuard.IsAllowedText(text, fact))
            return AiValidation<string>.Fail("invalid narration text");

        return AiValidation<string>.Ok(text);
    }

    private static JsonDocument? Parse(string json, string[] allowed, out string error)
    {
        error = "";
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 16
            });
        }
        catch (JsonException)
        {
            error = "invalid json";
            return null;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            error = "root must be an object";
            document.Dispose();
            return null;
        }
        if (!HasOnlyProperties(document.RootElement, allowed, out error))
        {
            document.Dispose();
            return null;
        }
        return document;
    }

    private static bool HasOnlyProperties(JsonElement obj, string[] allowed, out string error)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in obj.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                error = "duplicate property: " + property.Name;
                return false;
            }
            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
            {
                error = "unknown property: " + property.Name;
                return false;
            }
        }
        foreach (var name in allowed)
        {
            if (!seen.Contains(name))
            {
                error = "missing property: " + name;
                return false;
            }
        }
        error = "";
        return true;
    }

    private static bool TryString(JsonElement obj, string name, out string value)
    {
        value = "";
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.String)
            return false;
        value = element.GetString() ?? "";
        return true;
    }

    private static bool TryNullableString(JsonElement obj, string name, out string? value)
    {
        value = null;
        if (!obj.TryGetProperty(name, out var element))
            return false;
        if (element.ValueKind == JsonValueKind.Null)
            return true;
        if (element.ValueKind != JsonValueKind.String)
            return false;
        value = element.GetString();
        return value is not null;
    }

    private static bool TryInt(JsonElement obj, string name, out int value)
    {
        value = 0;
        return obj.TryGetProperty(name, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt32(out value);
    }

    private static bool TryParseEnum<T>(string value, out T result) where T : struct, Enum =>
        Enum.TryParse(value, false, out result) && Enum.IsDefined(result);

    private static bool IsValidOptionalId(string? value) =>
        value is null || (value.Length > 0 && value.Length <= 64);
}

public static class AiNarrationGuard
{
    public static bool IsAllowedText(string text, Report fact)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 400)
            return false;
        if (text.Any(char.IsDigit))
            return false;
        if (text.Contains("уверен", StringComparison.OrdinalIgnoreCase))
            return false;
        return !text.Contains(fact.SourceId, StringComparison.Ordinal);
    }
}
