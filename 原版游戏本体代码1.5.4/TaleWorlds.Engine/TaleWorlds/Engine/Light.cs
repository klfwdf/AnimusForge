using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglLight")]
public sealed class Light : GameEntityComponent
{
	public enum ShadowType
	{
		NoShadow,
		StaticShadow,
		DynamicShadow,
		Count
	}

	public bool IsValid => base.Pointer != UIntPtr.Zero;

	public MatrixFrame Frame
	{
		get
		{
			EngineApplicationInterface.ILight.GetFrame(base.Pointer, out var result);
			return result;
		}
		set
		{
			EngineApplicationInterface.ILight.SetFrame(base.Pointer, ref value);
		}
	}

	public Vec3 LightColor
	{
		get
		{
			return EngineApplicationInterface.ILight.GetLightColor(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.ILight.SetLightColor(base.Pointer, value);
		}
	}

	public float Intensity
	{
		get
		{
			return EngineApplicationInterface.ILight.GetIntensity(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.ILight.SetIntensity(base.Pointer, value);
		}
	}

	public float Radius
	{
		get
		{
			return EngineApplicationInterface.ILight.GetRadius(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.ILight.SetRadius(base.Pointer, value);
		}
	}

	public bool ShadowEnabled
	{
		get
		{
			return EngineApplicationInterface.ILight.IsShadowEnabled(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.ILight.EnableShadow(base.Pointer, value);
		}
	}

	internal Light(UIntPtr pointer)
		: base(pointer)
	{
	}

	public static Light CreatePointLight(float lightRadius)
	{
		return EngineApplicationInterface.ILight.CreatePointLight(lightRadius);
	}

	public void SetShadowType(ShadowType type)
	{
		EngineApplicationInterface.ILight.SetShadows(base.Pointer, (int)type);
	}

	public void SetLightFlicker(float magnitude, float interval)
	{
		EngineApplicationInterface.ILight.SetLightFlicker(base.Pointer, magnitude, interval);
	}

	public void SetVolumetricProperties(bool volumetricLightEnabled, float volumeParameters)
	{
		EngineApplicationInterface.ILight.SetVolumetricProperties(base.Pointer, volumetricLightEnabled, volumeParameters);
	}

	public void Dispose()
	{
		if (IsValid)
		{
			Release();
			GC.SuppressFinalize(this);
		}
	}

	public void SetVisibility(bool value)
	{
		EngineApplicationInterface.ILight.SetVisibility(base.Pointer, value);
	}

	private void Release()
	{
		EngineApplicationInterface.ILight.Release(base.Pointer);
	}

	~Light()
	{
		Dispose();
	}
}
