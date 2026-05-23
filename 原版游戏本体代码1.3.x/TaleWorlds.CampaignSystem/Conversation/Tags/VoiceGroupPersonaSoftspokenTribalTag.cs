using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000296 RID: 662
	public class VoiceGroupPersonaSoftspokenTribalTag : ConversationTag
	{
		// Token: 0x170008E4 RID: 2276
		// (get) Token: 0x060023A8 RID: 9128 RVA: 0x000995C4 File Offset: 0x000977C4
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaSoftspokenTribalTag";
			}
		}

		// Token: 0x060023A9 RID: 9129 RVA: 0x000995CB File Offset: 0x000977CB
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaSoftspoken && ConversationTagHelper.TribalVoiceGroup(character);
		}

		// Token: 0x04000AA3 RID: 2723
		public const string Id = "VoiceGroupPersonaSoftspokenTribalTag";
	}
}
