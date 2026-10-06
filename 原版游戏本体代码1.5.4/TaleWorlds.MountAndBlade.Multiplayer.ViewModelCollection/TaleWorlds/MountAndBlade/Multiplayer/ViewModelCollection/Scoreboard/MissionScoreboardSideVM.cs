using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Scoreboard;

public class MissionScoreboardSideVM : ViewModel
{
	private readonly MissionScoreboardComponent.MissionScoreboardSide _missionScoreboardSide;

	private readonly Dictionary<MissionPeer, MissionScoreboardPlayerVM> _playersMap;

	private MissionScoreboardPlayerVM _bot;

	private Action<MissionScoreboardPlayerVM> _executeActivate;

	private const string _avatarHeaderId = "avatar";

	private readonly int _avatarHeaderIndex;

	private List<string> _irregularHeaderIDs = new List<string> { "name", "avatar", "score", "kill", "assist" };

	private MBBindingList<MissionScoreboardPlayerVM> _players;

	private MBBindingList<MissionScoreboardHeaderItemVM> _entryProperties;

	private MissionScoreboardPlayerSortControllerVM _playerSortController;

	private bool _isSingleSide;

	private bool _isSecondSide;

	private bool _showAttackerOrDefenderIcons;

	private bool _isAttacker;

	private int _roundsWon;

	private string _name;

	private string _cultureId;

	private string _teamColor;

	private string _playersText;

	private Color _cultureColor1;

	private Color _cultureColor2;

