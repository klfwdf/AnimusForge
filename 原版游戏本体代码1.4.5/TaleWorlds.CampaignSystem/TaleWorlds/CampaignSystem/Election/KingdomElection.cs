using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Election;

public class KingdomElection
{
	public enum ElectionOutcomeSupport
	{
		LowSupport,
		SlightSupport,
		GoodSupport,
		StrongSupport
	}

	private const float StrongSupportThreshold = 0.75f;

	private const float GoodSupportThreshold = 0.5f;

	private const float SlightSupportThreshold = 0.2f;

	[SaveableField(0)]
	private readonly KingdomDecision _decision;

	private MBList<DecisionOutcome> _possibleOutcomes;

	[SaveableField(2)]
	private List<Supporter> _supporters;

	[SaveableField(3)]
	private Clan _chooser;

	[SaveableField(4)]
	private DecisionOutcome _chosenOutcome;

	[SaveableField(5)]
	private bool _ignorePlayerSupport;

	[SaveableField(6)]
	private bool _hasPlayerVoted;

	public MBReadOnlyList<DecisionOutcome> PossibleOutcomes => _possibleOutcomes;

	[SaveableProperty(7)]
	public bool IsCancelled { get; private set; }

	public bool IsPlayerSupporter => PlayerAsSupporter != null;

	private Supporter PlayerAsSupporter => _supporters.FirstOrDefault((Supporter x) => x.IsPlayer);

	public bool IsPlayerChooser => _chooser.Leader.IsHumanPlayerCharacter;

	public KingdomElection(KingdomDecision decision)
	{
		_decision = decision;
		Setup();
	}

	private void Setup()
	{
		MBList<DecisionOutcome> initialCandidates = _decision.DetermineInitialCandidates().ToMBList();
		_possibleOutcomes = _decision.NarrowDownCandidates(initialCandidates, 3);
		_supporters = _decision.DetermineSupporters().ToList();
		_chooser = _decision.DetermineChooser();
		_decision.DetermineSponsors(_possibleOutcomes);
		_hasPlayerVoted = false;
		IsCancelled = false;
		foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
		{
			possibleOutcome.InitialSupport = DetermineInitialSupport(possibleOutcome);
		}
		float num = _possibleOutcomes.Sum((DecisionOutcome x) => x.InitialSupport);
		foreach (DecisionOutcome possibleOutcome2 in _possibleOutcomes)
		{
			possibleOutcome2.Likelihood = ((num == 0f) ? 0f : (possibleOutcome2.InitialSupport / num));
		}
	}

	public void StartElection()
	{
		Setup();
		DetermineSupport(_possibleOutcomes, calculateRelationshipEffect: false);
		_decision.DetermineSponsors(_possibleOutcomes);
		UpdateSupport(_possibleOutcomes);
		if (_decision.ShouldBeCancelled())
		{
			Debug.Print(string.Concat("SELIM_DEBUG - ", _decision.GetSupportTitle(), " has been cancelled"));
			IsCancelled = true;
			bool isPlayerInvolved = _decision.DetermineChooser().Leader.IsHumanPlayerCharacter || _decision.DetermineSupporters().Any((Supporter x) => x.IsPlayer);
			CampaignEventDispatcher.Instance.OnKingdomDecisionCancelled(_decision, isPlayerInvolved);
		}
		else if (!IsPlayerSupporter || _ignorePlayerSupport)
		{
			ReadyToAiChoose();
		}
		else if (_decision.IsSingleClanDecision())
		{
			_chosenOutcome = _possibleOutcomes.FirstOrDefault((DecisionOutcome t) => t.SponsorClan != null && t.SponsorClan == Clan.PlayerClan);
			Supporter supporter = new Supporter(Clan.PlayerClan);
			supporter.SupportWeight = Supporter.SupportWeights.FullyPush;
			_chosenOutcome.AddSupport(supporter);
		}
	}

	public static ElectionOutcomeSupport GetElectionOutcomeSupport(KingdomDecision decision, Clan sponsor)
	{
		KingdomElection kingdomElection = new KingdomElection(decision);
		kingdomElection.SetupResultWithoutPlayerSupport();
		return kingdomElection.GetDecisionOutcomeSupportForSponsor(sponsor);
	}

	public void SetupResultWithoutPlayerSupport()
	{
		DetermineSupport(_possibleOutcomes, calculateRelationshipEffect: false);
		_decision.DetermineSponsors(_possibleOutcomes);
		UpdateSupport(_possibleOutcomes);
		DetermineOfficialSupport();
	}

