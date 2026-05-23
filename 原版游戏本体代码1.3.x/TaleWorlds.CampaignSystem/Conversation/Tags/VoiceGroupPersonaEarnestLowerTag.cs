using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200028F RID: 655
	public class VoiceGroupPersonaEarnestLowerTag : ConversationTag
	{
		// Token: 0x170008DD RID: 2269
		// (get) Token: 0x06002393 RID: 9107 RVA: 0x000994BA File Offset: 0x000976BA
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaEarnestLowerTag";
			}
		}

		// Token: 0x06002394 RID: 9108 RVA: 0x000994C1 File Offset: 0x000976C1
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaEarnest && ConversationTagHelper.UsesLowRegister(character);
		}

		// Token: 0x04000A9C RID: 2716
		public const string Id = "VoiceGroupPersonaEarnestLowerTag";
	}
}
