using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000247 RID: 583
	public class PlayerIsSpouseTag : ConversationTag
	{
		// Token: 0x17000895 RID: 2197
		// (get) Token: 0x060022BB RID: 8891 RVA: 0x0009846E File Offset: 0x0009666E
		public override string StringId
		{
			get
			{
				return "PlayerIsSpouseTag";
			}
		}

		// Token: 0x060022BC RID: 8892 RVA: 0x00098475 File Offset: 0x00096675
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && Hero.MainHero.Spouse == character.HeroObject;
		}

		// Token: 0x04000A53 RID: 2643
		public const string Id = "PlayerIsSpouseTag";
	}
}
