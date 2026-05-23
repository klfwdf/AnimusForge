using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public static class BannerlordConfig
{
	private interface IConfigPropertyBoundChecker<T>
	{
	}

	private abstract class ConfigProperty : Attribute
	{
	}

	private sealed class ConfigPropertyInt : ConfigProperty
	{
		private int[] _possibleValues;

		private bool _isRange;

		public ConfigPropertyInt(int[] possibleValues, bool isRange = false)
		{
			_possibleValues = possibleValues;
			_isRange = isRange;
			_ = _isRange;
		}

		public bool IsValidValue(int value)
		{
			if (_isRange)
			{
				if (value >= _possibleValues[0])
				{
					return value <= _possibleValues[1];
				}
				return false;
			}
			int[] possibleValues = _possibleValues;
			for (int i = 0; i < possibleValues.Length; i++)
			{
				if (possibleValues[i] == value)
				{
					return true;
				}
			}
			return false;
		}
	}

	private sealed class ConfigPropertyUnbounded : ConfigProperty
	{
	}

	private static int[] _battleSizes = new int[7] { 200, 300, 400, 500, 600, 800, 1000 };

	private static int[] _siegeBattleSizes = new int[7] { 150, 230, 320, 425, 540, 625, 1000 };

	private static int[] _sallyOutBattleSizes = new int[7] { 150, 200, 240, 280, 320, 360, 400 };

	private static int[] _reinforcementWaveCounts = new int[4] { 3, 4, 5, 0 };

	public const int MaxCorpseCount = 1021;

	public static double SiegeBattleSizeMultiplier = 0.8;

	public const int DefaultMapDoubleClickBehavior = 0;

	public const int DefaultPlayerReceviedDamageDifficulty = 0;

	public const bool DefaultGyroOverrideForAttackDefend = false;

	public const int DefaultAttackDirectionControl = 1;

	public const int DefaultDefendDirectionControl = 0;

	public const int DefaultNumberOfCorpses = 3;

	public const bool DefaultShowBlood = true;

	public const bool DefaultDisplayAttackDirection = true;

	public const bool DefaultDisplayTargetingReticule = true;

	public const bool DefaultForceVSyncInMenus = true;

	public const int DefaultBattleSize = 2;

	public const int DefaultReinforcementWaveCount = 3;

	public const float DefaultBattleSizeMultiplier = 0.5f;

	public const float DefaultFirstPersonFov = 65f;

	public const float DefaultUIScale = 1f;

	public const float DefaultCombatCameraDistance = 1f;

	public const int DefaultCombatAI = 0;

	public const int DefaultTurnCameraWithHorseInFirstPerson = 2;

	public const int DefaultAutoSaveInterval = 30;

	public const float DefaultFriendlyTroopsBannerOpacity = 1f;

	public const int DefaultAlwaysShowFriendlyTroopBannersType = 1;

	public const bool DefaultShowFormationDistances = false;

	public const bool DefaultReportDamage = true;

	public const bool DefaultReportBark = true;

	public const bool DefaultEnableTutorialHints = true;

	public const int DefaultKillFeedVisualType = 1;

	public const int DefaultAutoTrackAttackedSettlements = 0;

	public const bool DefaultReportPersonalDamage = true;

	public const bool DefaultStopGameOnFocusLost = true;

	public const bool DefaultSlowDownOnOrder = true;

	public const bool DefaultReportExperience = true;

	public const bool DefaultEnableDamageTakenVisuals = true;

	public const bool DefaultEnableVoiceChat = true;

	public const bool DefaultEnableDeathIcon = true;

	public const bool DefaultEnableNetworkAlertIcons = true;

	public const bool DefaultEnableVerticalAimCorrection = true;

	public const float DefaultZoomSensitivityModifier = 0.66666f;

	public const bool DefaultSingleplayerEnableChatBox = true;

	public const bool DefaultMultiplayerEnableChatBox = true;

	public const float DefaultChatBoxSizeX = 495f;

	public const float DefaultChatBoxSizeY = 340f;

	public const int DefaultCrosshairType = 0;

	public const bool DefaultEnableGenericAvatars = false;

	public const bool DefaultEnableGenericNames = false;

	public const bool DefaultHideFullServers = false;

	public const bool DefaultHideEmptyServers = false;

	public const bool DefaultHidePasswordProtectedServers = false;

	public const bool DefaultHideUnofficialServers = false;

	public const bool DefaultHideModuleIncompatibleServers = false;

	public const bool DefaultShowOnlyFavoriteServers = false;

	public const int DefaultOrderLayoutType = 0;

	public const bool DefaultHideBattleUI = false;

	public const int DefaultUnitSpawnPrioritization = 0;

	public const int DefaultOrderType = 0;

	public const bool DefaultLockTarget = false;

	private static string _language = DefaultLanguage;

	private static string _voiceLanguage = DefaultLanguage;

	private static int _numberOfCorpses = 3;

	private static int _battleSize = 2;

	private static int _autoSaveInterval = 30;

	private static bool _stopGameOnFocusLost = true;

	private static int _orderType = 0;

	private static int _orderLayoutType = 0;

	public static int MinBattleSize => _battleSizes[0];

	public static int MaxBattleSize => _battleSizes[_battleSizes.Length - 1];

	public static int MinReinforcementWaveCount => _reinforcementWaveCounts[0];

	public static int MaxReinforcementWaveCount => _reinforcementWaveCounts[_reinforcementWaveCounts.Length - 1];

	public static string DefaultLanguage => GetDefaultLanguage();

	[ConfigPropertyUnbounded]
	public static string Language
	{
		get
		{
			return _language;
		}
		set
		{
			if (_language != value)
			{
				if (MBTextManager.LanguageExistsInCurrentConfiguration(value, NativeConfig.IsDevelopmentMode) && MBTextManager.ChangeLanguage(value))
				{
					_language = value;
				}
				else if (MBTextManager.ChangeLanguage("English"))
				{
					_language = "English";
				}
				else
				{
					Debug.FailedAssert("Language cannot be set!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\BannerlordConfig.cs", "Language", 391);
				}
				MBTextManager.LocalizationDebugMode = NativeConfig.LocalizationDebugMode;
			}
		}
	}

	[ConfigPropertyUnbounded]
	public static string VoiceLanguage
	{
		get
		{
			return _voiceLanguage;
		}
		set
		{
			if (_voiceLanguage != value)
			{
				if (MBTextManager.LanguageExistsInCurrentConfiguration(value, NativeConfig.IsDevelopmentMode) && MBTextManager.TryChangeVoiceLanguage(value))
				{
					_voiceLanguage = value;
				}
				else if (MBTextManager.TryChangeVoiceLanguage("English"))
				{
					_voiceLanguage = "English";
				}
				else
				{
					Debug.FailedAssert("Voice Language cannot be set!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\BannerlordConfig.cs", "VoiceLanguage", 418);
				}
			}
		}
	}

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int MapDoubleClickBehavior { get; set; } = MapDoubleClickBehavior;

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int PlayerReceivedDamageDifficulty { get; set; } = 0;

	[ConfigPropertyUnbounded]
	public static bool GyroOverrideForAttackDefend { get; set; } = false;

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int AttackDirectionControl { get; set; } = 1;

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int DefendDirectionControl { get; set; } = 0;

	[ConfigPropertyInt(new int[] { 0, 1, 2, 3, 4, 5 }, false)]
	public static int NumberOfCorpses
	{
		get
		{
			return _numberOfCorpses;
		}
		set
		{
			_numberOfCorpses = value;
		}
	}

	[ConfigPropertyUnbounded]
	public static bool ShowBlood { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool DisplayAttackDirection { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool DisplayTargetingReticule { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool ForceVSyncInMenus { get; set; } = true;

	[ConfigPropertyInt(new int[] { 0, 1, 2, 3, 4, 5, 6 }, false)]
	public static int BattleSize
	{
		get
		{
			return _battleSize;
		}
		set
		{
			_battleSize = value;
		}
	}

	[ConfigPropertyInt(new int[] { 0, 1, 2, 3 }, false)]
	public static int ReinforcementWaveCount { get; set; } = 3;

	public static float CivilianAgentCount => (float)GetRealBattleSize() * 0.5f;

	[ConfigPropertyUnbounded]
	public static float FirstPersonFov { get; set; } = 65f;

	[ConfigPropertyUnbounded]
	public static float UIScale { get; set; } = 1f;

	[ConfigPropertyUnbounded]
	public static float CombatCameraDistance { get; set; } = 1f;

	[ConfigPropertyInt(new int[] { 0, 1, 2, 3 }, false)]
	public static int TurnCameraWithHorseInFirstPerson { get; set; } = 2;

	[ConfigPropertyUnbounded]
	public static bool ReportDamage { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool ReportBark { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool LockTarget { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool EnableTutorialHints { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static int AutoSaveInterval
	{
		get
		{
			return _autoSaveInterval;
		}
		set
		{
			if (value == 4)
			{
				_autoSaveInterval = -1;
			}
			else
			{
				_autoSaveInterval = value;
			}
		}
	}

	[ConfigPropertyUnbounded]
	public static float FriendlyTroopsBannerOpacity { get; set; } = 1f;

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int AlwaysShowFriendlyTroopBannersType { get; set; } = 1;

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int KillFeedVisualType { get; set; } = 1;

	[ConfigPropertyUnbounded]
	public static bool ShowFormationDistances { get; set; } = false;

	[ConfigPropertyInt(new int[] { 0, 1, 2 }, false)]
	public static int AutoTrackAttackedSettlements { get; set; } = 0;

	[ConfigPropertyUnbounded]
	public static bool ReportPersonalDamage { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool SlowDownOnOrder { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool StopGameOnFocusLost
	{
		get
		{
			return _stopGameOnFocusLost;
		}
		set
		{
			_stopGameOnFocusLost = value;
		}
	}

	[ConfigPropertyUnbounded]
	public static bool ReportExperience { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool EnableDamageTakenVisuals { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool EnableVerticalAimCorrection { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static float ZoomSensitivityModifier { get; set; } = 0.66666f;

	[ConfigPropertyInt(new int[] { 0, 1 }, false)]
	public static int CrosshairType { get; set; } = 0;

	[ConfigPropertyUnbounded]
	public static bool EnableGenericAvatars { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool EnableGenericNames { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool HideFullServers { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool HideEmptyServers { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool HidePasswordProtectedServers { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool HideUnofficialServers { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool HideModuleIncompatibleServers { get; set; } = false;

	[ConfigPropertyUnbounded]
	public static bool ShowOnlyFavoriteServers { get; set; } = false;

	[ConfigPropertyInt(new int[] { 0, 1 }, false)]
	public static int OrderType
	{
		get
		{
			return _orderType;
		}
		set
		{
			_orderType = value;
		}
	}

	[ConfigPropertyInt(new int[] { 0, 1 }, false)]
	public static int OrderLayoutType
	{
		get
		{
			return _orderLayoutType;
		}
		set
		{
			_orderLayoutType = value;
		}
	}

	[ConfigPropertyUnbounded]
	public static bool EnableVoiceChat { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool EnableDeathIcon { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool EnableNetworkAlertIcons { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool EnableSingleplayerChatBox { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static bool EnableMultiplayerChatBox { get; set; } = true;

	[ConfigPropertyUnbounded]
	public static float ChatBoxSizeX { get; set; } = 495f;

	[ConfigPropertyUnbounded]
	public static float ChatBoxSizeY { get; set; } = 340f;

	[ConfigPropertyUnbounded]
	public static string LatestSaveGameName { get; set; } = string.Empty;

	[ConfigPropertyUnbounded]
	public static bool HideBattleUI { get; set; } = false;

	[ConfigPropertyInt(new int[] { 0, 1, 2, 3 }, false)]
	public static int UnitSpawnPrioritization { get; set; } = 0;

	[ConfigPropertyUnbounded]
	public static bool IAPNoticeConfirmed { get; set; } = false;

	public static void Initialize()
	{
		string text = Utilities.LoadBannerlordConfigFile();
		if (string.IsNullOrEmpty(text))
		{
			Save();
		}
		else
		{
			bool flag = false;
			string[] array = text.Split(new char[1] { '\n' });
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[1] { '=' });
				PropertyInfo property = typeof(BannerlordConfig).GetProperty(array2[0]);
				if (property == null)
				{
					flag = true;
					continue;
				}
				string text2 = array2[1];
				try
				{
					if (property.PropertyType == typeof(string))
					{
						string value = Regex.Replace(text2, "\\r", "");
						property.SetValue(null, value);
					}
					else if (property.PropertyType == typeof(float))
					{
						if (float.TryParse(text2, out var result))
						{
							property.SetValue(null, result);
						}
						else
						{
							flag = true;
						}
					}
					else if (property.PropertyType == typeof(int))
					{
						if (int.TryParse(text2, out var result2))
						{
							ConfigPropertyInt customAttribute = property.GetCustomAttribute<ConfigPropertyInt>();
							if (customAttribute == null || customAttribute.IsValidValue(result2))
							{
								property.SetValue(null, result2);
							}
							else
							{
								flag = true;
							}
						}
						else
						{
							flag = true;
						}
					}
					else if (property.PropertyType == typeof(bool))
					{
						if (bool.TryParse(text2, out var result3))
						{
							property.SetValue(null, result3);
						}
						else
						{
							flag = true;
						}
					}
					else
					{
						flag = true;
						Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\BannerlordConfig.cs", "Initialize", 114);
					}
				}
				catch
				{
					flag = true;
				}
			}
			if (flag)
			{
				Save();
			}
			MBAPI.IMBBannerlordConfig.ValidateOptions();
		}
		MBTextManager.TryChangeVoiceLanguage(VoiceLanguage);
		MBTextManager.ChangeLanguage(Language);
		MBTextManager.LocalizationDebugMode = NativeConfig.LocalizationDebugMode;
	}

	public static SaveResult Save()
	{
		Dictionary<PropertyInfo, object> dictionary = new Dictionary<PropertyInfo, object>();
		PropertyInfo[] properties = typeof(BannerlordConfig).GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetCustomAttribute<ConfigProperty>() != null)
			{
				dictionary.Add(propertyInfo, propertyInfo.GetValue(null, null));
			}
		}
		string text = "";
		foreach (KeyValuePair<PropertyInfo, object> item in dictionary)
		{
			text = text + item.Key.Name + "=" + item.Value.ToString() + "\n";
		}
		SaveResult result = Utilities.SaveConfigFile(text);
		MBAPI.IMBBannerlordConfig.ValidateOptions();
		return result;
	}

	public static float GetDamageToPlayerMultiplier()
	{
		return PlayerReceivedDamageDifficulty switch
		{
			0 => 0.25f, 
			1 => 0.5f, 
			2 => 1f, 
			_ => 1f, 
		};
	}

	public static int GetRealBattleSize()
	{
		return _battleSizes[BattleSize];
	}

	public static int GetRealBattleSizeForSiege()
	{
		return _siegeBattleSizes[BattleSize];
	}

	public static int GetRealBattleSizeForNaval()
	{
		return _battleSizes[BattleSize];
	}

	public static int GetReinforcementWaveCount()
	{
		return _reinforcementWaveCounts[ReinforcementWaveCount];
	}

	public static int GetRealBattleSizeForSallyOut()
	{
		return _sallyOutBattleSizes[BattleSize];
	}

	private static string GetDefaultLanguage()
	{
		return LocalizedTextManager.GetLocalizationCodeOfISOLanguageCode(Utilities.GetSystemLanguage());
	}
}
