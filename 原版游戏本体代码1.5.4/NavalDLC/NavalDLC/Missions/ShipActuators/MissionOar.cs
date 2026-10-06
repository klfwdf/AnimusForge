using System;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.ShipActuators;

public class MissionOar
{
	private class OarFoamDecal
	{
		private Decal _splashFoamDecal;

		private MatrixFrame _currentFrame;

		private Vec3 _sideVectorStart;

		private Vec3 _sideVectorEnd;

		private float _cumulativeDtTillStart;

		private float _randomScale;

		private Vec3 _currentSpeed;

		private float _lifeTimeRandomness;

		internal OarFoamDecal()
		{
			_splashFoamDecal = null;
			_currentFrame = MatrixFrame.Identity;
			_cumulativeDtTillStart = 0f;
			_randomScale = 1f;
			_currentSpeed = Vec3.Zero;
			_lifeTimeRandomness = 0f;
			_sideVectorStart = Vec3.Zero;
			_sideVectorEnd = Vec3.Zero;
		}

		internal void Tick(float dt, MissionShip ownerShip)
		{
			float num = 5.8f + _lifeTimeRandomness;
			if (_splashFoamDecal != null && _cumulativeDtTillStart < num)
			{
				Vec3 vec = new Vec3(0.65f, 1f, 1f);
				Vec3 v = vec * 4.5f;
				_cumulativeDtTillStart += dt;
				float x;
				if (_cumulativeDtTillStart > 1.05f)
				{
					float num2 = _cumulativeDtTillStart - 1.05f;
					x = TaleWorlds.Library.MathF.Clamp(1f - num2 / (num - 1.05f), 0f, 1f);
				}
				else
				{
					x = TaleWorlds.Library.MathF.Clamp(_cumulativeDtTillStart / 1.05f, 0f, 1f);
				}
				float num3 = TaleWorlds.Library.MathF.Pow(x, 4f);
				_splashFoamDecal.SetAlpha(num3 * 0.17499998f + 0.475f);
				_currentFrame.origin.z = ownerShip.Scene.GetWaterLevelAtPosition(_currentFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) - 0.15f;
				_currentFrame.origin += _currentSpeed * dt;
				Vec3 currentSpeed = _currentSpeed;
				float num4 = currentSpeed.Normalize();
				num4 = TaleWorlds.Library.MathF.Max(num4 - dt * 0.5f, 0f);
				_currentSpeed = num4 * currentSpeed;
				float alpha = TaleWorlds.Library.MathF.Clamp(_cumulativeDtTillStart / num, 0f, 1f);
				Vec3 scaleAmountXYZ = Vec3.Lerp(vec, v, alpha) * _randomScale;
				float percent = TaleWorlds.Library.MathF.Clamp(_cumulativeDtTillStart / num, 0f, 1f);
				Vec3 s = Vec3.Slerp(_sideVectorStart, _sideVectorEnd, percent);
				s.Normalize();
				_currentFrame.rotation.s = s;
				_currentFrame.rotation.u = Vec3.Up;
				_currentFrame.rotation.f = -_currentFrame.rotation.s.CrossProductWithUp();
				_currentFrame.rotation.ApplyScaleLocal(in scaleAmountXYZ);
				_splashFoamDecal.Frame = _currentFrame;
			}
		}

