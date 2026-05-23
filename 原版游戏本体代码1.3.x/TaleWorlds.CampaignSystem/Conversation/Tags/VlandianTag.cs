using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000277 RID: 631
	public class VlandianTag : ConversationTag
	{
		// Token: 0x170008C5 RID: 2245
		// (get) Token: 0x0600234B RID: 9035 RVA: 0x0009903B File Offset: 0x0009723B
		public override string StringId
		{
			get
			{
				return "VlandianTag";
			}
		}

		// Token: 0x0600234C RID: 9036 RVA: 0x00099042 File Offset: 0x00097242
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.Culture.StringId == "vlandia";
		}

		// Token: 0x04000A84 RID: 2692
		public const string Id = "VlandianTag";
	}
}
