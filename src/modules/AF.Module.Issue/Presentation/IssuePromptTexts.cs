using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class IssuePromptTexts
{
	private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
		JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
			AnimusForgeModulePaths.GetModuleDataFilePath("IssuePromptTexts.json"), Encoding.UTF8)));
	private static readonly Regex Variable = new Regex(@"\{([A-Z_][A-Z_0-9]*)\}", RegexOptions.Compiled);

	internal static string Text(string id, string fallback)
		=> Values.Value.TryGetValue(id, out string value) ? value : fallback;

	internal static string Get(string id) => Values.Value[id];

	internal static string Format(string id, string fallback, params (string Name, object Value)[] variables)
	{
		string template = Text(id, fallback);
		return Variable.Replace(template, match =>
		{
			foreach (var variable in variables ?? Array.Empty<(string Name, object Value)>())
				if (variable.Name == match.Groups[1].Value) return variable.Value?.ToString() ?? "";
			return match.Value;
		});
	}
}
