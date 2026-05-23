using System;

namespace TaleWorlds.CampaignSystem.Conversation
{
	// Token: 0x02000235 RID: 565
	public interface IConversationStateHandler
	{
		// Token: 0x06002260 RID: 8800
		void OnConversationInstall();

		// Token: 0x06002261 RID: 8801
		void OnConversationUninstall();

		// Token: 0x06002262 RID: 8802
		void OnConversationActivate();

		// Token: 0x06002263 RID: 8803
		void OnConversationDeactivate();

		// Token: 0x06002264 RID: 8804
		void OnConversationContinue();

		// Token: 0x06002265 RID: 8805
		void ExecuteConversationContinue();
	}
}
