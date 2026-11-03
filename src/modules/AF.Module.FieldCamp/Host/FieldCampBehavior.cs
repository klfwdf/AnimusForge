using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Player-only field camp (docs/designs/field-camp-v1.md, step 1).
/// Frequency: rest effects run on HourlyTick only while camped. Campaign TickEvent
/// (time running) and the engine tick (hotkey + map visuals) do O(1) checks per frame
/// and return immediately when no camp is active.
/// </summary>
public sealed partial class FieldCampBehavior : CampaignBehaviorBase
{
	private const string LogSource = "FieldCamp";
	private const float MoveBlockedHintIntervalSeconds = 5f;

	internal enum CampState
	{
		None = 0,
		Pitching = 1,
		Camped = 2,
		Striking = 3
	}

	// Save fields (keys listed in SyncData; approved 2026-10-30).
	private int _state;
	private double _phaseEndHours;
	private double _startHours;
	private float _anchorX;
	private float _anchorY;
	private List<string> _partyIds = new List<string>();
	private double _lastRestHours = -1;

	// Runtime only.
	private readonly Dictionary<PartyBase, Vec2> _healCarry = new Dictionary<PartyBase, Vec2>();
	private readonly List<MobileParty> _parties = new List<MobileParty>();
	private bool _partiesResolved = true;
	private float _nextMoveBlockedHintTime;
	private bool _patrolPending;
	private bool _patrolMissionSeen;
	private bool _patrolPlayerReady;
	private float _patrolPendingSince;
	private double _patrolSettleUntilHours = -1;
	private readonly FieldCampMapVisuals _visuals = new FieldCampMapVisuals();

	internal static FieldCampBehavior Instance { get; private set; }

	internal CampState State => (CampState)_state;

	internal bool IsActive => _state != (int)CampState.None;

	/// <summary>Campaign hour of the last settlement visit or camp end; -1 when unknown.</summary>
	internal double LastRestHours => _lastRestHours;

	internal Vec2 Anchor => new Vec2(_anchorX, _anchorY);

