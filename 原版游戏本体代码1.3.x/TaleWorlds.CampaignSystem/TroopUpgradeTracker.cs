using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x020000AC RID: 172
	public class TroopUpgradeTracker
	{
		// Token: 0x06001365 RID: 4965 RVA: 0x00059F91 File Offset: 0x00058191
		public void AddParty(MapEventParty mapEventParty)
		{
			this._mapEventParties.Add(mapEventParty);
		}

		// Token: 0x06001366 RID: 4966 RVA: 0x00059F9F File Offset: 0x0005819F
		public void RemoveParty(MapEventParty mapEventParty)
		{
			this._mapEventParties.Add(mapEventParty);
		}

		// Token: 0x06001367 RID: 4967 RVA: 0x00059FB0 File Offset: 0x000581B0
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
				this._heroSkills[character.HeroObject] = array;
				return;
			}
			int num = party.MemberRoster.FindIndexOfTroop(character);
			if (num >= 0)
			{
				TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(num);
				int value = this.CalculateReadyToUpgradeSafe(ref elementCopyAtIndex, party);
				this._upgradedRegulars[new Tuple<PartyBase, CharacterObject>(party, character)] = value;
			}
		}

		// Token: 0x06001368 RID: 4968 RVA: 0x0005A048 File Offset: 0x00058248
		public IEnumerable<SkillObject> CheckSkillUpgrades(Hero hero)
		{
			if (!this._heroSkills.IsEmpty<KeyValuePair<Hero, int[]>>() && this._heroSkills.ContainsKey(hero))
			{
				int[] oldSkillLevels = this._heroSkills[hero];
				int num;
				for (int i = 0; i < Skills.All.Count; i = num)
				{
					SkillObject skill = Skills.All[i];
					int newSkillLevel = hero.CharacterObject.GetSkillValue(skill);
					while (newSkillLevel > oldSkillLevels[i])
					{
						oldSkillLevels[i]++;
						yield return skill;
					}
					skill = null;
					num = i + 1;
				}
				oldSkillLevels = null;
			}
			yield break;
		}

		// Token: 0x06001369 RID: 4969 RVA: 0x0005A060 File Offset: 0x00058260
		public int CheckUpgradedCount(PartyBase party, CharacterObject character)
		{
			int result = 0;
			if (!character.IsHero && party != null)
			{
				int num = party.MemberRoster.FindIndexOfTroop(character);
				int num4;
				if (num >= 0)
				{
					TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(num);
					int num2 = this.CalculateReadyToUpgradeSafe(ref elementCopyAtIndex, party);
					int num3;
					if (this._upgradedRegulars.TryGetValue(new Tuple<PartyBase, CharacterObject>(party, character), out num3) && num2 > num3)
					{
						num3 = MathF.Min(elementCopyAtIndex.Number, num3);
						result = num2 - num3;
						this._upgradedRegulars[new Tuple<PartyBase, CharacterObject>(party, character)] = num2;
					}
				}
				else if (this._upgradedRegulars.TryGetValue(new Tuple<PartyBase, CharacterObject>(party, character), out num4) && num4 > 0)
				{
					result = -num4;
				}
			}
			return result;
		}

		// Token: 0x0600136A RID: 4970 RVA: 0x0005A110 File Offset: 0x00058310
		private int CalculateReadyToUpgradeSafe(ref TroopRosterElement el, PartyBase owner)
		{
			int b = 0;
			CharacterObject character = el.Character;
			int num;
			if (!character.IsHero && character.UpgradeTargets.Length != 0 && MobilePartyHelper.CanTroopGainXp(owner, el.Character, out num))
			{
				int num2 = 0;
				for (int i = 0; i < character.UpgradeTargets.Length; i++)
				{
					int upgradeXpCost = character.GetUpgradeXpCost(owner, i);
					if (num2 < upgradeXpCost)
					{
						num2 = upgradeXpCost;
					}
				}
				if (num2 > 0)
				{
					MapEventParty mapEventParty = this._mapEventParties.Find((MapEventParty p) => p.Party == owner);
					int num3 = el.Xp;
					foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in mapEventParty.Troops)
					{
						if (flattenedTroopRosterElement.Troop == el.Character && !flattenedTroopRosterElement.IsKilled)
						{
							num3 += flattenedTroopRosterElement.XpGained;
						}
					}
					b = num3 / num2;
				}
			}
			return MathF.Max(MathF.Min(el.Number, b), 0);
		}

		// Token: 0x04000657 RID: 1623
		private Dictionary<Tuple<PartyBase, CharacterObject>, int> _upgradedRegulars = new Dictionary<Tuple<PartyBase, CharacterObject>, int>();

		// Token: 0x04000658 RID: 1624
		private List<MapEventParty> _mapEventParties = new List<MapEventParty>();

		// Token: 0x04000659 RID: 1625
		private Dictionary<Hero, int[]> _heroSkills = new Dictionary<Hero, int[]>();
	}
}
