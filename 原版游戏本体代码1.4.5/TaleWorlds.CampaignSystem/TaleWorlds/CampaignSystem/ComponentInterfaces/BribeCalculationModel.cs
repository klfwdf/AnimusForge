using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BribeCalculationModel : MBGameModel<BribeCalculationModel>
{
	public abstract int GetBribeToEnterLordsHall(Settlement settlement);

	public abstract int GetBribeToEnterDungeon(Settlement settlement);

	public abstract bool IsBribeNotNeededToEnterKeep(Settlement settlement);

	public abstract bool IsBribeNotNeededToEnterDungeon(Settlement settlement);
}
