using TaleWorlds.MountAndBlade;

using TaleWorlds.CampaignSystem.Settlements.Locations;

using System;

using System.Text;

using System.Linq;

using TaleWorlds.CampaignSystem;

using TaleWorlds.CampaignSystem.Party;

using TaleWorlds.CampaignSystem.Roster;

using System.Collections.Generic;



namespace AnimusForge.Refactor.Adapters;



internal static class SceneRosterPromptCaptureAdapter

{

internal static NpcDataPacket BuildSceneNpcDataFromLocationCharacter(LocationCharacter locationCharacter)

	{

		if (locationCharacter?.Character == null)

		{

			return null;

		}

		Agent agent = SceneMovementController.ResolveAgentForLocationCharacter(locationCharacter);

		if (agent != null)

		{

			return ShoutUtils.ExtractNpcData(agent);

		}

		CharacterObject character = locationCharacter.Character;

		NpcDataPacket npcDataPacket = new NpcDataPacket

		{

			Name = character.Name?.ToString() ?? "NPC",

			AgentIndex = -1,

			IsHero = character.IsHero,

			CultureId = character.Culture?.StringId?.ToLowerInvariant() ?? "neutral",

			IsFemale = character.IsFemale,

			Age = character.IsHero ? (character.HeroObject?.Age ?? 30f) : 30f,

			UnnamedKey = "",

			TroopId = "",

			UnnamedRank = "",

			RoleDesc = character.IsHero ? AdapterPromptCaptureTexts.PcapCompat("Project_RosterHero") : AdapterPromptCaptureTexts.PcapCompat("Project_RosterCivilian"),

			PersonalityDesc = "",

			BackgroundDesc = ""

		};

		if (character.IsHero && character.HeroObject != null)

		{

			Hero hero = character.HeroObject;

			if (hero.IsLord)

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterLord");

			}

			else if (hero.IsWanderer)

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterWanderer");

			}

			else if (hero.IsNotable)

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterNotable");

			}

			MyBehavior.GetNpcPersonaForExternal(hero, out var personality, out var background);

			npcDataPacket.PersonalityDesc = (personality ?? "").Trim();

			npcDataPacket.BackgroundDesc = (background ?? "").Trim();

		}

		else

		{

			npcDataPacket.TroopId = (character.StringId ?? "").Trim().ToLowerInvariant();

			if (character.Occupation == Occupation.Villager)

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterVillager");

			}

			else if (character.Occupation == Occupation.Guard)

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterGuard");

			}

			else if (character.Occupation == Occupation.Mercenary)

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterSoldier");

			}

			else

			{

				npcDataPacket.RoleDesc = AdapterPromptCaptureTexts.PcapCompat("Project_RosterCivilian");

			}

		}

		ShoutUtils.EnsurePromptNameFields(npcDataPacket);

		return npcDataPacket;

	}



	internal static List<NpcDataPacket> BuildGroupSpeakingCandidates(List<NpcDataPacket> allNpcData, NpcDataPacket primaryNpc)

	{

		List<NpcDataPacket> speakingCandidates = new List<NpcDataPacket>();

		HashSet<int> addedAgentIndices = new HashSet<int>();

		if (primaryNpc != null && addedAgentIndices.Add(primaryNpc.AgentIndex))

		{

			speakingCandidates.Add(primaryNpc);

		}

		if (allNpcData != null)

		{

			foreach (NpcDataPacket n in allNpcData)

			{

				if (n == null || !addedAgentIndices.Add(n.AgentIndex))

				{

					continue;

				}

				speakingCandidates.Add(n);

			}

		}

		return speakingCandidates;

	}

	internal static NpcDataPacket CloneNpcDataPacket(NpcDataPacket npc)

	{

		if (npc == null)

		{

			return null;

		}

		return new NpcDataPacket

		{

			Name = npc.Name,

			RoleDesc = npc.RoleDesc,

			PersonalityDesc = npc.PersonalityDesc,

			BackgroundDesc = npc.BackgroundDesc,

			AgentIndex = npc.AgentIndex,

			IsHero = npc.IsHero,

			CultureId = npc.CultureId,

			UnnamedKey = npc.UnnamedKey,

			TroopId = npc.TroopId,

			UnnamedRank = npc.UnnamedRank,

			IsFemale = npc.IsFemale,

			Age = npc.Age,

			PromptGivenName = npc.PromptGivenName,

			PromptDisplayName = npc.PromptDisplayName,

			HasScenePosition = npc.HasScenePosition,

			ScenePositionX = npc.ScenePositionX,

			ScenePositionY = npc.ScenePositionY,

			ScenePositionZ = npc.ScenePositionZ

		};

	}

	internal static List<NpcDataPacket> CloneNpcDataSnapshot(List<NpcDataPacket> allNpcData)

	{

		List<NpcDataPacket> list = new List<NpcDataPacket>();

		if (allNpcData == null || allNpcData.Count == 0)

		{

			return list;

		}

		foreach (NpcDataPacket npcDataPacket in allNpcData)

		{

			NpcDataPacket item = CloneNpcDataPacket(npcDataPacket);

			if (item != null)

			{

				list.Add(item);

			}

		}

		return list;

	}

	internal static void ApplySceneLocalDisambiguatedNames(List<NpcDataPacket> allNpcData)

	{

		if (allNpcData == null || allNpcData.Count == 0)

		{

			return;

		}

		ShoutUtils.EnsureScenePromptNames(allNpcData);

	}



