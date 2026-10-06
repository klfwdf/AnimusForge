using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class ScenePropPositiveLight : ScriptComponentBehavior
{
	public float Flatness_X;

	public float Flatness_Y;

	public float Flatness_Z;

	public float DirectLightRed = 1f;

	public float DirectLightGreen = 1f;

	public float DirectLightBlue = 1f;

	public float DirectLightIntensity = 1f;

	public float AmbientLightRed;

	public float AmbientLightGreen;

	public float AmbientLightBlue = 1f;

	public float AmbientLightIntensity = 1f;

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		SetMeshParams();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		SetMeshParams();
	}

	private void SetMeshParams()
	{
		MetaMesh metaMesh = base.GameEntity.GetMetaMesh(0);
		if (metaMesh != null)
		{
			uint factor1Linear = CalculateFactor(new Vec3(DirectLightRed, DirectLightGreen, DirectLightBlue), DirectLightIntensity);
			metaMesh.SetFactor1Linear(factor1Linear);
			uint factor2Linear = CalculateFactor(new Vec3(AmbientLightRed, AmbientLightGreen, AmbientLightBlue), AmbientLightIntensity);
			metaMesh.SetFactor2Linear(factor2Linear);
			metaMesh.SetVectorArgument(Flatness_X, Flatness_Y, Flatness_Z, 1f);
		}
	}

	private uint CalculateFactor(Vec3 color, float alpha)
	{
		float num = 10f;
		byte b = byte.MaxValue;
		alpha = MathF.Min(MathF.Max(0f, alpha), num);
		return ((uint)(alpha / num * (float)(int)b) << 24) + ((uint)(color.x * (float)(int)b) << 16) + ((uint)(color.y * (float)(int)b) << 8) + (uint)(color.z * (float)(int)b);
	}

	protected internal override bool IsOnlyVisual()
	{
		return true;
	}
}
