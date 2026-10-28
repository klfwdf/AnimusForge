using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerGameTypeInfo
{
	public string GameModule { get; private set; }

	public string GameType { get; private set; }

	public List<string> Scenes { get; private set; }

	public MultiplayerGameTypeInfo(string gameModule, string gameType)
	{
		GameModule = gameModule;
		GameType = gameType;
		Scenes = new List<string>();
	}
}
