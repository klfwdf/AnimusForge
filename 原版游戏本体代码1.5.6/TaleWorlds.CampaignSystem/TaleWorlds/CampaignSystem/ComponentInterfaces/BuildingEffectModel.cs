using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BuildingEffectModel : MBGameModel<BuildingEffectModel>
{
	public abstract ExplainedNumber GetBuildingEffect(Building building, BuildingEffectEnum effect);
}
