using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200028B RID: 651
	public class PersonaIronicTag : ConversationTag
	{
		// Token: 0x170008D9 RID: 2265
		// (get) Token: 0x06002387 RID: 9095 RVA: 0x0009941E File Offset: 0x0009761E
		public override string StringId
		{
			get
			{
				return "PersonaIronicTag";
			}
		}

		// Token: 0x06002388 RID: 9096 RVA: 0x00099425 File Offset: 0x00097625
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.GetPersona() == DefaultTraits.PersonaIronic;
		}

		// Token: 0x04000A98 RID: 2712
		public const string Id = "PersonaIronicTag";
	}
}
