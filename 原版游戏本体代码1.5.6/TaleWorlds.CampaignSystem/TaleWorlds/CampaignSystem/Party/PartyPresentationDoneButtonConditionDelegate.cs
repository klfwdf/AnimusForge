using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party;

public delegate Tuple<bool, TextObject> PartyPresentationDoneButtonConditionDelegate(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum);