internal static PartyBase ResolveLedPartyBaseForPrompt(Hero hero)

	{

		if (hero == null)

		{

			return null;

		}

		try

		{

			MobileParty mobileParty = hero.PartyBelongedTo;

			if (hero == Hero.MainHero)

			{

				if (mobileParty?.LeaderHero == hero && mobileParty?.Party != null)

				{

					return mobileParty.Party;

				}

				return MobileParty.MainParty?.Party;

			}

			if (mobileParty != null && mobileParty.LeaderHero == hero)

			{

				return mobileParty.Party;

			}

		}

		catch

		{

		}

		return null;

	}

internal static void AggregateRosterForPrompt(TroopRoster roster, bool excludeHeroes,

		out int total, out int infantry, out int cavalry, out int archer, out int horseArcher,

		out List<KeyValuePair<string, int>> tieredTopN, int topN = 10)

	{

		total = 0;

		infantry = 0;

		cavalry = 0;

		archer = 0;

		horseArcher = 0;

		tieredTopN = new List<KeyValuePair<string, int>>();

		if (roster == null)

		{

			return;

		}

		Dictionary<string, int> countByKey = new Dictionary<string, int>(StringComparer.Ordinal);

		Dictionary<string, string> nameByKey = new Dictionary<string, string>(StringComparer.Ordinal);

		Dictionary<string, int> tierByKey = new Dictionary<string, int>(StringComparer.Ordinal);

		try

		{

			int rosterCount = roster.Count;

			for (int i = 0; i < rosterCount; i++)

			{

				TroopRosterElement element;

				try

				{

					element = roster.GetElementCopyAtIndex(i);

				}

				catch

				{

					continue;

				}

				CharacterObject character = element.Character;

				if (character == null || element.Number <= 0)

				{

					continue;

				}

				if (excludeHeroes && character.IsHero)

				{

					continue;

				}

				int count = Math.Max(0, element.Number);

				if (count <= 0)

				{

					continue;

				}

				total += count;

				try

				{

					string label = PartyAssetTransferBannerlordAdapter.GetPartyTransferTroopTypeLabelForExternal(character);

					if (label == AdapterPromptCaptureTexts.PcapCompat("PromptCompat_58798cdd0ce4")) cavalry += count;

						else if (label == AdapterPromptCaptureTexts.PcapCompat("PromptCompat_46435cf51142")) archer += count;

						else if (label == AdapterPromptCaptureTexts.PcapCompat("PromptCompat_f3ecd4cb98f0")) horseArcher += count;

						else infantry += count;

				}

				catch

				{

					infantry += count;

				}

				string key = (character.StringId ?? "").Trim();

				if (string.IsNullOrWhiteSpace(key))

				{

					try

					{

						key = character.Name?.ToString() ?? "";

					}

					catch

					{

						key = "";

					}

				}

				if (string.IsNullOrWhiteSpace(key))

				{

					key = AdapterPromptCaptureTexts.PcapCompat("Project_RosterUnknownTroop");

				}

				string displayName;

				try

				{

					displayName = (character.Name?.ToString() ?? "").Trim();

				}

				catch

				{

					displayName = "";

				}

				if (string.IsNullOrWhiteSpace(displayName))

				{

					displayName = AdapterPromptCaptureTexts.PcapCompat("Project_RosterUnknownTroop");

				}

				int tier = 0;

				try

				{

					tier = Math.Max(0, character.Tier);

				}

				catch

				{

					tier = 0;

				}

				if (countByKey.ContainsKey(key))

				{

					countByKey[key] += count;

				}

				else

				{

					countByKey[key] = count;

					nameByKey[key] = displayName;

					tierByKey[key] = tier;

				}

			}

		}

		catch

		{

		}

		try

		{

			tieredTopN = countByKey

				.Select(kv => new

				{

					Key = kv.Key,

					Name = nameByKey.TryGetValue(kv.Key, out var n) ? n : kv.Key,

					Count = kv.Value,

					Tier = tierByKey.TryGetValue(kv.Key, out var t) ? t : 0

				})

				.OrderByDescending(x => x.Tier)

				.ThenByDescending(x => x.Count)

				.ThenBy(x => x.Name, StringComparer.Ordinal)

				.Take(Math.Max(1, topN))

				.Select(x => new KeyValuePair<string, int>(x.Name, x.Count))

				.ToList();

		}

		catch

		{

			tieredTopN = new List<KeyValuePair<string, int>>();

		}

	}

