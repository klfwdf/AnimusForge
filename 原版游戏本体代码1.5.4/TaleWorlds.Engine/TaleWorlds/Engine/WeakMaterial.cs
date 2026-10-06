using System;

namespace TaleWorlds.Engine;

public struct WeakMaterial
{
	public static readonly WeakMaterial Invalid = new WeakMaterial(UIntPtr.Zero);

	public UIntPtr Pointer { get; private set; }

	public bool IsValid => Pointer != UIntPtr.Zero;

	public string Name
	{
		get
		{
			return EngineApplicationInterface.IMaterial.GetName(Pointer);
		}
		set
		{
			EngineApplicationInterface.IMaterial.SetName(Pointer, value);
		}
	}

	internal WeakMaterial(UIntPtr pointer)
	{
		Pointer = pointer;
	}

	public Shader GetShader()
	{
		return EngineApplicationInterface.IMaterial.GetShader(Pointer);
	}

	public ulong GetShaderFlags()
	{
		return EngineApplicationInterface.IMaterial.GetShaderFlags(Pointer);
	}

	public void SetShaderFlags(ulong flagEntry)
	{
		EngineApplicationInterface.IMaterial.SetShaderFlags(Pointer, flagEntry);
	}

	public void SetMeshVectorArgument(float x, float y, float z, float w)
	{
		EngineApplicationInterface.IMaterial.SetMeshVectorArgument(Pointer, x, y, z, w);
	}

	public void SetTexture(Material.MBTextureType textureType, Texture texture)
	{
		EngineApplicationInterface.IMaterial.SetTexture(Pointer, (int)textureType, texture.Pointer);
	}

	public void SetTextureAtSlot(int textureSlot, Texture texture)
	{
		EngineApplicationInterface.IMaterial.SetTextureAtSlot(Pointer, textureSlot, texture.Pointer);
	}

	public void SetAreaMapScale(float scale)
	{
		EngineApplicationInterface.IMaterial.SetAreaMapScale(Pointer, scale);
	}

	public void SetEnableSkinning(bool enable)
	{
		EngineApplicationInterface.IMaterial.SetEnableSkinning(Pointer, enable);
	}

	public bool UsingSkinning()
	{
		return EngineApplicationInterface.IMaterial.UsingSkinning(Pointer);
	}

	public Texture GetTexture(Material.MBTextureType textureType)
	{
		return EngineApplicationInterface.IMaterial.GetTexture(Pointer, (int)textureType);
	}

	public Texture GetTextureWithSlot(int textureSlot)
	{
		return EngineApplicationInterface.IMaterial.GetTexture(Pointer, textureSlot);
	}

	public void AddMaterialShaderFlag(string flagName, bool showErrors)
	{
		EngineApplicationInterface.IMaterial.AddMaterialShaderFlag(Pointer, flagName, showErrors);
	}

	public void RemoveMaterialShaderFlag(string flagName)
	{
		EngineApplicationInterface.IMaterial.RemoveMaterialShaderFlag(Pointer, flagName);
	}

	public static bool operator ==(WeakMaterial weakMaterial1, WeakMaterial weakMaterial2)
	{
		return weakMaterial1.Pointer == weakMaterial2.Pointer;
	}

	public static bool operator !=(WeakMaterial weakMaterial1, WeakMaterial weakMaterial2)
	{
		return weakMaterial1.Pointer != weakMaterial2.Pointer;
	}

	public override bool Equals(object obj)
	{
		return ((Material)obj).Pointer == Pointer;
	}

	public override int GetHashCode()
	{
		return Pointer.GetHashCode();
	}
}
