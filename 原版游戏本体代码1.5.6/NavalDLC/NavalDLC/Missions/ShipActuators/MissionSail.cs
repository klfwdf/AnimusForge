using System;
using System.Collections.Generic;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.ShipActuators;

public class MissionSail : MissionObject
{
	public enum SailTurningState : sbyte
	{
		Stationary,
		TurningLeft,
		TurningRight
	}

	public const float OptimalDirectionSearchInterval = 1f;

	private const int PhysicsPointCountPerAxis = 9;

	private const float BlowSoundEventCooldown = 10f;

	private static readonly int _sailContinuousSoundEventId = SoundEvent.GetEventIdFromString("event:/mission/movement/vessel/sail/sail_movement");

	private static readonly int _sailRotationSoundEventId = SoundEvent.GetEventIdFromString("event:/mission/movement/vessel/sail/sail_rotation");

	private const float MinSearchSpaceForTargetSailRotationInRadians = System.MathF.PI / 30f;

	private ShipSail _sailObject;

	private MissionShip _ownerShip;

	private SailVisual _sailVisual;

	private float _localSailRotation;

	private float _currentSailRotationSpeed;

	private Vec3 _centerOfSailForceShipLocal;

	private float _width;

	private float _height;

	private float _sailRotationStateTimer;

	private float _fullSailWeight;

	private bool _fullSailMode;

	private ShipForce _force;

	private bool _gustMode;

	private SailTurningState _currentSailTurningState;

	private float _targetSailRotation;

	private SoundEvent _sailContinuousSoundEvent;

	private SoundEvent _sailRotationSoundEvent;

	private float _blowSoundEventCooldown;

	private float _sailSoundEventRotationParam;

	private bool _shouldMakeBlowingSound;

	public override TextObject HitObjectName => new TextObject("{=92jVTPDA}Ship Sails");

	public ShipSail SailObject => _sailObject;

	public ShipForce Force => _force;

	public float LocalSailRotation => _localSailRotation;

	public float Setting { get; private set; }

	public float TargetSailSetting { get; private set; }

	public Vec3 CenterOfSailForceShipLocal => _centerOfSailForceShipLocal;

	public float FoldDuration => _sailVisual.TotalFoldDuration;

	public float UnfoldDuration => _sailVisual.TotalUnfoldDuration;

	public GameEntity SailEntity { get; private set; }

	public float Area => _width * ((_sailObject.Type == SailType.Lateen) ? (_height * 0.5f) : _height);

	internal void InitWithVariables(ShipSail sailObject, MissionShip ownerShip, SailVisual sailVisual)
	{
		_sailObject = sailObject;
		_ownerShip = ownerShip;
		_sailVisual = sailVisual;
		SailEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(sailVisual.GameEntity);
		InitializeCenterOfSailForceLocal();
		Setting = 0f;
		_sailRotationStateTimer = 7f;
		_fullSailWeight = 0f;
		_localSailRotation = (0f - _sailObject.RightRotationLimit + _sailObject.LeftRotationLimit) * 0.5f;
		_localSailRotation = TaleWorlds.Library.MathF.Clamp(_localSailRotation, 0f - _sailObject.RightRotationLimit, _sailObject.LeftRotationLimit);
		_targetSailRotation = _localSailRotation;
		_currentSailRotationSpeed = 0f;
		TargetSailSetting = 1f;
		_currentSailTurningState = SailTurningState.Stationary;
		_gustMode = false;
		SetVisualSailEnabled(visualSailEnabled: false);
		InitializeSailSounds();
		InitSailRotationAccordingToWindDirection();
	}

	private void InitSailRotationAccordingToWindDirection()
	{
		Vec2 globalWindVelocity = _ownerShip.Scene.GetGlobalWindVelocity();
		if (globalWindVelocity.LengthSquared > 1f)
		{
			Vec2 relWindDirection2DShip = _ownerShip.GameEntity.GetGlobalFrame().rotation.TransformToLocal(globalWindVelocity.ToVec3()).AsVec2.Normalized();
			FixedTickTargetSailRotation(relWindDirection2DShip, forceFindTheBestAngle: true);
			_localSailRotation = _targetSailRotation;
		}
	}

