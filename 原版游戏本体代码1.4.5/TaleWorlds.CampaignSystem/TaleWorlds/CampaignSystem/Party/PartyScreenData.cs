using System;
using System.Collections;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Party;

public class PartyScreenData : IEnumerable<(TroopRosterElement, bool)>, IEnumerable
{
	public TroopRoster RightMemberRoster;

	public TroopRoster LeftMemberRoster;

	public TroopRoster RightPrisonerRoster;

	public TroopRoster LeftPrisonerRoster;

	public ItemRoster RightItemRoster;

	public Dictionary<CharacterObject, int> RightRecruitableData;

	public int PartyGoldChangeAmount;

	public (int, int, int) PartyInfluenceChangeAmount;

	public int PartyMoraleChangeAmount;

	public int PartyHorseChangeAmount;

	public List<Tuple<CharacterObject, CharacterObject, int>> UpgradedTroopsHistory;

	public List<Tuple<CharacterObject, int>> TransferredPrisonersHistory;

	public List<Tuple<CharacterObject, int>> RecruitedPrisonersHistory;

	public List<Tuple<EquipmentElement, int>> UsedUpgradeHorsesHistory;

	public PartyBase RightParty { get; private set; }

	public PartyBase LeftParty { get; private set; }

	public Hero RightPartyLeaderHero { get; private set; }

	public Hero LeftPartyLeaderHero { get; private set; }

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public PartyScreenData()
	{
		PartyGoldChangeAmount = 0;
		PartyInfluenceChangeAmount = (0, 0, 0);
		PartyMoraleChangeAmount = 0;
		PartyHorseChangeAmount = 0;
		RightRecruitableData = new Dictionary<CharacterObject, int>();
		UpgradedTroopsHistory = new List<Tuple<CharacterObject, CharacterObject, int>>();
		TransferredPrisonersHistory = new List<Tuple<CharacterObject, int>>();
		RecruitedPrisonersHistory = new List<Tuple<CharacterObject, int>>();
		UsedUpgradeHorsesHistory = new List<Tuple<EquipmentElement, int>>();
	}

	public void InitializeCopyFrom(PartyBase rightParty, PartyBase leftParty)
	{
		if (rightParty != null)
		{
			RightParty = rightParty;
			RightPartyLeaderHero = rightParty.LeaderHero;
		}
		if (leftParty != null)
		{
			LeftParty = leftParty;
			LeftPartyLeaderHero = leftParty.LeaderHero;
		}
		RightMemberRoster = TroopRoster.CreateDummyTroopRoster();
		LeftMemberRoster = TroopRoster.CreateDummyTroopRoster();
		RightPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
		LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster();
		RightItemRoster = new ItemRoster();
	}

