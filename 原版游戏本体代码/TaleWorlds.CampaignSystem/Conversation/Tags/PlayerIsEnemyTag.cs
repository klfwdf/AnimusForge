using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000244 RID: 580
	public class PlayerIsEnemyTag : ConversationTag
	{
		// Token: 0x17000892 RID: 2194
		// (get) Token: 0x060022B2 RID: 8882 RVA: 0x000983E4 File Offset: 0x000965E4
		public override string StringId
		{
			get
			{
				return "PlayerIsEnemyTag";
			}
		}

		// Token: 0x060022B3 RID: 8883 RVA: 0x000983EB File Offset: 0x000965EB
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && FactionManager.IsAtWarAgainstFaction(character.HeroObject.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x04000A50 RID: 2640
		public const string Id = "PlayerIsEnemyTag";
	}
}
