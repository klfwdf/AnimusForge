using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class RoadStart : ScriptComponentBehavior
{
	public float textureSweepX;

	public float textureSweepY;

	public string materialName = "blood_decal_terrain_material";

	private GameEntity pathEntity;

	private MetaMesh pathMesh;

	protected internal override void OnInit()
	{
		pathEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(base.Scene, isModifiableFromEditor: false);
		pathEntity.Name = "Road_Entity";
		UpdatePathMesh();
	}

	protected internal override void OnEditorInit()
	{
		OnInit();
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		if (pathEntity != null)
		{
			pathEntity.Remove(removeReason);
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		if (base.Scene.IsEntityFrameChanged(base.GameEntity.Name))
		{
			UpdatePathMesh();
		}
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		if (variableName == MBGlobals.GetMemberName(() => materialName))
		{
			UpdatePathMesh();
		}
		if (pathMesh != null)
		{
			pathMesh.SetVectorArgument2(textureSweepX, textureSweepY, 0f, 0f);
		}
	}

	private void UpdatePathMesh()
	{
		pathEntity.ClearComponents();
		pathMesh = MetaMesh.CreateMetaMesh();
		Material fromResource = Material.GetFromResource(materialName);
		if (fromResource != null)
		{
			pathMesh.SetMaterial(fromResource);
		}
		else
		{
			pathMesh.SetMaterial(Material.GetDefaultMaterial());
		}
		pathEntity.AddMultiMesh(pathMesh);
		pathMesh.SetVectorArgument2(textureSweepX, textureSweepY, 0f, 0f);
	}

	protected internal override bool MovesEntity()
	{
		return false;
	}
}
