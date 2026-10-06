using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class TownManagementReserveControlVM : ViewModel
{
	private readonly Action _onReserveUpdated;

	private readonly Settlement _settlement;

	private const int MaxOneTimeAmount = 10000;

	private bool _isEnabled;

	private string _reserveText;

	private int _currentReserveAmount;

	private int _currentGivenAmount;

	private int _maxReserveAmount;

	private string _reserveBonusText;

	private string _currentReserveText;

	private HintViewModel _addGoldToReserveHint;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentReserveAmount
	{
		get
		{
			return _currentReserveAmount;
		}
		set
		{
			if (value != _currentReserveAmount)
			{
				_currentReserveAmount = value;
				OnPropertyChangedWithValue(value, "CurrentReserveAmount");
				CurrentReserveText = (CurrentGivenAmount + value).ToString();
			}
		}
	}

	[DataSourceProperty]
	public int CurrentGivenAmount
	{
		get
		{
			return _currentGivenAmount;
		}
		set
		{
			if (value != _currentGivenAmount)
			{
				_currentGivenAmount = value;
				OnPropertyChangedWithValue(value, "CurrentGivenAmount");
			}
		}
	}

	[DataSourceProperty]
	public int MaxReserveAmount
	{
		get
		{
			return _maxReserveAmount;
		}
		set
		{
			if (value != _maxReserveAmount)
			{
				_maxReserveAmount = value;
				OnPropertyChangedWithValue(value, "MaxReserveAmount");
			}
		}
	}

	[DataSourceProperty]
	public string ReserveBonusText
	{
		get
		{
			return _reserveBonusText;
		}
		set
		{
			if (value != _reserveBonusText)
			{
				_reserveBonusText = value;
				OnPropertyChangedWithValue(value, "ReserveBonusText");
			}
		}
	}

	[DataSourceProperty]
	public string ReserveText
	{
		get
		{
			return _reserveText;
		}
		set
		{
			if (value != _reserveText)
			{
				_reserveText = value;
				OnPropertyChangedWithValue(value, "ReserveText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentReserveText
	{
		get
		{
			return _currentReserveText;
		}
		set
		{
			if (value != _currentReserveText)
			{
				_currentReserveText = value;
				OnPropertyChangedWithValue(value, "CurrentReserveText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AddGoldToReserveHint
	{
		get
		{
			return _addGoldToReserveHint;
		}
		set
		{
			if (value != _addGoldToReserveHint)
			{
				_addGoldToReserveHint = value;
				OnPropertyChangedWithValue(value, "AddGoldToReserveHint");
			}
		}
	}

	public TownManagementReserveControlVM(Settlement settlement, Action onReserveUpdated)
	{
		_settlement = settlement;
		_onReserveUpdated = onReserveUpdated;
		if (settlement?.Town != null)
		{
			CurrentReserveAmount = Settlement.CurrentSettlement.Town.BoostBuildingProcess;
			CurrentGivenAmount = 0;
			MaxReserveAmount = TaleWorlds.Library.MathF.Min(Hero.MainHero.Gold, 10000);
			AddGoldToReserveHint = new HintViewModel(new TextObject("{=TeZ3QzN6}Add gold to the reserve"));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_settlement?.Town != null)
		{
			ReserveText = new TextObject("{=2ckyCKR7}Reserve").ToString();
			GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			UpdateReserveText();
		}
	}

	private void UpdateReserveText()
	{
		TextObject textObject = GameTexts.FindText("str_town_management_reserve_explanation");
		textObject.SetTextVariable("BOOST", Campaign.Current.Models.BuildingConstructionModel.GetBoostAmount(_settlement.Town));
		textObject.SetTextVariable("COST", Campaign.Current.Models.BuildingConstructionModel.GetBoostCost(_settlement.Town));
		ReserveBonusText = textObject.ToString();
	}

	public void ExecuteConfirm()
	{
		IsEnabled = false;
		BuildingHelper.BoostBuildingProcessWithGold(CurrentReserveAmount + CurrentGivenAmount, Settlement.CurrentSettlement.Town);
		CurrentGivenAmount = 0;
		GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		UpdateReserveText();
		MaxReserveAmount = TaleWorlds.Library.MathF.Min(Hero.MainHero.Gold, 10000);
		CurrentReserveAmount = Settlement.CurrentSettlement.Town.BoostBuildingProcess;
		_onReserveUpdated?.Invoke();
	}

	public void ExecuteCancel()
	{
		IsEnabled = false;
	}
}
