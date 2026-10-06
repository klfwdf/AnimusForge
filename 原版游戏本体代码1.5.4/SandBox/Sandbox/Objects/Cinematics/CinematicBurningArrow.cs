using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.Objects.Cinematics;

public class CinematicBurningArrow : ScriptComponentBehavior
{
	private enum BurningArrowState
	{
		None,
		StartMovement,
		MovementInProgress,
		EndMovement
	}

	private const float Gravity = 9.8f;

	private BurningArrowState _state;

	private float _speedCache;

	[EditableScriptComponentVariable(true, "")]
	private float _speed = 10f;

	private Vec3 _speedVector = Vec3.Zero;

	private float _arrowMovementTimer;

	private SoundEvent _arrowSound;

	private MatrixFrame _initialFrameCacheForShootArrowButton;

	private MatrixFrame _initialGlobalFrameCacheForShootArrowButton;

	public SimpleButton ShootArrow;

	public SimpleButton StopMovement;

	public void StartMovement()
	{
		_initialFrameCacheForShootArrowButton = base.GameEntity.GetFrame();
		_initialGlobalFrameCacheForShootArrowButton = base.GameEntity.GetGlobalFrame();
		_state = BurningArrowState.StartMovement;
		_speedVector = _speed * _initialFrameCacheForShootArrowButton.rotation.u;
		_arrowSound = SoundEvent.CreateEventFromString("event:/mission/ambient/special/alert_arrow", base.Scene);
		_arrowSound.Play();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnInit()
	{
		base.OnInit();
		base.GameEntity.SetVisibilityExcludeParents(visible: false);
	}

	protected override void OnTick(float dt)
	{
		Tick(dt);
		if (!_speed.Equals(_speedCache))
		{
			_speedCache = _speed;
			_speedVector = _speed * _initialFrameCacheForShootArrowButton.rotation.u;
		}
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		Tick(dt);
		Vec3 startPosition;
		Vec3 speedVector;
		if (_state == BurningArrowState.None)
		{
			startPosition = base.GameEntity.GetGlobalFrame().origin;
			speedVector = _speed * base.GameEntity.GetGlobalFrame().rotation.u;
		}
		else
		{
			startPosition = _initialGlobalFrameCacheForShootArrowButton.origin;
			speedVector = _speed * _initialGlobalFrameCacheForShootArrowButton.rotation.u;
		}
		List<Vec3> list = new List<Vec3>();
		list.Add(startPosition);
		float num = 0f;
		float num2 = _speed * 100f / 15f;
		for (int i = 1; (float)i < num2; i++)
		{
			num += 0.03f;
			list.Add(GetPositionAtTime(in startPosition, in speedVector, num));
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (j != list.Count - 1)
			{
				_ = list[j];
				_ = list[j + 1];
			}
		}
	}

	private Vec3 GetPositionAtTime(in Vec3 startPosition, in Vec3 speedVector, float time)
	{
		Vec3 zero = Vec3.Zero;
		zero.x = startPosition.x + speedVector.x * time;
		zero.y = startPosition.y + speedVector.y * time;
		zero.z = startPosition.z + speedVector.z * time - 4.9f * time * time;
		return zero;
	}

	private void Tick(float dt)
	{
		if (_state != BurningArrowState.EndMovement)
		{
			if (_state == BurningArrowState.StartMovement)
			{
				base.GameEntity.SetVisibilityExcludeParents(visible: true);
				_state = BurningArrowState.MovementInProgress;
			}
			if (_state == BurningArrowState.MovementInProgress)
			{
				Move(dt);
			}
		}
	}

	private void Move(float dt)
	{
		if (_speed <= 0f || _arrowMovementTimer >= 4f)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: false);
			_state = BurningArrowState.EndMovement;
			_arrowSound.Stop();
			_arrowMovementTimer = 0f;
			return;
		}
		MatrixFrame frame = base.GameEntity.GetFrame();
		_speedVector.z -= 9.8f * dt;
		Vec3 origin = frame.origin + _speedVector * dt;
		LookAtWithZAsForward(ref frame, _speedVector.NormalizedCopy(), Vec3.Up);
		frame.origin = origin;
		base.GameEntity.SetFrame(ref frame);
		_arrowSound.SetPosition(base.GameEntity.GlobalPosition);
		_arrowMovementTimer += dt;
	}

	private void LookAtWithZAsForward(ref MatrixFrame frame, Vec3 direction, Vec3 upVector)
	{
		Vec3 vec = direction;
		Vec3 vb = upVector;
		vec.Normalize();
		Vec3 vec2 = Vec3.CrossProduct(vec, vb);
		vec2.Normalize();
		vb = Vec3.CrossProduct(vec2, vec);
		vb.Normalize();
		frame.rotation.s = vec2;
		frame.rotation.f = vb;
		frame.rotation.u = -vec;
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "ShootArrow")
		{
			if (_state != BurningArrowState.None)
			{
				_state = BurningArrowState.None;
				base.GameEntity.SetFrame(ref _initialFrameCacheForShootArrowButton);
			}
			StartMovement();
		}
		if (variableName == "StopMovement")
		{
			_state = BurningArrowState.None;
			base.GameEntity.SetFrame(ref _initialFrameCacheForShootArrowButton);
			_arrowMovementTimer = 0f;
		}
	}
}
