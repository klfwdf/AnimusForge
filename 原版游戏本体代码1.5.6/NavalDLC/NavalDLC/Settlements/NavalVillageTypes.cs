using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.Settlements;

public class NavalVillageTypes
{
	private static NavalVillageTypes Instance => NavalDLCManager.Instance.NavalVillageTypes;

	public static VillageType WalrusHunter => Instance.VillageTypeWalrusHunter;

	public static VillageType Whaler => Instance.VillageTypeWhaler;

	internal VillageType VillageTypeWalrusHunter { get; private set; }

	internal VillageType VillageTypeWhaler { get; private set; }

	public NavalVillageTypes()
	{
		RegisterAll();
		InitializeAll();
		AddProductions();
	}

	private VillageType Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new VillageType(stringId));
	}

	private void RegisterAll()
	{
		VillageTypeWalrusHunter = Create("walrus_hunter");
		VillageTypeWhaler = Create("whaler");
	}

	private ItemObject GetItemObject(string objectId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new ItemObject(objectId));
	}

	private void InitializeAll()
	{
		VillageTypeWalrusHunter.Initialize(new TextObject("{=Eg7KEtGg}Walrus Tusk Hunters"), "kitchen_horn", "fisherman_ucon", "fisherman_burned", new(ItemObject, float)[1] { (GetItemObject("fish"), 5f) });
		VillageTypeWhaler.Initialize(new TextObject("{=QdCFs5tT}Whalers"), "bd_barrel_a", "fisherman_ucon", "fisherman_burned", new(ItemObject, float)[1] { (GetItemObject("fish"), 5f) });
	}

	private void AddProductions()
	{
		VillageTypeWalrusHunter.AddProductions(new(ItemObject, float)[1] { (GetItemObject("walrus_tusk"), 1.4f) });
		VillageTypeWhaler.AddProductions(new(ItemObject, float)[1] { (GetItemObject("whale_oil"), 1.8f) });
	}
}
