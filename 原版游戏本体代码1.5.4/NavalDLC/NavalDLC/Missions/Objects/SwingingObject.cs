using System;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

public class SwingingObject : MissionObject
{
	[EditableScriptComponentVariable(true, "Damping")]
	public float _damping = 5f;

	[EditableScriptComponentVariable(true, "Center of Mass Height")]
	public float _centerOfMassHeight = -0.8f;

	[EditableScriptComponentVariable(true, "Mass")]
	public float _mass = 1f;

	[EditableScriptComponentVariable(true, "Moment Of Inertia")]
	public float _momentOfInertia = 0.5f;

	[EditableScriptComponentVariable(true, "Reset Simulation")]
	public SimpleButton _resetSimulation = new SimpleButton();

	[EditableScriptComponentVariable(true, "Test Collision")]
	public SimpleButton _testCollision = new SimpleButton();

	private Vec2 _currSwing;

	private Vec2 _prevSwing;

	private Vec2 _swingVelocity;

	private float _minLimitXRotation;

	private Vec3 _accumulatedAcceleration = Vec3.Zero;

	private WeakGameEntity _swingingEntity = WeakGameEntity.Invalid;

	private Vec3 _parentPrevVelocity = Vec3.Zero;

	private MatrixFrame _frameWrtDynamicRoot = MatrixFrame.Identity;

	private Scene _ownerSceneCached;

	internal SwingingObject()
	{
	}

	public void DummyFunc()
	{
		Debug.Print(_resetSimulation.ToString());
	}

	private void InitAux()
	{
		_swingingEntity = base.GameEntity.GetFirstChildEntityWithTag("swinging_entity");
		_frameWrtDynamicRoot = base.GameEntity.Root.GetGlobalFrame().TransformToLocalNonOrthogonal(base.GameEntity.GetGlobalFrame());
		_ownerSceneCached = base.GameEntity.Scene;
		Vec3 origin = base.GameEntity.GetFirstChildEntityWithName("collision_sphere").GetLocalFrame().origin;
		origin.x = 0f;
		origin.y = 0f - origin.y;
		origin.Normalize();
		_minLimitXRotation = 0f - Vec3.DotProduct(origin, Vec3.Forward);
		if (_minLimitXRotation < -System.MathF.PI / 3f)
		{
			_minLimitXRotation = -System.MathF.PI / 3f;
		}
	}

