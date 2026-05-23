using System;
using Sandbox.View.GameStates;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.ScreenSystem;

namespace SandBox.View;

[GameStateScreen(typeof(PreloadState))]
public class PreloadScreen : ScreenBase, IGameStateListener
{
	private readonly PreloadState _state;

	private readonly int _delayInFrames;

	private int _delayCounter;

	public PreloadScreen(PreloadState inventoryState)
	{
		_state = inventoryState;
		_delayCounter = 0;
		_delayInFrames = Math.Max(0, _state.LoadDelayInFrames);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		LoadingWindow.EnableGlobalLoadingWindow();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_delayCounter == _delayInFrames)
		{
			SandBoxSaveHelper.TryLoadSave(MBSaveLoad.GetSaveFileWithName(_state.SaveToLoad) ?? throw new MBException("Preload state called without a valid save name. Game will be stuck at this point."), StartGame);
		}
		else
		{
			_delayCounter++;
		}
	}

	private void StartGame(LoadResult loadResult)
	{
		MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}
}
