using BUTR.CrashReport.Models;

namespace BUTR.CrashReport.Bannerlord;

internal class LoaderPluginInfo : ILoaderPluginInfo
{
	public string Id { get; set; } = string.Empty;

	public string? Version { get; set; }

	public string? UpdateInfo { get; set; }
}
