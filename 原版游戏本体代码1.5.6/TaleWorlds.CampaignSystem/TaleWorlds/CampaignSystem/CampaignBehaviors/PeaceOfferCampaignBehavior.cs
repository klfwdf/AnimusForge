using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PeaceOfferCampaignBehavior : CampaignBehaviorBase
{
	private static TextObject PeaceOfferDefaultPanelDescriptionText = new TextObject("{=IB1xsVEr}A courier has arrived from the {MAP_FACTION_NAME}. They offer you a white peace. Your vassals have left the decision with you.");

	private static TextObject PeaceOfferTributePaidPanelDescriptionText = new TextObject("{=JJQ0Hp4m}A courier has arrived from the {MAP_FACTION_NAME}. The {MAP_FACTION_NAME} will pay {GOLD_AMOUNT} {GOLD_ICON} in tribute each day to end the war between your realms. Your vassals have left the decision with you.");

	private static TextObject PeaceOfferTributeWantedPanelDescriptionText = new TextObject("{=Nd0Vhkxn}A courier has arrived from the {MAP_FACTION_NAME}. They offer you peace if you agree to pay a {GOLD_AMOUNT} {GOLD_ICON} daily tribute. Your vassals have left the decision with you.");

	private static TextObject PeaceOfferDefaultPanelPlayerIsVassalDescriptionText = new TextObject("{=gNf0ALKw}A courier has arrived from the {MAP_FACTION_NAME}. They offer you a white peace. Your kingdom will vote whether to accept the offer.");

	private static TextObject PeaceOfferTributePaidPanelPlayerIsVassalDescriptionText = new TextObject("{=SR9FC5jH}A courier has arrived from the {MAP_FACTION_NAME} bearing a peace offer. The {MAP_FACTION_NAME} will pay {GOLD_AMOUNT} {GOLD_ICON} in tribute each day to end the war between your realms. Your kingdom will vote whether to accept the offer.");

	private static TextObject PeaceOfferTributeWantedPanelPlayerIsVassalDescriptionText = new TextObject("{=sbFboHmV}A courier has arrived from the {MAP_FACTION_NAME}. They offer you peace if you agree to pay a {GOLD_AMOUNT} {GOLD_ICON} daily tribute. Your kingdom will vote whether to accept the offer.");

	private IFaction _opponentFaction;

	private int _currentPeaceOfferTributeAmount;

	private int _currentPeaceOfferTributeDuration;

	private int _influenceCostOfDecline;

	private static TextObject PeacePanelTitleText => new TextObject("{=ho5EndaV}Decision");

	private static TextObject PeacePanelOkText => new TextObject("{=oHaWR73d}Ok");

	private static TextObject PeacePanelAffirmativeText => new TextObject("{=Y94H6XnK}Accept");

	private static TextObject PeacePanelNegativeText => new TextObject("{=cOgmdp9e}Decline");

	public override void RegisterEvents()
	{
		CampaignEvents.OnPeaceOfferedToPlayerEvent.AddNonSerializedListener(this, OnPeaceOffered);
		CampaignEvents.OnPeaceOfferResolvedEvent.AddNonSerializedListener(this, OnPeaceOfferResolved);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_currentPeaceOfferTributeAmount", ref _currentPeaceOfferTributeAmount);
		dataStore.SyncData("_opponentFaction", ref _opponentFaction);
	}

	private void OnPeaceOffered(IFaction opponentFaction, int tributeAmount, int tributeDuration)
	{
		if (_opponentFaction == null)
		{
			_opponentFaction = opponentFaction;
			_currentPeaceOfferTributeAmount = tributeAmount;
			_currentPeaceOfferTributeDuration = tributeDuration;
			TextObject textObject = ((tributeAmount <= 0) ? ((tributeAmount >= 0) ? ((Hero.MainHero.MapFaction.Leader == Hero.MainHero) ? PeaceOfferDefaultPanelDescriptionText : PeaceOfferDefaultPanelPlayerIsVassalDescriptionText) : ((Hero.MainHero.MapFaction.Leader == Hero.MainHero) ? PeaceOfferTributeWantedPanelDescriptionText : PeaceOfferTributeWantedPanelPlayerIsVassalDescriptionText)) : ((Hero.MainHero.MapFaction.Leader == Hero.MainHero) ? PeaceOfferTributePaidPanelDescriptionText : PeaceOfferTributePaidPanelPlayerIsVassalDescriptionText));
			textObject.SetTextVariable("MAP_FACTION_NAME", opponentFaction.InformalName);
			textObject.SetTextVariable("GOLD_AMOUNT", MathF.Abs(_currentPeaceOfferTributeAmount));
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			TextObject peacePanelNegativeText = PeacePanelNegativeText;
			_influenceCostOfDecline = 0;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			if (Hero.MainHero.MapFaction.Leader == Hero.MainHero)
			{
				InformationManager.ShowInquiry(new InquiryData(PeacePanelTitleText.ToString(), textObject.ToString(), isAffirmativeOptionShown: true, (float)_influenceCostOfDecline <= 0.1f || Hero.MainHero.Clan.Influence >= (float)_influenceCostOfDecline, PeacePanelAffirmativeText.ToString(), peacePanelNegativeText.ToString(), AcceptPeaceOffer, DeclinePeaceOffer), pauseGameActiveState: true);
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(PeacePanelTitleText.ToString(), textObject.ToString(), isAffirmativeOptionShown: false, isNegativeOptionShown: true, PeacePanelOkText.ToString(), PeacePanelOkText.ToString(), OkPeaceOffer, OkPeaceOffer), pauseGameActiveState: true);
			}
		}
	}

	private void OnPeaceOfferResolved(IFaction opponentFaction)
	{
		if (Hero.MainHero.MapFaction.Leader != Hero.MainHero && opponentFaction != null)
		{
			_opponentFaction = opponentFaction;
			OkPeaceOffer();
		}
	}

	private void OkPeaceOffer()
	{
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			AcceptPeaceOffer();
			return;
		}
		Kingdom kingdom = Clan.PlayerClan.Kingdom;
		KingdomDecision kingdomDecision = kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision s) => s is MakePeaceKingdomDecision makePeaceKingdomDecision && makePeaceKingdomDecision.ProposerClan.MapFaction == Hero.MainHero.MapFaction && makePeaceKingdomDecision.FactionToMakePeaceWith == _opponentFaction);
		if (kingdomDecision != null)
		{
			kingdom.RemoveDecision(kingdomDecision);
		}
		MakePeaceKingdomDecision kingdomDecision2 = new MakePeaceKingdomDecision(Hero.MainHero.MapFaction.Leader.Clan, _opponentFaction, -_currentPeaceOfferTributeAmount, _currentPeaceOfferTributeDuration, applyResults: true, isProposedByOpponent: true);
		((Kingdom)Hero.MainHero.MapFaction).AddDecision(kingdomDecision2, ignoreInfluenceCost: true);
		_opponentFaction = null;
	}

	private void AcceptPeaceOffer()
	{
		MakePeaceAction.ApplyByKingdomDecision(_opponentFaction, Hero.MainHero.MapFaction, _currentPeaceOfferTributeAmount, _currentPeaceOfferTributeDuration);
		_opponentFaction = null;
	}

	private void DeclinePeaceOffer()
	{
		_opponentFaction = null;
		ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -_influenceCostOfDecline);
	}
}
