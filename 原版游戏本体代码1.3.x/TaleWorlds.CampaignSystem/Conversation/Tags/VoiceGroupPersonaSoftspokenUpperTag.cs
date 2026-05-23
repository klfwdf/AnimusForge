using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000297 RID: 663
	public class VoiceGroupPersonaSoftspokenUpperTag : ConversationTag
	{
		// Token: 0x170008E5 RID: 2277
		// (get) Token: 0x060023AB RID: 9131 RVA: 0x000995EA File Offset: 0x000977EA
		public override string StringId
		{
			get
			{
				return "VoiceGroupPersonaSoftspokenUpperTag";
			}
		}

		// Token: 0x060023AC RID: 9132 RVA: 0x000995F1 File Offset: 0x000977F1
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.GetPersona() == DefaultTraits.PersonaSoftspoken && ConversationTagHelper.UsesHighRegister(character);
		}

		// Token: 0x04000AA4 RID: 2724
		public const string Id = "VoiceGroupPersonaSoftspokenUpperTag";
	}
}
