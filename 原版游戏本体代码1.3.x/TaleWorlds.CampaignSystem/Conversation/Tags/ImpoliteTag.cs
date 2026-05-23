using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000260 RID: 608
	public class ImpoliteTag : ConversationTag
	{
		// Token: 0x170008AE RID: 2222
		// (get) Token: 0x06002306 RID: 8966 RVA: 0x00098B06 File Offset: 0x00096D06
		public override string StringId
		{
			get
			{
				return "ImpoliteTag";
			}
		}

		// Token: 0x06002307 RID: 8967 RVA: 0x00098B10 File Offset: 0x00096D10
		public override bool IsApplicableTo(CharacterObject character)
		{
			if (!character.IsHero)
			{
				return false;
			}
			int heroRelation = CharacterRelationManager.GetHeroRelation(character.HeroObject, Hero.MainHero);
			return (character.HeroObject.IsLord || character.HeroObject.IsMerchant || character.HeroObject.IsGangLeader) && Clan.PlayerClan.Renown < 100f && heroRelation < 1 && character.GetTraitLevel(DefaultTraits.Mercy) + character.GetTraitLevel(DefaultTraits.Generosity) < 0;
		}

		// Token: 0x04000A6C RID: 2668
		public const string Id = "ImpoliteTag";
	}
}
