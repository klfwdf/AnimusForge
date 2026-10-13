using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal static class UnnamedPersonaImportValidationOwner
{
    // Disk work is limited to the explicit import boundary. Empty catalog files contain no NPC.
    internal static Dictionary<string, JObject> ReadProfiles(string importDir)
    {
        if (!Directory.Exists(importDir)) throw new DirectoryNotFoundException(importDir);
        string child = Path.Combine(importDir, "unnamed_persona");
        string directory = Directory.Exists(child) ? child : importDir;
        var profiles = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.GetFiles(directory, "*.json"))
        {
            string json = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(json)) continue;
            JObject record = JObject.Parse(json);
            JToken catalog = record["Profiles"] ?? record["profiles"];
            if (catalog != null)
            {
                if (!(catalog is JObject entries)) throw new InvalidDataException("Invalid unnamed persona Profiles object: " + Path.GetFileName(file));
                foreach (JProperty entry in entries.Properties())
                {
                    if (!(entry.Value is JObject profile)) throw new InvalidDataException("Invalid unnamed persona profile: " + entry.Name);
                    AddProfile(profiles, entry.Name, profile);
                }
            }
            else
            {
                string key = TryGetUnnamedPersonaKeyFromImportFile(file, _ => (record["Key"] ?? record["key"])?.ToString());
                AddProfile(profiles, key, record);
            }
        }
        return profiles;
    }

    private static void AddProfile(Dictionary<string, JObject> profiles, string key, JObject profile)
    {
        bool hasContent = new[] { "Description", "description", "Personality", "personality", "Background", "background" }
            .Any(field => !string.IsNullOrWhiteSpace(profile[field]?.ToString()));
        if (!hasContent) return;
        key = (key ?? "").Trim().ToLowerInvariant();
        if (key.Length == 0) throw new InvalidDataException("Unnamed persona profile has no key.");
        if (profiles.ContainsKey(key)) throw new InvalidDataException("Duplicate unnamed persona key: " + key);
        profiles.Add(key, profile);
    }
	internal static string TryGetUnnamedPersonaKeyFromImportFile(string file, Func<string, string> readKey)
	{
		string text = null;
		try
		{
			text = (readKey(file) ?? "").Trim().ToLower();
		}
		catch
		{
			text = null;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				string text2 = Path.GetFileNameWithoutExtension(file) ?? "";
				int num = text2.LastIndexOf("__", StringComparison.Ordinal);
				if (num > 0)
				{
					text2 = text2.Substring(0, num);
				}
				text = (text2 ?? "").Trim().ToLower();
			}
			catch
			{
				text = null;
			}
		}
		return (text ?? "").Trim().ToLower();
	}

	internal static bool ValidateUnnamedPersonaKeysForImport(string importDir, Func<string, string> readKey, Func<string, bool> hasExistingKey, out string error)
	{
		error = "";
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				error = PersonaUiTexts.Text("AF_PERSONA_UI_ImportMissingDirectory", "Import failed: the export directory could not be found.");
				return false;
			}
            foreach (string key in ReadProfiles(importDir).Keys)
            {
                if (hasExistingKey(key))
                {
                    error = PersonaUiTexts.Format("AF_PERSONA_UI_ImportExistingKeyConflict", "Import failed: Key conflict ({KEY}). The current game already contains this Key; import cannot overwrite it.", ("KEY", key));
                    return false;
                }
            }
			return true;
		}
		catch (Exception ex)
		{
			error = PersonaUiTexts.Format("AF_PERSONA_UI_ImportValidationException", "Import failed: Key validation exception: {ERROR}", ("ERROR", ex.Message));
			return false;
		}
	}
}
