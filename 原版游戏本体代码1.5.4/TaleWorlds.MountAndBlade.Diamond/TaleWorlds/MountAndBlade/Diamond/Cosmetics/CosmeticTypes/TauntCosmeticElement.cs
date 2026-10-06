using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;

public class TauntCosmeticElement : CosmeticElement
{
	public static int MaxNumberOfTaunts => 6;

	public TextObject Name { get; }

	public TauntCosmeticElement(int index, string id, CosmeticsManager.CosmeticRarity rarity, int cost, string name)
		: base(id, rarity, cost, CosmeticsManager.CosmeticType.Taunt)
	{
		UsageIndex = index;
		Name = new TextObject(name);
	}
}
