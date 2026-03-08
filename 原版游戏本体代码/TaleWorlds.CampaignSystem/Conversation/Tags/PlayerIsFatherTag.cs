using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200024A RID: 586
	public class PlayerIsFatherTag : ConversationTag
	{
		// Token: 0x17000898 RID: 2200
		// (get) Token: 0x060022C4 RID: 8900 RVA: 0x00098535 File Offset: 0x00096735
		public override string StringId
		{
			get
			{
				return "PlayerIsFatherTag";
			}
		}

		// Token: 0x060022C5 RID: 8901 RVA: 0x0009853C File Offset: 0x0009673C
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.Father == Hero.MainHero;
		}

		// Token: 0x04000A56 RID: 2646
		public const string Id = "PlayerIsFatherTag";
	}
}
