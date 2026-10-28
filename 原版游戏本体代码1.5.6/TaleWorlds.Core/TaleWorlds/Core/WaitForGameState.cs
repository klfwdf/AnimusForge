using System;
using TaleWorlds.Network;

namespace TaleWorlds.Core;

public class WaitForGameState : CoroutineState
{
	private Type _stateType;

	protected override bool IsFinished
	{
		get
		{
			GameState gameState = ((GameStateManager.Current != null) ? GameStateManager.Current.ActiveState : null);
			if (gameState != null)
			{
				return _stateType.IsInstanceOfType(gameState);
			}
			return false;
		}
	}

	public WaitForGameState(Type stateType)
	{
		_stateType = stateType;
	}
}
