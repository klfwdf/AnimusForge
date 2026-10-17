using System;
using System.IO;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Player-facing onboarding and API setup copy. Protocol payloads remain in their original source.</summary>
internal static class OnboardingUiText
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    internal static string T(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    // Only known module assets receive display labels. Paths and user folder names stay literal.
    // Called when opening the picker; the compatibility reader loads once, UI labels resolve per call.
    internal static string ImportFolderDisplayName(string sourceLabel, string name)
    {
        if (!string.Equals(sourceLabel, "Module directory", StringComparison.Ordinal)) return name;
        if (string.Equals(name, AdapterPromptCaptureTexts.PcapCompat("Compat_BuiltinWorldbook_Calradia"), StringComparison.Ordinal))
            return T("AF_ONBOARDING_BUILTIN_BOOK_CALRADIA", "Calradia Chronicles");
        if (string.Equals(name, AdapterPromptCaptureTexts.PcapCompat("Compat_BuiltinWorldbook_CalradiaLite"), StringComparison.Ordinal))
            return T("AF_ONBOARDING_BUILTIN_BOOK_CALRADIALITE", "Calradia Chronicles (Lite Edition) (choose this for a lower-performance CPU, such as the i5-12400F)");
        if (string.Equals(name, AdapterPromptCaptureTexts.PcapCompat("Compat_BuiltinWorldbook_Warhammer"), StringComparison.Ordinal))
            return T("AF_ONBOARDING_BUILTIN_BOOK_WARHAMMER", "Warhammer Old World Chronicles");
        if (string.Equals(name, AdapterPromptCaptureTexts.PcapCompat("Compat_BuiltinWorldbook_GameOfThrones"), StringComparison.Ordinal))
            return T("AF_ONBOARDING_BUILTIN_BOOK_GAMEOFTHRONES", "Game of Thrones Chronicles");
        return name;
    }

    internal static string ImportPathDisplayName(string path)
    {
        try
        {
            string full = Path.GetFullPath(path ?? "").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string root = Path.GetFullPath(PlayerExportsStore.GetPlayerExportsRootPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(Path.GetDirectoryName(full), root, StringComparison.OrdinalIgnoreCase)) return path;
            string name = Path.GetFileName(full);
            string display = ImportFolderDisplayName("Module directory", name);
            return display == name ? path : display;
        }
        catch (ArgumentException) { return path; }
        catch (IOException) { return path; }
        catch (UnauthorizedAccessException) { return path; }
    }

    internal static string F(string id, string englishFallback, params string[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
            for (int i = 0; i + 1 < variables.Length; i += 2)
                text.SetTextVariable(variables[i], "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) + 1] ?? string.Empty);
    }
}
