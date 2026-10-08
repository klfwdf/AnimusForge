using EventRecordEntry = AnimusForge.MyBehavior.EventRecordEntry;
using TaleWorlds.Core;
using TaleWorlds.Library;
using ClanVisualSnapshot = AnimusForge.MyBehavior.ClanVisualSnapshot;
using RebelFactionColorChoice = AnimusForge.MyBehavior.RebelFactionColorChoice;
using TaleWorlds.CampaignSystem.Extensions;
using KingdomRebellionCandidateInfo = AnimusForge.MyBehavior.KingdomRebellionCandidateInfo;
using KingdomRebellionFollowerInfo = AnimusForge.MyBehavior.KingdomRebellionFollowerInfo;
using System;using System.Collections.Generic;using System.Linq;
using TaleWorlds.CampaignSystem;using TaleWorlds.CampaignSystem.Actions;using TaleWorlds.CampaignSystem.Party;using TaleWorlds.CampaignSystem.Party.PartyComponents;using TaleWorlds.CampaignSystem.Settlements;
namespace AnimusForge;
// Existing domain owners retain rebel identity/stability and their IO scratch; this adapter owns live game transitions only.
internal sealed class KingdomRebellionGameAdapter
{
 private readonly RebelKingdomIdentityOwner _rebels;
 private readonly Func<KingdomStabilityOwner> _state;
 private readonly CampaignKingdomPersistenceAdapter _persistence;
 internal KingdomRebellionGameAdapter(RebelKingdomIdentityOwner rebels,Func<KingdomStabilityOwner> state,CampaignKingdomPersistenceAdapter persistence)
 { _rebels=rebels;_state=state;_persistence=persistence; }
internal void MarkModCreatedRebelKingdom(Kingdom kingdom)
	{
		_rebels.Mark(MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom));
	}

internal bool IsKnownOrLegacyModCreatedRebelKingdom(Kingdom kingdom)
	{
		string id = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom);
		return RebellionRules.IsKnownOrLegacy(id, _rebels.IsKnown(id), _state().Values != null && _state().Values.ContainsKey(id));
	}

internal static void FinalizeMapEventsForKingdomDiscontinuation(Clan clan)
	{
		if (clan == null)
		{
			return;
		}
		foreach (WarPartyComponent warPartyComponent in (clan.WarPartyComponents?.ToList() ?? new List<WarPartyComponent>()))
		{
			MobileParty mobileParty = warPartyComponent?.MobileParty;
			if (mobileParty == null || warPartyComponent?.Party == null || !warPartyComponent.Party.IsActive)
			{
				continue;
			}
			if (mobileParty.MapEvent != null)
			{
				mobileParty.MapEvent.FinalizeEvent();
			}
			if (mobileParty.SiegeEvent != null)
			{
				mobileParty.SiegeEvent.FinalizeSiegeEvent();
			}
		}
		foreach (Settlement settlement in (clan.Settlements?.ToList() ?? new List<Settlement>()))
		{
			if (settlement?.Party?.MapEvent != null)
			{
				settlement.Party.MapEvent.FinalizeEvent();
			}
			if (settlement?.Party?.SiegeEvent != null)
			{
				settlement.Party.SiegeEvent.FinalizeSiegeEvent();
			}
		}
	}

