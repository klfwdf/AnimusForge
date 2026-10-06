namespace TaleWorlds.Core;

public interface ISceneNotificationContextProvider
{
	bool IsContextAllowed(SceneNotificationData.RelevantContextType relevantType);
}
