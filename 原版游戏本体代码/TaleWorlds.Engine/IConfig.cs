using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000018 RID: 24
	[ApplicationInterfaceBase]
	internal interface IConfig
	{
		// Token: 0x060000E7 RID: 231
		[EngineMethod("check_gfx_support_status", false, null, false)]
		bool CheckGFXSupportStatus(int enum_id);

		// Token: 0x060000E8 RID: 232
		[EngineMethod("is_dlss_available", false, null, false)]
		bool IsDlssAvailable();

		// Token: 0x060000E9 RID: 233
		[EngineMethod("is_120hz_available", false, null, false)]
		bool Is120HzAvailable();

		// Token: 0x060000EA RID: 234
		[EngineMethod("get_dlss_technique", false, null, false)]
		int GetDlssTechnique();

		// Token: 0x060000EB RID: 235
		[EngineMethod("get_dlss_option_count", false, null, false)]
		int GetDlssOptionCount();

		// Token: 0x060000EC RID: 236
		[EngineMethod("get_disable_sound", false, null, false)]
		bool GetDisableSound();

		// Token: 0x060000ED RID: 237
		[EngineMethod("get_cheat_mode", false, null, false)]
		bool GetCheatMode();

		// Token: 0x060000EE RID: 238
		[EngineMethod("get_development_mode", false, null, false)]
		bool GetDevelopmentMode();

		// Token: 0x060000EF RID: 239
		[EngineMethod("get_localization_debug_mode", false, null, false)]
		bool GetLocalizationDebugMode();

		// Token: 0x060000F0 RID: 240
		[EngineMethod("get_do_localization_check_at_startup", false, null, false)]
		bool GetDoLocalizationCheckAtStartup();

		// Token: 0x060000F1 RID: 241
		[EngineMethod("get_tableau_cache_mode", false, null, false)]
		bool GetTableauCacheMode();

		// Token: 0x060000F2 RID: 242
		[EngineMethod("get_enable_edit_mode", false, null, false)]
		bool GetEnableEditMode();

		// Token: 0x060000F3 RID: 243
		[EngineMethod("get_enable_cloth_simulation", false, null, false)]
		bool GetEnableClothSimulation();

		// Token: 0x060000F4 RID: 244
		[EngineMethod("get_character_detail", false, null, false)]
		int GetCharacterDetail();

		// Token: 0x060000F5 RID: 245
		[EngineMethod("get_invert_mouse", false, null, false)]
		bool GetInvertMouse();

		// Token: 0x060000F6 RID: 246
		[EngineMethod("get_last_opened_scene", false, null, false)]
		string GetLastOpenedScene();

		// Token: 0x060000F7 RID: 247
		[EngineMethod("read_rgl_config_files", false, null, false)]
		void ReadRGLConfigFiles();

		// Token: 0x060000F8 RID: 248
		[EngineMethod("set_rgl_config", false, null, false)]
		void SetRGLConfig(int type, float value);

		// Token: 0x060000F9 RID: 249
		[EngineMethod("apply_config_changes", false, null, false)]
		void ApplyConfigChanges(bool resizeWindow);

		// Token: 0x060000FA RID: 250
		[EngineMethod("get_rgl_config_for_default_settings", false, null, false)]
		float GetRGLConfigForDefaultSettings(int type, int defaultSettings);

		// Token: 0x060000FB RID: 251
		[EngineMethod("get_rgl_config", false, null, false)]
		float GetRGLConfig(int type);

		// Token: 0x060000FC RID: 252
		[EngineMethod("get_default_rgl_config", false, null, false)]
		float GetDefaultRGLConfig(int type);

		// Token: 0x060000FD RID: 253
		[EngineMethod("save_rgl_config", false, null, false)]
		int SaveRGLConfig();

		// Token: 0x060000FE RID: 254
		[EngineMethod("set_brightness", false, null, false)]
		void SetBrightness(float brightness);

		// Token: 0x060000FF RID: 255
		[EngineMethod("set_sharpen_amount", false, null, false)]
		void SetSharpenAmount(float sharpen_amount);

		// Token: 0x06000100 RID: 256
		[EngineMethod("get_sound_device_name", false, null, false)]
		string GetSoundDeviceName(int i);

		// Token: 0x06000101 RID: 257
		[EngineMethod("get_current_sound_device_index", false, null, false)]
		int GetCurrentSoundDeviceIndex();

		// Token: 0x06000102 RID: 258
		[EngineMethod("get_sound_device_count", false, null, false)]
		int GetSoundDeviceCount();

		// Token: 0x06000103 RID: 259
		[EngineMethod("get_resolution_count", false, null, false)]
		int GetResolutionCount();

		// Token: 0x06000104 RID: 260
		[EngineMethod("get_refresh_rate_count", false, null, false)]
		int GetRefreshRateCount();

		// Token: 0x06000105 RID: 261
		[EngineMethod("get_refresh_rate_at_index", false, null, false)]
		int GetRefreshRateAtIndex(int index);

		// Token: 0x06000106 RID: 262
		[EngineMethod("get_resolution", false, null, false)]
		void GetResolution(ref int width, ref int height);

		// Token: 0x06000107 RID: 263
		[EngineMethod("get_desktop_resolution", false, null, false)]
		void GetDesktopResolution(ref int width, ref int height);

		// Token: 0x06000108 RID: 264
		[EngineMethod("get_resolution_at_index", false, null, false)]
		Vec2 GetResolutionAtIndex(int index);

		// Token: 0x06000109 RID: 265
		[EngineMethod("set_custom_resolution", false, null, false)]
		void SetCustomResolution(int width, int height);

		// Token: 0x0600010A RID: 266
		[EngineMethod("refresh_options_data ", false, null, false)]
		void RefreshOptionsData();

		// Token: 0x0600010B RID: 267
		[EngineMethod("set_sound_device", false, null, false)]
		void SetSoundDevice(int i);

		// Token: 0x0600010C RID: 268
		[EngineMethod("set_sound_preset", false, null, false)]
		void SetSoundPreset(int i);

		// Token: 0x0600010D RID: 269
		[EngineMethod("apply", false, null, false)]
		void Apply(int texture_budget, int sharpen_amount, int hdr, int dof_mode, int motion_blur, int ssr, int size, int texture_filtering, int trail_amount, int dynamic_resolution_target);

		// Token: 0x0600010E RID: 270
		[EngineMethod("set_default_game_config", false, null, false)]
		void SetDefaultGameConfig();

		// Token: 0x0600010F RID: 271
		[EngineMethod("auto_save_in_minutes", false, null, false)]
		int AutoSaveInMinutes();

		// Token: 0x06000110 RID: 272
		[EngineMethod("get_ui_debug_mode", false, null, false)]
		bool GetUIDebugMode();

		// Token: 0x06000111 RID: 273
		[EngineMethod("get_ui_do_not_use_generated_prefabs", false, null, false)]
		bool GetUIDoNotUseGeneratedPrefabs();

		// Token: 0x06000112 RID: 274
		[EngineMethod("get_debug_login_username", false, null, false)]
		string GetDebugLoginUserName();

		// Token: 0x06000113 RID: 275
		[EngineMethod("get_debug_login_password", false, null, false)]
		string GetDebugLoginPassword();

		// Token: 0x06000114 RID: 276
		[EngineMethod("get_disable_gui_messages", false, null, false)]
		bool GetDisableGuiMessages();

		// Token: 0x06000115 RID: 277
		[EngineMethod("get_auto_gfx_quality", false, null, false)]
		int GetAutoGFXQuality();

		// Token: 0x06000116 RID: 278
		[EngineMethod("set_auto_config_wrt_hardware", false, null, false)]
		void SetAutoConfigWrtHardware();

		// Token: 0x06000117 RID: 279
		[EngineMethod("get_monitor_device_name", false, null, false)]
		string GetMonitorDeviceName(int i);

		// Token: 0x06000118 RID: 280
		[EngineMethod("get_video_device_name", false, null, false)]
		string GetVideoDeviceName(int i);

		// Token: 0x06000119 RID: 281
		[EngineMethod("get_monitor_device_count", false, null, false)]
		int GetMonitorDeviceCount();

		// Token: 0x0600011A RID: 282
		[EngineMethod("get_video_device_count", false, null, false)]
		int GetVideoDeviceCount();
	}
}
