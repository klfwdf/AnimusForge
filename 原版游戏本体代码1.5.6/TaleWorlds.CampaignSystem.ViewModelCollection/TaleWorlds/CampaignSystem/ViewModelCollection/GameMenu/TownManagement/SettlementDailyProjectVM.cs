using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class SettlementDailyProjectVM : SettlementProjectVM
{
	private bool _isDefault;

	private string _defaultText;

	[DataSourceProperty]
	public bool IsDefault
	{
		get
		{
			return _isDefault;
		}
		set
		{
			if (value != _isDefault)
			{
				_isDefault = value;
				OnPropertyChangedWithValue(value, "IsDefault");
			}
		}
	}

	[DataSourceProperty]
	public string DefaultText
	{
		get
		{
			return _defaultText;
		}
		set
		{
			if (value != _defaultText)
			{
				_defaultText = value;
				OnPropertyChangedWithValue(value, "DefaultText");
			}
		}
	}

	public SettlementDailyProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building, Settlement settlement)
		: base(onSelection, onSetAsCurrent, onResetCurrent, building, settlement)
	{
		base.IsDaily = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		DefaultText = GameTexts.FindText("str_default").ToString();
	}

	public override void RefreshProductionText()
	{
		base.RefreshProductionText();
		base.ProductionText = new TextObject("{=bd7oAQq6}Daily").ToString();
	}

	public override void ExecuteAddRemoveToQueue()
	{
	}

	public override void ExecuteSetAsActiveDevelopment()
	{
		_onSelection(this, arg2: false);
	}

	public override void ExecuteSetAsCurrent()
	{
		_onSetAsCurrent?.Invoke(this);
	}

	public override void ExecuteResetCurrent()
	{
		_onResetCurrent?.Invoke();
	}

	public override void ExecuteToggleSelected()
	{
	}
}
