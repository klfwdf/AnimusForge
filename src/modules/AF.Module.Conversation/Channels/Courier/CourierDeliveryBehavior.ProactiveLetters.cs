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
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender) ?? Hero.MainHero?.Name?.ToString() ?? "玩家";
		string bondFact = "[AFEF NPC行为补充] " + npcName + "与" + playerName + "已有私人交往。" + DescribeNpcInitiatedLetterBond(candidate);
		if (candidate.PrivateLove > 20 && !ProactiveNpcRequestBehavior.IsGreetingUnavailableForExternal())
		{
			AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
			{
				MotiveType = LetterMotiveGreeting,
				LetterKind = InboundLetterKindPersonal,
				NeedType = "Greeting",
				IntentText = "你决定主动写一封问候信，关心玩家近况。不要虚构任何事件或请求。",
				FactText = bondFact + " " + npcName + "决定主动写信问候" + playerName + "。",
				FallbackLetter = playerName + "，许久未曾好好问候。愿你近来一切安好；得空时，也请回信告诉我你的近况。"
			});
		}
		AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
		{
			MotiveType = LetterMotiveStatus,
			LetterKind = InboundLetterKindPersonal,
			IntentText = "你决定主动写一封近况信，只能使用当前地点、身份和已提供的真实经历。",
			FactText = bondFact + " " + npcName + "决定把自己当前可确认的近况写给" + playerName + "。",
			FallbackLetter = playerName + "，我写信只是想报个平安，也想知道你近来身在何处。若方便，请给我回信。"
		});
		AddNpcLetterMotive(candidate, settings, nowDays, new NpcInitiatedLetterMotive
		{
			MotiveType = LetterMotiveEmotion,
			LetterKind = InboundLetterKindPersonal,
			IntentText = "按两人的真实交情表达关切或感受；克制，不夸大感情，也不虚构承诺。",
			FactText = bondFact + " " + npcName + "决定主动写信表达与当前关系程度相符的关切或感受。",
			FallbackLetter = playerName + "，想到我们过去的交往，我觉得有些话还是写下来更合适。愿你平安，也愿我们的信任不被辜负。"
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
				IntentText = "你决定把这件近期真实经历写给玩家。只能围绕该事件，不要改写结果或虚构后续。",
				FactText = bondFact + " [AFEF NPC行为补充] " + npcName + "近期有一段真实经历：" + eventText,
				FallbackLetter = playerName + "，近来发生了一件我想让你知道的事：" + eventText
			});
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
				FallbackLetter = playerName + "，我当前正面临“" + (string.IsNullOrWhiteSpace(selectedNeed.DisplayName) ? "一项具体困难" : selectedNeed.DisplayName) + "”，希望与你商量。若你愿意，请回信。"
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
				FallbackLetter = playerName + "，我希望通过这封信与你讨论一项当前真实存在的外交事务。若你愿意，请回信说明看法。"
			});
		}
	}

	private static string DescribeNpcInitiatedLetterBond(NpcInitiatedLetterCandidate candidate)
	{
		if (candidate == null)
		{
			return "两人之间仍有旧日往来。";
		}
		if (candidate.PrivateLove < -20 || candidate.PersonalTrust < -20)
		{
			return "两人虽有往来，心里仍有芥蒂。";
		}
		if (candidate.BondScore >= 70)
		{
			return "彼此十分亲近，也敢把心里话说出来。";
		}
		if (candidate.BondScore >= 40)
		{
			return "彼此颇为亲近，足以相互信赖。";
		}
		return "彼此熟识，但说话仍留着分寸。";
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
		string senderKingdomName = senderKingdom?.Name?.ToString() ?? senderKingdom?.StringId ?? "unknown kingdom";
		string playerKingdomName = playerKingdom?.Name?.ToString() ?? playerKingdom?.StringId ?? "your kingdom";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "player";
		}
		string kind = (diplomacyKind ?? "").Trim().ToUpperInvariant();
		if (kind == "MAKE_PEACE")
		{
			return senderName + "致" + playerName + "：\n\n我们两国，" + senderKingdomName + "与" + playerKingdomName + "，继续流血只会削弱各自的王冠。我愿意讨论议和，包括无条件和平、贡金方向和期限。若你愿意给出条件，请回信或当面谈判。";
		}
		if (kind == "FORM_ALLIANCE")
		{
			return senderName + "致" + playerName + "：\n\n" + senderKingdomName + "与" + playerKingdomName + "面对共同威胁。若我们结盟，双方都能从中获益。我愿意听取你的条件，也可以讨论期限和互相支持的边界。";
		}
		if (kind == "MAKE_TRADE")
		{
			return senderName + "致" + playerName + "：\n\n" + senderKingdomName + "愿与" + playerKingdomName + "建立贸易协议，让商队与市场都获得更稳定的道路。我愿意讨论协议期限和附带条件。";
		}
		return senderName + "致" + playerName + "：\n\n我希望以国王身份与你讨论两国外交。若你愿意，请回信说明你的条件。";
	}
}