internal bool TryDiscontinueLandlessKingdom(Kingdom kingdom, string reason, bool requireKnownModRebelKingdom, bool allowPlayerKingdom)
	{
		try
		{
			if (!RebellionRules.CanDiscontinue(kingdom != null, kingdom?.IsEliminated ?? false,
				Clan.PlayerClan?.Kingdom == kingdom, kingdom?.Settlements != null && kingdom.Settlements.Count > 0,
				!requireKnownModRebelKingdom || IsKnownOrLegacyModCreatedRebelKingdom(kingdom), allowPlayerKingdom)) return false;
			bool canBeDiscontinued = true;
			CampaignEventDispatcher.Instance.CanKingdomBeDiscontinued(kingdom, ref canBeDiscontinued);
			if (!canBeDiscontinued)
			{
				Logger.Log("KingdomRebellion", "[landless_kingdom_discontinue_vetoed] reason=" + (reason ?? "") + " kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom));
				return false;
			}
			string kingdomId = MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom);
            if (requireKnownModRebelKingdom) MyBehavior.PacifyInheritedRebelWars(kingdom, null);
			int clanCount = kingdom.Clans?.Count ?? 0;
			int settlementCount = kingdom.Settlements?.Count ?? 0;
			foreach (Clan clan in (kingdom.Clans?.ToList() ?? new List<Clan>()))
			{
				if (clan == null)
				{
					continue;
				}
				FinalizeMapEventsForKingdomDiscontinuation(clan);
				if (!clan.IsEliminated && clan.Kingdom == kingdom)
				{
					ChangeKingdomAction.ApplyByLeaveByKingdomDestruction(clan, showNotification: true);
				}
			}
			kingdom.RulingClan = null;
			if (!kingdom.IsEliminated)
			{
				DestroyKingdomAction.Apply(kingdom);
			}
			if (requireKnownModRebelKingdom)
			{
				CleanupModCreatedRebelKingdomState(kingdomId);
			}
			Logger.Log("KingdomRebellion", "[landless_kingdom_discontinued] reason=" + (reason ?? "") + " kingdom=" + kingdomId + " clans=" + clanCount + " settlements=" + settlementCount + " requireKnownModRebelKingdom=" + requireKnownModRebelKingdom);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomRebellion", "[ERROR] landless_kingdom_discontinue failed kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return false;
		}
	}

internal bool TryDiscontinueLandlessModRebelKingdom(Kingdom kingdom, string reason)
	{
		return TryDiscontinueLandlessKingdom(kingdom, reason, requireKnownModRebelKingdom: true, allowPlayerKingdom: false);
	}

