using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

public class PartyNameplateWidget : Widget
{
	public enum TutorialAnimState
	{
		Idle,
		Start,
		FirstFrame,
		Playing
	}

	protected bool _isFirstFrame = true;

	protected float _screenWidth;

	protected float _screenHeight;

	protected float _initialDelayAmount = 2f;

	protected int _defaultNameplateFontSize;

	protected TutorialAnimState _tutorialAnimState;

	private Vec2 _position;

	private Vec2 _headPosition;

	private TextWidget _nameplateTextWidget;

	private TextWidget _nameplateFullNameTextWidget;

	private TextWidget _speedTextWidget;

	private Widget _speedIconWidget;

	private Widget _parleyIconWidget;

	private TextWidget _nameplateExtraInfoTextWidget;

	private Widget _trackerFrame;

	private Widget _disorganizedWidget;

	private ListPanel _nameplateLayoutListPanel;

	private MaskedTextureWidget _partyBannerWidget;

	private bool _isVisibleOnMap;

	private bool _isInside;

	private bool _isBehind;

	private bool _isHigh;

	private bool _isInArmy;

	private bool _isInSettlement;

	private bool _isArmy;

	private bool _isTargetedByTutorial;

	private bool _shouldShowFullName;

	private bool _canParley;

	private bool _isDisorganized;

	protected float _animSpeedModifier => 8f;

	protected int _armyFontSizeOffset => 10;

	public Widget HeadGroupWidget { get; set; }

	public ListPanel NameplateLayoutListPanel
	{
		get
		{
			return _nameplateLayoutListPanel;
		}
		set
		{
			if (_nameplateLayoutListPanel != value)
			{
				_nameplateLayoutListPanel = value;
				OnPropertyChanged(value, "NameplateLayoutListPanel");
			}
		}
	}

	public MaskedTextureWidget PartyBannerWidget
	{
		get
		{
			return _partyBannerWidget;
		}
		set
		{
			if (_partyBannerWidget != value)
			{
				_partyBannerWidget = value;
				OnPropertyChanged(value, "PartyBannerWidget");
			}
		}
	}

