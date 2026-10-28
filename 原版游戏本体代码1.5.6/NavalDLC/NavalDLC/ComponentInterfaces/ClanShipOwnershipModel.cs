using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace NavalDLC.ComponentInterfaces;

public abstract class ClanShipOwnershipModel : MBGameModel<ClanShipOwnershipModel>
{
	public abstract int GetIdealShipNumberForClan(Clan clan);
}
