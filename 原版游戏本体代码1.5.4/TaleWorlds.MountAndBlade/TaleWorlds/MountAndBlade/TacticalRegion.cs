using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class TacticalRegion : MissionObject
{
	public enum TacticalRegionTypeEnum
	{
		Forest,
		DifficultTerrain,
		Opening
	}

	public bool IsEditorDebugRingVisible;

	private WorldPosition _position;

	public float radius = 1f;

	private List<TacticalPosition> _linkedTacticalPositions;

	public TacticalRegionTypeEnum tacticalRegionType;

	public WorldPosition Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
		}
	}

	public List<TacticalPosition> LinkedTacticalPositions
	{
		get
		{
			return _linkedTacticalPositions;
		}
		set
		{
			_linkedTacticalPositions = value;
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_position = new WorldPosition(base.GameEntity.GetScenePointer(), base.GameEntity.GlobalPosition);
	}

	public override void AfterMissionStart()
	{
		base.AfterMissionStart();
		_linkedTacticalPositions = new List<TacticalPosition>();
		_linkedTacticalPositions = (from c in base.GameEntity.GetChildren()
			where c.HasScriptOfType<TacticalPosition>()
			select c.GetFirstScriptOfType<TacticalPosition>()).ToList();
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		_position = new WorldPosition(base.GameEntity.GetScenePointer(), UIntPtr.Zero, base.GameEntity.GlobalPosition, hasValidZ: false);
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		_position.SetVec3(UIntPtr.Zero, base.GameEntity.GlobalPosition, hasValidZ: false);
		if (IsEditorDebugRingVisible)
		{
			MBEditor.HelpersEnabled();
		}
	}
}