internal static List<string> ExtractHeroPrisonerNamesForPrompt(TroopRoster roster)

	{

		List<string> result = new List<string>();

		if (roster == null)

		{

			return result;

		}

		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

		try

		{

			int rosterCount = roster.Count;

			for (int i = 0; i < rosterCount; i++)

			{

				TroopRosterElement element;

				try

				{

					element = roster.GetElementCopyAtIndex(i);

				}

				catch

				{

					continue;

				}

				CharacterObject character = element.Character;

				if (character == null || element.Number <= 0 || !character.IsHero)

				{

					continue;

				}

				string name = "";

				try

				{

					name = (character.HeroObject?.Name?.ToString() ?? "").Trim();

				}

				catch

				{

					name = "";

				}

				if (string.IsNullOrWhiteSpace(name))

				{

					try

					{

						name = (character.Name?.ToString() ?? "").Trim();

					}

					catch

					{

						name = "";

					}

				}

				if (string.IsNullOrWhiteSpace(name))

				{

					name = AdapterPromptCaptureTexts.PcapText("roster_prompt_1");

				}

				if (seen.Add(name))

				{

					result.Add(name);

				}

			}

		}

		catch

		{

		}

		return result;

	}

internal static List<string> ExtractHeroMemberNamesForPrompt(TroopRoster roster, Hero leaderHero, string leaderDisplayNameOverride, int maxCount, out int totalHeroCount)

	{

		totalHeroCount = 0;

		List<string> result = new List<string>();

		if (roster == null)

		{

			return result;

		}

		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

		try

		{

			int rosterCount = roster.Count;

			for (int i = 0; i < rosterCount; i++)

			{

				TroopRosterElement element;

				try

				{

					element = roster.GetElementCopyAtIndex(i);

				}

				catch

				{

					continue;

				}

				CharacterObject character = element.Character;

				if (character == null || element.Number <= 0 || !character.IsHero)

				{

					continue;

				}

				Hero hero = character.HeroObject;

				if (hero == Hero.MainHero)

				{

					continue;

				}

				string key = (hero?.StringId ?? character.StringId ?? "").Trim();

				if (string.IsNullOrWhiteSpace(key))

				{

					key = character.Name?.ToString() ?? "";

				}

				if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))

				{

					continue;

				}

				totalHeroCount++;

				string name = "";

				if (hero != null && leaderHero != null && hero == leaderHero && !string.IsNullOrWhiteSpace(leaderDisplayNameOverride))

				{

					name = leaderDisplayNameOverride.Trim();

				}

				if (string.IsNullOrWhiteSpace(name))

				{

					try

					{

						name = (hero?.Name?.ToString() ?? "").Trim();

					}

					catch

					{

						name = "";

					}

				}

				if (string.IsNullOrWhiteSpace(name))

				{

					try

					{

						name = (character.Name?.ToString() ?? "").Trim();

					}

					catch

					{

						name = "";

					}

				}

				if (string.IsNullOrWhiteSpace(name))

				{

					name = AdapterPromptCaptureTexts.PcapText("roster_prompt_2");

				}

				if (hero != null && leaderHero != null && hero == leaderHero)

				{

					name += AdapterPromptCaptureTexts.PcapText("roster_prompt_3");

				}

				if (result.Count < Math.Max(1, maxCount))

				{

					result.Add(name);

				}

			}

		}

		catch

		{

		}

		return result;

	}

