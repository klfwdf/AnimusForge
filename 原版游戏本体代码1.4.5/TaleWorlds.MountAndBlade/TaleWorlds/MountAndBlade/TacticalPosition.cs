using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticalPosition : MissionObject
{
	public enum TacticalPositionTypeEnum
	{
		Regional,
		HighGround,
		ChokePoint,
		Cliff,
		SpecialMissionPosition
	}

	private WorldPosition _position;

	private Vec2 _direction;

	private float _oldWidth;

	[EditableScriptComponentVariable(true, "")]
	private float _width;

	[EditableScriptComponentVariable(true, "")]
	private float _slope;

	[EditableScriptComponentVariable(true, "")]
	private bool _isInsurmountable;

	[EditableScriptComponentVariable(true, "")]
	private bool _isOuterEdge;

	private List<TacticalPosition> _linkedTacticalPositions;

	[EditableScriptComponentVariable(true, "")]
	private TacticalPositionTypeEnum _tacticalPositionType;

	[EditableScriptComponentVariable(true, "")]
	private TacticalRegion.TacticalRegionTypeEnum _tacticalRegionMembership;

	[EditableScriptComponentVariable(true, "")]
	private FormationAI.BehaviorSide _tacticalPositionSide = FormationAI.BehaviorSide.BehaviorSideNotSet;

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

	public Vec2 Direction
	{
		get
		{
			return _direction;
		}
		set
		{
			_direction = value;
		}
	}

	public float Width => _width;

	public float Slope => _slope;

	public bool IsInsurmountable => _isInsurmountable;

	public bool IsOuterEdge => _isOuterEdge;

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

	public TacticalPositionTypeEnum TacticalPositionType => _tacticalPositionType;

	public TacticalRegion.TacticalRegionTypeEnum TacticalRegionMembership => _tacticalRegionMembership;

	public FormationAI.BehaviorSide TacticalPositionSide => _tacticalPositionSide;

	public TacticalPosition()
	{
		_width = 1f;
		_slope = 0f;
	}

	public TacticalPosition(WorldPosition position, Vec2 direction, float width, float slope = 0f, bool isInsurmountable = false, TacticalPositionTypeEnum tacticalPositionType = TacticalPositionTypeEnum.Regional, TacticalRegion.TacticalRegionTypeEnum tacticalRegionMembership = TacticalRegion.TacticalRegionTypeEnum.Opening)
	{
		_position = position;
		_direction = direction;
		_width = width;
		_slope = slope;
		_isInsurmountable = isInsurmountable;
		_tacticalPositionType = tacticalPositionType;
		_tacticalRegionMembership = tacticalRegionMembership;
		_tacticalPositionSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
		_isOuterEdge = false;
		_linkedTacticalPositions = new List<TacticalPosition>();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_position = new WorldPosition(base.GameEntity.GetScenePointer(), base.GameEntity.GlobalPosition);
		_direction = base.GameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
	}

	public override void AfterMissionStart()
	{
		base.AfterMissionStart();
		_linkedTacticalPositions = (from c in base.GameEntity.GetChildren()
			where c.HasScriptOfType<TacticalPosition>()
			select c.GetFirstScriptOfType<TacticalPosition>()).ToList();
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		ApplyChangesFromEditor();
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		ApplyChangesFromEditor();
	}

	private void ApplyChangesFromEditor()
	{
		MatrixFrame frame = base.GameEntity.GetGlobalFrame();
		if (_width > 0f && _width != _oldWidth)
		{
			frame.rotation.MakeUnit();
			frame.rotation.ApplyScaleLocal(new Vec3(_width, 1f, 1f));
			base.GameEntity.SetGlobalFrame(in frame);
			base.GameEntity.UpdateTriadFrameForEditorForAllChildren();
			_oldWidth = _width;
		}
		_direction = frame.rotation.f.AsVec2.Normalized();
	}

	public void SetWidth(float width)
	{
		_width = width;
	}
}
