using System;
using Helpers;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200025F RID: 607
	public class UnderCommandTag : ConversationTag
	{
		// Token: 0x170008AD RID: 2221
		// (get) Token: 0x06002303 RID: 8963 RVA: 0x00098ACE File Offset: 0x00096CCE
		public override string StringId
		{
			get
			{
				return "UnderCommandTag";
			}
		}

		// Token: 0x06002304 RID: 8964 RVA: 0x00098AD5 File Offset: 0x00096CD5
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.Spouse != Hero.MainHero && HeroHelper.UnderPlayerCommand(character.HeroObject);
		}

		// Token: 0x04000A6B RID: 2667
		public const string Id = "UnderCommandTag";
	}
}
