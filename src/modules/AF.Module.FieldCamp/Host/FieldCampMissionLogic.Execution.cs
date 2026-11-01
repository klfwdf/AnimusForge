using System;
using System.Collections.Generic;
using RichExecutions.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Camp execution ceremony (field camp v3). The camp menu opens the camp on a provisional
/// Vengeance custom-site session; the player picks the place, prisoner and punishment in the
/// deployment studio (B) and the full public-execution ceremony runs here. Vengeance applies the
/// camp consequences (no influence, town order or local relations).
/// While the ceremony runs, the nearest idle soldiers stand in a crowd in front of the site,
/// everyone else inside the site steps aside, and camp life pauses; afterwards they walk home.
/// Frequency: staging and release run once per ceremony; the walk home is re-checked in the
/// 0.5 s upkeep over the moved soldiers only, and stops once they are all sent back.
/// </summary>
public sealed partial class FieldCampMissionLogic
{
	private const int MaxCeremonyWatchers = 16;
	private const int WatchersPerRow = 8;
	private const float CeremonyClearRadius = 9f;
	private const float WatcherFrontDistance = 11f;
	private const float WatcherRowSpacing = 1.4f;
	private const float WatcherFileSpacing = 1.3f;
	private const float CeremonyReturnGiveUpSeconds = 60f;

	private readonly List<CampAgent> _ceremonyMoved = new List<CampAgent>();
	private bool _ceremonyActive;
	private float _ceremonyReturnDeadline = -1f;

	internal static bool IsExecutionCeremonyAvailable =>
		VengeanceIntegration.IsEnabled && VengeanceIntegration.IsEmbeddedHostActive && RichExecutionApi.IsInitialized;

	/// <summary>Set once when the embedded Vengeance host starts; cleared when it stops.</summary>
	internal static void RegisterVengeanceHooks()
	{
		VengeanceIntegration.FieldCampMissionProbe = IsCampMission;
		VengeanceIntegration.FieldCampCeremonyAdmissionProbe = CanStartCeremony;
		VengeanceIntegration.FieldCampCeremonyStaged = OnCeremonyStagedHook;
		VengeanceIntegration.FieldCampCeremonyReleased = OnCeremonyReleasedHook;
	}

	internal static void UnregisterVengeanceHooks()
	{
		VengeanceIntegration.FieldCampMissionProbe = null;
		VengeanceIntegration.FieldCampCeremonyAdmissionProbe = null;
		VengeanceIntegration.FieldCampCeremonyStaged = null;
		VengeanceIntegration.FieldCampCeremonyReleased = null;
	}

	private static bool IsCampMission(Mission mission) =>
		mission != null && mission.GetMissionBehavior<FieldCampMissionLogic>() != null;

	private static bool CanStartCeremony(Mission mission)
	{
		var camp = mission?.GetMissionBehavior<FieldCampMissionLogic>();
		return camp != null && camp._spawned && !mission.IsMissionEnding &&
			!camp._ceremonyActive && !camp.SlaughterBusy && camp._duelists.Count == 0;
	}

	private static void OnCeremonyStagedHook(Hero victim, Vec3 origin, Vec2 forward) => Current?.OnCeremonyStaged(victim, origin, forward);

	private static void OnCeremonyReleasedHook() => Current?.OnCeremonyReleased();

	private void OnCeremonyStaged(Hero victim, Vec3 origin, Vec2 forward)
	{
		if (_ceremonyActive || !_spawned)
		{
			return;
		}
		_ceremonyActive = true;
		_ceremonyReturnDeadline = -1f;
		_ceremonyMoved.Clear();
		if (_envoy != null)
		{
			// The ceremony moves the crowd itself; the matter is raised again afterwards.
			Requeue(_envoyIssue);
			DropEnvoy(sendHome: false);
		}
		try
		{
			HideCampCopyOfCondemned(victim);
			Vec2 f = forward.IsNonZero() ? forward.Normalized() : new Vec2(0f, 1f);
			Vec2 right = new Vec2(f.y, -f.x);
			Vec2 site = origin.AsVec2;

			// Watchers: the nearest soldiers off duty (no sentries, wounded, companions or lords).
			var candidates = new List<CampAgent>();
			for (int i = 0; i < _agents.Count; i++)
			{
				CampAgent entry = _agents[i];
				if (IsMovableForCeremony(entry) && entry.Activity != Activity.Sentry && entry.Activity != Activity.Wounded && entry.Activity != Activity.Post)
				{
					candidates.Add(entry);
				}
			}
			candidates.Sort((a, b) => a.Agent.Position.AsVec2.DistanceSquared(site).CompareTo(b.Agent.Position.AsVec2.DistanceSquared(site)));
			int watchers = Math.Min(MaxCeremonyWatchers, candidates.Count);
			for (int i = 0; i < watchers; i++)
			{
				int row = i / WatchersPerRow;
				int file = i % WatchersPerRow;
				float lateral = (file - (WatchersPerRow - 1) * 0.5f) * WatcherFileSpacing + (row % 2) * 0.6f;
				StandAside(candidates[i], site + f * (WatcherFrontDistance + row * WatcherRowSpacing) + right * lateral, origin);
			}

			// Anyone else still on the site steps out of the way at once: the ceremony checks
			// its marks for live agents right after this call.
			float clearSquared = CeremonyClearRadius * CeremonyClearRadius;
			int cleared = 0;
			for (int i = 0; i < _agents.Count; i++)
			{
				CampAgent entry = _agents[i];
				if (!IsMovableForCeremony(entry) || _ceremonyMoved.Contains(entry) || entry.Agent.Position.AsVec2.DistanceSquared(site) >= clearSquared)
				{
					continue;
				}
				StandAside(entry, site + AwayFrom(site, entry.Agent.Position.AsVec2, f) * (CeremonyClearRadius + 1.5f), origin);
				cleared++;
			}
			for (int i = 0; i < _prisoners.Count; i++)
			{
				Agent agent = _prisoners[i];
				if (agent != null && agent.IsActive() && agent.Position.AsVec2.DistanceSquared(site) < clearSquared)
				{
					TeleportFlat(agent, site + AwayFrom(site, agent.Position.AsVec2, f) * (CeremonyClearRadius + 1.5f), origin);
					cleared++;
				}
			}
			Logger.Log(LogSource, "execution ceremony staged watchers=" + watchers + " cleared=" + cleared);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[WARN] execution ceremony staging failed: " + ex.Message);
		}
	}