		internal void Fill(in Vec3 spawnPosition, MissionShip ownerShip)
		{
			if (_splashFoamDecal == null)
			{
				Decal decal = Decal.CreateDecal();
				decal.SetMaterial(Material.GetFromResource("decal_water_foam"));
				ownerShip.Scene.AddDecalInstance(decal, "editor_set", deletable: true);
				_splashFoamDecal = decal;
			}
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = spawnPosition;
			identity.rotation.u = Vec3.Up;
			Vec3 linearVelocityAtGlobalPointForEntityWithDynamicBody = ownerShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(spawnPosition);
			Vec2 asVec = ownerShip.GameEntity.GetGlobalFrame().rotation.s.AsVec2;
			asVec.Normalize();
			identity.rotation.s = new Vec3(asVec);
			identity.rotation.f = -identity.rotation.s.CrossProductWithUp();
			identity.rotation.f.Normalize();
			identity.origin += (-0.5f + MBRandom.RandomFloat) * identity.rotation.f;
			identity.origin += (-0.5f + MBRandom.RandomFloat) * identity.rotation.s;
			_cumulativeDtTillStart = 0f;
			TaleWorlds.Library.MathF.Clamp((linearVelocityAtGlobalPointForEntityWithDynamicBody.Length - 4f) / 8f, 0f, 1f);
			float num = 1f;
			_randomScale = (0.7f + MBRandom.RandomFloat * 0.6f) * num;
			_splashFoamDecal.Frame = identity;
			_splashFoamDecal.SetAlpha(0f);
			_currentFrame = identity;
			int num2 = MBRandom.RandomInt(3);
			float vectorArgument = (float)(num2 % 2) * 0.5f;
			float vectorArgument2 = (float)(num2 / 2) * 0.5f;
			_splashFoamDecal.SetVectorArgument(vectorArgument, vectorArgument2, -0.5f, -0.5f);
			float num3 = 0.1f * (-0.5f + MBRandom.RandomFloat) * 0.25f;
			float num4 = 0.2f * (0.9f + MBRandom.RandomFloat * 0.2f);
			_currentSpeed = linearVelocityAtGlobalPointForEntityWithDynamicBody * num4 + identity.rotation.s * linearVelocityAtGlobalPointForEntityWithDynamicBody.Length * num3;
			_lifeTimeRandomness = (-0.5f + MBRandom.RandomFloat) * 2f;
			float a = System.MathF.PI * (2f * MBRandom.RandomFloat - 1f);
			float a2 = -0.34906584f * (0.8f + MBRandom.RandomFloat * 0.4f);
			_sideVectorStart = new Vec3(asVec);
			_sideVectorStart.RotateAboutZ(a);
			_sideVectorEnd = _sideVectorStart;
			_sideVectorEnd.RotateAboutZ(a2);
			Vec2 data = new Vec2(2.5f, 2.5f);
			_splashFoamDecal.OverrideRoadBoundaryP0(data);
			Vec2 data2 = new Vec2(MBRandom.RandomFloat, MBRandom.RandomFloat);
			_splashFoamDecal.OverrideRoadBoundaryP1(data2);
		}
	}

	private struct OarRollAnimKeyFrame
	{
		public float KeyProgress;

		public float RollAngleInRad;

		public OarRollAnimKeyFrame(float keyProgress, float rollAngleInRad)
		{
			KeyProgress = keyProgress;
			RollAngleInRad = rollAngleInRad;
		}
	}

	private static class OarRollAnimManager
	{
		private static readonly OarRollAnimKeyFrame[] rollAnim = new OarRollAnimKeyFrame[7]
		{
			new OarRollAnimKeyFrame(0f, -1.2217305f),
			new OarRollAnimKeyFrame(0.15f, System.MathF.PI / 10f),
			new OarRollAnimKeyFrame(0.25f, 0.34906584f),
			new OarRollAnimKeyFrame(0.5f, 0.34906584f),
			new OarRollAnimKeyFrame(0.7f, -0.6981317f),
			new OarRollAnimKeyFrame(0.73f, -1.2217305f),
			new OarRollAnimKeyFrame(1f, -1.2217305f)
		};

		private static readonly OarRollAnimKeyFrame[] rollAnim2 = new OarRollAnimKeyFrame[5]
		{
			new OarRollAnimKeyFrame(0f, System.MathF.PI * -13f / 36f),
			new OarRollAnimKeyFrame(0.25f, 0.34906584f),
			new OarRollAnimKeyFrame(0.5f, 0.34906584f),
			new OarRollAnimKeyFrame(0.7f, 0.34906584f),
			new OarRollAnimKeyFrame(1f, System.MathF.PI * -13f / 36f)
		};

		private static readonly OarRollAnimKeyFrame[] rollAnim3 = new OarRollAnimKeyFrame[6]
		{
			new OarRollAnimKeyFrame(0f, -1.2217305f),
			new OarRollAnimKeyFrame(0.25f, 0f),
			new OarRollAnimKeyFrame(0.5f, 0f),
			new OarRollAnimKeyFrame(0.75f, -0.6981317f),
			new OarRollAnimKeyFrame(0.88f, -System.MathF.PI / 3f),
			new OarRollAnimKeyFrame(1f, -1.2217305f)
		};

