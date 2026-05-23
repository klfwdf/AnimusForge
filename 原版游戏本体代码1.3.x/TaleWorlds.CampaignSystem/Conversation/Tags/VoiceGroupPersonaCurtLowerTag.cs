using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000292 RID: 658
	public class VoiceGroupPersonaCurtLowerTag : ConversationTag
	{
		// Token: 0x170008E0 RID: 2272
		// (get) Token: 0x0600239C RID: 9116 RVA: 0x0009952C File Offset: 0x0009772C
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaCurtLowerTag";
			}
		}

		// Token: 0x0600239D RID: 9117 RVA: 0x00099533 File Offset: 0x00097733
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaCurt && ConversationTagHelper.UsesLowRegister(character);
		}

		// Token: 0x04000A9F RID: 2719
		public const string Id = "VoiceGroupPersonaCurtLowerTag";
	}
}
