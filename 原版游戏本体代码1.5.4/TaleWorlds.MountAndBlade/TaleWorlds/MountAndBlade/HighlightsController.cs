using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class HighlightsController : MissionLogic
{
	public struct HighlightType
	{
		public string Id { get; private set; }

		public string Description { get; private set; }

		public string GroupId { get; private set; }

		public int StartDelta { get; private set; }

		public int EndDelta { get; private set; }

		public float MinVisibilityScore { get; private set; }

		public float MaxHighlightDistance { get; private set; }

		public bool IsVisibilityRequired { get; private set; }

		public HighlightType(string id, string description, string groupId, int startDelta, int endDelta, float minVisibilityScore, float maxHighlightDistance, bool isVisibilityRequired)
		{
			Id = id;
			Description = description;
			GroupId = groupId;
			StartDelta = startDelta;
			EndDelta = endDelta;
			MinVisibilityScore = minVisibilityScore;
			MaxHighlightDistance = maxHighlightDistance;
			IsVisibilityRequired = isVisibilityRequired;
		}
	}

	public struct Highlight
	{
		public HighlightType HighlightType;

		public float Start;

		public float End;
	}

	private bool _isKillingSpreeHappening;

	private List<float> _playerKillTimes;

	private const int MinKillingSpreeKills = 4;

	private const float MaxKillingSpreeDuration = 10f;

	private const float HighShotDifficultyThreshold = 7.5f;

	private bool _isArcherSalvoHappening;

	private List<float> _archerSalvoKillTimes;

	private const int MinArcherSalvoKills = 5;

	private const float MaxArcherSalvoDuration = 4f;

	private bool _isFirstImpact = true;

	private List<float> _cavalryChargeHitTimes;

	private const float CavalryChargeImpactTimeFrame = 3f;

	private const int MinCavalryChargeHits = 5;

	private Tuple<float, float> _lastSavedHighlightData;

	private List<Highlight> _highlightSaveQueue;

	private const float IgnoreIfOverlapsLastVideoPercent = 0.5f;

	private List<string> _savedHighlightGroups;

	private List<string> _highlightGroupIds = new List<string> { "grpid_incidents", "grpid_achievements" };

	protected static List<HighlightType> HighlightTypes { get; private set; }

	public static bool IsHighlightsInitialized { get; private set; }

	public bool IsAnyHighlightSaved => _savedHighlightGroups.Count > 0;

	public static void RemoveHighlights()
	{
		if (!IsHighlightsInitialized)
		{
			return;
		}
		foreach (HighlightType highlightType in HighlightTypes)
		{
			Highlights.RemoveHighlight(highlightType.Id);
		}
	}

	public HighlightType GetHighlightTypeWithId(string highlightId)
	{
		return HighlightTypes.First((HighlightType h) => h.Id == highlightId);
	}

	private void SaveVideo(string highlightID, string groupID, int startDelta, int endDelta)
	{
		Highlights.SaveVideo(highlightID, groupID, startDelta, endDelta);
		if (!_savedHighlightGroups.Contains(groupID))
		{
			_savedHighlightGroups.Add(groupID);
		}
	}

	public override void AfterStart()
	{
		if (!IsHighlightsInitialized)
		{
			HighlightTypes = new List<HighlightType>
			{
				new HighlightType("hlid_killing_spree", "Killing Spree", "grpid_incidents", -2010, 3000, 0.25f, float.MaxValue, isVisibilityRequired: true),
				new HighlightType("hlid_high_ranged_shot_difficulty", "Sharpshooter", "grpid_incidents", -5000, 3000, 0.25f, float.MaxValue, isVisibilityRequired: true),
				new HighlightType("hlid_archer_salvo_kills", "Death from Above", "grpid_incidents", -5004, 3000, 0.5f, 150f, isVisibilityRequired: false),
				new HighlightType("hlid_couched_lance_against_mounted_opponent", "Lance A Lot", "grpid_incidents", -5000, 3000, 0.25f, float.MaxValue, isVisibilityRequired: true),
				new HighlightType("hlid_cavalry_charge_first_impact", "Cavalry Charge First Impact", "grpid_incidents", -5000, 5000, 0.25f, float.MaxValue, isVisibilityRequired: false),
				new HighlightType("hlid_headshot_kill", "Headshot!", "grpid_incidents", -5000, 3000, 0.25f, 150f, isVisibilityRequired: true),
				new HighlightType("hlid_burning_ammunition_kill", "Burn Baby", "grpid_incidents", -5000, 3000, 0.25f, 100f, isVisibilityRequired: true),
				new HighlightType("hlid_throwing_weapon_kill_against_charging_enemy", "Throwing Weapon Kill Against Charging Enemy", "grpid_incidents", -5000, 3000, 0.25f, 150f, isVisibilityRequired: true)
			};
			Highlights.Initialize();
			foreach (HighlightType highlightType in HighlightTypes)
			{
				Highlights.AddHighlight(highlightType.Id, highlightType.Description);
			}
			IsHighlightsInitialized = true;
		}
		foreach (string highlightGroupId in _highlightGroupIds)
		{
			Highlights.OpenGroup(highlightGroupId);
		}
		_highlightSaveQueue = new List<Highlight>();
		_playerKillTimes = new List<float>();
		_archerSalvoKillTimes = new List<float>();
		_cavalryChargeHitTimes = new List<float>();
		_savedHighlightGroups = new List<string>();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectorAgent == null || affectedAgent == null || !affectorAgent.IsHuman || !affectedAgent.IsHuman || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
		{
			return;
		}
		bool flag = affectorAgent.Team != null && affectorAgent.Team.IsPlayerTeam;
		bool isMainAgent = affectorAgent.IsMainAgent;
		if ((((isMainAgent || flag) && !affectedAgent.Team.IsPlayerAlly && killingBlow.WeaponClass == 12) || killingBlow.WeaponClass == 13) && CanSaveHighlight(GetHighlightTypeWithId("hlid_archer_salvo_kills"), affectedAgent.Position))
		{
			if (!_isArcherSalvoHappening)
			{
				_archerSalvoKillTimes.RemoveAll((float ht) => ht + 4f < Mission.Current.CurrentTime);
			}
			_archerSalvoKillTimes.Add(Mission.Current.CurrentTime);
			if (_archerSalvoKillTimes.Count >= 5)
			{
				_isArcherSalvoHappening = true;
			}
		}
		if (isMainAgent && CanSaveHighlight(GetHighlightTypeWithId("hlid_killing_spree"), affectedAgent.Position))
		{
			if (!_isKillingSpreeHappening)
			{
				_playerKillTimes.RemoveAll((float ht) => ht + 10f < Mission.Current.CurrentTime);
			}
			_playerKillTimes.Add(Mission.Current.CurrentTime);
			if (_playerKillTimes.Count >= 4)
			{
				_isKillingSpreeHappening = true;
			}
		}
		Highlight highlight = new Highlight
		{
			Start = Mission.Current.CurrentTime,
			End = Mission.Current.CurrentTime
		};
		bool flag2 = false;
		if (isMainAgent && killingBlow.WeaponRecordWeaponFlags.HasAllFlags(WeaponFlags.Burning))
		{
			highlight.HighlightType = GetHighlightTypeWithId("hlid_burning_ammunition_kill");
			flag2 = true;
		}
		if (isMainAgent && killingBlow.IsMissile && killingBlow.IsHeadShot())
		{
			highlight.HighlightType = GetHighlightTypeWithId("hlid_headshot_kill");
			flag2 = true;
		}
		if (isMainAgent && killingBlow.IsMissile && affectedAgent.HasMount && affectedAgent.IsDoingPassiveAttack && (killingBlow.WeaponClass == 21 || killingBlow.WeaponClass == 22))
		{
			highlight.HighlightType = GetHighlightTypeWithId("hlid_throwing_weapon_kill_against_charging_enemy");
			flag2 = true;
		}
		if (_isFirstImpact && affectorAgent.Formation != null && affectorAgent.Formation.PhysicalClass.IsMeleeCavalry() && affectorAgent.Formation.GetReadonlyMovementOrderReference() == MovementOrder.MovementOrderCharge && CanSaveHighlight(GetHighlightTypeWithId("hlid_cavalry_charge_first_impact"), affectedAgent.Position))
		{
			_cavalryChargeHitTimes.RemoveAll((float ht) => ht + 3f < Mission.Current.CurrentTime);
			_cavalryChargeHitTimes.Add(Mission.Current.CurrentTime);
			if (_cavalryChargeHitTimes.Count >= 5)
			{
				highlight.HighlightType = GetHighlightTypeWithId("hlid_cavalry_charge_first_impact");
				highlight.Start = _cavalryChargeHitTimes[0];
				highlight.End = _cavalryChargeHitTimes[_cavalryChargeHitTimes.Count - 1];
				flag2 = true;
				_isFirstImpact = false;
				_cavalryChargeHitTimes.Clear();
			}
		}
		if (flag2)
		{
			SaveHighlight(highlight, affectedAgent.Position);
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent == null || affectedAgent == null || !affectorAgent.IsHuman || !affectedAgent.IsHuman)
		{
			return;
		}
		bool isMainAgent = affectorAgent.IsMainAgent;
		Highlight highlight = new Highlight
		{
			Start = Mission.Current.CurrentTime,
			End = Mission.Current.CurrentTime
		};
		bool flag = false;
		if (isMainAgent && shotDifficulty >= 7.5f)
		{
			highlight.HighlightType = GetHighlightTypeWithId("hlid_high_ranged_shot_difficulty");
			flag = true;
		}
		if (isMainAgent && affectedAgent.HasMount && blow.AttackType == AgentAttackType.Standard && affectorAgent.HasMount && affectorAgent.IsDoingPassiveAttack)
		{
			highlight.HighlightType = GetHighlightTypeWithId("hlid_couched_lance_against_mounted_opponent");
			flag = true;
		}
		if (_isFirstImpact && affectorAgent.Formation != null && affectorAgent.Formation.PhysicalClass.IsMeleeCavalry() && affectorAgent.Formation.GetReadonlyMovementOrderReference() == MovementOrder.MovementOrderCharge && CanSaveHighlight(GetHighlightTypeWithId("hlid_cavalry_charge_first_impact"), affectedAgent.Position))
		{
			_cavalryChargeHitTimes.RemoveAll((float ht) => ht + 3f < Mission.Current.CurrentTime);
			_cavalryChargeHitTimes.Add(Mission.Current.CurrentTime);
			if (_cavalryChargeHitTimes.Count >= 5)
			{
				highlight.HighlightType = GetHighlightTypeWithId("hlid_cavalry_charge_first_impact");
				highlight.Start = _cavalryChargeHitTimes[0];
				highlight.End = _cavalryChargeHitTimes[_cavalryChargeHitTimes.Count - 1];
				flag = true;
				_isFirstImpact = false;
				_cavalryChargeHitTimes.Clear();
			}
		}
		if (flag)
		{
			SaveHighlight(highlight, affectedAgent.Position);
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (_isArcherSalvoHappening && _archerSalvoKillTimes[0] + 4f < Mission.Current.CurrentTime)
		{
			Highlight highlight = default(Highlight);
			highlight.HighlightType = GetHighlightTypeWithId("hlid_archer_salvo_kills");
			highlight.Start = _archerSalvoKillTimes[0];
			highlight.End = _archerSalvoKillTimes[_archerSalvoKillTimes.Count - 1];
			SaveHighlight(highlight);
			_isArcherSalvoHappening = false;
			_archerSalvoKillTimes.Clear();
		}
		if (_isKillingSpreeHappening && _playerKillTimes[0] + 10f < Mission.Current.CurrentTime)
		{
			Highlight highlight2 = default(Highlight);
			highlight2.HighlightType = GetHighlightTypeWithId("hlid_killing_spree");
			highlight2.Start = _playerKillTimes[0];
			highlight2.End = _playerKillTimes[_playerKillTimes.Count - 1];
			SaveHighlight(highlight2);
			_isKillingSpreeHappening = false;
			_playerKillTimes.Clear();
		}
		TickHighlightsToBeSaved();
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		foreach (string highlightGroupId in _highlightGroupIds)
		{
			Highlights.CloseGroup(highlightGroupId);
		}
		_highlightSaveQueue = null;
		_lastSavedHighlightData = null;
		_playerKillTimes = null;
		_archerSalvoKillTimes = null;
		_cavalryChargeHitTimes = null;
	}

	public static void AddHighlightType(HighlightType highlightType)
	{
		if (!HighlightTypes.Any((HighlightType h) => h.Id == highlightType.Id))
		{
			if (IsHighlightsInitialized)
			{
				Highlights.AddHighlight(highlightType.Id, highlightType.Description);
			}
			HighlightTypes.Add(highlightType);
		}
	}

	public void SaveHighlight(Highlight highlight)
	{
		_highlightSaveQueue.Add(highlight);
	}

	public void SaveHighlight(Highlight highlight, Vec3 position)
	{
		if (CanSaveHighlight(highlight.HighlightType, position))
		{
			_highlightSaveQueue.Add(highlight);
		}
	}

	public bool CanSaveHighlight(HighlightType highlightType, Vec3 position)
	{
		if (highlightType.MaxHighlightDistance >= Mission.Current.Scene.LastFinalRenderCameraFrame.origin.Distance(position) && highlightType.MinVisibilityScore <= GetPlayerIsLookingAtPositionScore(position))
		{
			if (highlightType.IsVisibilityRequired)
			{
				return CanSeePosition(position);
			}
			return true;
		}
		return false;
	}

	public float GetPlayerIsLookingAtPositionScore(Vec3 position)
	{
		Vec3 vec = -Mission.Current.Scene.LastFinalRenderCameraFrame.rotation.u;
		Vec3 origin = Mission.Current.Scene.LastFinalRenderCameraFrame.origin;
		return TaleWorlds.Library.MathF.Max(Vec3.DotProduct(vec.NormalizedCopy(), (position - origin).NormalizedCopy()), 0f);
	}

	public bool CanSeePosition(Vec3 position)
	{
		Vec3 origin = Mission.Current.Scene.LastFinalRenderCameraFrame.origin;
		if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(origin, position, out var collisionDistance, 0.01f, BodyFlags.CameraCollisionRayCastExludeFlags))
		{
			return TaleWorlds.Library.MathF.Abs(position.Distance(origin) - collisionDistance) < 0.1f;
		}
		return true;
	}

	public void ShowSummary()
	{
		if (IsAnyHighlightSaved)
		{
			Highlights.OpenSummary(_savedHighlightGroups);
		}
	}

	private void TickHighlightsToBeSaved()
	{
		if (_highlightSaveQueue == null)
		{
			return;
		}
		if (_lastSavedHighlightData != null && _highlightSaveQueue.Count > 0)
		{
			float item = _lastSavedHighlightData.Item1;
			float item2 = _lastSavedHighlightData.Item2;
			float num = item2 - (item2 - item) * 0.5f;
			for (int i = 0; i < _highlightSaveQueue.Count; i++)
			{
				if (_highlightSaveQueue[i].Start - (float)_highlightSaveQueue[i].HighlightType.StartDelta < num)
				{
					_highlightSaveQueue.Remove(_highlightSaveQueue[i]);
					i--;
				}
			}
		}
		if (_highlightSaveQueue.Count <= 0)
		{
			return;
		}
		float num2 = _highlightSaveQueue[0].Start + (float)(_highlightSaveQueue[0].HighlightType.StartDelta / 1000);
		float num3 = _highlightSaveQueue[0].End + (float)(_highlightSaveQueue[0].HighlightType.EndDelta / 1000);
		for (int j = 1; j < _highlightSaveQueue.Count; j++)
		{
			float num4 = _highlightSaveQueue[j].Start + (float)(_highlightSaveQueue[j].HighlightType.StartDelta / 1000);
			float num5 = _highlightSaveQueue[j].End + (float)(_highlightSaveQueue[j].HighlightType.EndDelta / 1000);
			if (num4 < num2)
			{
				num2 = num4;
			}
			if (num5 > num3)
			{
				num3 = num5;
			}
		}
		SaveVideo(_highlightSaveQueue[0].HighlightType.Id, _highlightSaveQueue[0].HighlightType.GroupId, (int)(num2 - Mission.Current.CurrentTime) * 1000, (int)(num3 - Mission.Current.CurrentTime) * 1000);
		_lastSavedHighlightData = new Tuple<float, float>(num2, num3);
		_highlightSaveQueue.Clear();
	}
}
