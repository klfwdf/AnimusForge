using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;

public class NativeSceneNotificationContextProvider : ISceneNotificationContextProvider
{
	public bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType)
	{
		if (relevantType == SceneNotificationData.RelevantContextType.Mission)
		{
			return GameStateManager.Current.ActiveState is MissionState;
		}
		return true;
	}
}