internal static string BuildPartyTroopsLineForPrompt(PartyBase partyBase, string leadingText, string noTroopsText, bool includeDetails = true, string leaderDisplayNameOverride = null)

	{

		leadingText = (leadingText ?? "").Trim();

		if (string.IsNullOrWhiteSpace(leadingText))

		{

			leadingText = AdapterPromptCaptureTexts.PcapText("roster_prompt_4");

		}

		noTroopsText = string.IsNullOrWhiteSpace(noTroopsText) ? AdapterPromptCaptureTexts.PcapText("roster_prompt_5") : noTroopsText.Trim();

		try

		{

			if (partyBase == null || partyBase.MemberRoster == null)

			{

				return noTroopsText;

			}

			string shipPromptText = BuildPartyShipPromptSuffixForPrompt(partyBase);

			int rosterTotal = 0;

			try

			{

				rosterTotal = Math.Max(0, partyBase.MemberRoster.TotalManCount);

			}

			catch

			{

				rosterTotal = 0;

			}

			AggregateRosterForPrompt(partyBase.MemberRoster, excludeHeroes: true,

				out int total, out int inf, out int cav, out int arc, out int hArc,

				out List<KeyValuePair<string, int>> top, 10);

			Hero leaderHero = null;

			try

			{

				leaderHero = partyBase.LeaderHero ?? partyBase.MobileParty?.LeaderHero;

			}

			catch

			{

				leaderHero = null;

			}

			List<string> heroNames = ExtractHeroMemberNamesForPrompt(partyBase.MemberRoster, leaderHero, leaderDisplayNameOverride, 12, out int heroCount);

			if (rosterTotal <= 0)

			{

				return noTroopsText + shipPromptText;

			}

			List<string> classParts = new List<string>(4);

			if (inf > 0) classParts.Add(AdapterPromptCaptureTexts.PcapText("roster_prompt_6") + inf);

			if (cav > 0) classParts.Add(AdapterPromptCaptureTexts.PcapText("roster_prompt_cavalry") + cav);

			if (arc > 0) classParts.Add(AdapterPromptCaptureTexts.PcapText("roster_prompt_7") + arc);

			if (hArc > 0) classParts.Add(AdapterPromptCaptureTexts.PcapText("roster_prompt_8") + hArc);

			StringBuilder sb = new StringBuilder();

			sb.Append(leadingText).Append(rosterTotal).Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_9"));

			if (!includeDetails)

			{

				if (!string.IsNullOrWhiteSpace(shipPromptText))

				{

					sb.Append(shipPromptText);

				}

				return sb.ToString().Trim();

			}

			if (heroNames != null && heroNames.Count > 0)

			{

				sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_10")).Append(string.Join("、", heroNames));

				if (heroCount > heroNames.Count)

				{

					sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_11")).Append(heroCount).Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_12"));

				}

			}

			if (total > 0 && classParts.Count > 0)

			{

				if (heroNames != null && heroNames.Count > 0)

				{

					sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_13")).Append(total).Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_14")).Append(string.Join("、", classParts)).Append("）");

				}

				else

				{

					sb.Append("（").Append(string.Join("、", classParts)).Append("）");

				}

			}

			if (total > 0 && top != null && top.Count > 0)

			{

				List<string> troopParts = new List<string>(top.Count);

				foreach (KeyValuePair<string, int> kv in top)

				{

					troopParts.Add(kv.Key + "x" + kv.Value);

				}

				sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_15")).Append(string.Join("、", troopParts));

			}

			if (!string.IsNullOrWhiteSpace(shipPromptText))

			{

				sb.Append(shipPromptText);

			}

			return sb.ToString().Replace("\r", " ").Replace("\n", " ").Trim();

		}

		catch

		{

			return noTroopsText;

		}

	}

