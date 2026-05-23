using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

public class SPScoreboardShipVM : ViewModel
{
	public readonly IShipOrigin Ship;

	public readonly IBattleCombatant Owner;

	public static Func<SPScoreboardShipVM, List<TooltipProperty>> GetTooltip;

	private string _shipType;

	private bool _isPlayerTeam;

	private bool _isPlayerAllyTeam;

	private bool _isEnemyTeam;

	private float _currentHealth;

	private float _maxHealth;

	private bool _isDestroyed;

	private bool _isInactive;

	private BasicTooltipViewModel _tooltip;

	[DataSourceProperty]
	public string ShipType
	{
		get
		{
			return _shipType;
		}
		set
		{
			if (value != _shipType)
			{
				_shipType = value;
				OnPropertyChangedWithValue(value, "ShipType");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerTeam
	{
		get
		{
			return _isPlayerTeam;
		}
		set
		{
			if (value != _isPlayerTeam)
			{
				_isPlayerTeam = value;
				OnPropertyChangedWithValue(value, "IsPlayerTeam");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerAllyTeam
	{
		get
		{
			return _isPlayerAllyTeam;
		}
		set
		{
			if (value != _isPlayerAllyTeam)
			{
				_isPlayerAllyTeam = value;
				OnPropertyChangedWithValue(value, "IsPlayerAllyTeam");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnemyTeam
	{
		get
		{
			return _isEnemyTeam;
		}
		set
		{
			if (value != _isEnemyTeam)
			{
				_isEnemyTeam = value;
				OnPropertyChangedWithValue(value, "IsEnemyTeam");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentHealth
	{
		get
		{
			return _currentHealth;
		}
		set
		{
			if (value != _currentHealth)
			{
				_currentHealth = value;
				OnPropertyChangedWithValue(value, "CurrentHealth");
				IsDestroyed = _currentHealth == 0f;
			}
		}
	}

	[DataSourceProperty]
	public float MaxHealth
	{
		get
		{
			return _maxHealth;
		}
		set
		{
			if (value != _maxHealth)
			{
				_maxHealth = value;
				OnPropertyChangedWithValue(value, "MaxHealth");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDestroyed
	{
		get
		{
			return _isDestroyed;
		}
		set
		{
			if (value != _isDestroyed)
			{
				_isDestroyed = value;
				OnPropertyChangedWithValue(value, "IsDestroyed");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInactive
	{
		get
		{
			return _isInactive;
		}
		set
		{
			if (value != _isInactive)
			{
				_isInactive = value;
				OnPropertyChangedWithValue(value, "IsInactive");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	public SPScoreboardShipVM(IShipOrigin ship, string shipType, IBattleCombatant owner, TeamSideEnum teamSideEnum)
	{
		Ship = ship;
		ShipType = "Ship_" + shipType;
		Owner = owner;
		IsPlayerTeam = teamSideEnum == TeamSideEnum.PlayerTeam;
		IsPlayerAllyTeam = teamSideEnum == TeamSideEnum.PlayerAllyTeam;
		IsEnemyTeam = teamSideEnum == TeamSideEnum.EnemyTeam;
		MaxHealth = Ship.MaxHitPoints;
		CurrentHealth = Ship.HitPoints;
		Tooltip = new BasicTooltipViewModel(() => GetTooltip?.Invoke(this));
	}
}