	public void CopyFromPartyAndRoster(TroopRoster rightPartyMemberRoster, TroopRoster rightPartyPrisonerRoster, TroopRoster leftPartyMemberRoster, TroopRoster leftPartyPrisonerRoster, PartyBase rightParty)
	{
		PrisonerRecruitmentCalculationModel prisonerRecruitmentCalculationModel = Campaign.Current.Models.PrisonerRecruitmentCalculationModel;
		for (int i = 0; i < rightPartyMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = rightPartyMemberRoster.GetElementCopyAtIndex(i);
			RightMemberRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber, elementCopyAtIndex.Xp);
		}
		for (int j = 0; j < leftPartyMemberRoster.Count; j++)
		{
			TroopRosterElement elementCopyAtIndex2 = leftPartyMemberRoster.GetElementCopyAtIndex(j);
			LeftMemberRoster.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber, elementCopyAtIndex2.Xp);
		}
		RightRecruitableData.Clear();
		for (int k = 0; k < rightPartyPrisonerRoster.Count; k++)
		{
			TroopRosterElement elementCopyAtIndex3 = rightPartyPrisonerRoster.GetElementCopyAtIndex(k);
			RightPrisonerRoster.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.Xp);
			if (rightParty != null && rightParty.MobileParty?.IsMainParty == true)
			{
				int value = prisonerRecruitmentCalculationModel.CalculateRecruitableNumber(PartyBase.MainParty, elementCopyAtIndex3.Character);
				if (!RightRecruitableData.ContainsKey(elementCopyAtIndex3.Character))
				{
					RightRecruitableData.Add(elementCopyAtIndex3.Character, value);
				}
			}
		}
		for (int l = 0; l < leftPartyPrisonerRoster.Count; l++)
		{
			TroopRosterElement elementCopyAtIndex4 = leftPartyPrisonerRoster.GetElementCopyAtIndex(l);
			LeftPrisonerRoster.AddToCounts(elementCopyAtIndex4.Character, elementCopyAtIndex4.Number, insertAtFront: false, elementCopyAtIndex4.WoundedNumber, elementCopyAtIndex4.Xp);
		}
		if (rightParty != null)
		{
			for (int m = 0; m < rightParty.ItemRoster.Count; m++)
			{
				ItemRosterElement elementCopyAtIndex5 = rightParty.ItemRoster.GetElementCopyAtIndex(m);
				RightItemRoster.AddToCounts(elementCopyAtIndex5.EquipmentElement, elementCopyAtIndex5.Amount);
			}
		}
	}

	public void CopyFromScreenData(PartyScreenData data)
	{
		RightMemberRoster.Clear();
		for (int i = 0; i < data.RightMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = data.RightMemberRoster.GetElementCopyAtIndex(i);
			RightMemberRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber, elementCopyAtIndex.Xp);
		}
		RightPrisonerRoster.Clear();
		for (int j = 0; j < data.RightPrisonerRoster.Count; j++)
		{
			TroopRosterElement elementCopyAtIndex2 = data.RightPrisonerRoster.GetElementCopyAtIndex(j);
			RightPrisonerRoster.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber, elementCopyAtIndex2.Xp);
		}
		RightItemRoster.Clear();
		if (data.RightItemRoster != null)
		{
			for (int k = 0; k < data.RightItemRoster.Count; k++)
			{
				ItemRosterElement elementCopyAtIndex3 = data.RightItemRoster.GetElementCopyAtIndex(k);
				RightItemRoster.AddToCounts(elementCopyAtIndex3.EquipmentElement, elementCopyAtIndex3.Amount);
			}
		}
		LeftMemberRoster.Clear();
		for (int l = 0; l < data.LeftMemberRoster.Count; l++)
		{
			TroopRosterElement elementCopyAtIndex4 = data.LeftMemberRoster.GetElementCopyAtIndex(l);
			LeftMemberRoster.AddToCounts(elementCopyAtIndex4.Character, elementCopyAtIndex4.Number, insertAtFront: false, elementCopyAtIndex4.WoundedNumber, elementCopyAtIndex4.Xp);
		}
		LeftPrisonerRoster.Clear();
		for (int m = 0; m < data.LeftPrisonerRoster.Count; m++)
		{
			TroopRosterElement elementCopyAtIndex5 = data.LeftPrisonerRoster.GetElementCopyAtIndex(m);
			LeftPrisonerRoster.AddToCounts(elementCopyAtIndex5.Character, elementCopyAtIndex5.Number, insertAtFront: false, elementCopyAtIndex5.WoundedNumber, elementCopyAtIndex5.Xp);
		}
		PartyGoldChangeAmount = data.PartyGoldChangeAmount;
		PartyInfluenceChangeAmount = data.PartyInfluenceChangeAmount;
		PartyMoraleChangeAmount = data.PartyMoraleChangeAmount;
		PartyHorseChangeAmount = data.PartyHorseChangeAmount;
		RightRecruitableData = new Dictionary<CharacterObject, int>(data.RightRecruitableData);
		UpgradedTroopsHistory = new List<Tuple<CharacterObject, CharacterObject, int>>(data.UpgradedTroopsHistory);
		TransferredPrisonersHistory = new List<Tuple<CharacterObject, int>>(data.TransferredPrisonersHistory);
		RecruitedPrisonersHistory = new List<Tuple<CharacterObject, int>>(data.RecruitedPrisonersHistory);
		UsedUpgradeHorsesHistory = new List<Tuple<EquipmentElement, int>>(data.UsedUpgradeHorsesHistory);
	}

	public void BindRostersFrom(TroopRoster rightPartyMemberRoster, TroopRoster rightPartyPrisonerRoster, TroopRoster leftPartyMemberRoster, TroopRoster leftPartyPrisonerRoster, PartyBase rightParty, PartyBase leftParty)
	{
		RightParty = rightParty;
		LeftParty = leftParty;
		if (rightParty != null)
		{
			RightItemRoster = rightParty.ItemRoster;
			RightPartyLeaderHero = rightParty.LeaderHero;
		}
		if (leftParty != null)
		{
			LeftPartyLeaderHero = leftParty.LeaderHero;
		}
		RightMemberRoster = rightPartyMemberRoster;
		LeftMemberRoster = leftPartyMemberRoster;
		RightPrisonerRoster = rightPartyPrisonerRoster;
		LeftPrisonerRoster = leftPartyPrisonerRoster;
		if (rightParty == null || rightParty.MobileParty?.IsMainParty != true)
		{
			return;
		}
		RightRecruitableData = new Dictionary<CharacterObject, int>();
		PrisonerRecruitmentCalculationModel prisonerRecruitmentCalculationModel = Campaign.Current.Models.PrisonerRecruitmentCalculationModel;
		foreach (TroopRosterElement item in rightParty.PrisonRoster.GetTroopRoster())
		{
			int value = prisonerRecruitmentCalculationModel.CalculateRecruitableNumber(PartyBase.MainParty, item.Character);
			if (!RightRecruitableData.ContainsKey(item.Character))
			{
				RightRecruitableData.Add(item.Character, value);
			}
		}
	}

	private List<Tuple<Hero, List<PartyRole>>> GetPartyHeroesWithPerks(TroopRoster roster)
	{
		MobileParty mobileParty = roster?.OwnerParty?.MobileParty;
		if (mobileParty == null)
		{
			return null;
		}
		List<Tuple<Hero, List<PartyRole>>> list = new List<Tuple<Hero, List<PartyRole>>>();
		for (int i = 0; i < roster.Count; i++)
		{
			Hero hero = roster.GetCharacterAtIndex(i)?.HeroObject;
			if (hero != null)
			{
				List<PartyRole> heroPartyRoles = mobileParty.GetHeroPartyRoles(hero);
				if (heroPartyRoles.Count > 0)
				{
					list.Add(new Tuple<Hero, List<PartyRole>>(hero, heroPartyRoles));
				}
			}
		}
		return list;
	}

	public void ResetUsing(PartyScreenData partyScreenData)
	{
		List<Tuple<Hero, List<PartyRole>>> partyHeroesWithPerks = GetPartyHeroesWithPerks(LeftMemberRoster);
		List<Tuple<Hero, List<PartyRole>>> partyHeroesWithPerks2 = GetPartyHeroesWithPerks(RightMemberRoster);
		RightMemberRoster.Clear();
		RightMemberRoster.RemoveZeroCounts();
		for (int i = 0; i < partyScreenData.RightMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = partyScreenData.RightMemberRoster.GetElementCopyAtIndex(i);
			RightMemberRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber, elementCopyAtIndex.Xp);
		}
		if (RightParty?.MobileParty != null && RightParty.MobileParty.LeaderHero != partyScreenData.RightPartyLeaderHero)
		{
			RightParty.MobileParty.ChangePartyLeader(partyScreenData.RightPartyLeaderHero);
		}
		LeftMemberRoster.Clear();
		LeftMemberRoster.RemoveZeroCounts();
		for (int j = 0; j < partyScreenData.LeftMemberRoster.Count; j++)
		{
			TroopRosterElement elementCopyAtIndex2 = partyScreenData.LeftMemberRoster.GetElementCopyAtIndex(j);
			LeftMemberRoster.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber, elementCopyAtIndex2.Xp);
		}
		if (LeftParty?.MobileParty != null && LeftParty.MobileParty.LeaderHero != partyScreenData.LeftPartyLeaderHero)
		{
			LeftParty.MobileParty.ChangePartyLeader(partyScreenData.LeftPartyLeaderHero);
		}
		RightPrisonerRoster.Clear();
		LeftPrisonerRoster.Clear();
		RightPrisonerRoster.RemoveZeroCounts();
		for (int k = 0; k < partyScreenData.RightPrisonerRoster.Count; k++)
		{
			TroopRosterElement elementCopyAtIndex3 = partyScreenData.RightPrisonerRoster.GetElementCopyAtIndex(k);
			RightPrisonerRoster.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.Xp);
		}
		LeftPrisonerRoster.RemoveZeroCounts();
		for (int l = 0; l < partyScreenData.LeftPrisonerRoster.Count; l++)
		{
			TroopRosterElement elementCopyAtIndex4 = partyScreenData.LeftPrisonerRoster.GetElementCopyAtIndex(l);
			LeftPrisonerRoster.AddToCounts(elementCopyAtIndex4.Character, elementCopyAtIndex4.Number, insertAtFront: false, elementCopyAtIndex4.WoundedNumber, elementCopyAtIndex4.Xp);
		}
		if (RightItemRoster != null)
		{
			RightItemRoster.Clear();
			for (int m = 0; m < partyScreenData.RightItemRoster.Count; m++)
			{
				ItemRosterElement elementCopyAtIndex5 = partyScreenData.RightItemRoster.GetElementCopyAtIndex(m);
				RightItemRoster.AddToCounts(elementCopyAtIndex5.EquipmentElement, elementCopyAtIndex5.Amount);
			}
		}
		PartyGoldChangeAmount = partyScreenData.PartyGoldChangeAmount;
		PartyInfluenceChangeAmount = partyScreenData.PartyInfluenceChangeAmount;
		PartyMoraleChangeAmount = partyScreenData.PartyMoraleChangeAmount;
		PartyHorseChangeAmount = partyScreenData.PartyHorseChangeAmount;
		RightRecruitableData = new Dictionary<CharacterObject, int>(partyScreenData.RightRecruitableData);
		UpgradedTroopsHistory = new List<Tuple<CharacterObject, CharacterObject, int>>(partyScreenData.UpgradedTroopsHistory);
		TransferredPrisonersHistory = new List<Tuple<CharacterObject, int>>(partyScreenData.TransferredPrisonersHistory);
		RecruitedPrisonersHistory = new List<Tuple<CharacterObject, int>>(partyScreenData.RecruitedPrisonersHistory);
		UsedUpgradeHorsesHistory = new List<Tuple<EquipmentElement, int>>(partyScreenData.UsedUpgradeHorsesHistory);
		if (partyHeroesWithPerks != null && LeftParty?.MobileParty != null)
		{
			for (int n = 0; n < partyHeroesWithPerks.Count; n++)
			{
				foreach (PartyRole item in partyHeroesWithPerks[n].Item2)
				{
					LeftParty.MobileParty.SetHeroPartyRole(partyHeroesWithPerks[n].Item1, item);
				}
			}
		}
		if (partyHeroesWithPerks2 == null || RightParty?.MobileParty == null)
		{
			return;
		}
		for (int num = 0; num < partyHeroesWithPerks2.Count; num++)
		{
			foreach (PartyRole item2 in partyHeroesWithPerks2[num].Item2)
			{
				RightParty.MobileParty.SetHeroPartyRole(partyHeroesWithPerks2[num].Item1, item2);
			}
		}
	}

	public bool IsThereAnyTroopTradeDifferenceBetween(PartyScreenData other)
	{
		MBList<TroopRosterElement> troopRoster = RightMemberRoster.GetTroopRoster();
		MBList<TroopRosterElement> troopRoster2 = other.RightMemberRoster.GetTroopRoster();
		if (troopRoster.Count != troopRoster2.Count)
		{
			return true;
		}
		foreach (TroopRosterElement elem in troopRoster)
		{
			if (troopRoster2.FindIndex((TroopRosterElement x) => x.Character == elem.Character && x.Number == elem.Number) == -1)
			{
				return true;
			}
		}
		MBList<TroopRosterElement> troopRoster3 = RightPrisonerRoster.GetTroopRoster();
		MBList<TroopRosterElement> troopRoster4 = other.RightPrisonerRoster.GetTroopRoster();
		if (troopRoster3.Count != troopRoster4.Count)
		{
			return true;
		}
		foreach (TroopRosterElement elem2 in troopRoster3)
		{
			if (troopRoster4.FindIndex((TroopRosterElement x) => x.Character == elem2.Character && x.Number == elem2.Number) == -1)
			{
				return true;
			}
		}
		return false;
	}

	public List<TroopTradeDifference> GetTroopTradeDifferencesFromTo(PartyScreenData toPartyScreenData, PartyScreenLogic.PartyRosterSide side = PartyScreenLogic.PartyRosterSide.None)
	{
		List<TroopTradeDifference> list = new List<TroopTradeDifference>();
		Debug.Print("Current settlement: " + Settlement.CurrentSettlement?.StringId);
		Debug.Print("Left party id: " + toPartyScreenData.LeftParty?.MobileParty?.StringId);
		Debug.Print("Right party id: " + toPartyScreenData.RightParty?.MobileParty?.StringId);
		if (side == PartyScreenLogic.PartyRosterSide.None || side == PartyScreenLogic.PartyRosterSide.Right)
		{
			using (IEnumerator<(TroopRosterElement, bool)> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					(TroopRosterElement, bool) current = enumerator.Current;
					TroopRosterElement item = current.Item1;
					int number = item.Number;
					int num = 0;
					foreach (var toPartyScreenDatum in toPartyScreenData)
					{
						if (toPartyScreenDatum.Item1.Character == current.Item1.Character && toPartyScreenDatum.Item2 == current.Item2)
						{
							int num2 = num;
							item = toPartyScreenDatum.Item1;
							num = num2 + item.Number;
						}
					}
					if (number != num)
					{
						TroopTradeDifference item2 = new TroopTradeDifference
						{
							Troop = current.Item1.Character,
							ToCount = num,
							FromCount = number,
							IsPrisoner = current.Item2
						};
						list.Add(item2);
					}
					Debug.Print("currently owned: " + number + ", previously owned: " + num + " name: " + current.Item1.Character.StringId);
				}
			}
			foreach (var toPartyScreenDatum2 in toPartyScreenData)
			{
				TroopRosterElement item = toPartyScreenDatum2.Item1;
				int number2 = item.Number;
				int num3 = 0;
				using (IEnumerator<(TroopRosterElement, bool)> enumerator2 = GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						(TroopRosterElement, bool) current4 = enumerator2.Current;
						if (toPartyScreenDatum2.Item1.Character == current4.Item1.Character && toPartyScreenDatum2.Item2 == current4.Item2)
						{
							int num4 = num3;
							item = toPartyScreenDatum2.Item1;
							num3 = num4 + item.Number;
						}
					}
				}
				if (num3 != number2)
				{
					TroopTradeDifference item3 = new TroopTradeDifference
					{
						Troop = toPartyScreenDatum2.Item1.Character,
						ToCount = number2,
						FromCount = num3,
						IsPrisoner = toPartyScreenDatum2.Item2
					};
					if (!list.Contains(item3))
					{
						list.Add(item3);
						Debug.Print("currently owned: " + num3 + ", previously owned: " + number2 + " name: " + toPartyScreenDatum2.Item1.Character.StringId);
					}
				}
			}
		}
		else
		{
			foreach (var leftSideElement in GetLeftSideElements())
			{
				TroopRosterElement item = leftSideElement.Item1;
				int number3 = item.Number;
				int num5 = 0;
				foreach (var leftSideElement2 in toPartyScreenData.GetLeftSideElements())
				{
					if (leftSideElement2.Item1.Character == leftSideElement.Item1.Character && leftSideElement2.Item2 == leftSideElement.Item2)
					{
						int num6 = num5;
						item = leftSideElement2.Item1;
						num5 = num6 + item.Number;
					}
				}
				if (number3 != num5)
				{
					TroopTradeDifference item4 = new TroopTradeDifference
					{
						Troop = leftSideElement.Item1.Character,
						ToCount = num5,
						FromCount = number3,
						IsPrisoner = leftSideElement.Item2
					};
					list.Add(item4);
				}
				Debug.Print("currently owned: " + number3 + ", previously owned: " + num5 + " name: " + leftSideElement.Item1.Character.StringId);
			}
			foreach (var leftSideElement3 in toPartyScreenData.GetLeftSideElements())
			{
				TroopRosterElement item = leftSideElement3.Item1;
				int number4 = item.Number;
				int num7 = 0;
				foreach (var leftSideElement4 in GetLeftSideElements())
				{
					if (leftSideElement3.Item1.Character == leftSideElement4.Item1.Character && leftSideElement3.Item2 == leftSideElement4.Item2)
					{
						int num8 = num7;
						item = leftSideElement3.Item1;
						num7 = num8 + item.Number;
					}
				}
				if (num7 != number4)
				{
					TroopTradeDifference item5 = new TroopTradeDifference
					{
						Troop = leftSideElement3.Item1.Character,
						ToCount = number4,
						FromCount = num7,
						IsPrisoner = leftSideElement3.Item2
					};
					if (!list.Contains(item5))
					{
						list.Add(item5);
						Debug.Print("currently owned: " + num7 + ", previously owned: " + number4 + " name: " + leftSideElement3.Item1.Character.StringId);
					}
				}
			}
		}
		return list;
	}

	private List<(TroopRosterElement, bool)> GetLeftSideElements()
	{
		List<(TroopRosterElement, bool)> list = new List<(TroopRosterElement, bool)>();
		for (int i = 0; i < LeftMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = LeftMemberRoster.GetElementCopyAtIndex(i);
			list.Add((elementCopyAtIndex, false));
		}
		for (int j = 0; j < LeftPrisonerRoster.Count; j++)
		{
			TroopRosterElement elementCopyAtIndex2 = LeftPrisonerRoster.GetElementCopyAtIndex(j);
			list.Add((elementCopyAtIndex2, true));
		}
		return list;
	}

	private IEnumerator<(TroopRosterElement, bool)> EnumerateElements()
	{
		for (int i = 0; i < RightMemberRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = RightMemberRoster.GetElementCopyAtIndex(i);
			yield return (elementCopyAtIndex, false);
		}
		for (int i = 0; i < RightPrisonerRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex2 = RightPrisonerRoster.GetElementCopyAtIndex(i);
			yield return (elementCopyAtIndex2, true);
		}
	}

	public IEnumerator<(TroopRosterElement, bool)> GetEnumerator()
	{
		return EnumerateElements();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return EnumerateElements();
	}

	public override bool Equals(object obj)
	{
		return this == obj;
	}

	public static bool operator ==(PartyScreenData a, PartyScreenData b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a.PartyGoldChangeAmount != b.PartyGoldChangeAmount || a.PartyInfluenceChangeAmount.Item1 != b.PartyInfluenceChangeAmount.Item1 || a.PartyInfluenceChangeAmount.Item2 != b.PartyInfluenceChangeAmount.Item2 || a.PartyInfluenceChangeAmount.Item3 != b.PartyInfluenceChangeAmount.Item3 || a.PartyMoraleChangeAmount != b.PartyMoraleChangeAmount || a.PartyHorseChangeAmount != b.PartyHorseChangeAmount)
		{
			return false;
		}
		if (a.RightMemberRoster.Count != b.RightMemberRoster.Count || a.RightPrisonerRoster.Count != b.RightPrisonerRoster.Count || a.RightRecruitableData.Count != b.RightRecruitableData.Count || a.UpgradedTroopsHistory.Count != b.UpgradedTroopsHistory.Count || a.TransferredPrisonersHistory.Count != b.TransferredPrisonersHistory.Count || a.RecruitedPrisonersHistory.Count != b.RecruitedPrisonersHistory.Count || a.UsedUpgradeHorsesHistory.Count != b.UsedUpgradeHorsesHistory.Count)
		{
			return false;
		}
		if (!TroopRoster.RostersAreIdentical(a.RightMemberRoster, b.RightMemberRoster))
		{
			return false;
		}
		if (!TroopRoster.RostersAreIdentical(a.RightPrisonerRoster, b.LeftPrisonerRoster))
		{
			return false;
		}
		foreach (CharacterObject key in a.RightRecruitableData.Keys)
		{
			if (!b.RightRecruitableData.ContainsKey(key) || a.RightRecruitableData[key] != b.RightRecruitableData[key])
			{
				return false;
			}
		}
		for (int i = 0; i < a.UpgradedTroopsHistory.Count; i++)
		{
			if (a.UpgradedTroopsHistory[i].Item1 != b.UpgradedTroopsHistory[i].Item1 || a.UpgradedTroopsHistory[i].Item2 != b.UpgradedTroopsHistory[i].Item2 || a.UpgradedTroopsHistory[i].Item3 != b.UpgradedTroopsHistory[i].Item3)
			{
				return false;
			}
		}
		for (int j = 0; j < a.TransferredPrisonersHistory.Count; j++)
		{
			if (a.TransferredPrisonersHistory[j].Item1 != b.TransferredPrisonersHistory[j].Item1 || a.TransferredPrisonersHistory[j].Item2 != b.TransferredPrisonersHistory[j].Item2)
			{
				return false;
			}
		}
		for (int k = 0; k < a.RecruitedPrisonersHistory.Count; k++)
		{
			if (a.RecruitedPrisonersHistory[k].Item1 != b.RecruitedPrisonersHistory[k].Item1 || a.RecruitedPrisonersHistory[k].Item2 != b.RecruitedPrisonersHistory[k].Item2)
			{
				return false;
			}
		}
		for (int l = 0; l < a.UsedUpgradeHorsesHistory.Count; l++)
		{
			if (a.UsedUpgradeHorsesHistory[l].Item1.Item != b.UsedUpgradeHorsesHistory[l].Item1.Item || a.UsedUpgradeHorsesHistory[l].Item2 != b.UsedUpgradeHorsesHistory[l].Item2)
			{
				return false;
			}
		}
		return true;
	}

	public static bool operator !=(PartyScreenData first, PartyScreenData second)
	{
		return !(first == second);
	}
}
