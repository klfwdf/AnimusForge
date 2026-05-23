using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000294 RID: 660
	public class VoiceGroupPersonaIronicUpperTag : ConversationTag
	{
		// Token: 0x170008E2 RID: 2274
		// (get) Token: 0x060023A2 RID: 9122 RVA: 0x00099578 File Offset: 0x00097778
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaIronicUpperTag";
			}
		}

		// Token: 0x060023A3 RID: 9123 RVA: 0x0009957F File Offset: 0x0009777F
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaIronic && ConversationTagHelper.UsesHighRegister(character);
		}

		// Token: 0x04000AA1 RID: 2721
		public const string Id = "VoiceGroupPersonaIronicUpperTag";
	}
}