	public bool CheckSailFlags(bool editMode)
	{
		List<GameEntity> children = new List<GameEntity>();
		SailEntity.GetChildrenRecursive(ref children);
		bool flag = false;
		children.Add(SailEntity);
		foreach (GameEntity item in children)
		{
			if (!item.EntityFlags.HasAnyFlag(EntityFlags.DontSaveToScene) && !item.EntityFlags.HasAnyFlag(EntityFlags.DoesNotAffectParentsLocalBb))
			{
				flag = true;
			}
		}
		if (flag)
		{
			string msg = "In Root Entity " + SailEntity.Root.Name + ", " + SailEntity.Name + "'s every descendant including itself must have Does not Affect Parent's Local Bounding Box flag.";
			if (editMode)
			{
				MBEditor.AddEntityWarning(SailEntity.WeakEntity, msg);
			}
		}
		return flag;
	}

	public void UpdateForcedWindOfSailsAndTopBanner(float dt)
	{
		Vec3 linearVelocity = _ownerShip.Physics.LinearVelocity;
		Vec3 angularVelocity = _ownerShip.Physics.AngularVelocity;
		Vec3 vec = _ownerShip.GameEntity.GetBodyWorldTransform().TransformToParent(_ownerShip.Physics.LocalCenterOfMass);
		Vec3 vec2 = _ownerShip.Scene.GetGlobalWindVelocity().ToVec3();
		Vec3 vb = ComputeCenterOfSailForceGlobal() - vec;
		Vec3 vec3 = Vec3.CrossProduct(angularVelocity, vb) + linearVelocity;
		Vec3 sailRelativeGlobalWindVelocity = vec2 - vec3;
		Vec3 vb2 = _sailVisual.SailTopBannerEntity.GetGlobalFrame().origin - vec;
		Vec3 vec4 = Vec3.CrossProduct(angularVelocity, vb2) + linearVelocity;
		Vec3 globalBannerRelativeWindVelocity = vec2 - vec4;
		Vec3 globalSailForce = ((!_force.IsApplicable) ? Vec3.Zero : (_force.Force / _force.GamifiedForceMultiplier));
		_sailVisual.UpdateForcedWindOfSailsAndTopBanner(dt, globalBannerRelativeWindVelocity, in sailRelativeGlobalWindVelocity, in globalSailForce);
	}

	private void SetTargetSailSetting(in ShipActuatorRecord actuatorInput)
	{
		if (_sailObject.Type == SailType.Square)
		{
			TargetSailSetting = actuatorInput.SquareSailSetting;
		}
		else if (_sailObject.Type == SailType.Lateen)
		{
			TargetSailSetting = actuatorInput.LateenSailSetting;
		}
	}

	private void FixedUpdateSailForce(Vec3 windVelocityGlobal, Vec3 sailLinearVelocityGlobal, Vec3 sailLinearVelocityFromAngularGlobal)
	{
		MatrixFrame shipFrame = _ownerShip.GameEntity.GetBodyWorldTransform();
		Vec3 v = Compute3DSailDirection();
		Vec3 force = Vec3.Zero;
		if (shipFrame.rotation.u.z > 0f)
		{
			Vec3 v2 = windVelocityGlobal * shipFrame.rotation.u.z * shipFrame.rotation.u.z - sailLinearVelocityGlobal;
			float num = 16f;
			if (v2.LengthSquared > num * num)
			{
				v2 = v2.NormalizedCopy() * num;
			}
			Vec2 relWindDirection2DShip = shipFrame.rotation.TransformToLocal(in v2).AsVec2;
			float num2 = relWindDirection2DShip.Normalize();
			Vec2 sailDirection2DShip = shipFrame.rotation.TransformToLocal(in v).AsVec2.Normalized();
			float num3 = TaleWorlds.Library.MathF.Abs(shipFrame.rotation.u.z);
			float effectiveSailArea = Setting * Area * num3;
			float relWindSpeed2DShip = num2 * num3;
			Vec3 vec = ComputeSailForce(in sailDirection2DShip, in relWindDirection2DShip, relWindSpeed2DShip, in shipFrame, effectiveSailArea, _sailObject.Type);
			if (_gustMode)
			{
				vec *= 0.5f;
			}
			force += vec;
		}
		Vec3 v3 = -sailLinearVelocityFromAngularGlobal;
		Vec2 relWindDirection2DShip2 = shipFrame.rotation.TransformToLocal(in v3).AsVec2;
		float num4 = relWindDirection2DShip2.Normalize();
		Vec2 sailDirection2DShip2 = shipFrame.rotation.TransformToLocal(in v).AsVec2.Normalized();
		float effectiveSailArea2 = Setting * Area;
		float relWindSpeed2DShip2 = num4;
		Vec3 vec2 = ComputeSailForce(in sailDirection2DShip2, in relWindDirection2DShip2, relWindSpeed2DShip2, in shipFrame, effectiveSailArea2, _sailObject.Type);
		if (shipFrame.rotation.u.z <= 0f)
		{
			vec2 *= 2f;
		}
		force += vec2;
		float num5 = (1f + _ownerShip.ShipOrigin.SailForceFactor) * _sailObject.ForceMultiplier;
		force *= num5;
		float num6 = force.Normalize();
		float num7 = MissionGameModels.Current.MissionShipParametersModel.CalculateWindBonus(_ownerShip.ShipOrigin, _ownerShip.Captain, num6);
		float num8 = ((num6 > 0f) ? (num7 / num6) : 1f);
		num5 *= num8;
		force *= num7;
		_force = new ShipForce(in _centerOfSailForceShipLocal, in force, ShipForce.SourceType.Sail, num5);
	}

