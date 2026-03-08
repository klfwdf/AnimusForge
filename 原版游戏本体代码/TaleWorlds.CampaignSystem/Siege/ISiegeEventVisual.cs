using System;

namespace TaleWorlds.CampaignSystem.Siege
{
	// Token: 0x020002DB RID: 731
	public interface ISiegeEventVisual
	{
		// Token: 0x060027DC RID: 10204
		void Initialize();

		// Token: 0x060027DD RID: 10205
		void OnSiegeEventEnd();

		// Token: 0x060027DE RID: 10206
		void Tick();
	}
}
