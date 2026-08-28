using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AnimusForge.SiegeAftermathIntervention;
using HarmonyLib;
using SandBox;
using SandBox.BoardGames.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public class SceneTauntBehavior : CampaignBehaviorBase
{
	private static readonly Regex SceneTauntWarnTagRegex = new Regex("\\[ACTION:SCENE_TAUNT_WARN\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneTauntFightTagRegex = new Regex("\\[ACTION:SCENE_TAUNT_FIGHT\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	internal const float ForcedExecutionCrimeThreshold = 90f;

	private HashSet<string> _warnedSceneTargetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private List<string> _warnedSceneTargetKeysStorage = new List<string>();

	private bool _pendingTemporaryDungeonWarPeace;

	private string _pendingTemporaryDungeonWarPlayerFactionId = "";

	private string _pendingTemporaryDungeonWarEnemyFactionId = "";

	private bool _pendingDeferredLordSceneDiplomacy;

	private string _pendingDeferredLordSceneTargetHeroId = "";

	private string _pendingDeferredLordSceneTargetFactionId = "";

	private string _pendingDeferredLordSceneSettlementId = "";

	private string _pendingDeferredLordSceneReason = "";

	private bool _armedSettlementCarryoverActive;

	private string _armedSettlementCarryoverSettlementId = "";

	private string _armedSettlementCarryoverSource = "";

	private string _armedCarryoverLastAlertSettlementId = "";

	private string _armedCarryoverLastAlertLocationId = "";

	private static bool _pendingLocalDungeonCaptivityMenu;

	private static float _pendingLocalDungeonCaptivityMenuAtTime;

	private static PartyBase _pendingLocalDungeonCaptivityParty;

	private static bool _pendingMainHeroBattleDeath;

	private static string _pendingMainHeroBattleDeathKillerHeroId = "";

	private static long _pendingMainHeroBattleDeathRequestUtcTicks;

	private readonly Dictionary<Hero, Hero> _pendingSceneNotableBattleDeaths = new Dictionary<Hero, Hero>();

	private Dictionary<string, float> _pendingDeferredCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, float> _pendingDeferredCrimeByFactionStorage = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, float> _crimeRefillReserveByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, float> _crimeRefillReserveByFactionStorage = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, float> _lastObservedNativeCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, float> _lastObservedNativeCrimeByFactionStorage = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _criminalTrustRewardTenthBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, int> _criminalTrustRewardTenthBySettlementStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private bool _isCommittingDeferredCrime;

	private bool _isCommittingDeferredLordSceneDiplomacy;

	private bool _pendingForcedPlayerExecution;

	private string _pendingForcedPlayerExecutionExecutorHeroId = "";

	private string _pendingForcedPlayerExecutionMenuId = "";

	public static SceneTauntBehavior Instance { get; private set; }

	public SceneTauntBehavior()
	{
		Instance = this;
	}

	internal static bool IsPeaceSceneConflictEnabled()
	{
		return DuelSettings.IsPeaceSceneConflictEnabled();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, OnCanHeroDie);
		CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener(this, OnBeforeMainCharacterDied);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_warnedSceneTargetKeys == null)
		{
			_warnedSceneTargetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		if (_warnedSceneTargetKeysStorage == null)
		{
			_warnedSceneTargetKeysStorage = new List<string>();
		}
		if (_pendingTemporaryDungeonWarPlayerFactionId == null)
		{
			_pendingTemporaryDungeonWarPlayerFactionId = "";
		}
		if (_pendingTemporaryDungeonWarEnemyFactionId == null)
		{
			_pendingTemporaryDungeonWarEnemyFactionId = "";
		}
		if (_pendingDeferredLordSceneTargetHeroId == null)
		{
			_pendingDeferredLordSceneTargetHeroId = "";
		}
		if (_pendingDeferredLordSceneTargetFactionId == null)
		{
			_pendingDeferredLordSceneTargetFactionId = "";
		}
		if (_pendingDeferredLordSceneSettlementId == null)
		{
			_pendingDeferredLordSceneSettlementId = "";
		}
		if (_pendingDeferredLordSceneReason == null)
		{
			_pendingDeferredLordSceneReason = "";
		}
		if (_pendingDeferredCrimeByFaction == null)
		{
			_pendingDeferredCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		}
		if (_pendingDeferredCrimeByFactionStorage == null)
		{
			_pendingDeferredCrimeByFactionStorage = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		}
		if (_crimeRefillReserveByFaction == null)
		{
			_crimeRefillReserveByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		}
		if (_crimeRefillReserveByFactionStorage == null)
		{
			_crimeRefillReserveByFactionStorage = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		}
		if (_lastObservedNativeCrimeByFaction == null)
		{
			_lastObservedNativeCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		}
		if (_lastObservedNativeCrimeByFactionStorage == null)
		{
			_lastObservedNativeCrimeByFactionStorage = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		}
		if (_criminalTrustRewardTenthBySettlement == null)
		{
			_criminalTrustRewardTenthBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_criminalTrustRewardTenthBySettlementStorage == null)
		{
			_criminalTrustRewardTenthBySettlementStorage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_armedSettlementCarryoverSettlementId == null)
		{
			_armedSettlementCarryoverSettlementId = "";
		}
		if (_armedSettlementCarryoverSource == null)
		{
			_armedSettlementCarryoverSource = "";
		}
		if (_armedCarryoverLastAlertSettlementId == null)
		{
			_armedCarryoverLastAlertSettlementId = "";
		}
		if (_armedCarryoverLastAlertLocationId == null)
		{
			_armedCarryoverLastAlertLocationId = "";
		}
		if (dataStore.IsSaving)
		{
			_warnedSceneTargetKeysStorage = _warnedSceneTargetKeys.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			_pendingDeferredCrimeByFactionStorage = _pendingDeferredCrimeByFaction.Where((KeyValuePair<string, float> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0f).ToDictionary((KeyValuePair<string, float> x) => x.Key, (KeyValuePair<string, float> x) => x.Value, StringComparer.OrdinalIgnoreCase);
			_crimeRefillReserveByFactionStorage = _crimeRefillReserveByFaction.Where((KeyValuePair<string, float> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0f).ToDictionary((KeyValuePair<string, float> x) => x.Key, (KeyValuePair<string, float> x) => x.Value, StringComparer.OrdinalIgnoreCase);
			_lastObservedNativeCrimeByFactionStorage = _lastObservedNativeCrimeByFaction.Where((KeyValuePair<string, float> x) => !string.IsNullOrWhiteSpace(x.Key)).ToDictionary((KeyValuePair<string, float> x) => x.Key, (KeyValuePair<string, float> x) => MathF.Max(0f, x.Value), StringComparer.OrdinalIgnoreCase);
			_criminalTrustRewardTenthBySettlementStorage = _criminalTrustRewardTenthBySettlement.Where((KeyValuePair<string, int> x) => !string.IsNullOrWhiteSpace(x.Key) && x.Value > 0).ToDictionary((KeyValuePair<string, int> x) => x.Key, (KeyValuePair<string, int> x) => x.Value, StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData("_sceneTauntWarnedTargets_v1", ref _warnedSceneTargetKeysStorage);
		dataStore.SyncData("_sceneTauntPendingTempWarPeace_v1", ref _pendingTemporaryDungeonWarPeace);
		dataStore.SyncData("_sceneTauntPendingTempWarPlayerFactionId_v1", ref _pendingTemporaryDungeonWarPlayerFactionId);
		dataStore.SyncData("_sceneTauntPendingTempWarEnemyFactionId_v1", ref _pendingTemporaryDungeonWarEnemyFactionId);
		dataStore.SyncData("_sceneTauntPendingDeferredLordSceneDiplomacy_v1", ref _pendingDeferredLordSceneDiplomacy);
		dataStore.SyncData("_sceneTauntPendingDeferredLordSceneTargetHeroId_v1", ref _pendingDeferredLordSceneTargetHeroId);
		dataStore.SyncData("_sceneTauntPendingDeferredLordSceneTargetFactionId_v1", ref _pendingDeferredLordSceneTargetFactionId);
		dataStore.SyncData("_sceneTauntPendingDeferredLordSceneSettlementId_v1", ref _pendingDeferredLordSceneSettlementId);
		dataStore.SyncData("_sceneTauntPendingDeferredLordSceneReason_v1", ref _pendingDeferredLordSceneReason);
		dataStore.SyncData("_sceneTauntDeferredCrimeByFaction_v1", ref _pendingDeferredCrimeByFactionStorage);
		dataStore.SyncData("_sceneTauntCrimeRefillReserveByFaction_v1", ref _crimeRefillReserveByFactionStorage);
		dataStore.SyncData("_sceneTauntLastObservedNativeCrimeByFaction_v1", ref _lastObservedNativeCrimeByFactionStorage);
		dataStore.SyncData("_sceneTauntCriminalTrustRewardTenthBySettlement_v1", ref _criminalTrustRewardTenthBySettlementStorage);
		dataStore.SyncData("_sceneTauntArmedCarryoverActive_v1", ref _armedSettlementCarryoverActive);
		dataStore.SyncData("_sceneTauntArmedCarryoverSettlementId_v1", ref _armedSettlementCarryoverSettlementId);
		dataStore.SyncData("_sceneTauntArmedCarryoverSource_v1", ref _armedSettlementCarryoverSource);
		if (!dataStore.IsSaving)
		{
			_warnedSceneTargetKeys = new HashSet<string>(_warnedSceneTargetKeysStorage ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			_pendingDeferredCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			if (_pendingDeferredCrimeByFactionStorage != null)
			{
				foreach (KeyValuePair<string, float> item in _pendingDeferredCrimeByFactionStorage)
				{
					string text = (item.Key ?? "").Trim();
					float num = MathF.Max(0f, item.Value);
					if (!string.IsNullOrWhiteSpace(text) && num > 0f)
					{
						_pendingDeferredCrimeByFaction[text] = num;
					}
				}
			}
			_crimeRefillReserveByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			// Deprecated: refill reserve caused duplicate scene-taunt crime accounting.
			// Keep the field for save compatibility, but do not restore old values.
			_lastObservedNativeCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			if (_lastObservedNativeCrimeByFactionStorage != null)
			{
				foreach (KeyValuePair<string, float> item3 in _lastObservedNativeCrimeByFactionStorage)
				{
					string text3 = (item3.Key ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						_lastObservedNativeCrimeByFaction[text3] = MathF.Max(0f, item3.Value);
					}
				}
			}
			_criminalTrustRewardTenthBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			if (_criminalTrustRewardTenthBySettlementStorage != null)
			{
				foreach (KeyValuePair<string, int> item4 in _criminalTrustRewardTenthBySettlementStorage)
				{
					string text4 = (item4.Key ?? "").Trim();
					int num2 = Math.Max(0, item4.Value);
					if (!string.IsNullOrWhiteSpace(text4) && num2 > 0)
					{
						_criminalTrustRewardTenthBySettlement[text4] = num2;
					}
				}
			}
			_pendingTemporaryDungeonWarPlayerFactionId = (_pendingTemporaryDungeonWarPlayerFactionId ?? "").Trim();
			_pendingTemporaryDungeonWarEnemyFactionId = (_pendingTemporaryDungeonWarEnemyFactionId ?? "").Trim();
			_pendingDeferredLordSceneTargetHeroId = (_pendingDeferredLordSceneTargetHeroId ?? "").Trim();
			_pendingDeferredLordSceneTargetFactionId = (_pendingDeferredLordSceneTargetFactionId ?? "").Trim();
			_pendingDeferredLordSceneSettlementId = (_pendingDeferredLordSceneSettlementId ?? "").Trim();
			_pendingDeferredLordSceneReason = (_pendingDeferredLordSceneReason ?? "").Trim();
			_armedSettlementCarryoverSettlementId = (_armedSettlementCarryoverSettlementId ?? "").Trim();
			_armedSettlementCarryoverSource = (_armedSettlementCarryoverSource ?? "").Trim();
			_armedCarryoverLastAlertSettlementId = (_armedCarryoverLastAlertSettlementId ?? "").Trim();
			_armedCarryoverLastAlertLocationId = (_armedCarryoverLastAlertLocationId ?? "").Trim().ToLowerInvariant();
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		try
		{
			if (!(mission is Mission mission2))
			{
				return;
			}
			if (DuelBehavior.IsAnimusForgeIndependentDuelMission(mission2))
			{
				Logger.Log("SceneTaunt", "Skipped scene-taunt behaviors for an AnimusForge independent duel mission.");
				return;
			}
			if (mission2.GetMissionBehavior<SceneTauntMissionBehavior>() == null)
			{
				mission2.AddMissionBehavior(new SceneTauntMissionBehavior());
			}
			if (mission2.GetMissionBehavior<SceneTauntConsequenceMissionLogic>() == null)
			{
				mission2.AddMissionBehavior(new SceneTauntConsequenceMissionLogic());
			}
			if (mission2.GetMissionBehavior<SceneTauntPlayerDeathAgentStateDeciderLogic>() == null)
			{
				mission2.AddMissionBehavior(new SceneTauntPlayerDeathAgentStateDeciderLogic());
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "OnMissionStarted failed: " + ex.Message);
		}
	}

	private void OnCampaignTick(float dt)
	{
		using (PerfProbe.Scope("SceneTaunt.OnCampaignTick"))
		{
		if (TryCommitPendingMainHeroBattleDeath())
		{
			return;
		}
		if (TryCommitPendingForcedPlayerExecution())
		{
			return;
		}
		TryForcePendingForcedPlayerExecutionMenuIfReady();
		if (_pendingForcedPlayerExecution)
		{
			return;
		}
		TryForcePendingLocalDungeonCaptivityMenuIfReady();
		TryClearExpiredArmedSettlementCarryover();
		TryCommitDeferredCrimeWhenBackOnWorldMap();
		TryCommitDeferredLordSceneDiplomacyWhenBackOnWorldMap();
		TryCommitPendingSceneNotableBattleDeaths();
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		TryCommitPendingMainHeroBattleDeath();
		TryCommitPendingForcedPlayerExecution();
		TryCommitDeferredCrimeWhenBackOnWorldMap();
		TryCommitDeferredLordSceneDiplomacyWhenBackOnWorldMap();
	}

	private void OnDailyTick()
	{
		TryCommitPendingMainHeroBattleDeath();
		TryCommitDeferredCrimeWhenBackOnWorldMap();
		TryCommitDeferredLordSceneDiplomacyWhenBackOnWorldMap();
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		if (prisoner != Hero.MainHero || capturer == null || _pendingForcedPlayerExecution)
		{
			return;
		}
		try
		{
			IFaction faction = capturer.MapFaction ?? capturer.LeaderHero?.MapFaction;
			float effectiveCrimeRatingForExternal = GetEffectiveCrimeRatingForExternal(faction);
			if (faction == null || effectiveCrimeRatingForExternal < ForcedExecutionCrimeThreshold)
			{
				return;
			}
			Hero executor = capturer.LeaderHero ?? faction.Leader;
			QueuePendingForcedPlayerExecutionForExternal(executor, "", "scene_taunt_capture_execution_threshold");
			AnimusForgeQuickInfo.Show($"{faction.Name} 认定你的罪行已满，俘虏后将处决你。", executor?.CharacterObject);
			Logger.Log("SceneTaunt", $"Queued forced execution after capture. Captor={capturer.Name}, Faction={faction.Name}, EffectiveCrime={effectiveCrimeRatingForExternal:0.##}, Executor={executor?.Name}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Capture-based forced execution check failed: " + ex.Message);
		}
	}

	private void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		if (victim != Hero.MainHero)
		{
			return;
		}
		try
		{
			_pendingDeferredCrimeByFaction?.Clear();
			_crimeRefillReserveByFaction?.Clear();
			ClearPendingDeferredLordSceneDiplomacy("main_character_died");
			ClearPendingMainHeroBattleDeath("main_character_died");
			Logger.Log("SceneTaunt", $"Cleared scene-taunt crime tracking after main hero death. Detail={detail}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Clearing scene-taunt crime tracking on main hero death failed: " + ex.Message);
		}
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		if (prisoner != Hero.MainHero || !_pendingTemporaryDungeonWarPeace)
		{
			return;
		}
		try
		{
			IFaction factionById = ResolveFactionById(_pendingTemporaryDungeonWarPlayerFactionId);
			IFaction factionById2 = ResolveFactionById(_pendingTemporaryDungeonWarEnemyFactionId);
			if (factionById != null && factionById2 != null && factionById != factionById2 && FactionManager.IsAtWarAgainstFaction(factionById, factionById2))
			{
				MakePeaceAction.Apply(factionById, factionById2);
				Logger.Log("SceneTaunt", $"Temporary dungeon war ended after player release. PlayerFaction={factionById.Name}, EnemyFaction={factionById2.Name}, Detail={detail}");
			}
			else
			{
				Logger.Log("SceneTaunt", $"Temporary dungeon war peace cleanup skipped. PlayerFaction={factionById?.Name}, EnemyFaction={factionById2?.Name}, Detail={detail}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Ending temporary dungeon war after release failed: " + ex.Message);
		}
		finally
		{
			ClearPendingTemporaryDungeonWarPeace("player_released");
		}
	}

	private void OnCanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		if (!result || hero == null || causeOfDeath != KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
		{
			return;
		}
		try
		{
			if (SceneTauntMissionBehavior.ShouldSuppressSceneNotableDeathExternal(hero))
			{
				result = false;
				Logger.Log("SceneTaunt", $"Suppressed notable battle death after non-lethal scene-taunt hit. Hero={hero.Name}");
			}
			else if (SceneTauntMissionBehavior.ShouldDeferSceneNotableBattleDeathExternal(hero))
			{
				result = false;
				Logger.Log("SceneTaunt", $"Deferred notable battle death until after mission cleanup. Hero={hero.Name}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "CanHeroDie scene-taunt override failed: " + ex.Message);
		}
	}

	private void TryCommitPendingSceneNotableBattleDeaths()
	{
		if (_pendingSceneNotableBattleDeaths.Count == 0)
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		foreach (KeyValuePair<Hero, Hero> item in _pendingSceneNotableBattleDeaths.ToList())
		{
			Hero key = item.Key;
			Hero value = item.Value;
			if (key == null || !key.IsAlive)
			{
				_pendingSceneNotableBattleDeaths.Remove(key);
				continue;
			}
			try
			{
				KillCharacterAction.ApplyByBattle(key, value, true);
				Logger.Log("SceneTaunt", $"Committed deferred scene notable battle death. Hero={key.Name}, Killer={value?.Name}");
				_pendingSceneNotableBattleDeaths.Remove(key);
			}
			catch (Exception ex)
			{
				Logger.Log("SceneTaunt", "Committing deferred scene notable battle death failed: " + ex.Message);
			}
		}
	}

	private bool TryCommitPendingMainHeroBattleDeath()
	{
		if (!_pendingMainHeroBattleDeath)
		{
			return false;
		}
		try
		{
			if (Hero.MainHero == null || !Hero.MainHero.IsAlive)
			{
				ClearPendingMainHeroBattleDeath("main_hero_not_alive");
				return false;
			}
			if (Mission.Current != null || Game.Current?.GameStateManager?.ActiveState is MissionState || Campaign.Current == null)
			{
				return true;
			}
			bool flag = false;
			try
			{
				flag = Campaign.Current.ConversationManager?.OneToOneConversationAgent != null;
			}
			catch
			{
			}
			if (flag)
			{
				return true;
			}
			bool flag2 = false;
			try
			{
				string text = (Game.Current?.GameStateManager?.ActiveState)?.GetType()?.FullName ?? string.Empty;
				flag2 = !string.IsNullOrEmpty(text) && text.IndexOf("MapState", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			catch
			{
				flag2 = false;
			}
			if (!flag2)
			{
				return true;
			}
			Hero hero = ResolveHeroById(_pendingMainHeroBattleDeathKillerHeroId);
			ClearPendingMainHeroBattleDeath("committed");
			ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_battle_death_committed");
			ClearArmedCarryoverForExternal("scene_taunt_battle_death_committed");
			KillCharacterAction.ApplyByBattle(Hero.MainHero, hero);
			Logger.Log("SceneTaunt", $"Committed pending scene-taunt main hero battle death. Killer={hero?.Name}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing pending scene-taunt main hero battle death failed: " + ex.Message);
			return true;
		}
	}

	private static void MarkPendingLocalDungeonCaptivityMenu(PartyBase captorParty, string reason)
	{
		_pendingLocalDungeonCaptivityMenu = true;
		_pendingLocalDungeonCaptivityParty = captorParty;
		try
		{
			_pendingLocalDungeonCaptivityMenuAtTime = TaleWorlds.Engine.Time.ApplicationTime;
		}
		catch
		{
			_pendingLocalDungeonCaptivityMenuAtTime = 0f;
		}
		Logger.Log("SceneTaunt", $"Marked pending local dungeon captivity menu. Reason={reason ?? "N/A"}, Captor={captorParty?.Name}");
	}

	private static void ClearPendingLocalDungeonCaptivityMenu(string reason)
	{
		_pendingLocalDungeonCaptivityMenu = false;
		_pendingLocalDungeonCaptivityMenuAtTime = 0f;
		_pendingLocalDungeonCaptivityParty = null;
		Logger.Log("SceneTaunt", "Cleared pending local dungeon captivity menu. Reason=" + (reason ?? "N/A"));
	}

	private static void TryForcePendingLocalDungeonCaptivityMenuIfReady()
	{
		if (!_pendingLocalDungeonCaptivityMenu)
		{
			return;
		}
		try
		{
			if (Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return;
			}
		}
		catch
		{
		}
		string text = null;
		try
		{
			text = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
		}
		catch
		{
			text = null;
		}
		if (text == "menu_captivity_castle_taken_prisoner")
		{
			ClearPendingLocalDungeonCaptivityMenu("local_dungeon_menu_opened");
			return;
		}
		if (text == "taken_prisoner" || text == "defeated_and_taken_prisoner")
		{
			ClearPendingLocalDungeonCaptivityMenu("generic_captivity_menu_opened");
			return;
		}
		if (text == "town_inside_criminal")
		{
			ClearPendingLocalDungeonCaptivityMenu("criminal_judgment_menu_opened");
			return;
		}
		bool flag = false;
		try
		{
			flag = Hero.MainHero != null && Hero.MainHero.IsPrisoner;
		}
		catch
		{
			flag = false;
		}
		if (flag)
		{
			ClearPendingLocalDungeonCaptivityMenu("player_already_prisoner");
			return;
		}
		if (Settlement.CurrentSettlement == null)
		{
			float num = 0f;
			try
			{
				float applicationTime = TaleWorlds.Engine.Time.ApplicationTime;
				if (_pendingLocalDungeonCaptivityMenuAtTime > 0f)
				{
					num = applicationTime - _pendingLocalDungeonCaptivityMenuAtTime;
				}
			}
			catch
			{
			}
			if (num > 10f)
			{
				ClearPendingLocalDungeonCaptivityMenu("local_settlement_context_timeout");
			}
			return;
		}
		try
		{
			if (Campaign.Current?.CurrentMenuContext != null)
			{
				GameMenu.SwitchToMenu("menu_captivity_castle_taken_prisoner");
			}
			else
			{
				GameMenu.ActivateGameMenu("menu_captivity_castle_taken_prisoner");
			}
			string text2 = null;
			try
			{
				text2 = Campaign.Current?.CurrentMenuContext?.GameMenu?.StringId;
			}
			catch
			{
				text2 = null;
			}
			if (text2 == "menu_captivity_castle_taken_prisoner")
			{
				ClearPendingLocalDungeonCaptivityMenu("local_dungeon_menu_activated");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Force pending local dungeon captivity menu failed: " + ex.Message);
		}
	}

	internal static void MarkPendingLocalDungeonCaptivityForExternal(PartyBase captorParty, string reason)
	{
		MarkPendingLocalDungeonCaptivityMenu(captorParty, reason);
	}

	internal static void ClearPendingLocalDungeonCaptivityForExternal(string reason)
	{
		ClearPendingLocalDungeonCaptivityMenu(reason);
	}

	private static Settlement GetActiveSettlementSafe()
	{
		try
		{
			return Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static string GetActiveSettlementIdSafe()
	{
		try
		{
			return (GetActiveSettlementSafe()?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static bool IsReadyToCommitDeferredCrime()
	{
		try
		{
			if (Mission.Current != null || Game.Current?.GameStateManager?.ActiveState is MissionState)
			{
				return false;
			}
			if (CampaignMission.Current?.Location != null)
			{
				return false;
			}
			if (Campaign.Current?.GameMenuManager?.NextLocation != null)
			{
				return false;
			}
			if (Hero.MainHero != null && Hero.MainHero.IsPrisoner)
			{
				return false;
			}
			if (GetActiveSettlementSafe() != null)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return true;
	}

	private bool TryCommitPendingForcedPlayerExecution()
	{
		if (!_pendingForcedPlayerExecution)
		{
			return false;
		}
		try
		{
			if (Hero.MainHero == null || !Hero.MainHero.IsAlive)
			{
				ClearPendingForcedPlayerExecution("main_hero_not_alive");
				return false;
			}
			if (Mission.Current != null || Game.Current?.GameStateManager?.ActiveState is MissionState || Campaign.Current?.CurrentMenuContext == null)
			{
				return true;
			}
			Hero hero = ResolveHeroById(_pendingForcedPlayerExecutionExecutorHeroId);
			ClearPendingForcedPlayerExecution("committed");
			KillCharacterAction.ApplyByExecution(Hero.MainHero, hero, true, false);
			Logger.Log("SceneTaunt", $"Committed pending forced player execution. Executor={hero?.Name}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing pending forced player execution failed: " + ex.Message);
			try
			{
				Hero hero2 = ResolveHeroById(_pendingForcedPlayerExecutionExecutorHeroId);
				ClearPendingForcedPlayerExecution("fallback_murder");
				KillCharacterAction.ApplyByMurder(Hero.MainHero, hero2, true);
				Logger.Log("SceneTaunt", $"Fallback player execution used murder path. Executor={hero2?.Name}");
				return true;
			}
			catch (Exception ex2)
			{
				Logger.Log("SceneTaunt", "Fallback forced player murder failed: " + ex2.Message);
				ClearPendingForcedPlayerExecution("failed");
				return false;
			}
		}
	}

	private void TryForcePendingForcedPlayerExecutionMenuIfReady()
	{
		if (!_pendingForcedPlayerExecution)
		{
			return;
		}
		try
		{
			if (Mission.Current != null || Game.Current?.GameStateManager?.ActiveState is MissionState || Campaign.Current?.CurrentMenuContext != null)
			{
				return;
			}
			string text = (_pendingForcedPlayerExecutionMenuId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			GameMenu.ActivateGameMenu(text);
			Logger.Log("SceneTaunt", "Activated pending forced player execution menu. Menu=" + text);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Activating pending forced player execution menu failed: " + ex.Message);
		}
	}

	private void TryCommitDeferredCrimeWhenBackOnWorldMap()
	{
		if (_isCommittingDeferredCrime || _pendingDeferredCrimeByFaction == null || _pendingDeferredCrimeByFaction.Count == 0 || !IsReadyToCommitDeferredCrime())
		{
			return;
		}
		_isCommittingDeferredCrime = true;
		try
		{
			foreach (KeyValuePair<string, float> item in _pendingDeferredCrimeByFaction.ToList())
			{
				string text = (item.Key ?? "").Trim();
				float num = MathF.Max(0f, item.Value);
				if (string.IsNullOrWhiteSpace(text) || num <= 0f)
				{
					_pendingDeferredCrimeByFaction.Remove(item.Key);
					continue;
				}
				IFaction factionById = ResolveFactionById(text);
				if (factionById == null)
				{
					Logger.Log("SceneTaunt", $"Deferred scene crime dropped because faction could not be resolved. FactionId={text}, Amount={num:0.##}");
					_pendingDeferredCrimeByFaction.Remove(item.Key);
					continue;
				}
				try
				{
					float num2 = MathF.Max(0f, factionById.MainHeroCrimeRating);
					float num3 = Campaign.Current?.Models?.CrimeModel?.GetMaxCrimeRating() ?? 100f;
					float num4 = MathF.Max(0f, num3 - num2);
					if (num4 <= 0f)
					{
						Logger.Log("SceneTaunt", $"Deferred scene-taunt crime pool not injected because native crime is already at max. Faction={factionById.Name}, NativeCrime={num2:0.##}, Pool={num:0.##}, Max={num3:0.##}");
						continue;
					}
					float num5 = MathF.Min(num, num4);
					if (num5 <= 0f)
					{
						continue;
					}
					float num6 = MathF.Max(0f, num - num5);
					if (num6 <= 0f)
					{
						_pendingDeferredCrimeByFaction.Remove(item.Key);
					}
					else
					{
						_pendingDeferredCrimeByFaction[text] = num6;
					}
					ChangeCrimeRatingAction.Apply(factionById, num5, true);
					AnimusForgeQuickInfo.Show($"离开当前场景后，{factionById.Name} 的累计犯罪度 +{num5:0.#}。");
					Logger.Log("SceneTaunt", $"Injected scene-taunt crime pool into native crime. Faction={factionById.Name}, NativeBefore={num2:0.##}, Added={num5:0.##}, RemainingPool={num6:0.##}, NativeAfter={MathF.Max(0f, factionById.MainHeroCrimeRating):0.##}");
				}
				catch (Exception ex)
				{
					if (num > 0f)
					{
						_pendingDeferredCrimeByFaction[text] = num;
					}
					Logger.Log("SceneTaunt", "Committing deferred scene crime on world map failed: " + ex.Message);
				}
			}
		}
		finally
		{
			_isCommittingDeferredCrime = false;
		}
	}

	private void TryCommitDeferredLordSceneDiplomacyWhenBackOnWorldMap()
	{
		if (_isCommittingDeferredLordSceneDiplomacy || !_pendingDeferredLordSceneDiplomacy || !IsReadyToCommitDeferredCrime())
		{
			return;
		}
		_isCommittingDeferredLordSceneDiplomacy = true;
		try
		{
			Hero targetHero = ResolveHeroById(_pendingDeferredLordSceneTargetHeroId);
			IFaction targetFaction = ResolveFactionById(_pendingDeferredLordSceneTargetFactionId);
			if (targetHero == null && targetFaction != null)
			{
				targetHero = targetFaction.Leader;
			}
			if (targetHero == null && targetFaction == null)
			{
				Logger.Log("SceneTaunt", "Deferred lord scene diplomacy dropped because target hero/faction could not be resolved.");
				ClearPendingDeferredLordSceneDiplomacy("missing_target");
				return;
			}
			PartyBase defenderParty = ResolveDeferredLordSceneDefenderParty(targetHero, targetFaction);
			string reason = string.IsNullOrWhiteSpace(_pendingDeferredLordSceneReason) ? "scene_taunt_lord_scene_deferred" : _pendingDeferredLordSceneReason;
			bool applied = LordEncounterBehavior.ApplyHostileEscalationDiplomaticConsequences(defenderParty, targetHero, reason, "SceneTaunt");
			Logger.Log("SceneTaunt", $"Committed deferred lord scene diplomacy after leaving settlement. Applied={applied}, TargetHero={targetHero?.Name}, TargetFaction={targetFaction?.Name}, Defender={defenderParty?.Name}, Reason={reason}");
			ClearPendingDeferredLordSceneDiplomacy("committed");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing deferred lord scene diplomacy failed: " + ex.Message);
		}
		finally
		{
			_isCommittingDeferredLordSceneDiplomacy = false;
		}
	}

	private void TryClearExpiredArmedSettlementCarryover()
	{
		if (!_armedSettlementCarryoverActive)
		{
			return;
		}
		string activeSettlementIdSafe = GetActiveSettlementIdSafe();
		if (string.IsNullOrWhiteSpace(activeSettlementIdSafe))
		{
			ClearArmedSettlementCarryover("left_settlement");
		}
		else if (!string.Equals(activeSettlementIdSafe, _armedSettlementCarryoverSettlementId, StringComparison.OrdinalIgnoreCase))
		{
			ClearArmedSettlementCarryover("changed_settlement");
		}
	}

	private void MarkArmedSettlementCarryover(string settlementId, string source)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		_armedSettlementCarryoverActive = true;
		_armedSettlementCarryoverSettlementId = text;
		_armedSettlementCarryoverSource = (source ?? "").Trim();
		Logger.Log("SceneTaunt", $"Marked armed settlement carryover. SettlementId={text}, Source={_armedSettlementCarryoverSource}");
	}

	private void ClearArmedSettlementCarryover(string reason)
	{
		if (!_armedSettlementCarryoverActive && string.IsNullOrWhiteSpace(_armedSettlementCarryoverSettlementId))
		{
			return;
		}
		_armedSettlementCarryoverActive = false;
		_armedSettlementCarryoverSettlementId = "";
		_armedSettlementCarryoverSource = "";
		_armedCarryoverLastAlertSettlementId = "";
		_armedCarryoverLastAlertLocationId = "";
		Logger.Log("SceneTaunt", "Cleared armed settlement carryover. Reason=" + (reason ?? "N/A"));
	}

	private static string GetCurrentCarryoverLocationIdSafe()
	{
		try
		{
			return (CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			return "";
		}
	}

	private bool HasShownCarryoverNoAuthorityAlertForCurrentLocation()
	{
		string activeSettlementIdSafe = GetActiveSettlementIdSafe();
		string currentCarryoverLocationIdSafe = GetCurrentCarryoverLocationIdSafe();
		return !string.IsNullOrWhiteSpace(activeSettlementIdSafe) && !string.IsNullOrWhiteSpace(currentCarryoverLocationIdSafe) && string.Equals(activeSettlementIdSafe, _armedCarryoverLastAlertSettlementId, StringComparison.OrdinalIgnoreCase) && string.Equals(currentCarryoverLocationIdSafe, _armedCarryoverLastAlertLocationId, StringComparison.OrdinalIgnoreCase);
	}

	private void MarkCarryoverNoAuthorityAlertShownForCurrentLocation()
	{
		_armedCarryoverLastAlertSettlementId = GetActiveSettlementIdSafe();
		_armedCarryoverLastAlertLocationId = GetCurrentCarryoverLocationIdSafe();
	}

	internal static bool HasShownCarryoverNoAuthorityAlertForCurrentLocationExternal()
	{
		try
		{
			return Instance?.HasShownCarryoverNoAuthorityAlertForCurrentLocation() ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static void MarkCarryoverNoAuthorityAlertShownForCurrentLocationExternal()
	{
		try
		{
			Instance?.MarkCarryoverNoAuthorityAlertShownForCurrentLocation();
		}
		catch
		{
		}
	}

	internal static void MarkArmedCarryoverForCurrentSettlement(string reason)
	{
		if (Instance == null)
		{
			return;
		}
		string activeSettlementIdSafe = GetActiveSettlementIdSafe();
		if (!string.IsNullOrWhiteSpace(activeSettlementIdSafe))
		{
			Instance.MarkArmedSettlementCarryover(activeSettlementIdSafe, reason);
		}
	}

	internal static bool HasArmedCarryoverForCurrentSettlement()
	{
		if (Instance == null || !Instance._armedSettlementCarryoverActive)
		{
			return false;
		}
		string activeSettlementIdSafe = GetActiveSettlementIdSafe();
		return !string.IsNullOrWhiteSpace(activeSettlementIdSafe) && string.Equals(activeSettlementIdSafe, Instance._armedSettlementCarryoverSettlementId, StringComparison.OrdinalIgnoreCase);
	}

	internal static string GetArmedCarryoverSourceForCurrentSettlement()
	{
		if (!HasArmedCarryoverForCurrentSettlement())
		{
			return "";
		}
		return (Instance?._armedSettlementCarryoverSource ?? "").Trim();
	}

	internal static void MarkPendingSceneNotableBattleDeathForExternal(Hero victim, Hero killer, string reason)
	{
		if (Instance == null || victim == null)
		{
			return;
		}
		Instance._pendingSceneNotableBattleDeaths[victim] = killer;
		Logger.Log("SceneTaunt", $"Marked pending deferred scene notable battle death. Hero={victim.Name}, Killer={killer?.Name}, Reason={reason ?? "N/A"}");
	}

	internal static void ClearArmedCarryoverForExternal(string reason)
	{
		Instance?.ClearArmedSettlementCarryover(reason);
	}

	internal static void QueueDeferredCrimeForExternal(IFaction faction, float amount, string reason)
	{
		try
		{
			if (Instance == null || faction == null)
			{
				return;
			}
			string text = (faction.StringId ?? "").Trim();
			float num = MathF.Max(0f, amount);
			if (string.IsNullOrWhiteSpace(text) || num <= 0f)
			{
				return;
			}
			if (Instance._pendingDeferredCrimeByFaction == null)
			{
				Instance._pendingDeferredCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			}
			Instance._pendingDeferredCrimeByFaction.TryGetValue(text, out var value);
			Instance._pendingDeferredCrimeByFaction[text] = value + num;
			Logger.Log("SceneTaunt", $"Queued deferred scene-taunt crime. Faction={faction.Name}, Added={num:0.##}, Pending={Instance._pendingDeferredCrimeByFaction[text]:0.##}, Reason={reason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing deferred scene-taunt crime failed: " + ex.Message);
		}
	}

	private float GetPendingDeferredCrimeAmount(IFaction faction)
	{
		try
		{
			string text = (faction?.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || _pendingDeferredCrimeByFaction == null)
			{
				return 0f;
			}
			return _pendingDeferredCrimeByFaction.TryGetValue(text, out var value) ? MathF.Max(0f, value) : 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private float GetTrackedCrimeTotalAmount(IFaction faction)
	{
		try
		{
			float num = MathF.Max(0f, faction?.MainHeroCrimeRating ?? 0f);
			float num2 = GetPendingDeferredCrimeAmount(faction);
			return MathF.Max(0f, num + num2);
		}
		catch
		{
			return MathF.Max(0f, faction?.MainHeroCrimeRating ?? 0f);
		}
	}

	private void TryShowTrackedCrimeTotalMessage(IFaction faction)
	{
		try
		{
			if (faction == null)
			{
				return;
			}
			float trackedCrimeTotalAmount = GetTrackedCrimeTotalAmount(faction);
			if (trackedCrimeTotalAmount <= 0f)
			{
				return;
			}
			AnimusForgeQuickInfo.Show($"你在{faction.Name}积累了{trackedCrimeTotalAmount:0.#}犯罪度！");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Showing tracked crime total message failed: " + ex.Message);
		}
	}

	internal static void TryShowTrackedCrimeTotalMessageForExternal(IFaction faction)
	{
		Instance?.TryShowTrackedCrimeTotalMessage(faction);
	}

	internal static float GetTrackedCrimeTotalForExternal(IFaction faction)
	{
		try
		{
			return Instance?.GetTrackedCrimeTotalAmount(faction)
				?? MathF.Max(0f, faction?.MainHeroCrimeRating ?? 0f);
		}
		catch
		{
			return MathF.Max(0f, faction?.MainHeroCrimeRating ?? 0f);
		}
	}

	internal static void TryRewardSettlementTrustForCriminalKnockdownForExternal(Settlement settlement, string victimName)
	{
		try
		{
			if (settlement == null || RewardSystemBehavior.Instance == null || Instance == null)
			{
				return;
			}
			string text = (settlement.StringId ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (Instance._criminalTrustRewardTenthBySettlement == null)
			{
				Instance._criminalTrustRewardTenthBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			}
			Instance._criminalTrustRewardTenthBySettlement.TryGetValue(text, out var value);
			int num = Math.Max(0, value) + 13;
			int num2 = num / 10;
			int num3 = num % 10;
			if (num3 > 0)
			{
				Instance._criminalTrustRewardTenthBySettlement[text] = num3;
			}
			else
			{
				Instance._criminalTrustRewardTenthBySettlement.Remove(text);
			}
			if (num2 > 0)
			{
				RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(settlement, num2, "scene_taunt_criminal_knockdown_reward");
			}
			string text2 = string.IsNullOrWhiteSpace(victimName) ? "匪类" : victimName;
			AnimusForgeQuickInfo.Show($"击倒 {text2}：{settlement.Name} 的公共信任 +1.3。");
			Logger.Log("SceneTaunt", $"Rewarded settlement trust for criminal knockdown. Settlement={settlement.Name}, Victim={text2}, GrantedTenths=13, WholeApplied={num2}, CarryTenths={num3}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Rewarding settlement trust for criminal knockdown failed: " + ex.Message);
		}
	}

	private float GetCrimeRefillReserveAmount(IFaction faction)
	{
		try
		{
			string text = (faction?.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || _crimeRefillReserveByFaction == null)
			{
				return 0f;
			}
			return _crimeRefillReserveByFaction.TryGetValue(text, out var value) ? MathF.Max(0f, value) : 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private void AddCrimeRefillReserve(IFaction faction, float amount, string reason)
	{
		try
		{
			string text = (faction?.StringId ?? "").Trim();
			float num = MathF.Max(0f, amount);
			if (string.IsNullOrWhiteSpace(text) || num <= 0f)
			{
				return;
			}
			if (_crimeRefillReserveByFaction == null)
			{
				_crimeRefillReserveByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			}
			_crimeRefillReserveByFaction.TryGetValue(text, out var value);
			_crimeRefillReserveByFaction[text] = value + num;
			_lastObservedNativeCrimeByFaction[text] = MathF.Max(0f, faction.MainHeroCrimeRating);
			Logger.Log("SceneTaunt", $"Added scene-taunt crime refill reserve. Faction={faction.Name}, Added={num:0.##}, Reserve={_crimeRefillReserveByFaction[text]:0.##}, Reason={reason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Adding scene-taunt crime refill reserve failed: " + ex.Message);
		}
	}

	internal static void AddCrimeRefillReserveForExternal(IFaction faction, float amount, string reason)
	{
		Instance?.AddCrimeRefillReserve(faction, amount, reason);
	}

	private void TryRefillCrimeRatingsFromReserve()
	{
		if (_crimeRefillReserveByFaction == null || _crimeRefillReserveByFaction.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, float> item in _crimeRefillReserveByFaction.ToList())
		{
			string text = (item.Key ?? "").Trim();
			float num = MathF.Max(0f, item.Value);
			if (string.IsNullOrWhiteSpace(text) || num <= 0f)
			{
				_crimeRefillReserveByFaction.Remove(item.Key);
				continue;
			}
			IFaction factionById = ResolveFactionById(text);
			if (factionById == null)
			{
				_crimeRefillReserveByFaction.Remove(item.Key);
				continue;
			}
			try
			{
				float num2 = MathF.Max(0f, factionById.MainHeroCrimeRating);
				float num3 = _lastObservedNativeCrimeByFaction.TryGetValue(text, out var value) ? MathF.Max(0f, value) : num2;
				if (num2 >= num3 - 0.01f)
				{
					_lastObservedNativeCrimeByFaction[text] = num2;
					continue;
				}
				float num4 = MathF.Max(0f, (Campaign.Current?.Models?.CrimeModel?.GetMaxCrimeRating() ?? 100f) - num2);
				float num5 = MathF.Min(num, num4);
				if (num5 <= 0f)
				{
					_lastObservedNativeCrimeByFaction[text] = num2;
					continue;
				}
				ChangeCrimeRatingAction.Apply(factionById, num5, true);
				float num6 = MathF.Max(0f, num - num5);
				float num7 = MathF.Max(0f, factionById.MainHeroCrimeRating);
				_lastObservedNativeCrimeByFaction[text] = num7;
				if (num6 <= 0f)
				{
					_crimeRefillReserveByFaction.Remove(item.Key);
				}
				else
				{
					_crimeRefillReserveByFaction[text] = num6;
				}
				Logger.Log("SceneTaunt", $"Injected scene-taunt crime reserve into native crime after native decay. Faction={factionById.Name}, PreviousNative={num3:0.##}, CurrentNativeBeforeInject={num2:0.##}, Added={num5:0.##}, RemainingReserve={num6:0.##}, NativeAfterInject={num7:0.##}");
			}
			catch (Exception ex)
			{
				Logger.Log("SceneTaunt", "Refilling native crime from scene-taunt reserve failed: " + ex.Message);
			}
		}
	}

	internal static float GetEffectiveCrimeRatingForExternal(IFaction faction)
	{
		try
		{
			float num = MathF.Max(0f, faction?.MainHeroCrimeRating ?? 0f);
			float num2 = Instance?.GetPendingDeferredCrimeAmount(faction) ?? 0f;
			float maxCrimeRating = Campaign.Current?.Models?.CrimeModel?.GetMaxCrimeRating() ?? 100f;
			return MBMath.ClampFloat(num + num2, 0f, maxCrimeRating);
		}
		catch
		{
			return MathF.Max(0f, faction?.MainHeroCrimeRating ?? 0f);
		}
	}

	internal static float ClearDeferredCrimeForExternal(IFaction faction, string reason)
	{
		try
		{
			if (Instance == null || faction == null || Instance._pendingDeferredCrimeByFaction == null)
			{
				return 0f;
			}
			string text = (faction.StringId ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || !Instance._pendingDeferredCrimeByFaction.TryGetValue(text, out var value))
			{
				return 0f;
			}
			float num = MathF.Max(0f, value);
			Instance._pendingDeferredCrimeByFaction.Remove(text);
			Logger.Log("SceneTaunt", $"Cleared deferred scene-taunt crime. Faction={faction.Name}, Amount={num:0.##}, Reason={reason ?? "N/A"}");
			return num;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Clearing deferred scene-taunt crime failed: " + ex.Message);
			return 0f;
		}
	}

	internal static void QueuePendingForcedPlayerExecutionForExternal(Hero executor, string menuId, string reason)
	{
		try
		{
			if (Instance == null)
			{
				return;
			}
			Instance._pendingForcedPlayerExecution = true;
			Instance._pendingForcedPlayerExecutionExecutorHeroId = (executor?.StringId ?? "").Trim();
			Instance._pendingForcedPlayerExecutionMenuId = (menuId ?? "").Trim();
			Logger.Log("SceneTaunt", $"Marked pending forced player execution. Executor={executor?.Name}, Menu={Instance._pendingForcedPlayerExecutionMenuId}, Reason={reason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing pending forced player execution failed: " + ex.Message);
		}
	}

	internal static void QueuePendingMainHeroBattleDeathForExternal(Hero killer, string reason)
	{
		string text = (killer?.StringId ?? "").Trim();
		_pendingMainHeroBattleDeath = true;
		if (!string.IsNullOrWhiteSpace(text))
		{
			_pendingMainHeroBattleDeathKillerHeroId = text;
		}
		_pendingMainHeroBattleDeathRequestUtcTicks = DateTime.UtcNow.Ticks;
		Instance?.ClearPendingForcedPlayerExecution("scene_taunt_battle_death");
		Logger.Log("SceneTaunt", $"Marked pending scene-taunt main hero battle death. Killer={killer?.Name}, Reason={reason ?? "N/A"}");
	}

	internal static void ClearPendingForcedPlayerExecutionForExternal(string reason)
	{
		Instance?.ClearPendingForcedPlayerExecution(reason);
	}

	internal static void ClearPendingMainHeroBattleDeathForExternal(string reason)
	{
		if (Instance != null)
		{
			Instance.ClearPendingMainHeroBattleDeath(reason);
		}
		_pendingMainHeroBattleDeath = false;
		_pendingMainHeroBattleDeathKillerHeroId = "";
		_pendingMainHeroBattleDeathRequestUtcTicks = 0L;
	}

	private void ClearPendingForcedPlayerExecution(string reason)
	{
		_pendingForcedPlayerExecution = false;
		_pendingForcedPlayerExecutionExecutorHeroId = "";
		_pendingForcedPlayerExecutionMenuId = "";
		Logger.Log("SceneTaunt", "Cleared pending forced player execution. Reason=" + (reason ?? "N/A"));
	}

	private void ClearPendingMainHeroBattleDeath(string reason)
	{
		_pendingMainHeroBattleDeath = false;
		_pendingMainHeroBattleDeathKillerHeroId = "";
		_pendingMainHeroBattleDeathRequestUtcTicks = 0L;
		Logger.Log("SceneTaunt", "Cleared pending scene-taunt main hero battle death. Reason=" + (reason ?? "N/A"));
	}

	private static IFaction ResolveFactionById(string factionId)
	{
		string text = (factionId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Campaign.Current?.Factions?.FirstOrDefault((IFaction x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveHeroById(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			Hero object2 = Game.Current?.ObjectManager?.GetObject<Hero>(text);
			if (object2 != null)
			{
				return object2;
			}
		}
		catch
		{
		}
		try
		{
			Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			if (hero != null)
			{
				return hero;
			}
			return Hero.DeadOrDisabledHeroes.FirstOrDefault((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveSettlementById(string settlementId)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Campaign.Current?.Settlements?.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private PartyBase ResolveDeferredLordSceneDefenderParty(Hero targetHero, IFaction targetFaction)
	{
		try
		{
			PartyBase partyBase = targetHero?.PartyBelongedTo?.Party;
			if (partyBase != null)
			{
				return partyBase;
			}
		}
		catch
		{
		}
		try
		{
			Settlement settlement = ResolveSettlementById(_pendingDeferredLordSceneSettlementId);
			PartyBase partyBase2 = settlement?.Party;
			IFaction faction = partyBase2?.MapFaction ?? settlement?.MapFaction;
			if (partyBase2 != null && (targetFaction == null || faction == null || faction == targetFaction))
			{
				return partyBase2;
			}
		}
		catch
		{
		}
		return null;
	}

	private void QueueDeferredLordSceneDiplomacy(Hero targetHero, string reason)
	{
		try
		{
			IFaction faction = null;
			try
			{
				faction = targetHero?.PartyBelongedTo?.Party?.MapFaction ?? targetHero?.MapFaction;
			}
			catch
			{
				faction = targetHero?.MapFaction;
			}
			if (targetHero == null && faction == null)
			{
				return;
			}
			string text = (faction?.StringId ?? "").Trim();
			string text2 = (targetHero?.StringId ?? "").Trim();
			if (_pendingDeferredLordSceneDiplomacy && string.Equals(_pendingDeferredLordSceneTargetFactionId, text, StringComparison.OrdinalIgnoreCase))
			{
				if (string.IsNullOrWhiteSpace(_pendingDeferredLordSceneTargetHeroId) && !string.IsNullOrWhiteSpace(text2))
				{
					_pendingDeferredLordSceneTargetHeroId = text2;
				}
				Logger.Log("SceneTaunt", $"Deferred lord scene diplomacy already queued. TargetHero={targetHero?.Name}, TargetFaction={faction?.Name}, Reason={reason ?? "N/A"}");
				return;
			}
			_pendingDeferredLordSceneDiplomacy = true;
			_pendingDeferredLordSceneTargetHeroId = text2;
			_pendingDeferredLordSceneTargetFactionId = text;
			_pendingDeferredLordSceneSettlementId = GetActiveSettlementIdSafe();
			_pendingDeferredLordSceneReason = string.IsNullOrWhiteSpace(reason) ? "scene_taunt_lord_scene_deferred" : reason.Trim();
			Logger.Log("SceneTaunt", $"Queued deferred lord scene diplomacy until world map. TargetHero={targetHero?.Name}, TargetFaction={faction?.Name}, SettlementId={_pendingDeferredLordSceneSettlementId}, Reason={_pendingDeferredLordSceneReason}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing deferred lord scene diplomacy failed: " + ex.Message);
		}
	}

	internal static void QueueDeferredLordSceneDiplomacyForExternal(Hero targetHero, string reason)
	{
		Instance?.QueueDeferredLordSceneDiplomacy(targetHero, reason);
	}

	private void ClearPendingDeferredLordSceneDiplomacy(string reason)
	{
		if (!_pendingDeferredLordSceneDiplomacy && string.IsNullOrWhiteSpace(_pendingDeferredLordSceneTargetHeroId) && string.IsNullOrWhiteSpace(_pendingDeferredLordSceneTargetFactionId))
		{
			return;
		}
		_pendingDeferredLordSceneDiplomacy = false;
		_pendingDeferredLordSceneTargetHeroId = "";
		_pendingDeferredLordSceneTargetFactionId = "";
		_pendingDeferredLordSceneSettlementId = "";
		_pendingDeferredLordSceneReason = "";
		Logger.Log("SceneTaunt", "Cleared deferred lord scene diplomacy. Reason=" + (reason ?? "N/A"));
	}

	private static void MarkPendingTemporaryDungeonWarPeace(IFaction playerFaction, IFaction enemyFaction, string reason)
	{
		if (Instance == null || playerFaction == null || enemyFaction == null || playerFaction == enemyFaction)
		{
			return;
		}
		Instance._pendingTemporaryDungeonWarPeace = true;
		Instance._pendingTemporaryDungeonWarPlayerFactionId = (playerFaction.StringId ?? "").Trim();
		Instance._pendingTemporaryDungeonWarEnemyFactionId = (enemyFaction.StringId ?? "").Trim();
		Logger.Log("SceneTaunt", $"Marked pending temporary dungeon war peace. Reason={reason ?? "N/A"}, PlayerFaction={playerFaction.Name}, EnemyFaction={enemyFaction.Name}");
	}

	private static void ClearPendingTemporaryDungeonWarPeace(string reason)
	{
		if (Instance == null)
		{
			return;
		}
		Instance._pendingTemporaryDungeonWarPeace = false;
		Instance._pendingTemporaryDungeonWarPlayerFactionId = "";
		Instance._pendingTemporaryDungeonWarEnemyFactionId = "";
		Logger.Log("SceneTaunt", "Cleared pending temporary dungeon war peace. Reason=" + (reason ?? "N/A"));
	}

	internal static void TryStartTemporaryDungeonWarForExternal(PartyBase captorParty, Hero targetHero, string reason)
	{
		try
		{
			IFaction faction = PartyBase.MainParty?.MapFaction;
			IFaction faction2 = captorParty?.MapFaction ?? targetHero?.MapFaction;
			bool flag = faction != null && faction2 != null && faction != faction2 && FactionManager.IsAtWarAgainstFaction(faction, faction2);
			LordEncounterBehavior.ApplyHostileEscalationDiplomaticConsequences(captorParty, targetHero, reason ?? "scene_taunt_dungeon_defeat", "SceneTaunt");
			IFaction faction3 = PartyBase.MainParty?.MapFaction;
			if (!flag && faction3 != null && faction2 != null && faction3 != faction2 && FactionManager.IsAtWarAgainstFaction(faction3, faction2))
			{
				MarkPendingTemporaryDungeonWarPeace(faction3, faction2, reason ?? "scene_taunt_dungeon_defeat");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting temporary dungeon war failed: " + ex.Message);
		}
	}

	internal static bool IsEligibleSceneTauntCharacter(CharacterObject targetCharacter)
	{
		if (targetCharacter == null || targetCharacter.IsHero || IsChildSceneProtectedTarget(targetCharacter))
		{
			return false;
		}
		switch (targetCharacter.Occupation)
		{
		case Occupation.Gangster:
		case Occupation.GangLeader:
		case Occupation.Bandit:
			return true;
		}
		if (IsSoldierSceneTauntTarget(targetCharacter))
		{
			return true;
		}
		switch (targetCharacter.Occupation)
		{
		case Occupation.Guard:
		case Occupation.PrisonGuard:
		case Occupation.Mercenary:
		case Occupation.ArenaMaster:
			return false;
		default:
			return !targetCharacter.IsSoldier;
		}
	}

	internal static bool IsSoldierSceneTauntTarget(CharacterObject targetCharacter)
	{
		return targetCharacter != null && !targetCharacter.IsHero && targetCharacter.Occupation == Occupation.Soldier;
	}

	internal static bool IsSceneLordTauntTarget(Hero targetHero)
	{
		return targetHero != null && targetHero.IsLord && !IsPlayerProtectedSceneAttackTarget(targetHero) && !IsMeetingTauntContext(targetHero);
	}

	internal static bool IsSceneNotableTauntTarget(Hero targetHero)
	{
		try
		{
			if (targetHero == null || IsMeetingTauntContext(targetHero) || IsSceneLordTauntTarget(targetHero))
			{
				return false;
			}
			if (IsChildSceneProtectedTarget(targetHero.CharacterObject))
			{
				return false;
			}
			switch (targetHero.Occupation)
			{
			case Occupation.Headman:
			case Occupation.RuralNotable:
			case Occupation.Merchant:
			case Occupation.Artisan:
			case Occupation.GangLeader:
			case Occupation.Preacher:
				return true;
			default:
				return false;
			}
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsChildSceneProtectedTarget(CharacterObject targetCharacter)
	{
		try
		{
			if (targetCharacter == null)
			{
				return false;
			}
			if (targetCharacter.IsChildTemplate)
			{
				return true;
			}
			return targetCharacter.Age > 0f && targetCharacter.Age < Campaign.Current?.Models?.AgeModel?.HeroComesOfAge;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsPlayerProtectedSceneAttackTarget(Hero targetHero)
	{
		try
		{
			if (targetHero == null)
			{
				return false;
			}
			if (targetHero == Hero.MainHero || targetHero.IsPlayerCompanion || IsPlayerMainPartyHero(targetHero))
			{
				return true;
			}
			Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
			return playerClan != null && targetHero.Clan == playerClan;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsPlayerMainPartyHero(Hero targetHero)
	{
		try
		{
			if (targetHero == null || targetHero.IsPrisoner)
			{
				return false;
			}
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty == null)
			{
				return false;
			}
			if (targetHero.PartyBelongedTo == mainParty)
			{
				return true;
			}
			CharacterObject characterObject = targetHero.CharacterObject;
			return characterObject != null && mainParty.MemberRoster != null && mainParty.MemberRoster.FindIndexOfTroop(characterObject) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsEligibleSceneTauntTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		return IsSceneLordTauntTarget(targetHero) || IsSceneNotableTauntTarget(targetHero) || IsEligibleSceneTauntCharacter(targetCharacter);
	}

	private static bool IsMeetingTauntContext(Hero targetHero)
	{
		if (targetHero == null)
		{
			return false;
		}
		bool flag = false;
		try
		{
			flag = MeetingBattleRuntime.IsMeetingActive || LordEncounterBehavior.IsEncounterMeetingMissionActive;
		}
		catch
		{
			flag = false;
		}
		if (!flag)
		{
			return false;
		}
		try
		{
			Hero hero = MeetingBattleRuntime.TargetHero;
			if (hero != null && hero != targetHero)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	internal static string BuildSceneTauntRuntimeInstructionForExternal(CharacterObject targetCharacter, int targetAgentIndex)
	{
		return BuildSceneTauntRuntimeInstructionForExternal(targetCharacter?.HeroObject, targetCharacter, targetAgentIndex);
	}

	internal static string BuildSceneTauntRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			string text = (MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			if (IsChildSceneProtectedTarget(targetCharacter))
			{
				return "他是未成年。禁止输出[ACTION:SCENE_TAUNT_WARN]或[ACTION:SCENE_TAUNT_FIGHT]；只能害怕、求救或躲开。";
			}
			if (!IsSceneTauntApplicable(targetHero, targetCharacter, targetAgentIndex))
			{
				return "";
			}
			if (SceneTauntMissionBehavior.IsSettlementCriminalConflictTargetExternal(targetHero, targetCharacter))
			{
				return "【AFEF与匪徒冲突规则】你是匪徒/帮派成员，这一类冲突不走普通场景口角的警告-升级流程，也不要输出[ACTION:SCENE_TAUNT_WARN]。" + text + "若只是嘴硬、试探、骂你几句，你可以继续放狠话、威胁、索要买路钱或逼他滚开；只有当你明确决定和他短暂对峙后翻脸开打时，才在句末输出[ACTION:SCENE_TAUNT_FIGHT]。输出后会先维持对话约10秒，然后自动退出对话并进入原版匪徒冲突流程。";
			}
			string sceneTauntTargetKey = BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex);
			bool flag = HasSceneTauntWarning(sceneTauntTargetKey);
			bool flag2 = IsSceneLordTauntTarget(targetHero);
			bool flag3 = IsSoldierSceneTauntTarget(targetCharacter);
			bool flag4 = flag2 || flag3;
			if (flag)
			{
				if (flag4)
				{
					return "你已警告过" + text + "。若还忍不了，就在句末输出[ACTION:SCENE_TAUNT_FIGHT]；输出后会先维持对话约10秒，然后自动退出对话并爆发冲突，场上己方士兵和领主会来帮你砍他。";
				}
				return "你已警告过" + text + "。若还忍不了，就在句末输出[ACTION:SCENE_TAUNT_FIGHT]，短暂对峙后用拳头教训他。输出后会先维持对话约10秒，然后自动退出对话并爆发冲突。";
			}
			if (flag4)
			{
				return "若" + text + "辱骂，贬低或者招惹你你可在句末输出[ACTION:SCENE_TAUNT_WARN]警告他；若忍无可忍，可直接输出[ACTION:SCENE_TAUNT_FIGHT]。输出后会先维持对话约10秒，然后自动退出对话并爆发冲突，场上己方士兵和领主会来帮你砍他。";
			}
			return "若" + text + "辱骂，贬低或者招惹你可在句末输出[ACTION:SCENE_TAUNT_WARN]警告他；若忍无可忍，可直接输出[ACTION:SCENE_TAUNT_FIGHT]，短暂对峙后用拳头教训他。输出后会先维持对话约10秒，然后自动退出对话并爆发冲突。";
		}
		catch
		{
			return "";
		}
	}

	internal static string BuildUnifiedTauntRuntimeInstructionForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			string meetingInstruction = LordEncounterBehavior.BuildMeetingTauntRuntimeInstructionForExternal(targetHero, targetCharacter);
			if (!string.IsNullOrWhiteSpace(meetingInstruction))
			{
				return meetingInstruction;
			}
			return BuildSceneTauntRuntimeInstructionForExternal(targetHero, targetCharacter, targetAgentIndex);
		}
		catch
		{
			return "";
		}
	}

	internal static bool TryProcessSceneTauntAction(CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToFight)
	{
		return TryProcessSceneTauntAction(targetCharacter?.HeroObject, targetCharacter, targetAgentIndex, ref content, out escalatedToFight);
	}

	internal static bool TryProcessSceneTauntAction(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool escalatedToFight)
	{
		escalatedToFight = false;
		try
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}
			bool flag = SceneTauntWarnTagRegex.IsMatch(content);
			bool flag2 = SceneTauntFightTagRegex.IsMatch(content);
			if (!flag && !flag2)
			{
				return false;
			}
			content = SceneTauntWarnTagRegex.Replace(content, "").Trim();
			content = SceneTauntFightTagRegex.Replace(content, "").Trim();
			if (SceneTauntMissionBehavior.IsSettlementCriminalConflictTargetExternal(targetHero, targetCharacter))
			{
				if (flag || flag2)
				{
					escalatedToFight = TryStartSceneTauntFight(targetHero, targetCharacter, targetAgentIndex, BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex));
				}
				return flag || flag2;
			}
			if (!IsEligibleSceneTauntTarget(targetHero, targetCharacter))
			{
				return flag || flag2;
			}
			string sceneTauntTargetKey = BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex);
			if (flag && IsSceneTauntApplicable(targetHero, targetCharacter, targetAgentIndex))
			{
				RememberSceneTauntWarning(sceneTauntTargetKey);
			}
			if (flag2)
			{
				escalatedToFight = TryStartSceneTauntFight(targetHero, targetCharacter, targetAgentIndex, sceneTauntTargetKey);
			}
			return flag || flag2;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Processing scene taunt tag failed: " + ex.Message);
			return false;
		}
	}

	internal static string BuildSceneTauntTargetKey(CharacterObject targetCharacter, int targetAgentIndex)
	{
		return BuildSceneTauntTargetKey(targetCharacter?.HeroObject, targetCharacter, targetAgentIndex);
	}

	internal static bool HasSceneTauntFightTagForExternal(string content)
	{
		try
		{
			return !string.IsNullOrWhiteSpace(content) && SceneTauntFightTagRegex.IsMatch(content);
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryConsumeSceneTauntTagsForDelayedFightExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, ref string content, out bool hadWarnTag, out bool hadFightTag, out string targetKey)
	{
		hadWarnTag = false;
		hadFightTag = false;
		targetKey = "";
		try
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}
			hadWarnTag = SceneTauntWarnTagRegex.IsMatch(content);
			hadFightTag = SceneTauntFightTagRegex.IsMatch(content);
			if (!hadWarnTag && !hadFightTag)
			{
				return false;
			}
			content = SceneTauntWarnTagRegex.Replace(content, "").Trim();
			content = SceneTauntFightTagRegex.Replace(content, "").Trim();
			targetKey = BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex);
			if (hadWarnTag && !string.IsNullOrWhiteSpace(targetKey) && IsSceneTauntApplicable(targetHero, targetCharacter, targetAgentIndex))
			{
				RememberSceneTauntWarning(targetKey);
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Consuming delayed scene taunt tag failed: " + ex.Message);
			return false;
		}
	}

	internal static bool TryStartSceneTauntFightForExternal(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey = null, string reason = null)
	{
		string resolvedTargetKey = string.IsNullOrWhiteSpace(targetKey) ? BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex) : targetKey;
		bool result = TryStartSceneTauntFight(targetHero, targetCharacter, targetAgentIndex, resolvedTargetKey);
		Logger.Log("SceneTaunt", $"Delayed scene taunt fight requested. Started={result}, Reason={reason ?? "N/A"}, Target={targetHero?.Name ?? targetCharacter?.Name}, AgentIndex={targetAgentIndex}, Key={resolvedTargetKey}");
		return result;
	}

	internal static string BuildSceneTauntTargetKey(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			string text = (currentSettlement?.StringId ?? "").Trim().ToLowerInvariant();
			string text2 = (CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant();
			string text3 = (targetHero?.StringId ?? "").Trim().ToLowerInvariant();
			if (IsSceneLordTauntTarget(targetHero) && !string.IsNullOrWhiteSpace(text3))
			{
				return $"scene_lord:{text}:{text2}:{text3}";
			}
			string text4 = (targetCharacter?.StringId ?? "").Trim().ToLowerInvariant();
			string text5 = (targetCharacter?.Name?.ToString() ?? "").Trim().ToLowerInvariant();
			if (RewardSystemBehavior.Instance != null && targetCharacter != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
			{
				return $"merchant:{text}:{kind}:{text4}:{text5}";
			}
			if (targetAgentIndex >= 0)
			{
				return $"scene_agent:{text}:{text2}:{targetAgentIndex}:{text4}";
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				return $"scene_troop:{text}:{text2}:{text4}:{text5}";
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool IsSceneTauntApplicable(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		if (!IsEligibleSceneTauntTarget(targetHero, targetCharacter))
		{
			return false;
		}
		try
		{
			Mission current = Mission.Current;
			SceneTauntMissionBehavior missionBehavior = current?.GetMissionBehavior<SceneTauntMissionBehavior>();
			return missionBehavior != null && missionBehavior.CanStartConflict(targetHero, targetCharacter, targetAgentIndex);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryStartSceneTauntFight(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey)
	{
		try
		{
			if (!IsEligibleSceneTauntTarget(targetHero, targetCharacter))
			{
				return false;
			}
			SceneTauntMissionBehavior missionBehavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
			if (missionBehavior == null || !missionBehavior.CanStartConflict(targetHero, targetCharacter, targetAgentIndex))
			{
				Logger.Log("SceneTaunt", "Fight tag ignored because current scene taunt context is not applicable.");
				return false;
			}
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
			bool flag = missionBehavior.TryStartConflict(targetHero, targetCharacter, targetAgentIndex, targetKey, fromVerbalTaunt: true);
			if (flag)
			{
				Logger.Log("SceneTaunt", $"Scene taunt fight started. Target={targetHero?.Name ?? targetCharacter?.Name}, AgentIndex={targetAgentIndex}, Key={targetKey}");
			}
			return flag;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting scene taunt fight failed: " + ex.Message);
			return false;
		}
	}

	private static bool HasSceneTauntWarning(string targetKey)
	{
		string text = (targetKey ?? "").Trim();
		return !string.IsNullOrWhiteSpace(text) && Instance != null && Instance._warnedSceneTargetKeys != null && Instance._warnedSceneTargetKeys.Contains(text);
	}

	internal static Dictionary<string, string> BuildTauntRuntimeTokens(bool isHeroMeeting, bool isSceneLord = false)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (isHeroMeeting)
		{
			dictionary["tauntContext"] = "hero会面场景";
			dictionary["warnTag"] = "MEETING_TAUNT_BATTLE";
			dictionary["fightTag"] = "MEETING_TAUNT_BATTLE";
			dictionary["fightEffectText"] = "这会把当前会面立刻升级为战斗，并按玩家攻击了你方军队来处理后果。";
		}
		else if (isSceneLord)
		{
			dictionary["tauntContext"] = "非会面场景中的领主互动";
			dictionary["warnTag"] = "SCENE_TAUNT_WARN";
			dictionary["fightTag"] = "SCENE_TAUNT_FIGHT";
			dictionary["fightEffectText"] = "这会在约10秒对峙后自动退出对话，并把当前场景升级为持械冲突；该领主和场上士兵会拿武器围攻玩家；并且会按玩家公开敌对该领主所属势力来处理，必要时会先强制让玩家脱离原势力再宣战。";
		}
		else
		{
			dictionary["tauntContext"] = "普通NPC的场景互动";
			dictionary["warnTag"] = "SCENE_TAUNT_WARN";
			dictionary["fightTag"] = "SCENE_TAUNT_FIGHT";
			dictionary["fightEffectText"] = "这会在约10秒对峙后自动退出对话，并把当前场景升级为冲突。";
		}
		return dictionary;
	}

	private static void RememberSceneTauntWarning(string targetKey)
	{
		string text = (targetKey ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (Instance == null)
		{
			return;
		}
		if (Instance._warnedSceneTargetKeys == null)
		{
			Instance._warnedSceneTargetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		if (!Instance._warnedSceneTargetKeys.Add(text))
		{
			return;
		}
		Logger.Log("SceneTaunt", $"Recorded warning state for target={text}");
	}

	internal static string BuildFrightenedCivilianShoutExtraFactExternal(Agent targetAgent)
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>()?.BuildFrightenedCivilianShoutExtraFact(targetAgent) ?? "";
		}
		catch
		{
			return "";
		}
	}
}

public class SceneTauntMissionBehavior : MissionBehavior
{
	internal const string WantedSceneExitNotice = "你现在正在被通缉，无法离开当前场景！请立刻跑出地图边缘红区，摆脱追击。";

	private static readonly FieldInfo PlayerSideOldTeamDataField = AccessTools.Field(typeof(MissionFightHandler), "_playerSideAgentsOldTeamData");

	private static readonly FieldInfo OpponentSideOldTeamDataField = AccessTools.Field(typeof(MissionFightHandler), "_opponentSideAgentsOldTeamData");

	private static readonly FieldInfo OpponentSideAgentsField = AccessTools.Field(typeof(MissionFightHandler), "_opponentSideAgents");

	private static readonly FieldInfo FinishTimerField = AccessTools.Field(typeof(MissionFightHandler), "_finishTimer");
	private static readonly FieldInfo NativeAlleyFightPositionField = AccessTools.Field(typeof(MissionAlleyHandler), "_fightPosition");

	private const float SceneTauntNativeFightAutoEndDelaySeconds = 3600f;

	private const float ArmedBystanderReactionRefreshIntervalSeconds = 0.5f;

	private const float ArmedBystanderReactionRadiusMeters = 20f;

	private const int ArmedConflictReactionMaxCount = 8;

	private const float ArmedConflictSecondReactionMinDelaySeconds = 6f;

	private const float ArmedConflictSecondReactionMaxDelaySeconds = 8f;

	private const float ArmedConflictSustainedReactionMinDelaySeconds = 13f;

	private const float ArmedConflictSustainedReactionMaxDelaySeconds = 16f;

	private const float ArmedConflictProtractedThresholdSeconds = 60f;

	private const float ArmedConflictProtractedReactionMinDelaySeconds = 20f;

	private const float ArmedConflictProtractedReactionMaxDelaySeconds = 24f;

	private const float ArmedConflictReactionRetryDelaySeconds = 1.5f;

	private const float ArmedConflictReactionCandidateRefreshIntervalSeconds = 0.75f;

	private const float ArmedConflictReactionPerAgentCooldownSeconds = 30f;

	private const double SceneTauntPerfStageThresholdMs = 4.0;

	private const double SceneTauntPerfHeavyStageThresholdMs = 10.0;

	private const double SceneTauntPerfTickThresholdMs = 12.0;

	private const string FallbackSoldierWeaponId = "iron_spatha_sword_t2";

	private const float SceneTauntCrimeCapBeforeWar = 59f;

	private const float SceneTauntInitialArmedCrimeAmount = 35f;

	private const float SceneTauntPerKnockdownCrimeAmount = 20f;

	private const int SceneTauntPerKnockdownTrustPenalty = 20;

	private const int OwnedSettlementPassiveAttackLoyaltyPenalty = 20;

	private const float OwnedSettlementPassiveAttackReactionCooldownSeconds = 4f;

	private const float OwnedSettlementPassiveHandsUpPoseRefreshInterval = 0.35f;

	private const float OwnedSettlementPassiveHandsUpPoseStartProgress = 0.35f;

	private const float OwnedSettlementPassiveHandsUpPoseActionSpeed = 0f;

	private const float SceneGoldPickupDistanceSquared = 4f;

	private const int SceneGoldMaxVisualCoins = 1000;

	private const int SceneGoldCoinDenarValue = 10;

	private const int SceneGoldIngotDenarValue = 1000;

	private const float SceneGoldSimulatedGravity = 9.8f;

	private const float SceneGoldGroundOffset = 0.035f;

	private const double SceneGoldTavernSettlementPoolRatio = 0.1;

	private const double SceneGoldLordHallSettlementPoolRatio = 0.3;

	private const string SceneGoldCustomItemId = "animusforge_denar_coin_item";

	private const string SceneGoldCustomIngotItemId = "animusforge_denar_ingot_item";

	private const string SceneGoldFallbackNativeItemId = "sling_leadammo";

	private const string SceneGoldCustomVisualPrefab = "animusforge_denar_coin";

	private const string SceneGoldPrimarySoundEvent = "event:/ui/notification/coins_positive";

	private const string SceneGoldFallbackSoundEvent = "event:/ui/multiplayer/coin_add";

	private const float SceneGoldNativeCoinVisualScale = 1.4f;

	private const float SceneGoldNativeIngotVisualScale = 1.15f;

	private const float SceneGoldBurstSpawnHeight = 1.08f;

	private const float SceneGoldBurstSpawnRadius = 0.16f;

	private const float SceneGoldBurstHorizontalVelocityMin = 3.2f;

	private const float SceneGoldBurstHorizontalVelocityMax = 6.2f;

	private const float SceneGoldBurstVerticalVelocityMin = 1.6f;

	private const float SceneGoldBurstVerticalVelocityMax = 3.4f;

	private static readonly uint SceneGoldTintColor = new Color(1f, 0.72f, 0.08f, 1f).ToUnsignedInteger();

	private enum SceneGoldSettlementPoolLocation
	{
		None,
		Tavern,
		LordHall
	}

	private sealed class SceneGoldDrop
	{
		public int AgentIndex;

		public Vec3 Position;

		public GameEntity Entity;

		public List<GameEntity> Entities;

		public List<SceneGoldCoinSim> SimulatedCoins;

		public Hero SourceHero;

		public Settlement SourceSettlement;

		public int FixedSettlementGold;

		public bool UsesLocationSettlementPool;

		public int VisualGoldAmount;

		public bool IsHeroDrop;

		public bool UsesNativeItemPhysics;
	}

	private sealed class SceneGoldCoinSim
	{
		public GameEntity Entity;

		public MatrixFrame Frame;

		public Vec3 Velocity;

		public Vec3 AngularVelocity;

		public bool UsesNativeCoinScale;

		public bool UsesNativeIngotScale;

		public bool Settled;
	}

	private sealed class SceneGoldVisualPiece
	{
		public ItemObject Item;

		public int DenarValue;

		public bool UsesNativeCoinScale;

		public bool UsesNativeIngotScale;
	}

	private sealed class PlayerSceneConflictMajorMaterialDraft
	{
		public bool HasAnyAction;

		public int Day = -1;

		public string GameDate = "";

		public string SettlementId = "";

		public string SettlementName = "";

		public string LocationText = "";

		public string ActorCultureId = "";

		public string TargetCultureId = "";

		public string SettlementCultureId = "";

		public int DamageCount;

		public int UnconsciousCount;

		public int KilledCount;

		public float CrimeAmount;

		public bool HadCriminalTarget;

		public bool HadAuthorityTarget;

		public bool HadOwnedSettlementPassiveAttack;

		public readonly HashSet<string> VictimKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public readonly List<string> VictimNames = new List<string>();
	}

	private MissionFightHandler _fightHandler;

	private readonly HashSet<int> _playerAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _opponentAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _guardAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _blockedAiWeaponAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _armedBystanderWatcherIndices = new HashSet<int>();

	private readonly List<Agent> _armedConflictReactionCandidates = new List<Agent>();

	private readonly Dictionary<int, float> _armedConflictReactionLastStartedByAgentIndex = new Dictionary<int, float>();

	private int _armedConflictReactionCount;

	private float _lastArmedConflictReactionMissionTime = -1f;

	private float _armedConflictReactionStartedAtMissionTime = -1f;

	private float _nextArmedConflictReactionMissionTime = -1f;

	private float _lastArmedConflictReactionCandidateRefreshAtMissionTime = -1f;

	private bool _armedConflictReactionRequestPending;

	private int _armedConflictReactionPendingAgentIndex = -1;

	private float _armedConflictReactionPendingStartedAtMissionTime = -1f;

	private readonly HashSet<int> _penalizedArmedKnockdownAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _ownedSettlementPassiveVictimAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _ownedSettlementPassiveDamagedAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _ownedSettlementPassiveKnockdownAgentIndices = new HashSet<int>();

	private readonly Dictionary<int, float> _ownedSettlementPassiveReactionTimes = new Dictionary<int, float>();

	private readonly HashSet<int> _ownedSettlementPassiveHandsUpPoseAppliedAgentIndices = new HashSet<int>();

	private float _ownedSettlementPassiveHandsUpPoseRefreshTimer;

	private bool _ownedSettlementPassiveHandsUpActionMissingLogged;

	private bool _ownedSettlementPassiveHandsUpActionRejectedLogged;

	private readonly HashSet<string> _recordedPlayerSceneConflictActionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private int _playerSceneConflictActionSequence;

	private readonly PlayerSceneConflictMajorMaterialDraft _playerSceneConflictMajorMaterialDraft = new PlayerSceneConflictMajorMaterialDraft();

	private readonly Dictionary<int, Team> _ownedSettlementPassiveOriginalTeams = new Dictionary<int, Team>();

	private Team _ownedSettlementPassivePlayerTeam;

	private Team _ownedSettlementPassiveEnemyTeam;

	private Team _ownedSettlementPassiveOriginalMainTeam;

	private readonly HashSet<int> _sceneGoldDropAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _sceneGoldEligibleAgentIndices = new HashSet<int>();

	private readonly List<SceneGoldDrop> _sceneGoldDrops = new List<SceneGoldDrop>();

	private readonly Dictionary<int, int> _sceneGoldSettlementShareByAgentIndex = new Dictionary<int, int>();

	private bool _sceneGoldShareSnapshotCaptured;

	private string _sceneGoldShareSnapshotSettlementId = "";

	private SceneGoldSettlementPoolLocation _sceneGoldShareSnapshotLocation;

	private int _sceneGoldMotionDiagTicks;

	private readonly Dictionary<int, MissionEquipment> _cachedUnarmedConflictEquipment = new Dictionary<int, MissionEquipment>();

	private readonly Dictionary<Hero, bool> _sceneNotableRecentHitNonLethal = new Dictionary<Hero, bool>();

	private readonly HashSet<Hero> _sceneNotableDeferredBattleDeathCandidates = new HashSet<Hero>();

	private bool _conflictActive;

	private bool _armedConflict;

	private float _nextSetsFollowerArmedReadinessMissionTime;

	private bool _sceneAttackReleaseSuppressed;

	private bool _playerAttackReleasePrimed;

	private Agent.ActionStage? _lastMainAgentAttackStage;

	private bool _pendingImmediateUnarmedFightEnd;

	private bool _pendingImmediateUnarmedFightEndPlayerWon;

	private bool _armedCarryoverSceneInitialized;

	private bool _armedCarryoverNoAuthoritySceneNotified;

	private bool _armedCarryoverHandledInThisMission;

	private float _lastArmedCarryoverAttemptAtMissionTime = -1f;

	private float _lastArmedBystanderReactionRefreshAtMissionTime = -1f;

	private bool _pendingPlayerUnarmedPrep;

	private float _pendingPlayerUnarmedPrepAtMissionTime = -1f;

	private bool _pendingPlayerRearmAfterArmedConflictEnd;

	private float _pendingPlayerRearmAfterArmedConflictEndAtMissionTime = -1f;

	private bool _pendingActiveUnarmedTargetFlee;

	private int _pendingActiveUnarmedTargetFleeAgentIndex = -1;

	private float _pendingActiveUnarmedTargetFleeAtMissionTime = -1f;

	private float _lastArmedEscalationAtMissionTime = -1f;

	private readonly Dictionary<int, float> _recentNeutralizedFleeingCivilianUntilMissionTime = new Dictionary<int, float>();

	private bool _armedConflictOccurredThisConflict;

	private bool _armedDefeatOutcomeHandled;

	private bool _baseConsequencesApplied;

	private bool _pendingPlayerBattleDeathAfterMission;

	private bool _pendingPlayerBattleDeathDecisionCaptured;

	private Hero _pendingPlayerBattleDeathKiller;

	private float _appliedCrimeRatingAmount;

	private bool _armedDefeatWasCriminalConflict;

	private string _activeTargetKey = "";

	private string _activeTargetName = "";

	private int _activeTargetAgentIndex = -1;

	private bool _openedAsUnarmedBrawl;

	private bool _openedFromVerbalTaunt;

	private bool _suppressSettlementConsequencesForCurrentConflict;

	private int _lastNativeCriminalConflictTargetAgentIndex = -1;

	private float _lastNativeCriminalConflictMissionTime = -999f;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	private static long StartPerfTimer()
	{
		try
		{
			return Stopwatch.GetTimestamp();
		}
		catch
		{
			return 0L;
		}
	}

	private static double GetElapsedPerfMs(long startTimestamp)
	{
		try
		{
			if (startTimestamp <= 0L)
			{
				return 0.0;
			}
			return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
		}
		catch
		{
			return 0.0;
		}
	}

	private void LogPerfPoint(string stage, string details = null)
	{
		try
		{
			if (!Logger.IsVerboseModLogicEnabled)
			{
				return;
			}
			Logger.LogVerbose("SceneTauntPerf", "perf_point:" + (stage ?? ""), () => $"{stage} {BuildPerfContext()}{FormatPerfDetails(details)}", 2.0);
		}
		catch
		{
		}
	}

	private void LogPerfElapsed(string stage, long startTimestamp, string details = null, double thresholdMs = SceneTauntPerfStageThresholdMs)
	{
		try
		{
			double elapsedPerfMs = GetElapsedPerfMs(startTimestamp);
			if (elapsedPerfMs < thresholdMs)
			{
				return;
			}
			if (!Logger.IsVerboseModLogicEnabled)
			{
				return;
			}
			Logger.LogVerbose("SceneTauntPerf", "perf_elapsed:" + (stage ?? ""), () => $"{stage} elapsedMs={elapsedPerfMs:0.###} {BuildPerfContext()}{FormatPerfDetails(details)}", 2.0);
		}
		catch
		{
		}
	}

	private string BuildPerfContext()
	{
		int totalAgents = 0;
		int activeHumanAgents = 0;
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents != null)
			{
				foreach (Agent agent in agents)
				{
					totalAgents++;
					if (agent != null && agent.IsHuman && agent.IsActive())
					{
						activeHumanAgents++;
					}
				}
			}
		}
		catch
		{
		}
		string text = "";
		string text2 = "";
		float num = -1f;
		try
		{
			num = Mission.Current?.CurrentTime ?? -1f;
		}
		catch
		{
		}
		try
		{
			text = (CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant();
		}
		catch
		{
			text = "";
		}
		try
		{
			text2 = Settlement.CurrentSettlement?.StringId ?? "";
		}
		catch
		{
			text2 = "";
		}
		return $"t={num:0.###} loc={text} settlement={text2} agents={totalAgents} activeHumans={activeHumanAgents} conflict={_conflictActive} armed={_armedConflict} player={_playerAgentIndices.Count} opponents={_opponentAgentIndices.Count} guards={_guardAgentIndices.Count} blockedWield={_blockedAiWeaponAgentIndices.Count} armedWatchers={_armedBystanderWatcherIndices.Count} passiveVictims={_ownedSettlementPassiveVictimAgentIndices.Count} goldDrops={_sceneGoldDrops.Count}";
	}

	private static string FormatPerfDetails(string details)
	{
		return string.IsNullOrWhiteSpace(details) ? "" : (" " + details.Trim());
	}

	internal bool IsConflictActive => _conflictActive;

	internal bool ShouldSuppressNativeMissionConversation()
	{
		return _conflictActive || SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal string BuildFrightenedCivilianShoutExtraFact(Agent targetAgent)
	{
		try
		{
			if (targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return "";
			}
			if ((!_conflictActive || !_armedConflict) && !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
			{
				return "";
			}
			if (_playerAgentIndices.Contains(targetAgent.Index))
			{
				return "";
			}
			if (!ShouldFleeWhenArmedVictim(targetAgent))
			{
				return "";
			}
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			return "[AFEF玩家行为补充] " + text + "在定居点内乱砍人，你被吓的半死。";
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Building frightened civilian shout extra fact failed: " + ex.Message);
			return "";
		}
	}

	public override void OnBehaviorInitialize()
	{
		_fightHandler = Mission.Current?.GetMissionBehavior<MissionFightHandler>();
	}

	public override void OnMissionTick(float dt)
	{
		using PerfProbe.ScopeToken perfScope = PerfProbe.Scope("Mission.SceneTauntMissionBehavior.OnMissionTick.total");
		long tickStart = StartPerfTimer();
		long sectionStart = StartPerfTimer();
		TryActivateSettlementArmedCarryover();
		LogPerfElapsed("tick.TryActivateSettlementArmedCarryover", sectionStart, $"dt={dt:0.####}");
		sectionStart = StartPerfTimer();
		TryMaintainOwnedSettlementPassiveAttackVictims(dt);
		LogPerfElapsed("tick.TryMaintainOwnedSettlementPassiveAttackVictims", sectionStart, $"dt={dt:0.####}");
		TryResolveCompletedUnarmedConflictBeforeEscalation();
		TryCommitPendingImmediateUnarmedFightEnd();
		sectionStart = StartPerfTimer();
		TryMaintainSceneGoldCoinMotion(dt);
		LogPerfElapsed("tick.TryMaintainSceneGoldCoinMotion", sectionStart, $"dt={dt:0.####}");
		sectionStart = StartPerfTimer();
		TryHandleSceneGoldPickupInput();
		LogPerfElapsed("tick.TryHandleSceneGoldPickupInput", sectionStart, $"dt={dt:0.####}");
		TryCommitPendingPlayerUnarmedPrep();
		TryCommitPendingPlayerRearmAfterArmedConflictEnd();
		TryCommitPendingActiveUnarmedTargetFlee();
		TryForceActiveUnarmedTargetFleeFallback();
		sectionStart = StartPerfTimer();
		TryMaintainRecentlyNeutralizedFleeingCivilians();
		LogPerfElapsed("tick.TryMaintainRecentlyNeutralizedFleeingCivilians", sectionStart, $"dt={dt:0.####}");
		sectionStart = StartPerfTimer();
		TryMaintainHostileUnarmedOpponentsFleeing();
		LogPerfElapsed("tick.TryMaintainHostileUnarmedOpponentsFleeing", sectionStart, $"dt={dt:0.####}");
		TryMaintainMainAgentArmedPresence();
		TryMaintainSetsSelectedFollowerArmedReadiness();
		sectionStart = StartPerfTimer();
		TryMaintainArmedBystanderReactions();
		LogPerfElapsed("tick.TryMaintainArmedBystanderReactions", sectionStart, $"dt={dt:0.####}");
		sectionStart = StartPerfTimer();
		TryAppendNearbyArmedEscalationBehaviorFacts();
		LogPerfElapsed("tick.TryAppendNearbyArmedEscalationBehaviorFacts", sectionStart, $"dt={dt:0.####}");
		if (IsPlayerInteractionInputSuppressed())
		{
			_sceneAttackReleaseSuppressed = false;
			_playerAttackReleasePrimed = false;
		}
		else if (Input.IsKeyDown(InputKey.LeftMouseButton) && Input.IsKeyDown(InputKey.RightMouseButton))
		{
			_sceneAttackReleaseSuppressed = true;
		}
		if (!IsPlayerInteractionInputSuppressed() && ShouldTriggerPlayerAttackRelease())
		{
			Logger.LogVerbose("SceneTaunt", "attack_timing_release", () => $"[AttackTiming] release_triggered time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} weapon={IsAgentUsingRealWeapon(Agent.Main)} conflict={_conflictActive} armed={_armedConflict}", 1.0);
			if (!_sceneAttackReleaseSuppressed)
			{
				if (!_conflictActive)
				{
					TryStartConflictFromFacingAttackInput();
				}
				else if (!_armedConflict)
				{
					TryHandleFacingAttackDuringUnarmedConflict();
				}
				else if (_armedConflict)
				{
					TryTauntFacingAgentDuringArmedConflict();
				}
			}
			_sceneAttackReleaseSuppressed = false;
		}
		if (_conflictActive && !_armedConflict && IsPlayerAttemptingWeaponDrawInput(Agent.Main))
		{
			EscalateToArmedConflict("player_requested_weapon_draw");
		}
		if (_conflictActive && !_armedConflict && !IsMainAgentSeated() && IsAgentUsingRealWeapon(Agent.Main))
		{
			EscalateToArmedConflict("player_drew_weapon");
		}
		UpdateMainAgentAttackReleaseTracking();
		LogPerfElapsed("tick.total", tickStart, $"dt={dt:0.####}", SceneTauntPerfTickThresholdMs);
	}

	private void UpdateMainAgentAttackReleaseTracking()
	{
		_lastMainAgentAttackStage = GetMainAgentAttackStage();
	}

	private bool ShouldTriggerPlayerAttackRelease()
	{
		Agent.ActionStage? mainAgentAttackStage = GetMainAgentAttackStage();
		if (mainAgentAttackStage == Agent.ActionStage.AttackReady || mainAgentAttackStage == Agent.ActionStage.AttackQuickReady)
		{
			_playerAttackReleasePrimed = true;
			return false;
		}
		bool flag = mainAgentAttackStage == Agent.ActionStage.AttackRelease && _lastMainAgentAttackStage != Agent.ActionStage.AttackRelease;
		if (flag && (_playerAttackReleasePrimed || IsAgentUsingRealWeapon(Agent.Main)))
		{
			_playerAttackReleasePrimed = false;
			return true;
		}
		if (mainAgentAttackStage != Agent.ActionStage.AttackRelease && mainAgentAttackStage != Agent.ActionStage.AttackReady && mainAgentAttackStage != Agent.ActionStage.AttackQuickReady && !Input.IsKeyDown(InputKey.LeftMouseButton))
		{
			_playerAttackReleasePrimed = false;
		}
		return false;
	}

	private static Agent.ActionStage? GetMainAgentAttackStage()
	{
		try
		{
			if (Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			return Agent.Main.GetCurrentActionStage(1);
		}
		catch
		{
			return null;
		}
	}

	private static bool IsPlayerInteractionInputSuppressed()
	{
		return IsBoardGameInteractionActive() || IsMainAgentSeated() || ShoutBehavior.IsSceneShoutInputActiveForExternal();
	}

	private static bool IsBoardGameInteractionActive()
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<MissionBoardGameLogic>()?.IsGameInProgress ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMainAgentSeated()
	{
		try
		{
			return Agent.Main != null && Agent.Main.IsActive() && Agent.Main.IsSitting();
		}
		catch
		{
			return false;
		}
	}

	private void TryResolveCompletedUnarmedConflictBeforeEscalation()
	{
		try
		{
			if (!_conflictActive || _armedConflict || _fightHandler == null || !_fightHandler.IsThereActiveFight())
			{
				return;
			}
			if (IsIndexedSideDefeated(_opponentAgentIndices))
			{
				_pendingImmediateUnarmedFightEnd = false;
				ClearMissionFightHandlerPendingFinishTimer();
				_fightHandler.EndFight(overrideDuelWonByPlayer: true);
				return;
			}
			if (IsIndexedSideDefeated(_playerAgentIndices))
			{
				_pendingImmediateUnarmedFightEnd = false;
				ClearMissionFightHandlerPendingFinishTimer();
				_fightHandler.EndFight(overrideDuelWonByPlayer: false);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Resolving completed unarmed conflict before escalation failed: " + ex.Message);
		}
	}

	private void TryStartConflictFromFacingAttackInput()
	{
		try
		{
			if (_conflictActive || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false)
			{
				return;
			}
			List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
			Agent facingAgent = FindFacingCriminalAttackTarget(nearbyNPCAgents) ?? FindFacingPhysicalAttackTarget() ?? FindClosestEligiblePhysicalAttackTarget();
			Logger.LogVerbose("SceneTaunt", "attack_timing_facing_scan", () => $"[AttackTiming] facing_attack_scan time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} nearbyCount={(nearbyNPCAgents != null ? nearbyNPCAgents.Count : 0)} target={(facingAgent?.Name?.ToString() ?? "null")} targetIndex={(facingAgent != null ? facingAgent.Index : -1)}", 1.0);
			if (facingAgent == null || !facingAgent.IsActive())
			{
				return;
			}
			// In town/village peace scenes, player attacks only deal real damage after the target
			// has been moved onto a hostile team. Ordinary owned-settlement NPCs use the passive
			// preparation path here; criminal/alley NPCs must fall through to the original conflict
			// path below, because that path performs its own native/custom team conversion.
			if (IsOwnedSettlementPassiveAttackScene() && IsValidOwnedSettlementPassiveAttackTarget(facingAgent))
			{
				PrepareOwnedSettlementPassiveAttackTargetForDamage(facingAgent, "player_attack_release_targeting");
				return;
			}
			TryStartConflictFromPhysicalAttack(facingAgent, IsAgentUsingRealWeapon(Agent.Main), "player_attack_release_targeting");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting conflict from facing attack input failed: " + ex.Message);
		}
	}

	private static Agent FindFacingPhysicalAttackTarget()
	{
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = -1f;
			foreach (Agent agent in agents)
			{
				if (agent == null || agent == Agent.Main || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				CharacterObject characterObject = agent.Character as CharacterObject;
				Hero hero = characterObject?.HeroObject;
				if (!IsEligiblePhysicalAttackTarget(hero, characterObject) || SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))
				{
					continue;
				}
				Vec3 v = agent.Position - position;
				float length = v.Length;
				if (length > 4.5f)
				{
					continue;
				}
				v.Normalize();
				float num2 = Vec3.DotProduct(lookDirection, v);
				if (num2 < 0.55f)
				{
					continue;
				}
				float num3 = num2 / Math.Max(0.35f, length);
				if (num3 > num)
				{
					num = num3;
					result = agent;
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static Agent FindClosestEligiblePhysicalAttackTarget()
	{
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = float.MaxValue;
			foreach (Agent agent in agents)
			{
				if (agent == null || agent == Agent.Main || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				CharacterObject characterObject = agent.Character as CharacterObject;
				Hero hero = characterObject?.HeroObject;
				if (!IsEligiblePhysicalAttackTarget(hero, characterObject) || SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))
				{
					continue;
				}
				Vec3 v = agent.Position - position;
				float length = v.Length;
				if (length > 2.2f)
				{
					continue;
				}
				v.Normalize();
				if (Vec3.DotProduct(lookDirection, v) < 0.2f)
				{
					continue;
				}
				if (length < num)
				{
					num = length;
					result = agent;
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private static Agent FindFacingCriminalAttackTarget(List<Agent> nearbyAgents)
	{
		try
		{
			if (Agent.Main == null || nearbyAgents == null || nearbyAgents.Count == 0)
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = -1f;
			foreach (Agent nearbyAgent in nearbyAgents)
			{
				if (nearbyAgent == null || !nearbyAgent.IsHuman || !nearbyAgent.IsActive())
				{
					continue;
				}
				CharacterObject characterObject = nearbyAgent.Character as CharacterObject;
				if (!IsSettlementCriminalConflictTarget(characterObject?.HeroObject, characterObject))
				{
					continue;
				}
				Vec3 v = nearbyAgent.Position - position;
				float length = v.Length;
				if (length > 3.2f)
				{
					continue;
				}
				v.Normalize();
				float num2 = Vec3.DotProduct(lookDirection, v);
				if (num2 < 0.9f)
				{
					continue;
				}
				float num3 = num2 / Math.Max(0.25f, length);
				if (num3 > num)
				{
					num = num3;
					result = nearbyAgent;
				}
			}
			return result;
		}
		catch
		{
			return null;
		}
	}

	private void TryTauntFacingAgentDuringArmedConflict()
	{
		try
		{
			if (!_conflictActive || !_armedConflict || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false)
			{
				return;
			}
			List<Agent> nearbyNPCAgents = ShoutUtils.GetNearbyNPCAgents();
			if (nearbyNPCAgents == null || nearbyNPCAgents.Count == 0)
			{
				return;
			}
			Agent facingAgent = ShoutUtils.GetFacingAgent(nearbyNPCAgents);
			if (facingAgent == null || !facingAgent.IsActive())
			{
				return;
			}
			TryAddFacingAgentToArmedConflict(facingAgent, "player_attack_release_targeting_existing_armed_conflict");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Taunting facing agent during armed conflict failed: " + ex.Message);
		}
	}

	private void TryHandleFacingAttackDuringUnarmedConflict()
	{
		try
		{
			if (!_conflictActive || _armedConflict || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false)
			{
				return;
			}
			// During an unarmed brawl, only actual hits should pull additional civilians into the conflict.
			// Mere facing/attack release was causing nearby townsfolk to get swept into the brawl unexpectedly.
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling facing attack during unarmed conflict failed: " + ex.Message);
		}
	}

	private bool IsOwnedSettlementPassiveAttackScene()
	{
		try
		{
			Settlement settlement = GetCurrentSettlementForOwnedSettlementPassiveAttack();
			if (settlement == null || !SettlementEntryTroopSelectionBehavior.IsPlayerAuthoritySettlementForExternal(settlement))
			{
				return false;
			}
			if (!SceneTauntBehavior.IsPeaceSceneConflictEnabled())
			{
				return false;
			}
			if (IsOwnedSettlementPassiveAttackActive())
			{
				return true;
			}
			return IsOwnedSettlementPassiveAttackPeaceLocationScene(settlement);
		}
		catch
		{
			return false;
		}
	}

	private bool IsOwnedSettlementPassiveAttackActive()
	{
		return _ownedSettlementPassiveVictimAgentIndices.Count > 0
			|| _ownedSettlementPassiveDamagedAgentIndices.Count > 0
			|| _ownedSettlementPassiveKnockdownAgentIndices.Count > 0
			|| _ownedSettlementPassivePlayerTeam != null
			|| _ownedSettlementPassiveEnemyTeam != null;
	}

	private bool IsOwnedSettlementPassiveAttackPeaceLocationScene(Settlement settlement)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || settlement == null)
			{
				return false;
			}
			if (IsCampaignBattleContextForOwnedSettlementPassiveAttack(mission, settlement))
			{
				return false;
			}
			if (PlayerEncounter.LocationEncounter == null || CampaignMission.Current?.Location == null)
			{
				return false;
			}
			Settlement encounterSettlement = PlayerEncounter.LocationEncounter.Settlement;
			if (encounterSettlement != null && encounterSettlement != settlement)
			{
				return false;
			}
			string locationId = (CampaignMission.Current.Location.StringId ?? "").Trim().ToLowerInvariant();
			if (locationId == "arena" || locationId == "training_field")
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool IsCampaignBattleContextForOwnedSettlementPassiveAttack(Mission mission, Settlement settlement)
	{
		try
		{
			if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null || MapEvent.PlayerMapEvent != null)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			CampaignSiegeStateHandler siegeStateHandler = mission?.GetMissionBehavior<CampaignSiegeStateHandler>();
			if (siegeStateHandler != null)
			{
				return true;
			}
			if (mission != null && (mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege || mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.SallyOut || mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle))
			{
				return true;
			}
			if (mission != null && (mission.Mode == MissionMode.Deployment || mission.Mode == MissionMode.Stealth || mission.Mode == MissionMode.Duel))
			{
				return true;
			}
			if (settlement?.IsUnderSiege ?? false)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static Settlement GetCurrentSettlementForOwnedSettlementPassiveAttack()
	{
		try
		{
			return Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveLoyaltySettlementForOwnedSettlementPassiveAttack(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return null;
			}
			if (settlement.Town != null)
			{
				return settlement;
			}
			if (settlement.IsVillage && settlement.Village?.Bound?.Town != null)
			{
				return settlement.Village.Bound;
			}
		}
		catch
		{
		}
		return null;
	}

	private bool IsValidOwnedSettlementPassiveAttackTarget(Agent targetAgent)
	{
		if (targetAgent == null || targetAgent == Agent.Main || !targetAgent.IsHuman || !targetAgent.IsActive())
		{
			return false;
		}
		if (IsSetsSelectedEntryFollower(targetAgent))
		{
			return false;
		}
		if (IsPlayerProtectedSceneAttackAgent(targetAgent))
		{
			return false;
		}
		return ResolveOwnedSettlementAttackRouting(targetAgent) == SetsOwnedSettlementAttackRouting.PassiveSurrender;
	}

	private void PrimeOwnedSettlementPassiveAttackTarget(Agent targetAgent, string reason)
	{
		try
		{
			if (!IsOwnedSettlementPassiveAttackScene() || !IsValidOwnedSettlementPassiveAttackTarget(targetAgent))
			{
				return;
			}
			_ownedSettlementPassiveVictimAgentIndices.Add(targetAgent.Index);
			RegisterSceneGoldEligibleAgent(targetAgent, "owned_settlement_passive_attack_target");
			Logger.Log("SceneTaunt", $"Owned settlement passive attack target tracked. Reason={reason}, Target={targetAgent.Name}, AgentIndex={targetAgent.Index}, Settlement={GetCurrentSettlementForOwnedSettlementPassiveAttack()?.StringId}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Priming owned settlement passive attack target failed: " + ex.Message);
		}
	}

	private void PrepareOwnedSettlementPassiveAttackTargetForDamage(Agent targetAgent, string reason)
	{
		try
		{
			if (!IsOwnedSettlementPassiveAttackScene() || !IsValidOwnedSettlementPassiveAttackTarget(targetAgent))
			{
				return;
			}
			PrimeOwnedSettlementPassiveAttackTarget(targetAgent, reason);
			TryForceAgentMortal(targetAgent);
			EnsureOwnedSettlementPassiveAttackHostility(targetAgent, reason);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Preparing owned settlement passive attack target failed: " + ex.Message);
		}
	}

	private void EnsureOwnedSettlementPassiveAttackHostility(Agent targetAgent, string reason)
	{
		try
		{
			Mission current = Mission.Current;
			Agent main = Agent.Main;
			if (current == null || main == null || !main.IsActive() || targetAgent == null || !targetAgent.IsActive())
			{
				return;
			}
			if (_ownedSettlementPassiveOriginalMainTeam == null)
			{
				_ownedSettlementPassiveOriginalMainTeam = main.Team;
			}
			if (!_ownedSettlementPassiveOriginalTeams.ContainsKey(targetAgent.Index))
			{
				_ownedSettlementPassiveOriginalTeams[targetAgent.Index] = targetAgent.Team;
			}
			if (_ownedSettlementPassivePlayerTeam == null)
			{
				_ownedSettlementPassivePlayerTeam = main.Team ?? current.PlayerTeam;
				if (_ownedSettlementPassivePlayerTeam == null)
				{
					uint color = Hero.MainHero?.MapFaction?.Color ?? 4278190335u;
					uint color2 = Hero.MainHero?.MapFaction?.Color2 ?? 4278190208u;
					Banner banner = Hero.MainHero?.Clan?.Banner;
					_ownedSettlementPassivePlayerTeam = current.Teams.Add(BattleSideEnum.Attacker, color, color2, banner, isPlayerGeneral: true, isPlayerSergeant: false);
				}
			}
			if (_ownedSettlementPassiveEnemyTeam == null)
			{
				uint color3 = targetAgent.Team?.Color ?? 4294901760u;
				uint color4 = targetAgent.Team?.Color2 ?? 4286578688u;
				_ownedSettlementPassiveEnemyTeam = current.Teams.Add(BattleSideEnum.Defender, color3, color4, null, isPlayerGeneral: false, isPlayerSergeant: true);
			}
			if (_ownedSettlementPassivePlayerTeam == null || _ownedSettlementPassiveEnemyTeam == null || _ownedSettlementPassivePlayerTeam == _ownedSettlementPassiveEnemyTeam)
			{
				return;
			}
			try
			{
				current.PlayerTeam = _ownedSettlementPassivePlayerTeam;
			}
			catch
			{
			}
			if (main.Team != _ownedSettlementPassivePlayerTeam)
			{
				main.SetTeam(_ownedSettlementPassivePlayerTeam, sync: true);
			}
			try
			{
				Agent mountAgent = main.MountAgent;
				if (mountAgent != null && mountAgent.IsActive() && mountAgent.Team != _ownedSettlementPassivePlayerTeam)
				{
					mountAgent.SetTeam(_ownedSettlementPassivePlayerTeam, sync: true);
				}
			}
			catch
			{
			}
			if (targetAgent.Team != _ownedSettlementPassiveEnemyTeam)
			{
				targetAgent.SetTeam(_ownedSettlementPassiveEnemyTeam, sync: true);
			}
			try
			{
				Agent mountAgent2 = targetAgent.MountAgent;
				if (mountAgent2 != null && mountAgent2.IsActive() && mountAgent2.Team != _ownedSettlementPassiveEnemyTeam)
				{
					mountAgent2.SetTeam(_ownedSettlementPassiveEnemyTeam, sync: true);
				}
			}
			catch
			{
			}
			_ownedSettlementPassivePlayerTeam.SetIsEnemyOf(_ownedSettlementPassiveEnemyTeam, isEnemyOf: true);
			_ownedSettlementPassiveEnemyTeam.SetIsEnemyOf(_ownedSettlementPassivePlayerTeam, isEnemyOf: true);
			TryApplyOwnedSettlementPassiveHandsUpPose(targetAgent);
			try
			{
				if (targetAgent.IsAIControlled)
				{
					targetAgent.ResetEnemyCaches();
					targetAgent.InvalidateTargetAgent();
					targetAgent.InvalidateAIWeaponSelections();
				}
				if (main.IsAIControlled)
				{
					main.ResetEnemyCaches();
					main.InvalidateTargetAgent();
					main.InvalidateAIWeaponSelections();
				}
			}
			catch
			{
			}
			Logger.Log("SceneTaunt", $"Owned settlement passive attack hostility prepared. Reason={reason}, Target={targetAgent.Name}, TargetIndex={targetAgent.Index}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Preparing owned settlement passive attack hostility failed: " + ex.Message);
		}
	}

	private bool TryPrimeOwnedSettlementPassiveAttackOnHit(Agent targetAgent, string reason)
	{
		if (!IsOwnedSettlementPassiveAttackScene() || !IsValidOwnedSettlementPassiveAttackTarget(targetAgent))
		{
			return false;
		}
		PrepareOwnedSettlementPassiveAttackTargetForDamage(targetAgent, reason);
		return true;
	}

	private bool TryHandleOwnedSettlementPassiveAttackDamage(Agent targetAgent, float damagedHp, string reason)
	{
		try
		{
			if (damagedHp <= 0f || !IsOwnedSettlementPassiveAttackScene() || !IsValidOwnedSettlementPassiveAttackTarget(targetAgent))
			{
				return false;
			}
			PrepareOwnedSettlementPassiveAttackTargetForDamage(targetAgent, reason);
			Settlement settlement = GetCurrentSettlementForOwnedSettlementPassiveAttack();
			bool firstDamage = _ownedSettlementPassiveDamagedAgentIndices.Add(targetAgent.Index);
			if (firstDamage)
			{
				ApplyOwnedSettlementPassiveAttackLoyaltyPenalty(settlement, OwnedSettlementPassiveAttackLoyaltyPenalty, "owned_settlement_npc_first_damage", targetAgent);
				TryTriggerOwnedSettlementPassiveAttackReaction(targetAgent, knockedDown: false);
				TryRecordPlayerSceneConflictRecentAction(targetAgent, Agent.Main, "damage", reason);
			}
			TryHoldOwnedSettlementPassiveVictimInHandsUpPose(targetAgent);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling owned settlement passive attack damage failed: " + ex.Message);
			return false;
		}
	}

	private void TryHandleOwnedSettlementPassiveAttackKnockdown(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			if ((agentState != AgentState.Unconscious && agentState != AgentState.Killed) || affectorAgent != Agent.Main || !IsOwnedSettlementPassiveAttackScene() || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
			{
				return;
			}
			if (!_ownedSettlementPassiveVictimAgentIndices.Contains(affectedAgent.Index) && !_ownedSettlementPassiveDamagedAgentIndices.Contains(affectedAgent.Index))
			{
				return;
			}
			Settlement settlement = GetCurrentSettlementForOwnedSettlementPassiveAttack();
			bool firstDamage = _ownedSettlementPassiveDamagedAgentIndices.Add(affectedAgent.Index);
			if (firstDamage)
			{
				ApplyOwnedSettlementPassiveAttackLoyaltyPenalty(settlement, OwnedSettlementPassiveAttackLoyaltyPenalty, "owned_settlement_npc_first_damage_before_knockdown", affectedAgent);
			}
			bool firstKnockdown = _ownedSettlementPassiveKnockdownAgentIndices.Add(affectedAgent.Index);
			if (firstKnockdown)
			{
				ApplyOwnedSettlementPassiveAttackLoyaltyPenalty(settlement, OwnedSettlementPassiveAttackLoyaltyPenalty, "owned_settlement_npc_knockdown", affectedAgent);
				TryQueueOwnedSettlementPassiveNotableBattleDeath(affectedAgent, affectorAgent, agentState);
				TryRecordPlayerSceneConflictRecentAction(affectedAgent, affectorAgent, agentState == AgentState.Killed ? "killed" : "unconscious", "owned_settlement_passive_knockdown");
			}
			Logger.Log("SceneTaunt", $"Owned settlement passive attack knockdown handled. Target={affectedAgent.Name}, AgentIndex={affectedAgent.Index}, State={agentState}, Settlement={settlement?.StringId}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling owned settlement passive attack knockdown failed: " + ex.Message);
		}
	}

	private void TryRecordPlayerSceneConflictRecentAction(Agent victimAgent, Agent affectorAgent, string actionKind, string reason, float crimeAmount = 0f)
	{
		try
		{
			if (affectorAgent != Agent.Main || victimAgent == null || !victimAgent.IsHuman || victimAgent == Agent.Main)
			{
				return;
			}
			CharacterObject victimCharacter = victimAgent.Character as CharacterObject;
			Hero victimHero = victimCharacter?.HeroObject;
			if (IsPlayerProtectedSceneAttackAgent(victimAgent) || SceneTauntBehavior.IsChildSceneProtectedTarget(victimCharacter))
			{
				return;
			}
			string normalizedKind = (actionKind ?? "").Trim().ToLowerInvariant();
			if (normalizedKind != "damage" && normalizedKind != "unconscious" && normalizedKind != "killed")
			{
				return;
			}
			int day = GetCurrentGameDayIndexForSceneConflictAction();
			string targetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(victimHero, victimCharacter, victimAgent.Index);
			string stableKey = "scene_conflict_player:" + normalizedKind + ":" + day + ":" + targetKey;
			if (!_recordedPlayerSceneConflictActionKeys.Add(stableKey))
			{
				return;
			}
			Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.LocationEncounter?.Settlement;
			string settlementName = settlement?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(settlementName))
			{
				settlementName = "当前定居点";
			}
			string locationName = CampaignMission.Current?.Location?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(locationName))
			{
				locationName = CampaignMission.Current?.Location?.StringId;
			}
			string locationSuffix = string.IsNullOrWhiteSpace(locationName) ? "" : "的" + locationName.Trim();
			string victimName = victimAgent.Name?.ToString();
			if (string.IsNullOrWhiteSpace(victimName))
			{
				victimName = victimHero?.Name?.ToString() ?? victimCharacter?.Name?.ToString() ?? "目标";
			}
			string verb = normalizedKind == "killed" ? "杀死了" : (normalizedKind == "unconscious" ? "击倒了" : "攻击并打伤了");
			string text = "你在" + settlementName + locationSuffix + verb + victimName + "。";
			if (crimeAmount > 0f)
			{
				text += "你的犯罪度因此上升约 " + crimeAmount.ToString("0.#") + "。";
			}
			bool criminalTarget = IsSettlementCriminalConflictTarget(victimHero, victimCharacter);
			if (criminalTarget)
			{
				text += "对方被当地视为犯罪分子。";
			}
			bool authorityTarget = IsAuthorityPhysicalAttackTarget(victimHero, victimCharacter);
			bool ownedSettlementPassiveAttack = (reason ?? "").IndexOf("owned_settlement", StringComparison.OrdinalIgnoreCase) >= 0;
			RememberPlayerSceneConflictMajorMaterialCandidate(normalizedKind, victimName, targetKey, day, GetCurrentGameDateTextForSceneConflictAction(day), settlement, locationName, victimHero, victimCharacter, crimeAmount, criminalTarget, authorityTarget, ownedSettlementPassiveAttack);
			int sequence = ++_playerSceneConflictActionSequence;
			PlayerNotorietyBehavior.RecordPlayerActionForExternal(
				text,
				stableKey,
				"scene_conflict_" + normalizedKind,
				isMajor: false,
				day,
				GetCurrentGameDateTextForSceneConflictAction(day),
				sequence,
				settlement?.StringId ?? "",
				settlementName,
				locationName ?? "",
				Hero.MainHero?.Culture?.StringId ?? "",
				victimHero?.Culture?.StringId ?? victimCharacter?.Culture?.StringId ?? "",
				settlement?.Culture?.StringId ?? "",
				won: null);
			Logger.Log("SceneTaunt", $"Recorded player scene conflict recent action. Kind={normalizedKind}, Reason={reason}, Victim={victimName}, StableKey={stableKey}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Recording player scene conflict recent action failed: " + ex.Message);
		}
	}

	private void RememberPlayerSceneConflictMajorMaterialCandidate(string actionKind, string victimName, string victimKey, int day, string gameDate, Settlement settlement, string locationText, Hero victimHero, CharacterObject victimCharacter, float crimeAmount, bool criminalTarget, bool authorityTarget, bool ownedSettlementPassiveAttack)
	{
		try
		{
			PlayerSceneConflictMajorMaterialDraft draft = _playerSceneConflictMajorMaterialDraft;
			if (!draft.HasAnyAction)
			{
				draft.HasAnyAction = true;
				draft.Day = day;
				draft.GameDate = gameDate ?? "";
				draft.SettlementId = settlement?.StringId ?? "";
				draft.SettlementName = settlement?.Name?.ToString() ?? "";
				draft.LocationText = locationText ?? "";
				draft.ActorCultureId = Hero.MainHero?.Culture?.StringId ?? "";
				draft.SettlementCultureId = settlement?.Culture?.StringId ?? "";
			}
			if (string.IsNullOrWhiteSpace(draft.TargetCultureId))
			{
				draft.TargetCultureId = victimHero?.Culture?.StringId ?? victimCharacter?.Culture?.StringId ?? "";
			}
			switch ((actionKind ?? "").Trim().ToLowerInvariant())
			{
				case "killed":
					draft.KilledCount++;
					break;
				case "unconscious":
					draft.UnconsciousCount++;
					break;
				default:
					draft.DamageCount++;
					break;
			}
			draft.CrimeAmount += MathF.Max(0f, crimeAmount);
			draft.HadCriminalTarget |= criminalTarget;
			draft.HadAuthorityTarget |= authorityTarget;
			draft.HadOwnedSettlementPassiveAttack |= ownedSettlementPassiveAttack;
			string key = string.IsNullOrWhiteSpace(victimKey) ? (victimName ?? "") : victimKey;
			if (!string.IsNullOrWhiteSpace(key) && draft.VictimKeys.Add(key) && !string.IsNullOrWhiteSpace(victimName) && draft.VictimNames.Count < 8)
			{
				draft.VictimNames.Add(victimName.Trim());
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Remembering player scene conflict major material failed: " + ex.Message);
		}
	}

	private void FlushPlayerSceneConflictMajorMaterial()
	{
		try
		{
			PlayerSceneConflictMajorMaterialDraft draft = _playerSceneConflictMajorMaterialDraft;
			if (!draft.HasAnyAction)
			{
				return;
			}
			string settlementName = string.IsNullOrWhiteSpace(draft.SettlementName) ? "当前定居点" : draft.SettlementName.Trim();
			string locationSuffix = string.IsNullOrWhiteSpace(draft.LocationText) ? "" : "的" + draft.LocationText.Trim();
			int victimCount = draft.VictimKeys.Count;
			string victimSummary = BuildPlayerSceneConflictVictimSummary(draft);
			string text = "你在" + settlementName + locationSuffix + "卷入和平场景冲突";
			if (victimCount > 0)
			{
				text += "，伤及 " + victimCount + " 名 NPC";
			}
			if (!string.IsNullOrWhiteSpace(victimSummary))
			{
				text += "（" + victimSummary + "）";
			}
			List<string> consequences = new List<string>();
			if (draft.KilledCount > 0)
			{
				consequences.Add("杀死 " + draft.KilledCount + " 人");
			}
			if (draft.UnconsciousCount > 0)
			{
				consequences.Add("击倒 " + draft.UnconsciousCount + " 人");
			}
			if (draft.DamageCount > 0)
			{
				consequences.Add("造成 " + draft.DamageCount + " 次伤害");
			}
			if (consequences.Count > 0)
			{
				text += "，" + string.Join("，", consequences);
			}
			if (draft.CrimeAmount > 0f)
			{
				text += "，犯罪度累计上升约 " + draft.CrimeAmount.ToString("0.#");
			}
			if (draft.HadOwnedSettlementPassiveAttack)
			{
				text += "，并在自有定居点内造成忠诚度损失";
			}
			if (draft.HadAuthorityTarget)
			{
				text += "，目标包含当地权威人物";
			}
			if (draft.HadCriminalTarget)
			{
				text += "，目标中包含当地犯罪分子";
			}
			text += "。";
			int day = draft.Day >= 0 ? draft.Day : GetCurrentGameDayIndexForSceneConflictAction();
			int hash = text.GetHashCode() & int.MaxValue;
			string stableKey = "scene_conflict_player_major:" + day + ":" + (draft.SettlementId ?? "") + ":" + (draft.LocationText ?? "") + ":" + hash;
			PlayerNotorietyBehavior.RecordPlayerHistoryMaterialForExternal(
				text,
				stableKey,
				"scene_conflict_summary",
				day,
				draft.GameDate,
				draft.ActorCultureId,
				draft.TargetCultureId,
				draft.SettlementCultureId);
			MyBehavior.RecordPlayerSceneConflictWeeklyMaterialForExternal(
				text,
				stableKey,
				day,
				draft.GameDate,
				draft.SettlementId,
				settlementName,
				draft.LocationText);
			Logger.Log("SceneTaunt", $"Recorded player scene conflict major material. Day={day}, Settlement={draft.SettlementId}, Victims={victimCount}, Damage={draft.DamageCount}, Unconscious={draft.UnconsciousCount}, Killed={draft.KilledCount}, Crime={draft.CrimeAmount:0.##}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Flushing player scene conflict major material failed: " + ex.Message);
		}
		finally
		{
			ResetPlayerSceneConflictMajorMaterialDraft();
		}
	}

	private static string BuildPlayerSceneConflictVictimSummary(PlayerSceneConflictMajorMaterialDraft draft)
	{
		if (draft?.VictimNames == null || draft.VictimNames.Count == 0)
		{
			return "";
		}
		int count = Math.Min(3, draft.VictimNames.Count);
		string text = string.Join("、", draft.VictimNames.Take(count));
		if (draft.VictimNames.Count > count)
		{
			text += "等";
		}
		return text;
	}

	private void ResetPlayerSceneConflictMajorMaterialDraft()
	{
		PlayerSceneConflictMajorMaterialDraft draft = _playerSceneConflictMajorMaterialDraft;
		draft.HasAnyAction = false;
		draft.Day = -1;
		draft.GameDate = "";
		draft.SettlementId = "";
		draft.SettlementName = "";
		draft.LocationText = "";
		draft.ActorCultureId = "";
		draft.TargetCultureId = "";
		draft.SettlementCultureId = "";
		draft.DamageCount = 0;
		draft.UnconsciousCount = 0;
		draft.KilledCount = 0;
		draft.CrimeAmount = 0f;
		draft.HadCriminalTarget = false;
		draft.HadAuthorityTarget = false;
		draft.HadOwnedSettlementPassiveAttack = false;
		draft.VictimKeys.Clear();
		draft.VictimNames.Clear();
	}

	private static int GetCurrentGameDayIndexForSceneConflictAction()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static string GetCurrentGameDateTextForSceneConflictAction(int day)
	{
		try
		{
			string text = CampaignTime.Now.ToString();
			return string.IsNullOrWhiteSpace(text) ? ("第 " + day + " 日") : text.Trim();
		}
		catch
		{
			return "第 " + day + " 日";
		}
	}

	private static float EstimatePlayerSceneConflictStartCrimeAmount(Agent targetAgent, bool playerUsedWeapon)
	{
		try
		{
			CharacterObject targetCharacter = targetAgent?.Character as CharacterObject;
			Hero targetHero = targetCharacter?.HeroObject;
			if (IsSettlementCriminalConflictTarget(targetHero, targetCharacter))
			{
				return 0f;
			}
			return (playerUsedWeapon || IsAuthorityPhysicalAttackTarget(targetHero, targetCharacter)) ? SceneTauntInitialArmedCrimeAmount : 5f;
		}
		catch
		{
			return 0f;
		}
	}

	private void ApplyOwnedSettlementPassiveAttackLoyaltyPenalty(Settlement currentSettlement, int penalty, string reason, Agent targetAgent)
	{
		try
		{
			Settlement loyaltySettlement = ResolveLoyaltySettlementForOwnedSettlementPassiveAttack(currentSettlement);
			if (loyaltySettlement?.Town == null || penalty <= 0)
			{
				return;
			}
			float loyalty = loyaltySettlement.Town.Loyalty;
			float num = MBMath.ClampFloat(loyalty - penalty, 0f, 100f);
			loyaltySettlement.Town.Loyalty = num;
			float num2 = MathF.Max(0f, loyalty - num);
			string text = targetAgent?.Name?.ToString() ?? "NPC";
			AnimusForgeQuickInfo.Show($"{loyaltySettlement.Name} 忠诚度 -{num2:0.#}：你在自己的定居点内伤害了 {text}。", targetAgent?.Character as BasicCharacterObject);
			Logger.Log("SceneTaunt", $"Owned settlement passive attack loyalty penalty. CurrentSettlement={currentSettlement?.StringId}, LoyaltySettlement={loyaltySettlement.StringId}, Target={text}, Reason={reason}, Loyalty={loyalty:0.##}->{num:0.##}, Requested={penalty}, Applied={num2:0.##}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying owned settlement passive attack loyalty penalty failed: " + ex.Message);
		}
	}

	private void TryQueueOwnedSettlementPassiveNotableBattleDeath(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
			{
				return;
			}
			CharacterObject characterObject = affectedAgent?.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (!SceneTauntBehavior.IsSceneNotableTauntTarget(hero) || !_sceneNotableDeferredBattleDeathCandidates.Contains(hero))
			{
				return;
			}
			Hero hero2 = (affectorAgent?.Character as CharacterObject)?.HeroObject;
			if (hero2 == null && affectorAgent == Agent.Main)
			{
				hero2 = Hero.MainHero;
			}
			SceneTauntBehavior.MarkPendingSceneNotableBattleDeathForExternal(hero, hero2, agentState == AgentState.Killed ? "owned_settlement_passive_location_kill" : "owned_settlement_passive_unconscious_deathmark");
			_sceneNotableDeferredBattleDeathCandidates.Remove(hero);
			_sceneNotableRecentHitNonLethal.Remove(hero);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing owned settlement notable battle death failed: " + ex.Message);
		}
	}

	private void TryTriggerOwnedSettlementPassiveAttackReaction(Agent targetAgent, bool knockedDown)
	{
		try
		{
			if (!IsValidOwnedSettlementPassiveAttackTarget(targetAgent))
			{
				return;
			}
			float num = Mission.Current?.CurrentTime ?? 0f;
			if (!knockedDown && _ownedSettlementPassiveReactionTimes.TryGetValue(targetAgent.Index, out var value) && num - value < OwnedSettlementPassiveAttackReactionCooldownSeconds)
			{
				return;
			}
			_ownedSettlementPassiveReactionTimes[targetAgent.Index] = num;
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			string factText = knockedDown ? ("[AFEF NPC行为补充] " + text + "在自己的定居点内把你击倒了。这里属于" + text + "的家族，" + text + "是此地领主/主人；你已经被领主打倒，正在原地抱头，不会反击。你必须把" + text + "当作你的领主大人来回应，只能恐惧、服从、求饶、认错，说出类似“领主大人饶命”“求您宽恕我”的话。不要强硬回嘴、威胁、挑衅、摆贵族架子、宣称对方不敢动你、指责对方会遭报应，也不要用民族或国家立场顶嘴。不要生成犯罪值相关内容。") : ("[AFEF NPC行为补充] " + text + "在自己的定居点内直接打伤了你。这里属于" + text + "的家族，" + text + "是此地领主/主人；你被领主惩戒后正在原地抱头，不会反击。你必须把" + text + "当作你的领主大人来回应，只能恐惧、服从、求饶、认错，说出类似“领主大人饶命”“求您宽恕我”的话。不要强硬回嘴、威胁、挑衅、摆贵族架子、宣称对方不敢动你、指责对方会遭报应，也不要用民族或国家立场顶嘴。不要生成犯罪值相关内容。");
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, targetAgent.Index, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Triggering owned settlement passive attack reaction failed: " + ex.Message);
		}
	}

	private void TryMaintainOwnedSettlementPassiveAttackVictims(float dt)
	{
		try
		{
			if (!IsOwnedSettlementPassiveAttackActive())
			{
				return;
			}
			if (!SceneTauntBehavior.IsPeaceSceneConflictEnabled())
			{
				ClearOwnedSettlementPassiveAttackState("peace_scene_conflict_disabled");
				return;
			}
			if (_ownedSettlementPassiveVictimAgentIndices.Count == 0)
			{
				return;
			}
			var agents = Mission.Current?.Agents;
			if (agents == null)
			{
				return;
			}
			List<int> list = null;
			foreach (int item in _ownedSettlementPassiveVictimAgentIndices)
			{
				Agent agent = agents.FirstOrDefault(a => a != null && a.Index == item);
				if (!IsValidOwnedSettlementPassiveAttackTarget(agent))
				{
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(item);
					continue;
				}
				EnsureOwnedSettlementPassiveAttackHostility(agent, "owned_settlement_passive_maintain");
				if (ShouldRefreshOwnedSettlementPassiveHandsUpPose(dt) || _ownedSettlementPassiveDamagedAgentIndices.Contains(item))
				{
					TryHoldOwnedSettlementPassiveVictimInHandsUpPose(agent);
				}
			}
			if (list == null)
			{
				return;
			}
			foreach (int item2 in list)
			{
				_ownedSettlementPassiveVictimAgentIndices.Remove(item2);
				_ownedSettlementPassiveReactionTimes.Remove(item2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining owned settlement passive attack victims failed: " + ex.Message);
		}
	}

	private bool ShouldRefreshOwnedSettlementPassiveHandsUpPose(float dt)
	{
		_ownedSettlementPassiveHandsUpPoseRefreshTimer -= dt;
		if (_ownedSettlementPassiveHandsUpPoseRefreshTimer > 0f)
		{
			return false;
		}
		_ownedSettlementPassiveHandsUpPoseRefreshTimer = OwnedSettlementPassiveHandsUpPoseRefreshInterval;
		return true;
	}

	private void TryHoldOwnedSettlementPassiveVictimInHandsUpPose(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			try
			{
				agent.SetLookAgent(null);
				agent.DisableScriptedMovement();
				agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
			}
			catch
			{
			}
			TryDisableOwnedSettlementPassiveScriptedBehavior(agent);
			TryApplyOwnedSettlementPassiveHandsUpPose(agent);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Holding owned settlement passive victim in hands-up pose failed: " + ex.Message);
		}
	}

	private void TryDisableOwnedSettlementPassiveScriptedBehavior(Agent agent)
	{
		try
		{
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			AlarmedBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<AlarmedBehaviorGroup>();
			behaviorGroup?.DisableScriptedBehavior();
		}
		catch
		{
		}
	}

	private void TryApplyOwnedSettlementPassiveHandsUpPose(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
			agent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
		try
		{
			agent.SetCrouchMode(false);
		}
		catch
		{
		}
		try
		{
			ActionIndexCache action = ActionIndexCache.act_scared_idle_1;
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, action))
			{
				if (!_ownedSettlementPassiveHandsUpActionMissingLogged)
				{
					_ownedSettlementPassiveHandsUpActionMissingLogged = true;
					Logger.Log("SceneTaunt", "owned_settlement_passive_hands_up_action_missing action=act_scared_idle_1");
				}
				return;
			}
			int channelNo = 0;
			if (agent.GetCurrentAction(channelNo) == action && _ownedSettlementPassiveHandsUpPoseAppliedAgentIndices.Contains(agent.Index))
			{
				try
				{
					agent.SetCurrentActionProgress(channelNo, OwnedSettlementPassiveHandsUpPoseStartProgress);
				}
				catch
				{
				}
				return;
			}
			AnimFlags poseFlags = AnimFlags.anf_disable_alternative_randomization | AnimFlags.anf_disable_auto_increment_progress | AnimFlags.anf_enforce_all;
			bool actionSet = agent.SetActionChannel(channelNo, action, true, poseFlags, 0f, OwnedSettlementPassiveHandsUpPoseActionSpeed, -0.2f, 0.4f, OwnedSettlementPassiveHandsUpPoseStartProgress, false, -0.2f, 0, true);
			if (actionSet)
			{
				try
				{
					agent.SetCurrentActionProgress(channelNo, OwnedSettlementPassiveHandsUpPoseStartProgress);
				}
				catch
				{
				}
				_ownedSettlementPassiveHandsUpPoseAppliedAgentIndices.Add(agent.Index);
				return;
			}
			if (!_ownedSettlementPassiveHandsUpActionRejectedLogged)
			{
				_ownedSettlementPassiveHandsUpActionRejectedLogged = true;
				Logger.Log("SceneTaunt", "owned_settlement_passive_hands_up_action_rejected action=act_scared_idle_1");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying owned settlement passive hands-up pose failed: " + ex.Message);
		}
	}

	private void ClearOwnedSettlementPassiveAttackState(string reason)
	{
		try
		{
			bool hadState = IsOwnedSettlementPassiveAttackActive();
			RestoreOwnedSettlementPassiveAttackTeams();
			_ownedSettlementPassiveVictimAgentIndices.Clear();
			_ownedSettlementPassiveDamagedAgentIndices.Clear();
			_ownedSettlementPassiveKnockdownAgentIndices.Clear();
			_ownedSettlementPassiveReactionTimes.Clear();
			_ownedSettlementPassiveOriginalTeams.Clear();
			_ownedSettlementPassivePlayerTeam = null;
			_ownedSettlementPassiveEnemyTeam = null;
			_ownedSettlementPassiveOriginalMainTeam = null;
			if (hadState)
			{
				Logger.Log("SceneTaunt", $"Owned settlement passive attack state cleared. Reason={reason}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Clearing owned settlement passive attack state failed: " + ex.Message);
		}
	}

	private void RestoreOwnedSettlementPassiveAttackTeams()
	{
		try
		{
			if (_ownedSettlementPassivePlayerTeam != null && _ownedSettlementPassiveEnemyTeam != null)
			{
				try
				{
					_ownedSettlementPassivePlayerTeam.SetIsEnemyOf(_ownedSettlementPassiveEnemyTeam, isEnemyOf: false);
				}
				catch
				{
				}
				try
				{
					_ownedSettlementPassiveEnemyTeam.SetIsEnemyOf(_ownedSettlementPassivePlayerTeam, isEnemyOf: false);
				}
				catch
				{
				}
			}
			foreach (KeyValuePair<int, Team> item in _ownedSettlementPassiveOriginalTeams.ToList())
			{
				try
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == item.Key);
					if (agent != null && agent.IsActive() && item.Value != null && agent.Team != item.Value)
					{
						agent.SetTeam(item.Value, sync: true);
					}
				}
				catch
				{
				}
			}
			try
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive() && _ownedSettlementPassiveOriginalMainTeam != null && main.Team != _ownedSettlementPassiveOriginalMainTeam)
				{
					main.SetTeam(_ownedSettlementPassiveOriginalMainTeam, sync: true);
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Restoring owned settlement passive attack teams failed: " + ex.Message);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		if (affectorAgent == Agent.Main && IsPlayerProtectedSceneAttackAgent(affectedAgent))
		{
			return;
		}
		if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main)
		{
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(affectedAgent.Index, affectorAgent == Agent.Main ? "scene_taunt_agent_hit" : "scene_agent_hit_any_source");
		}
		if (affectorAgent != Agent.Main || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
		{
			return;
		}
		bool attackerWeaponIsReal = IsMissionWeaponRealWeapon(attackerWeapon);
		Logger.LogVerbose("SceneTaunt", "attack_timing_on_agent_hit:" + affectedAgent.Index, () => $"[AttackTiming] on_agent_hit time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} target={affectedAgent.Name} targetIndex={affectedAgent.Index} weapon={attackerWeaponIsReal} conflict={_conflictActive} armed={_armedConflict}", 1.0);
		if (SettlementEntryTroopSelectionBehavior.ShouldHandlePhysicalAttackForExternal(Mission.Current, affectorAgent, affectedAgent, attackerWeaponIsReal))
		{
			Logger.LogVerbose("SceneTaunt", "sets_entry_suppress_hit:" + affectedAgent.Index, () => $"Suppressed SceneTaunt conflict because SETS will handle this settlement attack. Target={affectedAgent.Name}", 1.0);
			return;
		}
		if (TryPrimeOwnedSettlementPassiveAttackOnHit(affectedAgent, "player_physical_hit"))
		{
			return;
		}
		if (!SceneTauntBehavior.IsPeaceSceneConflictEnabled() && !_conflictActive)
		{
			return;
		}
		if (!_conflictActive)
		{
			TryStartConflictFromPhysicalAttack(affectedAgent, IsMissionWeaponRealWeapon(attackerWeapon), "player_physical_hit");
			return;
		}
		if (_armedConflict)
		{
			return;
		}
		if (IsMissionWeaponRealWeapon(attackerWeapon))
		{
			EscalateToArmedConflict("player_attacked_with_weapon");
			return;
		}
		TryAddFacingAgentToUnarmedConflict(affectedAgent, "player_physical_hit_existing_unarmed_conflict");
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
		if (affectorAgent == Agent.Main && IsPlayerProtectedSceneAttackAgent(affectedAgent))
		{
			return;
		}
		RememberSceneNotableHitLethality(affectedAgent, affectorAgent, attackerWeapon, in blow, damagedHp);
		if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main && damagedHp > 0f)
		{
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(affectedAgent.Index, affectorAgent == Agent.Main ? "scene_taunt_score_hit" : "scene_score_hit_any_source");
		}
		if (damagedHp <= 0f || affectorAgent != Agent.Main || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
		{
			return;
		}
		bool attackerWeaponIsReal = IsWeaponComponentRealWeapon(attackerWeapon);
		Logger.LogVerbose("SceneTaunt", "attack_timing_on_score_hit:" + affectedAgent.Index, () => $"[AttackTiming] on_score_hit time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} target={affectedAgent.Name} targetIndex={affectedAgent.Index} weapon={attackerWeaponIsReal} damage={damagedHp:0.##} blocked={isBlocked} conflict={_conflictActive} armed={_armedConflict}", 1.0);
		if (SettlementEntryTroopSelectionBehavior.ShouldHandlePhysicalAttackForExternal(Mission.Current, affectorAgent, affectedAgent, attackerWeaponIsReal))
		{
			Logger.LogVerbose("SceneTaunt", "sets_entry_suppress_score_hit:" + affectedAgent.Index, () => $"Suppressed SceneTaunt score-hit conflict because SETS will handle this settlement attack. Target={affectedAgent.Name}", 1.0);
			return;
		}
		if (TryHandleOwnedSettlementPassiveAttackDamage(affectedAgent, damagedHp, "player_physical_score_hit"))
		{
			return;
		}
		if (!SceneTauntBehavior.IsPeaceSceneConflictEnabled() && !_conflictActive)
		{
			return;
		}
		if (!_conflictActive)
		{
			bool playerUsedWeapon = attackerWeaponIsReal;
			float startCrimeAmount = EstimatePlayerSceneConflictStartCrimeAmount(affectedAgent, playerUsedWeapon);
			if (TryStartConflictFromPhysicalAttack(affectedAgent, playerUsedWeapon, "player_physical_score_hit"))
			{
				TryRecordPlayerSceneConflictRecentAction(affectedAgent, affectorAgent, "damage", "player_physical_score_hit_start", startCrimeAmount);
			}
			return;
		}
		TryRecordPlayerSceneConflictRecentAction(affectedAgent, affectorAgent, "damage", "player_physical_score_hit_existing_conflict");
		if (!_armedConflict && attackerWeaponIsReal)
		{
			EscalateToArmedConflict("player_dealt_weapon_damage");
			return;
		}
		if (!_armedConflict)
		{
			TryAddFacingAgentToUnarmedConflict(affectedAgent, "player_physical_score_hit_existing_unarmed_conflict");
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		try
		{
			if (affectedAgent != null && affectedAgent.IsHuman)
			{
				ShoutBehavior.CancelAgentSpeechForRemovalExternal(affectedAgent.Index, "scene_taunt_agent_removed_" + agentState);
			}
			TryHandleOwnedSettlementPassiveAttackKnockdown(affectedAgent, affectorAgent, agentState);
			TryQueuePendingPlayerBattleDeathOutcome(affectedAgent, affectorAgent, agentState);
			TryApplyNativeAlleyNpcKnockdownConsequences(affectedAgent, affectorAgent, agentState);
			bool canTrySceneGoldDrop = affectedAgent != null && affectedAgent.IsHuman && (_conflictActive || IsSceneGoldEligibleDropAgent(affectedAgent));
			if (canTrySceneGoldDrop)
			{
				LogSceneGoldDiag($"OnAgentRemoved before gold spawn. affected={(affectedAgent?.Name ?? "null")} idx={affectedAgent?.Index ?? -1} state={agentState} affector={(affectorAgent?.Name ?? "null")} conflict={_conflictActive} armed={_armedConflict} eligible={IsSceneGoldEligibleDropAgent(affectedAgent)}");
				TrySpawnSceneGoldDropForKnockdown(affectedAgent, agentState);
				LogSceneGoldDiag($"OnAgentRemoved after gold spawn. affected={(affectedAgent?.Name ?? "null")} idx={affectedAgent?.Index ?? -1} drops={_sceneGoldDrops.Count}");
			}
			if (!_conflictActive || affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			TryApplyArmedNpcKnockdownConsequences(affectedAgent, affectorAgent, agentState);
			CharacterObject characterObject = affectedAgent.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (!SceneTauntBehavior.IsSceneNotableTauntTarget(hero))
			{
				return;
			}
			if ((agentState == AgentState.Killed || agentState == AgentState.Unconscious) && _sceneNotableDeferredBattleDeathCandidates.Contains(hero))
			{
				Hero hero2 = (affectorAgent?.Character as CharacterObject)?.HeroObject;
				if (hero2 == null && affectorAgent == Agent.Main)
				{
					hero2 = Hero.MainHero;
				}
				SceneTauntBehavior.MarkPendingSceneNotableBattleDeathForExternal(hero, hero2, agentState == AgentState.Killed ? "scene_taunt_location_kill" : "scene_taunt_location_unconscious_deathmark");
			}
			_sceneNotableDeferredBattleDeathCandidates.Remove(hero);
			_sceneNotableRecentHitNonLethal.Remove(hero);
			TryQueueImmediateUnarmedFightEndAfterAgentRemoval(affectedAgent, agentState);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling scene notable removal failed: " + ex.Message);
		}
	}

	private void TrySpawnSceneGoldDropForKnockdown(Agent affectedAgent, AgentState agentState)
	{
		try
		{
			bool canSpawnForConflict = _conflictActive || IsSceneGoldEligibleDropAgent(affectedAgent);
			LogSceneGoldDiag($"spawn_enter affected={(affectedAgent?.Name ?? "null")} idx={affectedAgent?.Index ?? -1} state={agentState} conflict={_conflictActive} eligible={IsSceneGoldEligibleDropAgent(affectedAgent)}");
			if (!canSpawnForConflict || affectedAgent == null || affectedAgent.IsMainAgent || !affectedAgent.IsHuman)
			{
				LogSceneGoldDiag("spawn_skip basic_guard");
				return;
			}
			if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
			{
				LogSceneGoldDiag($"spawn_skip state={agentState}");
				return;
			}
			if (!_sceneGoldDropAgentIndices.Add(affectedAgent.Index))
			{
				LogSceneGoldDiag($"spawn_skip duplicate idx={affectedAgent.Index}");
				return;
			}
			CharacterObject characterObject = affectedAgent.Character as CharacterObject;
			if (characterObject == null || SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))
			{
				LogSceneGoldDiag($"spawn_skip character invalid null={characterObject == null} childProtected={(characterObject != null && SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))}");
				return;
			}
			Hero heroObject = characterObject.HeroObject;
			SceneGoldSettlementPoolLocation sceneGoldSettlementPoolLocation = GetCurrentSceneGoldSettlementPoolLocation();
			if (sceneGoldSettlementPoolLocation == SceneGoldSettlementPoolLocation.LordHall && !SceneTauntBehavior.IsSceneLordTauntTarget(heroObject))
			{
				LogSceneGoldDiag($"spawn_skip lordhall_non_lord idx={affectedAgent.Index} hero={(heroObject?.StringId ?? "null")} character={characterObject.StringId}");
				return;
			}
			Settlement currentSettlement = Settlement.CurrentSettlement;
			int fixedSettlementGold = 0;
			int visualGoldAmount = 0;
			bool isHeroDrop = heroObject != null;
			bool usesLocationSettlementPool = sceneGoldSettlementPoolLocation != SceneGoldSettlementPoolLocation.None;
			if (usesLocationSettlementPool)
			{
				fixedSettlementGold = GetSceneGoldSettlementShareForAgent(currentSettlement, affectedAgent, sceneGoldSettlementPoolLocation);
				visualGoldAmount = fixedSettlementGold;
				LogSceneGoldDiag($"spawn_amount location_pool location={sceneGoldSettlementPoolLocation} hero={(heroObject?.StringId ?? "null")} settlementGold={currentSettlement?.SettlementComponent?.Gold ?? -1} fixed={fixedSettlementGold} visual={visualGoldAmount}");
				if (visualGoldAmount <= 0)
				{
					LogSceneGoldDiag("spawn_skip location_pool_amount_zero");
					return;
				}
			}
			else if (isHeroDrop)
			{
				int heroGoldAmount = GetHeroSceneGoldPickupAmount(heroObject);
				if (heroGoldAmount > 0)
				{
					visualGoldAmount = heroGoldAmount;
				}
				else
				{
					fixedSettlementGold = GetHeroSceneGoldSettlementFallbackAmount(currentSettlement);
					visualGoldAmount = fixedSettlementGold;
				}
				LogSceneGoldDiag($"spawn_amount hero hero={(heroObject?.StringId ?? "null")} heroGold={heroObject?.Gold ?? -1} settlementGold={currentSettlement?.SettlementComponent?.Gold ?? -1} fixedSettlementFallback={fixedSettlementGold} visual={visualGoldAmount}");
				if (visualGoldAmount <= 0)
				{
					LogSceneGoldDiag("spawn_skip hero_amount_zero");
					return;
				}
			}
			else
			{
				if (currentSettlement?.SettlementComponent == null)
				{
					LogSceneGoldDiag("spawn_skip no_settlement_component");
					return;
				}
				LogSceneGoldDiag($"spawn_snapshot_begin settlement={currentSettlement.StringId} settlementGold={currentSettlement.SettlementComponent.Gold}");
				CaptureSceneGoldShareSnapshot(currentSettlement, affectedAgent, sceneGoldSettlementPoolLocation);
				LogSceneGoldDiag($"spawn_snapshot_done shares={_sceneGoldSettlementShareByAgentIndex.Count}");
				if (!_sceneGoldSettlementShareByAgentIndex.TryGetValue(affectedAgent.Index, out fixedSettlementGold) || fixedSettlementGold <= 0)
				{
					LogSceneGoldDiag($"spawn_skip share_missing idx={affectedAgent.Index} fixed={fixedSettlementGold}");
					return;
				}
				visualGoldAmount = fixedSettlementGold;
				LogSceneGoldDiag($"spawn_amount settlement fixed={fixedSettlementGold} visual={visualGoldAmount}");
			}
			if (visualGoldAmount <= 0)
			{
				LogSceneGoldDiag("spawn_skip amount_zero");
				return;
			}
			LogSceneGoldDiag($"spawn_entities_begin visual={visualGoldAmount} pos={FormatSceneGoldVec(affectedAgent.Position)}");
			bool usesNativeItemPhysics = false;
			List<SceneGoldCoinSim> simulatedCoins = TryCreateSceneGoldEntities(affectedAgent.Position, visualGoldAmount, out usesNativeItemPhysics);
			LogSceneGoldDiag($"spawn_entities_done count={simulatedCoins?.Count ?? -1}");
			if (simulatedCoins == null || simulatedCoins.Count == 0)
			{
				Logger.Log("SceneTaunt", $"Scene gold drop entity spawn failed. Victim={affectedAgent.Name}, AgentIndex={affectedAgent.Index}");
				return;
			}
			List<GameEntity> gameEntities = simulatedCoins.Select(coin => coin.Entity).Where(entity => entity != null).ToList();
			LogSceneGoldDiag($"spawn_entities_materialized entityCount={gameEntities.Count}");
			Vec3 position = affectedAgent.Position;
			try
			{
				if (base.Mission?.Scene != null)
				{
					LogSceneGoldDiag($"spawn_ground_begin pos={FormatSceneGoldVec(position)}");
					position.z = base.Mission.Scene.GetGroundHeightAtPosition(position) + 0.03f;
					LogSceneGoldDiag($"spawn_ground_done pos={FormatSceneGoldVec(position)}");
				}
			}
			catch
			{
				LogSceneGoldDiag("spawn_ground_exception_ignored");
			}
			LogSceneGoldDiag("spawn_drop_add_begin");
			_sceneGoldDrops.Add(new SceneGoldDrop
			{
				AgentIndex = affectedAgent.Index,
				Position = position,
				Entity = gameEntities[0],
				Entities = gameEntities,
				SimulatedCoins = simulatedCoins,
				SourceHero = heroObject,
				SourceSettlement = currentSettlement,
				FixedSettlementGold = Math.Max(0, fixedSettlementGold),
				UsesLocationSettlementPool = usesLocationSettlementPool,
				VisualGoldAmount = Math.Max(0, visualGoldAmount),
				IsHeroDrop = isHeroDrop,
				UsesNativeItemPhysics = usesNativeItemPhysics
			});
			LogSceneGoldDiag($"spawn_drop_add_done drops={_sceneGoldDrops.Count}");
			Logger.Log("SceneTaunt", $"Scene gold drop spawned. Victim={affectedAgent.Name}, AgentIndex={affectedAgent.Index}, HeroDrop={isHeroDrop}, FixedGold={fixedSettlementGold}, VisualGold={visualGoldAmount}, VisualEntities={gameEntities.Count}, NativeItemPhysics={usesNativeItemPhysics}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Spawning scene gold drop failed: " + ex.Message);
		}
	}

	internal static bool TrySpawnSceneGoldDropForExternal(Agent affectedAgent, AgentState agentState, string source)
	{
		try
		{
			Mission mission = affectedAgent?.Mission ?? Mission.Current;
			SceneTauntMissionBehavior behavior = mission?.GetMissionBehavior<SceneTauntMissionBehavior>();
			if (behavior == null)
			{
				LogSceneGoldDiag($"external_spawn_skip behavior_null source={source ?? "N/A"}");
				return false;
			}
			int beforeCount = behavior._sceneGoldDrops.Count;
			behavior.RegisterSceneGoldEligibleAgent(affectedAgent, "external_" + (string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim()));
			behavior.TrySpawnSceneGoldDropForKnockdown(affectedAgent, agentState);
			return behavior._sceneGoldDrops.Count > beforeCount;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "External scene gold drop spawn failed: " + ex.Message);
			return false;
		}
	}

	internal static void TryMaintainSceneGoldDropsForExternal(float dt, string source)
	{
		try
		{
			SceneTauntMissionBehavior behavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
			if (behavior == null)
			{
				return;
			}
			behavior.TryMaintainSceneGoldCoinMotion(dt);
			behavior.TryHandleSceneGoldPickupInput();
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "External scene gold drop tick failed (" + (source ?? "N/A") + "): " + ex.Message);
		}
	}

	private List<SceneGoldCoinSim> TryCreateSceneGoldEntities(Vec3 sourcePosition, int visualGoldAmount, out bool usesNativeItemPhysics)
	{
		usesNativeItemPhysics = false;
		LogSceneGoldDiag($"create_enter visual={visualGoldAmount} source={FormatSceneGoldVec(sourcePosition)}");
		Scene scene = base.Mission?.Scene;
		if (scene == null)
		{
			LogSceneGoldDiag("create_skip scene_null");
			return null;
		}
		Vec3 origin = sourcePosition;
		try
		{
			LogSceneGoldDiag($"create_ground_begin origin={FormatSceneGoldVec(origin)}");
			origin.z = scene.GetGroundHeightAtPosition(origin) + 0.03f;
			LogSceneGoldDiag($"create_ground_done origin={FormatSceneGoldVec(origin)}");
		}
		catch
		{
			LogSceneGoldDiag("create_ground_exception_ignored");
		}
		try
		{
			List<SceneGoldVisualPiece> visualPieces = BuildSceneGoldVisualPieces(visualGoldAmount);
			if (visualPieces != null && visualPieces.Count > 0)
			{
				LogSceneGoldDiag($"create_item_path_begin pieces={visualPieces.Count} visual={visualGoldAmount}");
				Vec3 burstOrigin = origin;
				burstOrigin.z += SceneGoldBurstSpawnHeight;
				List<SceneGoldCoinSim> nativePhysicsCoins = TryCreateSceneGoldItemDrops(burstOrigin, visualPieces);
				if (nativePhysicsCoins != null && nativePhysicsCoins.Count > 0)
				{
					usesNativeItemPhysics = false;
					LogSceneGoldDiag($"create_item_path_done count={nativePhysicsCoins.Count}");
					return nativePhysicsCoins;
				}
				LogSceneGoldDiag("create_item_path_fallback_to_prefab");
			}
			else
			{
				LogSceneGoldDiag($"create_item_missing ids={SceneGoldCustomItemId},{SceneGoldCustomIngotItemId},{SceneGoldFallbackNativeItemId}");
			}
			int coinCount = Math.Min(Math.Max(1, visualGoldAmount), SceneGoldMaxVisualCoins);
			LogSceneGoldDiag($"create_loop_begin coinCount={coinCount} max={SceneGoldMaxVisualCoins} visualRequested={visualGoldAmount} capped={visualGoldAmount > SceneGoldMaxVisualCoins}");
			List<SceneGoldCoinSim> simulatedCoins = new List<SceneGoldCoinSim>(coinCount);
			float spawnRadius = Math.Min(0.28f, 0.05f + (float)Math.Sqrt(coinCount) * 0.025f);
			for (int i = 0; i < coinCount; i++)
			{
				MatrixFrame frame = MatrixFrame.Identity;
				float angle = (float)(Math.PI * 2.0 * i / Math.Max(1, coinCount));
				float offsetRadius = spawnRadius * (float)Math.Sqrt((i + 0.5f) / Math.Max(1, coinCount));
				frame.origin = new Vec3(origin.x + (float)Math.Cos(angle) * offsetRadius * 0.35f, origin.y + (float)Math.Sin(angle) * offsetRadius * 0.35f, origin.z + 0.82f + (i % 4) * 0.025f, -1f);
				frame.rotation.RotateAboutSide(0.35f);
				frame.rotation.RotateAboutUp(angle);
				LogSceneGoldDiag($"coin[{i}] prefab_create_begin prefab={SceneGoldCustomVisualPrefab} frameOrigin={FormatSceneGoldVec(frame.origin)}");
				GameEntity gameEntity = TryInstantiateSceneGoldCoinPrefab(scene);
				LogSceneGoldDiag($"coin[{i}] entity_create_done null={gameEntity == null}");
				if (gameEntity == null)
				{
					LogSceneGoldDiag($"coin[{i}] prefab_missing prefab={SceneGoldCustomVisualPrefab}");
					break;
				}
				LogSceneGoldDiag($"coin[{i}] name_begin");
				gameEntity.Name = "animusforge_scene_denar_coin";
				LogSceneGoldDiag($"coin[{i}] name_done");
				LogSceneGoldDiag($"coin[{i}] set_frame_begin");
				gameEntity.SetFrame(ref frame);
				LogSceneGoldDiag($"coin[{i}] set_frame_done");
				LogSceneGoldDiag($"coin[{i}] mobility_begin");
				gameEntity.SetMobility(GameEntity.Mobility.Stationary);
				LogSceneGoldDiag($"coin[{i}] mobility_done");
				float burstSpeed = 0.75f;
				simulatedCoins.Add(new SceneGoldCoinSim
				{
					Entity = gameEntity,
					Frame = frame,
					Velocity = new Vec3((float)Math.Cos(angle) * burstSpeed, (float)Math.Sin(angle) * burstSpeed, 1.25f, -1f),
					AngularVelocity = new Vec3(5.5f + i * 0.17f, 3.8f + i * 0.11f, 6.2f + i * 0.13f, -1f),
					UsesNativeCoinScale = false,
					UsesNativeIngotScale = false,
					Settled = false
				});
				LogSceneGoldDiag($"coin[{i}] sim_add_done count={simulatedCoins.Count}");
			}
			LogSceneGoldDiag($"create_loop_done count={simulatedCoins.Count}");
			return simulatedCoins.Count > 0 ? simulatedCoins : null;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Scene gold custom prefab spawn failed: " + ex.Message);
			return null;
		}
	}

	private static GameEntity TryInstantiateSceneGoldCoinPrefab(Scene scene)
	{
		try
		{
			return GameEntity.Instantiate(scene, SceneGoldCustomVisualPrefab, callScriptCallbacks: false, createPhysics: false);
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"prefab_instantiate_exception prefab={SceneGoldCustomVisualPrefab} message={ex.Message}");
			return null;
		}
	}

	private static ItemObject TryGetSceneGoldCoinItemObject()
	{
		try
		{
			var objectManager = Game.Current?.ObjectManager;
			ItemObject customItem = objectManager?.GetObject<ItemObject>(SceneGoldCustomItemId);
			if (customItem != null)
			{
				LogSceneGoldDiag($"item_lookup_hit id={SceneGoldCustomItemId}");
				return customItem;
			}
			ItemObject fallbackItem = objectManager?.GetObject<ItemObject>(SceneGoldFallbackNativeItemId);
			if (fallbackItem != null)
			{
				LogSceneGoldDiag($"item_lookup_fallback id={SceneGoldFallbackNativeItemId}");
				return fallbackItem;
			}
			return null;
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"item_lookup_exception ids={SceneGoldCustomItemId},{SceneGoldFallbackNativeItemId} message={ex.Message}");
			return null;
		}
	}

	private static List<SceneGoldVisualPiece> BuildSceneGoldVisualPieces(int goldAmount)
	{
		try
		{
			if (goldAmount <= 0)
			{
				return null;
			}
			var objectManager = Game.Current?.ObjectManager;
			ItemObject ingotItem = objectManager?.GetObject<ItemObject>(SceneGoldCustomIngotItemId);
			ItemObject coinItem = objectManager?.GetObject<ItemObject>(SceneGoldCustomItemId) ?? objectManager?.GetObject<ItemObject>(SceneGoldFallbackNativeItemId);
			if (ingotItem == null && coinItem == null)
			{
				return null;
			}
			List<SceneGoldVisualPiece> pieces = new List<SceneGoldVisualPiece>();
			int remainingGold = goldAmount;
			if (ingotItem != null)
			{
				int ingotCount = Math.Min(remainingGold / SceneGoldIngotDenarValue, SceneGoldMaxVisualCoins);
				for (int i = 0; i < ingotCount; i++)
				{
					pieces.Add(new SceneGoldVisualPiece
					{
						Item = ingotItem,
						DenarValue = SceneGoldIngotDenarValue,
						UsesNativeCoinScale = false,
						UsesNativeIngotScale = true
					});
				}
				remainingGold -= ingotCount * SceneGoldIngotDenarValue;
			}
			if (remainingGold > 0 && coinItem != null && pieces.Count < SceneGoldMaxVisualCoins)
			{
				int coinCount = Math.Max(1, (remainingGold + SceneGoldCoinDenarValue - 1) / SceneGoldCoinDenarValue);
				coinCount = Math.Min(coinCount, SceneGoldMaxVisualCoins - pieces.Count);
				for (int i = 0; i < coinCount; i++)
				{
					pieces.Add(new SceneGoldVisualPiece
					{
						Item = coinItem,
						DenarValue = SceneGoldCoinDenarValue,
						UsesNativeCoinScale = coinItem.StringId == SceneGoldCustomItemId,
						UsesNativeIngotScale = false
					});
				}
			}
			LogSceneGoldDiag($"visual_pieces amount={goldAmount} count={pieces.Count} ingotItem={(ingotItem?.StringId ?? "null")} coinItem={(coinItem?.StringId ?? "null")}");
			return pieces.Count > 0 ? pieces : null;
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"visual_pieces_exception amount={goldAmount} message={ex.Message}");
			return null;
		}
	}

	private List<SceneGoldCoinSim> TryCreateSceneGoldItemDrops(Vec3 origin, List<SceneGoldVisualPiece> visualPieces)
	{
		Mission mission = base.Mission;
		if (mission == null || visualPieces == null || visualPieces.Count == 0)
		{
			return null;
		}
		int coinCount = Math.Min(visualPieces.Count, SceneGoldMaxVisualCoins);
		List<SceneGoldCoinSim> coins = new List<SceneGoldCoinSim>(coinCount);
		for (int i = 0; i < coinCount; i++)
		{
			SceneGoldVisualPiece piece = visualPieces[i];
			ItemObject coinItem = piece?.Item;
			if (coinItem == null)
			{
				continue;
			}
			float angle = (float)(Math.PI * 2.0 * SceneGoldScatterUnit(i, 17));
			float offsetRadius = SceneGoldBurstSpawnRadius * (float)Math.Sqrt(SceneGoldScatterUnit(i, 31));
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = new Vec3(origin.x + (float)Math.Cos(angle) * offsetRadius, origin.y + (float)Math.Sin(angle) * offsetRadius, origin.z + (SceneGoldScatterUnit(i, 43) - 0.5f) * 0.18f, -1f);
			frame.rotation.RotateAboutSide(0.35f + SceneGoldScatterUnit(i, 53) * 1.7f);
			frame.rotation.RotateAboutUp(angle);
			frame.rotation.RotateAboutForward(SceneGoldScatterUnit(i, 59) * (float)Math.PI);
			if (piece.UsesNativeCoinScale)
			{
				frame.rotation.ApplyScaleLocal(SceneGoldNativeCoinVisualScale);
			}
			if (piece.UsesNativeIngotScale)
			{
				frame.rotation.ApplyScaleLocal(SceneGoldNativeIngotVisualScale);
			}
			try
			{
				MissionWeapon missionWeapon = new MissionWeapon(coinItem, null, null, 1);
				LogSceneGoldDiag($"item_coin[{i}] spawn_begin item={coinItem.StringId} value={piece.DenarValue} pos={FormatSceneGoldVec(frame.origin)}");
				GameEntity gameEntity = mission.SpawnWeaponWithNewEntity(ref missionWeapon, Mission.WeaponSpawnFlags.WithPhysics | Mission.WeaponSpawnFlags.CannotBePickedUp, frame);
				if (gameEntity == null)
				{
					LogSceneGoldDiag($"item_coin[{i}] spawn_null");
					continue;
				}
				gameEntity.Name = piece.UsesNativeIngotScale ? "animusforge_scene_denar_ingot_item" : "animusforge_scene_denar_coin_item";
				TryTintSceneGoldEntity(gameEntity);
				TryPrepareSceneGoldEntityForManualBurst(gameEntity);
				TryPlaySceneGoldCoinSound(frame.origin, $"drop_coin_{i}");
				float horizontalVelocity = SceneGoldBurstHorizontalVelocityMin + (SceneGoldBurstHorizontalVelocityMax - SceneGoldBurstHorizontalVelocityMin) * SceneGoldScatterUnit(i, 89);
				float verticalVelocity = SceneGoldBurstVerticalVelocityMin + (SceneGoldBurstVerticalVelocityMax - SceneGoldBurstVerticalVelocityMin) * SceneGoldScatterUnit(i, 97);
				Vec3 velocity = new Vec3((float)Math.Cos(angle) * horizontalVelocity, (float)Math.Sin(angle) * horizontalVelocity, verticalVelocity, -1f);
				coins.Add(new SceneGoldCoinSim
				{
					Entity = gameEntity,
					Frame = frame,
					Velocity = velocity,
					AngularVelocity = new Vec3(9f + SceneGoldScatterUnit(i, 101) * 9f, 8f + SceneGoldScatterUnit(i, 103) * 10f, 10f + SceneGoldScatterUnit(i, 107) * 12f, -1f),
					UsesNativeCoinScale = piece.UsesNativeCoinScale,
					UsesNativeIngotScale = piece.UsesNativeIngotScale,
					Settled = false
				});
			}
			catch (Exception ex)
			{
				LogSceneGoldDiag($"item_coin[{i}] spawn_exception message={ex.Message}");
			}
		}
		return coins.Count > 0 ? coins : null;
	}

	private static float SceneGoldScatterUnit(int index, int salt)
	{
		unchecked
		{
			uint hash = (uint)(index + 1) * 747796405u + (uint)(salt + 37) * 2891336453u;
			hash ^= hash >> 16;
			hash *= 2246822519u;
			hash ^= hash >> 13;
			hash *= 3266489917u;
			hash ^= hash >> 16;
			return (hash & 0xFFFFu) / 65535f;
		}
	}

	private static void TryPlaySceneGoldCoinSound(Vec3 position, string reason)
	{
		try
		{
			if (SoundEvent.PlaySound2D(SceneGoldPrimarySoundEvent))
			{
				return;
			}
			if (!SoundEvent.PlaySound2D(SceneGoldFallbackSoundEvent))
			{
				LogSceneGoldDiag($"sound_failed reason={reason} pos={FormatSceneGoldVec(position)} primary={SceneGoldPrimarySoundEvent} fallback={SceneGoldFallbackSoundEvent}");
			}
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"sound_exception reason={reason} message={ex.Message}");
		}
	}

	private static void TryTintSceneGoldEntity(GameEntity entity)
	{
		try
		{
			if (entity == null)
			{
				return;
			}
			entity.SetVectorArgument(1f, 0.72f, 0.08f, 1f);
			for (int i = 0; i < 8; i++)
			{
				MetaMesh metaMesh = entity.GetMetaMesh(i);
				if (metaMesh == null)
				{
					continue;
				}
				metaMesh.SetFactor1(SceneGoldTintColor);
				metaMesh.SetFactor2(SceneGoldTintColor);
				metaMesh.SetGlossMultiplier(1.8f);
			}
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"tint_exception message={ex.Message}");
		}
	}

	private static void TryPrepareSceneGoldEntityForManualBurst(GameEntity entity)
	{
		try
		{
			if (entity == null)
			{
				return;
			}
			entity.SetPhysicsState(false, setChildren: true);
			entity.SetMobility(GameEntity.Mobility.Stationary);
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"manual_burst_prepare_exception message={ex.Message}");
		}
	}

	private static void SetSceneGoldCoinRestingFrame(ref MatrixFrame frame, int coinIndex, bool usesNativeCoinScale, bool usesNativeIngotScale)
	{
		Vec3 origin = frame.origin;
		frame = MatrixFrame.Identity;
		frame.origin = origin;
		frame.rotation.RotateAboutUp((float)(Math.PI * 2.0 * SceneGoldScatterUnit(coinIndex, 131)));
		if (usesNativeCoinScale)
		{
			frame.rotation.ApplyScaleLocal(SceneGoldNativeCoinVisualScale);
		}
		if (usesNativeIngotScale)
		{
			frame.rotation.ApplyScaleLocal(SceneGoldNativeIngotVisualScale);
		}
	}

	private void TryMaintainSceneGoldCoinMotion(float dt)
	{
		try
		{
			if (_sceneGoldDrops.Count == 0 || base.Mission?.Scene == null)
			{
				return;
			}
			float tick = Math.Min(Math.Max(dt, 0f), 0.05f);
			if (tick <= 0f)
			{
				return;
			}
			bool shouldLogTick = _sceneGoldMotionDiagTicks < 8;
			if (shouldLogTick)
			{
				LogSceneGoldDiag($"motion_tick_begin tickIndex={_sceneGoldMotionDiagTicks} dt={dt:0.####} drops={_sceneGoldDrops.Count}");
			}
			Scene scene = base.Mission.Scene;
			foreach (SceneGoldDrop drop in _sceneGoldDrops)
			{
				if (drop?.SimulatedCoins == null || drop.UsesNativeItemPhysics)
				{
					continue;
				}
				for (int coinIndex = 0; coinIndex < drop.SimulatedCoins.Count; coinIndex++)
				{
					SceneGoldCoinSim coin = drop.SimulatedCoins[coinIndex];
					if (coin == null || coin.Settled || coin.Entity == null)
					{
						continue;
					}
					MatrixFrame frame = coin.Frame;
					coin.Velocity.z -= SceneGoldSimulatedGravity * tick;
					frame.origin += coin.Velocity * tick;
					float groundZ = frame.origin.z;
					try
					{
						groundZ = scene.GetGroundHeightAtPosition(frame.origin) + SceneGoldGroundOffset;
					}
					catch
					{
					}
					if (frame.origin.z <= groundZ)
					{
						frame.origin.z = groundZ;
						if (coin.Velocity.z < -0.45f)
						{
							coin.Velocity.z = -coin.Velocity.z * 0.28f;
							coin.Velocity.x *= 0.72f;
							coin.Velocity.y *= 0.72f;
						}
						else
						{
							coin.Velocity.z = 0f;
							coin.Velocity.x *= 0.84f;
							coin.Velocity.y *= 0.84f;
						}
					}
					float damping = (float)Math.Pow(0.18f, tick);
					coin.Velocity.x *= damping;
					coin.Velocity.y *= damping;
					coin.AngularVelocity.x *= damping;
					coin.AngularVelocity.y *= damping;
					coin.AngularVelocity.z *= damping;
					frame.rotation.RotateAboutSide(coin.AngularVelocity.x * tick);
					frame.rotation.RotateAboutForward(coin.AngularVelocity.y * tick);
					frame.rotation.RotateAboutUp(coin.AngularVelocity.z * tick);
					float horizontalSpeedSquared = coin.Velocity.x * coin.Velocity.x + coin.Velocity.y * coin.Velocity.y;
					bool shouldSettle = frame.origin.z <= groundZ + 0.001f && horizontalSpeedSquared < 0.0008f && Math.Abs(coin.Velocity.z) < 0.02f;
					if (shouldSettle)
					{
						coin.Velocity = Vec3.Zero;
						coin.AngularVelocity = Vec3.Zero;
						SetSceneGoldCoinRestingFrame(ref frame, coinIndex, coin.UsesNativeCoinScale, coin.UsesNativeIngotScale);
						coin.Settled = true;
					}
					coin.Frame = frame;
					if (shouldLogTick && (coinIndex < 5 || coinIndex % 20 == 0))
					{
						LogSceneGoldDiag($"motion_set_frame_begin tickIndex={_sceneGoldMotionDiagTicks} dropAgent={drop.AgentIndex} coin={coinIndex} pos={FormatSceneGoldVec(frame.origin)} settled={coin.Settled}");
					}
					coin.Entity.SetFrame(ref frame);
					if (shouldLogTick && (coinIndex < 5 || coinIndex % 20 == 0))
					{
						LogSceneGoldDiag($"motion_set_frame_done tickIndex={_sceneGoldMotionDiagTicks} dropAgent={drop.AgentIndex} coin={coinIndex}");
					}
				}
			}
			if (shouldLogTick)
			{
				LogSceneGoldDiag($"motion_tick_done tickIndex={_sceneGoldMotionDiagTicks}");
			}
			_sceneGoldMotionDiagTicks++;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining scene gold coin motion failed: " + ex.Message);
		}
	}

	private void TryHandleSceneGoldPickupInput()
	{
		try
		{
			if (_sceneGoldDrops.Count == 0 || Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return;
			}
			if (HotkeyInputGuard.IsTextInputFocused() || IsPlayerInteractionInputSuppressed())
			{
				return;
			}
			if (!Input.IsKeyPressed(GetConfiguredSceneGoldPickupKey()))
			{
				return;
			}
			SceneGoldDrop nearestDrop = null;
			float nearestDistanceSquared = float.MaxValue;
			Vec2 playerPosition = Agent.Main.Position.AsVec2;
			foreach (SceneGoldDrop drop in _sceneGoldDrops)
			{
				if (drop == null)
				{
					continue;
				}
				float distanceSquared = GetSceneGoldDropDistanceSquared(drop, playerPosition);
				if (distanceSquared <= SceneGoldPickupDistanceSquared && distanceSquared < nearestDistanceSquared)
				{
					nearestDrop = drop;
					nearestDistanceSquared = distanceSquared;
				}
			}
			if (nearestDrop == null)
			{
				return;
			}
			int pickedGold = TryCollectSceneGoldDrop(nearestDrop);
			if (pickedGold > 0)
			{
				TryPlaySceneGoldCoinSound(nearestDrop.Position, "pickup");
			}
			RemoveSceneGoldDrop(nearestDrop, removeEntity: true);
			if (pickedGold > 0)
			{
				AnimusForgeQuickInfo.Show($"你拾取了 {pickedGold} 第纳尔。");
				Logger.Log("SceneTaunt", $"Scene gold drop picked. AgentIndex={nearestDrop.AgentIndex}, Amount={pickedGold}, HeroDrop={nearestDrop.IsHeroDrop}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling scene gold pickup failed: " + ex.Message);
		}
	}

	private static float GetSceneGoldDropDistanceSquared(SceneGoldDrop drop, Vec2 playerPosition)
	{
		if (drop == null)
		{
			return float.MaxValue;
		}
		float nearestDistanceSquared = drop.Position.AsVec2.DistanceSquared(playerPosition);
		if (drop.SimulatedCoins == null)
		{
			return nearestDistanceSquared;
		}
		foreach (SceneGoldCoinSim coin in drop.SimulatedCoins)
		{
			if (coin == null)
			{
				continue;
			}
			float distanceSquared = coin.Frame.origin.AsVec2.DistanceSquared(playerPosition);
			if (distanceSquared < nearestDistanceSquared)
			{
				nearestDistanceSquared = distanceSquared;
			}
		}
		return nearestDistanceSquared;
	}

	private int TryCollectSceneGoldDrop(SceneGoldDrop drop)
	{
		try
		{
			if (drop == null || Hero.MainHero == null)
			{
				return 0;
			}
			if (drop.UsesLocationSettlementPool)
			{
				return TryCollectFixedSettlementSceneGold(drop);
			}
			if (drop.IsHeroDrop)
			{
				Hero sourceHero = drop.SourceHero;
				if (sourceHero == null)
				{
					return 0;
				}
				int amount = GetHeroSceneGoldPickupAmount(sourceHero);
				if (amount <= 0)
				{
					amount = Math.Max(0, drop.FixedSettlementGold);
				}
				if (amount <= 0)
				{
					return 0;
				}
				int heroGold = Math.Max(0, sourceHero.Gold);
				int heroPaidGold = Math.Min(amount, heroGold);
				int settlementPaidGold = 0;
				if (heroPaidGold > 0)
				{
					sourceHero.ChangeHeroGold(-heroPaidGold);
				}
				int remainingGold = amount - heroPaidGold;
				if (remainingGold > 0)
				{
					Settlement heroSettlement = drop.SourceSettlement ?? Settlement.CurrentSettlement;
					int availableSettlementGold = Math.Max(0, heroSettlement?.SettlementComponent?.Gold ?? 0);
					settlementPaidGold = Math.Min(remainingGold, availableSettlementGold);
					if (settlementPaidGold > 0)
					{
						heroSettlement.SettlementComponent.ChangeGold(-settlementPaidGold);
					}
				}
				int heroPickedGold = heroPaidGold + settlementPaidGold;
				if (heroPickedGold <= 0)
				{
					return 0;
				}
				Hero.MainHero.ChangeHeroGold(heroPickedGold);
				LogSceneGoldDiag($"collect_hero amount={heroPickedGold} heroPaid={heroPaidGold} settlementPaid={settlementPaidGold} hero={sourceHero.StringId}");
				return heroPickedGold;
			}
			return TryCollectFixedSettlementSceneGold(drop);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Collecting scene gold drop failed: " + ex.Message);
			return 0;
		}
	}

	private static int TryCollectFixedSettlementSceneGold(SceneGoldDrop drop)
	{
		Settlement settlement = drop?.SourceSettlement ?? Settlement.CurrentSettlement;
		if (settlement?.SettlementComponent == null)
		{
			return 0;
		}
		int availableGold = Math.Max(0, settlement.SettlementComponent.Gold);
		int pickedGold = Math.Min(Math.Max(0, drop.FixedSettlementGold), availableGold);
		if (pickedGold <= 0)
		{
			return 0;
		}
		settlement.SettlementComponent.ChangeGold(-pickedGold);
		Hero.MainHero.ChangeHeroGold(pickedGold);
		return pickedGold;
	}

	private static int GetHeroSceneGoldPickupAmount(Hero sourceHero)
	{
		try
		{
			return Math.Max(0, (int)Math.Floor((double)Math.Max(0, sourceHero?.Gold ?? 0) * 0.3));
		}
		catch
		{
			return 0;
		}
	}

	private static SceneGoldSettlementPoolLocation GetCurrentSceneGoldSettlementPoolLocation()
	{
		try
		{
			string locationId = (CampaignMission.Current?.Location?.StringId ?? "").Trim();
			if (string.Equals(locationId, "tavern", StringComparison.OrdinalIgnoreCase))
			{
				return SceneGoldSettlementPoolLocation.Tavern;
			}
			if (string.Equals(locationId, "lordshall", StringComparison.OrdinalIgnoreCase) || string.Equals(locationId, "lords_hall", StringComparison.OrdinalIgnoreCase))
			{
				return SceneGoldSettlementPoolLocation.LordHall;
			}
		}
		catch
		{
		}
		return SceneGoldSettlementPoolLocation.None;
	}

	private static int GetSceneGoldSettlementPoolAmount(Settlement settlement, SceneGoldSettlementPoolLocation location)
	{
		try
		{
			int settlementGold = Math.Max(0, settlement?.SettlementComponent?.Gold ?? 0);
			double poolRatio = location switch
			{
				SceneGoldSettlementPoolLocation.Tavern => SceneGoldTavernSettlementPoolRatio,
				SceneGoldSettlementPoolLocation.LordHall => SceneGoldLordHallSettlementPoolRatio,
				_ => 1.0
			};
			return Math.Max(0, (int)Math.Floor(settlementGold * poolRatio));
		}
		catch
		{
			return 0;
		}
	}

	private static int GetHeroSceneGoldSettlementFallbackAmount(Settlement settlement)
	{
		try
		{
			return Math.Max(0, (int)Math.Floor((double)Math.Max(0, settlement?.SettlementComponent?.Gold ?? 0) * 0.3));
		}
		catch
		{
			return 0;
		}
	}

	private int GetSceneGoldSettlementShareForAgent(Settlement settlement, Agent affectedAgent, SceneGoldSettlementPoolLocation location)
	{
		CaptureSceneGoldShareSnapshot(settlement, affectedAgent, location);
		return _sceneGoldSettlementShareByAgentIndex.TryGetValue(affectedAgent?.Index ?? -1, out int share) ? Math.Max(0, share) : 0;
	}

	private void CaptureSceneGoldShareSnapshot(Settlement settlement, Agent affectedAgent, SceneGoldSettlementPoolLocation location)
	{
		string settlementId = settlement?.StringId ?? "";
		if (_sceneGoldShareSnapshotCaptured && string.Equals(_sceneGoldShareSnapshotSettlementId, settlementId, StringComparison.OrdinalIgnoreCase) && _sceneGoldShareSnapshotLocation == location)
		{
			return;
		}
		_sceneGoldShareSnapshotCaptured = true;
		_sceneGoldShareSnapshotSettlementId = settlementId;
		_sceneGoldShareSnapshotLocation = location;
		_sceneGoldSettlementShareByAgentIndex.Clear();
		int settlementGold = Math.Max(0, settlement?.SettlementComponent?.Gold ?? 0);
		int poolGold = GetSceneGoldSettlementPoolAmount(settlement, location);
		if (poolGold <= 0)
		{
			return;
		}
		List<int> ordinaryAgentIndices = new List<int>();
		List<int> functionalAgentIndices = new List<int>();
		foreach (Agent agent in Mission.Current?.Agents ?? Enumerable.Empty<Agent>())
		{
			AddAgentToSceneGoldSnapshot(agent, ordinaryAgentIndices, functionalAgentIndices, location);
		}
		AddAgentToSceneGoldSnapshot(affectedAgent, ordinaryAgentIndices, functionalAgentIndices, location);
		if (location == SceneGoldSettlementPoolLocation.None)
		{
			AssignSceneGoldSnapshotShares(ordinaryAgentIndices, poolGold / 2);
			AssignSceneGoldSnapshotShares(functionalAgentIndices, poolGold - poolGold / 2);
		}
		else
		{
			AssignSceneGoldSnapshotShares(ordinaryAgentIndices, poolGold);
		}
		Logger.Log("SceneTaunt", $"Captured scene gold share snapshot. Settlement={settlementId}, LocationPool={location}, SettlementGold={settlementGold}, PoolGold={poolGold}, Eligible={ordinaryAgentIndices.Count}, Functional={functionalAgentIndices.Count}");
	}

	private void AddAgentToSceneGoldSnapshot(Agent agent, List<int> ordinaryAgentIndices, List<int> functionalAgentIndices, SceneGoldSettlementPoolLocation location)
	{
		if (agent == null || agent.IsMainAgent || !agent.IsHuman || agent.Index < 0)
		{
			return;
		}
		CharacterObject characterObject = agent.Character as CharacterObject;
		if (characterObject == null || SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))
		{
			return;
		}
		if (location == SceneGoldSettlementPoolLocation.None && characterObject.IsHero)
		{
			return;
		}
		if (location == SceneGoldSettlementPoolLocation.LordHall && !SceneTauntBehavior.IsSceneLordTauntTarget(characterObject.HeroObject))
		{
			return;
		}
		List<int> targetList = location == SceneGoldSettlementPoolLocation.None && IsSceneGoldFunctionalServiceAgent(agent, characterObject) ? functionalAgentIndices : ordinaryAgentIndices;
		if (!targetList.Contains(agent.Index))
		{
			targetList.Add(agent.Index);
		}
	}

	private void AssignSceneGoldSnapshotShares(List<int> agentIndices, int poolGold)
	{
		if (agentIndices == null || agentIndices.Count == 0 || poolGold <= 0)
		{
			return;
		}
		agentIndices.Sort();
		int baseShare = poolGold / agentIndices.Count;
		int remainder = poolGold % agentIndices.Count;
		for (int i = 0; i < agentIndices.Count; i++)
		{
			_sceneGoldSettlementShareByAgentIndex[agentIndices[i]] = baseShare + (i < remainder ? 1 : 0);
		}
	}

	private bool IsSceneGoldFunctionalServiceAgent(Agent agent, CharacterObject characterObject)
	{
		try
		{
			if (characterObject == null || characterObject.IsHero)
			{
				return false;
			}
			if (RewardSystemBehavior.Instance != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(characterObject, out var kind) && kind != RewardSystemBehavior.SettlementMerchantKind.None)
			{
				return true;
			}
			LocationCharacter locationCharacter = TryGetSceneGoldLocationCharacter(agent);
			if (string.Equals(locationCharacter?.SpecialTargetTag ?? "", "sp_barber", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			switch (characterObject.Occupation)
			{
			case Occupation.GoodsTrader:
			case Occupation.Weaponsmith:
			case Occupation.Armorer:
			case Occupation.HorseTrader:
			case Occupation.Blacksmith:
			case Occupation.Tavernkeeper:
			case Occupation.TavernWench:
			case Occupation.TavernGameHost:
			case Occupation.RansomBroker:
			case Occupation.ArenaMaster:
			case Occupation.ShopWorker:
			case Occupation.Musician:
			case Occupation.ShipWright:
				return true;
			default:
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Checking scene gold functional service agent failed: " + ex.Message);
			return false;
		}
	}

	private static LocationCharacter TryGetSceneGoldLocationCharacter(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return null;
			}
			return LocationComplex.Current?.FindCharacter(agent) ?? CampaignMission.Current?.Location?.GetLocationCharacter(agent.Origin);
		}
		catch
		{
			return null;
		}
	}

	private void RegisterSceneGoldEligibleAgent(Agent agent, string reason)
	{
		try
		{
			if (agent == null || agent.IsMainAgent || !agent.IsHuman)
			{
				return;
			}
			if (_sceneGoldEligibleAgentIndices.Add(agent.Index))
			{
				LogSceneGoldDiag($"eligible_add idx={agent.Index} name={agent.Name} reason={reason}");
			}
		}
		catch (Exception ex)
		{
			LogSceneGoldDiag($"eligible_add_exception reason={reason} message={ex.Message}");
		}
	}

	private bool IsSceneGoldEligibleDropAgent(Agent agent)
	{
		try
		{
			return agent != null && _sceneGoldEligibleAgentIndices.Contains(agent.Index);
		}
		catch
		{
			return false;
		}
	}

	private static void LogSceneGoldDiag(string message)
	{
		try
		{
			Logger.LogVerbose("SceneGoldDiag", "scene_gold_diag:" + (message ?? ""), () => message ?? "", 2.0);
		}
		catch
		{
		}
	}

	private static string FormatSceneGoldVec(Vec3 vec)
	{
		return $"{vec.x:0.###},{vec.y:0.###},{vec.z:0.###}";
	}

	private static InputKey GetConfiguredSceneGoldPickupKey()
	{
		try
		{
			string configuredKey = DuelSettings.GetSettings()?.SceneTauntGoldPickupKey;
			if (!string.IsNullOrWhiteSpace(configuredKey) && Enum.TryParse<InputKey>(configuredKey.Trim().ToUpperInvariant(), out var result))
			{
				return result;
			}
		}
		catch
		{
		}
		return InputKey.F;
	}

	private void RemoveSceneGoldDrop(SceneGoldDrop drop, bool removeEntity)
	{
		try
		{
			LogSceneGoldDiag($"remove_enter null={drop == null} removeEntity={removeEntity} entityCount={drop?.Entities?.Count ?? -1}");
			if (drop == null)
			{
				return;
			}
			if (removeEntity)
			{
				foreach (GameEntity entity in drop.Entities ?? Enumerable.Empty<GameEntity>())
				{
					if (entity == null)
					{
						continue;
					}
					try
					{
						LogSceneGoldDiag("remove_entity_begin");
						entity.Remove(95);
						LogSceneGoldDiag("remove_entity_done");
					}
					catch
					{
						LogSceneGoldDiag("remove_entity_exception_ignored");
					}
				}
				if (drop.Entity != null && (drop.Entities == null || !drop.Entities.Contains(drop.Entity)))
				{
					try
					{
						LogSceneGoldDiag("remove_primary_entity_begin");
						drop.Entity.Remove(95);
						LogSceneGoldDiag("remove_primary_entity_done");
					}
					catch
					{
						LogSceneGoldDiag("remove_primary_entity_exception_ignored");
					}
				}
				drop.Entities?.Clear();
				drop.SimulatedCoins?.Clear();
				drop.Entity = null;
			}
			LogSceneGoldDiag("remove_drop_list_begin");
			_sceneGoldDrops.Remove(drop);
			LogSceneGoldDiag($"remove_drop_list_done drops={_sceneGoldDrops.Count}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Removing scene gold drop failed: " + ex.Message);
		}
	}

	private void ClearSceneGoldDrops(bool removeEntities)
	{
		foreach (SceneGoldDrop drop in _sceneGoldDrops.ToList())
		{
			RemoveSceneGoldDrop(drop, removeEntities);
		}
		_sceneGoldDrops.Clear();
		_sceneGoldDropAgentIndices.Clear();
		_sceneGoldEligibleAgentIndices.Clear();
		_sceneGoldSettlementShareByAgentIndex.Clear();
		_sceneGoldShareSnapshotCaptured = false;
		_sceneGoldShareSnapshotSettlementId = "";
		_sceneGoldShareSnapshotLocation = SceneGoldSettlementPoolLocation.None;
	}

	private void TryApplyNativeAlleyNpcKnockdownConsequences(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			if (!IsNativeAlleyFightKnockdownContext(affectedAgent, affectorAgent, agentState))
			{
				return;
			}
			if (_playerAgentIndices.Contains(affectedAgent.Index) || !_penalizedArmedKnockdownAgentIndices.Add(affectedAgent.Index))
			{
				return;
			}
			CharacterObject characterObject = affectedAgent.Character as CharacterObject;
			ApplyPerNpcKnockdownConsequences(affectedAgent, characterObject, affectedAgent.Name?.ToString());
			TryRecordPlayerSceneConflictRecentAction(affectedAgent, affectorAgent, agentState == AgentState.Killed ? "killed" : "unconscious", "native_alley_knockdown");
			Logger.Log("SceneTaunt", $"Applied native alley criminal knockdown consequences. Victim={affectedAgent.Name}, Affector={affectorAgent?.Name}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying native alley knockdown consequences failed: " + ex.Message);
		}
	}

	private bool IsNativeAlleyFightKnockdownContext(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		if (_conflictActive || affectedAgent == null || !affectedAgent.IsHuman)
		{
			return false;
		}
		if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
		{
			return false;
		}
		CharacterObject characterObject = affectedAgent.Character as CharacterObject;
		if (!IsSettlementCriminalConflictTarget(characterObject?.HeroObject, characterObject))
		{
			return false;
		}
		CampaignAgentComponent component = affectedAgent.GetComponent<CampaignAgentComponent>();
		if (component?.AgentNavigator?.MemberOfAlley == null)
		{
			return false;
		}
		_fightHandler = _fightHandler ?? Mission.Current?.GetMissionBehavior<MissionFightHandler>();
		if (_fightHandler == null || !_fightHandler.IsThereActiveFight())
		{
			return false;
		}
		return IsNativeAlleyPlayerSideAgent(affectorAgent);
	}

	private static bool IsNativeAlleyPlayerSideAgent(Agent agent)
	{
		if (agent == null || !agent.IsHuman)
		{
			return false;
		}
		if (agent == Agent.Main)
		{
			return true;
		}
		if (IsSetsSelectedEntryFollower(agent))
		{
			return true;
		}
		Agent main = Agent.Main;
		if (main == null || agent.Team == null || main.Team == null || agent.Team != main.Team)
		{
			return false;
		}
		CharacterObject characterObject = agent.Character as CharacterObject;
		Hero hero = characterObject?.HeroObject;
		return hero != null && !IsSettlementCriminalConflictTarget(hero, characterObject);
	}

	protected override void OnEndMission()
	{
		FlushPlayerSceneConflictMajorMaterial();
		ClearSceneGoldDrops(removeEntities: true);
		ClearRuntimeState();
	}

	internal bool CanStartConflict(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		_fightHandler = _fightHandler ?? Mission.Current?.GetMissionBehavior<MissionFightHandler>();
		if (SettlementEntryTroopSelectionBehavior.IsSetsConflictProxyActiveForExternal(Mission.Current))
		{
			return false;
		}
		if (_conflictActive || Mission.Current == null || _fightHandler == null || Settlement.CurrentSettlement == null)
		{
			return false;
		}
		if (_fightHandler.IsThereActiveFight())
		{
			return false;
		}
		Agent agent = ResolveTargetAgent(targetHero, targetCharacter, targetAgentIndex);
		return agent != null && agent.IsHuman && agent.IsActive() && !IsPlayerAlignedConflictAgent(agent);
	}

	private bool ShouldPrioritizeUnarmedVillageBrawlOverSets(Agent attacker, Agent target, bool attackerUsedRealWeapon)
	{
		try
		{
			if (attacker == null
				|| target == null
				|| !target.IsHuman
				|| !target.IsActive()
				|| target.IsMainAgent
				|| SettlementEntryTroopSelectionBehavior.IsSetsConflictProxyActiveForExternal(base.Mission))
			{
				return false;
			}
			if (_conflictActive)
			{
				bool attackerOnPlayerSide = _playerAgentIndices.Contains(attacker.Index);
				bool attackerOnOpponentSide = _opponentAgentIndices.Contains(attacker.Index);
				bool targetOnPlayerSide = _playerAgentIndices.Contains(target.Index);
				bool targetOnOpponentSide = _opponentAgentIndices.Contains(target.Index);
				return _openedAsUnarmedBrawl
					&& !_armedConflict
					&& ((attackerOnPlayerSide && targetOnOpponentSide) || (attackerOnOpponentSide && targetOnPlayerSide));
			}
			if (attackerUsedRealWeapon
				|| (!attacker.IsMainAgent && attacker != Agent.Main)
				|| IsSetsSelectedEntryFollower(target)
				|| !SceneTauntBehavior.IsPeaceSceneConflictEnabled())
			{
				return false;
			}
			CharacterObject targetCharacter = target.Character as CharacterObject;
			Hero targetHero = targetCharacter?.HeroObject;
			if (IsPlayerProtectedSceneAttackAgent(target) || SceneTauntBehavior.IsChildSceneProtectedTarget(targetCharacter))
			{
				return false;
			}
			if (IsOwnedSettlementPassiveAttackScene())
			{
				return false;
			}
			if (!IsEligiblePhysicalAttackTarget(targetHero, targetCharacter)
				|| IsSettlementCriminalConflictTarget(targetHero, targetCharacter))
			{
				return false;
			}
			if (IsAuthorityPhysicalAttackTarget(targetHero, targetCharacter))
			{
				return false;
			}
			_fightHandler = _fightHandler ?? base.Mission?.GetMissionBehavior<MissionFightHandler>();
			return base.Mission != null
				&& Settlement.CurrentSettlement != null
				&& _fightHandler != null
				&& !_fightHandler.IsThereActiveFight();
		}
		catch
		{
			return false;
		}
	}

	internal bool TryStartConflict(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey, bool fromVerbalTaunt = false, bool playerUsedWeaponOverride = false)
	{
		try
		{
			long totalStart = StartPerfTimer();
			if (!SceneTauntBehavior.IsPeaceSceneConflictEnabled() && !fromVerbalTaunt)
			{
				return false;
			}
			_fightHandler = _fightHandler ?? Mission.Current?.GetMissionBehavior<MissionFightHandler>();
			if (!CanStartConflict(targetHero, targetCharacter, targetAgentIndex))
			{
				return false;
			}
			Agent agent = ResolveTargetAgent(targetHero, targetCharacter, targetAgentIndex);
			if (agent == null)
			{
				return false;
			}
			bool flag = playerUsedWeaponOverride || IsAgentUsingRealWeapon(Agent.Main);
			bool flag2 = SceneTauntBehavior.IsSoldierSceneTauntTarget(targetCharacter);
			bool flag3 = SceneTauntBehavior.IsSceneLordTauntTarget(targetHero);
			bool flag4 = IsSettlementCriminalConflictTarget(targetHero, targetCharacter);
			bool flag5 = flag4 && CanUseNativeCriminalConflict(agent);
			if (flag5)
			{
				return TryStartNativeCriminalConflict(agent, fromVerbalTaunt ? "scene_taunt_verbal_criminal_conflict" : "scene_taunt_physical_criminal_conflict");
			}
			LogPerfPoint("startConflict.start", $"target={agent.Name} targetIndex={agent.Index} key={targetKey ?? ""} fromVerbal={fromVerbalTaunt} playerUsedWeapon={flag} soldier={flag2} lord={flag3} criminal={flag4}");
			long sectionStart = StartPerfTimer();
			List<Agent> list = CollectPlayerSideAgents();
			List<Agent> list2 = CollectOpponentSideAgents(agent);
			List<Agent> list3 = flag4 ? new List<Agent>() : CollectGuardAgents(list, list2);
			bool selectedFollowerArmedSupport = SetsCityConflictPolicy.ShouldEscalateForSelectedFollowerSupport(
				settlementControlledByPlayer: IsCurrentSettlementControlledByPlayer(),
				hasSelectedEntryFollower: list.Any(IsSetsSelectedEntryFollower));
			LogPerfElapsed("startConflict.collectSides", sectionStart, $"player={list.Count} opponents={list2.Count} guards={list3.Count}");
			if (flag)
			{
				foreach (Agent item in list3)
				{
					SetsCityConflictSide guardSide = ResolveCityConflictSide(item, agent, isTargetEscort: false, armedConflict: true);
					if (guardSide == SetsCityConflictSide.Player)
					{
						AddUniqueAgent(list, item);
					}
					else if (guardSide == SetsCityConflictSide.Opponent)
					{
						AddUniqueAgent(list2, item);
					}
				}
			}
			if (!NormalizeInitialConflictSides(list, list2, agent))
			{
				Logger.Log("SceneTaunt", $"Rejected conflict with ambiguous or missing active opponent. Target={agent.Name}, TargetIndex={agent.Index}");
				return false;
			}
			_conflictActive = true;
			_armedConflict = false;
			_armedConflictOccurredThisConflict = false;
			_armedDefeatOutcomeHandled = false;
			ResetArmedConflictReactionBudget();
			_baseConsequencesApplied = false;
			_appliedCrimeRatingAmount = 0f;
			_activeTargetKey = (targetKey ?? "").Trim();
			_activeTargetName = agent.Name?.ToString() ?? targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC";
			_activeTargetAgentIndex = agent.Index;
			_openedAsUnarmedBrawl = false;
			_openedFromVerbalTaunt = fromVerbalTaunt;
			_suppressSettlementConsequencesForCurrentConflict = flag4;
			_armedDefeatWasCriminalConflict = flag4;
			_playerAgentIndices.Clear();
			_opponentAgentIndices.Clear();
			_guardAgentIndices.Clear();
			_blockedAiWeaponAgentIndices.Clear();
			foreach (Agent item2 in list)
			{
				_playerAgentIndices.Add(item2.Index);
			}
			foreach (Agent item3 in list2)
			{
				_opponentAgentIndices.Add(item3.Index);
				RegisterSceneGoldEligibleAgent(item3, "start_conflict_opponent");
			}
			foreach (Agent item4 in list3)
			{
				_guardAgentIndices.Add(item4.Index);
				if (!list.Contains(item4))
				{
					RegisterSceneGoldEligibleAgent(item4, "start_conflict_guard");
				}
			}
			sectionStart = StartPerfTimer();
			_fightHandler.StartCustomFight(list, list2, dropWeapons: false, isItemUseDisabled: false, OnConflictFinished, float.Epsilon);
			LogPerfElapsed("startConflict.StartCustomFight", sectionStart, $"player={list.Count} opponents={list2.Count}", SceneTauntPerfHeavyStageThresholdMs);
			sectionStart = StartPerfTimer();
			ApplyBaseConsequences(targetCharacter, (flag || flag2 || flag3) ? SceneTauntInitialArmedCrimeAmount : 5f);
			LogPerfElapsed("startConflict.ApplyBaseConsequences", sectionStart);
			bool flag6 = SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement() && _armedCarryoverHandledInThisMission;
			if (flag3)
			{
				ApplyLordSceneFightConsequences(targetHero);
			}
			if (flag3)
			{
				EscalateToArmedConflict("taunted_lord_scene", flag6);
			}
			else if (flag2)
			{
				EscalateToArmedConflict("taunted_soldier", flag6);
			}
			else if (flag)
			{
				EscalateToArmedConflict("player_already_wielding", flag6);
			}
			else if (selectedFollowerArmedSupport)
			{
				EscalateToArmedConflict("sets_owned_settlement_follower_support", flag6);
			}
			else
			{
				PrepareUnarmedConflict();
				if (fromVerbalTaunt)
				{
					TryAppendNpcBehaviorFactForVerbalConflict(targetAgentIndex);
				}
				else
				{
					TryAppendPlayerBehaviorFactForOpenedBrawl(targetHero, targetCharacter, targetAgentIndex);
				}
			}
			LogPerfPoint("startConflict.end", $"target={agent.Name} targetIndex={agent.Index} elapsedMs={GetElapsedPerfMs(totalStart):0.###}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "TryStartConflict failed: " + ex.Message);
			ClearRuntimeState();
			return false;
		}
	}

	private bool TryStartConflictFromPhysicalAttack(Agent targetAgent, bool playerUsedWeapon, string reason)
	{
		try
		{
			long totalStart = StartPerfTimer();
			if (!SceneTauntBehavior.IsPeaceSceneConflictEnabled())
			{
				return false;
			}
			Logger.LogVerbose("SceneTaunt", "attack_timing_try_start:" + (targetAgent?.Index ?? -1), () => $"[AttackTiming] try_start_conflict_from_physical_attack time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} reason={reason} target={(targetAgent?.Name?.ToString() ?? "null")} targetIndex={(targetAgent != null ? targetAgent.Index : -1)} playerUsedWeapon={playerUsedWeapon} conflict={_conflictActive} armed={_armedConflict}", 1.0);
			if (_conflictActive || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			if (IsPlayerProtectedSceneAttackAgent(targetAgent))
			{
				Logger.LogVerbose("SceneTaunt", "player_protected_proxy_attack_suppressed:" + targetAgent.Index, () => $"Suppressed SceneTaunt physical conflict for player-protected agent/proxy. Reason={reason}, Target={targetAgent.Name}, AgentIndex={targetAgent.Index}", 1.0);
				return false;
			}
			if (SettlementEntryTroopSelectionBehavior.ShouldHandlePhysicalAttackForExternal(Mission.Current, Agent.Main, targetAgent, playerUsedWeapon))
			{
				Logger.LogVerbose("SceneTaunt", "sets_entry_suppress_physical_start:" + targetAgent.Index, () => $"Suppressed SceneTaunt physical conflict start because SETS will handle this settlement attack. Reason={reason}", 1.0);
				return false;
			}
			CharacterObject characterObject = targetAgent.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			bool useNativeCriminalConflict = IsSettlementCriminalConflictTarget(hero, characterObject) && CanUseNativeCriminalConflict(targetAgent);
			if (!IsEligiblePhysicalAttackTarget(hero, characterObject))
			{
				return false;
			}
			if (useNativeCriminalConflict)
			{
				if (ShouldSuppressDuplicateNativeCriminalConflict(targetAgent))
				{
					Logger.Log("SceneTaunt", $"Skipped duplicate native criminal conflict redirect. Reason={reason}, Target={targetAgent.Name}, AgentIndex={targetAgent.Index}");
					LogPerfElapsed("physicalAttack.duplicateNativeSuppress", totalStart, $"reason={reason ?? "N/A"} target={targetAgent.Name} targetIndex={targetAgent.Index}");
					return true;
				}
				LogPerfPoint("physicalAttack.native.start", $"reason={reason ?? "N/A"} target={targetAgent.Name} targetIndex={targetAgent.Index} playerUsedWeapon={playerUsedWeapon}");
				long sectionStart = StartPerfTimer();
				try
				{
					Campaign.Current?.ConversationManager?.EndConversation();
				}
				catch
				{
				}
				LogPerfElapsed("physicalAttack.native.EndConversation", sectionStart);
				sectionStart = StartPerfTimer();
				bool startedNativeCriminalConflict = TryStartNativeCriminalConflict(targetAgent, reason + "_native_alley");
				LogPerfElapsed("physicalAttack.native.TryStartNativeCriminalConflict", sectionStart, $"started={startedNativeCriminalConflict}", SceneTauntPerfHeavyStageThresholdMs);
				if (startedNativeCriminalConflict)
				{
					RememberNativeCriminalConflictTarget(targetAgent);
					Logger.Log("SceneTaunt", $"Physical attack bypassed custom scene conflict and redirected to native criminal conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}");
				}
				LogPerfPoint("physicalAttack.native.end", $"reason={reason ?? "N/A"} target={targetAgent.Name} targetIndex={targetAgent.Index} started={startedNativeCriminalConflict} elapsedMs={GetElapsedPerfMs(totalStart):0.###}");
				return startedNativeCriminalConflict;
			}
			long customSectionStart = StartPerfTimer();
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
			LogPerfElapsed("physicalAttack.custom.EndConversation", customSectionStart);
			string sceneTauntTargetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(hero, characterObject, targetAgent.Index);
			customSectionStart = StartPerfTimer();
			bool flag = TryStartConflict(hero, characterObject, targetAgent.Index, sceneTauntTargetKey, fromVerbalTaunt: false, playerUsedWeaponOverride: playerUsedWeapon);
			LogPerfElapsed("physicalAttack.custom.TryStartConflict", customSectionStart, $"started={flag}", SceneTauntPerfHeavyStageThresholdMs);
			Logger.LogVerbose("SceneTaunt", "attack_timing_result:" + (targetAgent?.Index ?? -1), () => $"[AttackTiming] try_start_conflict_result time={Mission.Current?.CurrentTime:0.###} reason={reason} target={(targetAgent?.Name?.ToString() ?? "null")} started={flag} conflict={_conflictActive} armed={_armedConflict}", 1.0);
			if (!flag)
			{
				return false;
			}
			if (useNativeCriminalConflict)
			{
				Logger.Log("SceneTaunt", $"Physical attack redirected to native criminal conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}");
				return true;
			}
			bool flag2 = IsAuthorityPhysicalAttackTarget(hero, characterObject);
			if ((playerUsedWeapon || flag2) && !_armedConflict)
			{
				EscalateToArmedConflict(flag2 ? "player_attacked_authority_in_peace_scene" : "player_started_scene_fight_with_weapon");
			}
			Logger.Log("SceneTaunt", $"Physical attack triggered scene conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}, AuthorityTarget={flag2}");
			LogPerfPoint("physicalAttack.custom.end", $"reason={reason ?? "N/A"} target={targetAgent.Name} targetIndex={targetAgent.Index} started={flag} elapsedMs={GetElapsedPerfMs(totalStart):0.###}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting scene conflict from physical attack failed: " + ex.Message);
			return false;
		}
	}

	private bool ShouldSuppressDuplicateNativeCriminalConflict(Agent targetAgent)
	{
		Mission mission = Mission.Current;
		if (targetAgent == null || targetAgent.Index < 0 || mission == null)
		{
			return false;
		}
		return _lastNativeCriminalConflictTargetAgentIndex == targetAgent.Index && mission.CurrentTime - _lastNativeCriminalConflictMissionTime <= 0.45f;
	}

	private void RememberNativeCriminalConflictTarget(Agent targetAgent)
	{
		Mission mission = Mission.Current;
		if (targetAgent == null || targetAgent.Index < 0 || mission == null)
		{
			return;
		}
		_lastNativeCriminalConflictTargetAgentIndex = targetAgent.Index;
		_lastNativeCriminalConflictMissionTime = mission.CurrentTime;
	}

	private bool TryAddFacingAgentToArmedConflict(Agent targetAgent, string reason)
	{
		try
		{
			if (!_conflictActive || !_armedConflict || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			if (targetAgent == Agent.Main || _playerAgentIndices.Contains(targetAgent.Index) || _guardAgentIndices.Contains(targetAgent.Index))
			{
				return false;
			}
			CharacterObject characterObject = targetAgent.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (IsPlayerProtectedSceneAttackAgent(targetAgent))
			{
				return false;
			}
			if (IsAuthorityPhysicalAttackTarget(hero, characterObject))
			{
				_activeTargetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(hero, characterObject, targetAgent.Index);
				_activeTargetName = targetAgent.Name?.ToString() ?? hero?.Name?.ToString() ?? characterObject?.Name?.ToString() ?? _activeTargetName;
				_activeTargetAgentIndex = targetAgent.Index;
				EnableSettlementConsequencesForCurrentConflict(characterObject, hero, SceneTauntInitialArmedCrimeAmount, "authority_targeted_during_armed_conflict");
			}
			if (SceneTauntBehavior.IsChildSceneProtectedTarget(targetAgent.Character as CharacterObject))
			{
				return false;
			}
			bool flag = ShouldFleeWhenArmedVictim(targetAgent);
			if (_opponentAgentIndices.Contains(targetAgent.Index))
			{
				if (_armedBystanderWatcherIndices.Contains(targetAgent.Index))
				{
					ReleaseArmedBystanderWatcher(targetAgent);
					if (flag)
					{
						TryForceUnarmedBystanderToFlee(targetAgent);
						Logger.Log("SceneTaunt", $"Released frozen fleeing civilian into active armed conflict while preserving flee. Reason={reason}, Target={targetAgent.Name}");
					}
					else
					{
						Logger.Log("SceneTaunt", $"Released frozen armed bystander into active combat. Reason={reason}, Target={targetAgent.Name}");
					}
					return true;
				}
				if (flag)
				{
					TryForceUnarmedBystanderToFlee(targetAgent);
					Logger.Log("SceneTaunt", $"Refreshed fleeing hostile civilian during armed conflict. Reason={reason}, Target={targetAgent.Name}");
					return true;
				}
				return false;
			}
			AddAgentToFightSide(targetAgent, isPlayerSide: false);
			TryForceAgentMortal(targetAgent);
			TryAlarmAgent(targetAgent);
			foreach (Agent escortedFollower in CollectEscortedFollowers(targetAgent))
			{
				AddAgentToFightSide(escortedFollower, isPlayerSide: false);
				TryForceAgentMortal(escortedFollower);
				TryAlarmAgent(escortedFollower);
			}
			if (flag)
			{
				TryForceUnarmedBystanderToFlee(targetAgent);
				Logger.Log("SceneTaunt", $"Added fleeing civilian to armed scene conflict while preserving flee. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
			}
			else
			{
				Logger.Log("SceneTaunt", $"Added facing civilian to armed scene conflict. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Adding facing agent to armed conflict failed: " + ex.Message);
			return false;
		}
	}

	private bool TryAddFacingAgentToUnarmedConflict(Agent targetAgent, string reason)
	{
		try
		{
			if (!_conflictActive || _armedConflict || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			if (targetAgent == Agent.Main || _playerAgentIndices.Contains(targetAgent.Index))
			{
				return false;
			}
			CharacterObject characterObject = targetAgent.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (IsPlayerProtectedSceneAttackAgent(targetAgent) || !IsEligiblePhysicalAttackTarget(hero, characterObject) || SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))
			{
				return false;
			}
			bool flag = IsAuthorityPhysicalAttackTarget(hero, characterObject);
			if (flag)
			{
				_activeTargetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(hero, characterObject, targetAgent.Index);
				_activeTargetName = targetAgent.Name?.ToString() ?? hero?.Name?.ToString() ?? characterObject?.Name?.ToString() ?? _activeTargetName;
				_activeTargetAgentIndex = targetAgent.Index;
				EnableSettlementConsequencesForCurrentConflict(characterObject, hero, SceneTauntInitialArmedCrimeAmount, "authority_targeted_during_unarmed_conflict");
				if (!_opponentAgentIndices.Contains(targetAgent.Index) && !_guardAgentIndices.Contains(targetAgent.Index))
				{
					AddAgentToFightSide(targetAgent, isPlayerSide: false);
					foreach (Agent escortedFollower in CollectEscortedFollowers(targetAgent))
					{
						AddAgentToFightSide(escortedFollower, isPlayerSide: false);
					}
				}
				ClearMissionFightHandlerPendingFinishTimer();
				EscalateToArmedConflict("player_attacked_authority_during_unarmed_conflict");
				Logger.Log("SceneTaunt", $"Escalated existing unarmed conflict after attacking authority. Reason={reason}, Target={targetAgent.Name}, TargetIsLord={SceneTauntBehavior.IsSceneLordTauntTarget(hero)}");
				return true;
			}
			if (_opponentAgentIndices.Contains(targetAgent.Index) || _guardAgentIndices.Contains(targetAgent.Index))
			{
				return false;
			}
			ClearMissionFightHandlerPendingFinishTimer();
			AddAgentToFightSide(targetAgent, isPlayerSide: false);
			TryStripWeaponsForUnarmedConflict(targetAgent);
			TryAlarmAgent(targetAgent);
			foreach (Agent escortedFollower2 in CollectEscortedFollowers(targetAgent))
			{
				AddAgentToFightSide(escortedFollower2, isPlayerSide: false);
				TryStripWeaponsForUnarmedConflict(escortedFollower2);
				TryAlarmAgent(escortedFollower2);
			}
			TryAppendPlayerBehaviorFactForOpenedBrawl(hero, characterObject, targetAgent.Index);
			Logger.Log("SceneTaunt", $"Added facing agent to existing unarmed scene conflict. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Adding facing agent to unarmed conflict failed: " + ex.Message);
			return false;
		}
	}

	private static void TryAppendPlayerBehaviorFactForOpenedBrawl(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			string factText = BuildDirectBrawlImmediateReactionFactText();
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, targetAgentIndex, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending player behavior fact for opened brawl failed: " + ex.Message);
		}
	}

	private static string BuildDirectBrawlImmediateReactionFactText()
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		return "[AFEF NPC行为补充] "+text + "一拳打到了你的身上，你也开始拿拳头殴打玩家。";
	}

	private static string BuildDirectArmedImmediateReactionFactText(Agent targetAgent)
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		string text2 = TryGetActiveWeaponDisplayName(targetAgent);
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "武器";
		}
		return "[AFEF NPC行为补充] ，" + text + "一刀砍向了你，而你现在也拔出了" + text2 + "与" + text + "厮杀了起来。";
	}

	private static string TryGetActiveWeaponDisplayName(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return "";
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (TryGetRealWeaponDisplayName(agent, primaryWieldedItemIndex, out var weaponName))
			{
				return weaponName;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if (TryGetRealWeaponDisplayName(agent, offhandWieldedItemIndex, out weaponName))
			{
				return weaponName;
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!IsMissionWeaponRealWeapon(agent.Equipment[equipmentIndex]))
				{
					continue;
				}
				string text3 = agent.Equipment[equipmentIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text3))
				{
					return text3.Trim();
				}
			}
		}
		catch
		{
		}
		return "";
	}

	private static bool TryGetRealWeaponDisplayName(Agent agent, EquipmentIndex equipmentIndex, out string weaponName)
	{
		weaponName = "";
		try
		{
			if (!IsRealWeaponWieldedSlot(agent, equipmentIndex))
			{
				return false;
			}
			string text = agent.Equipment[equipmentIndex].Item?.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			weaponName = text.Trim();
			return weaponName.Length > 0;
		}
		catch
		{
			return false;
		}
	}

	private static void TryAppendNpcBehaviorFactForVerbalConflict(int targetAgentIndex)
	{
		try
		{
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			string factText = "经过交流，你和" + text + "发生了冲突";
			ShoutBehavior.AppendExternalTargetedSceneNpcFactForExternal(factText, targetAgentIndex, persistHeroPrivateHistory: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending NPC behavior fact for verbal conflict failed: " + ex.Message);
		}
	}

	private void TryAppendNpcBehaviorFactForVerbalArmedEscalation()
	{
		try
		{
			if (_activeTargetAgentIndex < 0)
			{
				return;
			}
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
			CharacterObject characterObject = agent?.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			bool flag = _guardAgentIndices.Count > 0;
			bool flag2 = SceneTauntBehavior.IsSoldierSceneTauntTarget(characterObject);
			bool flag3 = SceneTauntBehavior.IsSceneLordTauntTarget(hero);
			bool flag4 = IsAgentCarryingRealWeapon(agent);
			bool flag5 = IsSettlementCriminalConflictTarget(hero, characterObject);
			bool flag6 = IsCurrentSettlementControlledByPlayer();
			string factText;
			if (flag6)
			{
				factText = flag2 || flag3
					? "经过交流，你和" + text + "在他的领地内彻底爆发冲突；你拔出武器反击，而他的随行士兵和其他本地守卫开始保护他"
					: "经过交流，你和" + text + "在他的领地内爆发冲突；他的随行士兵拔出武器支援他，你被迫应战";
			}
			else if (flag3)
			{
				factText = "经过交流，你和" + text + "彻底撕破了脸，你身边的士兵立刻拔出武器开始围剿他";
			}
			else if (flag2)
			{
				factText = "经过交流，你和" + text + "发生了冲突，周围的士兵立刻拔出武器开始围剿他";
			}
			else if (flag5)
			{
				factText = flag4 ? "经过交流，你和" + text + "彻底闹翻了，他直接亮出了武器，你也开始和他械斗" : "经过交流，你和" + text + "彻底闹翻了，他突然亮出了武器，你被吓得开始逃跑";
			}
			else if (flag4)
			{
				factText = flag ? "经过交流，你和" + text + "发生了冲突，你也拿出武器开始和他械斗，周围的守卫也开始帮助你" : "经过交流，你和" + text + "发生了冲突，你也拿出武器开始和他械斗";
			}
			else
			{
				factText = flag ? "经过交流，你和" + text + "发生了冲突，他随即亮出了武器，周围的守卫立刻开始围剿他" : "经过交流，你和" + text + "发生了冲突，他随即亮出了武器，你被吓得开始逃跑";
			}
			ShoutBehavior.AppendExternalTargetedSceneNpcFactForExternal(factText, _activeTargetAgentIndex, persistHeroPrivateHistory: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending NPC behavior fact for verbal armed escalation failed: " + ex.Message);
		}
	}

	private bool TryAppendPlayerBehaviorFactForArmedEscalation(string reason)
	{
		try
		{
			if (_activeTargetAgentIndex < 0)
			{
				return false;
			}
			if (_openedFromVerbalTaunt || string.Equals(reason, "taunted_lord_scene", StringComparison.Ordinal) || string.Equals(reason, "taunted_soldier", StringComparison.Ordinal))
			{
				TryAppendNpcBehaviorFactForVerbalArmedEscalation();
				return false;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
			string factText = BuildDirectArmedImmediateReactionFactText(agent);
			return TryTriggerBudgetedArmedConflictReaction(factText, _activeTargetAgentIndex);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending player behavior fact for armed escalation failed: " + ex.Message);
			return false;
		}
	}

	private string BuildGuardReactionFactText()
	{
		string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
		CharacterObject characterObject = agent?.Character as CharacterObject;
		Hero hero = characterObject?.HeroObject;
		bool flag = IsAgentUsingRealWeapon(Agent.Main);
		bool flag2 = SceneTauntBehavior.IsSceneLordTauntTarget(hero);
		bool flag3 = SceneTauntBehavior.IsSoldierSceneTauntTarget(characterObject);
		bool flag4 = IsSettlementCriminalConflictTarget(hero, characterObject);
		if (IsCurrentSettlementControlledByPlayer())
		{
			return text + "在自己的领地内与人爆发了持械冲突；冲突对象进行反击，其他本地守卫和随行士兵保护领主";
		}
		if (_openedFromVerbalTaunt)
		{
			if (flag2 || flag3)
			{
				return text + "在定居点内与你爆发了冲突，你和其他守卫开始向他发动攻击";
			}
			if (flag4)
			{
				return text + (flag ? "在定居点内和暴徒爆发了冲突，并亮出了武器，周围的人立刻开始躲避" : "在定居点内和暴徒爆发了冲突，周围的人立刻开始躲避");
			}
			return text + (flag ? "在定居点内和你爆发了冲突，并亮出了武器，你和其他守卫开始向他发动攻击" : "在定居点内和你爆发了冲突，你和其他守卫开始向他发动攻击");
		}
		if (flag4)
		{
			return text + (flag ? "在定居点内和暴徒爆发了械斗，周围的人立刻开始四散躲避" : "在定居点内和暴徒爆发了冲突，周围的人立刻开始四散躲避");
		}
		if (!flag && (_openedAsUnarmedBrawl || flag2 || flag3))
		{
			return text + "在定居点内殴打了平民，你和其他守卫开始向他发动攻击";
		}
		return text + "在定居点内拿武器乱砍人，你和其他守卫开始向他发动攻击";
	}

	private void TryAppendGuardBehaviorFactsForArmedEscalation()
	{
		try
		{
			Agent main = Agent.Main;
			if (main == null || !main.IsActive())
			{
				return;
			}
			string factText = BuildGuardReactionFactText();
			TryRollArmedEscalationBehaviorFacts(main, factText);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending guard behavior facts for armed escalation failed: " + ex.Message);
		}
	}

	private void TryAppendNearbyArmedEscalationBehaviorFacts()
	{
		try
		{
			if (!_conflictActive || !_armedConflict)
			{
				return;
			}
			Agent main = Agent.Main;
			if (main == null || !main.IsActive())
			{
				return;
			}
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			string factText = BuildGuardReactionFactText();
			TryRollArmedEscalationBehaviorFacts(main, factText);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending nearby armed escalation behavior facts failed: " + ex.Message);
		}
	}

	private void TryRollArmedEscalationBehaviorFacts(Agent main, string factText)
	{
		Mission mission = Mission.Current;
		if (main == null || string.IsNullOrWhiteSpace(factText) || mission == null || !CanTryArmedConflictReactionNow())
		{
			return;
		}
		if (ShoutBehavior.HasAnyImmediateSceneReactionInFlightForExternal())
		{
			ScheduleArmedConflictReactionRetry(mission.CurrentTime, "global_immediate_reaction_in_flight");
			return;
		}
		EnsureArmedConflictReactionCandidateCache(mission, main);
		Agent targetAgent = SelectArmedConflictReactionCandidate(main, mission.CurrentTime);
		if (targetAgent == null)
		{
			ScheduleArmedConflictReactionRetry(mission.CurrentTime, "no_eligible_speaker");
			return;
		}
		if (!TryTriggerBudgetedArmedConflictReaction(factText, targetAgent.Index))
		{
			for (int i = _armedConflictReactionCandidates.Count - 1; i >= 0; i--)
			{
				if (_armedConflictReactionCandidates[i]?.Index == targetAgent.Index)
				{
					_armedConflictReactionCandidates.RemoveAt(i);
				}
			}
			ScheduleArmedConflictReactionRetry(mission.CurrentTime, "speaker_request_rejected");
		}
	}

	private void ResetArmedConflictReactionBudget()
	{
		_armedConflictReactionCount = 0;
		_lastArmedConflictReactionMissionTime = -1f;
		_armedConflictReactionStartedAtMissionTime = -1f;
		_nextArmedConflictReactionMissionTime = -1f;
		_lastArmedConflictReactionCandidateRefreshAtMissionTime = -1f;
		_armedConflictReactionRequestPending = false;
		_armedConflictReactionPendingAgentIndex = -1;
		_armedConflictReactionPendingStartedAtMissionTime = -1f;
		_armedConflictReactionCandidates.Clear();
		_armedConflictReactionLastStartedByAgentIndex.Clear();
	}

	private void InitializeArmedConflictReactionSchedule()
	{
		ResetArmedConflictReactionBudget();
		float currentTime = Mission.Current?.CurrentTime ?? 0f;
		_armedConflictReactionStartedAtMissionTime = currentTime;
		_nextArmedConflictReactionMissionTime = currentTime;
	}

	private bool CanTryArmedConflictReactionNow()
	{
		Mission mission = Mission.Current;
		if (!_conflictActive || !_armedConflict || mission == null || _armedConflictReactionRequestPending)
		{
			return false;
		}
		if (_armedConflictReactionCount >= ArmedConflictReactionMaxCount)
		{
			return false;
		}
		return _nextArmedConflictReactionMissionTime < 0f || mission.CurrentTime >= _nextArmedConflictReactionMissionTime;
	}

	private bool TryTriggerBudgetedArmedConflictReaction(string factText, int targetAgentIndex)
	{
		Mission mission = Mission.Current;
		if (string.IsNullOrWhiteSpace(factText) || targetAgentIndex < 0 || !_conflictActive || !_armedConflict || mission == null || _armedConflictReactionRequestPending)
		{
			return false;
		}
		Agent targetAgent = mission.Agents?.FirstOrDefault(a => a != null && a.Index == targetAgentIndex && a.IsActive());
		if (!IsEligibleArmedConflictReactionSpeaker(targetAgent, Agent.Main, mission.CurrentTime, allowPreviouslyUsed: true))
		{
			return false;
		}
		if (_armedConflictReactionCount >= ArmedConflictReactionMaxCount)
		{
			return false;
		}
		if (ShoutBehavior.HasAnyImmediateSceneReactionInFlightForExternal())
		{
			return false;
		}
		float requestStartedAtMissionTime = mission.CurrentTime;
		Mission requestMission = mission;
		bool accepted = ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, targetAgentIndex, persistHeroPrivateHistory: true, suppressStare: true, canStillPublish: () => _conflictActive && _armedConflict && Mission.Current == requestMission, onCompleted: generated => OnArmedConflictReactionGenerationCompleted(targetAgentIndex, requestStartedAtMissionTime, generated));
		if (!accepted)
		{
			return false;
		}
		_armedConflictReactionRequestPending = true;
		_armedConflictReactionPendingAgentIndex = targetAgentIndex;
		_armedConflictReactionPendingStartedAtMissionTime = requestStartedAtMissionTime;
		Logger.Log("SceneTaunt", $"Armed conflict reaction request started. AgentIndex={targetAgentIndex}, Completed={_armedConflictReactionCount}/{ArmedConflictReactionMaxCount}, MissionTime={requestStartedAtMissionTime:0.###}");
		return true;
	}

	private void OnArmedConflictReactionGenerationCompleted(int targetAgentIndex, float requestStartedAtMissionTime, bool generated)
	{
		if (!_armedConflictReactionRequestPending || _armedConflictReactionPendingAgentIndex != targetAgentIndex || Math.Abs(_armedConflictReactionPendingStartedAtMissionTime - requestStartedAtMissionTime) > 0.01f)
		{
			return;
		}
		_armedConflictReactionRequestPending = false;
		_armedConflictReactionPendingAgentIndex = -1;
		_armedConflictReactionPendingStartedAtMissionTime = -1f;
		Mission mission = Mission.Current;
		if (!_conflictActive || !_armedConflict || mission == null)
		{
			return;
		}
		if (!generated)
		{
			ScheduleArmedConflictReactionRetry(mission.CurrentTime, "generation_failed_or_stale");
			return;
		}
		_armedConflictReactionCount++;
		_lastArmedConflictReactionMissionTime = requestStartedAtMissionTime;
		_armedConflictReactionLastStartedByAgentIndex[targetAgentIndex] = requestStartedAtMissionTime;
		ScheduleNextArmedConflictReaction(requestStartedAtMissionTime);
		Logger.Log("SceneTaunt", $"Armed conflict reaction triggered. AgentIndex={targetAgentIndex}, Count={_armedConflictReactionCount}/{ArmedConflictReactionMaxCount}, StartedAt={requestStartedAtMissionTime:0.###}, NextAt={_nextArmedConflictReactionMissionTime:0.###}");
	}

	private void ScheduleNextArmedConflictReaction(float requestStartedAtMissionTime)
	{
		if (_armedConflictReactionCount >= ArmedConflictReactionMaxCount)
		{
			_nextArmedConflictReactionMissionTime = float.MaxValue;
			return;
		}
		float minDelay;
		float maxDelay;
		if (_armedConflictReactionCount == 1)
		{
			minDelay = ArmedConflictSecondReactionMinDelaySeconds;
			maxDelay = ArmedConflictSecondReactionMaxDelaySeconds;
		}
		else
		{
			float elapsed = Math.Max(0f, requestStartedAtMissionTime - _armedConflictReactionStartedAtMissionTime);
			if (elapsed < ArmedConflictProtractedThresholdSeconds)
			{
				minDelay = ArmedConflictSustainedReactionMinDelaySeconds;
				maxDelay = ArmedConflictSustainedReactionMaxDelaySeconds;
			}
			else
			{
				minDelay = ArmedConflictProtractedReactionMinDelaySeconds;
				maxDelay = ArmedConflictProtractedReactionMaxDelaySeconds;
			}
		}
		float delay = minDelay + (maxDelay - minDelay) * MBRandom.RandomFloat;
		_nextArmedConflictReactionMissionTime = requestStartedAtMissionTime + delay;
	}

	private void ScheduleArmedConflictReactionRetry(float currentTime, string reason)
	{
		if (!_conflictActive || !_armedConflict)
		{
			return;
		}
		_nextArmedConflictReactionMissionTime = currentTime + ArmedConflictReactionRetryDelaySeconds;
		Logger.LogVerbose("SceneTaunt", "armed_reaction_retry:" + (reason ?? "unknown"), () => $"Armed conflict reaction retry scheduled. Reason={reason}, Current={currentTime:0.###}, Next={_nextArmedConflictReactionMissionTime:0.###}, Completed={_armedConflictReactionCount}/{ArmedConflictReactionMaxCount}", 3.0);
	}

	private void EnsureArmedConflictReactionCandidateCache(Mission mission, Agent main)
	{
		if (mission == null || main == null)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		if (_lastArmedConflictReactionCandidateRefreshAtMissionTime >= 0f && currentTime - _lastArmedConflictReactionCandidateRefreshAtMissionTime < ArmedConflictReactionCandidateRefreshIntervalSeconds)
		{
			return;
		}
		RefreshArmedConflictReactionCandidateCache(mission, main, currentTime);
	}

	private void RefreshArmedConflictReactionCandidateCache(Mission mission, Agent main, float currentTime)
	{
		_armedConflictReactionCandidates.Clear();
		var agents = mission?.Agents;
		if (agents != null)
		{
			foreach (Agent agent in agents)
			{
				TryCacheArmedConflictReactionCandidate(agent, main, currentTime);
			}
		}
		_lastArmedConflictReactionCandidateRefreshAtMissionTime = currentTime;
	}

	private void TryCacheArmedConflictReactionCandidate(Agent agent, Agent main, float currentTime)
	{
		if (IsEligibleArmedConflictReactionSpeaker(agent, main, currentTime, allowPreviouslyUsed: true))
		{
			_armedConflictReactionCandidates.Add(agent);
		}
	}

	private bool IsEligibleArmedConflictReactionSpeaker(Agent agent, Agent main, float currentTime, bool allowPreviouslyUsed)
	{
		if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled || _playerAgentIndices.Contains(agent.Index))
		{
			return false;
		}
		if (IsSetsSelectedEntryFollower(agent) || SceneTauntBehavior.IsChildSceneProtectedTarget(agent.Character as CharacterObject) || !IsAgentWithinArmedBystanderReactionRadius(agent, main))
		{
			return false;
		}
		if (!_opponentAgentIndices.Contains(agent.Index) && !_guardAgentIndices.Contains(agent.Index) && !IsAgentCarryingRealWeapon(agent))
		{
			return false;
		}
		if (!_armedConflictReactionLastStartedByAgentIndex.TryGetValue(agent.Index, out var lastStarted))
		{
			return true;
		}
		return allowPreviouslyUsed && currentTime - lastStarted >= ArmedConflictReactionPerAgentCooldownSeconds;
	}

	private Agent SelectArmedConflictReactionCandidate(Agent main, float currentTime)
	{
		try
		{
			bool requireUnused = false;
			foreach (Agent candidate in _armedConflictReactionCandidates)
			{
				if (IsEligibleArmedConflictReactionSpeaker(candidate, main, currentTime, allowPreviouslyUsed: false))
				{
					requireUnused = true;
					break;
				}
			}
			int totalWeight = 0;
			foreach (Agent candidate2 in _armedConflictReactionCandidates)
			{
				if (!IsEligibleArmedConflictReactionSpeaker(candidate2, main, currentTime, allowPreviouslyUsed: !requireUnused))
				{
					continue;
				}
				totalWeight += GetArmedConflictReactionSpeakerWeight(candidate2);
			}
			if (totalWeight <= 0)
			{
				return null;
			}
			float roll = MBRandom.RandomFloat * totalWeight;
			int accumulatedWeight = 0;
			Agent lastEligible = null;
			foreach (Agent candidate3 in _armedConflictReactionCandidates)
			{
				if (!IsEligibleArmedConflictReactionSpeaker(candidate3, main, currentTime, allowPreviouslyUsed: !requireUnused))
				{
					continue;
				}
				lastEligible = candidate3;
				accumulatedWeight += GetArmedConflictReactionSpeakerWeight(candidate3);
				if (roll < accumulatedWeight)
				{
					return candidate3;
				}
			}
			return lastEligible;
		}
		catch
		{
			return null;
		}
	}

	private int GetArmedConflictReactionSpeakerWeight(Agent agent)
	{
		if (agent != null && _guardAgentIndices.Contains(agent.Index))
		{
			return 6;
		}
		if (agent != null && _opponentAgentIndices.Contains(agent.Index))
		{
			return 5;
		}
		return 1;
	}

	internal bool ShouldBlockAgentWeaponWield(Agent agent)
	{
		return _conflictActive && !_armedConflict && agent != null && _blockedAiWeaponAgentIndices.Contains(agent.Index);
	}

	internal bool ShouldUseFullCombatDamage(Agent victimAgent, Agent attackerAgent)
	{
		if (!_conflictActive || victimAgent == null || attackerAgent == null)
		{
			return false;
		}
		if ((attackerAgent == Agent.Main || attackerAgent.IsMainAgent) && IsPlayerProtectedSceneAttackAgent(victimAgent))
		{
			return false;
		}
		bool flag = _playerAgentIndices.Contains(attackerAgent.Index);
		bool flag2 = _opponentAgentIndices.Contains(attackerAgent.Index);
		bool flag3 = _playerAgentIndices.Contains(victimAgent.Index);
		bool flag4 = _opponentAgentIndices.Contains(victimAgent.Index);
		if ((flag && flag4) || (flag2 && flag3))
		{
			return true;
		}
		if (!_armedConflict)
		{
			return false;
		}
		if (flag && IsArmedConflictCollateralVictim(victimAgent))
		{
			return true;
		}
		return flag2 && flag3;
	}

	private static bool IsPlayerProtectedSceneAttackAgent(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return false;
			}
			if (SceneTauntBehavior.IsPlayerProtectedSceneAttackTarget((agent.Character as CharacterObject)?.HeroObject))
			{
				return true;
			}
			return RewardSystemBehavior.TryResolvePromotedNonHeroCompanionForSceneAgentExternal(agent.Index, out var promotedHero)
				&& SceneTauntBehavior.IsPlayerProtectedSceneAttackTarget(promotedHero);
		}
		catch
		{
			return false;
		}
	}

	private bool IsArmedConflictCollateralVictim(Agent victimAgent)
	{
		try
		{
			if (victimAgent == null || !victimAgent.IsHuman || !victimAgent.IsActive() || victimAgent.IsMainAgent || _playerAgentIndices.Contains(victimAgent.Index))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal bool ShouldBlockSceneExit()
	{
		return _conflictActive || SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal bool ShouldShowWantedSceneExitNotice()
	{
		return (_conflictActive && (_armedConflict || _armedConflictOccurredThisConflict)) || SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal bool ShouldDelayNativeFightAutoEndLong()
	{
		return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
	}

	internal bool ShouldSendPlayerToLocalDungeonOnDefeat()
	{
		if (_armedDefeatOutcomeHandled || !_armedConflictOccurredThisConflict || ShouldCommitPlayerBattleDeathAfterMission())
		{
			return false;
		}
		try
		{
			return Agent.Main == null || !Agent.Main.IsActive();
		}
		catch
		{
			return false;
		}
	}

	internal void MarkPlayerDefeatOutcomeHandled()
	{
		_armedDefeatOutcomeHandled = true;
	}

	internal bool TryUseSafeMainHeroDefeatState(Agent effectedAgent, float deathProbability, out AgentState result)
	{
		result = AgentState.Unconscious;
		try
		{
			bool externalArmedConflict = SettlementEntryTroopSelectionBehavior.IsSetsDefenderConflictActiveForExternal(Mission.Current);
			if (((!_conflictActive || (!_armedConflict && !_armedConflictOccurredThisConflict)) && !externalArmedConflict)
				|| effectedAgent == null
				|| !effectedAgent.IsMainAgent)
			{
				return false;
			}
			if (externalArmedConflict)
			{
				_armedConflictOccurredThisConflict = true;
				_armedDefeatWasCriminalConflict = false;
			}
			if (!_pendingPlayerBattleDeathDecisionCaptured)
			{
				_pendingPlayerBattleDeathAfterMission = RollDeferredPlayerBattleDeath(deathProbability);
				_pendingPlayerBattleDeathDecisionCaptured = true;
				Logger.Log("SceneTaunt", $"Deferred main hero defeat state inside mission. PendingBattleDeath={_pendingPlayerBattleDeathAfterMission}, DeathProbability={MathF.Max(0f, MathF.Min(1f, deathProbability)):0.###}");
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Deferring main hero defeat state failed: " + ex.Message);
			return false;
		}
	}

	internal bool ShouldCommitPlayerBattleDeathAfterMission()
	{
		return _pendingPlayerBattleDeathAfterMission && _armedConflictOccurredThisConflict;
	}

	internal void EnsurePendingPlayerBattleDeathQueued(string reason)
	{
		if (!ShouldCommitPlayerBattleDeathAfterMission())
		{
			return;
		}
		SceneTauntBehavior.QueuePendingMainHeroBattleDeathForExternal(_pendingPlayerBattleDeathKiller, reason);
	}

	internal bool WasLastArmedDefeatCriminalConflict()
	{
		return _armedDefeatWasCriminalConflict;
	}

	internal static bool ShouldBlockAgentWeaponWieldExternal(Agent agent)
	{
		try
		{
			return agent?.Mission?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldBlockAgentWeaponWield(agent) ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsAgentUsingRealWeaponForExternal(Agent agent)
	{
		return IsAgentUsingRealWeapon(agent);
	}

	internal static bool IsMissionWeaponRealWeaponForExternal(in MissionWeapon attackerWeapon)
	{
		return IsMissionWeaponRealWeapon(attackerWeapon);
	}

	internal static bool IsWeaponComponentRealWeaponForExternal(WeaponComponentData attackerWeapon)
	{
		return IsWeaponComponentRealWeapon(attackerWeapon);
	}

	internal static bool ShouldPrioritizeUnarmedVillageBrawlOverSetsForExternal(
		Mission mission,
		Agent attacker,
		Agent target,
		bool attackerUsedRealWeapon)
	{
		try
		{
			return mission?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldPrioritizeUnarmedVillageBrawlOverSets(
				attacker,
				target,
				attackerUsedRealWeapon) ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static void ApplyArmedConflictStartCrimeForExternal(IFaction faction, string reason)
	{
		try
		{
			SceneTauntMissionBehavior behavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
			if (behavior == null || faction == null)
			{
				return;
			}
			behavior.ApplySceneTauntCrimeWithDeferredCap(
				faction,
				SceneTauntInitialArmedCrimeAmount,
				string.IsNullOrWhiteSpace(reason) ? "external_armed_conflict_start" : reason);
			Logger.Log("SceneTaunt", $"Applied AF armed-conflict start crime for external scene bridge. Faction={faction.Name}, Amount={SceneTauntInitialArmedCrimeAmount:0.##}, Reason={reason ?? "N/A"}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying external armed-conflict start crime failed: " + ex.Message);
		}
	}

	internal static float GetCrimeCapBeforeWarForExternal()
	{
		return SceneTauntCrimeCapBeforeWar;
	}

	internal static void TryApplyArmedNpcKnockdownConsequencesForExternal(
		Agent affectedAgent,
		Agent affectorAgent,
		AgentState agentState,
		string reason)
	{
		try
		{
			Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>()?.TryApplyArmedNpcKnockdownConsequencesCore(
				affectedAgent,
				affectorAgent,
				agentState,
				string.IsNullOrWhiteSpace(reason) ? "external_armed_knockdown" : reason);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying external armed knockdown consequences failed: " + ex.Message);
		}
	}

	internal static bool ShouldUseFullCombatDamageExternal(Agent victimAgent, Agent attackerAgent)
	{
		try
		{
			Mission mission = victimAgent?.Mission ?? attackerAgent?.Mission;
			return mission?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldUseFullCombatDamage(victimAgent, attackerAgent) ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldDelayNativeFightAutoEndLongExternal(Mission mission)
	{
		try
		{
			SceneTauntMissionBehavior missionBehavior = mission?.GetMissionBehavior<SceneTauntMissionBehavior>();
			return missionBehavior != null && missionBehavior.ShouldDelayNativeFightAutoEndLong();
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldSuppressSceneNotableDeathExternal(Hero hero)
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldSuppressSceneNotableDeath(hero) ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldDeferSceneNotableBattleDeathExternal(Hero hero)
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldDeferSceneNotableBattleDeath(hero) ?? false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldSuppressNativeMissionConversationExternal(Mission mission)
	{
		try
		{
			if (mission?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldSuppressNativeMissionConversation() ?? false)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
		}
		catch
		{
			return false;
		}
	}

	internal static string BuildFrightenedCivilianShoutExtraFactExternal(Agent targetAgent)
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>()?.BuildFrightenedCivilianShoutExtraFact(targetAgent) ?? "";
		}
		catch
		{
			return "";
		}
	}

	internal static bool ShouldBlockSceneExitExternal(Mission mission)
	{
		if (SettlementEntryTroopSelectionBehavior.ShouldBypassSceneTauntExitBlockForExternal(mission))
		{
			return false;
		}
		try
		{
			if (mission?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldBlockSceneExit() ?? false)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldShowWantedSceneExitNoticeExternal(Mission mission)
	{
		try
		{
			if (mission?.GetMissionBehavior<SceneTauntMissionBehavior>()?.ShouldShowWantedSceneExitNotice() ?? false)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement();
		}
		catch
		{
			return false;
		}
	}

	internal static void ShowBlockedSceneExitNotice(Mission mission)
	{
		if (ShouldShowWantedSceneExitNoticeExternal(mission))
		{
			AnimusForgeQuickInfo.Show(WantedSceneExitNotice);
		}
		else
		{
			AnimusForgeQuickInfo.Show("这场冲突还没结束，不能离开场景。");
		}
	}

	internal static InquiryData CreateBlockedSceneExitInquiry(Mission mission)
	{
		string text = ShouldShowWantedSceneExitNoticeExternal(mission) ? WantedSceneExitNotice : "这场冲突还没结束，不能离开场景。";
		return new InquiryData("无法离开", text, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", "确定", null, null);
	}

	private Agent ResolveTargetAgent(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			if (targetAgentIndex >= 0)
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == targetAgentIndex);
				if (agent != null)
				{
					return agent;
				}
			}
		}
		catch
		{
		}
		try
		{
			Agent agent2 = Campaign.Current?.ConversationManager?.OneToOneConversationAgent as Agent;
			CharacterObject characterObject = agent2?.Character as CharacterObject;
			if (agent2 != null && (agent2.Character == targetCharacter || characterObject?.HeroObject == targetHero))
			{
				return agent2;
			}
		}
		catch
		{
		}
		try
		{
			if (targetHero != null)
			{
				Agent agent3 = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.IsHuman && (x.Character as CharacterObject)?.HeroObject == targetHero);
				if (agent3 != null)
				{
					return agent3;
				}
			}
		}
		catch
		{
		}
		try
		{
			return Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.IsHuman && x.Character == targetCharacter);
		}
		catch
		{
			return null;
		}
	}

	private static bool IsEligiblePhysicalAttackTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		if (SceneTauntBehavior.IsPlayerProtectedSceneAttackTarget(targetHero ?? targetCharacter?.HeroObject))
		{
			return false;
		}
		return IsAuthorityPhysicalAttackTarget(targetHero, targetCharacter) || IsSettlementCriminalConflictTarget(targetHero, targetCharacter) || SceneTauntBehavior.IsSceneNotableTauntTarget(targetHero) || SceneTauntBehavior.IsEligibleSceneTauntCharacter(targetCharacter);
	}

	private static bool CanUseNativeCriminalConflict(Agent targetAgent)
	{
		try
		{
			return targetAgent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator?.MemberOfAlley != null && Mission.Current != null;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsAuthorityPhysicalAttackTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		if (SceneTauntBehavior.IsSceneLordTauntTarget(targetHero))
		{
			return true;
		}
		if (targetCharacter == null || targetCharacter.IsHero)
		{
			return false;
		}
		switch (targetCharacter.Occupation)
		{
		case Occupation.Guard:
		case Occupation.PrisonGuard:
		case Occupation.Soldier:
			return true;
		default:
			return false;
		}
	}

	internal static bool IsAuthorityPhysicalAttackTargetForExternal(Agent targetAgent)
	{
		CharacterObject targetCharacter = targetAgent?.Character as CharacterObject;
		return IsAuthorityPhysicalAttackTarget(targetCharacter?.HeroObject, targetCharacter);
	}

	private static bool IsSettlementCriminalConflictTarget(Hero targetHero, CharacterObject targetCharacter)
	{
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (hero != null)
		{
			switch (hero.Occupation)
			{
			case Occupation.GangLeader:
			case Occupation.Bandit:
				return true;
			}
		}
		if (targetCharacter == null)
		{
			return false;
		}
		switch (targetCharacter.Occupation)
		{
		case Occupation.Gangster:
		case Occupation.GangLeader:
		case Occupation.Bandit:
			return true;
		default:
			return false;
		}
	}

	internal static bool IsSettlementCriminalConflictTargetForExternal(Agent targetAgent)
	{
		CharacterObject targetCharacter = targetAgent?.Character as CharacterObject;
		return IsSettlementCriminalConflictTarget(targetCharacter?.HeroObject, targetCharacter);
	}

	private static SetsOwnedSettlementAttackRouting ResolveOwnedSettlementAttackRouting(Agent targetAgent)
	{
		return SetsCityConflictPolicy.ResolveOwnedAttackRouting(
			settlementControlledByPlayer: IsCurrentSettlementControlledByPlayer(),
			isSettlementAuthority: IsAuthorityPhysicalAttackTargetForExternal(targetAgent),
			isCriminalConflictTarget: IsSettlementCriminalConflictTargetForExternal(targetAgent));
	}

	internal static bool ShouldUseOwnedSettlementPassiveAttackForExternal(Agent targetAgent)
	{
		return ResolveOwnedSettlementAttackRouting(targetAgent) == SetsOwnedSettlementAttackRouting.PassiveSurrender;
	}

	private static bool IsCurrentSettlementControlledByPlayer()
	{
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			return SettlementEntryTroopSelectionBehavior.IsPlayerAuthoritySettlementForExternal(settlement);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsSettlementCriminalConflictTargetExternal(Hero targetHero, CharacterObject targetCharacter)
	{
		return IsSettlementCriminalConflictTarget(targetHero, targetCharacter);
	}

	private bool IsActiveTargetSettlementCriminalConflict()
	{
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
			CharacterObject targetCharacter = agent?.Character as CharacterObject;
			Hero targetHero = targetCharacter?.HeroObject;
			return IsSettlementCriminalConflictTarget(targetHero, targetCharacter);
		}
		catch
		{
			return false;
		}
	}

	private void TryRewardSettlementTrustForCriminalKnockdown(Settlement settlement, string victimName)
	{
		SceneTauntBehavior.TryRewardSettlementTrustForCriminalKnockdownForExternal(settlement, victimName);
	}

	private static Hero TryResolveCriminalOwnerHeroFromAgent(Agent victimAgent)
	{
		try
		{
			CharacterObject characterObject = victimAgent?.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (hero != null && hero.IsGangLeader)
			{
				return hero;
			}
			CampaignAgentComponent component = victimAgent?.GetComponent<CampaignAgentComponent>();
			Hero hero2 = component?.AgentNavigator?.MemberOfAlley?.Owner;
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

	private bool TryStartNativeCriminalConflict(Agent targetAgent, string reason)
	{
		try
		{
			Alley alley = targetAgent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator?.MemberOfAlley;
			if (alley == null || Mission.Current == null)
			{
				Logger.Log("SceneTaunt", "Native criminal conflict start skipped because alley context is unavailable.");
				return false;
			}
			_fightHandler = _fightHandler ?? Mission.Current.GetMissionBehavior<MissionFightHandler>();
			if (_fightHandler?.IsThereActiveFight() == true)
			{
				bool nativeAlleyFightActive = IsNativeAlleyFightCurrentlyActive();
				if (nativeAlleyFightActive)
				{
					RememberNativeCriminalConflictTarget(targetAgent);
				}
				Logger.LogVerbose("SceneTaunt", "native_alley_existing_fight:" + targetAgent.Index, () => $"Skipped native alley start because MissionFightHandler is already active. NativeAlley={nativeAlleyFightActive}, Reason={reason}, Target={targetAgent.Name}", 0.5);
				return nativeAlleyFightActive;
			}
			Type type = AccessTools.TypeByName("SandBox.Missions.MissionLogics.MissionAlleyHandler");
			MethodInfo methodInfo = typeof(Mission).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo m) => m.Name == "GetMissionBehavior" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
			MethodInfo methodInfo2 = AccessTools.Method(type, "StartCommonAreaBattle");
			if (type == null || methodInfo == null || methodInfo2 == null)
			{
				Logger.Log("SceneTaunt", "Native criminal conflict start skipped because alley handler reflection failed.");
				return false;
			}
			object obj = methodInfo.MakeGenericMethod(type).Invoke(Mission.Current, null);
			if (obj == null)
			{
				Logger.Log("SceneTaunt", "Native criminal conflict start skipped because MissionAlleyHandler was not found.");
				return false;
			}
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
			TryTriggerNativeCriminalConflictReaction(targetAgent, reason);
			methodInfo2.Invoke(obj, new object[1] { alley });
			Logger.Log("SceneTaunt", $"Redirected criminal conflict to native alley flow. Reason={reason}, Target={targetAgent?.Name}, Alley={alley?.Name}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting native criminal conflict failed: " + ex.Message);
			return false;
		}
	}

	private static bool IsNativeAlleyFightCurrentlyActive()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission?.GetMissionBehavior<MissionAlleyHandler>() == null)
			{
				return false;
			}
			return NativeAlleyFightPositionField?.GetValue(null) is Vec3 fightPosition && fightPosition != Vec3.Invalid;
		}
		catch
		{
			return false;
		}
	}

	private void TryTriggerNativeCriminalConflictReaction(Agent targetAgent, string reason)
	{
		try
		{
			if (targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return;
			}
			string text = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			string factText = ((reason ?? "").IndexOf("verbal", StringComparison.OrdinalIgnoreCase) >= 0) ? ("经过交流，" + text + "把你彻底激怒了，你立刻招呼同伙扑上去，要狠狠干他一顿") : ("经过交流，" + text + "竟敢直接对你动手，你一边破口大骂，一边立刻招呼同伙围上去狠狠干他一顿");
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, targetAgent.Index, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Triggering native criminal conflict reaction failed: " + ex.Message);
		}
	}

	private void TryApplyCriminalOwnerPenalty(Hero ownerHero, string victimName)
	{
		try
		{
			if (ownerHero == null || Hero.MainHero == null)
			{
				return;
			}
			RewardSystemBehavior.Instance?.AdjustTrustForExternal(ownerHero, -1, 0, "scene_taunt_criminal_owner_knockdown");
			RomanceSystemBehavior.Instance?.AdjustPrivateLove(ownerHero, -1, "scene_taunt_criminal_owner_knockdown");
			string text = string.IsNullOrWhiteSpace(victimName) ? "匪类" : victimName;
			AnimusForgeQuickInfo.Show($"击倒 {text}：{ownerHero.Name} 的个人信任 -1，私人关系 -1。", ownerHero.CharacterObject);
			Logger.Log("SceneTaunt", $"Applied criminal owner penalty after knockdown. Owner={ownerHero.Name}, Victim={text}, PersonalTrustDelta=-1, PrivateLoveDelta=-1");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying criminal owner penalty failed: " + ex.Message);
		}
	}

	private List<Agent> CollectPlayerSideAgents()
	{
		List<Agent> list = new List<Agent>();
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents != null)
			{
				foreach (Agent agent in agents)
				{
					if (IsPlayerAlignedConflictAgent(agent))
					{
						AddUniqueAgent(list, agent);
					}
				}
			}
		}
		catch
		{
		}
		if (!list.Contains(Agent.Main))
		{
			AddUniqueAgent(list, Agent.Main);
		}
		return list;
	}

	private static bool IsPlayerAlignedConflictAgent(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return false;
			}
			if (agent == Agent.Main || IsSetsSelectedEntryFollower(agent) || IsPlayerProtectedSceneAttackAgent(agent))
			{
				return true;
			}
			Hero hero = (agent.Character as CharacterObject)?.HeroObject;
			if (SceneTauntBehavior.IsPlayerMainPartyHero(hero))
			{
				return true;
			}
			LocationCharacter locationCharacter = LocationComplex.Current?.FindCharacter(agent);
			AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter?.GetAccompanyingCharacter(locationCharacter);
			return accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart;
		}
		catch
		{
			return false;
		}
	}

	private List<Agent> CollectOpponentSideAgents(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		AddUniqueAgent(list, targetAgent);
		foreach (Agent escortedFollower in CollectEscortedFollowers(targetAgent))
		{
			AddUniqueAgent(list, escortedFollower);
		}
		return list;
	}

	private static bool NormalizeInitialConflictSides(List<Agent> playerSideAgents, List<Agent> opponentSideAgents, Agent activeTarget)
	{
		if (playerSideAgents == null || opponentSideAgents == null || activeTarget == null)
		{
			return false;
		}
		HashSet<int> playerIndices = new HashSet<int>(playerSideAgents
			.Where(agent => agent != null)
			.Select(agent => agent.Index));
		opponentSideAgents.RemoveAll(agent => agent == null || playerIndices.Contains(agent.Index));
		return opponentSideAgents.Any(agent => agent == activeTarget);
	}

	private List<Agent> CollectEscortedFollowers(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		if (targetAgent == null)
		{
			return list;
		}
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents == null)
			{
				return list;
			}
			foreach (Agent agent in agents)
			{
				if (agent == null || agent == targetAgent || !agent.IsHuman || !agent.IsActive() || IsPlayerAlignedConflictAgent(agent))
				{
					continue;
				}
				if (EscortAgentBehavior.CheckIfAgentIsEscortedBy(agent, targetAgent)
					&& ResolveCityConflictSide(agent, targetAgent, isTargetEscort: true, armedConflict: _armedConflict) == SetsCityConflictSide.Opponent)
				{
					AddUniqueAgent(list, agent);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<Agent> CollectGuardAgents(List<Agent> playerSideAgents, List<Agent> opponentSideAgents)
	{
		HashSet<int> hashSet = new HashSet<int>(playerSideAgents.Where((Agent x) => x != null).Select((Agent x) => x.Index));
		foreach (Agent opponentSideAgent in opponentSideAgents)
		{
			if (opponentSideAgent != null)
			{
				hashSet.Add(opponentSideAgent.Index);
			}
		}
		List<Agent> list = new List<Agent>();
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents == null)
			{
				return list;
			}
			foreach (Agent agent in agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index) || IsSetsSelectedEntryFollower(agent))
				{
					continue;
				}
				CharacterObject characterObject = agent.Character as CharacterObject;
				if (characterObject == null)
				{
					continue;
				}
				if (characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.PrisonGuard || characterObject.Occupation == Occupation.Soldier)
				{
					list.Add(agent);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private List<Agent> CollectAuthorityCarryoverOpponentAgents(List<Agent> playerSideAgents, out List<Agent> guardAgents)
	{
		HashSet<int> hashSet = new HashSet<int>(playerSideAgents.Where((Agent x) => x != null).Select((Agent x) => x.Index));
		List<Agent> list = new List<Agent>();
		guardAgents = new List<Agent>();
		try
		{
			var agents = Mission.Current?.Agents;
			bool settlementControlledByPlayer = IsCurrentSettlementControlledByPlayer();
			if (agents == null)
			{
				return list;
			}
			foreach (Agent agent in agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index) || IsSetsSelectedEntryFollower(agent))
				{
					continue;
				}
				CharacterObject characterObject = agent.Character as CharacterObject;
				Hero hero = characterObject?.HeroObject;
				if (!IsCarryoverAuthorityOpponent(hero, characterObject))
				{
					continue;
				}
				if (settlementControlledByPlayer)
				{
					if (IsGuardLikeCharacter(characterObject))
					{
						AddUniqueAgent(playerSideAgents, agent);
						AddUniqueAgent(guardAgents, agent);
					}
					continue;
				}
				AddUniqueAgent(list, agent);
				if (IsGuardLikeCharacter(characterObject))
				{
					AddUniqueAgent(guardAgents, agent);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static bool IsCarryoverAuthorityOpponent(Hero targetHero, CharacterObject targetCharacter)
	{
		if (SceneTauntBehavior.IsSceneLordTauntTarget(targetHero))
		{
			return true;
		}
		return IsGuardLikeCharacter(targetCharacter);
	}

	private static bool IsGuardLikeCharacter(CharacterObject targetCharacter)
	{
		if (targetCharacter == null || targetCharacter.IsHero)
		{
			return false;
		}
		switch (targetCharacter.Occupation)
		{
		case Occupation.Guard:
		case Occupation.PrisonGuard:
		case Occupation.Soldier:
			return true;
		default:
			return false;
		}
	}

	private static SetsCityConflictSide ResolveCityConflictSide(
		Agent agent,
		Agent activeTarget,
		bool isTargetEscort,
		bool armedConflict)
	{
		return SetsCityConflictPolicy.ResolveSide(
			settlementControlledByPlayer: IsCurrentSettlementControlledByPlayer(),
			isSelectedEntryFollower: IsSetsSelectedEntryFollower(agent),
			isActiveTarget: agent != null && agent == activeTarget,
			isTargetEscort: isTargetEscort,
			isSettlementAuthority: IsAuthorityPhysicalAttackTargetForExternal(agent),
			armedConflict: armedConflict);
	}

	private void TryActivateSettlementArmedCarryover()
	{
		if (_conflictActive || _armedCarryoverSceneInitialized || _armedCarryoverHandledInThisMission || !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
		{
			return;
		}
		Mission mission = Mission.Current;
		if (mission == null || Agent.Main == null || !Agent.Main.IsActive() || Settlement.CurrentSettlement == null || _fightHandler == null)
		{
			return;
		}
		if (CampaignMission.Current?.Location == null || PlayerEncounter.LocationEncounter == null)
		{
			return;
		}
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false)
		{
			return;
		}
		if (_fightHandler.IsThereActiveFight())
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		if (_lastArmedCarryoverAttemptAtMissionTime >= 0f && currentTime - _lastArmedCarryoverAttemptAtMissionTime < 0.25f)
		{
			return;
		}
		_lastArmedCarryoverAttemptAtMissionTime = currentTime;
		long totalStart = StartPerfTimer();
		List<Agent> list = CollectPlayerSideAgents();
		List<Agent> list2 = CollectAuthorityCarryoverOpponentAgents(list, out var guardAgents);
		LogPerfElapsed("carryover.collectSides", totalStart, $"player={list.Count} opponents={list2.Count} guards={guardAgents.Count}");
		if (list2.Count == 0)
		{
			if (!_armedCarryoverNoAuthoritySceneNotified && !SceneTauntBehavior.HasShownCarryoverNoAuthorityAlertForCurrentLocationExternal())
			{
				AlarmNearbyBystanders();
				AnimusForgeQuickInfo.Show("持械冲突的警报蔓延到了这个场景，周围的人立刻紧张起来。");
				_armedCarryoverNoAuthoritySceneNotified = true;
				_armedCarryoverHandledInThisMission = true;
				SceneTauntBehavior.MarkCarryoverNoAuthorityAlertShownForCurrentLocationExternal();
				Logger.Log("SceneTaunt", $"Armed carryover reached scene without authority opponents. Settlement={Settlement.CurrentSettlement?.Name}, Source={SceneTauntBehavior.GetArmedCarryoverSourceForCurrentSettlement()}");
				LogPerfPoint("carryover.noAuthority", $"elapsedMs={GetElapsedPerfMs(totalStart):0.###}");
			}
			return;
		}
		try
		{
			LogPerfPoint("carryover.start", $"player={list.Count} opponents={list2.Count} guards={guardAgents.Count}");
			_conflictActive = true;
			_armedConflict = true;
			_armedConflictOccurredThisConflict = true;
			_armedDefeatOutcomeHandled = false;
			InitializeArmedConflictReactionSchedule();
			_baseConsequencesApplied = true;
			_appliedCrimeRatingAmount = SceneTauntInitialArmedCrimeAmount;
			_activeTargetKey = "armed_settlement_carryover";
			_activeTargetName = Settlement.CurrentSettlement?.Name?.ToString() ?? "当前场景";
			_playerAgentIndices.Clear();
			_opponentAgentIndices.Clear();
			_guardAgentIndices.Clear();
			_blockedAiWeaponAgentIndices.Clear();
			foreach (Agent item in list)
			{
				_playerAgentIndices.Add(item.Index);
			}
			foreach (Agent item2 in list2)
			{
				_opponentAgentIndices.Add(item2.Index);
				RegisterSceneGoldEligibleAgent(item2, "carryover_opponent");
			}
			foreach (Agent guardAgent in guardAgents)
			{
				_guardAgentIndices.Add(guardAgent.Index);
				RegisterSceneGoldEligibleAgent(guardAgent, "carryover_guard");
			}
			long sectionStart = StartPerfTimer();
			_fightHandler.StartCustomFight(list, list2, dropWeapons: false, isItemUseDisabled: false, OnConflictFinished, float.Epsilon);
			LogPerfElapsed("carryover.StartCustomFight", sectionStart, $"player={list.Count} opponents={list2.Count}", SceneTauntPerfHeavyStageThresholdMs);
			sectionStart = StartPerfTimer();
			int conflictAgents = 0;
			foreach (Agent agent in EnumerateConflictAgents(includeGuards: true))
			{
				if (agent == null || !agent.IsActive())
				{
					continue;
				}
				conflictAgents++;
				TryRestoreWeaponsAfterUnarmedConflict(agent);
				TryAlarmAgent(agent);
				if (agent != Agent.Main)
				{
					TryArmAgent(agent);
				}
			}
			LogPerfElapsed("carryover.armConflictAgents", sectionStart, $"processed={conflictAgents}");
			TryMaintainSetsSelectedFollowerArmedReadiness();
			sectionStart = StartPerfTimer();
			ForceAllNonPlayerSceneAgentsMortal();
			LogPerfElapsed("carryover.forceAllMortal", sectionStart, null, SceneTauntPerfHeavyStageThresholdMs);
			sectionStart = StartPerfTimer();
			AlarmNearbyBystanders();
			LogPerfElapsed("carryover.alarmNearbyBystanders", sectionStart, null, SceneTauntPerfHeavyStageThresholdMs);
			_armedCarryoverSceneInitialized = true;
			_armedCarryoverHandledInThisMission = true;
			AnimusForgeQuickInfo.Show("你的持械冲突已经蔓延到这个场景，守卫和武装平民立刻开始围堵你。");
			Logger.Log("SceneTaunt", $"Activated armed settlement carryover in scene. Settlement={Settlement.CurrentSettlement?.Name}, Opponents={list2.Count}, Guards={guardAgents.Count}, Source={SceneTauntBehavior.GetArmedCarryoverSourceForCurrentSettlement()}");
			LogPerfPoint("carryover.end", $"elapsedMs={GetElapsedPerfMs(totalStart):0.###}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Activating armed settlement carryover failed: " + ex.Message);
			ClearRuntimeState();
		}
	}

	private void PrepareUnarmedConflict()
	{
		_openedAsUnarmedBrawl = true;
		foreach (Agent agent in EnumerateConflictAgents(includeGuards: false))
		{
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			if (agent == Agent.Main)
			{
				QueuePendingPlayerUnarmedPrep();
			}
			else
			{
				TryStripWeaponsForUnarmedConflict(agent);
				TrySheathWeapons(agent);
			}
			TryAlarmAgent(agent);
			if (agent != Agent.Main && agent.IsAIControlled)
			{
				_blockedAiWeaponAgentIndices.Add(agent.Index);
			}
		}
		Logger.Log("SceneTaunt", $"Started unarmed scene conflict. Target={_activeTargetName}, PlayerSide={_playerAgentIndices.Count}, OpponentSide={_opponentAgentIndices.Count}");
	}

	private void TryQueueImmediateUnarmedFightEndAfterAgentRemoval(Agent affectedAgent, AgentState agentState)
	{
		try
		{
			if (!_conflictActive || _armedConflict || affectedAgent == null || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
			{
				return;
			}
			if (!_playerAgentIndices.Contains(affectedAgent.Index) && !_opponentAgentIndices.Contains(affectedAgent.Index))
			{
				return;
			}
			if (IsIndexedSideDefeated(_opponentAgentIndices))
			{
				_pendingImmediateUnarmedFightEnd = true;
				_pendingImmediateUnarmedFightEndPlayerWon = true;
				return;
			}
			if (IsIndexedSideDefeated(_playerAgentIndices))
			{
				_pendingImmediateUnarmedFightEnd = true;
				_pendingImmediateUnarmedFightEndPlayerWon = false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing immediate unarmed fight end failed: " + ex.Message);
		}
	}

	private bool IsIndexedSideDefeated(HashSet<int> indices)
	{
		try
		{
			var agents = Mission.Current?.Agents;
			if (indices == null || indices.Count == 0 || agents == null)
			{
				return true;
			}
			foreach (int index in indices)
			{
				Agent agent = agents.FirstOrDefault(a => a != null && a.Index == index);
				if (agent != null && agent.IsHuman && agent.State == AgentState.Active)
				{
					return false;
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void TryCommitPendingImmediateUnarmedFightEnd()
	{
		try
		{
			if (!_pendingImmediateUnarmedFightEnd || _armedConflict || !_conflictActive)
			{
				return;
			}
			if (_fightHandler == null || !_fightHandler.IsThereActiveFight())
			{
				_pendingImmediateUnarmedFightEnd = false;
				return;
			}
			bool pendingImmediateUnarmedFightEndPlayerWon = _pendingImmediateUnarmedFightEndPlayerWon;
			_pendingImmediateUnarmedFightEnd = false;
			ClearMissionFightHandlerPendingFinishTimer();
			_fightHandler.EndFight(pendingImmediateUnarmedFightEndPlayerWon);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing immediate unarmed fight end failed: " + ex.Message);
		}
	}

	private void ClearMissionFightHandlerPendingFinishTimer()
	{
		try
		{
			if (_fightHandler != null && FinishTimerField != null)
			{
				FinishTimerField.SetValue(_fightHandler, null);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Clearing MissionFightHandler finish timer failed: " + ex.Message);
		}
	}

	private void QueuePendingPlayerUnarmedPrep()
	{
		_pendingPlayerUnarmedPrep = true;
		_pendingPlayerUnarmedPrepAtMissionTime = (Mission.Current?.CurrentTime ?? 0f) + 0.14f;
	}

	private void TryCommitPendingPlayerUnarmedPrep()
	{
		try
		{
			Mission mission = Mission.Current;
			if (!_pendingPlayerUnarmedPrep || mission == null || _armedConflict)
			{
				if (_armedConflict)
				{
					ClearPendingPlayerUnarmedPrep();
				}
				return;
			}
			if (mission.CurrentTime < _pendingPlayerUnarmedPrepAtMissionTime)
			{
				return;
			}
			if (Agent.Main != null && Agent.Main.IsActive())
			{
				TryStripWeaponsForUnarmedConflict(Agent.Main);
				TrySheathWeapons(Agent.Main);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying delayed player unarmed prep failed: " + ex.Message);
		}
		finally
		{
			ClearPendingPlayerUnarmedPrep();
		}
	}

	private void ClearPendingPlayerUnarmedPrep()
	{
		_pendingPlayerUnarmedPrep = false;
		_pendingPlayerUnarmedPrepAtMissionTime = -1f;
	}

	private void QueuePendingPlayerRearmAfterArmedConflictEnd()
	{
		_pendingPlayerRearmAfterArmedConflictEnd = true;
		_pendingPlayerRearmAfterArmedConflictEndAtMissionTime = (Mission.Current?.CurrentTime ?? 0f) + 0.2f;
	}

	private void QueuePendingActiveUnarmedTargetFleeIfNeeded()
	{
		try
		{
			_pendingActiveUnarmedTargetFlee = false;
			_pendingActiveUnarmedTargetFleeAgentIndex = -1;
			_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (!_armedConflict || _activeTargetAgentIndex < 0 || agents == null)
			{
				return;
			}
			Agent agent = agents.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
			if (agent == null || !agent.IsActive() || !ShouldFleeWhenArmedVictim(agent))
			{
				return;
			}
			_pendingActiveUnarmedTargetFlee = true;
			_pendingActiveUnarmedTargetFleeAgentIndex = agent.Index;
			_pendingActiveUnarmedTargetFleeAtMissionTime = mission.CurrentTime + 0.12f;
			TryForceUnarmedBystanderToFlee(agent);
			Logger.Log("SceneTaunt", $"Queued active unarmed target to flee after armed escalation. Agent={agent.Name}, AgentIndex={agent.Index}, ExecuteAt={_pendingActiveUnarmedTargetFleeAtMissionTime:0.###}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing active unarmed target flee failed: " + ex.Message);
		}
	}

	private void TryCommitPendingActiveUnarmedTargetFlee()
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (!_pendingActiveUnarmedTargetFlee || agents == null)
			{
				return;
			}
			if (mission.CurrentTime < _pendingActiveUnarmedTargetFleeAtMissionTime)
			{
				return;
			}
			Agent agent = agents.FirstOrDefault(a => a != null && a.Index == _pendingActiveUnarmedTargetFleeAgentIndex);
			bool flag = agent != null && agent.IsActive();
			bool flag2 = flag && ShouldFleeWhenArmedVictim(agent);
			bool flag3 = false;
			if (flag2)
			{
				flag3 = TryRemoveAgentFromOpponentFightSide(agent);
			}
			if (flag3)
			{
				TryForceUnarmedBystanderToFlee(agent);
				Logger.Log("SceneTaunt", $"Converted active unarmed civilian target to fleeing bystander after armed escalation delay. Agent={agent.Name}");
			}
			else
			{
				Logger.Log("SceneTaunt", $"Skipped converting active unarmed target after delay. Agent={(agent?.Name?.ToString() ?? "null")}, AgentIndex={_pendingActiveUnarmedTargetFleeAgentIndex}, Active={flag}, ShouldFlee={flag2}, Removed={flag3}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing active unarmed target flee failed: " + ex.Message);
		}
		finally
		{
			_pendingActiveUnarmedTargetFlee = false;
			_pendingActiveUnarmedTargetFleeAgentIndex = -1;
			_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
		}
	}

	private void ClearPendingActiveUnarmedTargetFlee()
	{
		_pendingActiveUnarmedTargetFlee = false;
		_pendingActiveUnarmedTargetFleeAgentIndex = -1;
		_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
	}

	private void TryForceActiveUnarmedTargetFleeFallback()
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (!_armedConflict || !_pendingActiveUnarmedTargetFlee || agents == null || _pendingActiveUnarmedTargetFleeAgentIndex < 0)
			{
				return;
			}
			if (mission.CurrentTime < _pendingActiveUnarmedTargetFleeAtMissionTime)
			{
				return;
			}
			Agent agent = agents.FirstOrDefault(a => a != null && a.Index == _pendingActiveUnarmedTargetFleeAgentIndex);
			if (agent == null || !agent.IsActive())
			{
				ClearPendingActiveUnarmedTargetFlee();
				return;
			}
			if (!ShouldFleeWhenArmedVictim(agent))
			{
				ClearPendingActiveUnarmedTargetFlee();
				return;
			}
			if (!_opponentAgentIndices.Contains(agent.Index))
			{
				TryForceUnarmedBystanderToFlee(agent);
				ClearPendingActiveUnarmedTargetFlee();
				return;
			}
			if (!TryRemoveAgentFromOpponentFightSide(agent))
			{
				return;
			}
			TryForceUnarmedBystanderToFlee(agent);
			ClearPendingActiveUnarmedTargetFlee();
			Logger.Log("SceneTaunt", $"Fallback-converted active unarmed civilian target to fleeing bystander after armed escalation. Agent={agent.Name}, AgentIndex={agent.Index}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Fallback-converting active unarmed target after armed escalation failed: " + ex.Message);
		}
	}

	private void TryMaintainRecentlyNeutralizedFleeingCivilians()
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (agents == null || _recentNeutralizedFleeingCivilianUntilMissionTime.Count == 0)
			{
				return;
			}
			float currentTime = mission.CurrentTime;
			foreach (int item in _recentNeutralizedFleeingCivilianUntilMissionTime.Keys.ToList())
			{
				if (!_recentNeutralizedFleeingCivilianUntilMissionTime.TryGetValue(item, out var value) || currentTime > value)
				{
					_recentNeutralizedFleeingCivilianUntilMissionTime.Remove(item);
					continue;
				}
				Agent agent = agents.FirstOrDefault(a => a != null && a.Index == item);
				if (agent == null || !agent.IsActive() || _opponentAgentIndices.Contains(item) || !ShouldFleeWhenArmedVictim(agent))
				{
					_recentNeutralizedFleeingCivilianUntilMissionTime.Remove(item);
					continue;
				}
				TryForceUnarmedBystanderToFlee(agent);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining recently neutralized fleeing civilians failed: " + ex.Message);
		}
	}

	private void TryMaintainHostileUnarmedOpponentsFleeing()
	{
		try
		{
			var agents = Mission.Current?.Agents;
			if (!_conflictActive || !_armedConflict || agents == null || _opponentAgentIndices.Count == 0)
			{
				return;
			}
			foreach (int item in _opponentAgentIndices.ToList())
			{
				Agent agent = agents.FirstOrDefault(a => a != null && a.Index == item);
				if (agent == null || !agent.IsActive() || !ShouldFleeWhenArmedVictim(agent))
				{
					continue;
				}
				TryForceUnarmedBystanderToFlee(agent);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining hostile unarmed opponents fleeing failed: " + ex.Message);
		}
	}

	private void TryCommitPendingPlayerRearmAfterArmedConflictEnd()
	{
		try
		{
			Mission mission = Mission.Current;
			if (!_pendingPlayerRearmAfterArmedConflictEnd || mission == null)
			{
				return;
			}
			if (mission.CurrentTime < _pendingPlayerRearmAfterArmedConflictEndAtMissionTime)
			{
				return;
			}
			if (!SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
			{
				return;
			}
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || IsAgentUsingRealWeapon(main))
			{
				return;
			}
			// Keep weapons available after the fight, but don't force-draw them.
			// Otherwise the player can never manually sheath during armed carryover.
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Re-arming player after armed conflict end failed: " + ex.Message);
		}
		finally
		{
			ClearPendingPlayerRearmAfterArmedConflictEnd();
		}
	}

	private void ClearPendingPlayerRearmAfterArmedConflictEnd()
	{
		_pendingPlayerRearmAfterArmedConflictEnd = false;
		_pendingPlayerRearmAfterArmedConflictEndAtMissionTime = -1f;
	}

	private void TryMaintainMainAgentArmedPresence()
	{
		try
		{
			if (Mission.Current == null || !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
			{
				return;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false)
			{
				return;
			}
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || IsAgentUsingRealWeapon(main) || !IsAgentCarryingRealWeapon(main))
			{
				return;
			}
			// Respect manual sheathing during armed conflict / carryover.
			// Auto-wielding every tick makes the player unable to put weapons away.
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining main agent armed presence failed: " + ex.Message);
		}
	}

	private void TryMaintainSetsSelectedFollowerArmedReadiness()
	{
		try
		{
			Mission mission = Mission.Current;
			if (!_conflictActive || !_armedConflict || mission?.Agents == null || mission.CurrentTime < _nextSetsFollowerArmedReadinessMissionTime)
			{
				return;
			}
			_nextSetsFollowerArmedReadinessMissionTime = mission.CurrentTime + 0.5f;
			foreach (Agent agent in mission.Agents)
			{
				if (!SetsCityConflictPolicy.ShouldEnsureArmedReadiness(
						isSelectedEntryFollower: IsSetsSelectedEntryFollower(agent),
						side: _playerAgentIndices.Contains(agent?.Index ?? -1) ? SetsCityConflictSide.Player : SetsCityConflictSide.None,
						armedConflict: true))
				{
					continue;
				}
				TryRestoreWeaponsAfterUnarmedConflict(agent);
				EnsureSetsFollowerArmedCombatReadyForExternal(agent);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining SETS follower armed readiness failed: " + ex.Message);
		}
	}

	private void TryStripWeaponsForUnarmedConflict(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || _cachedUnarmedConflictEquipment.ContainsKey(agent.Index))
			{
				return;
			}
			MissionEquipment missionEquipment = new MissionEquipment();
			missionEquipment.FillFrom(agent.Equipment);
			_cachedUnarmedConflictEquipment[agent.Index] = missionEquipment;
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				try
				{
					agent.RemoveEquippedWeapon(equipmentIndex);
				}
				catch
				{
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Stripping weapons for unarmed conflict failed: " + ex.Message);
		}
	}

	private void TryRestoreWeaponsAfterUnarmedConflict(Agent agent)
	{
		try
		{
			if (agent == null || !_cachedUnarmedConflictEquipment.TryGetValue(agent.Index, out var value))
			{
				return;
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				try
				{
					MissionWeapon missionWeapon = value[equipmentIndex];
					agent.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon);
				}
				catch
				{
				}
			}
			_cachedUnarmedConflictEquipment.Remove(agent.Index);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Restoring weapons after unarmed conflict failed: " + ex.Message);
		}
	}

	private bool IsPlayerAttemptingWeaponDrawInput(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || agent.IsSitting() || !HasAvailableRealWeaponForEscalation(agent))
			{
				return false;
			}
			return Input.IsKeyPressed(InputKey.D1) || Input.IsKeyPressed(InputKey.D2) || Input.IsKeyPressed(InputKey.D3) || Input.IsKeyPressed(InputKey.D4) || Input.IsKeyPressed(InputKey.Numpad1) || Input.IsKeyPressed(InputKey.Numpad2) || Input.IsKeyPressed(InputKey.Numpad3) || Input.IsKeyPressed(InputKey.Numpad4) || Input.IsKeyPressed(InputKey.MouseScrollUp) || Input.IsKeyPressed(InputKey.MouseScrollDown);
		}
		catch
		{
			return false;
		}
	}

	private bool HasAvailableRealWeaponForEscalation(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return false;
			}
			if (IsAgentCarryingRealWeapon(agent))
			{
				return true;
			}
			if (!_cachedUnarmedConflictEquipment.TryGetValue(agent.Index, out var value))
			{
				return false;
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (IsMissionWeaponRealWeapon(value[equipmentIndex]))
				{
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Checking player real weapon availability for escalation failed: " + ex.Message);
		}
		return false;
	}

	private void RestoreAllCachedWeapons()
	{
		foreach (int item in _cachedUnarmedConflictEquipment.Keys.ToList())
		{
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == item);
				if (agent != null)
				{
					TryRestoreWeaponsAfterUnarmedConflict(agent);
				}
			}
			catch
			{
			}
		}
		_cachedUnarmedConflictEquipment.Clear();
	}

	private void ApplyLordSceneFightConsequences(Hero targetHero)
	{
		try
		{
			SceneTauntBehavior.QueueDeferredLordSceneDiplomacyForExternal(targetHero, "scene_taunt_lord_scene");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing lord scene fight consequences failed: " + ex.Message);
		}
	}

	private void EscalateToArmedConflict(string reason, bool suppressAnnouncement = false)
	{
		if (!_conflictActive || _armedConflict)
		{
			return;
		}
		long totalStart = StartPerfTimer();
		LogPerfPoint("escalate.start", $"reason={reason ?? "N/A"} suppressAnnouncement={suppressAnnouncement}");
		ClearMissionFightHandlerPendingFinishTimer();
		_armedConflict = true;
		_armedConflictOccurredThisConflict = true;
		_lastArmedEscalationAtMissionTime = Mission.Current?.CurrentTime ?? -1f;
		InitializeArmedConflictReactionSchedule();
		_armedCarryoverHandledInThisMission = true;
		SceneTauntBehavior.MarkArmedCarryoverForCurrentSettlement(reason);
		_blockedAiWeaponAgentIndices.Clear();
		long sectionStart = StartPerfTimer();
		int playerGuardAdds = 0;
		int opponentGuardAdds = 0;
		Agent activeTarget = Mission.Current?.Agents?.FirstOrDefault(candidate => candidate != null && candidate.Index == _activeTargetAgentIndex);
		foreach (int guardAgentIndex in _guardAgentIndices.ToList())
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == guardAgentIndex);
			if (agent != null && agent.IsActive())
			{
				SetsCityConflictSide guardSide = ResolveCityConflictSide(agent, activeTarget, isTargetEscort: false, armedConflict: true);
				if (guardSide == SetsCityConflictSide.Player)
				{
					AddAgentToFightSide(agent, isPlayerSide: true);
					playerGuardAdds++;
				}
				else if (guardSide == SetsCityConflictSide.Opponent)
				{
					AddAgentToFightSide(agent, isPlayerSide: false);
					opponentGuardAdds++;
				}
			}
		}
		LogPerfElapsed("escalate.addGuards", sectionStart, $"playerGuardAdds={playerGuardAdds} opponentGuardAdds={opponentGuardAdds} guardIndexCount={_guardAgentIndices.Count}");
		sectionStart = StartPerfTimer();
		int conflictAgents = 0;
		foreach (Agent agent2 in EnumerateConflictAgents(includeGuards: true))
		{
			if (agent2 == null || !agent2.IsActive())
			{
				continue;
			}
			conflictAgents++;
			TryRestoreWeaponsAfterUnarmedConflict(agent2);
			TryAlarmAgent(agent2);
			TryArmAgent(agent2);
		}
		LogPerfElapsed("escalate.armConflictAgents", sectionStart, $"processed={conflictAgents}");
		TryMaintainSetsSelectedFollowerArmedReadiness();
		sectionStart = StartPerfTimer();
		TryConvertUnarmedCivilianOpponentsToFleeingBystanders();
		LogPerfElapsed("escalate.convertUnarmedOpponents", sectionStart);
		sectionStart = StartPerfTimer();
		QueuePendingActiveUnarmedTargetFleeIfNeeded();
		LogPerfElapsed("escalate.queueActiveTargetFlee", sectionStart);
		sectionStart = StartPerfTimer();
		ForceAllNonPlayerSceneAgentsMortal();
		LogPerfElapsed("escalate.forceAllMortal", sectionStart, null, SceneTauntPerfHeavyStageThresholdMs);
		sectionStart = StartPerfTimer();
		EnsureCrimeRatingAtLeast(SceneTauntInitialArmedCrimeAmount);
		LogPerfElapsed("escalate.ensureCrime", sectionStart);
		sectionStart = StartPerfTimer();
		AlarmNearbyBystanders();
		LogPerfElapsed("escalate.alarmNearbyBystanders", sectionStart, null, SceneTauntPerfHeavyStageThresholdMs);
		sectionStart = StartPerfTimer();
		bool openingReactionStarted = TryAppendPlayerBehaviorFactForArmedEscalation(reason);
		if (!openingReactionStarted)
		{
			_nextArmedConflictReactionMissionTime = Mission.Current?.CurrentTime ?? 0f;
		}
		TryAppendGuardBehaviorFactsForArmedEscalation();
		LogPerfElapsed("escalate.appendBehaviorFacts", sectionStart);
		_openedAsUnarmedBrawl = false;
		if (!suppressAnnouncement)
		{
			AnimusForgeQuickInfo.Show(IsCurrentSettlementControlledByPlayer()
				? "持械冲突爆发，你的随行士兵和本地守卫开始保护你。"
				: "持械冲突爆发，守卫开始敌视你和你的同伴。");
		}
		Logger.Log("SceneTaunt", $"Escalated scene conflict to armed combat. Reason={reason}, Target={_activeTargetName}, PlayerGuards={playerGuardAdds}, OpponentGuards={opponentGuardAdds}");
		LogPerfPoint("escalate.end", $"reason={reason ?? "N/A"} elapsedMs={GetElapsedPerfMs(totalStart):0.###}");
	}

	private void TryConvertUnarmedCivilianOpponentsToFleeingBystanders()
	{
		try
		{
			foreach (int item in _opponentAgentIndices.ToList())
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault(x => x != null && x.Index == item);
				if (agent != null && agent.Index == _activeTargetAgentIndex)
				{
					continue;
				}
				if (agent == null || !ShouldFleeWhenArmedVictim(agent))
				{
					continue;
				}
				if (!TryRemoveAgentFromOpponentFightSide(agent))
				{
					continue;
				}
				TryForceUnarmedBystanderToFlee(agent);
				Logger.Log("SceneTaunt", $"Converted unarmed civilian opponent to fleeing bystander during armed escalation. Agent={agent.Name}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Converting unarmed civilian opponents to fleeing bystanders failed: " + ex.Message);
		}
	}

	private bool TryRemoveAgentFromOpponentFightSide(Agent agent)
	{
		try
		{
			if (agent == null || _fightHandler == null)
			{
				return false;
			}
			List<Agent> list = OpponentSideAgentsField?.GetValue(_fightHandler) as List<Agent>;
			Dictionary<Agent, Team> dictionary = OpponentSideOldTeamDataField?.GetValue(_fightHandler) as Dictionary<Agent, Team>;
			if (list == null || !list.Remove(agent))
			{
				return false;
			}
			_opponentAgentIndices.Remove(agent.Index);
			ReleaseArmedBystanderWatcher(agent);
			Team team = null;
			if (dictionary != null && dictionary.TryGetValue(agent, out team))
			{
				dictionary.Remove(agent);
			}
			try
			{
				CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
				AlarmedBehaviorGroup behaviorGroup = component?.AgentNavigator?.GetBehaviorGroup<AlarmedBehaviorGroup>();
				behaviorGroup?.DisableScriptedBehavior();
			}
			catch
			{
			}
			try
			{
				if (team != null)
				{
					agent.SetTeam(new Team(team.MBTeam, BattleSideEnum.None, Mission.Current, uint.MaxValue, uint.MaxValue, null), true);
				}
			}
			catch
			{
			}
			try
			{
				if (agent.IsAIControlled)
				{
					agent.ResetEnemyCaches();
					agent.InvalidateTargetAgent();
					agent.InvalidateAIWeaponSelections();
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
			}
			catch
			{
			}
			Mission mission = Mission.Current;
			if (mission != null && ShouldFleeWhenArmedVictim(agent))
			{
				_recentNeutralizedFleeingCivilianUntilMissionTime[agent.Index] = mission.CurrentTime + 6f;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Removing agent from opponent fight side failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldFleeWhenArmedVictim(Agent agent)
	{
		try
		{
			CharacterObject characterObject = agent?.Character as CharacterObject;
			if (agent == null || characterObject == null || !agent.IsHuman || !agent.IsActive() || characterObject.IsHero)
			{
				return false;
			}
			if (IsAuthorityPhysicalAttackTarget(null, characterObject))
			{
				return false;
			}
			return !IsAgentCarryingRealWeapon(agent);
		}
		catch
		{
			return false;
		}
	}

	private void EnsureCrimeRatingAtLeast(float targetCrimeAmount)
	{
		try
		{
			if (_suppressSettlementConsequencesForCurrentConflict)
			{
				return;
			}
			IFaction mapFaction = Settlement.CurrentSettlement?.MapFaction;
			if (mapFaction == null || targetCrimeAmount <= _appliedCrimeRatingAmount)
			{
				return;
			}
			float num = targetCrimeAmount - _appliedCrimeRatingAmount;
			if (num <= 0f)
			{
				return;
			}
			ApplySceneTauntCrimeWithDeferredCap(mapFaction, num, "scene_taunt_armed_escalation");
			_appliedCrimeRatingAmount += num;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Ensuring crime rating for armed conflict failed: " + ex.Message);
		}
	}

	private void ForceAllNonPlayerSceneAgentsMortal()
	{
		try
		{
			long startTimestamp = StartPerfTimer();
			int scanned = 0;
			int eligible = 0;
			var agents = Mission.Current?.Agents;
			if (agents == null)
			{
				LogPerfElapsed("forceAllMortal.inner", startTimestamp, $"scanned={scanned} eligible={eligible}", SceneTauntPerfHeavyStageThresholdMs);
				return;
			}
			foreach (Agent agent in agents)
			{
				scanned++;
				if (agent == null || !agent.IsHuman || !agent.IsActive() || _playerAgentIndices.Contains(agent.Index))
				{
					continue;
				}
				eligible++;
				TryForceAgentMortal(agent);
			}
			LogPerfElapsed("forceAllMortal.inner", startTimestamp, $"scanned={scanned} eligible={eligible}", SceneTauntPerfHeavyStageThresholdMs);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing scene agents mortal failed: " + ex.Message);
		}
	}

	private static void TryForceAgentMortal(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			if (agent.CurrentMortalityState != Agent.MortalityState.Mortal)
			{
				agent.SetMortalityState(Agent.MortalityState.Mortal);
				Logger.Log("SceneTaunt", $"Forced agent to mortal state during armed conflict. Agent={agent.Name}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing agent mortal failed: " + ex.Message);
		}
	}

	private void AddAgentToFightSide(Agent agent, bool isPlayerSide)
	{
		try
		{
			if (agent == null || !agent.IsActive() || _fightHandler == null)
			{
				return;
			}
			if (!isPlayerSide && IsSetsSelectedEntryFollower(agent))
			{
				Logger.Log("SceneTaunt", $"Redirected SETS selected follower away from opponent side. Agent={agent.Name}, AgentIndex={agent.Index}");
				isPlayerSide = true;
			}
			if (isPlayerSide)
			{
				if (_opponentAgentIndices.Contains(agent.Index) && !TryRemoveAgentFromOpponentFightSide(agent))
				{
					Logger.Log("SceneTaunt", $"Rejected ambiguous side reassignment because opponent removal failed. Agent={agent.Name}, AgentIndex={agent.Index}");
					return;
				}
				if (_playerAgentIndices.Contains(agent.Index))
				{
					return;
				}
			}
			else
			{
				if (_playerAgentIndices.Contains(agent.Index))
				{
					Logger.Log("SceneTaunt", $"Rejected attempt to move player-side agent to opponent side. Agent={agent.Name}, AgentIndex={agent.Index}");
					return;
				}
				if (_opponentAgentIndices.Contains(agent.Index))
				{
					return;
				}
			}
			ReleaseArmedBystanderWatcher(agent);
			Team team = agent.Team;
			_fightHandler.AddAgentToSide(agent, isPlayerSide);
			FixMissionFightHandlerStoredTeam(agent, isPlayerSide, team);
			_recentNeutralizedFleeingCivilianUntilMissionTime.Remove(agent.Index);
			if (isPlayerSide)
			{
				_playerAgentIndices.Add(agent.Index);
				_opponentAgentIndices.Remove(agent.Index);
				_sceneGoldEligibleAgentIndices.Remove(agent.Index);
			}
			else
			{
				_opponentAgentIndices.Add(agent.Index);
				_playerAgentIndices.Remove(agent.Index);
				RegisterSceneGoldEligibleAgent(agent, "add_to_opponent_side");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "AddAgentToFightSide failed: " + ex.Message);
		}
	}

	private void FixMissionFightHandlerStoredTeam(Agent agent, bool isPlayerSide, Team originalTeam)
	{
		try
		{
			FieldInfo fieldInfo = (isPlayerSide ? PlayerSideOldTeamDataField : OpponentSideOldTeamDataField);
			Dictionary<Agent, Team> dictionary = fieldInfo?.GetValue(_fightHandler) as Dictionary<Agent, Team>;
			if (dictionary == null || agent == null || originalTeam == null)
			{
				return;
			}
			dictionary[agent] = originalTeam;
		}
		catch
		{
		}
	}

	private IEnumerable<Agent> EnumerateConflictAgents(bool includeGuards)
	{
		long startTimestamp = StartPerfTimer();
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_opponentAgentIndices);
		if (includeGuards)
		{
			hashSet.UnionWith(_guardAgentIndices);
		}
		List<Agent> list = new List<Agent>();
		foreach (int item in hashSet)
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == item);
			if (agent != null)
			{
				list.Add(agent);
			}
		}
		LogPerfElapsed("enumerateConflictAgents", startTimestamp, $"includeGuards={includeGuards} indexCount={hashSet.Count} resolved={list.Count}");
		return list;
	}

	private void ApplyBaseConsequences(CharacterObject targetCharacter, float crimeRatingAmount)
	{
		if (_baseConsequencesApplied)
		{
			return;
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null)
		{
			return;
		}
		if (IsSettlementCriminalConflictTarget(targetCharacter?.HeroObject, targetCharacter))
		{
			Logger.Log("SceneTaunt", $"Skipped settlement trust/crime consequences for criminal target conflict. Target={targetCharacter?.Name}");
			return;
		}
		_baseConsequencesApplied = true;
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				if (targetCharacter != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(targetCharacter, out var kind))
				{
					RewardSystemBehavior.Instance.AdjustSettlementMerchantTrustForExternal(currentSettlement, kind, -10, "scene_taunt_brawl");
					AnimusForgeQuickInfo.Show($"{currentSettlement.Name} 的{GetMerchantTrustLabel(kind)}信任 -10。");
				}
				else
				{
					RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(currentSettlement, -10, "scene_taunt_brawl");
					AnimusForgeQuickInfo.Show($"{currentSettlement.Name} 的公共信任 -10。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying trust consequence failed: " + ex.Message);
		}
		try
		{
			if (currentSettlement.MapFaction != null)
			{
				float num = MathF.Max(0f, crimeRatingAmount);
				if (num > 0f)
				{
					ApplySceneTauntCrimeWithDeferredCap(currentSettlement.MapFaction, num, "scene_taunt_conflict_started");
					_appliedCrimeRatingAmount = num;
				}
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("SceneTaunt", "Applying crime consequence failed: " + ex2.Message);
		}
	}

	private void EnableSettlementConsequencesForCurrentConflict(CharacterObject targetCharacter, Hero targetHero, float crimeRatingAmount, string reason)
	{
		if (!_suppressSettlementConsequencesForCurrentConflict)
		{
			return;
		}
		_suppressSettlementConsequencesForCurrentConflict = false;
		ApplyBaseConsequences(targetCharacter, crimeRatingAmount);
		if (SceneTauntBehavior.IsSceneLordTauntTarget(targetHero))
		{
			ApplyLordSceneFightConsequences(targetHero);
		}
		Logger.Log("SceneTaunt", $"Settlement crime/trust consequences were enabled for current conflict. Reason={reason}, Target={targetCharacter?.Name?.ToString() ?? targetHero?.Name?.ToString() ?? "N/A"}");
	}

	private static float ApplySceneTauntCrimeWithCap(IFaction faction, float requestedAmount)
	{
		try
		{
			if (faction == null)
			{
				return 0f;
			}
			float num = MathF.Max(0f, requestedAmount);
			if (num <= 0f)
			{
				return 0f;
			}
			float num2 = MathF.Max(0f, faction.MainHeroCrimeRating);
			float num3 = MathF.Max(0f, SceneTauntCrimeCapBeforeWar - num2);
			float num4 = MathF.Min(num, num3);
			if (num4 <= 0f)
			{
				Logger.Log("SceneTaunt", $"Crime increase skipped because faction crime is already at cap. Faction={faction.Name}, Current={num2:0.##}, Cap={SceneTauntCrimeCapBeforeWar:0.##}");
				return 0f;
			}
			ChangeCrimeRatingAction.Apply(faction, num4, true);
			Logger.Log("SceneTaunt", $"Applied capped scene-taunt crime. Faction={faction.Name}, Requested={num:0.##}, Applied={num4:0.##}, Result={MathF.Min(SceneTauntCrimeCapBeforeWar, num2 + num4):0.##}");
			return num4;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying capped scene-taunt crime failed: " + ex.Message);
			return 0f;
		}
	}

	private void ApplySceneTauntCrimeWithDeferredCap(IFaction faction, float requestedAmount, string reason)
	{
		try
		{
			float num = MathF.Max(0f, requestedAmount);
			if (faction == null || num <= 0f)
			{
				return;
			}
			float num2 = ApplySceneTauntCrimeWithCap(faction, num);
			float num3 = MathF.Max(0f, num - num2);
			if (num3 > 0f)
			{
				QueueDeferredCrimeForFaction(faction, num3, reason);
			}
			if (num2 > 0f || num3 > 0f)
			{
				SceneTauntBehavior.TryShowTrackedCrimeTotalMessageForExternal(faction);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying deferred-cap scene-taunt crime failed: " + ex.Message);
		}
	}

	private void QueueDeferredCrimeForFaction(IFaction faction, float amount, string reason)
	{
		SceneTauntBehavior.QueueDeferredCrimeForExternal(faction, amount, reason);
	}

	private void TryApplyArmedNpcKnockdownConsequences(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			if (!_conflictActive || !_armedConflict || affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			TryApplyArmedNpcKnockdownConsequencesCore(
				affectedAgent,
				affectorAgent,
				agentState,
				"scene_taunt_armed_knockdown");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC armed knockdown consequences failed: " + ex.Message);
		}
	}

	private void TryApplyArmedNpcKnockdownConsequencesCore(
		Agent affectedAgent,
		Agent affectorAgent,
		AgentState agentState,
		string reason)
	{
		try
		{
			if (affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
			{
				return;
			}
			Hero affectedHero = (affectedAgent.Character as CharacterObject)?.HeroObject;
			bool affectedIsPlayerSide = _playerAgentIndices.Contains(affectedAgent.Index)
				|| IsSetsSelectedEntryFollower(affectedAgent)
				|| SceneTauntBehavior.IsPlayerMainPartyHero(affectedHero);
			if (affectedIsPlayerSide || !_penalizedArmedKnockdownAgentIndices.Add(affectedAgent.Index))
			{
				return;
			}
			Hero affectorHero = (affectorAgent?.Character as CharacterObject)?.HeroObject;
			bool affectorIsPlayerSide = affectorAgent == Agent.Main
				|| (affectorAgent != null
					&& (_playerAgentIndices.Contains(affectorAgent.Index)
						|| IsSetsSelectedEntryFollower(affectorAgent)
						|| SceneTauntBehavior.IsPlayerMainPartyHero(affectorHero)));
			if (!affectorIsPlayerSide)
			{
				return;
			}
			CharacterObject characterObject = affectedAgent.Character as CharacterObject;
			ApplyPerNpcKnockdownConsequences(affectedAgent, characterObject, affectedAgent.Name?.ToString());
			float crimeAmount = IsSettlementCriminalConflictTarget(characterObject?.HeroObject, characterObject) ? 0f : SceneTauntPerKnockdownCrimeAmount;
			TryRecordPlayerSceneConflictRecentAction(
				affectedAgent,
				affectorAgent,
				agentState == AgentState.Killed ? "killed" : "unconscious",
				reason,
				crimeAmount);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC armed knockdown consequences failed: " + ex.Message);
		}
	}

	private void ApplyPerNpcKnockdownConsequences(Agent victimAgent, CharacterObject victimCharacter, string victimName)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement == null)
		{
			return;
		}
		string text = victimName ?? victimCharacter?.Name?.ToString() ?? "目标";
		if (IsSettlementCriminalConflictTarget(victimCharacter?.HeroObject, victimCharacter))
		{
			TryRewardSettlementTrustForCriminalKnockdown(currentSettlement, text);
			Hero hero = TryResolveCriminalOwnerHeroFromAgent(victimAgent);
			if (hero != null)
			{
				TryApplyCriminalOwnerPenalty(hero, text);
			}
			Logger.Log("SceneTaunt", $"Handled criminal target knockdown consequences. Victim={text}, Owner={hero?.Name?.ToString() ?? "N/A"}");
			return;
		}
		try
		{
			if (RewardSystemBehavior.Instance != null)
			{
				if (victimCharacter != null && RewardSystemBehavior.Instance.TryGetSettlementMerchantKind(victimCharacter, out var kind))
				{
					RewardSystemBehavior.Instance.AdjustSettlementMerchantTrustForExternal(currentSettlement, kind, -SceneTauntPerKnockdownTrustPenalty, "scene_taunt_armed_knockdown");
					AnimusForgeQuickInfo.Show($"击倒 {text}：{currentSettlement.Name} 的{GetMerchantTrustLabel(kind)}信任 -{SceneTauntPerKnockdownTrustPenalty}。");
				}
				else
				{
					RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(currentSettlement, -SceneTauntPerKnockdownTrustPenalty, "scene_taunt_armed_knockdown");
					AnimusForgeQuickInfo.Show($"击倒 {text}：{currentSettlement.Name} 的公共信任 -{SceneTauntPerKnockdownTrustPenalty}。");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC knockdown trust consequence failed: " + ex.Message);
		}
		try
		{
			if (currentSettlement.MapFaction != null)
			{
				ApplySceneTauntCrimeWithDeferredCap(currentSettlement.MapFaction, SceneTauntPerKnockdownCrimeAmount, "scene_taunt_armed_knockdown");
				_appliedCrimeRatingAmount += SceneTauntPerKnockdownCrimeAmount;
				AnimusForgeQuickInfo.Show($"击倒 {text}：累计犯罪度 +{SceneTauntPerKnockdownCrimeAmount:0.#}。超出 59 的部分会在离开定居点后再结算。");
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC knockdown crime consequence failed: " + ex2.Message);
		}
	}

	private void AlarmNearbyBystanders()
	{
		long startTimestamp = StartPerfTimer();
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_opponentAgentIndices);
		hashSet.UnionWith(_guardAgentIndices);
		Agent main = Agent.Main;
		int scanned = 0;
		int inRadius = 0;
		int joined = 0;
		int fled = 0;
		try
		{
			var agents = Mission.Current?.Agents;
			if (agents == null)
			{
				LogPerfElapsed("alarmNearbyBystanders.inner", startTimestamp, $"scanned={scanned} inRadius={inRadius} joined={joined} fled={fled}", SceneTauntPerfHeavyStageThresholdMs);
				return;
			}
			foreach (Agent agent in agents)
			{
				scanned++;
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index) || !IsAgentWithinArmedBystanderReactionRadius(agent, main))
				{
					continue;
				}
				inRadius++;
				TryAlarmAgent(agent);
				if (!TryJoinArmedBystanderToConflict(agent))
				{
					TryForceUnarmedBystanderToFlee(agent);
					fled++;
				}
				else
				{
					joined++;
				}
			}
			LogPerfElapsed("alarmNearbyBystanders.inner", startTimestamp, $"scanned={scanned} inRadius={inRadius} joined={joined} fled={fled}", SceneTauntPerfHeavyStageThresholdMs);
		}
		catch
		{
		}
	}

	private void TryMaintainArmedBystanderReactions()
	{
		try
		{
			Mission mission = Mission.Current;
			var agents = mission?.Agents;
			if (!_conflictActive || !_armedConflict || agents == null)
			{
				return;
			}
			float currentTime = mission.CurrentTime;
			if (_lastArmedBystanderReactionRefreshAtMissionTime >= 0f && currentTime - _lastArmedBystanderReactionRefreshAtMissionTime < ArmedBystanderReactionRefreshIntervalSeconds)
			{
				return;
			}
			long startTimestamp = StartPerfTimer();
			_lastArmedBystanderReactionRefreshAtMissionTime = currentTime;
			_armedConflictReactionCandidates.Clear();
			_lastArmedConflictReactionCandidateRefreshAtMissionTime = currentTime;
			HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
			hashSet.UnionWith(_opponentAgentIndices);
			hashSet.UnionWith(_guardAgentIndices);
			Agent main = Agent.Main;
			int scanned = 0;
			int inRadius = 0;
			int joined = 0;
			int fled = 0;
			foreach (Agent agent in agents)
			{
				scanned++;
				TryCacheArmedConflictReactionCandidate(agent, main, currentTime);
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index) || !IsAgentWithinArmedBystanderReactionRadius(agent, main))
				{
					continue;
				}
				inRadius++;
				if (!TryJoinArmedBystanderToConflict(agent))
				{
					TryForceUnarmedBystanderToFlee(agent);
					fled++;
				}
				else
				{
					joined++;
				}
			}
			LogPerfElapsed("maintainArmedBystanders.inner", startTimestamp, $"scanned={scanned} inRadius={inRadius} joined={joined} fled={fled}", SceneTauntPerfHeavyStageThresholdMs);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Maintaining armed bystander reactions failed: " + ex.Message);
		}
	}

	private static bool IsAgentWithinArmedBystanderReactionRadius(Agent agent, Agent main)
	{
		try
		{
			if (agent == null || main == null || !main.IsActive())
			{
				return false;
			}
			float num = ArmedBystanderReactionRadiusMeters * ArmedBystanderReactionRadiusMeters;
			return agent.Position.AsVec2.DistanceSquared(main.Position.AsVec2) <= num;
		}
		catch
		{
			return false;
		}
	}

	private bool TryJoinArmedBystanderToConflict(Agent agent)
	{
		try
		{
			if (!ShouldJoinArmedBystanderToConflict(agent))
			{
				return false;
			}
			ReleaseArmedBystanderWatcher(agent);
			if (!_opponentAgentIndices.Contains(agent.Index))
			{
				AddAgentToFightSide(agent, isPlayerSide: false);
			}
			TryForceAgentMortal(agent);
			TryAlarmAgent(agent);
			TryArmAgent(agent);
			TryWakeArmedBystanderCombatAi(agent);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Joining armed bystander to conflict failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldJoinArmedBystanderToConflict(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled)
			{
				return false;
			}
			if (IsSetsSelectedEntryFollower(agent))
			{
				return false;
			}
			if (IsCurrentSettlementControlledByPlayer())
			{
				return false;
			}
			if (SceneTauntBehavior.IsChildSceneProtectedTarget(agent.Character as CharacterObject))
			{
				return false;
			}
			return IsAgentCarryingRealWeapon(agent);
		}
		catch
		{
			return false;
		}
	}

	private static void TryForceUnarmedBystanderToFlee(Agent agent)
	{
		try
		{
			if (!ShouldForceUnarmedBystanderToFlee(agent))
			{
				return;
			}
			agent.SetLookAgent(null);
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
			agent.DisableScriptedMovement();
			if (TryForceUnarmedBystanderDirectRetreat(agent))
			{
				return;
			}
			AlarmedBehaviorGroup behaviorGroup = EnsureAlarmedBehaviorGroup(agent);
			if (behaviorGroup == null)
			{
				return;
			}
			FleeBehavior behavior = behaviorGroup.GetBehavior<FleeBehavior>();
			if (behavior == null)
			{
				behavior = behaviorGroup.AddBehavior<FleeBehavior>();
			}
			if (behaviorGroup.ScriptedBehavior != behavior)
			{
				behaviorGroup.SetScriptedBehavior<FleeBehavior>();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing unarmed bystander to flee failed: " + ex.Message);
		}
	}

	private static bool TryForceUnarmedBystanderDirectRetreat(Agent agent)
	{
		try
		{
			Agent main = Agent.Main;
			Mission current = Mission.Current;
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			if (agent == null || main == null || !main.IsActive() || current?.Scene == null || agentNavigator == null)
			{
				return false;
			}
			Vec2 asVec = agent.Position.AsVec2;
			Vec2 asVec2 = main.Position.AsVec2;
			Vec2 vec = asVec - asVec2;
			if (vec.LengthSquared < 0.04f)
			{
				vec = agent.Frame.rotation.f.AsVec2;
			}
			if (vec.LengthSquared < 0.04f)
			{
				vec = new Vec2(1f, 0f);
			}
			vec.Normalize();
			WorldPosition worldPosition = WorldPosition.Invalid;
			float num = float.MinValue;
			for (int i = 0; i < 16; i++)
			{
				bool flag = i % 2 == 0;
				Vec3 randomPositionAroundPoint = current.GetRandomPositionAroundPoint(agent.Position, 4f, 14f, flag);
				WorldPosition worldPosition2 = new WorldPosition(current.Scene, randomPositionAroundPoint);
				if (worldPosition2.GetNearestNavMesh() == UIntPtr.Zero)
				{
					continue;
				}
				Vec2 vec2 = worldPosition2.AsVec2 - asVec;
				if (vec2.LengthSquared < 0.25f)
				{
					continue;
				}
				vec2.Normalize();
				float num2 = Vec2.DotProduct(vec2, vec);
				if (num2 < 0.2f)
				{
					continue;
				}
				float num3 = worldPosition2.AsVec2.DistanceSquared(asVec2) + num2 * 25f;
				if (num3 > num)
				{
					worldPosition = worldPosition2;
					num = num3;
				}
			}
			if (num <= 0f)
			{
				return false;
			}
			Vec2 vec3 = worldPosition.AsVec2 - asVec;
			float rotationInRadians = (vec3.LengthSquared > 0.04f) ? vec3.RotationInRadians : vec.RotationInRadians;
			agentNavigator.SetTargetFrame(worldPosition, rotationInRadians, 0.6f, -10f, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.NeverSlowDown, false);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing direct retreat target failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldForceUnarmedBystanderToFlee(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled)
			{
				return false;
			}
			if (IsSetsSelectedEntryFollower(agent))
			{
				return false;
			}
			return !IsAgentCarryingRealWeapon(agent);
		}
		catch
		{
			return false;
		}
	}

	private static AlarmedBehaviorGroup EnsureAlarmedBehaviorGroup(Agent agent)
	{
		try
		{
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			if (component == null)
			{
				return null;
			}
			AgentNavigator agentNavigator = component.AgentNavigator ?? component.CreateAgentNavigator();
			if (agentNavigator == null)
			{
				return null;
			}
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (behaviorGroup == null)
			{
				try
				{
					agentNavigator.AddBehaviorGroup<DailyBehaviorGroup>();
				}
				catch
				{
				}
				try
				{
					agentNavigator.AddBehaviorGroup<InterruptingBehaviorGroup>();
				}
				catch
				{
				}
				agentNavigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
				behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			}
			if (behaviorGroup != null && !behaviorGroup.HasBehavior<FleeBehavior>())
			{
				behaviorGroup.AddBehavior<FleeBehavior>();
			}
			return behaviorGroup;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Ensuring alarmed behavior group failed: " + ex.Message);
			return null;
		}
	}

	private static void TryWakeArmedBystanderCombatAi(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || !agent.IsAIControlled)
			{
				return;
			}
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
		try
		{
			agent.ResetEnemyCaches();
			agent.InvalidateTargetAgent();
			agent.InvalidateAIWeaponSelections();
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Waking armed bystander combat AI failed: " + ex.Message);
		}
	}

	private static bool IsAgentCarryingRealWeapon(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (IsMissionWeaponRealWeapon(agent.Equipment[equipmentIndex]))
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

	private static bool TryForceArmedCivilianBystanderToFlee(Agent agent)
	{
		try
		{
			if (!ShouldForceArmedCivilianBystanderToFlee(agent))
			{
				return false;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator;
			if (agentNavigator == null)
			{
				return false;
			}
			AlarmedBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>();
			if (behaviorGroup == null)
			{
				return false;
			}
			if (!behaviorGroup.HasBehavior<FleeBehavior>())
			{
				behaviorGroup.AddBehavior<FleeBehavior>();
			}
			behaviorGroup.SetScriptedBehavior<FleeBehavior>();
			Logger.Log("SceneTaunt", $"Forced armed civilian bystander to flee. Agent={agent.Name}");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Forcing armed civilian bystander to flee failed: " + ex.Message);
			return false;
		}
	}

	private static bool ShouldForceArmedCivilianBystanderToFlee(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled || !IsAgentUsingRealWeapon(agent))
			{
				return false;
			}
			if (IsSetsSelectedEntryFollower(agent))
			{
				return false;
			}
			CharacterObject characterObject = agent.Character as CharacterObject;
			if (characterObject == null || IsGuardLikeCharacter(characterObject) || SceneTauntBehavior.IsSceneLordTauntTarget(characterObject.HeroObject))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void TryForceArmedBystanderToWatchPlayer(Agent agent)
	{
		try
		{
			if (!ShouldForceArmedBystanderToWatchPlayer(agent))
			{
				return;
			}
			if (!_opponentAgentIndices.Contains(agent.Index))
			{
				AddAgentToFightSide(agent, isPlayerSide: false);
				TryForceAgentMortal(agent);
				TryAlarmAgent(agent);
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
			agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
			var worldPosition = agent.GetWorldPosition();
			agent.SetScriptedPosition(ref worldPosition, addHumanLikeDelay: false);
			_armedBystanderWatcherIndices.Add(agent.Index);
			Logger.Log("SceneTaunt", $"Frozen armed bystander inside player conflict. Agent={agent.Name}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Freezing armed bystander inside player conflict failed: " + ex.Message);
		}
	}

	private static bool ShouldForceArmedBystanderToWatchPlayer(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.IsMainAgent || !agent.IsAIControlled || !IsAgentUsingRealWeapon(agent))
			{
				return false;
			}
			if (IsSetsSelectedEntryFollower(agent))
			{
				return false;
			}
			return !ShouldForceArmedCivilianBystanderToFlee(agent);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSetsSelectedEntryFollower(Agent agent)
	{
		try
		{
			return SettlementEntryTroopSelectionBehavior.IsSetsSelectedFollowerAgentForExternal(agent);
		}
		catch
		{
			return false;
		}
	}

	private void ReleaseArmedBystanderWatcher(Agent agent)
	{
		try
		{
			if (agent == null || !_armedBystanderWatcherIndices.Remove(agent.Index))
			{
				return;
			}
			if (agent.IsActive())
			{
				agent.SetLookAgent(null);
				agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
				agent.DisableScriptedMovement();
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Releasing armed bystander watcher failed: " + ex.Message);
		}
	}

	private void ReleaseAllArmedBystanderWatchers()
	{
		foreach (int item in _armedBystanderWatcherIndices.ToList())
		{
			try
			{
				Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == item);
				ReleaseArmedBystanderWatcher(agent);
			}
			catch
			{
			}
		}
		_armedBystanderWatcherIndices.Clear();
	}

	private static void TryAlarmAgent(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		try
		{
			if (!agent.IsMainAgent)
			{
				AgentFlag agentFlags = agent.GetAgentFlags();
				agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
			}
		}
		catch
		{
		}
		try
		{
			AlarmedBehaviorGroup.AlarmAgent(agent);
		}
		catch
		{
		}
		try
		{
			agent.SetAlarmState(Agent.AIStateFlag.Alarmed);
		}
		catch
		{
		}
	}

	private static void TrySheathWeapons(Agent agent)
	{
		try
		{
			agent?.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
		try
		{
			agent?.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.Instant);
		}
		catch
		{
		}
	}

	private static void TryArmAgent(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
		}
		catch
		{
		}
		if (!IsAgentUsingRealWeapon(agent))
		{
			TryWieldFirstCarriedRealWeapon(agent);
		}
		if (!IsAgentCarryingRealWeapon(agent))
		{
			TryGiveFallbackSoldierWeapon(agent);
		}
		try
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}
		catch
		{
		}
	}

	private static void TryWieldFirstCarriedRealWeapon(Agent agent)
	{
		try
		{
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumAllWeaponSlots; slot++)
			{
				if (!IsMissionWeaponRealWeapon(agent.Equipment[slot]))
				{
					continue;
				}
				agent.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.Instant, false);
				if (IsAgentUsingRealWeapon(agent))
				{
					return;
				}
			}
		}
		catch
		{
		}
	}

	internal static void EnsureSetsFollowerArmedCombatReadyForExternal(Agent agent)
	{
		if (agent == null || !agent.IsHuman || !agent.IsActive() || !IsSetsSelectedEntryFollower(agent))
		{
			return;
		}
		try
		{
			agent.ResetEnemyCaches();
			agent.InvalidateTargetAgent();
			agent.InvalidateAIWeaponSelections();
		}
		catch
		{
		}
		TryAlarmAgent(agent);
		TryArmAgent(agent);
	}

	private static void TryGiveFallbackSoldierWeapon(Agent agent)
	{
		if (!ShouldReceiveFallbackSoldierWeapon(agent))
		{
			return;
		}
		try
		{
			ItemObject @object = Game.Current?.ObjectManager?.GetObject<ItemObject>(FallbackSoldierWeaponId);
			if (@object == null)
			{
				return;
			}
			EquipmentIndex fallbackWeaponSlot = FindFallbackWeaponSlot(agent);
			if (fallbackWeaponSlot == EquipmentIndex.None)
			{
				return;
			}
			MissionWeapon missionWeapon = new MissionWeapon(@object, null, agent.Origin?.Banner);
			agent.EquipWeaponWithNewEntity(fallbackWeaponSlot, ref missionWeapon);
			agent.TryToWieldWeaponInSlot(fallbackWeaponSlot, Agent.WeaponWieldActionType.Instant, false);
			Logger.Log("SceneTaunt", $"Granted fallback sword to scene soldier. Agent={agent.Name}, Slot={fallbackWeaponSlot}");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Granting fallback soldier weapon failed: " + ex.Message);
		}
	}

	private static bool ShouldReceiveFallbackSoldierWeapon(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || agent.IsMount)
			{
				return false;
			}
			if (IsSetsSelectedEntryFollower(agent))
			{
				return true;
			}
			CharacterObject characterObject = agent.Character as CharacterObject;
			if (characterObject == null)
			{
				return false;
			}
			switch (characterObject.Occupation)
			{
			case Occupation.Guard:
			case Occupation.PrisonGuard:
			case Occupation.Soldier:
				return true;
			default:
				return false;
			}
		}
		catch
		{
			return false;
		}
	}

	private static EquipmentIndex FindFallbackWeaponSlot(Agent agent)
	{
		try
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (agent.Equipment[equipmentIndex].IsEmpty)
				{
					return equipmentIndex;
				}
			}
			for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.NumAllWeaponSlots; equipmentIndex2++)
			{
				if (!IsMissionWeaponRealWeapon(agent.Equipment[equipmentIndex2]))
				{
					return equipmentIndex2;
				}
			}
			return EquipmentIndex.Weapon3;
		}
		catch
		{
			return EquipmentIndex.None;
		}
	}

	private static bool IsAgentUsingRealWeapon(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return false;
		}
		try
		{
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (IsRealWeaponWieldedSlot(agent, primaryWieldedItemIndex))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			return IsRealWeaponWieldedSlot(agent, offhandWieldedItemIndex);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsRealWeaponWieldedSlot(Agent agent, EquipmentIndex equipmentIndex)
	{
		try
		{
			if (agent == null || equipmentIndex == EquipmentIndex.None || equipmentIndex < EquipmentIndex.WeaponItemBeginSlot || equipmentIndex >= EquipmentIndex.NumAllWeaponSlots)
			{
				return false;
			}
			return IsMissionWeaponRealWeapon(agent.Equipment[equipmentIndex]);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMissionWeaponRealWeapon(MissionWeapon missionWeapon)
	{
		try
		{
			WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
			return currentUsageItem != null && !currentUsageItem.IsShield;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsWeaponComponentRealWeapon(WeaponComponentData attackerWeapon)
	{
		try
		{
			return attackerWeapon != null && !attackerWeapon.IsShield && attackerWeapon.WeaponClass != WeaponClass.Undefined;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsMissionWeaponRealWeapon(EquipmentElement equipmentElement)
	{
		try
		{
			ItemObject item = equipmentElement.Item;
			if (item == null)
			{
				return false;
			}
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			return primaryWeapon != null && !primaryWeapon.IsShield && item.Type != ItemObject.ItemTypeEnum.Shield;
		}
		catch
		{
			return false;
		}
	}

	private static void AddUniqueAgent(List<Agent> agents, Agent agent)
	{
		if (agent != null && !agents.Contains(agent))
		{
			agents.Add(agent);
		}
	}

	private static string GetMerchantTrustLabel(RewardSystemBehavior.SettlementMerchantKind kind)
	{
		return kind switch
		{
			RewardSystemBehavior.SettlementMerchantKind.Weapon => "武器市场",
			RewardSystemBehavior.SettlementMerchantKind.Armor => "盔甲市场",
			RewardSystemBehavior.SettlementMerchantKind.Horse => "马匹市场",
			RewardSystemBehavior.SettlementMerchantKind.Goods => "杂货市场",
			RewardSystemBehavior.SettlementMerchantKind.Blacksmith => "铁匠铺",
			_ => "市场"
		};
	}

	private void OnConflictFinished(bool playerWon)
	{
		Logger.Log("SceneTaunt", $"Scene taunt conflict ended. PlayerWon={playerWon}, Armed={_armedConflict}, Target={_activeTargetName}, Key={_activeTargetKey}");
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = _armedConflictOccurredThisConflict && (Agent.Main == null || !Agent.Main.IsActive());
			flag2 = _armedConflict && !flag && SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement() && Agent.Main != null && Agent.Main.IsActive();
		}
		catch
		{
			flag = false;
			flag2 = false;
		}
		ClearRuntimeState(flag);
		if (flag2)
		{
			QueuePendingPlayerRearmAfterArmedConflictEnd();
		}
	}

	private void ClearRuntimeState(bool preserveArmedDefeatState = false)
	{
		ReleaseAllArmedBystanderWatchers();
		ResetArmedConflictReactionBudget();
		RestoreAllCachedWeapons();
		_conflictActive = false;
		_armedConflict = false;
		_nextSetsFollowerArmedReadinessMissionTime = 0f;
		_baseConsequencesApplied = false;
		_appliedCrimeRatingAmount = 0f;
		_activeTargetKey = "";
		_activeTargetName = "";
		_activeTargetAgentIndex = -1;
		_openedAsUnarmedBrawl = false;
		_openedFromVerbalTaunt = false;
		_suppressSettlementConsequencesForCurrentConflict = false;
		_sceneAttackReleaseSuppressed = false;
		_pendingImmediateUnarmedFightEnd = false;
		_pendingImmediateUnarmedFightEndPlayerWon = false;
		_armedCarryoverSceneInitialized = false;
		_armedCarryoverNoAuthoritySceneNotified = false;
		_lastArmedCarryoverAttemptAtMissionTime = -1f;
		_pendingActiveUnarmedTargetFlee = false;
		_pendingActiveUnarmedTargetFleeAgentIndex = -1;
		_pendingActiveUnarmedTargetFleeAtMissionTime = -1f;
		ClearPendingPlayerUnarmedPrep();
		ClearPendingPlayerRearmAfterArmedConflictEnd();
		_sceneNotableRecentHitNonLethal.Clear();
		_sceneNotableDeferredBattleDeathCandidates.Clear();
		_playerAgentIndices.Clear();
		_opponentAgentIndices.Clear();
		_guardAgentIndices.Clear();
		_blockedAiWeaponAgentIndices.Clear();
		_penalizedArmedKnockdownAgentIndices.Clear();
		_recordedPlayerSceneConflictActionKeys.Clear();
		_playerSceneConflictActionSequence = 0;
		ClearOwnedSettlementPassiveAttackState("clear_runtime_state");
		if (!preserveArmedDefeatState)
		{
			_pendingPlayerBattleDeathAfterMission = false;
			_pendingPlayerBattleDeathDecisionCaptured = false;
			_pendingPlayerBattleDeathKiller = null;
			_armedConflictOccurredThisConflict = false;
			_armedDefeatOutcomeHandled = false;
			_armedDefeatWasCriminalConflict = false;
		}
	}

	private void RememberSceneNotableHitLethality(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, in Blow blow, float damagedHp)
	{
		try
		{
			bool ownedSettlementPassivePlayerHit = IsOwnedSettlementPassiveAttackScene() && affectorAgent == Agent.Main && IsValidOwnedSettlementPassiveAttackTarget(affectedAgent);
			if ((!_conflictActive && !ownedSettlementPassivePlayerHit) || damagedHp <= 0f || affectedAgent == null || !affectedAgent.IsHuman)
			{
				return;
			}
			CharacterObject characterObject = affectedAgent.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (!SceneTauntBehavior.IsSceneNotableTauntTarget(hero))
			{
				return;
			}
			bool flag = blow.DamageType == DamageTypes.Blunt || !IsWeaponComponentRealWeapon(attackerWeapon);
			_sceneNotableRecentHitNonLethal[hero] = flag;
			if (flag)
			{
				_sceneNotableDeferredBattleDeathCandidates.Remove(hero);
			}
			else
			{
				_sceneNotableDeferredBattleDeathCandidates.Add(hero);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Recording scene notable hit lethality failed: " + ex.Message);
		}
	}

	private bool ShouldSuppressSceneNotableDeath(Hero hero)
	{
		try
		{
			return hero != null && (_conflictActive || IsOwnedSettlementPassiveAttackScene()) && _sceneNotableRecentHitNonLethal.TryGetValue(hero, out bool flag) && flag;
		}
		catch
		{
			return false;
		}
	}

	private bool ShouldDeferSceneNotableBattleDeath(Hero hero)
	{
		try
		{
			return hero != null && (_conflictActive || IsOwnedSettlementPassiveAttackScene()) && _sceneNotableDeferredBattleDeathCandidates.Contains(hero);
		}
		catch
		{
			return false;
		}
	}

	private static bool RollDeferredPlayerBattleDeath(float deathProbability)
	{
		float num = deathProbability;
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		return MBRandom.RandomFloat <= num;
	}

	private void TryQueuePendingPlayerBattleDeathOutcome(Agent affectedAgent, Agent affectorAgent, AgentState agentState)
	{
		try
		{
			bool externalArmedConflict = SettlementEntryTroopSelectionBehavior.IsSetsDefenderConflictActiveForExternal(Mission.Current);
			if ((!_conflictActive && !externalArmedConflict) || affectedAgent == null || !affectedAgent.IsMainAgent)
			{
				return;
			}
			if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
			{
				return;
			}
			Hero hero = null;
			try
			{
				hero = ((affectorAgent?.Character is CharacterObject characterObject) ? characterObject.HeroObject : null);
			}
			catch
			{
			}
			if (hero == null && affectorAgent == Agent.Main)
			{
				hero = Hero.MainHero;
			}
			if (hero != null)
			{
				_pendingPlayerBattleDeathKiller = hero;
			}
			if (!_armedConflictOccurredThisConflict || !_pendingPlayerBattleDeathAfterMission)
			{
				return;
			}
			SceneTauntBehavior.QueuePendingMainHeroBattleDeathForExternal(_pendingPlayerBattleDeathKiller, agentState == AgentState.Killed ? "scene_taunt_player_killed" : "scene_taunt_player_unconscious_deathmark");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing pending player battle death outcome failed: " + ex.Message);
		}
	}
}

public sealed class SceneTauntPlayerDeathAgentStateDeciderLogic : MissionLogic, IAgentStateDecider, IMissionBehavior
{
	public AgentState GetAgentState(Agent effectedAgent, float deathProbability, out bool usedSurgery)
	{
		usedSurgery = false;
		try
		{
			SceneTauntMissionBehavior missionBehavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
			if (missionBehavior != null && missionBehavior.TryUseSafeMainHeroDefeatState(effectedAgent, deathProbability, out var result))
			{
				return result;
			}
		}
		catch
		{
		}
		float num = deathProbability;
		if (num < 0f)
		{
			num = 0f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		return (MBRandom.RandomFloat <= num) ? AgentState.Killed : AgentState.Unconscious;
	}
}

public class SceneTauntConsequenceMissionLogic : MissionLogic
{
	private float _pendingDefeatCaptivityAtMissionTime = -1f;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
	{
		canPlayerLeave = true;
		if (SettlementEntryTroopSelectionBehavior.ShouldBypassSceneTauntExitBlockForExternal(Mission.Current))
		{
			return null;
		}
		SceneTauntMissionBehavior missionBehavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
		if (missionBehavior != null && missionBehavior.ShouldBlockSceneExit())
		{
			canPlayerLeave = false;
			SceneTauntMissionBehavior.ShowBlockedSceneExitNotice(Mission.Current);
		}
		return null;
	}

	public override void OnMissionTick(float dt)
	{
		Mission mission = Mission.Current;
		SceneTauntMissionBehavior missionBehavior = mission?.GetMissionBehavior<SceneTauntMissionBehavior>();
		if (missionBehavior == null)
		{
			_pendingDefeatCaptivityAtMissionTime = -1f;
			return;
		}
		if (missionBehavior.ShouldCommitPlayerBattleDeathAfterMission())
		{
			if (_pendingDefeatCaptivityAtMissionTime < 0f)
			{
				_pendingDefeatCaptivityAtMissionTime = mission.CurrentTime + 0.2f;
				return;
			}
			if (mission.CurrentTime < _pendingDefeatCaptivityAtMissionTime)
			{
				return;
			}
			TryCommitPendingPlayerBattleDeath(missionBehavior);
			return;
		}
		if (!missionBehavior.ShouldSendPlayerToLocalDungeonOnDefeat())
		{
			_pendingDefeatCaptivityAtMissionTime = -1f;
			return;
		}
		if (_pendingDefeatCaptivityAtMissionTime < 0f)
		{
			_pendingDefeatCaptivityAtMissionTime = mission.CurrentTime + 0.5f;
			return;
		}
		if (mission.CurrentTime < _pendingDefeatCaptivityAtMissionTime)
		{
			return;
		}
		TryCommitLocalDungeonCaptivity(missionBehavior);
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
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			PartyBase party = currentSettlement?.Party;
			if (party == null)
			{
				Logger.Log("SceneTaunt", "Local dungeon captivity skipped because current settlement party is unavailable.");
				missionBehavior.MarkPlayerDefeatOutcomeHandled();
				return;
			}
			IFaction faction = party.MapFaction ?? currentSettlement?.MapFaction;
			bool flag = missionBehavior.WasLastArmedDefeatCriminalConflict();
			if (flag && currentSettlement != null && currentSettlement.IsTown)
			{
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_criminal_target_no_captivity");
				SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_defeat_criminal_target_no_captivity");
				try
				{
					Campaign.Current?.GameMenuManager?.SetNextMenu("town");
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
				Logger.Log("SceneTaunt", $"Player was defeated by a criminal-target scene conflict and returned without trial or captivity. Settlement={currentSettlement?.Name}, Captor={party.Name}");
				return;
			}
			float effectiveCrimeRatingForExecution = SceneTauntBehavior.GetEffectiveCrimeRatingForExternal(faction);
			if (effectiveCrimeRatingForExecution >= SceneTauntBehavior.ForcedExecutionCrimeThreshold)
			{
				Hero hero = ResolveExecutionExecutor(party, currentSettlement);
				string executionMenuId = ResolveExecutionMenuId(currentSettlement);
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_forced_execution");
				SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_execution_threshold");
				SceneTauntBehavior.ClearDeferredCrimeForExternal(faction, "scene_taunt_execution_threshold");
				SceneTauntBehavior.QueuePendingForcedPlayerExecutionForExternal(hero, executionMenuId, "scene_taunt_execution_threshold");
				missionBehavior.MarkPlayerDefeatOutcomeHandled();
				AnimusForgeQuickInfo.Show($"你的累计犯罪度已达 {SceneTauntBehavior.ForcedExecutionCrimeThreshold:0}，你将被处决。", hero?.CharacterObject);
				try
				{
					Mission.Current.NextCheckTimeEndMission = 0f;
				}
				catch
				{
				}
				Mission.Current.EndMission();
				Logger.Log("SceneTaunt", $"Player was defeated after armed escalation and reached execution threshold. Settlement={currentSettlement?.Name}, Faction={faction?.Name}, EffectiveCrime={effectiveCrimeRatingForExecution:0.##}, Executor={hero?.Name}");
				return;
			}
			bool flag2 = IsCaptorSameMapFactionAsPlayer(party);
			if (flag2 && currentSettlement != null && currentSettlement.IsTown)
			{
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_criminal_flow");
				try
				{
					Campaign.Current?.GameMenuManager?.SetNextMenu("town_inside_criminal");
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
				Logger.Log("SceneTaunt", $"Player was defeated after armed escalation and redirected to criminal judgment flow. Settlement={currentSettlement?.Name}, Captor={party.Name}");
				return;
			}
			if (flag2)
			{
				SceneTauntBehavior.TryStartTemporaryDungeonWarForExternal(party, party.LeaderHero, "scene_taunt_armed_defeat_temp_war");
			}
			SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_local_dungeon");
			SceneTauntBehavior.ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_armed_defeat_reset");
			try
			{
				Campaign.Current?.GameMenuManager?.SetNextMenu("menu_captivity_castle_taken_prisoner");
			}
			catch
			{
			}
			SceneTauntBehavior.MarkPendingLocalDungeonCaptivityForExternal(party, "scene_taunt_armed_defeat");
			missionBehavior.MarkPlayerDefeatOutcomeHandled();
			try
			{
				Mission.Current.NextCheckTimeEndMission = 0f;
			}
			catch
			{
			}
			Mission.Current.EndMission();
			Logger.Log("SceneTaunt", $"Player was defeated after armed escalation and redirected to local dungeon flow. Settlement={currentSettlement?.Name}, Captor={party.Name}");
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
			IFaction faction = captorParty?.MapFaction;
			IFaction faction2 = PartyBase.MainParty?.MapFaction;
			return faction != null && faction2 != null && faction == faction2;
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
			return captorParty?.LeaderHero ?? settlement?.OwnerClan?.Leader ?? settlement?.MapFaction?.Leader;
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

public static class SceneTauntWieldBlockPatch
{
	private static bool _patched;

	private static float _lastLogTime;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type typeFromHandle = typeof(Agent);
			MethodInfo method = typeof(SceneTauntWieldBlockPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (typeFromHandle == null || method == null)
			{
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.wieldblock");
			int num = 0;
			foreach (MethodInfo methodInfo in typeFromHandle.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (methodInfo == null)
				{
					continue;
				}
				string name = methodInfo.Name;
				if (!(name != "TryToWieldWeaponInSlot") || !(name != "TryToWieldWeaponInHand") || !(name != "WieldInitialWeapons"))
				{
					try
					{
						harmony.Patch(methodInfo, new HarmonyMethod(method));
						num++;
					}
					catch
					{
					}
				}
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ SceneTauntWieldBlockPatch 已打补丁。Patched={num}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntWieldBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(Agent __instance)
	{
		try
		{
			if (!SceneTauntMissionBehavior.ShouldBlockAgentWeaponWieldExternal(__instance))
			{
				return true;
			}
			float applicationTime = TaleWorlds.Engine.Time.ApplicationTime;
			if (applicationTime - _lastLogTime > 1f)
			{
				_lastLogTime = applicationTime;
				Logger.Log("SceneTaunt", "Blocked AI wield attempt during unarmed scene conflict.");
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}

public static class SceneTauntMissionDifficultyPatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("SandBox.GameComponents.SandboxMissionDifficultyModel");
			MethodInfo method = AccessTools.Method(type, "GetDamageMultiplierOfCombatDifficulty");
			MethodInfo method2 = typeof(SceneTauntMissionDifficultyPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
			if (type == null || method == null || method2 == null)
			{
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.damagemultiplier");
			harmony.Patch(method, postfix: new HarmonyMethod(method2));
			_patched = true;
			Logger.LogTrace("System", "✅ SceneTauntMissionDifficultyPatch 已打补丁。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntMissionDifficultyPatch 打补丁失败: " + ex.Message);
		}
	}

	public static void Postfix(Agent victimAgent, Agent attackerAgent, ref float __result)
	{
		try
		{
			if (SceneTauntMissionBehavior.ShouldUseFullCombatDamageExternal(victimAgent, attackerAgent))
			{
				__result = 1f;
			}
		}
		catch
		{
		}
	}
}

public static class SceneTauntFightAutoEndDelayPatch
{
	private static bool _patched;

	private static readonly FieldInfo FinishTimerField = AccessTools.Field(typeof(MissionFightHandler), "_finishTimer");

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			MethodInfo method = AccessTools.Method(typeof(MissionFightHandler), "OnMissionTick");
			MethodInfo method2 = typeof(SceneTauntFightAutoEndDelayPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (method == null || method2 == null)
			{
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.fightautodelay");
			harmony.Patch(method, prefix: new HarmonyMethod(method2));
			_patched = true;
			Logger.LogTrace("System", "✅ SceneTauntFightAutoEndDelayPatch 已打补丁。");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntFightAutoEndDelayPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(MissionFightHandler __instance)
	{
		try
		{
			Mission mission = __instance?.Mission ?? Mission.Current;
			if (!SceneTauntMissionBehavior.ShouldDelayNativeFightAutoEndLongExternal(mission))
			{
				return true;
			}
			BasicMissionTimer basicMissionTimer = FinishTimerField?.GetValue(__instance) as BasicMissionTimer;
			if (__instance != null && mission != null && mission.CurrentTime > __instance.MinMissionEndTime && basicMissionTimer != null && basicMissionTimer.ElapsedTime > 3600f)
			{
				FinishTimerField?.SetValue(__instance, null);
				__instance.EndFight(false);
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}

public static class SceneTauntNativeConversationBlockPatch
{
	private static bool _patched;

	private static float _lastLogTime;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.nativeconversationblock");
			int num = 0;
			Type type = AccessTools.TypeByName("SandBox.Conversation.MissionLogics.MissionConversationLogic");
			MethodInfo method = AccessTools.Method(type, "StartConversation", new Type[3]
			{
				typeof(Agent),
				typeof(bool),
				typeof(bool)
			});
			MethodInfo method2 = typeof(SceneTauntNativeConversationBlockPatch).GetMethod("StartConversationPrefix", BindingFlags.Static | BindingFlags.Public);
			if (type != null && method != null && method2 != null)
			{
				harmony.Patch(method, prefix: new HarmonyMethod(method2));
				num++;
			}
			Type type2 = AccessTools.TypeByName("SandBox.Missions.MissionLogics.MissionAlleyHandler");
			MethodInfo method3 = AccessTools.Method(type2, "CheckAndTriggerConversationWithRivalThug");
			MethodInfo method4 = AccessTools.Method(type2, "StartCommonAreaBattle");
			MethodInfo method5 = typeof(SceneTauntNativeConversationBlockPatch).GetMethod("AlleyPrefix", BindingFlags.Static | BindingFlags.Public);
			if (type2 != null && method3 != null && method5 != null)
			{
				harmony.Patch(method3, prefix: new HarmonyMethod(method5));
				num++;
			}
			if (type2 != null && method4 != null && method5 != null)
			{
				harmony.Patch(method4, prefix: new HarmonyMethod(method5));
				num++;
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ SceneTauntNativeConversationBlockPatch 已打补丁。Patched={num}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntNativeConversationBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool StartConversationPrefix(object __instance, Agent agent)
	{
		try
		{
			Mission mission = agent?.Mission ?? Mission.Current;
			if (!SceneTauntMissionBehavior.ShouldSuppressNativeMissionConversationExternal(mission))
			{
				return true;
			}
			LogBlockedConversation(agent, "native_start_conversation");
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool AlleyPrefix()
	{
		try
		{
			if (!SceneTauntMissionBehavior.ShouldSuppressNativeMissionConversationExternal(Mission.Current))
			{
				return true;
			}
			LogBlockedConversation(null, "native_alley_flow");
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static void LogBlockedConversation(Agent agent, string reason)
	{
		try
		{
			float applicationTime = TaleWorlds.Engine.Time.ApplicationTime;
			if (!(applicationTime - _lastLogTime > 1f))
			{
				return;
			}
			_lastLogTime = applicationTime;
			Logger.Log("SceneTaunt", $"Blocked native mission conversation/alley flow during SceneTaunt escalation. Reason={reason}, Agent={agent?.Name}");
		}
		catch
		{
		}
	}
}

public static class SceneTauntLeaveMissionBlockPatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Harmony harmony = new Harmony("AnimusForge.scene.taunt.leavemissionblock");
			int num = 0;
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.BasicLeaveMissionLogic");
			MethodInfo method = AccessTools.Method(type, "OnEndMissionRequest");
			MethodInfo method2 = typeof(SceneTauntLeaveMissionBlockPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
			if (type != null && method != null && method2 != null)
			{
				harmony.Patch(method, prefix: new HarmonyMethod(method2));
				num++;
			}
			MethodInfo method4 = AccessTools.Method(typeof(MissionFightHandler), "OnEndMissionRequest");
			if (method4 != null && method2 != null)
			{
				harmony.Patch(method4, prefix: new HarmonyMethod(method2));
				num++;
			}
			_patched = num > 0;
			if (_patched)
			{
				Logger.LogTrace("System", $"✅ SceneTauntLeaveMissionBlockPatch 已打补丁。Patched={num}");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntLeaveMissionBlockPatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(ref bool canPlayerLeave, ref InquiryData __result)
	{
		try
		{
			if (!SceneTauntMissionBehavior.ShouldBlockSceneExitExternal(Mission.Current))
			{
				return true;
			}
			canPlayerLeave = false;
			__result = SceneTauntMissionBehavior.CreateBlockedSceneExitInquiry(Mission.Current);
			return false;
		}
		catch
		{
			return true;
		}
	}
}
