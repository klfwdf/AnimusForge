using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.MissionControllers;

public class CosmeticShipSpawnMissionLogic : MissionLogic
{
	private const string CosmeticShipSpawnPointTag = "cosmetic_ship_spawn_point";

	private const float AnimationSpeedMultiplier = 0.1f;

	private List<string> _cosmeticShipIdList = new List<string> { "nord_medium_ship", "khuzait_heavy_ship", "eastern_medium_ship", "empire_trade_ship" };

	private Queue<GameEntity> _cosmeticShipSpawnPointEntities = new Queue<GameEntity>();

	private Dictionary<GameEntity, MatrixFrame> _spawnedShipVisuals = new Dictionary<GameEntity, MatrixFrame>();

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("cosmetic_ship_spawn_point"))
		{
			_cosmeticShipSpawnPointEntities.Enqueue(item);
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		while (!_cosmeticShipSpawnPointEntities.IsEmpty())
		{
			ShipHull shipHull = MBObjectManager.Instance.GetObject<ShipHull>(_cosmeticShipIdList.GetRandomElement());
			SpawnShip(shipHull);
		}
	}

	private void SpawnShip(ShipHull shipHull)
	{
		MissionShipObject missionShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(shipHull.MissionShipObjectId);
		uint sailColor = 4291609515u;
		uint sailColor2 = 4291609515u;
		GameEntity gameEntity = VisualShipFactory.CreateVisualShip(missionShipObject.Prefab, base.Mission.Scene, new List<ShipVisualSlotInfo>(), MBRandom.RandomInt(), 1f, sailColor, sailColor2, createPhysics: true, shipHull.FloatingForceMultiplier);
		MatrixFrame frame = _cosmeticShipSpawnPointEntities.Dequeue().GetGlobalFrame();
		frame.rotation.MakeUnit();
		float waterLevelAtPosition = base.Mission.Scene.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		frame.origin.z = waterLevelAtPosition;
		gameEntity.SetFrame(ref frame);
		List<SailVisual> sailVisuals = new List<SailVisual>();
		CollectSailVisuals(gameEntity.WeakEntity, sailVisuals);
		FoldSails(sailVisuals);
		_spawnedShipVisuals.Add(gameEntity, frame);
	}

	private void CollectSailVisuals(WeakGameEntity shipEntity, List<SailVisual> sailVisuals)
	{
		sailVisuals.Clear();
		ShipVisual firstScriptOfType = shipEntity.GetFirstScriptOfType<ShipVisual>();
		if (firstScriptOfType == null)
		{
			return;
		}
		foreach (ScriptComponentBehavior sailVisual2 in firstScriptOfType.SailVisuals)
		{
			if (sailVisual2 is SailVisual sailVisual)
			{
				sailVisual.SailEnabled = false;
				sailVisual.SetFoldSailStepMultiplier(0.3f);
				sailVisual.SetFoldSailDuration(0.4f);
				sailVisual.SetUnfoldSailDuration(0.2f);
				sailVisual.FoldAnimationEnabled = false;
				sailVisuals.Add(sailVisual);
			}
		}
	}

	private void FoldSails(List<SailVisual> sailVisuals)
	{
		foreach (SailVisual sailVisual in sailVisuals)
		{
			sailVisual.SailEnabled = false;
		}
	}
}
