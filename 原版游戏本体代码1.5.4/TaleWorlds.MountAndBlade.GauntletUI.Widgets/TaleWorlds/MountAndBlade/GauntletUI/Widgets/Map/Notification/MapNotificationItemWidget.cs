using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Notification;

public class MapNotificationItemWidget : BrushWidget
{
	private bool _ringHoverBegan;

	private bool _extensionHoverBegan;

	private bool _removeHoverBegan;

	private bool _removeInitiated;

	private bool _imageDetermined;

	private bool _sizeDetermined;

	private bool _isExtended;

	private bool _isFocusItem;

	private float _defaultWidth;

	private float _extendedWidth;

	private bool _isInspectionForced;

	private string _notificationType = "Default";

	private Sprite _defaultWidthSprite;

	private Sprite _extendedWidthSprite;

	private Widget _notificationRingWidget;

	private Widget _notificationRingImageWidget;

	private Widget _notificationExtensionWidget;

	private Widget _notificationTextContainerWidget;

	private ButtonWidget _removeNotificationButtonWidget;

	private RichTextWidget _notificationDescriptionText;

	private InputKeyVisualWidget _removeButtonVisualWidget;

	[Editor(false)]
	public bool IsFocusItem
	{
		get
		{
			return _isFocusItem;
		}
		set
		{
			if (value != _isFocusItem)
			{
				_isFocusItem = value;
				OnPropertyChanged(value, "IsFocusItem");
			}
		}
	}

	[Editor(false)]
	public float DefaultWidth
	{
		get
		{
			return _defaultWidth;
		}
		set
		{
			if (value != _defaultWidth)
			{
				_defaultWidth = value;
				OnPropertyChanged(value, "DefaultWidth");
			}
		}
	}