internal static string BuildPartyShipPromptSuffixForPrompt(PartyBase partyBase)

	{

		try

		{

			string shipText = MapSeaContextGuard.BuildMobilePartyShipPromptText(partyBase?.MobileParty);

			return string.IsNullOrWhiteSpace(shipText) ? "" : (AdapterPromptCaptureTexts.PcapText("roster_prompt_16") + shipText);

		}

		catch

		{

			return "";

		}

	}

internal static string BuildHeroPartyTroopsLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true, string leaderDisplayNameOverride = null)

	{

		string subject = secondPerson ? AdapterPromptCaptureTexts.PcapText("roster_prompt_17") : AdapterPromptCaptureTexts.PcapText("roster_prompt_18");

		return BuildPartyTroopsLineForPrompt(ResolveLedPartyBaseForPrompt(hero), subject + AdapterPromptCaptureTexts.PcapText("roster_prompt_19"), subject + AdapterPromptCaptureTexts.PcapText("roster_prompt_20"), includeDetails, leaderDisplayNameOverride);

	}

internal static string BuildPartyPrisonersLineForPrompt(PartyBase partyBase, string subject, string noPrisonersText, bool includeDetails = true)

	{

		subject = string.IsNullOrWhiteSpace(subject) ? AdapterPromptCaptureTexts.PcapText("roster_prompt_21") : subject.Trim();

		noPrisonersText = string.IsNullOrWhiteSpace(noPrisonersText) ? (subject + AdapterPromptCaptureTexts.PcapText("roster_prompt_22")) : noPrisonersText.Trim();

		try

		{

			if (partyBase == null || partyBase.PrisonRoster == null)

			{

				return noPrisonersText;

			}

			List<string> heroNames = ExtractHeroPrisonerNamesForPrompt(partyBase.PrisonRoster);

			AggregateRosterForPrompt(partyBase.PrisonRoster, excludeHeroes: true,

				out int total, out int _, out int _, out int _, out int _,

				out List<KeyValuePair<string, int>> top, 10);

			if ((heroNames == null || heroNames.Count == 0) && total <= 0)

			{

				return noPrisonersText;

			}

			if (!includeDetails)

			{

				int prisonerTotal = 0;

				try

				{

					prisonerTotal = Math.Max(0, partyBase.PrisonRoster?.TotalManCount ?? 0);

				}

				catch

				{

					prisonerTotal = Math.Max(0, total + (heroNames?.Count ?? 0));

				}

				return prisonerTotal <= 0 ? noPrisonersText : (subject + AdapterPromptCaptureTexts.PcapText("roster_prompt_23") + prisonerTotal + AdapterPromptCaptureTexts.PcapText("roster_prompt_24"));

			}

			StringBuilder sb = new StringBuilder();

			sb.Append(subject);

			if (heroNames != null && heroNames.Count > 0)

			{

				sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_23")).Append(heroNames.Count).Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_25"))

					.Append(string.Join("、", heroNames));

				if (top != null && top.Count > 0)

				{

					List<string> troopParts = new List<string>(top.Count);

					foreach (KeyValuePair<string, int> kv in top)

					{

						troopParts.Add(kv.Key + "x" + kv.Value);

					}

					sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_26")).Append(string.Join("、", troopParts));

				}

			}

			else

			{

				sb.Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_23")).Append(total).Append(AdapterPromptCaptureTexts.PcapText("roster_prompt_24"));

				if (top != null && top.Count > 0)

				{

					List<string> troopParts = new List<string>(top.Count);

					foreach (KeyValuePair<string, int> kv in top)

					{

						troopParts.Add(kv.Key + "x" + kv.Value);

					}

					sb.Append("：").Append(string.Join("、", troopParts));

				}

			}

			return sb.ToString().Replace("\r", " ").Replace("\n", " ").Trim();

		}

		catch

		{

			return noPrisonersText;

		}

	}

internal static string BuildHeroPartyPrisonersLineForPrompt(Hero hero, bool secondPerson, bool includeDetails = true)

	{

		string subject = secondPerson ? AdapterPromptCaptureTexts.PcapText("roster_prompt_17") : AdapterPromptCaptureTexts.PcapText("roster_prompt_18");

		return BuildPartyPrisonersLineForPrompt(ResolveLedPartyBaseForPrompt(hero), subject, subject + AdapterPromptCaptureTexts.PcapText("roster_prompt_22"), includeDetails);

	}

}

