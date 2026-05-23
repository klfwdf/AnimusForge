using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.Options;

public class NativeOptions
{
	public enum ConfigQuality
	{
		GFXVeryLow,
		GFXLow,
		GFXMedium,
		GFXHigh,
		GFXVeryHigh,
		GFXCustom
	}

	public enum NativeOptionsType
	{
		None = -1,
		MasterVolume,
		SoundVolume,
		MusicVolume,
		VoiceChatVolume,
		VoiceOverVolume,
		SoundDevice,
		MaxSimultaneousSoundEventCount,
		SoundPreset,
		KeepSoundInBackground,
		SoundOcclusion,
		MouseSensitivity,
		InvertMouseYAxis,
		MouseYMovementScale,
		TrailAmount,
		EnableVibration,
		EnableGyroAssistedAim,
		GyroAimSensitivity,
		EnableTouchpadMouse,
		EnableAlternateAiming,
		DisplayMode,
		SelectedMonitor,
		SelectedAdapter,
		ScreenResolution,
		RefreshRate,
		ResolutionScale,
		FrameLimiter,
		VSync,
		Brightness,
		OverAll,
		ShaderQuality,
		TextureBudget,
		TextureQuality,
		ShadowmapResolution,
		ShadowmapType,
		ShadowmapFiltering,
		ParticleDetail,
		ParticleQuality,
		FoliageQuality,
		CharacterDetail,
		EnvironmentDetail,
		TerrainQuality,
		NumberOfRagDolls,
		AnimationSamplingQuality,
		Occlusion,
		TextureFiltering,
		WaterQuality,
		SSRQuality,
		Antialiasing,
		DLSS,
		LightingQuality,
		DecalQuality,
		DepthOfField,
		SSR,
		ClothSimulation,
		InteractiveGrass,
		SunShafts,
		SSSSS,
		Tesselation,
		Bloom,
		FilmGrain,
		MotionBlur,
		SharpenAmount,
		PostFXLensFlare,
		PostFXStreaks,
		PostFXChromaticAberration,
		PostFXVignette,
		PostFXHexagonVignette,
		BrightnessMin,
		BrightnessMax,
		BrightnessCalibrated,
		ExposureCompensation,
		DynamicResolution,
		DynamicResolutionTarget,
		FSR,
		PhysicsTickRate,
		NumOfOptionTypes,
		TotalOptions
	}

	public delegate void OnNativeOptionChangedDelegate(NativeOptionsType changedNativeOptionsType);

	public static OnNativeOptionChangedDelegate OnNativeOptionChanged;

	private static List<NativeOptionData> _videoOptions;

	private static List<NativeOptionData> _graphicsOptions;

	public static List<NativeOptionData> VideoOptions
	{
		get
		{
			if (_videoOptions == null)
			{
				_videoOptions = new List<NativeOptionData>();
				for (NativeOptionsType nativeOptionsType = NativeOptionsType.None; nativeOptionsType < NativeOptionsType.TotalOptions; nativeOptionsType++)
				{
					if ((uint)(nativeOptionsType - 19) <= 7u || nativeOptionsType == NativeOptionsType.SharpenAmount)
					{
						_videoOptions.Add(new NativeNumericOptionData(nativeOptionsType));
					}
				}
			}
			return _videoOptions;
		}
	}

