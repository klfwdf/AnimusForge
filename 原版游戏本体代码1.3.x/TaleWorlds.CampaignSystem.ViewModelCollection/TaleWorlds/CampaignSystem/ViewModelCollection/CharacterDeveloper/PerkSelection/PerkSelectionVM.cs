using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection
{
	// Token: 0x02000148 RID: 328
	public class PerkSelectionVM : ViewModel
	{
		// Token: 0x06001F50 RID: 8016 RVA: 0x00072B4D File Offset: 0x00070D4D
		public PerkSelectionVM(HeroDeveloper developer, Action<SkillObject> refreshPerksOf, Action onPerkSelection)
		{
			this._developer = developer;
			this._refreshPerksOf = refreshPerksOf;
			this._onPerkSelection = onPerkSelection;
			this._selectedPerks = new List<PerkObject>();
			this.AvailablePerks = new MBBindingList<PerkSelectionItemVM>();
			this.IsActive = false;
		}

		// Token: 0x06001F51 RID: 8017 RVA: 0x00072B88 File Offset: 0x00070D88
		public void SetCurrentSelectionPerk(PerkVM perk)
		{
			if (this.AvailablePerks.Count > 0 || this.IsActive)
			{
				this.ExecuteDeactivate();
			}
			this.AvailablePerks.Clear();
			this._currentInitialPerk = perk;
			this.AvailablePerks.Add(new PerkSelectionItemVM(perk.Perk, new Action<PerkSelectionItemVM>(this.OnSelectPerk)));
			if (perk.AlternativeType == 2)
			{
				this.AvailablePerks.Insert(0, new PerkSelectionItemVM(perk.Perk.AlternativePerk, new Action<PerkSelectionItemVM>(this.OnSelectPerk)));
			}
			else if (perk.AlternativeType == 1)
			{
				this.AvailablePerks.Add(new PerkSelectionItemVM(perk.Perk.AlternativePerk, new Action<PerkSelectionItemVM>(this.OnSelectPerk)));
			}
			perk.IsInSelection = true;
			this.IsActive = true;
		}

		// Token: 0x06001F52 RID: 8018 RVA: 0x00072C58 File Offset: 0x00070E58
		private void OnSelectPerk(PerkSelectionItemVM selectedPerk)
		{
			this._selectedPerks.Add(selectedPerk.Perk);
			this._refreshPerksOf(selectedPerk.Perk.Skill);
			this._currentInitialPerk.IsInSelection = false;
			this.IsActive = false;
			Game.Current.EventManager.TriggerEvent<PerkSelectedByPlayerEvent>(new PerkSelectedByPlayerEvent(selectedPerk.Perk));
			Action onPerkSelection = this._onPerkSelection;
			if (onPerkSelection == null)
			{
				return;
			}
			onPerkSelection();
		}

		// Token: 0x06001F53 RID: 8019 RVA: 0x00072CCC File Offset: 0x00070ECC
		public void ResetSelectedPerks()
		{
			foreach (PerkObject perkObject in this._selectedPerks)
			{
				this._refreshPerksOf(perkObject.Skill);
			}
			this._selectedPerks.Clear();
		}

		// Token: 0x06001F54 RID: 8020 RVA: 0x00072D34 File Offset: 0x00070F34
		public void ApplySelectedPerks()
		{
			foreach (PerkObject perkObject in this._selectedPerks.ToList<PerkObject>())
			{
				this._developer.AddPerk(perkObject);
				this._selectedPerks.Remove(perkObject);
			}
		}

		// Token: 0x06001F55 RID: 8021 RVA: 0x00072DA0 File Offset: 0x00070FA0
		public bool IsPerkSelected(PerkObject perk)
		{
			return this._selectedPerks.Contains(perk);
		}

		// Token: 0x06001F56 RID: 8022 RVA: 0x00072DAE File Offset: 0x00070FAE
		public bool IsAnyPerkSelected()
		{
			return this._selectedPerks.Count > 0;
		}

		// Token: 0x06001F57 RID: 8023 RVA: 0x00072DBE File Offset: 0x00070FBE
		public void ExecuteDeactivate()
		{
			this.IsActive = false;
			this._refreshPerksOf(this._currentInitialPerk.Perk.Skill);
			this._currentInitialPerk.IsInSelection = false;
		}

		// Token: 0x17000AAD RID: 2733
		// (get) Token: 0x06001F58 RID: 8024 RVA: 0x00072DEE File Offset: 0x00070FEE
		// (set) Token: 0x06001F59 RID: 8025 RVA: 0x00072DF6 File Offset: 0x00070FF6
		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value != this._isActive)
				{
					this._isActive = value;
					base.OnPropertyChangedWithValue(value, "IsActive");
					Game.Current.EventManager.TriggerEvent<PerkSelectionToggleEvent>(new PerkSelectionToggleEvent(this.IsActive));
				}
			}
		}

		// Token: 0x17000AAE RID: 2734
		// (get) Token: 0x06001F5A RID: 8026 RVA: 0x00072E2E File Offset: 0x0007102E
		// (set) Token: 0x06001F5B RID: 8027 RVA: 0x00072E36 File Offset: 0x00071036
		[DataSourceProperty]
		public MBBindingList<PerkSelectionItemVM> AvailablePerks
		{
			get
			{
				return this._availablePerks;
			}
			set
			{
				if (value != this._availablePerks)
				{
					this._availablePerks = value;
					base.OnPropertyChangedWithValue<MBBindingList<PerkSelectionItemVM>>(value, "AvailablePerks");
				}
			}
		}

		// Token: 0x04000E9F RID: 3743
		private readonly HeroDeveloper _developer;

		// Token: 0x04000EA0 RID: 3744
		private readonly List<PerkObject> _selectedPerks;

		// Token: 0x04000EA1 RID: 3745
		private readonly Action<SkillObject> _refreshPerksOf;

		// Token: 0x04000EA2 RID: 3746
		private readonly Action _onPerkSelection;

		// Token: 0x04000EA3 RID: 3747
		private PerkVM _currentInitialPerk;

		// Token: 0x04000EA4 RID: 3748
		private bool _isActive;

		// Token: 0x04000EA5 RID: 3749
		private MBBindingList<PerkSelectionItemVM> _availablePerks;
	}
}
