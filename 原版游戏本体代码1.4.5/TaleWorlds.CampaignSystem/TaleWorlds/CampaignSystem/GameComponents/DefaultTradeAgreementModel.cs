using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultTradeAgreementModel : TradeAgreementModel
{
	private const float MaxExposureEffect = 30f;

	private const float MarriageBetweenRulingClansBonus = 10f;

	private const float MarriageBetweenKingdomsBonus = 5f;

	private const float AlliedScoreBonus = 5f;

	private const float WarWithAllyPenalty = -15f;

	private const float WarWithAnyKingdomPenalty = -1.5f;

	private const float NoWarBonus = 2.5f;

	private const float ProsperityCheckRange = 2500f;

	private static readonly TextObject _kingdomsAtWarText = new TextObject("{=vo7kAlkR}The kingdoms are at war.");

	private static readonly TextObject _eliminatedKingdomText = new TextObject("{=ZeNt57yM}The kingdom is eliminated.");

	private static readonly TextObject _existingTradeAgreementText = new TextObject("{=8HXcla1b}These kingdoms already have a trade agreement.");

	private static readonly TextObject _maximumNumberOfTradeAgreementsText = new TextObject("{=DJ51OJWj}You already have maximum number of trade agreements.");

	private static readonly TextObject _noTownText = new TextObject("{=QQ4bi6Zr}You don't own any towns.");

	private static readonly TextObject _landlockedText = new TextObject("{=Ig8l75Rg}One of the kingdoms is landlocked.");

	private static readonly TextObject _kingdomsNotNeighborsText = new TextObject("{=Bu6YdMme}Kingdoms aren't neighbors.");

	private static readonly TextObject _limitedSharedBordersText = new TextObject("{=EapZFDGF}Limited shared borders");

	private static readonly TextObject _relationsText = new TextObject("{=3YVDMg5X}Low relations between rulers");

	private static readonly TextObject _warWithAlliedText = new TextObject("{=tT91z3AL}Your realm is at war with their ally.");

	private static readonly TextObject _warText = new TextObject("{=FaEOnF8q}Your realm is a participant in a war.");

	private static readonly TextObject _lowSecurityText = new TextObject("{=aTuRql06}Target faction is concerned by security of towns in your realm.");

	private static readonly TextObject _higherQuerierProsperityText = new TextObject("{=ji8oPOXU}Your realm is not open to negotiation as target faction’s prosperity is too low.");

	private static readonly TextObject _higherQueriedProsperityText = new TextObject("{=lYaumdUj}Your realm has lower prosperity then target faction.");

	private static readonly TextObject _recentWarText = new TextObject("{=lDIz0nEY}Recent war");

	private const int MaxReasonsInExplanation = 3;

	private ITradeAgreementsCampaignBehavior _tradeAgreementsBehavior;

	private ITradeAgreementsCampaignBehavior TradeAgreementsCampaignBehavior
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

	public override int GetInfluenceCostOfProposingTradeAgreement(Clan proposerClan)
	{
		return 200;
	}

	public override int GetMaximumTradeAgreementCount(Kingdom kingdom)
	{
		return 2;
	}

	public override bool CanMakeTradeAgreement(Kingdom querierKingdom, Kingdom queriedKingdom, bool checkOtherSideSupport, out TextObject reason, bool includeReason = false)
	{
		reason = (includeReason ? TextObject.GetEmpty() : null);
		if (querierKingdom.IsAtWarWith(queriedKingdom))
		{
			reason = _kingdomsAtWarText;
			return false;
		}
		if (queriedKingdom.IsEliminated)
		{
			reason = _eliminatedKingdomText;
			return false;
		}
		ITradeAgreementsCampaignBehavior tradeAgreementsCampaignBehavior = TradeAgreementsCampaignBehavior;
		if (tradeAgreementsCampaignBehavior != null && tradeAgreementsCampaignBehavior.HasTradeAgreement(querierKingdom, queriedKingdom, out var _))
		{
			reason = _existingTradeAgreementText;
			return false;
		}
		if (Kingdom.All.Count((Kingdom x) => x != querierKingdom && !x.IsEliminated && (TradeAgreementsCampaignBehavior?.HasTradeAgreement(querierKingdom, x, out var _) ?? false)) >= Campaign.Current.Models.TradeAgreementModel.GetMaximumTradeAgreementCount(querierKingdom))
		{
			reason = _maximumNumberOfTradeAgreementsText;
			return false;
		}
		if (Kingdom.All.Count((Kingdom x) => x != queriedKingdom && !x.IsEliminated && (TradeAgreementsCampaignBehavior?.HasTradeAgreement(queriedKingdom, x, out tradeAgreement2) ?? false)) >= Campaign.Current.Models.TradeAgreementModel.GetMaximumTradeAgreementCount(querierKingdom))
		{
			if (includeReason)
			{
				reason = new TextObject("{=O6zpuLGa}{OTHER_KINGDOM} already has maximum number of trade agreements.");
				reason.SetTextVariable("OTHER_KINGDOM", queriedKingdom.Name);
			}
			return false;
		}
		if (querierKingdom.Towns.Count == 0)
		{
			reason = _noTownText;
			return false;
		}
		if (queriedKingdom.Towns.Count == 0)
		{
			if (includeReason)
			{
				reason = new TextObject("{=XkbKOO3v}{OTHER_KINGDOM} does not own any towns.");
				reason.SetTextVariable("OTHER_KINGDOM", queriedKingdom.Name);
			}
			return false;
		}
		if (!querierKingdom.Fiefs.Any((Town x) => x.GetNeighborFortifications(MobileParty.NavigationType.All).Any((Settlement y) => y.MapFaction == queriedKingdom)))
		{
			reason = _kingdomsNotNeighborsText;
			return false;
		}
		if ((querierKingdom.Towns.All((Town x) => x.Settlement.HasPort) && queriedKingdom.Towns.All((Town x) => !x.Settlement.HasPort)) || (querierKingdom.Towns.All((Town x) => !x.Settlement.HasPort) && queriedKingdom.Towns.All((Town x) => x.Settlement.HasPort)))
		{
			reason = _landlockedText;
			return false;
		}
		if (checkOtherSideSupport)
		{
			TradeAgreementDecision tradeAgreementDecision = new TradeAgreementDecision(queriedKingdom.RulingClan, querierKingdom);
			KingdomElection kingdomElection = new KingdomElection(tradeAgreementDecision);
			kingdomElection.SetupResultWithoutPlayerSupport();
			if (queriedKingdom == Clan.PlayerClan.Kingdom)
			{
				DecisionOutcome supportedOutcome = kingdomElection.PossibleOutcomes.FirstOrDefault((DecisionOutcome x) => x is TradeAgreementDecision.TradeAgreementDecisionOutcome tradeAgreementDecisionOutcome && tradeAgreementDecisionOutcome.ShouldTradeAgreementStart);
				return kingdomElection.GetWinChanceWithPlayerSupport(supportedOutcome, Supporter.SupportWeights.FullyPush) > 0.5f;
			}
			if (kingdomElection.GetWinChanceForSponsor(queriedKingdom.RulingClan) < 0.5f)
			{
				tradeAgreementDecision.CalculateSupport(queriedKingdom.RulingClan, out reason);
				return false;
			}
		}
		return true;
	}

	public override float GetScoreOfStartingTradeAgreement(Kingdom querierKingdom, Kingdom queriedKingdom, Clan clan, out TextObject detailedBreakdownTooltip, bool includeExplanation = false)
	{
		detailedBreakdownTooltip = null;
		float securityEffect = GetSecurityEffect(queriedKingdom);
		float relationEffectBetweenRulers = GetRelationEffectBetweenRulers(querierKingdom, queriedKingdom);
		float allianceEffect = GetAllianceEffect(querierKingdom, queriedKingdom);
		bool isAtWarWithAlliedKingdom;
		bool isAtWarWithAnyKingdom;
		float diplomacyEffect = GetDiplomacyEffect(querierKingdom, queriedKingdom, out isAtWarWithAlliedKingdom, out isAtWarWithAnyKingdom);
		float marriageEffect = GetMarriageEffect(querierKingdom, queriedKingdom);
		float querierKingdomAverageProsperity;
		float queriedKingdomAverageProsperity;
		float prosperityEffect = GetProsperityEffect(querierKingdom, queriedKingdom, out querierKingdomAverageProsperity, out queriedKingdomAverageProsperity);
		float exposureEffect = GetExposureEffect(querierKingdom, queriedKingdom);
		float num = 0f;
		if (querierKingdom == Clan.PlayerClan.Kingdom && !Clan.PlayerClan.IsUnderMercenaryService)
		{
			num += GetSubjectiveEffect(queriedKingdom, clan);
		}
		float value = securityEffect + prosperityEffect + relationEffectBetweenRulers + allianceEffect + diplomacyEffect + exposureEffect + num + marriageEffect;
		if (includeExplanation)
		{
			List<(float, TextObject)> list = new List<(float, TextObject)>();
			if (securityEffect < 0f)
			{
				list.Add((TaleWorlds.Library.MathF.Abs(securityEffect), _lowSecurityText));
			}
			if (relationEffectBetweenRulers < 0f)
			{
				list.Add(((float)Campaign.Current.Models.DiplomacyModel.MaxRelationLimit * 0.1f - relationEffectBetweenRulers, _relationsText));
			}
			if (isAtWarWithAlliedKingdom)
			{
				list.Add((TaleWorlds.Library.MathF.Abs(-15f), _warWithAlliedText));
			}
			else if (isAtWarWithAnyKingdom)
			{
				list.Add((TaleWorlds.Library.MathF.Abs(-1.5f), _warText));
			}
			if (list.Sum(((float, TextObject) x) => x.Item1).ApproximatelyEqualsTo(0f))
			{
				if (querierKingdomAverageProsperity > queriedKingdomAverageProsperity)
				{
					list.Add((1f, _higherQuerierProsperityText));
				}
				else
				{
					list.Add((1f, _higherQueriedProsperityText));
				}
				if (exposureEffect < 30f)
				{
					list.Add((1f, _limitedSharedBordersText));
				}
			}
			list = list.OrderByDescending(((float, TextObject) x) => x.Item1).ToList();
			List<TextObject> list2 = new List<TextObject>();
			foreach (var item in list)
			{
				list2.Add(item.Item2);
				if (list2.Count >= 3)
				{
					break;
				}
			}
			TextObject variable = GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list2, new TextObject("{=!}{newline}"));
			detailedBreakdownTooltip = new TextObject("{=jXcb9oHi}{KINGDOM} is not considering a trade agreement with your realm due to:{newline}{newline}{REASONS_BY_LINE}");
			detailedBreakdownTooltip.SetTextVariable("KINGDOM", querierKingdom.Name);
			detailedBreakdownTooltip.SetTextVariable("REASONS_BY_LINE", variable);
		}
		return MBMath.ClampFloat(value, 0f, 100f);
	}

	private float GetMarriageEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		if (IsThereMarriageBetweenClans(queriedKingdom.RulingClan, querierKingdom.RulingClan))
		{
			return 10f;
		}
		if (queriedKingdom.Clans.AnyQ((Clan clan) => clan != queriedKingdom.RulingClan && IsThereMarriageBetweenClans(querierKingdom.RulingClan, clan)))
		{
			return 5f;
		}
		return 0f;
	}

	private bool IsThereMarriageBetweenClans(Clan clan1, Clan clan2)
	{
		if (!clan1.AliveLords.Any((Hero x) => x.OriginClan == clan2))
		{
			return clan2.AliveLords.Any((Hero x) => x.OriginClan == clan1);
		}
		return true;
	}

	private float GetSubjectiveEffect(Kingdom queriedKingdom, Clan clan)
	{
		return (float)clan.Leader.GetRelation(queriedKingdom.Leader) * 0.25f + clan.Leader.RandomFloatWithSeed((uint)CampaignTime.Now.ToWeeks, -10f, 10f);
	}

	private float GetAllianceEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		if (queriedKingdom.IsAllyWith(querierKingdom))
		{
			return 5f;
		}
		return 0f;
	}

	private float GetDiplomacyEffect(Kingdom querierKingdom, Kingdom queriedKingdom, out bool isAtWarWithAlliedKingdom, out bool isAtWarWithAnyKingdom)
	{
		isAtWarWithAlliedKingdom = querierKingdom.AlliedKingdoms.Any((Kingdom x) => x.IsAtWarWith(queriedKingdom));
		isAtWarWithAnyKingdom = isAtWarWithAlliedKingdom || Kingdom.All.Any((Kingdom x) => !x.IsEliminated && x != queriedKingdom && x != querierKingdom && x.IsAtWarWith(queriedKingdom));
		return 2.5f;
	}

	private float GetRelationEffectBetweenRulers(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		return (float)querierKingdom.Leader.GetRelation(queriedKingdom.Leader) * 0.1f;
	}

	private float GetProsperityEffect(Kingdom querierKingdom, Kingdom queriedKingdom, out float querierKingdomAverageProsperity, out float queriedKingdomAverageProsperity)
	{
		querierKingdomAverageProsperity = GetAverageProsperityOfTownsInKingdom(querierKingdom);
		queriedKingdomAverageProsperity = GetAverageProsperityOfTownsInKingdom(queriedKingdom);
		return (2500f - TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Abs(queriedKingdomAverageProsperity - querierKingdomAverageProsperity), 0.1f, 2500f)) / 2500f * 45f;
	}

	private float GetAverageProsperityOfTownsInKingdom(Kingdom kingdom)
	{
		if (kingdom.Towns.Count > 0)
		{
			float num = 0f;
			for (int i = 0; i < kingdom.Towns.Count; i++)
			{
				num += kingdom.Towns[i].Prosperity;
			}
			return num / (float)kingdom.Towns.Count;
		}
		return 0f;
	}

	private float GetSecurityEffect(Kingdom queriedKingdom)
	{
		if (queriedKingdom.Towns.Count > 0)
		{
			float num = 0f;
			for (int i = 0; i < queriedKingdom.Towns.Count; i++)
			{
				num += queriedKingdom.Towns[i].Security;
			}
			return TaleWorlds.Library.MathF.Clamp((num / (float)queriedKingdom.Towns.Count - 85f) * 0.4f, -5f, 0f);
		}
		return 0f;
	}

	public override CampaignTime GetTradeAgreementDurationInYears(Kingdom iniatatingKingdom, Kingdom otherKingdom)
	{
		return CampaignTime.Years(1f);
	}

	private float GetExposureEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		float result = 0f;
		if (queriedKingdom.Fiefs.Count > 0 && querierKingdom.Fiefs.Count > 0)
		{
			HashSet<Settlement> hashSet = new HashSet<Settlement>();
			int num = 0;
			int num2 = 0;
			foreach (Town fief in querierKingdom.Fiefs)
			{
				foreach (Settlement neighborFortification in fief.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (neighborFortification.IsFortification && !hashSet.Contains(neighborFortification) && neighborFortification.MapFaction != querierKingdom)
					{
						if (neighborFortification.MapFaction == queriedKingdom)
						{
							num2++;
						}
						num++;
						hashSet.Add(neighborFortification);
					}
				}
			}
			result = Math.Min((float)num2 / (float)num * 50f, 30f);
		}
		return result;
	}

	public override int GetProfitPerCaravanVisit(MobileParty mobileParty)
	{
		return 500;
	}
}