	public void FixedUpdate(float fixedDt, in ShipActuatorRecord actuatorInput, in Vec3 shipLinearVelocityGlobal, in Vec3 shipAngularVelocityGlobal)
	{
		if (_ownerShip.ShipSailState == MissionShip.SailState.Intact)
		{
			Vec3 vec = _ownerShip.GameEntity.GetBodyWorldTransform().TransformToParent(_ownerShip.Physics.LocalCenterOfMass);
			Vec3 vb = ComputeCenterOfSailForceGlobal() - vec;
			Vec3 sailLinearVelocityFromAngularGlobal = Vec3.CrossProduct(shipAngularVelocityGlobal, vb);
			Vec3 vec2 = shipLinearVelocityGlobal;
			Vec3 vec3 = _ownerShip.GameEntity.GetGlobalWindVelocityOfScene().ToVec3();
			Vec3 relWindVelocityGlobal = vec3 - vec2;
			SetTargetSailSetting(in actuatorInput);
			float localSailRotation = _localSailRotation;
			FixedUpdateSailRotation(fixedDt, in actuatorInput, in relWindVelocityGlobal);
			if (TargetSailSetting == 1f)
			{
				Vec3 force = _force.Force;
				FixedUpdateSailForce(vec3, vec2, sailLinearVelocityFromAngularGlobal);
				if (_ownerShip.ShouldUpdateSoundPos && _blowSoundEventCooldown <= 0.01f && _force.Force.LengthSquared / force.LengthSquared > 1.21f)
				{
					_shouldMakeBlowingSound = true;
					_blowSoundEventCooldown += 10f;
				}
				CalculateSailSoundEventRotationParamAndShouldUpdateSoundPos(fixedDt, TaleWorlds.Library.MathF.Abs(_localSailRotation - localSailRotation));
				_blowSoundEventCooldown -= fixedDt;
				_blowSoundEventCooldown = ((_blowSoundEventCooldown < 0f) ? 0f : _blowSoundEventCooldown);
			}
			else
			{
				_force = new ShipForce(in _centerOfSailForceShipLocal, in Vec3.Zero, ShipForce.SourceType.Sail, 1f);
			}
		}
		else
		{
			_force = new ShipForce(in _centerOfSailForceShipLocal, in Vec3.Zero, ShipForce.SourceType.Sail, 1f);
		}
	}

	private void UpdateSailRotationVisual(float dt)
	{
		float value = _targetSailRotation - _localSailRotation;
		float val = Math.Abs(value);
		float val2 = dt * _currentSailRotationSpeed;
		val2 = Math.Min(val, val2);
		float value2 = _localSailRotation + (float)Math.Sign(value) * val2;
		_localSailRotation = TaleWorlds.Library.MathF.Clamp(value2, 0f - _sailObject.RightRotationLimit, _sailObject.LeftRotationLimit);
	}

	private void UpdateSailSetting(float dt)
	{
		float targetSailSetting = TargetSailSetting;
		float num = ((targetSailSetting - Setting >= 0f) ? UnfoldDuration : FoldDuration);
		float num2 = 1f / num;
		Setting = ShipActuators.ComputeActuatorParameter(Setting, targetSailSetting, dt, num2 * (1f + _ownerShip.ShipOrigin.FurlUnfurlSpeedFactor));
		Setting = TaleWorlds.Library.MathF.Clamp(Setting, 0f, 1f);
	}

	private void UpdateSailVisuals(float dt)
	{
		MatrixFrame frame = _sailVisual.SailYawRotationEntity.GetLocalFrame();
		frame.rotation = Mat3.Identity;
		frame.rotation.RotateAboutUp(_localSailRotation);
		_sailVisual.SailYawRotationEntity.SetLocalFrame(ref frame, isTeleportation: false);
		SetVisualSailEnabled(TargetSailSetting > 0.5f);
		UpdateForcedWindOfSailsAndTopBanner(dt);
	}

