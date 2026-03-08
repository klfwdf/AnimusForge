using System;
using TaleWorlds.CampaignSystem.Incidents;

namespace SandBox.View.Map
{
	// Token: 0x02000050 RID: 80
	public class MapIncidentView : MapView
	{
		// Token: 0x060002AC RID: 684 RVA: 0x00018311 File Offset: 0x00016511
		public MapIncidentView()
		{
		}

		// Token: 0x060002AD RID: 685 RVA: 0x00018319 File Offset: 0x00016519
		public MapIncidentView(Incident incident)
		{
			this.Incident = incident;
		}

		// Token: 0x0400017E RID: 382
		public readonly Incident Incident;
	}
}
