namespace TaleWorlds.MountAndBlade.Diamond.Cosmetics.CosmeticTypes;

public class SigilCosmeticElement : CosmeticElement
{
	public string BannerCode;

	public SigilCosmeticElement(string id, CosmeticsManager.CosmeticRarity rarity, int cost, string bannerCode)
		: base(id, rarity, cost, CosmeticsManager.CosmeticType.Sigil)
	{
		BannerCode = bannerCode;
	}
}
