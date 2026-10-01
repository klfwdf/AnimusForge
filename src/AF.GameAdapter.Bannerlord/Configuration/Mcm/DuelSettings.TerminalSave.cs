using System;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class DuelSettings
{
	public static bool SaveCurrentSettings()
	{
		try
		{
			DuelSettings settings = GetSettings();
			if (settings != null && BaseSettingsProvider.Instance != null)
			{
				BaseSettingsProvider.Instance.SaveSettings(settings);
				return true;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Terminal", "[ERROR] Failed to save DuelSettings: " + ex);
		}
		return false;
	}
}
