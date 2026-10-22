using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class HostGameSettingsDataContainer : MultiplayerLocalDataContainer<HostGameSettingsData>
{
	protected override string GetSaveDirectoryName()
	{
		return "Data";
	}

	protected override string GetSaveFileName()
	{
		return "HostGameSettings.json";
	}

	public bool TryGetSettings(string scope, out HostGameSettingsData settingsData)
	{
		MBReadOnlyList<HostGameSettingsData> entries = GetEntries();
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].Scope == scope)
			{
				settingsData = entries[i];
				return true;
			}
		}
		settingsData = null;
		return false;
	}

	public void SaveSettings(HostGameSettingsData settingsData)
	{
		if (TryGetSettings(settingsData.Scope, out var settingsData2))
		{
			RemoveEntry(settingsData2);
		}
		AddEntry(settingsData);
	}
}
