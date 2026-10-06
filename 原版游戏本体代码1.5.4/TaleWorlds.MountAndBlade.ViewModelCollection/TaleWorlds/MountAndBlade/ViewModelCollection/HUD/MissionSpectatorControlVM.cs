using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class MissionSpectatorControlVM : ViewModel
{
	private readonly Mission _mission;

	private bool _isMainHeroDead;

	private readonly TextObject _deadTextObject = GameTexts.FindText("str_battle_hero_dead");

	private bool _isEnabled;

	private string _prevCharacterText;

	private string _nextCharacterText;

	private string _takeControlText;

	private string _statusText;

	private bool _isTakeControlRelevant;

	private bool _isTakeControlEnabled;

	private string _spectatedAgentName;

	private InputKeyItemVM _prevCharacterKey;

	private InputKeyItemVM _nextCharacterKey;

	private InputKeyItemVM _takeControlKey;

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
	public string PrevCharacterText
	{
		get
		{
			return _prevCharacterText;
		}
		set
		{
			if (value != _prevCharacterText)
			{
				_prevCharacterText = value;
				OnPropertyChangedWithValue(value, "PrevCharacterText");
			}
		}
	}

	[DataSourceProperty]
	public string NextCharacterText
	{
		get
		{
			return _nextCharacterText;
		}
		set
		{
			if (value != _nextCharacterText)
			{
				_nextCharacterText = value;
				OnPropertyChangedWithValue(value, "NextCharacterText");
			}
		}
	}

	[DataSourceProperty]
	public string TakeControlText
	{
		get
		{
			return _takeControlText;
		}
		set
		{
			if (value != _takeControlText)
			{
				_takeControlText = value;
				OnPropertyChangedWithValue(value, "TakeControlText");
			}
		}
	}

	[DataSourceProperty]
	public string StatusText
	{
		get
		{
			return _statusText;
		}
		set
		{
			if (value != _statusText)
			{
				_statusText = value;
				OnPropertyChangedWithValue(value, "StatusText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTakeControlRelevant
	{
		get
		{
			return _isTakeControlRelevant;
		}
		set
		{
			if (value != _isTakeControlRelevant)
			{
				_isTakeControlRelevant = value;
				OnPropertyChangedWithValue(value, "IsTakeControlRelevant");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTakeControlEnabled
	{
		get
		{
			return _isTakeControlEnabled;
		}
		set
		{
			if (value != _isTakeControlEnabled)
			{
				_isTakeControlEnabled = value;
				OnPropertyChangedWithValue(value, "IsTakeControlEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string SpectatedAgentName
	{
		get
		{
			return _spectatedAgentName;
		}
		set
		{
			if (value != _spectatedAgentName)
			{
				_spectatedAgentName = value;
				OnPropertyChangedWithValue(value, "SpectatedAgentName");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM PrevCharacterKey
	{
		get
		{
			return _prevCharacterKey;
		}
		set
		{
			if (value != _prevCharacterKey)
			{
				_prevCharacterKey = value;
				OnPropertyChangedWithValue(value, "PrevCharacterKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM NextCharacterKey
	{
		get
		{
			return _nextCharacterKey;
		}
		set
		{
			if (value != _nextCharacterKey)
			{
				_nextCharacterKey = value;
				OnPropertyChangedWithValue(value, "NextCharacterKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM TakeControlKey
	{
		get
		{
			return _takeControlKey;
		}
		set
		{
			if (value != _takeControlKey)
			{
				_takeControlKey = value;
				OnPropertyChangedWithValue(value, "TakeControlKey");
			}
		}
	}

	public MissionSpectatorControlVM(Mission mission)
	{
		_mission = mission;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PrevCharacterText = new TextObject("{=BANC61K5}Previous Character").ToString();
		NextCharacterText = new TextObject("{=znKxunbQ}Next Character").ToString();
		TakeControlText = new TextObject("{=TGpbi44D}Take Control of Character").ToString();
		UpdateStatusText();
	}

	public void OnSpectatedAgentFocusIn(Agent followedAgent)
	{
		SpectatedAgentName = followedAgent.MissionPeer?.DisplayedName ?? followedAgent.Name;
	}

	public void OnSpectatedAgentFocusOut(Agent followedAgent)
	{
		SpectatedAgentName = "";
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		PrevCharacterKey?.OnFinalize();
		NextCharacterKey?.OnFinalize();
		TakeControlKey?.OnFinalize();
	}

	public void SetMainAgentStatus(bool isDead)
	{
		if (_isMainHeroDead != isDead)
		{
			_isMainHeroDead = isDead;
			UpdateStatusText();
		}
	}

	private void UpdateStatusText()
	{
		if (_isMainHeroDead)
		{
			StatusText = _deadTextObject.ToString();
		}
		else
		{
			StatusText = string.Empty;
		}
	}

	public void SetPrevCharacterInputKey(GameKey gameKey)
	{
		PrevCharacterKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
	}

	public void SetNextCharacterInputKey(GameKey gameKey)
	{
		NextCharacterKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
	}

	public void SetTakeControlInputKey(GameKey gameKey)
	{
		TakeControlKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
	}
}
