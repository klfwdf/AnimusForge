using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

public class MultiplayerSceneNotificationContextProvider : ISceneNotificationContextProvider
{
	public bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType)
	{
		if (relevantType == SceneNotificationData.RelevantContextType.MPLobby)
		{
			return GameStateManager.Current.ActiveState is LobbyState;
		}
		return true;
	}
}