	protected override void OnInit()
	{
		InitAux();
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		if (fixedDt > 0f)
		{
			HandleSwingMotion(fixedDt);
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		if (variableName == "Reset Simulation")
		{
			_prevSwing = Vec2.Zero;
			_currSwing = Vec2.Zero;
			_swingVelocity = Vec2.Zero;
		}
		else if (variableName == "Test Collision")
		{
			InitAux();
			_prevSwing.x = _minLimitXRotation;
			_currSwing.x = _minLimitXRotation;
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel | TickRequirement.FixedParallelTick;
	}

	protected override void OnTickParallel(float dt)
	{
		if (_ownerSceneCached.GetEnginePhysicsEnabled())
		{
			_ownerSceneCached.GetInterpolationFactorForBodyWorldTransformSmoothing(out var interpolationFactor, out var _);
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation.RotateAboutForward(TaleWorlds.Library.MathF.Lerp(_prevSwing.y, _currSwing.y, interpolationFactor));
			frame.rotation.RotateAboutSide(TaleWorlds.Library.MathF.Lerp(_prevSwing.x, _currSwing.x, interpolationFactor));
			_swingingEntity.SetFrame(ref frame, isTeleportation: false);
		}
	}

	private void HandleSwingMotion(float fixedDt)
	{
		Vec3 vec = Vec3.Zero;
		MatrixFrame matrixFrame;
		if (base.GameEntity.Root.HasPhysicsBody())
		{
			matrixFrame = base.GameEntity.Root.GetBodyWorldTransform().TransformToParent(in _frameWrtDynamicRoot);
			vec = base.GameEntity.Root.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(matrixFrame.origin);
		}
		else
		{
			matrixFrame = base.GameEntity.Root.GetFrame().TransformToParent(in _frameWrtDynamicRoot);
		}
		Vec3 vec2 = (vec - _parentPrevVelocity) / fixedDt;
		_parentPrevVelocity = vec;
		Vec3 v = MBGlobals.GravitationalAcceleration - vec2 + _accumulatedAcceleration;
		_accumulatedAcceleration = Vec3.Zero;
		MatrixFrame matrixFrame2 = matrixFrame.TransformToParent(_swingingEntity.GetFrame());
		Vec3 v2 = matrixFrame2.origin;
		Vec3 v3 = v2 + matrixFrame2.rotation.u * _centerOfMassHeight;
		Vec3 vec3 = matrixFrame2.TransformToLocalNonOrthogonal(in v2);
		Vec3 vec4 = matrixFrame2.TransformToLocalNonOrthogonal(in v3);
		Vec3 vec5 = matrixFrame2.rotation.TransformToLocal(in v);
		Vec3 va = vec4 - vec3;
		Vec3 vb = vec5 * _mass;
		Vec3 v4 = Vec3.CrossProduct(va, vb);
		float num = TaleWorlds.Library.MathF.Max(_momentOfInertia * _mass, 0.001f);
		Vec3 side = Vec3.Side;
		float num2 = Vec3.DotProduct(v4, side) / num;
		_swingVelocity.x += num2 * fixedDt;
		Vec3 forward = Vec3.Forward;
		float num3 = Vec3.DotProduct(v4, forward) / num;
		_swingVelocity.y += num3 * fixedDt;
		if (TaleWorlds.Library.MathF.Abs(_swingVelocity.x) > 5f)
		{
			_swingVelocity.x = 5f * (float)TaleWorlds.Library.MathF.Sign(_swingVelocity.x);
		}
		if (TaleWorlds.Library.MathF.Abs(_swingVelocity.y) > 5f)
		{
			_swingVelocity.y = 5f * (float)TaleWorlds.Library.MathF.Sign(_swingVelocity.y);
		}
		_prevSwing = _currSwing;
		_currSwing += _swingVelocity * fixedDt;
		if (_currSwing.x > System.MathF.PI / 3f && _swingVelocity.x > 0f)
		{
			_swingVelocity.x *= -0.1f;
		}
		if (_currSwing.x < _minLimitXRotation)
		{
			_currSwing.x = _minLimitXRotation;
			if (_swingVelocity.x < 0f)
			{
				_swingVelocity.x *= -0.1f;
			}
		}
		if (_currSwing.y > System.MathF.PI / 3f)
		{
			_currSwing.y = System.MathF.PI / 3f;
			if (_swingVelocity.y > 0f)
			{
				_swingVelocity.y *= -0.1f;
			}
		}
		if (_currSwing.y < -System.MathF.PI / 3f)
		{
			_currSwing.y = -System.MathF.PI / 3f;
			if (_swingVelocity.y < 0f)
			{
				_swingVelocity.y *= -0.1f;
			}
		}
		Vec2 swingVelocity = _swingVelocity;
		float num4 = swingVelocity.Normalize();
		float num5 = (_damping * 0.2f * num4 + _damping * 0.03f) / _mass;
		if (num5 > num4)
		{
			_swingVelocity = Vec2.Zero;
		}
		else
		{
			_swingVelocity -= swingVelocity * num5;
		}
	}

	protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage, out float finalDamage, out float fireDamage, out float modifiedFireDamage)
	{
		float value = ((weapon.Item != null) ? weapon.GetWeight() : 1f);
		value = TaleWorlds.Library.MathF.Clamp(value, 0.5f, 2f);
		Vec3 vec = impactDirection * value * 300f;
		_accumulatedAcceleration += vec / _mass;
		reportDamage = false;
		finalDamage = 0f;
		fireDamage = -1f;
		modifiedFireDamage = -1f;
		return true;
	}
}
