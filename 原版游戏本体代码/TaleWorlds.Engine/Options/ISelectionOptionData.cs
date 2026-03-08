using System;
using System.Collections.Generic;

namespace TaleWorlds.Engine.Options
{
	// Token: 0x020000A6 RID: 166
	public interface ISelectionOptionData : IOptionData
	{
		// Token: 0x06000F35 RID: 3893
		int GetSelectableOptionsLimit();

		// Token: 0x06000F36 RID: 3894
		IEnumerable<SelectionData> GetSelectableOptionNames();
	}
}
