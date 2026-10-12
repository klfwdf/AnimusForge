using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.MountAndBlade;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;
using static AnimusForge.AdapterPromptCaptureTexts;

namespace AnimusForge.Refactor.Adapters;

internal static class ArmyIdentityPromptCaptureAdapter
{
	internal static string BuildHeroArmyRuntimeFactForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		try
		{
			MobileParty party = hero.PartyBelongedTo;
			if (hero == Hero.MainHero && party == null)
			{
				party = MobileParty.MainParty;
			}
			Army army = party?.Army;
			if (army == null)
			{
				return "";
			}
			MobileParty leaderParty = army.LeaderParty;
			Hero leaderHero = leaderParty?.LeaderHero ?? army.ArmyOwner;
			bool isArmyLeader = leaderHero == hero || leaderParty == party;
			string armyName = GetArmyDisplayNameForPrompt(army);
			string leaderName = GetHeroDisplayNameForPrompt(leaderHero);
			int partyCount = 0;
			int totalMen = 0;
			try
			{
				partyCount = Math.Max(0, army.Parties?.Count ?? 0);
			}
			catch
			{
				partyCount = 0;
			}
			try
			{
				totalMen = Math.Max(0, army.TotalManCount);
			}
			catch
			{
				totalMen = 0;
			}
			List<string> heroNames = BuildArmyHeroListForPrompt(army, hero, leaderHero, 18, out int heroCount);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(PcapText("AF_PCAP_Army_Head", "[Current army facts] "));
			if (isArmyLeader)
			{
				stringBuilder.Append(PcapFormat("AF_PCAP_Army_HeroLeads", "You are leading {ARMY}", ("ARMY", armyName)));
			}
			else
			{
				stringBuilder.Append(PcapFormat("AF_PCAP_Army_HeroBelongs", "You now belong to {ARMY}, commanded by {LEADER}", ("LEADER", leaderName), ("ARMY", armyName)));
			}
			if (partyCount > 0 || totalMen > 0)
			{
				stringBuilder.Append(PcapText("AF_PCAP_Army_SizeLead", ", currently about "));
				if (partyCount > 0)
				{
					stringBuilder.Append(PcapFormat("AF_PCAP_Army_Parties", "{COUNT} parties", ("COUNT", partyCount)));
					if (totalMen > 0)
					{
						stringBuilder.Append(PcapText("AF_PCAP_Army_SizeJoin", " and "));
					}
				}
				if (totalMen > 0)
				{
					stringBuilder.Append(PcapFormat("AF_PCAP_Army_Men", "{COUNT} soldiers", ("COUNT", totalMen)));
				}
			}
			if (heroNames.Count > 0)
			{
				stringBuilder.Append(PcapText("AF_PCAP_Army_HeroListHero", ". You know the heroes in this army include: ")).Append(string.Join(PcapText("AF_PCAP_Army_ListSep", ", "), heroNames));
				if (heroCount > heroNames.Count)
				{
					stringBuilder.Append(PcapFormat("AF_PCAP_Army_HeroMore", " and others, {COUNT} in total", ("COUNT", heroCount)));
				}
			}
			stringBuilder.Append(PcapText("AF_PCAP_Army_HeroTail", ". This is a real-time fact you should know right now; do not say you are unsure whether you are in an army."));
			return stringBuilder.ToString().Replace("\r", " ").Replace("\n", " ").Trim();
		}
		catch
		{
			return "";
		}
	}
	internal static string BuildPlayerArmyRuntimeFactForPrompt(Hero observerHero, CharacterObject observerCharacter, int targetAgentIndex)
	{
		try
		{
			Hero playerHero = Hero.MainHero;
			MobileParty playerParty = MobileParty.MainParty ?? playerHero?.PartyBelongedTo;
			Army army = playerParty?.Army;
			if (playerHero == null || army == null)
			{
				return "";
			}
			Hero targetHero = observerHero ?? observerCharacter?.HeroObject;
			if (targetHero == playerHero)
			{
				return "";
			}
			try
			{
				MobileParty targetParty = targetHero?.PartyBelongedTo;
				if (targetParty != null && targetParty.Army == army)
				{
					return "";
				}
			}
			catch
			{
			}
			MobileParty leaderParty = army.LeaderParty;
			Hero leaderHero = leaderParty?.LeaderHero ?? army.ArmyOwner;
			bool playerIsArmyLeader = leaderHero == playerHero || leaderParty == playerParty;
			string playerName = (PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(observerHero, observerCharacter, targetAgentIndex) ?? "").Trim();
			if (string.IsNullOrWhiteSpace(playerName))
			{
				playerName = PcapText("AF_PCAP_Army_PlayerFallback", "Player");
			}
			string armyName = GetArmyDisplayNameForPrompt(army);
			if (leaderHero == playerHero && string.IsNullOrWhiteSpace(army?.Name?.ToString()))
			{
				armyName = PcapFormat("AF_PCAP_Army_PlayerDefaultName", "{PLAYER}'s army", ("PLAYER", playerName));
			}
			string leaderName = leaderHero == playerHero ? playerName : GetHeroDisplayNameForPrompt(leaderHero);
			int partyCount = 0;
			int totalMen = 0;
			try
			{
				partyCount = Math.Max(0, army.Parties?.Count ?? 0);
			}
			catch
			{
				partyCount = 0;
			}
			try
			{
				totalMen = Math.Max(0, army.TotalManCount);
			}
			catch
			{
				totalMen = 0;
			}
			List<string> heroNames = BuildArmyHeroListForPrompt(army, null, leaderHero, 18, out int heroCount, playerHero, playerName);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(PcapText("AF_PCAP_Army_PlayerHead", "[Player's current army facts] "));
			if (playerIsArmyLeader)
			{
				stringBuilder.Append(PcapFormat("AF_PCAP_Army_PlayerLeads", "{PLAYER} is leading {ARMY}", ("PLAYER", playerName), ("ARMY", armyName)));
			}
			else
			{
				stringBuilder.Append(PcapFormat("AF_PCAP_Army_PlayerBelongs", "{PLAYER} now belongs to {ARMY}, commanded by {LEADER}", ("PLAYER", playerName), ("LEADER", leaderName), ("ARMY", armyName)));
			}
			if (partyCount > 0 || totalMen > 0)
			{
				stringBuilder.Append(PcapText("AF_PCAP_Army_SizeLead", ", currently about "));
				if (partyCount > 0)
				{
					stringBuilder.Append(PcapFormat("AF_PCAP_Army_Parties", "{COUNT} parties", ("COUNT", partyCount)));
					if (totalMen > 0)
					{
						stringBuilder.Append(PcapText("AF_PCAP_Army_SizeJoin", " and "));
					}
				}
				if (totalMen > 0)
				{
					stringBuilder.Append(PcapFormat("AF_PCAP_Army_Men", "{COUNT} soldiers", ("COUNT", totalMen)));
				}
			}
			if (heroNames.Count > 0)
			{
				stringBuilder.Append(PcapText("AF_PCAP_Army_PlayerHeroList", "; the heroes in the army include: ")).Append(string.Join(PcapText("AF_PCAP_Army_ListSep", ", "), heroNames));
				if (heroCount > heroNames.Count)
				{
					stringBuilder.Append(PcapFormat("AF_PCAP_Army_HeroMore", " and others, {COUNT} in total", ("COUNT", heroCount)));
				}
			}
			stringBuilder.Append(PcapText("AF_PCAP_Army_End", "."));
			return stringBuilder.ToString().Replace("\r", " ").Replace("\n", " ").Trim();
		}
		catch
		{
			return "";
		}
	}
	internal static List<string> BuildArmyHeroListForPrompt(Army army, Hero perspectiveHero, Hero leaderHero, int maxCount, out int totalHeroCount)
	{
		return BuildArmyHeroListForPrompt(army, perspectiveHero, leaderHero, maxCount, out totalHeroCount, null, null);
	}
	internal static List<string> BuildArmyHeroListForPrompt(Army army, Hero perspectiveHero, Hero leaderHero, int maxCount, out int totalHeroCount, Hero displayOverrideHero, string displayOverrideName)
	{
		totalHeroCount = 0;
		List<Tuple<Hero, string>> heroes = new List<Tuple<Hero, string>>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		void addHero(Hero value, string role)
		{
			if (value == null)
			{
				return;
			}
			string key = GetHeroId(value);
			if (string.IsNullOrWhiteSpace(key))
			{
				key = (value.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
			{
				return;
			}
			heroes.Add(Tuple.Create(value, role ?? ""));
		}
			try
			{
				string armyCommanderRole = PcapText("AF_PCAP_Army_RoleCommander", "army commander");
				string partyLeaderRole = PcapText("AF_PCAP_Army_RolePartyLeader", "party leader");
				addHero(leaderHero, armyCommanderRole);
				IEnumerable<MobileParty> parties = (army?.Parties != null) ? army.Parties : Enumerable.Empty<MobileParty>();
				foreach (MobileParty mobileParty in parties)
				{
					if (mobileParty == null)
					{
						continue;
					}
					addHero(mobileParty.LeaderHero, mobileParty == army.LeaderParty ? armyCommanderRole : partyLeaderRole);
				}
			foreach (MobileParty mobileParty2 in parties)
			{
				TroopRoster roster = mobileParty2?.MemberRoster;
				if (roster == null)
				{
					continue;
				}
				for (int i = 0; i < roster.Count; i++)
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
					addHero(character.HeroObject, PcapText("AF_PCAP_Army_RoleMember", "accompanying Hero"));
				}
			}
		}
		catch
		{
		}
		totalHeroCount = heroes.Count;
		return heroes
			.Take(Math.Max(1, maxCount))
			.Select(delegate(Tuple<Hero, string> item)
			{
				Hero value = item.Item1;
				List<string> tags = new List<string>();
				if (value == perspectiveHero)
				{
					tags.Add(PcapText("AF_PCAP_Army_TagYou", "you"));
				}
				string role = (item.Item2 ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(role))
				{
					tags.Add(role);
				}
				string name = value == displayOverrideHero && !string.IsNullOrWhiteSpace(displayOverrideName) ? displayOverrideName.Trim() : GetHeroDisplayNameForPrompt(value);
				return tags.Count > 0 ? PcapFormat("AF_PCAP_Army_NameTags", "{NAME} ({TAGS})", ("NAME", name), ("TAGS", string.Join(PcapText("AF_PCAP_Army_TagSep", ", "), tags.Distinct()))) : name;
			})
			.ToList();
	}
	internal static string BuildHeroArmyRuntimeFactForExternal(Hero hero)
	{
		return BuildHeroArmyRuntimeFactForPrompt(hero);
	}
	internal static string BuildPlayerCrimeRatingPromptLineForExternal(IFaction perspectiveFaction)
	{
		try
		{
			if (perspectiveFaction == null)
			{
				return "";
			}
			float crimeRating = SceneTauntBehavior.GetEffectiveCrimeRatingForExternal(perspectiveFaction);
			if (crimeRating <= 0f)
			{
				return "";
			}
			float maxCrimeRating = 100f;
			try
			{
				maxCrimeRating = Campaign.Current?.Models?.CrimeModel?.GetMaxCrimeRating() ?? 100f;
			}
			catch
			{
				maxCrimeRating = 100f;
			}
			if (maxCrimeRating <= 0f)
			{
				maxCrimeRating = 100f;
			}
			string factionName = (perspectiveFaction.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(factionName))
			{
				return "";
			}
			string[] idioms =
			{
				PcapText("AF_PCAP_Army_Crime01", "lawless conduct"),
				PcapText("AF_PCAP_Army_Crime02", "habitual wrongdoing"),
				PcapText("AF_PCAP_Army_Crime03", "committing evil deeds"),
				PcapText("AF_PCAP_Army_Crime04", "reckless misconduct"),
				PcapText("AF_PCAP_Army_Crime05", "utter disregard for the law"),
				PcapText("AF_PCAP_Army_Crime06", "a notorious record of misdeeds"),
				PcapText("AF_PCAP_Army_Crime07", "glaringly evident crimes"),
				PcapText("AF_PCAP_Army_Crime08", "the gravest of crimes"),
				PcapText("AF_PCAP_Army_Crime09", "a cup of wickedness filled to the brim"),
				PcapText("AF_PCAP_Army_Crime10", "crimes beyond all forgiveness")
			};
			int level = Math.Max(1, Math.Min(idioms.Length, (int)Math.Ceiling(crimeRating / maxCrimeRating * idioms.Length)));
			return PcapFormat("AF_PCAP_Army_CrimeLine", "This person's criminal notoriety in {FACTION} is rated as {IDIOM}.", ("FACTION", factionName), ("IDIOM", idioms[level - 1]));
		}
		catch
		{
			return "";
		}
	}
	internal static string BuildPlayerCourierSenderIdentityForExternal()
	{
		return BuildPlayerCourierSenderIdentityForExternal(PersonaIdentityPromptCaptureAdapter.ResolveCurrentPlayerIdentityObserverForPrompt());
	}
	internal static string BuildPlayerCourierSenderIdentityForExternal(Hero observer)
	{
		return BuildPlayerCourierIdentityForExternal(observer, PcapText("AF_PCAP_Army_CourierSender", "the sender"), PcapText("AF_PCAP_Army_CourierSenderContext", "a formal reply"));
	}
	internal static string BuildPlayerCourierRecipientIdentityForExternal(Hero observer)
	{
		return BuildPlayerCourierIdentityForExternal(observer, PcapText("AF_PCAP_Army_CourierRecipient", "the recipient"), PcapText("AF_PCAP_Army_CourierRecipientContext", "a formal letter"));
	}
	internal static string BuildPlayerCourierIdentityForExternal(Hero observer, string participantLabel, string formalAddressContext)
	{
		try
		{
			Hero playerHero = Hero.MainHero;
			if (playerHero == null)
			{
				return "";
			}
			bool includeFullIdentity = PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observer);
				string playerDisplayName = (PersonaIdentityPromptCaptureAdapter.BuildPlayerPublicDisplayNameForPrompt(observer) ?? "").Trim();
				if (string.IsNullOrWhiteSpace(playerDisplayName))
				{
					playerDisplayName = includeFullIdentity ? (playerHero.Name?.ToString() ?? "").Trim() : PcapText("AF_PCAP_Army_PlayerFallback", "Player");
				}
				if (string.IsNullOrWhiteSpace(playerDisplayName))
				{
					playerDisplayName = PcapText("AF_PCAP_Army_PlayerFallback", "Player");
				}
				int clanTier = 0;
				string noClanText = PcapText("AF_PCAP_Word_NoClan", "no clan");
				string clanName = noClanText;
				string clanRole = playerHero.IsFemale ? PcapText("AF_PCAP_Role_FemaleMember", "female member") : PcapText("AF_PCAP_Role_MaleMember", "male member");
				try
				{
					clanTier = playerHero.Clan?.Tier ?? 0;
					string rawClanName = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(rawClanName))
					{
						clanName = rawClanName;
					}
					string clanSuffix = PcapText("AF_PCAP_Suffix_Clan", " clan");
					if (!string.IsNullOrWhiteSpace(clanName) && clanName != noClanText && !clanName.EndsWith(clanSuffix, StringComparison.Ordinal))
					{
						clanName += clanSuffix;
					}
					if (playerHero.Clan?.Leader == playerHero)
					{
						clanRole = PcapText("AF_PCAP_Role_ClanLeader", "clan leader");
					}
				}
			catch
			{
			}
			PersonaIdentityPromptCaptureAdapter.GetHeroFactionAndLiegeForPrompt(playerHero, out var factionName, out var liegeName);
			string identityTitle = PersonaIdentityPromptCaptureAdapter.CaptureHeroIdentityTitle(playerHero);
				string cultureText = PersonaIdentityPromptCaptureAdapter.GetHeroCultureNameForPrompt(playerHero);
				string peopleSuffix = PcapText("AF_PCAP_Suffix_People", "");
				if (!string.IsNullOrWhiteSpace(cultureText) && !cultureText.EndsWith(peopleSuffix, StringComparison.Ordinal))
				{
					cultureText += peopleSuffix;
				}
				string ageText = PersonaIntroTextRules.BuildAgeBracketLabel(playerHero.Age);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierHeader", "[Public identity of {LABEL}]", ("LABEL", participantLabel)));
				stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierName", "Public name of {LABEL}: {NAME}", ("LABEL", participantLabel), ("NAME", playerDisplayName)));
				stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierCultureAge", "Culture and age of {LABEL}: {CULTURE}, {AGE}", ("LABEL", participantLabel), ("CULTURE", cultureText), ("AGE", ageText)));
				if (includeFullIdentity)
				{
					stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt(PcapFormat("AF_PCAP_Army_CourierFactionLabel", "Faction of {LABEL}: ", ("LABEL", participantLabel)), factionName, liegeName));
					stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierIdentity", "Identity of {LABEL}: {TITLE}", ("LABEL", participantLabel), ("TITLE", identityTitle)));
					stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierClan", "Clan of {LABEL}: {CLAN} (level {TIER}, {ROLE})", ("LABEL", participantLabel), ("CLAN", clanName), ("TIER", Math.Max(0, clanTier)), ("ROLE", clanRole)));
					try
					{
						if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(playerHero, out Kingdom ruledKingdom))
						{
							string kingdomName = (ruledKingdom.Name?.ToString() ?? factionName ?? "").Trim();
							string sovereignTitle = playerHero.IsFemale ? PcapText("AF_PCAP_Army_TitleQueen", "queen/ruler") : PcapText("AF_PCAP_Army_TitleKing", "king/ruler");
							string scope = string.IsNullOrWhiteSpace(kingdomName) ? "" : PcapFormat("AF_PCAP_Army_CourierScope", "{KINGDOM}'s ", ("KINGDOM", kingdomName));
							stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierSovereign", "Address requirement: {LABEL} is {SCOPE}{TITLE}; in {CONTEXT} address them as \"{NAME}, Your Majesty\" or with another sovereign/ruler-level form of address; do not demote this person to \"lord\", \"baron\" or an ordinary noble.", ("LABEL", participantLabel), ("SCOPE", scope), ("TITLE", sovereignTitle), ("CONTEXT", formalAddressContext), ("NAME", playerDisplayName)));
						}
						else if (playerHero.Clan?.Leader == playerHero)
						{
							stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierClanLeader", "Address requirement: {LABEL} is only the leader of {CLAN}, not a kingdom ruler; in {CONTEXT} do not call them \"Your Majesty\"; use their public name or a clan-leader or lord-level form of address.", ("LABEL", participantLabel), ("CLAN", clanName), ("CONTEXT", formalAddressContext)));
						}
					}
					catch
					{
					}
				}
				else
				{
					stringBuilder.AppendLine(PcapFormat("AF_PCAP_Army_CourierUnconfirmed", "Detailed identity of {LABEL}: you have not confirmed their real name, clan, faction or titles, and can only judge from their public name, the contents of the letter and the public rumors you already know.", ("LABEL", participantLabel)));
				}
				string vassalageRelationLine = BuildPlayerVassalageRelationPromptLineForExternal(observer);
			if (!string.IsNullOrWhiteSpace(vassalageRelationLine))
			{
				stringBuilder.AppendLine(vassalageRelationLine);
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}
	internal static string BuildPlayerVassalageRelationPromptLineForExternal(Hero observer, CharacterObject observerCharacter = null, string kingdomIdOverride = null, string counterpartKingdomLabel = null, int targetAgentIndex = -1)
		{
			try
			{
				counterpartKingdomLabel ??= PcapText("AF_PCAP_Army_PlayerKingdomLabel", "the player's kingdom");
				Kingdom observerKingdom = ResolveObserverKingdomForVassalagePrompt(observer, observerCharacter, kingdomIdOverride, targetAgentIndex);
			Kingdom playerKingdom = ResolvePlayerKingdomForVassalagePrompt();
			return VassalageBehavior.BuildKingdomVassalageRelationPromptLineForExternal(observerKingdom, playerKingdom, counterpartKingdomLabel);
		}
		catch
		{
			return "";
		}
	}
	internal static Kingdom ResolveObserverKingdomForVassalagePrompt(Hero observer, CharacterObject observerCharacter, string kingdomIdOverride, int targetAgentIndex)
	{
		try
		{
			Hero targetHero = observer ?? observerCharacter?.HeroObject;
			Kingdom kingdom = ResolveKingdomFromFactionForVassalagePrompt(targetHero?.Clan?.Kingdom ?? targetHero?.MapFaction);
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = VassalageBehavior.ResolveKingdomById(kingdomIdOverride);
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = ResolveAgentKingdomForVassalagePrompt(targetAgentIndex);
			if (kingdom != null)
			{
				return kingdom;
			}
			return ResolveCurrentSettlementKingdomForVassalagePrompt();
		}
		catch
		{
			return null;
		}
	}
	internal static Kingdom ResolveAgentKingdomForVassalagePrompt(int targetAgentIndex)
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (targetAgentIndex < 0 || agents == null)
			{
				return null;
			}
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
			CharacterObject characterObject = agent?.Character as CharacterObject;
			Kingdom kingdom = ResolveKingdomFromFactionForVassalagePrompt(characterObject?.HeroObject?.Clan?.Kingdom ?? characterObject?.HeroObject?.MapFaction);
			if (kingdom != null)
			{
				return kingdom;
			}
			PartyBase party = agent?.Origin?.BattleCombatant as PartyBase;
			kingdom = ResolveKingdomFromFactionForVassalagePrompt(party?.MapFaction);
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = ResolveKingdomFromFactionForVassalagePrompt(party?.MobileParty?.MapFaction);
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = ResolveKingdomFromFactionForVassalagePrompt(party?.MobileParty?.ActualClan);
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = ResolveKingdomFromFactionForVassalagePrompt(party?.Owner?.MapFaction);
			if (kingdom != null)
			{
				return kingdom;
			}
			return party?.Owner?.Clan?.Kingdom;
		}
		catch
		{
			return null;
		}
	}
	internal static Kingdom ResolveCurrentSettlementKingdomForVassalagePrompt()
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			return settlement?.OwnerClan?.Kingdom ?? ResolveKingdomFromFactionForVassalagePrompt(settlement?.MapFaction);
		}
		catch
		{
			return null;
		}
	}
	internal static Kingdom ResolveKingdomFromFactionForVassalagePrompt(IFaction faction)
	{
		try
		{
			if (faction is Kingdom kingdom)
			{
				return kingdom;
			}
			if (faction is Clan clan)
			{
				return clan.Kingdom;
			}
		}
		catch
		{
		}
		return null;
	}
	internal static Kingdom ResolvePlayerKingdomForVassalagePrompt()
	{
		try
		{
			Kingdom kingdom = Hero.MainHero?.Clan?.Kingdom;
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = Hero.MainHero?.MapFaction as Kingdom;
			if (kingdom != null)
			{
				return kingdom;
			}
			kingdom = Clan.PlayerClan?.Kingdom;
			if (kingdom != null)
			{
				return kingdom;
			}
			Clan playerFactionClan = Clan.PlayerClan?.MapFaction as Clan;
			return playerFactionClan?.Kingdom;
		}
		catch
		{
			return null;
		}
	}
}