internal void TryDiscontinueLandlessModRebelKingdoms(string reason)
	{
		try
		{
			bool scannedKnownRebelKingdom = false;
			foreach (string kingdomId in _rebels.Snapshot().ToList())
			{
				string text = (kingdomId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(text);
				if (kingdom == null)
				{
					CleanupModCreatedRebelKingdomState(text);
					continue;
				}
				scannedKnownRebelKingdom = true;
				TryDiscontinueLandlessModRebelKingdom(kingdom, reason);
			}
			if (scannedKnownRebelKingdom)
			{
				return;
			}
			if (Kingdom.All == null || Kingdom.All.Count == 0)
			{
				return;
			}
			foreach (Kingdom kingdom in Kingdom.All.Where((Kingdom x) => x != null).ToList())
			{
				TryDiscontinueLandlessModRebelKingdom(kingdom, reason);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomRebellion", "[ERROR] landless_mod_rebel_kingdom_scan failed reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

internal void CleanupModCreatedRebelKingdomState(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		_rebels.Remove(text);
		_persistence.RebelStorage?.Remove(text);
		_state().Values?.Remove(text);
		_persistence.StabilityStorage?.Remove(text);
		if (_state().RelationOffsets != null)
		{
			foreach (string key in _state().RelationOffsets.Keys.Where((string x) => !string.IsNullOrWhiteSpace(x) && x.StartsWith(text + "|", StringComparison.OrdinalIgnoreCase)).ToList())
			{
				_state().RelationOffsets.Remove(key);
			}
		}
		if (_persistence.RelationStorage != null)
		{
			foreach (string key2 in _persistence.RelationStorage.Keys.Where((string x) => !string.IsNullOrWhiteSpace(x) && x.StartsWith(text + "|", StringComparison.OrdinalIgnoreCase)).ToList())
			{
				_persistence.RelationStorage.Remove(key2);
			}
		}
	}

internal static RebellionClanFacts CaptureRebellionClanFacts(Clan clan, Kingdom kingdom)
	{
		return new RebellionClanFacts
		{
			Exists = clan != null, KingdomExists = kingdom != null, KingdomEliminated = kingdom?.IsEliminated ?? false,
			PlayerClan = clan != null && clan == Clan.PlayerClan, InKingdom = clan != null && clan.Kingdom == kingdom,
			RulingClan = clan != null && (clan == kingdom?.RulingClan || clan == kingdom?.Leader?.Clan),
			Eliminated = clan?.IsEliminated ?? false, Bandit = clan?.IsBanditFaction ?? false,
			Minor = clan?.IsMinorFaction ?? false, Rebel = clan?.IsRebelClan ?? false,
			Mercenary = clan != null && (clan.IsUnderMercenaryService || clan.IsClanTypeMercenary),
			LeaderExists = clan?.Leader != null, LeaderAlive = clan?.Leader?.IsAlive ?? false,
			LeaderChild = clan?.Leader?.IsChild ?? false, LeaderPrisoner = clan?.Leader?.IsPrisoner ?? false
		};
	}

internal bool TryValidateClanForKingdomRebellion(Clan clan, Kingdom kingdom, bool forceTrigger, out string note, out int relationToKing, out int townCount, out int castleCount, Dictionary<Clan, RebellionClanFacts> captured = null)
	{
		RebellionClanFacts facts;
		if (clan == null || captured == null || !captured.TryGetValue(clan, out facts))
		{
			facts = CaptureRebellionClanFacts(clan, kingdom);
			if (string.IsNullOrEmpty(RebellionRules.CandidatePreflightNote(facts)))
			{
				facts.TownCount = clan.Settlements?.Count(x => x != null && x.IsTown) ?? 0;
				facts.CastleCount = clan.Settlements?.Count(x => x != null && x.IsCastle) ?? 0;
				if (string.IsNullOrEmpty(RebellionRules.CandidateNote(facts, true)))
				{
					try { facts.RelationToKing = kingdom.Leader != null ? clan.Leader.GetRelation(kingdom.Leader) : 0; }
					catch { facts.RelationToKing = 0; }
				}
			}
			if (clan != null && captured != null) captured[clan] = facts;
		}
		relationToKing = facts.RelationToKing; townCount = facts.TownCount; castleCount = facts.CastleCount;
		note = RebellionRules.CandidateNote(facts, forceTrigger);
		return string.IsNullOrEmpty(note);
	}

internal static float ComputeKingdomRebellionCandidateScore(Clan clan, Kingdom kingdom, int relationToKing, int townCount, int castleCount)
	{
		return RebellionRules.CandidateScore(new RebellionClanFacts
		{
			Renown = clan?.Renown ?? 0f, Tier = clan?.Tier ?? 0, Strength = clan?.CurrentTotalStrength ?? 0f,
			TownCount = townCount, CastleCount = castleCount, RelationToKing = relationToKing,
			SameKingCulture = clan?.Leader?.Culture != null && kingdom?.Leader?.Culture != null && clan.Leader.Culture == kingdom.Leader.Culture
		});
	}

internal bool TryValidateClanForRebelFollower(Clan clan, Kingdom kingdom, Clan leaderClan, bool forceTrigger, out string note, out int relationToKing, out int relationToLeader, out int townCount, out int castleCount, Dictionary<Clan, RebellionClanFacts> captured = null)
	{
		note = "";
		relationToKing = 0;
		relationToLeader = 0;
		townCount = 0;
		castleCount = 0;
		note = RebellionRules.FollowerPreflightNote(clan != null, leaderClan != null, clan == leaderClan);
		if (!string.IsNullOrEmpty(note)) return false;
		if (!TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger: true, out note, out relationToKing, out townCount, out castleCount, captured))
		{
			return false;
		}
		Hero leader = clan.Leader;
		Hero leader2 = leaderClan.Leader;
		note = RebellionRules.FollowerLeaderNote(leader != null, leader2 != null);
		if (!string.IsNullOrEmpty(note)) return false;
		try
		{
			relationToLeader = leader.GetRelation(leader2);
		}
		catch
		{
			relationToLeader = 0;
		}
		return true;
	}

internal static float ComputeKingdomRebellionFollowerScore(Clan clan, Kingdom kingdom, Clan leaderClan, int relationToKing, int relationToLeader)
	{
		return RebellionRules.FollowerScore(new RebellionClanFacts
		{
			RelationToKing = relationToKing, RelationToLeader = relationToLeader,
			SameLeaderCulture = clan?.Leader?.Culture != null && leaderClan?.Leader?.Culture != null && clan.Leader.Culture == leaderClan.Leader.Culture,
			SameKingCulture = clan?.Leader?.Culture != null && kingdom?.Leader?.Culture != null && clan.Leader.Culture == kingdom.Leader.Culture
		});
	}

internal List<KingdomRebellionFollowerInfo> EvaluateKingdomRebellionFollowers(Kingdom kingdom, Clan leaderClan, bool forceTrigger, Dictionary<Clan, RebellionClanFacts> captured = null)
	{
		captured ??= new Dictionary<Clan, RebellionClanFacts>();
		List<KingdomRebellionFollowerInfo> list = new List<KingdomRebellionFollowerInfo>();
		if (kingdom == null || leaderClan == null)
		{
			return list;
		}
		IEnumerable<Clan> enumerable = kingdom.Clans ?? Enumerable.Empty<Clan>();
		foreach (Clan clan in enumerable)
		{
			if (clan == null)
			{
				continue;
			}
			KingdomRebellionFollowerInfo kingdomRebellionFollowerInfo = new KingdomRebellionFollowerInfo
			{
				Clan = clan,
				ClanId = MemoryEntityIdentityBannerlordAdapter.GetClanId(clan),
				ClanName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan)
			};
			if (!TryValidateClanForRebelFollower(clan, kingdom, leaderClan, forceTrigger, out var note, out var relationToKing, out var relationToLeader, out var townCount, out var castleCount, captured))
			{
				kingdomRebellionFollowerInfo.Eligible = false;
				kingdomRebellionFollowerInfo.Note = note;
				kingdomRebellionFollowerInfo.RelationToKing = relationToKing;
				kingdomRebellionFollowerInfo.RelationToLeader = relationToLeader;
				kingdomRebellionFollowerInfo.TownCount = Math.Max(0, townCount);
				kingdomRebellionFollowerInfo.CastleCount = Math.Max(0, castleCount);
				kingdomRebellionFollowerInfo.ClanTier = Math.Max(0, clan.Tier);
			}
			else
			{
				kingdomRebellionFollowerInfo.RelationToKing = relationToKing;
				kingdomRebellionFollowerInfo.RelationToLeader = relationToLeader;
				kingdomRebellionFollowerInfo.TownCount = townCount;
				kingdomRebellionFollowerInfo.CastleCount = castleCount;
				kingdomRebellionFollowerInfo.ClanTier = Math.Max(0, clan.Tier);
				kingdomRebellionFollowerInfo.Score = ComputeKingdomRebellionFollowerScore(clan, kingdom, leaderClan, relationToKing, relationToLeader);
				bool flag = RebellionRules.FollowerEligible(relationToKing, relationToLeader);
				bool flag2 = RebellionRules.FollowerFallback();
				kingdomRebellionFollowerInfo.Eligible = flag || flag2;
				int num = relationToLeader - relationToKing;
				if (flag)
				{
					kingdomRebellionFollowerInfo.Note = "满足联合叛乱跟随条件：对叛乱领袖关系减去对国王关系 = " + num + "（>= 15）。";
				}
				else
				{
					kingdomRebellionFollowerInfo.Note = "不满足联合叛乱跟随条件：对叛乱领袖关系减去对国王关系 = " + num + "（需 >= 15）。";
				}
			}
			list.Add(kingdomRebellionFollowerInfo);
		}
		return RebellionRules.Sort(list, x => x.Eligible, x => x.Score, x => x.ClanName);
	}

internal List<KingdomRebellionCandidateInfo> EvaluateKingdomRebellionCandidates(Kingdom kingdom, bool forceTrigger, Dictionary<Clan, RebellionClanFacts> captured = null)
	{
		captured ??= new Dictionary<Clan, RebellionClanFacts>();
		List<KingdomRebellionCandidateInfo> list = new List<KingdomRebellionCandidateInfo>();
		if (kingdom == null)
		{
			return list;
		}
		IEnumerable<Clan> enumerable = kingdom.Clans ?? Enumerable.Empty<Clan>();
		foreach (Clan clan in enumerable)
		{
			if (clan == null)
			{
				continue;
			}
			KingdomRebellionCandidateInfo kingdomRebellionCandidateInfo = new KingdomRebellionCandidateInfo
			{
				Clan = clan,
				ClanId = MemoryEntityIdentityBannerlordAdapter.GetClanId(clan),
				ClanName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(clan)
			};
			if (!TryValidateClanForKingdomRebellion(clan, kingdom, forceTrigger, out var note, out var relationToKing, out var townCount, out var castleCount, captured))
			{
				kingdomRebellionCandidateInfo.Eligible = false;
				kingdomRebellionCandidateInfo.Note = note;
				kingdomRebellionCandidateInfo.RelationToKing = relationToKing;
				kingdomRebellionCandidateInfo.TownCount = Math.Max(0, townCount);
				kingdomRebellionCandidateInfo.CastleCount = Math.Max(0, castleCount);
				kingdomRebellionCandidateInfo.TotalFortificationCount = kingdomRebellionCandidateInfo.TownCount + kingdomRebellionCandidateInfo.CastleCount;
				kingdomRebellionCandidateInfo.ClanTier = Math.Max(0, clan.Tier);
			}
			else
			{
				kingdomRebellionCandidateInfo.Eligible = true;
				kingdomRebellionCandidateInfo.Note = forceTrigger ? "满足强制触发条件。" : "满足自动叛乱条件。";
				kingdomRebellionCandidateInfo.RelationToKing = relationToKing;
				kingdomRebellionCandidateInfo.TownCount = townCount;
				kingdomRebellionCandidateInfo.CastleCount = castleCount;
				kingdomRebellionCandidateInfo.TotalFortificationCount = townCount + castleCount;
				kingdomRebellionCandidateInfo.ClanTier = Math.Max(0, clan.Tier);
				kingdomRebellionCandidateInfo.Score = ComputeKingdomRebellionCandidateScore(clan, kingdom, relationToKing, townCount, castleCount);
			}
			list.Add(kingdomRebellionCandidateInfo);
		}
		return RebellionRules.Sort(list, x => x.Eligible, x => x.Score, x => x.ClanName);
	}

internal static ClanVisualSnapshot CaptureClanVisualSnapshot(Clan clan)
	{
		if (clan == null)
		{
			return null;
		}
		Banner banner = clan.Banner;
		return new ClanVisualSnapshot
		{
			Banner = ((banner != null) ? new Banner(banner) : null),
			Color = clan.Color,
			Color2 = clan.Color2,
			BackgroundColor = (banner?.GetPrimaryColor() ?? clan.Color),
			IconColor = (banner?.GetFirstIconColor() ?? clan.Color2)
		};
	}

internal static void RestoreClanVisualSnapshot(Clan clan, ClanVisualSnapshot snapshot)
	{
		if (clan == null || snapshot == null)
		{
			return;
		}
		clan.Color = snapshot.Color;
		clan.Color2 = snapshot.Color2;
		if (snapshot.Banner != null)
		{
			clan.Banner = new Banner(snapshot.Banner);
		}
		clan.UpdateBannerColor(snapshot.BackgroundColor, snapshot.IconColor);
	}

internal static void MarkClanVisualsDirty(Clan clan)
	{
		if (clan == null)
		{
			return;
		}
		try
		{
			foreach (Settlement settlement in clan.Settlements)
			{
				settlement?.Party?.SetVisualAsDirty();
			}
		}
		catch
		{
		}
		try
		{
			foreach (WarPartyComponent warPartyComponent in clan.WarPartyComponents)
			{
				warPartyComponent?.Party?.SetVisualAsDirty();
			}
		}
		catch
		{
		}
	}

internal static List<uint> GetBannerPaletteColors()
	{
		List<uint> list = new List<uint>();
		HashSet<uint> hashSet = new HashSet<uint>();
		for (int i = 0; i <= 1024; i++)
		{
			uint color = BannerManager.GetColor(i);
			if (color != 3735928559U && hashSet.Add(color))
			{
				list.Add(color);
			}
		}
		return list;
	}

internal static HashSet<uint> CollectUsedFactionPrimaryColors(Kingdom oldKingdom, Clan founderClan)
	{
		HashSet<uint> hashSet = new HashSet<uint>();
		try
		{
			foreach (Kingdom item in Kingdom.All)
			{
				if (item != null && !item.IsEliminated)
				{
					hashSet.Add(item.PrimaryBannerColor);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Clan item2 in Clan.All)
			{
				if (item2 == null || item2 == founderClan || item2.IsEliminated || item2.Kingdom != null)
				{
					continue;
				}
				if (item2.Settlements != null && item2.Settlements.Any((Settlement x) => x != null && (x.IsTown || x.IsCastle)))
				{
					hashSet.Add(item2.Banner?.GetPrimaryColor() ?? item2.Color);
				}
			}
		}
		catch
		{
		}
		if (oldKingdom != null)
		{
			hashSet.Add(oldKingdom.PrimaryBannerColor);
			hashSet.Add(oldKingdom.Color);
		}
		return hashSet;
	}

internal static RebelFactionColorChoice BuildRandomUniqueRebelFactionColors(Clan clan, Kingdom oldKingdom, ClanVisualSnapshot snapshot)
	{
		List<uint> bannerPaletteColors = GetBannerPaletteColors();
		uint num = snapshot?.BackgroundColor ?? RebellionRules.DefaultBackground;
		uint num2 = snapshot?.IconColor ?? uint.MaxValue;
		bool noPalette = bannerPaletteColors.Count == 0;
		var used = noPalette ? new HashSet<uint>() : CollectUsedFactionPrimaryColors(oldKingdom, clan);
		RebellionRules.SelectColors(bannerPaletteColors, used, num, num2, noPalette || BannerManager.GetColorId(num2) >= 0,
			count => MBRandom.RandomInt(count), out uint backgroundColor, out uint iconColor);
		if (noPalette)
		{
			return new RebelFactionColorChoice
			{
				BackgroundColor = backgroundColor, IconColor = iconColor,
				Banner = snapshot?.Banner != null ? new Banner(snapshot.Banner, backgroundColor, iconColor) : null
			};
		}
		Banner banner = snapshot?.Banner ?? clan?.ClanOriginalBanner ?? clan?.Banner;
		Banner banner2 = ((banner != null) ? new Banner(banner, backgroundColor, iconColor) : Banner.CreateRandomClanBanner((clan?.StringId ?? "new_kingdom").GetDeterministicHashCode()));
		banner2.ChangePrimaryColor(backgroundColor);
		if (iconColor != uint.MaxValue)
		{
			banner2.ChangeIconColors(iconColor);
		}
		return new RebelFactionColorChoice
		{
			BackgroundColor = backgroundColor,
			IconColor = iconColor,
			Banner = banner2
		};
	}

internal static void ApplyRebelFactionColorChoiceToClan(Clan clan, RebelFactionColorChoice colorChoice)
	{
		if (clan == null || colorChoice == null)
		{
			return;
		}
		clan.Color = colorChoice.BackgroundColor;
		clan.Color2 = colorChoice.IconColor;
		if (colorChoice.Banner != null)
		{
			clan.Banner = new Banner(colorChoice.Banner);
		}
		clan.UpdateBannerColor(colorChoice.BackgroundColor, colorChoice.IconColor);
	}

internal static void PrepareClanVisualForRebelKingdomCreation(Clan clan, ClanVisualSnapshot snapshot)
	{
		if (clan == null || snapshot == null)
		{
			return;
		}
		uint backgroundColor = snapshot.BackgroundColor;
		uint iconColor = snapshot.IconColor;
		clan.Color = backgroundColor;
		clan.Color2 = iconColor;
		if (snapshot.Banner != null)
		{
			clan.Banner = new Banner(snapshot.Banner, backgroundColor, iconColor);
		}
		clan.UpdateBannerColor(backgroundColor, iconColor);
	}

internal static string BuildHeroBackgroundForRebelNamingPrompt(Hero hero)
	{
		return RebellionNamingRules.HeroBackground(hero != null, hero?.EncyclopediaText?.ToString(), MemoryEntityIdentityBannerlordAdapter.GetHeroDisplayName(hero), MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(hero?.Clan), hero?.Culture?.Name?.ToString());
	}

internal static string BuildKingdomBackgroundForRebelNamingPrompt(Kingdom kingdom, WeeklyEventRecordStateOwner records)
	{
		return RebellionNamingRules.KingdomBackground(kingdom != null, kingdom?.EncyclopediaText?.ToString(), kingdom != null ? WeeklyEventDataImportOwner.GetKingdomOpeningSummary(kingdom.StringId, records.KingdomOpenings) : "", MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(kingdom, "该王国"));
	}

internal static string BuildSettlementBackgroundForRebelNamingPrompt(Settlement settlement)
	{
		return RebellionNamingRules.SettlementBackground(settlement != null, settlement?.EncyclopediaText?.ToString(), MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(settlement), settlement?.IsTown ?? false, settlement?.IsCastle ?? false, settlement?.Culture?.Name?.ToString(), MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(settlement?.MapFaction as Kingdom, "未明确归属王国"));
	}

internal static string BuildRebelBackgroundForNamingPrompt(Kingdom oldKingdom, int weekIndex, WeeklyEventRecordStateOwner records)
	{
		EventRecordEntry weeklyReportRecordByWeek = records.FindWeeklyReportRecordByWeek("kingdom", MemoryEntityIdentityBannerlordAdapter.GetKingdomId(oldKingdom), weekIndex - 1);
		return RebellionNamingRules.RebelBackground(weeklyReportRecordByWeek?.ShortSummary, weeklyReportRecordByWeek?.Summary, MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(oldKingdom, "原王国"));
	}

internal static bool IsDuplicateKingdomName(string text)
	{
		string text2 = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		try
		{
			return Kingdom.All.Any((Kingdom x) => x != null && string.Equals((x.Name?.ToString() ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

internal static Clan FindKingdomClanMatchingBanner(Kingdom kingdom, Banner banner, Clan excludedClan)
	{
		try
		{
			if (kingdom?.Clans == null || banner == null)
			{
				return null;
			}
			return kingdom.Clans.FirstOrDefault((Clan clan) => clan != null && clan != excludedClan && clan.Banner != null && clan.Banner.IsContentsSameWith(banner));
		}
		catch
		{
			return null;
		}
	}

internal static void MarkKingdomBannerVisualsDirty(Kingdom kingdom)
	{
		try
		{
			foreach (Clan clan in (kingdom?.Clans?.ToList() ?? new List<Clan>()))
			{
				MarkClanVisualsDirty(clan);
			}
		}
		catch
		{
		}
	}

internal bool TrySyncModCreatedRebelKingdomBannerToRulingClan(Kingdom kingdom, Clan eventRulingClan, string reason)
	{
		try
		{
			if (kingdom == null || kingdom.IsEliminated || !IsKnownOrLegacyModCreatedRebelKingdom(kingdom))
			{
				return false;
			}
			Clan rulingClan = kingdom.RulingClan;
			if (rulingClan == null || rulingClan.IsEliminated || rulingClan.Kingdom != kingdom || rulingClan.Banner == null)
			{
				Logger.Log("KingdomRebellion", "[ruler_banner_sync_skipped] reason=" + (reason ?? "") + " kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " rulingClan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(rulingClan) + " result=invalid_ruling_clan_or_banner");
				return false;
			}
			Banner banner = kingdom.Banner;
			if (banner != null && banner.IsContentsSameWith(rulingClan.Banner))
			{
				return false;
			}
			Clan clan = FindKingdomClanMatchingBanner(kingdom, banner, rulingClan);
			kingdom.Banner = new Banner(rulingClan.Banner);
			MarkKingdomBannerVisualsDirty(kingdom);
			Logger.Log("KingdomRebellion", "[ruler_banner_synced] reason=" + (reason ?? "") + " kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " previousBannerClan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(clan) + " eventClan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(eventRulingClan) + " rulingClan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(rulingClan) + " result=updated");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomRebellion", "[ERROR] ruler_banner_sync_failed reason=" + (reason ?? "") + " kingdom=" + MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom) + " eventClan=" + MemoryEntityIdentityBannerlordAdapter.GetClanId(eventRulingClan) + " error=" + ex.Message);
			return false;
		}
	}

internal void SyncModCreatedRebelKingdomBannersOnGameLoad()
	{
		try
		{
			foreach (Kingdom kingdom in (Kingdom.All?.ToList() ?? new List<Kingdom>()))
			{
				TrySyncModCreatedRebelKingdomBannerToRulingClan(kingdom, null, "game_load_finished");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("KingdomRebellion", "[ERROR] ruler_banner_load_sync_failed error=" + ex.Message);
		}
	}
}
