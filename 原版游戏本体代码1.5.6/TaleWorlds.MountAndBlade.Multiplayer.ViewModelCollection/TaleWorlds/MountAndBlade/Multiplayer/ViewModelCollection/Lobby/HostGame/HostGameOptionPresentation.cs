using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.HostGame;

public sealed class HostGameOptionPresentation
{
	public readonly MultiplayerOptions.OptionType OptionType;

	public readonly HostGameOptionSection Section;

	public readonly int Order;

	public readonly HostGameOptionUnit Unit;

	public readonly int SliderStep;

	public readonly bool IsPassword;

	public readonly bool IsSavedBetweenSessions;

	public readonly bool IsSharedAcrossGameTypes;

	public readonly MultiplayerOptions.OptionType? PairedTeam2Option;

	public readonly Func<bool> IsRelevant;

	public readonly HostGameOptionSpecialValue SpecialValue;

	private static readonly Dictionary<MultiplayerOptions.OptionType, HostGameOptionPresentation> _presentations;

	private static readonly HashSet<MultiplayerOptions.OptionType> _team2HalvesOfPairs;

	public bool IsTeam2HalfOfPair => _team2HalvesOfPairs.Contains(OptionType);

	private HostGameOptionPresentation(MultiplayerOptions.OptionType optionType, HostGameOptionSection section, int order, HostGameOptionUnit unit, int sliderStep, bool isPassword, bool isSavedBetweenSessions, bool isSharedAcrossGameTypes, MultiplayerOptions.OptionType? pairedTeam2Option, Func<bool> isRelevant, HostGameOptionSpecialValue specialValue)
	{
		OptionType = optionType;
		Section = section;
		Order = order;
		Unit = unit;
		SliderStep = sliderStep;
		IsPassword = isPassword;
		IsSavedBetweenSessions = isSavedBetweenSessions;
		IsSharedAcrossGameTypes = isSharedAcrossGameTypes;
		PairedTeam2Option = pairedTeam2Option;
		IsRelevant = isRelevant;
		SpecialValue = specialValue;
	}

	public string FormatValue(int value)
	{
		if (SpecialValue != null && SpecialValue.Value == value)
		{
			return SpecialValue.Label.ToString();
		}
		return Unit switch
		{
			HostGameOptionUnit.Seconds => FormatSeconds(value), 
			HostGameOptionUnit.Minutes => new TextObject("{=*}{MINUTES} min").SetTextVariable("MINUTES", value).ToString(), 
			HostGameOptionUnit.Percent => new TextObject("{=*}{PERCENT}%").SetTextVariable("PERCENT", value).ToString(), 
			_ => value.ToString(), 
		};
	}

	private static string FormatSeconds(int totalSeconds)
	{
		if (totalSeconds < 60)
		{
			return new TextObject("{=*}{SECONDS} s").SetTextVariable("SECONDS", totalSeconds).ToString();
		}
		return totalSeconds / 60 + ":" + (totalSeconds % 60).ToString("00");
	}

