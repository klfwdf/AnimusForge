using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.View;

public class NavalShipsPreloadView : MissionView
{
	private PreloadHelper _helperInstance = new PreloadHelper();

	public override void OnBehaviorInitialize()
	{
		Mission.Current.Scene.SetDoNotAddEntitiesToTickList(value: true);
		DefaultNavalMissionLogic missionBehavior = base.Mission.GetMissionBehavior<DefaultNavalMissionLogic>();
		if (missionBehavior != null)
		{
			if (missionBehavior.PlayerShips != null)
			{
				foreach (IShipOrigin playerShip in missionBehavior.PlayerShips)
				{
					PreloadShip(playerShip);
				}
			}
			if (missionBehavior.PlayerAllyShips != null)
			{
				foreach (IShipOrigin playerAllyShip in missionBehavior.PlayerAllyShips)
				{
					PreloadShip(playerAllyShip);
				}
			}
			if (missionBehavior.PlayerEnemyShips != null)
			{
				foreach (IShipOrigin playerEnemyShip in missionBehavior.PlayerEnemyShips)
				{
					PreloadShip(playerEnemyShip);
				}
			}
			_helperInstance.PreloadMeshesAndPhysics();
		}
		Mission.Current.Scene.SetDoNotAddEntitiesToTickList(value: false);
	}

	public override void OnSceneRenderingStarted()
	{
		_helperInstance.WaitForMeshesToBeLoaded();
	}

	public void PreloadShip(IShipOrigin ship)
	{
		MissionShipObject missionShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.OriginShipId);
		GameEntity gameEntity = GameEntity.InstantiateWithRestOffset(base.Mission.Scene, missionShipObject.Prefab, createPhysics: true, MatrixFrame.Identity, -0.1f, callScriptCallbacks: false);
		MissionShipFactory.CleanNonExistingUpgrades(gameEntity.WeakEntity, ship.GetShipVisualSlotInfos());
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		gameEntity.WeakEntity.GetChildrenRecursive(ref children);
		children.Add(gameEntity.WeakEntity);
		_helperInstance.PreloadEntities(children);
		gameEntity.Remove(76);
	}
}
