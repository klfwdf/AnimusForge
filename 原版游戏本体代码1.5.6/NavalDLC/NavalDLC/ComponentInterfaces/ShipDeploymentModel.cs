using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace NavalDLC.ComponentInterfaces;

public abstract class ShipDeploymentModel : MBGameModel<ShipDeploymentModel>
{
	internal static bool IgnoreDeploymentLimits;

	public abstract int GetShipDeploymentLimit(MobileParty party);

	public abstract void GetMapEventPartiesOfPlayerTeams(MBReadOnlyList<MapEventParty> playerSideMapEventParties, bool isPlayerSergeant, out MapEventParty playerMapEventParty, out MBList<MapEventParty> playerTeamMapEventParties, out MBList<MapEventParty> playerAllyTeamMapEventParties);

	public abstract void GetShipDeploymentLimitsOfPlayerTeams(MBList<MapEventParty> playerTeamMapEventParties, MBList<MapEventParty> playerAllyTeamMapEventParties, out NavalShipDeploymentLimit playerTeamDeploymentLimit, out NavalShipDeploymentLimit playerAllyTeamDeploymentLimit);

	public abstract NavalShipDeploymentLimit GetTeamShipDeploymentLimit(MBReadOnlyList<MapEventParty> teamMapEventParties);

	public abstract Ship GetSuitablePlayerShip(MapEventParty playerMapEventParty, MBList<MapEventParty> playerTeamMapEventParties);

	public abstract void FillShipsOfTeamParties(MBReadOnlyList<MapEventParty> teamMapEventParties, NavalShipDeploymentLimit shipDeploymentLimit, MBList<IShipOrigin> teamShips);

	public abstract void GetOrderedCaptainsForPlayerTeamShips(MBReadOnlyList<MapEventParty> playerTeamMapEventParties, MBReadOnlyList<IShipOrigin> playerTeamShips, out List<string> playerTeamCaptainsByPriority);

	public abstract int GetMaximumDeployableTroopCountForTeam(MBList<IShipOrigin> teamShips, bool isPlayerTeam = false);
}