	private void UpdateSoundPos()
	{
		if (_ownerShip.ShouldUpdateSoundPos && _sailContinuousSoundEvent == null)
		{
			_sailContinuousSoundEvent = SoundEvent.CreateEvent(_sailContinuousSoundEventId, _ownerShip.GameEntity.Scene);
			_sailRotationSoundEvent = SoundEvent.CreateEvent(_sailRotationSoundEventId, _ownerShip.GameEntity.Scene);
			Vec3 position = ComputeCenterOfSailForceGlobal();
			_sailContinuousSoundEvent.SetPosition(position);
			_sailRotationSoundEvent.SetPosition(position);
			_sailRotationSoundEvent.SetParameter("SailRotation", _sailSoundEventRotationParam);
			_sailRotationSoundEvent.Play();
			_sailContinuousSoundEvent.Play();
		}
		else if (_ownerShip.ShouldUpdateSoundPos)
		{
			Vec3 position2 = ComputeCenterOfSailForceGlobal();
			_sailContinuousSoundEvent.SetPosition(position2);
			_sailRotationSoundEvent.SetPosition(position2);
			_sailRotationSoundEvent.SetParameter("SailRotation", _sailSoundEventRotationParam);
			if (_shouldMakeBlowingSound)
			{
				MatrixFrame globalFrame = SailEntity.GetGlobalFrame();
				SoundManager.StartOneShotEvent("event:/mission/movement/vessel/sail/sail_blow", in globalFrame.origin);
				_shouldMakeBlowingSound = false;
			}
		}
		else if (_sailContinuousSoundEvent != null)
		{
			_sailRotationSoundEvent.Stop();
			_sailContinuousSoundEvent.Stop();
			_sailRotationSoundEvent = null;
			_sailContinuousSoundEvent = null;
		}
	}

	public void Update(float dt)
	{
		UpdateSailRotationVisual(dt);
		UpdateSailSetting(dt);
		UpdateSailVisuals(dt);
		UpdateSoundPos();
	}

	public static Vec3 ComputeSailForce(in Vec2 sailDirection2DShip, in Vec2 relWindDirection2DShip, float relWindSpeed2DShip, in MatrixFrame shipFrame, float effectiveSailArea, SailType sailType)
	{
		Vec2 sailForceCoefficients = SailWindProfile.Instance.GetSailForceCoefficients(sailType, sailDirection2DShip, relWindDirection2DShip);
		float num = relWindSpeed2DShip * relWindSpeed2DShip;
		float airDensity = GameModels.Instance.ShipPhysicsParametersModel.GetAirDensity();
		Vec2 vec = 0.5f * airDensity * num * sailForceCoefficients * effectiveSailArea;
		return shipFrame.rotation.TransformToParent(vec.ToVec3());
	}

	public float ComputeMaximumForceMagnitudeSailCanApply()
	{
		Vec2 maximumSailForceCoefficients = SailWindProfile.Instance.GetMaximumSailForceCoefficients(_sailObject.Type);
		float maximumWindSpeed = Scene.MaximumWindSpeed;
		float area = Area;
		return 0.5f * GameModels.Instance.ShipPhysicsParametersModel.GetAirDensity() * maximumWindSpeed * maximumWindSpeed * (_sailObject.ForceMultiplier * maximumSailForceCoefficients.Length) * area * (1f + _ownerShip.ShipOrigin.SailForceFactor);
	}

	private Vec3 ComputeWindVectorForSailVisuals(in Vec3 sailForceGlobal)
	{
		Vec3 vec = sailForceGlobal.NormalizedCopy();
		float num = TaleWorlds.Library.MathF.Sqrt(sailForceGlobal.Length * 2f / (GameModels.Instance.ShipPhysicsParametersModel.GetAirDensity() * _sailObject.ForceMultiplier * Area));
		return vec * num;
	}

	private void SetVisualSailEnabled(bool visualSailEnabled)
	{
		_sailVisual.SailEnabled = visualSailEnabled;
	}

