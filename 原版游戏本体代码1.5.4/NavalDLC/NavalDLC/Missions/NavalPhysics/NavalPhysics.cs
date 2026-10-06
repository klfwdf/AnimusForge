using System;
using System.Linq;
using System.Runtime.InteropServices;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace NavalDLC.Missions.NavalPhysics;

[ScriptComponentParams("ship_visual_only", "")]
public class NavalPhysics : ScriptComponentBehavior
{
	public struct NavalPhysicsParameters
	{
		public float OverrideMass;

		public float MassMultiplier;

		public Vec3 MomentOfInertiaMultiplier;

		public float FloatingForceMultiplier;

		public float MaximumSubmergedVolumeRatio;

		public float ForwardDragMultiplier;

		public LinearFrictionTerm LinearFrictionMultiplier;

		public Vec3 AngularFrictionMultiplier;

		public float TorqueMultiplierOfLateralBuoyantForces;

		public Vec3 TorqueMultiplierOfVerticalBuoyantForces;

		public float UpSideDownFrictionMultiplier;

		public float MaxLinearSpeedForLateralDragCenterShift;

		public float MaxLateralDragShift;

		public float LateralDragShiftCriticalAngle;

		public float StepAgentWeightMultiplier;

		public bool MakeAgentsStepToEntityEvenUnderWater;
	}

	public struct BuoyancyComputationResult
	{
		public float PitchSubmergedAreaFactor;

		public float RollSubmergedAreaFactor;

		public float SubmergedHeightFactor;

		public float SubmergedFloaterCountFactor;

		public Vec3 AvgLocalBuoyancyApplyPosition;

		public Vec3 NetGlobalBuoyancyForce;

		public Vec3 NetBuoyancyTorque;

		public bool SimulatingAirFriction;

		public void Reset()
		{
			PitchSubmergedAreaFactor = 0f;
			RollSubmergedAreaFactor = 0f;
			SubmergedHeightFactor = 0f;
			SubmergedFloaterCountFactor = 1f;
			AvgLocalBuoyancyApplyPosition = Vec3.Zero;
			NetGlobalBuoyancyForce = Vec3.Zero;
			NetBuoyancyTorque = Vec3.Zero;
			SimulatingAirFriction = false;
		}
	}

	public struct DragForceComputationResult
	{
		public Vec3 CenterOfLateralDragLocal;

		public Vec3 LateralDragForceGlobal;

		public Vec3 CenterOfVerticalDragLocal;

		public Vec3 VerticalDragForceGlobal;

		public Vec3 CenterOfLongitudinalDragLocal;

		public Vec3 LongitudinalDragForceGlobal;

		public Vec3 AngularDragTorqueGlobal;

		public Vec3 DriftForceFromAngularDragGlobal;

		public void Reset()
		{
			CenterOfLateralDragLocal = Vec3.Zero;
			LateralDragForceGlobal = Vec3.Zero;
			CenterOfVerticalDragLocal = Vec3.Zero;
			VerticalDragForceGlobal = Vec3.Zero;
			CenterOfLongitudinalDragLocal = Vec3.Zero;
			LongitudinalDragForceGlobal = Vec3.Zero;
			AngularDragTorqueGlobal = Vec3.Zero;
			DriftForceFromAngularDragGlobal = Vec3.Zero;
		}
	}

	public struct WaterDriftForceData
	{
		public float DriftSpeed;

		public float DriftForceTimer;

		public MBFastRandom DriftRandom;

		public Vec3 ResultForce;

		public void Initialize()
		{
			DriftSpeed = 0f;
			DriftForceTimer = 0f;
			DriftRandom = new MBFastRandom();
			DriftForceTimer = DriftRandom.NextFloatRanged(0f, System.MathF.PI * 10f);
			ResultForce = Vec3.Zero;
		}
	}

	public enum ShipPart : byte
	{
		LeftBack,
		RightBack,
		LeftMid,
		RightMid,
		LeftFront,
		RightFront,
		Count
	}

	public enum SinkingState : byte
	{
		Floating,
		Sinking,
		Sunk
	}

	public const byte VerticalPartitionCount = 3;

	public const byte HorizontalPartitionCount = 2;

	private NavalPhysicsParameters _physicsParameters;

	private float _stabilityAvgSubmergedHeight;

	private int _stabilitySubmergedFloaterCount;

	private float _minFloaterEntitialBottomPos;

	private Scene _ownScene;

	private float _maxFloaterEntitialTopPos;

	private float _minimumFloaterDurabilityToFloatWhileNotSinking;

	[EditableScriptComponentVariable(false, "")]
	public Vec3 AngularDragTerm;

	[EditableScriptComponentVariable(true, "Sink")]
	private SimpleButton _sinkButton = new SimpleButton();

	private float _angularDragYSideComponentTerm;

	[EditableScriptComponentVariable(false, "")]
	public Vec3 AngularDampingTerm;

	private float _angularDampingYSideComponentTerm;

	private float _cachedMass;

	private float[] _shipPartsDurabilities;

	private ShipPart[] _floaterVolumesShipPartMap;

	private float[] _shipPartsTargetDurabilities;

	private VolumeDataForSubmergeComputation[] _floaterVolumeData;

	private UIntPtr _floaterVolumeDataPinnedPointer = UIntPtr.Zero;

	private GCHandle _floaterVolumeDataPinnedGCHandler;

	private float _totalFloaterVolumeCached;

	private ShipForceRecord _shipForceRecord;

	private BuoyancyComputationResult _buoyancyComputationResult;

	private DragForceComputationResult _dragComputationResult;

	private MatrixFrame _anchorGlobalFrame;

	private float _anchorForceMultiplier = 1f;

	private Vec3 _weightedAgentsPosition;

	private float _totalMass;

	private Vec3 _committedWeightedAgentsPosition;

	private float _committedTotalMass;

	private WaterDriftForceData _continuousDriftForceData;

	public bool IsInitialized { get; private set; }

	public Vec3 PhysicsBoundingBoxWithChildrenSize { get; private set; }

	public Vec3 PhysicsBoundingBoxSizeWithoutChildren { get; private set; }

	public BoundingBox PhysicsBoundingBoxWithChildren { get; private set; }

	public BoundingBox PhysicsBoundingBoxWithoutChildren { get; private set; }

	public float Mass => _cachedMass;

	public Vec3 LocalCenterOfMass => base.GameEntity.CenterOfMass;

	public Vec3 MassSpaceInertia => base.GameEntity.GetMassSpaceInertia();

	public ref readonly NavalPhysicsParameters PhysicsParameters => ref _physicsParameters;

	public SinkingState NavalSinkingState { get; private set; }

	private float StabilitySubmergedVolume => Mass / (GetWaterDensity() * _physicsParameters.FloatingForceMultiplier);

	public float FloatingForceMultiplierWhenDamaged => StabilitySubmergedVolume / (_totalFloaterVolumeCached * _physicsParameters.MaximumSubmergedVolumeRatio);

	public float StabilitySubmergedHeightOfShip { get; private set; }

	public float LastSubmergedHeightFactorForActuators { get; private set; }

	public LinearFrictionTerm LinearDragTerm { get; private set; }

	public LinearFrictionTerm LinearDampingTerm { get; private set; }

	public float MinFloaterEntitialBottomPos => _minFloaterEntitialBottomPos;

	public float MaxFloaterEntitialTopPos => _maxFloaterEntitialTopPos;

	public float AngularDragYSideComponentTerm => _angularDragYSideComponentTerm;

	public LinearFrictionTerm ConstantLinearDampingTerm { get; private set; }

	public float AngularDampingYSideComponentTerm => _angularDampingYSideComponentTerm;

	public Vec3 LinearVelocity => base.GameEntity.GetLinearVelocity();

	public Vec3 AngularVelocity => base.GameEntity.GetAngularVelocity();

	public bool IsAnchored { get; private set; }

	public MatrixFrame AnchorGlobalFrame => _anchorGlobalFrame;

	protected override void OnEditorInit()
	{
		_ownScene = base.GameEntity.Scene;
		if (_ownScene.GetEnginePhysicsEnabled())
		{
			if (!IsInitialized && !base.GameEntity.HasScriptOfType<MissionShip>())
			{
				OnInit();
			}
		}
		else
		{
			IsInitialized = false;
		}
	}

