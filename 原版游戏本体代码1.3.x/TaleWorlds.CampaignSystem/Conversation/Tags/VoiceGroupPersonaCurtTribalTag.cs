using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000290 RID: 656
	public class VoiceGroupPersonaCurtTribalTag : ConversationTag
	{
		// Token: 0x170008DE RID: 2270
		// (get) Token: 0x06002396 RID: 9110 RVA: 0x000994E0 File Offset: 0x000976E0
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaCurtTribalTag";
			}
		}

		// Token: 0x06002397 RID: 9111 RVA: 0x000994E7 File Offset: 0x000976E7
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaCurt && ConversationTagHelper.TribalVoiceGroup(character);
		}

		// Token: 0x04000A9D RID: 2717
		public const string Id = "VoiceGroupPersonaCurtTribalTag";
	}
}
