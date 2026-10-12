using System;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Explicit developer imports only. Validate the wire shape before defaults can turn
// an unrelated JSON object into an empty bundle that overwrites existing memory.
internal static class CompressedMemoryExportBundleReader
{
    internal static string ForDisplay(string error)
    {
        if (error == MemoryTextResources.Raw("ExportInvalid"))
            return MemoryFailureUiText.T("AF_MEMORY_UI_ExportInvalid", "Invalid compressed-memory file. Choose JSON exported from Compressed Memory.").Resolve();
        if (error == MemoryTextResources.Raw("ExportPersona"))
            return MemoryFailureUiText.T("AF_MEMORY_UI_ExportPersona", "This is a personality/background file, not compressed memory. Import it under Single Hero NPC > Personality/background. Existing memories were not changed.").Resolve();
        if (error == MemoryTextResources.Raw("ExportNoFields"))
            return MemoryFailureUiText.T("AF_MEMORY_UI_ExportNoFields", "The file has no compressed-memory fields. Choose JSON exported from Compressed Memory; existing memories were not changed.").Resolve();
        string prefix = MemoryTextResources.Raw("ExportFieldPrefix");
        string suffix = MemoryTextResources.Raw("ExportFieldSuffix");
        if (error != null && error.StartsWith(prefix, StringComparison.Ordinal) && error.EndsWith(suffix, StringComparison.Ordinal))
            return MemoryFailureUiText.T("AF_MEMORY_UI_ExportFieldType", "Compressed-memory field '{FIELD}' has the wrong type. Choose a valid compressed-memory export.",
                ("FIELD", error.Substring(prefix.Length, error.Length - prefix.Length - suffix.Length))).Resolve();
        return error;
    }

    internal static bool TryRead(string path, out CompressedMemoryExportBundle bundle, out string error)
    {
        bundle = null;
        error = MemoryTextResources.Raw("ExportInvalid");
        JObject document = PlayerExportsStore.ReadJson<JObject>(path);
        if (document == null) return false;

        bool hasMemoryField = false;
        bool hasPersonaField = false;
        foreach (JProperty property in document.Properties())
        {
            string name = property.Name;
            bool isOverview = string.Equals(name, "Overview", StringComparison.OrdinalIgnoreCase);
            bool isList = string.Equals(name, "DailyDrafts", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Blocks", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "SummaryQueue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "OverviewQueue", StringComparison.OrdinalIgnoreCase);
            hasPersonaField |= string.Equals(name, "Personality", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Background", StringComparison.OrdinalIgnoreCase);
            if (!isOverview && !isList) continue;
            hasMemoryField = true;
            JTokenType type = property.Value.Type;
            if (type != JTokenType.Null && type != (isOverview ? JTokenType.Object : JTokenType.Array))
            {
                error = MemoryTextResources.Raw("ExportFieldPrefix") + name + MemoryTextResources.Raw("ExportFieldSuffix");
                return false;
            }
        }
        if (!hasMemoryField)
        {
            error = hasPersonaField
                ? MemoryTextResources.Raw("ExportPersona")
                : MemoryTextResources.Raw("ExportNoFields");
            return false;
        }
        try
        {
            CompressedMemoryExportBundle parsed = document.ToObject<CompressedMemoryExportBundle>();
            if (parsed == null) return false;
            bundle = parsed;
            error = "";
            return true;
        }
        catch
        {
            return false;
        }
    }
}