	private void FixedTickFullSailInputWeight(float fixedDt, in ShipActuatorRecord actuatorInput)
	{
		float num = actuatorInput.RowerThrust;
		if (TargetSailSetting <= 0f || (!_gustMode && TargetSailSetting < 1f))
		{
			num = 0f;
		}
		if (num > 0f)
		{
			float rowerThrustDoubleTap = actuatorInput.RowerThrustDoubleTap;
			if (_fullSailWeight >= 0f)
			{
				_fullSailWeight += fixedDt * 0.4f;
				if (rowerThrustDoubleTap > 0f && _fullSailWeight < 0.5f)
				{
					_fullSailWeight = 0.5f;
				}
			}
			else
			{
				_fullSailWeight += fixedDt * 2f;
				_fullSailMode = false;
			}
			if (_fullSailWeight >= 1f)
			{
				_fullSailMode = true;
				_fullSailWeight = 1f;
			}
		}
		else if (num < 0f)
		{
			if (_fullSailWeight <= 0f)
			{
				_fullSailWeight -= fixedDt * 0.4f;
			}
			else
			{
				_fullSailWeight -= fixedDt * 2f;
				_fullSailMode = false;
			}
			if (_fullSailWeight <= -1f)
			{
				_fullSailMode = true;
				_fullSailWeight = -1f;
			}
		}
		else
		{
			float num2 = fixedDt * 2f;
			if (TaleWorlds.Library.MathF.Abs(_fullSailWeight) <= num2)
			{
				_fullSailMode = false;
				_fullSailWeight = 0f;
			}
			else
			{
				_fullSailWeight -= (float)TaleWorlds.Library.MathF.Sign(_fullSailWeight) * num2;
			}
		}
	}

	public bool GetVisualSailEnabled()
	{
		return _sailVisual.SailEnabled;
	}

	private void FixedTickTargetSailRotation(Vec2 relWindDirection2DShip, bool forceFindTheBestAngle)
	{
		float num = ((_currentSailTurningState != SailTurningState.Stationary) ? _targetSailRotation : _localSailRotation);
		Vec2 forward = Vec2.Forward;
		forward.RotateCCW(num);
		float num2 = SailWindProfile.Instance.ComputeSailThrustValue(_sailObject.Type, forward, Vec2.Forward, relWindDirection2DShip);
		float targetSailRotation = num;
		float num3 = 1f;
		if (!forceFindTheBestAngle && !_gustMode && _currentSailTurningState == SailTurningState.Stationary)
		{
			num3 = ((!_fullSailMode || !(_fullSailWeight > 0f)) ? 1.3f : 1.1f);
		}
		float num4 = num2 * num3;
		float num5 = 0f - _sailObject.RightRotationLimit;
		float num6 = _sailObject.LeftRotationLimit;
		if (_currentSailTurningState == SailTurningState.TurningLeft)
		{
			num5 = _localSailRotation;
		}
		else if (_currentSailTurningState == SailTurningState.TurningRight)
		{
			num6 = _localSailRotation;
		}
		float num7 = (num6 - num5) * 0.01f;
		if (num6 - num5 > System.MathF.PI / 30f)
		{
			for (float num8 = num5; num8 <= num6; num8 += num7)
			{
				Vec2 forward2 = Vec2.Forward;
				forward2.RotateCCW(num8);
				float num9 = SailWindProfile.Instance.ComputeSailThrustValue(_sailObject.Type, forward2, Vec2.Forward, relWindDirection2DShip);
				float num10 = num9;
				if (num10 > num4)
				{
					num4 = num10;
					num2 = num9;
					targetSailRotation = num8;
				}
			}
			if (forceFindTheBestAngle)
			{
				if (num2 > 0f)
				{
					_targetSailRotation = targetSailRotation;
				}
			}
			else if (!_gustMode || num2 > 0f)
			{
				_targetSailRotation = targetSailRotation;
			}
		}
		_targetSailRotation = TaleWorlds.Library.MathF.Clamp(_targetSailRotation, 0f - _sailObject.RightRotationLimit, _sailObject.LeftRotationLimit);
	}

	private void FixedTickSailGustMode(float thrustDirection, float curSailThrustValue, float maxThrustValue)
	{
		if (thrustDirection >= 0f)
		{
			if (_fullSailMode && _fullSailWeight > 0f)
			{
				_gustMode = curSailThrustValue < 0f;
			}
			else if (_gustMode && (curSailThrustValue > 0f || maxThrustValue > 0f || !(curSailThrustValue * _fullSailWeight <= 0f)))
			{
				_gustMode = false;
			}
		}
		else
		{
			_gustMode = true;
		}
	}

	private Vec3 Compute3DSailDirection()
	{
		MatrixFrame bodyWorldTransform = _ownerShip.GameEntity.GetBodyWorldTransform();
		Vec3 f = bodyWorldTransform.rotation.f;
		Vec3 u = bodyWorldTransform.rotation.u;
		Vec3 result = f.RotateAboutAnArbitraryVector(u, _localSailRotation);
		result.Normalize();
		return result;
	}

