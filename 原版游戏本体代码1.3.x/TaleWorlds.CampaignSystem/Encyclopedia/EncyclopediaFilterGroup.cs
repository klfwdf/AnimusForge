using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000171 RID: 369
	public class EncyclopediaFilterGroup : ViewModel
	{
		// Token: 0x06001B11 RID: 6929 RVA: 0x0008B035 File Offset: 0x00089235
		public EncyclopediaFilterGroup(List<EncyclopediaFilterItem> filters, TextObject name)
		{
			this.Filters = filters;
			this.Name = name;
		}

		// Token: 0x170006E8 RID: 1768
		// (get) Token: 0x06001B12 RID: 6930 RVA: 0x0008B04B File Offset: 0x0008924B
		public Predicate<object> Predicate
		{
			get
			{
				return delegate(object item)
				{
					if (!this.Filters.Any((EncyclopediaFilterItem f) => f.IsActive))
					{
						return true;
					}
					foreach (EncyclopediaFilterItem encyclopediaFilterItem in this.Filters)
					{
						if (encyclopediaFilterItem.IsActive && encyclopediaFilterItem.Predicate(item))
						{
							return true;
						}
					}
					return false;
				};
			}
		}

		// Token: 0x04000904 RID: 2308
		public readonly List<EncyclopediaFilterItem> Filters;

		// Token: 0x04000905 RID: 2309
		public readonly TextObject Name;
	}
}
