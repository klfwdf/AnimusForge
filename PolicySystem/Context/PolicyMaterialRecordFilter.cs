using System;
using System.IO;
using Newtonsoft.Json;

namespace AnimusForge;

// Request-boundary filtering of existing raw records. No new policy index/cache:
// inspect top-level metadata before constructing or normalizing the policy body.
internal static class PolicyMaterialRecordFilter
{
    internal static string Field(string raw, string name)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var reader = new JsonTextReader(new StringReader(raw)) { DateParseHandling = DateParseHandling.None };
            while (reader.Read())
            {
                if (reader.TokenType != JsonToken.PropertyName || reader.Depth != 1) continue;
                bool match = string.Equals((string)reader.Value, name, StringComparison.OrdinalIgnoreCase);
                if (!reader.Read()) return null;
                if (match) return Convert.ToString(reader.Value, System.Globalization.CultureInfo.InvariantCulture);
                reader.Skip();
            }
        }
        catch (JsonException) { /* Invalid source remains owned by the policy loader. */ }
        return null;
    }

    internal static bool Since(string raw, string dayField, int minimumDay)
        => minimumDay <= 0 || int.TryParse(Field(raw, dayField), out int day) && day >= minimumDay;

    internal static bool HasRecord(string raw, System.Collections.Generic.ISet<string> ids)
    {
        if (ids == null) return true;
        string id = Field(raw, "RecordId");
        return ids.Contains(string.IsNullOrWhiteSpace(id) ? Field(raw, "PolicyObjectId") ?? "" : id);
    }
}