		private static readonly OarRollAnimKeyFrame[] rollAnim4 = new OarRollAnimKeyFrame[4]
		{
			new OarRollAnimKeyFrame(0f, System.MathF.PI * -13f / 36f),
			new OarRollAnimKeyFrame(0.27f, System.MathF.PI / 6f),
			new OarRollAnimKeyFrame(0.7f, System.MathF.PI / 6f),
			new OarRollAnimKeyFrame(1f, System.MathF.PI * -13f / 36f)
		};

		private static readonly OarRollAnimKeyFrame[] rollAnim5 = new OarRollAnimKeyFrame[6]
		{
			new OarRollAnimKeyFrame(0f, -0.34906584f),
			new OarRollAnimKeyFrame(0.25f, -0.6981317f),
			new OarRollAnimKeyFrame(0.27f, 0.17453292f),
			new OarRollAnimKeyFrame(0.7f, 0.34906584f),
			new OarRollAnimKeyFrame(0.77f, -0.43633232f),
			new OarRollAnimKeyFrame(1f, -0.34906584f)
		};

		private static readonly OarRollAnimKeyFrame[] rollAnim6 = new OarRollAnimKeyFrame[5]
		{
			new OarRollAnimKeyFrame(0f, System.MathF.PI * -13f / 36f),
			new OarRollAnimKeyFrame(0.15f, 0.34906584f),
			new OarRollAnimKeyFrame(0.5f, 0.34906584f),
			new OarRollAnimKeyFrame(0.55f, 0.34906584f),
			new OarRollAnimKeyFrame(1f, System.MathF.PI * -13f / 36f)
		};

		private static readonly OarRollAnimKeyFrame[] rollAnim7 = new OarRollAnimKeyFrame[3]
		{
			new OarRollAnimKeyFrame(0f, -1.4835298f),
			new OarRollAnimKeyFrame(0.5f, 0.61086524f),
			new OarRollAnimKeyFrame(1f, -1.4835298f)
		};

		public static readonly OarRollAnimKeyFrame[][] RollAnimations = new OarRollAnimKeyFrame[7][] { rollAnim, rollAnim2, rollAnim3, rollAnim4, rollAnim5, rollAnim6, rollAnim7 };
	}

	private const int NumberOfFoamDecals = 4;

	private float _phaseDelayForSlowDown;

	private float _phaseDelayOffset;

	private float _phaseDelayOffsetTimeScale;

	private float _visualVerticalBaseAngleOffset;

	private float _visualVerticalAngleMultiplier;

	private float _visualLateralAngleMultiplier;

	private float _visualOarConstantRollAngle;

	private float _visualOarRollAnimationAngleFactor;

	private int _visualOarRollAnimationIndex;

	private float _slowDownPhaseMultiplier;

	private float _slowDownPhaseDuration;

	private readonly OarFoamDecal[] _splashFoamDecals = new OarFoamDecal[4];

	private int _nextDecalIndexToUse;

	private Vec3 _bladeContact = Vec3.Invalid;

	private readonly Vec3 _oarGateOffset;

	private OarSidePhaseController _sidePhaseData;

	private float _timeLeftToCheckForCloseShipsForRetraction;

	private Vec3 _lastGlobalBladeContact;

	private ParticleSystem _oarWaterParticleSmall;

	private bool _wakeActive;

	private bool _decalSpawned;

	private MBFastRandom _oarEffectsRandom;

	private Scene _ownerSceneCached;

	public MissionShip OwnerShip { get; }

	public float VisualPhase { get; private set; }

	public Vec3 GateOffset => _oarGateOffset;

	public float Extraction { get; private set; }

	public bool IsRetracted => Extraction <= 0f;

	public bool IsExtracted => Extraction >= 1f;

	public bool IsUsed { get; private set; }

	public bool IsRetracting { get; private set; }

	public Vec3 BladeContact
	{
		get
		{
			if (!_bladeContact.IsValid)
			{
				_bladeContact = ComputeBladeContactPosition();
			}
			return _bladeContact;
		}
	}

	public OarDeckParameters DeckParameters { get; private set; }

	public float ForceMultiplierFromUserAgent { get; private set; }

