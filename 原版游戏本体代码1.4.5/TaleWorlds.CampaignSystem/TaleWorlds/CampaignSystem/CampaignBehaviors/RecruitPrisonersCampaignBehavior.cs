using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class RecruitPrisonersCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnMainPartyPrisonerRecruitedEvent.AddNonSerializedListener(this, OnMainPartyPrisonerRecruited);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickAIMobileParty);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickMainParty);
	}

	private void HourlyTickMainParty()
	{
		MobileParty mainParty = MobileParty.MainParty;
		TroopRoster memberRoster = mainParty.MemberRoster;
		TroopRoster prisonRoster = mainParty.PrisonRoster;
		if (memberRoster.Count == 0 || memberRoster.TotalManCount <= 0 || prisonRoster.Count == 0 || prisonRoster.TotalRegulars <= 0 || mainParty.MapEvent != null)
		{
			return;
		}
		int num = MBRandom.RandomInt(0, prisonRoster.Count);
		bool flag = false;
		for (int i = num; i < prisonRoster.Count + num; i++)
		{
			int index = i % prisonRoster.Count;
			CharacterObject characterAtIndex = prisonRoster.GetCharacterAtIndex(index);
			if (characterAtIndex.IsRegular)
			{
				CharacterObject characterObject = characterAtIndex;
				int elementNumber = mainParty.PrisonRoster.GetElementNumber(index);
				int num2 = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.CalculateRecruitableNumber(mainParty.Party, characterObject);
				if (!flag && num2 < elementNumber)
				{
					flag = GenerateConformityForTroop(mainParty, characterObject);
				}
			}
			if (flag)
			{
				break;
			}
		}
	}

	private void DailyTickAIMobileParty(MobileParty mobileParty)
	{
		if (mobileParty.IsMainParty || !mobileParty.IsLordParty || mobileParty.MapEvent != null)
		{
			return;
		}
		TroopRoster prisonRoster = mobileParty.PrisonRoster;
		if (prisonRoster.Count == 0 || prisonRoster.TotalRegulars <= 0)
		{
			return;
		}
		int num = MBRandom.RandomInt(0, prisonRoster.Count);
		bool flag = false;
		for (int i = num; i < prisonRoster.Count + num; i++)
		{
			int index = i % prisonRoster.Count;
			CharacterObject characterAtIndex = prisonRoster.GetCharacterAtIndex(index);
			if (!characterAtIndex.IsRegular)
			{
				continue;
			}
			CharacterObject characterObject = characterAtIndex;
			int elementNumber = mobileParty.PrisonRoster.GetElementNumber(index);
			int num2 = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.CalculateRecruitableNumber(mobileParty.Party, characterObject);
			if (!flag && num2 < elementNumber)
			{
				flag = GenerateConformityForTroop(mobileParty, characterObject, CampaignTime.HoursInDay);
			}
			if (Campaign.Current.Models.PrisonerRecruitmentCalculationModel.ShouldPartyRecruitPrisoners(mobileParty.Party))
			{
				if (IsPrisonerRecruitable(mobileParty, characterObject, out var conformityNeeded))
				{
					int num3 = mobileParty.Party.PartySizeLimit - mobileParty.MemberRoster.TotalManCount;
					int a = MathF.Min((num3 > 0) ? ((num3 > num2) ? num2 : num3) : 0, prisonRoster.GetElementNumber(characterObject));
					int characterWage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(characterObject);
					a = MathF.Min(a, mobileParty.GetAvailableWageBudget() / characterWage);
					if (a > 0)
					{
						RecruitPrisonersAi(mobileParty, characterObject, a, conformityNeeded);
					}
				}
			}
			else if (flag)
			{
				break;
			}
		}
	}

	private bool GenerateConformityForTroop(MobileParty mobileParty, CharacterObject troop, int hours = 1)
	{
		int xpAmount = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetConformityChangePerHour(mobileParty.Party, troop).RoundedResultNumber * hours;
		mobileParty.PrisonRoster.AddXpToTroop(troop, xpAmount);
		return true;
	}

	private void ApplyPrisonerRecruitmentEffects(MobileParty mobileParty, CharacterObject troop, int num)
	{
		int prisonerRecruitmentMoraleEffect = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.GetPrisonerRecruitmentMoraleEffect(mobileParty.Party, troop, num);
		mobileParty.RecentEventsMorale += prisonerRecruitmentMoraleEffect;
	}

	private void RecruitPrisonersAi(MobileParty mobileParty, CharacterObject troop, int num, int conformityCost)
	{
		mobileParty.PrisonRoster.GetElementNumber(troop);
		mobileParty.PrisonRoster.GetElementXp(troop);
		mobileParty.PrisonRoster.AddToCounts(troop, -num, insertAtFront: false, 0, -conformityCost * num);
		mobileParty.MemberRoster.AddToCounts(troop, num);
		CampaignEventDispatcher.Instance.OnTroopRecruited(mobileParty.LeaderHero, null, null, troop, num);
		ApplyPrisonerRecruitmentEffects(mobileParty, troop, num);
	}

	private bool IsPrisonerRecruitable(MobileParty mobileParty, CharacterObject character, out int conformityNeeded)
	{
		return Campaign.Current.Models.PrisonerRecruitmentCalculationModel.IsPrisonerRecruitable(mobileParty.Party, character, out conformityNeeded);
	}

	private void OnMainPartyPrisonerRecruited(FlattenedTroopRoster flattenedTroopRosters)
	{
		foreach (CharacterObject troop in flattenedTroopRosters.Troops)
		{
			CampaignEventDispatcher.Instance.OnUnitRecruited(troop, 1);
			ApplyPrisonerRecruitmentEffects(MobileParty.MainParty, troop, 1);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
