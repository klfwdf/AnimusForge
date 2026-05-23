using System;
using System.Diagnostics;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class MissionEntitySelectionUIHandler : MissionView
{
	private Action<WeakGameEntity> onSelect;

	private Action<WeakGameEntity> onHover;

	public MissionEntitySelectionUIHandler(Action<WeakGameEntity> onSelect = null, Action<WeakGameEntity> onHover = null)
	{
		this.onSelect = onSelect;
		this.onHover = onHover;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		WeakGameEntity value = new Lazy<WeakGameEntity>(GetCollidedEntity).Value;
		onHover?.Invoke(value);
		if (base.Input.IsKeyReleased(InputKey.LeftMouseButton))
		{
			onSelect?.Invoke(value);
		}
	}

	private WeakGameEntity GetCollidedEntity()
	{
		Vec2 mousePositionRanged = base.Input.GetMousePositionRanged();
		base.MissionScreen.ScreenPointToWorldRay(mousePositionRanged, out var rayBegin, out var rayEnd);
		WeakGameEntity result;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			if (Mission.Current != null)
			{
				Mission.Current.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float _, out WeakGameEntity collidedEntity, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags);
				while (collidedEntity.IsValid)
				{
					result = collidedEntity.Parent;
					if (!result.IsValid)
					{
						break;
					}
					collidedEntity = collidedEntity.Parent;
				}
				result = collidedEntity;
			}
			else
			{
				result = WeakGameEntity.Invalid;
			}
		}
		return result;
	}

	public override void OnRemoveBehavior()
	{
		onSelect = null;
		onHover = null;
		base.OnRemoveBehavior();
	}

	[Conditional("DEBUG")]
	public void TickDebug()
	{
		WeakGameEntity collidedEntity = GetCollidedEntity();
		if (collidedEntity.IsValid)
		{
			_ = collidedEntity.Name;
		}
	}
}
