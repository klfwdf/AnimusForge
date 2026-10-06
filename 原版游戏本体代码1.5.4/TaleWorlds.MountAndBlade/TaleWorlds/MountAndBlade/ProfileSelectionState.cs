using TaleWorlds.Core;
using TaleWorlds.Engine.Options;

namespace TaleWorlds.MountAndBlade;

public class ProfileSelectionState : GameState
{
	public delegate void OnProfileSelectionEvent();

	public bool IsDirectPlayPossible { get; private set; } = true;

	public event OnProfileSelectionEvent OnProfileSelection;

	public void OnProfileSelected()
	{
		NativeOptions.ReadRGLConfigFiles();
		BannerlordConfig.Initialize();
		this.OnProfileSelection?.Invoke();
		StartGame();
	}

	public void StartGame()
	{
		Module.CurrentModule.SetInitialModuleScreenAsRootScreen();
	}
}
