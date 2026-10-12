using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class WorldMapPromptTexts
{
	private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
		() => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
			AnimusForgeModulePaths.GetModuleDataFilePath("WorldMapCommandPromptTexts.json"), Encoding.UTF8)));

	internal static string Get(string id) => Values.Value[id];

	internal static string Remaining(int count)
		=> Get("AF_WM_TasksRemaining").Replace("{COUNT}", count.ToString(CultureInfo.InvariantCulture));

	internal static string Summary(string id, int days, string target = "")
		=> Get(id).Replace("{DAYS}", days.ToString(CultureInfo.InvariantCulture)).Replace("{TARGET}", target ?? "");
}
