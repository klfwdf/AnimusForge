using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class CosmeticItemInfo
{
	public string TroopId { get; set; }

	public string CosmeticIndex { get; set; }

	public bool IsEquipped { get; set; }

	public CosmeticItemInfo()
	{
	}

	public CosmeticItemInfo(string troopId, string cosmeticIndex, bool isEquipped)
	{
		TroopId = troopId;
		CosmeticIndex = cosmeticIndex;
		IsEquipped = isEquipped;
	}
}
