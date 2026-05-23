using System;

namespace TaleWorlds.Engine.Options
{
	// Token: 0x020000A7 RID: 167
	public struct SelectionData
	{
		// Token: 0x06000F37 RID: 3895 RVA: 0x00011BF6 File Offset: 0x0000FDF6
		public SelectionData(bool isLocalizationId, string data)
		{
			this.IsLocalizationId = isLocalizationId;
			this.Data = data;
		}

		// Token: 0x04000218 RID: 536
		public bool IsLocalizationId;

		// Token: 0x04000219 RID: 537
		public string Data;
	}
}
