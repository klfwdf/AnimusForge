using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000278 RID: 632
	public class KhuzaitTag : ConversationTag
	{
		// Token: 0x170008C6 RID: 2246
		// (get) Token: 0x0600234E RID: 9038 RVA: 0x00099061 File Offset: 0x00097261
		public override string StringId
		{
			get
			{
				return "KhuzaitTag";
			}
		}

		// Token: 0x0600234F RID: 9039 RVA: 0x00099068 File Offset: 0x00097268
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "khuzait";
		}

		// Token: 0x04000A85 RID: 2693
		public const string Id = "KhuzaitTag";
	}
}
