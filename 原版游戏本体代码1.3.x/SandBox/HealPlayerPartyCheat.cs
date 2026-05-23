using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000016 RID: 22
	public class HealPlayerPartyCheat : GameplayCheatItem
	{
		// Token: 0x06000042 RID: 66 RVA: 0x00003C90 File Offset: 0x00001E90
		public override void ExecuteCheat()
		{
			if (Mission.Current == null && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.SiegeEvent == null && Campaign.Current.ConversationManager.OneToOneConversationCharacter == null)
			{
				for (int i = 0; i < PartyBase.MainParty.MemberRoster.Count; i++)
				{
					TroopRosterElement elementCopyAtIndex = PartyBase.MainParty.MemberRoster.GetElementCopyAtIndex(i);
					if (elementCopyAtIndex.Character.IsHero)
					{
						elementCopyAtIndex.Character.HeroObject.Heal(elementCopyAtIndex.Character.HeroObject.MaxHitPoints, false);
					}
					else
					{
						MobileParty.MainParty.Party.AddToMemberRosterElementAtIndex(i, 0, -PartyBase.MainParty.MemberRoster.GetElementWoundedNumber(i));
					}
				}
			}
		}

		// Token: 0x06000043 RID: 67 RVA: 0x00003D53 File Offset: 0x00001F53
		public override TextObject GetName()
		{
			return new TextObject("{=HidEvGr4}Heal Player Party", null);
		}
	}
}