	static HostGameOptionPresentation()
	{
		_presentations = new Dictionary<MultiplayerOptions.OptionType, HostGameOptionPresentation>();
		_team2HalvesOfPairs = new HashSet<MultiplayerOptions.OptionType>();
		Add(MultiplayerOptions.OptionType.ServerName, HostGameOptionSection.Server, 0, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: true);
		Add(MultiplayerOptions.OptionType.GamePassword, HostGameOptionSection.Server, 1, HostGameOptionUnit.None, 1, isPassword: true);
		Add(MultiplayerOptions.OptionType.AdminPassword, HostGameOptionSection.Server, 2, HostGameOptionUnit.None, 1, isPassword: true);
		Add(MultiplayerOptions.OptionType.WelcomeMessage, HostGameOptionSection.Server, 3, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: true);
		Add(MultiplayerOptions.OptionType.MaxNumberOfPlayers, HostGameOptionSection.Server, 4);
		Add(MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart, HostGameOptionSection.Server, 5);
		Add(MultiplayerOptions.OptionType.GameType, HostGameOptionSection.Match, 0, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: true);
		Add(MultiplayerOptions.OptionType.PremadeMatchGameMode, HostGameOptionSection.Match, 1, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: false);
		Add(MultiplayerOptions.OptionType.PremadeGameType, HostGameOptionSection.Match, 2, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: false);
		Add(MultiplayerOptions.OptionType.Map, HostGameOptionSection.Match, 3);
		Add(MultiplayerOptions.OptionType.CultureTeam1, HostGameOptionSection.Match, 4);
		Add(MultiplayerOptions.OptionType.CultureTeam2, HostGameOptionSection.Match, 5);
		Add(MultiplayerOptions.OptionType.WarmupTimeLimitInSeconds, HostGameOptionSection.RulesAndTiming, 0, HostGameOptionUnit.Seconds, 30);
		Add(MultiplayerOptions.OptionType.MapTimeLimit, HostGameOptionSection.RulesAndTiming, 1, HostGameOptionUnit.Minutes);
		Add(MultiplayerOptions.OptionType.RoundTotal, HostGameOptionSection.RulesAndTiming, 2);
		Add(MultiplayerOptions.OptionType.RoundTimeLimit, HostGameOptionSection.RulesAndTiming, 3, HostGameOptionUnit.Seconds, 30);
		Add(MultiplayerOptions.OptionType.RoundPreparationTimeLimit, HostGameOptionSection.RulesAndTiming, 4, HostGameOptionUnit.Seconds);
		Add(MultiplayerOptions.OptionType.MinScoreToWinMatch, HostGameOptionSection.RulesAndTiming, 5, HostGameOptionUnit.None, 1000);
		Add(MultiplayerOptions.OptionType.MinScoreToWinDuel, HostGameOptionSection.RulesAndTiming, 6);
		Add(MultiplayerOptions.OptionType.UnlimitedGold, HostGameOptionSection.RulesAndTiming, 7);
		Add(MultiplayerOptions.OptionType.AllowIndividualBanners, HostGameOptionSection.RulesAndTiming, 8);
		Add(MultiplayerOptions.OptionType.UseRealisticBlocking, HostGameOptionSection.RulesAndTiming, 9);
		Add(MultiplayerOptions.OptionType.DisableInactivityKick, HostGameOptionSection.RulesAndTiming, 10);
		AddTeamPair(MultiplayerOptions.OptionType.NumberOfBotsTeam1, MultiplayerOptions.OptionType.NumberOfBotsTeam2, HostGameOptionSection.TeamsAndBots, 0);
		Add(MultiplayerOptions.OptionType.NumberOfBotsPerFormation, HostGameOptionSection.TeamsAndBots, 1);
		AddTeamPair(MultiplayerOptions.OptionType.RespawnPeriodTeam1, MultiplayerOptions.OptionType.RespawnPeriodTeam2, HostGameOptionSection.TeamsAndBots, 2, HostGameOptionUnit.Seconds);
		AddTeamPair(MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1, MultiplayerOptions.OptionType.GoldGainChangePercentageTeam2, HostGameOptionSection.TeamsAndBots, 3, HostGameOptionUnit.Percent);
		Add(MultiplayerOptions.OptionType.AutoTeamBalanceThreshold, HostGameOptionSection.TeamsAndBots, 4, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, null, OffAt(0));
		Add(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeFriendPercent, HostGameOptionSection.FriendlyFire, 0, HostGameOptionUnit.Percent, 5);
		Add(MultiplayerOptions.OptionType.FriendlyFireDamageMeleeSelfPercent, HostGameOptionSection.FriendlyFire, 1, HostGameOptionUnit.Percent, 5);
		Add(MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent, HostGameOptionSection.FriendlyFire, 2, HostGameOptionUnit.Percent, 5);
		Add(MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent, HostGameOptionSection.FriendlyFire, 3, HostGameOptionUnit.Percent, 5);
		Add(MultiplayerOptions.OptionType.AllowPollsToKickPlayers, HostGameOptionSection.Voting, 0);
		Add(MultiplayerOptions.OptionType.AllowPollsToBanPlayers, HostGameOptionSection.Voting, 1);
		Add(MultiplayerOptions.OptionType.AllowPollsToChangeMaps, HostGameOptionSection.Voting, 2);
		Add(MultiplayerOptions.OptionType.PollAcceptThreshold, HostGameOptionSection.Voting, 3, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, IsAnyPollAllowed);
		Add(MultiplayerOptions.OptionType.SpectatorCamera, HostGameOptionSection.Spectators, 0);
		Add(MultiplayerOptions.OptionType.MaxStreamerCount, HostGameOptionSection.Streamers, 0);
		Add(MultiplayerOptions.OptionType.StreamerPassword, HostGameOptionSection.Streamers, 1, HostGameOptionUnit.None, 1, isPassword: true, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, AreStreamersEnabled);
		Add(MultiplayerOptions.OptionType.SingleSpawn, HostGameOptionSection.Advanced, 0);
		Add(MultiplayerOptions.OptionType.EnableMissionRecording, HostGameOptionSection.Advanced, 1);
		Add(MultiplayerOptions.OptionType.FormationTargetingVisibilityMode, HostGameOptionSection.Advanced, 2);
		Add(MultiplayerOptions.OptionType.FormationTargetingVisibilityThreshold, HostGameOptionSection.Advanced, 3, HostGameOptionUnit.Percent, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, IsFormationTargetingVisibilityRuleOn);
		Add(MultiplayerOptions.OptionType.FormationMarkerAlwaysOnDistance, HostGameOptionSection.Advanced, 4, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, null, UseDefaultAt(-1));
		Add(MultiplayerOptions.OptionType.FormationMarkerFarDistanceCutoff, HostGameOptionSection.Advanced, 5, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, null, UseDefaultAt(-1));
		Add(MultiplayerOptions.OptionType.FormationMarkerFarAlphaTarget, HostGameOptionSection.Advanced, 6, HostGameOptionUnit.Percent, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, null, UseDefaultAt(-1));
		Add(MultiplayerOptions.OptionType.GameDefinitionId, HostGameOptionSection.Advanced, 7, HostGameOptionUnit.None, 1, isPassword: false, isSavedBetweenSessions: false);
		for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
		{
		}
	}

