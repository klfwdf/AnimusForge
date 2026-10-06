using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.ShipActuators;

public class ShipActuators
{
	private struct RowingSoundEventData
	{
		internal float SoundEventRowingPowerParam;

		internal int NumberOfActiveOars;

		internal bool ShouldTriggerOarSound;

		internal bool IsOarsInWater;

		internal Vec3 RowingSoundEventPositions;

		internal int FurthestOarIndex;

		internal int ClosestOarIndex;

		internal SoundEvent OarsSoundEvents;
	}

	public struct OarPhaseData
	{
		public float CurPhase;

		public float LastNonZeroRevolutionRate;

		public bool LockedToTargetPhase;

		public float CycleArcSizeMult;
	}

	public struct OarAnimKeyFrame
	{
		public float KeyProgress;

		public float Speed;

		public OarAnimKeyFrame(float keyProgress, float speed)
		{
			KeyProgress = keyProgress;
			Speed = speed;
		}
	}

	private static class OarRowSpeedAnimationManager
	{
		public static OarAnimKeyFrame[] ForwardPhaseSpeedAnim = new OarAnimKeyFrame[9]
		{
			new OarAnimKeyFrame(0f, 1.5f),
			new OarAnimKeyFrame(0.15f, 1.6f),
			new OarAnimKeyFrame(0.25f, 1.2f),
			new OarAnimKeyFrame(0.3f, 1f),
			new OarAnimKeyFrame(0.65f, 1f),
			new OarAnimKeyFrame(0.7f, 1.4f),
			new OarAnimKeyFrame(0.75f, 1.5f),
			new OarAnimKeyFrame(0.9f, 1.5f),
			new OarAnimKeyFrame(1f, 1.5f)
		};

		public static OarAnimKeyFrame[] PartialPhaseSpeedAnim = new OarAnimKeyFrame[9]
		{
			new OarAnimKeyFrame(0f, 1.5f),
			new OarAnimKeyFrame(0.15f, 1.6f),
			new OarAnimKeyFrame(0.25f, 1.2f),
			new OarAnimKeyFrame(0.3f, 1f),
			new OarAnimKeyFrame(0.65f, 1f),
			new OarAnimKeyFrame(0.7f, 1.4f),
			new OarAnimKeyFrame(0.75f, 1.5f),
			new OarAnimKeyFrame(0.9f, 1.5f),
			new OarAnimKeyFrame(1f, 1.5f)
		};

		public static OarAnimKeyFrame[] OnPointTurnPhaseSpeedAnim = new OarAnimKeyFrame[9]
		{
			new OarAnimKeyFrame(0f, 1.5f),
			new OarAnimKeyFrame(0.15f, 1.6f),
			new OarAnimKeyFrame(0.25f, 1.2f),
			new OarAnimKeyFrame(0.3f, 1f),
			new OarAnimKeyFrame(0.65f, 1f),
			new OarAnimKeyFrame(0.7f, 1.4f),
			new OarAnimKeyFrame(0.75f, 1.5f),
			new OarAnimKeyFrame(0.9f, 1.5f),
			new OarAnimKeyFrame(1f, 1.5f)
		};
	}

	private static readonly int[] _rowingSoundEventIds = new int[2]
	{
		SoundManager.GetEventGlobalIndex("event:/mission/movement/vessel/rowing/rowing_left_side"),
		SoundManager.GetEventGlobalIndex("event:/mission/movement/vessel/rowing/rowing_right_side")
	};

	public const string SailTagPrefix = "sail_center_";

	public const string RudderStockPositionTag = "rudder_stock";

	private const float MinSpeedToUseBothOarsToTurn = 6f;

	private static readonly int _rudderSoundEventId = SoundEvent.GetEventIdFromString("event:/mission/movement/vessel/ship_steering");

	private static readonly int _shipPresenceSoundEventId = SoundEvent.GetEventIdFromString("event:/mission/movement/vessel/basic_ship_presence");

	private float _rudderLocalRotation;

	private float _lastRudderLocalRotation;

	private float _lastAddedFromInputRudderLocalRotation;

	private float _lastTargetRudderStabilityLocalRotation;

	private Vec3 _rudderStockLocalPosition;

	private readonly MissionShip _ownerMissionShip;

	private readonly Scene _cachedOwnerScene;

	private float _rowersPhase;

	private float _lastFramePhaseRate;

	private bool _evenCycle;

	private OarPhaseData _leftPhaseData;

	private OarPhaseData _rightPhaseData;

	private readonly MBList<MissionSail> _sails = new MBList<MissionSail>();

	private readonly MBList<(GameEntity entity, MissionOar oar)> _leftSideOars = new MBList<(GameEntity, MissionOar)>();

	private readonly MBList<(GameEntity entity, MissionOar oar)> _rightSideOars = new MBList<(GameEntity, MissionOar)>();

	private MBList<ShipForce> _leftOarForces = new MBList<ShipForce>();

	private MBList<ShipForce> _rightOarForces = new MBList<ShipForce>();

	private MBList<ShipForce> _sailForces = new MBList<ShipForce>();

	private ShipForce _rudderShipForce;

	private OarSidePhaseController _leftOarsPhaseController;

	private OarSidePhaseController _rightOarsPhaseController;

	private float _oarsmenForceMultiplier;

	private float _oarsmenSpeedMultiplier;

	private float _oarsTipSpeedReferenceMultiplier;

	private float _oarFrictionMultiplier;

	private float _oarAppliedForceMultiplierForStoryMission;

	private float _maxOarLength;

	private readonly MBList<(MissionShip ship, OarSidePhaseController.OarSide shipSide)> _nearbyShips;

	private float _timeLeftToUpdateNearbyShips;

	private readonly NavalShipsLogic _navalShipsLogic;

	private Vec3 _leftSideAverageOarLocalPos;

	private Vec3 _rightSideAverageOarLocalPos;

	private SoundEvent _rudderSoundEvent;

	private SoundEvent _shipPresenceSoundEvent;

	private RowingSoundEventData[] _rowingSoundEventData = new RowingSoundEventData[2];

	private float _rudderStressSoundParam;

	private float _shipPresenceSoundParam;

	public int VisualRudderPullDirection { get; private set; }

	public float VisualRudderLocalRotation { get; private set; }

	public MBReadOnlyList<MissionSail> Sails => _sails;

	public ShipActuators(MissionShip ownerShip)
	{
		_ownerMissionShip = ownerShip;
		_cachedOwnerScene = ownerShip.GameEntity.Scene;
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		OnShipObjectUpdated();
		_rowersPhase = System.MathF.PI;
		_evenCycle = true;
		_nearbyShips = new MBList<(MissionShip, OarSidePhaseController.OarSide)>();
		_timeLeftToUpdateNearbyShips = 0f;
	}

	public void OnShipObjectUpdated()
	{
		LoadRudder();
		LoadOars();
		LoadSails();
	}

