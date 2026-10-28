using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

public class VisibilityMask : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "Distance")]
	public float distance = 50f;

	private bool currentlyVisible = true;

	private void UpdateVisibility()
	{
		if (distance <= 0f)
		{
			if (!currentlyVisible)
			{
				base.GameEntity.SetVisibilityExcludeParents(visible: true);
				currentlyVisible = true;
			}
			return;
		}
		Vec3 lastFinalRenderCameraPositionOfScene = base.GameEntity.GetLastFinalRenderCameraPositionOfScene();
		bool flag = base.GameEntity.GetGlobalFrame().origin.DistanceSquared(lastFinalRenderCameraPositionOfScene) <= distance * distance;
		if (flag != currentlyVisible)
		{
			base.GameEntity.SetVisibilityExcludeParents(flag);
			currentlyVisible = flag;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		UpdateVisibility();
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		UpdateVisibility();
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "distance")
		{
			distance = MathF.Max(0f, distance);
		}
	}
}
