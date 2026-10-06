using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleFormationClassBrushWidget : BrushWidget
{
	private bool _hasBaseBrushSet;

	private int _formationClass;

	private Color _erroredColor;

	private bool _isErrored;

	private Brush _unsetBrush;

	private Brush _infantryBrush;

	private Brush _rangedBrush;

	private Brush _cavalryBrush;

	private Brush _horseArcherBrush;

	private Brush _infantryAndRangedBrush;

	private Brush _cavalryAndHorseArcherBrush;

	[Editor(false)]
	public int FormationClass
	{
		get
		{
			return _formationClass;
		}
		set
		{
			if (value != _formationClass || !_hasBaseBrushSet)
			{
				_formationClass = value;
				OnPropertyChanged(value, "FormationClass");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Color ErroredColor
	{
		get
		{
			return _erroredColor;
		}
		set
		{
			if (value != _erroredColor)
			{
				_erroredColor = value;
				OnPropertyChanged(value, "ErroredColor");
			}
		}
	}

	[Editor(false)]
	public bool IsErrored
	{
		get
		{
			return _isErrored;
		}
		set
		{
			if (value != _isErrored)
			{
				_isErrored = value;
				OnPropertyChanged(value, "IsErrored");
				SetColor();
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
	public Brush InfantryBrush
	{
		get
		{
			return _infantryBrush;
		}
		set
		{
			if (value != _infantryBrush)
			{
				_infantryBrush = value;
				OnPropertyChanged(value, "InfantryBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush RangedBrush
	{
		get
		{
			return _rangedBrush;
		}
		set
		{
			if (value != _rangedBrush)
			{
				_rangedBrush = value;
				OnPropertyChanged(value, "RangedBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush CavalryBrush
	{
		get
		{
			return _cavalryBrush;
		}
		set
		{
			if (value != _cavalryBrush)
			{
				_cavalryBrush = value;
				OnPropertyChanged(value, "CavalryBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush HorseArcherBrush
	{
		get
		{
			return _horseArcherBrush;
		}
		set
		{
			if (value != _horseArcherBrush)
			{
				_horseArcherBrush = value;
				OnPropertyChanged(value, "HorseArcherBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush InfantryAndRangedBrush
	{
		get
		{
			return _infantryAndRangedBrush;
		}
		set
		{
			if (value != _infantryAndRangedBrush)
			{
				_infantryAndRangedBrush = value;
				OnPropertyChanged(value, "InfantryAndRangedBrush");
				SetBaseBrush();
			}
		}
	}

	[Editor(false)]
	public Brush CavalryAndHorseArcherBrush
	{
		get
		{
			return _cavalryAndHorseArcherBrush;
		}
		set
		{
			if (value != _cavalryAndHorseArcherBrush)
			{
				_cavalryAndHorseArcherBrush = value;
				OnPropertyChanged(value, "CavalryAndHorseArcherBrush");
				SetBaseBrush();
			}
		}
	}

	public OrderOfBattleFormationClassBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void SetBaseBrush()
	{
		switch (FormationClass)
		{
		case 0:
			base.Brush = UnsetBrush;
			break;
		case 1:
			base.Brush = InfantryBrush;
			break;
		case 2:
			base.Brush = RangedBrush;
			break;
		case 3:
			base.Brush = CavalryBrush;
			break;
		case 4:
			base.Brush = HorseArcherBrush;
			break;
		case 5:
			base.Brush = InfantryAndRangedBrush;
			break;
		case 6:
			base.Brush = CavalryAndHorseArcherBrush;
			break;
		default:
			base.Brush = UnsetBrush;
			break;
		}
		_hasBaseBrushSet = true;
		SetColor();
	}

	private void SetColor()
	{
		if (IsErrored)
		{
			base.Brush.Color = ErroredColor;
		}
	}
}
