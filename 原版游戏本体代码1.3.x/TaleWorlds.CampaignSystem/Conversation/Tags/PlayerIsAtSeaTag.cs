using System;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000285 RID: 645
	public class PlayerIsAtSeaTag : ConversationTag
	{
		// Token: 0x170008D3 RID: 2259
		// (get) Token: 0x06002375 RID: 9077 RVA: 0x0009929F File Offset: 0x0009749F
		public override string StringId
		{
			get
			{
				return "PlayerIsAtSeaTag";
			}
		}

		// Token: 0x06002376 RID: 9078 RVA: 0x000992A8 File Offset: 0x000974A8
		public override bool IsApplicableTo(CharacterObject character)
		{
			MobileParty mobileParty = (Hero.MainHero.IsPrisoner ? Hero.MainHero.PartyBelongedToAsPrisoner.MobileParty : Hero.MainHero.PartyBelongedTo);
			MobileParty mobileParty2 = (character.HeroObject.IsPrisoner ? character.HeroObject.PartyBelongedToAsPrisoner.MobileParty : character.HeroObject.PartyBelongedTo);
			return mobileParty.IsCurrentlyAtSea && mobileParty2 != mobileParty;
		}

		// Token: 0x04000A92 RID: 2706
		public const string Id = "PlayerIsAtSeaTag";
	}
}
