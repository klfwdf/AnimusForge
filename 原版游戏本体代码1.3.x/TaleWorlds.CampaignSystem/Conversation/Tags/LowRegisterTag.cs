using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000242 RID: 578
	public class LowRegisterTag : ConversationTag
	{
		// Token: 0x17000890 RID: 2192
		// (get) Token: 0x060022AC RID: 8876 RVA: 0x00098397 File Offset: 0x00096597
		public override string StringId
		{
			get
			{
				return "LowRegisterTag";
			}
		}

		// Token: 0x060022AD RID: 8877 RVA: 0x0009839E File Offset: 0x0009659E
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && !ConversationTagHelper.UsesHighRegister(character) && ConversationTagHelper.UsesLowRegister(character);
		}

		// Token: 0x04000A4E RID: 2638
		public const string Id = "LowRegisterTag";
	}
}