	[DataSourceProperty]
	public MBBindingList<MissionScoreboardPlayerVM> Players
	{
		get
		{
			return _players;
		}
		set
		{
			if (_players != value)
			{
				_players = value;
				OnPropertyChangedWithValue(value, "Players");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionScoreboardHeaderItemVM> EntryProperties
	{
		get
		{
			return _entryProperties;
		}
		set
		{
			if (value != _entryProperties)
			{
				_entryProperties = value;
				OnPropertyChangedWithValue(value, "EntryProperties");
			}
		}
	}

	[DataSourceProperty]
	public MissionScoreboardPlayerSortControllerVM PlayerSortController
	{
		get
		{
			return _playerSortController;
		}
		set
		{
			if (value != _playerSortController)
			{
				_playerSortController = value;
				OnPropertyChangedWithValue(value, "PlayerSortController");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSingleSide
	{
		get
		{
			return _isSingleSide;
		}
		set
		{
			if (value != _isSingleSide)
			{
				_isSingleSide = value;
				OnPropertyChangedWithValue(value, "IsSingleSide");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor1
	{
		get
		{
			return _cultureColor1;
		}
		set
		{
			if (value != _cultureColor1)
			{
				_cultureColor1 = value;
				OnPropertyChangedWithValue(value, "CultureColor1");
			}
		}
	}

	[DataSourceProperty]
	public Color CultureColor2
	{
		get
		{
			return _cultureColor2;
		}
		set
		{
			if (value != _cultureColor2)
			{
				_cultureColor2 = value;
				OnPropertyChangedWithValue(value, "CultureColor2");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSecondSide
	{
		get
		{
			return _isSecondSide;
		}
		set
		{
			if (value != _isSecondSide)
			{
				_isSecondSide = value;
				OnPropertyChangedWithValue(value, "IsSecondSide");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAttackerOrDefenderIcons
	{
		get
		{
			return _showAttackerOrDefenderIcons;
		}
		set
		{
			if (value != _showAttackerOrDefenderIcons)
			{
				_showAttackerOrDefenderIcons = value;
				OnPropertyChangedWithValue(value, "ShowAttackerOrDefenderIcons");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAttacker
	{
		get
		{
			return _isAttacker;
		}
		set
		{
			if (value != _isAttacker)
			{
				_isAttacker = value;
				OnPropertyChangedWithValue(value, "IsAttacker");
			}
		}
	}

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
	public string PlayersText
	{
		get
		{
			return _playersText;
		}
		set
		{
			if (value != _playersText)
			{
				_playersText = value;
				OnPropertyChangedWithValue(value, "PlayersText");
			}
		}
	}

	[DataSourceProperty]
	public string CultureId
	{
		get
		{
			return _cultureId;
		}
		set
		{
			if (value != _cultureId)
			{
				_cultureId = value;
				OnPropertyChangedWithValue(value, "CultureId");
			}
		}
	}

	[DataSourceProperty]
	public int RoundsWon
	{
		get
		{
			return _roundsWon;
		}
		set
		{
			if (_roundsWon != value)
			{
				_roundsWon = value;
				OnPropertyChangedWithValue(value, "RoundsWon");
			}
		}
	}

	[DataSourceProperty]
	public string TeamColor
	{
		get
		{
			return _teamColor;
		}
		set
		{
			if (value != _teamColor)
			{
				_teamColor = value;
				OnPropertyChangedWithValue(value, "TeamColor");
			}
		}
	}

	public MissionScoreboardSideVM(MissionScoreboardComponent.MissionScoreboardSide missionScoreboardSide, Action<MissionScoreboardPlayerVM> executeActivate, bool isSingleSide, bool isSecondSide, MultiplayerBattleColors.MultiplayerCultureColorInfo cultureColorInfo)
	{
		_executeActivate = executeActivate;
		_missionScoreboardSide = missionScoreboardSide;
		_playersMap = new Dictionary<MissionPeer, MissionScoreboardPlayerVM>();
		Players = new MBBindingList<MissionScoreboardPlayerVM>();
		PlayerSortController = new MissionScoreboardPlayerSortControllerVM(ref _players);
		_avatarHeaderIndex = missionScoreboardSide.GetHeaderIds().IndexOf("avatar");
		int score = missionScoreboardSide.GetScore(null);
		string[] valuesOf = missionScoreboardSide.GetValuesOf(null);
		string[] headerIds = missionScoreboardSide.GetHeaderIds();
		_bot = new MissionScoreboardPlayerVM(valuesOf, headerIds, score, _executeActivate);
		foreach (MissionPeer player in missionScoreboardSide.Players)
		{
			AddPlayer(player);
		}
		UpdateBotAttributes();
		UpdateRoundAttributes();
		IsSingleSide = isSingleSide;
		IsSecondSide = isSecondSide;
		CultureId = cultureColorInfo.Culture?.StringId ?? string.Empty;
		CultureColor1 = cultureColorInfo.Color1;
		CultureColor2 = cultureColorInfo.Color2;
		TeamColor = "0x" + cultureColorInfo.Culture?.Color2.ToString("X");
		ShowAttackerOrDefenderIcons = Mission.Current.HasMissionBehavior<MissionMultiplayerSiegeClient>();
		IsAttacker = missionScoreboardSide.Side == BattleSideEnum.Attacker;
		RefreshValues();
		NetworkCommunicator.OnPeerAveragePingUpdated += OnPeerPingUpdated;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>((_missionScoreboardSide.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		if (IsSingleSide)
		{
			Name = MultiplayerOptions.OptionType.GameType.GetStrValue();
		}
		else
		{
			Name = basicCultureObject.Name.ToString();
		}
		EntryProperties = new MBBindingList<MissionScoreboardHeaderItemVM>();
		string[] headerIds = _missionScoreboardSide.GetHeaderIds();
		string[] headerNames = _missionScoreboardSide.GetHeaderNames();
		for (int i = 0; i < headerIds.Length; i++)
		{
			EntryProperties.Add(new MissionScoreboardHeaderItemVM(this, headerIds[i], headerNames[i], headerIds[i] == "avatar", _irregularHeaderIDs.Contains(headerIds[i])));
		}
		UpdatePlayersText();
		PlayerSortController?.RefreshValues();
	}

	public void Tick(float dt)
	{
		foreach (MissionScoreboardPlayerVM player in Players)
		{
			player.Tick(dt);
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		NetworkCommunicator.OnPeerAveragePingUpdated -= OnPeerPingUpdated;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	public void UpdateRoundAttributes()
	{
		RoundsWon = _missionScoreboardSide.SideScore;
		SortPlayers();
	}

	public void UpdateBotAttributes()
	{
		int num = ((_missionScoreboardSide.Side == BattleSideEnum.Attacker) ? MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() : MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue());
		if (num > 0)
		{
			int score = _missionScoreboardSide.GetScore(null);
			string[] valuesOf = _missionScoreboardSide.GetValuesOf(null);
			_bot.UpdateAttributes(valuesOf, score);
			if (!Players.Contains(_bot))
			{
				Players.Add(_bot);
			}
		}
		else if (num == 0 && Players.Contains(_bot))
		{
			Players.Remove(_bot);
		}
		SortPlayers();
	}

	public void UpdatePlayerAttributes(MissionPeer player)
	{
		if (_playersMap.ContainsKey(player))
		{
			int score = _missionScoreboardSide.GetScore(player);
			string[] valuesOf = _missionScoreboardSide.GetValuesOf(player);
			_playersMap[player].UpdateAttributes(valuesOf, score);
		}
		SortPlayers();
	}

	public void RemovePlayer(MissionPeer peer)
	{
		if (_playersMap.TryGetValue(peer, out var value))
		{
			Players.Remove(value);
			_playersMap.Remove(peer);
		}
		else
		{
			Debug.FailedAssert("Trying to remove a player that is not registered", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\Scoreboard\\MissionScoreboardSideVM.cs", "RemovePlayer", 167);
		}
		SortPlayers();
		UpdatePlayersText();
	}

	public void AddPlayer(MissionPeer peer)
	{
		if (!_playersMap.ContainsKey(peer))
		{
			int score = _missionScoreboardSide.GetScore(peer);
			string[] valuesOf = _missionScoreboardSide.GetValuesOf(peer);
			string[] headerIds = _missionScoreboardSide.GetHeaderIds();
			MissionScoreboardPlayerVM missionScoreboardPlayerVM = new MissionScoreboardPlayerVM(peer, valuesOf, headerIds, score, _executeActivate);
			_playersMap.Add(peer, missionScoreboardPlayerVM);
			Players.Add(missionScoreboardPlayerVM);
		}
		SortPlayers();
		UpdatePlayersText();
	}

	private void UpdatePlayersText()
	{
		TextObject textObject = new TextObject("{=R28ac5ij}{NUMBER} Players");
		textObject.SetTextVariable("NUMBER", Players.Count);
		PlayersText = textObject.ToString();
	}

	private void SortPlayers()
	{
		PlayerSortController.SortByCurrentState();
	}

	private void OnPeerPingUpdated(NetworkCommunicator peer)
	{
		MissionPeer component = peer.GetComponent<MissionPeer>();
		if (component != null)
		{
			UpdatePlayerAttributes(component);
		}
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		if (changedManagedOptionsType != ManagedOptions.ManagedOptionsType.EnableGenericAvatars)
		{
			return;
		}
		foreach (MissionScoreboardPlayerVM player in Players)
		{
			if (!player.IsBot)
			{
				player.RefreshAvatar();
			}
		}
	}
}
