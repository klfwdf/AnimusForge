using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

public class SettlementNameplateWidget : Widget, IComparable<SettlementNameplateWidget>
{
	public enum TutorialAnimState
	{
		Idle,
		Start,
		FirstFrame,
		Playing
	}

	private float _positionTimer;

	private bool _updatePositionNextFrame;

	private TutorialAnimState _tutorialAnimState;

	private float _lerpThreshold = 5E-05f;

	private float _lerpModifier = 10f;

	private Vector2 _cachedItemSize;

	private bool _lateUpdateActionAdded;

	private Vec2 _position;

	private bool _isVisibleOnMap;

	private bool _isTracked;

	private bool _isInsideWindow;

	private bool _isTargetedByTutorial;

	private int _relationType = -1;

	private int _wSign;

	private float _wPos;

	private float _distanceToCamera;

	private bool _isInRange;

	private bool _canParley;

	private bool _hasPort;

	private SettlementNameplateItemWidget _nameplateItem;

	private ListPanel _notificationListPanel;

	private ListPanel _eventsListPanel;

	private float _screenEdgeAlphaTarget => 1f;

	private float _normalNeutralAlphaTarget => 0.35f;

	private float _normalAllyAlphaTarget => 0.5f;

	private float _normalEnemyAlphaTarget => 0.35f;

	private float _trackedAlphaTarget => 0.8f;

	private float _trackedColorFactorTarget => 1.3f;