	protected override void OnInit()
	{
		if (!IsInitialized && base.GameEntity.GetFirstScriptOfType<MissionShip>() == null)
		{
			StabilitySubmergedHeightOfShip = 0f;
			_weightedAgentsPosition = Vec3.Zero;
			_totalMass = 0f;
			_committedWeightedAgentsPosition = Vec3.Zero;
			_committedTotalMass = 0f;
			CustomNavalPhysicsParameters customNavalPhysicsParameters = base.GameEntity.GetFirstScriptOfType<CustomNavalPhysicsParameters>() ?? new CustomNavalPhysicsParameters();
			ShipVisual firstScriptOfType = base.GameEntity.GetFirstScriptOfType<ShipVisual>();
			if (firstScriptOfType != null)
			{
				customNavalPhysicsParameters.FloatingForceMultiplier = firstScriptOfType.FloatingForceMultiplier;
				customNavalPhysicsParameters.BehaveLikeShip = true;
			}
			ShipPhysicsReference basePhysicsRef = (customNavalPhysicsParameters.BehaveLikeShip ? ShipPhysicsReference.Default : ShipPhysicsReference.DefaultDebris);
			NavalPhysicsParameters physicsParameters = new NavalPhysicsParameters
			{
				OverrideMass = 0f,
				MassMultiplier = 1f,
				MomentOfInertiaMultiplier = Vec3.One,
				FloatingForceMultiplier = customNavalPhysicsParameters.FloatingForceMultiplier,
				LinearFrictionMultiplier = new LinearFrictionTerm(customNavalPhysicsParameters.LinearFrictionMultiplierRight, customNavalPhysicsParameters.LinearFrictionMultiplierLeft, customNavalPhysicsParameters.LinearFrictionMultiplierForward, customNavalPhysicsParameters.LinearFrictionMultiplierBackward, customNavalPhysicsParameters.LinearFrictionMultiplierUp, customNavalPhysicsParameters.LinearFrictionMultiplierDown),
				AngularFrictionMultiplier = customNavalPhysicsParameters.AngularFrictionMultiplier,
				MaximumSubmergedVolumeRatio = 0.7f,
				ForwardDragMultiplier = 1f,
				TorqueMultiplierOfLateralBuoyantForces = 1f,
				TorqueMultiplierOfVerticalBuoyantForces = Vec3.One,
				UpSideDownFrictionMultiplier = 1f,
				MaxLinearSpeedForLateralDragCenterShift = 1E+09f,
				MaxLateralDragShift = 0f,
				LateralDragShiftCriticalAngle = 1f,
				StepAgentWeightMultiplier = 1f,
				MakeAgentsStepToEntityEvenUnderWater = false
			};
			Initialize(physicsParameters, basePhysicsRef);
		}
		_ownScene = base.GameEntity.Scene;
	}

	protected override void OnPreInit()
	{
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("batched_physics_entity");
		if (firstChildEntityWithTag != WeakGameEntity.Invalid)
		{
			firstChildEntityWithTag.CreateVariableRatePhysics(forChildren: true);
		}
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (child != firstChildEntityWithTag)
			{
				child.CreateVariableRatePhysics(forChildren: true);
			}
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		if (_floaterVolumeDataPinnedPointer != UIntPtr.Zero)
		{
			_floaterVolumeDataPinnedGCHandler.Free();
			_floaterVolumeDataPinnedPointer = UIntPtr.Zero;
		}
	}

	public void Initialize(NavalPhysicsParameters physicsParameters, ShipPhysicsReference basePhysicsRef)
	{
		_shipForceRecord = ShipForceRecord.None();
		_continuousDriftForceData.Initialize();
		base.GameEntity.Scene.SetOnCollisionFilterCallbackActive(isActive: true);
		UpdateShipPhysics(physicsParameters, basePhysicsRef);
		LoadFloaterVolumes();
		PreComputeAngularDragTerms(out AngularDampingTerm, out AngularDragTerm, out _angularDampingYSideComponentTerm, out _angularDragYSideComponentTerm);
		if (!physicsParameters.MakeAgentsStepToEntityEvenUnderWater)
		{
			base.GameEntity.AddBodyFlags(BodyFlags.Sinking | BodyFlags.FloatingDebris);
		}
		base.GameEntity.Scene.SetFixedTickCallbackActive(isActive: true);
		IsInitialized = true;
		IsAnchored = false;
		_anchorGlobalFrame = MatrixFrame.Zero;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel | TickRequirement.FixedTick | TickRequirement.FixedParallelTick;
	}

	protected override void OnSaveAsPrefab()
	{
	}

	public static float GetAirDensity()
	{
		return GameModels.Instance.ShipPhysicsParametersModel.GetAirDensity();
	}

	public static float GetWaterDensity()
	{
		return GameModels.Instance.ShipPhysicsParametersModel.GetWaterDensity();
	}

	public void CheckPrefab()
	{
		float waterDensity = GetWaterDensity();
		float num = base.GameEntity.Mass * 9.806f * 1.01f;
		float num2 = _totalFloaterVolumeCached * waterDensity * 9.806f * _physicsParameters.FloatingForceMultiplier;
		if (num2 <= num)
		{
			base.GameEntity.GetFirstScriptOfType<MissionShip>();
		}
		_ = num2 * FloatingForceMultiplierWhenDamaged;
	}

	public void OnShipObjectUpdated(NavalPhysicsParameters physicsParameters, ShipPhysicsReference basePhysicsRef)
	{
		UpdateShipPhysics(physicsParameters, basePhysicsRef);
	}

	public void SetShipForceRecord(in ShipForceRecord record)
	{
		_shipForceRecord = record;
	}

	public void SetContinuousDriftSpeed(float driftSpeed)
	{
		_continuousDriftForceData.DriftSpeed = driftSpeed;
	}

	public void SetAnchor(bool isAnchored, bool anchorInPlace = false, float forceMultiplier = 1f)
	{
		IsAnchored = isAnchored;
		if (IsAnchored)
		{
			if (_anchorGlobalFrame.IsZero || anchorInPlace)
			{
				MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
				SetAnchorFrame(globalFrame.origin.AsVec2, globalFrame.rotation.f.AsVec2.Normalized(), forceMultiplier);
			}
		}
		else
		{
			_anchorGlobalFrame = MatrixFrame.Zero;
			_anchorForceMultiplier = 1f;
		}
	}

