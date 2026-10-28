namespace TaleWorlds.MountAndBlade.Diamond.Cosmetics;

public class CosmeticElement
{
	public int UsageIndex;

	public string Id;

	public CosmeticsManager.CosmeticRarity Rarity;

	public int Cost;

	public CosmeticsManager.CosmeticType Type;

	public bool IsFree => Cost <= 0;

	public CosmeticElement(string id, CosmeticsManager.CosmeticRarity rarity, int cost, CosmeticsManager.CosmeticType type)
	{
		UsageIndex = -1;
		Id = id;
		Rarity = rarity;
		Cost = cost;
		Type = type;
	}
}
