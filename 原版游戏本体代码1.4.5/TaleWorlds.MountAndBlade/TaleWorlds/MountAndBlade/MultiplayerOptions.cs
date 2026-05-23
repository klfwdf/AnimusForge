using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerOptions
{
	public enum MultiplayerOptionsAccessMode
	{
		DefaultMapOptions,
		CurrentMapOptions,
		NextMapOptions,
		NumAccessModes
	}

	public enum OptionValueType
	{
		Bool,
		Integer,
		Enum,
		String
	}

	public enum OptionType
	{
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Changes the name of the server in the server list", 0, 0, null, false, null)]
		ServerName,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Welcome messages which is shown to all players when they enter the server.", 0, 0, null, false, null)]
		WelcomeMessage,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.Never, "Sets a password that clients have to enter before connecting to the server.", 0, 0, null, false, null)]
		GamePassword,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.Never, "Sets a password that allows players access to admin tools during the game.", 0, 0, null, false, null)]
		AdminPassword,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Never, "Sets ID of the private game definition.", int.MinValue, int.MaxValue, null, false, null)]
		GameDefinitionId,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Allow players to start polls to kick other players.", 0, 0, null, false, null)]
		AllowPollsToKickPlayers,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Allow players to start polls to ban other players.", 0, 0, null, false, null)]
		AllowPollsToBanPlayers,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Allow players to start polls to change the current map.", 0, 0, null, false, null)]
		AllowPollsToChangeMaps,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Allow players to use their custom banner.", 0, 0, null, false, null)]
		AllowIndividualBanners,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Use animation progress dependent blocking.", 0, 0, null, false, null)]
		UseRealisticBlocking,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Changes the game type.", 0, 0, null, true, null)]
		PremadeMatchGameMode,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Changes the game type.", 0, 0, null, true, null)]
		GameType,
		[MultiplayerOptionsProperty(OptionValueType.Enum, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Type of the premade game.", 0, 1, null, true, typeof(PremadeGameType))]
		PremadeGameType,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Map of the game.", 0, 0, null, true, null)]
		Map,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Sets culture for team 1", 0, 0, null, true, null)]
		CultureTeam1,
		[MultiplayerOptionsProperty(OptionValueType.String, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Sets culture for team 2", 0, 0, null, true, null)]
		CultureTeam2,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Set the maximum amount of player allowed on the server.", 1, 1023, null, false, null)]
		MaxNumberOfPlayers,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Set the amount of players that are needed to start the first round. If not met, players will just wait.", 0, 20, null, false, null)]
		MinNumberOfPlayersForMatchStart,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Amount of bots on team 1", 0, 510, null, false, null)]
		NumberOfBotsTeam1,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Amount of bots on team 2", 0, 510, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege", "TeamDeathmatch" }, false, null)]
		NumberOfBotsTeam2,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Amount of bots per formation", 0, 100, new string[] { "Captain" }, false, null)]
		NumberOfBotsPerFormation,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "A percentage of how much melee damage inflicted upon a friend is dealt back to the inflictor.", 0, 2000, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege", "TeamDeathmatch" }, false, null)]
		FriendlyFireDamageMeleeSelfPercent,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "A percentage of how much melee damage inflicted upon a friend is actually dealt.", 0, 2000, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege", "TeamDeathmatch" }, false, null)]
		FriendlyFireDamageMeleeFriendPercent,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "A percentage of how much ranged damage inflicted upon a friend is dealt back to the inflictor.", 0, 2000, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege", "TeamDeathmatch" }, false, null)]
		FriendlyFireDamageRangedSelfPercent,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "A percentage of how much ranged damage inflicted upon a friend is actually dealt.", 0, 2000, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege", "TeamDeathmatch" }, false, null)]
		FriendlyFireDamageRangedFriendPercent,
		[MultiplayerOptionsProperty(OptionValueType.Enum, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Who can spectators look at, and how.", 0, 7, null, true, typeof(SpectatorCameraTypes))]
		SpectatorCamera,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Maximum duration for the warmup. In seconds.", 60, 3600, null, false, null)]
		WarmupTimeLimitInSeconds,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Maximum duration for the map. In minutes.", 1, 60, null, false, null)]
		MapTimeLimit,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Maximum duration for each round. In seconds.", 60, 3600, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege" }, false, null)]
		RoundTimeLimit,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Time available to select class/equipment. In seconds.", 2, 60, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege" }, false, null)]
		RoundPreparationTimeLimit,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Maximum amount of rounds before the game ends.", 1, 99, new string[] { "Battle", "NewBattle", "ClassicBattle", "Captain", "Skirmish", "Siege" }, false, null)]
		RoundTotal,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Wait time after death, before respawning again. In seconds.", 1, 60, new string[] { "Siege" }, false, null)]
		RespawnPeriodTeam1,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Wait time after death, before respawning again. In seconds.", 1, 60, new string[] { "Siege" }, false, null)]
		RespawnPeriodTeam2,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Unlimited gold option.", 0, 0, new string[] { "Battle", "Skirmish", "Siege", "TeamDeathmatch" }, false, null)]
		UnlimitedGold,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Gold gain multiplier from agent deaths.", -100, 100, new string[] { "Siege", "TeamDeathmatch" }, false, null)]
		GoldGainChangePercentageTeam1,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Gold gain multiplier from agent deaths.", -100, 100, new string[] { "Siege", "TeamDeathmatch" }, false, null)]
		GoldGainChangePercentageTeam2,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Min score to win match.", 0, 1023000, new string[] { "TeamDeathmatch" }, false, null)]
		MinScoreToWinMatch,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Min score to win duel.", 0, 7, new string[] { "Duel" }, false, null)]
		MinScoreToWinDuel,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Minimum needed difference in poll results before it is accepted.", 0, 10, null, false, null)]
		PollAcceptThreshold,
		[MultiplayerOptionsProperty(OptionValueType.Integer, MultiplayerOptionsProperty.ReplicationOccurrence.Immediately, "Maximum player imbalance between team 1 and team 2. Selecting 0 will disable auto team balancing.", 0, 30, null, false, null)]
		AutoTeamBalanceThreshold,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Enables mission recording.", 0, 0, null, false, null)]
		EnableMissionRecording,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Sets if the game mode uses single spawning.", 0, 0, null, false, null)]
		SingleSpawn,
		[MultiplayerOptionsProperty(OptionValueType.Bool, MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad, "Disables the inactivity kick timer.", 0, 0, null, false, null)]
		DisableInactivityKick,
		NumOfSlots
	}

	public enum OptionsCategory
	{
		Default,
		PremadeMatch
	}

	public class MultiplayerOption
	{
		private struct IntegerValue
		{
			public static IntegerValue Invalid => default(IntegerValue);

			public bool IsValid { get; private set; }

			public int Value { get; private set; }

			public static IntegerValue Create()
			{
				return new IntegerValue
				{
					IsValid = true
				};
			}

			public void UpdateValue(int value)
			{
				Value = value;
			}
		}

		private struct StringValue
		{
			public static StringValue Invalid => default(StringValue);

			public bool IsValid { get; private set; }

			public string Value { get; private set; }

			public static StringValue Create()
			{
				return new StringValue
				{
					IsValid = true
				};
			}

			public void UpdateValue(string value)
			{
				Value = value;
			}
		}

		public readonly OptionType OptionType;

		private IntegerValue _intValue;

		private StringValue _stringValue;

		public static MultiplayerOption CreateMultiplayerOption(OptionType optionType)
		{
			return new MultiplayerOption(optionType);
		}

		public static MultiplayerOption CopyMultiplayerOption(MultiplayerOption option)
		{
			return new MultiplayerOption(option.OptionType)
			{
				_intValue = option._intValue,
				_stringValue = option._stringValue
			};
		}

		private MultiplayerOption(OptionType optionType)
		{
			OptionType = optionType;
			if (optionType.GetOptionProperty().OptionValueType == OptionValueType.String)
			{
				_intValue = IntegerValue.Invalid;
				_stringValue = StringValue.Create();
			}
			else
			{
				_intValue = IntegerValue.Create();
				_stringValue = StringValue.Invalid;
			}
		}

		public MultiplayerOption UpdateValue(bool value)
		{
			UpdateValue(value ? 1 : 0);
			return this;
		}

		public MultiplayerOption UpdateValue(int value)
		{
			_intValue.UpdateValue(value);
			return this;
		}

		public MultiplayerOption UpdateValue(string value)
		{
			_stringValue.UpdateValue(value);
			return this;
		}

		public void GetValue(out bool value)
		{
			value = _intValue.Value == 1;
		}

		public void GetValue(out int value)
		{
			value = _intValue.Value;
		}

		public void GetValue(out string value)
		{
			value = _stringValue.Value;
		}
	}

	private class MultiplayerOptionsContainer
	{
		private readonly MultiplayerOption[] _multiplayerOptions;

		public MultiplayerOptionsContainer()
		{
			_multiplayerOptions = new MultiplayerOption[43];
		}

		public MultiplayerOption GetOptionFromOptionType(OptionType optionType)
		{
			return _multiplayerOptions[(int)optionType];
		}

		private void CopyOptionFromOther(OptionType optionType, MultiplayerOption option)
		{
			_multiplayerOptions[(int)optionType] = MultiplayerOption.CopyMultiplayerOption(option);
		}

		public void CreateOption(OptionType optionType)
		{
			_multiplayerOptions[(int)optionType] = MultiplayerOption.CreateMultiplayerOption(optionType);
		}

		public void UpdateOptionValue(OptionType optionType, int value)
		{
			_multiplayerOptions[(int)optionType].UpdateValue(value);
		}

		public void UpdateOptionValue(OptionType optionType, string value)
		{
			_multiplayerOptions[(int)optionType].UpdateValue(value);
		}

		public void UpdateOptionValue(OptionType optionType, bool value)
		{
			_multiplayerOptions[(int)optionType].UpdateValue(value ? 1 : 0);
		}

		public void CopyAllValuesTo(MultiplayerOptionsContainer other)
		{
			for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
			{
				other.CopyOptionFromOther(optionType, _multiplayerOptions[(int)optionType]);
			}
		}
	}

	private const int PlayerCountLimitMin = 1;

	private const int PlayerCountLimitMax = 1023;

	private const int PlayerCountLimitForMatchStartMin = 0;

	private const int PlayerCountLimitForMatchStartMax = 20;

	private const int MapTimeLimitMin = 1;

	private const int MapTimeLimitMax = 60;

	private const int WarmupTimeLimitMin = 60;

	private const int WarmupTimeLimitMax = 3600;

	private const int RoundLimitMin = 1;

	private const int RoundLimitMax = 99;

	private const int RoundTimeLimitMin = 60;

	private const int RoundTimeLimitMax = 3600;

	private const int RoundPreparationTimeLimitMin = 2;

	private const int RoundPreparationTimeLimitMax = 60;

	private const int RespawnPeriodMin = 1;

	private const int RespawnPeriodMax = 60;

	private const int GoldGainChangePercentageMin = -100;

	private const int GoldGainChangePercentageMax = 100;

	private const int PollAcceptThresholdMin = 0;

	private const int PollAcceptThresholdMax = 10;

	private const int BotsPerTeamLimitMin = 0;

	private const int BotsPerTeamLimitMax = 510;

	private const int BotsPerFormationLimitMin = 0;

	private const int BotsPerFormationLimitMax = 100;

	private const int FriendlyFireDamagePercentMin = 0;

	private const int FriendlyFireDamagePercentMax = 2000;

	private const int GameDefinitionIdMin = int.MinValue;

	private const int GameDefinitionIdMax = int.MaxValue;

	private const int MaxScoreToEndDuel = 7;

	private static MultiplayerOptions _instance;

	private readonly MultiplayerOptionsContainer _default;

	private readonly MultiplayerOptionsContainer _current;

	private readonly MultiplayerOptionsContainer _next;

	public OptionsCategory CurrentOptionsCategory;

	public static MultiplayerOptions Instance => _instance ?? (_instance = new MultiplayerOptions());

	public MultiplayerOptions()
	{
		_default = new MultiplayerOptionsContainer();
		_current = new MultiplayerOptionsContainer();
		_next = new MultiplayerOptionsContainer();
		for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
		{
			_current.CreateOption(optionType);
			_default.CreateOption(optionType);
		}
		MBReadOnlyList<MultiplayerGameTypeInfo> multiplayerGameTypes = Module.CurrentModule.GetMultiplayerGameTypes();
		if (multiplayerGameTypes.Count > 0)
		{
			MultiplayerGameTypeInfo multiplayerGameTypeInfo = multiplayerGameTypes[0];
			_current.UpdateOptionValue(OptionType.GameType, multiplayerGameTypeInfo.GameType);
			_current.UpdateOptionValue(OptionType.PremadeMatchGameMode, multiplayerGameTypes.First((MultiplayerGameTypeInfo info) => info.GameType == "Skirmish").GameType);
			_current.UpdateOptionValue(OptionType.Map, multiplayerGameTypeInfo.Scenes.FirstOrDefault());
		}
		_current.UpdateOptionValue(OptionType.CultureTeam1, MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>()[0].StringId);
		_current.UpdateOptionValue(OptionType.CultureTeam2, MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>()[2].StringId);
		_current.UpdateOptionValue(OptionType.MaxNumberOfPlayers, 120);
		_current.UpdateOptionValue(OptionType.MinNumberOfPlayersForMatchStart, 1);
		_current.UpdateOptionValue(OptionType.WarmupTimeLimitInSeconds, 300);
		_current.UpdateOptionValue(OptionType.MapTimeLimit, 30);
		_current.UpdateOptionValue(OptionType.RoundTimeLimit, 120);
		_current.UpdateOptionValue(OptionType.RoundPreparationTimeLimit, 10);
		_current.UpdateOptionValue(OptionType.RoundTotal, 1);
		_current.UpdateOptionValue(OptionType.RespawnPeriodTeam1, 3);
		_current.UpdateOptionValue(OptionType.RespawnPeriodTeam2, 3);
		_current.UpdateOptionValue(OptionType.MinScoreToWinMatch, 120000);
		_current.UpdateOptionValue(OptionType.AutoTeamBalanceThreshold, 0);
		_current.CopyAllValuesTo(_next);
		_current.CopyAllValuesTo(_default);
	}

	public static void Release()
	{
		_instance = null;
	}

	public MultiplayerOption GetOptionFromOptionType(OptionType optionType, MultiplayerOptionsAccessMode mode = MultiplayerOptionsAccessMode.CurrentMapOptions)
	{
		return GetContainer(mode).GetOptionFromOptionType(optionType);
	}

	public void OnGameTypeChanged(MultiplayerOptionsAccessMode mode = MultiplayerOptionsAccessMode.CurrentMapOptions)
	{
		string text = "";
		if (CurrentOptionsCategory == OptionsCategory.Default)
		{
			text = OptionType.GameType.GetStrValue(mode);
		}
		else if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
		{
			text = OptionType.PremadeMatchGameMode.GetStrValue(mode);
		}
		OptionType.DisableInactivityKick.SetValue(value: false);
		switch (text)
		{
		case "TeamDeathmatch":
			InitializeForTeamDeathmatch(mode);
			break;
		case "Duel":
			InitializeForDuel(mode);
			break;
		case "Siege":
			InitializeForSiege(mode);
			break;
		case "Captain":
			InitializeForCaptain(mode);
			break;
		case "Skirmish":
			InitializeForSkirmish(mode);
			break;
		case "Battle":
			InitializeForBattle(mode);
			break;
		}
		MBList<string> mapList = GetMapList();
		if (mapList.Count > 0)
		{
			OptionType.Map.SetValue(mapList[0]);
		}
	}

	public void InitializeNextAndDefaultOptionContainers()
	{
		_current.CopyAllValuesTo(_next);
		_current.CopyAllValuesTo(_default);
	}

	private void InitializeForTeamDeathmatch(MultiplayerOptionsAccessMode mode)
	{
		string gameModeID = "TeamDeathmatch";
		OptionType.MaxNumberOfPlayers.SetValue(GetNumberOfPlayersForGameMode(gameModeID), mode);
		OptionType.NumberOfBotsPerFormation.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeFriendPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedFriendPercent.SetValue(0, mode);
		OptionType.SpectatorCamera.SetValue(0, mode);
		OptionType.MapTimeLimit.SetValue(GetRoundTimeLimitInMinutesForGameMode(gameModeID), mode);
		OptionType.RespawnPeriodTeam1.SetValue(3, mode);
		OptionType.RespawnPeriodTeam2.SetValue(3, mode);
		OptionType.GoldGainChangePercentageTeam1.SetValue(0, mode);
		OptionType.GoldGainChangePercentageTeam2.SetValue(0, mode);
		OptionType.MinScoreToWinMatch.SetValue(120000, mode);
		OptionType.AutoTeamBalanceThreshold.SetValue(2, mode);
	}

	private void InitializeForDuel(MultiplayerOptionsAccessMode mode)
	{
		string gameModeID = "Duel";
		OptionType.MaxNumberOfPlayers.SetValue(GetNumberOfPlayersForGameMode(gameModeID), mode);
		OptionType.NumberOfBotsPerFormation.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeFriendPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedFriendPercent.SetValue(0, mode);
		OptionType.SpectatorCamera.SetValue(0, mode);
		OptionType.MapTimeLimit.SetValue(OptionType.MapTimeLimit.GetMaximumValue(), mode);
		OptionType.RespawnPeriodTeam1.SetValue(3, mode);
		OptionType.RespawnPeriodTeam2.SetValue(3, mode);
		OptionType.GoldGainChangePercentageTeam1.SetValue(0, mode);
		OptionType.GoldGainChangePercentageTeam2.SetValue(0, mode);
		OptionType.AutoTeamBalanceThreshold.SetValue(0, mode);
		OptionType.MinScoreToWinDuel.SetValue(3, mode);
	}

	private void InitializeForSiege(MultiplayerOptionsAccessMode mode)
	{
		string gameModeID = "Siege";
		OptionType.MaxNumberOfPlayers.SetValue(GetNumberOfPlayersForGameMode(gameModeID), mode);
		OptionType.NumberOfBotsPerFormation.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeSelfPercent.SetValue(50, mode);
		OptionType.FriendlyFireDamageMeleeFriendPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedSelfPercent.SetValue(50, mode);
		OptionType.FriendlyFireDamageRangedFriendPercent.SetValue(0, mode);
		OptionType.SpectatorCamera.SetValue(0, mode);
		OptionType.WarmupTimeLimitInSeconds.SetValue(180, mode);
		OptionType.MapTimeLimit.SetValue(GetRoundTimeLimitInMinutesForGameMode(gameModeID), mode);
		OptionType.RespawnPeriodTeam1.SetValue(3, mode);
		OptionType.RespawnPeriodTeam2.SetValue(12, mode);
		OptionType.GoldGainChangePercentageTeam1.SetValue(30, mode);
		OptionType.GoldGainChangePercentageTeam2.SetValue(0, mode);
		OptionType.AutoTeamBalanceThreshold.SetValue(2, mode);
	}

	private void InitializeForCaptain(MultiplayerOptionsAccessMode mode)
	{
		string gameModeID = "Captain";
		OptionType.MaxNumberOfPlayers.SetValue(GetNumberOfPlayersForGameMode(gameModeID), mode);
		OptionType.NumberOfBotsPerFormation.SetValue(25, mode);
		OptionType.FriendlyFireDamageMeleeSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeFriendPercent.SetValue(50, mode);
		OptionType.FriendlyFireDamageRangedSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedFriendPercent.SetValue(50, mode);
		OptionType.SpectatorCamera.SetValue(6, mode);
		OptionType.WarmupTimeLimitInSeconds.SetValue(300, mode);
		OptionType.MapTimeLimit.SetValue(5, mode);
		OptionType.RoundTimeLimit.SetValue(GetRoundTimeLimitInMinutesForGameMode(gameModeID) * 60, mode);
		OptionType.RoundPreparationTimeLimit.SetValue(20, mode);
		OptionType.RoundTotal.SetValue(GetRoundCountForGameMode(gameModeID), mode);
		OptionType.RespawnPeriodTeam1.SetValue(3, mode);
		OptionType.RespawnPeriodTeam2.SetValue(3, mode);
		OptionType.GoldGainChangePercentageTeam1.SetValue(0, mode);
		OptionType.GoldGainChangePercentageTeam2.SetValue(0, mode);
		OptionType.AutoTeamBalanceThreshold.SetValue(2, mode);
		OptionType.AllowPollsToKickPlayers.SetValue(value: true, mode);
		OptionType.SingleSpawn.SetValue(value: true);
	}

	private void InitializeForSkirmish(MultiplayerOptionsAccessMode mode)
	{
		string gameModeID = "Skirmish";
		OptionType.MaxNumberOfPlayers.SetValue(GetNumberOfPlayersForGameMode(gameModeID), mode);
		OptionType.NumberOfBotsPerFormation.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeFriendPercent.SetValue(50, mode);
		OptionType.FriendlyFireDamageRangedSelfPercent.SetValue(0, mode);
		OptionType.FriendlyFireDamageRangedFriendPercent.SetValue(50, mode);
		OptionType.SpectatorCamera.SetValue(6, mode);
		OptionType.WarmupTimeLimitInSeconds.SetValue(300, mode);
		OptionType.MapTimeLimit.SetValue(5, mode);
		OptionType.RoundTimeLimit.SetValue(GetRoundTimeLimitInMinutesForGameMode(gameModeID) * 60, mode);
		OptionType.RoundPreparationTimeLimit.SetValue(20, mode);
		OptionType.RoundTotal.SetValue(GetRoundCountForGameMode(gameModeID), mode);
		OptionType.RespawnPeriodTeam1.SetValue(3, mode);
		OptionType.RespawnPeriodTeam2.SetValue(3, mode);
		OptionType.GoldGainChangePercentageTeam1.SetValue(0, mode);
		OptionType.GoldGainChangePercentageTeam2.SetValue(0, mode);
		OptionType.AutoTeamBalanceThreshold.SetValue(2, mode);
		OptionType.AllowPollsToKickPlayers.SetValue(value: true, mode);
	}

	private void InitializeForBattle(MultiplayerOptionsAccessMode mode)
	{
		string gameModeID = "Battle";
		OptionType.MaxNumberOfPlayers.SetValue(GetNumberOfPlayersForGameMode(gameModeID), mode);
		OptionType.NumberOfBotsPerFormation.SetValue(0, mode);
		OptionType.FriendlyFireDamageMeleeSelfPercent.SetValue(25, mode);
		OptionType.FriendlyFireDamageMeleeFriendPercent.SetValue(50, mode);
		OptionType.FriendlyFireDamageRangedSelfPercent.SetValue(25, mode);
		OptionType.FriendlyFireDamageRangedFriendPercent.SetValue(50, mode);
		OptionType.SpectatorCamera.SetValue(6, mode);
		OptionType.WarmupTimeLimitInSeconds.SetValue(300, mode);
		OptionType.MapTimeLimit.SetValue(90, mode);
		OptionType.RoundTimeLimit.SetValue(GetRoundTimeLimitInMinutesForGameMode(gameModeID) * 60, mode);
		OptionType.RoundPreparationTimeLimit.SetValue(20, mode);
		OptionType.RoundTotal.SetValue(GetRoundCountForGameMode(gameModeID), mode);
		OptionType.RespawnPeriodTeam1.SetValue(3, mode);
		OptionType.RespawnPeriodTeam2.SetValue(3, mode);
		OptionType.GoldGainChangePercentageTeam1.SetValue(0, mode);
		OptionType.GoldGainChangePercentageTeam2.SetValue(0, mode);
		OptionType.AutoTeamBalanceThreshold.SetValue(2, mode);
		OptionType.SingleSpawn.SetValue(value: true);
	}

	public int GetNumberOfPlayersForGameMode(string gameModeID)
	{
		switch (gameModeID)
		{
		case "TeamDeathmatch":
		case "Siege":
		case "Battle":
			return 120;
		case "Captain":
		case "Skirmish":
			return 12;
		case "Duel":
			return 32;
		default:
			return 0;
		}
	}

	public int GetRoundCountForGameMode(string gameModeID)
	{
		switch (gameModeID)
		{
		case "TeamDeathmatch":
		case "Siege":
		case "Duel":
			return 1;
		case "Battle":
			return 9;
		case "Captain":
		case "Skirmish":
			return 5;
		default:
			return 0;
		}
	}

	public int GetRoundTimeLimitInMinutesForGameMode(string gameModeID)
	{
		switch (gameModeID)
		{
		case "TeamDeathmatch":
		case "Siege":
		case "Duel":
			return 30;
		case "Battle":
			return 20;
		case "Captain":
			return 10;
		case "Skirmish":
			return 7;
		default:
			return 0;
		}
	}

	public void InitializeFromCommandList(List<string> arguments)
	{
		foreach (string argument in arguments)
		{
			GameNetwork.HandleConsoleCommand(argument);
		}
	}

	public void ResetDefaultsToCurrent()
	{
		_current.CopyAllValuesTo(_default);
	}

	public List<string> GetMultiplayerOptionsTextList(OptionType optionType)
	{
		List<string> list = new List<string>();
		string text = new TextObject("{=vBkrw5VV}Random").ToString();
		string item = "-- " + text + " --";
		switch (optionType)
		{
		case OptionType.GameType:
			list = (from q in Module.CurrentModule.GetMultiplayerGameTypes()
				select GameTexts.FindText("str_multiplayer_official_game_type_name", q.GameType).ToString()).ToList();
			break;
		case OptionType.PremadeMatchGameMode:
			list = (from q in Module.CurrentModule.GetMultiplayerGameTypes()
				where q.GameType == "Skirmish" || q.GameType == "Captain"
				select GameTexts.FindText("str_multiplayer_official_game_type_name", q.GameType).ToString()).ToList();
			break;
		case OptionType.Map:
		{
			List<string> list2 = new List<string>();
			if (CurrentOptionsCategory == OptionsCategory.Default)
			{
				list2 = MultiplayerGameTypes.GetGameTypeInfo(OptionType.GameType.GetStrValue()).Scenes.ToList();
			}
			else if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
			{
				list2 = GetAvailableClanMatchScenes();
				list.Insert(0, item);
			}
			foreach (string item3 in list2)
			{
				TextObject textObject;
				string item2 = ((!GameTexts.TryGetText("str_multiplayer_scene_name", out textObject, item3)) ? item3 : textObject.ToString());
				list.Add(item2);
			}
			break;
		}
		case OptionType.PremadeGameType:
			list = new List<string>
			{
				new TextObject("{=H5tiRTya}Practice").ToString(),
				new TextObject("{=YNkPy4ta}Clan Match").ToString()
			};
			break;
		case OptionType.SpectatorCamera:
			list = new List<string>
			{
				GameTexts.FindText("str_multiplayer_spectator_camera_type", SpectatorCameraTypes.LockToAnyAgent.ToString()).ToString(),
				GameTexts.FindText("str_multiplayer_spectator_camera_type", SpectatorCameraTypes.LockToAnyPlayer.ToString()).ToString(),
				GameTexts.FindText("str_multiplayer_spectator_camera_type", SpectatorCameraTypes.LockToTeamMembers.ToString()).ToString(),
				GameTexts.FindText("str_multiplayer_spectator_camera_type", SpectatorCameraTypes.LockToTeamMembersView.ToString()).ToString()
			};
			break;
		case OptionType.CultureTeam1:
		case OptionType.CultureTeam2:
			list = (from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>()
				where x.IsMainCulture
				select GetLocalizedCultureNameFromStringID(x.StringId)).ToList();
			if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
			{
				list.Insert(0, item);
			}
			break;
		default:
			list = GetMultiplayerOptionsList(optionType);
			break;
		}
		return list;
	}

	public List<string> GetMultiplayerOptionsList(OptionType optionType)
	{
		List<string> list = new List<string>();
		switch (optionType)
		{
		case OptionType.GameType:
			list = (from q in Module.CurrentModule.GetMultiplayerGameTypes()
				select q.GameType).ToList();
			break;
		case OptionType.PremadeMatchGameMode:
			list = (from q in Module.CurrentModule.GetMultiplayerGameTypes()
				select q.GameType).ToList();
			list.Remove("TeamDeathmatch");
			list.Remove("Duel");
			list.Remove("Siege");
			break;
		case OptionType.Map:
			if (CurrentOptionsCategory == OptionsCategory.Default)
			{
				list = MultiplayerGameTypes.GetGameTypeInfo(OptionType.GameType.GetStrValue()).Scenes.ToList();
			}
			else if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
			{
				OptionType.PremadeMatchGameMode.GetStrValue();
				list = GetAvailableClanMatchScenes();
				list.Insert(0, "RandomSelection");
			}
			break;
		case OptionType.PremadeGameType:
			list = new List<string>
			{
				PremadeGameType.Practice.ToString(),
				PremadeGameType.Clan.ToString()
			};
			break;
		case OptionType.SpectatorCamera:
			list = new List<string>
			{
				SpectatorCameraTypes.LockToAnyAgent.ToString(),
				SpectatorCameraTypes.LockToAnyPlayer.ToString(),
				SpectatorCameraTypes.LockToTeamMembers.ToString(),
				SpectatorCameraTypes.LockToTeamMembersView.ToString()
			};
			break;
		case OptionType.CultureTeam1:
		case OptionType.CultureTeam2:
			list = (from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>()
				where x.IsMainCulture
				select x.StringId).ToList();
			if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
			{
				list.Insert(0, Parameters.RandomSelectionString);
			}
			break;
		}
		return list;
	}

	private List<string> GetAvailableClanMatchScenes()
	{
		string[] source = new string[0];
		if (NetworkMain.GameClient.AvailableScenes.ScenesByGameTypes.TryGetValue(OptionType.PremadeMatchGameMode.GetStrValue(), out var value))
		{
			source = value;
		}
		return source.ToList();
	}

	private MultiplayerOptionsContainer GetContainer(MultiplayerOptionsAccessMode mode = MultiplayerOptionsAccessMode.CurrentMapOptions)
	{
		return mode switch
		{
			MultiplayerOptionsAccessMode.DefaultMapOptions => _default, 
			MultiplayerOptionsAccessMode.CurrentMapOptions => _current, 
			MultiplayerOptionsAccessMode.NextMapOptions => _next, 
			_ => null, 
		};
	}

	public void InitializeAllOptionsFromNext()
	{
		_next.CopyAllValuesTo(_current);
		UpdateMbMultiplayerData(_current);
	}

	private void UpdateMbMultiplayerData(MultiplayerOptionsContainer container)
	{
		container.GetOptionFromOptionType(OptionType.ServerName).GetValue(out MBMultiplayerData.ServerName);
		if (CurrentOptionsCategory == OptionsCategory.Default)
		{
			container.GetOptionFromOptionType(OptionType.GameType).GetValue(out MBMultiplayerData.GameType);
		}
		else if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
		{
			container.GetOptionFromOptionType(OptionType.PremadeMatchGameMode).GetValue(out MBMultiplayerData.GameType);
		}
		container.GetOptionFromOptionType(OptionType.Map).GetValue(out MBMultiplayerData.Map);
		container.GetOptionFromOptionType(OptionType.MaxNumberOfPlayers).GetValue(out MBMultiplayerData.PlayerCountLimit);
	}

	public MBList<string> GetMapList()
	{
		MultiplayerGameTypeInfo multiplayerGameTypeInfo = null;
		if (CurrentOptionsCategory == OptionsCategory.Default)
		{
			multiplayerGameTypeInfo = MultiplayerGameTypes.GetGameTypeInfo(OptionType.GameType.GetStrValue());
		}
		else if (CurrentOptionsCategory == OptionsCategory.PremadeMatch)
		{
			multiplayerGameTypeInfo = MultiplayerGameTypes.GetGameTypeInfo(OptionType.PremadeMatchGameMode.GetStrValue());
		}
		MBList<string> mBList = new MBList<string>();
		if (multiplayerGameTypeInfo.Scenes.Count > 0)
		{
			mBList.Add(multiplayerGameTypeInfo.Scenes[0]);
			OptionType.Map.SetValue(mBList[0]);
		}
		return mBList;
	}

	public string GetValueTextForOptionWithMultipleSelection(OptionType optionType)
	{
		MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
		return optionProperty.OptionValueType switch
		{
			OptionValueType.Enum => Enum.ToObject(optionProperty.EnumType, optionType.GetIntValue()).ToString(), 
			OptionValueType.String => optionType.GetStrValue(), 
			_ => null, 
		};
	}

	public void SetValueForOptionWithMultipleSelectionFromText(OptionType optionType, string value)
	{
		MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
		switch (optionProperty.OptionValueType)
		{
		case OptionValueType.Enum:
			optionType.SetValue((int)Enum.Parse(optionProperty.EnumType, value));
			break;
		case OptionValueType.String:
			optionType.SetValue(value);
			break;
		}
		if (optionType == OptionType.GameType || optionType == OptionType.PremadeMatchGameMode)
		{
			OnGameTypeChanged();
		}
	}

	private static string GetLocalizedCultureNameFromStringID(string cultureID)
	{
		switch (cultureID)
		{
		case "sturgia":
			return new TextObject("{=PjO7oY16}Sturgia").ToString();
		case "vlandia":
			return new TextObject("{=FjwRsf1C}Vlandia").ToString();
		case "battania":
			return new TextObject("{=0B27RrYJ}Battania").ToString();
		case "empire":
			return new TextObject("{=empirefaction}Empire").ToString();
		case "khuzait":
			return new TextObject("{=sZLd6VHi}Khuzait").ToString();
		case "aserai":
			return new TextObject("{=aseraifaction}Aserai").ToString();
		default:
			Debug.FailedAssert("Unidentified culture id: " + cultureID, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\Gameplay\\MultiplayerOptions.cs", "GetLocalizedCultureNameFromStringID", 974);
			return "";
		}
	}

	public static bool TryGetOptionTypeFromString(string optionTypeString, out OptionType optionType, out MultiplayerOptionsProperty optionAttribute)
	{
		optionAttribute = null;
		for (optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			if (optionProperty != null && optionType.ToString().Equals(optionTypeString))
			{
				optionAttribute = optionProperty;
				return true;
			}
		}
		return false;
	}
}