	private float DetermineInitialSupport(DecisionOutcome possibleOutcome)
	{
		float num = 0f;
		foreach (Supporter supporter in _supporters)
		{
			if (!supporter.IsPlayer)
			{
				num += MathF.Clamp(_decision.DetermineSupport(supporter.Clan, possibleOutcome), 0f, 100f);
			}
		}
		return num;
	}

	public void StartElectionWithoutPlayer()
	{
		_ignorePlayerSupport = true;
		StartElection();
	}

	public float GetLikelihoodForSponsor(Clan sponsor)
	{
		foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
		{
			if (possibleOutcome.SponsorClan == sponsor)
			{
				return possibleOutcome.Likelihood;
			}
		}
		Debug.FailedAssert("This clan is not a sponsor of any of the outcomes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Election\\KingdomDecisionMaker.cs", "GetLikelihoodForSponsor", 174);
		return -1f;
	}

	public float GetWinChanceForSponsor(Clan sponsor)
	{
		foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
		{
			if (possibleOutcome.SponsorClan == sponsor)
			{
				return possibleOutcome.WinChance;
			}
		}
		Debug.FailedAssert("This clan is not a sponsor of any of the outcomes.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Election\\KingdomDecisionMaker.cs", "GetWinChanceForSponsor", 189);
		return -1f;
	}

	public ElectionOutcomeSupport GetDecisionOutcomeSupportForSponsor(Clan sponsor)
	{
		float winChanceForSponsor = GetWinChanceForSponsor(sponsor);
		if (winChanceForSponsor > 0.75f)
		{
			return ElectionOutcomeSupport.StrongSupport;
		}
		if (winChanceForSponsor > 0.5f)
		{
			return ElectionOutcomeSupport.GoodSupport;
		}
		if (winChanceForSponsor > 0.2f)
		{
			return ElectionOutcomeSupport.SlightSupport;
		}
		return ElectionOutcomeSupport.LowSupport;
	}

	private void DetermineSupport(MBReadOnlyList<DecisionOutcome> possibleOutcomes, bool calculateRelationshipEffect)
	{
		foreach (Supporter supporter in _supporters)
		{
			if (!supporter.IsPlayer)
			{
				Supporter.SupportWeights supportWeightOfSelectedOutcome = Supporter.SupportWeights.StayNeutral;
				DecisionOutcome decisionOutcome = _decision.DetermineSupportOption(supporter, possibleOutcomes, out supportWeightOfSelectedOutcome, calculateRelationshipEffect);
				if (decisionOutcome != null)
				{
					supporter.SupportWeight = supportWeightOfSelectedOutcome;
					decisionOutcome.AddSupport(supporter);
				}
			}
		}
	}

	private void UpdateSupport(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
	{
		foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
		{
			foreach (Supporter item in new List<Supporter>(possibleOutcome.SupporterList))
			{
				possibleOutcome.ResetSupport(item);
			}
		}
		DetermineSupport(possibleOutcomes, calculateRelationshipEffect: true);
	}

	private void ReadyToAiChoose()
	{
		_chosenOutcome = GetAiChoice(_possibleOutcomes);
		if (_decision.OnShowDecision())
		{
			ApplyChosenOutcome();
		}
	}

	private void ApplyChosenOutcome()
	{
		_decision.ApplyChosenOutcome(_chosenOutcome);
		_decision.SupportStatusOfFinalDecision = GetSupportStatusOfDecisionOutcome(_chosenOutcome);
		HandleInfluenceCosts();
		ApplySecondaryEffects(_possibleOutcomes, _chosenOutcome);
		for (int i = 0; i < _possibleOutcomes.Count; i++)
		{
			if (_possibleOutcomes[i].SponsorClan == null)
			{
				continue;
			}
			foreach (Supporter supporter in _possibleOutcomes[i].SupporterList)
			{
				if (supporter.Clan.Leader != _possibleOutcomes[i].SponsorClan.Leader && supporter.Clan == Clan.PlayerClan)
				{
					int relationChangeWithSponsor = GetRelationChangeWithSponsor(supporter.Clan.Leader, supporter.SupportWeight, isOpposingSides: false);
					if (relationChangeWithSponsor != 0)
					{
						relationChangeWithSponsor *= ((_possibleOutcomes.Count <= 2) ? 1 : 2);
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(supporter.Clan.Leader, _possibleOutcomes[i].SponsorClan.Leader, relationChangeWithSponsor);
					}
				}
			}
			for (int j = 0; j < _possibleOutcomes.Count; j++)
			{
				if (i == j)
				{
					continue;
				}
				foreach (Supporter supporter2 in _possibleOutcomes[j].SupporterList)
				{
					if (supporter2.Clan.Leader != _possibleOutcomes[i].SponsorClan.Leader && supporter2.Clan == Clan.PlayerClan)
					{
						int relationChangeWithSponsor2 = GetRelationChangeWithSponsor(supporter2.Clan.Leader, supporter2.SupportWeight, isOpposingSides: true);
						if (relationChangeWithSponsor2 != 0)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(supporter2.Clan.Leader, _possibleOutcomes[i].SponsorClan.Leader, relationChangeWithSponsor2);
						}
					}
				}
			}
		}
		_decision.Kingdom.RemoveDecision(_decision);
		_decision.Kingdom.OnKingdomDecisionConcluded();
		CampaignEventDispatcher.Instance.OnKingdomDecisionConcluded(_decision, _chosenOutcome, IsPlayerChooser || _hasPlayerVoted);
	}