	public void SetAnchorFrame(in Vec2 position, in Vec2 direction, float forceMultiplier = 1f)
	{
		float waterLevelAtPosition = base.GameEntity.Scene.GetWaterLevelAtPosition(position, useWaterRenderer: true, checkWaterBodyEntities: false);
		_anchorGlobalFrame.origin = position.ToVec3(waterLevelAtPosition);
		_anchorGlobalFrame.rotation.f = direction.ToVec3();
		_anchorGlobalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		_anchorForceMultiplier = forceMultiplier;
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		if (base.GameEntity.HasDynamicRigidBodyAndActiveSimulation())
		{
			base.GameEntity.GetBodyWorldTransform();
			Vec3 globalLinearVelocity;
			Vec3 globalAngularVelocity;
			Vec3 massSpaceLocalInertia;
			using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
			{
				globalLinearVelocity = LinearVelocity;
				globalAngularVelocity = AngularVelocity;
				massSpaceLocalInertia = MassSpaceInertia;
			}
			if (IsAnchored)
			{
				float waterLevelAtPosition = base.GameEntity.Scene.GetWaterLevelAtPosition(_anchorGlobalFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
				_anchorGlobalFrame.origin.z = waterLevelAtPosition;
			}
			UpdateFloaterVolumeData();
			TickFloaterDurabilities(fixedDt);
			FillWaterHeightQueryResultsIterative();
			ComputeBuoyancyForces(fixedDt, in globalLinearVelocity, in globalAngularVelocity);
			ComputeDragForces(fixedDt, in globalLinearVelocity, in globalAngularVelocity, in massSpaceLocalInertia);
			ComputeContinuousDriftForce(fixedDt);
		}
	}

	protected override void OnFixedTick(float fixedDt)
	{
		if (base.GameEntity.HasDynamicRigidBodyAndActiveSimulation())
		{
			LastSubmergedHeightFactorForActuators = _buoyancyComputationResult.SubmergedHeightFactor;
			ApplyForceToDynamicBody(MBGlobals.GravitationalAcceleration * Mass);
			ApplyAgentForces();
			ApplyBuoyancyForces();
			ApplyDragForces();
			ApplyActuatorForces();
			ApplyAnchorForces();
			ApplyContinuousDriftForce();
		}
	}

	protected override void OnEditorTick(float dt)
	{
		if (base.GameEntity.Scene.GetEnginePhysicsEnabled())
		{
			if (!IsInitialized && !base.GameEntity.HasScriptOfType<MissionShip>())
			{
				OnInit();
			}
		}
		else
		{
			IsInitialized = false;
		}
	}

	public void ApplyGlobalForceAtLocalPos(in Vec3 localPos, in Vec3 globalForceVec, GameEntityPhysicsExtensions.ForceMode forceMode = GameEntityPhysicsExtensions.ForceMode.Force)
	{
		base.GameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(localPos, globalForceVec, forceMode);
	}

	public void ApplyLocalForceAtLocalPos(in Vec3 localPos, in Vec3 localForceVec, GameEntityPhysicsExtensions.ForceMode forceMode = GameEntityPhysicsExtensions.ForceMode.Force)
	{
		base.GameEntity.ApplyLocalForceAtLocalPosToDynamicBody(localPos, localForceVec, forceMode);
	}

	public void ApplyForceToDynamicBody(in Vec3 forceVec, GameEntityPhysicsExtensions.ForceMode forceMode = GameEntityPhysicsExtensions.ForceMode.Force)
	{
		base.GameEntity.ApplyForceToDynamicBody(forceVec, forceMode);
	}

	public void ApplyTorque(in Vec3 torqueVec, GameEntityPhysicsExtensions.ForceMode forceMode = GameEntityPhysicsExtensions.ForceMode.Force)
	{
		base.GameEntity.ApplyTorqueToDynamicBody(torqueVec, forceMode);
	}

	public MatrixFrame GetGlobalMassFrame()
	{
		MatrixFrame bodyWorldTransform = base.GameEntity.GetBodyWorldTransform();
		MatrixFrame result = default(MatrixFrame);
		result.rotation = bodyWorldTransform.rotation;
		result.origin = bodyWorldTransform.TransformToParent(LocalCenterOfMass);
		return result;
	}

	public Vec3 GetClosestPointToBoundingBox(in Vec3 localPoint)
	{
		Vec3 min = PhysicsBoundingBoxWithoutChildren.min;
		Vec3 max = PhysicsBoundingBoxWithoutChildren.max;
		float x = Math.Max(min.x, Math.Min(max.x, localPoint.x));
		float y = Math.Max(min.y, Math.Min(max.y, localPoint.y));
		float z = Math.Max(min.z, Math.Min(max.z, localPoint.z));
		return new Vec3(x, y, z);
	}

	public void SetTargetDurabilityOfPart(int part, float targetDurability)
	{
		_shipPartsTargetDurabilities[part] = TaleWorlds.Library.MathF.Max(0.01f, TaleWorlds.Library.MathF.Min(_shipPartsTargetDurabilities[part], targetDurability));
	}

	private void SetTargetDurabilityToAdjacentParts(int part, float targetDurability)
	{
		if (part - 1 >= 0 && part % 2 - (part - 1) % 2 == 1)
		{
			SetTargetDurabilityOfPart(part - 1, targetDurability);
		}
		if (part + 1 < 6 && (part + 1) % 2 - part % 2 == 1)
		{
			SetTargetDurabilityOfPart(part + 1, targetDurability);
		}
		if (part - 2 >= 0)
		{
			SetTargetDurabilityOfPart(part - 2, targetDurability);
		}
		if (part + 2 < 6)
		{
			SetTargetDurabilityOfPart(part + 2, targetDurability);
		}
	}

	private void TickFloaterDurabilities(float fixedDt)
	{
		float floatingForceMultiplierWhenDamaged = FloatingForceMultiplierWhenDamaged;
		for (int i = 0; i < 6; i++)
		{
			float num = ((NavalSinkingState != SinkingState.Floating) ? _shipPartsTargetDurabilities[i] : TaleWorlds.Library.MathF.Max(_shipPartsTargetDurabilities[i], floatingForceMultiplierWhenDamaged));
			if (num < _shipPartsDurabilities[i])
			{
				_shipPartsDurabilities[i] = TaleWorlds.Library.MathF.Max(num, _shipPartsDurabilities[i] - 0.2f * fixedDt * TaleWorlds.Library.MathF.Max(0.5f, _shipPartsDurabilities[i]));
				float targetDurability = ((_shipPartsDurabilities[i] <= 0.01f) ? 0.01f : TaleWorlds.Library.MathF.Min(1f, 1f - (1f - _shipPartsDurabilities[i]) / 2f));
				SetTargetDurabilityToAdjacentParts(i, targetDurability);
			}
		}
	}

	protected override bool CanPhysicsCollideBetweenTwoEntities(WeakGameEntity myEntity, BodyFlags myEntityBodyFlags, WeakGameEntity otherEntity, BodyFlags otherEntityBodyFlags)
	{
		if (!myEntityBodyFlags.HasAnyFlag(BodyFlags.FloatingDebris))
		{
			return true;
		}
		if (!otherEntityBodyFlags.HasAnyFlag(BodyFlags.Moveable))
		{
			return true;
		}
		if (otherEntityBodyFlags.HasAnyFlag(BodyFlags.FloatingDebris))
		{
			return otherEntityBodyFlags.HasAnyFlag(BodyFlags.Dynamic);
		}
		return !otherEntityBodyFlags.HasAnyFlag(BodyFlags.Dynamic);
	}

	private void FillWaterHeightQueryResultsIterative()
	{
		MatrixFrame globalFrame = base.GameEntity.GetBodyWorldTransform();
		_ownScene.GetBulkWaterLevelAtVolumes(_floaterVolumeDataPinnedPointer, _floaterVolumeData.Length, in globalFrame);
	}

	private static (float, float) RungeKuttaIntegrationStepForBuoyancyAndGravity(float prevIterationUpSpeed, float prevIterationUpAcceleration, float baseShipUpSpeed, float fixedDt, float baseSubmergedHeight, float volumeHeight, float volumeWidthMultDepth, float waterDensity, float durabilityMultiplier, float curInvVolumeMass)
	{
		float num = prevIterationUpSpeed * fixedDt;
		float item = TaleWorlds.Library.MathF.Clamp(baseSubmergedHeight - num, 0f, volumeHeight) * volumeWidthMultDepth * waterDensity * 9.806f * durabilityMultiplier * curInvVolumeMass + -9.806f;
		float item2 = baseShipUpSpeed + fixedDt * prevIterationUpAcceleration;
		return (item, item2);
	}

	private void ComputeBuoyancyForces(float fixedDt, in Vec3 globalLinearVelocity, in Vec3 globalAngularVelocity)
	{
		MatrixFrame bodyWorldTransform = base.GameEntity.GetBodyWorldTransform();
		MatrixFrame globalMassFrame = GetGlobalMassFrame();
		float waterDensity = GetWaterDensity();
		float z = globalLinearVelocity.z;
		float num = Mass / _totalFloaterVolumeCached;
		float num2 = ((NavalSinkingState != SinkingState.Floating) ? 0f : _minimumFloaterDurabilityToFloatWhileNotSinking);
		_buoyancyComputationResult.Reset();
		int num3 = 0;
		Vec3 localCenterOfMass = LocalCenterOfMass;
		float floatingForceMultiplier = _physicsParameters.FloatingForceMultiplier;
		float num4 = 0f;
		Vec3 zero = Vec3.Zero;
		float num5 = 0f;
		Vec3 va = globalMassFrame.rotation.TransformToLocal(in globalAngularVelocity);
		Mat3 identity = Mat3.Identity;
		Vec3 vec = bodyWorldTransform.rotation.TransformToLocal(in Vec3.Up);
		float num6 = 0f;
		float num7 = 0f;
		float num8 = (LinearDampingTerm.Up / LinearDampingTerm.Down + LinearDragTerm.Up / LinearDragTerm.Down) * 0.5f;
		for (int i = 0; i < _floaterVolumeData.Length; i++)
		{
			VolumeDataForSubmergeComputation volumeDataForSubmergeComputation = _floaterVolumeData[i];
			float inOutWaterHeightWrtVolume = volumeDataForSubmergeComputation.InOutWaterHeightWrtVolume;
			if (inOutWaterHeightWrtVolume > 0f)
			{
				float height = volumeDataForSubmergeComputation.Height;
				float width = volumeDataForSubmergeComputation.Width;
				float depth = volumeDataForSubmergeComputation.Depth;
				float num9 = TaleWorlds.Library.MathF.Clamp(inOutWaterHeightWrtVolume, 0f, height);
				num3++;
				num5 += num9;
				Vec3 v = Vec3.CrossProduct(va, volumeDataForSubmergeComputation.DynamicLocalBottomPos - localCenterOfMass);
				Vec3 vec2 = globalMassFrame.rotation.TransformToParent(in v);
				float baseShipUpSpeed = vec2.z + z;
				if (inOutWaterHeightWrtVolume >= volumeDataForSubmergeComputation.Height || vec2.z <= 0f)
				{
					num6 += 1f;
					num7 += 1f;
				}
				else
				{
					num6 += num8;
					num7 += num8;
				}
				float num10 = width * depth;
				float num11 = height * num10 * num;
				float curInvVolumeMass = 1f / num11;
				float num12 = _shipPartsDurabilities[(uint)_floaterVolumesShipPartMap[i]] * floatingForceMultiplier;
				if (num12 < num2)
				{
					num12 = num2;
				}
				Vec3 vec3 = ((!(bodyWorldTransform.rotation[(int)volumeDataForSubmergeComputation.DynamicUpAxis].z < 0f)) ? (volumeDataForSubmergeComputation.DynamicLocalBottomPos + identity[(int)volumeDataForSubmergeComputation.DynamicUpAxis] * (num9 * 0.5f)) : (volumeDataForSubmergeComputation.DynamicLocalBottomPos + identity[(int)volumeDataForSubmergeComputation.DynamicUpAxis] * (height - num9 * 0.5f)));
				(float, float) tuple = RungeKuttaIntegrationStepForBuoyancyAndGravity(0f, 0f, baseShipUpSpeed, fixedDt, inOutWaterHeightWrtVolume, height, num10, waterDensity, num12, curInvVolumeMass);
				float item = tuple.Item1;
				(float, float) tuple2 = RungeKuttaIntegrationStepForBuoyancyAndGravity(tuple.Item2, item, baseShipUpSpeed, fixedDt * 0.5f, inOutWaterHeightWrtVolume, height, num10, waterDensity, num12, curInvVolumeMass);
				float item2 = tuple2.Item1;
				(float, float) tuple3 = RungeKuttaIntegrationStepForBuoyancyAndGravity(tuple2.Item2, item2, baseShipUpSpeed, fixedDt * 0.5f, inOutWaterHeightWrtVolume, height, num10, waterDensity, num12, curInvVolumeMass);
				float item3 = tuple3.Item1;
				float item4 = RungeKuttaIntegrationStepForBuoyancyAndGravity(tuple3.Item2, item3, baseShipUpSpeed, fixedDt, inOutWaterHeightWrtVolume, height, num10, waterDensity, num12, curInvVolumeMass).Item1;
				float num13 = 1f / 6f * (item + 2f * item2 + 2f * item3 + item4);
				float num14 = num9 * num10;
				float num15 = (num13 + 9.806f) * num11;
				num4 += num14;
				zero += vec3 * num14;
				Vec3 vec6;
				if (inOutWaterHeightWrtVolume < height)
				{
					Vec3 outGlobalWaterSurfaceNormal = _floaterVolumeData[i].OutGlobalWaterSurfaceNormal;
					float a = TaleWorlds.Library.MathF.Clamp(outGlobalWaterSurfaceNormal.x / outGlobalWaterSurfaceNormal.z * width, 0f - height, height);
					float b = TaleWorlds.Library.MathF.Clamp(outGlobalWaterSurfaceNormal.y / outGlobalWaterSurfaceNormal.z * depth, 0f - height, height);
					Vec2 vec4 = new Vec2(a, b);
					Vec2 vb = Vec2.Abs(new Vec2(vec4.x * width, vec4.y * depth));
					Vec2 vec5 = vec4 * 0.5f;
					Vec2 xy = Vec2.ElementWiseProduct(waterDensity * 9.806f * vec5, vb) * num12;
					vec6 = bodyWorldTransform.rotation.TransformToLocal(new Vec3(xy));
				}
				else
				{
					vec6 = Vec3.Zero;
				}
				Vec3 vec7 = bodyWorldTransform.rotation.TransformToLocal(in _floaterVolumeData[i].OutGlobalWaterSurfaceNormal) * (num15 * 0.1f) + vec * num15;
				Vec3 v2 = vec7 + vec6;
				_buoyancyComputationResult.NetGlobalBuoyancyForce += bodyWorldTransform.rotation.TransformToParent(in v2);
				Vec3 torqueMultiplierOfVerticalBuoyantForces = _physicsParameters.TorqueMultiplierOfVerticalBuoyantForces;
				Vec3 v3 = Vec3.CrossProduct(Vec3.ElementWiseProduct(vec7, torqueMultiplierOfVerticalBuoyantForces) + vec6 * _physicsParameters.TorqueMultiplierOfLateralBuoyantForces, localCenterOfMass - vec3);
				Vec3 vec8 = bodyWorldTransform.rotation.TransformToParent(in v3);
				_buoyancyComputationResult.NetBuoyancyTorque += vec8;
			}
		}
		_buoyancyComputationResult.SubmergedFloaterCountFactor = (float)num3 / (float)_stabilitySubmergedFloaterCount;
		_buoyancyComputationResult.PitchSubmergedAreaFactor = num6 / ((float)_stabilitySubmergedFloaterCount * 0.5f) / (1f + num8);
		_buoyancyComputationResult.RollSubmergedAreaFactor = num7 / ((float)_stabilitySubmergedFloaterCount * 0.5f) / (1f + num8);
		if (num3 > 0)
		{
			float num16 = num5 / (float)_stabilitySubmergedFloaterCount;
			_buoyancyComputationResult.SubmergedHeightFactor = num16 / _stabilityAvgSubmergedHeight;
			if (_buoyancyComputationResult.SubmergedHeightFactor > 2f)
			{
				_buoyancyComputationResult.SubmergedHeightFactor = 2f;
			}
		}
		else
		{
			_buoyancyComputationResult.SubmergedHeightFactor = 0f;
		}
		float num17 = GetAirDensity() / GetWaterDensity();
		if (_buoyancyComputationResult.SubmergedHeightFactor < num17)
		{
			_buoyancyComputationResult.SubmergedHeightFactor = num17;
			_buoyancyComputationResult.SimulatingAirFriction = true;
		}
		if (_buoyancyComputationResult.SubmergedFloaterCountFactor < num17)
		{
			_buoyancyComputationResult.SubmergedFloaterCountFactor = num17;
			_buoyancyComputationResult.SimulatingAirFriction = true;
		}
		if (_buoyancyComputationResult.PitchSubmergedAreaFactor < num17)
		{
			_buoyancyComputationResult.PitchSubmergedAreaFactor = num17;
			_buoyancyComputationResult.SimulatingAirFriction = true;
		}
		if (_buoyancyComputationResult.RollSubmergedAreaFactor < num17)
		{
			_buoyancyComputationResult.RollSubmergedAreaFactor = num17;
			_buoyancyComputationResult.SimulatingAirFriction = true;
		}
		if (_buoyancyComputationResult.RollSubmergedAreaFactor < 0.25f)
		{
			_buoyancyComputationResult.RollSubmergedAreaFactor = 0.25f;
		}
		if (num4 > 0f)
		{
			zero /= num4;
			_buoyancyComputationResult.AvgLocalBuoyancyApplyPosition = zero;
		}
		else
		{
			_buoyancyComputationResult.AvgLocalBuoyancyApplyPosition = Vec3.Zero;
		}
	}

	private void PreComputeAngularDragTerms(out Vec3 angularDampingTerm, out Vec3 angularDragTerm, out float angularDampingYSideComponentTerm, out float angularDragYSideComponentTerm)
	{
		angularDampingTerm = Vec3.One;
		angularDragTerm = Vec3.One;
		Vec3 localCenterOfMass = LocalCenterOfMass;
		double num = PhysicsBoundingBoxWithoutChildren.max.y - PhysicsBoundingBoxWithoutChildren.min.y;
		double num2 = 0.001;
		double num3 = 0.001;
		double num4 = localCenterOfMass.y;
		double num5 = (double)LinearDragTerm.Up / num;
		double num6 = (double)LinearDampingTerm.Up / num;
		double num7 = (double)LinearDragTerm.Down / num;
		double num8 = (double)LinearDampingTerm.Down / num;
		double num9 = num3 * num5 + num3 * num7;
		double num10 = num3 * num6 + num3 * num8;
		double num11 = 0.0;
		double num12 = 0.0;
		for (double num13 = PhysicsBoundingBoxWithoutChildren.min.y; num13 <= (double)PhysicsBoundingBoxWithoutChildren.max.y; num13 += num3)
		{
			double num14 = Math.Abs(num13 - num4);
			num11 += num14 * num14 * num14;
			num12 += num14 * num14;
		}
		num11 *= num9;
		num12 *= num10;
		angularDampingTerm.x = (float)num12;
		angularDragTerm.x = (float)num11;
		double num15 = localCenterOfMass.x;
		double num16 = (double)(TaleWorlds.Library.MathF.Abs(PhysicsBoundingBoxWithoutChildren.min.x) + TaleWorlds.Library.MathF.Abs(PhysicsBoundingBoxWithoutChildren.max.x) + TaleWorlds.Library.MathF.Abs(PhysicsBoundingBoxWithoutChildren.min.x) + TaleWorlds.Library.MathF.Abs(PhysicsBoundingBoxWithoutChildren.max.x)) * 0.25;
		double num17 = (double)LinearDragTerm.Up / num16;
		double num18 = (double)LinearDampingTerm.Up / num16;
		double num19 = (double)LinearDragTerm.Down / num16;
		double num20 = (double)LinearDampingTerm.Down / num16;
		double num21 = num2 * num17 + num2 * num19;
		double num22 = num2 * num18 + num2 * num20;
		double num23 = 0.0;
		double num24 = 0.0;
		for (double num25 = 0.0 - num16; num25 <= num16; num25 += num2)
		{
			double num26 = Math.Abs(num25 - num15);
			num23 += num26 * num26 * num26;
			num24 += num26 * num26;
		}
		num23 *= num21;
		num24 *= num22;
		angularDampingTerm.y = (float)num24;
		angularDragTerm.y = (float)num23;
		double num27 = localCenterOfMass.z;
		double num28 = StabilitySubmergedHeightOfShip;
		double num29 = num28 - (double)_minFloaterEntitialBottomPos;
		double num30 = 0.001;
		double num31 = (double)(LinearDragTerm.Left + LinearDragTerm.Right) * 1.0 / num29;
		double num32 = (double)(LinearDampingTerm.Left + LinearDampingTerm.Right) * 1.0 / num29;
		double num33 = num30 * num31;
		double num34 = num30 * num32;
		double num35 = 0.0;
		double num36 = 0.0;
		for (double num37 = _minFloaterEntitialBottomPos; num37 <= num28; num37 += num30)
		{
			double num38 = Math.Abs(num37 - num27);
			num35 += num38 * num38 * num38;
			num36 += num38 * num38;
		}
		num35 *= num33;
		num36 *= num34;
		angularDampingYSideComponentTerm = (float)num36;
		angularDragYSideComponentTerm = (float)num35;
		double num39 = localCenterOfMass.y;
		double num40 = num;
		double num41 = (double)(LinearDragTerm.Left + LinearDragTerm.Right) * 0.5 / num40;
		double num42 = (double)(LinearDampingTerm.Left + LinearDampingTerm.Right) * 0.5 / num40;
		double num43 = num3 * num41;
		double num44 = num3 * num42;
		double num45 = 0.0;
		double num46 = 0.0;
		for (double num47 = PhysicsBoundingBoxWithoutChildren.min.y; num47 <= (double)PhysicsBoundingBoxWithoutChildren.max.y; num47 += num3)
		{
			double num48 = Math.Abs(num47 - num39);
			num45 += num48 * num48 * num48;
			num46 += num48 * num48;
		}
		num45 *= num43;
		num46 *= num44;
		angularDampingTerm.z = (float)num46;
		angularDragTerm.z = (float)num45;
	}

	private void ComputeDragForces(float fixedDt, in Vec3 globalLinearVelocity, in Vec3 globalAngularVelocity, in Vec3 massSpaceLocalInertia)
	{
		_dragComputationResult.Reset();
		MatrixFrame entityGlobalFrame = base.GameEntity.GetBodyWorldTransform();
		MatrixFrame centerOfMassGlobalFrame = GetGlobalMassFrame();
		Vec3 localCenterOfMass = LocalCenterOfMass;
		int substepCount = TaleWorlds.Library.MathF.Ceiling(fixedDt / (1f / 60f));
		ComputeAngularDrag(fixedDt, substepCount, in globalAngularVelocity, in centerOfMassGlobalFrame, in massSpaceLocalInertia, in _physicsParameters, in _buoyancyComputationResult, in AngularDragTerm, in AngularDampingTerm, _angularDragYSideComponentTerm, _angularDampingYSideComponentTerm, ref _dragComputationResult);
		ComputeDriftFromAngularFriction(fixedDt, in entityGlobalFrame, in centerOfMassGlobalFrame);
		ComputeLinearDrag(fixedDt, substepCount, in globalLinearVelocity, in entityGlobalFrame, Mass, in localCenterOfMass, in _physicsParameters, in _buoyancyComputationResult, LinearDragTerm, LinearDampingTerm, ConstantLinearDampingTerm, _minFloaterEntitialBottomPos, _maxFloaterEntitialTopPos, ref _dragComputationResult, out var _);
	}

	private void ComputeDriftFromAngularFriction(float fixedDt, in MatrixFrame entityGlobalFrame, in MatrixFrame centerOfMassGlobalFrame)
	{
		if (!_buoyancyComputationResult.SimulatingAirFriction && _buoyancyComputationResult.SubmergedHeightFactor < 2f && NavalSinkingState == SinkingState.Floating)
		{
			Vec3 vec = entityGlobalFrame.TransformToParent(in _buoyancyComputationResult.AvgLocalBuoyancyApplyPosition);
			Vec3 impulsiveTorqueGlobal = _dragComputationResult.AngularDragTorqueGlobal * fixedDt;
			impulsiveTorqueGlobal.z = 0f;
			base.GameEntity.ComputeVelocityDeltaFromImpulse(in Vec3.Zero, in impulsiveTorqueGlobal, out var _, out var deltaGlobalAngularVelocity);
			Vec3 vec2 = -Vec3.CrossProduct(centerOfMassGlobalFrame.origin - vec, deltaGlobalAngularVelocity);
			float num = 1f;
			if (_buoyancyComputationResult.SubmergedHeightFactor > 1f)
			{
				num = 2f / _buoyancyComputationResult.SubmergedHeightFactor - 1f;
			}
			vec2 *= num;
			if (vec2.LengthSquared > 0.010000001f)
			{
				vec2 = vec2.NormalizedCopy() * 0.1f;
			}
			_dragComputationResult.DriftForceFromAngularDragGlobal = Mass * (-vec2 / fixedDt);
		}
	}

	protected override void OnTickParallel(float dt)
	{
		_committedWeightedAgentsPosition = _weightedAgentsPosition;
		_committedTotalMass = _totalMass;
		ClearAgentWeightAndPositionInformation();
	}

	private void ApplyDragForces()
	{
		ApplyGlobalForceAtLocalPos(in _dragComputationResult.CenterOfLateralDragLocal, in _dragComputationResult.LateralDragForceGlobal);
		ApplyGlobalForceAtLocalPos(in _dragComputationResult.CenterOfLongitudinalDragLocal, in _dragComputationResult.LongitudinalDragForceGlobal);
		ApplyGlobalForceAtLocalPos(in _dragComputationResult.CenterOfVerticalDragLocal, in _dragComputationResult.VerticalDragForceGlobal);
		ApplyTorque(in _dragComputationResult.AngularDragTorqueGlobal);
		ApplyForceToDynamicBody(in _dragComputationResult.DriftForceFromAngularDragGlobal);
	}

	private void ApplyAgentForces()
	{
		if (_committedTotalMass > 0f)
		{
			Vec3 v = _committedWeightedAgentsPosition / _committedTotalMass;
			Vec3 localPos = base.GameEntity.GetBodyWorldTransform().TransformToLocal(in v);
			if (PhysicsBoundingBoxWithoutChildren.PointInsideBox(localPos, 0.1f))
			{
				float stepAgentWeightMultiplier = _physicsParameters.StepAgentWeightMultiplier;
				ApplyGlobalForceAtLocalPos(in localPos, _committedTotalMass * stepAgentWeightMultiplier * MBGlobals.GravitationalAcceleration);
			}
		}
	}

	private void ClearAgentWeightAndPositionInformation()
	{
		_weightedAgentsPosition = Vec3.Zero;
		_totalMass = 0f;
	}

	private void ApplyBuoyancyForces()
	{
		ApplyForceToDynamicBody(in _buoyancyComputationResult.NetGlobalBuoyancyForce);
		ApplyTorque(in _buoyancyComputationResult.NetBuoyancyTorque);
	}

	public void AddAgentWeightAndPositionInformation(Agent agent)
	{
		float totalMass = agent.GetTotalMass();
		Vec3 v = agent.Position;
		Vec3 point = base.GameEntity.GetBodyWorldTransform().TransformToLocal(in v);
		if (PhysicsBoundingBoxWithoutChildren.PointInsideBox(point, 0.1f))
		{
			_weightedAgentsPosition += totalMass * v;
			_totalMass += totalMass;
		}
	}

	private void ApplyActuatorForces()
	{
		if (_shipForceRecord.HasLeftOarForces)
		{
			foreach (ShipForce leftOarForce in _shipForceRecord.LeftOarForces)
			{
				ShipForce current = leftOarForce;
				if (current.IsApplicable)
				{
					ApplyGlobalForceAtLocalPos(in current.LocalPosition, in current.Force);
				}
			}
		}
		if (_shipForceRecord.HasRightOarForces)
		{
			foreach (ShipForce rightOarForce in _shipForceRecord.RightOarForces)
			{
				ShipForce current2 = rightOarForce;
				if (current2.IsApplicable)
				{
					ApplyGlobalForceAtLocalPos(in current2.LocalPosition, in current2.Force);
				}
			}
		}
		if (_shipForceRecord.HasSailForces)
		{
			foreach (ShipForce sailForce in _shipForceRecord.SailForces)
			{
				ShipForce current3 = sailForce;
				if (current3.IsApplicable)
				{
					current3.ComputeRealisticAndGamifiedForceComponents(out var realisticForce, out var gamifiedForce);
					ApplyGlobalForceAtLocalPos(in current3.LocalPosition, in realisticForce);
					ApplyForceToDynamicBody(in gamifiedForce);
				}
			}
		}
		if (_shipForceRecord.RudderForce.IsApplicable)
		{
			_shipForceRecord.RudderForce.ComputeRealisticAndGamifiedForceComponents(out var realisticForce2, out var gamifiedForce2);
			ApplyGlobalForceAtLocalPos(in _shipForceRecord.RudderForce.LocalPosition, in realisticForce2);
			Vec3 localPos = _shipForceRecord.RudderForce.LocalPosition;
			localPos.z = LocalCenterOfMass.z;
			ApplyGlobalForceAtLocalPos(in localPos, in gamifiedForce2);
		}
	}

	private void ApplyAnchorForces()
	{
		Vec3 zero = Vec3.Zero;
		Vec3 zero2 = Vec3.Zero;
		_ = Vec3.Zero;
		if (IsAnchored)
		{
			MatrixFrame bodyWorldTransform = base.GameEntity.GetBodyWorldTransform();
			bodyWorldTransform.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			Vec3 vec = _anchorGlobalFrame.origin - bodyWorldTransform.origin;
			zero = Mass * _anchorForceMultiplier * (1.2f * vec - 3.6f * LinearVelocity);
			zero.z = 0f;
			float a = zero.Normalize();
			float b = 2f * Mass * 9.806f;
			zero = TaleWorlds.Library.MathF.Min(a, b) * zero;
			float y = PhysicsBoundingBoxWithChildrenSize.y;
			float num = 0.6f * y;
			if (vec.LengthSquared <= num * num)
			{
				Vec2 vec2 = bodyWorldTransform.rotation.f.AsVec2.Normalized();
				Vec2 vec3 = AnchorGlobalFrame.rotation.f.AsVec2.Normalized();
				float num2 = TaleWorlds.Library.MathF.Atan2(Vec2.Determinant(in vec2, in vec3), Vec2.DotProduct(vec2, vec3));
				float f = (1.4f * num2 - 4.2f * AngularVelocity.z) * _anchorForceMultiplier;
				f = (float)TaleWorlds.Library.MathF.Sign(f) * TaleWorlds.Library.MathF.Min(System.MathF.PI / 9f, TaleWorlds.Library.MathF.Abs(f));
				Vec3 v = f * Vec3.Up;
				Vec3 vb = bodyWorldTransform.rotation.TransformToLocal(in v);
				Vec3 v2 = Vec3.ElementWiseProduct(MassSpaceInertia, vb);
				Vec3 torqueVec = bodyWorldTransform.rotation.TransformToParent(in v2);
				ApplyGlobalForceAtLocalPos(LocalCenterOfMass, in zero);
				ApplyTorque(in torqueVec);
			}
			else
			{
				Vec3 vec4 = ((Vec3.DotProduct(vec.NormalizedCopy(), bodyWorldTransform.rotation.f) >= 0f) ? 1f : (-1f)) * (0.1f * y * Vec3.Forward);
				ApplyGlobalForceAtLocalPos(LocalCenterOfMass + vec4, in zero);
			}
		}
	}

	public Oriented2DArea GetGlobalMaximal2DArea()
	{
		Vec2 asVec = PhysicsBoundingBoxWithChildren.min.AsVec2;
		Vec2 asVec2 = PhysicsBoundingBoxWithChildren.max.AsVec2;
		Vec2 vec = (asVec2 + asVec) / 2f;
		MatrixFrame bodyWorldTransform = base.GameEntity.GetBodyWorldTransform();
		Vec2 globalForward = bodyWorldTransform.rotation.f.AsVec2.Normalized();
		Vec2 vec2 = -globalForward.LeftVec();
		return new Oriented2DArea(bodyWorldTransform.origin.AsVec2 + vec.X * vec2 + vec.Y * globalForward, in globalForward, asVec2 - asVec);
	}

	public int GetPartIndexAtPosition(Vec3 position)
	{
		Vec2 asVec = PhysicsBoundingBoxWithoutChildren.min.AsVec2;
		Vec2 asVec2 = PhysicsBoundingBoxWithoutChildren.max.AsVec2;
		float num = asVec2.Y - asVec.Y;
		float num2 = asVec2.X - asVec.X;
		float num3 = num / 3f;
		float num4 = num2 / 2f;
		float num5 = position.y + num * 0.5f - (asVec2.y + asVec.y) * 0.5f;
		float num6 = position.x + num2 * 0.5f - (asVec2.x + asVec.x) * 0.5f;
		int value = TaleWorlds.Library.MathF.Floor(num5 / num3);
		int value2 = TaleWorlds.Library.MathF.Floor(num6 / num4);
		value = MBMath.ClampIndex(value, 0, 3);
		value2 = MBMath.ClampIndex(value2, 0, 2);
		return value * 2 + value2;
	}

	private void LoadFloaterVolumes()
	{
		PhysicsBoundingBoxWithChildren = base.GameEntity.GetLocalPhysicsBoundingBox(includeChildren: true);
		PhysicsBoundingBoxWithChildrenSize = PhysicsBoundingBoxWithChildren.max - PhysicsBoundingBoxWithChildren.min;
		PhysicsBoundingBoxWithoutChildren = base.GameEntity.GetLocalPhysicsBoundingBox(includeChildren: false);
		PhysicsBoundingBoxSizeWithoutChildren = PhysicsBoundingBoxWithoutChildren.max - PhysicsBoundingBoxWithoutChildren.min;
		_totalFloaterVolumeCached = 0f;
		WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			if (child.Name == "floater_volume_holder")
			{
				weakGameEntity = child;
				break;
			}
		}
		if (!(weakGameEntity == WeakGameEntity.Invalid))
		{
			int num = weakGameEntity.GetChildren().Count();
			_floaterVolumesShipPartMap = new ShipPart[num];
			_floaterVolumeData = new VolumeDataForSubmergeComputation[num];
			_floaterVolumeDataPinnedGCHandler = GCHandle.Alloc(_floaterVolumeData, GCHandleType.Pinned);
			_floaterVolumeDataPinnedPointer = (UIntPtr)(ulong)(long)_floaterVolumeDataPinnedGCHandler.AddrOfPinnedObject();
			float num2 = float.MaxValue;
			float num3 = float.MinValue;
			for (int i = 0; i < num; i++)
			{
				MatrixFrame localFrame = weakGameEntity.GetChild(i).GetLocalFrame();
				_floaterVolumeData[i].DynamicUpAxis = FloaterVolumeDynamicUpAxis.Z;
				_floaterVolumeData[i].DynamicLocalBottomPos = localFrame.origin;
				_floaterVolumeData[i].LocalFrame = localFrame;
				_floaterVolumeData[i].LocalScale = localFrame.GetScale();
				_floaterVolumeData[i].OutGlobalWaterSurfaceNormal = Vec3.Up;
				_floaterVolumeData[i].InOutWaterHeightWrtVolume = _floaterVolumeData[i].Height * 0.5f;
				_floaterVolumesShipPartMap[i] = (ShipPart)GetPartIndexAtPosition(_floaterVolumeData[i].DynamicLocalBottomPos);
				_totalFloaterVolumeCached += _floaterVolumeData[i].Width * _floaterVolumeData[i].Depth * _floaterVolumeData[i].Height;
				num2 = Math.Min(num2, _floaterVolumeData[i].DynamicLocalBottomPos.z);
				num3 = Math.Max(num3, _floaterVolumeData[i].DynamicLocalBottomPos.z + _floaterVolumeData[i].Height);
			}
			float waterDensity = GetWaterDensity();
			float num4 = Mass * 9.806f;
			float num5 = _totalFloaterVolumeCached * waterDensity * 9.806f;
			_minimumFloaterDurabilityToFloatWhileNotSinking = num4 * 1.1f / num5;
			_shipPartsDurabilities = Enumerable.Repeat(1f, 6).ToArray();
			_shipPartsTargetDurabilities = Enumerable.Repeat(1f, 6).ToArray();
			ComputeAndCacheStabilityAvgSubmergedHeight(num2, num3);
		}
	}

