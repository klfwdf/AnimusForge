using TaleWorlds.Core;

namespace Bannerlord.ButterLib.MBSubModuleBaseExtended;

public interface IMBSubModuleBaseEx
{
	void OnAllSubModulesUnLoaded();

	void OnBeforeInitialModuleScreenSetAsRootDelayed();

	void OnGameStartDelayed(Game game, IGameStarter gameStarterObject);

	void OnGameEndDelayed(Game game);
}
