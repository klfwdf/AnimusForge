using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000DC RID: 220
	public class EncyclopediaListFilterVM : ViewModel
	{
		// Token: 0x060014FF RID: 5375 RVA: 0x0005305E File Offset: 0x0005125E
		public EncyclopediaListFilterVM(EncyclopediaFilterItem filter, Action<EncyclopediaListFilterVM> UpdateFilters)
		{
			this.Filter = filter;
			this._isSelected = this.Filter.IsActive;
			this._updateFilters = UpdateFilters;
			this.RefreshValues();
		}

		// Token: 0x06001500 RID: 5376 RVA: 0x0005308B File Offset: 0x0005128B
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.Filter.Name.ToString();
		}

		// Token: 0x06001501 RID: 5377 RVA: 0x000530A9 File Offset: 0x000512A9
		public void CopyFilterFrom(Dictionary<EncyclopediaFilterItem, bool> filters)
		{
			if (filters.ContainsKey(this.Filter))
			{
				this.IsSelected = filters[this.Filter];
			}
		}

		// Token: 0x06001502 RID: 5378 RVA: 0x000530CB File Offset: 0x000512CB
		public void ExecuteOnFilterActivated()
		{
			Game.Current.EventManager.TriggerEvent<OnEncyclopediaFilterActivatedEvent>(new OnEncyclopediaFilterActivatedEvent());
		}

		// Token: 0x170006F6 RID: 1782
		// (get) Token: 0x06001503 RID: 5379 RVA: 0x000530E1 File Offset: 0x000512E1
		// (set) Token: 0x06001504 RID: 5380 RVA: 0x000530E9 File Offset: 0x000512E9
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
					this.Filter.IsActive = value;
					this._updateFilters(this);
				}
			}
		}

		// Token: 0x170006F7 RID: 1783
		// (get) Token: 0x06001505 RID: 5381 RVA: 0x0005311F File Offset: 0x0005131F
		// (set) Token: 0x06001506 RID: 5382 RVA: 0x00053127 File Offset: 0x00051327
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x04000995 RID: 2453
		public readonly EncyclopediaFilterItem Filter;

		// Token: 0x04000996 RID: 2454
		private readonly Action<EncyclopediaListFilterVM> _updateFilters;

		// Token: 0x04000997 RID: 2455
		private string _name;

		// Token: 0x04000998 RID: 2456
		private bool _isSelected;
	}
}
