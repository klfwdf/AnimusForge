using System;
using System.Collections.Generic;

namespace SandBox.ViewModelCollection.GameOver
{
	// Token: 0x0200005B RID: 91
	public class StatCategory
	{
		// Token: 0x06000591 RID: 1425 RVA: 0x00014BA2 File Offset: 0x00012DA2
		public StatCategory(string id, IEnumerable<StatItem> items)
		{
			this.ID = id;
			this.Items = items;
		}

		// Token: 0x040002BC RID: 700
		public readonly IEnumerable<StatItem> Items;

		// Token: 0x040002BD RID: 701
		public readonly string ID;
	}
}
