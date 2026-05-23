using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000258 RID: 600
	public class FriendlyRelationshipTag : ConversationTag
	{
		// Token: 0x170008A6 RID: 2214
		// (get) Token: 0x060022EE RID: 8942 RVA: 0x00098864 File Offset: 0x00096A64
		public override string StringId
		{
			get
			{
				return "FriendlyRelationshipTag";
			}
		}

		// Token: 0x060022EF RID: 8943 RVA: 0x0009886C File Offset: 0x00096A6C
		public override bool IsApplicableTo(CharacterObject character)
		{
			if (!character.IsHero)
			{
				return false;
			}
			float unmodifiedClanLeaderRelationshipWithPlayer = character.HeroObject.GetUnmodifiedClanLeaderRelationshipWithPlayer();
			int num = ConversationTagHelper.TraitCompatibility(character.HeroObject, Hero.MainHero, DefaultTraits.Mercy);
			int num2 = ConversationTagHelper.TraitCompatibility(character.HeroObject, Hero.MainHero, DefaultTraits.Honor);
			int num3 = ConversationTagHelper.TraitCompatibility(character.HeroObject, Hero.MainHero, DefaultTraits.Valor);
			return (num + num2 + num3 > 0 && unmodifiedClanLeaderRelationshipWithPlayer >= 5f) || unmodifiedClanLeaderRelationshipWithPlayer >= 20f;
		}

		// Token: 0x04000A64 RID: 2660
		public const string Id = "FriendlyRelationshipTag";
	}
}
