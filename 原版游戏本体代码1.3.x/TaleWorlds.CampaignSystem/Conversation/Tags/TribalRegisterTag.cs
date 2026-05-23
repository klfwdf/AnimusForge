using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000243 RID: 579
	public class TribalRegisterTag : ConversationTag
	{
		// Token: 0x17000891 RID: 2193
		// (get) Token: 0x060022AF RID: 8879 RVA: 0x000983C0 File Offset: 0x000965C0
		public override string StringId
		{
			get
			{
				return "TribalRegisterTag";
			}
		}

		// Token: 0x060022B0 RID: 8880 RVA: 0x000983C7 File Offset: 0x000965C7
		public override bool IsApplicableTo(CharacterObject character)
		{
			return !ConversationTagHelper.UsesHighRegister(character) && !ConversationTagHelper.UsesLowRegister(character);
		}

		// Token: 0x04000A4F RID: 2639
		public const string Id = "TribalRegisterTag";
	}
}
