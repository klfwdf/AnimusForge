using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.General;

public class SPGeneralKillNotificationItemVM : ViewModel
{
	private readonly Color _friendlyColor = new Color(0.54296875f, 0.77734375f, 27f / 64f);

	private readonly Color _enemyColor = new Color(61f / 64f, 0.48828125f, 0.42578125f);

	private readonly Agent _affectedAgent;

	private readonly Agent _affectorAgent;

	private readonly Action<SPGeneralKillNotificationItemVM> _onRemove;

	private readonly bool _showNames;

	private string _murdererName;

	private string _murdererType;

	private string _victimName;

	private string _victimType;

	private bool _isUnconscious;

	private bool _isHeadshot;

	private bool _isSuicide;

	private bool _isDrowning;

	private Color _backgroundColor;

	private bool _isPaused;

	[DataSourceProperty]
	public string MurdererName
	{
		get
		{
			return _murdererName;
		}
		set
		{
			if (value != _murdererName)
			{
				_murdererName = value;
				OnPropertyChangedWithValue(value, "MurdererName");
			}
		}
	}

	[DataSourceProperty]
	public string MurdererType
	{
		get
		{
			return _murdererType;
		}
		set
		{
			if (value != _murdererType)
			{
				_murdererType = value;
				OnPropertyChangedWithValue(value, "MurdererType");
			}
		}
	}

	[DataSourceProperty]
	public string VictimName
	{
		get
		{
			return _victimName;
		}
		set
		{
			if (value != _victimName)
			{
				_victimName = value;
				OnPropertyChangedWithValue(value, "VictimName");
			}
		}
	}

	[DataSourceProperty]
	public string VictimType
	{
		get
		{
			return _victimType;
		}
		set
		{
			if (value != _victimType)
			{
				_victimType = value;
				OnPropertyChangedWithValue(value, "VictimType");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUnconscious
	{
		get
		{
			return _isUnconscious;
		}
		set
		{
			if (value != _isUnconscious)
			{
				_isUnconscious = value;
				OnPropertyChangedWithValue(value, "IsUnconscious");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHeadshot
	{
		get
		{
			return _isHeadshot;
		}
		set
		{
			if (value != _isHeadshot)
			{
				_isHeadshot = value;
				OnPropertyChangedWithValue(value, "IsHeadshot");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSuicide
	{
		get
		{
			return _isSuicide;
		}
		set
		{
			if (value != _isSuicide)
			{
				_isSuicide = value;
				OnPropertyChangedWithValue(value, "IsSuicide");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDrowning
	{
		get
		{
			return _isDrowning;
		}
		set
		{
			if (value != _isDrowning)
			{
				_isDrowning = value;
				OnPropertyChangedWithValue(value, "IsDrowning");
			}
		}
	}

	[DataSourceProperty]
	public Color BackgroundColor
	{
		get
		{
			return _backgroundColor;
		}
		set
		{
			if (value != _backgroundColor)
			{
				_backgroundColor = value;
				OnPropertyChangedWithValue(value, "BackgroundColor");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPaused
	{
		get
		{
			return _isPaused;
		}
		set
		{
			if (value != _isPaused)
			{
				_isPaused = value;
				OnPropertyChangedWithValue(value, "IsPaused");
			}
		}
	}

	public SPGeneralKillNotificationItemVM(Agent affectedAgent, Agent affectorAgent, bool isHeadshot, bool isSuicide, bool isDrowning, Action<SPGeneralKillNotificationItemVM> onRemove)
	{
		_affectedAgent = affectedAgent;
		_affectorAgent = affectorAgent;
		_onRemove = onRemove;
		_showNames = BannerlordConfig.KillFeedVisualType == 0;
		InitProperties(_affectedAgent, _affectorAgent, isHeadshot, isSuicide, isDrowning);
	}

	private void InitProperties(Agent affectedAgent, Agent affectorAgent, bool isHeadshot, bool isSuicide, bool isDrowning)
	{
		if (_showNames || (affectorAgent != null && affectorAgent.Character?.IsHero == true))
		{
			MurdererName = affectorAgent.Name;
		}
		MurdererType = GetAgentType(affectorAgent);
		if (!_showNames)
		{
			BasicCharacterObject character = affectedAgent.Character;
			if (character == null || !character.IsHero)
			{
				goto IL_0081;
			}
		}
		VictimName = affectedAgent.Name;
		goto IL_0081;
		IL_0081:
		VictimType = GetAgentType(affectedAgent);
		IsUnconscious = affectedAgent.State == AgentState.Unconscious;
		IsHeadshot = isHeadshot;
		IsSuicide = isSuicide;
		IsDrowning = isDrowning;
		Team team = affectedAgent.Team;
		Color backgroundColor = ((team == null || !team.IsValid) ? Color.FromUint(4284111450u) : ((!affectedAgent.Team.IsPlayerAlly) ? _friendlyColor : _enemyColor));
		BackgroundColor = backgroundColor;
	}

	private static string GetAgentType(Agent agent)
	{
		if (agent?.Character != null)
		{
			switch ((FormationClass)agent.Character.DefaultFormationGroup)
			{
			case FormationClass.Infantry:
				return "Infantry_Light";
			case FormationClass.Ranged:
				return "Archer_Light";
			case FormationClass.Cavalry:
				return "Cavalry_Light";
			case FormationClass.HorseArcher:
				return "HorseArcher_Light";
			case FormationClass.LightCavalry:
				return "Cavalry_Light";
			case FormationClass.HeavyCavalry:
				return "Cavalry_Heavy";
			case FormationClass.NumberOfDefaultFormations:
			case FormationClass.HeavyInfantry:
				return "Infantry_Heavy";
			case FormationClass.NumberOfRegularFormations:
			case FormationClass.Bodyguard:
			case FormationClass.NumberOfAllFormations:
				return "Infantry_Heavy";
			default:
				return "None";
			}
		}
		return "None";
	}

	public void ExecuteRemove()
	{
		_onRemove(this);
	}
}
