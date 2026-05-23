using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200023A RID: 570
	public class DefaultTag : ConversationTag
	{
		// Token: 0x17000888 RID: 2184
		// (get) Token: 0x06002294 RID: 8852 RVA: 0x00098198 File Offset: 0x00096398
		public override string StringId
		{
			get
			{
				return "DefaultTag";
			}
		}

		// Token: 0x06002295 RID: 8853 RVA: 0x0009819F File Offset: 0x0009639F
		public override bool IsApplicableTo(CharacterObject character)
		{
			return true;
		}

		// Token: 0x04000A46 RID: 2630
		public const string Id = "DefaultTag";
	}
}
