using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets.Graph
{
	// Token: 0x02000019 RID: 25
	public class GraphLinePointWidget : BrushWidget
	{
		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000149 RID: 329 RVA: 0x0000756B File Offset: 0x0000576B
		// (set) Token: 0x0600014A RID: 330 RVA: 0x00007573 File Offset: 0x00005773
		public float HorizontalValue { get; set; }

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x0600014B RID: 331 RVA: 0x0000757C File Offset: 0x0000577C
		// (set) Token: 0x0600014C RID: 332 RVA: 0x00007584 File Offset: 0x00005784
		public float VerticalValue { get; set; }

		// Token: 0x0600014D RID: 333 RVA: 0x0000758D File Offset: 0x0000578D
		public GraphLinePointWidget(UIContext context)
			: base(context)
		{
		}
	}
}
