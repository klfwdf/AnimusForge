using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class ScenePropNegativeLight : ScriptComponentBehavior
{
	public float Flatness_X;

	public float Flatness_Y;

	public float Flatness_Z;

	public float Alpha = 1f;

	public bool Is_Dark_Light = true;

	protected internal override void OnEditorTick(float dt)
	{
		SetMeshParameters();
	}

	private void SetMeshParameters()
	{
		MetaMesh metaMesh = base.GameEntity.GetMetaMesh(0);
		if (metaMesh != null)
		{
			metaMesh.SetVectorArgument(Flatness_X, Flatness_Y, Flatness_Z, Alpha);
			if (Is_Dark_Light)
			{
				metaMesh.SetVectorArgument2(1f, 0f, 0f, 0f);
			}
			else
			{
				metaMesh.SetVectorArgument2(0f, 0f, 0f, 0f);
			}
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetMeshParameters();
	}

	protected internal override bool IsOnlyVisual()
	{
		return true;
	}
}
