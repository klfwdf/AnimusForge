using System;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x0200000E RID: 14
	internal readonly struct ViewBindDataInfo
	{
		// Token: 0x06000098 RID: 152 RVA: 0x00004483 File Offset: 0x00002683
		internal ViewBindDataInfo(GauntletView view, string property, BindingPath path)
		{
			this.IsValid = true;
			this.Owner = view;
			this.Property = property;
			this.Path = path;
		}

		// Token: 0x04000028 RID: 40
		internal readonly bool IsValid;

		// Token: 0x04000029 RID: 41
		internal readonly GauntletView Owner;

		// Token: 0x0400002A RID: 42
		internal readonly string Property;

		// Token: 0x0400002B RID: 43
		internal readonly BindingPath Path;
	}
}
