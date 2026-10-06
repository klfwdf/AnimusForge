using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.BoardGames.Pawns;

public class PawnPuluc : PawnBase
{
	public enum MovementState
	{
		MovingForward,
		MovingBackward,
		ChangingDirection
	}

	public MovementState State;

	public PawnPuluc CapturedBy;

	public Vec3 SpawnPos;

	public bool IsInSpawn = true;

	public bool IsTopPawn = true;

	private static float _height;

	private int _x;

	public float Height
	{
		get
		{
			if (_height == 0f)
			{
				_height = (base.Entity.GetBoundingBoxMax() - base.Entity.GetBoundingBoxMin()).z;
			}
			return _height;
		}
	}

	public override Vec3 PosBeforeMoving => PosBeforeMovingBase - new Vec3(0f, 0f, Height * (float)PawnsBelow.Count);

	public override bool IsPlaced
	{
		get
		{
			if (InPlay || IsInSpawn)
			{
				return IsTopPawn;
			}
			return false;
		}
	}

	public int X
	{
		get
		{
			return _x;
		}
		set
		{
			_x = value;
			if (value >= 0 && value < 11)
			{
				IsInSpawn = false;
			}
			else
			{
				IsInSpawn = true;
			}
		}
	}

	public List<PawnPuluc> PawnsBelow { get; }

	public bool InPlay
	{
		get
		{
			if (X >= 0)
			{
				return X < 11;
			}
			return false;
		}
	}

	public PawnPuluc(GameEntity entity, bool playerOne)
		: base(entity, playerOne)
	{
		PawnsBelow = new List<PawnPuluc>();
		SpawnPos = base.CurrentPos;
		X = -1;
	}

	public override void Reset()
	{
		base.Reset();
		X = -1;
		State = MovementState.MovingForward;
		IsTopPawn = true;
		IsInSpawn = true;
		CapturedBy = null;
		PawnsBelow.Clear();
	}

	public override void AddGoalPosition(Vec3 goal)
	{
		if (IsTopPawn)
		{
			goal.z += Height * (float)PawnsBelow.Count;
			int count = PawnsBelow.Count;
			for (int i = 0; i < count; i++)
			{
				PawnsBelow[i].AddGoalPosition(goal - new Vec3(0f, 0f, (float)(i + 1) * Height));
			}
		}
		base.GoalPositions.Add(goal);
	}

	public override void MovePawnToGoalPositions(bool instantMove, float speed, bool dragged = false)
	{
		if (base.GoalPositions.Count == 0)
		{
			return;
		}
		base.MovePawnToGoalPositions(instantMove, speed, dragged);
		if (!IsTopPawn)
		{
			return;
		}
		foreach (PawnPuluc item in PawnsBelow)
		{
			item.MovePawnToGoalPositions(instantMove, speed, dragged);
		}
	}

	public override void SetPawnAtPosition(Vec3 position)
	{
		base.SetPawnAtPosition(position);
		if (!IsTopPawn)
		{
			return;
		}
		int num = 1;
		foreach (PawnPuluc item in PawnsBelow)
		{
			item.SetPawnAtPosition(new Vec3(position.x, position.y, position.z - Height * (float)num));
			num++;
		}
	}

	public override void EnableCollisionBody()
	{
		base.EnableCollisionBody();
		foreach (PawnPuluc item in PawnsBelow)
		{
			item.Entity.BodyFlag &= ~BodyFlags.Disabled;
		}
	}

	public override void DisableCollisionBody()
	{
		base.DisableCollisionBody();
		foreach (PawnPuluc item in PawnsBelow)
		{
			item.Entity.BodyFlag |= BodyFlags.Disabled;
		}
	}

	public void MovePawnBackToSpawn(bool instantMove, float speed, bool fake = false)
	{
		X = -1;
		State = MovementState.MovingForward;
		IsTopPawn = true;
		IsInSpawn = true;
		base.Captured = false;
		CapturedBy = null;
		PawnsBelow.Clear();
		if (!fake)
		{
			AddGoalPosition(SpawnPos);
			MovePawnToGoalPositions(instantMove, speed);
		}
	}
}
