using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MPDeathCardVM : ViewModel
{
	private readonly TextObject _killedByStrayHorse = GameTexts.FindText("str_killed_by_stray_horse");

	private readonly TextObject _killedSelfText = GameTexts.FindText("str_killed_self");

	private readonly TextObject _killedByText = GameTexts.FindText("str_killed_by");

	private readonly TextObject _enemyText = GameTexts.FindText("str_death_card_enemy");

	private readonly TextObject _allyText = GameTexts.FindText("str_death_card_ally");

	private bool _isActive;

	private bool _isSelfInflicted;

	private bool _killCountsEnabled;

	private int _numOfTimesPlayerKilled;

	private int _numOfTimesPlayerGotKilled;

	private string _titleText;

	private string _usedWeaponName;

	private string _killerName;

	private string _killerText;

	private string _youText;

	private MPPlayerVM _playerProperties;

	private int _bodyPartHit;

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelfInflicted
	{
		get
		{
			return _isSelfInflicted;
		}
		set
		{
			if (value != _isSelfInflicted)
			{
				_isSelfInflicted = value;
				OnPropertyChangedWithValue(value, "IsSelfInflicted");
			}
		}
	}

	[DataSourceProperty]
	public bool KillCountsEnabled
	{
		get
		{
			return _killCountsEnabled;
		}
		set
		{
			if (value != _killCountsEnabled)
			{
				_killCountsEnabled = value;
				OnPropertyChangedWithValue(value, "KillCountsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string UsedWeaponName
	{
		get
		{
			return _usedWeaponName;
		}
		set
		{
			if (value != _usedWeaponName)
			{
				_usedWeaponName = value;
				OnPropertyChangedWithValue(value, "UsedWeaponName");
			}
		}
	}

	[DataSourceProperty]
	public string KillerName
	{
		get
		{
			return _killerName;
		}
		set
		{
			if (value != _killerName)
			{
				_killerName = value;
				OnPropertyChangedWithValue(value, "KillerName");
			}
		}
	}

	[DataSourceProperty]
	public string KillerText
	{
		get
		{
			return _killerText;
		}
		set
		{
			if (value != _killerText)
			{
				_killerText = value;
				OnPropertyChangedWithValue(value, "KillerText");
			}
		}
	}

	[DataSourceProperty]
	public string YouText
	{
		get
		{
			return _youText;
		}
		set
		{
			if (value != _youText)
			{
				_youText = value;
				OnPropertyChangedWithValue(value, "YouText");
			}
		}
	}

	[DataSourceProperty]
	public MPPlayerVM PlayerProperties
	{
		get
		{
			return _playerProperties;
		}
		set
		{
			if (value != _playerProperties)
			{
				_playerProperties = value;
				OnPropertyChangedWithValue(value, "PlayerProperties");
			}
		}
	}

	[DataSourceProperty]
	public int BodyPartHit
	{
		get
		{
			return _bodyPartHit;
		}
		set
		{
			if (value != _bodyPartHit)
			{
				_bodyPartHit = value;
				OnPropertyChangedWithValue(value, "BodyPartHit");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfTimesPlayerKilled
	{
		get
		{
			return _numOfTimesPlayerKilled;
		}
		set
		{
			if (value != _numOfTimesPlayerKilled)
			{
				_numOfTimesPlayerKilled = value;
				OnPropertyChangedWithValue(value, "NumOfTimesPlayerKilled");
			}
		}
	}

	[DataSourceProperty]
	public int NumOfTimesPlayerGotKilled
	{
		get
		{
			return _numOfTimesPlayerGotKilled;
		}
		set
		{
			if (value != _numOfTimesPlayerGotKilled)
			{
				_numOfTimesPlayerGotKilled = value;
				OnPropertyChangedWithValue(value, "NumOfTimesPlayerGotKilled");
			}
		}
	}

	public MPDeathCardVM(MultiplayerGameType gameType)
	{
		KillCountsEnabled = gameType != MultiplayerGameType.Captain;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		YouText = GameTexts.FindText("str_death_card_you").ToString();
		Deactivate();
	}

	public void OnMainAgentRemoved(Agent affectorAgent, KillingBlow blow)
	{
		ResetProperties();
		if (affectorAgent != null && affectorAgent == Agent.Main)
		{
			TitleText = _killedSelfText.ToString();
			IsSelfInflicted = true;
		}
		else if (affectorAgent != null && affectorAgent.IsMount && affectorAgent.RiderAgent == null)
		{
			_killedByStrayHorse.SetTextVariable("MOUNT_NAME", affectorAgent.NameTextObject);
			TitleText = _killedByStrayHorse.ToString();
			IsSelfInflicted = true;
		}
		else
		{
			IsSelfInflicted = false;
			TitleText = _killedByText.ToString();
		}
		KillerText = ((affectorAgent?.Team == Agent.Main?.Team) ? _allyText.ToString() : _enemyText.ToString());
		if (IsSelfInflicted)
		{
			PlayerProperties = new MPPlayerVM(GameNetwork.MyPeer.GetComponent<MissionPeer>());
			PlayerProperties.RefreshDivision();
		}
		else
		{
			KillerName = affectorAgent?.Name ?? "";
			if (blow.WeaponItemKind >= 0)
			{
				UsedWeaponName = ItemObject.GetItemFromWeaponKind(blow.WeaponItemKind).Name.ToString();
			}
			else
			{
				UsedWeaponName = new TextObject("{=GAZ5QLZi}Unarmed").ToString();
			}
			bool isServerOrRecorder = GameNetwork.IsServerOrRecorder;
			if (affectorAgent?.MissionPeer != null)
			{
				PlayerProperties = new MPPlayerVM(affectorAgent.MissionPeer);
				PlayerProperties.RefreshDivision();
				NumOfTimesPlayerKilled = Agent.Main.MissionPeer.GetNumberOfTimesPeerKilledPeer(affectorAgent.MissionPeer);
				NumOfTimesPlayerGotKilled = affectorAgent.MissionPeer.GetNumberOfTimesPeerKilledPeer(Agent.Main.MissionPeer) + ((!isServerOrRecorder) ? 1 : 0);
			}
			else if (affectorAgent?.OwningAgentMissionPeer != null)
			{
				PlayerProperties = new MPPlayerVM(affectorAgent.OwningAgentMissionPeer);
				PlayerProperties.RefreshDivision();
			}
			else
			{
				PlayerProperties = new MPPlayerVM(affectorAgent);
			}
		}
		IsActive = true;
	}

	private void ResetProperties()
	{
		IsActive = false;
		TitleText = "";
		UsedWeaponName = "";
		BodyPartHit = -1;
	}

	public void Deactivate()
	{
		IsActive = false;
	}
}
