using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "rope_segment_cosmetics")]
internal class RopeSegmentCosmetics : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "Normalized Location wrt Rope")]
	private float _ropeLocalPosition = 0.5f;

	public bool IsBurningNode { get; private set; }

	public float RopeLocalPosition
	{
		get
		{
			return _ropeLocalPosition;
		}
		set
		{
			_ropeLocalPosition = value;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		FetchEntities();
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		FetchEntities();
	}

	protected override void OnEditorTick(float dt)
	{
		FetchEntities();
	}

	private void FetchEntities()
	{
		IsBurningNode = base.GameEntity.HasTag("burning_node");
	}
}
