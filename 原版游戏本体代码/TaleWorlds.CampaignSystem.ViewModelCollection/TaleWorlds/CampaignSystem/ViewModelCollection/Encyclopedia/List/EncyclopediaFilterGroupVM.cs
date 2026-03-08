using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000DB RID: 219
	public class EncyclopediaFilterGroupVM : ViewModel
	{
		// Token: 0x060014F8 RID: 5368 RVA: 0x00052F0C File Offset: 0x0005110C
		public EncyclopediaFilterGroupVM(EncyclopediaFilterGroup filterGroup, Action<EncyclopediaListFilterVM> UpdateFilters)
		{
			this.FilterGroup = filterGroup;
			this.Filters = new MBBindingList<EncyclopediaListFilterVM>();
			foreach (EncyclopediaFilterItem filter in filterGroup.Filters)
			{
				this.Filters.Add(new EncyclopediaListFilterVM(filter, UpdateFilters));
			}
			this.RefreshValues();
		}

		// Token: 0x060014F9 RID: 5369 RVA: 0x00052F88 File Offset: 0x00051188
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Filters.ApplyActionOnAllItems(delegate(EncyclopediaListFilterVM x)
			{
				x.RefreshValues();
			});
			this.FilterName = this.FilterGroup.Name.ToString();
		}

		// Token: 0x060014FA RID: 5370 RVA: 0x00052FDC File Offset: 0x000511DC
		public void CopyFiltersFrom(Dictionary<EncyclopediaFilterItem, bool> filters)
		{
			this.Filters.ApplyActionOnAllItems(delegate(EncyclopediaListFilterVM x)
			{
				x.CopyFilterFrom(filters);
			});
		}

		// Token: 0x170006F4 RID: 1780
		// (get) Token: 0x060014FB RID: 5371 RVA: 0x0005300D File Offset: 0x0005120D
		// (set) Token: 0x060014FC RID: 5372 RVA: 0x00053015 File Offset: 0x00051215
		[DataSourceProperty]
		public string FilterName
		{
			get
			{
				return this._filterName;
			}
			set
			{
				if (value != this._filterName)
				{
					this._filterName = value;
					base.OnPropertyChangedWithValue<string>(value, "FilterName");
				}
			}
		}

		// Token: 0x170006F5 RID: 1781
		// (get) Token: 0x060014FD RID: 5373 RVA: 0x00053038 File Offset: 0x00051238
		// (set) Token: 0x060014FE RID: 5374 RVA: 0x00053040 File Offset: 0x00051240
		[DataSourceProperty]
		public MBBindingList<EncyclopediaListFilterVM> Filters
		{
			get
			{
				return this._filters;
			}
			set
			{
				if (value != this._filters)
				{
					this._filters = value;
					base.OnPropertyChangedWithValue<MBBindingList<EncyclopediaListFilterVM>>(value, "Filters");
				}
			}
		}

		// Token: 0x04000992 RID: 2450
		public readonly EncyclopediaFilterGroup FilterGroup;

		// Token: 0x04000993 RID: 2451
		private MBBindingList<EncyclopediaListFilterVM> _filters;

		// Token: 0x04000994 RID: 2452
		private string _filterName;
	}
}
