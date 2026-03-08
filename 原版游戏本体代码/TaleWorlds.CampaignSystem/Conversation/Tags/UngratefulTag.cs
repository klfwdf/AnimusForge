using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200027E RID: 638
	public class UngratefulTag : ConversationTag
	{
		// Token: 0x170008CC RID: 2252
		// (get) Token: 0x06002360 RID: 9056 RVA: 0x0009915D File Offset: 0x0009735D
		public override string StringId
		{
			get
			{
				return "UngratefulTag";
			}
		}

		// Token: 0x06002361 RID: 9057 RVA: 0x00099164 File Offset: 0x00097364
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Generosity) < 0;
		}

		// Token: 0x04000A8B RID: 2699
		public const string Id = "UngratefulTag";
	}
}
