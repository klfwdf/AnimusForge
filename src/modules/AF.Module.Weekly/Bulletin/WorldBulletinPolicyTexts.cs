using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

internal static class WorldBulletinPolicyTexts
{
	private static readonly BilingualTextTable RawValues =
		BilingualTextTable.FromEmbedded(typeof(WorldBulletinPolicyTexts).Assembly, "AnimusForge.Weekly.WorldBulletinCompatibility.json");

	private static readonly PromptLanguageFile<Dictionary<string, string>> PromptValues = new PromptLanguageFile<Dictionary<string, string>>("WorldBulletinPromptTexts.json", path =>
		Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(
			path, Encoding.UTF8)));

	internal static string Raw(string key) => RawValues.Get(key);
	/// <summary>Chinese source value, for matching text written before the language switch.</summary>
	internal static string RawSource(string key) => RawValues.Chinese(key);
	internal static string[] RawVariants(string key) => RawValues.Variants(key);
	internal static bool IsRaw(string value, string key) => RawValues.Is(value, key);

	internal static string Prompt(string id) => PromptValues.Value[id];

	internal static string Title(string id, string englishFallback)
		=> new TaleWorlds.Localization.TextObject("{=" + id + "}" + englishFallback).ToString();

	internal static string Prompt(string id, string englishFallback, params (string Name, string Value)[] variables)
	{
		string template = PromptValues.Value.TryGetValue(id, out string text) ? text : englishFallback;
		return variables == null || variables.Length == 0 ? template : Fill(template, variables);
	}

	private static string Fill(string template, (string Name, string Value)[] variables)
	{
		var builder = new StringBuilder(template.Length + 16);
		for (int i = 0; i < template.Length; i++)
		{
			char c = template[i];
			if (c == '{')
			{
				int end = template.IndexOf('}', i + 1);
				if (end > i)
				{
					string name = template.Substring(i + 1, end - i - 1);
					bool matched = false;
					for (int v = 0; v < variables.Length; v++)
					{
						if (string.Equals(variables[v].Name, name, StringComparison.Ordinal))
						{
							builder.Append(variables[v].Value ?? "");
							matched = true;
							break;
						}
					}
					if (matched)
					{
						i = end;
						continue;
					}
				}
			}
			builder.Append(c);
		}
		return builder.ToString();
	}
}
