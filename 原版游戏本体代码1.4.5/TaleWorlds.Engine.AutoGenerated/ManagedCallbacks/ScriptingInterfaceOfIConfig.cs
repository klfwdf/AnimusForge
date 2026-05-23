using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIConfig : IConfig
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ApplyDelegate(int texture_budget, int sharpen_amount, int hdr, int dof_mode, int motion_blur, int ssr, int size, int texture_filtering, int trail_amount, int dynamic_resolution_target);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ApplyConfigChangesDelegate([MarshalAs(UnmanagedType.U1)] bool resizeWindow);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AutoSaveInMinutesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckGFXSupportStatusDelegate(int enum_id);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAutoGFXQualityDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetCharacterDetailDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetCheatModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetCurrentSoundDeviceIndexDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDebugLoginPasswordDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDebugLoginUserNameDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetDefaultRGLConfigDelegate(int type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetDesktopResolutionDelegate(ref int width, ref int height);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetDevelopmentModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetDisableGuiMessagesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetDisableSoundDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDlssOptionCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDlssTechniqueDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetDoLocalizationCheckAtStartupDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetEnableClothSimulationDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetEnableEditModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetInvertMouseDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetLastOpenedSceneDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetLocalizationDebugModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMonitorDeviceCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetMonitorDeviceNameDelegate(int i);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetRefreshRateAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetRefreshRateCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetResolutionDelegate(ref int width, ref int height);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetResolutionAtIndexDelegate(int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetResolutionCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRGLConfigDelegate(int type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRGLConfigForDefaultSettingsDelegate(int type, int defaultSettings);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSoundDeviceCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSoundDeviceNameDelegate(int i);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetTableauCacheModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetUIDebugModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetUIDoNotUseGeneratedPrefabsDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVideoDeviceCountDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetVideoDeviceNameDelegate(int i);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool Is120HzAvailableDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsDlssAvailableDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReadRGLConfigFilesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RefreshOptionsDataDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SaveRGLConfigDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAutoConfigWrtHardwareDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBrightnessDelegate(float brightness);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCustomResolutionDelegate(int width, int height);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDefaultGameConfigDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRGLConfigDelegate(int type, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSharpenAmountDelegate(float sharpen_amount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSoundDeviceDelegate(int i);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSoundPresetDelegate(int i);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static ApplyDelegate call_ApplyDelegate;

	public static ApplyConfigChangesDelegate call_ApplyConfigChangesDelegate;

	public static AutoSaveInMinutesDelegate call_AutoSaveInMinutesDelegate;

	public static CheckGFXSupportStatusDelegate call_CheckGFXSupportStatusDelegate;

	public static GetAutoGFXQualityDelegate call_GetAutoGFXQualityDelegate;

	public static GetCharacterDetailDelegate call_GetCharacterDetailDelegate;

	public static GetCheatModeDelegate call_GetCheatModeDelegate;

	public static GetCurrentSoundDeviceIndexDelegate call_GetCurrentSoundDeviceIndexDelegate;

	public static GetDebugLoginPasswordDelegate call_GetDebugLoginPasswordDelegate;

	public static GetDebugLoginUserNameDelegate call_GetDebugLoginUserNameDelegate;

	public static GetDefaultRGLConfigDelegate call_GetDefaultRGLConfigDelegate;

	public static GetDesktopResolutionDelegate call_GetDesktopResolutionDelegate;

	public static GetDevelopmentModeDelegate call_GetDevelopmentModeDelegate;

	public static GetDisableGuiMessagesDelegate call_GetDisableGuiMessagesDelegate;

	public static GetDisableSoundDelegate call_GetDisableSoundDelegate;

	public static GetDlssOptionCountDelegate call_GetDlssOptionCountDelegate;

	public static GetDlssTechniqueDelegate call_GetDlssTechniqueDelegate;

	public static GetDoLocalizationCheckAtStartupDelegate call_GetDoLocalizationCheckAtStartupDelegate;

	public static GetEnableClothSimulationDelegate call_GetEnableClothSimulationDelegate;

	public static GetEnableEditModeDelegate call_GetEnableEditModeDelegate;

	public static GetInvertMouseDelegate call_GetInvertMouseDelegate;

	public static GetLastOpenedSceneDelegate call_GetLastOpenedSceneDelegate;

	public static GetLocalizationDebugModeDelegate call_GetLocalizationDebugModeDelegate;

	public static GetMonitorDeviceCountDelegate call_GetMonitorDeviceCountDelegate;

	public static GetMonitorDeviceNameDelegate call_GetMonitorDeviceNameDelegate;

	public static GetRefreshRateAtIndexDelegate call_GetRefreshRateAtIndexDelegate;

	public static GetRefreshRateCountDelegate call_GetRefreshRateCountDelegate;

	public static GetResolutionDelegate call_GetResolutionDelegate;

	public static GetResolutionAtIndexDelegate call_GetResolutionAtIndexDelegate;

	public static GetResolutionCountDelegate call_GetResolutionCountDelegate;

	public static GetRGLConfigDelegate call_GetRGLConfigDelegate;

	public static GetRGLConfigForDefaultSettingsDelegate call_GetRGLConfigForDefaultSettingsDelegate;

	public static GetSoundDeviceCountDelegate call_GetSoundDeviceCountDelegate;

	public static GetSoundDeviceNameDelegate call_GetSoundDeviceNameDelegate;

	public static GetTableauCacheModeDelegate call_GetTableauCacheModeDelegate;

	public static GetUIDebugModeDelegate call_GetUIDebugModeDelegate;

	public static GetUIDoNotUseGeneratedPrefabsDelegate call_GetUIDoNotUseGeneratedPrefabsDelegate;

	public static GetVideoDeviceCountDelegate call_GetVideoDeviceCountDelegate;

	public static GetVideoDeviceNameDelegate call_GetVideoDeviceNameDelegate;

	public static Is120HzAvailableDelegate call_Is120HzAvailableDelegate;

	public static IsDlssAvailableDelegate call_IsDlssAvailableDelegate;

	public static ReadRGLConfigFilesDelegate call_ReadRGLConfigFilesDelegate;

	public static RefreshOptionsDataDelegate call_RefreshOptionsDataDelegate;

	public static SaveRGLConfigDelegate call_SaveRGLConfigDelegate;

	public static SetAutoConfigWrtHardwareDelegate call_SetAutoConfigWrtHardwareDelegate;

	public static SetBrightnessDelegate call_SetBrightnessDelegate;

	public static SetCustomResolutionDelegate call_SetCustomResolutionDelegate;

	public static SetDefaultGameConfigDelegate call_SetDefaultGameConfigDelegate;

	public static SetRGLConfigDelegate call_SetRGLConfigDelegate;

	public static SetSharpenAmountDelegate call_SetSharpenAmountDelegate;

	public static SetSoundDeviceDelegate call_SetSoundDeviceDelegate;

	public static SetSoundPresetDelegate call_SetSoundPresetDelegate;

	public void Apply(int texture_budget, int sharpen_amount, int hdr, int dof_mode, int motion_blur, int ssr, int size, int texture_filtering, int trail_amount, int dynamic_resolution_target)
	{
		call_ApplyDelegate(texture_budget, sharpen_amount, hdr, dof_mode, motion_blur, ssr, size, texture_filtering, trail_amount, dynamic_resolution_target);
	}

	public void ApplyConfigChanges(bool resizeWindow)
	{
		call_ApplyConfigChangesDelegate(resizeWindow);
	}

	public int AutoSaveInMinutes()
	{
		return call_AutoSaveInMinutesDelegate();
	}

	public bool CheckGFXSupportStatus(int enum_id)
	{
		return call_CheckGFXSupportStatusDelegate(enum_id);
	}

	public int GetAutoGFXQuality()
	{
		return call_GetAutoGFXQualityDelegate();
	}

	public int GetCharacterDetail()
	{
		return call_GetCharacterDetailDelegate();
	}

	public bool GetCheatMode()
	{
		return call_GetCheatModeDelegate();
	}

	public int GetCurrentSoundDeviceIndex()
	{
		return call_GetCurrentSoundDeviceIndexDelegate();
	}

	public string GetDebugLoginPassword()
	{
		if (call_GetDebugLoginPasswordDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetDebugLoginUserName()
	{
		if (call_GetDebugLoginUserNameDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetDefaultRGLConfig(int type)
	{
		return call_GetDefaultRGLConfigDelegate(type);
	}

	public void GetDesktopResolution(ref int width, ref int height)
	{
		call_GetDesktopResolutionDelegate(ref width, ref height);
	}

	public bool GetDevelopmentMode()
	{
		return call_GetDevelopmentModeDelegate();
	}

	public bool GetDisableGuiMessages()
	{
		return call_GetDisableGuiMessagesDelegate();
	}

	public bool GetDisableSound()
	{
		return call_GetDisableSoundDelegate();
	}

	public int GetDlssOptionCount()
	{
		return call_GetDlssOptionCountDelegate();
	}

	public int GetDlssTechnique()
	{
		return call_GetDlssTechniqueDelegate();
	}

	public bool GetDoLocalizationCheckAtStartup()
	{
		return call_GetDoLocalizationCheckAtStartupDelegate();
	}

	public bool GetEnableClothSimulation()
	{
		return call_GetEnableClothSimulationDelegate();
	}

	public bool GetEnableEditMode()
	{
		return call_GetEnableEditModeDelegate();
	}

	public bool GetInvertMouse()
	{
		return call_GetInvertMouseDelegate();
	}

	public string GetLastOpenedScene()
	{
		if (call_GetLastOpenedSceneDelegate() != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool GetLocalizationDebugMode()
	{
		return call_GetLocalizationDebugModeDelegate();
	}

	public int GetMonitorDeviceCount()
	{
		return call_GetMonitorDeviceCountDelegate();
	}

	public string GetMonitorDeviceName(int i)
	{
		if (call_GetMonitorDeviceNameDelegate(i) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public int GetRefreshRateAtIndex(int index)
	{
		return call_GetRefreshRateAtIndexDelegate(index);
	}

	public int GetRefreshRateCount()
	{
		return call_GetRefreshRateCountDelegate();
	}

	public void GetResolution(ref int width, ref int height)
	{
		call_GetResolutionDelegate(ref width, ref height);
	}

	public Vec2 GetResolutionAtIndex(int index)
	{
		return call_GetResolutionAtIndexDelegate(index);
	}

	public int GetResolutionCount()
	{
		return call_GetResolutionCountDelegate();
	}

	public float GetRGLConfig(int type)
	{
		return call_GetRGLConfigDelegate(type);
	}

	public float GetRGLConfigForDefaultSettings(int type, int defaultSettings)
	{
		return call_GetRGLConfigForDefaultSettingsDelegate(type, defaultSettings);
	}

	public int GetSoundDeviceCount()
	{
		return call_GetSoundDeviceCountDelegate();
	}

	public string GetSoundDeviceName(int i)
	{
		if (call_GetSoundDeviceNameDelegate(i) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool GetTableauCacheMode()
	{
		return call_GetTableauCacheModeDelegate();
	}

	public bool GetUIDebugMode()
	{
		return call_GetUIDebugModeDelegate();
	}

	public bool GetUIDoNotUseGeneratedPrefabs()
	{
		return call_GetUIDoNotUseGeneratedPrefabsDelegate();
	}

	public int GetVideoDeviceCount()
	{
		return call_GetVideoDeviceCountDelegate();
	}

	public string GetVideoDeviceName(int i)
	{
		if (call_GetVideoDeviceNameDelegate(i) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public bool Is120HzAvailable()
	{
		return call_Is120HzAvailableDelegate();
	}

	public bool IsDlssAvailable()
	{
		return call_IsDlssAvailableDelegate();
	}

	public void ReadRGLConfigFiles()
	{
		call_ReadRGLConfigFilesDelegate();
	}

	public void RefreshOptionsData()
	{
		call_RefreshOptionsDataDelegate();
	}

	public int SaveRGLConfig()
	{
		return call_SaveRGLConfigDelegate();
	}

	public void SetAutoConfigWrtHardware()
	{
		call_SetAutoConfigWrtHardwareDelegate();
	}

	public void SetBrightness(float brightness)
	{
		call_SetBrightnessDelegate(brightness);
	}

	public void SetCustomResolution(int width, int height)
	{
		call_SetCustomResolutionDelegate(width, height);
	}

	public void SetDefaultGameConfig()
	{
		call_SetDefaultGameConfigDelegate();
	}

	public void SetRGLConfig(int type, float value)
	{
		call_SetRGLConfigDelegate(type, value);
	}

	public void SetSharpenAmount(float sharpen_amount)
	{
		call_SetSharpenAmountDelegate(sharpen_amount);
	}

	public void SetSoundDevice(int i)
	{
		call_SetSoundDeviceDelegate(i);
	}

	public void SetSoundPreset(int i)
	{
		call_SetSoundPresetDelegate(i);
	}
}
