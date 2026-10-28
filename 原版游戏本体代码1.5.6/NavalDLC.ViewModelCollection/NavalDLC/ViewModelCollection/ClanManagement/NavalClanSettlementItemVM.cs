using System;
using NavalDLC.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace NavalDLC.ViewModelCollection.ClanManagement;

public class NavalClanSettlementItemVM : ClanSettlementItemVM
{
	public NavalClanSettlementItemVM(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
		: base(settlement, onSelection, onShowSendMembers, teleportationBehavior)
	{
	}

	protected override ClanSettlementItemVM CreateSettlementItem(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
	{
		return new NavalClanSettlementItemVM(settlement, onSelection, onShowSendMembers, teleportationBehavior);
	}

	protected override void UpdateProperties()
	{
		base.UpdateProperties();
		Building building = Settlement.Town?.GetShipyard();
		if (building != null)
		{
			BasicTooltipViewModel hint = new BasicTooltipViewModel(() => NavalUIHelper.GetShipyardTooltip(Settlement.Town));
			int currentLevel = building.CurrentLevel;
			base.ItemProperties.Insert(1, new SelectableFiefItemPropertyVM(GameTexts.FindText("str_shipyard").ToString(), currentLevel.ToString(), 0, SelectableItemPropertyVM.PropertyType.Shipyard, hint));
		}
		if (Settlement.IsTown && Settlement.HasPort)
		{
			BasicTooltipViewModel hint2 = new BasicTooltipViewModel(() => NavalUIHelper.GetTownCoastalPatrolTooltip(Settlement.Town));
			base.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_coastal_patrol").ToString(), Campaign.Current.GetCampaignBehavior<INavalPatrolPartiesCampaignBehavior>().GetSettlementPatrolStatus(Settlement).ToString(), 0, SelectableItemPropertyVM.PropertyType.CoastalPatrol, hint2));
		}
	}
}
