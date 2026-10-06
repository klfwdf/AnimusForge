using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.OfficialGame;

public class MPMatchmakingSelectionInfoVM : ViewModel
{
	private string _playersDescription;

	private string _averagePlaytimeDescription;

	private string _roundsDescription;

	private string _roundTimeDescription;

	private string _objectivesDescription;

	private string _troopsDescription;

	private string _name;

	private string _description;

	private bool _isEnabled;

	private MBBindingList<StringPairItemVM> _extraInfos;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> ExtraInfos
	{
		get
		{
			return _extraInfos;
		}
		set
		{
			if (value != _extraInfos)
			{
				_extraInfos = value;
				OnPropertyChangedWithValue(value, "ExtraInfos");
			}
		}
	}

	public MPMatchmakingSelectionInfoVM()
	{
		Name = "";
		Description = "";
		ExtraInfos = new MBBindingList<StringPairItemVM>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_playersDescription = new TextObject("{=RfXJdNye}Players").ToString();
		_averagePlaytimeDescription = new TextObject("{=YAaAlbkX}Avg. Playtime").ToString();
		_roundsDescription = new TextObject("{=iKtIhlbo}Rounds").ToString();
		_roundTimeDescription = new TextObject("{=r5WzivPb}Round Time").ToString();
		_objectivesDescription = new TextObject("{=gqNxq11A}Objectives").ToString();
		_troopsDescription = new TextObject("{=5k4dxUEJ}Troops").ToString();
	}

	public void UpdateForGameType(string gameTypeStr)
	{
		Name = GameTexts.FindText("str_multiplayer_official_game_type_name", gameTypeStr).ToString();
		MBTextManager.SetTextVariable("newline", "\n");
		Description = GameTexts.FindText("str_multiplayer_official_game_type_description", gameTypeStr).ToString();
		ExtraInfos.Clear();
		int num = MultiplayerOptions.Instance.GetNumberOfPlayersForGameMode(gameTypeStr) / 2;
		int roundCountForGameMode = MultiplayerOptions.Instance.GetRoundCountForGameMode(gameTypeStr);
		int roundTimeLimitInMinutesForGameMode = MultiplayerOptions.Instance.GetRoundTimeLimitInMinutesForGameMode(gameTypeStr);
		int num2 = ((roundCountForGameMode == 1) ? 1 : (roundCountForGameMode / 2 + 1));
		int num3 = num2 * roundTimeLimitInMinutesForGameMode;
		MBTextManager.SetTextVariable("PLAYER_COUNT", num.ToString());
		string value = GameTexts.FindText("str_multiplayer_official_game_type_player_info_for_versus").ToString();
		MBTextManager.SetTextVariable("PLAY_TIME", num3.ToString());
		string value2 = GameTexts.FindText("str_multiplayer_official_game_type_playtime_info_in_minutes").ToString();
		MBTextManager.SetTextVariable("ROUND_COUNT", num2.ToString());
		string value3 = GameTexts.FindText("str_multiplayer_official_game_type_rounds_info_for_best_of").ToString();
		MBTextManager.SetTextVariable("PLAY_TIME", roundTimeLimitInMinutesForGameMode.ToString());
		string value4 = GameTexts.FindText("str_multiplayer_official_game_type_playtime_info_in_minutes").ToString();
		string value5 = GameTexts.FindText("str_multiplayer_official_game_type_objective_info", gameTypeStr).ToString();
		string value6 = GameTexts.FindText("str_multiplayer_official_game_type_troops_info", gameTypeStr).ToString();
		ExtraInfos.Add(new StringPairItemVM(_playersDescription, value));
		ExtraInfos.Add(new StringPairItemVM(_averagePlaytimeDescription, value2));
		ExtraInfos.Add(new StringPairItemVM(_roundsDescription, value3));
		ExtraInfos.Add(new StringPairItemVM(_roundTimeDescription, value4));
		ExtraInfos.Add(new StringPairItemVM(_objectivesDescription, value5));
		ExtraInfos.Add(new StringPairItemVM(_troopsDescription, value6));
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
	}
}
