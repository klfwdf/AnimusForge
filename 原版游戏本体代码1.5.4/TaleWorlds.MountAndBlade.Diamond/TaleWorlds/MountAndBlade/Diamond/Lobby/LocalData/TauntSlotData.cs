using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class TauntSlotData : MultiplayerLocalData
{
	public string PlayerId { get; set; }

	public List<TauntIndexData> TauntIndices { get; set; }

	public TauntSlotData(string playerId)
	{
		PlayerId = playerId;
		TauntIndices = new List<TauntIndexData>();
	}

	public override bool HasSameContentWith(MultiplayerLocalData other)
	{
		if (other is TauntSlotData tauntSlotData)
		{
			if (PlayerId == tauntSlotData.PlayerId)
			{
				return TauntIndices.SequenceEqual(tauntSlotData.TauntIndices);
			}
			return false;
		}
		return false;
	}
}
