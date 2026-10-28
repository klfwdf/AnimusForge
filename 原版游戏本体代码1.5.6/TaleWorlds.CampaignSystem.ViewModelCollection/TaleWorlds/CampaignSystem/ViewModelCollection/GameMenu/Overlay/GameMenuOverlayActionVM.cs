using System;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

public class GameMenuOverlayActionVM : StringItemWithEnabledAndHintVM
{
	private bool _isHiglightEnabled;

	[DataSourceProperty]
	public bool IsHiglightEnabled
	{
		get
		{
			return _isHiglightEnabled;
		}
		set
		{
			if (value != _isHiglightEnabled)
			{
				_isHiglightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHiglightEnabled");
			}
		}
	}

	public GameMenuOverlayActionVM(Action<object> onExecute, string item, bool isEnabled, object identifier, TextObject hint = null)
		: base(onExecute, item, isEnabled, identifier, hint)
	{
	}
}
