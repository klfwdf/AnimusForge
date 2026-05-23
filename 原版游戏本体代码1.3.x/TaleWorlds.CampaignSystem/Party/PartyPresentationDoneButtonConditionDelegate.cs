using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party
{
	// Token: 0x020002F6 RID: 758
	// (Invoke) Token: 0x06002BF1 RID: 11249
	public delegate Tuple<bool, TextObject> PartyPresentationDoneButtonConditionDelegate(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum);
}