	public int GetRelationChangeWithSponsor(Hero opposerOrSupporter, Supporter.SupportWeights supportWeight, bool isOpposingSides)
	{
		int num = 0;
		Clan clan = opposerOrSupporter.Clan;
		switch (supportWeight)
		{
		case Supporter.SupportWeights.FullyPush:
			num = (int)((float)_decision.GetInfluenceCostOfSupport(clan, Supporter.SupportWeights.FullyPush) / 20f);
			break;
		case Supporter.SupportWeights.StronglyFavor:
			num = (int)((float)_decision.GetInfluenceCostOfSupport(clan, Supporter.SupportWeights.StronglyFavor) / 20f);
			break;
		case Supporter.SupportWeights.SlightlyFavor:
			num = (int)((float)_decision.GetInfluenceCostOfSupport(clan, Supporter.SupportWeights.SlightlyFavor) / 20f);
			break;
		}
		int num2 = (isOpposingSides ? (num * -1) : (num * 2));
		if (isOpposingSides && opposerOrSupporter.Culture.HasFeat(DefaultCulturalFeats.SturgianDecisionPenaltyFeat))
		{
			num2 += (int)((float)num2 * DefaultCulturalFeats.SturgianDecisionPenaltyFeat.EffectBonus);
		}
		return num2;
	}

	private void HandleInfluenceCosts()
	{
		DecisionOutcome decisionOutcome = _possibleOutcomes[0];
		foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
		{
			if (possibleOutcome.TotalSupportPoints > decisionOutcome.TotalSupportPoints)
			{
				decisionOutcome = possibleOutcome;
			}
			for (int i = 0; i < possibleOutcome.SupporterList.Count; i++)
			{
				Clan clan = possibleOutcome.SupporterList[i].Clan;
				int num = _decision.GetInfluenceCost(possibleOutcome, clan, possibleOutcome.SupporterList[i].SupportWeight);
				if (_supporters.Count == 1)
				{
					num = 0;
				}
				if (_chosenOutcome != possibleOutcome)
				{
					num /= 2;
				}
				if (possibleOutcome == _chosenOutcome || !clan.Leader.GetPerkValue(DefaultPerks.Charm.GoodNatured))
				{
					ChangeClanInfluenceAction.Apply(clan, -num);
				}
			}
		}
		if (_chosenOutcome != decisionOutcome)
		{
			int influenceRequiredToOverrideKingdomDecision = Campaign.Current.Models.ClanPoliticsModel.GetInfluenceRequiredToOverrideKingdomDecision(decisionOutcome, _chosenOutcome, _decision);
			ChangeClanInfluenceAction.Apply(_chooser, -influenceRequiredToOverrideKingdomDecision);
		}
	}

	private void ApplySecondaryEffects(MBReadOnlyList<DecisionOutcome> possibleOutcomes, DecisionOutcome chosenOutcome)
	{
		_decision.ApplySecondaryEffects(possibleOutcomes, chosenOutcome);
	}

	private int GetInfluenceRequiredToOverrideDecision(DecisionOutcome popularOutcome, DecisionOutcome overridingOutcome)
	{
		return Campaign.Current.Models.ClanPoliticsModel.GetInfluenceRequiredToOverrideKingdomDecision(popularOutcome, overridingOutcome, _decision);
	}

	private DecisionOutcome GetAiChoice(MBReadOnlyList<DecisionOutcome> possibleOutcomes)
	{
		DecisionOutcome decisionOutcome = null;
		DetermineOfficialSupport();
		DecisionOutcome decisionOutcome2 = TaleWorlds.Core.Extensions.MaxBy(possibleOutcomes, (DecisionOutcome t) => t.TotalSupportPoints);
		decisionOutcome = decisionOutcome2;
		if (_decision.IsKingsVoteAllowed)
		{
			DecisionOutcome decisionOutcome3 = TaleWorlds.Core.Extensions.MaxBy(possibleOutcomes, (DecisionOutcome t) => _decision.DetermineSupport(_chooser, t));
			float num = _decision.DetermineSupport(_chooser, decisionOutcome3);
			float num2 = _decision.DetermineSupport(_chooser, decisionOutcome2);
			float a = num - num2;
			a = MathF.Min(a, _chooser.Influence);
			if (a > 10f)
			{
				float num3 = 300f + (float)GetInfluenceRequiredToOverrideDecision(decisionOutcome2, decisionOutcome3);
				if (a > num3)
				{
					float num4 = num3 / a;
					if (MBRandom.RandomFloat > num4)
					{
						decisionOutcome = decisionOutcome3;
					}
				}
			}
		}
		return decisionOutcome;
	}

