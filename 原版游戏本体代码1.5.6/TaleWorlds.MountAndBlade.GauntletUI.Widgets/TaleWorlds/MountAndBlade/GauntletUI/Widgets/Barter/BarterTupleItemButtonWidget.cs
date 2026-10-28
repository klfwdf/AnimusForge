using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Barter;

public class BarterTupleItemButtonWidget : ButtonWidget
{
	private bool _initialized;

	private bool _isMultiple;

	private bool _isOffered;

	public ListPanel SliderParentList { get; set; }

	public TextWidget CountText { get; set; }

	[Editor(false)]
	public bool IsMultiple
	{
		get
		{
			return _isMultiple;
		}
		set
		{
			if (_isMultiple != value)
			{
				_isMultiple = value;
				OnPropertyChanged(value, "IsMultiple");
				Refresh();
			}
		}
	}

	[Editor(false)]
	public bool IsOffered
	{
		get
		{
			return _isOffered;
		}
		set
		{
			if (_isOffered != value)
			{
				_isOffered = value;
				OnPropertyChanged(value, "IsOffered");
				Refresh();
			}
		}
	}

	public BarterTupleItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			Refresh();
			_initialized = true;
		}
	}

	private void Refresh()
	{
		SliderParentList.IsVisible = IsMultiple && IsOffered;
		CountText.IsHidden = IsMultiple && IsOffered;
		base.IsSelected = IsOffered;
		base.DoNotAcceptEvents = IsOffered;
	}
}
