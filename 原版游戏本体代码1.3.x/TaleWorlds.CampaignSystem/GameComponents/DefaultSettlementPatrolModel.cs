using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200014D RID: 333
	public class DefaultSettlementPatrolModel : SettlementPatrolModel
	{
		// Token: 0x060019EA RID: 6634 RVA: 0x00081FF4 File Offset: 0x000801F4
		public override CampaignTime GetPatrolPartySpawnDuration(Settlement settlement, bool naval)
		{
			Building guardHouse = this.GetGuardHouse(settlement);
			return CampaignTime.Days(10f - ((float)guardHouse.CurrentLevel - 1f) * 2f);
		}

		// Token: 0x060019EB RID: 6635 RVA: 0x00082027 File Offset: 0x00080227
		public override bool CanSettlementHavePatrolParties(Settlement settlement, bool naval)
		{
			return settlement.OwnerClan != null && !settlement.OwnerClan.IsRebelClan && settlement.IsTown && this.HasGuardHouse(settlement);
		}

		// Token: 0x060019EC RID: 6636 RVA: 0x0008204F File Offset: 0x0008024F
		private bool HasGuardHouse(Settlement settlement)
		{
			return this.GetGuardHouse(settlement) != null;
		}

		// Token: 0x060019ED RID: 6637 RVA: 0x0008205C File Offset: 0x0008025C
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

		// Token: 0x060019EE RID: 6638 RVA: 0x000820D4 File Offset: 0x000802D4
		public override PartyTemplateObject GetPartyTemplateForPatrolParty(Settlement settlement, bool naval)
		{
			Building guardHouse = this.GetGuardHouse(settlement);
			if (guardHouse == null)
			{
				return null;
			}
			switch ((int)Campaign.Current.Models.BuildingEffectModel.GetBuildingEffect(guardHouse, BuildingEffectEnum.PatrolPartyStrength).ResultNumber)
			{
			case 1:
				return settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateWeak;
			case 2:
				return settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateModerate;
			case 3:
				return settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateStrong;
			default:
				return settlement.OwnerClan.Culture.SettlementPatrolPartyTemplateWeak;
			}
		}
	}
}
