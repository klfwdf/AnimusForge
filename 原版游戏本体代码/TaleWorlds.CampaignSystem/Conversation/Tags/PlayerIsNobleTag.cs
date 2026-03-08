using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000251 RID: 593
	public class PlayerIsNobleTag : ConversationTag
	{
		// Token: 0x1700089F RID: 2207
		// (get) Token: 0x060022D9 RID: 8921 RVA: 0x0009873D File Offset: 0x0009693D
		public override string StringId
		{
			get
			{
				return "PlayerIsNobleTag";
			}
		}

		// Token: 0x060022DA RID: 8922 RVA: 0x00098744 File Offset: 0x00096944
		public override bool IsApplicableTo(CharacterObject character)
		{
			return Settlement.All.Any((Settlement x) => x.OwnerClan == Hero.MainHero.Clan);
		}

		// Token: 0x04000A5D RID: 2653
		public const string Id = "PlayerIsNobleTag";
	}
}
