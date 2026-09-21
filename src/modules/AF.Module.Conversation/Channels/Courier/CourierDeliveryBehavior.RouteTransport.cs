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
	private static CourierRoutePlan BuildCourierRoutePlan(MobileParty courier, CampaignVec2 targetPosition, Settlement targetSettlement, MobileParty targetParty, bool preferPort)
	{
		bool requiresNaval = ShouldUseNavalRoute(courier, targetPosition, targetSettlement, targetParty, preferPort);
		bool usePort = targetSettlement != null && targetSettlement.HasPort && (requiresNaval || preferPort || courier?.IsCurrentlyAtSea == true || targetParty?.IsCurrentlyAtSea == true);
		return new CourierRoutePlan
		{
			RequiresNaval = requiresNaval,
			UsePort = usePort,
			NavigationType = GetEffectiveCourierNavigationType(courier, requiresNaval),
			Reason = BuildNavalRouteReason(courier, targetPosition, targetSettlement, targetParty, preferPort, requiresNaval)
		};
	}

	private static bool ShouldUseNavalRoute(MobileParty courier, CampaignVec2 targetPosition, Settlement targetSettlement, MobileParty targetParty, bool preferPort)
	{
		try
		{
			if (courier == null || !IsNavalRuntimeAvailable())
			{
				return false;
			}
			if (courier.IsCurrentlyAtSea || !courier.Position.IsOnLand || targetParty?.IsCurrentlyAtSea == true || !targetPosition.IsOnLand)
			{
				return true;
			}
			if (preferPort && targetSettlement?.HasPort == true)
			{
				return true;
			}
			CampaignVec2 landTarget = targetSettlement?.GatePosition ?? targetPosition;
			return IsValidCampaignPosition(courier.Position) && IsValidCampaignPosition(landTarget) && !DefaultLandPathExists(courier.Position, landTarget);
		}
		catch
		{
			return false;
		}
	}

	private static string BuildNavalRouteReason(MobileParty courier, CampaignVec2 targetPosition, Settlement targetSettlement, MobileParty targetParty, bool preferPort, bool requiresNaval)
	{
		if (!requiresNaval)
		{
			return "land";
		}
		if (courier?.IsCurrentlyAtSea == true || courier?.Position.IsOnLand == false)
		{
			return "courier_sea";
		}
		if (targetParty?.IsCurrentlyAtSea == true || !targetPosition.IsOnLand)
		{
			return "target_sea";
		}
		if (preferPort && targetSettlement?.HasPort == true)
		{
			return "prefer_port";
		}
		return "no_land_path";
	}

	private static MobileParty.NavigationType GetEffectiveCourierNavigationType(MobileParty courier, bool requiresNaval)
	{
		if (requiresNaval)
		{
			return MobileParty.NavigationType.All;
		}
		MobileParty.NavigationType navigationType = courier?.NavigationCapability ?? MobileParty.NavigationType.Default;
		return navigationType == MobileParty.NavigationType.None ? MobileParty.NavigationType.Default : navigationType;
	}

	private static bool EnsureCourierNavalReadiness(CourierSession session, MobileParty courier, CourierRoutePlan plan, string reason)
	{
		if (session == null || courier == null || plan == null || !plan.RequiresNaval)
		{
			return true;
		}
		try
		{
			if (courier.Ships != null && courier.Ships.Count > 0)
			{
				MarkExistingCourierTemporaryShips(session, courier);
				if (!courier.HasNavalNavigationCapability)
				{
					LogCourierStatusVerbose("naval_existing_no_cap:" + session.Id, "naval_existing_no_cap session=" + session.Id + " reason=" + (reason ?? "") + " courier=" + DescribeMobileParty(courier) + " tempShipCreated=" + session.TemporaryShipCreated + " hull=" + (session.TemporaryShipHullId ?? ""), 10.0);
				}
				return courier.HasNavalNavigationCapability;
			}
			if (!IsNavalRuntimeAvailable() || courier.Party == null)
			{
				LogVerbose("naval_unavailable:" + session.Id, "courier naval runtime unavailable session=" + session.Id + " reason=" + (reason ?? ""), 10.0);
				LogCourierStatusVerbose("naval_unavailable:" + session.Id, "naval_unavailable session=" + session.Id + " reason=" + (reason ?? "") + " runtime=" + IsNavalRuntimeAvailable() + " hasParty=" + (courier.Party != null) + " courier=" + DescribeMobileParty(courier), 10.0);
				return false;
			}
			ShipHull hull = SelectCourierTemporaryShipHull(courier);
			if (hull == null)
			{
				Log("courier temporary ship hull missing session=" + session.Id + " reason=" + (reason ?? ""));
				LogCourierStatus("temporary_ship_hull_missing session=" + session.Id + " reason=" + (reason ?? "") + " courier=" + DescribeMobileParty(courier));
				return false;
			}
			if (!IsCourierSafeForTemporaryShipOwner(courier, out string ownerReason))
			{
				Log("courier temporary ship owner invalid session=" + session.Id + " reason=" + (reason ?? "") + " ownerReason=" + ownerReason);
				LogCourierStatus("temporary_ship_owner_invalid session=" + session.Id + " reason=" + (reason ?? "") + " ownerReason=" + ownerReason + " courier=" + DescribeMobileParty(courier));
				return false;
			}
			Ship ship = new Ship(hull);
			ship.SetName(new TextObject(TemporaryCourierShipName));
			ship.IsUsedByQuest = true;
			ship.IsInvulnerable = true;
			ChangeShipOwnerAction.ApplyByMobilePartyCreation(courier.Party, ship);
			session.TemporaryShipCreated = true;
			session.TemporaryShipHullId = hull.StringId ?? "";
			session.LastRouteKey = "";
			Log("courier temporary ship created session=" + session.Id + " party=" + (courier.StringId ?? "") + " hull=" + (hull.StringId ?? "") + " reason=" + (reason ?? ""));
			LogCourierStatus("temporary_ship_created session=" + session.Id + " reason=" + (reason ?? "") + " hull=" + (hull.StringId ?? "") + " capacity=" + hull.TotalCrewCapacity + " speed=" + hull.BaseSpeed + " courier=" + DescribeMobileParty(courier));
			return courier.HasNavalNavigationCapability || (courier.Ships != null && courier.Ships.Count > 0);
		}
		catch (Exception ex)
		{
			Log("courier temporary ship create failed session=" + session.Id + " reason=" + (reason ?? "") + " error=" + ex.Message);
			LogCourierStatus("temporary_ship_create_failed session=" + session.Id + " reason=" + (reason ?? "") + " error=" + ex.Message + " courier=" + DescribeMobileParty(courier));
			return false;
		}
	}

	private static bool IsCourierSafeForTemporaryShipOwner(MobileParty courier, out string reason)
	{
		reason = "";
		try
		{
			if (courier == null)
			{
				reason = "courier_null";
				return false;
			}
			if (!courier.IsActive)
			{
				reason = "courier_inactive";
				return false;
			}
			if (courier.Party == null)
			{
				reason = "partybase_null";
				return false;
			}
			if (courier.Party.MobileParty != courier)
			{
				reason = "partybase_mobile_party_mismatch";
				return false;
			}
			if (courier.MemberRoster == null)
			{
				reason = "member_roster_null";
				return false;
			}
			if (courier.PrisonRoster == null)
			{
				reason = "prison_roster_null";
				return false;
			}
			if (courier.ItemRoster == null)
			{
				reason = "item_roster_null";
				return false;
			}
			if (courier.Party.Owner == null && courier.LeaderHero == null && courier.ActualClan?.Leader == null)
			{
				reason = "owner_leader_null";
				return false;
			}
			if (courier.MapFaction == null)
			{
				reason = "map_faction_null";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static void MarkExistingCourierTemporaryShips(CourierSession session, MobileParty courier)
	{
		if (session == null || courier?.Ships == null || !session.TemporaryShipCreated)
		{
			return;
		}
		foreach (Ship ship in courier.Ships)
		{
			if (IsCourierTemporaryShip(session, ship))
			{
				ship.IsUsedByQuest = true;
				ship.IsInvulnerable = true;
				ship.SetName(new TextObject(TemporaryCourierShipName));
			}
		}
	}

	private static ShipHull SelectCourierTemporaryShipHull(MobileParty courier)
	{
		try
		{
			Ship playerShip = MobileParty.MainParty?.Ships?.FirstOrDefault(x => x?.ShipHull != null);
			if (playerShip?.ShipHull != null)
			{
				return playerShip.ShipHull;
			}
		}
		catch
		{
		}
		List<ShipHull> hulls = GetLoadedShipHulls();
		if (hulls.Count == 0)
		{
			return null;
		}
		int crewCount = Math.Max(1, courier?.MemberRoster?.TotalManCount ?? 1);
		ShipHull fitting = hulls
			.Where(x => x != null && x.TotalCrewCapacity >= crewCount)
			.OrderBy(x => x.TotalCrewCapacity)
			.ThenByDescending(x => x.BaseSpeed)
			.FirstOrDefault();
		return fitting ?? hulls
			.Where(x => x != null)
			.OrderByDescending(x => x.TotalCrewCapacity)
			.ThenByDescending(x => x.BaseSpeed)
			.FirstOrDefault();
	}

	private static List<ShipHull> GetLoadedShipHulls()
	{
		Dictionary<string, ShipHull> hulls = new Dictionary<string, ShipHull>(StringComparer.OrdinalIgnoreCase);
		AddLoadedShipHulls(hulls, MBObjectManager.Instance);
		AddLoadedShipHulls(hulls, Game.Current?.ObjectManager);
		return hulls.Values.Where(x => x != null && x.TotalCrewCapacity > 0).ToList();
	}

	private static void AddLoadedShipHulls(Dictionary<string, ShipHull> hulls, MBObjectManager manager)
	{
		if (hulls == null || manager == null)
		{
			return;
		}
		try
		{
			MBReadOnlyList<ShipHull> list = manager.GetObjectTypeList<ShipHull>();
			if (list == null)
			{
				return;
			}
			foreach (ShipHull hull in list)
			{
				if (hull == null)
				{
					continue;
				}
				string key = string.IsNullOrWhiteSpace(hull.StringId) ? hull.GetHashCode().ToString() : hull.StringId;
				hulls[key] = hull;
			}
		}
		catch
		{
		}
	}

	private static bool IsNavalRuntimeAvailable()
	{
		try
		{
			if (GetLoadedShipHulls().Count == 0 || Settlement.All == null || !Settlement.All.Any(x => x != null && x.HasPort))
			{
				return false;
			}
			string modelName = Campaign.Current?.Models?.PartyNavigationModel?.GetType()?.FullName ?? "";
			if (modelName.IndexOf("Naval", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			return MobileParty.All?.Any(x => x != null && x.IsActive && x.HasNavalNavigationCapability) == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool DefaultLandPathExists(CampaignVec2 fromPoint, CampaignVec2 toPoint)
	{
		try
		{
			if (!IsValidCampaignPosition(fromPoint) || !IsValidCampaignPosition(toPoint))
			{
				return true;
			}
			return Campaign.Current?.Models?.MapDistanceModel?.PathExistBetweenPoints(fromPoint, toPoint, MobileParty.NavigationType.Default) != false;
		}
		catch
		{
			return fromPoint.IsOnLand && toPoint.IsOnLand;
		}
	}

	private static bool IsValidCampaignPosition(CampaignVec2 position)
	{
		try
		{
			return position.IsValid();
		}
		catch
		{
			return false;
		}
	}

	private static bool ShouldPreferSafeSettlementPort(MobileParty courier, string reason)
	{
		return courier?.IsCurrentlyAtSea == true || courier?.Position.IsOnLand == false || (reason ?? "").IndexOf("naval", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static float GetSafeSettlementDistanceSquared(Settlement settlement, CampaignVec2 position, bool preferPort)
	{
		if (settlement == null)
		{
			return float.MaxValue;
		}
		CampaignVec2 approach = preferPort && settlement.HasPort ? settlement.PortPosition : settlement.GatePosition;
		return approach.DistanceSquared(position);
	}

	private static bool ShouldDivertToSafeSettlementAfterNavalStuck(CourierSession session, CourierRoutePlan plan)
	{
		return session != null && plan?.RequiresNaval == true && session.NavalStuckRefreshCount >= NavalStuckSafeRouteThreshold;
	}

	private static void DestroyCourierTemporaryShips(CourierSession session, MobileParty courier, string reason)
	{
		try
		{
			if (session == null || courier?.Ships == null)
			{
				return;
			}
			List<Ship> ships = courier.Ships.Where(x => IsCourierTemporaryShip(session, x)).ToList();
			foreach (Ship ship in ships)
			{
				DestroyShipAction.Apply(ship);
			}
			if (ships.Count > 0)
			{
				Log("courier temporary ships destroyed session=" + session.Id + " count=" + ships.Count + " reason=" + (reason ?? ""));
			}
			session.TemporaryShipCreated = false;
			session.TemporaryShipHullId = "";
		}
		catch (Exception ex)
		{
			Log("courier temporary ship cleanup failed session=" + (session?.Id ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsCourierTemporaryShip(CourierSession session, Ship ship)
	{
		if (ship == null)
		{
			return false;
		}
		string shipName = ship.Name?.ToString() ?? "";
		bool nameMatches = string.Equals(shipName, TemporaryCourierShipName, StringComparison.OrdinalIgnoreCase);
		string hullId = (session?.TemporaryShipHullId ?? "").Trim();
		bool hullMatches = !string.IsNullOrWhiteSpace(hullId) && string.Equals(ship.ShipHull?.StringId ?? "", hullId, StringComparison.OrdinalIgnoreCase);
		if (session?.TemporaryShipCreated == true)
		{
			return hullMatches || nameMatches || ship.IsUsedByQuest;
		}
		return nameMatches && ship.IsUsedByQuest;
	}

	private void RouteToRecipient(CourierSession session, MobileParty courier, MobileParty targetParty, Settlement targetSettlement)
	{
		if (session == null || courier == null)
		{
			return;
		}
		if (TryRouteCourierAwayFromBanditThreat(session, courier, "recipient"))
		{
			return;
		}
		CourierRoutePlan plan = BuildCourierRoutePlan(courier, targetParty?.Position ?? targetSettlement?.GatePosition ?? courier.Position, targetSettlement, targetParty, false);
		if (!EnsureCourierNavalReadiness(session, courier, plan, "recipient"))
		{
			plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
			plan.RequiresNaval = false;
			plan.UsePort = false;
		}
		string key = BuildRecipientRouteKey(targetParty, targetSettlement, plan);
		AiBehavior expectedBehavior = targetParty != null && targetParty.IsActive ? AiBehavior.GoToPoint : AiBehavior.GoToSettlement;
		LogCourierStatusVerbose("route_eval_recipient:" + session.Id, "route_eval_recipient session=" + session.Id + " key=" + key + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount, 2.0);
		if (IsCourierRouteTargetMismatched(courier, expectedBehavior, plan, targetParty, targetSettlement, targetParty?.Position ?? CampaignVec2.Invalid))
		{
			LogCourierStatusVerbose("route_target_mismatch:" + session.Id + ":recipient", "route_target_mismatch session=" + session.Id + " route=recipient key=" + key + " expectedBehavior=" + expectedBehavior + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier), 2.0);
			session.LastRouteKey = "";
		}
		if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, expectedBehavior))
		{
			if (ShouldDivertToSafeSettlementAfterNavalStuck(session, plan))
			{
				LogCourierStatus("divert_to_safe_after_stuck session=" + session.Id + " route=recipient key=" + key + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount);
				RouteToSafeSettlement(session, courier, "naval_stuck");
				return;
			}
			if (targetParty != null && targetParty.IsActive)
			{
				courier.SetMoveGoToPoint(targetParty.Position, plan.NavigationType);
				LogCourierStatus("route_recipient_set session=" + session.Id + " command=go_to_party key=" + key + " " + DescribeRoutePlan(plan) + " targetParty=" + DescribeMobileParty(targetParty) + " courier=" + DescribeMobileParty(courier));
			}
			else if (targetSettlement != null)
			{
				courier.SetMoveGoToSettlement(targetSettlement, plan.NavigationType, plan.UsePort);
				LogCourierStatus("route_recipient_set session=" + session.Id + " command=go_to_settlement key=" + key + " " + DescribeRoutePlan(plan) + " targetSettlement=" + DescribeSettlement(targetSettlement) + " courier=" + DescribeMobileParty(courier));
			}
			ApplyCourierAiOverrides(courier, "route_recipient");
			LogVerbose("route_recipient:" + session.Id, "route recipient session=" + session.Id + " key=" + key, 5.0);
		}
	}

	private static string BuildRecipientRouteKey(MobileParty targetParty, Settlement targetSettlement, CourierRoutePlan plan)
	{
		string suffix = plan?.KeySuffix ?? "";
		if (targetParty != null && targetParty.IsActive)
		{
			CampaignVec2 position = targetParty.Position;
			int x = (int)MathF.Round(position.X * 2f);
			int y = (int)MathF.Round(position.Y * 2f);
			return "recipient_party:" + (targetParty.StringId ?? "") + ":" + x + ":" + y + ":targetSea=" + (targetParty.IsCurrentlyAtSea ? "1" : "0") + suffix;
		}
		return "recipient_settlement:" + (targetSettlement?.StringId ?? "") + suffix;
	}

	private void RouteToSender(CourierSession session, MobileParty courier)
	{
		MobileParty mainParty = MobileParty.MainParty;
		if (session == null || courier == null || mainParty == null)
		{
			return;
		}
		if (TryRouteCourierAwayFromBanditThreat(session, courier, "sender"))
		{
			return;
		}
		Settlement targetSettlement = mainParty.CurrentSettlement;
		CourierRoutePlan plan = BuildCourierRoutePlan(courier, targetSettlement?.GatePosition ?? mainParty.Position, targetSettlement, mainParty, targetSettlement != null && courier.IsCurrentlyAtSea);
		if (!EnsureCourierNavalReadiness(session, courier, plan, "sender"))
		{
			plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
			plan.RequiresNaval = false;
			plan.UsePort = false;
		}
		string key = BuildSenderRouteKey(mainParty, plan);
		AiBehavior expectedBehavior = mainParty.CurrentSettlement != null ? AiBehavior.GoToSettlement : AiBehavior.GoToPoint;
		LogCourierStatusVerbose("route_eval_sender:" + session.Id, "route_eval_sender session=" + session.Id + " key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount, 2.0);
		if (IsCourierRouteTargetMismatched(courier, expectedBehavior, plan, mainParty, mainParty.CurrentSettlement, mainParty.Position))
		{
			LogCourierStatusVerbose("route_target_mismatch:" + session.Id + ":sender", "route_target_mismatch session=" + session.Id + " route=sender key=" + key + " expectedBehavior=" + expectedBehavior + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier), 2.0);
			session.LastRouteKey = "";
		}
		if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, expectedBehavior))
		{
			if (ShouldDivertToSafeSettlementAfterNavalStuck(session, plan))
			{
				LogCourierStatus("divert_to_safe_after_stuck session=" + session.Id + " route=sender key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount);
				RouteToSafeSettlement(session, courier, "naval_stuck_return");
				return;
			}
			if (mainParty.CurrentSettlement != null)
			{
				courier.SetMoveGoToSettlement(mainParty.CurrentSettlement, plan.NavigationType, plan.UsePort);
				LogCourierStatus("route_sender_set session=" + session.Id + " command=go_to_settlement key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier));
			}
			else
			{
				courier.SetMoveGoToPoint(mainParty.Position, plan.NavigationType);
				LogCourierStatus("route_sender_set session=" + session.Id + " command=go_to_party key=" + key + " " + DescribeRoutePlan(plan) + " sender=" + DescribeMobileParty(mainParty) + " courier=" + DescribeMobileParty(courier));
			}
			ApplyCourierAiOverrides(courier, "route_sender");
			LogVerbose("route_sender:" + session.Id, "route sender session=" + session.Id + " key=" + key, 5.0);
		}
	}

	private bool TryRouteCourierAwayFromBanditThreat(CourierSession session, MobileParty courier, string routeReason)
	{
		try
		{
			if (session == null || courier == null || courier.Ai == null || Campaign.Current?.Models?.MobilePartyAIModel == null)
			{
				return false;
			}
			if (!Campaign.Current.Models.MobilePartyAIModel.ShouldPartyCheckInitiativeBehavior(courier))
			{
				return false;
			}
			Campaign.Current.Models.MobilePartyAIModel.GetBestInitiativeBehavior(courier, out AiBehavior behavior, out MobileParty threat, out float score, out Vec2 averageEnemyVec);
			if (score <= CourierThreatAvoidanceMinimumScore || !MobileParty.IsFleeBehavior(behavior) || threat == null || threat == courier || !threat.IsActive || !IsBanditOrOutlawParty(threat))
			{
				return false;
			}
			if (courier.CurrentSettlement != null && IsCourierSafeSettlementCandidate(courier.CurrentSettlement, courier))
			{
				string holdKey = "avoid_bandit_hold:" + (routeReason ?? "") + ":" + (threat.StringId ?? "");
				if (ShouldRefreshRoute(session, holdKey, courier, AiBehavior.Hold))
				{
					courier.SetMoveModeHold();
					ApplyCourierAiOverrides(courier, "avoid_bandit_hold");
					LogCourierStatus("route_avoid_bandit_hold session=" + session.Id + " reason=" + (routeReason ?? "") + " threat=" + DescribeMobileParty(threat) + " courier=" + DescribeMobileParty(courier));
				}
				return true;
			}
			courier.Ai.CalculateFleePosition(out CampaignVec2 fleePoint, threat, averageEnemyVec);
			if (!IsValidCampaignPosition(fleePoint) || fleePoint.DistanceSquared(courier.Position) <= CourierThreatAvoidanceMinimumMoveDistanceSquared)
			{
				LogCourierStatusVerbose("route_avoid_bandit_fallback:" + session.Id, "route_avoid_bandit_fallback session=" + session.Id + " reason=" + (routeReason ?? "") + " behavior=" + behavior + " score=" + score + " fleePoint=" + FormatCampaignVec2(fleePoint) + " threat=" + DescribeMobileParty(threat) + " courier=" + DescribeMobileParty(courier), 2.0);
				RouteToSafeSettlementForThreat(session, courier, "bandit_threat_" + (routeReason ?? ""));
				return true;
			}
			CourierRoutePlan plan = BuildCourierRoutePlan(courier, fleePoint, null, null, courier.IsCurrentlyAtSea || threat.IsCurrentlyAtSea);
			if (!EnsureCourierNavalReadiness(session, courier, plan, "avoid_bandit_" + (routeReason ?? "")))
			{
				if (plan.RequiresNaval)
				{
					RouteToSafeSettlementForThreat(session, courier, "bandit_threat_no_naval_" + (routeReason ?? ""));
					return true;
				}
				plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
				plan.RequiresNaval = false;
				plan.UsePort = false;
			}
			string key = BuildThreatAvoidanceRouteKey(threat, fleePoint, plan, routeReason);
			if (ShouldDivertToSafeSettlementAfterNavalStuck(session, plan))
			{
				LogCourierStatus("route_avoid_bandit_divert_safe session=" + session.Id + " reason=" + (routeReason ?? "") + " key=" + key + " threat=" + DescribeMobileParty(threat) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount);
				RouteToSafeSettlementForThreat(session, courier, "bandit_threat_naval_stuck_" + (routeReason ?? ""));
				return true;
			}
			if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, AiBehavior.GoToPoint))
			{
				courier.SetMoveGoToPoint(fleePoint, plan.NavigationType);
				ApplyCourierAiOverrides(courier, "avoid_bandit");
				LogCourierStatus("route_avoid_bandit_set session=" + session.Id + " reason=" + (routeReason ?? "") + " key=" + key + " behavior=" + behavior + " score=" + score + " fleePoint=" + FormatCampaignVec2(fleePoint) + " " + DescribeRoutePlan(plan) + " threat=" + DescribeMobileParty(threat) + " courier=" + DescribeMobileParty(courier));
			}
			return true;
		}
		catch (Exception ex)
		{
			LogVerbose("route_avoid_bandit_failed:" + (session?.Id ?? ""), "route avoid bandit failed session=" + (session?.Id ?? "") + " reason=" + (routeReason ?? "") + " error=" + ex.Message, 5.0);
			return false;
		}
	}

	private void RouteToSafeSettlementForThreat(CourierSession session, MobileParty courier, string reason)
	{
		try
		{
			bool preferPort = ShouldPreferSafeSettlementPort(courier, reason);
			Settlement settlement = ResolveSafeSettlement(session, courier, preferPort);
			if (settlement == null)
			{
				if (ShouldRefreshRoute(session, "avoid_bandit_safe_hold:" + (reason ?? ""), courier, AiBehavior.Hold))
				{
					courier.SetMoveModeHold();
					ApplyCourierAiOverrides(courier, "avoid_bandit_safe_hold");
				}
				return;
			}
			CourierRoutePlan plan = BuildCourierRoutePlan(courier, settlement.GatePosition, settlement, null, preferPort);
			if (!EnsureCourierNavalReadiness(session, courier, plan, "avoid_bandit_safe_" + (reason ?? "")))
			{
				plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
				plan.RequiresNaval = false;
				plan.UsePort = false;
			}
			string key = "avoid_bandit_safe:" + (reason ?? "") + ":" + (settlement.StringId ?? "") + (plan?.KeySuffix ?? "");
			if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, AiBehavior.GoToSettlement))
			{
				courier.SetMoveGoToSettlement(settlement, plan.NavigationType, plan.UsePort);
				ApplyCourierAiOverrides(courier, "avoid_bandit_safe");
				LogCourierStatus("route_avoid_bandit_safe_set session=" + session.Id + " reason=" + (reason ?? "") + " key=" + key + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier));
			}
		}
		catch (Exception ex)
		{
			LogVerbose("route_avoid_bandit_safe_failed:" + (session?.Id ?? ""), "route avoid bandit safe failed session=" + (session?.Id ?? "") + " reason=" + (reason ?? "") + " error=" + ex.Message, 5.0);
		}
	}

	private static string BuildThreatAvoidanceRouteKey(MobileParty threat, CampaignVec2 fleePoint, CourierRoutePlan plan, string routeReason)
	{
		int x = (int)MathF.Round(fleePoint.X * 2f);
		int y = (int)MathF.Round(fleePoint.Y * 2f);
		return "avoid_bandit:" + (routeReason ?? "") + ":" + (threat?.StringId ?? "") + ":" + x + ":" + y + (plan?.KeySuffix ?? "");
	}

	private static string BuildSenderRouteKey(MobileParty mainParty, CourierRoutePlan plan)
	{
		string suffix = plan?.KeySuffix ?? "";
		if (mainParty == null)
		{
			return "sender_point:null" + suffix;
		}
		if (mainParty.CurrentSettlement != null)
		{
			return "sender_settlement:" + mainParty.CurrentSettlement.StringId + suffix;
		}
		CampaignVec2 position = mainParty.Position;
		int x = (int)MathF.Round(position.X * 2f);
		int y = (int)MathF.Round(position.Y * 2f);
		return "sender_point:" + x + ":" + y + ":targetSea=" + (mainParty.IsCurrentlyAtSea ? "1" : "0") + suffix;
	}

	private void RouteToSafeSettlement(CourierSession session, MobileParty courier, string reason)
	{
		if (session == null || courier == null)
		{
			return;
		}
		bool preferPort = ShouldPreferSafeSettlementPort(courier, reason);
		Settlement settlement = ResolveSafeSettlement(session, courier, preferPort);
		if (settlement == null)
		{
			courier.SetMoveModeHold();
			ApplyCourierAiOverrides(courier, "route_safe_hold");
			LogCourierStatusVerbose("route_safe_hold:" + session.Id + ":" + (reason ?? ""), "route_safe_hold session=" + session.Id + " reason=" + (reason ?? "") + " preferPort=" + preferPort + " courier=" + DescribeMobileParty(courier), 5.0);
			return;
		}
		CourierRoutePlan plan = BuildCourierRoutePlan(courier, settlement.GatePosition, settlement, null, preferPort);
		if (!EnsureCourierNavalReadiness(session, courier, plan, "safe_" + (reason ?? "")))
		{
			plan.NavigationType = GetEffectiveCourierNavigationType(courier, false);
			plan.RequiresNaval = false;
			plan.UsePort = false;
		}
		string key = "safe:" + settlement.StringId + ":" + reason + plan.KeySuffix;
		LogCourierStatusVerbose("route_eval_safe:" + session.Id + ":" + (reason ?? ""), "route_eval_safe session=" + session.Id + " reason=" + (reason ?? "") + " key=" + key + " preferPort=" + preferPort + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier) + " stuckCount=" + session.NavalStuckRefreshCount, 2.0);
		if (IsCourierRouteTargetMismatched(courier, AiBehavior.GoToSettlement, plan, null, settlement, CampaignVec2.Invalid))
		{
			LogCourierStatusVerbose("route_target_mismatch:" + session.Id + ":safe", "route_target_mismatch session=" + session.Id + " route=safe reason=" + (reason ?? "") + " key=" + key + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier), 2.0);
			session.LastRouteKey = "";
		}
		if (ShouldRefreshRouteWithProgress(session, key, courier, plan.RequiresNaval, AiBehavior.GoToSettlement))
		{
			courier.SetMoveGoToSettlement(settlement, plan.NavigationType, plan.UsePort);
			ApplyCourierAiOverrides(courier, "route_safe");
			LogCourierStatus("route_safe_set session=" + session.Id + " reason=" + (reason ?? "") + " key=" + key + " " + DescribeRoutePlan(plan) + " safeSettlement=" + DescribeSettlement(settlement) + " courier=" + DescribeMobileParty(courier));
			LogVerbose("route_safe:" + session.Id, "route safe session=" + session.Id + " settlement=" + settlement.StringId + " reason=" + reason, 5.0);
		}
	}

	private Settlement ResolveSafeSettlement(CourierSession session, MobileParty courier, bool preferPort)
	{
		try
		{
			CampaignVec2 position = courier?.Position ?? MobileParty.MainParty?.Position ?? CampaignVec2.Invalid;
			List<Settlement> candidates = Settlement.All?
				.Where(x => IsCourierSafeSettlementCandidate(x, courier))
				.ToList() ?? new List<Settlement>();
			if (preferPort && candidates.Any(x => x.HasPort))
			{
				candidates = candidates.Where(x => x.HasPort).ToList();
			}
			List<Settlement> friendlyCandidates = candidates
				.Where(x => IsCourierFriendlySafeSettlementCandidate(x, courier))
				.ToList();
			if (!string.IsNullOrWhiteSpace(session.SafeSettlementId))
			{
				Settlement existing = Settlement.Find(session.SafeSettlementId);
				if (existing != null && candidates.Contains(existing) && (friendlyCandidates.Count == 0 || IsCourierFriendlySafeSettlementCandidate(existing, courier)))
				{
					return existing;
				}
				session.SafeSettlementId = "";
			}
			IEnumerable<Settlement> primaryCandidates = friendlyCandidates.Count > 0 ? friendlyCandidates : candidates;
			Settlement settlement = primaryCandidates
				.OrderBy(x => GetSafeSettlementDistanceSquared(x, position, preferPort))
				.FirstOrDefault();
			session.SafeSettlementId = settlement?.StringId ?? "";
			return settlement;
		}
		catch
		{
			return null;
		}
	}

	private static bool IsCourierFriendlySafeSettlementCandidate(Settlement settlement, MobileParty courier)
	{
		try
		{
			if (!IsCourierSafeSettlementCandidate(settlement, courier))
			{
				return false;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			Clan courierClan = courier?.ActualClan ?? playerClan;
			IFaction courierFaction = courier?.MapFaction ?? courierClan ?? playerClan ?? Hero.MainHero?.MapFaction;
			if (playerClan != null && settlement.OwnerClan == playerClan)
			{
				return true;
			}
			if (courierClan != null && settlement.OwnerClan == courierClan)
			{
				return true;
			}
			if (courierFaction != null && settlement.MapFaction == courierFaction)
			{
				return true;
			}
			Kingdom courierKingdom = courierFaction as Kingdom ?? courierClan?.Kingdom ?? playerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
			return courierKingdom != null && (settlement.MapFaction == courierKingdom || settlement.OwnerClan?.Kingdom == courierKingdom);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCourierSafeSettlementCandidate(Settlement settlement, MobileParty courier)
	{
		try
		{
			if (settlement == null || !settlement.IsFortification || settlement.IsHideout || settlement.IsUnderSiege)
			{
				return false;
			}
			IFaction courierFaction = courier?.MapFaction ?? courier?.ActualClan ?? Clan.PlayerClan ?? Hero.MainHero?.MapFaction;
			IFaction settlementFaction = settlement.MapFaction ?? settlement.OwnerClan;
			if (courierFaction == null || settlementFaction == null || courierFaction == settlementFaction)
			{
				return true;
			}
			return !FactionManager.IsAtWarAgainstFaction(courierFaction, settlementFaction);
		}
		catch
		{
			return false;
		}
	}

	private bool HandleRecipientUnavailableStatus(CourierSession session, MobileParty courier, Hero recipient)
	{
		if (session == null || courier == null || recipient == null)
		{
			return false;
		}
		string reason = GetRecipientUnavailableReason(recipient);
		if (!string.IsNullOrWhiteSpace(reason))
		{
			LogCourierStatusVerbose("recipient_unavailable:" + session.Id, "recipient_unavailable session=" + session.Id + " reason=" + reason + " " + DescribeHero(recipient) + " courier=" + DescribeMobileParty(courier), 5.0);
			SetRecipientWaitReason(session, reason, "target_" + reason);
			session.Stage = CourierStage.WaitingRecipient.ToString();
			RouteToSafeSettlement(session, courier, "recipient_" + reason + "_wait");
			return true;
		}
		return false;
	}

	private static string GetRecipientUnavailableReason(Hero recipient)
	{
		if (recipient == null || recipient.IsDead)
		{
			return "";
		}
		try
		{
			if (recipient.IsFugitive)
			{
				return "fugitive";
			}
			if (recipient.IsPrisoner || recipient.PartyBelongedToAsPrisoner != null)
			{
				return "prisoner";
			}
		}
		catch
		{
		}
		return "";
	}

	private void SetRecipientWaitReason(CourierSession session, string reason, string source)
	{
		if (session == null)
		{
			return;
		}
		string normalized = (reason ?? "").Trim();
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return;
		}
		if (string.Equals(session.RecipientWaitReason ?? "", normalized, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		session.RecipientWaitReason = normalized;
		Log("recipient unavailable session=" + session.Id + " recipient=" + session.RecipientHeroId + " status=" + normalized + " source=" + (source ?? "") + " action=safe_wait");
		LogCourierStatus("recipient_wait_set session=" + session.Id + " recipient=" + session.RecipientHeroId + " status=" + normalized + " source=" + (source ?? "") + " action=safe_wait safeSettlement=" + (session.SafeSettlementId ?? ""));
		if (string.Equals(normalized, "fugitive", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("信使目标正在逃亡，信使正前往最近定居点等待。", Colors.Yellow));
		}
		else if (string.Equals(normalized, "prisoner", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage("信使目标处于俘虏状态，信使正前往最近定居点等待。", Colors.Yellow));
		}
	}

	private void ClearRecipientWaitReasonIfNeeded(CourierSession session, string source)
	{
		if (session == null || string.IsNullOrWhiteSpace(session.RecipientWaitReason))
		{
			return;
		}
		string previous = session.RecipientWaitReason;
		session.RecipientWaitReason = "";
		session.SafeSettlementId = "";
		session.LastRouteKey = "";
		if (IsBlockingRecipientWaitReason(previous))
		{
			LogCourierStatus("recipient_wait_cleared session=" + session.Id + " recipient=" + session.RecipientHeroId + " previousStatus=" + previous + " source=" + (source ?? "") + " status=resume_route");
			Log("recipient reappeared session=" + session.Id + " recipient=" + session.RecipientHeroId + " previousStatus=" + previous + " source=" + (source ?? "") + " status=resume_route");
			InformationManager.DisplayMessage(new InformationMessage("信使目标重新出现，信使正在路上。", Colors.Green));
			return;
		}
		Log("recipient wait cleared session=" + session.Id + " previousStatus=" + previous + " source=" + (source ?? ""));
		LogCourierStatus("recipient_wait_cleared session=" + session.Id + " recipient=" + session.RecipientHeroId + " previousStatus=" + previous + " source=" + (source ?? ""));
	}

	private static bool IsBlockingRecipientWaitReason(string reason)
	{
		string value = (reason ?? "").Trim();
		return string.Equals(value, "fugitive", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "prisoner", StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryGetRecipientTarget(Hero recipient, out MobileParty party, out Settlement settlement)
	{
		party = null;
		settlement = null;
		if (recipient == null || recipient.IsDead)
		{
			return false;
		}
		party = recipient.PartyBelongedTo;
		if (party != null && party.IsActive)
		{
			settlement = party.CurrentSettlement;
			return true;
		}
		try
		{
			if (recipient.PartyBelongedToAsPrisoner != null)
			{
				PartyBase prisonerParty = recipient.PartyBelongedToAsPrisoner;
				party = prisonerParty.MobileParty;
				settlement = prisonerParty.Settlement;
				if ((party != null && party.IsActive) || settlement != null)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		settlement = recipient.CurrentSettlement ?? recipient.StayingInSettlement;
		return settlement != null;
	}

	private static bool IsAtRecipient(MobileParty courier, MobileParty targetParty, Settlement settlement)
	{
		if (courier == null)
		{
			return false;
		}
		if (targetParty != null && targetParty.IsActive)
		{
			if (courier.CurrentSettlement != null && courier.CurrentSettlement == targetParty.CurrentSettlement)
			{
				return true;
			}
			float arrivalDistance = GetMobilePartyArrivalDistance();
			try
			{
				MobileParty.NavigationType navigationType = targetParty.IsCurrentlyAtSea || courier.IsCurrentlyAtSea ? MobileParty.NavigationType.All : courier.NavigationCapability;
				float distance = DistanceHelper.FindClosestDistanceFromMobilePartyToMobileParty(courier, targetParty, navigationType);
				if (distance <= arrivalDistance)
				{
					return true;
				}
			}
			catch
			{
			}
			return courier.Position.DistanceSquared(targetParty.Position) <= arrivalDistance * arrivalDistance;
		}
		if (settlement != null)
		{
			return IsAtSettlementApproach(courier, settlement) || courier.CurrentSettlement == settlement;
		}
		return false;
	}

	private static bool IsAtSettlementApproach(MobileParty courier, Settlement settlement)
	{
		if (courier == null || settlement == null)
		{
			return false;
		}
		if (courier.Position.DistanceSquared(settlement.GatePosition) <= SettlementArrivalDistanceSquared)
		{
			return true;
		}
		return settlement.HasPort && courier.Position.DistanceSquared(settlement.PortPosition) <= SettlementArrivalDistanceSquared;
	}

	private static float GetMobilePartyArrivalDistance()
	{
		try
		{
			float encounterRadius = Campaign.Current?.Models?.EncounterModel?.GetEncounterJoiningRadius ?? 0f;
			if (encounterRadius > 0f)
			{
				return MathF.Max(MobilePartyArrivalDistance, encounterRadius * 2.5f);
			}
		}
		catch
		{
		}
		return MobilePartyArrivalDistance;
	}

	private static bool IsAtSender(MobileParty courier, MobileParty sender)
	{
		if (courier == null || sender == null)
		{
			return false;
		}
		if (sender.CurrentSettlement != null)
		{
			return IsAtSettlementApproach(courier, sender.CurrentSettlement) || courier.CurrentSettlement == sender.CurrentSettlement;
		}
		return courier.Position.DistanceSquared(sender.Position) <= SenderArrivalDistanceSquared;
	}

	private static bool ShouldRefreshRoute(CourierSession session, string routeKey, MobileParty courier = null, params AiBehavior[] expectedDefaultBehaviors)
	{
		return ShouldRefreshRouteCore(session, routeKey, courier, false, expectedDefaultBehaviors);
	}

	private static bool ShouldRefreshRouteWithProgress(CourierSession session, string routeKey, MobileParty courier, bool monitorProgress, params AiBehavior[] expectedDefaultBehaviors)
	{
		return ShouldRefreshRouteCore(session, routeKey, courier, monitorProgress, expectedDefaultBehaviors);
	}

	private static bool IsCourierRouteTargetMismatched(MobileParty courier, AiBehavior expectedBehavior, CourierRoutePlan plan, MobileParty expectedParty, Settlement expectedSettlement, CampaignVec2 expectedPoint)
	{
		if (courier == null || plan == null)
		{
			return false;
		}
		try
		{
			if (courier.DefaultBehavior != expectedBehavior)
			{
				return true;
			}
			if (courier.DesiredAiNavigationType != plan.NavigationType)
			{
				return true;
			}
			if (expectedBehavior == AiBehavior.GoToSettlement)
			{
				return courier.TargetSettlement != expectedSettlement || courier.IsTargetingPort != plan.UsePort;
			}
			if (expectedBehavior == AiBehavior.GoToPoint)
			{
				if (courier.TargetSettlement != null || courier.TargetParty != null)
				{
					return true;
				}
				if (expectedParty != null && expectedParty.IsActive)
				{
					expectedPoint = expectedParty.Position;
				}
				if (IsValidCampaignPosition(expectedPoint) && IsValidCampaignPosition(courier.TargetPosition) && courier.TargetPosition.DistanceSquared(expectedPoint) > 1f)
				{
					return true;
				}
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	private static bool ShouldRefreshRouteCore(CourierSession session, string routeKey, MobileParty courier, bool monitorProgress, params AiBehavior[] expectedDefaultBehaviors)
	{
		if (session == null)
		{
			return false;
		}
		string key = routeKey ?? "";
		if (!string.Equals(session.LastRouteKey ?? "", key, StringComparison.OrdinalIgnoreCase))
		{
			session.LastRouteKey = key;
			ResetCourierRouteProgress(session, key, courier);
			return true;
		}
		if (monitorProgress && IsCourierRouteStuck(session, key, courier))
		{
			return true;
		}
		if (courier != null && expectedDefaultBehaviors != null && expectedDefaultBehaviors.Length > 0 && !expectedDefaultBehaviors.Contains(courier.DefaultBehavior))
		{
			LogVerbose("route_refresh_forced:" + session.Id, "route refresh forced session=" + session.Id + " key=" + key + " defaultBehavior=" + courier.DefaultBehavior + " shortTerm=" + courier.ShortTermBehavior, 5.0);
			LogCourierStatusVerbose("route_refresh_forced:" + session.Id, "route_refresh_forced session=" + session.Id + " key=" + key + " expected=" + string.Join(",", expectedDefaultBehaviors.Select(x => x.ToString())) + " courier=" + DescribeMobileParty(courier), 5.0);
			return true;
		}
		return false;
	}

	private static void ResetCourierRouteProgress(CourierSession session, string routeKey, MobileParty courier)
	{
		if (session == null)
		{
			return;
		}
		session.LastProgressRouteKey = routeKey ?? "";
		if (courier != null)
		{
			session.LastProgressX = courier.Position.X;
			session.LastProgressY = courier.Position.Y;
		}
		session.LastProgressCampaignHours = GetCampaignHours();
		session.NavalStuckRefreshCount = 0;
	}

	private static bool IsCourierRouteStuck(CourierSession session, string routeKey, MobileParty courier)
	{
		if (session == null || courier == null)
		{
			return false;
		}
		double nowHours = GetCampaignHours();
		if (nowHours <= 0)
		{
			return false;
		}
		if (!string.Equals(session.LastProgressRouteKey ?? "", routeKey ?? "", StringComparison.OrdinalIgnoreCase) || session.LastProgressCampaignHours <= 0)
		{
			ResetCourierRouteProgress(session, routeKey, courier);
			return false;
		}
		float dx = courier.Position.X - session.LastProgressX;
		float dy = courier.Position.Y - session.LastProgressY;
		float distanceSquared = dx * dx + dy * dy;
		if (distanceSquared > NavalStuckDistanceSquared)
		{
			ResetCourierRouteProgress(session, routeKey, courier);
			return false;
		}
		double elapsedHours = nowHours - session.LastProgressCampaignHours;
		if (elapsedHours < NavalStuckRefreshHours)
		{
			return false;
		}
		session.LastProgressCampaignHours = nowHours;
		session.NavalStuckRefreshCount++;
		session.LastRouteKey = "";
		LogVerbose("naval_route_stuck:" + session.Id, "naval route stuck session=" + session.Id + " key=" + (routeKey ?? "") + " count=" + session.NavalStuckRefreshCount + " distanceSquared=" + distanceSquared, 1.0);
		LogCourierStatus("naval_route_stuck session=" + session.Id + " key=" + (routeKey ?? "") + " count=" + session.NavalStuckRefreshCount + " distanceSquared=" + distanceSquared + " elapsedHours=" + elapsedHours + " courier=" + DescribeMobileParty(courier));
		return true;
	}

	private static double GetCampaignHours()
	{
		try
		{
			return CampaignTime.Now.ToHours;
		}
		catch
		{
			return 0;
		}
	}
}
