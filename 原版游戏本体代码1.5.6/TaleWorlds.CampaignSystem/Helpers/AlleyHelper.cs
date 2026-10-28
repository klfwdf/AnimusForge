using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace Helpers;

public static class AlleyHelper
{
	public static void OpenScreenForManagingAlley(bool isNewAlley, TroopRoster leftMemberRoster, PartyPresentationDoneButtonDelegate onDoneButtonClicked, TextObject leftText, PartyPresentationCancelButtonDelegate onCancelButtonClicked = null)
	{
		PartyScreenHelper.OpenScreenForManagingAlley(isNewAlley, leftMemberRoster, IsTroopTransferable, DoneButtonCondition, onDoneButtonClicked, leftText, onCancelButtonClicked);
	}

	private static Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int lefLimitNum, int rightLimitNum)
	{
		if (leftMemberRoster.TotalRegulars > Campaign.Current.Models.AlleyModel.MaximumTroopCountInPlayerOwnedAlley)
		{
			TextObject textObject = new TextObject("{=5Y4rnaDX}You can not transfer more than {UPPER_LIMIT} troops");
			textObject.SetTextVariable("UPPER_LIMIT", Campaign.Current.Models.AlleyModel.MaximumTroopCountInPlayerOwnedAlley);
			return new Tuple<bool, TextObject>(item1: false, textObject);
		}
		if (leftMemberRoster.TotalRegulars < Campaign.Current.Models.AlleyModel.MinimumTroopCountInPlayerOwnedAlley)
		{
			TextObject textObject2 = new TextObject("{=v5HPLGXs}You can not transfer less than {LOWER_LIMIT} troops");
			textObject2.SetTextVariable("LOWER_LIMIT", Campaign.Current.Models.AlleyModel.MinimumTroopCountInPlayerOwnedAlley);
			return new Tuple<bool, TextObject>(item1: false, textObject2);
		}
		return new Tuple<bool, TextObject>(item1: true, null);
	}

	private static bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		if (!character.IsHero)
		{
			return type != PartyScreenLogic.TroopType.Prisoner;
		}
		return false;
	}

	public static void CreateMultiSelectionInquiryForSelectingClanMemberToAlley(Alley alley, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction)
	{
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (var item3 in Campaign.Current.Models.AlleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(alley))
		{
			Hero item = item3.Item1;
			DefaultAlleyModel.AlleyMemberAvailabilityDetail item2 = item3.Item2;
			TextObject disabledReasonTextForHero = Campaign.Current.Models.AlleyModel.GetDisabledReasonTextForHero(item, alley, item2);
			list.Add(new InquiryElement(item.CharacterObject, item.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(item.CharacterObject)), item2 == DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available || item2 == DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay, disabledReasonTextForHero.ToString()));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=FLXzhZCo}Select companion").ToString(), new TextObject("{=QGlhXD4F}Select a companion to lead this alley.").ToString(), list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), new TextObject("{=3CpNUnVl}Cancel").ToString(), affirmativeAction, negativeAction), pauseGameActiveState: true);
	}
}
