using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp
{
	// Token: 0x02000035 RID: 53
	public class PartyUpgradeTroopVM : PartyTroopManagerVM
	{
		// Token: 0x0600054A RID: 1354 RVA: 0x0001CC7C File Offset: 0x0001AE7C
		public PartyUpgradeTroopVM(PartyVM partyVM)
			: base(partyVM)
		{
			this.RefreshValues();
			base.IsUpgradePopUp = true;
			this._openButtonEnabledHint = new TextObject("{=hRSezxnT}Some of your troops are ready to upgrade.", null);
			this._openButtonNoTroopsHint = new TextObject("{=fpE7BQ7f}You don't have any upgradable troops.", null);
			this._openButtonIrrelevantScreenHint = new TextObject("{=mdvnjI72}Troops are not upgradable in this screen.", null);
			this._openButtonUpgradesDisabledHint = new TextObject("{=R4rTlKMU}Troop upgrades are currently disabled.", null);
			base.UsedHorsesHint = new BasicTooltipViewModel(() => this.GetUsedHorsesTooltip());
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x0001CD00 File Offset: 0x0001AF00
		public override void RefreshValues()
		{
			base.RefreshValues();
			base.TitleText = new TextObject("{=IgoxNz2H}Upgrade Troops", null).ToString();
			this.UpgradeCostText = new TextObject("{=SK8G9QpE}Upgrd. Cost", null).ToString();
			GameTexts.SetVariable("LEFT", new TextObject("{=6bx9IhpD}Upgrades", null).ToString());
			GameTexts.SetVariable("RIGHT", new TextObject("{=guxNZZWh}Requirements", null).ToString());
			this.UpgradesAndRequirementsText = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x0001CD8C File Offset: 0x0001AF8C
		public void OnRanOutTroop(PartyCharacterVM troop)
		{
			if (!base.IsOpen)
			{
				return;
			}
			PartyTroopManagerItemVM item = base.Troops.FirstOrDefault((PartyTroopManagerItemVM x) => x.PartyCharacter == troop);
			base.Troops.Remove(item);
			this._disabledTroopsStartIndex--;
		}

		// Token: 0x0600054D RID: 1357 RVA: 0x0001CDE4 File Offset: 0x0001AFE4
		public void OnTroopUpgraded()
		{
			if (!base.IsOpen)
			{
				return;
			}
			this._hasMadeChanges = true;
			for (int i = 0; i < this._disabledTroopsStartIndex; i++)
			{
				if (base.Troops[i].PartyCharacter.NumOfReadyToUpgradeTroops <= 0)
				{
					this._disabledTroopsStartIndex--;
					base.Troops.RemoveAt(i);
					i--;
				}
				else if (base.Troops[i].PartyCharacter.NumOfUpgradeableTroops <= 0)
				{
					this._disabledTroopsStartIndex--;
					PartyTroopManagerItemVM item = base.Troops[i];
					base.Troops.RemoveAt(i);
					base.Troops.Insert(this._disabledTroopsStartIndex, item);
					i--;
				}
			}
			base.UpdateLabels();
		}

		// Token: 0x0600054E RID: 1358 RVA: 0x0001CEAD File Offset: 0x0001B0AD
		public override void OpenPopUp()
		{
			base.OpenPopUp();
			this.PopulateTroops();
			this.UpdateUpgradesOfAllTroops();
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x0001CEC1 File Offset: 0x0001B0C1
		public override void ExecuteDone()
		{
			base.ExecuteDone();
			this._partyVM.OnUpgradePopUpClosed(false);
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x0001CED5 File Offset: 0x0001B0D5
		public override void ExecuteCancel()
		{
			base.ShowCancelInquiry(new Action(this.ConfirmCancel));
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x0001CEEA File Offset: 0x0001B0EA
		protected override void ConfirmCancel()
		{
			base.ConfirmCancel();
			this._partyVM.OnUpgradePopUpClosed(true);
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x0001CF00 File Offset: 0x0001B100
		private void UpdateUpgradesOfAllTroops()
		{
			foreach (PartyTroopManagerItemVM partyTroopManagerItemVM in base.Troops)
			{
				partyTroopManagerItemVM.PartyCharacter.InitializeUpgrades();
			}
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x0001CF50 File Offset: 0x0001B150
		private void PopulateTroops()
		{
			base.Troops = new MBBindingList<PartyTroopManagerItemVM>();
			this._disabledTroopsStartIndex = 0;
			foreach (PartyCharacterVM partyCharacterVM in this._partyVM.MainPartyTroops)
			{
				if (partyCharacterVM.IsTroopUpgradable)
				{
					base.Troops.Insert(this._disabledTroopsStartIndex, new PartyTroopManagerItemVM(partyCharacterVM, new Action<PartyTroopManagerItemVM>(base.SetFocusedCharacter)));
					this._disabledTroopsStartIndex++;
				}
				else if (partyCharacterVM.NumOfReadyToUpgradeTroops > 0)
				{
					base.Troops.Add(new PartyTroopManagerItemVM(partyCharacterVM, new Action<PartyTroopManagerItemVM>(base.SetFocusedCharacter)));
				}
			}
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x0001D010 File Offset: 0x0001B210
		private List<TooltipProperty> GetUsedHorsesTooltip()
		{
			List<Tuple<EquipmentElement, int>> list = this._partyVM.PartyScreenLogic.CurrentData.UsedUpgradeHorsesHistory.ToList<Tuple<EquipmentElement, int>>();
			using (List<Tuple<EquipmentElement, int>>.Enumerator enumerator = this._initialUsedUpgradeHorsesHistory.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Tuple<EquipmentElement, int> item = enumerator.Current;
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
			}
			return CampaignUIHelper.GetUsedHorsesTooltip(list);
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x0001D0F4 File Offset: 0x0001B2F4
		public override void ExecuteItemPrimaryAction()
		{
			this.UpgradeTroopAtIndex(0);
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x0001D0FD File Offset: 0x0001B2FD
		public override void ExecuteItemSecondaryAction()
		{
			this.UpgradeTroopAtIndex(1);
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x0001D106 File Offset: 0x0001B306
		public override void ExecuteItemTertiaryAction()
		{
			this.UpgradeTroopAtIndex(2);
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x0001D110 File Offset: 0x0001B310
		private void UpgradeTroopAtIndex(int upgradeIndex)
		{
			PartyTroopManagerItemVM focusedTroop = base.FocusedTroop;
			PartyCharacterVM partyCharacterVM = ((focusedTroop != null) ? focusedTroop.PartyCharacter : null);
			if (partyCharacterVM != null && partyCharacterVM.Upgrades.Count > upgradeIndex && partyCharacterVM.Upgrades[upgradeIndex].IsAvailable)
			{
				partyCharacterVM.Upgrades[upgradeIndex].ExecuteUpgrade();
			}
		}

		// Token: 0x17000183 RID: 387
		// (get) Token: 0x06000559 RID: 1369 RVA: 0x0001D168 File Offset: 0x0001B368
		// (set) Token: 0x0600055A RID: 1370 RVA: 0x0001D170 File Offset: 0x0001B370
		[DataSourceProperty]
		public string UpgradeCostText
		{
			get
			{
				return this._upgradeCostText;
			}
			set
			{
				if (value != this._upgradeCostText)
				{
					this._upgradeCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "UpgradeCostText");
				}
			}
		}

		// Token: 0x17000184 RID: 388
		// (get) Token: 0x0600055B RID: 1371 RVA: 0x0001D193 File Offset: 0x0001B393
		// (set) Token: 0x0600055C RID: 1372 RVA: 0x0001D19B File Offset: 0x0001B39B
		[DataSourceProperty]
		public string UpgradesAndRequirementsText
		{
			get
			{
				return this._upgradesAndRequirementsText;
			}
			set
			{
				if (value != this._upgradesAndRequirementsText)
				{
					this._upgradesAndRequirementsText = value;
					base.OnPropertyChangedWithValue<string>(value, "UpgradesAndRequirementsText");
				}
			}
		}

		// Token: 0x04000247 RID: 583
		private int _disabledTroopsStartIndex = -1;

		// Token: 0x04000248 RID: 584
		private string _upgradeCostText;

		// Token: 0x04000249 RID: 585
		private string _upgradesAndRequirementsText;
	}
}
