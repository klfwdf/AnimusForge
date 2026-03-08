using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200026D RID: 621
	public class HeadmanNotableTypeTag : ConversationTag
	{
		// Token: 0x170008BB RID: 2235
		// (get) Token: 0x0600232D RID: 9005 RVA: 0x00098E6B File Offset: 0x0009706B
		public override string StringId
		{
			get
			{
				return "HeadmanNotableTypeTag";
			}
		}

		// Token: 0x0600232E RID: 9006 RVA: 0x00098E72 File Offset: 0x00097072
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.Occupation == Occupation.Headman;
		}

		// Token: 0x04000A7A RID: 2682
		public const string Id = "HeadmanNotableTypeTag";
	}
}