	[Editor(false)]
	public float ExtendedWidth
	{
		get
		{
			return _extendedWidth;
		}
		set
		{
			if (value != _extendedWidth)
			{
				_extendedWidth = value;
				OnPropertyChanged(value, "ExtendedWidth");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget RemoveNotificationButtonWidget
	{
		get
		{
			return _removeNotificationButtonWidget;
		}
		set
		{
			if (_removeNotificationButtonWidget != value)
			{
				_removeNotificationButtonWidget = value;
				OnPropertyChanged(value, "RemoveNotificationButtonWidget");
				value.ClickEventHandlers.Add(OnRemoveClick);
				value.boolPropertyChanged += RemoveButtonWidgetPropertyChanged;
			}
		}
	}

	[Editor(false)]
	public Widget NotificationRingImageWidget
	{
		get
		{
			return _notificationRingImageWidget;
		}
		set
		{
			if (_notificationRingImageWidget != value)
			{
				_notificationRingImageWidget = value;
				OnPropertyChanged(value, "NotificationRingImageWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsInspectionForced
	{
		get
		{
			return _isInspectionForced;
		}
		set
		{
			if (_isInspectionForced != value)
			{
				_isInspectionForced = value;
				OnPropertyChanged(value, "IsInspectionForced");
			}
		}
	}

	[Editor(false)]
	public string NotificationType
	{
		get
		{
			return _notificationType;
		}
		set
		{
			if (_notificationType != value)
			{
				_notificationType = value;
				OnPropertyChanged(value, "NotificationType");
			}
		}
	}

	[Editor(false)]
	public Sprite DefaultWidthSprite
	{
		get
		{
			return _defaultWidthSprite;
		}
		set
		{
			if (_defaultWidthSprite != value)
			{
				_defaultWidthSprite = value;
				OnPropertyChanged(value, "DefaultWidthSprite");
			}
		}
	}

	[Editor(false)]
	public Sprite ExtendedWidthSprite
	{
		get
		{
			return _extendedWidthSprite;
		}
		set
		{
			if (_extendedWidthSprite != value)
			{
				_extendedWidthSprite = value;
				OnPropertyChanged(value, "ExtendedWidthSprite");
			}
		}
	}

	[Editor(false)]
	public Widget NotificationRingWidget
	{
		get
		{
			return _notificationRingWidget;
		}
		set
		{
			if (_notificationRingWidget != value)
			{
				_notificationRingWidget = value;
				OnPropertyChanged(value, "NotificationRingWidget");
				value.boolPropertyChanged += RingWidgetOnPropertyChanged;
				value.EventFire += InspectionWidgetsEventFire;
			}
		}
	}

	[Editor(false)]
	public Widget NotificationExtensionWidget
	{
		get
		{
			return _notificationExtensionWidget;
		}
		set
		{
			if (_notificationExtensionWidget != value)
			{
				_notificationExtensionWidget = value;
				OnPropertyChanged(value, "NotificationExtensionWidget");
				value.boolPropertyChanged += ExtensionWidgetOnPropertyChanged;
				value.EventFire += InspectionWidgetsEventFire;
			}
		}
	}

	[Editor(false)]
	public Widget NotificationTextContainerWidget
	{
		get
		{
			return _notificationTextContainerWidget;
		}
		set
		{
			if (_notificationTextContainerWidget != value)
			{
				_notificationTextContainerWidget = value;
				OnPropertyChanged(value, "NotificationTextContainerWidget");
			}
		}
	}

	[Editor(false)]
	public RichTextWidget NotificationDescriptionText
	{
		get
		{
			return _notificationDescriptionText;
		}
		set
		{
			if (_notificationDescriptionText != value)
			{
				_notificationDescriptionText = value;
				OnPropertyChanged(value, "NotificationDescriptionText");
			}
		}
	}

	[Editor(false)]
	public InputKeyVisualWidget RemoveButtonVisualWidget
	{
		get
		{
			return _removeButtonVisualWidget;
		}
		set
		{
			if (_removeButtonVisualWidget != value)
			{
				_removeButtonVisualWidget = value;
				OnPropertyChanged(value, "RemoveButtonVisualWidget");
			}
		}
	}

	public MapNotificationItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_imageDetermined)
		{
			NotificationRingImageWidget.RegisterBrushStatesOfWidget();
			NotificationRingImageWidget.SetState(NotificationType);
			_imageDetermined = true;
		}
		if (!_sizeDetermined && NotificationDescriptionText != null)
		{
			DetermineSize();
		}
		bool flag = (_isExtended = _ringHoverBegan || _extensionHoverBegan || _removeHoverBegan);
		if (RemoveButtonVisualWidget != null)
		{
			RemoveButtonVisualWidget.IsVisible = _isExtended && base.EventManager.IsControllerActive;
		}
		NotificationRingWidget.IsEnabled = !_removeInitiated;
		NotificationExtensionWidget.IsEnabled = !_removeInitiated;
		RemoveNotificationButtonWidget.IsVisible = flag && !IsInspectionForced;
		NotificationTextContainerWidget.IsVisible = flag;
		RefreshHorizontalPositioning(dt, flag);
	}

	private void DetermineSize()
	{
		if (NotificationDescriptionText.Size.Y > base.Size.Y - 45f * base._scaleToUse)
		{
			NotificationExtensionWidget.Sprite = ExtendedWidthSprite;
			NotificationExtensionWidget.SuggestedWidth = ExtendedWidth;
		}
		else
		{
			NotificationExtensionWidget.Sprite = DefaultWidthSprite;
		}
		_sizeDetermined = true;
	}

	private void RefreshHorizontalPositioning(float dt, bool shouldExtend)
	{
		float num = NotificationExtensionWidget.Size.X - NotificationRingWidget.Size.X + 20f * base._scaleToUse;
		float num2 = 0f - (NotificationExtensionWidget.Size.X - (NotificationExtensionWidget.Size.X - NotificationRingWidget.Size.X)) + 35f * base._scaleToUse;
		float end = (shouldExtend ? num2 : num);
		NotificationExtensionWidget.ScaledPositionXOffset = LocalLerp(NotificationExtensionWidget.ScaledPositionXOffset, end, dt * 18f);
		float num3 = 0f;
		if (_removeInitiated)
		{
			num3 = NotificationRingWidget.Size.X;
		}
		else if (!base.IsVisible)
		{
			num3 = NotificationRingWidget.Size.X;
		}
		base.ScaledPositionXOffset = LocalLerp(base.ScaledPositionXOffset, num3, dt * 18f);
		if (_removeInitiated && MathF.Abs(base.ScaledPositionXOffset - num3) < 0.7f)
		{
			EventFired("OnRemove");
		}
	}

	private void OnRemoveClick(Widget button)
	{
		if (!IsInspectionForced)
		{
			_removeInitiated = true;
			EventFired("OnRemoveBegin");
		}
	}

	private void OnInspectionClick(Widget button)
	{
		EventFired("OnInspection");
	}

	private void RemoveButtonWidgetPropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsHovered")
		{
			_removeHoverBegan = propertyValue;
		}
	}

	private void RingWidgetOnPropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsHovered")
		{
			_ringHoverBegan = propertyValue;
		}
	}

	private void InspectionWidgetsEventFire(Widget widget, string eventName, object[] eventParameters)
	{
		if (eventName == "Click")
		{
			OnInspectionClick(widget);
		}
		else if (eventName == "AlternateClick")
		{
			OnRemoveClick(this);
		}
	}

	private void ExtensionWidgetOnPropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsHovered")
		{
			_extensionHoverBegan = propertyValue;
		}
	}

	private float LocalLerp(float start, float end, float delta)
	{
		if (MathF.Abs(start - end) > float.Epsilon)
		{
			return (end - start) * delta + start;
		}
		return end;
	}
}
