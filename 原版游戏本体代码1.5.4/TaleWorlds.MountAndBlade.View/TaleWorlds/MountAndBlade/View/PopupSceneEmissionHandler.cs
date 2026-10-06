using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View;

public class PopupSceneEmissionHandler : ScriptComponentBehavior
{
	public float startTime;

	public float transitionTime;

	private float timeElapsed;

	protected override void OnInit()
	{
		base.OnInit();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		timeElapsed += dt;
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			Mesh firstMesh = child.GetFirstMesh();
			if (firstMesh != null)
			{
				firstMesh.SetVectorArgument(1f, 0.5f, 1f, MBMath.SmoothStep(startTime, startTime + transitionTime, timeElapsed) * 10f);
			}
		}
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		OnTick(dt);
	}
}
