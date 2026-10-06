using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyUpgradeButtonWidget : ButtonWidget
{
	private ImageIdentifierWidget _imageIdentifierWidget;

	private Brush _defaultBrush;

	private Brush _unavailableBrush;

	private Brush _insufficientBrush;

	private BrushWidget _marinerTroopBrush;

	private bool _isAvailable;

	private bool _isInsufficient;

	[Editor(false)]
	public ImageIdentifierWidget ImageIdentifierWidget
	{
		get
		{
			return _imageIdentifierWidget;
		}
		set
		{
			if (_imageIdentifierWidget != value)
			{
				_imageIdentifierWidget = value;
				OnPropertyChanged(value, "ImageIdentifierWidget");
			}
		}
	}

	[Editor(false)]
	public Brush DefaultBrush
	{
		get
		{
			return _defaultBrush;
		}
		set
		{
			if (_defaultBrush != value)
			{
				_defaultBrush = value;
				OnPropertyChanged(value, "DefaultBrush");
			}
		}
	}

	[Editor(false)]
	public BrushWidget MarinerTroopBrush
	{
		get
		{
			return _marinerTroopBrush;
		}
		set
		{
			if (_marinerTroopBrush != value)
			{
				_marinerTroopBrush = value;
				OnPropertyChanged(value, "MarinerTroopBrush");
			}
		}
	}

	[Editor(false)]
	public Brush UnavailableBrush
	{
		get
		{
			return _unavailableBrush;
		}
		set
		{
			if (_unavailableBrush != value)
			{
				_unavailableBrush = value;
				OnPropertyChanged(value, "UnavailableBrush");
			}
		}
	}

	[Editor(false)]
	public Brush InsufficientBrush
	{
		get
		{
			return _insufficientBrush;
		}
		set
		{
			if (_insufficientBrush != value)
			{
				_insufficientBrush = value;
				OnPropertyChanged(value, "InsufficientBrush");
			}
		}
	}

	[Editor(false)]
	public bool IsAvailable
	{
		get
		{
			return _isAvailable;
		}
		set
		{
			if (_isAvailable != value)
			{
				_isAvailable = value;
				OnPropertyChanged(value, "IsAvailable");
			}
			UpdateVisual();
		}
	}

	[Editor(false)]
	public bool IsInsufficient
	{
		get
		{
			return _isInsufficient;
		}
		set
		{
			if (_isInsufficient != value)
			{
				_isInsufficient = value;
				OnPropertyChanged(value, "IsInsufficient");
			}
			UpdateVisual();
		}
	}

	public PartyUpgradeButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateVisual()
	{
		if (ImageIdentifierWidget != null && UnavailableBrush != null && InsufficientBrush != null)
		{
			if (!IsAvailable)
			{
				ImageIdentifierWidget.Brush.GlobalColor = new Color(1f, 1f, 1f);
				ImageIdentifierWidget.Brush.SaturationFactor = -100f;
				_marinerTroopBrush.SetState("Disabled");
				base.UpdateChildrenStates = false;
				base.IsEnabled = true;
				base.Brush = UnavailableBrush;
			}
			else if (IsAvailable && IsInsufficient)
			{
				ImageIdentifierWidget.Brush.GlobalColor = new Color(0.9f, 0.5f, 0.5f);
				ImageIdentifierWidget.Brush.SaturationFactor = -150f;
				_marinerTroopBrush.SetState("Disabled");
				base.UpdateChildrenStates = false;
				base.IsEnabled = true;
				base.Brush = InsufficientBrush;
			}
			else
			{
				ImageIdentifierWidget.Brush.GlobalColor = new Color(1f, 1f, 1f);
				ImageIdentifierWidget.Brush.SaturationFactor = 0f;
				_marinerTroopBrush.SetState("Default");
				base.UpdateChildrenStates = true;
				base.IsEnabled = true;
				base.Brush = DefaultBrush;
			}
		}
	}
}
