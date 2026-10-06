using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.Core;

namespace SandBox.View.Map;

public class MapViewsContainer
{
	public readonly ObservableCollection<MapView> MapViews;

	private List<MapView> _mapViewsCopyCache;

	private bool _isViewListDirty;

	public MapViewsContainer()
	{
		MapViews = new ObservableCollection<MapView>();
		_mapViewsCopyCache = MapViews.ToList();
	}

	public void Add(MapView mapView)
	{
		MapViews.Add(mapView);
		_isViewListDirty = true;
	}

	public void Remove(MapView mapView)
	{
		MapViews.Remove(mapView);
		_isViewListDirty = true;
	}

	public bool Contains(MapView mapView)
	{
		return MapViews.Contains(mapView);
	}

	public void Foreach(Action<MapView> action)
	{
		foreach (MapView item in GetMapViewsCopy())
		{
			if (!item.IsFinalized)
			{
				action(item);
			}
		}
	}

	public void ForeachReverse(Action<MapView> action)
	{
		List<MapView> mapViewsCopy = GetMapViewsCopy();
		for (int num = mapViewsCopy.Count - 1; num >= 0; num--)
		{
			if (!mapViewsCopy[num].IsFinalized)
			{
				action(mapViewsCopy[num]);
			}
		}
	}

	public MapView ReturnFirstElementWithCondition(Func<MapView, bool> condition)
	{
		foreach (MapView item in GetMapViewsCopy())
		{
			if (!item.IsFinalized && condition(item))
			{
				return item;
			}
		}
		return null;
	}

	public T GetMapViewWithType<T>() where T : MapView
	{
		foreach (MapView item in GetMapViewsCopy())
		{
			if (!item.IsFinalized && item is T)
			{
				return item as T;
			}
		}
		return null;
	}

	public TutorialContexts GetContextToChangeTo()
	{
		foreach (MapView item in GetMapViewsCopy())
		{
			if (!item.IsFinalized)
			{
				TutorialContexts tutorialContext = item.GetTutorialContext();
				if (tutorialContext != TutorialContexts.MapWindow)
				{
					return tutorialContext;
				}
			}
		}
		return TutorialContexts.MapWindow;
	}

	public bool IsThereAnyViewIsEscaped()
	{
		return ReturnFirstElementWithCondition((MapView view) => view.IsEscaped()) != null;
	}

	public bool IsOpeningEscapeMenuOnFocusChangeAllowedForAll()
	{
		bool flag = true;
		foreach (MapView item in GetMapViewsCopy())
		{
			if (!item.IsFinalized)
			{
				flag &= item.IsOpeningEscapeMenuOnFocusChangeAllowed();
			}
		}
		return flag;
	}

	private List<MapView> GetMapViewsCopy()
	{
		if (_isViewListDirty)
		{
			_mapViewsCopyCache = MapViews.ToList();
			_isViewListDirty = false;
		}
		return _mapViewsCopyCache;
	}
}
