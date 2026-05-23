using System;
using System.Linq;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200024D RID: 589
	public class PlayerIsSisterTag : ConversationTag
	{
		// Token: 0x1700089B RID: 2203
		// (get) Token: 0x060022CD RID: 8909 RVA: 0x000985CB File Offset: 0x000967CB
		public override string StringId
		{
			get
			{
				return "PlayerIsSisterTag";
			}
		}

		// Token: 0x060022CE RID: 8910 RVA: 0x000985D2 File Offset: 0x000967D2
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Hero.MainHero.IsFemale && character.IsHero && character.HeroObject.Siblings.Contains(Hero.MainHero);
		}

		// Token: 0x04000A59 RID: 2649
		public const string Id = "PlayerIsSisterTag";
	}
}
