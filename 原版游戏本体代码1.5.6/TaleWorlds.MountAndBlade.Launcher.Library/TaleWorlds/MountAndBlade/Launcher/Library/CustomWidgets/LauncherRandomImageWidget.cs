using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;

public class LauncherRandomImageWidget : Widget
{
	private readonly Random _random;

	private List<int> _imageIndices;

	private int _currentIndex;

	private int _imageCount;

	private bool _changeTrigger;

	[DataSourceProperty]
	public int ImageCount
	{
		get
		{
			return _imageCount;
		}
		set
		{
			if (value != _imageCount)
			{
				_imageCount = value;
				OnPropertyChanged(value, "ImageCount");
			}
		}
	}

	[DataSourceProperty]
	public bool ChangeTrigger
	{
		get
		{
			return _changeTrigger;
		}
		set
		{
			if (value != _changeTrigger)
			{
				_changeTrigger = value;
				OnPropertyChanged(value, "ChangeTrigger");
				TriggerChanged();
			}
		}
	}

	public LauncherRandomImageWidget(UIContext context)
		: base(context)
	{
		_random = new Random();
	}

	private void ShuffleList<T>(List<T> list)
	{
		for (int i = 0; i < list.Count; i++)
		{
			T value = list[i];
			int index = _random.Next(i, list.Count);
			list[i] = list[index];
			list[index] = value;
		}
	}

	private void CreateIndicesList()
	{
		_imageIndices = new List<int>();
		for (int i = 0; i < ImageCount; i++)
		{
			_imageIndices.Add(i);
		}
		ShuffleList(_imageIndices);
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		CreateIndicesList();
		int num = _imageIndices[_currentIndex];
		base.Sprite = base.Context.SpriteData.GetSprite("ConceptArts\\ConceptArt_" + num);
	}

	private void TriggerChanged()
	{
		_currentIndex = (_currentIndex + 1) % _imageIndices.Count;
		int num = _imageIndices[_currentIndex];
		base.Sprite = base.Context.SpriteData.GetSprite("ConceptArts\\ConceptArt_" + num);
	}
}
