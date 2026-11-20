using System.Globalization;
using System.Text;

namespace Kriegsspiel.Core.Content;

public sealed class ReportTemplates
{
    public Dictionary<string, List<string>> ByKind { get; init; } = new();
    public Dictionary<string, List<string>> Placeholders { get; init; } = new();

    public string Pick(string kind, IReadOnlyDictionary<string, string> vars, Random rng)
    {
        if (!ByKind.TryGetValue(kind, out var list) || list.Count == 0)
            return "[нет шаблона]";
        var t = list[rng.Next(list.Count)];
        var merged = MergeWithDefaults(kind, vars);
        foreach (var (k, v) in merged)
            t = t.Replace("{" + k + "}", v);
        return t;
    }

    public IReadOnlyDictionary<string, string> MergeWithDefaults(string kind, IReadOnlyDictionary<string, string> vars)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["eventMinute"] = "",
            ["eventTime"] = "",
            ["deliveryMinute"] = "",
            ["deliveryTime"] = "",
            ["sourceId"] = "",
            ["source"] = "",
            ["confidence"] = "",
            ["kind"] = kind,
            ["zoneId"] = "",
            ["zone"] = "",
            ["unitId"] = "",
            ["unit"] = "",
            ["targetUnitId"] = "",
            ["targetUnit"] = "",
            ["strength"] = "",
            ["losses"] = "",
            ["morale"] = "",
            ["ammo"] = "",
            ["readiness"] = "",
            ["fatigue"] = "",
            ["factKey"] = "",
            ["status"] = "",
            ["number"] = "",
            ["count"] = ""
        };

        if (Placeholders.TryGetValue(kind, out var names))
        {
            foreach (var name in names)
            {
                if (!result.ContainsKey(name))
                    result[name] = "";
            }
        }

        foreach (var (k, v) in vars)
            result[k] = v;

        if (string.IsNullOrEmpty(result["eventTime"]) && int.TryParse(result["eventMinute"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var em))
            result["eventTime"] = FormatTime(em);
        if (string.IsNullOrEmpty(result["deliveryTime"]) && int.TryParse(result["deliveryMinute"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dm))
            result["deliveryTime"] = FormatTime(dm);
        if (string.IsNullOrEmpty(result["source"]) && !string.IsNullOrEmpty(result["sourceId"]))
            result["source"] = result["sourceId"];
        if (string.IsNullOrEmpty(result["zone"]) && !string.IsNullOrEmpty(result["zoneId"]))
            result["zone"] = result["zoneId"];
        if (string.IsNullOrEmpty(result["unit"]) && !string.IsNullOrEmpty(result["unitId"]))
            result["unit"] = result["unitId"];

        return result;
    }

    public static string FormatTime(int minute)
    {
        var h = minute / 60;
        var m = minute % 60;
        return $"{h:00}:{m:00}";
    }
}