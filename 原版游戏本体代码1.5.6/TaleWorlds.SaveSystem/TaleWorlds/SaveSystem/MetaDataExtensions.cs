using TaleWorlds.Library;

namespace TaleWorlds.SaveSystem;

public static class MetaDataExtensions
{
	public static ApplicationVersion GetApplicationVersion(this MetaData metaData)
	{
		string text = metaData?["ApplicationVersion"];
		if (text == null)
		{
			return ApplicationVersion.Empty;
		}
		return ApplicationVersion.FromString(text);
	}

	public static ApplicationVersion GetNewGameVersion(this MetaData metaData)
	{
		string text = metaData?["NewGameVersion"];
		if (string.IsNullOrEmpty(text))
		{
			return ApplicationVersion.Empty;
		}
		return ApplicationVersion.FromString(text);
	}
}
