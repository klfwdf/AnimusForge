using System;
using TaleWorlds.GauntletUI.Layout;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000056 RID: 86
	public class DragCarrierWidget : Widget
	{
		// Token: 0x060005BF RID: 1471 RVA: 0x00017D47 File Offset: 0x00015F47
		public DragCarrierWidget(UIContext context)
			: base(context)
		{
			base.LayoutImp = new DragCarrierLayout();
			base.DoNotAcceptEvents = true;
			base.DoNotPassEventsToChildren = true;
			base.IsDisabled = true;
		}
	}
}
