using System;
using Helpers;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000245 RID: 581
	public class AlliedLordTag : ConversationTag
	{
		// Token: 0x17000893 RID: 2195
		// (get) Token: 0x060022B5 RID: 8885 RVA: 0x00098419 File Offset: 0x00096619
		public override string StringId
		{
			get
			{
				return "PlayerIsAlliedTag";
			}
		}

		// Token: 0x060022B6 RID: 8886 RVA: 0x00098420 File Offset: 0x00096620
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && DiplomacyHelper.IsSameFactionAndNotEliminated(character.HeroObject.MapFaction, Hero.MainHero.MapFaction);
		}

		// Token: 0x04000A51 RID: 2641
		public const string Id = "PlayerIsAlliedTag";
	}
}
