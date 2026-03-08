using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000279 RID: 633
	public class AseraiTag : ConversationTag
	{
		// Token: 0x170008C7 RID: 2247
		// (get) Token: 0x06002351 RID: 9041 RVA: 0x00099087 File Offset: 0x00097287
		public override string StringId
		{
			get
			{
				return "AseraiTag";
			}
		}

		// Token: 0x06002352 RID: 9042 RVA: 0x0009908E File Offset: 0x0009728E
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "aserai";
		}

		// Token: 0x04000A86 RID: 2694
		public const string Id = "AseraiTag";
	}
}
