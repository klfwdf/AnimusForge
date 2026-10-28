using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

public class PowerLevelComparerWidget : Widget
{
	private Widget _centerSeperatorWidget;

	private bool _isCenterSeperatorEnabled;

	private float _centerSpace;

	private double _defenderPower;

	private double _attackerPower;

	private double _initialAttackerBattlePower;

	private double _initialDefenderBattlePower;

	private Widget _defenderPowerWidget;

	private Widget _attackerPowerWidget;

	private ListPanel _powerListPanel;

	private ListPanel _defenderPowerListPanel;

	private ListPanel _attackerPowerListPanel;

	private ContainerItemDescription _defenderSideInitialPowerLevelDescription;

	private ContainerItemDescription _attackerSideInitialPowerLevelDescription;

	private ContainerItemDescription _defenderSidePowerLevelDescription;

	private ContainerItemDescription _defenderSideEmptyPowerLevelDescription;

	private ContainerItemDescription _attackerSidePowerLevelDescription;

	private ContainerItemDescription _attackerSideEmptyPowerLevelDescription;

	[Editor(false)]
	public bool IsCenterSeperatorEnabled
	{
		get
		{
			return _isCenterSeperatorEnabled;
		}
		set
		{
			if (_isCenterSeperatorEnabled != value)
			{
				_isCenterSeperatorEnabled = value;
				OnPropertyChanged(value, "IsCenterSeperatorEnabled");
			}
		}
	}

	[Editor(false)]
	public float CenterSpace
	{
		get
		{
			return _centerSpace;
		}
		set
		{
			if (_centerSpace != value)
			{
				_centerSpace = value;
				OnPropertyChanged(value, "CenterSpace");
			}
		}
	}

	[Editor(false)]
	public double DefenderPower
	{
		get
		{
			return _defenderPower;
		}
		set
		{
			if (_defenderPower != value && !double.IsNaN(value))
			{
				_defenderPower = value;
				OnPropertyChanged(value, "DefenderPower");
			}
		}
	}

	[Editor(false)]
	public double AttackerPower
	{
		get
		{
			return _attackerPower;
		}
		set
		{
			if (_attackerPower != value && !double.IsNaN(value))
			{
				_attackerPower = value;
				OnPropertyChanged(value, "AttackerPower");
			}
		}
	}

	[Editor(false)]
	public double InitialAttackerBattlePower
	{
		get
		{
			return _initialAttackerBattlePower;
		}
		set
		{
			if (_initialAttackerBattlePower != value && !double.IsNaN(value))
			{
				_initialAttackerBattlePower = value;
				OnPropertyChanged(value, "InitialAttackerBattlePower");
			}
		}
	}

	[Editor(false)]
	public double InitialDefenderBattlePower
	{
		get
		{
			return _initialDefenderBattlePower;
		}
		set
		{
			if (_initialDefenderBattlePower != value && !double.IsNaN(value))
			{
				_initialDefenderBattlePower = value;
				OnPropertyChanged(value, "InitialDefenderBattlePower");
			}
		}
	}

	[Editor(false)]
	public Widget AttackerPowerWidget
	{
		get
		{
			return _attackerPowerWidget;
		}
		set
		{
			if (_attackerPowerWidget != value)
			{
				_attackerPowerWidget = value;
				OnPropertyChanged(value, "AttackerPowerWidget");
			}
		}
	}

	[Editor(false)]
	public Widget DefenderPowerWidget
	{
		get
		{
			return _defenderPowerWidget;
		}
		set
		{
			if (_defenderPowerWidget != value)
			{
				_defenderPowerWidget = value;
				OnPropertyChanged(value, "DefenderPowerWidget");
			}
		}
	}

	[Editor(false)]
	public ListPanel PowerListPanel
	{
		get
		{
			return _powerListPanel;
		}
		set
		{
			if (_powerListPanel != value)
			{
				_powerListPanel = value;
				OnPropertyChanged(value, "PowerListPanel");
			}
		}
	}

	[Editor(false)]
	public ListPanel AttackerPowerListPanel
	{
		get
		{
			return _attackerPowerListPanel;
		}
		set
		{
			if (_attackerPowerListPanel != value)
			{
				_attackerPowerListPanel = value;
				OnPropertyChanged(value, "AttackerPowerListPanel");
			}
		}
	}

	[Editor(false)]
	public ListPanel DefenderPowerListPanel
	{
		get
		{
			return _defenderPowerListPanel;
		}
		set
		{
			if (_defenderPowerListPanel != value)
			{
				_defenderPowerListPanel = value;
				OnPropertyChanged(value, "DefenderPowerListPanel");
			}
		}
	}

	[Editor(false)]
	public Widget CenterSeperatorWidget
	{
		get
		{
			return _centerSeperatorWidget;
		}
		set
		{
			if (_centerSeperatorWidget != value)
			{
				_centerSeperatorWidget = value;
				OnPropertyChanged(value, "CenterSeperatorWidget");
			}
		}
	}

