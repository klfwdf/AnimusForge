using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class SceneTauntConsequenceMissionLogic : MissionLogic
{
	private float _pendingDefeatCaptivityAtMissionTime = -1f;

	public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		canPlayerLeave = true;
		Mission current = Mission.Current;
		SceneTauntMissionBehavior sceneTauntMissionBehavior = ((current != null) ? current.GetMissionBehavior<SceneTauntMissionBehavior>() : null);
		if (sceneTauntMissionBehavior != null && sceneTauntMissionBehavior.ShouldBlockSceneExit())
		{
			canPlayerLeave = false;
			InformationManager.DisplayMessage(new InformationMessage("这场冲突还没结束，不能离开场景。", Color.FromUint(4294901760u)));
		}
		return null;
	}

	public override void OnMissionTick(float dt)
	{
		Mission current = Mission.Current;
		SceneTauntMissionBehavior sceneTauntMissionBehavior = ((current != null) ? current.GetMissionBehavior<SceneTauntMissionBehavior>() : null);
		if (sceneTauntMissionBehavior == null)
		{
			_pendingDefeatCaptivityAtMissionTime = -1f;
		}
		else if (sceneTauntMissionBehavior.ShouldCommitPlayerBattleDeathAfterMission())
		{
			if (_pendingDefeatCaptivityAtMissionTime < 0f)
			{
				_pendingDefeatCaptivityAtMissionTime = Mission.Current.CurrentTime + 0.2f;
			}
			else if (!(Mission.Current.CurrentTime < _pendingDefeatCaptivityAtMissionTime))
			{
				TryCommitPendingPlayerBattleDeath(sceneTauntMissionBehavior);
			}
		}
		else if (!sceneTauntMissionBehavior.ShouldSendPlayerToLocalDungeonOnDefeat())
		{
			_pendingDefeatCaptivityAtMissionTime = -1f;
		}
		else if (_pendingDefeatCaptivityAtMissionTime < 0f)
		{
			_pendingDefeatCaptivityAtMissionTime = Mission.Current.CurrentTime + 0.5f;
		}
		else if (!(Mission.Current.CurrentTime < _pendingDefeatCaptivityAtMissionTime))
		{
			TryCommitLocalDungeonCaptivity(sceneTauntMissionBehavior);
		}
	}

	private void TryCommitPendingPlayerBattleDeath(SceneTauntMissionBehavior missionBehavior)
	{
		try
		{
			missionBehavior.EnsurePendingPlayerBattleDeathQueued("scene_taunt_defeat_battle_death");
			SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_battle_death");
			SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_defeat_battle_death");
			missionBehavior.MarkPlayerDefeatOutcomeHandled();
			try
			{
				Mission.Current.NextCheckTimeEndMission = 0f;
			}
			catch
			{
			}
			Mission.Current.EndMission();
			Logger.Log("SceneTaunt", "Player was defeated after scene-taunt armed escalation and will die after mission cleanup.");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Ending mission for pending player battle death failed: " + ex.Message);
			missionBehavior.MarkPlayerDefeatOutcomeHandled();
		}
	}

	private void TryCommitLocalDungeonCaptivity(SceneTauntMissionBehavior missionBehavior)
	{
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			PartyBase val = ((currentSettlement != null) ? currentSettlement.Party : null);
			if (val == null)
			{
				Logger.Log("SceneTaunt", "Local dungeon captivity skipped because current settlement party is unavailable.");
				missionBehavior.MarkPlayerDefeatOutcomeHandled();
				return;
			}
			IFaction val2 = val.MapFaction ?? ((currentSettlement != null) ? currentSettlement.MapFaction : null);
			float effectiveCrimeRatingForExternal = SceneTauntBehavior.GetEffectiveCrimeRatingForExternal(val2);
			if (effectiveCrimeRatingForExternal >= 90f)
			{
				Hero val3 = ResolveExecutionExecutor(val, currentSettlement);
				string menuId = ResolveExecutionMenuId(currentSettlement);
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_forced_execution");
				SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_execution_threshold");
				SceneTauntBehavior.ClearDeferredCrimeForExternal(val2, "scene_taunt_execution_threshold");
				SceneTauntBehavior.QueuePendingForcedPlayerExecutionForExternal(val3, menuId, "scene_taunt_execution_threshold");
				missionBehavior.MarkPlayerDefeatOutcomeHandled();
				InformationManager.DisplayMessage(new InformationMessage($"你的累计犯罪度已达 {90f:0}，你将被处决。", Color.FromUint(4294901760u)));
				try
				{
					Mission.Current.NextCheckTimeEndMission = 0f;
				}
				catch
				{
				}
				Mission.Current.EndMission();
				Logger.Log("SceneTaunt", $"Player was defeated after armed escalation and reached execution threshold. Settlement={((currentSettlement != null) ? currentSettlement.Name : null)}, Faction={((val2 != null) ? val2.Name : null)}, EffectiveCrime={effectiveCrimeRatingForExternal:0.##}, Executor={((val3 != null) ? val3.Name : null)}");
				return;
			}
			if (missionBehavior.WasLastArmedDefeatCriminalConflict() && currentSettlement != null && currentSettlement.IsTown)
			{
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_criminal_target_flow");
				try
				{
					Campaign current = Campaign.Current;
					if (current != null)
					{
						GameMenuManager gameMenuManager = current.GameMenuManager;
						if (gameMenuManager != null)
						{
							gameMenuManager.SetNextMenu("town_inside_criminal");
						}
					}
				}
				catch
				{
				}
				missionBehavior.MarkPlayerDefeatOutcomeHandled();
				try
				{
					Mission.Current.NextCheckTimeEndMission = 0f;
				}
				catch
				{
				}
				Mission.Current.EndMission();
				Logger.Log("SceneTaunt", $"Player was defeated after criminal-target armed conflict and redirected to criminal judgment flow. Settlement={((currentSettlement != null) ? currentSettlement.Name : null)}, Captor={val.Name}");
				return;
			}
			bool flag = IsCaptorSameMapFactionAsPlayer(val);
			if (flag && currentSettlement != null && currentSettlement.IsTown)
			{
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_criminal_flow");
				try
				{
					Campaign current2 = Campaign.Current;
					if (current2 != null)
					{
						GameMenuManager gameMenuManager2 = current2.GameMenuManager;
						if (gameMenuManager2 != null)
						{
							gameMenuManager2.SetNextMenu("town_inside_criminal");
						}
					}
				}
				catch
				{
				}
				missionBehavior.MarkPlayerDefeatOutcomeHandled();
				try
				{
					Mission.Current.NextCheckTimeEndMission = 0f;
				}
				catch
				{
				}
				Mission.Current.EndMission();
				Logger.Log("SceneTaunt", $"Player was defeated after armed escalation and redirected to criminal judgment flow. Settlement={((currentSettlement != null) ? currentSettlement.Name : null)}, Captor={val.Name}");
				return;
			}
			if (flag)
			{
				SceneTauntBehavior.TryStartTemporaryDungeonWarForExternal(val, val.LeaderHero, "scene_taunt_armed_defeat_temp_war");
			}
			SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_local_dungeon");
			SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_armed_defeat_reset");
			try
			{
				Campaign current3 = Campaign.Current;
				if (current3 != null)
				{
					GameMenuManager gameMenuManager3 = current3.GameMenuManager;
					if (gameMenuManager3 != null)
					{
						gameMenuManager3.SetNextMenu("menu_captivity_castle_taken_prisoner");
					}
				}
			}
			catch
			{
			}
			SceneTauntBehavior.MarkPendingLocalDungeonCaptivityForExternal(val, "scene_taunt_armed_defeat");
			missionBehavior.MarkPlayerDefeatOutcomeHandled();
			try
			{
				Mission.Current.NextCheckTimeEndMission = 0f;
			}
			catch
			{
			}
			Mission.Current.EndMission();
			Logger.Log("SceneTaunt", $"Player was defeated after armed escalation and redirected to local dungeon flow. Settlement={((currentSettlement != null) ? currentSettlement.Name : null)}, Captor={val.Name}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing local dungeon captivity failed: " + ex.Message);
			missionBehavior.MarkPlayerDefeatOutcomeHandled();
		}
	}

	private static bool IsCaptorSameMapFactionAsPlayer(PartyBase captorParty)
	{
		try
		{
			IFaction val = ((captorParty != null) ? captorParty.MapFaction : null);
			PartyBase mainParty = PartyBase.MainParty;
			IFaction val2 = ((mainParty != null) ? mainParty.MapFaction : null);
			return val != null && val2 != null && val == val2;
		}
		catch
		{
			return false;
		}
	}

	private static Hero ResolveExecutionExecutor(PartyBase captorParty, Settlement settlement)
	{
		try
		{
			object obj = ((captorParty != null) ? captorParty.LeaderHero : null);
			if (obj == null)
			{
				if (settlement == null)
				{
					obj = null;
				}
				else
				{
					Clan ownerClan = settlement.OwnerClan;
					obj = ((ownerClan != null) ? ownerClan.Leader : null);
				}
				if (obj == null)
				{
					if (settlement == null)
					{
						obj = null;
					}
					else
					{
						IFaction mapFaction = settlement.MapFaction;
						obj = ((mapFaction != null) ? mapFaction.Leader : null);
					}
				}
			}
			return (Hero)obj;
		}
		catch
		{
			return null;
		}
	}

	private static string ResolveExecutionMenuId(Settlement settlement)
	{
		try
		{
			if (settlement != null && settlement.IsTown)
			{
				return "town_inside_criminal";
			}
		}
		catch
		{
		}
		return "menu_captivity_castle_taken_prisoner";
	}
}
