using TaleWorlds.Core;

namespace Sandbox.View.GameStates;

public class PreloadState : GameState
{
	public readonly string SaveToLoad;

	public readonly int LoadDelayInFrames;

	public PreloadState()
	{
		LoadDelayInFrames = 1;
		SaveToLoad = string.Empty;
	}

	public PreloadState(string saveName)
	{
		LoadDelayInFrames = 2;
		SaveToLoad = saveName;
	}
}
