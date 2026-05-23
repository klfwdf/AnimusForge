using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000265 RID: 613
	public class EngagedToPlayerTag : ConversationTag
	{
		// Token: 0x170008B3 RID: 2227
		// (get) Token: 0x06002315 RID: 8981 RVA: 0x00098C95 File Offset: 0x00096E95
		public override string StringId
		{
			get
			{
				return "EngagedToPlayerTag";
			}
		}

		// Token: 0x06002316 RID: 8982 RVA: 0x00098C9C File Offset: 0x00096E9C
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && Romance.GetRomanticLevel(character.HeroObject, Hero.MainHero) == Romance.RomanceLevelEnum.CoupleAgreedOnMarriage;
		}

		// Token: 0x04000A72 RID: 2674
		public const string Id = "EngagedToPlayerTag";
	}
}
