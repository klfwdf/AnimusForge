using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000298 RID: 664
	public class VoiceGroupPersonaSoftspokenLowerTag : ConversationTag
	{
		// Token: 0x170008E6 RID: 2278
		// (get) Token: 0x060023AE RID: 9134 RVA: 0x00099610 File Offset: 0x00097810
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaSoftspokenLowerTag";
			}
		}

		// Token: 0x060023AF RID: 9135 RVA: 0x00099617 File Offset: 0x00097817
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaSoftspoken && ConversationTagHelper.UsesLowRegister(character);
		}

		// Token: 0x04000AA5 RID: 2725
		public const string Id = "VoiceGroupPersonaSoftspokenLowerTag";
	}
}
