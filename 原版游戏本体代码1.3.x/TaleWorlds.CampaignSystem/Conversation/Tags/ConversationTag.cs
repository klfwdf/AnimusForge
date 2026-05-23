using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000239 RID: 569
	public abstract class ConversationTag
	{
		// Token: 0x17000887 RID: 2183
		// (get) Token: 0x06002290 RID: 8848
		public abstract string StringId { get; }

		// Token: 0x06002291 RID: 8849
		public abstract bool IsApplicableTo(CharacterObject character);

		// Token: 0x06002292 RID: 8850 RVA: 0x00098188 File Offset: 0x00096388
		public override string ToString()
		{
			return this.StringId;
		}
	}
}
