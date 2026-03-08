using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000289 RID: 649
	public class PersonaEarnestTag : ConversationTag
	{
		// Token: 0x170008D7 RID: 2263
		// (get) Token: 0x06002381 RID: 9089 RVA: 0x000993CE File Offset: 0x000975CE
		public override string StringId
		{
			get
			{
				return "PersonaEarnestTag";
			}
		}

		// Token: 0x06002382 RID: 9090 RVA: 0x000993D5 File Offset: 0x000975D5
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.GetPersona() == DefaultTraits.PersonaEarnest;
		}

		// Token: 0x04000A96 RID: 2710
		public const string Id = "PersonaEarnestTag";
	}
}