	public static List<NativeOptionData> GraphicsOptions
	{
		get
		{
			if (_graphicsOptions == null)
			{
				_graphicsOptions = new List<NativeOptionData>();
				for (NativeOptionsType nativeOptionsType = NativeOptionsType.None; nativeOptionsType < NativeOptionsType.TotalOptions; nativeOptionsType++)
				{
					switch (nativeOptionsType)
					{
					case NativeOptionsType.MaxSimultaneousSoundEventCount:
					case NativeOptionsType.OverAll:
					case NativeOptionsType.ShaderQuality:
					case NativeOptionsType.TextureBudget:
					case NativeOptionsType.TextureQuality:
					case NativeOptionsType.ShadowmapResolution:
					case NativeOptionsType.ShadowmapType:
					case NativeOptionsType.ShadowmapFiltering:
					case NativeOptionsType.ParticleDetail:
					case NativeOptionsType.ParticleQuality:
					case NativeOptionsType.FoliageQuality:
					case NativeOptionsType.CharacterDetail:
					case NativeOptionsType.EnvironmentDetail:
					case NativeOptionsType.TerrainQuality:
					case NativeOptionsType.NumberOfRagDolls:
					case NativeOptionsType.AnimationSamplingQuality:
					case NativeOptionsType.Occlusion:
					case NativeOptionsType.TextureFiltering:
					case NativeOptionsType.WaterQuality:
					case NativeOptionsType.SSRQuality:
					case NativeOptionsType.Antialiasing:
					case NativeOptionsType.LightingQuality:
					case NativeOptionsType.DecalQuality:
					case NativeOptionsType.PhysicsTickRate:
						_graphicsOptions.Add(new NativeSelectionOptionData(nativeOptionsType));
						break;
					case NativeOptionsType.DLSS:
						if (GetIsDLSSAvailable())
						{
							_graphicsOptions.Add(new NativeSelectionOptionData(nativeOptionsType));
						}
						break;
					case NativeOptionsType.DepthOfField:
					case NativeOptionsType.SSR:
					case NativeOptionsType.ClothSimulation:
					case NativeOptionsType.InteractiveGrass:
					case NativeOptionsType.SunShafts:
					case NativeOptionsType.SSSSS:
					case NativeOptionsType.Tesselation:
					case NativeOptionsType.Bloom:
					case NativeOptionsType.FilmGrain:
					case NativeOptionsType.MotionBlur:
					case NativeOptionsType.DynamicResolution:
						_graphicsOptions.Add(new NativeBooleanOptionData(nativeOptionsType));
						break;
					case NativeOptionsType.PostFXChromaticAberration:
						if (EngineApplicationInterface.IConfig.CheckGFXSupportStatus(64))
						{
							_graphicsOptions.Add(new NativeBooleanOptionData(nativeOptionsType));
						}
						break;
					case NativeOptionsType.PostFXHexagonVignette:
						if (EngineApplicationInterface.IConfig.CheckGFXSupportStatus(66))
						{
							_graphicsOptions.Add(new NativeBooleanOptionData(nativeOptionsType));
						}
						break;
					case NativeOptionsType.PostFXLensFlare:
						if (EngineApplicationInterface.IConfig.CheckGFXSupportStatus(62))
						{
							_graphicsOptions.Add(new NativeBooleanOptionData(nativeOptionsType));
						}
						break;
					case NativeOptionsType.PostFXStreaks:
						if (EngineApplicationInterface.IConfig.CheckGFXSupportStatus(63))
						{
							_graphicsOptions.Add(new NativeBooleanOptionData(nativeOptionsType));
						}
						break;
					case NativeOptionsType.PostFXVignette:
						if (EngineApplicationInterface.IConfig.CheckGFXSupportStatus(65))
						{
							_graphicsOptions.Add(new NativeBooleanOptionData(nativeOptionsType));
						}
						break;
					case NativeOptionsType.DynamicResolutionTarget:
						_graphicsOptions.Add(new NativeNumericOptionData(nativeOptionsType));
						break;
					}
				}
			}
			return _graphicsOptions;
		}
	}

	public static event Action OnNativeOptionsApplied;

	public static string GetGFXPresetName(ConfigQuality presetIndex)
	{
		return presetIndex switch
		{
			ConfigQuality.GFXVeryLow => "1", 
			ConfigQuality.GFXLow => "2", 
			ConfigQuality.GFXMedium => "3", 
			ConfigQuality.GFXHigh => "4", 
			ConfigQuality.GFXVeryHigh => "5", 
			ConfigQuality.GFXCustom => "Custom", 
			_ => "Unknown", 
		};
	}

	public static bool IsGFXOptionChangeable(ConfigQuality config)
	{
		return config < ConfigQuality.GFXCustom;
	}

	private static void CorrectSelection(List<NativeOptionData> audioOptions)
	{
		foreach (NativeOptionData audioOption in audioOptions)
		{
			if (audioOption.Type != NativeOptionsType.SoundDevice)
			{
				continue;
			}
			int num = 0;
			for (int i = 0; i < GetSoundDeviceCount(); i++)
			{
				if (GetSoundDeviceName(i) != "")
				{
					num = i;
				}
			}
			if (audioOption.GetValue(forceRefresh: false) > (float)num)
			{
				SetConfig(NativeOptionsType.SoundDevice, 0f);
				audioOption.SetValue(0f);
			}
		}
	}

	public static void ReadRGLConfigFiles()
	{
		EngineApplicationInterface.IConfig.ReadRGLConfigFiles();
	}

	public static float GetConfig(NativeOptionsType type)
	{
		return EngineApplicationInterface.IConfig.GetRGLConfig((int)type);
	}

	public static float GetDefaultConfig(NativeOptionsType type)
	{
		return EngineApplicationInterface.IConfig.GetDefaultRGLConfig((int)type);
	}

	public static float GetDefaultConfigForOverallSettings(NativeOptionsType type, int config)
	{
		return EngineApplicationInterface.IConfig.GetRGLConfigForDefaultSettings((int)type, config);
	}

