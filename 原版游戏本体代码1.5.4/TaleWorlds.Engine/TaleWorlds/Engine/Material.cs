using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

public sealed class Material : Resource
{
	public enum MBTextureType
	{
		DiffuseMap,
		DiffuseMap2,
		BumpMap,
		EnvironmentMap,
		SpecularMap
	}

	[EngineStruct("rglAlpha_blend_mode", true, "rgl_abm", false)]
	public enum MBAlphaBlendMode : byte
	{
		NoAlphaBlend,
		Modulate,
		AddAlpha,
		Multiply,
		Add,
		Max,
		Factor,
		AddModulateCombined,
		NoAlphaBlendNoWrite,
		ModulateNoWrite,
		GbufferAlphaBlend,
		GbufferAlphaBlendWithVtResolve,
		NoAlphaBlendNoAlphaWrite,
		Total
	}

	[Flags]
	private enum MBMaterialShaderFlags
	{
		UseSpecular = 1,
		UseSpecularMap = 2,
		UseHemisphericalAmbient = 4,
		UseEnvironmentMap = 8,
		UseDXT5Normal = 0x10,
		UseDynamicLight = 0x20,
		UseSunLight = 0x40,
		UseSpecularAlpha = 0x80,
		UseFresnel = 0x100,
		SunShadowReceiver = 0x200,
		DynamicShadowReceiver = 0x400,
		UseDiffuseAlphaMap = 0x800,
		UseParallaxMapping = 0x1000,
		UseParallaxOcclusion = 0x2000,
		UseAlphaTestingBit0 = 0x4000,
		UseAlphaTestingBit1 = 0x8000,
		UseAreaMap = 0x10000,
		UseDetailNormalMap = 0x20000,
		UseGroundSlopeAlpha = 0x40000,
		UseSelfIllumination = 0x80000,
		UseColorMapping = 0x100000,
		UseCubicAmbient = 0x200000
	}

