using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200028C RID: 652
	public class PersonaSoftspokenTag : ConversationTag
	{
		// Token: 0x170008DA RID: 2266
		// (get) Token: 0x0600238A RID: 9098 RVA: 0x00099446 File Offset: 0x00097646
		public override string StringId
		{
			get
			{
				return "PersonaSoftspokenTag";
			}
		}

		// Token: 0x0600238B RID: 9099 RVA: 0x0009944D File Offset: 0x0009764D
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.GetPersona() == DefaultTraits.PersonaSoftspoken;
		}

		// Token: 0x04000A99 RID: 2713
		public const string Id = "PersonaSoftspokenTag";
	}
}
