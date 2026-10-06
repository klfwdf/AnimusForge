using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace SandBox.GauntletUI;

public class SandboxSceneNotificationContextProvider : ISceneNotificationContextProvider
{
	public bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType)
	{
		if (relevantType == SceneNotificationData.RelevantContextType.Map)
		{
			return GameStateManager.Current.ActiveState is MapState;
		}
		return true;
	}
}