	private void UpdateFloaterVolumeData()
	{
		Mat3 rotation = base.GameEntity.GetBodyWorldTransform().rotation;
		for (int i = 0; i < _floaterVolumeData.Length; i++)
		{
			Vec3 localScale = _floaterVolumeData[i].LocalScale;
			int num = (int)_floaterVolumeData[i].DynamicUpAxis;
			float num2 = localScale[num] * TaleWorlds.Library.MathF.Abs(rotation[num].z);
			for (int j = 1; j < 3; j++)
			{
				int num3 = ((int)_floaterVolumeData[i].DynamicUpAxis + j) % 3;
				float num4 = localScale[num3] * TaleWorlds.Library.MathF.Abs(rotation[num3].z);
				if (num4 > num2 * 1.1f)
				{
					num2 = num4;
					num = num3;
				}
			}
			if ((uint)_floaterVolumeData[i].DynamicUpAxis != (byte)num)
			{
				float num5 = _floaterVolumeData[i].InOutWaterHeightWrtVolume / _floaterVolumeData[i].Height;
				_floaterVolumeData[i].DynamicUpAxis = (FloaterVolumeDynamicUpAxis)num;
				switch (_floaterVolumeData[i].DynamicUpAxis)
				{
				case FloaterVolumeDynamicUpAxis.X:
					_floaterVolumeData[i].DynamicLocalBottomPos = _floaterVolumeData[i].LocalFrame.origin + new Vec3((0f - localScale.x) * 0.5f, 0f, localScale.z * 0.5f);
					break;
				case FloaterVolumeDynamicUpAxis.Y:
					_floaterVolumeData[i].DynamicLocalBottomPos = _floaterVolumeData[i].LocalFrame.origin + new Vec3(0f, (0f - localScale.y) * 0.5f, localScale.z * 0.5f);
					break;
				case FloaterVolumeDynamicUpAxis.Z:
					_floaterVolumeData[i].DynamicLocalBottomPos = _floaterVolumeData[i].LocalFrame.origin;
					break;
				}
				_floaterVolumeData[i].InOutWaterHeightWrtVolume = _floaterVolumeData[i].Height * num5;
			}
		}
	}

