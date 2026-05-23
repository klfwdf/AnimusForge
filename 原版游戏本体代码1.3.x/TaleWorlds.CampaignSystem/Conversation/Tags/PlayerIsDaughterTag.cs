using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000248 RID: 584
	public class PlayerIsDaughterTag : ConversationTag
	{
		// Token: 0x17000896 RID: 2198
		// (get) Token: 0x060022BE RID: 8894 RVA: 0x0009849B File Offset: 0x0009669B
		public override string StringId
		{
			get
			{
				return "PlayerIsDaughterTag";
			}
		}

		// Token: 0x060022BF RID: 8895 RVA: 0x000984A2 File Offset: 0x000966A2
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && Hero.MainHero.IsFemale && (Hero.MainHero.Father == character.HeroObject || Hero.MainHero.Mother == character.HeroObject);
		}

		// Token: 0x04000A54 RID: 2644
		public const string Id = "PlayerIsDaughterTag";
	}
}