	public float NeededRevolutionRate => _sidePhaseData.NeededRevolutionRate;

	private MissionOar(MissionShip ownerShip, GameEntity entity, OarDeckParameters deckParameters, OarSidePhaseController phaseData)
	{
		OwnerShip = ownerShip;
		DeckParameters = deckParameters;
		_ownerSceneCached = OwnerShip.Scene;
		MatrixFrame m = entity.GetGlobalFrame();
		_oarGateOffset = OwnerShip.GameEntity.GetGlobalFrame().TransformToLocal(in m).origin;
		_sidePhaseData = phaseData;
		VisualPhase = _sidePhaseData.VisualPhase;
		ReRandomizeVisualParameters(-1);
		_phaseDelayForSlowDown = 0f;
		Extraction = 1f;
		IsRetracting = false;
		IsUsed = true;
		_slowDownPhaseMultiplier = 1f;
		_slowDownPhaseDuration = 0f;
		_timeLeftToCheckForCloseShipsForRetraction = 0f;
		ForceMultiplierFromUserAgent = 1f;
		if (!_ownerSceneCached.IsEditorScene())
		{
			MatrixFrame boneLocalFrame = MatrixFrame.Identity;
			_oarWaterParticleSmall = ParticleSystem.CreateParticleSystemAttachedToEntity("psys_naval_oar_on_move_small", ownerShip.GameEntity, ref boneLocalFrame);
			_oarWaterParticleSmall.SetEnable(enable: false);
		}
		for (int i = 0; i < 4; i++)
		{
			_splashFoamDecals[i] = new OarFoamDecal();
		}
	}

	private void ReRandomizeVisualParameters(int userAgentIndex)
	{
		uint seed = ((userAgentIndex < 0) ? ((uint)((_oarGateOffset.x + _oarGateOffset.y + _oarGateOffset.z) * 1000f)) : ((uint)userAgentIndex));
		_oarEffectsRandom = new MBFastRandom(seed);
		_phaseDelayOffset = _oarEffectsRandom.NextFloatRanged(-10f, 10f) * (System.MathF.PI / 180f);
		_phaseDelayOffsetTimeScale = _oarEffectsRandom.NextFloatRanged(0.5f, 1.2f);
		_visualVerticalBaseAngleOffset = _oarEffectsRandom.NextFloatRanged(-System.MathF.PI / 120f, System.MathF.PI / 120f);
		_visualVerticalAngleMultiplier = _oarEffectsRandom.NextFloatRanged(1f, 1.1f);
		_visualLateralAngleMultiplier = _oarEffectsRandom.NextFloatRanged(0.95f, 1.01f);
		_visualOarConstantRollAngle = _oarEffectsRandom.NextFloatRanged(-System.MathF.PI / 60f, System.MathF.PI / 60f);
		_visualOarRollAnimationAngleFactor = _oarEffectsRandom.NextFloatRanged(0.7f, 1f);
		_visualOarRollAnimationIndex = _oarEffectsRandom.Next(OarRollAnimManager.RollAnimations.Length);
	}

	public void SetUsed(bool newIsUsed, int userAgentIndex)
	{
		if (IsUsed != newIsUsed)
		{
			SetRetractOars(IsUsed);
			IsUsed = newIsUsed;
			if (IsUsed)
			{
				ReRandomizeVisualParameters(userAgentIndex);
			}
		}
	}

	public void SetRetractOars(bool value)
	{
		IsRetracting = value;
	}

	public void SetSlowDownPhaseForDuration(float slowDownMultiplier, float slowDownDuration)
	{
		_slowDownPhaseMultiplier = slowDownMultiplier;
		_slowDownPhaseDuration = slowDownDuration;
	}