	private bool IsMovableForCeremony(CampAgent entry) =>
		entry?.Agent != null && entry.Agent.IsActive() && entry.Agent != Agent.Main && !IsDuelist(entry.Agent);

	private static Vec2 AwayFrom(Vec2 site, Vec2 position, Vec2 fallback)
	{
		Vec2 away = position - site;
		return away.IsNonZero() ? away.Normalized() : -fallback;
	}

	/// <summary>The camp's own pen copy of the condemned hero leaves the scene; the ceremony spawns its own.</summary>
	private void HideCampCopyOfCondemned(Hero victim)
	{
		CharacterObject character = victim?.CharacterObject;
		if (character == null)
		{
			return;
		}
		for (int i = _prisoners.Count - 1; i >= 0; i--)
		{
			Agent agent = _prisoners[i];
			if (agent == null || agent.Character != character)
			{
				continue;
			}
			_prisoners.RemoveAt(i);
			try
			{
				if (agent.IsActive())
				{
					agent.FadeOut(true, false);
				}
			}
			catch (Exception ex)
			{
				Logger.Log(LogSource, "[WARN] could not hide the pen copy of the condemned: " + ex.Message);
			}
		}
	}

	private void StandAside(CampAgent entry, Vec2 flat, Vec3 lookAt)
	{
		ReleasePose(entry);
		entry.Seated = true;
		entry.Returning = false;
		TeleportFlat(entry.Agent, flat, lookAt);
		_ceremonyMoved.Add(entry);
	}

	private void TeleportFlat(Agent agent, Vec2 flat, Vec3 lookAt)
	{
		try
		{
			agent.DisableScriptedMovement();
			Vec3 pos = new Vec3(flat.x, flat.y, lookAt.z);
			pos.z = GroundHeight(Mission.Scene, pos);
			agent.TeleportToPosition(pos);
			Vec2 look = lookAt.AsVec2 - flat;
			if (look.IsNonZero())
			{
				look = look.Normalized();
				agent.LookDirection = new Vec3(look.x, look.y, 0f);
			}
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "[WARN] ceremony stand-aside failed agent=" + agent.Index + " error=" + ex.Message);
		}
	}

	/// <summary>
	/// The ceremony hands the camp back: the sides calm down (so the ceremony restores its paused
	/// soldiers) and the moved soldiers are sent home from the next upkeeps.
	/// </summary>
	private void OnCeremonyReleased()
	{
		if (!_ceremonyActive)
		{
			return;
		}
		_ceremonyActive = false;
		if (_duelists.Count == 0)
		{
			CalmTeams();
		}
		_ceremonyReturnDeadline = _ceremonyMoved.Count > 0 ? _elapsed + CeremonyReturnGiveUpSeconds : -1f;
		Logger.Log(LogSource, "execution ceremony released moved=" + _ceremonyMoved.Count);
	}

	/// <summary>In the 0.5 s upkeep: each moved soldier walks home once the ceremony has given him back to the AI.</summary>
	private void TickCeremonyReturn()
	{
		if (_ceremonyReturnDeadline < 0f)
		{
			return;
		}
		bool giveUp = _elapsed >= _ceremonyReturnDeadline;
		for (int i = _ceremonyMoved.Count - 1; i >= 0; i--)
		{
			CampAgent entry = _ceremonyMoved[i];
			Agent agent = entry.Agent;
			if (agent == null || !agent.IsActive())
			{
				_ceremonyMoved.RemoveAt(i);
				continue;
			}
			if (agent.Controller != AgentControllerType.AI && !giveUp)
			{
				continue;
			}
			entry.Seated = false;
			entry.Returning = false;
			entry.Target = entry.Home;
			entry.TargetFacing = entry.HomeFacing;
			MoveTo(entry, entry.Home, entry.HomeFacing, run: false);
			_ceremonyMoved.RemoveAt(i);
		}
		if (_ceremonyMoved.Count == 0)
		{
			_ceremonyReturnDeadline = -1f;
		}
	}
}
