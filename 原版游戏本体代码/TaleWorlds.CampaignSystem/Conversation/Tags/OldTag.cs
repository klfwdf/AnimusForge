using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000266 RID: 614
	public class OldTag : ConversationTag
	{
		// Token: 0x170008B4 RID: 2228
		// (get) Token: 0x06002318 RID: 8984 RVA: 0x00098CC3 File Offset: 0x00096EC3
		public override string StringId
		{
			get
			{
				return "OldTag";
			}
		}

		// Token: 0x06002319 RID: 8985 RVA: 0x00098CCA File Offset: 0x00096ECA
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Age > (float)Campaign.Current.Models.AgeModel.BecomeOldAge;
		}

		// Token: 0x04000A73 RID: 2675
		public const string Id = "OldTag";
	}
}
