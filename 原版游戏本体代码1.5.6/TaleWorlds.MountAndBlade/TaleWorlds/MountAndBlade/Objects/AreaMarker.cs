using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects;

public class AreaMarker : MissionObject, ITrackableBase
{
	public float AreaRadius = 3f;

	public int AreaIndex;

	public bool CheckToggle;

	public virtual string Tag => "area_marker_" + AreaIndex;

	protected internal override void OnEditorTick(float dt)
	{
		if (CheckToggle)
		{
			MBEditor.HelpersEnabled();
		}
	}

	protected internal override void OnEditorInit()
	{
		CheckToggle = false;
	}

	public bool IsPositionInRange(Vec3 position)
	{
		return position.DistanceSquared(base.GameEntity.GlobalPosition) <= AreaRadius * AreaRadius;
	}

	public virtual List<UsableMachine> GetUsableMachinesInRange(string excludeTag = null)
	{
		float distanceSquared = AreaRadius * AreaRadius;
		return (from x in Mission.Current.ActiveMissionObjects.FindAllWithType<UsableMachine>()
			where !x.IsDeactivated && x.GameEntity.GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition) <= distanceSquared && !x.GameEntity.HasTag(excludeTag)
			select x).ToList();
	}

	public virtual List<UsableMachine> GetUsableMachinesWithTagInRange(string tag)
	{
		float distanceSquared = AreaRadius * AreaRadius;
		return (from x in Mission.Current.ActiveMissionObjects.FindAllWithType<UsableMachine>()
			where x.GameEntity.GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition) <= distanceSquared && x.GameEntity.HasTag(tag)
			select x).ToList();
	}

	public virtual List<GameEntity> GetGameEntitiesWithTagInRange(string tag)
	{
		float distanceSquared = AreaRadius * AreaRadius;
		return (from x in Mission.Current.Scene.FindEntitiesWithTag(tag)
			where x.GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition) <= distanceSquared && x.HasTag(tag)
			select x).ToList();
	}

	public virtual TextObject GetName()
	{
		return new TextObject(base.GameEntity.Name);
	}

	public virtual Vec3 GetPosition()
	{
		return base.GameEntity.GlobalPosition;
	}

	TextObject ITrackableBase.GetName()
	{
		return GetName();
	}

	Vec3 ITrackableBase.GetPosition()
	{
		return GetPosition();
	}
}