	private float _normalColorFactorTarget => 1f;

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
				if (_isVisibleOnMap && !value)
				{
					_positionTimer = 0f;
				}
				_isVisibleOnMap = value;
				OnPropertyChanged(value, "IsVisibleOnMap");
			}
		}
	}

	public bool IsTracked
	{
		get
		{
			return _isTracked;
		}
		set
		{
			if (_isTracked != value)
			{
				_isTracked = value;
				OnPropertyChanged(value, "IsTracked");
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
				if (value)
				{
					_tutorialAnimState = TutorialAnimState.Start;
				}
			}
		}
	}

	public bool IsInsideWindow
	{
		get
		{
			return _isInsideWindow;
		}
		set
		{
			if (_isInsideWindow != value)
			{
				_isInsideWindow = value;
				OnPropertyChanged(value, "IsInsideWindow");
			}
		}
	}

	public bool IsInRange
	{
		get
		{
			return _isInRange;
		}
		set
		{
			if (_isInRange != value)
			{
				_isInRange = value;
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

	public bool HasPort
	{
		get
		{
			return _hasPort;
		}
		set
		{
			if (value != _hasPort)
			{
				_hasPort = value;
				OnPropertyChanged(value, "HasPort");
			}
		}
	}

	public int RelationType
	{
		get
		{
			return _relationType;
		}
		set
		{
			if (_relationType != value)
			{
				_relationType = value;
				OnPropertyChanged(value, "RelationType");
				SetNameplateRelationType(value);
			}
		}
	}

	public int WSign
	{
		get
		{
			return _wSign;
		}
		set
		{
			if (_wSign != value)
			{
				_wSign = value;
				OnPropertyChanged(value, "WSign");
			}
		}
	}

	public float WPos
	{
		get
		{
			return _wPos;
		}
		set
		{
			if (_wPos != value)
			{
				_wPos = value;
				OnPropertyChanged(value, "WPos");
			}
		}
	}

	public float DistanceToCamera
	{
		get
		{
			return _distanceToCamera;
		}
		set
		{
			if (_distanceToCamera != value)
			{
				_distanceToCamera = value;
				OnPropertyChanged(value, "DistanceToCamera");
			}
		}
	}

	public SettlementNameplateItemWidget NameplateItem
	{
		get
		{
			return _nameplateItem;
		}
		set
		{
			if (_nameplateItem != value)
			{
				_nameplateItem = value;
				OnPropertyChanged(value, "NameplateItem");
			}
		}
	}

	public ListPanel NotificationListPanel
	{
		get
		{
			return _notificationListPanel;
		}
		set
		{
			if (_notificationListPanel != value)
			{
				_notificationListPanel = value;
				OnPropertyChanged(value, "NotificationListPanel");
				_notificationListPanel.ItemAddEventHandlers.Add(OnNotificationListUpdated);
				_notificationListPanel.ItemAfterRemoveEventHandlers.Add(OnNotificationListUpdated);
			}
		}
	}

	public ListPanel EventsListPanel
	{
		get
		{
			return _eventsListPanel;
		}
		set
		{
			if (value != _eventsListPanel)
			{
				_eventsListPanel = value;
				OnPropertyChanged(value, "EventsListPanel");
			}
		}
	}

	public SettlementNameplateWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		SettlementNameplateItemWidget nameplateItem = NameplateItem;
		nameplateItem?.ParallelUpdate(dt);
		if (nameplateItem != null && _cachedItemSize != nameplateItem.Size)
		{
			_cachedItemSize = nameplateItem.Size;
			ListPanel eventsListPanel = _eventsListPanel;
			ListPanel notificationListPanel = _notificationListPanel;
			if (eventsListPanel != null)
			{
				eventsListPanel.ScaledPositionXOffset = _cachedItemSize.X;
			}
			if (notificationListPanel != null)
			{
				notificationListPanel.ScaledPositionYOffset = 0f - _cachedItemSize.Y;
			}
			base.SuggestedWidth = _cachedItemSize.X * base._inverseScaleToUse;
			base.SuggestedHeight = _cachedItemSize.Y * base._inverseScaleToUse;
			base.ScaledSuggestedWidth = _cachedItemSize.X;
			base.ScaledSuggestedHeight = _cachedItemSize.Y;
		}
		base.IsEnabled = IsVisibleOnMap;
		UpdateNameplateTransparencyAndBrightness(dt);
		UpdatePosition(dt);
		UpdateTutorialState();
	}

	private void UpdatePosition(float dt)
	{
		SettlementNameplateItemWidget nameplateItem = NameplateItem;
		MapEventVisualBrushWidget mapEventVisualBrushWidget = nameplateItem?.MapEventVisualWidget;
		if (nameplateItem == null || mapEventVisualBrushWidget == null)
		{
			Debug.FailedAssert("Related widget null on UpdatePosition!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Nameplate\\SettlementNameplateWidget.cs", "UpdatePosition", 104);
			return;
		}
		bool flag = false;
		_positionTimer += dt;
		if (IsVisibleOnMap || _positionTimer < 2f)
		{
			float num = Position.X - base.Size.X / 2f - base.ScaledMarginLeft;
			float num2 = Position.X + base.Size.X / 2f + base.ScaledMarginRight;
			float num3 = Position.Y - base.Size.Y - base.ScaledMarginTop;
			float num4 = Position.Y + base.ScaledMarginBottom;
			bool flag2 = WSign > 0 && num > 0f && num2 < base.Context.EventManager.PageSize.X && num3 > 0f && num4 < base.Context.EventManager.PageSize.Y;
			if (IsTracked && !flag2)
			{
				Vec2 vec = new Vec2(num, num3);
				Vector2 vector = base.Context.EventManager.PageSize - base.Size;
				vector.X -= base.ScaledMarginLeft + base.ScaledMarginRight;
				vector.Y -= base.ScaledMarginTop + base.ScaledMarginBottom;
				Vec2 vec2 = vector / 2f;
				vec -= vec2;
				if (WSign < 0)
				{
					vec *= -1f;
				}
				float radian = Mathf.Atan2(vec.y, vec.x) - System.MathF.PI / 2f;
				float num5 = Mathf.Cos(radian);
				float num6 = Mathf.Sin(radian);
				float num7 = num5 / num6;
				Vec2 vec3 = vec2 * 1f;
				vec = ((num5 > 0f) ? new Vec2((0f - vec3.y) / num7, vec2.y) : new Vec2(vec3.y / num7, 0f - vec2.y));
				if (vec.x > vec3.x)
				{
					vec = new Vec2(vec3.x, (0f - vec3.x) * num7);
				}
				else if (vec.x < 0f - vec3.x)
				{
					vec = new Vec2(0f - vec3.x, vec3.x * num7);
				}
				vec += vec2;
				base.ScaledPositionXOffset = Mathf.Clamp(vec.x, 0f, vector.X);
				base.ScaledPositionYOffset = Mathf.Clamp(vec.y, 0f, vector.Y);
			}
			else
			{
				base.ScaledPositionXOffset = num;
				base.ScaledPositionYOffset = num3;
			}
			flag = base.ScaledPositionYOffset - mapEventVisualBrushWidget.Size.Y < 0f;
		}
		if (flag)
		{
			mapEventVisualBrushWidget.VerticalAlignment = VerticalAlignment.Bottom;
			mapEventVisualBrushWidget.ScaledPositionYOffset = mapEventVisualBrushWidget.Size.Y;
		}
		else
		{
			mapEventVisualBrushWidget.VerticalAlignment = VerticalAlignment.Top;
			mapEventVisualBrushWidget.ScaledPositionYOffset = 0f - mapEventVisualBrushWidget.Size.Y;
		}
	}

	private void OnNotificationListUpdated(Widget widget)
	{
		_updatePositionNextFrame = true;
		AddLateUpdateAction();
	}

	private void OnNotificationListUpdated(Widget parentWidget, Widget addedWidget)
	{
		_updatePositionNextFrame = true;
		AddLateUpdateAction();
	}

	private void AddLateUpdateAction()
	{
		if (!_lateUpdateActionAdded)
		{
			base.EventManager.AddLateUpdateAction(this, CustomLateUpdate, 1);
			_lateUpdateActionAdded = true;
		}
	}

	private void CustomLateUpdate(float dt)
	{
		if (_updatePositionNextFrame)
		{
			UpdatePosition(dt);
			_updatePositionNextFrame = false;
		}
		_lateUpdateActionAdded = false;
	}

	private void UpdateTutorialState()
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

	private void SetNameplateRelationType(int type)
	{
		if (NameplateItem != null)
		{
			switch (type)
			{
			case 0:
				NameplateItem.Color = Color.Black;
				break;
			case 1:
				NameplateItem.Color = Color.ConvertStringToColor("#245E05FF");
				break;
			case 2:
				NameplateItem.Color = Color.ConvertStringToColor("#870707FF");
				break;
			case 3:
				NameplateItem.Color = Color.ConvertStringToColor("#2986CCFF");
				break;
			}
		}
	}

	private void UpdateNameplateTransparencyAndBrightness(float dt)
	{
		SettlementNameplateItemWidget nameplateItem = NameplateItem;
		TextWidget textWidget = nameplateItem?.SettlementNameTextWidget;
		MaskedTextureWidget maskedTextureWidget = nameplateItem?.SettlementBannerWidget;
		GridWidget gridWidget = nameplateItem?.SettlementPartiesGridWidget;
		Widget widget = nameplateItem?.InspectedIconWidget;
		Widget widget2 = nameplateItem?.PortIconWidget;
		Widget widget3 = nameplateItem?.ParleyIconWidget;
		ListPanel eventsListPanel = _eventsListPanel;
		if (nameplateItem == null || textWidget == null || maskedTextureWidget == null || gridWidget == null || widget == null || widget2 == null || widget3 == null || eventsListPanel == null)
		{
			Debug.FailedAssert("Related widget null on UpdateNameplateTransparencyAndBrightness!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Nameplate\\SettlementNameplateWidget.cs", "UpdateNameplateTransparencyAndBrightness", 302);
			return;
		}
		widget2.IsVisible = HasPort;
		float amount = dt * _lerpModifier;
		if (IsVisibleOnMap)
		{
			base.IsVisible = true;
			float valueTo = DetermineTargetAlphaValue();
			float valueTo2 = DetermineTargetColorFactor();
			float alphaFactor = TaleWorlds.Library.MathF.Lerp(nameplateItem.AlphaFactor, valueTo, amount);
			float colorFactor = TaleWorlds.Library.MathF.Lerp(nameplateItem.ColorFactor, valueTo2, amount);
			float num = TaleWorlds.Library.MathF.Lerp(textWidget.ReadOnlyBrush.GlobalAlphaFactor, 1f, amount);
			nameplateItem.AlphaFactor = alphaFactor;
			nameplateItem.ColorFactor = colorFactor;
			textWidget.Brush.GlobalAlphaFactor = num;
			maskedTextureWidget.Brush.GlobalAlphaFactor = num;
			gridWidget.SetGlobalAlphaRecursively(num);
			widget3.AlphaFactor = TaleWorlds.Library.MathF.Lerp(widget3.AlphaFactor, CanParley ? 1 : 0, amount);
			eventsListPanel.SetGlobalAlphaRecursively(num);
		}
		else if (nameplateItem.AlphaFactor > _lerpThreshold)
		{
			float num2 = (nameplateItem.AlphaFactor = TaleWorlds.Library.MathF.Lerp(nameplateItem.AlphaFactor, 0f, amount));
			textWidget.Brush.GlobalAlphaFactor = num2;
			maskedTextureWidget.Brush.GlobalAlphaFactor = num2;
			gridWidget.SetGlobalAlphaRecursively(num2);
			widget3.AlphaFactor = num2;
			eventsListPanel.SetGlobalAlphaRecursively(num2);
		}
		else
		{
			base.IsVisible = false;
		}
		if (IsInRange && IsVisibleOnMap)
		{
			if (Math.Abs(widget.AlphaFactor - 1f) > _lerpThreshold)
			{
				widget.AlphaFactor = TaleWorlds.Library.MathF.Lerp(widget.AlphaFactor, 1f, amount);
			}
		}
		else if (nameplateItem.AlphaFactor - 0f > _lerpThreshold)
		{
			widget.AlphaFactor = TaleWorlds.Library.MathF.Lerp(widget.AlphaFactor, 0f, amount);
		}
	}

	private float DetermineTargetAlphaValue()
	{
		if (IsInsideWindow)
		{
			if (IsTracked)
			{
				return _trackedAlphaTarget;
			}
			if (RelationType == 0)
			{
				return _normalNeutralAlphaTarget;
			}
			if (RelationType == 1)
			{
				return _normalAllyAlphaTarget;
			}
			return _normalEnemyAlphaTarget;
		}
		if (IsTracked)
		{
			return _screenEdgeAlphaTarget;
		}
		return 0f;
	}

	private float DetermineTargetColorFactor()
	{
		if (IsTracked)
		{
			return _trackedColorFactorTarget;
		}
		return _normalColorFactorTarget;
	}

	public int CompareTo(SettlementNameplateWidget other)
	{
		return other.DistanceToCamera.CompareTo(DistanceToCamera);
	}
}
