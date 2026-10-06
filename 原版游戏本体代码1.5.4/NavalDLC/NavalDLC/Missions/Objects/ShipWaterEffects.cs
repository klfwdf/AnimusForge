using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "ship_water_effects")]
public class ShipWaterEffects : ScriptComponentBehavior
{
	internal enum ParticleType
	{
		Movement,
		Splash
	}

	internal enum MovementParticleType
	{
		Small,
		Medium,
		Large
	}

	internal enum ShipHullHeightType
	{
		Small,
		Medium,
		Large
	}

	internal enum ResolutionScale
	{
		one,
		half,
		quarter,
		one_eight,
		one_sixteenth
	}

	private struct FloaterData
	{
		internal float HeightMin;

		internal float VerticalLength;

		internal float HorizontalArea;
	}

	private class WetnessDecalData
	{
		internal Decal Decal;

		internal Vec3 Normal;

		internal Vec3 LocalPosition;

		internal float CurrentAlpha;
	}

	private struct SliceSampleData
	{
		internal Vec3 localPosition;

		internal Vec3 limitingUpVector;
	}

	private class ParticleData
	{
		internal ParticleSystem MovementParticleSystem;

		internal MatrixFrame LocalFrame = MatrixFrame.Identity;

		internal Vec3 SurfaceNormal = Vec3.Zero;

		internal ParticleSystem CurrentSplashParticle;

		internal float SplashTimer;

		internal float LastSpawnTime;

		internal bool WasAboveWater = true;

		internal Vec3 SplashVelocity = Vec3.Zero;

		internal Vec3 SplashPosition = Vec3.Zero;

		internal float SplashWaterMultiplier;

		internal List<KeyValuePair<float, SliceSampleData>> PerSlicePositions;

		internal float Size;
	}

	private class SplashFoamDecal
	{
		internal Decal _splashFoamDecal;

		internal MatrixFrame _currentFrame;

		internal float _cumulativeDtTillStart;

		internal Vec3 _randomScale;

		internal Vec3 _currentSpeed;

		internal Vec3 _sideVectorStart;

		internal Vec3 _sideVectorEnd;

		internal bool _isLeft;

		internal SplashFoamDecal()
		{
			_splashFoamDecal = null;
			_currentFrame = MatrixFrame.Identity;
			_sideVectorStart = Vec3.Zero;
			_sideVectorEnd = Vec3.Zero;
			_cumulativeDtTillStart = 0f;
			_randomScale = new Vec3(1f, 1f, 1f);
			_currentSpeed = Vec3.Zero;
			_isLeft = false;
		}
	}

	private const string FloaterHolderTag = "floater_volume_holder";

	private const string FloaterTag = "floater_volume";

	private const string BodyMeshTag = "body_mesh";

	private const string SplashEntityTag = "splash_particles";

	private const string MovementEntityTag = "movement_particles";

	private const string WaterDepthRenderMeshTag = "render_to_depth";

	private const float ParticleSliceHeightDx = 0.5f;

	private const int NumberOfSplashDecal = 50;

	private const float SmallSplashSoundEventMaxDistanceSquared = 400f;

	private static readonly Comparer<KeyValuePair<float, SliceSampleData>> _cacheCompareDelegate = Comparer<KeyValuePair<float, SliceSampleData>>.Create((KeyValuePair<float, SliceSampleData> x, KeyValuePair<float, SliceSampleData> y) => x.Key.CompareTo(y.Key));

	[EditableScriptComponentVariable(true, "Water Simulation Bounding Box")]
	private Vec3 _waterSimulationBoundingBox = Vec3.One;

	[EditableScriptComponentVariable(true, "Show Water Simulation Bounding Box")]
	private bool _showWaterSimulationBoundingBox;

	[EditableScriptComponentVariable(true, "Reset Water Simulation Bounding Box")]
	private SimpleButton _resetWaterSimulationBoundingBox = new SimpleButton();

	[EditableScriptComponentVariable(true, "Re-render Depth Texture")]
	private SimpleButton _reRenderDepthTexture = new SimpleButton();

	[EditableScriptComponentVariable(true, "Reset In-Hull Water")]
	private SimpleButton _resetInHullWater = new SimpleButton();

	[EditableScriptComponentVariable(true, "Show Hull Water Debug Panel")]
	private bool _showHullWaterDebugPanel;

	[EditableScriptComponentVariable(true, "Hull Water Simulation Resolution Scale")]
	private ResolutionScale _hullWaterResScale = ResolutionScale.half;

	[EditableScriptComponentVariable(true, "Hull Water Splash Water Multiplier")]
	private float _hullWaterSplashWaterMultiplier = 1.75f;

	[EditableScriptComponentVariable(true, "Hull Water Splash Point Initial Offset")]
	private float _hullWaterSplashPointInitialOffset = 0.5f;

	[EditableScriptComponentVariable(true, "Hull Water Splash Point Speed Multiplier")]
	private float _hullWaterSplashPointSpeedMultiplier = 1f;

	[EditableScriptComponentVariable(true, "Ship Hull Height Type")]
	private ShipHullHeightType _shipHullHeightType;

	[EditableScriptComponentVariable(true, "Movement Particle Height Offset")]
	private float _movementParticleHeightOffset = 0.34f;

	[EditableScriptComponentVariable(true, "Splash Particle Height Offset")]
	private float _splashParticleHeightOffset = 0.4f;

	[EditableScriptComponentVariable(true, "Movement Particle Surface Distance Offset")]
	private float _movementParticleSurfaceDistanceOffset = 0.7f;

	[EditableScriptComponentVariable(true, "Splash Particle Surface Distance Offset")]
	private float _splashParticleSurfaceDistanceOffset = 0.7f;

	[EditableScriptComponentVariable(true, "Movement Particle Type")]
	private MovementParticleType _movementParticleType;

	[EditableScriptComponentVariable(true, "Movement Particle Side Speed Vector")]
	private float _movementParticleSideSpeedVector = 0.5f;

	[EditableScriptComponentVariable(true, "Show Movement Particles")]
	private bool _showMovementParticles;

	[EditableScriptComponentVariable(true, "Show Splash Particles")]
	private bool _showSplashParticles;

	[EditableScriptComponentVariable(true, "Show Water Balance Plane")]
	private bool _showWaterBalancePlane;

	[EditableScriptComponentVariable(true, "Show Wetness Decal Values")]
	private bool _showWetnessDecalValues;

	[EditableScriptComponentVariable(true, "Force Wetness Decal To Full")]
	private bool _forceWetnessDecalsToFull;

	private UIntPtr _waterVisualRecord = UIntPtr.Zero;

	private GameEntity _movementParticleEntity;

	private GameEntity _splashParticleEntity;

	private readonly List<ParticleData> _movementParticles = new List<ParticleData>();

	private readonly List<ParticleData> _splashParticles = new List<ParticleData>();

	private readonly MBFastRandom _splashRandom = new MBFastRandom();

	private readonly List<WetnessDecalData> _wetnessDecals = new List<WetnessDecalData>();

	private MatrixFrame _previousShipFrame = MatrixFrame.Identity;

	private float _cumulativeDt;

	private bool _inCampaignMode;

	private Scene _ownerSceneCached;

	private int _smallSplashParticleIndex = -1;

	private int _mediumSplashParticleIndex = -1;

	private int _largeSplashParticleIndex = -1;

	private bool _hullLocalFramesSetForMission;

	private bool _wakeAndParticlesEnabled;

	private BoundingBox _bodyBB;

	private readonly SplashFoamDecal[] _splashFoamDecals = new SplashFoamDecal[50];

	private int _nextDecalToUse;

	private Vec3 _lastDecalLeftSpawnPosition = Vec3.Zero;

	private Vec3 _lastDecalRightSpawnPosition = Vec3.Zero;

	private float _nextDecalLeftSpawnMetersSq = 49f;

	private float _nextDecalRightSpawnMetersSq = 49f;

	private Vec3 _previousShipFrameForDecalSpawn = Vec3.Zero;

	private int _leftDecalParticleIndex = -1;

	private int _rightDecalParticleIndex = -1;

