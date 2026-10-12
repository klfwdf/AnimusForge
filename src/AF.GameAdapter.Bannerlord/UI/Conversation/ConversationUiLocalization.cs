using TaleWorlds.Localization;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AnimusForge;

/// <summary>
/// Player-visible text owned by the Conversation channel. Keep dynamic input and
/// letter bodies outside this catalog; they are user or model content.
/// </summary>
internal static class ConversationUiLocalization
{
    private static readonly Regex LiteralValues = new Regex(@"AFCONVUILITERAL([0-9]+)END", RegexOptions.CultureInvariant);
	internal static TextObject Object(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback);
	}

	internal static TextObject Object(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
		TextObject text = Object(id, englishFallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
			{
				if (value is TextObject variableText)
					text.SetTextVariable(name, variableText);
				else
					text.SetTextVariable(name, LlmRetryPrompt.ProjectKnownErrorForUi(value?.ToString()) ?? string.Empty);
			}
		}
		return text;
	}

	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Text(string id, string englishFallback, string variableName, string variableValue)
	{
        return Text(id, englishFallback, (variableName, (object)variableValue));
	}

	internal static string Text(string id, string englishFallback, params (string Name, object Value)[] variables)
	{
        TextObject text = Object(id, englishFallback);
        if (variables != null)
            for (int i = 0; i < variables.Length; i++)
                if (variables[i].Value is TextObject variableText)
                    text.SetTextVariable(variables[i].Name, variableText);
                else
                    text.SetTextVariable(variables[i].Name, "AFCONVUILITERAL" + i.ToString(CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match =>
            LlmRetryPrompt.ProjectKnownErrorForUi(variables[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)].Value?.ToString()) ?? string.Empty);
	}
}
