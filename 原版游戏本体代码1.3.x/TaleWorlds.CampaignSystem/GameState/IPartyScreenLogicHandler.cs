using System;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200039A RID: 922
	public interface IPartyScreenLogicHandler
	{
		// Token: 0x060034F4 RID: 13556
		void RequestUserInput(string text, Action accept, Action cancel);
	}
}
