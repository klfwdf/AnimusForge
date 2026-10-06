using NavalDLC.ComponentInterfaces;
using TaleWorlds.CampaignSystem;

namespace NavalDLC.GameComponents;

public class NavalDLCClanShipOwnershipModel : ClanShipOwnershipModel
{
	public override int GetIdealShipNumberForClan(Clan clan)
	{
		return Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan, clan.Tier) * 3;
	}
}