	public TextObject GetChosenOutcomeText()
	{
		return _decision.GetChosenOutcomeText(_chosenOutcome, _decision.SupportStatusOfFinalDecision);
	}

	private KingdomDecision.SupportStatus GetSupportStatusOfDecisionOutcome(DecisionOutcome chosenOutcome)
	{
		KingdomDecision.SupportStatus result = KingdomDecision.SupportStatus.Equal;
		float num = chosenOutcome.WinChance * 100f;
		int num2 = 50;
		if (num > (float)(num2 + 5))
		{
			result = KingdomDecision.SupportStatus.Majority;
		}
		else if (num < (float)(num2 - 5))
		{
			result = KingdomDecision.SupportStatus.Minority;
		}
		return result;
	}

	public void DetermineOfficialSupport()
	{
		float num = 0.001f;
		foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
		{
			float num2 = 0f;
			foreach (Supporter supporter in possibleOutcome.SupporterList)
			{
				num2 += (float)MathF.Max(0, (int)(supporter.SupportWeight - 1));
			}
			possibleOutcome.TotalSupportPoints = num2;
			num += possibleOutcome.TotalSupportPoints;
		}
		foreach (DecisionOutcome possibleOutcome2 in _possibleOutcomes)
		{
			possibleOutcome2.WinChance = possibleOutcome2.TotalSupportPoints / num;
		}
	}

	public float GetWinChanceWithPlayerSupport(DecisionOutcome supportedOutcome, Supporter.SupportWeights supportWeight)
	{
		if (_possibleOutcomes.Contains(supportedOutcome))
		{
			float num = 0f;
			num = ((supportedOutcome.WinChance == 0f) ? _possibleOutcomes.First((DecisionOutcome x) => x != supportedOutcome).TotalSupportPoints : (supportedOutcome.TotalSupportPoints / supportedOutcome.WinChance));
			int num2 = MathF.Max(0, (int)(supportWeight - 1));
			supportedOutcome.TotalSupportPoints += num2;
			num += (float)num2;
			return supportedOutcome.TotalSupportPoints / num;
		}
		return 0f;
	}

	public int GetInfluenceCostOfOutcome(DecisionOutcome outcome, Clan supporter, Supporter.SupportWeights weight)
	{
		return _decision.GetInfluenceCostOfSupport(supporter, weight);
	}

	public TextObject GetSecondaryEffects()
	{
		return _decision.GetSecondaryEffects();
	}

	public void OnPlayerSupport(DecisionOutcome decisionOutcome, Supporter.SupportWeights supportWeight)
	{
		if (!IsPlayerChooser)
		{
			foreach (DecisionOutcome possibleOutcome in _possibleOutcomes)
			{
				possibleOutcome.ResetSupport(PlayerAsSupporter);
			}
			_hasPlayerVoted = true;
			if (decisionOutcome != null)
			{
				PlayerAsSupporter.SupportWeight = supportWeight;
				decisionOutcome.AddSupport(PlayerAsSupporter);
			}
		}
		else
		{
			_chosenOutcome = decisionOutcome;
		}
	}

	public void OnPlayerAbstainedAsRuler()
	{
		_chosenOutcome = GetAiChoice(_possibleOutcomes);
	}

	public void ApplySelection()
	{
		if (!IsCancelled)
		{
			if (_chooser != Clan.PlayerClan)
			{
				ReadyToAiChoose();
			}
			else
			{
				ApplyChosenOutcome();
			}
		}
	}

	public MBList<DecisionOutcome> GetSortedDecisionOutcomes()
	{
		return _decision.SortDecisionOutcomes(_possibleOutcomes);
	}

	public TextObject GetGeneralTitle()
	{
		return _decision.GetGeneralTitle();
	}

	public TextObject GetTitle()
	{
		if (IsPlayerChooser)
		{
			return _decision.GetChooseTitle();
		}
		return _decision.GetSupportTitle();
	}

	public TextObject GetDescription()
	{
		if (IsPlayerChooser)
		{
			return _decision.GetChooseDescription();
		}
		return _decision.GetSupportDescription();
	}
}
