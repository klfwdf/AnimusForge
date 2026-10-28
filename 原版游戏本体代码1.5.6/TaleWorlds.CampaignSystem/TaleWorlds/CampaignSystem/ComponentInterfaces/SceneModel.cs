using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SceneModel : MBGameModel<SceneModel>
{
	public abstract string GetConversationSceneForMapPosition(CampaignVec2 campaignPosition);

	public abstract string GetBattleSceneForMapPatch(MapPatchData mapPatch, bool isNavalEncounter);
}
