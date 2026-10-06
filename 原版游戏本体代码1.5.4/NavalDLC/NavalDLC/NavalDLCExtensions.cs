using System.Collections.Generic;
using NavalDLC.Settlements.Building;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace NavalDLC;

public static class NavalDLCExtensions
{
	public static bool IsFishingParty(this MobileParty party)
	{
		return party.PartyComponent is FishingPartyComponent;
	}

	public static MBReadOnlyList<FishingPartyComponent> FishingParties(this Village village)
	{
		if (!NavalDLCManager.Instance.FishingParties.TryGetValue(village, out var value))
		{
			value = new List<FishingPartyComponent>();
		}
		return new MBReadOnlyList<FishingPartyComponent>(value);
	}

	public static bool IsPirate(this CharacterObject characterObject)
	{
		if (characterObject.IsMariner && !characterObject.IsHero)
		{
			return characterObject.Occupation == Occupation.Bandit;
		}
		return false;
	}

	public static Building GetShipyard(this Town town)
	{
		foreach (Building building in town.Buildings)
		{
			if (building.BuildingType == NavalBuildingTypes.SettlementShipyard)
			{
				return building;
			}
		}
		return null;
	}

	public static List<ShipUpgradePiece> GetAvailableShipUpgradePieces(this Town town)
	{
		List<ShipUpgradePiece> list = new List<ShipUpgradePiece>();
		MBReadOnlyList<ShipUpgradePiece> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<ShipUpgradePiece>();
		CultureObject culture = town.Culture;
		int num = town.GetShipyard()?.CurrentLevel ?? 0;
		foreach (ShipUpgradePiece item in objectTypeList)
		{
			if (!item.NotMerchandise && item.RequiredPortLevel <= num && ((item.RequiredCulture1 == null && item.RequiredCulture2 == null) || culture == item.RequiredCulture1 || culture == item.RequiredCulture2))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool IsNavalStorylineQuestParty(this PartyBase party, out NavalStorylinePartyData partyData)
	{
		partyData = new NavalStorylinePartyData();
		NavalDLCEvents.Instance.IsNavalQuestParty(party, partyData);
		return partyData.IsQuestParty;
	}

	public static bool IsNavalStorylineQuestParty(this PartyBase party)
	{
		NavalStorylinePartyData partyData;
		return party.IsNavalStorylineQuestParty(out partyData);
	}

	public static bool IsNavalStorylineQuestParty(this MobileParty mobileParty, out NavalStorylinePartyData partyData)
	{
		return mobileParty.Party.IsNavalStorylineQuestParty(out partyData);
	}

	public static bool IsNavalStorylineQuestParty(this MobileParty mobileParty)
	{
		NavalStorylinePartyData partyData;
		return mobileParty.IsNavalStorylineQuestParty(out partyData);
	}
}
