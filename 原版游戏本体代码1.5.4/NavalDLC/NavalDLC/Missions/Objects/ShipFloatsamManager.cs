using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

public class ShipFloatsamManager : ScriptComponentBehavior
{
	private enum DebrisType
	{
		Generic,
		Scrape,
		Ramming
	}

	private enum DecalType
	{
		Collision,
		Scrape
	}

	private struct ImpulseRecord
	{
		internal Vec3 AveragePosition;

		internal Vec3 AverageNormal;

		internal float TotalImpulse;

		internal Vec3 Speed;

		internal DebrisType DebrisType;

		internal float InitialSpeedMultiplier;

		internal Vec3 ShipLocalPosition;

		internal Vec3 ShipLocalNormal;

		internal DecalType DecalType;
	}

	private struct ShieldBreakRecord
	{
		internal Vec3 LinearVelocity;

		internal Texture BannerTexture;

		internal MatrixFrame ShipLocalSpawnFrame;

		internal string PrefabName;
	}

	private class ScrapeRecord
	{
		internal ParticleSystem Particle;

		internal float AccumulatedDistance;

		internal Vec3 PreviousPosition;
	}

	private static readonly string[] GenericPrefabNames = new string[3] { "floatable_debris_broken_barrel", "floatable_debris_door", "floatable_debris_barrel_a" };

	private static readonly string[] RammingPrefabNames = new string[7] { "floatable_debris_plank_b", "floatable_debris_plank_e", "floatable_debris_plank_f", "floatable_debris_plank_g", "floatable_debris_plank_h", "floatable_debris_plank_j", "floatable_debris_plank_k" };

	private static readonly string[] ScrapeDebrisPrefabNames = new string[7] { "floatable_debris_plank_b", "floatable_debris_plank_e", "floatable_debris_plank_f", "floatable_debris_plank_g", "floatable_debris_plank_h", "floatable_debris_plank_j", "floatable_debris_plank_k" };

	private static readonly string[] CollisionDecalPrefabNames = new string[3] { "decal_ship_damaged_a", "decal_ship_damaged_b", "decal_ship_damaged_c" };

	private static readonly string[] ScrapeDecalPrefabNames = new string[3] { "decal_ship_damage_02", "decal_ship_damage_03", "decal_ship_damage_04" };

	private const string RudderPrefabName = "floatable_debris_rudder";

	private const string ShieldPrefabName = "floatable_debris_";

	private const string OarPrefabName = "floatable_debris_oar_a";

	private const string MastPrefabName = "floatable_debris_mast";

	private const string BodyMeshTag = "body_mesh";

	private const string BannerTag = "banner_with_faction_color";

	private const int MaxNumberOfPendingImpulseRecords = 10;

	private const float DebrisBreakImpulseThreshold = 150000f;

	private const int MaxDecalCount = 30;

	private Dictionary<WeakGameEntity, ScrapeRecord> _scrapeRecords = new Dictionary<WeakGameEntity, ScrapeRecord>();

	private GameEntity _identityFrameParticleParent;

	private int _scrapeParticleIndex = -1;

	private int _collisionHitParticleIndex = -1;

	private int _midCollisionHitParticleIndex = -1;

	private int _bigCollisionHitParticleIndex = -1;

	private readonly MBFastRandom _randomGenerator = new MBFastRandom();

	private ImpulseRecord[] _impulseRecordsToProcess = new ImpulseRecord[10];

	private ShieldBreakRecord[] _shieldBreakRecords = new ShieldBreakRecord[10];

	private uint _shipColor = Colors.White.ToUnsignedInteger();

	private int _numberOfPendingImpulseRecords;

	private int _numberOfPendingShieldBreakRecords;

	private uint _shipDecalColor = Colors.White.ToUnsignedInteger();

	private bool _sinkingFloatsamSpawned;

	private List<GameEntity> _collisionDecals = new List<GameEntity>();

	private string _shieldName = "";

	private NavalFloatsamLogic _floatsamMissionLogic;

	private GameEntity _bodyEntity;

	private MissionShip _ownMissionShipCached;

	private bool _floatsamSystemEnabled;

	internal ShipFloatsamManager()
	{
	}

