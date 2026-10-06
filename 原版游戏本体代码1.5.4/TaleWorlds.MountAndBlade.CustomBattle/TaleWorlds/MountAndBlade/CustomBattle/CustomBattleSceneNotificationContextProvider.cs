using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CustomBattleSceneNotificationContextProvider : ISceneNotificationContextProvider
{
	public bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType)
	{
		if (relevantType == SceneNotificationData.RelevantContextType.CustomBattle)
		{
			return GameStateManager.Current.ActiveState is CustomBattleState;
		}
		return true;
	}
}
