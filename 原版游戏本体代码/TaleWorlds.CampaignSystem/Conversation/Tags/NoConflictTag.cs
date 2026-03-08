using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200025A RID: 602
	public class NoConflictTag : ConversationTag
	{
		// Token: 0x170008A8 RID: 2216
		// (get) Token: 0x060022F4 RID: 8948 RVA: 0x00098984 File Offset: 0x00096B84
		public override string StringId
		{
			get
			{
				return "NoConflictTag";
			}
		}

		// Token: 0x060022F5 RID: 8949 RVA: 0x0009898C File Offset: 0x00096B8C
		public override bool IsApplicableTo(CharacterObject character)
		{
			bool flag = new HostileRelationshipTag().IsApplicableTo(character);
			bool flag2 = new PlayerIsEnemyTag().IsApplicableTo(character);
			return !flag && !flag2;
		}

		// Token: 0x04000A66 RID: 2662
		public const string Id = "NoConflictTag";
	}
}
