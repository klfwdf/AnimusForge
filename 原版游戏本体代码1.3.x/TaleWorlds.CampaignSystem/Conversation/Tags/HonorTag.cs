using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200027F RID: 639
	public class HonorTag : ConversationTag
	{
		// Token: 0x170008CD RID: 2253
		// (get) Token: 0x06002363 RID: 9059 RVA: 0x0009918B File Offset: 0x0009738B
		public override string StringId
		{
			get
			{
				return "HonorTag";
			}
		}

		// Token: 0x06002364 RID: 9060 RVA: 0x00099192 File Offset: 0x00097392
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Honor) > 0;
		}

		// Token: 0x04000A8C RID: 2700
		public const string Id = "HonorTag";
	}
}