	private void InitializeCenterOfSailForceLocal()
	{
		MatrixFrame shipFrame = _ownerShip.GameEntity.GetBodyWorldTransform();
		_sailVisual.GetDimensions(in shipFrame, _sailObject.Type == SailType.Lateen, out _width, out _height, out _centerOfSailForceShipLocal);
	}

	private void FixedUpdateSailRotation(float fixedDt, in ShipActuatorRecord actuatorInput, in Vec3 relWindVelocityGlobal)
	{
		Vec2 vec = _ownerShip.GameEntity.GetBodyWorldTransform().rotation.TransformToLocal(in relWindVelocityGlobal).AsVec2.Normalized();
		float rowerThrust = actuatorInput.RowerThrust;
		if (TargetSailSetting <= 0f)
		{
			_sailRotationStateTimer = float.MaxValue;
		}
		FixedTickFullSailInputWeight(fixedDt, in actuatorInput);
		bool flag = TargetSailSetting == 1f && Setting == 0f;
		if (flag)
		{
			_sailRotationStateTimer = 0f;
		}
		if (_fullSailMode && _fullSailWeight > 0f && _sailRotationStateTimer > 2f && _currentSailTurningState == SailTurningState.Stationary)
		{
			_sailRotationStateTimer = 2f;
		}
		_sailRotationStateTimer -= fixedDt;
		Vec2 forward = Vec2.Forward;
		forward.RotateCCW(_localSailRotation);
		float num = SailWindProfile.Instance.ComputeSailThrustValue(_sailObject.Type, forward, Vec2.Forward, vec);
		bool num2 = (_currentSailTurningState != SailTurningState.Stationary || _sailRotationStateTimer <= 0f) && TargetSailSetting >= 1f;
		float num3 = num;
		if (num2)
		{
			FixedTickTargetSailRotation(vec, flag);
			Vec2 forward2 = Vec2.Forward;
			forward2.RotateCCW(_targetSailRotation);
			float b = SailWindProfile.Instance.ComputeSailThrustValue(_sailObject.Type, forward2, Vec2.Forward, vec);
			num3 = TaleWorlds.Library.MathF.Max(num3, b);
			if (_currentSailTurningState == SailTurningState.Stationary && !_targetSailRotation.ApproximatelyEqualsTo(_localSailRotation, System.MathF.PI / 30f))
			{
				_sailRotationStateTimer = 30f;
				_currentSailTurningState = ((!(_targetSailRotation < _localSailRotation)) ? SailTurningState.TurningLeft : SailTurningState.TurningRight);
			}
		}
		FixedTickSailGustMode(rowerThrust, num, num3);
		if (_currentSailTurningState != SailTurningState.Stationary)
		{
			float num4 = _sailObject.RotationRate * (1f + _ownerShip.ShipOrigin.SailRotationSpeedFactor);
			float num5 = _targetSailRotation - _localSailRotation;
			float num6 = Math.Abs(num5);
			float num7 = num6 / num4;
			if (TargetSailSetting < 1f && num7 > 1f)
			{
				num6 = num4;
				num5 = (float)TaleWorlds.Library.MathF.Sign(num5) * num6;
				_targetSailRotation = _localSailRotation + num5;
				num7 = 1f;
			}
			_currentSailRotationSpeed = TaleWorlds.Library.MathF.Lerp(valueTo: (!(num7 > 1f)) ? (num6 / 1f) : num4, valueFrom: _currentSailRotationSpeed, amount: fixedDt * 2f);
			if (_currentSailRotationSpeed.ApproximatelyEqualsTo(0f, 0.005f) && num6.ApproximatelyEqualsTo(0f, 0.005f))
			{
				_sailRotationStateTimer = ((_fullSailMode && _fullSailWeight > 0f) ? 2f : 2f);
				_currentSailTurningState = SailTurningState.Stationary;
				_currentSailRotationSpeed = 0f;
			}
		}
	}

	private Vec3 ComputeCenterOfSailForceGlobal()
	{
		return _ownerShip.GameEntity.GetBodyWorldTransform().TransformToParent(in _centerOfSailForceShipLocal);
	}

	public void ForceFold()
	{
		_sailVisual.InstantCloseSails();
	}

	private void CalculateSailSoundEventRotationParamAndShouldUpdateSoundPos(float dt, float rotationDiff)
	{
		if (_ownerShip.ShouldUpdateSoundPos)
		{
			float num = dt * _sailObject.RotationRate;
			_sailSoundEventRotationParam = ((num > 0f) ? TaleWorlds.Library.MathF.Clamp(rotationDiff / num, 0f, 1f) : 0f);
		}
	}

