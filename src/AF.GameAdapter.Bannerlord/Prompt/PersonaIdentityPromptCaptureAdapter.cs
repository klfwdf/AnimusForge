using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Settlements;
using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge.Refactor.Adapters;

internal static class PersonaIdentityPromptCaptureAdapter
{
    internal sealed class PatiencePromptCapturePorts
    {
        internal readonly Func<Hero, MyBehavior.PatienceSnapshot> HeroSnapshot;
        internal readonly Func<string,string,string,MyBehavior.PatienceSnapshot> UnnamedSnapshot;
        internal readonly Func<int,string> RelationLevel;
        internal PatiencePromptCapturePorts(Func<Hero,MyBehavior.PatienceSnapshot> hero, Func<string,string,string,MyBehavior.PatienceSnapshot> unnamed, Func<int,string> relationLevel)
        { HeroSnapshot=hero ?? throw new ArgumentNullException(nameof(hero)); UnnamedSnapshot=unnamed ?? throw new ArgumentNullException(nameof(unnamed)); RelationLevel=relationLevel ?? throw new ArgumentNullException(nameof(relationLevel)); }
    }
    private static string CapturePatienceSceneInlineStateText(PatiencePromptCapturePorts ports, MyBehavior.PatienceSnapshot snap, bool includeRelationPenalty, bool includeClanRelation=true)
    {
        return AnimusForge.Refactor.Modules.PatiencePromptProjectionComposer.BuildSceneInlineStateText(snap, includeRelationPenalty, CapturePlayerPronoun(), snap.Current <= 0.01f ? BuildPlayerPublicDisplayNameForPrompt() : null, includeClanRelation && string.IsNullOrWhiteSpace(snap.RelationLevel) ? ports.RelationLevel(snap.Relation) : snap.RelationLevel, includeClanRelation);
    }

internal static string BuildDirectPatiencePrompt(PatiencePromptCapturePorts ports, Hero targetHero, out bool exhausted, out string exhaustedReply)
	{
		exhausted = false;
		exhaustedReply = "";
		if (targetHero == null)
		{
			return "";
		}
		MyBehavior.PatienceSnapshot heroPatienceSnapshot = ports.HeroSnapshot(targetHero);
		bool includeClanRelation = !ShouldOmitClanRelationFromPrompt(targetHero);
		if (heroPatienceSnapshot.Current <= 0.01f)
		{
			return AnimusForge.Refactor.Modules.PatiencePromptProjectionComposer.BuildPatiencePromptText(heroPatienceSnapshot, heroPatienceSnapshot.Current <= 0.01f ? BuildPlayerPublicDisplayNameForPrompt() : null, includeClanRelation);
		}
		return AnimusForge.Refactor.Modules.PatiencePromptProjectionComposer.BuildPatiencePromptText(heroPatienceSnapshot, heroPatienceSnapshot.Current <= 0.01f ? BuildPlayerPublicDisplayNameForPrompt() : null, includeClanRelation);
	}
internal static bool TryGetHeroSceneStatus(PatiencePromptCapturePorts ports, Hero hero, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		if (hero == null)
		{
			return false;
		}
		MyBehavior.PatienceSnapshot heroPatienceSnapshot = ports.HeroSnapshot(hero);
		int num = (int)Math.Round(heroPatienceSnapshot.Current);
		statusLine = "- " + heroPatienceSnapshot.DisplayName + ": " + AnimusForge.Refactor.Modules.PatiencePromptProjectionComposer.BuildCompactStateLine(heroPatienceSnapshot, !ShouldOmitClanRelationFromPrompt(hero));
		if (num <= 0)
		{
			statusLine += "，耐心已归零";
			statusLine += "\n  " + AnimusForge.Refactor.Modules.PatiencePromptProjectionComposer.BuildExhaustedPatienceInstruction(true, BuildPlayerPublicDisplayNameForPrompt());
		}
		return true;
	}
internal static bool TryGetHeroSceneInlineState(PatiencePromptCapturePorts ports, Hero hero, out string stateText, out bool canSpeak)
	{
		stateText = "";
		canSpeak = true;
		if (hero == null)
		{
			return false;
		}
		MyBehavior.PatienceSnapshot heroPatienceSnapshot = ports.HeroSnapshot(hero);
		stateText = CapturePatienceSceneInlineStateText(ports, heroPatienceSnapshot, includeRelationPenalty: true, includeClanRelation: !ShouldOmitClanRelationFromPrompt(hero));
		return true;
	}
internal static bool TryGetUnnamedSceneStatus(PatiencePromptCapturePorts ports, string unnamedKey, string npcName, string displayName, out string statusLine, out bool canSpeak)
	{
		statusLine = "";
		canSpeak = true;
		MyBehavior.PatienceSnapshot unnamedPatienceSnapshot = ports.UnnamedSnapshot(unnamedKey, npcName, displayName);
		if (string.IsNullOrWhiteSpace(unnamedPatienceSnapshot.Key))
		{
			return false;
		}
		int num = (int)Math.Round(unnamedPatienceSnapshot.Current);
		statusLine = $"- {unnamedPatienceSnapshot.DisplayName}: P(耐心)={num}/{unnamedPatienceSnapshot.Max}({unnamedPatienceSnapshot.PatienceLevel}) | R(家族关系)=0(中立) | T(综合信任)={unnamedPatienceSnapshot.Trust}({unnamedPatienceSnapshot.TrustLevel}) | L(私人关系)=0({RomanceSystemBehavior.GetPrivateLoveLevelText(0)})";
		if (num <= 0)
		{
			statusLine += "，耐心已归零";
			statusLine += "\n  " + AnimusForge.Refactor.Modules.PatiencePromptProjectionComposer.BuildExhaustedPatienceInstruction(false, BuildPlayerPublicDisplayNameForPrompt());
		}
		return true;
	}
internal static bool TryGetUnnamedSceneInlineState(PatiencePromptCapturePorts ports, string unnamedKey, string npcName, string displayName, out string stateText, out bool canSpeak)
	{
		stateText = "";
		canSpeak = true;
		MyBehavior.PatienceSnapshot unnamedPatienceSnapshot = ports.UnnamedSnapshot(unnamedKey, npcName, displayName);
		if (string.IsNullOrWhiteSpace(unnamedPatienceSnapshot.Key))
		{
			return false;
		}
		stateText = CapturePatienceSceneInlineStateText(ports, unnamedPatienceSnapshot, includeRelationPenalty: false);
		return true;
	}
    internal static string CapturePlayerPronoun() => (Hero.MainHero?.IsFemale ?? false) ? "她" : "他";
	internal static int GetCurrentHourOfDaySafeForPrompt()
	{
		if (Campaign.Current == null || !Campaign.Current.GameStarted)
		{
			return 0;
		}
		try
		{
			int getHourOfDay = CampaignTime.Now.GetHourOfDay;
			int hoursInDay = CampaignTime.HoursInDay;
			if (hoursInDay > 0)
			{
				getHourOfDay %= hoursInDay;
				if (getHourOfDay < 0)
				{
					getHourOfDay += hoursInDay;
				}
			}
			return Math.Max(0, Math.Min(23, getHourOfDay));
		}
		catch
		{
			try
			{
				return Math.Max(0, Math.Min(23, (int)Math.Floor(CampaignTime.Now.CurrentHourInDay)));
			}
			catch
			{
				return 0;
			}
		}
	}
	internal static bool ShouldOmitClanRelationFromPrompt(Hero targetHero)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			Clan playerClan = Clan.PlayerClan ?? mainHero?.Clan;
			return targetHero != null
				&& targetHero != mainHero
				&& (targetHero.IsPlayerCompanion
					|| (playerClan != null && (targetHero.CompanionOf == playerClan || targetHero.Clan == playerClan)));
		}
		catch
		{
			return false;
		}
	}
	internal static string BuildPlayerPublicDisplayNameForPrompt()
	{
		return BuildPlayerPublicDisplayNameForPrompt(ResolveCurrentPlayerIdentityObserverForPrompt());
	}
	internal static string BuildPlayerPublicDisplayNameForPrompt(Hero observer)
	{
		return TryBuildPlayerPublicDisplayNameForPrompt(observer, out var displayName, out var _) ? displayName : "玩家";
	}
	internal static string BuildPlayerPublicDisplayNameForPrompt(string observerKey, string cultureId)
	{
		return TryBuildPlayerPublicDisplayNameForPrompt(observerKey, cultureId, out var displayName, out var _) ? displayName : "玩家";
	}
	internal static string BuildPlayerPublicDisplayNameForPrompt(Hero observer, CharacterObject observerCharacter, int targetAgentIndex = -1)
	{
		if (observer != null || observerCharacter?.HeroObject != null)
		{
			return BuildPlayerPublicDisplayNameForPrompt(observer ?? observerCharacter?.HeroObject);
		}
		if (DoesPlayerNotorietyObserverKnowPlayer(null, observerCharacter, targetAgentIndex))
		{
			string name = (Hero.MainHero?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(name))
			{
				return name;
			}
		}
		return BuildPlayerPublicDisplayNameForPrompt((Hero)null);
	}
	internal static bool TryBuildPlayerPublicDisplayNameForPrompt(out string displayName, out bool isCompleteIdentity)
	{
		return TryBuildPlayerPublicDisplayNameForPrompt(ResolveCurrentPlayerIdentityObserverForPrompt(), out displayName, out isCompleteIdentity);
	}
	internal static bool TryBuildPlayerPublicDisplayNameForPrompt(Hero observer, out string displayName, out bool isCompleteIdentity)
	{
		displayName = "玩家";
		isCompleteIdentity = false;
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return false;
		}
		try
		{
			if (PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observer))
			{
				string text = (mainHero.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					displayName = text;
					isCompleteIdentity = true;
					return true;
				}
			}
		}
		catch
		{
		}
		string text2 = "未知";
		try
		{
			string text3 = (mainHero.Culture?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text2 = text3;
			}
		}
		catch
		{
		}
		string text4 = "未知";
		try
		{
			text4 = PersonaIntroTextRules.BuildAgeBracketLabel(mainHero.Age);
		}
		catch
		{
			text4 = "未知";
		}
		displayName = text2 + text4;
		return !string.IsNullOrWhiteSpace(displayName);
	}
	internal static bool TryBuildPlayerPublicDisplayNameForPrompt(string observerKey, string cultureId, out string displayName, out bool isCompleteIdentity)
	{
		displayName = "玩家";
		isCompleteIdentity = false;
		Hero mainHero = Hero.MainHero;
		if (mainHero == null)
		{
			return false;
		}
		try
		{
			if (PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerKey, cultureId))
			{
				string text = (mainHero.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					displayName = text;
					isCompleteIdentity = true;
					return true;
				}
			}
		}
		catch
		{
		}
		string text2 = "未知";
		try
		{
			string text3 = (mainHero.Culture?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text2 = text3;
			}
		}
		catch
		{
		}
		string text4 = "未知";
		try
		{
			text4 = PersonaIntroTextRules.BuildAgeBracketLabel(mainHero.Age);
		}
		catch
		{
			text4 = "未知";
		}
		displayName = text2 + text4;
		return !string.IsNullOrWhiteSpace(displayName);
	}
	internal static Hero ResolveCurrentPlayerIdentityObserverForPrompt()
	{
		try
		{
			Hero hero = Hero.OneToOneConversationHero;
			if (hero != null && hero != Hero.MainHero)
			{
				return hero;
			}
		}
		catch
		{
		}
		try
		{
			Hero hero2 = MobileParty.ConversationParty?.LeaderHero;
			if (hero2 != null && hero2 != Hero.MainHero)
			{
				return hero2;
			}
		}
		catch
		{
		}
		return null;
	}
	internal static string BuildPlayerPublicDisplayNameForExternal()
	{
		return BuildPlayerPublicDisplayNameForPrompt();
	}
	internal static string BuildPlayerPublicDisplayNameForExternal(Hero observer)
	{
		return BuildPlayerPublicDisplayNameForPrompt(observer);
	}
	internal static string BuildPlayerPublicDisplayNameForExternal(string observerKey, string cultureId)
	{
		return BuildPlayerPublicDisplayNameForPrompt(observerKey, cultureId);
	}
	internal static bool TryResolveActiveKingdomRuledByHeroForPrompt(Hero hero, out Kingdom kingdom)
	{
		kingdom = null;
		if (hero == null)
		{
			return false;
		}
		try
		{
			kingdom = hero.Clan?.Kingdom ?? (hero.MapFaction as Kingdom);
			return kingdom != null
				&& !kingdom.IsEliminated
				&& (kingdom.Leader == hero || kingdom.RulingClan?.Leader == hero);
		}
		catch
		{
			kingdom = null;
			return false;
		}
	}
	internal static string BuildHeroIdentityTitleForPrompt(Hero hero) => PersonaEquipmentPromptCaptureAdapter.CaptureHeroIdentityTitle(hero, CreateHeroIdentityPromptLivePort());
	internal static void GetHeroFactionAndLiegeForPrompt(Hero hero, out string factionName, out string liegeName)
	{
		factionName = "无（独立）";
		liegeName = "无";
		if (hero == null)
		{
			return;
		}
		try
		{
			Kingdom kingdom = hero.Clan?.Kingdom;
			if (kingdom != null)
			{
				string text = (kingdom.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					factionName = text;
				}
				string text2 = (kingdom.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					liegeName = text2;
				}
				if (kingdom.Leader == hero)
				{
					string text3 = (hero.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						liegeName = text3 + "（本人）";
					}
				}
				return;
			}
		}
		catch
		{
		}
		try
		{
			IFaction mapFaction = hero.MapFaction;
			if (mapFaction != null)
			{
				string text4 = (mapFaction.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text4))
				{
					factionName = text4;
				}
				string text5 = (mapFaction.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text5))
				{
					liegeName = text5;
				}
				if (mapFaction.Leader == hero)
				{
					string text6 = (hero.Name?.ToString() ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text6))
					{
						liegeName = text6 + "（本人）";
					}
				}
				return;
			}
		}
		catch
		{
		}
		try
		{
			Clan clan = hero.Clan;
			if (clan != null)
			{
				string text7 = (clan.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text7))
				{
					factionName = text7;
				}
				string text8 = (clan.Leader?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text8))
				{
					liegeName = text8;
				}
			}
		}
		catch
		{
		}
	}
	internal static string BuildFactionLineForPrompt(string label, string factionName, string liegeName)
	{
		string text = (factionName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "无（独立）";
		}
		string text2 = (liegeName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2) || text2 == "无" || text2.EndsWith("（本人）", StringComparison.Ordinal))
		{
			return label + text;
		}
		return label + text + "（效忠：" + text2 + "）";
	}
	internal static string GetHeroCultureNameForPrompt(Hero hero)
	{
		try
		{
			string text = (hero?.Culture?.Name?.ToString() ?? "").Trim();
			return string.IsNullOrWhiteSpace(text) ? "未知文化" : text;
		}
		catch
		{
			return "未知文化";
		}
	}
	internal static string BuildNpcClanRoleHintForPrompt(Hero npcHero)
	{
		try
		{
			if (npcHero == null || npcHero.Clan == null)
			{
				return "";
			}
			Hero leader = npcHero.Clan.Leader;
			if (leader == null)
			{
				return "";
			}
			if (leader == npcHero)
			{
				return "你是家族的族长";
			}
			if (npcHero.Spouse == leader || leader.Spouse == npcHero)
			{
				return "你是家族族长的配偶";
			}
			if (npcHero.Father == leader || npcHero.Mother == leader)
			{
				return npcHero.IsFemale ? "你是家族族长的女儿" : "你是家族族长的儿子";
			}
			if (leader.Father == npcHero || leader.Mother == npcHero)
			{
				return npcHero.IsFemale ? "你是家族族长的母亲" : "你是家族族长的父亲";
			}
		}
		catch
		{
		}
		return "";
	}
	internal static string BuildNpcPlayerKinshipPromptLine(Hero npcHero, bool includeSameClanFallback)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (npcHero == null || mainHero == null || npcHero == mainHero)
			{
				return "";
			}
			string playerDisplayName = GetPlayerDisplayNameForRelationshipPrompt(mainHero);
			string relationText = ResolveNpcPlayerKinshipText(npcHero, mainHero, playerDisplayName, includeSameClanFallback, out bool isPlayerParent);
			if (string.IsNullOrWhiteSpace(relationText))
			{
				return "";
			}
			string addressRule = isPlayerParent
				? "称呼要求：你必须按这条父母关系称呼" + playerDisplayName + "；只有系统明确写出父亲/母亲关系时才可使用父母称呼。"
				: "称呼要求：你必须按这条关系称呼" + playerDisplayName + "；不要把" + playerDisplayName + "称为父亲或母亲，同族、族长或关系亲近都不等于父母。";
			return "NPC与" + playerDisplayName + "的关系：" + relationText + "。" + addressRule;
		}
		catch
		{
			return "";
		}
	}
	internal static string BuildNpcPlayerKinshipPromptLineForExternal(Hero npcHero)
	{
		return BuildNpcPlayerKinshipPromptLine(npcHero, includeSameClanFallback: true);
	}
	internal static string ResolveNpcPlayerKinshipText(Hero npcHero, Hero mainHero, string playerDisplayName, bool includeSameClanFallback, out bool isPlayerParent)
	{
		isPlayerParent = false;
		if (npcHero.Spouse == mainHero || mainHero.Spouse == npcHero)
		{
			return playerDisplayName + "是你的" + (mainHero.IsFemale ? "妻子" : "丈夫");
		}
		if (npcHero.Father == mainHero)
		{
			isPlayerParent = true;
			return playerDisplayName + "是你的父亲";
		}
		if (npcHero.Mother == mainHero)
		{
			isPlayerParent = true;
			return playerDisplayName + "是你的母亲";
		}
		if (ContainsHero(mainHero.Children, npcHero))
		{
			isPlayerParent = true;
			return playerDisplayName + "是你的" + (mainHero.IsFemale ? "母亲" : "父亲");
		}
		if (mainHero.Father == npcHero || mainHero.Mother == npcHero || ContainsHero(npcHero.Children, mainHero))
		{
			return playerDisplayName + "是你的" + (mainHero.IsFemale ? "女儿" : "儿子");
		}
		if (ShareKnownParent(npcHero, mainHero))
		{
			return playerDisplayName + "是你的" + BuildPlayerSiblingLabelForPrompt(npcHero, mainHero);
		}
		if (includeSameClanFallback && npcHero.Clan != null && mainHero.Clan != null && npcHero.Clan == mainHero.Clan)
		{
			string clanName = (npcHero.Clan.Name?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(clanName))
			{
				clanName = "同一家族";
			}
			return playerDisplayName + "与你同属" + clanName + "，但系统没有明确父母、子女或配偶关系";
		}
		return "";
	}
	internal static string GetPlayerDisplayNameForRelationshipPrompt(Hero mainHero)
	{
		string text = (BuildPlayerPublicDisplayNameForPrompt() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.Equals(text, "玩家", StringComparison.Ordinal))
		{
			text = (mainHero?.Name?.ToString() ?? "").Trim();
		}
		return string.IsNullOrWhiteSpace(text) ? "玩家" : text;
	}
	internal static bool ContainsHero(IEnumerable<Hero> heroes, Hero target)
	{
		if (heroes == null || target == null)
		{
			return false;
		}
		try
		{
			foreach (Hero hero in heroes)
			{
				if (hero == target)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}
	internal static bool ShareKnownParent(Hero left, Hero right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		return (left.Father != null && left.Father == right.Father) || (left.Mother != null && left.Mother == right.Mother);
	}
	internal static string BuildPlayerSiblingLabelForPrompt(Hero npcHero, Hero mainHero)
	{
		try
		{
			float playerAge = mainHero?.Age ?? 0f;
			float npcAge = npcHero?.Age ?? 0f;
			if (playerAge > 0f && npcAge > 0f)
			{
				if (playerAge >= npcAge + 1f)
				{
					return mainHero.IsFemale ? "姐姐" : "哥哥";
				}
				if (npcAge >= playerAge + 1f)
				{
					return mainHero.IsFemale ? "妹妹" : "弟弟";
				}
			}
		}
		catch
		{
		}
		return mainHero != null && mainHero.IsFemale ? "姐妹" : "兄弟";
	}
	internal static int GetDaysInSeasonSafeForPrompt()
	{
		try
		{
			int daysInSeason = CampaignTime.DaysInSeason;
			if (daysInSeason > 0)
			{
				return daysInSeason;
			}
		}
		catch
		{
		}
		return 21;
	}
	internal static int GetDaysInYearSafeForPrompt()
	{
		try
		{
			int daysInYear = CampaignTime.DaysInYear;
			if (daysInYear > 0)
			{
				return daysInYear;
			}
		}
		catch
		{
		}
		return GetDaysInSeasonSafeForPrompt() * 4;
	}
	internal static string GetSeasonTextZhForPrompt(int seasonIndexZeroBased)
	{
		int num = seasonIndexZeroBased % 4;
		if (num < 0)
		{
			num += 4;
		}
		return num switch
		{
			0 => "春",
			1 => "夏",
			2 => "秋",
			_ => "冬",
		};
	}
	internal static int GetCurrentMemoryGameHourForExternal()
	{
		return GetCurrentHourOfDaySafeForPrompt();
	}
	internal static string GetTimeOfDayTextZhForPrompt(int hourOfDay)
	{
		string dayNightText = IsDayTimeForPrompt(hourOfDay) ? "白天" : "夜晚";
		string detailText = GetTimeOfDayDetailTextZhForPrompt(hourOfDay);
		if (string.Equals(dayNightText, detailText, StringComparison.Ordinal))
		{
			return dayNightText;
		}
		return dayNightText + "，" + detailText;
	}
	internal static bool IsDayTimeForPrompt(int hourOfDay)
	{
		try
		{
			return CampaignTime.Now.IsDayTime;
		}
		catch
		{
		}
		try
		{
			int hoursInDay = CampaignTime.HoursInDay;
			if (hoursInDay > 0)
			{
				int sunrise = CampaignTime.SunRise % hoursInDay;
				if (sunrise < 0)
				{
					sunrise += hoursInDay;
				}
				int sunset = CampaignTime.SunSet % hoursInDay;
				if (sunset < 0)
				{
					sunset += hoursInDay;
				}
				int hour = hourOfDay % hoursInDay;
				if (hour < 0)
				{
					hour += hoursInDay;
				}
				if (sunrise == sunset)
				{
					return true;
				}
				if (sunrise < sunset)
				{
					return hour >= sunrise && hour < sunset;
				}
				return hour >= sunrise || hour < sunset;
			}
		}
		catch
		{
		}
		return hourOfDay >= 5 && hourOfDay <= 17;
	}
	internal static string GetTimeOfDayDetailTextZhForPrompt(int hourOfDay)
	{
		if (hourOfDay >= 2 && hourOfDay <= 4)
		{
			return "黎明";
		}
		if (hourOfDay >= 5 && hourOfDay <= 10)
		{
			return "早晨";
		}
		if (hourOfDay >= 11 && hourOfDay <= 13)
		{
			return "中午";
		}
		if (hourOfDay >= 14 && hourOfDay <= 17)
		{
			return "下午";
		}
		if (hourOfDay >= 18 && hourOfDay <= 22)
		{
			return hourOfDay == 22 ? "夜晚" : "傍晚";
		}
		return "深夜";
	}
	internal static string BuildCurrentDateFactForPrompt()
	{
		try
		{
			int num = Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
			int daysInSeasonSafeForPrompt = GetDaysInSeasonSafeForPrompt();
			int daysInYearSafeForPrompt = GetDaysInYearSafeForPrompt();
			int num2 = num / Math.Max(1, daysInYearSafeForPrompt);
			int num3 = num % Math.Max(1, daysInYearSafeForPrompt);
			int seasonIndexZeroBased = num3 / Math.Max(1, daysInSeasonSafeForPrompt);
			int num4 = num3 % Math.Max(1, daysInSeasonSafeForPrompt) + 1;
			string seasonTextZhForPrompt = GetSeasonTextZhForPrompt(seasonIndexZeroBased);
			string text = "";
			try
			{
				text = CampaignTime.Now.ToString();
			}
			catch
			{
				text = "";
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = $"{num2}年{seasonTextZhForPrompt}第{num4}天";
			}
			int currentHourOfDaySafeForPrompt = GetCurrentHourOfDaySafeForPrompt();
			string timeOfDayTextZhForPrompt = GetTimeOfDayTextZhForPrompt(currentHourOfDaySafeForPrompt);
			return $"当前游戏日期：{text}{currentHourOfDaySafeForPrompt}时，{timeOfDayTextZhForPrompt}（绝对天数第 {num} 天；{num2}年{seasonTextZhForPrompt}第{num4}天；当日第 {currentHourOfDaySafeForPrompt} 时）";
		}
		catch
		{
			return "";
		}
	}
	internal static bool DoesPlayerNotorietyObserverKnowPlayer(Hero observerHero, CharacterObject observerCharacter = null, int targetAgentIndex = -1)
	{
		try
		{
			Hero hero = observerHero ?? observerCharacter?.HeroObject;
			if (hero != null)
			{
				return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(hero);
			}
			string observerKey = PromptRuleCaptureBannerlordAdapter.ResolveRuleTargetKey(null, observerCharacter, targetAgentIndex);
			string cultureId = (observerCharacter?.Culture?.StringId ?? "").Trim();
			return PlayerNotorietyBehavior.DoesObserverKnowPlayerForExternal(observerKey, cultureId);
		}
		catch
		{
			return false;
		}
	}
    internal static HeroIdentityPromptLivePort CreateHeroIdentityPromptLivePort() => new() { ResolveRuledKingdom = TryResolveActiveKingdomRuledByHeroForPrompt };
    internal static string CaptureHeroIdentityTitle(Hero hero) => PersonaEquipmentPromptCaptureAdapter.CaptureHeroIdentityTitle(hero, CreateHeroIdentityPromptLivePort());

	internal static int ResolvePlayerClanTierForPrompt(int cachedTier)
	{
		int tier = cachedTier;
		if (tier > 0)
		{
			return tier;
		}
		try
		{
			tier = Clan.PlayerClan?.Tier ?? 0;
		}
		catch
		{
		}
		if (tier <= 0)
		{
			try
			{
				tier = (Hero.MainHero?.Clan?.Tier).GetValueOrDefault();
			}
			catch
			{
			}
		}
		return tier;
	}

internal static string BuildPlayerAddressedInputForName(string npcName, string playerText, Hero observer = null, string actualTargetName = null)
	{
		string text = (playerText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = BuildPlayerPublicDisplayNameForPrompt(observer);
		string text3 = (actualTargetName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = (npcName ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "该NPC";
		}
		return text2 + "对" + text3 + "说: " + text;
	}
internal const int MarriageCandidateMinAgeForPrompt = 18;
internal const int MarriageCandidateMaxAgeForPrompt = 55;
internal const int MarriageCandidateMaxAgeGapForPrompt = 25;
internal static IEnumerable<Hero> GetClanMembersForPrompt(Clan clan)
	{
		if (clan == null)
		{
			yield break;
		}
		IEnumerable enumerable = null;
		try
		{
			PropertyInfo property = clan.GetType().GetProperty("Lords", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				enumerable = property.GetValue(clan, null) as IEnumerable;
			}
			if (enumerable == null)
			{
				PropertyInfo property2 = clan.GetType().GetProperty("Heroes", BindingFlags.Instance | BindingFlags.Public);
				if (property2 != null)
				{
					enumerable = property2.GetValue(clan, null) as IEnumerable;
				}
			}
		}
		catch
		{
			enumerable = null;
		}
		if (enumerable == null)
		{
			yield break;
		}
		HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (object item in enumerable)
		{
			if (item is Hero hero && hero != null)
			{
				string text = (hero.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text) && yielded.Add(text))
				{
					yield return hero;
				}
			}
		}
	}
internal static int GetMarriageCandidateMaxAgeSettingForPrompt()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAge).GetValueOrDefault(MarriageCandidateMaxAgeForPrompt);
			if (valueOrDefault < MarriageCandidateMinAgeForPrompt)
			{
				valueOrDefault = MarriageCandidateMinAgeForPrompt;
			}
			if (valueOrDefault > 80)
			{
				valueOrDefault = 80;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeForPrompt;
		}
	}
internal static int GetMarriageCandidateMaxAgeGapSettingForPrompt()
	{
		try
		{
			int valueOrDefault = (DuelSettings.GetSettings()?.MarriageCandidateMaxAgeGap).GetValueOrDefault(MarriageCandidateMaxAgeGapForPrompt);
			if (valueOrDefault < 0)
			{
				valueOrDefault = 0;
			}
			if (valueOrDefault > 60)
			{
				valueOrDefault = 60;
			}
			return valueOrDefault;
		}
		catch
		{
			return MarriageCandidateMaxAgeGapForPrompt;
		}
	}
internal static bool IsMarriageGenderCompatibleForPrompt(Hero candidate, Hero player)
	{
		return true;
	}
internal static bool IsMarriageAgeCompatibleForPrompt(Hero candidate, Hero player)
	{
		try
		{
			if (candidate == null)
			{
				return false;
			}
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			if (candidate.Age < (float)MarriageCandidateMinAgeForPrompt || candidate.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
			{
				return false;
			}
			if (player != null)
			{
				return Math.Abs(candidate.Age - player.Age) <= (float)GetMarriageCandidateMaxAgeGapSettingForPrompt();
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
internal static bool IsMarriageCandidateForPrompt(Hero hero, Hero player)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (!hero.IsAlive || hero.IsDead)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (!IsMarriageAgeCompatibleForPrompt(hero, player))
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (hero.IsPrisoner)
			{
				return false;
			}
		}
		catch
		{
		}
		if (player != null && hero == player)
		{
			return false;
		}
		if (!IsMarriageGenderCompatibleForPrompt(hero, player))
		{
			return false;
		}
		return true;
	}
internal static bool IsMarriagePoolCandidateForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (!hero.IsAlive || hero.IsDead)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			int marriageCandidateMaxAgeSettingForPrompt = GetMarriageCandidateMaxAgeSettingForPrompt();
			if (hero.Age < (float)MarriageCandidateMinAgeForPrompt || hero.Age > (float)marriageCandidateMaxAgeSettingForPrompt)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		try
		{
			if (hero.IsPrisoner)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}
internal static string GetMarriageCandidateGenderLabelForPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "未知";
		}
		try
		{
			return hero.IsFemale ? "女" : "男";
		}
		catch
		{
			return "未知";
		}
	}
internal static string GetNativeSpouseLabelForMarriagePrompt(Hero hero)
	{
		try
		{
			Hero spouse = hero?.Spouse;
			if (spouse != null)
			{
				return " | 原版当前配偶=" + (spouse.Name?.ToString() ?? spouse.StringId ?? "未知");
			}
		}
		catch
		{
		}
		return "";
	}
internal static string BuildClanUnmarriedCandidatesForPrompt(Hero npcHero, Hero player, int maxEntries = 12)
	{
		try
		{
			Clan clan = npcHero?.Clan;
			if (clan == null)
			{
				return "无（目标无家族）";
			}
			Hero leader = clan.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(clan)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（该家族当前没有基础适婚条件下的可婚配成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				Hero hero = list[i];
				string text = hero.Name?.ToString() ?? ("Hero#" + i);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(hero);
				int num = (int)Math.Round(hero.Age);
				string text2 = "";
				if (hero == npcHero)
				{
					text2 = "（你自己）";
				}
				else if (hero == leader)
				{
					text2 = "（族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num}" + GetNativeSpouseLabelForMarriagePrompt(hero) + $" | targetHeroId={hero.StringId}");
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "无（生成可婚配成员名单时发生异常）";
		}
	}
internal static string BuildPlayerClanUnmarriedCandidatesForPrompt(Hero playerHero, Hero targetHero, int maxEntries = 12)
	{
		try
		{
			Clan clan = playerHero?.Clan;
			if (clan == null)
			{
				return "无（玩家无家族）";
			}
			Hero leader = clan.Leader;
			List<Hero> list = (from h in GetClanMembersForPrompt(clan)
				where IsMarriagePoolCandidateForPrompt(h)
				orderby h.Age descending
				select h).Take(Math.Max(1, maxEntries)).ToList();
			if (list.Count <= 0)
			{
				return "无（玩家家族当前没有基础适婚条件下的可婚配成员）";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				Hero hero = list[i];
				string text = hero.Name?.ToString() ?? ("Hero#" + i);
				string marriageCandidateGenderLabelForPrompt = GetMarriageCandidateGenderLabelForPrompt(hero);
				int num = (int)Math.Round(hero.Age);
				string text2 = "";
				if (hero == playerHero)
				{
					text2 = "（玩家本人）";
				}
				else if (hero == leader)
				{
					text2 = "（玩家家族族长）";
				}
				stringBuilder.AppendLine("- " + text + text2 + $" | 性别={marriageCandidateGenderLabelForPrompt} | 年龄={num}" + GetNativeSpouseLabelForMarriagePrompt(hero) + $" | playerClanHeroId={hero.StringId}");
			}
			return stringBuilder.ToString().TrimEnd();
		}
		catch
		{
			return "无（生成玩家家族可婚配成员名单时发生异常）";
		}
	}
internal static string MergeUserHistoryAndInput(string historyContext, string addressedInput)
	{
		string text = (historyContext ?? "").Trim();
		string text2 = (addressedInput ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return text2;
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return text + "\n" + text2;
	}
internal static string JoinPromptSections(params string[] sections) => SceneAgentIdentityPromptCaptureAdapter.JoinPromptSections(sections);
internal static string BuildPlayerCustomPromptRuleBlock() => SceneAgentIdentityPromptCaptureAdapter.BuildPlayerCustomPromptRuleBlock();
internal static string NormalizePlayerHistoryLineForPrompt(string line, string targetDisplayName, bool addressToYou = true)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		DialogueHistoryLedger.TryStripSceneSessionMarker(text, out text, out var _);
		string firstMeetingFact = MemoryHistoryCommitBannerlordAdapter.NormalizeFirstMeetingNpcFactForPrompt(text);
		if (!string.IsNullOrWhiteSpace(firstMeetingFact))
		{
			return firstMeetingFact;
		}
		if (!MemoryRecallInputCaptureAdapter.TryStripPlayerSpeechPrefix(text, out var stripped))
		{
			return text;
		}
		string text2 = BuildPlayerPublicDisplayNameForPrompt();
		string text3 = addressToYou ? "你" : ((targetDisplayName ?? "").Trim());
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "该NPC";
		}
		return text2 + "对" + text3 + "说: " + stripped;
	}

