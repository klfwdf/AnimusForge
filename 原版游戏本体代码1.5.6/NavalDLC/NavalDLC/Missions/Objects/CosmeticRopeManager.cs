using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "")]
public class CosmeticRopeManager : ScriptComponentBehavior
{
	private const string RopeScriptEntityTag = "simple_rope_start";

	private const float InvisibleDistanceSquared = 10000f;

	private const float LinearDistanceSquared = 2025f;

	private List<RopeSegment> _cosmeticsRopeSegments = new List<RopeSegment>();

	private bool _ropesWereInvisibleLastFrame;

	private bool _ropesWereLinearLastFrame;

	private bool _lodCheckFirstFrame = true;

	protected override void OnEditorInit()
	{
		FetchEntities();
	}

	protected override void OnInit()
	{
		FetchEntities();
	}

	protected override void OnEditorTick(float dt)
	{
		FetchEntities();
		HandleLOD();
	}

	protected override void OnTickParallel(float dt)
	{
		HandleLOD();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel;
	}

	private void FetchEntities()
	{
		if (!base.GameEntity.IsInEditorScene())
		{
			base.GameEntity.SetEntityFlags(base.GameEntity.EntityFlags | EntityFlags.DoNotTick);
		}
		_cosmeticsRopeSegments.Clear();
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			WeakGameEntity firstChildEntityWithTag = child.GetFirstChildEntityWithTag("simple_rope_start");
			if (firstChildEntityWithTag.IsValid)
			{
				RopeSegment firstScriptOfType = firstChildEntityWithTag.GetFirstScriptOfType<RopeSegment>();
				if (firstScriptOfType != null)
				{
					_cosmeticsRopeSegments.Add(firstScriptOfType);
				}
			}
		}
	}

	private void HandleLOD()
	{
		Vec3 lastFinalRenderCameraPositionOfScene = base.GameEntity.GetLastFinalRenderCameraPositionOfScene();
		Vec3 origin = base.GameEntity.GetGlobalFrame().origin;
		float num = lastFinalRenderCameraPositionOfScene.DistanceSquared(origin);
		bool flag = num > 10000f;
		bool flag2 = num > 2025f;
		if (_ropesWereInvisibleLastFrame != flag || _lodCheckFirstFrame)
		{
			base.GameEntity.SetVisibilityExcludeParents(!flag);
		}
		if (_ropesWereLinearLastFrame != flag2 || _lodCheckFirstFrame)
		{
			foreach (RopeSegment cosmeticsRopeSegment in _cosmeticsRopeSegments)
			{
				cosmeticsRopeSegment.SetLinearMode(flag2);
			}
		}
		_ropesWereInvisibleLastFrame = flag;
		_ropesWereLinearLastFrame = flag2;
		_lodCheckFirstFrame = false;
	}
}
