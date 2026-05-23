using System;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200026E RID: 622
	public class GangLeaderNotableTypeTag : ConversationTag
	{
		// Token: 0x170008BC RID: 2236
		// (get) Token: 0x06002330 RID: 9008 RVA: 0x00098E90 File Offset: 0x00097090
		public override string StringId
		{
			get
			{
				return "GangLeaderNotableTypeTag";
			}
		}

		// Token: 0x06002331 RID: 9009 RVA: 0x00098E97 File Offset: 0x00097097
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.Occupation == Occupation.GangLeader;
		}

		// Token: 0x04000A7B RID: 2683
		public const string Id = "GangLeaderNotableTypeTag";
	}
}
