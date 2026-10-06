using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglView")]
public abstract class View : NativeObject
{
	public enum TextureSaveFormat
	{
		TextureTypeUnknown,
		TextureTypeBmp,
		TextureTypeJpg,
		TextureTypePng,
		TextureTypeDds,
		TextureTypeTif,
		TextureTypePsd,
		TextureTypeRaw
	}

	public enum PostfxConfig : uint
	{
		pfx_config_bloom = 1u,
		pfx_config_sunshafts = 2u,
		pfx_config_motionblur = 4u,
		pfx_config_dof = 8u,
		pfx_config_tsao = 16u,
		pfx_config_fxaa = 64u,
		pfx_config_smaa = 128u,
		pfx_config_temporal_smaa = 256u,
		pfx_config_temporal_resolve = 512u,
		pfx_config_temporal_filter = 1024u,
		pfx_config_contour = 2048u,
		pfx_config_ssr = 4096u,
		pfx_config_sssss = 8192u,
		pfx_config_streaks = 16384u,
		pfx_config_lens_flares = 32768u,
		pfx_config_chromatic_aberration = 65536u,
		pfx_config_vignette = 131072u,
		pfx_config_sharpen = 262144u,
		pfx_config_grain = 524288u,
		pfx_config_temporal_shadow = 1048576u,
		pfx_config_editor_scene = 2097152u,
		pfx_config_custom1 = 16777216u,
		pfx_config_custom2 = 33554432u,
		pfx_config_custom3 = 67108864u,
		pfx_config_custom4 = 134217728u,
		pfx_config_hexagon_vignette = 268435456u,
		pfx_config_screen_rt_injection = 536870912u,
		pfx_config_high_dof = 1073741824u,
		pfx_lower_bound = 1u,
		pfx_upper_bound = 536870912u
	}

	public enum ViewRenderOptions
	{
		ClearColor,
		ClearDepth
	}

	internal View(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public void SetScale(Vec2 scale)
	{
		EngineApplicationInterface.IView.SetScale(base.Pointer, scale.x, scale.y);
	}

	public void SetOffset(Vec2 offset)
	{
		EngineApplicationInterface.IView.SetOffset(base.Pointer, offset.x, offset.y);
	}

	public void SetRenderOrder(int value)
	{
		EngineApplicationInterface.IView.SetRenderOrder(base.Pointer, value);
	}

	public void SetRenderOption(ViewRenderOptions optionEnum, bool value)
	{
		EngineApplicationInterface.IView.SetRenderOption(base.Pointer, (int)optionEnum, value);
	}

	public void SetRenderTarget(Texture texture)
	{
		EngineApplicationInterface.IView.SetRenderTarget(base.Pointer, texture.Pointer);
	}

	public void SetDepthTarget(Texture texture)
	{
		EngineApplicationInterface.IView.SetDepthTarget(base.Pointer, texture.Pointer);
	}

	public void DontClearBackground()
	{
		SetRenderOption(ViewRenderOptions.ClearColor, value: false);
		SetRenderOption(ViewRenderOptions.ClearDepth, value: false);
	}

	public void SetClearColor(uint rgba)
	{
		EngineApplicationInterface.IView.SetClearColor(base.Pointer, rgba);
	}

	public void SetEnable(bool value)
	{
		EngineApplicationInterface.IView.SetEnable(base.Pointer, value);
	}

	public void SetRenderOnDemand(bool value)
	{
		EngineApplicationInterface.IView.SetRenderOnDemand(base.Pointer, value);
	}

	public void SetAutoDepthTargetCreation(bool value)
	{
		EngineApplicationInterface.IView.SetAutoDepthTargetCreation(base.Pointer, value);
	}

	public void SetSaveFinalResultToDisk(bool value)
	{
		EngineApplicationInterface.IView.SetSaveFinalResultToDisk(base.Pointer, value);
	}

	public void SetFileNameToSaveResult(string name)
	{
		EngineApplicationInterface.IView.SetFileNameToSaveResult(base.Pointer, name);
	}

	public void SetFileTypeToSave(TextureSaveFormat format)
	{
		EngineApplicationInterface.IView.SetFileTypeToSave(base.Pointer, (int)format);
	}

	public void SetFilePathToSaveResult(string name)
	{
		EngineApplicationInterface.IView.SetFilePathToSaveResult(base.Pointer, name);
	}
}
