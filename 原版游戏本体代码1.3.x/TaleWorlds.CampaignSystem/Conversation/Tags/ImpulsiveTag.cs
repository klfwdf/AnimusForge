using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000282 RID: 642
	public class ImpulsiveTag : ConversationTag
	{
		// Token: 0x170008D0 RID: 2256
		// (get) Token: 0x0600236C RID: 9068 RVA: 0x00099215 File Offset: 0x00097415
		public override string StringId
		{
			get
			{
				return "ImpulsiveTag";
			}
		}

		// Token: 0x0600236D RID: 9069 RVA: 0x0009921C File Offset: 0x0009741C
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) < 0;
		}

		// Token: 0x04000A8F RID: 2703
		public const string Id = "ImpulsiveTag";
	}
}
