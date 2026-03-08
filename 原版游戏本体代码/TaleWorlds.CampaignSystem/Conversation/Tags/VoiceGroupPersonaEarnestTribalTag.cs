using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200028D RID: 653
	public class VoiceGroupPersonaEarnestTribalTag : ConversationTag
	{
		// Token: 0x170008DB RID: 2267
		// (get) Token: 0x0600238D RID: 9101 RVA: 0x0009946E File Offset: 0x0009766E
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaEarnestTribalTag";
			}
		}

		// Token: 0x0600238E RID: 9102 RVA: 0x00099475 File Offset: 0x00097675
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaEarnest && ConversationTagHelper.TribalVoiceGroup(character);
		}

		// Token: 0x04000A9A RID: 2714
		public const string Id = "VoiceGroupPersonaEarnestTribalTag";
	}
}
