using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

public class NameMarkerScreenWidget : Widget
{
	private const float MinDistanceToFocusSquared = 3600f;

	private List<NameMarkerListPanel> _markers;

	private NameMarkerListPanel _lastFocusedWidget;

	private bool _isMarkersEnabled;

	private float _targetAlphaValue;

	private Widget _markersContainer;

	public bool IsMarkersEnabled
	{
		get
		{
			return _isMarkersEnabled;
		}
		set
		{
			if (_isMarkersEnabled != value)
			{
				_isMarkersEnabled = value;
				OnPropertyChanged(value, "IsMarkersEnabled");
			}
		}
	}

	public float TargetAlphaValue
	{
		get
		{
			return _targetAlphaValue;
		}
		set
		{
			if (_targetAlphaValue != value)
			{
				_targetAlphaValue = value;
				OnPropertyChanged(value, "TargetAlphaValue");
			}
		}
	}

	[Editor(false)]
	public Widget MarkersContainer
	{
		get
		{
			return _markersContainer;
		}
		set
		{
			if (value != _markersContainer)
			{
				if (_markersContainer != null)
				{
					_markersContainer.EventFire += OnMarkersChanged;
				}
				_markersContainer = value;
				if (_markersContainer != null)
				{
					_markersContainer.EventFire += OnMarkersChanged;
				}
				OnPropertyChanged(value, "MarkersContainer");
			}
		}
	}

	public NameMarkerScreenWidget(UIContext context)
		: base(context)
	{
		_markers = new List<NameMarkerListPanel>();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		float end = (IsMarkersEnabled ? TargetAlphaValue : 0f);
		float amount = MathF.Clamp(dt * 10f, 0f, 1f);
		base.AlphaFactor = Mathf.Lerp(base.AlphaFactor, end, amount);
		bool flag = _markers.Count > 0;
		for (int i = 0; i < _markers.Count; i++)
		{
			_markers[i].Update(dt);
			flag &= _markers[i].TypeVisualWidget.AlphaFactor > 0f;
		}
		if (!flag)
		{
			return;
		}
		_markers.Sort((NameMarkerListPanel m1, NameMarkerListPanel m2) => m1.Rect.Left.CompareTo(m2.Rect.Left));
		for (int num = 0; num < _markers.Count; num++)
		{
			for (int num2 = num + 1; num2 < _markers.Count && !(_markers[num2].Rect.Left - _markers[num].Rect.Left > _markers[num].Rect.Width); num2++)
			{
				if (_markers[num].Rect.IsOverlapping(_markers[num2].Rect))
				{
					_markers[num2].ScaledPositionXOffset += _markers[num].Rect.Right - _markers[num2].Rect.Left;
					_markers[num2].UpdateRectangle();
				}
			}
		}
		NameMarkerListPanel nameMarkerListPanel = null;
		float num3 = 3600f;
		for (int num4 = 0; num4 < _markers.Count; num4++)
		{
			if (_markers[num4].IsInScreenBoundaries)
			{
				NameMarkerListPanel nameMarkerListPanel2 = _markers[num4];
				float num5 = base.EventManager.PageSize.X / 2f;
				float num6 = base.EventManager.PageSize.Y / 2f;
				float num7 = Mathf.Abs(num5 - nameMarkerListPanel2.Rect.CenterX);
				float num8 = Mathf.Abs(num6 - nameMarkerListPanel2.Rect.CenterY);
				float num9 = num7 * num7 + num8 * num8;
				if (num9 < num3)
				{
					num3 = num9;
					nameMarkerListPanel = nameMarkerListPanel2;
				}
			}
		}
		if (nameMarkerListPanel != _lastFocusedWidget)
		{
			if (_lastFocusedWidget != null)
			{
				_lastFocusedWidget.IsFocused = false;
			}
			_lastFocusedWidget = nameMarkerListPanel;
			if (_lastFocusedWidget != null)
			{
				_lastFocusedWidget.IsFocused = true;
			}
		}
	}

	private void OnMarkersChanged(Widget widget, string eventName, object[] args)
	{
		if (args.Length == 1 && args[0] is NameMarkerListPanel item)
		{
			if (eventName == "ItemAdd")
			{
				_markers.Add(item);
			}
			else if (eventName == "ItemRemove")
			{
				_markers.Remove(item);
			}
		}
	}
}
