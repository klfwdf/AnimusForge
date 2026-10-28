using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.BoardGames.Pawns;

public abstract class PawnBase
{
	public Action<PawnBase, Vec3, Vec3> OnArrivedIntermediateGoalPosition;

	public Action<PawnBase, Vec3, Vec3> OnArrivedFinalGoalPosition;

	protected Vec3 PosBeforeMovingBase;

	private int _currentGoalPos;

	private float _dtCounter;

	private float _movePauseDuration;

	private float _movePauseTimer;

	private float _moveSpeed;

	private bool _moveTiming;

	private bool _dragged;

	private bool _freePathToDestination;

	public static int PawnMoveSoundCodeID { get; set; }

	public static int PawnSelectSoundCodeID { get; set; }

	public static int PawnTapSoundCodeID { get; set; }

	public static int PawnRemoveSoundCodeID { get; set; }

	public abstract bool IsPlaced { get; }

	public virtual Vec3 PosBeforeMoving
	{
		get
		{
			return PosBeforeMovingBase;
		}
		protected set
		{
			PosBeforeMovingBase = value;
		}
	}

	public GameEntity Entity { get; }

	protected List<Vec3> GoalPositions { get; }

	protected Vec3 CurrentPos { get; private set; }

	public bool Captured { get; set; }

	public bool MovingToDifferentTile { get; set; }

	public bool Moving { get; private set; }

	public bool PlayerOne { get; private set; }

	public bool HasAnyGoalPosition
	{
		get
		{
			bool result = false;
			if (GoalPositions != null)
			{
				result = !GoalPositions.IsEmpty();
			}
			return result;
		}
	}

	protected PawnBase(GameEntity entity, bool playerOne)
	{
		Entity = entity;
		PlayerOne = playerOne;
		CurrentPos = Entity.GetGlobalFrame().origin;
		PosBeforeMoving = CurrentPos;
		Moving = false;
		_dragged = false;
		Captured = false;
		_movePauseDuration = 0.3f;
		entity.CreateVariableRatePhysics(forChildren: true);
		GoalPositions = new List<Vec3>();
	}

	public virtual void Reset()
	{
		ClearGoalPositions();
		Moving = false;
		MovingToDifferentTile = false;
		_movePauseDuration = 0.3f;
		_movePauseTimer = 0f;
		_moveTiming = false;
		_dragged = false;
		Captured = false;
	}

	public virtual void AddGoalPosition(Vec3 goal)
	{
		GoalPositions.Add(goal);
	}

	public virtual void SetPawnAtPosition(Vec3 position)
	{
		MatrixFrame frame = Entity.GetGlobalFrame();
		frame.origin = position;
		Entity.SetGlobalFrame(in frame);
	}

	public virtual void MovePawnToGoalPositions(bool instantMove, float speed, bool dragged = false)
	{
		PosBeforeMoving = Entity.GlobalPosition;
		_moveSpeed = speed;
		_currentGoalPos = 0;
		_movePauseTimer = 0f;
		_dtCounter = 0f;
		_moveTiming = false;
		_dragged = dragged;
		if (GoalPositions.Count == 1 && PosBeforeMoving.Equals(GoalPositions[0]))
		{
			instantMove = true;
		}
		if (instantMove)
		{
			MatrixFrame frame = Entity.GetGlobalFrame();
			frame.origin = GoalPositions[GoalPositions.Count - 1];
			Entity.SetGlobalFrame(in frame);
			ClearGoalPositions();
		}
		else
		{
			Moving = true;
		}
	}

	public virtual void EnableCollisionBody()
	{
		Entity.BodyFlag &= ~BodyFlags.Disabled;
	}

	public virtual void DisableCollisionBody()
	{
		Entity.BodyFlag |= BodyFlags.Disabled;
	}

