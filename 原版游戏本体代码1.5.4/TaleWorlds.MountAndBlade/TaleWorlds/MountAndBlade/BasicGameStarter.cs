using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class BasicGameStarter : IGameStarter
{
	private List<GameModel> _models;

	IEnumerable<GameModel> IGameStarter.Models => _models;

	public BasicGameStarter()
	{
		_models = new List<GameModel>();
	}

	public T GetModel<T>() where T : GameModel
	{
		for (int num = _models.Count - 1; num >= 0; num--)
		{
			if (_models[num] is T result)
			{
				return result;
			}
		}
		return null;
	}

	public void AddModel(GameModel gameModel)
	{
		_models.Add(gameModel);
	}

	public void AddModel<T>(MBGameModel<T> gameModel) where T : GameModel
	{
		T model = GetModel<T>();
		gameModel.Initialize(model);
		_models.Add(gameModel);
	}
}
