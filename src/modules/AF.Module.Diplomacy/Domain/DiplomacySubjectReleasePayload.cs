using System;

namespace AnimusForge;

internal static class DiplomacySubjectReleasePayload
{
    internal static bool TryParse(string payload, out string target, out string agreement)
    {
        target = agreement = "";
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > 600) return false;
        string[] fields = payload.Split(';');
        if (fields.Length != 2) return false;
        foreach (string field in fields)
        {
            int separator = field.IndexOf('=');
            if (separator <= 0 || separator == field.Length - 1) return false;
            string key = field.Substring(0, separator).Trim();
            string value = field.Substring(separator + 1).Trim();
            if (value.Length == 0 || value.IndexOf('=') >= 0) return false;
            if (key.Equals("target", StringComparison.OrdinalIgnoreCase) && target.Length == 0) target = value;
            else if (key.Equals("agreement", StringComparison.OrdinalIgnoreCase) && agreement.Length == 0) agreement = value;
            else return false;
        }
        return target.Length > 0 && agreement.Length > 0;
    }
}
