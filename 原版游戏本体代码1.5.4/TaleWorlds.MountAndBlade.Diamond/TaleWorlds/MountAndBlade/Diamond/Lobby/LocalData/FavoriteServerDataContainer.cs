using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class FavoriteServerDataContainer : MultiplayerLocalDataContainer<FavoriteServerData>
{
	protected override string GetSaveDirectoryName()
	{
		return "Data";
	}

	protected override string GetSaveFileName()
	{
		return "FavoriteServers.json";
	}

	public bool TryGetServerData(GameServerEntry serverEntry, out FavoriteServerData favoriteServerData)
	{
		favoriteServerData = null;
		MBReadOnlyList<FavoriteServerData> entries = GetEntries();
		for (int i = 0; i < entries.Count; i++)
		{
			FavoriteServerData favoriteServerData2 = entries[i];
			if (favoriteServerData2.HasSameContentWith(serverEntry))
			{
				favoriteServerData = favoriteServerData2;
				return true;
			}
		}
		return false;
	}
}
