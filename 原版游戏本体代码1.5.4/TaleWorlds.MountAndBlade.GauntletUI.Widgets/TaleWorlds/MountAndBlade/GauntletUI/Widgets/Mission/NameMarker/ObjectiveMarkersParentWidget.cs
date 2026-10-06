using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

public class ObjectiveMarkersParentWidget : Widget
{
	private class MarkerPositionComparer : IComparer<ObjectiveMarkerWidget>
	{
		public int Compare(ObjectiveMarkerWidget x, ObjectiveMarkerWidget y)
		{
			return x.Position.x.CompareTo(y.Position.x);
		}
	}

	private class MarkerRenderOrderComparer : IComparer<Widget>
	{
		public int Compare(Widget x, Widget y)
		{
			ObjectiveMarkerWidget objectiveMarkerWidget = x as ObjectiveMarkerWidget;
			ObjectiveMarkerWidget objectiveMarkerWidget2 = y as ObjectiveMarkerWidget;
			int num = (objectiveMarkerWidget == null).CompareTo(objectiveMarkerWidget2 == null);
			if (num != 0)
			{
				return num;
			}
			int num2 = objectiveMarkerWidget.IsMainCombinationMarker.CompareTo(objectiveMarkerWidget2.IsMainCombinationMarker);
			if (num2 != 0)
			{
				return num2;
			}
			int num3 = objectiveMarkerWidget.CombinedSiblingsCount.CompareTo(objectiveMarkerWidget2.CombinedSiblingsCount);
			if (num3 != 0)
			{
				return num3;
			}
			return objectiveMarkerWidget.Position.x.CompareTo(objectiveMarkerWidget2.Position.x);
		}
	}

	private List<ObjectiveMarkerWidget> _markers;

	private ObjectiveMarkerWidget _lastFocusedWidget;

	private bool _isMarkersEnabled;

	private float _targetAlphaValue;

	private float _maxDistanceToCombineMarkers;

	private Widget _markersContainer;

	public float MinDistanceToFocus { get; set; }

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

	public float MaxDistanceToCombineMarkers
	{
		get
		{
			return _maxDistanceToCombineMarkers;
		}
		set
		{
			if (_maxDistanceToCombineMarkers != value)
			{
				_maxDistanceToCombineMarkers = value;
				OnPropertyChanged(value, "MaxDistanceToCombineMarkers");
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

	public ObjectiveMarkersParentWidget(UIContext context)
		: base(context)
	{
		_markers = new List<ObjectiveMarkerWidget>();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		float end = (IsMarkersEnabled ? TargetAlphaValue : 0f);
		float amount = MathF.Clamp(dt * 10f, 0f, 1f);
		base.AlphaFactor = Mathf.Lerp(base.AlphaFactor, end, amount);
		base.Children.Sort(new MarkerRenderOrderComparer());
		_markers.Sort(new MarkerPositionComparer());
		UpdateMarkerCombinations();
		UpdateMarkerFocus();
		for (int i = 0; i < _markers.Count; i++)
		{
			_markers[i].Update(dt);
		}
	}

	private void UpdateMarkerCombinations()
	{
		for (int i = 0; i < _markers.Count; i++)
		{
			_markers[i].CombinedSiblingsCount = 0;
			_markers[i].IsMainCombinationMarker = false;
		}
		for (int j = 0; j < _markers.Count; j++)
		{
			ObjectiveMarkerWidget objectiveMarkerWidget = _markers[j];
			if (!objectiveMarkerWidget.IsCombinedWithOtherMarkers && objectiveMarkerWidget.IsMarkerActive && objectiveMarkerWidget.IsMarkerEnabled)
			{
				Vec2 averageScreenPosition;
				List<ObjectiveMarkerWidget> list = FindClosestMarkersToCombine(objectiveMarkerWidget, out averageScreenPosition);
				objectiveMarkerWidget.CombinedSiblingsCount = list.Count;
				objectiveMarkerWidget.IsMainCombinationMarker = list.Count > 0;
				if (objectiveMarkerWidget.IsCombinedWithOtherMarkers)
				{
					objectiveMarkerWidget.CombinedAveragePosition = averageScreenPosition;
				}
				for (int k = 0; k < list.Count; k++)
				{
					list[k].CombinedSiblingsCount = list.Count;
					list[k].CombinedAveragePosition = averageScreenPosition;
					list[k].IsMainCombinationMarker = false;
				}
			}
		}
	}

	private void UpdateMarkerFocus()
	{
		float distanceSquared;
		ObjectiveMarkerWidget objectiveMarkerWidget = FindClosestMarkerToFocus(base.EventManager.PageSize * 0.5f, out distanceSquared);
		if (objectiveMarkerWidget != _lastFocusedWidget)
		{
			if (_lastFocusedWidget != null)
			{
				_lastFocusedWidget.IsFocused = false;
			}
			_lastFocusedWidget = objectiveMarkerWidget;
			if (_lastFocusedWidget != null)
			{
				_lastFocusedWidget.IsFocused = true;
			}
		}
	}

	private ObjectiveMarkerWidget FindClosestMarkerToFocus(Vec2 screenPosition, out float distanceSquared)
	{
		ObjectiveMarkerWidget result = null;
		distanceSquared = MinDistanceToFocus * MinDistanceToFocus;
		for (int i = 0; i < _markers.Count; i++)
		{
			ObjectiveMarkerWidget objectiveMarkerWidget = _markers[i];
			if (objectiveMarkerWidget.IsInScreenBoundaries)
			{
				float num = objectiveMarkerWidget.Position.DistanceSquared(screenPosition);
				if (num < distanceSquared)
				{
					distanceSquared = num;
					result = objectiveMarkerWidget;
				}
			}
		}
		return result;
	}

	private List<ObjectiveMarkerWidget> FindClosestMarkersToCombine(ObjectiveMarkerWidget marker, out Vec2 averageScreenPosition)
	{
		List<ObjectiveMarkerWidget> list = new List<ObjectiveMarkerWidget>();
		averageScreenPosition = marker.Position;
		for (int i = 0; i < _markers.Count; i++)
		{
			if (_markers[i] != marker && _markers[i].IsInScreenBoundaries && !_markers[i].IsCombinedWithOtherMarkers && _markers[i].IsMarkerActive && _markers[i].IsMarkerEnabled && marker.Position.Distance(_markers[i].Position) < MaxDistanceToCombineMarkers)
			{
				averageScreenPosition += _markers[i].Position;
				list.Add(_markers[i]);
			}
		}
		averageScreenPosition /= (float)(list.Count + 1);
		return list;
	}

	private void OnMarkersChanged(Widget widget, string eventName, object[] args)
	{
		if (args.Length == 1 && args[0] is ObjectiveMarkerWidget item)
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
