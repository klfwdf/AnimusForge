using System;
using System.Linq;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200024E RID: 590
	public class PlayerIsKinTag : ConversationTag
	{
		// Token: 0x1700089C RID: 2204
		// (get) Token: 0x060022D0 RID: 8912 RVA: 0x00098607 File Offset: 0x00096807
		public override string StringId
		{
			get
			{
				return "PlayerIsKinTag";
			}
		}

		// Token: 0x060022D1 RID: 8913 RVA: 0x00098610 File Offset: 0x00096810
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && (character.HeroObject.Siblings.Contains(Hero.MainHero) || character.HeroObject.Mother == Hero.MainHero || character.HeroObject.Father == Hero.MainHero || character.HeroObject.Spouse == Hero.MainHero);
		}

		// Token: 0x04000A5A RID: 2650
		public const string Id = "PlayerIsKinTag";
	}
}
