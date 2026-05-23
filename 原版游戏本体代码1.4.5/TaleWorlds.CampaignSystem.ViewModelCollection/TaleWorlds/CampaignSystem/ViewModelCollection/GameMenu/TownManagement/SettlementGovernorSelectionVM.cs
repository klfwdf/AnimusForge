using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class SettlementGovernorSelectionVM : ViewModel
{
	private readonly Settlement _settlement;

	private readonly Action<Hero> _onDone;

	private MBBindingList<SettlementGovernorSelectionItemVM> _availableGovernors;

	private int _currentGovernorIndex;

	[DataSourceProperty]
	public MBBindingList<SettlementGovernorSelectionItemVM> AvailableGovernors
	{
		get
		{
			return _availableGovernors;
		}
		set
		{
			if (value != _availableGovernors)
			{
				_availableGovernors = value;
				OnPropertyChangedWithValue(value, "AvailableGovernors");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentGovernorIndex
	{
		get
		{
			return _currentGovernorIndex;
		}
		set
		{
			if (value != _currentGovernorIndex)
			{
				_currentGovernorIndex = value;
				OnPropertyChangedWithValue(value, "CurrentGovernorIndex");
			}
		}
	}

	public SettlementGovernorSelectionVM(Settlement settlement, Action<Hero> onDone)
	{
		_settlement = settlement;
		_onDone = onDone;
		AvailableGovernors = new MBBindingList<SettlementGovernorSelectionItemVM>
		{
			new SettlementGovernorSelectionItemVM(null, OnSelection)
		};
		if (settlement?.OwnerClan == null)
		{
			return;
		}
		foreach (Hero hero in settlement.OwnerClan.Heroes)
		{
			if (Campaign.Current.Models.ClanPoliticsModel.CanHeroBeGovernor(hero) && !AvailableGovernors.Any((SettlementGovernorSelectionItemVM G) => G.Governor == hero) && (hero.GovernorOf == _settlement.Town || hero.GovernorOf == null))
			{
				AvailableGovernors.Add(new SettlementGovernorSelectionItemVM(hero, OnSelection));
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AvailableGovernors.ApplyActionOnAllItems(delegate(SettlementGovernorSelectionItemVM x)
		{
			x.RefreshValues();
		});
	}

	private void OnSelection(SettlementGovernorSelectionItemVM item)
	{
		_onDone?.Invoke(item.Governor);
	}
}
