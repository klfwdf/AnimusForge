using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000291 RID: 657
	public class VoiceGroupPersonaCurtUpperTag : ConversationTag
	{
		// Token: 0x170008DF RID: 2271
		// (get) Token: 0x06002399 RID: 9113 RVA: 0x00099506 File Offset: 0x00097706
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaCurtUpperTag";
			}
		}

		// Token: 0x0600239A RID: 9114 RVA: 0x0009950D File Offset: 0x0009770D
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaCurt && ConversationTagHelper.UsesHighRegister(character);
		}

		// Token: 0x04000A9E RID: 2718
		public const string Id = "VoiceGroupPersonaCurtUpperTag";
	}
}
