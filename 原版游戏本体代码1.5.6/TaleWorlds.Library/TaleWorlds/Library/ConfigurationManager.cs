namespace TaleWorlds.Library;

public static class ConfigurationManager
{
	private static IConfigurationManager _configurationManager;

	public static void SetConfigurationManager(IConfigurationManager configurationManager)
	{
		_configurationManager = configurationManager;
	}

	public static string GetAppSettings(string name)
	{
		if (_configurationManager != null)
		{
			return _configurationManager.GetAppSettings(name);
		}
		return null;
	}
}
