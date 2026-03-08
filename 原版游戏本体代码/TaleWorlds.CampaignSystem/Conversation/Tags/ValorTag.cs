using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000283 RID: 643
	public class ValorTag : ConversationTag
	{
		// Token: 0x170008D1 RID: 2257
		// (get) Token: 0x0600236F RID: 9071 RVA: 0x00099243 File Offset: 0x00097443
		public override string StringId
		{
			get
			{
				return "ValorTag";
			}
		}

		// Token: 0x06002370 RID: 9072 RVA: 0x0009924A File Offset: 0x0009744A
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.GetTraitLevel(DefaultTraits.Valor) > 0;
		}

		// Token: 0x04000A90 RID: 2704
		public const string Id = "ValorTag";
	}
}
