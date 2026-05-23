using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000241 RID: 577
	public class HighRegisterTag : ConversationTag
	{
		// Token: 0x1700088F RID: 2191
		// (get) Token: 0x060022A9 RID: 8873 RVA: 0x00098376 File Offset: 0x00096576
		public override string StringId
		{
			get
			{
				return "HighRegisterTag";
			}
		}

		// Token: 0x060022AA RID: 8874 RVA: 0x0009837D File Offset: 0x0009657D
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && ConversationTagHelper.UsesHighRegister(character);
		}

		// Token: 0x04000A4D RID: 2637
		public const string Id = "HighRegisterTag";
	}
}