	public void OnParallelTick(float dt)
	{
		float extraction = Extraction;
		if (IsRetracting)
		{
			extraction -= dt * DeckParameters.RetractionRate;
			extraction = TaleWorlds.Library.MathF.Max(0f, extraction);
		}
		else
		{
			extraction += dt * DeckParameters.RetractionRate;
			extraction = TaleWorlds.Library.MathF.Min(extraction, 1f);
		}
		Extraction = extraction;
		float num = 0f;
		if (!IsRetracted)
		{
			float currentTime = Mission.Current.CurrentTime;
			num = _phaseDelayOffset * TaleWorlds.Library.MathF.Sin(currentTime * _phaseDelayOffsetTimeScale) * extraction;
		}
		float num2 = MBMath.WrapAngleSafe(_sidePhaseData.VisualPhase + num);
		if (_slowDownPhaseDuration > 0f || _phaseDelayForSlowDown != 0f)
		{
			_slowDownPhaseDuration -= dt;
			if (_slowDownPhaseDuration > 0f)
			{
				_phaseDelayForSlowDown -= _sidePhaseData.PhaseRate * dt * (1f - _slowDownPhaseMultiplier);
				_phaseDelayForSlowDown = MBMath.WrapAngleSafe(_phaseDelayForSlowDown);
			}
			else
			{
				_slowDownPhaseDuration = 0f;
				float phaseDelayForSlowDown = _phaseDelayForSlowDown;
				_phaseDelayForSlowDown += _sidePhaseData.PhaseRate * dt * (1f - _slowDownPhaseMultiplier);
				_phaseDelayForSlowDown = MBMath.WrapAngleSafe(_phaseDelayForSlowDown);
				if (phaseDelayForSlowDown * _phaseDelayForSlowDown <= 0f && TaleWorlds.Library.MathF.Abs(phaseDelayForSlowDown) < System.MathF.PI / 2f && TaleWorlds.Library.MathF.Abs(_phaseDelayForSlowDown) < System.MathF.PI / 2f)
				{
					_phaseDelayForSlowDown = 0f;
				}
			}
		}
		num2 += _phaseDelayForSlowDown;
		VisualPhase = MBMath.WrapAngleSafe(num2);
		TickFoamDecals(dt);
		Vec3 spawnPosition = OwnerShip.GlobalFrame.TransformToParent(in _bladeContact);
		if (IsExtracted && MBMath.IsBetweenInclusive(VisualPhase, -0.87266463f, 0.87266463f))
		{
			if (_wakeActive)
			{
				float lastSubmergedHeightFactorForActuators = _sidePhaseData.GetLastSubmergedHeightFactorForActuators();
				if (_sidePhaseData.CycleArcSizeMult > 0.5f && lastSubmergedHeightFactorForActuators > 0.01f)
				{
					float num3 = (1f - _sidePhaseData.LastSlowDownFactor * _sidePhaseData.LastSlowDownFactor * _sidePhaseData.LastSlowDownFactor + 0.4f) * 0.25f * dt * lastSubmergedHeightFactorForActuators;
					_ownerSceneCached.AddWaterWakeWithCapsule(_lastGlobalBladeContact, 0.90000004f, spawnPosition, num3, num3, 0f);
				}
				if (lastSubmergedHeightFactorForActuators > 0.01f && _sidePhaseData.CycleArcSizeMult > 0.5f && (MBMath.IsBetweenInclusive(VisualPhase, -0.87266463f, -System.MathF.PI / 6f) || MBMath.IsBetweenInclusive(VisualPhase, 0.17453295f, 0.87266463f)))
				{
					MatrixFrame globalFrame = OwnerShip.GameEntity.GetGlobalFrame();
					MatrixFrame frame = MatrixFrame.Identity;
					frame.rotation.s = globalFrame.rotation.s;
					if (GateOffset.x < 0f)
					{
						frame.rotation.s *= -1f;
					}
					frame.rotation.s.z = 0f;
					frame.rotation.s.Normalize();
					frame.rotation.f = -frame.rotation.s.CrossProductWithUp();
					frame.origin = spawnPosition;
					frame.origin.z = _ownerSceneCached.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
					_oarWaterParticleSmall.SetLocalFrame(globalFrame.TransformToLocalNonOrthogonal(in frame));
					_oarWaterParticleSmall.SetEnable(enable: true);
					if (!_decalSpawned)
					{
						if (_oarEffectsRandom.NextFloat() > 0.4f)
						{
							SpawnNewDecal(in spawnPosition);
						}
						_decalSpawned = true;
					}
				}
				else
				{
					_oarWaterParticleSmall.SetEnable(enable: false);
				}
			}
			_wakeActive = true;
		}
		else
		{
			_oarWaterParticleSmall.SetEnable(enable: false);
			_wakeActive = false;
			_decalSpawned = false;
		}
		_lastGlobalBladeContact = spawnPosition;
	}

