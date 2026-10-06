namespace TaleWorlds.Library;

public static class ManagedDllFolder
{
	private static string _overridenFolder;

	public static string Name
	{
		get
		{
			if (!string.IsNullOrEmpty(_overridenFolder))
			{
				return _overridenFolder;
			}
			if (ApplicationPlatform.CurrentPlatform == Platform.Orbis)
			{
				return "/app0/";
			}
			if (ApplicationPlatform.CurrentPlatform == Platform.Durango)
			{
				return "/";
			}
			return "";
		}
	}

	public static void OverrideManagedDllFolder(string overridenFolder)
	{
		_overridenFolder = overridenFolder;
	}
}
