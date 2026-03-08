using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000271 RID: 625
	public class AnyNotableTypeTag : ConversationTag
	{
		// Token: 0x170008BF RID: 2239
		// (get) Token: 0x06002339 RID: 9017 RVA: 0x00098EFF File Offset: 0x000970FF
		public override string StringId
		{
			get
			{
				return "AnyNotableTypeTag";
			}
		}

		// Token: 0x0600233A RID: 9018 RVA: 0x00098F06 File Offset: 0x00097106
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.IsNotable;
		}

		// Token: 0x04000A7E RID: 2686
		public const string Id = "AnyNotableTypeTag";
	}
}