internal static bool TryResolveEquipmentContextForPrompt(Hero hero, out bool useCivilianEquipment)
	{
		useCivilianEquipment = false;
		try
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				useCivilianEquipment = current.DoesMissionRequireCivilianEquipment;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? hero?.CurrentSettlement;
			if (settlement != null)
			{
				useCivilianEquipment = !settlement.IsVillage;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
internal static ItemObject TryGetAgentEquipmentItemForPrompt(Hero hero, EquipmentIndex index)
	{
		if (hero != Hero.MainHero || Agent.Main == null || !Agent.Main.IsActive())
		{
			return null;
		}
		try
		{
			ItemObject item = Agent.Main.SpawnEquipment[index].Item;
			if (item != null)
			{
				return item;
			}
		}
		catch
		{
		}
		try
		{
			return Agent.Main.Equipment[index].Item;
		}
		catch
		{
			return null;
		}
	}
internal static ItemObject TryGetHeroEquipmentItemForPrompt(Hero hero, EquipmentIndex index, bool useCivilianEquipment)
	{
		if (hero == null)
		{
			return null;
		}
		ItemObject itemObject = TryGetAgentEquipmentItemForPrompt(hero, index);
		if (itemObject != null)
		{
			return itemObject;
		}
		try
		{
			Equipment equipment = (useCivilianEquipment ? hero.CivilianEquipment : hero.BattleEquipment);
			if (equipment == null)
			{
				return null;
			}
			return equipment[index].Item;
		}
		catch
		{
			return null;
		}
	}

internal static string BuildSiegeStartNarrative(Settlement settlement, bool isAttacker, SiegeEvent siegeEvent)
	{
		string text = settlement?.Name?.ToString() ?? "某处要塞";
		string text2 = MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(settlement?.MapFaction, "守军");
		string text3 = MemoryEntityIdentityBannerlordAdapter.GetFactionDisplayName(siegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction, "攻方");
		StringBuilder stringBuilder = new StringBuilder(isAttacker ? ("你参与了对" + text2 + "领土" + text + "的围攻。") : ("你参与了" + text + "的守城，对抗" + text3 + "。"));
		string text4 = CampaignBattleRecordCaptureAdapter.BuildTrackedHeroListText(siegeEvent, BattleSideEnum.Attacker, 5);
		string text5 = CampaignBattleRecordCaptureAdapter.BuildTrackedHeroListText(siegeEvent, BattleSideEnum.Defender, 5);
		if (!string.IsNullOrWhiteSpace(text4))
		{
			stringBuilder.Append(" 攻方领主：").Append(text4).Append('。');
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			stringBuilder.Append(" 守方领主：").Append(text5).Append('。');
		}
		return stringBuilder.ToString();
	}
internal static string BuildNobleEtiquettePromptForHero(Hero npcHero)
	{
		try
		{
			if (npcHero?.Clan == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder("【贵族礼仪常驻提示】虽然你有自己的个性，但身为贵族/有家族身份的人，你应保持基本礼仪，在个性与礼仪之间取得平衡。");
			int npcClanTier = Math.Max(0, npcHero.Clan.Tier);
			int playerClanTier = Math.Max(0, Hero.MainHero?.Clan?.Tier ?? 0);
			if (playerClanTier > npcClanTier)
			{
				stringBuilder.Append("尤其当你面前的人家族声望 level 比你高时，应更加克制、尊重，不要无故辱骂或轻慢。");
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return "";
		}
	}
internal static string BuildNpcInventorySummaryHeader(Hero npcHero)
    {
        string name = npcHero?.Name?.ToString();
        bool lord = npcHero != null && npcHero != Hero.MainHero && npcHero.IsLord;
        return PersonaIntroTextRules.BuildNpcInventorySummaryHeader(name, lord);
    }

internal sealed class HeroIdentityInfoSnapshot
{
    internal readonly int Tier;
    internal readonly string Clan;
    internal readonly string Faction;
    internal readonly string Liege;
    internal readonly string Title;
    internal readonly string Culture;
    internal readonly string ClanRole;
    internal readonly string Age;
    internal readonly string Equipment;
    internal readonly int Gold;
    internal readonly string Kinship;
    internal readonly bool HasLeader;
    internal readonly bool LeaderIsSelf;
    internal readonly string LeaderName;
    internal readonly bool IncludeRules;
    internal readonly bool HasFaction;
    internal readonly bool IncludeMarriage;
    internal readonly int MarriageMaxAge;
    internal readonly string MarriageCandidates;
    internal readonly string TradeSummary;
    internal readonly string InventoryHeader;
    internal readonly string InventorySummary;
    internal HeroIdentityInfoSnapshot(int tier, string clan, string faction, string liege, string title, string culture, string clanRole, string age, string equipment, int gold, string kinship, bool hasLeader, bool leaderIsSelf, string leaderName, bool includeRules, bool hasFaction, bool includeMarriage, int marriageMaxAge, string marriageCandidates, string tradeSummary, string inventoryHeader, string inventorySummary)
    {
        Tier=tier;
        Clan=clan;
        Faction=faction;
        Liege=liege;
        Title=title;
        Culture=culture;
        ClanRole=clanRole;
        Age=age;
        Equipment=equipment;
        Gold=gold;
        Kinship=kinship;
        HasLeader=hasLeader;
        LeaderIsSelf=leaderIsSelf;
        LeaderName=leaderName;
        IncludeRules=includeRules;
        HasFaction=hasFaction;
        IncludeMarriage=includeMarriage;
        MarriageMaxAge=marriageMaxAge;
        MarriageCandidates=marriageCandidates;
        TradeSummary=tradeSummary;
        InventoryHeader=inventoryHeader;
        InventorySummary=inventorySummary;
    }
}
internal static string BuildHeroEquipmentSummaryForPrompt(Hero hero,int maxEntries=8)
    => EquipmentPromptCaptureAdapter.BuildHeroEquipmentSummaryForPrompt(hero,new HeroEquipmentPromptLivePort {ResolveContext=TryResolveEquipmentContextForPrompt,GetItem=TryGetHeroEquipmentItemForPrompt},maxEntries);
internal static string BuildPlayerIdentityInfoForPrompt(Hero playerHero, bool includeRuleGatedFields, bool includeTradePricing, bool includeGuidePriceDetails, bool includeMarriageCandidates = false, Hero targetHero = null)
	{
		if (playerHero == null)
		{
			return "";
		}
		int num = 0;
		string text = "无家族";
		try
		{
			num = playerHero.Clan?.Tier ?? 0;
			string text2 = (playerHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(playerHero, out var factionName, out var liegeName);
		bool flag = false;
		try
		{
			flag = playerHero.Clan?.Kingdom != null;
		}
		catch
		{
			flag = false;
		}
		string text3 = BuildHeroIdentityTitleForPrompt(playerHero);
		string heroCultureNameForPrompt = GetHeroCultureNameForPrompt(playerHero);
		string text4 = PersonaIntroTextRules.BuildAgeBracketLabel(playerHero.Age);
		string text5 = BuildHeroEquipmentSummaryForPrompt(playerHero);
		string value = "";
		if (includeTradePricing)
		{
			try
			{
				if (RewardSystemBehavior.Instance != null)
				{
					value = RewardSystemBehavior.Instance.BuildVisibleEquipmentValueSummaryForAI(playerHero, useGuidePrice: includeGuidePriceDetails);
				}
			}
			catch
			{
			}
		}
        int marriageMaxAge = 0;
        string marriageCandidates = "";
        if (includeMarriageCandidates)
        {
            marriageMaxAge = GetMarriageCandidateMaxAgeSettingForPrompt();
            marriageCandidates = BuildPlayerClanUnmarriedCandidatesForPrompt(playerHero, targetHero, 12);
        }
        var snapshot = new HeroIdentityInfoSnapshot(num,text,factionName,liegeName,text3,heroCultureNameForPrompt,"",text4,text5,0,"",false,false,"",includeRuleGatedFields,flag,includeMarriageCandidates,marriageMaxAge,marriageCandidates,value,"","");
        return AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer.ComposePlayerIdentityInfo(snapshot);
    }
internal static string BuildNpcIdentityInfoForPrompt(Hero npcHero, bool includeTradePricing, bool includeMarriageCandidates = false)
	{
		if (npcHero == null)
		{
			return "";
		}
		int num = 0;
		string text = "无家族";
		try
		{
			num = npcHero.Clan?.Tier ?? 0;
			string text2 = (npcHero.Clan?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
		}
		catch
		{
		}
		GetHeroFactionAndLiegeForPrompt(npcHero, out var factionName, out var liegeName);
		string text3 = BuildHeroIdentityTitleForPrompt(npcHero);
		string heroCultureNameForPrompt = GetHeroCultureNameForPrompt(npcHero);
		string text4 = BuildNpcClanRoleHintForPrompt(npcHero);
		string text5 = PersonaIntroTextRules.BuildAgeBracketLabel(npcHero.Age);
		string text6 = BuildHeroEquipmentSummaryForPrompt(npcHero);
		int num2 = 0;
		try
		{
			num2 = Math.Max(0, npcHero.Gold);
		}
		catch
		{
			num2 = 0;
		}
		Hero hero = null;
		try
		{
			hero = npcHero.Clan?.Leader;
		}
		catch
		{
			hero = null;
		}
        string kinship = BuildNpcPlayerKinshipPromptLine(npcHero, includeSameClanFallback: true);
        bool leaderIsSelf = hero != null && hero == npcHero;
        string leaderName = hero != null && !leaderIsSelf ? hero.Name?.ToString() ?? "未知" : "";
        int marriageMaxAge = 0;
        string marriageCandidates = "";
        if (includeMarriageCandidates)
        {
            marriageMaxAge = GetMarriageCandidateMaxAgeSettingForPrompt();
            marriageCandidates = BuildClanUnmarriedCandidatesForPrompt(npcHero, Hero.MainHero, 12);
        }
        string inventoryHeader = "", inventorySummary = "";
        if (includeTradePricing && RewardSystemBehavior.Instance != null)
        {
            try
            {
                MentionedWorldEntities mentions = AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal();
                int promptListMax = PromptListRetrievalService.GetMaxCandidateCount();
                string candidate = RewardSystemBehavior.Instance.BuildFilteredInventorySummaryForAI(npcHero, mentions, promptListMax, includePrivateBattleEquipment: includeTradePricing);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    inventoryHeader = BuildNpcInventorySummaryHeader(npcHero);
                    inventorySummary = candidate;
                    Logger.Log("Logic", "[Context] InventorySummary=\n" + candidate);
                }
            }
            catch { }
        }
        var snapshot = new HeroIdentityInfoSnapshot(num,text,factionName,liegeName,text3,heroCultureNameForPrompt,text4,text5,text6,num2,kinship,hero!=null,leaderIsSelf,leaderName,true,true,includeMarriageCandidates,marriageMaxAge,marriageCandidates,"",inventoryHeader,inventorySummary);
        return AnimusForge.Refactor.Modules.ScenePromptMessageProjectionComposer.ComposeNpcIdentityInfo(snapshot);
    }
}