	private void ComputeAndCacheStabilityAvgSubmergedHeight(float minimumEntitialFloaterZ, float maximumEntitialFloaterZ)
	{
		float waterDensity = GetWaterDensity();
		float num = Mass * 9.806f;
		float num2 = minimumEntitialFloaterZ + 0.01f;
		float floatingForceMultiplier = _physicsParameters.FloatingForceMultiplier;
		_stabilityAvgSubmergedHeight = maximumEntitialFloaterZ - minimumEntitialFloaterZ;
		_stabilitySubmergedFloaterCount = _floaterVolumeData.Length;
		_minFloaterEntitialBottomPos = minimumEntitialFloaterZ;
		_maxFloaterEntitialTopPos = maximumEntitialFloaterZ;
		for (; maximumEntitialFloaterZ > num2; num2 += 0.01f)
		{
			float num3 = 0f;
			int num4 = 0;
			float num5 = 0f;
			for (int i = 0; i < _floaterVolumeData.Length; i++)
			{
				float num6 = num2 - _floaterVolumeData[i].DynamicLocalBottomPos.z;
				if (num6 > 0f)
				{
					float num7 = Math.Min(num6, _floaterVolumeData[i].Height);
					float num8 = num7 * _floaterVolumeData[i].Width * _floaterVolumeData[i].Depth * waterDensity * 9.806f * floatingForceMultiplier;
					num3 += num7;
					num4++;
					num5 += num8;
				}
			}
			if (num5 >= num)
			{
				StabilitySubmergedHeightOfShip = num2;
				_stabilityAvgSubmergedHeight = num3 / (float)num4;
				_stabilitySubmergedFloaterCount = num4;
				break;
			}
		}
	}

