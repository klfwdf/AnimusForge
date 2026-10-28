using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Core;

public abstract class GameModelsManager
{
	private readonly MBList<GameModel> _gameModels;

	protected GameModelsManager(IEnumerable<GameModel> inputComponents)
	{
		_gameModels = inputComponents.ToMBList();
	}

	protected T GetGameModel<T>() where T : GameModel
	{
		for (int num = _gameModels.Count - 1; num >= 0; num--)
		{
			if (_gameModels[num] is T result)
			{
				return result;
			}
		}
		return null;
	}

	public MBReadOnlyList<GameModel> GetGameModels()
	{
		return _gameModels;
	}
}