	private void SpawnNewDecal(in Vec3 spawnPosition)
	{
		_splashFoamDecals[_nextDecalIndexToUse].Fill(in spawnPosition, OwnerShip);
		_nextDecalIndexToUse = (_nextDecalIndexToUse + 1) % 4;
	}

	private void TickFoamDecals(float dt)
	{
		OarFoamDecal[] splashFoamDecals = _splashFoamDecals;
		for (int i = 0; i < splashFoamDecals.Length; i++)
		{
			splashFoamDecals[i].Tick(dt, OwnerShip);
		}
	}

	public void FixedUpdate(float fixedDt, in MatrixFrame shipGlobalFrame, MBList<(MissionShip ship, OarSidePhaseController.OarSide shipSide)> nearbyShips)
	{
		if (!IsUsed)
		{
			IsRetracting = true;
			_timeLeftToCheckForCloseShipsForRetraction = 0f;
		}
		else
		{
			_timeLeftToCheckForCloseShipsForRetraction -= fixedDt;
			if (_timeLeftToCheckForCloseShipsForRetraction < 0f)
			{
				_timeLeftToCheckForCloseShipsForRetraction = _oarEffectsRandom.NextFloatRanged(0.15f, 0.2f);
				IsRetracting = false;
				Vec3 v = shipGlobalFrame.TransformToParent(in _oarGateOffset);
				foreach (var nearbyShip in nearbyShips)
				{
					if (_sidePhaseData.Side == nearbyShip.shipSide)
					{
						MissionShip item = nearbyShip.ship;
						Vec3 localPoint = item.GameEntity.GetBodyWorldTransform().TransformToLocal(in v);
						Vec3 closestPointToBoundingBox = item.Physics.GetClosestPointToBoundingBox(in localPoint);
						float num = DeckParameters.OarLength + DeckParameters.RetractionOffset;
						if (closestPointToBoundingBox.DistanceSquared(localPoint) < num * num)
						{
							IsRetracting = true;
							break;
						}
					}
				}
			}
		}
		_bladeContact = ComputeBladeContactPosition();
	}

	public Vec3 ComputeBladeContactPosition(bool ignoreRetraction = true)
	{
		float retraction = (ignoreRetraction ? 1f : Extraction);
		return ComputeBladeContactPositionAux(in _oarGateOffset, DeckParameters, _sidePhaseData.Phase, retraction);
	}

	public Vec3 ComputeBladeVisualContactPosition(bool ignoreRetraction = true)
	{
		float retraction = (ignoreRetraction ? 1f : Extraction);
		float verticalBaseAngleOffset = _sidePhaseData.VisualVerticalBaseAngleOffsetFromShipRoll + _visualVerticalBaseAngleOffset;
		float verticalAngleMultiplier = _sidePhaseData.CycleArcSizeMult * _visualVerticalAngleMultiplier;
		float visualLateralAngleMultiplier = _visualLateralAngleMultiplier;
		return ComputeBladeContactPositionAux(in _oarGateOffset, DeckParameters, VisualPhase, retraction, verticalBaseAngleOffset, verticalAngleMultiplier, visualLateralAngleMultiplier);
	}

	public static Vec3 ComputeBladeContactPositionAux(in Vec3 gateOffset, OarDeckParameters deckParameters, float phase = 0f, float retraction = 1f, float verticalBaseAngleOffset = 0f, float verticalAngleMultiplier = 1f, float lateralAngleMultiplier = 1f)
	{
		int num = TaleWorlds.Library.MathF.Sign(gateOffset.x);
		Vec3 vec = new Vec3((float)num * deckParameters.OarLength * retraction);
		float verticalRotationAngle = deckParameters.VerticalRotationAngle * verticalAngleMultiplier;
		float lateralRotationAngle = deckParameters.LateralRotationAngle * lateralAngleMultiplier;
		float a = (float)num * GetVerticalAngle(phase, deckParameters.VerticalBaseAngle + verticalBaseAngleOffset, verticalRotationAngle);
		float a2 = (float)num * GetLateralAngle(phase, deckParameters.LateralBaseAngle, lateralRotationAngle);
		TaleWorlds.Library.MathF.SinCos(a, out var sa, out var ca);
		vec.z = (0f - vec.x) * sa;
		vec.x *= ca;
		TaleWorlds.Library.MathF.SinCos(a2, out sa, out ca);
		vec.y = vec.x * sa;
		vec.x *= ca;
		return gateOffset + vec;
	}

