namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class FavoriteServerData : MultiplayerLocalData
{
	public string Address { get; set; }

	public int Port { get; set; }

	public string GameType { get; set; }

	public string Name { get; set; }

	private FavoriteServerData()
	{
	}

	public static FavoriteServerData CreateFrom(GameServerEntry serverEntry)
	{
		if (serverEntry == null)
		{
			return null;
		}
		return new FavoriteServerData
		{
			Address = serverEntry.Address,
			Port = serverEntry.Port,
			GameType = serverEntry.GameType,
			Name = serverEntry.ServerName
		};
	}

	public override bool HasSameContentWith(MultiplayerLocalData other)
	{
		if (other is FavoriteServerData favoriteServerData)
		{
			if (Address == favoriteServerData.Address && Port == favoriteServerData.Port && GameType == favoriteServerData.GameType)
			{
				return Name == favoriteServerData.Name;
			}
			return false;
		}
		return false;
	}

	public bool HasSameContentWith(GameServerEntry serverEntry)
	{
		if (Address == serverEntry.Address && Port == serverEntry.Port && GameType == serverEntry.GameType)
		{
			return Name == serverEntry.ServerName;
		}
		return false;
	}
}
