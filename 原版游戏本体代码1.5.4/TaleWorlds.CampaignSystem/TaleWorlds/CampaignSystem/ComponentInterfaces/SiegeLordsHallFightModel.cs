using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SiegeLordsHallFightModel : MBGameModel<SiegeLordsHallFightModel>
{
	public abstract float AreaLostRatio { get; }

	public abstract float AttackerDefenderTroopCountRatio { get; }

	public abstract int DefenderTroopNumberForSuccessfulPullBack { get; }

	public abstract float DefenderMaxArcherRatio { get; }

	public abstract int MaxDefenderSideTroopCount { get; }

	public abstract int MaxDefenderArcherCount { get; }

	public abstract int MaxAttackerSideTroopCount { get; }

	public abstract FlattenedTroopRoster GetPriorityListForLordsHallFightMission(MapEvent playerMapEvent, BattleSideEnum side, int troopCount);
}
