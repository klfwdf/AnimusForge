using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions;

public class CivilianPortShipSpawnMissionLogic : MissionLogic
{
	private const string ShipyardShipSpawnPointTag = "shipyard_ship";

	private Queue<GameEntity> _shipyardShipSpawnPoints = new Queue<GameEntity>();

	private List<Ship> _mainPartyShips = new List<Ship>();

	private List<Ship> _townLordShips = new List<Ship>();

	private Dictionary<GameEntity, MatrixFrame> _spawnedShipVisuals = new Dictionary<GameEntity, MatrixFrame>();

	public CivilianPortShipSpawnMissionLogic(List<Ship> mainPartyShips, List<Ship> townLordShips)
	{
		_mainPartyShips = mainPartyShips;
		_townLordShips = townLordShips;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		foreach (GameEntity item in Mission.Current.Scene.FindEntitiesWithTag("shipyard_ship"))
		{
			_shipyardShipSpawnPoints.Enqueue(item);
		}
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		if (!_shipyardShipSpawnPoints.IsEmpty())
		{
			if (!_mainPartyShips.IsEmpty())
			{
				Ship randomElement = _mainPartyShips.GetRandomElement();
				SpawnShip(randomElement);
			}
			while (!_shipyardShipSpawnPoints.IsEmpty() && !_townLordShips.IsEmpty())
			{
				Ship randomElement2 = _townLordShips.GetRandomElement();
				_townLordShips.Remove(randomElement2);
				SpawnShip(randomElement2);
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		foreach (KeyValuePair<GameEntity, MatrixFrame> spawnedShipVisual in _spawnedShipVisuals)
		{
			TickShipAnimation(dt, spawnedShipVisual.Key, spawnedShipVisual.Value);
		}
	}

	private void SpawnShip(Ship ship)
	{
		MissionShipObject missionShipObject = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.ShipHull.MissionShipObjectId);
		(uint sailColor1, uint sailColor2) sailColors = ShipHelper.GetSailColors(ship);
		GameEntity gameEntity = VisualShipFactory.CreateVisualShip(sailColor1: sailColors.sailColor1, sailColor2: sailColors.sailColor2, shipPrefab: missionShipObject.Prefab, scene: base.Mission.Scene, upgrades: ship.GetShipVisualSlotInfos(), shipSeed: ship.RandomValue, hitPointRatio: ship.HitPoints / ship.MaxSailHitPoints, createPhysics: true);
		MatrixFrame frame = _shipyardShipSpawnPoints.Dequeue().GetGlobalFrame();
		float waterLevelAtPosition = base.Mission.Scene.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		frame.origin.z = waterLevelAtPosition;
		gameEntity?.SetFrame(ref frame);
		_spawnedShipVisuals.Add(gameEntity, frame);
	}

	private void TickShipAnimation(float dt, GameEntity shipVisualEntity, in MatrixFrame initialFrame)
	{
		if (shipVisualEntity == null)
		{
			return;
		}
		MatrixFrame m = shipVisualEntity.GetFrame();
		Vec3 vec = shipVisualEntity.GetBoundingBoxMin() + new Vec3(5f, 5f);
		Vec3 vec2 = shipVisualEntity.GetBoundingBoxMax() - new Vec3(5f, 5f);
		Vec2[] array = new Vec2[32];
		for (int i = 0; i < 4; i++)
		{
			float amount = (float)i / 3f;
			float x = MathF.Lerp(vec.x, vec2.x, amount);
			for (int j = 0; j < 8; j++)
			{
				float amount2 = (float)j / 7f;
				float y = MathF.Lerp(vec.y, vec2.y, amount2);
				Vec3 vec3 = m.origin + new Vec3(x, y);
				int num = i * 8 + j;
				array[num] = vec3.AsVec2;
			}
		}
		Vec3 zero = Vec3.Zero;
		float num2 = 0f;
		float[] waterHeightsAtVolumes = new float[array.Length];
		Vec3[] waterSurfaceNormals = new Vec3[array.Length];
		base.Mission.Scene.GetBulkWaterLevelAtPositions(array, ref waterHeightsAtVolumes, ref waterSurfaceNormals);
		for (int k = 0; k < waterSurfaceNormals.Length; k++)
		{
			Vec3 vec4 = waterSurfaceNormals[k];
			zero += vec4;
			num2 += waterHeightsAtVolumes[k];
		}
		zero.Normalize();
		num2 /= (float)waterSurfaceNormals.Length;
		MatrixFrame m2 = initialFrame;
		m2.origin.z = num2 + 0.5f;
		Mat3 identity = Mat3.Identity;
		identity.u = zero;
		identity.u.Normalize();
		identity.s = Vec3.CrossProduct(Vec3.Forward, identity.u);
		identity.s.Normalize();
		identity.f = Vec3.CrossProduct(identity.u, identity.s);
		identity.f.Normalize();
		m2.rotation = identity;
		MatrixFrame frame = MatrixFrame.Slerp(in m, in m2, dt * 1.5f);
		shipVisualEntity.SetFrame(ref frame);
	}
}
