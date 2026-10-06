using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSettlementPatrolModel : SettlementPatrolModel
{
	public override CampaignTime GetPatrolPartySpawnDuration(Settlement settlement, bool naval)
	{
		Building guardHouse = GetGuardHouse(settlement);
		return CampaignTime.Days(10f - ((float)guardHouse.CurrentLevel - 1f) * 2f);
	}

	public override bool CanSettlementHavePatrolParties(Settlement settlement, bool naval)
	{
		if (settlement.OwnerClan != null && !settlement.OwnerClan.IsRebelClan && settlement.IsTown)
		{
			return HasGuardHouse(settlement);
		}
		return false;
	}

	private bool HasGuardHouse(Settlement settlement)
	{
		return GetGuardHouse(settlement) != null;
	}

	private Building GetGuardHouse(Settlement settlement)
	{
		if (settlement.Town != null)
		{
			foreach (Building building in settlement.Town.Buildings)
			{
				if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse && building.CurrentLevel > 0)
				{
					return building;
				}
			}
		}
		return null;
	}

	public override PartyTemplateObject GetPartyTemplateForPatrolParty(Settlement settlement, bool naval)
	{
		Building guardHouse = GetGuardHouse(settlement);
		if (guardHouse != null)
		{
			return (int)Campaign.Current.Models.BuildingEffectModel.GetBuildingEffect(guardHouse, BuildingEffectEnum.PatrolPartyStrength).ResultNumber switch
			{
				1 => settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateWeak, 
				2 => settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateModerate, 
				3 => settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateStrong, 
				_ => settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateWeak, 
			};
		}
		return null;
	}
}
