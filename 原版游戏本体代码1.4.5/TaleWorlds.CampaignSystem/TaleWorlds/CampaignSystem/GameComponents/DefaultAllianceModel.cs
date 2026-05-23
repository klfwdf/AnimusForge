using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultAllianceModel : AllianceModel
{
	private const int ThresholdForCallToWarWallet = 100000;

	private const float FirstDegreeNeighborScore = 1f;

	private const int ThreatScoreCoefficient = 130;

	private const float AllianceScoreNormalizationFactor = 0.08f;

	private const float MarriageEffect = 50f;

	private const float AtWarEffect = -5f;

	private const float AtPeaceEffect = 25f;

	private const float AtWarWithAllyEffect = -100f;

	private const float AtWarWithEnemyEffect = 100f;

	private const float HonorableRulerEffect = 50f;

	private const float DishonorableRulerEffect = -50f;

	private const float TradeAgreementEffect = 50f;

	private const float CommonThreatEffect = 100f;

	private const float ThresholdForThreatScoreForQuerier = 430f;

	private const float ThresholdForThreatScoreForQueried = 430f;

	private const float SecondAlliancePenaltyForAllianceScore = -600f;

	private const float WarDeclarationScorePenaltyAgainstAllies = 0.5f;

	private const float WarDeclarationScoreBonusAgainstEnemiesOfAllies = 0.3f;

	private const float WarDeclarationScoreBonusAgainstBiggestThreat = 0.5f;

	private const float PeaceDeclarationScorePenaltyAgainstEnemiesOfAllies = 0.7f;

	private const float AllianceScoreThreshold = 50f;

	private readonly TextObject _relationshipText = new TextObject("{=3YVDMg5X}Low relations between rulers.");

	private readonly TextObject _kingdomsNotNeighborsText = new TextObject("{=*}Kingdoms aren't neighbors.");

	private readonly TextObject _kingdomsNotSeekingAllianceText = new TextObject("{=*}{KINGDOM_NAME} is not seeking alliances at the moment.");

	private readonly TextObject _atWarWithAllyText = new TextObject("{=*}Your realm is at war with their ally.");

	private readonly TextObject _sameCultureFiefsText = new TextObject("{=*}Your realm is occupying fiefs belonging to their culture.");

	private readonly TextObject _atWarText = new TextObject("{=*}Your realm is participating in a war.");

	private readonly TextObject _lowHonorText = new TextObject("{=*}{RULER.NAME} has low honor.");

	private readonly TextObject _kingdomNotConsederingAllianceText = new TextObject("{=*}{KINGDOM} is not considering an alliance with your realm due to:{newline}{newline}{REASONS_BY_LINE}");

	private readonly TextObject _allianceNotFormedExplanationText = new TextObject("{=*}An alliance cannot be formed due to:{newline}{newline}{REASON}");

	private readonly TextObject _tooManyAlliancePlayerPenaltyText = new TextObject("{=*}Number of alliances your realm's already in: {NUMBER_OF_ALLIES}/{MAX_NUMBER_OF_ALLIES}");

	private readonly TextObject _tooManyAllianceAIPenaltyText = new TextObject("{=*}Number of alliances {KINGDOM_NAME} is already in: {NUMBER_OF_ALLIES}/{MAX_NUMBER_OF_ALLIES}");

	private readonly TextObject _allianceScoreNotEnoughText = new TextObject("{=*}{KINGDOM_NAME} currently does not consider your realm to be a possible ally.");

	private readonly TextObject _threatEffect = new TextObject("{=!}Threat Effect");

	private readonly TextObject _marriageEffect = new TextObject("{=!}Marriage Effect");

	private readonly TextObject _atWarWithEnemyEffect = new TextObject("{=!}At war with enemy Effect");

	private readonly TextObject _tradeAgreementEffect = new TextObject("{=!}Trade Agreement Effect");

	private readonly TextObject _commonThreatEffect = new TextObject("{=!}Common Threat Effect");

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

	public override CampaignTime MaxDurationOfAlliance => CampaignTime.Days(84f);

	public override CampaignTime MaxDurationOfWarParticipation => CampaignTime.Days(42f);

	public override int MaxNumberOfAlliances => 2;

	public override CampaignTime DurationForOffers => CampaignTime.Hours(24f);

	public override int GetCallToWarCost(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		int callToWarCostForCalledKingdom = GetCallToWarCostForCalledKingdom(calledKingdom, kingdomToCallToWarAgainst);
		int callToWarBudgetOfCallingKingdom = GetCallToWarBudgetOfCallingKingdom(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		if (callingKingdom == Clan.PlayerClan.Kingdom && callToWarBudgetOfCallingKingdom < 0)
		{
			return callToWarCostForCalledKingdom;
		}
		return MathF.Min((int)((double)callToWarCostForCalledKingdom * 1.5), (callToWarCostForCalledKingdom + callToWarBudgetOfCallingKingdom) / 2);
	}

	public override ExplainedNumber GetScoreOfStartingAlliance(Kingdom querierKingdom, Kingdom queriedKingdom, out TextObject explanationText, bool includeDescription = false)
	{
		explanationText = _allianceNotFormedExplanationText;
		ExplainedNumber result = new ExplainedNumber(0f, includeDescription);
		List<(float, TextObject)> list = new List<(float, TextObject)>();
		if (AreKingdomsNeighbors(querierKingdom, queriedKingdom))
		{
			var (kingdom, num) = GetThreateningNeighbor(querierKingdom, out var exposureScore, out var powerRatio);
			if (kingdom != null && num > 430f && kingdom != queriedKingdom)
			{
				var (threateningKingdomForQueried, num2) = GetThreateningNeighbor(queriedKingdom, out powerRatio, out exposureScore);
				if (num2 > 430f)
				{
					float alliancePenalty = GetAlliancePenalty(querierKingdom);
					TextObject alliancePenaltyText = GetAlliancePenaltyText(querierKingdom, includeDescription);
					result.Add(alliancePenalty, alliancePenaltyText);
					float alliancePenalty2 = GetAlliancePenalty(queriedKingdom);
					TextObject alliancePenaltyText2 = GetAlliancePenaltyText(queriedKingdom, includeDescription);
					result.Add(alliancePenalty2, alliancePenaltyText2);
					float threatEffect = GetThreatEffect(num2, num);
					result.Add(threatEffect, _threatEffect);
					float relationshipEffect = GetRelationshipEffect(querierKingdom, queriedKingdom);
					result.Add(relationshipEffect, _relationshipText);
					float marriageEffect = GetMarriageEffect(querierKingdom, queriedKingdom);
					result.Add(marriageEffect, _marriageEffect);
					float atWarWithAllyEffect = GetAtWarWithAllyEffect(querierKingdom, queriedKingdom);
					result.Add(atWarWithAllyEffect, _atWarWithAllyText);
					float atWarWithEnemyEffect = GetAtWarWithEnemyEffect(querierKingdom, queriedKingdom);
					result.Add(atWarWithEnemyEffect, _atWarWithEnemyEffect);
					float atWarOrPeaceEffect = GetAtWarOrPeaceEffect(queriedKingdom);
					result.Add(atWarOrPeaceEffect, _atWarText);
					float fiefWithSameCultureEffect = GetFiefWithSameCultureEffect(querierKingdom, queriedKingdom);
					result.Add(fiefWithSameCultureEffect, _sameCultureFiefsText);
					float honorableKingEffect = GetHonorableKingEffect(querierKingdom, queriedKingdom);
					if (includeDescription)
					{
						_lowHonorText.SetCharacterProperties("RULER", querierKingdom.Leader.CharacterObject);
					}
					result.Add(honorableKingEffect, _lowHonorText);
					float tradeAgreementEffect = GetTradeAgreementEffect(querierKingdom, queriedKingdom);
					result.Add(tradeAgreementEffect, _tradeAgreementEffect);
					float commonThreatEffect = GetCommonThreatEffect(kingdom, threateningKingdomForQueried);
					result.Add(commonThreatEffect, _commonThreatEffect);
					if (includeDescription)
					{
						if (alliancePenalty < 0f)
						{
							list.Add((alliancePenalty, alliancePenaltyText));
						}
						if (alliancePenalty2 < 0f)
						{
							list.Add((alliancePenalty2, alliancePenaltyText2));
						}
						if (relationshipEffect < 0f)
						{
							list.Add((relationshipEffect, _relationshipText));
						}
						if (atWarWithAllyEffect < 0f)
						{
							list.Add((atWarWithAllyEffect, _atWarWithAllyText));
						}
						if (atWarOrPeaceEffect < 0f)
						{
							list.Add((atWarOrPeaceEffect, _atWarText));
						}
						if (atWarWithAllyEffect < 0f)
						{
							list.Add((fiefWithSameCultureEffect, _sameCultureFiefsText));
						}
						if (honorableKingEffect < 0f)
						{
							list.Add((honorableKingEffect, _lowHonorText));
						}
						if ((float)list.Count > 0f)
						{
							explanationText = BuildExplanationForAlliance(querierKingdom, list);
						}
						else
						{
							_allianceScoreNotEnoughText.SetTextVariable("KINGDOM_NAME", querierKingdom.Name);
							explanationText.SetTextVariable("REASON", _allianceScoreNotEnoughText);
						}
					}
				}
				else if (includeDescription)
				{
					_allianceScoreNotEnoughText.SetTextVariable("KINGDOM_NAME", querierKingdom.Name);
					explanationText.SetTextVariable("REASON", _allianceScoreNotEnoughText);
				}
			}
			else if (includeDescription)
			{
				_kingdomsNotSeekingAllianceText.SetTextVariable("KINGDOM_NAME", querierKingdom.Name);
				explanationText.SetTextVariable("REASON", _kingdomsNotSeekingAllianceText);
			}
		}
		else if (includeDescription)
		{
			explanationText.SetTextVariable("REASON", _kingdomsNotNeighborsText);
		}
		return result;
	}

	public override float GetSupportScoreOfStartingAllianceForClan(Kingdom querierKingdom, Kingdom queriedKingdom, Clan evaluatingClan, out TextObject explanationText, bool includeDescriptions = false)
	{
		explanationText = (includeDescriptions ? TextObject.GetEmpty() : null);
		int influenceCostOfProposingStartingAlliance = Campaign.Current.Models.AllianceModel.GetInfluenceCostOfProposingStartingAlliance(evaluatingClan);
		if (evaluatingClan == Clan.PlayerClan)
		{
			return influenceCostOfProposingStartingAlliance;
		}
		if (evaluatingClan.Kingdom != Clan.PlayerClan.Kingdom)
		{
			return influenceCostOfProposingStartingAlliance;
		}
		float num = Campaign.Current.Models.AllianceModel.GetScoreOfStartingAlliance(querierKingdom, queriedKingdom, out explanationText, includeDescriptions).ResultNumber;
		if (evaluatingClan.Leader != null)
		{
			num += (float)evaluatingClan.Leader.GetRelation(queriedKingdom.Leader) * 0.25f;
		}
		if (IsThereMarriageBetweenClans(evaluatingClan, queriedKingdom.RulingClan))
		{
			num += 25f;
		}
		else if (queriedKingdom.Clans.AnyQ((Clan clan) => clan != queriedKingdom.RulingClan && IsThereMarriageBetweenClans(evaluatingClan, clan)))
		{
			num += 5f;
		}
		if (queriedKingdom.Leader != null)
		{
			num += (float)queriedKingdom.Leader.GetTraitLevel(DefaultTraits.Honor) * 6.25f;
		}
		if (evaluatingClan.Leader != null)
		{
			num += evaluatingClan.Leader.RandomFloatWithSeed((uint)CampaignTime.Now.ToDays) * 20f - 10f;
		}
		if (num > 0f)
		{
			return MBMath.Map(num, 0f, 195f, 0f, influenceCostOfProposingStartingAlliance);
		}
		return MBMath.Map(num, -135f, 0f, -influenceCostOfProposingStartingAlliance, 0f);
	}

	public override bool CanMakeAlliance(Kingdom kingdom, Kingdom targetKingdom, IFaction evaluatingFaction, out TextObject reason, bool includeReason = false)
	{
		reason = (includeReason ? _allianceNotFormedExplanationText : null);
		if (targetKingdom.IsEliminated || kingdom.IsEliminated)
		{
			if (includeReason)
			{
				reason.SetTextVariable("REASON", new TextObject("{=a5EAl1aW}That realm has been eliminated."));
			}
			return false;
		}
		if (targetKingdom == kingdom)
		{
			if (includeReason)
			{
				reason.SetTextVariable("REASON", new TextObject("{=zPoS5fIu}You are referring to your own realm."));
			}
			return false;
		}
		if (targetKingdom.IsAtWarWith(kingdom))
		{
			if (includeReason)
			{
				TextObject textObject = new TextObject("{=lseJ70y0}Your realm is at war with the {KINGDOM_NAME}.");
				textObject.SetTextVariable("KINGDOM_NAME", targetKingdom.Name);
				reason.SetTextVariable("REASON", textObject);
			}
			return false;
		}
		if (kingdom.AlliedKingdoms.Count >= Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances)
		{
			if (includeReason)
			{
				TextObject textObject2 = new TextObject("{=*}Your realm's current number of allies: {NUMBER_OF_ALLIES}/{MAX_NUMBER_OF_ALLIES}");
				textObject2.SetTextVariable("NUMBER_OF_ALLIES", kingdom.AlliedKingdoms.Count);
				textObject2.SetTextVariable("MAX_NUMBER_OF_ALLIES", Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances);
				reason.SetTextVariable("REASON", textObject2);
			}
			return false;
		}
		if (targetKingdom.AlliedKingdoms.Count >= Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances)
		{
			if (includeReason)
			{
				TextObject textObject3 = new TextObject("{=rYssCdQb}{KINGDOM_NAME} cannot have any more allies.");
				textObject3.SetTextVariable("KINGDOM_NAME", targetKingdom.Name);
				reason.SetTextVariable("REASON", textObject3);
			}
			return false;
		}
		if (targetKingdom.IsAllyWith(kingdom))
		{
			if (includeReason)
			{
				TextObject textObject4 = new TextObject("{=zd9sawl9}You are already allied with the {KINGDOM_NAME}.");
				textObject4.SetTextVariable("KINGDOM_NAME", targetKingdom.Name);
				reason.SetTextVariable("REASON", textObject4);
			}
			return false;
		}
		if (kingdom == Clan.PlayerClan.Kingdom)
		{
			if (Campaign.Current.Models.AllianceModel.GetScoreOfStartingAlliance(targetKingdom, kingdom, out reason, includeReason).ResultNumber < 50f)
			{
				return false;
			}
			if (evaluatingFaction != Clan.PlayerClan && evaluatingFaction is Clan evaluatingClan && !CanMakeAllianceWithPlayerSupport(kingdom, targetKingdom, evaluatingClan))
			{
				return false;
			}
		}
		else
		{
			if (Campaign.Current.Models.AllianceModel.GetScoreOfStartingAlliance(kingdom, targetKingdom, out reason, includeReason).ResultNumber < 50f)
			{
				return false;
			}
			if (Clan.PlayerClan?.Kingdom != null && Clan.PlayerClan.Kingdom == targetKingdom)
			{
				if (!CanMakeAllianceWithPlayerSupport(targetKingdom, kingdom, Campaign.Current.Models.AllianceModel.GetProposerClanForAllianceDecision(targetKingdom, kingdom)))
				{
					return false;
				}
			}
			else if (Campaign.Current.Models.AllianceModel.GetScoreOfStartingAlliance(targetKingdom, kingdom, out reason, includeReason).ResultNumber < 50f)
			{
				return false;
			}
		}
		return true;
	}

	public override int GetInfluenceCostOfProposingStartingAlliance(Clan proposingClan)
	{
		return 200;
	}

	public override float GetScoreOfCallingToWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason)
	{
		float num = 60f;
		reason = TextObject.GetEmpty();
		int callToWarBudgetOfCallingKingdom = GetCallToWarBudgetOfCallingKingdom(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		Clan evaluatingClan = ((evaluatingFaction is Clan clan) ? clan : callingKingdom.RulingClan);
		TextObject reason2;
		float scoreOfDeclaringWar = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(callingKingdom, kingdomToCallToWarAgainst, evaluatingClan, out reason2);
		if (callToWarBudgetOfCallingKingdom < 0 || callingKingdom.CallToWarWallet < -100000 || (float)callToWarBudgetOfCallingKingdom * 1.5f < (float)callToWarCost || scoreOfDeclaringWar > 0f)
		{
			return -100f;
		}
		if (callToWarCost == 0)
		{
			return 100f;
		}
		float num2 = (float)callToWarBudgetOfCallingKingdom / (float)callToWarCost;
		num *= num2;
		return num + ((float)evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Calculating) * 2.5f - (float)evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Valor) * 2.5f);
	}

	public override float GetScoreOfJoiningWar(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst, IFaction evaluatingFaction, out TextObject reason)
	{
		float num = 70f;
		reason = TextObject.GetEmpty();
		int callToWarCostForCalledKingdom = GetCallToWarCostForCalledKingdom(calledKingdom, kingdomToCallToWarAgainst);
		int callToWarCost = Campaign.Current.Models.AllianceModel.GetCallToWarCost(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		if (callToWarCostForCalledKingdom == 0)
		{
			return 100f;
		}
		float value = (float)callToWarCost / (float)callToWarCostForCalledKingdom;
		value = MathF.Clamp(value, 1E-05f, 2f);
		num *= value;
		return num + ((float)evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Valor) * 2.5f + (float)evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Calculating) * 2.5f);
	}

	public override int GetInfluenceCostOfCallingToWar(Clan proposingClan)
	{
		return 200;
	}

	public override float GetAllianceFactorForDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
	{
		if (factionDeclaresWar.IsKingdomFaction && factionDeclaredWar.IsKingdomFaction)
		{
			bool flag = false;
			float num = 1f;
			Kingdom kingdom = (Kingdom)factionDeclaresWar;
			Kingdom kingdom2 = (Kingdom)factionDeclaredWar;
			foreach (Kingdom alliedKingdom in kingdom.AlliedKingdoms)
			{
				if (alliedKingdom == kingdom2)
				{
					num *= 0.5f;
					break;
				}
				if (!flag && alliedKingdom.IsAtWarWith(kingdom2))
				{
					num = ((GetThreateningNeighbor(kingdom, out var _, out var _).threateningKingdom != kingdom2) ? (num * 1.3f) : (num * 1.5f));
					flag = true;
				}
			}
			return num;
		}
		return 1f;
	}

	public override float GetAllianceFactorForDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
	{
		if (factionDeclaresPeace.IsKingdomFaction && factionDeclaredPeace.IsKingdomFaction)
		{
			float num = 1f;
			Kingdom obj = (Kingdom)factionDeclaresPeace;
			Kingdom other = (Kingdom)factionDeclaredPeace;
			foreach (Kingdom alliedKingdom in obj.AlliedKingdoms)
			{
				if (alliedKingdom.IsAtWarWith(other))
				{
					num *= 0.7f;
					break;
				}
			}
			return num;
		}
		return 1f;
	}

	public override Clan GetProposerClanForAllianceDecision(Kingdom proposerKingdom, Kingdom proposedKingdom)
	{
		TextObject explanation;
		Clan clan = ((proposerKingdom.RulingClan != Clan.PlayerClan && Campaign.Current.Models.AllianceModel.GetSupportScoreOfStartingAllianceForClan(proposerKingdom, proposedKingdom, proposerKingdom.RulingClan, out explanation) > 0f) ? proposerKingdom.RulingClan : null);
		if (clan == null)
		{
			List<(Clan, float)> list = new List<(Clan, float)>(proposerKingdom.Clans.Count);
			foreach (Clan clan2 in proposerKingdom.Clans)
			{
				if (!clan2.IsUnderMercenaryService && clan2 != Clan.PlayerClan)
				{
					float supportScoreOfStartingAllianceForClan = Campaign.Current.Models.AllianceModel.GetSupportScoreOfStartingAllianceForClan(proposerKingdom, proposedKingdom, clan2, out explanation);
					if (supportScoreOfStartingAllianceForClan > 0f)
					{
						list.Add((clan2, supportScoreOfStartingAllianceForClan));
					}
				}
			}
			clan = ((list.Count <= 0) ? ((proposerKingdom == Clan.PlayerClan.Kingdom) ? Clan.PlayerClan : proposerKingdom.RulingClan) : TaleWorlds.Core.Extensions.MaxBy<(Clan, float), float>(list, ((Clan Clan, float Score) x) => x.Score).Item1);
		}
		return clan;
	}

	private (Kingdom threateningKingdom, float threatScore) GetThreateningNeighbor(Kingdom querierKingdom, out float exposureScore, out float powerRatio)
	{
		HashSet<Settlement> hashSet = new HashSet<Settlement>();
		Dictionary<Kingdom, float> dictionary = new Dictionary<Kingdom, float>();
		float num = 0f;
		foreach (Town fief in querierKingdom.Fiefs)
		{
			foreach (Settlement neighborFortification in fief.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification.MapFaction != querierKingdom && neighborFortification.MapFaction.IsKingdomFaction && !hashSet.Contains(neighborFortification))
				{
					Kingdom key = (Kingdom)neighborFortification.MapFaction;
					if (dictionary.ContainsKey(key))
					{
						dictionary[key] += 1f;
					}
					else
					{
						dictionary.Add(key, 1f);
					}
					num += 1f;
					hashSet.Add(neighborFortification);
				}
			}
		}
		float num2 = 0f;
		Kingdom item = null;
		exposureScore = 0f;
		powerRatio = 0f;
		foreach (KeyValuePair<Kingdom, float> item2 in dictionary)
		{
			float exposureScore2;
			float powerRatio2;
			float num3 = CalculateThreatScore(item2.Value, num, CalculateKingdomStrength(item2.Key), CalculateKingdomStrength(querierKingdom), out exposureScore2, out powerRatio2);
			if (num2 < num3)
			{
				item = item2.Key;
				num2 = num3;
				exposureScore = exposureScore2;
				powerRatio = powerRatio2;
			}
		}
		return (threateningKingdom: item, threatScore: num2);
	}

	private bool IsThereMarriageBetweenClans(Clan clan1, Clan clan2)
	{
		if (!clan1.AliveLords.AnyQ((Hero x) => x.Spouse?.Father?.Clan == clan2))
		{
			return clan2.AliveLords.AnyQ((Hero x) => x.Spouse?.Father?.Clan == clan1);
		}
		return true;
	}

	private TextObject BuildExplanationForAlliance(Kingdom other, List<(float, TextObject)> explanationList)
	{
		TextObject textObject = null;
		textObject = _kingdomNotConsederingAllianceText;
		List<TextObject> list = new List<TextObject>();
		foreach (var item2 in explanationList.OrderBy(((float, TextObject) x) => x.Item1))
		{
			TextObject item = item2.Item2;
			list.Add(item);
			if (list.Count >= 3)
			{
				break;
			}
		}
		TextObject variable = GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}"));
		textObject.SetTextVariable("REASONS_BY_LINE", variable);
		textObject.SetTextVariable("KINGDOM", other.Name);
		return textObject;
	}

	private int GetCallToWarCostForCalledKingdom(Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		TextObject reason;
		float scoreOfDeclaringWar = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringWar(calledKingdom, kingdomToCallToWarAgainst, calledKingdom.RulingClan, out reason);
		float num = Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(kingdomToCallToWarAgainst) - scoreOfDeclaringWar;
		if (num <= 0f)
		{
			return 0;
		}
		float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(calledKingdom);
		float num2 = num / (valueOfSettlementsForFaction + 1f);
		double num3 = (double)calledKingdom.Fiefs.SumQ((Town x) => x.Prosperity) * 0.35;
		return (int)((double)num2 * num3 * Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation.ToDays);
	}

	private int GetCallToWarBudgetOfCallingKingdom(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		float num = CalculateKingdomStrength(callingKingdom);
		float num2 = CalculateKingdomStrength(calledKingdom);
		float num3 = CalculateKingdomStrength(kingdomToCallToWarAgainst);
		double num4 = (double)callingKingdom.Fiefs.SumQ((Town x) => x.Prosperity) * 0.35;
		float num5 = num - num3;
		if (num5.ApproximatelyEqualsTo(0f))
		{
			return int.MinValue;
		}
		return (int)((double)MathF.Clamp(0f - num2 / num5, float.MinValue, 1f) * num4 * Campaign.Current.Models.AllianceModel.MaxDurationOfWarParticipation.ToDays);
	}

	private bool AreKingdomsNeighbors(Kingdom kingdom1, Kingdom kingdom2)
	{
		if (kingdom1 == kingdom2 || kingdom1.Fiefs.Count == 0 || kingdom2.Fiefs.Count == 0)
		{
			return false;
		}
		foreach (Town fief in kingdom1.Fiefs)
		{
			foreach (Settlement neighborFortification in fief.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification.MapFaction == kingdom2)
				{
					return true;
				}
			}
		}
		return false;
	}

	private float CalculateThreatScore(float neighborScore, float totalNeighborScore, float powerOfThreat, float powerOfQuerier, out float exposureScore, out float powerRatio)
	{
		if (powerOfQuerier <= 0f || totalNeighborScore <= 0f)
		{
			exposureScore = 0f;
			powerRatio = 0f;
			return 0f;
		}
		exposureScore = MBMath.Map(neighborScore / totalNeighborScore, 0f, 1f, 1f, 2f);
		powerRatio = MathF.Clamp(powerOfThreat / powerOfQuerier, 0f, 3f);
		return (MathF.Min(exposureScore, 1.7f) + 0.4f + powerRatio) * 130f;
	}

	private float GetThreatEffect(float threatScoreForQuerier, float threatScoreForQueried)
	{
		return 0.08f * (threatScoreForQueried + threatScoreForQuerier) * 0.66f;
	}

	private float GetRelationshipEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		if (querierKingdom.Leader != null && queriedKingdom.Leader != null)
		{
			int relation = querierKingdom.Leader.GetRelation(queriedKingdom.Leader);
			int traitLevel = querierKingdom.Leader.GetTraitLevel(DefaultTraits.Calculating);
			if (relation > 0 || traitLevel <= 0)
			{
				return MathF.Clamp(relation, -100f, 100f) * 0.08f;
			}
		}
		return 0f;
	}

	private float GetMarriageEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		if (IsThereMarriageBetweenClans(querierKingdom.RulingClan, queriedKingdom.RulingClan))
		{
			return 8f;
		}
		if (querierKingdom.Leader != null && queriedKingdom.RulingClan.AliveLords.AnyQ((Hero x) => x.Spouse?.Father?.MapFaction == queriedKingdom))
		{
			return 4f;
		}
		return 0f;
	}

	private float GetAtWarWithAllyEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		return (queriedKingdom.FactionsAtWarWith.AnyQ((IFaction x) => x.IsKingdomFaction && querierKingdom.IsAllyWith((Kingdom)x)) ? (-100f) : 0f) * 0.08f;
	}

	private float GetAtWarWithEnemyEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		return (queriedKingdom.FactionsAtWarWith.AnyQ((IFaction x) => x.IsKingdomFaction && querierKingdom.IsAtWarWith((Kingdom)x)) ? 100f : 0f) * 0.08f;
	}

	private float GetAtWarOrPeaceEffect(Kingdom queriedKingdom)
	{
		return (queriedKingdom.FactionsAtWarWith.AnyQ((IFaction x) => x.IsKingdomFaction) ? (-5f) : 25f) * 0.08f;
	}

	private float GetFiefWithSameCultureEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		float num = queriedKingdom.Fiefs.Count;
		if (num > 0f)
		{
			return MathF.Clamp((float)queriedKingdom.Fiefs.Count((Town fief) => fief.Culture == querierKingdom.Culture) / num * -200f, -200f, 0f) * 0.08f;
		}
		return 0f;
	}

	private float GetHonorableKingEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		if (querierKingdom.Leader != null)
		{
			int traitLevel = querierKingdom.Leader.GetTraitLevel(DefaultTraits.Honor);
			int traitLevel2 = queriedKingdom.Leader.GetTraitLevel(DefaultTraits.Honor);
			if (traitLevel > 0)
			{
				return 4f;
			}
			if (traitLevel < 0 && traitLevel2 > 0)
			{
				return -4f;
			}
		}
		return 0f;
	}

	private float GetTradeAgreementEffect(Kingdom querierKingdom, Kingdom queriedKingdom)
	{
		ITradeAgreementsCampaignBehavior tradeAgreementsCampaignBehavior = TradeAgreementsCampaignBehavior;
		if (tradeAgreementsCampaignBehavior != null && tradeAgreementsCampaignBehavior.HasTradeAgreement(querierKingdom, queriedKingdom, out var _))
		{
			return 4f;
		}
		return 0f;
	}

	private float GetCommonThreatEffect(Kingdom threateningKingdomForQuerier, Kingdom threateningKingdomForQueried)
	{
		if (threateningKingdomForQuerier != null && threateningKingdomForQueried != null && threateningKingdomForQuerier == threateningKingdomForQueried)
		{
			return 8f;
		}
		return 0f;
	}

	private float GetAlliancePenalty(Kingdom kingdom)
	{
		if (kingdom.AlliedKingdoms.Count > 0)
		{
			float num = -48f;
			if (kingdom == Clan.PlayerClan.Kingdom)
			{
				num *= 0.5f;
			}
			return num;
		}
		return 0f;
	}

	private TextObject GetAlliancePenaltyText(Kingdom kingdom, bool includeDescription)
	{
		TextObject textObject = ((kingdom == Clan.PlayerClan.Kingdom) ? _tooManyAlliancePlayerPenaltyText : _tooManyAllianceAIPenaltyText);
		if (includeDescription)
		{
			textObject.SetTextVariable("NUMBER_OF_ALLIES", kingdom.AlliedKingdoms.Count);
			textObject.SetTextVariable("KINGDOM_NAME", kingdom.Name);
			textObject.SetTextVariable("MAX_NUMBER_OF_ALLIES", Campaign.Current.Models.AllianceModel.MaxNumberOfAlliances);
		}
		return textObject;
	}

	private bool CanMakeAllianceWithPlayerSupport(Kingdom proposingKingdom, Kingdom proposedKingdom, Clan evaluatingClan)
	{
		if (proposingKingdom == Clan.PlayerClan.Kingdom && evaluatingClan == Clan.PlayerClan)
		{
			return true;
		}
		KingdomElection kingdomElection = new KingdomElection(new StartAllianceDecision(evaluatingClan, proposedKingdom));
		DecisionOutcome supportedOutcome = kingdomElection.PossibleOutcomes.FirstOrDefault((DecisionOutcome x) => x is StartAllianceDecision.StartAllianceDecisionOutcome startAllianceDecisionOutcome && startAllianceDecisionOutcome.ShouldAllianceBeStarted);
		kingdomElection.SetupResultWithoutPlayerSupport();
		return kingdomElection.GetWinChanceWithPlayerSupport(supportedOutcome, Supporter.SupportWeights.FullyPush) > 0.5f;
	}

	private float CalculateKingdomStrength(Kingdom kingdom)
	{
		float num = 0f;
		foreach (Clan clan in kingdom.Clans)
		{
			if (!clan.IsUnderMercenaryService)
			{
				num += clan.CurrentTotalStrength;
			}
		}
		return num;
	}
}
