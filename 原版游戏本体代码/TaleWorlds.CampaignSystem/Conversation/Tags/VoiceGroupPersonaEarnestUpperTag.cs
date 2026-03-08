using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200028E RID: 654
	public class VoiceGroupPersonaEarnestUpperTag : ConversationTag
	{
		// Token: 0x170008DC RID: 2268
		// (get) Token: 0x06002390 RID: 9104 RVA: 0x00099494 File Offset: 0x00097694
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaEarnestUpperTag";
			}
		}

		// Token: 0x06002391 RID: 9105 RVA: 0x0009949B File Offset: 0x0009769B
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaEarnest && ConversationTagHelper.UsesHighRegister(character);
		}

		// Token: 0x04000A9B RID: 2715
		public const string Id = "VoiceGroupPersonaEarnestUpperTag";
	}
}
