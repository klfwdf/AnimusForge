using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000013 RID: 19
	public class Give5TroopsToPlayerCheat : GameplayCheatItem
	{
		// Token: 0x06000039 RID: 57 RVA: 0x00003A78 File Offset: 0x00001C78
		public override void ExecuteCheat()
		{
			Settlement settlement = SettlementHelper.FindNearestFortificationToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, null);
			if (Mission.Current == null && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.SiegeEvent == null && Campaign.Current.ConversationManager.OneToOneConversationCharacter == null && settlement != null)
			{
				CultureObject culture = settlement.Culture;
				Clan randomElementWithPredicate = Clan.All.GetRandomElementWithPredicate((Clan x) => x.Culture != null && (culture == null || culture == x.Culture) && !x.IsMinorFaction && !x.IsBanditFaction);
				int num = PartyBase.MainParty.PartySizeLimit - PartyBase.MainParty.NumberOfAllMembers;
				num = MBMath.ClampInt(num, 0, num);
				int num2 = 5;
				num2 = MBMath.ClampInt(num2, 0, num);
				if (randomElementWithPredicate != null && num2 > 0)
				{
					CharacterObject baseTroop = randomElementWithPredicate.Culture.BasicTroop;
					if (MBRandom.RandomFloat < 0.3f && randomElementWithPredicate.Culture.EliteBasicTroop != null)
					{
						baseTroop = randomElementWithPredicate.Culture.EliteBasicTroop;
					}
					CharacterObject randomElementInefficiently = CharacterHelper.GetTroopTree(baseTroop, 1f, float.MaxValue).GetRandomElementInefficiently<CharacterObject>();
					MobileParty.MainParty.AddElementToMemberRoster(randomElementInefficiently, num2, false);
				}
			}
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003B8D File Offset: 0x00001D8D
		public override TextObject GetName()
		{
			return new TextObject("{=9FMvBKrV}Give 5 Troops", null);
		}
	}
}
