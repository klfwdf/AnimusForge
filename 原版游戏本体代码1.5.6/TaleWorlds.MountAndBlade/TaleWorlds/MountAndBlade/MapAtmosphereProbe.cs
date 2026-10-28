using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MapAtmosphereProbe : ScriptComponentBehavior
{
	public bool visualizeRadius = true;

	public bool hideAllProbes = true;

	public static bool hideAllProbesStatic = true;

	public float minRadius = 1f;

	public float maxRadius = 2f;

	public float rainDensity;

	public float temperature;

	public string atmosphereType;

	public string colorGrade;

	private MetaMesh innerSphereMesh;

	private MetaMesh outerSphereMesh;

	public float GetInfluenceAmount(Vec3 worldPosition)
	{
		return MBMath.SmoothStep(minRadius, maxRadius, worldPosition.Distance(base.GameEntity.GetGlobalFrame().origin));
	}

	public MapAtmosphereProbe()
	{
		hideAllProbes = hideAllProbesStatic;
		if (MBEditor.IsEditModeOn)
		{
			innerSphereMesh = MetaMesh.GetCopy("physics_sphere_detailed");
			outerSphereMesh = MetaMesh.GetCopy("physics_sphere_detailed");
			innerSphereMesh.SetMaterial(Material.GetFromResource("light_radius_visualizer"));
			outerSphereMesh.SetMaterial(Material.GetFromResource("light_radius_visualizer"));
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (visualizeRadius && !hideAllProbesStatic)
		{
			uint num = 16711680u;
			uint num2 = 720640u;
			if (MBEditor.IsEntitySelected(base.GameEntity))
			{
				num |= 0x80000000u;
				num2 |= 0x80000000u;
			}
			else
			{
				num |= 0x40000000;
				num2 |= 0x40000000;
			}
			innerSphereMesh.SetFactor1(num);
			outerSphereMesh.SetFactor1(num2);
			MatrixFrame frame = default(MatrixFrame);
			frame.origin = base.GameEntity.GetGlobalFrame().origin;
			frame.rotation = Mat3.Identity;
			frame.rotation.ApplyScaleLocal(minRadius);
			MatrixFrame frame2 = default(MatrixFrame);
			frame2.origin = base.GameEntity.GetGlobalFrame().origin;
			frame2.rotation = Mat3.Identity;
			frame2.rotation.ApplyScaleLocal(maxRadius);
			innerSphereMesh.SetVectorArgument(minRadius, maxRadius, 0f, 0f);
			outerSphereMesh.SetVectorArgument(minRadius, maxRadius, 0f, 0f);
			MBEditor.RenderEditorMesh(innerSphereMesh, frame);
			MBEditor.RenderEditorMesh(outerSphereMesh, frame2);
		}
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == "minRadius")
		{
			minRadius = MBMath.ClampFloat(minRadius, 0.1f, maxRadius);
		}
		if (variableName == "maxRadius")
		{
			maxRadius = MBMath.ClampFloat(maxRadius, minRadius, float.MaxValue);
		}
		if (variableName == "hideAllProbes")
		{
			hideAllProbesStatic = hideAllProbes;
		}
	}
}