	public static int GetGameKeys(int keyType, int i)
	{
		Debug.FailedAssert("This is not implemented. Changed from Exception to not cause crash.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Options\\NativeOptions\\NativeOptions.cs", "GetGameKeys", 330);
		return 0;
	}

	public static string GetSoundDeviceName(int i)
	{
		return EngineApplicationInterface.IConfig.GetSoundDeviceName(i);
	}

	public static string GetMonitorDeviceName(int i)
	{
		return EngineApplicationInterface.IConfig.GetMonitorDeviceName(i);
	}

	public static string GetVideoDeviceName(int i)
	{
		return EngineApplicationInterface.IConfig.GetVideoDeviceName(i);
	}

	public static int GetSoundDeviceCount()
	{
		return EngineApplicationInterface.IConfig.GetSoundDeviceCount();
	}

	public static int GetMonitorDeviceCount()
	{
		return EngineApplicationInterface.IConfig.GetMonitorDeviceCount();
	}

	public static int GetVideoDeviceCount()
	{
		return EngineApplicationInterface.IConfig.GetVideoDeviceCount();
	}

	public static int GetResolutionCount()
	{
		return EngineApplicationInterface.IConfig.GetResolutionCount();
	}

	public static void RefreshOptionsData()
	{
		EngineApplicationInterface.IConfig.RefreshOptionsData();
	}

	public static int GetRefreshRateCount()
	{
		return EngineApplicationInterface.IConfig.GetRefreshRateCount();
	}

	public static int GetRefreshRateAtIndex(int index)
	{
		return EngineApplicationInterface.IConfig.GetRefreshRateAtIndex(index);
	}

	public static void SetCustomResolution(int width, int height)
	{
		EngineApplicationInterface.IConfig.SetCustomResolution(width, height);
	}

	public static void GetResolution(ref int width, ref int height)
	{
		EngineApplicationInterface.IConfig.GetDesktopResolution(ref width, ref height);
	}

	public static void GetDesktopResolution(ref int width, ref int height)
	{
		EngineApplicationInterface.IConfig.GetDesktopResolution(ref width, ref height);
	}

	public static Vec2 GetResolutionAtIndex(int index)
	{
		return EngineApplicationInterface.IConfig.GetResolutionAtIndex(index);
	}

	public static int GetDLSSTechnique()
	{
		return EngineApplicationInterface.IConfig.GetDlssTechnique();
	}

	public static bool Is120HzAvailable()
	{
		return EngineApplicationInterface.IConfig.Is120HzAvailable();
	}

	public static int GetDLSSOptionCount()
	{
		return EngineApplicationInterface.IConfig.GetDlssOptionCount();
	}

	public static bool GetIsDLSSAvailable()
	{
		return EngineApplicationInterface.IConfig.IsDlssAvailable();
	}

	public static bool CheckGFXSupportStatus(int enumType)
	{
		return EngineApplicationInterface.IConfig.CheckGFXSupportStatus(enumType);
	}

	public static void SetConfig(NativeOptionsType type, float value)
	{
		EngineApplicationInterface.IConfig.SetRGLConfig((int)type, value);
		OnNativeOptionChanged?.Invoke(type);
	}

	public static void ApplyConfigChanges(bool resizeWindow)
	{
		EngineApplicationInterface.IConfig.ApplyConfigChanges(resizeWindow);
	}

	public static void SetGameKeys(int keyType, int index, int key)
	{
		Debug.FailedAssert("This is not implemented. Changed from Exception to not cause crash.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Options\\NativeOptions\\NativeOptions.cs", "SetGameKeys", 443);
	}

	public static void Apply(int texture_budget, int sharpen_amount, int hdr, int dof_mode, int motion_blur, int ssr, int size, int texture_filtering, int trail_amount, int dynamic_resolution_target)
	{
		EngineApplicationInterface.IConfig.Apply(texture_budget, sharpen_amount, hdr, dof_mode, motion_blur, ssr, size, texture_filtering, trail_amount, dynamic_resolution_target);
		NativeOptions.OnNativeOptionsApplied?.Invoke();
	}

	public static SaveResult SaveConfig()
	{
		return (SaveResult)EngineApplicationInterface.IConfig.SaveRGLConfig();
	}

	public static void SetBrightness(float gamma)
	{
		EngineApplicationInterface.IConfig.SetBrightness(gamma);
	}

	public static void SetDefaultGameKeys()
	{
		Debug.FailedAssert("This is not implemented. Changed from Exception to not cause crash.", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Options\\NativeOptions\\NativeOptions.cs", "SetDefaultGameKeys", 468);
	}

	public static void SetDefaultGameConfig()
	{
		EngineApplicationInterface.IConfig.SetDefaultGameConfig();
	}
}
