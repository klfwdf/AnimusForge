using System;
using System.Diagnostics;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class OrderComponent
{
	private readonly Timer _tickTimer;

	protected Func<Formation, Vec3> Position;

	protected Func<Formation, Vec2> Direction;

	private Vec2 _previousDirection = Vec2.Invalid;

	public abstract OrderType OrderType { get; }

	protected internal virtual bool CanStack => false;

	protected internal virtual bool CancelsPreviousDirectionOrder => false;

	protected internal virtual bool CancelsPreviousArrangementOrder => false;

	public Vec2 GetDirection(Formation f)
	{
		Vec2 vec = Direction(f);
		if (f.IsAIControlled && vec.DotProduct(_previousDirection) > 0.87f)
		{
			vec = _previousDirection;
		}
		else
		{
			_previousDirection = vec;
		}
		return vec;
	}

	protected void CopyPositionAndDirectionFrom(OrderComponent order)
	{
		Position = order.Position;
		Direction = order.Direction;
	}

	protected OrderComponent(float tickTimerDuration = 0.5f)
	{
		_tickTimer = new Timer(Mission.Current.CurrentTime, tickTimerDuration);
	}

	internal bool Tick(Formation formation)
	{
		bool num = _tickTimer.Check(Mission.Current.CurrentTime);
		if (num)
		{
			TickOccasionally(formation, _tickTimer.PreviousDeltaTime);
		}
		return num;
	}

	[Conditional("DEBUG")]
	protected virtual void TickDebug(Formation formation)
	{
	}

	protected internal virtual void TickOccasionally(Formation formation, float dt)
	{
	}

	protected internal virtual void OnApply(Formation formation)
	{
	}

	protected internal virtual void OnCancel(Formation formation)
	{
	}

	protected internal virtual void OnUnitJoinOrLeave(Agent unit, bool isJoining)
	{
	}

	protected internal virtual bool IsApplicable(Formation formation)
	{
		return true;
	}

	protected internal virtual MovementOrder GetSubstituteOrder(Formation formation)
	{
		return MovementOrder.MovementOrderCharge;
	}

	protected internal virtual void OnArrangementChanged(Formation formation)
	{
	}
}
