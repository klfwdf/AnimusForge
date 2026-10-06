using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class HealPlayerPartyCheat : GameplayCheatItem
{
	public override void ExecuteCheat()
	{
		if (Mission.Current != null || MobileParty.MainParty.MapEvent != null || MobileParty.MainParty.SiegeEvent != null || Campaign.Current.ConversationManager.OneToOneConversationCharacter != null)
		{
			return;
		}
		for (int i = 0; i < PartyBase.MainParty.MemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = PartyBase.MainParty.MemberRoster.GetElementCopyAtIndex(i);
			if (elementCopyAtIndex.Character.IsHero)
			{
				elementCopyAtIndex.Character.HeroObject.Heal(elementCopyAtIndex.Character.HeroObject.MaxHitPoints);
			}
			else
			{
				MobileParty.MainParty.Party.AddToMemberRosterElementAtIndex(i, 0, -PartyBase.MainParty.MemberRoster.GetElementWoundedNumber(i));
			}
		}
	}

	public override TextObject GetName()
	{
		return new TextObject("{=HidEvGr4}Heal Player Party");
	}
}