	private void UpdateShipPhysics(NavalPhysicsParameters physicsParameters, ShipPhysicsReference basePhysicsRef)
	{
		_physicsParameters = physicsParameters;
		float overrideMass = _physicsParameters.OverrideMass;
		float num = ((!(overrideMass > 0f)) ? base.GameEntity.Mass : overrideMass);
		num *= _physicsParameters.MassMultiplier;
		Vec3 centerOfMass = base.GameEntity.CenterOfMass;
		base.GameEntity.SetMassAndUpdateInertiaAndCenterOfMass(num);
		base.GameEntity.SetCenterOfMass(centerOfMass);
		_cachedMass = base.GameEntity.Mass;
		Vec3 inertia = Vec3.ElementWiseProduct(base.GameEntity.GetMassSpaceInertia(), _physicsParameters.MomentOfInertiaMultiplier);
		base.GameEntity.SetMassSpaceInertia(inertia);
		LinearDragTerm = basePhysicsRef.LinearDragTerm * _cachedMass;
		LinearDampingTerm = basePhysicsRef.LinearDampingTerm * _cachedMass;
		ConstantLinearDampingTerm = basePhysicsRef.ConstantLinearDampingTerm * _cachedMass;
		base.GameEntity.SetLinearVelocity(Vec3.Zero);
		base.GameEntity.SetAngularVelocity(Vec3.Zero);
		base.GameEntity.DisableGravity();
		PhysicsMaterial physicsMaterial = base.GameEntity.GetPhysicsMaterial();
		base.GameEntity.SetDamping(physicsMaterial.GetLinearDamping(), physicsMaterial.GetAngularDamping());
	}

