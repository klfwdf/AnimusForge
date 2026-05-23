using System;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x0200023E RID: 574
	public class WaryTag : ConversationTag
	{
		// Token: 0x1700088C RID: 2188
		// (get) Token: 0x060022A0 RID: 8864 RVA: 0x0009821F File Offset: 0x0009641F
		public override string StringId
		{
			get
			{
				return "WaryTag";
			}
		}

		// Token: 0x060022A1 RID: 8865 RVA: 0x00098228 File Offset: 0x00096428
		public override bool IsApplicableTo(CharacterObject character)
		{
			return character.IsHero && character.HeroObject.MapFaction != Hero.MainHero.MapFaction && (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement.SiegeEvent != null) && (Campaign.Current.ConversationManager.CurrentConversationIsFirst || FactionManager.IsAtWarAgainstFaction(character.HeroObject.MapFaction, Hero.MainHero.MapFaction));
		}

		// Token: 0x04000A4A RID: 2634
		public const string Id = "WaryTag";
	}
}
