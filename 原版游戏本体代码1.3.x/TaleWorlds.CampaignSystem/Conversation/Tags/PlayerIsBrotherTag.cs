using System;
using System.Linq;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200024C RID: 588
	public class PlayerIsBrotherTag : ConversationTag
	{
		// Token: 0x1700089A RID: 2202
		// (get) Token: 0x060022CA RID: 8906 RVA: 0x0009858F File Offset: 0x0009678F
		public override string StringId
		{
			get
			{
				return "PlayerIsBrotherTag";
			}
		}

		// Token: 0x060022CB RID: 8907 RVA: 0x00098596 File Offset: 0x00096796
		public override bool IsApplicableTo(CharacterObject character)
		{
			return !Hero.MainHero.IsFemale && character.IsHero && character.HeroObject.Siblings.Contains(Hero.MainHero);
		}

		// Token: 0x04000A58 RID: 2648
		public const string Id = "PlayerIsBrotherTag";
	}
}
