using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleFormationItemListPanel : ListPanel
{
	private Widget _cardWidget;

	private DropdownWidget _formationClassDropdown;

	private bool _isControlledByPlayer;

	private bool _isClassDropdownEnabled;

	private bool _isSelected;

	private bool _hasFormation;

	private float _defaultFocusYOffsetFromCenter;

	private float _noFormationFocusYOffsetFromCenter;

	[Editor(false)]
	public Widget CardWidget
	{
		get
		{
			return _cardWidget;
		}
		set
		{
			if (value != _cardWidget)
			{
				_cardWidget = value;
				OnPropertyChanged(value, "CardWidget");
			}
		}
	}

	[Editor(false)]
	public DropdownWidget FormationClassDropdown
	{
		get
		{
			return _formationClassDropdown;
		}
		set
		{
			if (value != _formationClassDropdown)
			{
				if (_formationClassDropdown != null)
				{
					DropdownWidget formationClassDropdown = _formationClassDropdown;
					formationClassDropdown.OnOpenStateChanged = (Action<DropdownWidget>)Delegate.Remove(formationClassDropdown.OnOpenStateChanged, new Action<DropdownWidget>(OnClassDropdownEnabledStateChanged));
				}
				_formationClassDropdown = value;
				OnPropertyChanged(value, "FormationClassDropdown");
				if (_formationClassDropdown != null)
				{
					DropdownWidget formationClassDropdown2 = _formationClassDropdown;
					formationClassDropdown2.OnOpenStateChanged = (Action<DropdownWidget>)Delegate.Combine(formationClassDropdown2.OnOpenStateChanged, new Action<DropdownWidget>(OnClassDropdownEnabledStateChanged));
					OnClassDropdownEnabledStateChanged(_formationClassDropdown);
				}
			}
		}
	}

	[Editor(false)]
	public bool IsControlledByPlayer
	{
		get
		{
			return _isControlledByPlayer;
		}
		set
		{
			if (value != _isControlledByPlayer)
			{
				_isControlledByPlayer = value;
				OnPropertyChanged(value, "IsControlledByPlayer");
				if (FormationClassDropdown?.Button != null)
				{
					FormationClassDropdown.Button.IsEnabled = value;
				}
			}
		}
	}

	[Editor(false)]
	public bool IsClassDropdownEnabled
	{
		get
		{
			return _isClassDropdownEnabled;
		}
		set
		{
			if (value != _isClassDropdownEnabled)
			{
				_isClassDropdownEnabled = value;
				OnPropertyChanged(value, "IsClassDropdownEnabled");
				if (FormationClassDropdown != null)
				{
					FormationClassDropdown.IsOpen = value;
				}
			}
		}
	}

	[Editor(false)]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChanged(value, "IsSelected");
				OnStateChanged();
			}
		}
	}

	[Editor(false)]
	public bool HasFormation
	{
		get
		{
			return _hasFormation;
		}
		set
		{
			if (value != _hasFormation)
			{
				_hasFormation = value;
				OnPropertyChanged(value, "HasFormation");
			}
		}
	}

	[Editor(false)]
	public float DefaultFocusYOffsetFromCenter
	{
		get
		{
			return _defaultFocusYOffsetFromCenter;
		}
		set
		{
			if (value != _defaultFocusYOffsetFromCenter)
			{
				_defaultFocusYOffsetFromCenter = value;
				OnPropertyChanged(value, "DefaultFocusYOffsetFromCenter");
			}
		}
	}

	[Editor(false)]
	public float NoFormationFocusYOffsetFromCenter
	{
		get
		{
			return _noFormationFocusYOffsetFromCenter;
		}
		set
		{
			if (value != _noFormationFocusYOffsetFromCenter)
			{
				_noFormationFocusYOffsetFromCenter = value;
				OnPropertyChanged(value, "NoFormationFocusYOffsetFromCenter");
			}
		}
	}

	public OrderOfBattleFormationItemListPanel(UIContext context)
		: base(context)
	{
	}

	private void OnStateChanged()
	{
		if (IsSelected)
		{
			CardWidget?.SetState("Selected");
		}
		else
		{
			CardWidget?.SetState("Default");
		}
	}

	private void OnClassDropdownEnabledStateChanged(DropdownWidget widget)
	{
		IsClassDropdownEnabled = widget.IsOpen;
	}
}