	private void InitializeSailSounds()
	{
		CalculateSailSoundEventRotationParamAndShouldUpdateSoundPos(0f, 0f);
		UpdateSoundPos();
		_blowSoundEventCooldown = 0f;
	}

	private BoundingBox GetPhysicsBoundingBox()
	{
		BoundingBox result = default(BoundingBox);
		result.BeginRelaxation();
		MatrixFrame sailGlobalFrame = _sailVisual.SailSkeletonEntity.GetGlobalFrame();
		if (_sailObject.Type == SailType.Square)
		{
			Vec3 vec = new Vec3(-0.5f, 0f, -0.5f);
			for (int i = 0; i < 9; i++)
			{
				for (int j = 0; j < 9; j++)
				{
					result.RelaxMinMaxWithPoint(GetGlobalSailPoint(vec + 0.125f * new Vec3(j, 0f, i), in sailGlobalFrame));
				}
			}
		}
		else
		{
			Vec3 vec2 = new Vec3(-0.5f);
			for (int k = 0; k < 5; k++)
			{
				int num = 9 - k * 2;
				for (int l = 0; l < num; l++)
				{
					result.RelaxMinMaxWithPoint(GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k, 0f, -k), in sailGlobalFrame));
				}
			}
		}
		return result;
	}

	public bool IsBurningFinished()
	{
		return _sailVisual.IsBurningFinished();
	}

	public bool IsBurning()
	{
		return _sailVisual.IsBurning();
	}

	public void StartFire()
	{
		_sailVisual.StartFire();
	}

	public bool IntersectLineSegmentWithSail(in Vec3 lineSegmentStart, in Vec3 lineSegmentEnd)
	{
		BoundingBox physicsBoundingBox = GetPhysicsBoundingBox();
		if (MBMath.IntersectLineSegmentWithBoundingBox(in lineSegmentStart, in lineSegmentEnd, in physicsBoundingBox.min, in physicsBoundingBox.max))
		{
			MatrixFrame sailGlobalFrame = _sailVisual.SailSkeletonEntity.GetGlobalFrame();
			if (_sailObject.Type == SailType.Square)
			{
				Vec3 vec = new Vec3(-0.5f, 0f, -0.5f);
				for (int i = 0; i < 8; i++)
				{
					for (int j = 0; j < 8; j++)
					{
						Vec3 triA = GetGlobalSailPoint(vec + 0.125f * new Vec3(j, 0f, i), in sailGlobalFrame);
						Vec3 triC = GetGlobalSailPoint(vec + 0.125f * new Vec3(j + 1, 0f, i), in sailGlobalFrame);
						Vec3 triB = GetGlobalSailPoint(vec + 0.125f * new Vec3(j + 1, 0f, i + 1), in sailGlobalFrame);
						if (MBMath.IntersectLineSegmentWithTriangle(in lineSegmentStart, in lineSegmentEnd, in triA, in triB, in triC))
						{
							return true;
						}
						if (MBMath.IntersectLineSegmentWithTriangle(in lineSegmentStart, in lineSegmentEnd, in triA, GetGlobalSailPoint(vec + 0.125f * new Vec3(j, 0f, i + 1), in sailGlobalFrame), in triB))
						{
							return true;
						}
					}
				}
			}
			else
			{
				Vec3 vec2 = new Vec3(-0.5f);
				for (int k = 0; k < 4; k++)
				{
					int num = 9 - k * 2 - 1;
					for (int l = 0; l < num; l++)
					{
						if (l == num - 1)
						{
							Vec3 triA2 = GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k, 0f, -k), in sailGlobalFrame);
							Vec3 triC2 = GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k + 1, 0f, -k), in sailGlobalFrame);
							if (MBMath.IntersectLineSegmentWithTriangle(in lineSegmentStart, in lineSegmentEnd, in triA2, GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k, 0f, -k - 1), in sailGlobalFrame), in triC2))
							{
								return true;
							}
							continue;
						}
						Vec3 triA3 = GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k, 0f, -k), in sailGlobalFrame);
						Vec3 triC3 = GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k + 1, 0f, -k), in sailGlobalFrame);
						Vec3 triB2 = GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k + 1, 0f, -k - 1), in sailGlobalFrame);
						if (MBMath.IntersectLineSegmentWithTriangle(in lineSegmentStart, in lineSegmentEnd, in triA3, in triB2, in triC3))
						{
							return true;
						}
						if (l > 0 && MBMath.IntersectLineSegmentWithTriangle(in lineSegmentStart, in lineSegmentEnd, in triA3, GetGlobalSailPoint(vec2 + 0.125f * new Vec3(l + k, 0f, -k - 1), in sailGlobalFrame), in triB2))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private Vec3 GetGlobalSailPoint(Vec3 point, in MatrixFrame sailGlobalFrame)
	{
		Vec3 scale = sailGlobalFrame.GetScale();
		float num = _width / scale.x;
		float num2 = _height / scale.z;
		point.x *= num;
		point.z *= num2;
		float num3 = TaleWorlds.Library.MathF.Min((_sailObject.Type == SailType.Square) ? (0.5f * num2 - point.z) : (0f - point.z), (_sailObject.Type == SailType.Square) ? point.Distance(new Vec3(((point.x > 0f) ? 0.5f : (-0.5f)) * num, 0f, -0.5f * num2)) : point.Distance(new Vec3(0f, 0f, -0.5f * num2)));
		float val = ((_sailObject.Type == SailType.Square) ? (0.5f * num2 + ((point.z > 0f) ? (0f - point.z) : point.z)) : (0f - point.z));
		val = Math.Min(val, (_sailObject.Type == SailType.Square) ? (0.5f * num + ((point.x > 0f) ? (0f - point.x) : point.x)) : (0f - point.z));
		float num4 = TaleWorlds.Library.MathF.Sqrt(num3 * (val + 0.4f) / (Math.Min(num2, num) * 0.5f + 0.4f));
		point.z += (1f - Setting) * ((_sailObject.Type == SailType.Square) ? (0.25f * num2 - point.z) : (0f - point.z));
		Vec2 asVec = _force.Force.AsVec2;
		float x = asVec.Normalize();
		Vec2 vb = asVec * (TaleWorlds.Library.MathF.Sqrt(x) / 100f);
		Vec3 vec = sailGlobalFrame.TransformToParent(in point);
		if (_sailObject.Type == SailType.Square)
		{
			Vec2 vec2 = sailGlobalFrame.rotation.s.AsVec2.Normalized();
			Vec2 vec3 = sailGlobalFrame.rotation.f.AsVec2.Normalized();
			float num5 = Math.Max(0f, Vec2.DotProduct(vec3, vb));
			vec += new Vec3(vec2 * (Vec2.DotProduct(vec2, vb) * 0.65f * num4));
			vec += new Vec3(vec3 * (num5 * 0.9f * num4));
			vec += new Vec3(0f, 0f, (0.5f - point.z / num2) * 0.35f * num5 * 0.9f * num4);
			return vec + (0.5f - point.z / num2) * sailGlobalFrame.rotation.f * 0.6f;
		}
		Vec2 vec4 = -sailGlobalFrame.rotation.s.AsVec2.Normalized();
		Vec2 vec5 = -sailGlobalFrame.rotation.f.AsVec2.Normalized();
		float num6 = Math.Max(0f, Vec2.DotProduct(vec5, vb));
		vec += new Vec3(vec4 * (Vec2.DotProduct(vec4, vb) * 0.1f * num4));
		vec += new Vec3(vec5 * (num6 * 0.7f * num4));
		vec += new Vec3(0f, 0f, (0.5f - point.z / num2) * 0.1f * num6 * 0.7f * num4);
		return vec + sailGlobalFrame.rotation.f * 0.25f;
	}

	public void OnSailHit(Agent attackerAgent, float rawDamage, float inflictedDamage)
	{
		CombatLogData combatLog = new CombatLogData(isVictimAgentSameAsAttackerAgent: false, attackerAgent.IsHuman, attackerAgent.IsMine, attackerAgent.RiderAgent != null, attackerAgent.RiderAgent?.IsMine ?? false, attackerAgent.IsMount, isVictimAgentHuman: false, isVictimAgentMine: false, isVictimAgentDead: false, doesVictimAgentHaveRiderAgent: false, isVictimAgentRiderAgentIsMine: false, isVictimAgentMount: false, this, isVictimRiderAgentSameAsAttackerAgent: false, crushedThrough: false, chamber: false, 0f);
		combatLog.InflictedFireDamage = (int)rawDamage;
		combatLog.ModifiedFireDamage = TaleWorlds.Library.MathF.Round(inflictedDamage - rawDamage);
		Mission.Current.AddCombatLogSafe(attackerAgent, null, combatLog);
	}

	public void StartShipCaptureAnimation(Texture newTexture)
	{
		_sailVisual.StartFlagCaptureAnimation(newTexture);
	}
}