	public void Tick(float dt)
	{
		if (_moveTiming)
		{
			_movePauseTimer += dt;
			if (_movePauseTimer >= _movePauseDuration)
			{
				_moveTiming = false;
				_movePauseTimer = 0f;
			}
		}
		else
		{
			if (!Moving || !(dt > 0f))
			{
				return;
			}
			Vec3 vec = new Vec3(0f, 0f, 0f, -1f);
			Vec3 vec2 = GoalPositions[_currentGoalPos] - PosBeforeMoving;
			float num = vec2.Normalize();
			float num2 = num / _moveSpeed;
			float num3 = _dtCounter / num2;
			if (_dtCounter.Equals(0f))
			{
				float x = (Entity.GlobalBoxMax - Entity.GlobalBoxMin).x;
				float z = (Entity.GlobalBoxMax - Entity.GlobalBoxMin).z;
				Vec3 vec3 = new Vec3(0f, 0f, z / 2f);
				Vec3 sourcePoint = Entity.GetGlobalFrame().origin + vec3 + vec2 * (x / 1.8f);
				Vec3 targetPoint = GoalPositions[_currentGoalPos] + vec3;
				if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(sourcePoint, targetPoint, out var collisionDistance, 0.001f, BodyFlags.None))
				{
					_freePathToDestination = false;
					num = collisionDistance;
				}
				else
				{
					_freePathToDestination = true;
					if (!_dragged)
					{
						PlayPawnMoveSound();
					}
					else
					{
						PlayPawnTapSound();
					}
				}
			}
			if (!_freePathToDestination)
			{
				float num4 = TaleWorlds.Library.MathF.Sin(num3 * System.MathF.PI);
				float num5 = num / 6f;
				num4 *= num5;
				vec += new Vec3(0f, 0f, num4);
			}
			_ = _dtCounter;
			_dtCounter += dt;
			if (num3 >= 1f)
			{
				_dtCounter = 0f;
				CurrentPos = GoalPositions[_currentGoalPos];
				vec = Vec3.Zero;
				if (!_freePathToDestination && IsPlaced)
				{
					PlayPawnTapSound();
				}
				else if (!IsPlaced)
				{
					PlayPawnRemovedTapSound();
				}
				Vec3 vec4 = GoalPositions[_currentGoalPos];
				bool flag = true;
				while (_currentGoalPos < GoalPositions.Count - 1)
				{
					_currentGoalPos++;
					Vec3 vec5 = GoalPositions[_currentGoalPos];
					if ((vec4 - vec5).LengthSquared > 0f)
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					OnArrivedFinalGoalPosition?.Invoke(this, PosBeforeMoving, CurrentPos);
					Moving = false;
					ClearGoalPositions();
				}
				else
				{
					OnArrivedIntermediateGoalPosition?.Invoke(this, PosBeforeMoving, CurrentPos);
					_movePauseDuration = 0.3f;
					_moveTiming = true;
				}
				PosBeforeMoving = CurrentPos;
			}
			else
			{
				Moving = true;
				CurrentPos = MBMath.Lerp(PosBeforeMoving, GoalPositions[_currentGoalPos], num3, 0.005f);
			}
			MatrixFrame globalFrame = Entity.GetGlobalFrame();
			MatrixFrame frame = new MatrixFrame(in globalFrame.rotation, CurrentPos + vec);
			Entity.SetGlobalFrame(in frame);
		}
	}

	public void MovePawnToGoalPositionsDelayed(bool instantMove, float speed, bool dragged, float delay)
	{
		if (GoalPositions.Count > 0)
		{
			if (GoalPositions.Count == 1 && PosBeforeMoving.Equals(GoalPositions[0]))
			{
				ClearGoalPositions();
				return;
			}
			MovePawnToGoalPositions(instantMove, speed, dragged);
			_movePauseDuration = delay;
			_moveTiming = delay > 0f;
		}
	}

	public void SetPlayerOne(bool playerOne)
	{
		PlayerOne = playerOne;
	}

	public void ClearGoalPositions()
	{
		MovingToDifferentTile = false;
		GoalPositions.Clear();
	}

	public void UpdatePawnPosition()
	{
		PosBeforeMoving = Entity.GlobalPosition;
	}

	public void PlayPawnSelectSound()
	{
		Mission.Current.MakeSound(PawnSelectSoundCodeID, CurrentPos, soundCanBePredicted: true, isReliable: false, -1, -1);
	}

	private void PlayPawnTapSound()
	{
		Mission.Current.MakeSound(PawnTapSoundCodeID, CurrentPos, soundCanBePredicted: true, isReliable: false, -1, -1);
	}

	private void PlayPawnRemovedTapSound()
	{
		Mission.Current.MakeSound(PawnRemoveSoundCodeID, CurrentPos, soundCanBePredicted: true, isReliable: false, -1, -1);
	}

	private void PlayPawnMoveSound()
	{
		Mission.Current.MakeSound(PawnMoveSoundCodeID, CurrentPos, soundCanBePredicted: true, isReliable: false, -1, -1);
	}
}