	protected override void OnInit()
	{
		_identityFrameParticleParent = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene, isModifiableFromEditor: false, createPhysics: false, callScriptCallbacks: false);
		_identityFrameParticleParent.EntityFlags |= EntityFlags.DontSaveToScene;
		_scrapeParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_ship_scrape_emit_on_move");
		_collisionHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_ship_collision");
		_midCollisionHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_naval_ship_hit_mid");
		_bigCollisionHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_naval_ship_hit_large");
		WeakGameEntity firstChildEntityWithTagRecursive = base.GameEntity.GetFirstChildEntityWithTagRecursive("body_mesh");
		if (firstChildEntityWithTagRecursive != null)
		{
			_bodyEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(firstChildEntityWithTagRecursive);
		}
		ColorAssigner firstScriptOfType = base.GameEntity.GetFirstScriptOfType<ColorAssigner>();
		if (firstScriptOfType != null)
		{
			_shipColor = firstScriptOfType.ShipColor.ToUnsignedInteger();
			_shipDecalColor = firstScriptOfType.RamDebrisColor.ToUnsignedInteger();
		}
		_floatsamMissionLogic = Mission.Current.GetMissionBehavior<NavalFloatsamLogic>();
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			ShipShieldComponent firstScriptOfType2 = item.GetFirstScriptOfType<ShipShieldComponent>();
			if (firstScriptOfType2 != null)
			{
				firstScriptOfType2.OnDestroyed += OnShieldDestroyed;
				_shieldName = item.Name;
			}
		}
		_ownMissionShipCached = base.GameEntity.GetFirstScriptOfType<MissionShip>();
		if (_ownMissionShipCached != null)
		{
			NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
			if (missionBehavior != null)
			{
				missionBehavior.ShipHitEvent += OnShipHit;
				missionBehavior.ShipRammingEvent += OnShipRamming;
			}
		}
	}

	protected override void OnTick(float dt)
	{
		if (_floatsamSystemEnabled)
		{
			CheckSinking();
			ProcessImpulseEffects();
			ProcessShieldBreakRecords();
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnPhysicsCollision(ref PhysicsContact contact, WeakGameEntity entity0, WeakGameEntity entity1)
	{
		if (!entity1.HasScriptComponent(MissionShip.MissionShipScriptNameHash) || !_floatsamSystemEnabled)
		{
			return;
		}
		MatrixFrame bodyWorldTransform = entity0.GetBodyWorldTransform();
		bool flag = true;
		Vec3 v = Vec3.Zero;
		Vec3 v2 = Vec3.Zero;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < contact.NumberOfContactPairs; i++)
		{
			PhysicsContactPair physicsContactPair = contact[i];
			for (int j = 0; j < physicsContactPair.NumberOfContacts; j++)
			{
				PhysicsContactInfo physicsContactInfo = physicsContactPair[j];
				v += physicsContactInfo.Position;
				num += physicsContactInfo.Impulse.Length;
				v2 += physicsContactInfo.Normal;
				_ = Colors.White;
				if (physicsContactPair.ContactEventType == PhysicsEventType.CollisionStart)
				{
					flag = false;
				}
				else if (physicsContactPair.ContactEventType == PhysicsEventType.CollisionStay)
				{
					flag = false;
				}
				num2 += 1f;
			}
		}
		if (num2 > 0f)
		{
			v /= num2;
			v2 /= num2;
			v2.Normalize();
			v2 *= -1f;
		}
		if (_scrapeRecords.TryGetValue(entity1, out var value))
		{
			if (flag || num2 == 0f)
			{
				base.GameEntity.RemoveComponent(value.Particle);
				_scrapeRecords.Remove(entity1);
				return;
			}
			MatrixFrame newLocalFrame = MatrixFrame.Identity;
			newLocalFrame.rotation.u = Vec3.Up;
			newLocalFrame.rotation.s = v2;
			newLocalFrame.rotation.f = -newLocalFrame.rotation.s.CrossProductWithUp();
			newLocalFrame.rotation.s = Vec3.CrossProduct(newLocalFrame.rotation.f, newLocalFrame.rotation.u);
			newLocalFrame.origin = v;
			value.AccumulatedDistance += value.PreviousPosition.Distance(v);
			value.PreviousPosition = v;
			value.Particle.SetLocalFrame(in newLocalFrame);
			if (!(value.AccumulatedDistance > 2.5f))
			{
				return;
			}
			value.AccumulatedDistance = 0f;
			if (_numberOfPendingImpulseRecords < 10)
			{
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AveragePosition = v;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AverageNormal = v2;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].TotalImpulse = 150000f;
				Vec3 speed = Vec3.Zero;
				if (entity0.HasDynamicRigidBody())
				{
					speed = base.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(v);
				}
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].Speed = speed;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DebrisType = DebrisType.Scrape;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DecalType = DecalType.Scrape;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].InitialSpeedMultiplier = 0.25f;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalPosition = bodyWorldTransform.TransformToLocal(in v);
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalNormal = bodyWorldTransform.rotation.TransformToLocal(in v2);
				_numberOfPendingImpulseRecords++;
			}
		}
		else if (num2 > 0f)
		{
			ScrapeRecord scrapeRecord = new ScrapeRecord();
			MatrixFrame boneLocalFrame = MatrixFrame.Identity;
			boneLocalFrame.rotation.u = Vec3.Up;
			boneLocalFrame.rotation.s = v2;
			boneLocalFrame.rotation.f = -boneLocalFrame.rotation.s.CrossProductWithUp();
			boneLocalFrame.rotation.s = Vec3.CrossProduct(boneLocalFrame.rotation.f, boneLocalFrame.rotation.u);
			boneLocalFrame.origin = v;
			scrapeRecord.Particle = ParticleSystem.CreateParticleSystemAttachedToEntity(_scrapeParticleIndex, _identityFrameParticleParent, ref boneLocalFrame);
			scrapeRecord.PreviousPosition = v;
			_scrapeRecords.Add(entity1, scrapeRecord);
			if (num > 15000f)
			{
				base.GameEntity.Scene.CreateBurstParticle(_collisionHitParticleIndex, boneLocalFrame);
			}
			Vec3 vec = Vec3.Zero;
			if (entity0.HasDynamicRigidBody())
			{
				vec = base.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(v);
			}
			Vec3 vec2 = Vec3.Zero;
			if (entity1.HasDynamicRigidBody())
			{
				vec2 = base.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(v);
			}
			if (_numberOfPendingImpulseRecords < 10)
			{
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AveragePosition = v;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AverageNormal = v2;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].TotalImpulse = num;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].Speed = vec - vec2;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DebrisType = DebrisType.Scrape;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DecalType = DecalType.Collision;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].InitialSpeedMultiplier = 1f;
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalPosition = bodyWorldTransform.TransformToLocal(in v);
				_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalNormal = bodyWorldTransform.rotation.TransformToLocal(in v2);
				_numberOfPendingImpulseRecords++;
			}
		}
	}

	private void ProcessImpulseEffects()
	{
		while (_numberOfPendingImpulseRecords > 0)
		{
			int num = _numberOfPendingImpulseRecords - 1;
			ProcessImpactEffect(_impulseRecordsToProcess[num]);
			_numberOfPendingImpulseRecords--;
		}
	}

	private void ProcessShieldBreakRecords()
	{
		while (_numberOfPendingShieldBreakRecords > 0)
		{
			int num = _numberOfPendingShieldBreakRecords - 1;
			SpawnBrokenShield(_shieldBreakRecords[num]);
			_numberOfPendingShieldBreakRecords--;
		}
	}

	private void SpawnBrokenShield(ShieldBreakRecord record)
	{
		GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, record.PrefabName, callScriptCallbacks: true);
		MatrixFrame frame = base.GameEntity.GetGlobalFrame().TransformToParent(in record.ShipLocalSpawnFrame);
		Vec3 vec = ComputeRandomPositionOffset(in _randomGenerator, 0.75f);
		frame.origin += vec;
		gameEntity.SetFrame(ref frame);
		gameEntity.SetLinearVelocity(record.LinearVelocity);
		SetRandomAngularVelocityToEntity(gameEntity);
		if (record.BannerTexture != null)
		{
			foreach (Mesh item in gameEntity.GetFirstChildEntityWithTag("shield_mesh_entity").GetAllMeshesWithTag("banner_with_faction_color"))
			{
				Material material = item.GetMaterial().CreateCopy();
				material.SetTexture(Material.MBTextureType.DiffuseMap2, record.BannerTexture);
				uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
				ulong shaderFlags = material.GetShaderFlags();
				material.SetShaderFlags(shaderFlags | num);
				item.SetMaterial(material);
			}
		}
		if (_floatsamMissionLogic != null)
		{
			_floatsamMissionLogic.RegisterFloatsamInstance(gameEntity);
		}
	}

	private static Vec3 ComputeRandomPositionOffset(in MBFastRandom randGenerator, float halfRange)
	{
		return new Vec3
		{
			x = randGenerator.NextFloatRanged(0f - halfRange, halfRange),
			y = randGenerator.NextFloatRanged(0f - halfRange, halfRange),
			z = randGenerator.NextFloatRanged(0f - halfRange, halfRange)
		};
	}

	private void ProcessImpactEffect(ImpulseRecord record)
	{
		int b = ((record.DebrisType == DebrisType.Ramming) ? 10 : 7);
		int num = TaleWorlds.Library.MathF.Min((int)(record.TotalImpulse / 150000f), b);
		for (int i = 0; i < num; i++)
		{
			string randomDebrisPrefab = GetRandomDebrisPrefab(record.DebrisType);
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, randomDebrisPrefab, callScriptCallbacks: true);
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation.RotateAboutSide(_randomGenerator.NextFloatRanged(0f, System.MathF.PI * 2f));
			frame.rotation.RotateAboutForward(_randomGenerator.NextFloatRanged(0f, System.MathF.PI * 2f));
			frame.rotation.RotateAboutUp(_randomGenerator.NextFloatRanged(0f, System.MathF.PI * 2f));
			frame.rotation.Orthonormalize();
			Vec3 vec = ComputeRandomPositionOffset(in _randomGenerator, 0.75f);
			frame.origin = record.AveragePosition + vec;
			gameEntity.SetFrame(ref frame);
			Vec3 vec2 = record.TotalImpulse * record.AverageNormal;
			float num2 = (0.27f + _randomGenerator.NextFloatRanged(0f, 0.3f)) * 0.032f;
			Vec3 vec3 = record.Speed + vec2 / gameEntity.GetMass();
			float num3 = vec3.Normalize();
			vec3 = vec3.RotateAboutAnArbitraryVector(record.AverageNormal, _randomGenerator.NextFloatRanged(-System.MathF.PI / 2f, System.MathF.PI / 2f));
			num3 *= num2;
			num3 = TaleWorlds.Library.MathF.Min(num3, 30f);
			vec3 = (vec3 + Vec3.Up * 0.75f).NormalizedCopy() * num3;
			gameEntity.SetLinearVelocity(vec3);
			foreach (Mesh item in gameEntity.GetAllMeshesWithTag("auto_factor_color"))
			{
				item.Color = _shipColor;
			}
			SetRandomAngularVelocityToEntity(gameEntity);
			if (_floatsamMissionLogic != null)
			{
				_floatsamMissionLogic.RegisterFloatsamInstance(gameEntity);
			}
		}
		if (_collisionDecals.Count >= 30)
		{
			return;
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 origin = record.ShipLocalPosition;
		Vec3 u = record.ShipLocalNormal;
		if (_bodyEntity != null)
		{
			float num4 = 2.5f;
			Vec3 vec4 = -globalFrame.rotation.TransformToParent(in record.ShipLocalNormal).NormalizedCopy();
			Vec3 vec5 = globalFrame.TransformToParent(in record.ShipLocalPosition) - vec4 * num4;
			Vec3 resultNormal = Vec3.Zero;
			float resultLength = 0f;
			if (_bodyEntity.RayHitEntityWithNormal(vec5, vec4, num4, ref resultNormal, ref resultLength))
			{
				origin = globalFrame.TransformToLocalNonOrthogonal(vec5 + vec4 * resultLength);
				u = globalFrame.rotation.TransformToLocal(in resultNormal).NormalizedCopy();
			}
		}
		MatrixFrame frame2 = MatrixFrame.Identity;
		frame2.origin = origin;
		frame2.rotation.u = u;
		frame2.rotation.f = Vec3.Up;
		frame2.rotation.s = Vec3.CrossProduct(frame2.rotation.u, frame2.rotation.s);
		frame2.rotation.f.Normalize();
		frame2.rotation.s = Vec3.CrossProduct(frame2.rotation.f, frame2.rotation.u);
		if (record.DecalType == DecalType.Scrape)
		{
			float x = _randomGenerator.NextFloatRanged(1.75f, 2.75f);
			float y = _randomGenerator.NextFloatRanged(1.25f, 1.75f);
			frame2.rotation.ApplyScaleLocal(new Vec3(x, y, 0.2f));
		}
		else if (record.DecalType == DecalType.Collision)
		{
			float x2 = _randomGenerator.NextFloatRanged(1.55f, 2.55f);
			frame2.rotation.ApplyScaleLocal(new Vec3(x2, 1f, 0.2f));
		}
		string prefabName = "";
		if (record.DecalType == DecalType.Collision)
		{
			prefabName = GetRandomCollisionDecalPrefab();
		}
		else if (record.DecalType == DecalType.Scrape)
		{
			prefabName = GetRandomScrapeDecalPrefab();
		}
		GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName, MatrixFrame.Identity);
		base.GameEntity.AddChild(gameEntity2.WeakEntity);
		gameEntity2.SetFrame(ref frame2);
		gameEntity2.SetFactorColor(_shipDecalColor);
		_collisionDecals.Add(gameEntity2);
	}

	private string GetRandomDebrisPrefab(DebrisType type)
	{
		switch (type)
		{
		case DebrisType.Generic:
		{
			int num3 = _randomGenerator.Next(GenericPrefabNames.Length);
			return GenericPrefabNames[num3];
		}
		case DebrisType.Scrape:
		{
			int num2 = _randomGenerator.Next(ScrapeDebrisPrefabNames.Length);
			return ScrapeDebrisPrefabNames[num2];
		}
		case DebrisType.Ramming:
		{
			int num = _randomGenerator.Next(RammingPrefabNames.Length);
			return RammingPrefabNames[num];
		}
		default:
			return "";
		}
	}

	private string GetRandomCollisionDecalPrefab()
	{
		int num = _randomGenerator.Next(CollisionDecalPrefabNames.Length);
		return CollisionDecalPrefabNames[num];
	}

	private string GetRandomScrapeDecalPrefab()
	{
		int num = _randomGenerator.Next(ScrapeDecalPrefabNames.Length);
		return ScrapeDecalPrefabNames[num];
	}

	private void SetRandomAngularVelocityToEntity(GameEntity entity)
	{
		float num = 0.8f;
		entity.SetAngularVelocity(new Vec3(_randomGenerator.NextFloatRanged(0f - num, num), _randomGenerator.NextFloatRanged(0f - num, num), _randomGenerator.NextFloatRanged(0f - num, num)));
	}

	private void CheckSinking()
	{
		if (_sinkingFloatsamSpawned || _ownMissionShipCached.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating)
		{
			return;
		}
		Vec3 globalPosition = base.GameEntity.GlobalPosition;
		BoundingBox physicsBoundingBoxWithoutChildren = _ownMissionShipCached.Physics.PhysicsBoundingBoxWithoutChildren;
		float num = (physicsBoundingBoxWithoutChildren.max.z - physicsBoundingBoxWithoutChildren.min.z) * 0.75f;
		if (!(globalPosition.z + num < base.GameEntity.GetWaterLevelAtPosition(globalPosition.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false)))
		{
			return;
		}
		Vec3 min = physicsBoundingBoxWithoutChildren.min;
		Vec3 max = physicsBoundingBoxWithoutChildren.max;
		max.z = min.z;
		Vec3 vec = max - min;
		float num2 = TaleWorlds.Library.MathF.Max(Vec2.DotProduct(vec.AsVec2, vec.AsVec2) / 1000f, 1f);
		_sinkingFloatsamSpawned = true;
		int num3 = (int)((float)_randomGenerator.Next(7, 10) * num2);
		for (int i = 0; i < num3; i++)
		{
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, "floatable_debris_oar_a", callScriptCallbacks: true);
			if (gameEntity != null)
			{
				Vec3 vec2 = min + new Vec3(vec.x * _randomGenerator.NextFloat(), vec.y * _randomGenerator.NextFloat());
				MatrixFrame frame = MatrixFrame.Identity;
				frame.origin = globalPosition + vec2;
				float waterLevelAtPosition = base.GameEntity.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
				frame.origin.z = waterLevelAtPosition - 1.5f * _randomGenerator.NextFloatRanged(1f, 4.5f);
				gameEntity.SetFrame(ref frame);
				gameEntity.SetFactorColor(_shipColor);
				SetRandomAngularVelocityToEntity(gameEntity);
				if (_floatsamMissionLogic != null)
				{
					_floatsamMissionLogic.RegisterFloatsamInstance(gameEntity);
				}
			}
		}
		Vec3 vec3 = min + new Vec3(vec.x * _randomGenerator.NextFloat(), vec.y * _randomGenerator.NextFloat());
		GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, "floatable_debris_rudder", callScriptCallbacks: true);
		MatrixFrame frame2 = MatrixFrame.Identity;
		frame2.origin = globalPosition + vec3;
		float waterLevelAtPosition2 = base.GameEntity.GetWaterLevelAtPosition(frame2.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		frame2.origin.z = waterLevelAtPosition2 - 1.5f * _randomGenerator.NextFloatRanged(1f, 4.5f);
		gameEntity2.SetFrame(ref frame2);
		gameEntity2.SetFactorColor(_shipColor);
		SetRandomAngularVelocityToEntity(gameEntity2);
		if (_floatsamMissionLogic != null)
		{
			_floatsamMissionLogic.RegisterFloatsamInstance(gameEntity2);
		}
		GameEntity gameEntity3 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, "floatable_debris_mast", callScriptCallbacks: true);
		if (gameEntity3 != null)
		{
			Vec3 vec4 = min + new Vec3(vec.x * _randomGenerator.NextFloat(), vec.y * _randomGenerator.NextFloat());
			MatrixFrame frame3 = MatrixFrame.Identity;
			frame3.origin = globalPosition + vec4;
			float waterLevelAtPosition3 = base.GameEntity.GetWaterLevelAtPosition(frame3.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			frame3.origin.z = waterLevelAtPosition3 - 1.5f * _randomGenerator.NextFloatRanged(3.5f, 5.5f);
			gameEntity3.SetFrame(ref frame3);
			gameEntity3.SetFactorColor(_shipColor);
			SetRandomAngularVelocityToEntity(gameEntity3);
			if (_floatsamMissionLogic != null)
			{
				_floatsamMissionLogic.RegisterFloatsamInstance(gameEntity3);
			}
		}
		int num4 = (int)((float)_randomGenerator.Next(10, 16) * num2);
		for (int j = 0; j < num4; j++)
		{
			Vec3 vec5 = min + new Vec3(vec.x * _randomGenerator.NextFloat(), vec.y * _randomGenerator.NextFloat());
			GameEntity gameEntity4 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, GetRandomDebrisPrefab(DebrisType.Generic), callScriptCallbacks: true);
			MatrixFrame frame4 = MatrixFrame.Identity;
			frame4.origin = globalPosition + vec5;
			float waterLevelAtPosition4 = base.GameEntity.GetWaterLevelAtPosition(frame4.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			frame4.origin.z = waterLevelAtPosition4 - 1.5f * _randomGenerator.NextFloatRanged(1f, 4.5f);
			gameEntity4.SetFrame(ref frame4);
			gameEntity4.SetFactorColor(_shipColor);
			SetRandomAngularVelocityToEntity(gameEntity4);
			if (_floatsamMissionLogic != null)
			{
				_floatsamMissionLogic.RegisterFloatsamInstance(gameEntity4);
			}
		}
	}

	private void OnShieldDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		if (!_floatsamSystemEnabled || _numberOfPendingShieldBreakRecords >= 10)
		{
			return;
		}
		Texture bannerTexture = null;
		MetaMesh metaMesh = target.GameEntity.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.MetaMesh) as MetaMesh;
		if (metaMesh != null && metaMesh.MeshCount > 0)
		{
			bannerTexture = metaMesh.GetMeshAtIndex(0).GetMaterial().GetTexture(Material.MBTextureType.DiffuseMap2);
		}
		string text = "floatable_debris_";
		text += _shieldName;
		if (_randomGenerator.NextFloat() > 0.15f)
		{
			switch (_randomGenerator.Next(0, 3))
			{
			case 0:
				text += "_broken_a";
				break;
			case 1:
				text += "_broken_b";
				break;
			case 2:
				text += "_broken_c";
				break;
			}
		}
		Vec3 linearVelocity = target.GameEntity.Root.GetLinearVelocity();
		linearVelocity += Vec3.Up * 1.5f;
		_shieldBreakRecords[_numberOfPendingShieldBreakRecords].ShipLocalSpawnFrame = base.GameEntity.GetGlobalFrame().TransformToLocal(target.GameEntity.GetGlobalFrame());
		_shieldBreakRecords[_numberOfPendingShieldBreakRecords].BannerTexture = bannerTexture;
		_shieldBreakRecords[_numberOfPendingShieldBreakRecords].LinearVelocity = linearVelocity;
		_shieldBreakRecords[_numberOfPendingShieldBreakRecords].PrefabName = text;
		_numberOfPendingShieldBreakRecords++;
	}

	private void OnShipRamming(MissionShip rammingShip, MissionShip rammedShip, float damagePercent, bool isFirstImpact, CapsuleData capsuleData, int ramQuality)
	{
		if (isFirstImpact && rammedShip == _ownMissionShipCached)
		{
			Vec3 v = rammingShip.Physics.LinearVelocity;
			Vec3 vec = v.NormalizedCopy();
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AveragePosition = capsuleData.P2 + new Vec3(0f, 0f, 1f);
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AverageNormal = (-vec + new Vec3(0f, 0f, 1.75f)).NormalizedCopy();
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].TotalImpulse = (float)(ramQuality + 5) * 150000f;
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].Speed = v * 2f;
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DebrisType = DebrisType.Ramming;
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DecalType = DecalType.Collision;
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].InitialSpeedMultiplier = 1f;
			MatrixFrame bodyWorldTransform = rammedShip.GameEntity.GetBodyWorldTransform();
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalPosition = bodyWorldTransform.TransformToLocal(capsuleData.P2);
			_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalNormal = bodyWorldTransform.rotation.TransformToLocal(in v);
			_numberOfPendingImpulseRecords++;
		}
	}

	private void OnShipHit(MissionShip ship, Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, MissionWeapon weapon, int missileIndex)
	{
		if (!_floatsamSystemEnabled || ship != _ownMissionShipCached || weapon.CurrentUsageItem == null)
		{
			return;
		}
		WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;
		if ((weaponClass != WeaponClass.Boulder && weaponClass != WeaponClass.Stone && weaponClass != WeaponClass.BallistaBoulder && weaponClass != WeaponClass.BallistaStone) || _numberOfPendingImpulseRecords >= 10)
		{
			return;
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 vec = -impactDirection;
		Vec3 v = vec;
		if (_bodyEntity != null)
		{
			Vec3 resultNormal = Vec3.Zero;
			float resultLength = 0f;
			if (_bodyEntity.RayHitEntityWithNormal(impactPosition - impactDirection, impactDirection.NormalizedCopy(), 2f, ref resultNormal, ref resultLength))
			{
				vec = resultNormal;
				v = resultNormal;
				vec.Normalize();
			}
		}
		int particleId = ((!weapon.Item.StringId.Contains("grape")) ? _midCollisionHitParticleIndex : _collisionHitParticleIndex);
		MatrixFrame identity = MatrixFrame.Identity;
		identity.rotation.u = Vec3.Up;
		identity.rotation.s = vec;
		identity.rotation.f = -globalFrame.rotation.s.CrossProductWithUp();
		identity.rotation.s = Vec3.CrossProduct(globalFrame.rotation.f, globalFrame.rotation.u);
		identity.origin = impactPosition;
		base.GameEntity.Scene.CreateBurstParticle(particleId, identity);
		Vec3 speed = Vec3.Zero;
		if (base.GameEntity.HasDynamicRigidBody())
		{
			speed = base.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(impactPosition);
		}
		float num = (float)damage / 150f;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AveragePosition = impactPosition;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].AverageNormal = vec;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].TotalImpulse = 150000f * num;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].Speed = speed;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DebrisType = DebrisType.Scrape;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].DecalType = DecalType.Collision;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].InitialSpeedMultiplier = 1f;
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalPosition = globalFrame.TransformToLocal(in impactPosition);
		_impulseRecordsToProcess[_numberOfPendingImpulseRecords].ShipLocalNormal = globalFrame.rotation.TransformToLocal(in v);
		_numberOfPendingImpulseRecords++;
	}

	public void EnableFloatsamSystem()
	{
		_floatsamSystemEnabled = true;
	}
}