	private void ComputeContinuousDriftForce(float fixedDt)
	{
		_continuousDriftForceData.ResultForce = Vec3.Zero;
		if (_continuousDriftForceData.DriftSpeed > 0f && IsInitialized && !_buoyancyComputationResult.SimulatingAirFriction && NavalSinkingState == SinkingState.Floating)
		{
			Vec2 vec = base.GameEntity.GetGlobalWindVelocityOfScene().Normalized();
			Vec2 vec2 = vec * _continuousDriftForceData.DriftSpeed;
			Vec2 v = vec2 - LinearVelocity.AsVec2;
			float num = vec.DotProduct(v);
			if (num > 0f)
			{
				Vec2 vec3 = vec * num;
				float num2 = TaleWorlds.Library.MathF.Clamp(LastSubmergedHeightFactorForActuators, 0f, 1f);
				float num3 = TaleWorlds.Library.MathF.Sin(_continuousDriftForceData.DriftForceTimer * System.MathF.PI * 0.1f);
				_continuousDriftForceData.DriftForceTimer += fixedDt * num2 * _continuousDriftForceData.DriftRandom.NextFloat();
				num2 *= num3 * 0.4f + 0.8f;
				float num4 = num3;
				Vec2 vec4 = vec3;
				vec4.RotateCCW(num4 * 0.08726646f);
				_continuousDriftForceData.ResultForce = vec4.ToVec3() * num2 * Mass;
			}
		}
	}

	private void ApplyContinuousDriftForce()
	{
		if (_continuousDriftForceData.ResultForce.LengthSquared > 0f)
		{
			ApplyForceToDynamicBody(in _continuousDriftForceData.ResultForce);
		}
	}

	private static float ComputeLateralDragShift(in Vec3 localVelocity, float maxLateralDragShift, float lateralDragShiftCriticalAngle, float maxLateralShiftSpeed)
	{
		float num = TaleWorlds.Library.MathF.Acos(TaleWorlds.Library.MathF.Max(localVelocity.NormalizedCopy().y, 0f));
		float num2 = 2.5f * num / lateralDragShiftCriticalAngle;
		float num3 = 1f - (float)Math.Exp(0f - num2 * num2);
		return TaleWorlds.Library.MathF.Clamp(localVelocity.y / maxLateralShiftSpeed, 0f, 1f) * num3 * maxLateralDragShift;
	}

	public void SetSinkingState(SinkingState state)
	{
		NavalSinkingState = state;
	}

	public void ForceSink()
	{
		if (_shipPartsTargetDurabilities != null)
		{
			for (int i = 0; i < _shipPartsTargetDurabilities.Length; i++)
			{
				_shipPartsTargetDurabilities[i] = 0f;
			}
		}
		NavalSinkingState = SinkingState.Sinking;
	}

	private static Vec3 SubStepIntegrationStepForLinearFriction(Vec3 absLinearVelocityLocal, float subStepFixedDt, float mass, Vec3 submergedLinearDragTerm, Vec3 submergedLinearDampingTerm, Vec3 submergedConstantLinearDampingTerm, Vec3 submergedFactorLinear)
	{
		Vec3 vb = Vec3.ElementWiseProduct(ComputeVelocityFactorForClampingDrag(absLinearVelocityLocal), submergedFactorLinear);
		Vec3 vb2 = Vec3.ElementWiseProduct(absLinearVelocityLocal, absLinearVelocityLocal);
		return (Vec3.ElementWiseProduct(submergedLinearDragTerm, vb2) + Vec3.ElementWiseProduct(submergedLinearDampingTerm, absLinearVelocityLocal) + submergedConstantLinearDampingTerm + Vec3.ElementWiseProduct(submergedLinearDragTerm, vb)) / mass * subStepFixedDt;
	}

	private static Vec3 SubStepIntegrationStepForAngularFriction(Vec3 absMassLocalAngularVelocity, float subStepFixedDt, Vec3 massLocalInertia, Vec3 angularDragTerm, Vec3 angularDampingTerm, float angularDragYSideComponentTerm, float angularDampingYSideComponentTerm, in BuoyancyComputationResult buoyancyComputationResult)
	{
		Vec3 zero = Vec3.Zero;
		zero.x = angularDragTerm.x * absMassLocalAngularVelocity.x * absMassLocalAngularVelocity.x;
		zero.x += angularDampingTerm.x * absMassLocalAngularVelocity.x;
		zero.x *= buoyancyComputationResult.PitchSubmergedAreaFactor;
		float num = angularDragTerm.y * absMassLocalAngularVelocity.y * absMassLocalAngularVelocity.y;
		num += angularDampingTerm.y * absMassLocalAngularVelocity.y;
		num *= buoyancyComputationResult.RollSubmergedAreaFactor;
		zero.y = num;
		float num2 = angularDragYSideComponentTerm * absMassLocalAngularVelocity.y * absMassLocalAngularVelocity.y;
		num2 += angularDampingYSideComponentTerm * absMassLocalAngularVelocity.y;
		num2 *= buoyancyComputationResult.SubmergedHeightFactor;
		zero.y += num2;
		zero.z = angularDragTerm.z * absMassLocalAngularVelocity.z * absMassLocalAngularVelocity.z;
		zero.z += angularDampingTerm.z * absMassLocalAngularVelocity.z;
		zero.z *= buoyancyComputationResult.SubmergedHeightFactor;
		return Vec3.ElementWiseDivision(zero, massLocalInertia) * subStepFixedDt;
	}

