using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class ScenePropDecal : ScriptComponentBehavior
{
	public string DiffuseTexture;

	public string NormalTexture;

	public string SpecularTexture;

	public string MaskTexture;

	public bool UseBaseNormals;

	public float TilingSize = 1f;

	public float TilingOffset;

	public float AlphaTestValue;

	public float TextureSweepX;

	public float TextureSweepY;

	public string MaterialName = "deferred_decal_material";

	protected Material UniqueMaterial;

	protected internal override void OnInit()
	{
		base.OnInit();
		SetUpMaterial();
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		SetUpMaterial();
	}

	protected internal override void OnEditorVariableChanged(string variableName)
	{
		base.OnEditorVariableChanged(variableName);
		SetUpMaterial();
	}

	private void EnsureUniqueMaterial()
	{
		Material fromResource = Material.GetFromResource(MaterialName);
		UniqueMaterial = fromResource.CreateCopy();
	}

	private void SetUpMaterial()
	{
		EnsureUniqueMaterial();
		Texture texture = Texture.CheckAndGetFromResource(DiffuseTexture);
		Texture texture2 = Texture.CheckAndGetFromResource(NormalTexture);
		Texture texture3 = Texture.CheckAndGetFromResource(SpecularTexture);
		Texture texture4 = Texture.CheckAndGetFromResource(MaskTexture);
		if (texture != null)
		{
			UniqueMaterial.SetTexture(Material.MBTextureType.DiffuseMap, texture);
		}
		if (texture2 != null)
		{
			UniqueMaterial.SetTexture(Material.MBTextureType.BumpMap, texture2);
		}
		if (texture3 != null)
		{
			UniqueMaterial.SetTexture(Material.MBTextureType.SpecularMap, texture3);
		}
		if (texture4 != null)
		{
			UniqueMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, texture4);
			UniqueMaterial.AddMaterialShaderFlag("use_areamap", showErrors: false);
		}
		UniqueMaterial.SetAlphaTestValue(AlphaTestValue);
		base.GameEntity.SetMaterialForAllMeshes(UniqueMaterial);
		Mesh firstMesh = base.GameEntity.GetFirstMesh();
		if (firstMesh != null)
		{
			if (UniqueMaterial != null)
			{
				firstMesh.SetVectorArgument(TilingSize, TilingSize, TilingOffset, TilingOffset);
			}
			firstMesh.SetVectorArgument2(TextureSweepX, TextureSweepY, 0f, UseBaseNormals ? 1f : 0f);
		}
	}
}
