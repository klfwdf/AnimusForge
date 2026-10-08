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
			string armyName = GetArmyDisplayName(army);
			string leaderName = GetHeroDisplayName(leaderHero);
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
			stringBuilder.Append("【当前军团事实】");
			if (isArmyLeader)
			{
				stringBuilder.Append("你正在率领").Append(armyName);
			}
			else
			{
				stringBuilder.Append("你现在属于").Append(leaderName).Append("统率的").Append(armyName);
			}
			if (partyCount > 0 || totalMen > 0)
			{
				stringBuilder.Append("，当前约有");
				if (partyCount > 0)
				{
					stringBuilder.Append(partyCount).Append("支部队");
					if (totalMen > 0)
					{
						stringBuilder.Append("、");
					}
				}
				if (totalMen > 0)
				{
					stringBuilder.Append(totalMen).Append("名士兵");
				}
			}
			if (heroNames.Count > 0)
			{
				stringBuilder.Append("。你知道这支军团中的Hero包括：").Append(string.Join("、", heroNames));
				if (heroCount > heroNames.Count)
				{
					stringBuilder.Append("等").Append(heroCount).Append("人");
				}
			}
			stringBuilder.Append("。这是你此刻应当知道的实时事实，不要说自己不清楚是否在军团中。");
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
				playerName = "玩家";
			}
			string armyName = GetArmyDisplayName(army);
			if (leaderHero == playerHero && string.IsNullOrWhiteSpace(army?.Name?.ToString()))
			{
				armyName = playerName + "的军团";
			}
			string leaderName = leaderHero == playerHero ? playerName : GetHeroDisplayName(leaderHero);
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
			stringBuilder.Append("【玩家当前军团事实】");
			if (playerIsArmyLeader)
			{
				stringBuilder.Append(playerName).Append("正在率领").Append(armyName);
			}
			else
			{
				stringBuilder.Append(playerName).Append("现在属于").Append(leaderName).Append("统率的").Append(armyName);
			}
			if (partyCount > 0 || totalMen > 0)
			{
				stringBuilder.Append("，当前约有");
				if (partyCount > 0)
				{
					stringBuilder.Append(partyCount).Append("支部队");
					if (totalMen > 0)
					{
						stringBuilder.Append("、");
					}
				}
				if (totalMen > 0)
				{
					stringBuilder.Append(totalMen).Append("名士兵");
				}
			}
			if (heroNames.Count > 0)
			{
				stringBuilder.Append("；军团Hero包括：").Append(string.Join("、", heroNames));
				if (heroCount > heroNames.Count)
				{
					stringBuilder.Append("等").Append(heroCount).Append("人");
				}
			}
			stringBuilder.Append("。");
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
			addHero(leaderHero, "军团统帅");
			IEnumerable<MobileParty> parties = (army?.Parties != null) ? army.Parties : Enumerable.Empty<MobileParty>();
			foreach (MobileParty mobileParty in parties)
			{
				if (mobileParty == null)
				{
					continue;
				}
				addHero(mobileParty.LeaderHero, mobileParty == army.LeaderParty ? "军团统帅" : "部队领袖");
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
					addHero(character.HeroObject, "随队Hero");
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
					tags.Add("你");
				}
				string role = (item.Item2 ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(role))
				{
					tags.Add(role);
				}
				string name = value == displayOverrideHero && !string.IsNullOrWhiteSpace(displayOverrideName) ? displayOverrideName.Trim() : GetHeroDisplayName(value);
				return tags.Count > 0 ? (name + "（" + string.Join("，", tags.Distinct()) + "）") : name;
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
				"违法乱纪",
				"作奸犯科",
				"为非作歹",
				"胡作非为",
				"无法无天",
				"劣迹昭著",
				"罪恶昭彰",
				"罪大恶极",
				"恶贯满盈",
				"罪不容诛"
			};
			int level = Math.Max(1, Math.Min(idioms.Length, (int)Math.Ceiling(crimeRating / maxCrimeRating * idioms.Length)));
			return "此人在" + factionName + "的犯罪恶名被评为" + idioms[level - 1] + "。";
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
		return BuildPlayerCourierIdentityForExternal(observer, "来信者", "正式回信");
	}
	internal static string BuildPlayerCourierRecipientIdentityForExternal(Hero observer)
	{
		return BuildPlayerCourierIdentityForExternal(observer, "收信者", "正式信件中");
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
				playerDisplayName = includeFullIdentity ? (playerHero.Name?.ToString() ?? "").Trim() : "玩家";
			}
			if (string.IsNullOrWhiteSpace(playerDisplayName))
			{
				playerDisplayName = "玩家";
			}
			int clanTier = 0;
			string clanName = "无家族";
			string clanRole = playerHero.IsFemale ? "女性成员" : "男性成员";
			try
			{
				clanTier = playerHero.Clan?.Tier ?? 0;
				string rawClanName = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(rawClanName))
				{
					clanName = rawClanName;
				}
				if (!string.IsNullOrWhiteSpace(clanName) && clanName != "无家族" && !clanName.EndsWith("家族", StringComparison.Ordinal))
				{
					clanName += "家族";
				}
				if (playerHero.Clan?.Leader == playerHero)
				{
					clanRole = "族长";
				}
			}
			catch
			{
			}
			PersonaIdentityPromptCaptureAdapter.GetHeroFactionAndLiegeForPrompt(playerHero, out var factionName, out var liegeName);
			string identityTitle = PersonaIdentityPromptCaptureAdapter.CaptureHeroIdentityTitle(playerHero);
			string cultureText = PersonaIdentityPromptCaptureAdapter.GetHeroCultureNameForPrompt(playerHero);
			if (!string.IsNullOrWhiteSpace(cultureText) && !cultureText.EndsWith("人", StringComparison.Ordinal))
			{
				cultureText += "人";
			}
			string ageText = PersonaIntroTextRules.BuildAgeBracketLabel(playerHero.Age);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("【" + participantLabel + "公开身份】");
			stringBuilder.AppendLine(participantLabel + "公开称呼：" + playerDisplayName);
			stringBuilder.AppendLine(participantLabel + "文化与年纪：" + cultureText + "，" + ageText);
			if (includeFullIdentity)
			{
				stringBuilder.AppendLine(PersonaIdentityPromptCaptureAdapter.BuildFactionLineForPrompt(participantLabel + "势力：", factionName, liegeName));
				stringBuilder.AppendLine(participantLabel + "身份：" + identityTitle);
				stringBuilder.AppendLine(participantLabel + "家族：" + clanName + $"（{Math.Max(0, clanTier)} level，{clanRole}）");
				try
				{
					if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(playerHero, out Kingdom ruledKingdom))
					{
						string kingdomName = (ruledKingdom.Name?.ToString() ?? factionName ?? "").Trim();
						string sovereignTitle = playerHero.IsFemale ? "女王/统治者" : "国王/统治者";
						string scope = string.IsNullOrWhiteSpace(kingdomName) ? "" : (kingdomName + "的");
						stringBuilder.AppendLine("称呼要求：" + participantLabel + "是" + scope + sovereignTitle + "，" + formalAddressContext + "应称其为“" + playerDisplayName + "陛下”或使用君主/统治者级称呼；不要把此人降格称为“勋爵”“领主”或普通贵族。");
					}
					else if (playerHero.Clan?.Leader == playerHero)
					{
						stringBuilder.AppendLine("称呼要求：" + participantLabel + "只是" + clanName + "的族长，并非王国统治者；" + formalAddressContext + "不得称其为“陛下”，应使用其公开称呼或族长、领主级称呼。");
					}
				}
				catch
				{
				}
			}
			else
			{
				stringBuilder.AppendLine(participantLabel + "详细身份：你尚未确认其真实姓名、家族、势力或头衔，只能依据公开称呼、信件内容和你已知的公开传闻判断。");
			}
			string vassalageRelationLine = BuildPlayerVassalageRelationPromptLineForExternal(observer, counterpartKingdomLabel: "玩家所在王国");
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
	internal static string BuildPlayerVassalageRelationPromptLineForExternal(Hero observer, CharacterObject observerCharacter = null, string kingdomIdOverride = null, string counterpartKingdomLabel = "玩家所在王国", int targetAgentIndex = -1)
	{
		try
		{
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
