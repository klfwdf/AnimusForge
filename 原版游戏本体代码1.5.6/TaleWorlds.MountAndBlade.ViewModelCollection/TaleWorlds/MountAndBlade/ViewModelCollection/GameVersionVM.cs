using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class GameVersionVM : ViewModel
{
	private readonly Func<List<string>> _getVersionTexts;

	private MBBindingList<BindingListStringItem> _gameVersionTexts;

	[DataSourceProperty]
	public MBBindingList<BindingListStringItem> GameVersionTexts
	{
		get
		{
			return _gameVersionTexts;
		}
		set
		{
			if (value != _gameVersionTexts)
			{
				_gameVersionTexts = value;
				OnPropertyChangedWithValue(value, "GameVersionTexts");
			}
		}
	}

	public GameVersionVM(Func<List<string>> getVersionTexts)
	{
		GameVersionTexts = new MBBindingList<BindingListStringItem>();
		_getVersionTexts = getVersionTexts;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		GameVersionTexts.Clear();
		foreach (string item in _getVersionTexts?.Invoke())
		{
			GameVersionTexts.Add(new BindingListStringItem(item));
		}
	}
}
