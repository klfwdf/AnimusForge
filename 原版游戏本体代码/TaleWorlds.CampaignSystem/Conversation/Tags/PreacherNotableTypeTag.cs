using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200026C RID: 620
	public class PreacherNotableTypeTag : ConversationTag
	{
		// Token: 0x170008BA RID: 2234
		// (get) Token: 0x0600232A RID: 9002 RVA: 0x00098E46 File Offset: 0x00097046
		public override string StringId
		{
			get
			{
				return "PreacherNotableTypeTag";
			}
		}

		// Token: 0x0600232B RID: 9003 RVA: 0x00098E4D File Offset: 0x0009704D
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.Occupation == Occupation.Preacher;
		}

		// Token: 0x04000A79 RID: 2681
		public const string Id = "PreacherNotableTypeTag";
	}
}
