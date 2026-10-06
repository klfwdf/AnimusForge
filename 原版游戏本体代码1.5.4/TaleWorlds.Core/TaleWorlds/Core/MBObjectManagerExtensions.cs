using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public static class MBObjectManagerExtensions
{
	public static void LoadXML(this MBObjectManager objectManager, string id, bool skipXmlFilterForEditor = false)
	{
		Game current = Game.Current;
		bool isDevelopment = false;
		string gameType = "";
		if (current != null)
		{
			isDevelopment = current.GameType.IsDevelopment;
			gameType = current.GameType.GameTypeStringId;
		}
		objectManager.LoadXML(id, isDevelopment, gameType, skipXmlFilterForEditor);
	}
}