	public ShipForceRecord OnParallelFixedTick(float fixedDt, in ShipActuatorRecord actuatorInput)
	{
		MatrixFrame shipEntityGlobalFrame = _ownerMissionShip.GameEntity.GetBodyWorldTransform();
		Vec3 shipLinearVelocityGlobal;
		Vec3 shipAngularVelocityGlobal;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			shipLinearVelocityGlobal = _ownerMissionShip.GameEntity.GetLinearVelocityMT();
			shipAngularVelocityGlobal = _ownerMissionShip.GameEntity.GetAngularVelocityMT();
		}
		float shipForwardSpeed = Vec3.DotProduct(shipLinearVelocityGlobal, shipEntityGlobalFrame.rotation.f);
		FixedUpdateRowers(fixedDt, in actuatorInput, in shipEntityGlobalFrame, shipForwardSpeed);
		if (_sails.Count > 0)
		{
			FixedUpdateSails(fixedDt, in actuatorInput, in shipLinearVelocityGlobal, in shipAngularVelocityGlobal);
		}
		FixedUpdateRudder(fixedDt, in actuatorInput, in shipEntityGlobalFrame, shipForwardSpeed);
		MBList<ShipForce> leftOarForces = _leftOarForces;
		MBList<ShipForce> rightOarForces = _rightOarForces;
		MBReadOnlyList<ShipForce> sailForces = _sailForces;
		return new ShipForceRecord(leftOarForces, rightOarForces, in sailForces, in _rudderShipForce);
	}

	public void OnTickParallel(float dt)
	{
		OnParallelTickRowers(dt);
		OnParallelTickRudder(dt);
	}

	private void CalculateOarSoundPositionsAndParams()
	{
		if (!_ownerMissionShip.ShouldUpdateSoundPos)
		{
			return;
		}
		if (_ownerMissionShip.Physics.LastSubmergedHeightFactorForActuators > 0.01f)
		{
			MatrixFrame bodyWorldTransform = _ownerMissionShip.GameEntity.GetBodyWorldTransform();
			for (int i = 0; i < 2; i++)
			{
				if (_rowingSoundEventData[i].NumberOfActiveOars <= 0)
				{
					continue;
				}
				MBList<ShipForce> mBList = ((i == 0) ? _leftOarForces : _rightOarForces);
				float numberToCheck = ((i == 0) ? _leftOarsPhaseController.VisualPhase : _rightOarsPhaseController.VisualPhase);
				ShipForce shipForce = mBList[_rowingSoundEventData[i].ClosestOarIndex];
				Vec3 closestOarGlobalPos = bodyWorldTransform.TransformToParent(in shipForce.LocalPosition);
				shipForce = mBList[_rowingSoundEventData[i].FurthestOarIndex];
				Vec3 furthestOarGlobalPos = bodyWorldTransform.TransformToParent(in shipForce.LocalPosition);
				_rowingSoundEventData[i].RowingSoundEventPositions = CalculateRowingSoundPosition(in closestOarGlobalPos, in furthestOarGlobalPos);
				if (MBMath.IsBetweenInclusive(numberToCheck, -1.3962634f, 1.3962634f))
				{
					if (!_rowingSoundEventData[i].IsOarsInWater)
					{
						_rowingSoundEventData[i].SoundEventRowingPowerParam = CalculateOarRowingPowerSoundParameter((OarSidePhaseController.OarSide)i, in _rowingSoundEventData[i].RowingSoundEventPositions);
						if (_rowingSoundEventData[i].SoundEventRowingPowerParam > 0f)
						{
							_rowingSoundEventData[i].ShouldTriggerOarSound = true;
							_rowingSoundEventData[i].IsOarsInWater = true;
						}
					}
				}
				else
				{
					_rowingSoundEventData[i].IsOarsInWater = false;
				}
			}
		}
		else
		{
			_rowingSoundEventData[0].IsOarsInWater = false;
			_rowingSoundEventData[0].ShouldTriggerOarSound = false;
			_rowingSoundEventData[1].IsOarsInWater = false;
			_rowingSoundEventData[1].ShouldTriggerOarSound = false;
		}
	}

	internal void Update(float dt)
	{
		for (int i = 0; i < _sails.Count; i++)
		{
			_sails[i].Update(dt);
		}
		UpdateSoundEventPositions();
	}

	private void FixedUpdateSails(float fixedDt, in ShipActuatorRecord actuatorInput, in Vec3 shipLinearVelocityGlobal, in Vec3 shipAngularVelocityGlobal)
	{
		for (int i = 0; i < _sails.Count; i++)
		{
			MissionSail missionSail = _sails[i];
			missionSail.FixedUpdate(fixedDt, in actuatorInput, in shipLinearVelocityGlobal, in shipAngularVelocityGlobal);
			_sailForces[i] = missionSail.Force;
		}
	}

	private void UpdateSoundEventPositions()
	{
		if (_ownerMissionShip.ShouldUpdateSoundPos)
		{
			if (_rudderSoundEvent == null)
			{
				_rudderSoundEvent = SoundEvent.CreateEvent(_rudderSoundEventId, _cachedOwnerScene);
				_shipPresenceSoundEvent = SoundEvent.CreateEvent(_shipPresenceSoundEventId, _cachedOwnerScene);
				_rudderSoundEvent.Play();
				_shipPresenceSoundEvent.Play();
			}
			for (int i = 0; i < 2; i++)
			{
				if (_rowingSoundEventData[i].ShouldTriggerOarSound)
				{
					_rowingSoundEventData[i].OarsSoundEvents?.Stop();
					_rowingSoundEventData[i].OarsSoundEvents = SoundEvent.CreateEvent(_rowingSoundEventIds[i], _cachedOwnerScene);
					_rowingSoundEventData[i].OarsSoundEvents.SetParameter("RowingPower", _rowingSoundEventData[i].SoundEventRowingPowerParam);
					_rowingSoundEventData[i].OarsSoundEvents.SetParameter("OarsmanLevel", _rowingSoundEventData[i].NumberOfActiveOars);
					_rowingSoundEventData[i].OarsSoundEvents.SetPosition(_rowingSoundEventData[i].RowingSoundEventPositions);
					_rowingSoundEventData[i].OarsSoundEvents.Play();
					_rowingSoundEventData[i].ShouldTriggerOarSound = false;
				}
				else
				{
					_rowingSoundEventData[i].OarsSoundEvents?.SetPosition(_rowingSoundEventData[i].RowingSoundEventPositions);
				}
			}
			MatrixFrame globalFrame = _ownerMissionShip.GlobalFrame;
			Vec3 position = globalFrame.TransformToParent(_ownerMissionShip.GameEntity.CenterOfMass);
			position.z += 3f;
			_shipPresenceSoundEvent.SetPosition(position);
			_shipPresenceSoundEvent.SetParameter("ForceContinuous", _shipPresenceSoundParam);
			_rudderSoundEvent.SetPosition(globalFrame.TransformToParent(in _rudderShipForce.LocalPosition));
			_rudderSoundEvent.SetParameter("RudderStress", _rudderStressSoundParam);
		}
		else
		{
			_rowingSoundEventData[0].OarsSoundEvents?.Stop();
			_rowingSoundEventData[1].OarsSoundEvents?.Stop();
			_rudderSoundEvent?.Stop();
			_shipPresenceSoundEvent?.Stop();
			_rowingSoundEventData[0].OarsSoundEvents = null;
			_rowingSoundEventData[1].OarsSoundEvents = null;
			_rudderSoundEvent = null;
			_shipPresenceSoundEvent = null;
		}
	}

	private Vec3 CalculateRowingSoundPosition(in Vec3 closestOarGlobalPos, in Vec3 furthestOarGlobalPos)
	{
		Vec3 origin = SoundManager.GetListenerFrame().origin;
		Vec3 vec = furthestOarGlobalPos - closestOarGlobalPos;
		float value = Vec3.DotProduct(origin - closestOarGlobalPos, vec) / vec.LengthSquared;
		return closestOarGlobalPos + TaleWorlds.Library.MathF.Clamp(value, 0f, 1f) * vec;
	}

	private float CalculateOarRowingPowerSoundParameter(OarSidePhaseController.OarSide oarSide, in Vec3 soundPos)
	{
		MBList<ShipForce> mBList = null;
		MBList<(GameEntity, MissionOar)> mBList2 = null;
		switch (oarSide)
		{
		case OarSidePhaseController.OarSide.Left:
			mBList = _leftOarForces;
			mBList2 = _leftSideOars;
			break;
		case OarSidePhaseController.OarSide.Right:
			mBList = _rightOarForces;
			mBList2 = _rightSideOars;
			break;
		}
		MatrixFrame bodyWorldTransform = _ownerMissionShip.GameEntity.GetBodyWorldTransform();
		float num = 0f;
		float num2 = 0f;
		float num3 = -1f;
		for (int i = 0; i < mBList.Count; i++)
		{
			float num4 = (mBList2[i].Item2.IsExtracted ? 5000f : 0f);
			Vec3 vec = soundPos;
			ShipForce shipForce = mBList[i];
			float num5 = vec.Distance(bodyWorldTransform.TransformToParent(in shipForce.LocalPosition));
			if (num5 < 0.010000001f && num4 > 0f)
			{
				num3 = num4;
				break;
			}
			if (num5 > 0.010000001f)
			{
				float num6 = 1f / num5;
				num += num6 * num4;
				num2 += num6;
			}
		}
		if (num3 == -1f && num2 != 0f)
		{
			num3 = num / num2;
		}
		return TaleWorlds.Library.MathF.Min(num3 * 0.1f, 500f);
	}

	private void LoadOars()
	{
		MBList<ShipOarDeck> mBList = _ownerMissionShip.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<ShipOarDeck>();
		_leftOarsPhaseController = new OarSidePhaseController(_ownerMissionShip, OarSidePhaseController.OarSide.Left);
		_leftSideOars.Clear();
		_leftOarForces.Clear();
		_rightOarsPhaseController = new OarSidePhaseController(_ownerMissionShip, OarSidePhaseController.OarSide.Right);
		_rightSideOars.Clear();
		_rightOarForces.Clear();
		_maxOarLength = 0f;
		for (int i = 0; i < mBList.Count; i++)
		{
			ShipOarDeck shipOarDeck = mBList[i];
			OarDeckParameters parameters = shipOarDeck.GetParameters();
			_maxOarLength = TaleWorlds.Library.MathF.Max(_maxOarLength, parameters.OarLength);
			List<WeakGameEntity> list = shipOarDeck.GameEntity.CollectChildrenEntitiesWithTag("oar_gate_left");
			List<WeakGameEntity> list2 = shipOarDeck.GameEntity.CollectChildrenEntitiesWithTag("oar_gate_right");
			foreach (WeakGameEntity item in list)
			{
				GameEntity gameEntity = GameEntity.CreateFromWeakEntity(item);
				MissionOar missionOar = MissionOar.CreateShipOar(_ownerMissionShip, gameEntity, parameters, _leftOarsPhaseController);
				GetOarScriptFromEntity(item).InitializeOar(missionOar);
				_leftSideOars.Add((gameEntity, missionOar));
				_leftOarForces.Add(ShipForce.None(ShipForce.SourceType.Oar));
			}
			foreach (WeakGameEntity item2 in list2)
			{
				GameEntity gameEntity2 = GameEntity.CreateFromWeakEntity(item2);
				MissionOar missionOar2 = MissionOar.CreateShipOar(_ownerMissionShip, gameEntity2, parameters, _rightOarsPhaseController);
				GetOarScriptFromEntity(item2).InitializeOar(missionOar2);
				_rightSideOars.Add((gameEntity2, missionOar2));
				_rightOarForces.Add(ShipForce.None(ShipForce.SourceType.Oar));
			}
		}
		GenerateAverageSideDeckParameters(out var leftSideAverageDeckParameters, out var rightSideAverageDeckParameters, _leftSideOars, _rightSideOars);
		_leftOarsPhaseController.SetAverageOarDeckParameters(leftSideAverageDeckParameters);
		_rightOarsPhaseController.SetAverageOarDeckParameters(rightSideAverageDeckParameters);
		_rowingSoundEventData[0].ClosestOarIndex = 0;
		_rowingSoundEventData[1].ClosestOarIndex = 0;
		_rowingSoundEventData[0].FurthestOarIndex = 0;
		_rowingSoundEventData[1].FurthestOarIndex = 0;
		_leftSideAverageOarLocalPos = Vec3.Zero;
		_rightSideAverageOarLocalPos = Vec3.Zero;
		for (int j = 0; j < _leftSideOars.Count; j++)
		{
			Vec3 bladeContact = _leftSideOars[j].oar.BladeContact;
			if (bladeContact.DistanceSquared(_rudderStockLocalPosition) > _leftSideOars[_rowingSoundEventData[0].FurthestOarIndex].oar.BladeContact.DistanceSquared(_rudderStockLocalPosition))
			{
				_rowingSoundEventData[0].FurthestOarIndex = j;
			}
			_leftSideAverageOarLocalPos += bladeContact;
		}
		_leftSideAverageOarLocalPos /= (float)_leftSideOars.Count;
		for (int k = 0; k < _leftSideOars.Count; k++)
		{
			Vec3 bladeContact2 = _leftSideOars[k].oar.BladeContact;
			Vec3 bladeContact3 = _leftSideOars[_rowingSoundEventData[0].FurthestOarIndex].oar.BladeContact;
			if (bladeContact2.DistanceSquared(bladeContact3) > _leftSideOars[_rowingSoundEventData[0].ClosestOarIndex].oar.BladeContact.DistanceSquared(bladeContact3))
			{
				_rowingSoundEventData[0].ClosestOarIndex = k;
			}
		}
		for (int l = 0; l < _rightSideOars.Count; l++)
		{
			Vec3 bladeContact4 = _rightSideOars[l].oar.BladeContact;
			if (bladeContact4.DistanceSquared(_rudderStockLocalPosition) > _rightSideOars[_rowingSoundEventData[1].FurthestOarIndex].oar.BladeContact.DistanceSquared(_rudderStockLocalPosition))
			{
				_rowingSoundEventData[1].FurthestOarIndex = l;
			}
			_rightSideAverageOarLocalPos += bladeContact4;
		}
		_rightSideAverageOarLocalPos /= (float)_rightSideOars.Count;
		for (int m = 0; m < _rightSideOars.Count; m++)
		{
			Vec3 bladeContact5 = _rightSideOars[m].oar.BladeContact;
			Vec3 bladeContact6 = _rightSideOars[_rowingSoundEventData[1].FurthestOarIndex].oar.BladeContact;
			if (bladeContact5.DistanceSquared(bladeContact6) > _rightSideOars[_rowingSoundEventData[1].ClosestOarIndex].oar.BladeContact.DistanceSquared(bladeContact6))
			{
				_rowingSoundEventData[1].ClosestOarIndex = m;
			}
		}
		float num = 1f;
		float oarsmenSpeedMultiplier = 1f;
		if (_ownerMissionShip.ShipOrigin != null)
		{
			num = 1f + _ownerMissionShip.ShipOrigin.MaxOarForceFactor;
			oarsmenSpeedMultiplier = 1f + _ownerMissionShip.ShipOrigin.MaxOarPowerFactor;
		}
		_oarsmenForceMultiplier = _ownerMissionShip.MissionShipObject.OarsmenForceMultiplier * num;
		_oarsmenSpeedMultiplier = oarsmenSpeedMultiplier;
		_oarFrictionMultiplier = _ownerMissionShip.MissionShipObject.OarFrictionMultiplier;
		Vec3 vec = MissionOar.ComputeBladeContactVelocityAux(leftSideAverageDeckParameters, 0f, System.MathF.PI * 2f);
		_oarsTipSpeedReferenceMultiplier = TaleWorlds.Library.MathF.Abs(_ownerMissionShip.MissionShipObject.OarsTipSpeed / vec.y);
		_oarAppliedForceMultiplierForStoryMission = 1f;
	}

	public void OnShipRemoved(MissionShip ship)
	{
		_nearbyShips.Clear();
		_timeLeftToUpdateNearbyShips = 0f;
	}

	private static OarDeckParameters GenerateAverageSideDeckParametersAux(MBList<(GameEntity entity, MissionOar oar)> sideOars)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		foreach (var sideOar in sideOars)
		{
			OarDeckParameters deckParameters = sideOar.oar.DeckParameters;
			num += deckParameters.VerticalBaseAngle;
			num2 += deckParameters.LateralBaseAngle;
			num3 += deckParameters.VerticalRotationAngle;
			num4 += deckParameters.LateralRotationAngle;
			num5 += deckParameters.OarLength;
			num6 += deckParameters.RetractionRate;
			num7 += deckParameters.RetractionOffset;
		}
		float num8 = 1f / (float)sideOars.Count;
		num *= num8;
		num2 *= num8;
		num3 *= num8;
		num4 *= num8;
		num5 *= num8;
		num6 *= num8;
		num7 *= num8;
		return new OarDeckParameters(num, num2, num3, num4, num5, num6, num7);
	}

	private static void GenerateAverageSideDeckParameters(out OarDeckParameters leftSideAverageDeckParameters, out OarDeckParameters rightSideAverageDeckParameters, MBList<(GameEntity entity, MissionOar oar)> leftSideOars, MBList<(GameEntity entity, MissionOar oar)> rightSideOars)
	{
		leftSideAverageDeckParameters = GenerateAverageSideDeckParametersAux(leftSideOars);
		rightSideAverageDeckParameters = GenerateAverageSideDeckParametersAux(rightSideOars);
	}

	private void LoadSails()
	{
		_sails.Clear();
		_sailForces.Clear();
		WeakGameEntity gameEntity = _ownerMissionShip.GameEntity;
		for (int i = 0; i < _ownerMissionShip.MissionShipObject.Sails.Count; i++)
		{
			ShipSail sailObject = _ownerMissionShip.MissionShipObject.Sails[i];
			string text = "sail_center_" + i;
			List<WeakGameEntity> list = gameEntity.CollectChildrenEntitiesWithTag(text);
			if (list.Count > 0)
			{
				SailVisual firstScriptOfType = list[0].GetFirstScriptOfType<SailVisual>();
				firstScriptOfType.SoundsEnabled = true;
				list[0].CreateAndAddScriptComponent("MissionSail", callScriptCallbacks: true);
				MissionSail firstScriptOfType2 = list[0].GetFirstScriptOfType<MissionSail>();
				firstScriptOfType2.InitWithVariables(sailObject, _ownerMissionShip, firstScriptOfType);
				_sails.Add(firstScriptOfType2);
				_sailForces.Add(ShipForce.None());
			}
			else
			{
				Debug.FailedAssert("Unable to find a sail entity on ship prefab (" + gameEntity.GetPrefabName() + ") with tag: " + text, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\ShipActuators\\ShipActuators.cs", "LoadSails", 643);
			}
		}
	}

	private void LoadRudder()
	{
		WeakGameEntity gameEntity = _ownerMissionShip.GameEntity;
		if (_ownerMissionShip.MissionShipObject.HasValidRudderStockPosition)
		{
			_rudderStockLocalPosition = _ownerMissionShip.MissionShipObject.RudderStockPosition;
			return;
		}
		List<WeakGameEntity> list = gameEntity.CollectChildrenEntitiesWithTag("rudder_stock");
		if (list.Count > 0)
		{
			_rudderStockLocalPosition = list[0].GetFrame().origin;
			return;
		}
		Debug.FailedAssert("Stock position is not defined for ship: " + gameEntity.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\ShipActuators\\ShipActuators.cs", "LoadRudder", 665);
		_rudderStockLocalPosition = Vec3.Zero;
	}

	private void OnParallelTickRowers(float dt)
	{
		_leftOarsPhaseController.OnParallelTick(dt);
		_rightOarsPhaseController.OnParallelTick(dt);
		for (int i = 0; i < _leftSideOars.Count; i++)
		{
			_leftSideOars[i].oar.OnParallelTick(dt);
		}
		for (int j = 0; j < _rightSideOars.Count; j++)
		{
			_rightSideOars[j].oar.OnParallelTick(dt);
		}
	}

	public static void BlendPhaseTo(ref OarPhaseData phaseData, float targetPhase, float alphaInRadOverSeconds, float maxAlphaInRadOverSeconds, float fixedDt, bool toFullStop, bool isPartialStop)
	{
		targetPhase = MBMath.WrapAngleSafe(targetPhase);
		float num = TaleWorlds.Library.MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(phaseData.CurPhase, targetPhase));
		if (phaseData.LockedToTargetPhase && num > alphaInRadOverSeconds * fixedDt * 2f)
		{
			phaseData.LockedToTargetPhase = false;
		}
		bool flag = false;
		if (!phaseData.LockedToTargetPhase)
		{
			if (toFullStop)
			{
				alphaInRadOverSeconds = maxAlphaInRadOverSeconds * 1.4f;
			}
			else if (isPartialStop)
			{
				alphaInRadOverSeconds = maxAlphaInRadOverSeconds * 1.4f;
			}
			else
			{
				alphaInRadOverSeconds = maxAlphaInRadOverSeconds * 1.3f;
				flag = true;
			}
		}
		if (!phaseData.LockedToTargetPhase)
		{
			float smallestDifferenceBetweenTwoAngles = MBMath.GetSmallestDifferenceBetweenTwoAngles(MBMath.WrapAngleSafe(phaseData.CurPhase + alphaInRadOverSeconds * fixedDt), targetPhase);
			float smallestDifferenceBetweenTwoAngles2 = MBMath.GetSmallestDifferenceBetweenTwoAngles(MBMath.WrapAngleSafe(phaseData.CurPhase - alphaInRadOverSeconds * fixedDt), targetPhase);
			float smallestDifferenceBetweenTwoAngles3 = MBMath.GetSmallestDifferenceBetweenTwoAngles(phaseData.CurPhase, targetPhase);
			float num2 = (flag ? 0.005f : 0.3f);
			float num3 = TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles3) / alphaInRadOverSeconds;
			float b = ((toFullStop || isPartialStop) ? 0.03f : 0.1f);
			float num4 = (((TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles3) > System.MathF.PI / 2f) ? (TaleWorlds.Library.MathF.Sign(phaseData.LastNonZeroRevolutionRate) >= 0) : (TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles) < TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles2))) ? ((!(num3 > num2)) ? (alphaInRadOverSeconds * TaleWorlds.Library.MathF.Max(num3 / num2, b)) : alphaInRadOverSeconds) : ((!(num3 > num2)) ? ((0f - alphaInRadOverSeconds) * TaleWorlds.Library.MathF.Max(num3 / num2, b)) : (0f - alphaInRadOverSeconds)));
			if (TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles3 / num4) <= 0f)
			{
				phaseData.LockedToTargetPhase = true;
			}
			float smallestDifferenceBetweenTwoAngles4 = MBMath.GetSmallestDifferenceBetweenTwoAngles(MBMath.WrapAngleSafe(phaseData.CurPhase + num4 * fixedDt), targetPhase);
			float smallestDifferenceBetweenTwoAngles5 = MBMath.GetSmallestDifferenceBetweenTwoAngles(MBMath.WrapAngleSafe(phaseData.CurPhase - num4 * fixedDt), targetPhase);
			if (smallestDifferenceBetweenTwoAngles4 * smallestDifferenceBetweenTwoAngles5 <= 0f && TaleWorlds.Library.MathF.Abs(smallestDifferenceBetweenTwoAngles3) <= TaleWorlds.Library.MathF.Abs(num4 * fixedDt))
			{
				phaseData.LockedToTargetPhase = true;
			}
			phaseData.CurPhase += num4 * fixedDt;
			phaseData.CurPhase = MBMath.WrapAngleSafe(phaseData.CurPhase);
		}
		if (phaseData.LockedToTargetPhase)
		{
			phaseData.CurPhase = targetPhase;
		}
		phaseData.CurPhase = MBMath.WrapAngleSafe(phaseData.CurPhase);
		float valueTo = 1f;
		float num5 = 0.9599311f;
		if (!phaseData.LockedToTargetPhase && toFullStop && phaseData.CurPhase < num5 && phaseData.CurPhase > 0f - num5)
		{
			valueTo = 0f;
		}
		phaseData.CycleArcSizeMult = TaleWorlds.Library.MathF.Lerp(phaseData.CycleArcSizeMult, valueTo, fixedDt * 1.2f);
	}

	private static float GetRowSpeedAccordingToPhase(float phase, bool forwards, bool partialTurn, bool onPointTurn)
	{
		OarAnimKeyFrame[] array;
		if (onPointTurn)
		{
			array = OarRowSpeedAnimationManager.OnPointTurnPhaseSpeedAnim;
			forwards = true;
		}
		else
		{
			array = (partialTurn ? OarRowSpeedAnimationManager.PartialPhaseSpeedAnim : OarRowSpeedAnimationManager.ForwardPhaseSpeedAnim);
		}
		float num = ((forwards ? phase : MBMath.WrapAngleSafe(System.MathF.PI * 2f - phase)) + System.MathF.PI) / (System.MathF.PI * 2f);
		if (num >= 1f)
		{
			num -= 1f;
		}
		float result = 1f;
		if (forwards)
		{
			for (int i = 0; i < array.Length - 1; i++)
			{
				OarAnimKeyFrame oarAnimKeyFrame = array[i];
				OarAnimKeyFrame oarAnimKeyFrame2 = array[i + 1];
				if (oarAnimKeyFrame.KeyProgress <= num && num < oarAnimKeyFrame2.KeyProgress)
				{
					float num2 = oarAnimKeyFrame2.KeyProgress - oarAnimKeyFrame.KeyProgress;
					float amount = (num - oarAnimKeyFrame.KeyProgress) / num2;
					result = TaleWorlds.Library.MathF.Lerp(oarAnimKeyFrame.Speed, oarAnimKeyFrame2.Speed, amount);
					break;
				}
			}
		}
		else
		{
			for (int num3 = array.Length - 1; num3 >= 1; num3--)
			{
				OarAnimKeyFrame oarAnimKeyFrame3 = array[num3];
				OarAnimKeyFrame oarAnimKeyFrame4 = array[num3 - 1];
				if (oarAnimKeyFrame4.KeyProgress <= num && num < oarAnimKeyFrame3.KeyProgress)
				{
					float num4 = oarAnimKeyFrame3.KeyProgress - oarAnimKeyFrame4.KeyProgress;
					float amount2 = (num - oarAnimKeyFrame4.KeyProgress) / num4;
					result = TaleWorlds.Library.MathF.Lerp(oarAnimKeyFrame4.Speed, oarAnimKeyFrame3.Speed, amount2);
					break;
				}
			}
		}
		return result;
	}

	private void FixedUpdateRowers(float fixedDt, in ShipActuatorRecord actuatorInput, in MatrixFrame shipEntityGlobalFrame, float shipForwardSpeed)
	{
		if (_ownerMissionShip.Physics.NavalSinkingState == NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Floating && !_ownerMissionShip.BeingAbandoned)
		{
			_timeLeftToUpdateNearbyShips -= fixedDt;
			if (_timeLeftToUpdateNearbyShips < 0f)
			{
				_timeLeftToUpdateNearbyShips = MBRandom.RandomFloatRanged(0.15f, 0.2f);
				float distanceLimit = Vec2.Max(Vec2.Abs(_ownerMissionShip.Physics.PhysicsBoundingBoxWithChildren.max.AsVec2), Vec2.Abs(_ownerMissionShip.Physics.PhysicsBoundingBoxWithChildren.min.AsVec2)).Length + _maxOarLength;
				_nearbyShips.Clear();
				_navalShipsLogic?.FillClosestShips(in shipEntityGlobalFrame, distanceLimit, _nearbyShips, _ownerMissionShip);
			}
			int num = _leftSideOars.Count + _rightSideOars.Count;
			float num2 = (float)ComputeUsedOarCount() / (float)num;
			num2 = num2 * 0.9f + 0.1f;
			float maxForceMultiplierFromUser = 1f;
			FixedUpdateSideOars(fixedDt, in shipEntityGlobalFrame, _nearbyShips, _leftSideOars, ref maxForceMultiplierFromUser);
			FixedUpdateSideOars(fixedDt, in shipEntityGlobalFrame, _nearbyShips, _rightSideOars, ref maxForceMultiplierFromUser);
			UpdateRowerParameters(actuatorInput.RowerThrust, actuatorInput.RowerRotation, shipForwardSpeed, out var leftRowersNeededRevolutionRate, out var rightRowersNeededRevolutionRate);
			float num3 = ((leftRowersNeededRevolutionRate >= 0f) ? _rowersPhase : MBMath.WrapAngleSafe(System.MathF.PI * 2f - _rowersPhase));
			float num4 = ((rightRowersNeededRevolutionRate >= 0f) ? _rowersPhase : MBMath.WrapAngleSafe(System.MathF.PI * 2f - _rowersPhase));
			if (leftRowersNeededRevolutionRate == 0f && rightRowersNeededRevolutionRate == 0f)
			{
				num3 = System.MathF.PI;
				num4 = System.MathF.PI;
			}
			else if (leftRowersNeededRevolutionRate == 0f)
			{
				num3 = System.MathF.PI;
			}
			else if (rightRowersNeededRevolutionRate == 0f)
			{
				num4 = System.MathF.PI;
			}
			if (leftRowersNeededRevolutionRate != 0f)
			{
				_leftPhaseData.LastNonZeroRevolutionRate = leftRowersNeededRevolutionRate;
			}
			if (rightRowersNeededRevolutionRate != 0f)
			{
				_rightPhaseData.LastNonZeroRevolutionRate = rightRowersNeededRevolutionRate;
			}
			float num5 = TaleWorlds.Library.MathF.Abs(rightRowersNeededRevolutionRate);
			float num6 = TaleWorlds.Library.MathF.Abs(leftRowersNeededRevolutionRate);
			if (num5 == 1f && num6 == 1f)
			{
				_evenCycle = true;
			}
			bool partialTurn = false;
			if (!_evenCycle)
			{
				if (num5 < 1f && num5 > 0f)
				{
					num4 = System.MathF.PI;
					partialTurn = true;
				}
				else if (num6 < 1f && num6 > 0f)
				{
					num3 = System.MathF.PI;
					partialTurn = true;
				}
			}
			else if (num5 < 1f && num5 > 0f)
			{
				partialTurn = true;
			}
			else if (num6 < 1f && num6 > 0f)
			{
				partialTurn = true;
			}
			float num7 = TaleWorlds.Library.MathF.Clamp(_ownerMissionShip.Physics.LastSubmergedHeightFactorForActuators, 0f, 1.2f);
			bool onPointTurn = leftRowersNeededRevolutionRate * rightRowersNeededRevolutionRate < 0f;
			float a = GetRowSpeedAccordingToPhase(num3, leftRowersNeededRevolutionRate >= 0f, partialTurn, onPointTurn);
			float b = GetRowSpeedAccordingToPhase(num4, rightRowersNeededRevolutionRate >= 0f, partialTurn, onPointTurn);
			if (num5 < 1f && num5 > 0f)
			{
				b = float.MaxValue;
			}
			else if (num6 < 1f && num6 > 0f)
			{
				a = float.MaxValue;
			}
			float num8 = TaleWorlds.Library.MathF.Min(a, b);
			float num9 = System.MathF.PI * 2f * _oarsTipSpeedReferenceMultiplier * _oarsmenSpeedMultiplier;
			num9 *= num8;
			float num10 = ((leftRowersNeededRevolutionRate != 0f || rightRowersNeededRevolutionRate != 0f) ? TaleWorlds.Library.MathF.Lerp(_lastFramePhaseRate, num9, 5f * fixedDt) : 0f);
			(float, float) tuple = ComputeAverageOarTipPointForwardVelocities();
			float item = tuple.Item1;
			float item2 = tuple.Item2;
			float maxTipSpeed = _ownerMissionShip.MissionShipObject.OarsTipSpeed / TaleWorlds.Library.MathF.Max(num7, 0.5f);
			(float, float) tuple2 = _leftOarsPhaseController.ComputeForceAndSlowDownFactor(leftRowersNeededRevolutionRate, item, num3, num10, _oarsmenForceMultiplier * maxForceMultiplierFromUser, _oarFrictionMultiplier * num7, maxTipSpeed);
			float item3 = tuple2.Item1;
			float item4 = tuple2.Item2;
			(float, float) tuple3 = _rightOarsPhaseController.ComputeForceAndSlowDownFactor(rightRowersNeededRevolutionRate, item2, num4, num10, _oarsmenForceMultiplier * maxForceMultiplierFromUser, _oarFrictionMultiplier * num7, maxTipSpeed);
			float item5 = tuple3.Item1;
			float item6 = tuple3.Item2;
			float num11 = TaleWorlds.Library.MathF.Min(item4, item6);
			num10 = (_lastFramePhaseRate = num10 * num11);
			_rowersPhase += num10 * fixedDt;
			if (_rowersPhase >= System.MathF.PI)
			{
				_evenCycle = !_evenCycle;
			}
			_rowersPhase = MBMath.WrapAngleSafe(_rowersPhase);
			float num12 = num10;
			float num13 = num10;
			if (leftRowersNeededRevolutionRate == 0f)
			{
				num12 = 0f;
			}
			else if (rightRowersNeededRevolutionRate == 0f)
			{
				num13 = 0f;
			}
			bool isPartialStop = false;
			bool isPartialStop2 = false;
			if (!_evenCycle)
			{
				if (num5 < 1f && num5 > 0f)
				{
					num13 = 0f;
					num4 = System.MathF.PI;
					isPartialStop2 = true;
				}
				else if (num6 < 1f && num6 > 0f)
				{
					num12 = 0f;
					num3 = System.MathF.PI;
					isPartialStop = true;
				}
			}
			else
			{
				if (rightRowersNeededRevolutionRate < 1f && rightRowersNeededRevolutionRate > 0f && _rowersPhase > System.MathF.PI / 2f)
				{
					num4 = System.MathF.PI;
					isPartialStop2 = true;
				}
				else if (rightRowersNeededRevolutionRate > -1f && rightRowersNeededRevolutionRate < 0f && _rowersPhase > System.MathF.PI / 2f)
				{
					num4 = System.MathF.PI;
					isPartialStop2 = true;
				}
				if (leftRowersNeededRevolutionRate < 1f && leftRowersNeededRevolutionRate > 0f && _rowersPhase > System.MathF.PI / 2f)
				{
					num3 = System.MathF.PI;
					isPartialStop = true;
				}
				else if (leftRowersNeededRevolutionRate > -1f && leftRowersNeededRevolutionRate < 0f && _rowersPhase > System.MathF.PI / 2f)
				{
					num3 = System.MathF.PI;
					isPartialStop = true;
				}
			}
			bool toFullStop = false;
			if (leftRowersNeededRevolutionRate == 0f && rightRowersNeededRevolutionRate == 0f)
			{
				toFullStop = true;
				_rowersPhase = System.MathF.PI;
			}
			BlendPhaseTo(ref _leftPhaseData, num3, num12, num9, fixedDt, toFullStop, isPartialStop);
			BlendPhaseTo(ref _rightPhaseData, num4, num13, num9, fixedDt, toFullStop, isPartialStop2);
			_leftOarsPhaseController.SetPhaseData(_leftPhaseData.CurPhase, num12, _leftPhaseData.CycleArcSizeMult, leftRowersNeededRevolutionRate);
			_rightOarsPhaseController.SetPhaseData(_rightPhaseData.CurPhase, num13, _rightPhaseData.CycleArcSizeMult, rightRowersNeededRevolutionRate);
			Vec3 f = shipEntityGlobalFrame.rotation.f;
			f.z = 0f;
			f.Normalize();
			_rowingSoundEventData[0].NumberOfActiveOars = 0;
			_rowingSoundEventData[1].NumberOfActiveOars = 0;
			for (int i = 0; i < _leftSideOars.Count; i++)
			{
				Vec3 localPosition = _leftSideOars[i].oar.BladeContact;
				Vec3 force = num2 * item3 * _oarAppliedForceMultiplierForStoryMission * num7 * f;
				_leftOarForces[i] = new ShipForce(in localPosition, in force, ShipForce.SourceType.Oar, 1f);
				_rowingSoundEventData[0].NumberOfActiveOars += (_leftSideOars[i].oar.IsExtracted ? 1 : 0);
			}
			for (int j = 0; j < _rightSideOars.Count; j++)
			{
				Vec3 localPosition2 = _rightSideOars[j].oar.BladeContact;
				Vec3 force2 = num2 * item5 * _oarAppliedForceMultiplierForStoryMission * num7 * f;
				_rightOarForces[j] = new ShipForce(in localPosition2, in force2, ShipForce.SourceType.Oar, 1f);
				_rowingSoundEventData[1].NumberOfActiveOars += (_rightSideOars[j].oar.IsExtracted ? 1 : 0);
			}
			CalculateOarSoundPositionsAndParams();
		}
		else
		{
			StopRovers();
		}
	}

	private void StopRovers()
	{
		_leftOarsPhaseController.Stop();
		for (int i = 0; i < _leftSideOars.Count; i++)
		{
			_leftOarForces[i] = ShipForce.None();
		}
		_rightOarsPhaseController.Stop();
		for (int j = 0; j < _rightSideOars.Count; j++)
		{
			_rightOarForces[j] = ShipForce.None();
		}
		for (int k = 0; k < 2; k++)
		{
			_rowingSoundEventData[k].OarsSoundEvents?.Stop();
			_rowingSoundEventData[k].OarsSoundEvents = null;
		}
	}

	private void FixedUpdateRudder(float fixedDt, in ShipActuatorRecord actuatorInput, in MatrixFrame shipEntityGlobalFrame, float shipForwardSpeed)
	{
		Vec3 u = shipEntityGlobalFrame.rotation.u;
		Vec3 globalPoint = shipEntityGlobalFrame.TransformToParent(in _rudderStockLocalPosition);
		Vec3 v = _ownerMissionShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(globalPoint);
		Vec3 rudderStockLocalVelocity = shipEntityGlobalFrame.rotation.TransformToLocal(in v);
		float lengthSquared = rudderStockLocalVelocity.LengthSquared;
		if (lengthSquared < 16f)
		{
			if (lengthSquared <= 1f)
			{
				rudderStockLocalVelocity = Vec3.Zero;
			}
			else
			{
				float length = rudderStockLocalVelocity.Length;
				float alpha = 1f - (length - 1f) / 3f;
				rudderStockLocalVelocity = Vec3.Lerp(rudderStockLocalVelocity, new Vec3(0f, (float)TaleWorlds.Library.MathF.Sign(rudderStockLocalVelocity.y) * length), alpha);
			}
		}
		Vec3 vec = rudderStockLocalVelocity;
		vec.z = 0f;
		vec = ((vec.LengthSquared > 0.0001f) ? vec : new Vec3(0f, -1f));
		vec.Normalize();
		Vec3 unClampedRudderStabilityDirectionLocal = vec;
		if (unClampedRudderStabilityDirectionLocal.y >= 0f)
		{
			unClampedRudderStabilityDirectionLocal = -unClampedRudderStabilityDirectionLocal;
		}
		float rudderRotationMax = _ownerMissionShip.MissionShipObject.RudderRotationMax;
		float value = 0f - unClampedRudderStabilityDirectionLocal.AsVec2.AngleBetween(new Vec2(0f, -1f));
		float num = 0.8f;
		value = TaleWorlds.Library.MathF.Clamp(value, (0f - rudderRotationMax) * num, rudderRotationMax * num);
		float num2 = fixedDt * _ownerMissionShip.MissionShipObject.RudderRotationRate * 2f;
		_lastTargetRudderStabilityLocalRotation = value;
		if (_lastTargetRudderStabilityLocalRotation > value)
		{
			_lastTargetRudderStabilityLocalRotation -= num2;
			if (_lastTargetRudderStabilityLocalRotation < value)
			{
				_lastTargetRudderStabilityLocalRotation = value;
			}
		}
		else if (_lastTargetRudderStabilityLocalRotation < value)
		{
			_lastTargetRudderStabilityLocalRotation += num2;
			if (_lastTargetRudderStabilityLocalRotation > value)
			{
				_lastTargetRudderStabilityLocalRotation = value;
			}
		}
		value = _lastTargetRudderStabilityLocalRotation;
		float rudderRotation = actuatorInput.RudderRotation;
		rudderRotation = (float)TaleWorlds.Library.MathF.Sign(rudderRotation) * TaleWorlds.Library.MathF.Pow(rudderRotation, 2f);
		int num3 = -TaleWorlds.Library.MathF.Sign(rudderRotation);
		float num4 = rudderRotation * (float)TaleWorlds.Library.MathF.Sign((shipForwardSpeed > -1f) ? 1f : shipForwardSpeed) * _ownerMissionShip.MissionShipObject.RudderRotationMax;
		VisualRudderPullDirection = TaleWorlds.Library.MathF.Sign(num4);
		float num5 = fixedDt * _ownerMissionShip.MissionShipObject.RudderRotationRate * ((_lastAddedFromInputRudderLocalRotation * num4 <= 0f) ? 1f : 1f);
		if (_lastAddedFromInputRudderLocalRotation > num4)
		{
			_lastAddedFromInputRudderLocalRotation -= num5;
			if (_lastAddedFromInputRudderLocalRotation < num4)
			{
				_lastAddedFromInputRudderLocalRotation = num4;
			}
		}
		else if (_lastAddedFromInputRudderLocalRotation < num4)
		{
			_lastAddedFromInputRudderLocalRotation += num5;
			if (_lastAddedFromInputRudderLocalRotation > num4)
			{
				_lastAddedFromInputRudderLocalRotation = num4;
			}
		}
		_lastAddedFromInputRudderLocalRotation = TaleWorlds.Library.MathF.Clamp(value + _lastAddedFromInputRudderLocalRotation, 0f - rudderRotationMax, rudderRotationMax) - value;
		float num6 = TaleWorlds.Library.MathF.Clamp(_ownerMissionShip.Physics.LastSubmergedHeightFactorForActuators, 0f, 1f);
		float rudderSurfaceArea = _ownerMissionShip.MissionShipObject.RudderBladeLength * _ownerMissionShip.MissionShipObject.RudderBladeHeight;
		float rudderDeflectionCoef = _ownerMissionShip.MissionShipObject.RudderDeflectionCoef;
		float rudderForceMax = _ownerMissionShip.MissionShipObject.RudderForceMax;
		Vec3 force = Vec3.Zero;
		float num7 = _lastAddedFromInputRudderLocalRotation;
		int value2 = ((_lastAddedFromInputRudderLocalRotation == 0f) ? 1 : (TaleWorlds.Library.MathF.Ceiling(TaleWorlds.Library.MathF.Abs(_lastAddedFromInputRudderLocalRotation) / 0.0017453292f) + 1));
		value2 = MBMath.ClampInt(value2, 1, 250);
		for (int i = 0; i <= value2; i++)
		{
			float num8 = (float)i / (float)value2 * _lastAddedFromInputRudderLocalRotation;
			float value3 = value + num8;
			value3 = TaleWorlds.Library.MathF.Clamp(value3, 0f - rudderRotationMax, rudderRotationMax);
			var (vec2, vec3) = ComputeRudderDeflectionForce(value3, in unClampedRudderStabilityDirectionLocal, in rudderStockLocalVelocity, in vec, rudderSurfaceArea);
			if (TaleWorlds.Library.MathF.Sign(vec3.x) == num3)
			{
				Vec3 v2 = vec2 + vec3;
				v2 *= TaleWorlds.Library.MathF.Abs(u.z);
				v2 *= rudderDeflectionCoef;
				v2 *= num6;
				force = shipEntityGlobalFrame.rotation.TransformToParent(in v2);
				num7 = value3 - value;
				if (TaleWorlds.Library.MathF.Abs(v2.x) >= rudderForceMax)
				{
					force *= rudderForceMax / TaleWorlds.Library.MathF.Abs(v2.x);
					break;
				}
			}
		}
		_lastAddedFromInputRudderLocalRotation = num7;
		float num9 = fixedDt * _ownerMissionShip.MissionShipObject.RudderRotationRate * 0.5f;
		if (_lastAddedFromInputRudderLocalRotation > num7)
		{
			_lastAddedFromInputRudderLocalRotation -= num9;
			if (_lastAddedFromInputRudderLocalRotation < num7)
			{
				_lastAddedFromInputRudderLocalRotation = num7;
			}
		}
		else if (_lastAddedFromInputRudderLocalRotation < num7)
		{
			_lastAddedFromInputRudderLocalRotation += num9;
			if (_lastAddedFromInputRudderLocalRotation > num7)
			{
				_lastAddedFromInputRudderLocalRotation = num7;
			}
		}
		num7 = _lastAddedFromInputRudderLocalRotation;
		_lastRudderLocalRotation = _rudderLocalRotation;
		float valueTo = TaleWorlds.Library.MathF.Clamp(value + num7, 0f - rudderRotationMax, rudderRotationMax);
		_rudderLocalRotation = TaleWorlds.Library.MathF.Lerp(_rudderLocalRotation, valueTo, fixedDt * 5f);
		force *= 1f + _ownerMissionShip.ShipOrigin.RudderSurfaceAreaFactor;
		Vec3 vec4 = new Vec3(0f, -1f);
		vec4.RotateAboutZ(_rudderLocalRotation);
		_rudderShipForce = new ShipForce(_rudderStockLocalPosition + vec4 * (_ownerMissionShip.MissionShipObject.RudderBladeLength * 0.5f), in force, ShipForce.SourceType.Rudder, rudderDeflectionCoef);
		_shipPresenceSoundParam = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Abs(_rudderShipForce.Force.Length / 10000f), 1f);
		_rudderStressSoundParam = _rudderShipForce.Force.LengthSquared / (rudderForceMax * rudderForceMax);
	}

	private void OnParallelTickRudder(float dt)
	{
		_cachedOwnerScene.GetInterpolationFactorForBodyWorldTransformSmoothing(out var interpolationFactor, out var _);
		VisualRudderLocalRotation = TaleWorlds.Library.MathF.Lerp(_lastRudderLocalRotation, _rudderLocalRotation, interpolationFactor);
		Vec3 vec = _ownerMissionShip.GameEntity.GetGlobalFrame().TransformToParent(in _rudderStockLocalPosition);
		float num = TaleWorlds.Library.MathF.Clamp(_ownerMissionShip.Physics.LastSubmergedHeightFactorForActuators, 0f, 1f);
		float wakeVisibility = 0.15f * dt * num;
		float num2 = _shipPresenceSoundParam * 0.25f + 0.1f;
		_cachedOwnerScene.AddWaterWakeWithCapsule(vec, num2 * 1.5f, vec, num2, wakeVisibility, 0f);
	}

	private int ComputeExtractedOarCount()
	{
		int num = 0;
		for (int i = 0; i < _leftSideOars.Count; i++)
		{
			if (_leftSideOars[i].oar.IsExtracted)
			{
				num++;
			}
		}
		for (int j = 0; j < _rightSideOars.Count; j++)
		{
			if (_rightSideOars[j].oar.IsExtracted)
			{
				num++;
			}
		}
		return num;
	}

	private int ComputeUsedOarCount()
	{
		int num = 0;
		for (int i = 0; i < _leftSideOars.Count; i++)
		{
			if (_leftSideOars[i].oar.IsUsed)
			{
				num++;
			}
		}
		for (int j = 0; j < _rightSideOars.Count; j++)
		{
			if (_rightSideOars[j].oar.IsUsed)
			{
				num++;
			}
		}
		return num;
	}

	private (float, float) ComputeAverageOarTipPointForwardVelocities()
	{
		MatrixFrame bodyWorldTransform = _ownerMissionShip.GameEntity.GetBodyWorldTransform();
		Vec3 vec = bodyWorldTransform.TransformToParent(_ownerMissionShip.GameEntity.CenterOfMass);
		Vec3 linearVelocity = _ownerMissionShip.Physics.LinearVelocity;
		Vec3 angularVelocity = _ownerMissionShip.Physics.AngularVelocity;
		Vec3 vb = bodyWorldTransform.TransformToParent(in _leftSideAverageOarLocalPos) - vec;
		Vec3 vec2 = Vec3.CrossProduct(angularVelocity, vb);
		float item = Vec3.DotProduct(linearVelocity + vec2, bodyWorldTransform.rotation.f);
		Vec3 vb2 = bodyWorldTransform.TransformToParent(in _rightSideAverageOarLocalPos) - vec;
		Vec3 vec3 = Vec3.CrossProduct(angularVelocity, vb2);
		float item2 = Vec3.DotProduct(linearVelocity + vec3, bodyWorldTransform.rotation.f);
		return (item, item2);
	}

	private void FixedUpdateSideOars(float fixedDt, in MatrixFrame shipGlobalFrame, MBList<(MissionShip ship, OarSidePhaseController.OarSide shipSide)> nearbyShips, MBList<(GameEntity entity, MissionOar oar)> shipOars, ref float maxForceMultiplierFromUser)
	{
		for (int i = 0; i < shipOars.Count; i++)
		{
			MissionOar item = shipOars[i].oar;
			item.FixedUpdate(fixedDt, in shipGlobalFrame, nearbyShips);
			maxForceMultiplierFromUser = TaleWorlds.Library.MathF.Max(maxForceMultiplierFromUser, item.ForceMultiplierFromUserAgent);
		}
	}

	private void UpdateRowerParameters(float rowersThrustRate, float rowersRotationRate, float shipForwardSpeed, out float leftRowersNeededRevolutionRate, out float rightRowersNeededRevolutionRate)
	{
		if (rowersThrustRate == 0f && rowersRotationRate != 0f)
		{
			if (TaleWorlds.Library.MathF.Abs(shipForwardSpeed) <= 6f)
			{
				leftRowersNeededRevolutionRate = 0f - rowersRotationRate;
				rightRowersNeededRevolutionRate = rowersRotationRate;
			}
			else if (rowersRotationRate > 0f)
			{
				rightRowersNeededRevolutionRate = rowersRotationRate;
				leftRowersNeededRevolutionRate = 0f;
			}
			else
			{
				leftRowersNeededRevolutionRate = 0f - rowersRotationRate;
				rightRowersNeededRevolutionRate = 0f;
			}
			return;
		}
		leftRowersNeededRevolutionRate = rowersThrustRate;
		rightRowersNeededRevolutionRate = rowersThrustRate;
		if (rowersRotationRate != 0f)
		{
			float num = 0.5f;
			if (shipForwardSpeed * rowersThrustRate < 0f && TaleWorlds.Library.MathF.Abs(shipForwardSpeed) > 6f)
			{
				num = 0f;
			}
			if (rowersThrustRate * rowersRotationRate > 0f)
			{
				leftRowersNeededRevolutionRate = num;
			}
			else
			{
				rightRowersNeededRevolutionRate = num;
			}
		}
	}

	private IShipOarScriptComponent GetOarScriptFromEntity(WeakGameEntity oarEntity)
	{
		IShipOarScriptComponent shipOarScriptComponent = null;
		WeakGameEntity weakGameEntity = oarEntity;
		while (weakGameEntity.IsValid && shipOarScriptComponent == null)
		{
			shipOarScriptComponent = weakGameEntity.GetFirstScriptOfType<ShipOarMachine>();
			if (shipOarScriptComponent == null)
			{
				shipOarScriptComponent = weakGameEntity.GetFirstScriptOfType<ShipUnmannedOar>();
			}
			weakGameEntity = weakGameEntity.Parent;
		}
		return shipOarScriptComponent;
	}

	internal static float ComputeActuatorParameter(float value, float target, float dt, float incrementRate)
	{
		float num = target - value;
		float num2 = Math.Min(Math.Abs(num), dt * incrementRate);
		return value + (float)TaleWorlds.Library.MathF.Sign(num) * num2;
	}

	private static (Vec3, Vec3) ComputeRudderDeflectionForce(float totalTargetRot, in Vec3 unClampedRudderStabilityDirectionLocal, in Vec3 rudderStockLocalVelocity, in Vec3 rudderStockLocalVelocityDirection, float rudderSurfaceArea)
	{
		Vec3 vec = new Vec3(0f, -1f);
		vec.RotateAboutZ(totalTargetRot);
		float num = vec.AsVec2.AngleBetween(unClampedRudderStabilityDirectionLocal.AsVec2);
		if (num < -System.MathF.PI / 2f)
		{
			num += System.MathF.PI;
		}
		else if (num > System.MathF.PI / 2f)
		{
			num -= System.MathF.PI;
		}
		float num2 = 0.5f * NavalDLC.Missions.NavalPhysics.NavalPhysics.GetWaterDensity() * rudderStockLocalVelocity.LengthSquared;
		float num3 = TaleWorlds.Library.MathF.Abs(num);
		float num4 = TaleWorlds.Library.MathF.Sign((num == 0f) ? 1f : num);
		float valueFrom = 0.72f * (System.MathF.PI * 2f * num);
		float valueTo = 1.1f * TaleWorlds.Library.MathF.Sin(2f * num3) * num4;
		float num5 = TaleWorlds.Library.MathF.Sin(num3);
		float amount = MBMath.SmoothStep(System.MathF.PI / 15f, 0.61086524f, num3);
		float num6 = MBMath.Lerp(valueFrom, valueTo, amount);
		float num7 = (0.06f + 0.1f * num6 * num6 + 1.1f * num5) * num5;
		float num8 = num6 * num2 * rudderSurfaceArea;
		float num9 = num7 * num2 * rudderSurfaceArea;
		Vec3 vec2 = -rudderStockLocalVelocityDirection;
		Vec3 vec3 = vec2;
		vec3.RotateAboutZ(System.MathF.PI / 2f);
		Vec3 item = num9 * vec2;
		Vec3 item2 = num8 * vec3;
		return (item, item2);
	}

	public void SetOarAppliedForceMultiplierForStoryMission(float newOarAppliedForceMultiplierForStoryMission)
	{
		_oarAppliedForceMultiplierForStoryMission = newOarAppliedForceMultiplierForStoryMission;
	}
}
