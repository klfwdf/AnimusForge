using System;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200039C RID: 924
	public interface IPartyScreenPrisonHandler
	{
		// Token: 0x060034F7 RID: 13559
		void ExecuteTakeAllPrisonersScript();

		// Token: 0x060034F8 RID: 13560
		void ExecuteDoneScript();

		// Token: 0x060034F9 RID: 13561
		void ExecuteResetScript();

		// Token: 0x060034FA RID: 13562
		void ExecuteSellAllPrisoners();
	}
}
