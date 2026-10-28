using System.Text;

namespace TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

public struct BannerDebugInfo
{
	public enum SourceTypes
	{
		Undefined,
		Widget,
		Manual
	}

	public SourceTypes SourceType;

	public string SourceName;

	public static BannerDebugInfo CreateManual(string sourceName)
	{
		return new BannerDebugInfo
		{
			SourceName = sourceName,
			SourceType = SourceTypes.Manual
		};
	}

	public static BannerDebugInfo CreateWidget(string sourceName)
	{
		return new BannerDebugInfo
		{
			SourceType = SourceTypes.Widget,
			SourceName = sourceName
		};
	}

	public string CreateName()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("type:");
		stringBuilder.Append(GetSourceTypeName(SourceType));
		stringBuilder.Append("name:");
		stringBuilder.Append(SourceName);
		return stringBuilder.ToString();
	}

	private static string GetSourceTypeName(SourceTypes type)
	{
		return type switch
		{
			SourceTypes.Widget => "Wi", 
			SourceTypes.Manual => "Mn", 
			_ => "Un", 
		};
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append($"type: {SourceType}_");
		stringBuilder.Append("name: " + SourceName + "_");
		return stringBuilder.ToString();
	}
}
