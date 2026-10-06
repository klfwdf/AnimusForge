using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PlayerProgressionModel : MBGameModel<PlayerProgressionModel>
{
	public abstract float GetPlayerProgress();
}
