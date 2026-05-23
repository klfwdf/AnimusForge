using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000286 RID: 646
	public class NordTag : ConversationTag
	{
		// Token: 0x170008D4 RID: 2260
		// (get) Token: 0x06002378 RID: 9080 RVA: 0x00099321 File Offset: 0x00097521
		public override string StringId
		{
			get
			{
				return "NordTag";
			}
		}

		// Token: 0x06002379 RID: 9081 RVA: 0x00099328 File Offset: 0x00097528
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "nord";
		}

		// Token: 0x04000A93 RID: 2707
		public const string Id = "NordTag";
	}
}