	public Widget TrackerFrame
	{
		get
		{
			return _trackerFrame;
		}
		set
		{
			if (_trackerFrame != value)
			{
				_trackerFrame = value;
				OnPropertyChanged(value, "TrackerFrame");
			}
		}
	}

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChanged(value, "Position");
			}
		}
	}

	public Vec2 HeadPosition
	{
		get
		{
			return _headPosition;
		}
		set
		{
			if (_headPosition != value)
			{
				_headPosition = value;
				OnPropertyChanged(value, "HeadPosition");
			}
		}
	}

	public bool ShouldShowFullName
	{
		get
		{
			return _shouldShowFullName;
		}
		set
		{
			if (_shouldShowFullName != value)
			{
				_shouldShowFullName = value;
				OnPropertyChanged(value, "ShouldShowFullName");
			}
		}
	}

	public bool CanParley
	{
		get
		{
			return _canParley;
		}
		set
		{
			if (_canParley != value)
			{
				_canParley = value;
				OnPropertyChanged(value, "CanParley");
			}
		}
	}

	public bool IsTargetedByTutorial
	{
		get
		{
			return _isTargetedByTutorial;
		}
		set
		{
			if (_isTargetedByTutorial != value)
			{
				_isTargetedByTutorial = value;
				OnPropertyChanged(value, "IsTargetedByTutorial");
				_tutorialAnimState = TutorialAnimState.Start;
			}
		}
	}

	public bool IsInArmy
	{
		get
		{
			return _isInArmy;
		}
		set
		{
			if (_isInArmy != value)
			{
				_isInArmy = value;
				OnPropertyChanged(value, "IsInArmy");
			}
		}
	}

	public bool IsInSettlement
	{
		get
		{
			return _isInSettlement;
		}
		set
		{
			if (_isInSettlement != value)
			{
				_isInSettlement = value;
				OnPropertyChanged(value, "IsInSettlement");
			}
		}
	}

	public bool IsArmy
	{
		get
		{
			return _isArmy;
		}
		set
		{
			if (_isArmy != value)
			{
				_isArmy = value;
				OnPropertyChanged(value, "IsArmy");
			}
		}
	}

	public bool IsVisibleOnMap
	{
		get
		{
			return _isVisibleOnMap;
		}
		set
		{
			if (_isVisibleOnMap != value)
			{
				_isVisibleOnMap = value;
				OnPropertyChanged(value, "IsVisibleOnMap");
			}
		}
	}

	public bool IsInside
	{
		get
		{
			return _isInside;
		}
		set
		{
			if (_isInside != value)
			{
				_isInside = value;
				OnPropertyChanged(value, "IsInside");
			}
		}
	}

	public bool IsHigh
	{
		get
		{
			return _isHigh;
		}
		set
		{
			if (_isHigh != value)
			{
				_isHigh = value;
				OnPropertyChanged(value, "IsHigh");
			}
		}
	}

	public bool IsBehind
	{
		get
		{
			return _isBehind;
		}
		set
		{
			if (_isBehind != value)
			{
				_isBehind = value;
				OnPropertyChanged(value, "IsBehind");
			}
		}
	}

	public bool IsDisorganized
	{
		get
		{
			return _isDisorganized;
		}
		set
		{
			if (_isDisorganized != value)
			{
				_isDisorganized = value;
				OnPropertyChanged(value, "IsDisorganized");
			}
		}
	}

	public TextWidget NameplateTextWidget
	{
		get
		{
			return _nameplateTextWidget;
		}
		set
		{
			if (_nameplateTextWidget != value)
			{
				_nameplateTextWidget = value;
				OnPropertyChanged(value, "NameplateTextWidget");
			}
		}
	}

	public TextWidget NameplateExtraInfoTextWidget
	{
		get
		{
			return _nameplateExtraInfoTextWidget;
		}
		set
		{
			if (_nameplateExtraInfoTextWidget != value)
			{
				_nameplateExtraInfoTextWidget = value;
				OnPropertyChanged(value, "NameplateExtraInfoTextWidget");
			}
		}
	}

	public TextWidget NameplateFullNameTextWidget
	{
		get
		{
			return _nameplateFullNameTextWidget;
		}
		set
		{
			if (_nameplateFullNameTextWidget != value)
			{
				_nameplateFullNameTextWidget = value;
				OnPropertyChanged(value, "NameplateFullNameTextWidget");
			}
		}
	}

	public TextWidget SpeedTextWidget
	{
		get
		{
			return _speedTextWidget;
		}
		set
		{
			if (_speedTextWidget != value)
			{
				_speedTextWidget = value;
				OnPropertyChanged(value, "SpeedTextWidget");
			}
		}
	}

	public Widget SpeedIconWidget
	{
		get
		{
			return _speedIconWidget;
		}
		set
		{
			if (value != _speedIconWidget)
			{
				_speedIconWidget = value;
				OnPropertyChanged(value, "SpeedIconWidget");
			}
		}
	}

	public Widget ParleyIconWidget
	{
		get
		{
			return _parleyIconWidget;
		}
		set
		{
			if (value != _parleyIconWidget)
			{
				_parleyIconWidget = value;
				OnPropertyChanged(value, "ParleyIconWidget");
			}
		}
	}

	public Widget DisorganizedWidget
	{
		get
		{
			return _disorganizedWidget;
		}
		set
		{
			if (_disorganizedWidget != value)
			{
				_disorganizedWidget = value;
				OnPropertyChanged(value, "DisorganizedWidget");
			}
		}
	}

	public PartyNameplateWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isFirstFrame)
		{
			NameplateFullNameTextWidget.Brush.GlobalAlphaFactor = 0f;
			NameplateTextWidget.Brush.GlobalAlphaFactor = 0f;
			NameplateExtraInfoTextWidget.Brush.GlobalAlphaFactor = 0f;
			PartyBannerWidget.Brush.GlobalAlphaFactor = 0f;
			SpeedTextWidget.Brush.GlobalAlphaFactor = 0f;
			ParleyIconWidget.AlphaFactor = 0f;
			_defaultNameplateFontSize = NameplateTextWidget.ReadOnlyBrush.FontSize;
			_isFirstFrame = false;
		}
		int num = (IsArmy ? (_defaultNameplateFontSize + _armyFontSizeOffset) : _defaultNameplateFontSize);
		if (NameplateTextWidget.Brush.FontSize != num)
		{
			NameplateTextWidget.Brush.FontSize = num;
		}
		UpdateNameplatesScreenPosition();
		UpdateNameplatesVisibility(dt);
		UpdateTutorialStatus();
	}

	protected virtual void UpdateNameplatesVisibility(float dt)
	{
		float num = 0f;
		float valueTo = 0f;
		PartyBannerWidget.IsVisible = true;
		NameplateTextWidget.IsVisible = IsVisibleOnMap;
		NameplateFullNameTextWidget.IsVisible = IsVisibleOnMap;
		SpeedTextWidget.IsVisible = IsVisibleOnMap;
		SpeedIconWidget.IsVisible = IsVisibleOnMap;
		DisorganizedWidget.IsVisible = IsVisibleOnMap && IsDisorganized;
		num = (IsVisibleOnMap ? 1 : 0);
		TrackerFrame.IsVisible = false;
		base.IsEnabled = false;
		if (IsVisibleOnMap)
		{
			if (_initialDelayAmount <= 0f)
			{
				valueTo = (ShouldShowFullName ? 1 : 0);
			}
			else
			{
				_initialDelayAmount -= dt;
				valueTo = 1f;
			}
		}
		NameplateTextWidget.Brush.GlobalAlphaFactor = MathF.Lerp(NameplateTextWidget.ReadOnlyBrush.GlobalAlphaFactor, num, dt * _animSpeedModifier);
		NameplateFullNameTextWidget.Brush.GlobalAlphaFactor = MathF.Lerp(NameplateFullNameTextWidget.ReadOnlyBrush.GlobalAlphaFactor, valueTo, dt * _animSpeedModifier);
		SpeedTextWidget.Brush.GlobalAlphaFactor = MathF.Lerp(SpeedTextWidget.ReadOnlyBrush.GlobalAlphaFactor, valueTo, dt * _animSpeedModifier);
		float alphaFactor = MathF.Lerp(SpeedIconWidget.AlphaFactor, valueTo, dt * _animSpeedModifier);
		SpeedIconWidget.SetGlobalAlphaRecursively(alphaFactor);
		NameplateExtraInfoTextWidget.Brush.GlobalAlphaFactor = MathF.Lerp(NameplateExtraInfoTextWidget.ReadOnlyBrush.GlobalAlphaFactor, ShouldShowFullName ? 1 : 0, dt * _animSpeedModifier);
		PartyBannerWidget.Brush.GlobalAlphaFactor = MathF.Lerp(PartyBannerWidget.ReadOnlyBrush.GlobalAlphaFactor, num, dt * _animSpeedModifier);
		ParleyIconWidget.AlphaFactor = MathF.Lerp(ParleyIconWidget.AlphaFactor, CanParley ? 1 : 0, dt * _animSpeedModifier);
	}

	protected virtual void UpdateNameplatesScreenPosition()
	{
		_screenWidth = base.Context.EventManager.PageSize.X;
		_screenHeight = base.Context.EventManager.PageSize.Y;
		base.IsVisible = IsVisibleOnMap && !IsPositionOutsideScreen();
		if (base.IsVisible)
		{
			float num = HeadGroupWidget?.Size.Y ?? 0f;
			NameplateLayoutListPanel.ScaledPositionXOffset = base.Size.X / 2f - PartyBannerWidget.Size.X;
			NameplateLayoutListPanel.ScaledPositionYOffset = Position.y - HeadPosition.y + num;
			base.ScaledPositionXOffset = HeadPosition.x - base.Size.X / 2f;
			base.ScaledPositionYOffset = HeadPosition.y - num;
		}
	}

	private void UpdateTutorialStatus()
	{
		if (_tutorialAnimState == TutorialAnimState.Start)
		{
			_tutorialAnimState = TutorialAnimState.FirstFrame;
		}
		else
		{
			_ = _tutorialAnimState;
			_ = 2;
		}
		if (IsTargetedByTutorial)
		{
			SetState("Default");
		}
		else
		{
			SetState("Disabled");
		}
	}

	protected bool IsPositionOutsideScreen()
	{
		if (!(Position.X > _screenWidth) && !(HeadPosition.X > _screenWidth) && !(Position.X < 0f) && !(HeadPosition.X < 0f) && !(Position.Y > _screenHeight) && !(HeadPosition.Y > _screenHeight) && !(Position.Y < 0f))
		{
			return HeadPosition.Y < 0f;
		}
		return true;
	}
}
