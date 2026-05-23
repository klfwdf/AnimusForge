using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000039 RID: 57
	public abstract class WidgetComponent
	{
		// Token: 0x17000133 RID: 307
		// (get) Token: 0x060003E3 RID: 995 RVA: 0x0000FB05 File Offset: 0x0000DD05
		// (set) Token: 0x060003E4 RID: 996 RVA: 0x0000FB0D File Offset: 0x0000DD0D
		public Widget Target { get; private set; }

		// Token: 0x060003E5 RID: 997 RVA: 0x0000FB16 File Offset: 0x0000DD16
		protected WidgetComponent(Widget target)
		{
			this.Target = target;
		}
	}
}
