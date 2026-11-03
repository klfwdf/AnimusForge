using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class PolicyCompatibilityTexts
{
    private static readonly BilingualTextTable Table = BilingualTextTable.FromModuleData("PolicyCompatibilityTexts.json");

    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);

    // Glue punctuation for text composed now; it follows the current prompt language.
    internal static string Colon => PromptLanguage.IsChinese ? "\uFF1A" : ": ";
    internal static string Comma => PromptLanguage.IsChinese ? "\uFF0C" : ", ";
    internal static string Semicolon => PromptLanguage.IsChinese ? "\uFF1B" : "; ";
    internal static string ListSeparator => PromptLanguage.IsChinese ? "\u3001" : ", ";
    internal static string Period => PromptLanguage.IsChinese ? "\u3002" : ".";
    internal static string FieldSeparator => PromptLanguage.IsChinese ? "\uFF5C" : " | ";
    internal static string OpenParen => PromptLanguage.IsChinese ? "\uFF08" : " (";
    internal static string CloseParen => PromptLanguage.IsChinese ? "\uFF09" : ")";
    internal static string OpenBracket => PromptLanguage.IsChinese ? "\u3010" : "[";
    internal static string CloseBracket => PromptLanguage.IsChinese ? "\u3011" : "]";
    internal static string OpenTitleQuote => PromptLanguage.IsChinese ? "\u300A" : "\"";
    internal static string CloseTitleQuote => PromptLanguage.IsChinese ? "\u300B" : "\"";
    internal static char PeriodChar => PromptLanguage.IsChinese ? '\u3002' : '.';
    /// <summary>Space between two concatenated fragments in English; Chinese needs none.</summary>
    internal static string WordGap => PromptLanguage.IsChinese ? string.Empty : " ";
    /// <summary>Clause separator between two concatenated fragments in English; Chinese needs none.</summary>
    internal static string ClauseGap => PromptLanguage.IsChinese ? string.Empty : ", ";
}
