using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

internal static class WorldBulletinPolicyTexts
{
	private static readonly Lazy<Dictionary<string, string>> RawValues = new Lazy<Dictionary<string, string>>(() =>
	{
		using (var stream = typeof(WorldBulletinPolicyTexts).Assembly.GetManifestResourceStream("AnimusForge.Weekly.WorldBulletinCompatibility.json"))
		using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
			return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
	});

	private static readonly Lazy<Dictionary<string, string>> PromptValues = new Lazy<Dictionary<string, string>>(() =>
		Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(
			AnimusForgeModulePaths.GetModuleDataFilePath("WorldBulletinPromptTexts.json"), Encoding.UTF8)));

	internal static string Raw(string key) => RawValues.Value[key];

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
