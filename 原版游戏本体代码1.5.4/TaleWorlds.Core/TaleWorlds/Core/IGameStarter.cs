using System.Collections.Generic;

namespace TaleWorlds.Core;

public interface IGameStarter
{
	IEnumerable<GameModel> Models { get; }

	void AddModel(GameModel gameModel);

	void AddModel<T>(MBGameModel<T> gameModel) where T : GameModel;
}
