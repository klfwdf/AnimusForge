using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200028A RID: 650
	public class PersonaCurtTag : ConversationTag
	{
		// Token: 0x170008D8 RID: 2264
		// (get) Token: 0x06002384 RID: 9092 RVA: 0x000993F6 File Offset: 0x000975F6
		public override string StringId
		{
			get
			{
				return "PersonaCurtTag";
			}
		}

		// Token: 0x06002385 RID: 9093 RVA: 0x000993FD File Offset: 0x000975FD
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.GetPersona() == DefaultTraits.PersonaCurt;
		}

		// Token: 0x04000A97 RID: 2711
		public const string Id = "PersonaCurtTag";
	}
}
