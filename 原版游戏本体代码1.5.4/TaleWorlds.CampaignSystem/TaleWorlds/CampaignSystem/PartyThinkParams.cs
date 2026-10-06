using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class PartyThinkParams
{
	public MobileParty MobilePartyOf;

	private readonly MBList<(AIBehaviorData, float)> _aiBehaviorScores;

	private MBList<MobileParty> _possibleArmyMembersUponArmyCreation;

	public float CurrentObjectiveValue;

	public bool WillGatherAnArmy;

	public bool DoNotChangeBehavior;

	public float StrengthOfLordsWithoutArmy;

	public float StrengthOfLordsWithArmy;

	public float StrengthOfLordsAtSameClanWithoutArmy;

	public MBReadOnlyList<(AIBehaviorData, float)> AIBehaviorScores => _aiBehaviorScores;

	public MBReadOnlyList<MobileParty> PossibleArmyMembersUponArmyCreation => _possibleArmyMembersUponArmyCreation;

	public PartyThinkParams(MobileParty mobileParty)
	{
		_aiBehaviorScores = new MBList<(AIBehaviorData, float)>(32);
		_possibleArmyMembersUponArmyCreation = null;
		MobilePartyOf = mobileParty;
		WillGatherAnArmy = false;
		DoNotChangeBehavior = false;
		CurrentObjectiveValue = 0f;
	}

	public void Reset(MobileParty mobileParty)
	{
		_aiBehaviorScores.Clear();
		_possibleArmyMembersUponArmyCreation?.Clear();
		MobilePartyOf = mobileParty;
		WillGatherAnArmy = false;
		DoNotChangeBehavior = false;
		CurrentObjectiveValue = 0f;
		StrengthOfLordsWithoutArmy = 0f;
		StrengthOfLordsWithArmy = 0f;
		StrengthOfLordsAtSameClanWithoutArmy = 0f;
	}

	public void Initialization()
	{
		StrengthOfLordsWithoutArmy = 0f;
		StrengthOfLordsWithArmy = 0f;
		StrengthOfLordsAtSameClanWithoutArmy = 0f;
		foreach (Hero hero in MobilePartyOf.MapFaction.Heroes)
		{
			if (hero.PartyBelongedTo == null)
			{
				continue;
			}
			MobileParty partyBelongedTo = hero.PartyBelongedTo;
			if (partyBelongedTo.Army != null)
			{
				StrengthOfLordsWithArmy += partyBelongedTo.Party.EstimatedStrength;
				continue;
			}
			StrengthOfLordsWithoutArmy += partyBelongedTo.Party.EstimatedStrength;
			if (hero.Clan == MobilePartyOf.LeaderHero?.Clan)
			{
				StrengthOfLordsAtSameClanWithoutArmy += partyBelongedTo.Party.EstimatedStrength;
			}
		}
	}

	public void SetArmyMembers(MBList<MobileParty> armyMembers)
	{
		_possibleArmyMembersUponArmyCreation = armyMembers;
	}

	public bool TryGetBehaviorScore(in AIBehaviorData aiBehaviorData, out float score)
	{
		foreach (var aiBehaviorScore in _aiBehaviorScores)
		{
			var (aIBehaviorData, _) = aiBehaviorScore;
			if (aIBehaviorData.Equals(aiBehaviorData))
			{
				score = aiBehaviorScore.Item2;
				return true;
			}
		}
		score = 0f;
		return false;
	}

	public void SetBehaviorScore(in AIBehaviorData aiBehaviorData, float score)
	{
		for (int i = 0; i < _aiBehaviorScores.Count; i++)
		{
			if (_aiBehaviorScores[i].Item1.Equals(aiBehaviorData))
			{
				_aiBehaviorScores[i] = (_aiBehaviorScores[i].Item1, score);
				return;
			}
		}
		Debug.FailedAssert("AIBehaviorScore not found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\ICampaignBehaviorManager.cs", "SetBehaviorScore", 196);
	}

	public void AddBehaviorScore(in (AIBehaviorData, float) value)
	{
		_aiBehaviorScores.Add(value);
	}
}
