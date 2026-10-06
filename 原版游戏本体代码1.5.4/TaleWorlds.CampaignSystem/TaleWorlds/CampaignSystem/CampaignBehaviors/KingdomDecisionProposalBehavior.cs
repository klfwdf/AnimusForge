using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class KingdomDecisionProposalBehavior : CampaignBehaviorBase
{
	private delegate KingdomDecision KingdomDecisionCreatorDelegate(Clan sponsorClan);

	private const float DaysBetweenSameProposal = 5f;

	private List<KingdomDecision> _kingdomDecisionsList = new List<KingdomDecision>();

	private ITradeAgreementsCampaignBehavior _tradeAgreementsBehavior;

	private IAllianceCampaignBehavior _allianceCampaignBehavior;

	public ITradeAgreementsCampaignBehavior TradeAgreementsCampaignBehavior
	{
		get
		{
			if (_tradeAgreementsBehavior == null)
			{
				_tradeAgreementsBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			}
			return _tradeAgreementsBehavior;
		}
	}

	private IAllianceCampaignBehavior AllianceCampaignBehavior
	{
		get
		{
			if (_allianceCampaignBehavior == null)
			{
				_allianceCampaignBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
			}
			return _allianceCampaignBehavior;
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, DailyTickClan);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnKingdomDecisionAdded);
	}

	private void OnKingdomDestroyed(Kingdom kingdom)
	{
		UpdateKingdomDecisions(kingdom);
	}

	private void DailyTickClan(Clan clan)
	{
		if ((float)(int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow < 5f || clan.IsEliminated || clan == Clan.PlayerClan || clan.CurrentTotalStrength <= 0f || clan.IsBanditFaction || clan.Kingdom == null || clan.Influence < 100f)
		{
			return;
		}
		KingdomDecision kingdomDecision = null;
		float randomFloat = MBRandom.RandomFloat;
		int num = ((Kingdom)clan.MapFaction).Clans.Count((Clan x) => x.Influence > 100f);
		float num2 = MathF.Min(0.33f, 1f / ((float)num + 2f));
		num2 *= ((clan.Kingdom != Hero.MainHero.MapFaction || Hero.MainHero.Clan.IsUnderMercenaryService) ? 1f : ((clan.Kingdom.Leader == Hero.MainHero) ? 0.5f : 0.75f));
		DiplomacyModel diplomacyModel = Campaign.Current.Models.DiplomacyModel;
		_ = Campaign.Current.Models.AllianceModel;
		if (randomFloat < num2 && clan.Influence > (float)diplomacyModel.GetInfluenceCostOfProposingPeace(clan))
		{
			kingdomDecision = GetRandomPeaceDecision(clan);
		}
		else if (randomFloat < num2 * 2f && clan.Influence > (float)diplomacyModel.GetInfluenceCostOfProposingWar(clan))
		{
			kingdomDecision = GetRandomWarDecision(clan);
		}
		else if (randomFloat < num2 * 2.5f)
		{
			kingdomDecision = ((MBRandom.RandomFloat < 0.5f) ? GetRandomTradeAgreementDecision(clan) : GetRandomStartingAllianceDecision(clan));
		}
		else if (randomFloat < num2 * 2.75f && clan.Influence > (float)(diplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan) * 4))
		{
			kingdomDecision = GetRandomPolicyDecision(clan);
		}
		else if (randomFloat < num2 * 3f && clan.Influence > 700f)
		{
			kingdomDecision = GetRandomAnnexationDecision(clan);
		}
		if (kingdomDecision != null)
		{
			bool flag = false;
			if (kingdomDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision)kingdomDecision).FactionToMakePeaceWith == Hero.MainHero.MapFaction)
			{
				foreach (KingdomDecision kingdomDecisions in _kingdomDecisionsList)
				{
					if (kingdomDecisions is MakePeaceKingdomDecision && kingdomDecisions.Kingdom == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision)kingdomDecisions).FactionToMakePeaceWith == clan.Kingdom && kingdomDecisions.TriggerTime.IsFuture)
					{
						flag = true;
						break;
					}
					if (kingdomDecisions is MakePeaceKingdomDecision && kingdomDecisions.Kingdom == clan.Kingdom && ((MakePeaceKingdomDecision)kingdomDecisions).FactionToMakePeaceWith == Hero.MainHero.MapFaction && kingdomDecisions.TriggerTime.IsFuture)
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				return;
			}
			bool flag2 = false;
			foreach (KingdomDecision kingdomDecisions2 in _kingdomDecisionsList)
			{
				if (kingdomDecisions2 is DeclareWarDecision declareWarDecision && kingdomDecision is DeclareWarDecision declareWarDecision2 && declareWarDecision.FactionToDeclareWarOn == declareWarDecision2.FactionToDeclareWarOn && declareWarDecision.ProposerClan.MapFaction == declareWarDecision2.ProposerClan.MapFaction)
				{
					flag2 = true;
					break;
				}
				if (kingdomDecisions2 is MakePeaceKingdomDecision makePeaceKingdomDecision && kingdomDecision is MakePeaceKingdomDecision makePeaceKingdomDecision2 && makePeaceKingdomDecision.FactionToMakePeaceWith == makePeaceKingdomDecision2.FactionToMakePeaceWith && makePeaceKingdomDecision.ProposerClan.MapFaction == makePeaceKingdomDecision2.ProposerClan.MapFaction)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				clan.Kingdom.AddDecision(kingdomDecision);
			}
		}
		else
		{
			UpdateKingdomDecisions(clan.Kingdom);
		}
	}

	private void HourlyTick()
	{
		if (Clan.PlayerClan.Kingdom != null)
		{
			UpdateKingdomDecisions(Clan.PlayerClan.Kingdom);
		}
	}

	private void DailyTick()
	{
		for (int num = _kingdomDecisionsList.Count - 1; num >= 0; num--)
		{
			if (_kingdomDecisionsList[num].TriggerTime.ElapsedDaysUntilNow > 5f)
			{
				_kingdomDecisionsList.RemoveAt(num);
			}
		}
	}

	public void UpdateKingdomDecisions(Kingdom kingdom)
	{
		List<KingdomDecision> list = new List<KingdomDecision>();
		List<KingdomDecision> list2 = new List<KingdomDecision>();
		foreach (KingdomDecision unresolvedDecision in kingdom.UnresolvedDecisions)
		{
			if (unresolvedDecision.ShouldBeCancelled())
			{
				list.Add(unresolvedDecision);
			}
			else if (!unresolvedDecision.IsPlayerParticipant || (unresolvedDecision.TriggerTime.IsPast && !unresolvedDecision.NeedsPlayerResolution))
			{
				list2.Add(unresolvedDecision);
			}
		}
		foreach (KingdomDecision item in list)
		{
			kingdom.RemoveDecision(item);
			bool isPlayerInvolved = item.DetermineChooser().Leader.IsHumanPlayerCharacter || item.DetermineSupporters().Any((Supporter x) => x.IsPlayer);
			CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(item, isPlayerInvolved);
		}
		foreach (KingdomDecision item2 in list2)
		{
			new KingdomElection(item2).StartElectionWithoutPlayer();
		}
	}

	private void OnPeaceMade(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
	}

	private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
	{
		HandleDiplomaticChangeBetweenFactions(side1Faction, side2Faction);
	}

	private void HandleDiplomaticChangeBetweenFactions(IFaction side1Faction, IFaction side2Faction)
	{
		if (side1Faction.IsKingdomFaction && side2Faction.IsKingdomFaction)
		{
			UpdateKingdomDecisions((Kingdom)side1Faction);
			UpdateKingdomDecisions((Kingdom)side2Faction);
		}
	}

	private KingdomDecision GetRandomStartingAllianceDecision(Clan clan)
	{
		Kingdom kingdom = clan.Kingdom;
		KingdomDecision kingdomDecision = null;
		if (kingdom.UnresolvedDecisions.AnyQ((KingdomDecision x) => x is StartAllianceDecision) || clan.Influence < (float)Campaign.Current.Models.AllianceModel.GetInfluenceCostOfProposingStartingAlliance(clan))
		{
			return null;
		}
		Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => !x.IsEliminated && x != kingdom);
		if (randomElementWithPredicate != null)
		{
			kingdomDecision = new StartAllianceDecision(clan, randomElementWithPredicate);
			if (!kingdomDecision.CanMakeDecision(out var _))
			{
				kingdomDecision = null;
			}
		}
		return kingdomDecision;
	}

	private KingdomDecision GetRandomWarDecision(Clan clan)
	{
		KingdomDecision result = null;
		Kingdom kingdom = clan.Kingdom;
		if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is DeclareWarDecision) != null)
		{
			return null;
		}
		Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => !x.IsEliminated && x != kingdom && !x.IsAtWarWith(kingdom) && x.GetStanceWith(kingdom).PeaceDeclarationDate.ElapsedDaysUntilNow > 20f);
		if (randomElementWithPredicate != null)
		{
			if ((float)new DeclareWarBarterable(kingdom, randomElementWithPredicate).GetValueForFaction(clan) < Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(randomElementWithPredicate))
			{
				return null;
			}
			if (ConsiderWar(clan, kingdom, randomElementWithPredicate))
			{
				result = new DeclareWarDecision(clan, randomElementWithPredicate);
			}
		}
		return result;
	}

	private KingdomDecision GetRandomPeaceDecision(Clan clan)
	{
		KingdomDecision result = null;
		Kingdom kingdom = clan.Kingdom;
		if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is MakePeaceKingdomDecision) != null)
		{
			return null;
		}
		Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(delegate(Kingdom x)
		{
			if (x.IsAtWarWith(kingdom) && !x.IsAtConstantWarWith(kingdom))
			{
				IAllianceCampaignBehavior allianceCampaignBehavior = AllianceCampaignBehavior;
				if (allianceCampaignBehavior == null || !allianceCampaignBehavior.IsAtWarByCallToWarAgreement(kingdom, x, out var callingKingdom))
				{
					IAllianceCampaignBehavior allianceCampaignBehavior2 = AllianceCampaignBehavior;
					if (allianceCampaignBehavior2 == null)
					{
						return true;
					}
					return !allianceCampaignBehavior2.IsAtWarByCallToWarAgreement(x, kingdom, out callingKingdom);
				}
			}
			return false;
		});
		if (randomElementWithPredicate != null && ConsiderPeace(clan, randomElementWithPredicate.RulingClan, randomElementWithPredicate, out var decision))
		{
			result = decision;
		}
		return result;
	}

	private bool ConsiderWar(Clan clan, Kingdom kingdom, IFaction otherFaction)
	{
		int num = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(clan) / 2;
		if (clan.Influence < (float)num)
		{
			return false;
		}
		DeclareWarDecision declareWarDecision = new DeclareWarDecision(clan, otherFaction);
		if (declareWarDecision.CalculateSupport(clan) > 50f)
		{
			KingdomElection kingdomElection = new KingdomElection(declareWarDecision);
			float num2 = 0f;
			foreach (DecisionOutcome possibleOutcome in kingdomElection.PossibleOutcomes)
			{
				if (possibleOutcome is DeclareWarDecision.DeclareWarDecisionOutcome { ShouldWarBeDeclared: not false } declareWarDecisionOutcome)
				{
					num2 = declareWarDecisionOutcome.Likelihood;
					break;
				}
			}
			if (MBRandom.RandomFloat < 1.4f * num2 - 0.55f)
			{
				return true;
			}
		}
		return false;
	}

	private static bool ConsiderPeace(Clan clan, Clan otherClan, IFaction otherFaction, out MakePeaceKingdomDecision decision)
	{
		if (!Campaign.Current.Models.DiplomacyModel.IsPeaceSuitable(clan.MapFaction, otherFaction))
		{
			decision = null;
			return false;
		}
		if (Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(clan.MapFaction, otherFaction) < Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(clan.Kingdom))
		{
			decision = null;
			return false;
		}
		int tributeDurationInDays;
		int dailyTributeToPay = Campaign.Current.Models.DiplomacyModel.GetDailyTributeToPay(clan, otherClan, out tributeDurationInDays);
		if (dailyTributeToPay < 0)
		{
			decision = null;
			return false;
		}
		MakePeaceKingdomDecision makePeaceKingdomDecision = new MakePeaceKingdomDecision(clan, otherFaction, dailyTributeToPay, tributeDurationInDays);
		DecisionOutcome possibleOutcome = makePeaceKingdomDecision.DetermineInitialCandidates().First((DecisionOutcome x) => x is MakePeaceKingdomDecision.MakePeaceDecisionOutcome makePeaceDecisionOutcome && makePeaceDecisionOutcome.ShouldPeaceBeDeclared);
		if (makePeaceKingdomDecision.DetermineSupport(clan, possibleOutcome) <= 0f)
		{
			decision = null;
			return false;
		}
		decision = makePeaceKingdomDecision;
		return true;
	}

	private KingdomDecision GetRandomPolicyDecision(Clan clan)
	{
		KingdomDecision result = null;
		Kingdom kingdom = clan.Kingdom;
		if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is KingdomPolicyDecision) != null)
		{
			return null;
		}
		if (clan.Influence < 200f)
		{
			return null;
		}
		PolicyObject randomElement = PolicyObject.All.GetRandomElement();
		bool flag = kingdom.ActivePolicies.Contains(randomElement);
		if (ConsiderPolicy(clan, kingdom, randomElement, flag))
		{
			result = new KingdomPolicyDecision(clan, randomElement, flag);
		}
		return result;
	}

	private bool ConsiderPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
	{
		int influenceCostOfPolicyProposalAndDisavowal = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
		if (clan.Influence < (float)influenceCostOfPolicyProposalAndDisavowal)
		{
			return false;
		}
		KingdomPolicyDecision kingdomPolicyDecision = new KingdomPolicyDecision(clan, policy, invert);
		if (kingdomPolicyDecision.CalculateSupport(clan) > 50f)
		{
			KingdomElection kingdomElection = new KingdomElection(kingdomPolicyDecision);
			float num = 0f;
			foreach (DecisionOutcome possibleOutcome in kingdomElection.PossibleOutcomes)
			{
				if (possibleOutcome is KingdomPolicyDecision.PolicyDecisionOutcome { ShouldDecisionBeEnforced: not false } policyDecisionOutcome)
				{
					num = policyDecisionOutcome.Likelihood;
					break;
				}
			}
			if ((double)MBRandom.RandomFloat < (double)num - 0.55)
			{
				return true;
			}
		}
		return false;
	}

	private float GetKingdomSupportForPolicy(Clan clan, Kingdom kingdom, PolicyObject policy, bool invert)
	{
		Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(clan);
		return new KingdomElection(new KingdomPolicyDecision(clan, policy, invert)).GetLikelihoodForSponsor(clan);
	}

	private KingdomDecision GetRandomAnnexationDecision(Clan clan)
	{
		KingdomDecision result = null;
		Kingdom kingdom = clan.Kingdom;
		if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is KingdomPolicyDecision) != null)
		{
			return null;
		}
		if (clan.Influence < 300f)
		{
			return null;
		}
		Clan randomElement = kingdom.Clans.GetRandomElement();
		if (randomElement != null && randomElement != clan && randomElement.GetRelationWithClan(clan) < -25)
		{
			if (randomElement.Fiefs.Count == 0)
			{
				return null;
			}
			Town randomElement2 = randomElement.Fiefs.GetRandomElement();
			if (ConsiderAnnex(clan, randomElement2))
			{
				result = new SettlementClaimantPreliminaryDecision(clan, randomElement2.Settlement);
			}
		}
		return result;
	}

	private bool ConsiderAnnex(Clan clan, Town targetSettlement)
	{
		int influenceCostOfAnnexation = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfAnnexation(clan);
		if (clan.Influence < (float)influenceCostOfAnnexation)
		{
			return false;
		}
		SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision = new SettlementClaimantPreliminaryDecision(clan, targetSettlement.Settlement);
		if (settlementClaimantPreliminaryDecision.CalculateSupport(clan) > 50f)
		{
			float num = 0f;
			foreach (DecisionOutcome possibleOutcome in new KingdomElection(settlementClaimantPreliminaryDecision).PossibleOutcomes)
			{
				if (possibleOutcome is SettlementClaimantPreliminaryDecision.SettlementClaimantPreliminaryOutcome { ShouldSettlementOwnerChange: not false } settlementClaimantPreliminaryOutcome)
				{
					num = settlementClaimantPreliminaryOutcome.Likelihood;
					break;
				}
			}
			if ((double)MBRandom.RandomFloat < (double)num - 0.6)
			{
				return true;
			}
		}
		return false;
	}

	private KingdomDecision GetRandomTradeAgreementDecision(Clan clan)
	{
		KingdomDecision result = null;
		Kingdom kingdom = clan.Kingdom;
		if (kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision x) => x is TradeAgreementDecision) != null || clan.Influence < (float)Campaign.Current.Models.TradeAgreementModel.GetInfluenceCostOfProposingTradeAgreement(clan))
		{
			return null;
		}
		Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate((Kingdom x) => x != kingdom);
		if (randomElementWithPredicate != null && ConsiderTradeAgreement(clan, kingdom, randomElementWithPredicate))
		{
			result = new TradeAgreementDecision(clan, randomElementWithPredicate);
		}
		return result;
	}

	private bool ConsiderTradeAgreement(Clan clan, Kingdom kingdom, Kingdom otherKingdom)
	{
		if (Campaign.Current.Models.TradeAgreementModel.CanMakeTradeAgreement(kingdom, otherKingdom, checkOtherSideTradeSupport: true, out var reason))
		{
			TradeAgreementDecision tradeAgreementDecision = new TradeAgreementDecision(clan, otherKingdom);
			if (kingdom != Clan.PlayerClan.Kingdom)
			{
				return tradeAgreementDecision.CalculateSupport(clan, out reason) > 50f;
			}
			KingdomElection kingdomElection = new KingdomElection(tradeAgreementDecision);
			kingdomElection.SetupResultWithoutPlayerSupport();
			DecisionOutcome supportedOutcome = kingdomElection.PossibleOutcomes.FirstOrDefault((DecisionOutcome x) => x is TradeAgreementDecision.TradeAgreementDecisionOutcome tradeAgreementDecisionOutcome && tradeAgreementDecisionOutcome.ShouldTradeAgreementStart);
			return kingdomElection.GetWinChanceWithPlayerSupport(supportedOutcome, Supporter.SupportWeights.FullyPush) > 0.5f;
		}
		return false;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_kingdomDecisionsList", ref _kingdomDecisionsList);
		if (dataStore.IsLoading && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")) && _kingdomDecisionsList == null)
		{
			_kingdomDecisionsList = new List<KingdomDecision>();
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan && oldKingdom != null && detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction)
		{
			UpdateKingdomDecisions(oldKingdom);
		}
	}

	private void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
		_kingdomDecisionsList.Add(decision);
	}
}