	public string Name
	{
		get
		{
			return EngineApplicationInterface.IMaterial.GetName(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IMaterial.SetName(base.Pointer, value);
		}
	}

	public bool UsingSpecular
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseSpecular);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseSpecular, value);
		}
	}

	public bool UsingSpecularMap
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseSpecularMap);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseSpecularMap, value);
		}
	}

	public bool UsingEnvironmentMap
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseEnvironmentMap);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseEnvironmentMap, value);
		}
	}

	public bool UsingSpecularAlpha
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseSpecularAlpha);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseSpecularAlpha, value);
		}
	}

	public bool UsingDynamicLight
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseDynamicLight);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseDynamicLight, value);
		}
	}

	public bool UsingSunLight
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseSunLight);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseSunLight, value);
		}
	}

	public bool UsingFresnel
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseFresnel);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseFresnel, value);
		}
	}

	public bool IsSunShadowReceiver
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.SunShadowReceiver);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.SunShadowReceiver, value);
		}
	}

	public bool IsDynamicShadowReceiver
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.DynamicShadowReceiver);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.DynamicShadowReceiver, value);
		}
	}

	public bool UsingDiffuseAlphaMap
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseDiffuseAlphaMap);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseDiffuseAlphaMap, value);
		}
	}

	public bool UsingParallaxMapping
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseParallaxMapping);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseParallaxMapping, value);
		}
	}

	public bool UsingParallaxOcclusion
	{
		get
		{
			return CheckMaterialShaderFlag(MBMaterialShaderFlags.UseParallaxOcclusion);
		}
		set
		{
			SetMaterialShaderFlag(MBMaterialShaderFlags.UseParallaxOcclusion, value);
		}
	}

	public MaterialFlags Flags
	{
		get
		{
			return EngineApplicationInterface.IMaterial.GetFlags(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IMaterial.SetFlags(base.Pointer, value);
		}
	}

	public static Material GetDefaultMaterial()
	{
		return EngineApplicationInterface.IMaterial.GetDefaultMaterial();
	}

	public static Material GetOutlineMaterial(Mesh mesh)
	{
		return EngineApplicationInterface.IMaterial.GetOutlineMaterial(mesh.GetMaterial().Pointer);
	}

	public static Material GetDefaultTableauSampleMaterial(bool transparency)
	{
		if (!transparency)
		{
			return GetFromResource("sample_shield_matte");
		}
		return GetFromResource("tableau_with_transparency");
	}

	public static Material CreateTableauMaterial(RenderTargetComponent.TextureUpdateEventHandler eventHandler, object objectRef, Material sampleMaterial, int tableauSizeX, int tableauSizeY, bool continuousTableau = false)
	{
		if (sampleMaterial == null)
		{
			sampleMaterial = GetDefaultTableauSampleMaterial(transparency: true);
		}
		Material material = sampleMaterial.CreateCopy();
		uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
		ulong shaderFlags = material.GetShaderFlags();
		material.SetShaderFlags(shaderFlags | num);
		string text = "";
		Type type = objectRef.GetType();
		if (!continuousTableau && HasTableauCache.TableauCacheTypes.TryGetValue(type, out var value))
		{
			text = value(objectRef);
			text = text.ToLower();
			Texture texture = Texture.CheckAndGetFromResource(text);
			if (texture != null)
			{
				material.SetTexture(MBTextureType.DiffuseMap2, texture);
				return material;
			}
		}
		if (text != "")
		{
			Texture.ScaleTextureWithRatio(ref tableauSizeX, ref tableauSizeY);
		}
		Texture texture2 = Texture.CreateTableauTexture(text, eventHandler, objectRef, tableauSizeX, tableauSizeY);
		if (text != "")
		{
			TableauView tableauView = texture2.TableauView;
			tableauView.SetSaveFinalResultToDisk(value: true);
			tableauView.SetFileNameToSaveResult(text);
			tableauView.SetFileTypeToSave(View.TextureSaveFormat.TextureTypeDds);
		}
		if (text != "")
		{
			texture2.TransformRenderTargetToResource(text);
		}
		material.SetTexture(MBTextureType.DiffuseMap2, texture2);
		return material;
	}

	internal Material(UIntPtr sourceMaterialPointer)
		: base(sourceMaterialPointer)
	{
	}

	public Material CreateCopy()
	{
		return EngineApplicationInterface.IMaterial.CreateCopy(base.Pointer);
	}

	public static Material GetFromResource(string materialName)
	{
		return EngineApplicationInterface.IMaterial.GetFromResource(materialName);
	}

	public void SetShader(Shader shader)
	{
		EngineApplicationInterface.IMaterial.SetShader(base.Pointer, shader.Pointer);
	}

	public Shader GetShader()
	{
		return EngineApplicationInterface.IMaterial.GetShader(base.Pointer);
	}

	public ulong GetShaderFlags()
	{
		return EngineApplicationInterface.IMaterial.GetShaderFlags(base.Pointer);
	}

	public void SetShaderFlags(ulong flagEntry)
	{
		EngineApplicationInterface.IMaterial.SetShaderFlags(base.Pointer, flagEntry);
	}

	public void SetMeshVectorArgument(float x, float y, float z, float w)
	{
		EngineApplicationInterface.IMaterial.SetMeshVectorArgument(base.Pointer, x, y, z, w);
	}

	public void SetTexture(MBTextureType textureType, Texture texture)
	{
		EngineApplicationInterface.IMaterial.SetTexture(base.Pointer, (int)textureType, texture.Pointer);
	}

	public void SetTextureAtSlot(int textureSlot, Texture texture)
	{
		EngineApplicationInterface.IMaterial.SetTextureAtSlot(base.Pointer, textureSlot, texture.Pointer);
	}

	public void SetAreaMapScale(float scale)
	{
		EngineApplicationInterface.IMaterial.SetAreaMapScale(base.Pointer, scale);
	}

	public void SetEnableSkinning(bool enable)
	{
		EngineApplicationInterface.IMaterial.SetEnableSkinning(base.Pointer, enable);
	}

	public bool UsingSkinning()
	{
		return EngineApplicationInterface.IMaterial.UsingSkinning(base.Pointer);
	}

	public Texture GetTexture(MBTextureType textureType)
	{
		return EngineApplicationInterface.IMaterial.GetTexture(base.Pointer, (int)textureType);
	}

	public Texture GetTextureWithSlot(int textureSlot)
	{
		return EngineApplicationInterface.IMaterial.GetTexture(base.Pointer, textureSlot);
	}

	public static Material GetAlphaMaskTableauMaterial()
	{
		return EngineApplicationInterface.IMaterial.GetFromResource("tableau_with_alpha_mask");
	}

	public MBAlphaBlendMode GetAlphaBlendMode()
	{
		return (MBAlphaBlendMode)EngineApplicationInterface.IMaterial.GetAlphaBlendMode(base.Pointer);
	}

	public void SetAlphaBlendMode(MBAlphaBlendMode alphaBlendMode)
	{
		EngineApplicationInterface.IMaterial.SetAlphaBlendMode(base.Pointer, (int)alphaBlendMode);
	}

	public void SetAlphaTestValue(float alphaTestValue)
	{
		EngineApplicationInterface.IMaterial.SetAlphaTestValue(base.Pointer, alphaTestValue);
	}

	public float GetAlphaTestValue()
	{
		return EngineApplicationInterface.IMaterial.GetAlphaTestValue(base.Pointer);
	}

	private bool CheckMaterialShaderFlag(MBMaterialShaderFlags flagEntry)
	{
		return (EngineApplicationInterface.IMaterial.GetShaderFlags(base.Pointer) & (ulong)flagEntry) != 0;
	}

	private void SetMaterialShaderFlag(MBMaterialShaderFlags flagEntry, bool value)
	{
		ulong shaderFlags = (EngineApplicationInterface.IMaterial.GetShaderFlags(base.Pointer) & (ulong)(~(long)flagEntry)) | ((ulong)flagEntry & (ulong)(value ? 255 : 0));
		EngineApplicationInterface.IMaterial.SetShaderFlags(base.Pointer, shaderFlags);
	}

	public void AddMaterialShaderFlag(string flagName, bool showErrors)
	{
		EngineApplicationInterface.IMaterial.AddMaterialShaderFlag(base.Pointer, flagName, showErrors);
	}

	public void RemoveMaterialShaderFlag(string flagName)
	{
		EngineApplicationInterface.IMaterial.RemoveMaterialShaderFlag(base.Pointer, flagName);
	}
}
