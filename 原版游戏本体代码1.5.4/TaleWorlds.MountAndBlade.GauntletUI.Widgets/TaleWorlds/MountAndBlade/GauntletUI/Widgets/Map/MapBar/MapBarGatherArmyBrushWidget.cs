using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapBar;

public class MapBarGatherArmyBrushWidget : BrushWidget
{
	private bool _isInfoBarExtended;

	private bool _initialized;

	private MapInfoBarWidget _infoBarWidget;

	private bool _isGatherArmyEnabled;

	private bool _isGatherArmyVisible;

	public MapInfoBarWidget InfoBarWidget
	{
		get
		{
			return _infoBarWidget;
		}
		set
		{
			if (_infoBarWidget != value)
			{
				_infoBarWidget = value;
				_infoBarWidget.OnMapInfoBarExtendStateChange += OnMapInfoBarExtendStateChange;
			}
		}
	}

	public bool IsGatherArmyEnabled
	{
		get
		{
			return _isGatherArmyEnabled;
		}
		set
		{
			if (_isGatherArmyEnabled != value)
			{
				_isGatherArmyEnabled = value;
				UpdateVisualState();
			}
		}
	}

	public bool IsGatherArmyVisible
	{
		get
		{
			return _isGatherArmyVisible;
		}
		set
		{
			if (_isGatherArmyVisible != value)
			{
				_isGatherArmyVisible = value;
				UpdateVisualState();
			}
		}
	}

	public MapBarGatherArmyBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			UpdateVisualState();
			_initialized = true;
		}
	}

	private void UpdateVisualState()
	{
		base.IsEnabled = IsGatherArmyVisible;
		if (IsGatherArmyVisible)
		{
			if (_isInfoBarExtended)
			{
				SetState("Extended");
			}
			else
			{
				SetState("Default");
			}
		}
		else
		{
			SetState("Disabled");
		}
	}

	private void OnMapInfoBarExtendStateChange(bool newState)
	{
		_isInfoBarExtended = newState;
		UpdateVisualState();
	}
}
