using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A5 RID: 165
	public class SettlementGovernorSelectionVM : ViewModel
	{
		// Token: 0x06000FDC RID: 4060 RVA: 0x0004111C File Offset: 0x0003F31C
		public SettlementGovernorSelectionVM(Settlement settlement, Action<Hero> onDone)
		{
			this._settlement = settlement;
			this._onDone = onDone;
			this.AvailableGovernors = new MBBindingList<SettlementGovernorSelectionItemVM>
			{
				new SettlementGovernorSelectionItemVM(null, new Action<SettlementGovernorSelectionItemVM>(this.OnSelection))
			};
			if (((settlement != null) ? settlement.OwnerClan : null) != null)
			{
				using (List<Hero>.Enumerator enumerator = settlement.OwnerClan.Heroes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Hero hero = enumerator.Current;
						if (Campaign.Current.Models.ClanPoliticsModel.CanHeroBeGovernor(hero) && !this.AvailableGovernors.Any((SettlementGovernorSelectionItemVM G) => G.Governor == hero) && (hero.GovernorOf == this._settlement.Town || hero.GovernorOf == null))
						{
							this.AvailableGovernors.Add(new SettlementGovernorSelectionItemVM(hero, new Action<SettlementGovernorSelectionItemVM>(this.OnSelection)));
						}
					}
				}
			}
		}

		// Token: 0x06000FDD RID: 4061 RVA: 0x0004124C File Offset: 0x0003F44C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.AvailableGovernors.ApplyActionOnAllItems(delegate(SettlementGovernorSelectionItemVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06000FDE RID: 4062 RVA: 0x0004127E File Offset: 0x0003F47E
		private void OnSelection(SettlementGovernorSelectionItemVM item)
		{
			Action<Hero> onDone = this._onDone;
			if (onDone == null)
			{
				return;
			}
			onDone(item.Governor);
		}

		// Token: 0x1700051F RID: 1311
		// (get) Token: 0x06000FDF RID: 4063 RVA: 0x00041296 File Offset: 0x0003F496
		// (set) Token: 0x06000FE0 RID: 4064 RVA: 0x0004129E File Offset: 0x0003F49E
		[DataSourceProperty]
		public MBBindingList<SettlementGovernorSelectionItemVM> AvailableGovernors
		{
			get
			{
				return this._availableGovernors;
			}
			set
			{
				if (value != this._availableGovernors)
				{
					this._availableGovernors = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementGovernorSelectionItemVM>>(value, "AvailableGovernors");
				}
			}
		}

		// Token: 0x17000520 RID: 1312
		// (get) Token: 0x06000FE1 RID: 4065 RVA: 0x000412BC File Offset: 0x0003F4BC
		// (set) Token: 0x06000FE2 RID: 4066 RVA: 0x000412C4 File Offset: 0x0003F4C4
		[DataSourceProperty]
		public int CurrentGovernorIndex
		{
			get
			{
				return this._currentGovernorIndex;
			}
			set
			{
				if (value != this._currentGovernorIndex)
				{
					this._currentGovernorIndex = value;
					base.OnPropertyChangedWithValue(value, "CurrentGovernorIndex");
				}
			}
		}

		// Token: 0x0400073A RID: 1850
		private readonly Settlement _settlement;

		// Token: 0x0400073B RID: 1851
		private readonly Action<Hero> _onDone;

		// Token: 0x0400073C RID: 1852
		private MBBindingList<SettlementGovernorSelectionItemVM> _availableGovernors;

		// Token: 0x0400073D RID: 1853
		private int _currentGovernorIndex;
	}
}
