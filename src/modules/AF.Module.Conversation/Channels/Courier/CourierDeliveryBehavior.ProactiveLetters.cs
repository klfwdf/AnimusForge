using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private void TryStartNpcInitiatedLetterScan()
	{
		if (_npcInitiatedLetterScan != null)
		{
			return;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings == null || !settings.EnableNpcInitiatedLetters)
		{
			return;
		}
		float nowHours = NowHours();
		int scanHours = ClampInt(settings.NpcInitiatedLetterScanIntervalHours, 1, 168);
		if (settings.NpcInitiatedLetterTestMode)
		{
			scanHours = 1;
		}
		if (nowHours < _nextNpcDiplomacyLetterScanHour)
		{
			return;
		}
		_nextNpcDiplomacyLetterScanHour = nowHours + scanHours;
		float nowDays = NowDays();
		PruneNpcInitiatedLetterState(nowDays);
		if (!settings.NpcInitiatedLetterTestMode && _npcDiplomacyLetterGlobalCooldownUntilDays > nowDays)
		{
			LogNpcInitiatedLetterDebug(settings, "scan skipped globalCooldownRemaining=" + (_npcDiplomacyLetterGlobalCooldownUntilDays - nowDays).ToString("0.0"));
			return;
		}
		if (HasAnyActiveNpcInitiatedInboundCourier())
		{
			LogNpcInitiatedLetterDebug(settings, "scan skipped activeInbound=true");
			return;
		}
		List<Hero> heroes = (Hero.AllAliveHeroes ?? new List<Hero>()).Where(hero => hero != null).ToList();
		int batchSize = Math.Max(1, (int)Math.Ceiling(heroes.Count / (double)NpcInitiatedLetterScanTargetTicks));
		batchSize = Math.Min(NpcInitiatedLetterScanMaxHeroesPerTick, batchSize);
		_npcInitiatedLetterScan = new NpcInitiatedLetterScanState
		{
			Settings = settings,
			Heroes = heroes,
			NowDays = nowDays,
			MinimumBond = settings.NpcInitiatedLetterTestMode ? 0 : ClampInt(settings.NpcInitiatedLetterMinBondScore, 0, 100),
			QuietDays = settings.NpcInitiatedLetterTestMode ? 0 : ClampInt(settings.NpcInitiatedLetterQuietDays, 0, 30),
			PublicTrustCap = ClampInt(settings.NpcInitiatedLetterPublicTrustCap, 0, 100),
			BatchSize = batchSize,
			StartedAtUtcTicks = DateTime.UtcNow.Ticks,
			ActiveInboundSenderIds = BuildActiveInboundCourierSenderIdSnapshot()
		};
		LogNpcInitiatedLetterDebug(settings, "incremental scan started heroes=" + heroes.Count + " batchSize=" + batchSize + " targetTicks=" + NpcInitiatedLetterScanTargetTicks);
	}

	private void ProcessNpcInitiatedLetterScan()
	{
		NpcInitiatedLetterScanState scan = _npcInitiatedLetterScan;
		if (scan == null)
		{
			return;
		}
		DuelSettings currentSettings = DuelSettings.GetSettings();
		if (currentSettings == null || !currentSettings.EnableNpcInitiatedLetters)
		{
			_npcInitiatedLetterScan = null;
			return;
		}
		if (HasAnyActiveNpcInitiatedInboundCourier())
		{
			LogNpcInitiatedLetterDebug(scan.Settings, "incremental scan cancelled activeInbound=true");
			_npcInitiatedLetterScan = null;
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		int processed = 0;
		while (scan.NextIndex < scan.Heroes.Count && processed < scan.BatchSize)
		{
			Hero hero = scan.Heroes[scan.NextIndex++];
			NpcInitiatedLetterCandidate candidate = BuildNpcInitiatedLetterCandidate(hero, scan);
			if (candidate != null)
			{
				scan.Candidates.Add(candidate);
			}
			processed++;
			if (stopwatch.Elapsed.TotalMilliseconds >= NpcInitiatedLetterScanFrameBudgetMilliseconds)
			{
				break;
			}
		}
		if (scan.NextIndex >= scan.Heroes.Count)
		{
			CompleteNpcInitiatedLetterScan(scan);
		}
	}

	private void CompleteNpcInitiatedLetterScan(NpcInitiatedLetterScanState scan)
	{
		if (!ReferenceEquals(_npcInitiatedLetterScan, scan))
		{
			return;
		}
		_npcInitiatedLetterScan = null;
		DuelSettings settings = scan.Settings;
		if (settings == null || !settings.EnableNpcInitiatedLetters || HasAnyActiveNpcInitiatedInboundCourier())
		{
			return;
		}
		if (!settings.NpcInitiatedLetterTestMode && _npcDiplomacyLetterGlobalCooldownUntilDays > scan.NowDays)
		{
			return;
		}
		NpcInitiatedLetterCandidate selected = PickWeightedNpcLetterCandidate(scan.Candidates);
		if (selected == null)
		{
			LogNpcInitiatedLetterDebug(settings, "scan no eligible candidate");
			return;
		}
		float chance = settings.NpcInitiatedLetterTestMode
			? 100f
			: ClampFloat(selected.BondScore * ClampFloat(settings.NpcInitiatedLetterChanceMultiplier, 0f, 2f), 0f, 100f);
		float roll = MBRandom.RandomFloat * 100f;
		if (chance <= 0f || roll >= chance)
		{
			return;
		}
		// Full need evaluation is expensive; only the one candidate that passed the send roll needs it.
		using (PerfProbe.Scope("CourierDelivery.BuildSelectedNpcInitiatedLetterMotives"))
		{
			BuildNpcInitiatedLetterMotives(selected, settings, scan.NowDays);
		}
		LogNpcInitiatedLetterDebug(settings, "candidate selected hero=" + SafeHeroId(selected.Sender) + " bond=" + selected.BondScore + " love=" + selected.PrivateLove + " personalTrust=" + selected.PersonalTrust + " publicTrust=" + selected.PublicTrust + " letterTrust=" + selected.LetterTrust + " silenceDays=" + selected.DaysSinceInteraction + " chance=" + chance.ToString("0.##") + " roll=" + roll.ToString("0.##") + " motives=" + string.Join("|", selected.Motives.Select(x => x.MotiveType)));
		NpcInitiatedLetterMotive motive = PickWeightedNpcLetterMotive(selected.Motives);
		if (motive == null)
		{
			return;
		}
		if (!TryCreateNpcInitiatedLetterSession(selected, motive, out string status))
		{
			Log("npc initiated letter create failed hero=" + SafeHeroId(selected.Sender) + " motive=" + (motive.MotiveType ?? "") + " status=" + (status ?? ""));
			return;
		}
		int senderCooldownDays = settings.NpcInitiatedLetterTestMode ? 0 : CalculateNpcInitiatedSenderCooldownDays(selected.BondScore, settings);
		string senderId = SafeHeroId(selected.Sender);
		if (!string.IsNullOrWhiteSpace(senderId))
		{
			_npcDiplomacyLetterSenderCooldownUntilDays[senderId] = scan.NowDays + senderCooldownDays;
			if (!string.IsNullOrWhiteSpace(motive.MotiveType))
			{
				int fatigueDays = settings.NpcInitiatedLetterTestMode ? 0 : ClampInt(settings.NpcInitiatedLetterMotiveFatigueDays, 0, 120);
				string fatigueKey = BuildNpcLetterMotiveFatigueKey(senderId, motive.MotiveType);
				if (fatigueDays > 0)
				{
					_npcLetterMotiveFatigueUntilDays[fatigueKey] = scan.NowDays + fatigueDays;
				}
				else
				{
					_npcLetterMotiveFatigueUntilDays.Remove(fatigueKey);
				}
			}
		}
		int globalCooldownDays = settings.NpcInitiatedLetterTestMode ? 0 : ClampInt(settings.NpcInitiatedLetterGlobalCooldownDays, 0, 60);
		_npcDiplomacyLetterGlobalCooldownUntilDays = scan.NowDays + globalCooldownDays;
		Log("npc initiated letter started hero=" + senderId + " motive=" + (motive.MotiveType ?? "") + " kind=" + (motive.LetterKind ?? "") + " need=" + (motive.NeedType ?? "") + " bond=" + selected.BondScore + " senderCooldownDays=" + senderCooldownDays + " globalCooldownDays=" + globalCooldownDays);
	}

	private NpcInitiatedLetterCandidate BuildNpcInitiatedLetterCandidate(Hero hero, NpcInitiatedLetterScanState scan)
	{
		if (scan == null)
		{
			return null;
		}
		string senderId = SafeHeroId(hero);
		if (!IsNpcInitiatedLetterSenderEligible(
			hero,
			senderId,
			scan,
			out int lastInteractionDay,
			out bool romanticEligibilityResolved,
			out bool romanticInteractionEligible,
			out string skipReason))
		{
			LogNpcInitiatedLetterDebug(scan.Settings, "candidate skipped hero=" + senderId + " reason=" + skipReason);
			return null;
		}
		if (!scan.Settings.NpcInitiatedLetterTestMode
			&& _npcDiplomacyLetterSenderCooldownUntilDays.TryGetValue(senderId, out float untilDays)
			&& untilDays > scan.NowDays)
		{
			return null;
		}
		int privateLove = ClampInt(RomanceSystemBehavior.Instance?.GetPrivateLove(hero) ?? 0, -100, 100);
		int personalTrust = ClampInt(RewardSystemBehavior.Instance?.GetNpcTrust(hero) ?? 0, -100, 100);
		int publicTrust = ClampInt(RewardSystemBehavior.Instance?.GetPublicTrust(hero) ?? 0, -100, 100);
		int letterTrust = ClampInt(personalTrust + ClampInt(publicTrust, -scan.PublicTrustCap, scan.PublicTrustCap), -100, 100);
		int bondScore = ClampInt((int)Math.Round((privateLove + letterTrust) / 2f), 0, 100);
		if (!romanticEligibilityResolved)
		{
			romanticInteractionEligible = ProactiveNpcRequestBehavior.IsRomanticInteractionEligibleForExternal(hero);
		}
		if (romanticInteractionEligible)
		{
			bondScore = Math.Max(bondScore, privateLove);
		}
		if (bondScore < scan.MinimumBond)
		{
			return null;
		}
		NpcInitiatedLetterCandidate candidate = new NpcInitiatedLetterCandidate
		{
			Sender = hero,
			BondScore = bondScore,
			PrivateLove = privateLove,
			PersonalTrust = personalTrust,
			PublicTrust = publicTrust,
			LetterTrust = letterTrust,
			DaysSinceInteraction = lastInteractionDay < 0 ? 999 : Math.Max(0, (int)Math.Floor(scan.NowDays) - lastInteractionDay)
		};
		candidate.SelectionWeight = Math.Max(1f, bondScore - scan.MinimumBond + 1f) * ClampFloat(candidate.DaysSinceInteraction / 30f, 0.5f, 2f);
		return candidate;
	}

	private bool IsNpcInitiatedLetterSenderEligible(
		Hero hero,
		string senderId,
		NpcInitiatedLetterScanState scan,
		out int lastInteractionDay,
		out bool romanticEligibilityResolved,
		out bool romanticInteractionEligible,
		out string reason)
	{
		lastInteractionDay = -1;
		romanticEligibilityResolved = false;
		romanticInteractionEligible = false;
		reason = "";
		int quietDays = scan?.QuietDays ?? 0;
		float nowDays = scan?.NowDays ?? 0f;
		if (hero == null || hero == Hero.MainHero || hero.IsDead || hero.Age < 18f)
		{
			reason = "invalid_or_underage";
			return false;
		}
		if (hero.IsPrisoner || hero.IsFugitive || hero.PartyBelongedToAsPrisoner != null)
		{
			reason = "unavailable";
			return false;
		}
		MobileParty senderParty = hero.PartyBelongedTo;
		MobileParty mainParty = MobileParty.MainParty;
		if (senderParty == mainParty || hero.PartyBelongedTo == mainParty)
		{
			reason = "same_party";
			return false;
		}
		Settlement playerSettlement = Hero.MainHero?.CurrentSettlement ?? mainParty?.CurrentSettlement;
		if (playerSettlement != null && hero.CurrentSettlement == playerSettlement)
		{
			reason = "same_settlement";
			return false;
		}
		if (senderParty?.MapEvent != null)
		{
			reason = "sender_map_event";
			return false;
		}
		if (senderParty != null && mainParty != null)
		{
			try
			{
				float nearDistance = Math.Max(1f, mainParty.SeeingRange);
				if (senderParty.Position.DistanceSquared(mainParty.Position) <= nearDistance * nearDistance)
				{
					reason = "nearby_for_direct_contact";
					return false;
				}
			}
			catch
			{
			}
		}
		if (scan?.ActiveInboundSenderIds?.Contains(senderId ?? "") == true || ProactiveNpcRequestBehavior.IsActiveRequestHero(hero))
		{
			reason = "active_contact";
			return false;
		}
		lastInteractionDay = MyBehavior.GetLastMeaningfulDialogueDayForExternal(hero);
		bool familiar = lastInteractionDay >= 0
			|| PlayerNotorietyBehavior.HasObserverUnlockedPlayerMajorForExternal(hero);
		if (!familiar)
		{
			romanticEligibilityResolved = true;
			romanticInteractionEligible = ProactiveNpcRequestBehavior.IsRomanticInteractionEligibleForExternal(hero);
			familiar = romanticInteractionEligible;
		}
		if (!familiar)
		{
			reason = "not_familiar";
			return false;
		}
		if (lastInteractionDay >= 0 && nowDays - lastInteractionDay < quietDays)
		{
			reason = "quiet_period";
			return false;
		}
		if (!TryGetNpcCourierStart(hero, out CampaignVec2 courierStart, out _))
		{
			reason = "sender_location_missing";
			return false;
		}
		if (mainParty != null)
		{
			try
			{
				float nearDistance = Math.Max(1f, mainParty.SeeingRange);
				if (courierStart.DistanceSquared(mainParty.Position) <= nearDistance * nearDistance)
				{
					reason = "nearby_for_direct_contact";
					return false;
				}
			}
			catch
			{
			}
		}
		return true;
	}

	private void BuildNpcInitiatedLetterMotives(NpcInitiatedLetterCandidate candidate, DuelSettings settings, float nowDays)
	{
		if (candidate?.Sender == null)
		{
			return;
		}
		Hero sender = candidate.Sender;
		string npcName = sender.Name?.ToString() ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0492");
		string bondFact = ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0493") + npcName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0494") + playerName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0495") + DescribeNpcInitiatedLetterBond(candidate);
		if (candidate.PrivateLove > 20 && !ProactiveNpcRequestBehavior.IsGreetingUnavailableForExternal())
		{
			AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
			{
				MotiveType = LetterMotiveGreeting,
				LetterKind = InboundLetterKindPersonal,
				NeedType = "Greeting",
				IntentText = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0496"),
				FactText = bondFact + " " + npcName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0497") + playerName + "。",
				FallbackLetter = ConversationRuntimeTexts.FormatPrompt("letter_fallback_greeting", ("PLAYER", playerName))
			});
		}
		AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
		{
			MotiveType = LetterMotiveStatus,
			LetterKind = InboundLetterKindPersonal,
			IntentText = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0499"),
			FactText = bondFact + " " + npcName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0500") + playerName + "。",
			FallbackLetter = ConversationRuntimeTexts.FormatPrompt("letter_fallback_status", ("PLAYER", playerName))
		});
		AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
		{
			MotiveType = LetterMotiveEmotion,
			LetterKind = InboundLetterKindPersonal,
			IntentText = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0502"),
			FactText = bondFact + " " + npcName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0503"),
			FallbackLetter = ConversationRuntimeTexts.FormatPrompt("letter_fallback_emotion", ("PLAYER", playerName))
		});
		string senderId = SafeHeroId(sender);
		if (MyBehavior.TryGetLatestNpcRecentActionForExternal(sender, out string eventKey, out string eventText, out int eventDay)
			&& (!_npcLetterLastDeliveredEventKeyBySender.TryGetValue(senderId, out string deliveredKey)
				|| !string.Equals(deliveredKey, eventKey, StringComparison.OrdinalIgnoreCase)))
		{
			AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
			{
				MotiveType = LetterMotiveEvent,
				LetterKind = InboundLetterKindPersonal,
				EventKey = eventKey,
				IntentText = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0505"),
				FactText = bondFact + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0506") + npcName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0507") + eventText,
				FallbackLetter = ConversationRuntimeTexts.FormatPrompt("letter_fallback_event", ("PLAYER", playerName), ("EVENT", eventText))
			});
		}
		foreach (var experience in PlayerNotorietyBehavior.CaptureExperience(sender, true, false))
        {
            AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive {
                MotiveType = experience.Kind, LetterKind = InboundLetterKindPersonal, Experience = experience,
                IntentText = AIConfigHandler.GetProactiveNpcRequestLetterIntent(experience.Kind),
                FactText = ProactiveNpcRequestBehavior.ExperienceFact(experience),
                FallbackLetter = SocialPromptTexts.Get("experience_letter_fallback") + "\n" + experience.Evidence });
        }
        List<ProactiveNpcRequestBehavior.LetterNeedSnapshot> needs = ProactiveNpcRequestBehavior.GetLetterNeedSnapshotsForExternal(sender);
		ProactiveNpcRequestBehavior.LetterNeedSnapshot selectedNeed = PickWeightedLetterNeed((needs ?? new List<ProactiveNpcRequestBehavior.LetterNeedSnapshot>())
			.Where(x => !string.Equals(x?.NeedType, "Diplomacy", StringComparison.OrdinalIgnoreCase))
			.ToList());
		if (selectedNeed != null)
		{
			AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
			{
				MotiveType = LetterMotiveNeed,
				LetterKind = InboundLetterKindNeedRequest,
				NeedType = selectedNeed.NeedType,
				IntentText = selectedNeed.IntentText,
				FactText = bondFact + " " + selectedNeed.FactText,
				FallbackLetter = ConversationRuntimeTexts.FormatPrompt("letter_fallback_need", ("PLAYER", playerName), ("NEED", string.IsNullOrWhiteSpace(selectedNeed.DisplayName) ? ConversationRuntimeTexts.Prompt("letter_need_unknown") : selectedNeed.DisplayName))
			});
		}
		ProactiveNpcRequestBehavior.LetterNeedSnapshot diplomacyNeed = (needs ?? new List<ProactiveNpcRequestBehavior.LetterNeedSnapshot>())
			.FirstOrDefault(x => string.Equals(x?.NeedType, "Diplomacy", StringComparison.OrdinalIgnoreCase));
		if (diplomacyNeed != null)
		{
			AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
			{
				MotiveType = LetterMotiveDiplomacy,
				LetterKind = InboundLetterKindDiplomacy,
				NeedType = diplomacyNeed.NeedType,
				IntentText = diplomacyNeed.IntentText,
				FactText = bondFact + " " + diplomacyNeed.FactText,
				FallbackLetter = ConversationRuntimeTexts.FormatPrompt("letter_fallback_diplomacy", ("PLAYER", playerName))
			});
		}
	}

	private static string DescribeNpcInitiatedLetterBond(NpcInitiatedLetterCandidate candidate)
	{
		if (candidate == null)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0513");
		}
		if (candidate.PrivateLove < -20 || candidate.PersonalTrust < -20)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0514");
		}
		if (candidate.BondScore >= 70)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0515");
		}
		if (candidate.BondScore >= 40)
		{
			return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0516");
		}
		return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0517");
	}

	private void AddNpcLetterMotive(NpcInitiatedLetterCandidate candidate, DuelSettings settings, float nowDays, NpcInitiatedLetterMotive motive)
	{
		if (candidate == null || motive == null || string.IsNullOrWhiteSpace(motive.MotiveType))
		{
			return;
		}
		string fatigueKey = BuildNpcLetterMotiveFatigueKey(SafeHeroId(candidate.Sender), motive.MotiveType);
		motive.Weight = _npcLetterMotiveFatigueUntilDays.TryGetValue(fatigueKey, out float untilDays) && untilDays > nowDays
			? ClampFloat(settings.NpcInitiatedLetterMotiveFatigueMultiplier, 0f, 1f)
			: 1f;
		candidate.Motives.Add(motive);
	}

	private static ProactiveNpcRequestBehavior.LetterNeedSnapshot PickWeightedLetterNeed(List<ProactiveNpcRequestBehavior.LetterNeedSnapshot> needs)
	{
		List<ProactiveNpcRequestBehavior.LetterNeedSnapshot> valid = (needs ?? new List<ProactiveNpcRequestBehavior.LetterNeedSnapshot>())
			.Where(x => x != null && !string.IsNullOrWhiteSpace(x.NeedType) && x.Urgency > 0f && x.Urgency * x.TypeFatigueMultiplier * x.TypeWeightMultiplier > 0f)
			.ToList();
		float total = valid.Sum(x => x.Urgency * x.TypeFatigueMultiplier * x.TypeWeightMultiplier);
		if (total <= 0f)
		{
			return null;
		}
		float roll = MBRandom.RandomFloat * total;
		foreach (ProactiveNpcRequestBehavior.LetterNeedSnapshot need in valid)
		{
			roll -= need.Urgency * need.TypeFatigueMultiplier * need.TypeWeightMultiplier;
			if (roll <= 0f)
			{
				return need;
			}
		}
		return valid.LastOrDefault();
	}

	private static NpcInitiatedLetterCandidate PickWeightedNpcLetterCandidate(List<NpcInitiatedLetterCandidate> candidates)
	{
		List<NpcInitiatedLetterCandidate> valid = (candidates ?? new List<NpcInitiatedLetterCandidate>()).Where(x => x != null && x.SelectionWeight > 0f).ToList();
		float total = valid.Sum(x => x.SelectionWeight);
		float roll = MBRandom.RandomFloat * total;
		foreach (NpcInitiatedLetterCandidate candidate in valid)
		{
			roll -= candidate.SelectionWeight;
			if (roll <= 0f)
			{
				return candidate;
			}
		}
		return valid.LastOrDefault();
	}

	private static NpcInitiatedLetterMotive PickWeightedNpcLetterMotive(List<NpcInitiatedLetterMotive> motives)
	{
		List<NpcInitiatedLetterMotive> valid = (motives ?? new List<NpcInitiatedLetterMotive>()).Where(x => x != null && x.Weight > 0f).ToList();
        if (valid.Any(x => x.Experience != null) && valid.Any(x => x.Experience == null))
        {
            bool chooseExperience = MBRandom.RandomFloat < .5f;
            valid = valid.Where(x => (x.Experience != null) == chooseExperience).ToList();
        }
		float total = valid.Sum(x => x.Weight);
		float roll = MBRandom.RandomFloat * total;
		foreach (NpcInitiatedLetterMotive motive in valid)
		{
			roll -= motive.Weight;
			if (roll <= 0f)
			{
				return motive;
			}
		}
		return valid.LastOrDefault();
	}

	internal static bool HasExperienceSession(string id)
    {
        var owner = Instance;
        if (owner == null) return false;
        lock (owner._sessionLock) return owner._sessions.ContainsKey(id);
    }

    private static int CalculateNpcInitiatedSenderCooldownDays(int bondScore, DuelSettings settings)
	{
		int minScore = ClampInt(settings.NpcInitiatedLetterMinBondScore, 0, 100);
		int lowCooldown = ClampInt(settings.NpcInitiatedLetterLowScoreCooldownDays, 1, 120);
		int highCooldown = ClampInt(settings.NpcInitiatedLetterHighScoreCooldownDays, 1, 60);
		float t = minScore >= 100 ? 1f : ClampFloat((bondScore - minScore) / (float)Math.Max(1, 100 - minScore), 0f, 1f);
		return Math.Max(0, (int)Math.Round(lowCooldown + (highCooldown - lowCooldown) * t));
	}

	private static string BuildNpcLetterMotiveFatigueKey(string senderId, string motiveType)
	{
		return (senderId ?? "").Trim() + "|" + (motiveType ?? "").Trim();
	}

	private void PruneNpcInitiatedLetterState(float nowDays)
	{
		foreach (string key in _npcDiplomacyLetterSenderCooldownUntilDays.Where(x => x.Value <= nowDays).Select(x => x.Key).ToList()) _npcDiplomacyLetterSenderCooldownUntilDays.Remove(key);
		foreach (string key in _npcLetterMotiveFatigueUntilDays.Where(x => x.Value <= nowDays).Select(x => x.Key).ToList()) _npcLetterMotiveFatigueUntilDays.Remove(key);
	}

	private static void LogNpcInitiatedLetterDebug(DuelSettings settings, string message)
	{
		if (settings?.NpcInitiatedLetterDebugLog == true || settings?.NpcInitiatedLetterTestMode == true)
		{
			Log("npc initiated letter debug " + (message ?? ""));
		}
	}

	private void TryStartNpcDiplomacyLetterScan()
	{
		float nowHours = NowHours();
		if (nowHours < _nextNpcDiplomacyLetterScanHour)
		{
			return;
		}
		_nextNpcDiplomacyLetterScanHour = nowHours + NpcDiplomacyLetterScanIntervalHours;
		float nowDays = NowDays();
		if (_npcDiplomacyLetterGlobalCooldownUntilDays > nowDays)
		{
			return;
		}
		if (MBRandom.RandomFloat > NpcDiplomacyLetterSendChance)
		{
			return;
		}
		Hero sender = SelectNpcDiplomacyLetterSender(out string diplomacyKind);
		if (sender == null)
		{
			return;
		}
		string letterText = BuildNpcDiplomacyLetterText(sender, diplomacyKind);
		if (!TryCreateNpcDiplomacyLetterSession(sender, letterText, "hourly_scan:" + diplomacyKind, out string status))
		{
			Log("npc diplomacy letter skipped sender=" + SafeHeroId(sender) + " kind=" + (diplomacyKind ?? "") + " status=" + (status ?? ""));
		}
	}

	private Hero SelectNpcDiplomacyLetterSender(out string diplomacyKind)
	{
		diplomacyKind = "";
		try
		{
			Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
			if (playerKingdom == null || playerKingdom.IsEliminated || Hero.MainHero != playerKingdom.RulingClan?.Leader)
			{
				return null;
			}
			float nowDays = NowDays();
			List<Tuple<Hero, string, float>> candidates = new List<Tuple<Hero, string, float>>();
			foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (kingdom == null || kingdom.IsEliminated || kingdom == playerKingdom)
				{
					continue;
				}
				Hero leader = kingdom.RulingClan?.Leader;
				if (!CanNpcKingSendDiplomacyLetter(leader, playerKingdom, out string reason))
				{
					continue;
				}
				string senderId = SafeHeroId(leader);
				if (!string.IsNullOrWhiteSpace(senderId)
					&& _npcDiplomacyLetterSenderCooldownUntilDays.TryGetValue(senderId, out float untilDays)
					&& untilDays > nowDays)
				{
					continue;
				}
				if (HasActiveInboundCourierFromSender(leader))
				{
					continue;
				}
				if (!TryResolveNpcDiplomacyLetterKind(kingdom, playerKingdom, out string kind, out float urgency))
				{
					continue;
				}
				candidates.Add(new Tuple<Hero, string, float>(leader, kind, urgency + MBRandom.RandomFloat * 5f));
			}
			Tuple<Hero, string, float> selected = candidates.OrderByDescending(x => x.Item3).FirstOrDefault();
			if (selected == null)
			{
				return null;
			}
			diplomacyKind = selected.Item2;
			return selected.Item1;
		}
		catch (Exception ex)
		{
			Log("select npc diplomacy letter sender failed: " + ex.Message);
			return null;
		}
	}

	private bool CanNpcKingSendDiplomacyLetter(Hero sender, Kingdom playerKingdom, out string reason)
	{
		reason = "";
		try
		{
			if (sender == null || sender == Hero.MainHero || sender.IsDead)
			{
				reason = "sender_invalid";
				return false;
			}
			Kingdom senderKingdom = sender.Clan?.Kingdom;
			if (senderKingdom == null || senderKingdom.IsEliminated)
			{
				reason = "sender_no_kingdom";
				return false;
			}
			if (sender != senderKingdom.RulingClan?.Leader)
			{
				reason = "sender_not_king";
				return false;
			}
			if (playerKingdom == null || playerKingdom.IsEliminated || Hero.MainHero != playerKingdom.RulingClan?.Leader)
			{
				reason = "player_not_king";
				return false;
			}
			if (senderKingdom == playerKingdom)
			{
				reason = "same_kingdom";
				return false;
			}
			if (sender.IsPrisoner || sender.IsFugitive || sender.PartyBelongedToAsPrisoner != null)
			{
				reason = "sender_unavailable";
				return false;
			}
			if (!TryGetNpcCourierStart(sender, out _, out _))
			{
				reason = "sender_location_missing";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.Message;
			return false;
		}
	}

	private static bool TryResolveNpcDiplomacyLetterKind(Kingdom senderKingdom, Kingdom playerKingdom, out string kind, out float urgency)
	{
		kind = "";
		urgency = 0f;
		try
		{
			if (senderKingdom == null || playerKingdom == null || senderKingdom == playerKingdom)
			{
				return false;
			}
			if (FactionManager.IsAtWarAgainstFaction(senderKingdom, playerKingdom))
			{
				kind = "MAKE_PEACE";
				urgency = 80f;
				return true;
			}
			if (HasCommonEnemyForDiplomacyLetter(senderKingdom, playerKingdom))
			{
				kind = "FORM_ALLIANCE";
				urgency = 62f;
				return true;
			}
			ITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
			if (!BannerlordApiCompat.HasTradeAgreement(tradeBeh, senderKingdom, playerKingdom))
			{
				kind = "MAKE_TRADE";
				urgency = 48f;
				return true;
			}
			return false;
		}
		catch
		{
			kind = "";
			urgency = 0f;
			return false;
		}
	}

	private static bool HasCommonEnemyForDiplomacyLetter(Kingdom first, Kingdom second)
	{
		try
		{
			if (first == null || second == null)
			{
				return false;
			}
			foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (kingdom == null || kingdom.IsEliminated || kingdom == first || kingdom == second)
				{
					continue;
				}
				if (FactionManager.IsAtWarAgainstFaction(first, kingdom) && FactionManager.IsAtWarAgainstFaction(second, kingdom))
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

	private static string BuildNpcDiplomacyLetterText(Hero sender, string diplomacyKind)
	{
		Kingdom senderKingdom = sender?.Clan?.Kingdom;
		Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
		string senderName = sender?.Name?.ToString() ?? "NPC";
		string senderKingdomName = senderKingdom?.Name?.ToString() ?? senderKingdom?.StringId ?? ConversationRuntimeTexts.Prompt("letter_unknown_kingdom");
		string playerKingdomName = playerKingdom?.Name?.ToString() ?? playerKingdom?.StringId ?? ConversationRuntimeTexts.Prompt("letter_player_kingdom");
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0530");
		}
		string kind = (diplomacyKind ?? "").Trim().ToUpperInvariant();
		if (kind == "MAKE_PEACE")
		{
			return ConversationRuntimeTexts.FormatPrompt("letter_diplomacy_peace", ("SENDER", senderName), ("PLAYER", playerName), ("SENDER_KINGDOM", senderKingdomName), ("PLAYER_KINGDOM", playerKingdomName));
		}
		if (kind == "FORM_ALLIANCE")
		{
			return ConversationRuntimeTexts.FormatPrompt("letter_diplomacy_alliance", ("SENDER", senderName), ("PLAYER", playerName), ("SENDER_KINGDOM", senderKingdomName), ("PLAYER_KINGDOM", playerKingdomName));
		}
		if (kind == "MAKE_TRADE")
		{
			return ConversationRuntimeTexts.FormatPrompt("letter_diplomacy_trade", ("SENDER", senderName), ("PLAYER", playerName), ("SENDER_KINGDOM", senderKingdomName), ("PLAYER_KINGDOM", playerKingdomName));
		}
		return ConversationRuntimeTexts.FormatPrompt("letter_diplomacy_formal", ("SENDER", senderName), ("PLAYER", playerName), ("SENDER_KINGDOM", senderKingdomName), ("PLAYER_KINGDOM", playerKingdomName));
	}
}
