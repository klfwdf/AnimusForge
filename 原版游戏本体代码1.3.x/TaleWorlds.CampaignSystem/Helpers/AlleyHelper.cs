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

namespace Helpers
{
	// Token: 0x02000008 RID: 8
	public static class AlleyHelper
	{
		// Token: 0x06000028 RID: 40 RVA: 0x00003C54 File Offset: 0x00001E54
		public static void OpenScreenForManagingAlley(bool isNewAlley, TroopRoster leftMemberRoster, PartyPresentationDoneButtonDelegate onDoneButtonClicked, TextObject leftText, PartyPresentationCancelButtonDelegate onCancelButtonClicked = null)
		{
			PartyScreenHelper.OpenScreenForManagingAlley(isNewAlley, leftMemberRoster, new IsTroopTransferableDelegate(AlleyHelper.IsTroopTransferable), new PartyPresentationDoneButtonConditionDelegate(AlleyHelper.DoneButtonCondition), onDoneButtonClicked, leftText, onCancelButtonClicked);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00003C7C File Offset: 0x00001E7C
		private static Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int lefLimitNum, int rightLimitNum)
		{
			if (leftMemberRoster.TotalRegulars > Campaign.Current.Models.AlleyModel.MaximumTroopCountInPlayerOwnedAlley)
			{
				TextObject textObject = new TextObject("{=5Y4rnaDX}You can not transfer more than {UPPER_LIMIT} troops", null);
				textObject.SetTextVariable("UPPER_LIMIT", Campaign.Current.Models.AlleyModel.MaximumTroopCountInPlayerOwnedAlley);
				return new Tuple<bool, TextObject>(false, textObject);
			}
			if (leftMemberRoster.TotalRegulars < Campaign.Current.Models.AlleyModel.MinimumTroopCountInPlayerOwnedAlley)
			{
				TextObject textObject2 = new TextObject("{=v5HPLGXs}You can not transfer less than {LOWER_LIMIT} troops", null);
				textObject2.SetTextVariable("LOWER_LIMIT", Campaign.Current.Models.AlleyModel.MinimumTroopCountInPlayerOwnedAlley);
				return new Tuple<bool, TextObject>(false, textObject2);
			}
			return new Tuple<bool, TextObject>(true, null);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003D30 File Offset: 0x00001F30
		private static bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
		{
			return !character.IsHero && type != PartyScreenLogic.TroopType.Prisoner;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003D44 File Offset: 0x00001F44
		public static void CreateMultiSelectionInquiryForSelectingClanMemberToAlley(Alley alley, Action<List<InquiryElement>> affirmativeAction, Action<List<InquiryElement>> negativeAction)
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (ValueTuple<Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail> valueTuple in Campaign.Current.Models.AlleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(alley))
			{
				Hero item = valueTuple.Item1;
				DefaultAlleyModel.AlleyMemberAvailabilityDetail item2 = valueTuple.Item2;
				TextObject disabledReasonTextForHero = Campaign.Current.Models.AlleyModel.GetDisabledReasonTextForHero(item, alley, item2);
				list.Add(new InquiryElement(item.CharacterObject, item.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(item.CharacterObject)), item2 == DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available || item2 == DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay, disabledReasonTextForHero.ToString()));
			}
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=FLXzhZCo}Select companion", null).ToString(), new TextObject("{=QGlhXD4F}Select a companion to lead this alley.", null).ToString(), list, true, 1, 1, GameTexts.FindText("str_done", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(), affirmativeAction, negativeAction, "", false), true, false);
		}
	}
}
