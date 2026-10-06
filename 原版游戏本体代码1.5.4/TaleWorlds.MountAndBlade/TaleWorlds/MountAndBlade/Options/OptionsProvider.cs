using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Options.ManagedOptions;

namespace TaleWorlds.MountAndBlade.Options;

public static class OptionsProvider
{
	private static readonly int _overallConfigCount = NativeSelectionOptionData.GetOptionsLimit(NativeOptions.NativeOptionsType.OverAll) - 1;

	private static Dictionary<NativeOptions.NativeOptionsType, float[]> _defaultNativeOptions;

	private static Dictionary<TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType, float[]> _defaultManagedOptions;

	public static OptionCategory GetVideoOptionCategory(bool isMainMenu, Action onBrightnessClick, Action onExposureClick, Action onBenchmarkClick)
	{
		return new OptionCategory(GetVideoGeneralOptions(isMainMenu, onBrightnessClick, onExposureClick, onBenchmarkClick), GetVideoOptionGroups());
	}

	private static IEnumerable<IOptionData> GetVideoGeneralOptions(bool isMainMenu, Action onBrightnessClick, Action onExposureClick, Action onBenchmarkClick)
	{
		if (isMainMenu)
		{
			yield return new ActionOptionData("Benchmark", onBenchmarkClick);
		}
		yield return new ActionOptionData(NativeOptions.NativeOptionsType.Brightness, onBrightnessClick);
		yield return new ActionOptionData(NativeOptions.NativeOptionsType.ExposureCompensation, onExposureClick);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.SelectedMonitor);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.SelectedAdapter);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.DisplayMode);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ScreenResolution);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.RefreshRate);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.VSync);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ForceVSyncInMenus);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.FrameLimiter);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.SharpenAmount);
	}

	private static IEnumerable<IOptionData> GetPerformanceGeneralOptions(bool isMultiplayer)
	{
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.OverAll);
	}

	private static IEnumerable<OptionGroup> GetVideoOptionGroups()
	{
		return null;
	}

	public static OptionCategory GetPerformanceOptionCategory(bool isMultiplayer)
	{
		return new OptionCategory(GetPerformanceGeneralOptions(isMultiplayer), GetPerformanceOptionGroups(isMultiplayer));
	}

	private static IEnumerable<OptionGroup> GetPerformanceOptionGroups(bool isMultiplayer)
	{
		yield return new OptionGroup(new TextObject("{=sRTd3RI5}Graphics"), GetPerformanceGraphicsOptions(isMultiplayer));
		yield return new OptionGroup(new TextObject("{=vDMe8SCV}Resolution Scaling"), GetPerformanceResolutionScalingOptions(isMultiplayer));
		yield return new OptionGroup(new TextObject("{=2zcrC0h1}Gameplay"), GetPerformanceGameplayOptions(isMultiplayer));
		yield return new OptionGroup(new TextObject("{=xebFLnH2}Audio"), GetPerformanceAudioOptions());
	}

	public static IEnumerable<IOptionData> GetPerformanceGraphicsOptions(bool isMultiplayer)
	{
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.Antialiasing);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ShaderQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.TextureBudget);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.TextureQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.TextureFiltering);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.CharacterDetail);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ShadowmapResolution);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ShadowmapType);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ShadowmapFiltering);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ParticleDetail);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.ParticleQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.FoliageQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.TerrainQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.EnvironmentDetail);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.Occlusion);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.DecalQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.WaterQuality);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.SSRQuality);
		if (!isMultiplayer)
		{
			yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.NumberOfCorpses);
		}
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.NumberOfRagDolls);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.LightingQuality);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.ClothSimulation);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.SunShafts);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.Tesselation);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.InteractiveGrass);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.SSR);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.SSSSS);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.MotionBlur);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.DepthOfField);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.Bloom);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.FilmGrain);
		if (NativeOptions.CheckGFXSupportStatus(65))
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.PostFXVignette);
		}
		if (NativeOptions.CheckGFXSupportStatus(64))
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.PostFXChromaticAberration);
		}
		if (NativeOptions.CheckGFXSupportStatus(62))
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.PostFXLensFlare);
		}
		if (NativeOptions.CheckGFXSupportStatus(66))
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.PostFXHexagonVignette);
		}
		if (NativeOptions.CheckGFXSupportStatus(63))
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.PostFXStreaks);
		}
	}

	public static IEnumerable<IOptionData> GetPerformanceResolutionScalingOptions(bool isMultiplayer)
	{
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.DLSS);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.ResolutionScale);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.DynamicResolution);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.DynamicResolutionTarget);
	}

	public static IEnumerable<IOptionData> GetPerformanceGameplayOptions(bool isMultiplayer)
	{
		if (!isMultiplayer)
		{
			yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.BattleSize);
			yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.PhysicsTickRate);
		}
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.AnimationSamplingQuality);
	}

	public static IEnumerable<IOptionData> GetPerformanceAudioOptions()
	{
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.MaxSimultaneousSoundEventCount);
	}

	private static IEnumerable<IOptionData> GetAudioGeneralOptions(bool isMultiplayer)
	{
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.MasterVolume);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.SoundVolume);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.MusicVolume);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.VoiceOverVolume);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.SoundPreset);
		yield return new NativeSelectionOptionData(NativeOptions.NativeOptionsType.SoundDevice);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.KeepSoundInBackground);
		if (isMultiplayer)
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableVoiceChat);
		}
		else
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.SoundOcclusion);
		}
	}

	public static OptionCategory GetAudioOptionCategory(bool isMultiplayer)
	{
		return new OptionCategory(GetAudioGeneralOptions(isMultiplayer), GetAudioOptionGroups(isMultiplayer));
	}

	private static IEnumerable<OptionGroup> GetAudioOptionGroups(bool isMultiplayer)
	{
		return null;
	}

	public static OptionCategory GetGameplayOptionCategory(bool isMainMenu, bool isMultiplayer)
	{
		return new OptionCategory(GetGameplayGeneralOptions(isMultiplayer), GetGameplayOptionGroups(isMainMenu, isMultiplayer));
	}

	private static IEnumerable<IOptionData> GetGameplayGeneralOptions(bool isMultiplayer)
	{
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.Language);
		if (!isMultiplayer)
		{
			yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.VoiceLanguage);
			yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.PlayerReceivedDamageDifficulty);
		}
	}

	private static IEnumerable<OptionGroup> GetGameplayOptionGroups(bool isMainMenu, bool isMultiplayer)
	{
		yield return new OptionGroup(new TextObject("{=m9KoYCv5}Controls"), GetGameplayControlsOptions(isMainMenu, isMultiplayer));
		yield return new OptionGroup(new TextObject("{=uZ6q4Qs2}Visuals"), GetGameplayVisualOptions(isMultiplayer));
		yield return new OptionGroup(new TextObject("{=gAfbULHM}Camera"), GetGameplayCameraOptions(isMultiplayer));
		yield return new OptionGroup(new TextObject("{=WRMyiiYJ}User Interface"), GetGameplayUIOptions(isMultiplayer));
		if (!isMultiplayer)
		{
			yield return new OptionGroup(new TextObject("{=ys9baYiQ}Campaign"), GetGameplayCampaignOptions());
		}
	}

	private static IEnumerable<IOptionData> GetGameplayControlsOptions(bool isMainMenu, bool isMultiplayer)
	{
		bool isDualSense = Input.ControllerType.IsPlaystation();
		if (isDualSense)
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.GyroOverrideForAttackDefend);
		}
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ControlBlockDirection);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ControlAttackDirection);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.MouseYMovementScale);
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.MouseSensitivity);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.InvertMouseYAxis);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.EnableVibration);
		yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.EnableAlternateAiming);
		if (isDualSense)
		{
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.EnableTouchpadMouse);
			yield return new NativeBooleanOptionData(NativeOptions.NativeOptionsType.EnableGyroAssistedAim);
			yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.GyroAimSensitivity);
		}
		if (!isMultiplayer)
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.LockTarget);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.SlowDownOnOrder);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.StopGameOnFocusLost);
		}
	}

	private static IEnumerable<IOptionData> GetGameplayVisualOptions(bool isMultiplayer)
	{
		yield return new NativeNumericOptionData(NativeOptions.NativeOptionsType.TrailAmount);
		yield return new ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.FriendlyTroopsBannerOpacity);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.AlwaysShowFriendlyTroopBannersType);
		if (!isMultiplayer)
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ShowFormationDistances);
		}
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ShowBlood);
	}

	private static IEnumerable<IOptionData> GetGameplayCameraOptions(bool isMultiplayer)
	{
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.TurnCameraWithHorseInFirstPerson);
		yield return new ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.FirstPersonFov);
		yield return new ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.CombatCameraDistance);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableVerticalAimCorrection);
		yield return new ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ZoomSensitivityModifier);
	}

	private static IEnumerable<IOptionData> GetGameplayUIOptions(bool isMultiplayer)
	{
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.CrosshairType);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.OrderType);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.OrderLayoutType);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ReportCasualtiesType);
		yield return new ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.UIScale);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ShowAttackDirection);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ShowTargetingReticle);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ReportDamage);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ReportBark);
		if (!isMultiplayer)
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ReportExperience);
		}
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ReportPersonalDamage);
		yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableDamageTakenVisuals);
		if (isMultiplayer)
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableMultiplayerChatBox);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableDeathIcon);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableNetworkAlertIcons);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableGenericAvatars);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableGenericNames);
		}
		else
		{
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableSingleplayerChatBox);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.HideBattleUI);
			yield return new ManagedBooleanOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.EnableTutorialHints);
		}
	}

	private static IEnumerable<IOptionData> GetGameplayCampaignOptions()
	{
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.AutoTrackAttackedSettlements);
		yield return new ManagedNumericOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.AutoSaveInterval);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.UnitSpawnPrioritization);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.ReinforcementWaveCount);
		yield return new ManagedSelectionOptionData(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.MapDoubleClickBehavior);
	}

	public static IEnumerable<string> GetGameKeyCategoriesList(bool isMultiplayer)
	{
		yield return GameKeyMainCategories.ActionCategory;
		yield return GameKeyMainCategories.OrderMenuCategory;
		if (!isMultiplayer)
		{
			yield return GameKeyMainCategories.ShipControlsCategory;
			yield return GameKeyMainCategories.CampaignMapCategory;
			yield return GameKeyMainCategories.MenuShortcutCategory;
			yield return GameKeyMainCategories.PhotoModeCategory;
		}
		else
		{
			yield return GameKeyMainCategories.PollCategory;
		}
		yield return GameKeyMainCategories.ChatCategory;
	}

	public static IEnumerable<int> GetHiddenGameKeys(bool isNavalModuleActive)
	{
		if (!isNavalModuleActive)
		{
			yield return 45;
		}
	}

	public static OptionCategory GetControllerOptionCategory()
	{
		return new OptionCategory(GetControllerBaseOptions(), GetControllerOptionGroups());
	}

	private static IEnumerable<IOptionData> GetControllerBaseOptions()
	{
		return null;
	}

	private static IEnumerable<OptionGroup> GetControllerOptionGroups()
	{
		return null;
	}

	public static Dictionary<NativeOptions.NativeOptionsType, float[]> GetDefaultNativeOptions()
	{
		if (_defaultNativeOptions == null)
		{
			_defaultNativeOptions = new Dictionary<NativeOptions.NativeOptionsType, float[]>();
			foreach (NativeOptionData item in NativeOptions.VideoOptions.Union(NativeOptions.GraphicsOptions))
			{
				float[] array = new float[_overallConfigCount];
				bool flag = false;
				for (int i = 0; i < _overallConfigCount; i++)
				{
					array[i] = NativeOptions.GetDefaultConfigForOverallSettings(item.Type, i);
					if (array[i] < 0f)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					_defaultNativeOptions[item.Type] = array;
				}
			}
		}
		return _defaultNativeOptions;
	}

	public static Dictionary<TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType, float[]> GetDefaultManagedOptions()
	{
		if (_defaultManagedOptions == null)
		{
			_defaultManagedOptions = new Dictionary<TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType, float[]>();
			float[] array = new float[_overallConfigCount];
			for (int i = 0; i < _overallConfigCount; i++)
			{
				array[i] = i;
			}
			_defaultManagedOptions.Add(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.BattleSize, array);
			array = new float[_overallConfigCount];
			for (int j = 0; j < _overallConfigCount; j++)
			{
				array[j] = j;
			}
			_defaultManagedOptions.Add(TaleWorlds.MountAndBlade.ManagedOptions.ManagedOptionsType.NumberOfCorpses, array);
		}
		return _defaultManagedOptions;
	}
}
