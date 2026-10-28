using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;

public class PerkSelectionVM : ViewModel
{
	private readonly HeroDeveloper _developer;

	private readonly List<PerkObject> _selectedPerks;

	private readonly Action<SkillObject> _refreshPerksOf;

	private readonly Action _onPerkSelection;

	private PerkVM _currentInitialPerk;

	private bool _isActive;

	private MBBindingList<PerkSelectionItemVM> _availablePerks;

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
				Game.Current.EventManager.TriggerEvent(new PerkSelectionToggleEvent(IsActive));
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<PerkSelectionItemVM> AvailablePerks
	{
		get
		{
			return _availablePerks;
		}
		set
		{
			if (value != _availablePerks)
			{
				_availablePerks = value;
				OnPropertyChangedWithValue(value, "AvailablePerks");
			}
		}
	}

	public PerkSelectionVM(HeroDeveloper developer, Action<SkillObject> refreshPerksOf, Action onPerkSelection)
	{
		_developer = developer;
		_refreshPerksOf = refreshPerksOf;
		_onPerkSelection = onPerkSelection;
		_selectedPerks = new List<PerkObject>();
		AvailablePerks = new MBBindingList<PerkSelectionItemVM>();
		IsActive = false;
	}

	public void SetCurrentSelectionPerk(PerkVM perk)
	{
		if (AvailablePerks.Count > 0 || IsActive)
		{
			ExecuteDeactivate();
		}
		AvailablePerks.Clear();
		_currentInitialPerk = perk;
		AvailablePerks.Add(new PerkSelectionItemVM(perk.Perk, OnSelectPerk));
		if (perk.AlternativeType == 2)
		{
			AvailablePerks.Insert(0, new PerkSelectionItemVM(perk.Perk.AlternativePerk, OnSelectPerk));
		}
		else if (perk.AlternativeType == 1)
		{
			AvailablePerks.Add(new PerkSelectionItemVM(perk.Perk.AlternativePerk, OnSelectPerk));
		}
		IsActive = true;
		OnSelectPerk(perk);
	}

	private void OnSelectPerk(PerkVM selectedPerk)
	{
		_selectedPerks.Add(selectedPerk.Perk);
		_refreshPerksOf(selectedPerk.Perk.Skill);
		IsActive = false;
		Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(selectedPerk.Perk));
		_onPerkSelection?.Invoke();
	}

	private void OnSelectPerk(PerkSelectionItemVM selectedPerk)
	{
		_selectedPerks.Add(selectedPerk.Perk);
		_refreshPerksOf(selectedPerk.Perk.Skill);
		IsActive = false;
		Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(selectedPerk.Perk));
		_onPerkSelection?.Invoke();
	}

	public void ResetSelectedPerks()
	{
		foreach (PerkObject selectedPerk in _selectedPerks)
		{
			_refreshPerksOf(selectedPerk.Skill);
		}
		_selectedPerks.Clear();
	}

	public void ApplySelectedPerks()
	{
		foreach (PerkObject item in _selectedPerks.ToList())
		{
			_developer.AddPerk(item);
			_selectedPerks.Remove(item);
		}
	}

	public bool IsPerkSelected(PerkObject perk)
	{
		return _selectedPerks.Contains(perk);
	}

	public bool IsAnyPerkSelected()
	{
		return _selectedPerks.Count > 0;
	}

	public void ExecuteDeactivate()
	{
		IsActive = false;
		_refreshPerksOf(_currentInitialPerk.Perk.Skill);
	}
}
