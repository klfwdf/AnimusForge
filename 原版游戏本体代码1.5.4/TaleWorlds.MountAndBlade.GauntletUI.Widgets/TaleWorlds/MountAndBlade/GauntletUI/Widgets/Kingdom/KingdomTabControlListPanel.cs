using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Kingdom;

public class KingdomTabControlListPanel : ListPanel
{
	private Widget _armiesPanel;

	private Widget _clansPanel;

	private Widget _policiesPanel;

	private Widget _fiefsPanel;

	private Widget _diplomacyPanel;

	private ButtonWidget _fiefsButton;

	private ButtonWidget _clansButton;

	private ButtonWidget _policiesButton;

	private ButtonWidget _armiesButton;

	private ButtonWidget _diplomacyButton;

	[Editor(false)]
	public Widget DiplomacyPanel
	{
		get
		{
			return _diplomacyPanel;
		}
		set
		{
			if (_diplomacyPanel != value)
			{
				_diplomacyPanel = value;
				OnPropertyChanged(value, "DiplomacyPanel");
			}
		}
	}

	[Editor(false)]
	public Widget ArmiesPanel
	{
		get
		{
			return _armiesPanel;
		}
		set
		{
			if (_armiesPanel != value)
			{
				_armiesPanel = value;
				OnPropertyChanged(value, "ArmiesPanel");
			}
		}
	}

	[Editor(false)]
	public Widget ClansPanel
	{
		get
		{
			return _clansPanel;
		}
		set
		{
			if (_clansPanel != value)
			{
				_clansPanel = value;
				OnPropertyChanged(value, "ClansPanel");
			}
		}
	}

	[Editor(false)]
	public Widget PoliciesPanel
	{
		get
		{
			return _policiesPanel;
		}
		set
		{
			if (_policiesPanel != value)
			{
				_policiesPanel = value;
				OnPropertyChanged(value, "PoliciesPanel");
			}
		}
	}

	[Editor(false)]
	public Widget FiefsPanel
	{
		get
		{
			return _fiefsPanel;
		}
		set
		{
			if (_fiefsPanel != value)
			{
				_fiefsPanel = value;
				OnPropertyChanged(value, "FiefsPanel");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget FiefsButton
	{
		get
		{
			return _fiefsButton;
		}
		set
		{
			if (_fiefsButton != value)
			{
				_fiefsButton = value;
				OnPropertyChanged(value, "FiefsButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget PoliciesButton
	{
		get
		{
			return _policiesButton;
		}
		set
		{
			if (_policiesButton != value)
			{
				_policiesButton = value;
				OnPropertyChanged(value, "PoliciesButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ClansButton
	{
		get
		{
			return _clansButton;
		}
		set
		{
			if (_clansButton != value)
			{
				_clansButton = value;
				OnPropertyChanged(value, "ClansButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ArmiesButton
	{
		get
		{
			return _armiesButton;
		}
		set
		{
			if (_armiesButton != value)
			{
				_armiesButton = value;
				OnPropertyChanged(value, "ArmiesButton");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget DiplomacyButton
	{
		get
		{
			return _diplomacyButton;
		}
		set
		{
			if (_diplomacyButton != value)
			{
				_diplomacyButton = value;
				OnPropertyChanged(value, "DiplomacyButton");
			}
		}
	}

	public KingdomTabControlListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		FiefsButton.IsSelected = FiefsPanel.IsVisible;
		PoliciesButton.IsSelected = PoliciesPanel.IsVisible;
		ClansButton.IsSelected = ClansPanel.IsVisible;
		ArmiesButton.IsSelected = ArmiesPanel.IsVisible;
		DiplomacyButton.IsSelected = DiplomacyPanel.IsVisible;
	}
}
