using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000249 RID: 585
	public class PlayerIsSonTag : ConversationTag
	{
		// Token: 0x17000897 RID: 2199
		// (get) Token: 0x060022C1 RID: 8897 RVA: 0x000984E8 File Offset: 0x000966E8
		public override string StringId
		{
			get
			{
				return "PlayerIsSonTag";
			}
		}

		// Token: 0x060022C2 RID: 8898 RVA: 0x000984EF File Offset: 0x000966EF
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && !Hero.MainHero.IsFemale && (Hero.MainHero.Father == character.HeroObject || Hero.MainHero.Mother == character.HeroObject);
		}

		// Token: 0x04000A55 RID: 2645
		public const string Id = "PlayerIsSonTag";
	}
}
