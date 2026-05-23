using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000259 RID: 601
	public class HostileRelationshipTag : ConversationTag
	{
		// Token: 0x170008A7 RID: 2215
		// (get) Token: 0x060022F1 RID: 8945 RVA: 0x000988F4 File Offset: 0x00096AF4
		public override string StringId
		{
			get
			{
				return "HostileRelationshipTag";
			}
		}

		// Token: 0x060022F2 RID: 8946 RVA: 0x000988FC File Offset: 0x00096AFC
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
			return (num + num2 + num3 < -1 && unmodifiedClanLeaderRelationshipWithPlayer <= -5f) || unmodifiedClanLeaderRelationshipWithPlayer <= -20f;
		}

		// Token: 0x04000A65 RID: 2661
		public const string Id = "HostileRelationshipTag";
	}
}
