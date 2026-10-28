using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class TroopUpgradeTracker
{
	private Dictionary<Tuple<PartyBase, CharacterObject>, int> _upgradedRegulars = new Dictionary<Tuple<PartyBase, CharacterObject>, int>();

	private List<MapEventParty> _mapEventParties = new List<MapEventParty>();

	private Dictionary<Hero, int[]> _heroSkills = new Dictionary<Hero, int[]>();

	public void AddParty(MapEventParty mapEventParty)
	{
		_mapEventParties.Add(mapEventParty);
	}

	public void RemoveParty(MapEventParty mapEventParty)
	{
		_mapEventParties.Add(mapEventParty);
	}

	public void AddTrackedTroop(PartyBase party, CharacterObject character)
	{
		if (character.IsHero)
		{
			int count = Skills.All.Count;
			int[] array = new int[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = character.GetSkillValue(Skills.All[i]);
			}
			_heroSkills[character.HeroObject] = array;
		}
		else
		{
			int num = party.MemberRoster.FindIndexOfTroop(character);
			if (num >= 0)
			{
				TroopRosterElement el = party.MemberRoster.GetElementCopyAtIndex(num);
				int value = CalculateReadyToUpgradeSafe(ref el, party);
				_upgradedRegulars[new Tuple<PartyBase, CharacterObject>(party, character)] = value;
			}
		}
	}

	public IEnumerable<SkillObject> CheckSkillUpgrades(Hero hero)
	{
		if (_heroSkills.IsEmpty() || !_heroSkills.ContainsKey(hero))
		{
			yield break;
		}
		int[] oldSkillLevels = _heroSkills[hero];
		int i = 0;
		while (i < Skills.All.Count)
		{
			SkillObject skill = Skills.All[i];
			int newSkillLevel = hero.CharacterObject.GetSkillValue(skill);
			while (newSkillLevel > oldSkillLevels[i])
			{
				oldSkillLevels[i]++;
				yield return skill;
			}
			int num = i + 1;
			i = num;
		}
	}

	public int CheckUpgradedCount(PartyBase party, CharacterObject character)
	{
		int result = 0;
		if (!character.IsHero && party != null)
		{
			int num = party.MemberRoster.FindIndexOfTroop(character);
			int value2;
			if (num >= 0)
			{
				TroopRosterElement el = party.MemberRoster.GetElementCopyAtIndex(num);
				int num2 = CalculateReadyToUpgradeSafe(ref el, party);
				if (_upgradedRegulars.TryGetValue(new Tuple<PartyBase, CharacterObject>(party, character), out var value) && num2 > value)
				{
					value = TaleWorlds.Library.MathF.Min(el.Number, value);
					result = num2 - value;
					_upgradedRegulars[new Tuple<PartyBase, CharacterObject>(party, character)] = num2;
				}
			}
			else if (_upgradedRegulars.TryGetValue(new Tuple<PartyBase, CharacterObject>(party, character), out value2) && value2 > 0)
			{
				result = -value2;
			}
		}
		return result;
	}

	private int CalculateReadyToUpgradeSafe(ref TroopRosterElement el, PartyBase owner)
	{
		int b = 0;
		CharacterObject character = el.Character;
		if (!character.IsHero && character.UpgradeTargets.Length != 0 && MobilePartyHelper.CanTroopGainXp(owner, el.Character, out var _))
		{
			int num = 0;
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				int upgradeXpCost = character.GetUpgradeXpCost(owner, i);
				if (num < upgradeXpCost)
				{
					num = upgradeXpCost;
				}
			}
			if (num > 0)
			{
				MapEventParty? mapEventParty = _mapEventParties.Find((MapEventParty p) => p.Party == owner);
				int num2 = el.Xp;
				foreach (FlattenedTroopRosterElement troop in mapEventParty.Troops)
				{
					if (troop.Troop == el.Character && !troop.IsKilled)
					{
						num2 += troop.XpGained;
					}
				}
				b = num2 / num;
			}
		}
		return TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Min(el.Number, b), 0);
	}
}
