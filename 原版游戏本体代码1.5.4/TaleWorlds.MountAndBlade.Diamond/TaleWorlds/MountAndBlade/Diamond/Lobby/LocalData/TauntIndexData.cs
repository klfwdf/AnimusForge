namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public struct TauntIndexData
{
	public string TauntId { get; set; }

	public int TauntIndex { get; set; }

	public TauntIndexData(string tauntId, int tauntIndex)
	{
		TauntId = tauntId;
		TauntIndex = tauntIndex;
	}

	public override bool Equals(object obj)
	{
		if (obj is TauntIndexData tauntIndexData)
		{
			if (TauntId == tauntIndexData.TauntId)
			{
				return TauntIndex == tauntIndexData.TauntIndex;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (TauntId.GetHashCode() * 397) ^ TauntIndex.GetHashCode();
	}

	public static bool operator ==(TauntIndexData first, TauntIndexData second)
	{
		if (first.TauntId == second.TauntId)
		{
			return first.TauntIndex == second.TauntIndex;
		}
		return false;
	}

	public static bool operator !=(TauntIndexData first, TauntIndexData second)
	{
		return !(first == second);
	}
}
