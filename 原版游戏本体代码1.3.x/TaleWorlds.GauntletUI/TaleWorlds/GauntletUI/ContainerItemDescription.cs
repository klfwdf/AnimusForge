using System;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200001E RID: 30
	public class ContainerItemDescription
	{
		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000249 RID: 585 RVA: 0x0000BD9D File Offset: 0x00009F9D
		// (set) Token: 0x0600024A RID: 586 RVA: 0x0000BDA5 File Offset: 0x00009FA5
		public string WidgetId { get; set; }

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x0600024B RID: 587 RVA: 0x0000BDAE File Offset: 0x00009FAE
		// (set) Token: 0x0600024C RID: 588 RVA: 0x0000BDB6 File Offset: 0x00009FB6
		public int WidgetIndex { get; set; }

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x0600024D RID: 589 RVA: 0x0000BDBF File Offset: 0x00009FBF
		// (set) Token: 0x0600024E RID: 590 RVA: 0x0000BDC7 File Offset: 0x00009FC7
		public float WidthStretchRatio { get; set; }

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x0600024F RID: 591 RVA: 0x0000BDD0 File Offset: 0x00009FD0
		// (set) Token: 0x06000250 RID: 592 RVA: 0x0000BDD8 File Offset: 0x00009FD8
		public float HeightStretchRatio { get; set; }

		// Token: 0x06000251 RID: 593 RVA: 0x0000BDE1 File Offset: 0x00009FE1
		public ContainerItemDescription()
		{
			this.WidgetId = "";
			this.WidgetIndex = -1;
			this.WidthStretchRatio = 1f;
			this.HeightStretchRatio = 1f;
		}
	}
}