	public void DummyFunc()
	{
		Debug.Print(_showWaterSimulationBoundingBox.ToString());
		Debug.Print(_movementParticleHeightOffset.ToString());
		Debug.Print(_splashParticleHeightOffset.ToString());
		Debug.Print(_showMovementParticles.ToString());
		Debug.Print(_showSplashParticles.ToString());
		Debug.Print(_showHullWaterDebugPanel.ToString());
		Debug.Print(_hullWaterResScale.ToString());
		Debug.Print(_showWaterBalancePlane.ToString());
		Debug.Print(_movementParticleSideSpeedVector.ToString());
		Debug.Print(_showWetnessDecalValues.ToString());
		Debug.Print(_forceWetnessDecalsToFull.ToString());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | TickRequirement.TickParallel;
	}

	protected override void OnInit()
	{
		base.OnInit();
		_showMovementParticles = false;
		_showSplashParticles = false;
		_movementParticleEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("movement_particles"));
		_splashParticleEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("splash_particles"));
		if (_splashParticleEntity != null)
		{
			foreach (GameEntity item in _splashParticleEntity.GetChildren().ToList())
			{
				item.Remove(23);
			}
		}
		if (_movementParticleEntity != null)
		{
			foreach (GameEntity item2 in _movementParticleEntity.GetChildren().ToList())
			{
				item2.Remove(23);
			}
		}
		_inCampaignMode = base.GameEntity.Scene.GetName() == "Main_map";
		_ownerSceneCached = base.GameEntity.Scene;
		FetchEntities();
		if (!_inCampaignMode)
		{
			if (_wakeAndParticlesEnabled)
			{
				float num = 0f;
				NavalDLC.Missions.NavalPhysics.NavalPhysics firstScriptOfType = base.GameEntity.Root.GetFirstScriptOfType<NavalDLC.Missions.NavalPhysics.NavalPhysics>();
				if (firstScriptOfType != null)
				{
					num = firstScriptOfType.StabilitySubmergedHeightOfShip;
				}
				PlaceParticles(ParticleType.Splash, num + _splashParticleHeightOffset);
				PlaceParticles(ParticleType.Movement, num + _movementParticleHeightOffset);
				if (_waterVisualRecord == UIntPtr.Zero)
				{
					CheckWaterVisualRegistry();
				}
			}
			_largeSplashParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_naval_ship_water_splash_large");
			_mediumSplashParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_naval_ship_water_splash_mid");
			_smallSplashParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_naval_ship_water_splash_small");
			if (_ownerSceneCached.HasDecalRenderer())
			{
				for (int i = 0; i < 50; i++)
				{
					_splashFoamDecals[i] = new SplashFoamDecal();
				}
			}
			WeakGameEntity parent = base.GameEntity.Parent;
			_wetnessDecals.Clear();
			MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
			if (parent != null && parent.Scene != null)
			{
				foreach (WeakGameEntity child in parent.GetFirstChildEntityWithTag("wetness_decals").GetChildren())
				{
					Decal decal = child.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.Decal) as Decal;
					if (decal != null)
					{
						WetnessDecalData wetnessDecalData = new WetnessDecalData();
						wetnessDecalData.Decal = decal;
						decal.CheckAndRegisterToDecalSet();
						wetnessDecalData.CurrentAlpha = 0f;
						wetnessDecalData.Normal = globalFrame.rotation.TransformToLocal(child.GetLocalFrame().rotation.u.NormalizedCopy());
						MatrixFrame globalFrame2 = child.GetGlobalFrame();
						wetnessDecalData.LocalPosition = globalFrame.TransformToLocalNonOrthogonal(in globalFrame2.origin);
						_wetnessDecals.Add(wetnessDecalData);
					}
				}
			}
		}
		ComputeWakeCapsuleParameters();
		_previousShipFrame = base.GameEntity.Root.GetGlobalFrame();
	}

	protected override void OnTick(float dt)
	{
		if (!_inCampaignMode && _waterVisualRecord == UIntPtr.Zero && _wakeAndParticlesEnabled)
		{
			CheckWaterVisualRegistry();
			ComputeWakeCapsuleParameters();
		}
	}

	protected override void OnTickParallel(float dt)
	{
		OnMissionTick(dt);
	}

	protected override void OnRemoved(int removeReason)
	{
		if (_waterVisualRecord != UIntPtr.Zero)
		{
			base.GameEntity.Scene.DeRegisterShipVisual(_waterVisualRecord);
		}
		if (!(_ownerSceneCached != null))
		{
			return;
		}
		if (_ownerSceneCached.HasDecalRenderer())
		{
			SplashFoamDecal[] splashFoamDecals = _splashFoamDecals;
			foreach (SplashFoamDecal splashFoamDecal in splashFoamDecals)
			{
				if (splashFoamDecal != null && splashFoamDecal._splashFoamDecal != null)
				{
					_ownerSceneCached.RemoveDecalInstance(splashFoamDecal._splashFoamDecal, "editor_set");
				}
			}
		}
		if (_ownerSceneCached != null)
		{
			_ownerSceneCached.ManualInvalidate();
			_ownerSceneCached = null;
		}
	}

	private void OnMissionTick(float dt)
	{
		if (_waterVisualRecord == UIntPtr.Zero)
		{
			return;
		}
		_cumulativeDt += dt;
		if (!_inCampaignMode)
		{
			if (_wakeAndParticlesEnabled)
			{
				SnapMovementParticlePositionsToWater(dt);
				if (dt > 1E-06f)
				{
					CheckAndSpawnSplashes(dt);
				}
			}
			TickHullWater(dt, fromEditor: false);
			HandleWetnessDecals(dt);
			if (_ownerSceneCached.HasDecalRenderer())
			{
				HandleSplashFoamDecals(dt);
			}
		}
		_previousShipFrame = base.GameEntity.Root.GetGlobalFrame();
	}

	private GameEntity GetParticleParentEntity(ParticleType particleType)
	{
		return particleType switch
		{
			ParticleType.Splash => _splashParticleEntity, 
			ParticleType.Movement => _movementParticleEntity, 
			_ => null, 
		};
	}

	private List<ParticleData> GetParticleDataList(ParticleType particleType)
	{
		return particleType switch
		{
			ParticleType.Splash => _splashParticles, 
			ParticleType.Movement => _movementParticles, 
			_ => null, 
		};
	}

	private ParticleSystem CreateMovementParticle(GameEntity parentEntity, MatrixFrame localFrame)
	{
		return _movementParticleType switch
		{
			MovementParticleType.Small => ParticleSystem.CreateParticleSystemAttachedToEntity("psys_naval_ship_emit_on_move_small", parentEntity, ref localFrame), 
			MovementParticleType.Medium => ParticleSystem.CreateParticleSystemAttachedToEntity("psys_naval_ship_emit_on_move_mid", parentEntity, ref localFrame), 
			MovementParticleType.Large => ParticleSystem.CreateParticleSystemAttachedToEntity("psys_naval_ship_emit_on_move_large", parentEntity, ref localFrame), 
			_ => null, 
		};
	}

	private void RecomputeWaterSimulationBoundingBox()
	{
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		base.GameEntity.Root.GetChildrenWithTagRecursive(list, "render_to_depth");
		BoundingBox boundingBox = default(BoundingBox);
		boundingBox.RecomputeRadius();
		MatrixFrame globalFrame = base.GameEntity.Root.GetGlobalFrame();
		foreach (WeakGameEntity item in list)
		{
			BoundingBox localBoundingBox = item.GetLocalBoundingBox();
			boundingBox.RelaxWithChildBoundingBox(localBoundingBox, globalFrame.TransformToLocalNonOrthogonal(item.GetGlobalFrame()));
		}
		float x = TaleWorlds.Library.MathF.Max(boundingBox.max.x, boundingBox.min.x);
		float y = TaleWorlds.Library.MathF.Max(boundingBox.max.y, boundingBox.min.y);
		float z = TaleWorlds.Library.MathF.Max(boundingBox.max.z, boundingBox.min.z);
		float multiplier = 1f;
		switch (_hullWaterResScale)
		{
		case ResolutionScale.half:
			multiplier = 0.5f;
			break;
		case ResolutionScale.quarter:
			multiplier = 0.25f;
			break;
		case ResolutionScale.one_eight:
			multiplier = 0.125f;
			break;
		case ResolutionScale.one_sixteenth:
			multiplier = 0.0625f;
			break;
		}
		_waterSimulationBoundingBox = new Vec3(x, y, z) * 2f;
		base.GameEntity.ChangeResolutionMultiplierOfWaterVisual(_waterVisualRecord, multiplier, in _waterSimulationBoundingBox);
		base.GameEntity.RefreshMeshesToRenderToHullWater(_waterVisualRecord, "render_to_depth");
	}

	private void FetchEntities()
	{
		_movementParticleEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("movement_particles"));
		_splashParticleEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("splash_particles"));
		if (_movementParticleEntity != null)
		{
			_movementParticleEntity.EntityFlags |= EntityFlags.DontSaveToScene;
		}
		else
		{
			_movementParticleEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene);
			_movementParticleEntity.Name = "movement_parent";
			_movementParticleEntity.AddTag("movement_particles");
			base.GameEntity.AddChild(_movementParticleEntity.WeakEntity);
			MatrixFrame frame = MatrixFrame.Identity;
			_movementParticleEntity.SetFrame(ref frame);
		}
		if (_splashParticleEntity != null)
		{
			_splashParticleEntity.EntityFlags |= EntityFlags.DontSaveToScene;
		}
		else
		{
			_splashParticleEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(base.GameEntity.Scene);
			_splashParticleEntity.Name = "movement_parent";
			_splashParticleEntity.AddTag("splash_particles");
			base.GameEntity.AddChild(_splashParticleEntity.WeakEntity);
			MatrixFrame frame2 = MatrixFrame.Identity;
			_splashParticleEntity.SetFrame(ref frame2);
		}
		MatrixFrame frame3 = MatrixFrame.Identity;
		_movementParticleEntity.SetLocalFrame(ref frame3, isTeleportation: true);
		_splashParticleEntity.SetLocalFrame(ref frame3, isTeleportation: true);
	}

	private void ComputeWakeCapsuleParameters()
	{
		if (_waterVisualRecord == UIntPtr.Zero)
		{
			return;
		}
		WeakGameEntity firstChildEntityWithTagRecursive = base.GameEntity.Root.GetFirstChildEntityWithTagRecursive("body_mesh");
		if (firstChildEntityWithTagRecursive.IsValid)
		{
			MatrixFrame globalFrame = base.GameEntity.Root.GetGlobalFrame();
			firstChildEntityWithTagRecursive.ValidateBoundingBox();
			BoundingBox globalBoundingBox = firstChildEntityWithTagRecursive.GetGlobalBoundingBox();
			_bodyBB = firstChildEntityWithTagRecursive.GetLocalBoundingBox();
			float num = globalBoundingBox.radius + 1f;
			Vec3 center = globalBoundingBox.center;
			center.z = MBMath.Lerp(center.z, globalBoundingBox.min.z, 0.5f);
			Vec3 vec = -globalFrame.rotation.f;
			Vec3 f = globalFrame.rotation.f;
			Vec3 s = globalFrame.rotation.s;
			Vec3 vec2 = -globalFrame.rotation.s;
			Vec3 vec3 = center - vec * num;
			Vec3 vec4 = center - f * num;
			Vec3 vec5 = center - s * num;
			Vec3 vec6 = center - vec2 * num;
			float resultLength = 0f;
			float resultLength2 = 0f;
			bool num2 = firstChildEntityWithTagRecursive.RayHitEntity(vec3, vec, num * 2f, ref resultLength);
			bool flag = firstChildEntityWithTagRecursive.RayHitEntity(vec4, f, num * 2f, ref resultLength2);
			float resultLength3 = 0f;
			float resultLength4 = 0f;
			bool flag2 = firstChildEntityWithTagRecursive.RayHitEntity(vec5, s, num * 2f, ref resultLength3);
			bool flag3 = firstChildEntityWithTagRecursive.RayHitEntity(vec6, vec2, num * 2f, ref resultLength4);
			if (num2 && flag && flag2 && flag3)
			{
				float x = center.Distance(vec3 + vec * (resultLength + 4.5f));
				float y = center.Distance(vec4 + f * resultLength2);
				float z = center.Distance(vec5 + s * resultLength3);
				float w = center.Distance(vec6 + vec2 * resultLength4);
				base.GameEntity.SetVisualRecordWakeParams(_waterVisualRecord, new Vec3(x, y, z, w));
			}
		}
	}

	private bool RayCastToEntities(List<WeakGameEntity> rayCastEntities, Vec3 rayStart, Vec3 rayDirection, float maxLength, ref float resultLength, ref Vec3 surfaceNormal)
	{
		bool result = false;
		resultLength = maxLength;
		foreach (WeakGameEntity rayCastEntity in rayCastEntities)
		{
			float resultLength2 = maxLength;
			if (rayCastEntity.RayHitEntityWithNormal(rayStart, rayDirection, maxLength, ref surfaceNormal, ref resultLength2) && resultLength2 < resultLength)
			{
				result = true;
				resultLength = resultLength2;
			}
		}
		return result;
	}

	private void PlaceParticles(ParticleType particleType, float waterLineHeight)
	{
		GameEntity particleParentEntity = GetParticleParentEntity(particleType);
		if (particleParentEntity == null)
		{
			return;
		}
		MatrixFrame globalFrame = particleParentEntity.GetGlobalFrame();
		List<ParticleData> particleDataList = GetParticleDataList(particleType);
		foreach (ParticleData item in particleDataList)
		{
			if (item.MovementParticleSystem != null)
			{
				particleParentEntity.RemoveComponent(item.MovementParticleSystem);
			}
		}
		particleDataList.Clear();
		WeakGameEntity root = base.GameEntity.Root;
		WeakGameEntity firstChildEntityWithTagRecursive = root.GetFirstChildEntityWithTagRecursive("body_mesh");
		if (!firstChildEntityWithTagRecursive.IsValid)
		{
			return;
		}
		MatrixFrame globalFrame2 = root.GetGlobalFrame();
		BoundingBox boundingBox = (firstChildEntityWithTagRecursive.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.MetaMesh) as MetaMesh).GetBoundingBox();
		float radius = boundingBox.radius;
		Vec3 center = boundingBox.center;
		center.z = waterLineHeight;
		Vec3 vec = boundingBox.max - boundingBox.min;
		Vec3 v = vec;
		float num = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Min(v.x, v.y), v.z);
		if (num > 0f)
		{
			v /= num;
		}
		v = Vec3.Lerp(v, Vec3.One, 0.5f);
		float num2 = ((particleType == ParticleType.Splash) ? _splashParticleSurfaceDistanceOffset : _movementParticleSurfaceDistanceOffset);
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		list.Add(firstChildEntityWithTagRecursive);
		float num3 = 0f;
		WeakGameEntity parent = base.GameEntity.Parent;
		if (parent != null)
		{
			foreach (WeakGameEntity child2 in parent.GetChildren())
			{
				if (child2.ChildCount <= 0)
				{
					continue;
				}
				WeakGameEntity child = child2.GetChild(0);
				if (!child.HasTag("bow"))
				{
					continue;
				}
				foreach (WeakGameEntity child3 in child.GetChildren())
				{
					if (child3.IsVisibleIncludeParents())
					{
						MissionShipRam firstScriptOfType = child3.GetFirstScriptOfType<MissionShipRam>();
						if (firstScriptOfType != null)
						{
							num3 = TaleWorlds.Library.MathF.Max(firstScriptOfType.RamLength, num3);
						}
					}
				}
				break;
			}
		}
		float num4 = 0f;
		int num5 = 5;
		for (int i = 0; i < num5; i++)
		{
			float resultLength = 0f;
			Vec3 surfaceNormal = Vec3.Zero;
			Vec3 v2 = new Vec3(0f, 1f) * vec.y;
			v2.z = waterLineHeight - 0.5f + (float)i * 0.2f;
			Vec3 v3 = new Vec3(0f, -1f);
			v2 = globalFrame2.TransformToParent(in v2);
			v3 = globalFrame2.rotation.TransformToParent(in v3);
			v3.Normalize();
			if (RayCastToEntities(list, v2, v3, radius * 8f, ref resultLength, ref surfaceNormal))
			{
				num4 = TaleWorlds.Library.MathF.Max(num4, vec.y - resultLength);
			}
		}
		num4 += num3;
		float num6 = 0f;
		int num7 = 5;
		for (int j = 0; j < num7; j++)
		{
			float resultLength2 = 0f;
			Vec3 surfaceNormal2 = Vec3.Zero;
			Vec3 v4 = new Vec3(0f, -1f) * vec.y;
			v4.z = waterLineHeight - 0.5f + (float)j * 0.2f;
			Vec3 v5 = new Vec3(0f, 1f);
			v4 = globalFrame2.TransformToParent(in v4);
			v5 = globalFrame2.rotation.TransformToParent(in v5);
			v5.Normalize();
			if (RayCastToEntities(list, v4, v5, radius * 8f, ref resultLength2, ref surfaceNormal2))
			{
				num6 = TaleWorlds.Library.MathF.Max(num6, vec.y - resultLength2);
			}
		}
		int num8 = 0;
		float num9 = num4 + num6;
		float num10 = 1f;
		int num11 = (int)(vec.y / 5.5f);
		if (particleType == ParticleType.Movement)
		{
			num8 = num11 * 2 + 1;
		}
		else
		{
			float num12 = num9 - 3f;
			num8 = (int)(num12 / num10);
			num10 = num12 / (float)num8;
			num8 *= 2;
		}
		int num13 = num8 / 2;
		int num14 = 0;
		int num15 = 0;
		for (int k = 0; k < num8; k++)
		{
			bool flag = false;
			bool flag2 = false;
			Vec3 v6 = new Vec3(0f, 0f, 0f, -1f);
			Vec3 v7;
			if (particleType == ParticleType.Splash)
			{
				float num16 = ((k >= num13) ? (-1f) : 1f);
				int num17 = k % num13;
				float y = num4 - 1.5f - (float)num17 * num10;
				v6.x = vec.x * 2f * num16;
				v6.y = y;
				v6.z = center.z;
				v7 = new Vec3(0f - num16);
			}
			else if (k == 0)
			{
				v6.x = 0f;
				v6.y = num4 + 4f;
				v6.z = center.z;
				v7 = new Vec3(0f, -1f);
			}
			else
			{
				float num18 = ((k - 1 >= num11) ? (-1f) : 1f);
				int num19 = (k - 1) % num11;
				float y2 = num4 - (0.7f + (float)num19) * 2.05f;
				v6.x = vec.x * 2f * num18;
				v6.y = y2;
				v6.z = center.z;
				v7 = new Vec3(0f - num18);
				flag = num19 == num14 && num18 == -1f;
				flag2 = num19 == num15 && num18 == 1f;
			}
			Vec3 vec2 = v6;
			v6 = globalFrame2.TransformToParent(in v6);
			v7 = globalFrame2.rotation.TransformToParent(in v7);
			v7.Normalize();
			float resultLength3 = 0f;
			Vec3 surfaceNormal3 = Vec3.Zero;
			int num20 = 5;
			bool flag3 = false;
			while (!flag3 && num20 > 0)
			{
				flag3 = RayCastToEntities(list, v6, v7, radius * 8f, ref resultLength3, ref surfaceNormal3);
				if (!flag3)
				{
					v6.z += 0.05f;
				}
				num20--;
			}
			if (flag3)
			{
				Vec3 vec3 = -v7;
				if (particleType == ParticleType.Movement && k == 0)
				{
					resultLength3 -= num3;
				}
				vec3.z = 0f;
				vec3.Normalize();
				MatrixFrame frame = MatrixFrame.Identity;
				frame.origin = v6 + resultLength3 * v7 + vec3 * num2;
				frame.rotation.s = vec3;
				frame.rotation.u = Vec3.Up;
				frame.rotation.f = -frame.rotation.s.CrossProductWithUp();
				ParticleData particleData = new ParticleData();
				particleData.LocalFrame = globalFrame.TransformToLocalNonOrthogonal(in frame);
				particleData.SurfaceNormal = globalFrame.rotation.TransformToLocal(in surfaceNormal3);
				if (particleType == ParticleType.Movement)
				{
					particleData.MovementParticleSystem = CreateMovementParticle(particleParentEntity, particleData.LocalFrame);
				}
				particleData.LastSpawnTime = 0f;
				if (flag)
				{
					_leftDecalParticleIndex = particleDataList.Count;
				}
				if (flag2)
				{
					_rightDecalParticleIndex = particleDataList.Count;
				}
				particleData.PerSlicePositions = new List<KeyValuePair<float, SliceSampleData>>();
				for (float num21 = boundingBox.min.z; num21 < boundingBox.max.z; num21 += 0.25f)
				{
					Vec3 v8 = vec2;
					v8.z = num21;
					v8 = globalFrame2.TransformToParent(in v8);
					Vec3 surfaceNormal4 = Vec3.Zero;
					float resultLength4 = 0f;
					if (!RayCastToEntities(list, v8, v7, radius * 8f, ref resultLength4, ref surfaceNormal4))
					{
						continue;
					}
					Vec3 v9 = v8 + resultLength4 * v7 + surfaceNormal4 * num2;
					Vec3 localPosition = globalFrame.TransformToLocalNonOrthogonal(in v9);
					Vec3 vec4 = Vec3.Up;
					Vec3 zero = Vec3.Zero;
					Vec3 vec5 = vec3;
					float resultLength5 = 0f;
					if (firstChildEntityWithTagRecursive.RayHitEntity(v9, vec4, 8f, ref resultLength5))
					{
						Vec3 vec6 = (Vec3.Up + vec3) * 0.5f;
						zero = vec3;
						vec5 = vec4;
						vec4 = vec6;
						do
						{
							float resultLength6 = 0f;
							Vec3 surfaceNormal5 = Vec3.Zero;
							if (!RayCastToEntities(list, v9, vec4, 8f, ref resultLength6, ref surfaceNormal5))
							{
								Vec3 vec7 = (vec4 + vec5) * 0.5f;
								zero = vec4;
								vec4 = vec7;
							}
							else
							{
								Vec3 vec8 = (vec4 + zero) * 0.5f;
								vec5 = vec4;
								vec4 = vec8;
							}
						}
						while (!(TaleWorlds.Library.MathF.Abs(TaleWorlds.Library.MathF.Asin(Vec3.CrossProduct(vec5, vec4).Length)) < System.MathF.PI / 60f));
					}
					Vec3 vec9 = Vec3.CrossProduct(vec3, vec4);
					vec9.Normalize();
					vec4 = vec4.RotateAboutAnArbitraryVector(vec9, -0.34906584f);
					Vec3 limitingUpVector = globalFrame.rotation.TransformToLocal(in vec4);
					SliceSampleData value = new SliceSampleData
					{
						localPosition = localPosition,
						limitingUpVector = limitingUpVector
					};
					particleData.PerSlicePositions.Add(new KeyValuePair<float, SliceSampleData>(num21, value));
				}
				particleDataList.Add(particleData);
			}
			else
			{
				if (flag)
				{
					num14++;
				}
				if (flag2)
				{
					num15++;
				}
			}
		}
		if (particleType == ParticleType.Movement && _movementParticles.Count > 0)
		{
			_lastDecalLeftSpawnPosition = globalFrame.TransformToParent(in _movementParticles[0].LocalFrame.origin);
			_lastDecalRightSpawnPosition = _lastDecalLeftSpawnPosition;
			_previousShipFrameForDecalSpawn = base.GameEntity.GetGlobalFrame().origin;
		}
	}

	private float GetFloaterForceMultiplier()
	{
		if (MBObjectManager.Instance != null)
		{
			MBReadOnlyList<MissionShipObject> objects = MBObjectManager.Instance.GetObjects((MissionShipObject x) => x.Prefab == base.GameEntity.Root.Name);
			if (objects.Count > 0)
			{
				return objects[0].FloatingForceMultiplier;
			}
		}
		return 1f;
	}

	private float CalculateWaterBalancePoint()
	{
		WeakGameEntity root = base.GameEntity.Root;
		MatrixFrame globalFrame = root.GetGlobalFrame();
		WeakGameEntity firstChildEntityWithName = root.GetFirstChildEntityWithName("floater_volume_holder");
		if (!firstChildEntityWithName.IsValid)
		{
			return 0f;
		}
		float floaterForceMultiplier = GetFloaterForceMultiplier();
		List<FloaterData> list = new List<FloaterData>();
		float num = 1000f;
		float num2 = -1000f;
		foreach (WeakGameEntity child in firstChildEntityWithName.GetChildren())
		{
			MatrixFrame globalFrame2 = child.GetGlobalFrame();
			MatrixFrame frame = child.GetFrame();
			Vec3 vec = globalFrame.TransformToLocalNonUnit(in globalFrame2.origin);
			Vec3 scaleVector = frame.rotation.GetScaleVector();
			FloaterData item = new FloaterData
			{
				HeightMin = vec.z,
				VerticalLength = scaleVector.z,
				HorizontalArea = scaleVector.x * scaleVector.y
			};
			list.Add(item);
			num = TaleWorlds.Library.MathF.Min(num, item.HeightMin);
			num2 = TaleWorlds.Library.MathF.Max(num2, item.HeightMin + item.VerticalLength);
		}
		float num3 = root.Mass * 9.806f;
		float num4 = 0.01f;
		float num5;
		for (num5 = num; num2 > num5; num5 += num4)
		{
			float num6 = 0f;
			foreach (FloaterData item2 in list)
			{
				if (num5 > item2.HeightMin)
				{
					float num7 = TaleWorlds.Library.MathF.Min(num5 - item2.HeightMin, item2.VerticalLength) * item2.HorizontalArea * 1020f * 9.806f * floaterForceMultiplier;
					num6 += num7;
				}
			}
			if (num6 > num3)
			{
				break;
			}
		}
		return num5;
	}

	private void CheckAndSpawnSplashes(float dt)
	{
		base.GameEntity.GetGlobalWindVelocityOfScene().Normalize();
		base.GameEntity.Root.GetGlobalFrame();
		GameEntity particleParentEntity = GetParticleParentEntity(ParticleType.Splash);
		MatrixFrame globalFrame = particleParentEntity.GetGlobalFrame();
		Vec3 origin = SoundManager.GetListenerFrame().origin;
		foreach (ParticleData splashParticle in _splashParticles)
		{
			if (splashParticle.SplashTimer > 0.001f)
			{
				splashParticle.SplashTimer -= dt;
				continue;
			}
			splashParticle.SplashTimer -= dt;
			if (splashParticle.CurrentSplashParticle != null)
			{
				if (!splashParticle.CurrentSplashParticle.HasAliveParticles())
				{
					if (splashParticle.CurrentSplashParticle.GetEntity() == particleParentEntity)
					{
						particleParentEntity.RemoveComponent(splashParticle.CurrentSplashParticle);
					}
					splashParticle.CurrentSplashParticle = null;
				}
				continue;
			}
			_ = splashParticle.LocalFrame;
			Vec3 zero = Vec3.Zero;
			Vec3 zero2 = Vec3.Zero;
			Vec3 limitingVector = Vec3.Zero;
			Vec3 v = globalFrame.TransformToParent(in splashParticle.LocalFrame.origin);
			float waterLevelAtPosition = base.GameEntity.GetWaterLevelAtPosition(v.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			v.z = waterLevelAtPosition;
			bool pointIsValid = false;
			zero = GetHeightCorrectedPosForSlice(splashParticle, globalFrame.TransformToLocalNonOrthogonal(in v).z, ref pointIsValid, ref limitingVector);
			if (!pointIsValid)
			{
				continue;
			}
			zero2 = globalFrame.TransformToParent(in zero);
			Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = base.GameEntity.Root.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(zero2);
			Vec3 waterSpeedAtPosition = _ownerSceneCached.GetWaterSpeedAtPosition(zero.AsVec2, doChoppinessCorrection: true);
			Vec3 v2 = (splashParticle.SurfaceNormal + splashParticle.LocalFrame.rotation.s) * 0.5f;
			Vec3 v3 = globalFrame.rotation.TransformToParent(in v2);
			Vec3 v4 = linearVelocityAtGlobalPointForEntityWithDynamicBody - waterSpeedAtPosition;
			float num = TaleWorlds.Library.MathF.Max(0f - v4.z, 0f);
			float num2 = TaleWorlds.Library.MathF.Max(Vec3.DotProduct(v3, v4), 0f);
			float num3 = num + num2;
			int num4 = -1;
			float num5 = 0f;
			splashParticle.WasAboveWater = false;
			bool flag = false;
			if (num3 > 8f)
			{
				num4 = _largeSplashParticleIndex;
				num5 = 3f;
			}
			else if (num3 > 5f)
			{
				num4 = _mediumSplashParticleIndex;
				num5 = 2f;
			}
			else
			{
				if (!(num3 > 2f))
				{
					continue;
				}
				num4 = _smallSplashParticleIndex;
				num5 = 1f;
				flag = num3 > 4f;
			}
			MatrixFrame boneLocalFrame = splashParticle.LocalFrame;
			boneLocalFrame.origin = zero;
			ParticleSystem particleSystem = ParticleSystem.CreateParticleSystemAttachedToEntity(num4, particleParentEntity, ref boneLocalFrame);
			particleSystem.SetDontRemoveFromEntity(value: true);
			splashParticle.CurrentSplashParticle = particleSystem;
			splashParticle.LastSpawnTime = _cumulativeDt;
			splashParticle.SplashPosition = splashParticle.PerSlicePositions[splashParticle.PerSlicePositions.Count - 1].Value.localPosition;
			splashParticle.SplashVelocity = -splashParticle.LocalFrame.rotation.s;
			splashParticle.SplashVelocity.Normalize();
			splashParticle.SplashVelocity *= (0.75f + _splashRandom.NextFloat() * 0.5f) * 0.6f;
			splashParticle.SplashPosition -= splashParticle.LocalFrame.rotation.s * _hullWaterSplashPointInitialOffset;
			MatrixFrame globalFrame2 = _previousShipFrame.TransformToParent(in boneLocalFrame);
			Vec3 vec = linearVelocityAtGlobalPointForEntityWithDynamicBody;
			vec.z = TaleWorlds.Library.MathF.Abs(vec.z);
			Vec3 vec2 = globalFrame.rotation.TransformToParent(in limitingVector);
			vec2.z = 0f;
			vec2.Normalize();
			float num6 = TaleWorlds.Library.MathF.Clamp(num3, 3f, 20f);
			if (num4 == _smallSplashParticleIndex)
			{
				num3 *= 1.35f;
			}
			float num7 = num / num3;
			float num8 = num2 / num3;
			Vec3 vec3 = (num7 * 0.75f + 0.25f) * Vec3.Up + vec2 * (num8 * 0.75f + 0.25f);
			vec3.Normalize();
			float amount = TaleWorlds.Library.MathF.Clamp((num6 - 2f) / 8f, 0.01f, 1f);
			float num9 = TaleWorlds.Library.MathF.Lerp(3.5f, 4.5f, amount);
			Vec3 v5 = vec3 * num6 * num9;
			Vec3 vec4 = Vec3.CrossProduct(splashParticle.LocalFrame.rotation.s, limitingVector);
			if (vec4.LengthSquared > 0f)
			{
				vec4.Normalize();
				Vec3 vec5 = globalFrame.rotation.TransformToLocal(in v5);
				Vec3 vec6 = Vec3.DotProduct(vec5, vec4) * vec4;
				Vec3 va = vec5 - vec6;
				Vec3 v6 = Vec3.CrossProduct(va, limitingVector);
				if (v6.LengthSquared > 0f && Vec3.DotProduct(v6, vec4) < 0f)
				{
					va = limitingVector * va.Length;
					vec5 = va + vec6;
					v5 = globalFrame.rotation.TransformToParent(in vec5);
				}
			}
			globalFrame2.origin = zero2 - v5 * dt;
			particleSystem.SetPreviousGlobalFrame(in globalFrame2);
			splashParticle.SplashTimer = num5 * 0.5f;
			if (flag && origin.DistanceSquared(globalFrame2.origin) < 400f)
			{
				SoundManager.StartOneShotEvent("event:/mission/ambient/special/wash_splash_small", in globalFrame2.origin);
			}
			splashParticle.Size = num5;
			if (_splashRandom.NextFloat() < 0.5f * num5)
			{
				splashParticle.SplashWaterMultiplier = (0.5f + 0.5f * _splashRandom.NextFloat()) * 0.53f * num5;
			}
			else
			{
				splashParticle.SplashWaterMultiplier = 0f;
			}
		}
	}

	private void SnapMovementParticlePositionsToWater(float dt)
	{
		float num = 1.5f;
		if (_movementParticleType == MovementParticleType.Small)
		{
			num = 1f;
		}
		MatrixFrame globalFrame = base.GameEntity.Root.GetGlobalFrame();
		bool flag = true;
		foreach (ParticleData movementParticle in _movementParticles)
		{
			if (movementParticle.MovementParticleSystem == null)
			{
				continue;
			}
			Vec3 vec = globalFrame.TransformToParent(in movementParticle.LocalFrame.origin);
			float z = base.GameEntity.GetWaterLevelAtPosition(vec.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) + _movementParticleHeightOffset;
			Vec3 v = vec;
			v.z = z;
			float z2 = globalFrame.TransformToLocal(in v).z;
			bool pointIsValid = false;
			Vec3 limitingVector = Vec3.Zero;
			Vec3 heightCorrectedPosForSlice = GetHeightCorrectedPosForSlice(movementParticle, z2, ref pointIsValid, ref limitingVector);
			if (pointIsValid)
			{
				movementParticle.MovementParticleSystem.SetEnable(enable: true);
				MatrixFrame newLocalFrame = movementParticle.LocalFrame;
				if (!flag)
				{
					newLocalFrame.origin = heightCorrectedPosForSlice;
				}
				movementParticle.MovementParticleSystem.SetLocalFrame(in newLocalFrame);
				float runtimeEmissionRateMultiplier = 1f;
				MatrixFrame matrixFrame = globalFrame.TransformToParent(in newLocalFrame);
				Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = base.GameEntity.Root.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(matrixFrame.origin);
				MatrixFrame globalFrame2 = MatrixFrame.Identity;
				float length = linearVelocityAtGlobalPointForEntityWithDynamicBody.Length;
				Vec3 vec2 = linearVelocityAtGlobalPointForEntityWithDynamicBody;
				vec2.z = 0f;
				globalFrame2.origin = matrixFrame.origin - vec2 * dt * 0.35f * num;
				Vec3 vec3 = globalFrame.rotation.TransformToParent(in limitingVector);
				vec3.Normalize();
				globalFrame2.origin -= length * vec3 * 0.06f * dt;
				if (!flag)
				{
					globalFrame2.origin -= length * matrixFrame.rotation.s * 0.25f * dt * num;
				}
				movementParticle.MovementParticleSystem.SetPreviousGlobalFrame(in globalFrame2);
				flag = false;
				movementParticle.MovementParticleSystem.SetRuntimeEmissionRateMultiplier(runtimeEmissionRateMultiplier);
			}
			else
			{
				movementParticle.MovementParticleSystem.SetEnable(enable: false);
			}
		}
	}

	private void TickHullWater(float dt, bool fromEditor)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		base.GameEntity.SetWaterVisualRecordFrameAndDt(_waterVisualRecord, globalFrame, dt);
		if (fromEditor)
		{
			base.GameEntity.UpdateHullWaterEffectFrames(_waterVisualRecord);
		}
		else if (!_hullLocalFramesSetForMission)
		{
			base.GameEntity.UpdateHullWaterEffectFrames(_waterVisualRecord);
			_hullLocalFramesSetForMission = true;
		}
		MatrixFrame globalFrame2 = GetParticleParentEntity(ParticleType.Splash).GetGlobalFrame();
		float num = 0.1f;
		foreach (ParticleData splashParticle in _splashParticles)
		{
			float num2 = 0.4f * splashParticle.Size;
			float num3 = 1f;
			if (_shipHullHeightType == ShipHullHeightType.Large)
			{
				if (splashParticle.Size == 1f)
				{
					num3 = 0.5f;
				}
				else if (splashParticle.Size == 0f)
				{
					num3 = 0f;
				}
			}
			if (num3 > 0f)
			{
				if (_cumulativeDt - splashParticle.LastSpawnTime > num && _cumulativeDt - splashParticle.LastSpawnTime < num2)
				{
					splashParticle.SplashPosition += splashParticle.SplashVelocity * dt * _hullWaterSplashPointSpeedMultiplier;
					Vec3 position = globalFrame2.TransformToParent(in splashParticle.SplashPosition);
					position.z = 1f;
					position.w = splashParticle.SplashWaterMultiplier * _hullWaterSplashWaterMultiplier * 2.75f * num3;
					base.GameEntity.AddSplashPositionToWaterVisualRecord(_waterVisualRecord, position);
				}
				else
				{
					splashParticle.SplashPosition += splashParticle.SplashVelocity * dt;
				}
			}
		}
		if (_ownerSceneCached.GetFallDensity() > 0.5f)
		{
			float num4 = TaleWorlds.Library.MathF.Clamp(0.016f / dt, 0f, 1f) * 0.9f;
			int num5 = 13;
			for (int i = 0; i < num5; i++)
			{
				Vec3 vec = _bodyBB.max - _bodyBB.min;
				Vec3 v = _bodyBB.min;
				v.x += vec.x * _splashRandom.NextFloatRanged(0.1f, 0.9f);
				v.y += vec.y * _splashRandom.NextFloatRanged(0.1f, 0.9f);
				Vec3 position2 = globalFrame2.TransformToParent(in v);
				position2.w = _splashRandom.NextFloatRanged(3.25f, 10.65f) * num4;
				position2.z = _splashRandom.NextFloatRanged(0.05f, 0.07f);
				base.GameEntity.AddSplashPositionToWaterVisualRecord(_waterVisualRecord, position2);
			}
			if ((float)_splashRandom.Next() > 0.2f)
			{
				Vec3 vec2 = _bodyBB.max - _bodyBB.min;
				Vec3 v2 = _bodyBB.min;
				v2.x += vec2.x * _splashRandom.NextFloatRanged(0.1f, 0.9f);
				v2.y += vec2.y * _splashRandom.NextFloatRanged(0.1f, 0.9f);
				Vec3 position3 = globalFrame2.TransformToParent(in v2);
				position3.w = _splashRandom.NextFloatRanged(1.05f, 2.05f) * num4;
				position3.z = _splashRandom.NextFloatRanged(0.45f, 0.85f);
				base.GameEntity.AddSplashPositionToWaterVisualRecord(_waterVisualRecord, position3);
			}
		}
	}

	private Vec3 GetHeightCorrectedPosForSlice(ParticleData particleData, float height, ref bool pointIsValid, ref Vec3 limitingVector)
	{
		int num = particleData.PerSlicePositions.BinarySearch(new KeyValuePair<float, SliceSampleData>(height, default(SliceSampleData)), _cacheCompareDelegate);
		if (num >= 0)
		{
			pointIsValid = true;
			limitingVector = particleData.PerSlicePositions[num].Value.limitingUpVector;
			return particleData.PerSlicePositions[num].Value.localPosition;
		}
		int num2 = ~num;
		if (num2 > 0 && num2 < particleData.PerSlicePositions.Count)
		{
			int index = num2 - 1;
			KeyValuePair<float, SliceSampleData> keyValuePair = particleData.PerSlicePositions[index];
			KeyValuePair<float, SliceSampleData> keyValuePair2 = particleData.PerSlicePositions[num2];
			float alpha = (height - keyValuePair.Key) / (keyValuePair2.Key - keyValuePair.Key);
			pointIsValid = true;
			limitingVector = Vec3.Lerp(keyValuePair.Value.limitingUpVector, keyValuePair2.Value.limitingUpVector, alpha);
			return Vec3.Lerp(keyValuePair.Value.localPosition, keyValuePair2.Value.localPosition, alpha);
		}
		pointIsValid = false;
		return Vec3.Zero;
	}

	private void CheckWaterVisualRegistry()
	{
		_waterVisualRecord = base.GameEntity.Scene.RegisterShipVisualToWaterRenderer(base.GameEntity, in _waterSimulationBoundingBox);
		if (_waterVisualRecord != UIntPtr.Zero)
		{
			float multiplier = 1f;
			switch (_hullWaterResScale)
			{
			case ResolutionScale.half:
				multiplier = 0.5f;
				break;
			case ResolutionScale.quarter:
				multiplier = 0.25f;
				break;
			case ResolutionScale.one_eight:
				multiplier = 0.125f;
				break;
			case ResolutionScale.one_sixteenth:
				multiplier = 0.0625f;
				break;
			}
			base.GameEntity.ChangeResolutionMultiplierOfWaterVisual(_waterVisualRecord, multiplier, in _waterSimulationBoundingBox);
			SetMeshesToRenderForInHullWater();
		}
	}

	private void SetMeshesToRenderForInHullWater()
	{
		base.GameEntity.RefreshMeshesToRenderToHullWater(_waterVisualRecord, "render_to_depth");
	}

	public void EnableWakeAndParticles()
	{
		if (!_wakeAndParticlesEnabled)
		{
			float num = 0f;
			NavalDLC.Missions.NavalPhysics.NavalPhysics firstScriptOfType = base.GameEntity.Root.GetFirstScriptOfType<NavalDLC.Missions.NavalPhysics.NavalPhysics>();
			if (firstScriptOfType != null)
			{
				num = firstScriptOfType.StabilitySubmergedHeightOfShip;
			}
			PlaceParticles(ParticleType.Splash, num + _splashParticleHeightOffset);
			PlaceParticles(ParticleType.Movement, num + _movementParticleHeightOffset);
		}
		_wakeAndParticlesEnabled = true;
	}

	public void DeregisterWaterMeshMaterials()
	{
		if (_waterVisualRecord != UIntPtr.Zero)
		{
			base.GameEntity.DeRegisterWaterMeshMaterials(_waterVisualRecord);
		}
	}

	private void HandleSplashFoamDecals(float dt)
	{
		if (_movementParticles.Count == 0)
		{
			return;
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 scaleAmountXYZ = new Vec3(1.564f, 1.428f, 2f);
		Vec3 v = new Vec3(scaleAmountXYZ.x * 17.5f, scaleAmountXYZ.y * 17.5f, scaleAmountXYZ.z);
		SplashFoamDecal[] splashFoamDecals = _splashFoamDecals;
		foreach (SplashFoamDecal splashFoamDecal in splashFoamDecals)
		{
			float num = 11.5f;
			if (_movementParticleType == MovementParticleType.Large)
			{
				num += 3f;
			}
			else if (_movementParticleType == MovementParticleType.Medium)
			{
				num += 1.5f;
			}
			float num2 = num - 0.75f;
			if (splashFoamDecal._splashFoamDecal != null && splashFoamDecal._cumulativeDtTillStart < num)
			{
				splashFoamDecal._cumulativeDtTillStart += dt;
				if (splashFoamDecal._cumulativeDtTillStart > 0.75f)
				{
					float num3 = splashFoamDecal._cumulativeDtTillStart - 0.75f;
					float x = TaleWorlds.Library.MathF.Clamp(1f - num3 / num2, 0f, 1f);
					float y = 4f;
					float num4 = 0.475f;
					float alpha = TaleWorlds.Library.MathF.Pow(x, y) * (0.95f - num4) + num4;
					splashFoamDecal._splashFoamDecal.SetAlpha(alpha);
				}
				else
				{
					float num5 = TaleWorlds.Library.MathF.Clamp(splashFoamDecal._cumulativeDtTillStart / 0.75f, 0f, 1f);
					float y2 = 4f;
					float num6 = 0.475f;
					float alpha2 = (1f - TaleWorlds.Library.MathF.Pow(1f - num5, y2)) * (0.95f - num6) + num6;
					splashFoamDecal._splashFoamDecal.SetAlpha(alpha2);
				}
				splashFoamDecal._currentFrame.origin += splashFoamDecal._currentSpeed * dt;
				splashFoamDecal._currentFrame.origin.z = _ownerSceneCached.GetWaterLevelAtPosition(splashFoamDecal._currentFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) - 0.15f;
				Vec3 currentSpeed = splashFoamDecal._currentSpeed;
				float num7 = currentSpeed.Normalize();
				num7 = TaleWorlds.Library.MathF.Max(num7 - dt * 2.5f, 0f);
				splashFoamDecal._currentSpeed = num7 * currentSpeed;
				float alpha3 = TaleWorlds.Library.MathF.Clamp(splashFoamDecal._cumulativeDtTillStart / num, 0f, 1f);
				Vec3 scaleAmountXYZ2 = Vec3.Lerp(scaleAmountXYZ, v, alpha3);
				scaleAmountXYZ2.x *= splashFoamDecal._randomScale.x;
				scaleAmountXYZ2.y *= splashFoamDecal._randomScale.y;
				scaleAmountXYZ2.z *= splashFoamDecal._randomScale.z;
				float num8 = num;
				float percent = TaleWorlds.Library.MathF.Clamp(splashFoamDecal._cumulativeDtTillStart / num8, 0f, 1f);
				Vec3 s = Vec3.Slerp(splashFoamDecal._sideVectorStart, splashFoamDecal._sideVectorEnd, percent);
				s.Normalize();
				splashFoamDecal._currentFrame.rotation.s = s;
				splashFoamDecal._currentFrame.rotation.u = Vec3.Up;
				splashFoamDecal._currentFrame.rotation.f = -splashFoamDecal._currentFrame.rotation.s.CrossProductWithUp();
				splashFoamDecal._currentFrame.rotation.ApplyScaleLocal(in scaleAmountXYZ2);
				splashFoamDecal._splashFoamDecal.Frame = splashFoamDecal._currentFrame;
			}
			else if (splashFoamDecal._splashFoamDecal != null)
			{
				splashFoamDecal._splashFoamDecal.SetIsVisible(value: false);
			}
		}
		Vec3 vec = globalFrame.TransformToParent(in _movementParticles[0].LocalFrame.origin);
		float num9 = _lastDecalLeftSpawnPosition.DistanceSquared(vec);
		if (_nextDecalLeftSpawnMetersSq < num9)
		{
			Vec3 vec2 = (globalFrame.origin - _previousShipFrameForDecalSpawn) / dt;
			Vec3 s2 = globalFrame.rotation.s;
			s2.z = 0f;
			s2.Normalize();
			SplashFoamDecal splashFoamDecal2 = _splashFoamDecals[_nextDecalToUse];
			if (splashFoamDecal2._splashFoamDecal == null)
			{
				Decal decal = Decal.CreateDecal();
				decal.SetMaterial(Material.GetFromResource("decal_water_foam"));
				_ownerSceneCached.AddDecalInstance(decal, "editor_set", deletable: true);
				splashFoamDecal2._splashFoamDecal = decal;
			}
			Vec3 v2 = _movementParticles[_leftDecalParticleIndex].LocalFrame.origin;
			bool pointIsValid = true;
			Vec3 v3 = globalFrame.TransformToParent(in v2);
			float waterLevelAtPosition = base.GameEntity.GetWaterLevelAtPosition(v3.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			v3.z = waterLevelAtPosition + 2.5f;
			Vec3 limitingVector = Vec3.Zero;
			v2 = GetHeightCorrectedPosForSlice(_movementParticles[_leftDecalParticleIndex], globalFrame.TransformToLocalNonOrthogonal(in v3).z, ref pointIsValid, ref limitingVector);
			if (pointIsValid)
			{
				float num10 = 4f + (MBRandom.RandomFloat - 0.5f) * 1.5f;
				_nextDecalLeftSpawnMetersSq = num10 * num10;
				Vec3 v4 = _movementParticles[_leftDecalParticleIndex].SurfaceNormal;
				MatrixFrame identity = MatrixFrame.Identity;
				identity.origin = globalFrame.TransformToParent(in v2);
				identity.rotation.u = Vec3.Up;
				Vec3 vec3 = globalFrame.rotation.TransformToParent(in v4);
				vec3.z = 0f;
				vec3.Normalize();
				identity.rotation.s = vec3;
				identity.rotation.f = -identity.rotation.s.CrossProductWithUp();
				identity.rotation.f.Normalize();
				identity.origin -= 0.35f * vec3;
				splashFoamDecal2._cumulativeDtTillStart = 0f;
				splashFoamDecal2._splashFoamDecal.SetIsVisible(value: true);
				float num11 = TaleWorlds.Library.MathF.Clamp((vec2.Length - 4f) / 8f, 0f, 1f);
				float num12 = 0.6f + num11 * 0.2f;
				splashFoamDecal2._randomScale = Vec3.One * (0.9f + MBRandom.RandomFloat * 0.2f) * num12;
				splashFoamDecal2._randomScale.x *= 1f * MBRandom.RandomFloat + 0.4f;
				identity.rotation.ApplyScaleLocal(in scaleAmountXYZ);
				splashFoamDecal2._splashFoamDecal.Frame = identity;
				splashFoamDecal2._splashFoamDecal.SetAlpha(0f);
				splashFoamDecal2._currentFrame = identity;
				int num13 = MBRandom.RandomInt(3);
				float vectorArgument = (float)(num13 % 2) * 0.5f;
				float vectorArgument2 = (float)(num13 / 2) * 0.5f;
				splashFoamDecal2._splashFoamDecal.SetVectorArgument(vectorArgument, vectorArgument2, -0.5f, -0.5f);
				float num14 = 0.16f * (0.8f + MBRandom.RandomFloat * 0.4f);
				float num15 = 0.45f * (0.8f + MBRandom.RandomFloat * 0.4f);
				splashFoamDecal2._currentSpeed = vec2 * num15 + identity.rotation.s * vec2.Length * num14;
				float a = -0.34906584f * (0.8f + MBRandom.RandomFloat * 0.4f);
				splashFoamDecal2._sideVectorStart = vec3;
				splashFoamDecal2._sideVectorStart.RotateAboutZ(System.MathF.PI / 2f);
				splashFoamDecal2._sideVectorEnd = splashFoamDecal2._sideVectorStart;
				splashFoamDecal2._sideVectorEnd.RotateAboutZ(a);
				splashFoamDecal2._isLeft = true;
				Vec2 data = new Vec2(2.5f, 2.5f);
				splashFoamDecal2._splashFoamDecal.OverrideRoadBoundaryP0(data);
				Vec2 data2 = new Vec2(MBRandom.RandomFloat, MBRandom.RandomFloat);
				splashFoamDecal2._splashFoamDecal.OverrideRoadBoundaryP1(data2);
				_nextDecalToUse = (_nextDecalToUse + 1) % 50;
				_lastDecalLeftSpawnPosition = vec;
			}
		}
		num9 = _lastDecalRightSpawnPosition.DistanceSquared(vec);
		if (_nextDecalRightSpawnMetersSq < num9)
		{
			Vec3 vec4 = (globalFrame.origin - _previousShipFrameForDecalSpawn) / dt;
			Vec3 s3 = globalFrame.rotation.s;
			s3.z = 0f;
			s3.Normalize();
			SplashFoamDecal splashFoamDecal3 = _splashFoamDecals[_nextDecalToUse];
			if (splashFoamDecal3._splashFoamDecal == null)
			{
				Decal decal2 = Decal.CreateDecal();
				decal2.SetMaterial(Material.GetFromResource("decal_water_foam"));
				_ownerSceneCached.AddDecalInstance(decal2, "editor_set", deletable: true);
				splashFoamDecal3._splashFoamDecal = decal2;
			}
			Vec3 v5 = _movementParticles[_rightDecalParticleIndex].LocalFrame.origin;
			bool pointIsValid2 = true;
			Vec3 v6 = globalFrame.TransformToParent(in v5);
			float waterLevelAtPosition2 = base.GameEntity.GetWaterLevelAtPosition(v6.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
			v6.z = waterLevelAtPosition2 + 2.5f;
			Vec3 limitingVector2 = Vec3.Zero;
			v5 = GetHeightCorrectedPosForSlice(_movementParticles[_rightDecalParticleIndex], globalFrame.TransformToLocalNonOrthogonal(in v6).z, ref pointIsValid2, ref limitingVector2);
			if (pointIsValid2)
			{
				float num16 = 4f + (MBRandom.RandomFloat - 0.5f) * 1.5f;
				_nextDecalRightSpawnMetersSq = num16 * num16;
				Vec3 v7 = _movementParticles[_rightDecalParticleIndex].SurfaceNormal;
				MatrixFrame identity2 = MatrixFrame.Identity;
				identity2.origin = globalFrame.TransformToParent(in v5);
				identity2.rotation.u = Vec3.Up;
				Vec3 vec5 = globalFrame.rotation.TransformToParent(in v7);
				vec5.z = 0f;
				vec5.Normalize();
				identity2.rotation.s = vec5;
				identity2.rotation.f = -identity2.rotation.s.CrossProductWithUp();
				identity2.rotation.f.Normalize();
				identity2.origin -= 0.35f * vec5;
				splashFoamDecal3._cumulativeDtTillStart = 0f;
				splashFoamDecal3._splashFoamDecal.SetIsVisible(value: true);
				float num17 = TaleWorlds.Library.MathF.Clamp((vec4.Length - 4f) / 8f, 0f, 1f);
				float num18 = 0.6f + num17 * 0.2f;
				splashFoamDecal3._randomScale = Vec3.One * (0.9f + MBRandom.RandomFloat * 0.2f) * num18;
				splashFoamDecal3._randomScale.x *= 1f * MBRandom.RandomFloat + 0.4f;
				identity2.rotation.ApplyScaleLocal(in scaleAmountXYZ);
				splashFoamDecal3._splashFoamDecal.Frame = identity2;
				splashFoamDecal3._splashFoamDecal.SetAlpha(0f);
				splashFoamDecal3._currentFrame = identity2;
				float num19 = 0.16f * (0.8f + MBRandom.RandomFloat * 0.4f);
				float num20 = 0.45f * (0.8f + MBRandom.RandomFloat * 0.4f);
				splashFoamDecal3._currentSpeed = vec4 * num20 + identity2.rotation.s * vec4.Length * num19;
				int num21 = MBRandom.RandomInt(3);
				float vectorArgument3 = (float)(num21 % 2) * 0.5f;
				float vectorArgument4 = (float)(num21 / 2) * 0.5f;
				splashFoamDecal3._splashFoamDecal.SetVectorArgument(vectorArgument3, vectorArgument4, -0.5f, 0.5f);
				float a2 = 0.34906584f * (0.8f + MBRandom.RandomFloat * 0.4f);
				splashFoamDecal3._sideVectorStart = vec5;
				splashFoamDecal3._sideVectorStart.RotateAboutZ(-System.MathF.PI / 2f);
				splashFoamDecal3._sideVectorEnd = splashFoamDecal3._sideVectorStart;
				splashFoamDecal3._sideVectorEnd.RotateAboutZ(a2);
				splashFoamDecal3._isLeft = false;
				Vec2 data3 = new Vec2(2.5f, 2.5f);
				splashFoamDecal3._splashFoamDecal.OverrideRoadBoundaryP0(data3);
				Vec2 data4 = new Vec2(MBRandom.RandomFloat, MBRandom.RandomFloat);
				splashFoamDecal3._splashFoamDecal.OverrideRoadBoundaryP1(data4);
				_nextDecalToUse = (_nextDecalToUse + 1) % 50;
				_lastDecalRightSpawnPosition = vec;
			}
		}
		_previousShipFrameForDecalSpawn = globalFrame.origin;
	}

	private void HandleWetnessDecals(float dt)
	{
		base.GameEntity.IsInEditorScene();
		float num = dt / 6f;
		foreach (WetnessDecalData wetnessDecal in _wetnessDecals)
		{
			foreach (ParticleData splashParticle in _splashParticles)
			{
				if (splashParticle.CurrentSplashParticle != null && splashParticle.CurrentSplashParticle.HasAliveParticles())
				{
					float num2 = 0.13f * splashParticle.Size * dt;
					float num3 = splashParticle.Size * 2.1f;
					if (Vec3.DotProduct(wetnessDecal.Normal, splashParticle.LocalFrame.rotation.s) > 0f && wetnessDecal.LocalPosition.AsVec2.Distance(splashParticle.LocalFrame.origin.AsVec2) < num3)
					{
						float num4 = 1f;
						wetnessDecal.CurrentAlpha = Math.Min(wetnessDecal.CurrentAlpha + num2 * num4, 1f);
					}
				}
			}
			wetnessDecal.CurrentAlpha = TaleWorlds.Library.MathF.Max(wetnessDecal.CurrentAlpha - num, 0f);
			float num5 = TaleWorlds.Library.MathF.Pow(wetnessDecal.CurrentAlpha, 0.5f);
			float a = 0.2f + num5 * 0.8f;
			wetnessDecal.Decal.SetAlpha(TaleWorlds.Library.MathF.Min(a, 1f));
		}
	}
}
