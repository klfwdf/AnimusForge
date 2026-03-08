using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000295 RID: 661
	public class VoiceGroupPersonaIronicLowerTag : ConversationTag
	{
		// Token: 0x170008E3 RID: 2275
		// (get) Token: 0x060023A5 RID: 9125 RVA: 0x0009959E File Offset: 0x0009779E
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaIronicLowerTag";
			}
		}

		// Token: 0x060023A6 RID: 9126 RVA: 0x000995A5 File Offset: 0x000977A5
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaIronic && ConversationTagHelper.UsesLowRegister(character);
		}

		// Token: 0x04000AA2 RID: 2722
		public const string Id = "VoiceGroupPersonaIronicLowerTag";
	}
}
