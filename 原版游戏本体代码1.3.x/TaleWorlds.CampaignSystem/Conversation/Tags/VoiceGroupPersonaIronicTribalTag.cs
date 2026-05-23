using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000293 RID: 659
	public class VoiceGroupPersonaIronicTribalTag : ConversationTag
	{
		// Token: 0x170008E1 RID: 2273
		// (get) Token: 0x0600239F RID: 9119 RVA: 0x00099552 File Offset: 0x00097752
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaIronicTribalTag";
			}
		}

		// Token: 0x060023A0 RID: 9120 RVA: 0x00099559 File Offset: 0x00097759
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaIronic && ConversationTagHelper.TribalVoiceGroup(character);
		}

		// Token: 0x04000AA0 RID: 2720
		public const string Id = "VoiceGroupPersonaIronicTribalTag";
	}
}
