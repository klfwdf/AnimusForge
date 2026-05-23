using System;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000082 RID: 130
	internal class DialogFlowContext
	{
		// Token: 0x060010D8 RID: 4312 RVA: 0x00050363 File Offset: 0x0004E563
		public DialogFlowContext(string token, bool byPlayer, DialogFlowContext parent, bool optionsUsedOnlyOnce)
		{
			this.Token = token;
			this.ByPlayer = byPlayer;
			this.Parent = parent;
			this.OptionsUsedOnlyOnce = optionsUsedOnlyOnce;
		}

		// Token: 0x04000543 RID: 1347
		internal readonly string Token;

		// Token: 0x04000544 RID: 1348
		internal readonly bool ByPlayer;

		// Token: 0x04000545 RID: 1349
		internal readonly DialogFlowContext Parent;

		// Token: 0x04000546 RID: 1350
		internal readonly bool OptionsUsedOnlyOnce;
	}
}
