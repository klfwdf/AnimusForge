using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x02000221 RID: 545
	public class WeatherNode
	{
		// Token: 0x17000803 RID: 2051
		// (get) Token: 0x0600208D RID: 8333 RVA: 0x0008F6DC File Offset: 0x0008D8DC
		// (set) Token: 0x0600208E RID: 8334 RVA: 0x0008F6E4 File Offset: 0x0008D8E4
		public bool IsVisuallyDirty { get; private set; }

		// Token: 0x0600208F RID: 8335 RVA: 0x0008F6ED File Offset: 0x0008D8ED
		public WeatherNode(CampaignVec2 position)
		{
			this.Position = position;
			this.CurrentWeatherEvent = MapWeatherModel.WeatherEvent.Clear;
		}

		// Token: 0x06002090 RID: 8336 RVA: 0x0008F703 File Offset: 0x0008D903
		public void SetVisualDirty()
		{
			this.IsVisuallyDirty = true;
		}

		// Token: 0x06002091 RID: 8337 RVA: 0x0008F70C File Offset: 0x0008D90C
		public void OnVisualUpdated()
		{
			this.IsVisuallyDirty = false;
		}

		// Token: 0x04000988 RID: 2440
		public CampaignVec2 Position;

		// Token: 0x0400098A RID: 2442
		public MapWeatherModel.WeatherEvent CurrentWeatherEvent;
	}
}