	public Vec3 ComputeBladeContactVelocity(bool ignoreRetraction = false)
	{
		float retraction = (ignoreRetraction ? 1f : Extraction);
		return ComputeBladeContactVelocityAux(DeckParameters, _sidePhaseData.Phase, _sidePhaseData.PhaseRate, retraction);
	}

	public static Vec3 ComputeBladeContactVelocityAux(OarDeckParameters deckParameters, float phase, float phaseRate, float retraction = 1f)
	{
		float verticalAngle = GetVerticalAngle(phase, deckParameters.VerticalBaseAngle, deckParameters.VerticalRotationAngle);
		float lateralAngle = GetLateralAngle(phase, deckParameters.LateralBaseAngle, deckParameters.LateralRotationAngle);
		float num = TaleWorlds.Library.MathF.Sin(0f - phase) * deckParameters.VerticalRotationAngle * phaseRate;
		float num2 = (0f - TaleWorlds.Library.MathF.Cos(0f - phase)) * deckParameters.LateralRotationAngle * phaseRate;
		float num3 = TaleWorlds.Library.MathF.Sin(verticalAngle);
		float num4 = TaleWorlds.Library.MathF.Cos(verticalAngle);
		float num5 = TaleWorlds.Library.MathF.Sin(lateralAngle);
		float num6 = TaleWorlds.Library.MathF.Cos(lateralAngle);
		float num7 = retraction * deckParameters.OarLength;
		float x = (0f - num7) * num3 * num * num6 - num7 * num4 * num5 * num2;
		float y = (0f - num7) * num3 * num * num5 + num7 * num4 * num6 * num2;
		float z = (0f - num7) * num4 * num;
		return new Vec3(x, y, z);
	}

	public static float GetVerticalAngle(float phase, float verticalBaseAngle, float verticalRotationAngle)
	{
		return verticalBaseAngle + TaleWorlds.Library.MathF.Cos(0f - phase) * verticalRotationAngle;
	}

	public static float GetLateralAngle(float phase, float lateralBaseAngle, float lateralRotationAngle)
	{
		return lateralBaseAngle + TaleWorlds.Library.MathF.Sin(0f - phase) * lateralRotationAngle;
	}

	public static MissionOar CreateShipOar(MissionShip ownerShip, GameEntity entity, OarDeckParameters deckParameters, OarSidePhaseController sidePhase)
	{
		return new MissionOar(ownerShip, entity, deckParameters, sidePhase);
	}