	public static void ComputeLinearDrag(float fixedDt, int substepCount, in Vec3 globalLinearVelocity, in MatrixFrame globalFrame, in float mass, in Vec3 localCenterOfMass, in NavalPhysicsParameters physicsParameters, in BuoyancyComputationResult buoyancyComputationResult, in LinearFrictionTerm linearDragTerm, in LinearFrictionTerm linearDampingTerm, in LinearFrictionTerm constantLinearDampingTerm, float minFloaterEntitialBottomPos, float maxFloaterEntitialTopPos, ref DragForceComputationResult dragComputationResult, out float lateralDragForwardShift)
	{
		Vec3 v = globalLinearVelocity;
		Vec3 localVelocity = globalFrame.rotation.TransformToLocal(in v);
		Vec3 vec = new Vec3(buoyancyComputationResult.SubmergedHeightFactor, buoyancyComputationResult.SubmergedHeightFactor, buoyancyComputationResult.SubmergedFloaterCountFactor);
		LinearFrictionTerm linearFrictionTerm = linearDragTerm.ElementWiseProduct(physicsParameters.LinearFrictionMultiplier);
		LinearFrictionTerm linearFrictionTerm2 = linearDampingTerm.ElementWiseProduct(physicsParameters.LinearFrictionMultiplier);
		LinearFrictionTerm linearFrictionTerm3 = constantLinearDampingTerm.ElementWiseProduct(physicsParameters.LinearFrictionMultiplier);
		Vec3 va = new Vec3((localVelocity.x >= 0f) ? linearFrictionTerm.Right : linearFrictionTerm.Left, (localVelocity.y >= 0f) ? linearFrictionTerm.Forward : linearFrictionTerm.Backward, (localVelocity.z >= 0f) ? linearFrictionTerm.Up : linearFrictionTerm.Down);
		Vec3 va2 = new Vec3((localVelocity.x >= 0f) ? linearFrictionTerm2.Right : linearFrictionTerm2.Left, (localVelocity.y >= 0f) ? linearFrictionTerm2.Forward : linearFrictionTerm2.Backward, (localVelocity.z >= 0f) ? linearFrictionTerm2.Up : linearFrictionTerm2.Down);
		Vec3 va3 = new Vec3((localVelocity.x >= 0f) ? linearFrictionTerm3.Right : linearFrictionTerm3.Left, (localVelocity.y >= 0f) ? linearFrictionTerm3.Forward : linearFrictionTerm3.Backward, (localVelocity.z >= 0f) ? linearFrictionTerm3.Up : linearFrictionTerm3.Down);
		Vec3 submergedLinearDragTerm = Vec3.ElementWiseProduct(va, vec);
		Vec3 submergedLinearDampingTerm = Vec3.ElementWiseProduct(va2, vec);
		Vec3 submergedConstantLinearDampingTerm = Vec3.ElementWiseProduct(va3, vec);
		Vec3 vec2 = Vec3.Abs(localVelocity);
		Vec3 one = Vec3.One;
		one.y *= physicsParameters.ForwardDragMultiplier;
		one *= GetWaterDensity();
		if (globalFrame.rotation.u.z < -0.4f)
		{
			one *= physicsParameters.UpSideDownFrictionMultiplier;
		}
		float subStepFixedDt = fixedDt / (float)substepCount;
		Vec3 vec3 = vec2;
		for (int i = 0; i < substepCount; i++)
		{
			Vec3 va4 = SubStepIntegrationStepForLinearFriction(vec3, subStepFixedDt, mass, submergedLinearDragTerm, submergedLinearDampingTerm, submergedConstantLinearDampingTerm, vec);
			va4 = Vec3.ElementWiseProduct(va4, one);
			vec3 -= va4;
			if (vec3.x < 0f)
			{
				vec3.x = 0f;
			}
			if (vec3.y < 0f)
			{
				vec3.y = 0f;
			}
			if (vec3.z < 0f)
			{
				vec3.z = 0f;
			}
		}
		Vec3 vec4 = (vec2 - vec3) * (mass / fixedDt);
		Vec3 vec5 = mass * vec2;
		Vec3 vec6 = 1f / fixedDt * vec5;
		Vec3 vec7 = new Vec3((float)(-TaleWorlds.Library.MathF.Sign(localVelocity.x)) * TaleWorlds.Library.MathF.Min(vec6.x, vec4.x), (float)(-TaleWorlds.Library.MathF.Sign(localVelocity.y)) * TaleWorlds.Library.MathF.Min(vec6.y, vec4.y), (float)(-TaleWorlds.Library.MathF.Sign(localVelocity.z)) * TaleWorlds.Library.MathF.Min(vec6.z, vec4.z));
		Vec3 lateralDragForceGlobal = vec7.x * globalFrame.rotation.s;
		Vec3 longitudinalDragForceGlobal = vec7.y * globalFrame.rotation.f;
		Vec3 verticalDragForceGlobal = vec7.z * globalFrame.rotation.u;
		float maxLateralShiftSpeed = physicsParameters.MaxLinearSpeedForLateralDragCenterShift * 0.2f;
		lateralDragForwardShift = ComputeLateralDragShift(in localVelocity, physicsParameters.MaxLateralDragShift, physicsParameters.LateralDragShiftCriticalAngle, maxLateralShiftSpeed);
		dragComputationResult.LateralDragForceGlobal = lateralDragForceGlobal;
		dragComputationResult.LongitudinalDragForceGlobal = longitudinalDragForceGlobal;
		dragComputationResult.VerticalDragForceGlobal = verticalDragForceGlobal;
		if (buoyancyComputationResult.SimulatingAirFriction)
		{
			dragComputationResult.CenterOfLateralDragLocal = localCenterOfMass;
			dragComputationResult.CenterOfLongitudinalDragLocal = localCenterOfMass;
			dragComputationResult.CenterOfVerticalDragLocal = localCenterOfMass;
			return;
		}
		dragComputationResult.CenterOfLateralDragLocal.x = buoyancyComputationResult.AvgLocalBuoyancyApplyPosition.x;
		dragComputationResult.CenterOfLateralDragLocal.y = localCenterOfMass.y - Vec3.Forward.y * lateralDragForwardShift;
		dragComputationResult.CenterOfLateralDragLocal.z = buoyancyComputationResult.AvgLocalBuoyancyApplyPosition.z;
		dragComputationResult.CenterOfLongitudinalDragLocal = localCenterOfMass;
		dragComputationResult.CenterOfVerticalDragLocal.x = buoyancyComputationResult.AvgLocalBuoyancyApplyPosition.x;
		dragComputationResult.CenterOfVerticalDragLocal.y = buoyancyComputationResult.AvgLocalBuoyancyApplyPosition.y;
		dragComputationResult.CenterOfVerticalDragLocal.z = ((globalFrame.rotation.u.z >= 0f) ? minFloaterEntitialBottomPos : maxFloaterEntitialTopPos);
	}

	public static void ComputeAngularDrag(float fixedDt, int substepCount, in Vec3 globalAngularVelocity, in MatrixFrame centerOfMassGlobalFrame, in Vec3 massSpaceLocalInertia, in NavalPhysicsParameters physicsParameters, in BuoyancyComputationResult buoyancyComputationResult, in Vec3 angularDragTerm, in Vec3 angularDampingTerm, float angularDragYSideComponentTerm, float angularDampingYSideComponentTerm, ref DragForceComputationResult dragComputationResult)
	{
		Vec3 v = globalAngularVelocity;
		Vec3 vec = centerOfMassGlobalFrame.rotation.TransformToLocal(in v);
		Vec3 vec2 = Vec3.Abs(vec);
		Vec3 vec3 = Vec3.ElementWiseProduct(massSpaceLocalInertia, vec2);
		Vec3 vec4 = 1f / fixedDt * vec3;
		Vec3 vb = physicsParameters.AngularFrictionMultiplier * GetWaterDensity();
		if (centerOfMassGlobalFrame.rotation.u.z < -0.4f)
		{
			vb *= physicsParameters.UpSideDownFrictionMultiplier;
		}
		float subStepFixedDt = fixedDt / (float)substepCount;
		Vec3 vec5 = vec2;
		for (int i = 0; i < substepCount; i++)
		{
			Vec3 va = SubStepIntegrationStepForAngularFriction(vec5, subStepFixedDt, massSpaceLocalInertia, angularDragTerm, angularDampingTerm, angularDragYSideComponentTerm, angularDampingYSideComponentTerm, in buoyancyComputationResult);
			va = Vec3.ElementWiseProduct(va, vb);
			vec5 -= va;
			if (vec5.x < 0f)
			{
				vec5.x = 0f;
			}
			if (vec5.y < 0f)
			{
				vec5.y = 0f;
			}
			if (vec5.z < 0f)
			{
				vec5.z = 0f;
			}
		}
		Vec3 vec6 = Vec3.ElementWiseProduct(vec2 - vec5, massSpaceLocalInertia) / fixedDt;
		Vec3 v2 = new Vec3((float)(-TaleWorlds.Library.MathF.Sign(vec.x)) * TaleWorlds.Library.MathF.Min(vec4.x, vec6.x), (float)(-TaleWorlds.Library.MathF.Sign(vec.y)) * TaleWorlds.Library.MathF.Min(vec4.y, vec6.y), (float)(-TaleWorlds.Library.MathF.Sign(vec.z)) * TaleWorlds.Library.MathF.Min(vec4.z, vec6.z));
		Vec3 angularDragTorqueGlobal = centerOfMassGlobalFrame.rotation.TransformToParent(in v2);
		dragComputationResult.AngularDragTorqueGlobal = angularDragTorqueGlobal;
	}

	private static Vec3 ComputeVelocityFactorForClampingDrag(Vec3 absLinearVelocityLocal)
	{
		Vec3 vec = new Vec3(7f, 20f, 20f);
		Vec3 zero = Vec3.Zero;
		for (int i = 0; i < 3; i++)
		{
			float num = absLinearVelocityLocal[i] - vec[i];
			if (num > 0f)
			{
				zero[i] = TaleWorlds.Library.MathF.Pow(num, 4f);
			}
		}
		return zero;
	}
}
