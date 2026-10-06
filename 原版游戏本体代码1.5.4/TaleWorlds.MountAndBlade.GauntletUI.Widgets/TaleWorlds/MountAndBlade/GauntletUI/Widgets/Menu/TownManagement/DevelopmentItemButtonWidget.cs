using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class DevelopmentItemButtonWidget : ButtonWidget
{
	private bool _isParentInitialized;

	private bool _isSelectedItem;

	private int _level;

	private int _progress;

	private bool _isDaily;

	private bool _isProgressIndicatorsEnabled;

	private Widget _developmentLevelVisualWidget;

	private Widget _developmentBackVisualWidget;

	private Widget _developmentFrontVisualWidget;

	private Widget _selectedBlackOverlayWidget;

	private ButtonWidget _addToQueueButtonWidget;

	private ButtonWidget _setAsActiveButtonWidget;

	private DevelopmentNameTextWidget _nameTextWidget;

	private Widget _progressClipWidget;

	private bool _isProgressShown;

	private bool _canBuild;

	[Editor(false)]
	public bool IsSelectedItem
	{
		get
		{
			return _isSelectedItem;
		}
		set
		{
			if (_isSelectedItem != value)
			{
				_isSelectedItem = value;
				OnPropertyChanged(value, "IsSelectedItem");
			}
		}
	}

	[Editor(false)]
	public Widget SelectedBlackOverlayWidget
	{
		get
		{
			return _selectedBlackOverlayWidget;
		}
		set
		{
			if (_selectedBlackOverlayWidget != value)
			{
				_selectedBlackOverlayWidget = value;
				OnPropertyChanged(value, "SelectedBlackOverlayWidget");
			}
		}
	}

	[Editor(false)]
	public DevelopmentNameTextWidget NameTextWidget
	{
		get
		{
			return _nameTextWidget;
		}
		set
		{
			if (_nameTextWidget != value)
			{
				_nameTextWidget = value;
				OnPropertyChanged(value, "NameTextWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget AddToQueueButtonWidget
	{
		get
		{
			return _addToQueueButtonWidget;
		}
		set
		{
			if (_addToQueueButtonWidget != value)
			{
				_addToQueueButtonWidget = value;
				OnPropertyChanged(value, "AddToQueueButtonWidget");
				value.ClickEventHandlers.Add(OnAddToQueueClick);
			}
		}
	}

	[Editor(false)]
	public ButtonWidget SetAsActiveButtonWidget
	{
		get
		{
			return _setAsActiveButtonWidget;
		}
		set
		{
			if (_setAsActiveButtonWidget != value)
			{
				_setAsActiveButtonWidget = value;
				OnPropertyChanged(value, "SetAsActiveButtonWidget");
				value.ClickEventHandlers.Add(OnSetAsActiveDevelopmentClick);
			}
		}
	}

	[Editor(false)]
	public Widget DevelopmentLevelVisualWidget
	{
		get
		{
			return _developmentLevelVisualWidget;
		}
		set
		{
			if (_developmentLevelVisualWidget != value)
			{
				_developmentLevelVisualWidget = value;
				OnPropertyChanged(value, "DevelopmentLevelVisualWidget");
				UpdateDevelopmentLevelVisual(Level);
			}
		}
	}

	[Editor(false)]
	public Widget ProgressClipWidget
	{
		get
		{
			return _progressClipWidget;
		}
		set
		{
			if (_progressClipWidget != value)
			{
				_progressClipWidget = value;
				OnPropertyChanged(value, "ProgressClipWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsProgressShown
	{
		get
		{
			return _isProgressShown;
		}
		set
		{
			if (_isProgressShown != value)
			{
				_isProgressShown = value;
				OnPropertyChanged(value, "IsProgressShown");
			}
		}
	}

	[Editor(false)]
	public bool CanBuild
	{
		get
		{
			return _canBuild;
		}
		set
		{
			if (_canBuild != value)
			{
				_canBuild = value;
				OnPropertyChanged(value, "CanBuild");
			}
		}
	}

	[Editor(false)]
	public Widget DevelopmentBackVisualWidget
	{
		get
		{
			return _developmentBackVisualWidget;
		}
		set
		{
			if (_developmentBackVisualWidget != value)
			{
				_developmentBackVisualWidget = value;
				OnPropertyChanged(value, "DevelopmentBackVisualWidget");
			}
		}
	}

	[Editor(false)]
	public Widget DevelopmentFrontVisualWidget
	{
		get
		{
			return _developmentFrontVisualWidget;
		}
		set
		{
			if (_developmentFrontVisualWidget != value)
			{
				_developmentFrontVisualWidget = value;
				OnPropertyChanged(value, "DevelopmentFrontVisualWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsProgressIndicatorsEnabled
	{
		get
		{
			return _isProgressIndicatorsEnabled;
		}
		set
		{
			if (_isProgressIndicatorsEnabled != value)
			{
				_isProgressIndicatorsEnabled = value;
				OnPropertyChanged(value, "IsProgressIndicatorsEnabled");
			}
		}
	}

	[Editor(false)]
	public bool IsDaily
	{
		get
		{
			return _isDaily;
		}
		set
		{
			if (_isDaily != value)
			{
				_isDaily = value;
				OnPropertyChanged(value, "IsDaily");
			}
		}
	}

	[Editor(false)]
	public int Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (_level != value)
			{
				_level = value;
				OnPropertyChanged(value, "Level");
				UpdateDevelopmentLevelVisual(value);
			}
		}
	}

	[Editor(false)]
	public int Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			if (_progress != value)
			{
				_progress = value;
				OnPropertyChanged(value, "Progress");
			}
		}
	}

	public DevelopmentItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_isParentInitialized && base.ParentWidget is ButtonWidget buttonWidget)
		{
			buttonWidget.ClickEventHandlers.Add(OnParentClick);
			_isParentInitialized = true;
		}
		if (!IsDaily)
		{
			HandleFocus();
			HandleEnabledStates();
			DevelopmentFrontVisualWidget.HeightSizePolicy = SizePolicy.Fixed;
			DevelopmentFrontVisualWidget.WidthSizePolicy = SizePolicy.Fixed;
			DevelopmentFrontVisualWidget.ScaledSuggestedHeight = DevelopmentBackVisualWidget.Size.Y;
			DevelopmentFrontVisualWidget.ScaledSuggestedWidth = DevelopmentBackVisualWidget.Size.X;
			if (IsProgressShown)
			{
				if (Progress > 0 || Level == 0)
				{
					ProgressClipWidget.HeightSizePolicy = SizePolicy.Fixed;
					ProgressClipWidget.ScaledSuggestedHeight = DevelopmentBackVisualWidget.Size.Y * ((float)Progress / 100f);
				}
				if (Level == 0)
				{
					DevelopmentBackVisualWidget.AlphaFactor = 0.8f;
					DevelopmentBackVisualWidget.SaturationFactor = -80f;
				}
				else
				{
					DevelopmentBackVisualWidget.AlphaFactor = 0.2f;
				}
			}
			else
			{
				ProgressClipWidget.HeightSizePolicy = SizePolicy.StretchToParent;
			}
			HandleChildrenVisibilities();
		}
		DevelopmentBackVisualWidget.CircularClipEnabled = true;
		DevelopmentBackVisualWidget.CircularClipRadius = DevelopmentBackVisualWidget.Size.X / 2f * base._inverseScaleToUse - 10f * base._scaleToUse;
		DevelopmentBackVisualWidget.CircularClipSmoothingRadius = 3f;
	}

	private void HandleFocus()
	{
		if (IsSelectedItem)
		{
			if (base.EventManager.LatestMouseUpWidget != null && base.EventManager.LatestMouseUpWidget != base.ParentWidget && (!(base.EventManager.LatestMouseUpWidget is DevelopmentItemVisualButtonWidget developmentItemVisualButtonWidget) || !(developmentItemVisualButtonWidget.SpriteCode == (DevelopmentBackVisualWidget as DevelopmentItemVisualButtonWidget)?.SpriteCode)))
			{
				IsSelectedItem = false;
			}
			if (base.EventManager.DraggedWidget != null)
			{
				IsSelectedItem = false;
			}
		}
	}

	private void HandleChildrenVisibilities()
	{
		SetAsActiveButtonWidget.IsVisible = IsSelectedItem;
		AddToQueueButtonWidget.IsVisible = IsSelectedItem;
		SelectedBlackOverlayWidget.IsVisible = IsSelectedItem;
	}

	private void HandleEnabledStates()
	{
		base.ParentWidget.DoNotPassEventsToChildren = !IsSelectedItem;
		base.DoNotPassEventsToChildren = !IsSelectedItem;
	}

	private void OnParentClick(Widget widget)
	{
		if (!IsSelectedItem && CanBuild)
		{
			IsSelectedItem = true;
		}
		if (!CanBuild)
		{
			NameTextWidget?.StartMaxTextAnimation();
		}
	}

	private void OnAddToQueueClick(Widget widget)
	{
		IsSelectedItem = false;
		EventFired("OnAddToQueue");
	}

	private void OnSetAsActiveDevelopmentClick(Widget widget)
	{
		IsSelectedItem = false;
		EventFired("SetAsActive");
	}

	private void UpdateDevelopmentLevelVisual(int level)
	{
		if (!IsDaily)
		{
			DevelopmentLevelVisualWidget.SetState(level.ToString());
			DevelopmentLevelVisualWidget.IsVisible = level >= 0;
		}
	}
}