	public MatrixFrame ComputeOarEntityFrame(float dt, in MatrixFrame oarMachineLocalFrame, in MatrixFrame oarEntityLocalFrame, in MatrixFrame _oarExtractedEntitialFrame, in MatrixFrame _oarRetractedEntitialFrame, float _lastIdleTime, bool forUnmanned)
	{
		Vec3 v = ComputeBladeVisualContactPosition();
		Vec3 vec = oarMachineLocalFrame.TransformToLocal(in v);
		if (IsExtracted)
		{
			float currentTime = Mission.Current.CurrentTime;
			MatrixFrame result = _oarExtractedEntitialFrame;
			result.rotation.f = vec - result.origin;
			result.rotation.Orthonormalize();
			float num = _phaseDelayOffset * TaleWorlds.Library.MathF.Sin(currentTime * _phaseDelayOffsetTimeScale);
			float num2 = ComputeOarRollAccordingToPhase(MBMath.WrapAngleSafe(num + VisualPhase)) * _visualOarRollAnimationAngleFactor + _visualOarConstantRollAngle;
			if (_sidePhaseData.Side == OarSidePhaseController.OarSide.Left)
			{
				num2 *= -1f;
			}
			result.rotation.RotateAboutForward(num2);
			float num3 = currentTime - _lastIdleTime;
			if (num3 < 1.5f)
			{
				Quaternion to = result.rotation.ToQuaternion();
				Quaternion quaternion = _oarExtractedEntitialFrame.rotation.ToQuaternion();
				result.rotation = Quaternion.Slerp(quaternion, to, num3 / 1.5f).ToMat3();
				result.rotation.Orthonormalize();
			}
			return result;
		}
		if (IsRetracted)
		{
			MatrixFrame result2 = _oarRetractedEntitialFrame;
			if (forUnmanned)
			{
				Vec2 vb = result2.origin.AsVec2 - _oarExtractedEntitialFrame.origin.AsVec2;
				result2.origin.z = _oarExtractedEntitialFrame.origin.z + (float)TaleWorlds.Library.MathF.Sign(Vec2.DotProduct(result2.rotation.f.AsVec2, vb)) * (vb.Length / result2.rotation.f.AsVec2.Length) * result2.rotation.f.z;
			}
			else
			{
				result2.origin.z = _oarExtractedEntitialFrame.origin.z + result2.origin.AsVec2.Distance(_oarExtractedEntitialFrame.origin.AsVec2) / result2.rotation.f.AsVec2.Length * result2.rotation.f.z;
			}
			return result2;
		}
		Quaternion to2 = oarEntityLocalFrame.rotation.ToQuaternion();
		Quaternion quaternion2 = (IsRetracting ? _oarRetractedEntitialFrame.rotation.ToQuaternion() : _oarExtractedEntitialFrame.rotation.ToQuaternion());
		MatrixFrame result3 = new MatrixFrame(Quaternion.Slerp(quaternion2, to2, TaleWorlds.Library.MathF.Pow(2f, dt * -3f)).ToMat3(), Vec3.Lerp(_oarRetractedEntitialFrame.origin, _oarExtractedEntitialFrame.origin, Extraction));
		result3.rotation.Orthonormalize();
		if (forUnmanned)
		{
			Vec2 vb2 = result3.origin.AsVec2 - _oarExtractedEntitialFrame.origin.AsVec2;
			result3.origin.z = _oarExtractedEntitialFrame.origin.z + (float)TaleWorlds.Library.MathF.Sign(Vec2.DotProduct(result3.rotation.f.AsVec2, vb2)) * (vb2.Length / result3.rotation.f.AsVec2.Length) * result3.rotation.f.z;
		}
		else
		{
			result3.origin.z = _oarExtractedEntitialFrame.origin.z + result3.origin.AsVec2.Distance(_oarExtractedEntitialFrame.origin.AsVec2) / result3.rotation.f.AsVec2.Length * result3.rotation.f.z;
		}
		return result3;
	}

	private float ComputeOarRollAccordingToPhase(float phase)
	{
		OarRollAnimKeyFrame[] array = OarRollAnimManager.RollAnimations[_visualOarRollAnimationIndex];
		float num = (phase + System.MathF.PI) / (System.MathF.PI * 2f);
		if (num >= 1f)
		{
			num -= 1f;
		}
		float result = 0f;
		for (int i = 0; i < array.Length - 1; i++)
		{
			OarRollAnimKeyFrame oarRollAnimKeyFrame = array[i];
			OarRollAnimKeyFrame oarRollAnimKeyFrame2 = array[i + 1];
			if (oarRollAnimKeyFrame.KeyProgress <= num && num < oarRollAnimKeyFrame2.KeyProgress)
			{
				float num2 = oarRollAnimKeyFrame2.KeyProgress - oarRollAnimKeyFrame.KeyProgress;
				float amount = (num - oarRollAnimKeyFrame.KeyProgress) / num2;
				result = TaleWorlds.Library.MathF.Lerp(oarRollAnimKeyFrame.RollAngleInRad, oarRollAnimKeyFrame2.RollAngleInRad, amount);
				break;
			}
		}
		return result;
	}

	public void SetOarForceMultiplierFromUserAgent(float forceMultiplierFromUserAgent)
	{
		ForceMultiplierFromUserAgent = forceMultiplierFromUserAgent;
	}

	public void OnPilotAssignedDuringSpawn()
	{
		IsRetracting = false;
		Extraction = 1f;
	}

	public bool IsInRowingMotion()
	{
		return _sidePhaseData.IsInRowingMotion();
	}
}
