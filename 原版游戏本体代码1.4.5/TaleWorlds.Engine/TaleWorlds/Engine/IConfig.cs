using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IConfig
{
	[EngineMethod("check_gfx_support_status", false, null, false)]
	bool CheckGFXSupportStatus(int enum_id);

	[EngineMethod("is_dlss_available", false, null, false)]
	bool IsDlssAvailable();

	[EngineMethod("is_120hz_available", false, null, false)]
	bool Is120HzAvailable();

	[EngineMethod("get_dlss_technique", false, null, false)]
	int GetDlssTechnique();

	[EngineMethod("get_dlss_option_count", false, null, false)]
	int GetDlssOptionCount();

	[EngineMethod("get_disable_sound", false, null, false)]
	bool GetDisableSound();

	[EngineMethod("get_cheat_mode", false, null, false)]
	bool GetCheatMode();

	[EngineMethod("get_development_mode", false, null, false)]
	bool GetDevelopmentMode();

	[EngineMethod("get_localization_debug_mode", false, null, false)]
	bool GetLocalizationDebugMode();

	[EngineMethod("get_do_localization_check_at_startup", false, null, false)]
	bool GetDoLocalizationCheckAtStartup();

	[EngineMethod("get_tableau_cache_mode", false, null, false)]
	bool GetTableauCacheMode();

	[EngineMethod("get_enable_edit_mode", false, null, false)]
	bool GetEnableEditMode();

	[EngineMethod("get_enable_cloth_simulation", false, null, false)]
	bool GetEnableClothSimulation();

	[EngineMethod("get_character_detail", false, null, false)]
	int GetCharacterDetail();

	[EngineMethod("get_invert_mouse", false, null, false)]
	bool GetInvertMouse();

	[EngineMethod("get_last_opened_scene", false, null, false)]
	string GetLastOpenedScene();

	[EngineMethod("read_rgl_config_files", false, null, false)]
	void ReadRGLConfigFiles();

	[EngineMethod("set_rgl_config", false, null, false)]
	void SetRGLConfig(int type, float value);

	[EngineMethod("apply_config_changes", false, null, false)]
	void ApplyConfigChanges(bool resizeWindow);

	[EngineMethod("get_rgl_config_for_default_settings", false, null, false)]
	float GetRGLConfigForDefaultSettings(int type, int defaultSettings);

	[EngineMethod("get_rgl_config", false, null, false)]
	float GetRGLConfig(int type);

	[EngineMethod("get_default_rgl_config", false, null, false)]
	float GetDefaultRGLConfig(int type);

	[EngineMethod("save_rgl_config", false, null, false)]
	int SaveRGLConfig();

	[EngineMethod("set_brightness", false, null, false)]
	void SetBrightness(float brightness);

	[EngineMethod("set_sharpen_amount", false, null, false)]
	void SetSharpenAmount(float sharpen_amount);

	[EngineMethod("get_sound_device_name", false, null, false)]
	string GetSoundDeviceName(int i);

	[EngineMethod("get_current_sound_device_index", false, null, false)]
	int GetCurrentSoundDeviceIndex();

	[EngineMethod("get_sound_device_count", false, null, false)]
	int GetSoundDeviceCount();

	[EngineMethod("get_resolution_count", false, null, false)]
	int GetResolutionCount();

	[EngineMethod("get_refresh_rate_count", false, null, false)]
	int GetRefreshRateCount();

	[EngineMethod("get_refresh_rate_at_index", false, null, false)]
	int GetRefreshRateAtIndex(int index);

	[EngineMethod("get_resolution", false, null, false)]
	void GetResolution(ref int width, ref int height);

	[EngineMethod("get_desktop_resolution", false, null, false)]
	void GetDesktopResolution(ref int width, ref int height);

	[EngineMethod("get_resolution_at_index", false, null, false)]
	Vec2 GetResolutionAtIndex(int index);

	[EngineMethod("set_custom_resolution", false, null, false)]
	void SetCustomResolution(int width, int height);

	[EngineMethod("refresh_options_data ", false, null, false)]
	void RefreshOptionsData();

	[EngineMethod("set_sound_device", false, null, false)]
	void SetSoundDevice(int i);

	[EngineMethod("set_sound_preset", false, null, false)]
	void SetSoundPreset(int i);

	[EngineMethod("apply", false, null, false)]
	void Apply(int texture_budget, int sharpen_amount, int hdr, int dof_mode, int motion_blur, int ssr, int size, int texture_filtering, int trail_amount, int dynamic_resolution_target);

	[EngineMethod("set_default_game_config", false, null, false)]
	void SetDefaultGameConfig();

	[EngineMethod("auto_save_in_minutes", false, null, false)]
	int AutoSaveInMinutes();

	[EngineMethod("get_ui_debug_mode", false, null, false)]
	bool GetUIDebugMode();

	[EngineMethod("get_ui_do_not_use_generated_prefabs", false, null, false)]
	bool GetUIDoNotUseGeneratedPrefabs();

	[EngineMethod("get_debug_login_username", false, null, false)]
	string GetDebugLoginUserName();

	[EngineMethod("get_debug_login_password", false, null, false)]
	string GetDebugLoginPassword();

	[EngineMethod("get_disable_gui_messages", false, null, false)]
	bool GetDisableGuiMessages();

	[EngineMethod("get_auto_gfx_quality", false, null, false)]
	int GetAutoGFXQuality();

	[EngineMethod("set_auto_config_wrt_hardware", false, null, false)]
	void SetAutoConfigWrtHardware();

	[EngineMethod("get_monitor_device_name", false, null, false)]
	string GetMonitorDeviceName(int i);

	[EngineMethod("get_video_device_name", false, null, false)]
	string GetVideoDeviceName(int i);

	[EngineMethod("get_monitor_device_count", false, null, false)]
	int GetMonitorDeviceCount();

	[EngineMethod("get_video_device_count", false, null, false)]
	int GetVideoDeviceCount();
}
