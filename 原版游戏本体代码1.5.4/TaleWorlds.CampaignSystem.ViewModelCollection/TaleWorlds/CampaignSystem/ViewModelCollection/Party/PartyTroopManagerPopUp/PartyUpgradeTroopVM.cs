using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;

public class PartyUpgradeTroopVM : PartyTroopManagerVM
{
	private int _disabledTroopsStartIndex = -1;

	private string _upgradeCostText;

	private string _upgradesAndRequirementsText;

	[DataSourceProperty]
	public string UpgradeCostText
	{
		get
		{
			return _upgradeCostText;
		}
		set
		{
			if (value != _upgradeCostText)
			{
				_upgradeCostText = value;
				OnPropertyChangedWithValue(value, "UpgradeCostText");
			}
		}
	}

	[DataSourceProperty]
	public string UpgradesAndRequirementsText
	{
		get
		{
			return _upgradesAndRequirementsText;
		}
		set
		{
			if (value != _upgradesAndRequirementsText)
			{
				_upgradesAndRequirementsText = value;
				OnPropertyChangedWithValue(value, "UpgradesAndRequirementsText");
			}
		}
	}

	public PartyUpgradeTroopVM(PartyVM partyVM)
		: base(partyVM)
	{
		RefreshValues();
		base.IsUpgradePopUp = true;
		_openButtonEnabledHint = new TextObject("{=hRSezxnT}Some of your troops are ready to upgrade.");
		_openButtonNoTroopsHint = new TextObject("{=fpE7BQ7f}You don't have any upgradable troops.");
		_openButtonIrrelevantScreenHint = new TextObject("{=mdvnjI72}Troops are not upgradable in this screen.");
		_openButtonUpgradesDisabledHint = new TextObject("{=R4rTlKMU}Troop upgrades are currently disabled.");
		base.UsedHorsesHint = new BasicTooltipViewModel(() => GetUsedHorsesTooltip());
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.TitleText = new TextObject("{=IgoxNz2H}Upgrade Troops").ToString();
		UpgradeCostText = new TextObject("{=SK8G9QpE}Upgrd. Cost").ToString();
		GameTexts.SetVariable("LEFT", new TextObject("{=6bx9IhpD}Upgrades").ToString());
		GameTexts.SetVariable("RIGHT", new TextObject("{=guxNZZWh}Requirements").ToString());
		UpgradesAndRequirementsText = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
	}

	public void OnRanOutTroop(PartyCharacterVM troop)
	{
		if (base.IsOpen)
		{
			PartyTroopManagerItemVM item = base.Troops.FirstOrDefault((PartyTroopManagerItemVM x) => x.PartyCharacter == troop);
			base.Troops.Remove(item);
			_disabledTroopsStartIndex--;
		}
	}

	public void OnTroopUpgraded()
	{
		if (!base.IsOpen)
		{
			return;
		}
		_hasMadeChanges = true;
		for (int i = 0; i < _disabledTroopsStartIndex; i++)
		{
			if (base.Troops[i].PartyCharacter.NumOfReadyToUpgradeTroops <= 0)
			{
				_disabledTroopsStartIndex--;
				base.Troops.RemoveAt(i);
				i--;
			}
			else if (base.Troops[i].PartyCharacter.NumOfUpgradeableTroops <= 0)
			{
				_disabledTroopsStartIndex--;
				PartyTroopManagerItemVM item = base.Troops[i];
				base.Troops.RemoveAt(i);
				base.Troops.Insert(_disabledTroopsStartIndex, item);
				i--;
			}
		}
		UpdateLabels();
	}

	public override void OpenPopUp()
	{
		base.OpenPopUp();
		PopulateTroops();
		UpdateUpgradesOfAllTroops();
	}

	public override void ExecuteDone()
	{
		base.ExecuteDone();
		_partyVM.OnUpgradePopUpClosed(isCancelled: false);
	}

	public override void ExecuteCancel()
	{
		ShowCancelInquiry(ConfirmCancel);
	}

	protected override void ConfirmCancel()
	{
		base.ConfirmCancel();
		_partyVM.OnUpgradePopUpClosed(isCancelled: true);
	}

	private void UpdateUpgradesOfAllTroops()
	{
		foreach (PartyTroopManagerItemVM troop in base.Troops)
		{
			troop.PartyCharacter.InitializeUpgrades();
		}
	}

	private void PopulateTroops()
	{
		base.Troops = new MBBindingList<PartyTroopManagerItemVM>();
		_disabledTroopsStartIndex = 0;
		foreach (PartyCharacterVM mainPartyTroop in _partyVM.MainPartyTroops)
		{
			if (mainPartyTroop.IsTroopUpgradable)
			{
				base.Troops.Insert(_disabledTroopsStartIndex, new PartyTroopManagerItemVM(mainPartyTroop, base.SetFocusedCharacter));
				_disabledTroopsStartIndex++;
			}
			else if (mainPartyTroop.NumOfReadyToUpgradeTroops > 0)
			{
				base.Troops.Add(new PartyTroopManagerItemVM(mainPartyTroop, base.SetFocusedCharacter));
			}
		}
	}

	private List<TooltipProperty> GetUsedHorsesTooltip()
	{
		List<Tuple<EquipmentElement, int>> list = _partyVM.PartyScreenLogic.CurrentData.UsedUpgradeHorsesHistory.ToList();
		foreach (Tuple<EquipmentElement, int> item in _initialUsedUpgradeHorsesHistory)
		{
			int num = list.FindIndex((Tuple<EquipmentElement, int> x) => x.Item1.IsEqualTo(item.Item1));
			if (num != -1)
			{
				if (list[num].Item2 > item.Item2)
				{
					list[num] = new Tuple<EquipmentElement, int>(list[num].Item1, list[num].Item2 - item.Item2);
				}
				else
				{
					list.RemoveAt(num);
				}
			}
		}
		return CampaignUIHelper.GetUsedHorsesTooltip(list);
	}

	public override void ExecuteItemPrimaryAction()
	{
		UpgradeTroopAtIndex(0);
	}

	public override void ExecuteItemSecondaryAction()
	{
		UpgradeTroopAtIndex(1);
	}

	public override void ExecuteItemTertiaryAction()
	{
		UpgradeTroopAtIndex(2);
	}

	private void UpgradeTroopAtIndex(int upgradeIndex)
	{
		PartyCharacterVM partyCharacterVM = base.FocusedTroop?.PartyCharacter;
		if (partyCharacterVM != null && partyCharacterVM.Upgrades.Count > upgradeIndex && partyCharacterVM.Upgrades[upgradeIndex].IsAvailable)
		{
			partyCharacterVM.Upgrades[upgradeIndex].ExecuteUpgrade();
		}
	}
}
