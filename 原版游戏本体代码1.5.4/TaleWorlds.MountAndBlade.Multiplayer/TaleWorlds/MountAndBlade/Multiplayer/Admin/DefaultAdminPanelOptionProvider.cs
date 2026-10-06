using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Multiplayer.Admin.Internal;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public class DefaultAdminPanelOptionProvider : IAdminPanelOptionProvider
{
	public static class DefaultOptionIds
	{
		public const string NextGameType = "next_game_type";

		public const string NextMap = "next_map";

		public const string NextCultureTeam1 = "next_culture_team_1";

		public const string NextCultureTeam2 = "next_culture_team_2";

		public const string NextNumberOfRounds = "next_number_of_rounds";

		public const string NextMinScoreToWinDuel = "next_min_score_to_win_duel";

		public const string NextMapTimeLimit = "next_map_time_limit";

		public const string NextRoundTimeLimit = "next_round_time_limit";

		public const string NextWarmupTimeLimit = "next_warmup_time_limit";

		public const string NextMaxNumberOfPlayers = "next_max_num_players";

		public const string ApplyAndStartMission = "apply_and_start";

		public const string WelcomeMessage = "welcome_message";

		public const string AutoTeamBalanceTreshold = "auto_balance_treshold";

		public const string FriendlyFireMeleePercent = "friendly_fire_melee_percent";

		public const string FriendlyFireMeleeReflectionPercent = "friendly_fire_melee_self_percent";

		public const string FriendlyFireRangedPercent = "friendly_fire_ranged_percent";

		public const string FriendlyFireRangedReflectionPercent = "friendly_fire_ranged_self_percent";

		public const string AllowInfantry = "allow_infantry";

		public const string AllowRanged = "allow_ranged";

		public const string AllowCavalry = "allow_cavalry";

		public const string AllowHorseArchers = "allow_horse_archers";

		public const string EnableSpectators = "enable_spectators";

		public const string StreamerModeEnabled = "streamer_mode_enabled";

		public const string FormationTargetingVisibilityMode = "formation_targeting_visibility_mode";

		public const string FormationTargetingVisibilityThreshold = "formation_targeting_visibility_threshold";

		public const string FormationTargetingVisibilityAppliesAtCloseRange = "formation_targeting_visibility_close_range";

		public const string FormationMarkerFarDistanceCutoff = "formation_marker_far_distance_cutoff";

		public const string FormationMarkerFarAlphaTarget = "formation_marker_far_alpha_target";

		public const string FormationMarkerAlwaysOnDistance = "formation_marker_always_on_distance";

		public const string EndWarmup = "end_warmup";

		public const string MutePlayer = "mute_player";

		public const string KickPlayer = "kick_player";

		public const string BanPlayer = "ban_player";
	}

	private class AdminPanelVotableMultiSelectionOption : AdminPanelMultiSelectionOption
	{
		protected readonly IAdminPanelMultiSelectionItem _undecidedOption;

		public bool IsUndecided { get; private set; }

		public AdminPanelVotableMultiSelectionOption(string uniqueId)
			: base(uniqueId)
		{
			_undecidedOption = new AdminPanelMultiSelectionItem(null, new TextObject("{=b5HkM0tT}Undecided"), isFallbackValue: true);
		}

		protected override void OnValueChanged(IAdminPanelMultiSelectionItem previousValue, IAdminPanelMultiSelectionItem newValue)
		{
			base.OnValueChanged(previousValue, newValue);
			IsUndecided = _selectedOption == _undecidedOption;
		}

		public override AdminPanelMultiSelectionOption BuildAvailableOptions(MBReadOnlyList<IAdminPanelMultiSelectionItem> options)
		{
			base.BuildAvailableOptions(options);
			AddUndecidedOption();
			if (!_availableOptions.Contains(base.CurrentValue) && _availableOptions.Count > 0)
			{
				BuildInitialValue(_availableOptions[0]);
				SetValue(_availableOptions[0]);
			}
			return this;
		}

		public override AdminPanelMultiSelectionOption BuildAvailableOptions(MultiplayerOptions.OptionType optionType, bool buildDefaultValue = true)
		{
			base.BuildAvailableOptions(optionType, buildDefaultValue: false);
			AddUndecidedOption();
			if (!_availableOptions.Contains(base.CurrentValue) && _availableOptions.Count > 0)
			{
				BuildInitialValue(_availableOptions[0]);
				SetValue(_availableOptions[0]);
			}
			return this;
		}

		protected void AddUndecidedOption()
		{
			for (int i = 0; i < _availableOptions.Count; i++)
			{
				if (_availableOptions[i] == _undecidedOption || _availableOptions[i].Value == _undecidedOption.Value)
				{
					return;
				}
			}
			if (!GetIsDisabled(out var _))
			{
				_availableOptions.Insert(0, _undecidedOption);
				BuildDefaultValue(_undecidedOption);
				BuildInitialValue(_undecidedOption);
				SetValue(_undecidedOption);
			}
		}

		protected void RemoveUndecidedOption()
		{
			bool flag = false;
			for (int i = 0; i < _availableOptions.Count; i++)
			{
				if (_availableOptions[i] == _undecidedOption || _availableOptions[i].Value == _undecidedOption.Value)
				{
					_availableOptions.RemoveAt(i);
					flag = true;
					break;
				}
			}
			if (flag && _availableOptions.Count > 0)
			{
				IAdminPanelMultiSelectionItem value = _availableOptions[0];
				BuildDefaultValue(value);
				BuildInitialValue(value);
				SetValue(value);
			}
		}
	}

	private class AdminPanelCultureOption : AdminPanelVotableMultiSelectionOption
	{
		private bool _shouldKeepUndecidedOption;

		private AdminPanelCultureOption _otherOption;

		public AdminPanelCultureOption(string uniqueId)
			: base(uniqueId)
		{
		}

		public AdminPanelCultureOption BuildOtherCultureOption(AdminPanelCultureOption otherOption)
		{
			_otherOption?.RemoveValueChangedCallback(OnOtherOptionValueChanged);
			_otherOption = otherOption;
			_otherOption?.AddValueChangedCallback(OnOtherOptionValueChanged);
			return this;
		}

		public override void OnFinalize()
		{
			base.OnFinalize();
			_otherOption?.RemoveValueChangedCallback(OnOtherOptionValueChanged);
		}

		protected override void OnValueChanged(IAdminPanelMultiSelectionItem previousValue, IAdminPanelMultiSelectionItem newValue)
		{
			bool isUndecided = base.IsUndecided;
			base.OnValueChanged(previousValue, newValue);
			if (isUndecided && !base.IsUndecided)
			{
				_shouldKeepUndecidedOption = true;
			}
			else if (!isUndecided && base.IsUndecided)
			{
				_shouldKeepUndecidedOption = false;
			}
		}

		private void OnOtherOptionValueChanged()
		{
			if (_otherOption.IsUndecided)
			{
				AddUndecidedOption();
			}
			else if (!_shouldKeepUndecidedOption)
			{
				RemoveUndecidedOption();
			}
		}
	}

	private class AdminPanelUsableMapsOption : AdminPanelVotableMultiSelectionOption
	{
		private const string _disabledOptionTag = "map_option_disabled";

		private const string _undecidedOptionTag = "map_option_undecided";

		private readonly Dictionary<string, MBList<IAdminPanelMultiSelectionItem>> _optionsByGameType;

		private readonly IAdminPanelMultiSelectionItem _disabledOption;

		private bool _isUpdatingOptions;

		private AdminPanelVotableMultiSelectionOption _gameTypeOption;

		public AdminPanelUsableMapsOption(string uniqueId)
			: base(uniqueId)
		{
			_optionsByGameType = new Dictionary<string, MBList<IAdminPanelMultiSelectionItem>>();
			_disabledOption = new AdminPanelMultiSelectionItem(null, new TextObject("{=1JlzQIXE}Disabled"), isFallbackValue: false, isDisabled: true);
			_optionsByGameType["map_option_disabled"] = new MBList<IAdminPanelMultiSelectionItem> { _disabledOption };
			_optionsByGameType["map_option_undecided"] = new MBList<IAdminPanelMultiSelectionItem> { _undecidedOption };
		}

		public AdminPanelUsableMapsOption BuildGameTypeOption(AdminPanelVotableMultiSelectionOption gameTypeOption)
		{
			_gameTypeOption = gameTypeOption;
			_gameTypeOption?.AddValueChangedCallback(UpdateOptions);
			UpdateOptions();
			return this;
		}

		public override void OnFinalize()
		{
			base.OnFinalize();
			_gameTypeOption?.RemoveValueChangedCallback(UpdateOptions);
			_gameTypeOption = null;
		}

		public override bool GetIsDisabled(out string reason)
		{
			if (_availableOptions.Count == 1 && _availableOptions[0] == _disabledOption)
			{
				reason = new TextObject("{=2WOGNYG4}No available maps added for game type").ToString();
				return true;
			}
			reason = string.Empty;
			return false;
		}

		private void UpdateOptions()
		{
			if (_isUpdatingOptions)
			{
				return;
			}
			_isUpdatingOptions = true;
			IAdminPanelMultiSelectionItem value = _gameTypeOption.GetValue();
			List<string> usableMaps = MultiplayerIntermissionVotingManager.Instance.GetUsableMaps(value.Value);
			FilterAvailableOptions(usableMaps);
			string key = (_gameTypeOption.IsUndecided ? "map_option_undecided" : ((usableMaps == null || usableMaps.Count <= 0) ? "map_option_disabled" : value.Value));
			if (_optionsByGameType.TryGetValue(key, out var value2))
			{
				if (!_availableOptions.SequenceEqual(value2))
				{
					BuildAvailableOptions(value2);
				}
				_isUpdatingOptions = false;
				return;
			}
			MBList<IAdminPanelMultiSelectionItem> mBList = new MBList<IAdminPanelMultiSelectionItem>();
			for (int i = 0; i < usableMaps.Count; i++)
			{
				AdminPanelMultiSelectionItem item = new AdminPanelMultiSelectionItem(usableMaps[i], null);
				mBList.Add(item);
			}
			BuildAvailableOptions(mBList);
			_optionsByGameType[key] = mBList;
			_isUpdatingOptions = false;
		}

		private void FilterAvailableOptions(List<string> availableOptions)
		{
			if (availableOptions.Count == 0)
			{
				return;
			}
			MBReadOnlyList<MultiplayerGameTypeInfo> multiplayerGameTypes = Module.CurrentModule.GetMultiplayerGameTypes();
			List<string> list = new List<string>();
			MultiplayerGameTypeInfo multiplayerGameTypeInfo = multiplayerGameTypes.FirstOrDefault((MultiplayerGameTypeInfo x) => x.GameType == _gameTypeOption.GetValue()?.Value);
			if (multiplayerGameTypeInfo == null)
			{
				return;
			}
			IEnumerable<string> source = multiplayerGameTypes.SelectMany((MultiplayerGameTypeInfo g) => g.Scenes);
			for (int num = 0; num < availableOptions.Count; num++)
			{
				string text = availableOptions[num];
				if (source.Contains(text) && !multiplayerGameTypeInfo.Scenes.Contains(text))
				{
					list.Add(text);
				}
			}
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				string item = list[num2];
				availableOptions.Remove(item);
			}
		}
	}

	private class AdminPanelStartMissionAction : AdminPanelAction
	{
		private MBReadOnlyList<IAdminPanelOptionGroup> _optionGroups;

		public AdminPanelStartMissionAction(string uniqueId)
			: base(uniqueId)
		{
		}

		public AdminPanelStartMissionAction BuildOptionGroups(MBReadOnlyList<IAdminPanelOptionGroup> optionGroups)
		{
			_optionGroups = optionGroups;
			return this;
		}

		public override bool GetIsDisabled(out string reason)
		{
			reason = string.Empty;
			if (_optionGroups != null)
			{
				for (int i = 0; i < _optionGroups.Count; i++)
				{
					for (int j = 0; j < _optionGroups[i].Options.Count; j++)
					{
						IAdminPanelOption adminPanelOption = _optionGroups[i].Options[j];
						if (adminPanelOption.IsRequired && adminPanelOption.GetIsAvailable() && adminPanelOption.GetIsDisabled(out var _))
						{
							reason = new TextObject("{=TrY4VS1R}Please select valid values for options.").ToString();
							return true;
						}
					}
				}
			}
			if (!MultiplayerIntermissionVotingManager.Instance.IsAutomatedBattleSwitchingEnabled)
			{
				reason = new TextObject("{=0WDSCBNa}Server does not support automated battle switching.").ToString();
				return true;
			}
			return false;
		}

		public override void OnFinalize()
		{
			base.OnFinalize();
			_optionGroups = null;
		}
	}

	private class AdminPanelGameTypeDependentNumericOption : AdminPanelNumericOption
	{
		private AdminPanelVotableMultiSelectionOption _gameTypeOption;

		private List<string> _invalidGameTypes;

		private List<string> _requiredGameTypes;

		public AdminPanelGameTypeDependentNumericOption(string uniqueId)
			: base(uniqueId)
		{
		}

		public override bool GetIsAvailable()
		{
			if (_gameTypeOption == null)
			{
				Debug.Print("Game type option is not set for game type dependent option: " + base.Name);
				Debug.FailedAssert("Game type option is not set for game type dependent option: " + base.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Admin\\DefaultAdminPanelOptionProvider.cs", "GetIsAvailable", 1063);
				return true;
			}
			if (_gameTypeOption.IsUndecided)
			{
				return true;
			}
			string value = _gameTypeOption.GetValue().Value;
			if (string.IsNullOrEmpty(value))
			{
				return true;
			}
			if (_invalidGameTypes != null)
			{
				return !_invalidGameTypes.Contains(value);
			}
			if (_requiredGameTypes != null)
			{
				return _requiredGameTypes.Contains(value);
			}
			return true;
		}

		public AdminPanelGameTypeDependentNumericOption BuildGameTypeOption(AdminPanelVotableMultiSelectionOption gameTypeOption)
		{
			_gameTypeOption = gameTypeOption;
			return this;
		}

		public AdminPanelGameTypeDependentNumericOption BuildInvalidGameTypes(string[] gameTypes)
		{
			_invalidGameTypes = new List<string>();
			if (gameTypes != null)
			{
				for (int i = 0; i < gameTypes.Length; i++)
				{
					_invalidGameTypes.Add(gameTypes[i]);
				}
			}
			return this;
		}

		public AdminPanelGameTypeDependentNumericOption BuildRequiredGameTypes(string[] gameTypes)
		{
			_requiredGameTypes = new List<string>();
			if (gameTypes != null)
			{
				for (int i = 0; i < gameTypes.Length; i++)
				{
					_requiredGameTypes.Add(gameTypes[i]);
				}
			}
			return this;
		}
	}

	private class AdminPanelGameTypeDependentAction : AdminPanelAction
	{
		private AdminPanelVotableMultiSelectionOption _gameTypeOption;

		private List<string> _invalidGameTypes;

		private List<string> _requiredGameTypes;

		public AdminPanelGameTypeDependentAction(string uniqueId)
			: base(uniqueId)
		{
		}

		public override bool GetIsAvailable()
		{
			if (_gameTypeOption == null)
			{
				Debug.Print("Game type option is not set for game type dependent option: " + base.Name);
				Debug.FailedAssert("Game type option is not set for game type dependent option: " + base.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Admin\\DefaultAdminPanelOptionProvider.cs", "GetIsAvailable", 1149);
				return true;
			}
			if (_gameTypeOption.IsUndecided)
			{
				return true;
			}
			string value = _gameTypeOption.GetValue().Value;
			if (string.IsNullOrEmpty(value))
			{
				return true;
			}
			if (_invalidGameTypes != null)
			{
				return !_invalidGameTypes.Contains(value);
			}
			if (_requiredGameTypes != null)
			{
				return _requiredGameTypes.Contains(value);
			}
			return true;
		}

		public AdminPanelGameTypeDependentAction BuildGameTypeOption(AdminPanelVotableMultiSelectionOption gameTypeOption)
		{
			_gameTypeOption = gameTypeOption;
			return this;
		}

		public AdminPanelGameTypeDependentAction BuildInvalidGameTypes(string[] gameTypes)
		{
			_invalidGameTypes = new List<string>();
			if (gameTypes != null)
			{
				for (int i = 0; i < gameTypes.Length; i++)
				{
					_invalidGameTypes.Add(gameTypes[i]);
				}
			}
			return this;
		}

		public AdminPanelGameTypeDependentAction BuildRequiredGameTypes(string[] gameTypes)
		{
			_requiredGameTypes = new List<string>();
			if (gameTypes != null)
			{
				for (int i = 0; i < gameTypes.Length; i++)
				{
					_requiredGameTypes.Add(gameTypes[i]);
				}
			}
			return this;
		}
	}

	private readonly MultiplayerAdminComponent _multiplayerAdminComponent;

	private readonly MissionLobbyComponent _missionLobbyComponent;

	private MBList<IAdminPanelOptionGroup> _optionGroups;

	private AdminPanelVotableMultiSelectionOption _gameTypeOption;

	public DefaultAdminPanelOptionProvider(MultiplayerAdminComponent adminComponent, MissionLobbyComponent missionLobbyComponent)
	{
		_multiplayerAdminComponent = adminComponent;
		_missionLobbyComponent = missionLobbyComponent;
		_optionGroups = new MBList<IAdminPanelOptionGroup>();
	}

	public void OnTick(float dt)
	{
		for (int i = 0; i < _optionGroups.Count; i++)
		{
			if (_optionGroups[i] is IAdminPanelTickable adminPanelTickable)
			{
				adminPanelTickable.OnTick(dt);
			}
		}
	}

	public void OnFinalize()
	{
		if (_optionGroups != null)
		{
			for (int i = 0; i < _optionGroups.Count; i++)
			{
				_optionGroups[i].OnFinalize();
			}
		}
		_gameTypeOption = null;
	}

	public IAdminPanelOption GetOptionWithId(string id)
	{
		foreach (IAdminPanelOptionGroup optionGroup in _optionGroups)
		{
			foreach (IAdminPanelOption option in optionGroup.Options)
			{
				if (option.UniqueId == id)
				{
					return option;
				}
			}
		}
		return null;
	}

	public IAdminPanelAction GetActionWithId(string id)
	{
		foreach (IAdminPanelOptionGroup optionGroup in _optionGroups)
		{
			foreach (IAdminPanelAction action in optionGroup.Actions)
			{
				if (action.UniqueId == id)
				{
					return action;
				}
			}
		}
		return null;
	}

	public void ApplyOptions()
	{
		AdminUpdateMultiplayerOptions adminUpdateMultiplayerOptions = new AdminUpdateMultiplayerOptions();
		IEnumerable<IAdminPanelOption> enumerable = _optionGroups.SelectMany((IAdminPanelOptionGroup x) => x.Options);
		foreach (IAdminPanelOption item in enumerable)
		{
			if (!(item is IAdminPanelOptionInternal adminPanelOptionInternal))
			{
				continue;
			}
			MultiplayerOptions.OptionType optionType = adminPanelOptionInternal.GetOptionType();
			MultiplayerOptions.MultiplayerOptionsAccessMode optionAccessMode = adminPanelOptionInternal.GetOptionAccessMode();
			if (optionType != MultiplayerOptions.OptionType.NumOfSlots && optionAccessMode != MultiplayerOptions.MultiplayerOptionsAccessMode.NumAccessModes)
			{
				if (item is IAdminPanelOption<bool> adminPanelOption)
				{
					adminUpdateMultiplayerOptions.AddMultiplayerOption(optionType, optionAccessMode, adminPanelOption.GetValue());
				}
				if (item is IAdminPanelOption<int> adminPanelOption2)
				{
					adminUpdateMultiplayerOptions.AddMultiplayerOption(optionType, optionAccessMode, adminPanelOption2.GetValue());
				}
				if (item is IAdminPanelOption<string> adminPanelOption3)
				{
					adminUpdateMultiplayerOptions.AddMultiplayerOption(optionType, optionAccessMode, adminPanelOption3.GetValue());
				}
				if (item is IAdminPanelMultiSelectionOption adminPanelMultiSelectionOption)
				{
					adminUpdateMultiplayerOptions.AddMultiplayerOption(optionType, optionAccessMode, adminPanelMultiSelectionOption.GetValue().Value);
				}
			}
		}
		GameNetwork.BeginModuleEventAsClient();
		GameNetwork.WriteMessage(adminUpdateMultiplayerOptions);
		GameNetwork.EndModuleEventAsClient();
		foreach (IAdminPanelOption item2 in enumerable)
		{
			if (item2 is IAdminPanelOptionInternal adminPanelOptionInternal2)
			{
				adminPanelOptionInternal2.OnApplyChanges();
			}
		}
	}

	public MBReadOnlyList<IAdminPanelOptionGroup> GetOptionGroups()
	{
		_optionGroups.Clear();
		if (MultiplayerIntermissionVotingManager.Instance.IsAutomatedBattleSwitchingEnabled)
		{
			_optionGroups.Add(GetMissionOptions());
		}
		_optionGroups.Add(GetImmediateEffectOptions());
		_optionGroups.Add(GetActions());
		return _optionGroups;
	}

	private T GetValueFromOption<T>(string optionId)
	{
		if (((IAdminPanelOptionProvider)this).GetOptionWithId(optionId) is IAdminPanelOption<T> adminPanelOption)
		{
			return adminPanelOption.GetValue();
		}
		Debug.FailedAssert($"Failed to find \"{typeof(T)}\" type option with id: {optionId}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Admin\\DefaultAdminPanelOptionProvider.cs", "GetValueFromOption", 193);
		return default(T);
	}

	private AdminPanelOptionGroup GetMissionOptions()
	{
		AdminPanelOptionGroup adminPanelOptionGroup = new AdminPanelOptionGroup("mission_options", new TextObject("{=xa8i1dM1}Mission Options"), requiresRestart: true);
		AdminPanelOption<IAdminPanelMultiSelectionItem> adminPanelOption = new AdminPanelVotableMultiSelectionOption("next_game_type").BuildAvailableOptions(MultiplayerOptions.OptionType.GameType).BuildOptionType(MultiplayerOptions.OptionType.GameType, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions, buildDefaultValue: false, buildInitialValue: false).BuildName(new TextObject("{=JPimShCw}Game Type"))
			.BuildDescription(new TextObject("{=ueFrMu6i}Next game type."))
			.BuildIsRequired(isRequired: true);
		_gameTypeOption = adminPanelOption as AdminPanelVotableMultiSelectionOption;
		adminPanelOptionGroup.AddOption(adminPanelOption);
		adminPanelOptionGroup.AddOption(new AdminPanelUsableMapsOption("next_map").BuildGameTypeOption(adminPanelOption as AdminPanelVotableMultiSelectionOption).BuildOptionType(MultiplayerOptions.OptionType.Map, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions, buildDefaultValue: false, buildInitialValue: false).BuildName(new TextObject("{=w9m11T1y}Map"))
			.BuildDescription(new TextObject("{=ok1CD7dH}Next map to play."))
			.BuildIsRequired(isRequired: true));
		AdminPanelCultureOption adminPanelCultureOption = new AdminPanelCultureOption("next_culture_team_1").BuildAvailableOptions(MultiplayerOptions.OptionType.CultureTeam1).BuildOptionType(MultiplayerOptions.OptionType.CultureTeam1, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions, buildDefaultValue: false, buildInitialValue: false).BuildName(new TextObject("{=sGDo0mxT}Attacker Culture"))
			.BuildDescription(new TextObject("{=wsOUaxf4}Culture of the attacker team in the next game."))
			.BuildIsRequired(isRequired: true) as AdminPanelCultureOption;
		AdminPanelCultureOption adminPanelCultureOption2 = new AdminPanelCultureOption("next_culture_team_2").BuildAvailableOptions(MultiplayerOptions.OptionType.CultureTeam2).BuildOptionType(MultiplayerOptions.OptionType.CultureTeam2, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions, buildDefaultValue: false, buildInitialValue: false).BuildName(new TextObject("{=CeERJpan}Defender Culture"))
			.BuildDescription(new TextObject("{=0jMXI0qT}Culture of the defender team in the next game."))
			.BuildIsRequired(isRequired: true) as AdminPanelCultureOption;
		adminPanelCultureOption.BuildOtherCultureOption(adminPanelCultureOption2);
		adminPanelCultureOption2.BuildOtherCultureOption(adminPanelCultureOption);
		adminPanelOptionGroup.AddOption(adminPanelCultureOption);
		adminPanelOptionGroup.AddOption(adminPanelCultureOption2);
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("next_number_of_rounds").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[3]
		{
			MultiplayerGameType.TeamDeathmatch.ToString(),
			MultiplayerGameType.Duel.ToString(),
			MultiplayerGameType.Siege.ToString()
		}).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.RoundTotal)
			.BuildOptionType(MultiplayerOptions.OptionType.RoundTotal, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions)
			.BuildName(new TextObject("{=VwveHldM}Number of Rounds"))
			.BuildDescription(new TextObject("{=ndCjGgEj}Total number of rounds in the next game."))
			.BuildIsRequired(isRequired: true));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("next_min_score_to_win_duel").BuildGameTypeOption(_gameTypeOption).BuildRequiredGameTypes(new string[1] { MultiplayerGameType.Duel.ToString() }).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.MinScoreToWinDuel)
			.BuildOptionType(MultiplayerOptions.OptionType.MinScoreToWinDuel, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions)
			.BuildName(new TextObject("{=JISyGr4E}Minimum Score to Win Duel"))
			.BuildDescription(new TextObject("{=5V30jDb7}Minimum score required to win duels."))
			.BuildIsRequired(isRequired: true));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("next_map_time_limit").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.MapTimeLimit).BuildOptionType(MultiplayerOptions.OptionType.MapTimeLimit, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions).BuildName(new TextObject("{=lf1eQ0tB}Map Time Limit"))
			.BuildDescription(new TextObject("{=xgps8dXU}Time limit in the next game."))
			.BuildIsRequired(isRequired: true));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("next_round_time_limit").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[3]
		{
			MultiplayerGameType.TeamDeathmatch.ToString(),
			MultiplayerGameType.Duel.ToString(),
			MultiplayerGameType.Siege.ToString()
		}).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.RoundTimeLimit)
			.BuildOptionType(MultiplayerOptions.OptionType.RoundTimeLimit, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions)
			.BuildName(new TextObject("{=9k0H0xu0}Round Time Limit"))
			.BuildDescription(new TextObject("{=ApQhQe6u}Round time limit in the next game."))
			.BuildIsRequired(isRequired: true));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("next_warmup_time_limit").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[2]
		{
			MultiplayerGameType.TeamDeathmatch.ToString(),
			MultiplayerGameType.Duel.ToString()
		}).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds)
			.BuildOptionType(MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions)
			.BuildName(new TextObject("{=XwZTiF8l}Warmup Time Limit"))
			.BuildDescription(new TextObject("{=S5Ayobba}Warmup time limit in the next game."))
			.BuildIsRequired(isRequired: true));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("next_max_num_players").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.MaxNumberOfPlayers).BuildOptionType(MultiplayerOptions.OptionType.MaxNumberOfPlayers, MultiplayerOptions.MultiplayerOptionsAccessMode.NextMapOptions).BuildName(new TextObject("{=tzcK3R0v}Maximum Number of Players"))
			.BuildDescription(new TextObject("{=RENeJbg5}Maximum number of players in the next game."))
			.BuildIsRequired(isRequired: true));
		adminPanelOptionGroup.AddAction(new AdminPanelStartMissionAction("apply_and_start").BuildOptionGroups(_optionGroups).BuildName(new TextObject("{=kwo09aDm}Apply and Start Mission")).BuildDescription(new TextObject("{=8D8KuKxk}Apply all changes and start a new mission."))
			.BuildOnActionExecutedCallback(delegate
			{
				ApplyOptions();
				_multiplayerAdminComponent.ChangeAdminMenuActiveState(isActive: false);
				_multiplayerAdminComponent.AdminEndMission();
			}));
		return adminPanelOptionGroup;
	}

	private AdminPanelOptionGroup GetImmediateEffectOptions()
	{
		AdminPanelOptionGroup adminPanelOptionGroup = new AdminPanelOptionGroup("immediate_effects", new TextObject("{=TcBcNdSE}Immediate Effects"));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<string>("welcome_message").BuildOptionType(MultiplayerOptions.OptionType.WelcomeMessage).BuildName(new TextObject("{=t2Oh6uty}Welcome Message")).BuildDescription(new TextObject("{=v1DiZaoK}Change the server welcome message.")));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("auto_balance_treshold").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[1] { MultiplayerGameType.Duel.ToString() }).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.AutoTeamBalanceThreshold)
			.BuildOptionType(MultiplayerOptions.OptionType.AutoTeamBalanceThreshold)
			.BuildName(new TextObject("{=YdnTEREg}Team Balance Threshold"))
			.BuildDescription(new TextObject("{=DenCZPAg}Change the team balance threshold value.")));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("friendly_fire_melee_percent").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[1] { MultiplayerGameType.Duel.ToString() }).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeFriendPercent)
			.BuildOptionType(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeFriendPercent)
			.BuildName(new TextObject("{=VpQZquwB}Friendly Melee Damage"))
			.BuildDescription(new TextObject("{=3HgzxHqT}Change the value of friendly melee damage.")));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("friendly_fire_melee_self_percent").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[1] { MultiplayerGameType.Duel.ToString() }).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeSelfPercent)
			.BuildOptionType(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeSelfPercent)
			.BuildName(new TextObject("{=wLTiwbBt}Friendly Reflective Melee Damage"))
			.BuildDescription(new TextObject("{=daq8AjgZ}Change the value of reflective friendly melee damage.")));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("friendly_fire_ranged_percent").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[1] { MultiplayerGameType.Duel.ToString() }).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent)
			.BuildOptionType(MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent)
			.BuildName(new TextObject("{=pzudHx88}Friendly Ranged Damage"))
			.BuildDescription(new TextObject("{=0H1Pg2RF}Change the value of friendly ranged damage.")));
		adminPanelOptionGroup.AddOption(new AdminPanelGameTypeDependentNumericOption("friendly_fire_ranged_self_percent").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[1] { MultiplayerGameType.Duel.ToString() }).SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent)
			.BuildOptionType(MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent)
			.BuildName(new TextObject("{=ZYw87dlh}Friendly Reflective Ranged Damage"))
			.BuildDescription(new TextObject("{=ih2t4B8E}Change the value of reflective friendly ranged damage.")));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("allow_infantry").BuildName(new TextObject("{=H72xVNwz}Allow Infantry")).BuildDescription(new TextObject("{=FB9tHuWF}Allow usage of infantry troops in game.")).BuildDefaultValue(_missionLobbyComponent.IsClassAvailable(FormationClass.Infantry))
			.BuildInitialValue(_missionLobbyComponent.IsClassAvailable(FormationClass.Infantry))
			.BuildOnAppliedCallback(delegate(bool val)
			{
				_multiplayerAdminComponent.ChangeClassRestriction(FormationClass.Infantry, !val);
			}));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("allow_ranged").BuildName(new TextObject("{=wFlbhObU}Allow Archers")).BuildDescription(new TextObject("{=3MiLBVAH}Allow usage of archer troops in game.")).BuildDefaultValue(_missionLobbyComponent.IsClassAvailable(FormationClass.Ranged))
			.BuildInitialValue(_missionLobbyComponent.IsClassAvailable(FormationClass.Ranged))
			.BuildOnAppliedCallback(delegate(bool val)
			{
				_multiplayerAdminComponent.ChangeClassRestriction(FormationClass.Ranged, !val);
			}));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("allow_cavalry").BuildName(new TextObject("{=nboyCQpj}Allow Cavalry")).BuildDescription(new TextObject("{=iTZkSZXI}Allow usage of cavalry troops in game.")).BuildDefaultValue(_missionLobbyComponent.IsClassAvailable(FormationClass.Cavalry))
			.BuildInitialValue(_missionLobbyComponent.IsClassAvailable(FormationClass.Cavalry))
			.BuildOnAppliedCallback(delegate(bool val)
			{
				_multiplayerAdminComponent.ChangeClassRestriction(FormationClass.Cavalry, !val);
			}));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("allow_horse_archers").BuildName(new TextObject("{=6yTHziN5}Allow Horse Archers")).BuildDescription(new TextObject("{=P8dk4qSf}Allow usage of horse archer troops in game.")).BuildDefaultValue(_missionLobbyComponent.IsClassAvailable(FormationClass.HorseArcher))
			.BuildInitialValue(_missionLobbyComponent.IsClassAvailable(FormationClass.HorseArcher))
			.BuildOnAppliedCallback(delegate(bool val)
			{
				_multiplayerAdminComponent.ChangeClassRestriction(FormationClass.HorseArcher, !val);
			}));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("enable_spectators").BuildOptionType(MultiplayerOptions.OptionType.EnableSpectators).BuildName(new TextObject("{=BAP7bTOd}Allow Spectators")).BuildDescription(new TextObject("{=Ws9noGZk}Allow players to join this server as spectators.")));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("streamer_mode_enabled").BuildOptionType(MultiplayerOptions.OptionType.StreamerModeEnabled).BuildName(new TextObject("{=R96gmaky}Streamer Mode")).BuildDescription(new TextObject("{=uwBnWIsX}Enable the dedicated streamer overlay (all-players overlay and through-wall silhouettes) for spectators.")));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("formation_targeting_visibility_mode").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FormationTargetingVisibilityMode).BuildOptionType(MultiplayerOptions.OptionType.FormationTargetingVisibilityMode).BuildName(new TextObject("{=ftvis_admin_mode}Formation Targeting Visibility Mode"))
			.BuildDescription(new TextObject("{=ftvis_admin_mode_desc}Captain only. Visibility rule that must be satisfied before an enemy formation can be focused for targeting. 0 = Disabled, 1 = Percentage of visible troops, 2 = Absolute count of visible troops.")));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("formation_targeting_visibility_threshold").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FormationTargetingVisibilityThreshold).BuildOptionType(MultiplayerOptions.OptionType.FormationTargetingVisibilityThreshold).BuildName(new TextObject("{=ftvis_admin_threshold}Formation Targeting Visibility Threshold"))
			.BuildDescription(new TextObject("{=ftvis_admin_threshold_desc}Captain only. Threshold for the formation targeting visibility rule. In Percentage mode: minimum percent (0-100) of sampled troops that must be visible. In AbsoluteCount mode: minimum estimated visible troop count.")));
		adminPanelOptionGroup.AddOption(new AdminPanelOption<bool>("formation_targeting_visibility_close_range").BuildOptionType(MultiplayerOptions.OptionType.FormationTargetingVisibilityAppliesAtCloseRange).BuildName(new TextObject("{=ftvis_admin_closerange}Formation Targeting Visibility At Close Range")).BuildDescription(new TextObject("{=ftvis_admin_closerange_desc}If enabled, the formation targeting visibility rule also applies to formations within close range (under 10 meters).")));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("formation_marker_far_distance_cutoff").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FormationMarkerFarDistanceCutoff).BuildOptionType(MultiplayerOptions.OptionType.FormationMarkerFarDistanceCutoff).BuildName(new TextObject("{=fmark_admin_far_distance}Formation Marker Far Distance Cutoff"))
			.BuildDescription(new TextObject("{=fmark_admin_far_distance_desc}Captain only. Distance in metres beyond which the formation marker is drawn at the far alpha target. The marker fades from full alpha at close range to that target at this distance. -1 uses the default.")));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("formation_marker_far_alpha_target").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FormationMarkerFarAlphaTarget).BuildOptionType(MultiplayerOptions.OptionType.FormationMarkerFarAlphaTarget).BuildName(new TextObject("{=fmark_admin_far_alpha}Formation Marker Far Alpha Target"))
			.BuildDescription(new TextObject("{=fmark_admin_far_alpha_desc}Captain only. Opacity of the formation marker at and beyond the far distance cutoff, as a percentage (0-100, where 0 hides the marker completely). -1 uses the default.")));
		adminPanelOptionGroup.AddOption(new AdminPanelNumericOption("formation_marker_always_on_distance").SetMinimumAndMaximumFrom(MultiplayerOptions.OptionType.FormationMarkerAlwaysOnDistance).BuildOptionType(MultiplayerOptions.OptionType.FormationMarkerAlwaysOnDistance).BuildName(new TextObject("{=fmark_admin_always_on}Formation Marker Always On Distance"))
			.BuildDescription(new TextObject("{=fmark_admin_always_on_desc}Captain only. Distance in metres within which a visible formation marker is always drawn at full opacity. Beyond it the marker fades toward the far alpha target. -1 uses the default.")));
		return adminPanelOptionGroup;
	}

	private AdminPanelOptionGroup GetActions()
	{
		AdminPanelOptionGroup adminPanelOptionGroup = new AdminPanelOptionGroup("actions", new TextObject("{=Za3U3MY4}Actions"));
		adminPanelOptionGroup.AddAction(new AdminPanelGameTypeDependentAction("end_warmup").BuildGameTypeOption(_gameTypeOption).BuildInvalidGameTypes(new string[2]
		{
			MultiplayerGameType.TeamDeathmatch.ToString(),
			MultiplayerGameType.Duel.ToString()
		}).BuildName(new TextObject("{=AVDDCWhv}End Warmup"))
			.BuildDescription(new TextObject("{=Q6HPNb6Q}Set warmup timer to maximum of 30 seconds."))
			.BuildOnActionExecutedCallback(delegate
			{
				_multiplayerAdminComponent.EndWarmup();
			}));
		adminPanelOptionGroup.AddAction(new AdminPanelAction("mute_player").BuildName(new TextObject("{=QvxOnnZg}Mute Players")).BuildDescription(new TextObject("{=qMJsMUtO}Select players to mute.")).BuildOnActionExecutedCallback(delegate
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				if (!MultiplayerGlobalMutedPlayersManager.IsUserMuted(networkPeer.VirtualPlayer.Id))
				{
					list.Add(new InquiryElement(networkPeer, networkPeer.UserName, null));
				}
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=QvxOnnZg}Mute Players").ToString(), new TextObject("{=qMJsMUtO}Select players to mute.").ToString(), list, isExitShown: true, 0, 1, new TextObject("{=SfJgnzdq}Mute").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(List<InquiryElement> selectedPlayers)
			{
				if (selectedPlayers != null && selectedPlayers.Count == 1)
				{
					NetworkCommunicator networkCommunicator = (NetworkCommunicator)selectedPlayers[0].Identifier;
					if (networkCommunicator != null)
					{
						_multiplayerAdminComponent.GlobalMuteUnmutePlayer(networkCommunicator, unmute: false);
					}
				}
			}, null, string.Empty, isSeachAvailable: true));
		}));
		adminPanelOptionGroup.AddAction(new AdminPanelAction("mute_player").BuildName(new TextObject("{=NkDBzEzd}Unmute Players")).BuildDescription(new TextObject("{=9zJaIpIZ}Select players to unmute.")).BuildOnActionExecutedCallback(delegate
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
			{
				if (MultiplayerGlobalMutedPlayersManager.IsUserMuted(networkPeer2.VirtualPlayer.Id))
				{
					list.Add(new InquiryElement(networkPeer2, networkPeer2.UserName, null));
				}
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=NkDBzEzd}Unmute Players").ToString(), new TextObject("{=9zJaIpIZ}Select players to unmute.").ToString(), list, isExitShown: true, 0, 1, new TextObject("{=HyG3eUFN}Unmute").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(List<InquiryElement> selectedPlayers)
			{
				if (selectedPlayers != null && selectedPlayers.Count == 1)
				{
					NetworkCommunicator networkCommunicator = (NetworkCommunicator)selectedPlayers[0].Identifier;
					if (networkCommunicator != null)
					{
						_multiplayerAdminComponent.GlobalMuteUnmutePlayer(networkCommunicator, unmute: true);
					}
				}
			}, null, string.Empty, isSeachAvailable: true));
		}));
		adminPanelOptionGroup.AddAction(new AdminPanelAction("kick_player").BuildName(new TextObject("{=cPbHqGrI}Kick Player")).BuildDescription(new TextObject("{=lZxxVl17}Select a player to kick.")).BuildOnActionExecutedCallback(delegate
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (NetworkCommunicator networkPeer3 in GameNetwork.NetworkPeers)
			{
				list.Add(new InquiryElement(networkPeer3, networkPeer3.UserName, null));
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=cPbHqGrI}Kick Player").ToString(), new TextObject("{=RKNTl0Tn}Select player to kick").ToString(), list, isExitShown: true, 0, 1, new TextObject("{=DdOgvhsV}Kick").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(List<InquiryElement> selectedPlayers)
			{
				if (selectedPlayers != null && selectedPlayers.Count == 1)
				{
					NetworkCommunicator networkCommunicator = (NetworkCommunicator)selectedPlayers[0].Identifier;
					if (networkCommunicator != null)
					{
						_multiplayerAdminComponent.KickPlayer(networkCommunicator, banPlayer: false);
					}
				}
			}, null, string.Empty, isSeachAvailable: true));
		}));
		adminPanelOptionGroup.AddAction(new AdminPanelAction("ban_player").BuildName(new TextObject("{=pbp0GQdO}Ban Player")).BuildDescription(new TextObject("{=aJGlM29l}Select a player to ban.")).BuildOnActionExecutedCallback(delegate
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (NetworkCommunicator networkPeer4 in GameNetwork.NetworkPeers)
			{
				list.Add(new InquiryElement(networkPeer4, networkPeer4.UserName, null));
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=pbp0GQdO}Ban Player").ToString(), new TextObject("{=jw2VQYeK}Select player to ban").ToString(), list, isExitShown: true, 0, 1, new TextObject("{=HjqcmY6X}Ban").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), delegate(List<InquiryElement> selectedPlayers)
			{
				if (selectedPlayers != null && selectedPlayers.Count == 1)
				{
					NetworkCommunicator networkCommunicator = (NetworkCommunicator)selectedPlayers[0].Identifier;
					if (networkCommunicator != null)
					{
						_multiplayerAdminComponent.KickPlayer(networkCommunicator, banPlayer: true);
					}
				}
			}, null, string.Empty, isSeachAvailable: true));
		}));
		return adminPanelOptionGroup;
	}
}
