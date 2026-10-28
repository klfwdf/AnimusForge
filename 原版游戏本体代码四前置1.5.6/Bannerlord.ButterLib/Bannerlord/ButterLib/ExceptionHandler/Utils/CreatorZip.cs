using System.Collections.Generic;
using System.IO;
using System.Text;
using BUTR.CrashReport.Models;
using BUTR.CrashReport.Renderer.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Bannerlord.ButterLib.ExceptionHandler.Utils;

internal static class CreatorZip
{
	public static void Create(CrashReportModel crashReport, ICollection<LogSource> logSources, Stream stream)
	{
		JsonSerializerSettings settings = new JsonSerializerSettings
		{
			Formatting = Formatting.None,
			StringEscapeHandling = StringEscapeHandling.EscapeHtml,
			DefaultValueHandling = DefaultValueHandling.Include,
			NullValueHandling = NullValueHandling.Include,
			Converters = { (JsonConverter)new StringEnumConverter() }
		};
		using MemoryStream crashReportJson = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(crashReport, settings)));
		using MemoryStream logsJson = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(logSources, settings)));
		using Stream miniDump = CreatorShared.GetMiniDump();
		using Stream saveFile = CreatorShared.GetSaveFile();
		using Stream screenshot = CreatorShared.GetScreenshot();
		using Stream stream2 = CrashReportZip.Build(crashReportJson, logsJson, miniDump, saveFile, screenshot);
		stream2.Seek(0L, SeekOrigin.Begin);
		stream2.CopyTo(stream);
	}
}