	public static HostGameOptionPresentation Get(MultiplayerOptions.OptionType optionType)
	{
		return _presentations[optionType];
	}

	private static void Add(MultiplayerOptions.OptionType optionType, HostGameOptionSection section, int order, HostGameOptionUnit unit = HostGameOptionUnit.None, int sliderStep = 1, bool isPassword = false, bool isSavedBetweenSessions = true, bool isSharedAcrossGameTypes = false, Func<bool> isRelevant = null, HostGameOptionSpecialValue specialValue = null, MultiplayerOptions.OptionType? pairedTeam2Option = null)
	{
		bool isSavedBetweenSessions2 = isSavedBetweenSessions && !isPassword;
		HostGameOptionPresentation value = new HostGameOptionPresentation(optionType, section, order, unit, sliderStep, isPassword, isSavedBetweenSessions2, isSharedAcrossGameTypes, pairedTeam2Option, isRelevant, specialValue);
		_presentations.Add(optionType, value);
	}

	private static void AddTeamPair(MultiplayerOptions.OptionType team1Option, MultiplayerOptions.OptionType team2Option, HostGameOptionSection section, int order, HostGameOptionUnit unit = HostGameOptionUnit.None)
	{
		Add(team1Option, section, order, unit, 1, isPassword: false, isSavedBetweenSessions: true, isSharedAcrossGameTypes: false, null, null, team2Option);
		Add(team2Option, section, order, unit);
		_team2HalvesOfPairs.Add(team2Option);
	}

	private static HostGameOptionSpecialValue UseDefaultAt(int value)
	{
		return new HostGameOptionSpecialValue(value, new TextObject("{=*}Default"));
	}

	private static HostGameOptionSpecialValue OffAt(int value)
	{
		return new HostGameOptionSpecialValue(value, new TextObject("{=*}Off"));
	}

	private static bool AreStreamersEnabled()
	{
		return MultiplayerOptions.OptionType.MaxStreamerCount.GetIntValue() > 0;
	}

	private static bool IsAnyPollAllowed()
	{
		if (!MultiplayerOptions.OptionType.AllowPollsToKickPlayers.GetBoolValue() && !MultiplayerOptions.OptionType.AllowPollsToBanPlayers.GetBoolValue())
		{
			return MultiplayerOptions.OptionType.AllowPollsToChangeMaps.GetBoolValue();
		}
		return true;
	}

	private static bool IsFormationTargetingVisibilityRuleOn()
	{
		return MultiplayerOptions.OptionType.FormationTargetingVisibilityMode.GetIntValue() != 0;
	}
}
