using System;
using System.Collections.Generic;

namespace TaleWorlds.ScreenSystem;

public class GlobalLayer : IComparable
{
	public ScreenLayer Layer { get; protected set; }

	internal void EarlyTick(float dt)
	{
		OnEarlyTick(dt);
	}

	internal void Tick(float dt)
	{
		OnTick(dt);
	}

	internal void LateTick(float dt)
	{
		OnLateTick(dt);
	}

	protected virtual void OnEarlyTick(float dt)
	{
	}

	protected virtual void OnTick(float dt)
	{
	}

	protected virtual void OnLateTick(float dt)
	{
	}

	internal void Update(IReadOnlyList<int> lastKeysPressed)
	{
		Layer.Update(lastKeysPressed);
	}

	public int CompareTo(object obj)
	{
		if (!(obj is GlobalLayer globalLayer))
		{
			return -1;
		}
		return Layer.CompareTo(globalLayer.Layer);
	}

	public virtual void UpdateLayout()
	{
		Layer.UpdateLayout();
	}
}