	private static double NowHours => CampaignTime.Now.ToHours;

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.PartyRemovedFromArmyEvent.AddNonSerializedListener(this, OnPartyRemovedFromArmy);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => ResetAll());
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEndedForCamp);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_afFieldCamp_state_v1", ref _state);
		dataStore.SyncData("_afFieldCamp_phaseEndHours_v1", ref _phaseEndHours);
		dataStore.SyncData("_afFieldCamp_startHours_v1", ref _startHours);
		dataStore.SyncData("_afFieldCamp_anchorX_v1", ref _anchorX);
		dataStore.SyncData("_afFieldCamp_anchorY_v1", ref _anchorY);
		dataStore.SyncData("_afFieldCamp_partyIds_v1", ref _partyIds);
		dataStore.SyncData("_afFieldCamp_lastRestHours_v1", ref _lastRestHours);
		SyncIssueData(dataStore);
		if (dataStore.IsLoading)
		{
			_partyIds = _partyIds ?? new List<string>();
			if (_state < (int)CampState.None || _state > (int)CampState.Striking)
			{
				_state = (int)CampState.None;
			}
			_parties.Clear();
			_partiesResolved = _state == (int)CampState.None;
			_healCarry.Clear();
			_visuals.Forget();
			// While camped, _phaseEndHours holds an unspent patrol charge (else -1),
			// so a save made during the charge resumes it in the wait menu.
			_patrolSettleUntilHours = _state == (int)CampState.Camped && _phaseEndHours >= 0 ? _phaseEndHours : -1;
			_resting = _patrolSettleUntilHours >= 0;
		}
	}

	// ---------- entry points ----------

	public static void OpenFromTerminal()
	{
		Instance?.RequestCamp("terminal");
	}

	/// <summary>Engine tick (also runs while the map is paused). Hotkey poll + map visuals.</summary>
	internal void OnEngineTick()
	{
		// MainParty is null while a save is still loading; never resolve or end a camp then.
		if (Campaign.Current == null || MobileParty.MainParty == null)
		{
			return;
		}
		if (Mission.Current != null)
		{
			_patrolMissionSeen |= _patrolPending;
			return;
		}
		if (_patrolPending)
		{
			if (_patrolMissionSeen)
			{
				ConsumePatrolReturn();
			}
			else if (TaleWorlds.Engine.Time.ApplicationTime - _patrolPendingSince > 15f)
			{
				// The camp mission never started; do not charge time for it.
				_patrolPending = false;
				Logger.Log(LogSource, "camp mission did not start; patrol cancelled");
			}
		}
		if (FieldCampHotkey.ConsumePress())
		{
			OnHotkeyPressed();
		}
		if (!IsActive)
		{
			return;
		}
		if (!FieldCampSettings.Enabled)
		{
			FinishCamp("disabled");
			LeaveCampMenusIfOpen();
			return;
		}
		EnsureParties();
		if (IsActive && FieldCampSettings.MapVisualsEnabled && _state != (int)CampState.Pitching)
		{
			_visuals.Ensure(_parties);
		}
		else
		{
			_visuals.Clear();
		}
	}

	private void OnHotkeyPressed()
	{
		if (Campaign.Current.CurrentMenuContext != null)
		{
			return;
		}
		RequestCamp("hotkey");
	}

	private void RequestCamp(string source)
	{
		if (!FieldCampSettings.Enabled)
		{
			AnimusForgeQuickInfo.Show(FieldCampText.Disabled());
			return;
		}
		if (IsActive)
		{
			OpenCampMenu();
			return;
		}
		if (!FieldCampRules.CanPitch(out var reason))
		{
			AnimusForgeQuickInfo.Show(reason);
			Logger.Log(LogSource, "pitch blocked source=" + source + " reason=" + reason);
			return;
		}
		BeginPitch();
		Logger.Log(LogSource, "pitch start source=" + source + " parties=" + _partyIds.Count);
		OpenCampMenu();
	}

	// ---------- state transitions ----------

	private void BeginPitch()
	{
		MobileParty main = MobileParty.MainParty;
		_partyIds.Clear();
		_parties.Clear();
		Army army = main.Army;
		if (army != null && army.LeaderParty == main)
		{
			foreach (MobileParty party in army.Parties)
			{
				// Parties still marching to join the army keep moving and do not camp.
				if (party == main || party?.AttachedTo == main)
				{
					AddParty(party);
				}
			}
		}
		AddParty(main);
		_partiesResolved = true;
		Vec2 anchor = main.Position.ToVec2();
		_anchorX = anchor.x;
		_anchorY = anchor.y;
		_startHours = NowHours;
		_phaseEndHours = _startHours + FieldCampSettings.PitchHours;
		_state = (int)CampState.Pitching;
		_healCarry.Clear();
		main.SetMoveModeHold();
		_visuals.Clear();
	}

	private void AddParty(MobileParty party)
	{
		if (party == null || string.IsNullOrEmpty(party.StringId) || _partyIds.Contains(party.StringId))
		{
			return;
		}
		_partyIds.Add(party.StringId);
		_parties.Add(party);
	}

	private void BeginStrike()
	{
		if (_state == (int)CampState.Pitching)
		{
			// Nothing has been set up yet: cancelling a pitch is free.
			FinishCamp("pitch_cancelled");
			return;
		}
		if (_state != (int)CampState.Camped)
		{
			return;
		}
		_state = (int)CampState.Striking;
		SettleIgnoredIssues();
		_phaseEndHours = NowHours + FieldCampSettings.StrikeHours;
		Logger.Log(LogSource, "strike start");
	}

	private void FinishCamp(string reason)
	{
		if (!IsActive)
		{
			return;
		}
		Logger.Log(LogSource, "camp end reason=" + reason + " hours=" + (NowHours - _startHours).ToString("0.0"));
		if (_state == (int)CampState.Camped && reason != "disabled")
		{
			SettleIgnoredIssues();
		}
		ClearIssueState();
		if (_state != (int)CampState.Pitching)
		{
			// Only a camp that was actually pitched counts as rest.
			_lastRestHours = NowHours;
		}
		_state = (int)CampState.None;
		_resting = false;
		_patrolPending = false;
		_patrolSettleUntilHours = -1;
		_partyIds.Clear();
		_parties.Clear();
		_partiesResolved = true;
		_healCarry.Clear();
		_visuals.Clear();
	}

	private void ResetAll()
	{
		_state = (int)CampState.None;
		_partyIds = new List<string>();
		_parties.Clear();
		_partiesResolved = true;
		_healCarry.Clear();
		_lastRestHours = -1;
		_lastDefeatHours = -1;
		ClearIssueState();
		_visuals.Forget();
	}

	// ---------- campaign ticks ----------

	/// <summary>Campaign TickEvent: fires only while campaign time runs.</summary>
	private void OnCampaignTick(float dt)
	{
		if (!IsActive)
		{
			return;
		}
		if (!FieldCampSettings.Enabled)
		{
			FinishCamp("disabled");
			LeaveCampMenusIfOpen();
			return;
		}
		EnsureParties();
		if (!IsActive)
		{
			LeaveCampMenusIfOpen();
			return;
		}
		if (PlayerEncounter.Current != null || MobileParty.MainParty.MapEvent != null)
		{
			FinishCamp("encounter");
			return;
		}
		EnforceHold();
		if ((_state == (int)CampState.Pitching || _state == (int)CampState.Striking) && NowHours >= _phaseEndHours)
		{
			if (_state == (int)CampState.Pitching)
			{
				_state = (int)CampState.Camped;
				_phaseEndHours = -1;
				JudgeIssues();
				Logger.Log(LogSource, "camp pitched");
				AnimusForgeQuickInfo.Show(FieldCampText.Pitched());
			}
			else
			{
				FinishCamp("struck");
				AnimusForgeQuickInfo.Show(FieldCampText.Struck());
				LeaveCampMenusIfOpen();
			}
		}
	}

	private void EnforceHold()
	{
		MobileParty main = MobileParty.MainParty;
		if (main.DefaultBehavior == AiBehavior.Hold)
		{
			return;
		}
		main.SetMoveModeHold();
		float now = TaleWorlds.Engine.Time.ApplicationTime;
		if (now >= _nextMoveBlockedHintTime)
		{
			_nextMoveBlockedHintTime = now + MoveBlockedHintIntervalSeconds;
			AnimusForgeQuickInfo.Show(FieldCampText.MoveBlocked());
		}
	}

	private void OnHourlyTick()
	{
		if (_state != (int)CampState.Camped)
		{
			return;
		}
		EnsureParties();
		float extraHealFactor = FieldCampSettings.HealMultiplier - 1f;
		float moraleGain = FieldCampSettings.MoralePerHour;
		float moraleCap = FieldCampSettings.MoraleCap;
		for (int i = 0; i < _parties.Count; i++)
		{
			MobileParty party = _parties[i];
			if (party == null || !party.IsActive || party.MapEvent != null)
			{
				continue;
			}
			if (extraHealFactor > 0f)
			{
				ApplyExtraHealing(party.Party, extraHealFactor);
			}
			if (moraleGain > 0f)
			{
				float morale = party.Morale;
				if (morale < moraleCap)
				{
					party.RecentEventsMorale += Math.Min(moraleGain, moraleCap - morale);
				}
			}
		}
	}

	/// <summary>
	/// Adds (multiplier - 1) x the native per-hour healing on top of vanilla
	/// PartyHealCampaignBehavior, so the native model stays the base rate.
	/// </summary>
	private void ApplyExtraHealing(PartyBase party, float extraFactor)
	{
		TroopRoster roster = party.MemberRoster;
		if (roster == null)
		{
			return;
		}
		_healCarry.TryGetValue(party, out Vec2 carry);
		carry.x += Math.Max(0f, party.HealingRateForMemberRegulars) / CampaignTime.HoursInDay * extraFactor;
		carry.y += Math.Max(0f, party.HealingRateForMemberHeroes) / CampaignTime.HoursInDay * extraFactor;
		int regulars = (int)carry.x;
		int heroHp = (int)carry.y;
		carry.x -= regulars;
		carry.y -= heroHp;
		_healCarry[party] = carry;
		for (int i = 0; i < roster.Count && (regulars > 0 || heroHp > 0); i++)
		{
			CharacterObject character = roster.GetCharacterAtIndex(i);
			if (character.IsRegular)
			{
				int healed = Math.Min(regulars, roster.GetElementWoundedNumber(i));
				if (healed > 0)
				{
					roster.AddToCountsAtIndex(i, 0, -healed);
					regulars -= healed;
				}
			}
			else if (heroHp > 0 && character.HeroObject != null && !character.HeroObject.IsHealthFull())
			{
				character.HeroObject.Heal(heroHp);
			}
		}
	}

	private void EnsureParties()
	{
		if (_partiesResolved || MobileParty.MainParty == null)
		{
			return;
		}
		_partiesResolved = true;
		_parties.Clear();
		for (int i = 0; i < _partyIds.Count; i++)
		{
			MobileParty party = Campaign.Current.CampaignObjectManager.Find<MobileParty>(_partyIds[i]);
			if (party != null && party.IsActive)
			{
				_parties.Add(party);
			}
		}
		_partyIds.Clear();
		for (int i = 0; i < _parties.Count; i++)
		{
			_partyIds.Add(_parties[i].StringId);
		}
		if (!_parties.Contains(MobileParty.MainParty))
		{
			FinishCamp("main_party_missing");
		}
	}

	// ---------- camp mission return ----------

	/// <summary>
	/// Runs on the engine tick once the camp mission has closed: the walk costs a
	/// fixed amount of campaign time, spent in the wait menu (rest effects apply).
	/// </summary>
	internal void BeginPatrol()
	{
		_patrolPending = true;
		_patrolMissionSeen = false;
		_patrolPlayerReady = false;
		_patrolPendingSince = TaleWorlds.Engine.Time.ApplicationTime;
	}

	internal void MarkPatrolPlayerReady() { if (_patrolPending) _patrolPlayerReady = true; }

	private void ConsumePatrolReturn()
	{
		_patrolPending = false;
		_patrolMissionSeen = false;
		bool ready = _patrolPlayerReady;
		_patrolPlayerReady = false;
		float hours = ready ? FieldCampSettings.PatrolHours : 0f;
		if (!IsActive || _state != (int)CampState.Camped)
		{
			return;
		}
		if (hours <= 0f)
		{
			OpenCampMenu();
			return;
		}
		_patrolSettleUntilHours = NowHours + hours;
		_phaseEndHours = _patrolSettleUntilHours;
		_resting = true;
		Logger.Log(LogSource, "patrol time charged hours=" + hours.ToString("0.0"));
		OpenCampMenu();
	}

	// ---------- campaign events ----------

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attacker, PartyBase defender)
	{
		if (!IsActive || mapEvent == null)
		{
			return;
		}
		if (attacker == PartyBase.MainParty || defender == PartyBase.MainParty || MobileParty.MainParty.MapEvent == mapEvent)
		{
			// Encounter strikes the camp at once; no strike time is charged.
			FinishCamp("encounter");
			return;
		}
		EnsureParties();
		for (int i = _parties.Count - 1; i >= 0; i--)
		{
			if (_parties[i]?.MapEvent == mapEvent)
			{
				DropParty(_parties[i], "map_event");
			}
		}
	}

	private void OnPartyRemovedFromArmy(MobileParty party)
	{
		if (IsActive && party != null && party != MobileParty.MainParty)
		{
			DropParty(party, "left_army");
		}
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase destroyer)
	{
		if (!IsActive || party == null)
		{
			return;
		}
		if (party == MobileParty.MainParty)
		{
			FinishCamp("main_party_destroyed");
			return;
		}
		DropParty(party, "destroyed");
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party != MobileParty.MainParty)
		{
			return;
		}
		_lastRestHours = NowHours;
		FinishCamp("entered_settlement");
	}

	private void DropParty(MobileParty party, string reason)
	{
		if (party == null || !_partyIds.Remove(party.StringId))
		{
			return;
		}
		_parties.Remove(party);
		_healCarry.Remove(party.Party);
		_visuals.Remove(party);
		Logger.Log(LogSource, "party dropped id=" + party.StringId + " reason=" + reason);
	}
}
