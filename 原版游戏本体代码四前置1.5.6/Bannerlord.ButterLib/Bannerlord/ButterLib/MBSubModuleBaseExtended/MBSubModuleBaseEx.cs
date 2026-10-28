using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.ButterLib.MBSubModuleBaseExtended;

public abstract class MBSubModuleBaseEx : MBSubModuleBase, IMBSubModuleBaseEx
{
	public virtual void OnAllSubModulesUnLoaded()
	{
	}

	public virtual void OnBeforeInitialModuleScreenSetAsRootDelayed()
	{
	}

	public virtual void OnGameStartDelayed(Game game, IGameStarter gameStarterObject)
	{
	}

	public virtual void OnGameEndDelayed(Game game)
	{
	}
}
