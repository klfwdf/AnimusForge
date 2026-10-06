using NavalDLC.Storyline;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GameComponents;

public class NavalDLCSettlementAccessModel : SettlementAccessModel
{
	public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId, out bool disableOption, out TextObject disabledText)
	{
		if (locationId.Equals("center"))
		{
			if (NavalStorylineData.IsNavalStoryLineActive())
			{
				disableOption = true;
				disabledText = new TextObject("{=ILnr9eCQ}Door is locked!");
				return false;
			}
		}
		else if (locationId == "port")
		{
			return CanMainHeroEnterPort(settlement, out disabledText, out disableOption);
		}
		return base.BaseModel.CanMainHeroAccessLocation(settlement, locationId, out disableOption, out disabledText);
	}

	public override void CanMainHeroEnterSettlement(Settlement settlement, out AccessDetails accessDetails)
	{
		base.BaseModel.CanMainHeroEnterSettlement(settlement, out accessDetails);
	}

	public override void CanMainHeroEnterLordsHall(Settlement settlement, out AccessDetails accessDetails)
	{
		base.BaseModel.CanMainHeroEnterLordsHall(settlement, out accessDetails);
	}

	public override void CanMainHeroEnterDungeon(Settlement settlement, out AccessDetails accessDetails)
	{
		base.BaseModel.CanMainHeroEnterDungeon(settlement, out accessDetails);
	}

	public override bool CanMainHeroDoSettlementAction(Settlement settlement, SettlementAction settlementAction, out bool disableOption, out TextObject disabledText)
	{
		if (settlement.IsVillage && MobileParty.MainParty.IsCurrentlyAtSea && settlementAction == SettlementAction.WaitInSettlement)
		{
			disableOption = true;
			disabledText = new TextObject("{=qVbAvzJM}You cannot wait in the village while you are at sea.");
			return false;
		}
		return base.BaseModel.CanMainHeroDoSettlementAction(settlement, settlementAction, out disableOption, out disabledText);
	}

	public override bool IsRequestMeetingOptionAvailable(Settlement settlement, out bool disableOption, out TextObject disabledText)
	{
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			disableOption = true;
			disabledText = new TextObject("{=W0YmExzK}You can not request a meeting while you are at sea.");
			return true;
		}
		return base.BaseModel.IsRequestMeetingOptionAvailable(settlement, out disableOption, out disabledText);
	}

	private bool CanMainHeroEnterPort(Settlement settlement, out TextObject disabledText, out bool disableOption)
	{
		bool result = true;
		disabledText = TextObject.GetEmpty();
		disableOption = false;
		if (Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)) && Mission.Current != null)
		{
			result = false;
			disableOption = true;
			disabledText = new TextObject("{=UjERCi2F}This feature is disabled.");
		}
		else if (Campaign.Current.IsMainHeroDisguised)
		{
			if (Mission.Current == null)
			{
				disabledText = new TextObject("{=i1npbbc4}You cannot enter the port while in disguise.");
			}
			else
			{
				disabledText = new TextObject("{=ILnr9eCQ}Door is locked!");
			}
			result = false;
			disableOption = true;
		}
		return result;
	}
}
