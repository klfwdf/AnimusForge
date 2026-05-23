using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class Give5TroopsToPlayerCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		Settlement settlement = SettlementHelper.FindNearestFortificationToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All);
		if (Mission.Current != null || MobileParty.MainParty.MapEvent != null || MobileParty.MainParty.SiegeEvent != null || Campaign.Current.ConversationManager.OneToOneConversationCharacter != null || settlement == null)
		{
			return;
		}
		CultureObject culture = settlement.Culture;
		Clan randomElementWithPredicate = Clan.All.GetRandomElementWithPredicate((Clan x) => x.Culture != null && (culture == null || culture == x.Culture) && !x.IsMinorFaction && !x.IsBanditFaction);
		int num = PartyBase.MainParty.PartySizeLimit - PartyBase.MainParty.NumberOfAllMembers;
		num = MBMath.ClampInt(num, 0, num);
		int value = 5;
		value = MBMath.ClampInt(value, 0, num);
		if (randomElementWithPredicate != null && value > 0)
		{
			CharacterObject baseTroop = randomElementWithPredicate.Culture.BasicTroop;
			if (MBRandom.RandomFloat < 0.3f && randomElementWithPredicate.Culture.EliteBasicTroop != null)
			{
				baseTroop = randomElementWithPredicate.Culture.EliteBasicTroop;
			}
			CharacterObject randomElementInefficiently = CharacterHelper.GetTroopTree(baseTroop, 1f).GetRandomElementInefficiently();
			MobileParty.MainParty.AddElementToMemberRoster(randomElementInefficiently, value);
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=9FMvBKrV}Give 5 Troops");
	}
}
