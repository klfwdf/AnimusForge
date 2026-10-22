using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class HostGameSettingsData : MultiplayerLocalData
{
	public string Scope { get; set; }

	public Dictionary<string, string> Values { get; set; }

	public HostGameSettingsData()
	{
		Values = new Dictionary<string, string>();
	}

	public override bool HasSameContentWith(MultiplayerLocalData other)
	{
		if (other is HostGameSettingsData hostGameSettingsData)
		{
			return Scope == hostGameSettingsData.Scope;
		}
		return false;
	}
}