	public PowerLevelComparerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		if (AttackerPowerWidget != null)
		{
			AttackerPowerWidget.AlphaFactor = 0.7f;
			AttackerPowerWidget.ValueFactor = -70f;
		}
		if (DefenderPowerWidget != null)
		{
			DefenderPowerWidget.AlphaFactor = 0.7f;
			DefenderPowerWidget.ValueFactor = -70f;
		}
		if (_powerListPanel != null)
		{
			if (_defenderSideInitialPowerLevelDescription == null)
			{
				_defenderSideInitialPowerLevelDescription = new ContainerItemDescription();
				_defenderSideInitialPowerLevelDescription.WidgetId = "DefenderSideInitialPowerLevel";
				_powerListPanel.AddItemDescription(_defenderSideInitialPowerLevelDescription);
			}
			if (_attackerSideInitialPowerLevelDescription == null)
			{
				_attackerSideInitialPowerLevelDescription = new ContainerItemDescription();
				_attackerSideInitialPowerLevelDescription.WidgetId = "AttackerSideInitialPowerLevel";
				_powerListPanel.AddItemDescription(_attackerSideInitialPowerLevelDescription);
			}
		}
		if (_defenderPowerListPanel != null)
		{
			if (_defenderSidePowerLevelDescription == null)
			{
				_defenderSidePowerLevelDescription = new ContainerItemDescription();
				_defenderSidePowerLevelDescription.WidgetId = "DefenderSidePowerLevel";
				_defenderPowerListPanel.AddItemDescription(_defenderSidePowerLevelDescription);
			}
			if (_defenderSideEmptyPowerLevelDescription == null)
			{
				_defenderSideEmptyPowerLevelDescription = new ContainerItemDescription();
				_defenderSideEmptyPowerLevelDescription.WidgetId = "DefenderSideEmptyPowerLevel";
				_defenderPowerListPanel.AddItemDescription(_defenderSideEmptyPowerLevelDescription);
			}
		}
		if (_attackerPowerListPanel != null)
		{
			if (_attackerSidePowerLevelDescription == null)
			{
				_attackerSidePowerLevelDescription = new ContainerItemDescription();
				_attackerSidePowerLevelDescription.WidgetId = "AttackerSidePowerLevel";
				_attackerPowerListPanel.AddItemDescription(_attackerSidePowerLevelDescription);
			}
			if (_attackerSideEmptyPowerLevelDescription == null)
			{
				_attackerSideEmptyPowerLevelDescription = new ContainerItemDescription();
				_attackerSideEmptyPowerLevelDescription.WidgetId = "AttackerSideEmptyPowerLevel";
				_attackerPowerListPanel.AddItemDescription(_attackerSideEmptyPowerLevelDescription);
			}
		}
		if (_defenderSideInitialPowerLevelDescription != null && _attackerSideInitialPowerLevelDescription != null)
		{
			float num = (float)InitialDefenderBattlePower / (float)(InitialAttackerBattlePower + InitialDefenderBattlePower);
			float num2 = (float)InitialAttackerBattlePower / (float)(InitialAttackerBattlePower + InitialDefenderBattlePower);
			if (_defenderSideInitialPowerLevelDescription.WidthStretchRatio != num || _attackerSideInitialPowerLevelDescription.WidthStretchRatio != num2)
			{
				_defenderSideInitialPowerLevelDescription.WidthStretchRatio = num;
				_attackerSideInitialPowerLevelDescription.WidthStretchRatio = num2;
				SetMeasureAndLayoutDirty();
			}
		}
		if (_defenderSidePowerLevelDescription != null && _defenderSideEmptyPowerLevelDescription != null)
		{
			float num3 = 1f - (float)DefenderPower / (float)InitialDefenderBattlePower;
			float num4 = (float)DefenderPower / (float)InitialDefenderBattlePower;
			if (_defenderSideEmptyPowerLevelDescription.WidthStretchRatio != num3 || _defenderSidePowerLevelDescription.WidthStretchRatio != num4)
			{
				_defenderSidePowerLevelDescription.WidthStretchRatio = num4;
				_defenderSideEmptyPowerLevelDescription.WidthStretchRatio = num3;
				SetMeasureAndLayoutDirty();
			}
		}
		if (_attackerSidePowerLevelDescription != null && _attackerSideEmptyPowerLevelDescription != null)
		{
			float num5 = 1f - (float)AttackerPower / (float)InitialAttackerBattlePower;
			float num6 = (float)AttackerPower / (float)InitialAttackerBattlePower;
			if (_attackerSidePowerLevelDescription.WidthStretchRatio != num6 || _attackerSideEmptyPowerLevelDescription.WidthStretchRatio != num5)
			{
				_attackerSidePowerLevelDescription.WidthStretchRatio = num6;
				_attackerSideEmptyPowerLevelDescription.WidthStretchRatio = num5;
				SetMeasureAndLayoutDirty();
			}
		}
		if (IsCenterSeperatorEnabled && CenterSeperatorWidget != null)
		{
			CenterSeperatorWidget.ScaledPositionXOffset = AttackerPowerWidget.Size.X - (CenterSeperatorWidget.Size.X - CenterSpace) / 2f;
		}
		base.OnLateUpdate(dt);
	}
}
