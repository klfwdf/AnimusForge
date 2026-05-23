using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleFormationFilterVisualBrushWidget : BrushWidget
{
	private bool _hasBaseBrushSet;

	private int _formationFilter;

	private Brush _unsetBrush;

	private Brush _spearBrush;

	private Brush _shieldBrush;

	private Brush _thrownBrush;

	private Brush _heavyBrush;

	private Brush _highTierBrush;

	private Brush _lowTierBrush;

	[Editor(false)]
	public int FormationFilter
	{
		get
		{
			return _formationFilter;
		}
		set
		{
			if (value != _formationFilter || !_hasBaseBrushSet)
			{
				_formationFilter = value;
				OnPropertyChanged(value, "FormationFilter");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush UnsetBrush
	{
		get
		{
			return _unsetBrush;
		}
		set
		{
			if (value != _unsetBrush)
			{
				_unsetBrush = value;
				OnPropertyChanged(value, "UnsetBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush SpearBrush
	{
		get
		{
			return _spearBrush;
		}
		set
		{
			if (value != _spearBrush)
			{
				_spearBrush = value;
				OnPropertyChanged(value, "SpearBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush ShieldBrush
	{
		get
		{
			return _shieldBrush;
		}
		set
		{
			if (value != _shieldBrush)
			{
				_shieldBrush = value;
				OnPropertyChanged(value, "ShieldBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush ThrownBrush
	{
		get
		{
			return _thrownBrush;
		}
		set
		{
			if (value != _thrownBrush)
			{
				_thrownBrush = value;
				OnPropertyChanged(value, "ThrownBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush HeavyBrush
	{
		get
		{
			return _heavyBrush;
		}
		set
		{
			if (value != _heavyBrush)
			{
				_heavyBrush = value;
				OnPropertyChanged(value, "HeavyBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush HighTierBrush
	{
		get
		{
			return _highTierBrush;
		}
		set
		{
			if (value != _highTierBrush)
			{
				_highTierBrush = value;
				OnPropertyChanged(value, "HighTierBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush LowTierBrush
	{
		get
		{
			return _lowTierBrush;
		}
		set
		{
			if (value != _lowTierBrush)
			{
				_lowTierBrush = value;
				OnPropertyChanged(value, "LowTierBrush");
				SetBaseBrush();
			}
		}
	}

	public OrderOfBattleFormationFilterVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void SetBaseBrush()
	{
		switch (FormationFilter)
		{
		case 0:
			base.Brush = UnsetBrush;
			break;
		case 1:
			base.Brush = ShieldBrush;
			break;
		case 2:
			base.Brush = SpearBrush;
			break;
		case 3:
			base.Brush = ThrownBrush;
			break;
		case 4:
			base.Brush = HeavyBrush;
			break;
		case 5:
			base.Brush = HighTierBrush;
			break;
		case 6:
			base.Brush = LowTierBrush;
			break;
		default:
			base.Brush = UnsetBrush;
			break;
		}
		_hasBaseBrushSet = true;
	}
}
