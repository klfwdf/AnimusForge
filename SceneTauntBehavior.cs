using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using SandBox;
using SandBox.BoardGames.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
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

	private bool _pendingForcedPlayerExecution;

	private string _pendingForcedPlayerExecutionExecutorHeroId = "";

	private string _pendingForcedPlayerExecutionMenuId = "";

	public static SceneTauntBehavior Instance { get; private set; }

	public SceneTauntBehavior()
	{
		Instance = this;
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
		TryCommitPendingSceneNotableBattleDeaths();
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		TryCommitPendingMainHeroBattleDeath();
		TryCommitPendingForcedPlayerExecution();
		TryCommitDeferredCrimeWhenBackOnWorldMap();
	}

	private void OnDailyTick()
	{
		TryCommitPendingMainHeroBattleDeath();
		TryCommitDeferredCrimeWhenBackOnWorldMap();
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
			InformationManager.DisplayMessage(new InformationMessage($"{faction.Name} 认定你的罪行已满，俘虏后将处决你。", Color.FromUint(4294901760u)));
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
					InformationManager.DisplayMessage(new InformationMessage($"离开当前场景后，{factionById.Name} 的累计犯罪度 +{num5:0.#}。", new Color(1f, 0.45f, 0.2f)));
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
			InformationManager.DisplayMessage(new InformationMessage($"你在{faction.Name}积累了{trackedCrimeTotalAmount:0.#}犯罪度！", new Color(1f, 0.45f, 0.2f)));
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
			InformationManager.DisplayMessage(new InformationMessage($"击倒 {text2}：{settlement.Name} 的公共信任 +1.3。", new Color(0.45f, 1f, 0.45f)));
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
		return targetHero != null && targetHero.IsLord && !IsMeetingTauntContext(targetHero);
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
			string text = Hero.MainHero?.Name?.ToString()?.Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "玩家";
			}
			text += "（玩家）";
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
				return "【AFEF与匪徒冲突规则】你是匪徒/帮派成员，这一类冲突不走普通场景口角的警告-升级流程，也不要输出[ACTION:SCENE_TAUNT_WARN]。" + text + "若只是嘴硬、试探、骂你几句，你可以继续放狠话、威胁、索要买路钱或逼他滚开；只有当你明确决定和他立刻翻脸开打时，才在句末输出[ACTION:SCENE_TAUNT_FIGHT]。一旦输出，该冲突会进入原版的匪徒冲突流程。";
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
					return "你已警告过" + text + "。若还忍不了，就在句末输出[ACTION:SCENE_TAUNT_FIGHT]；一旦开打，场上己方士兵和领主会来帮你砍他。";
				}
				return "你已警告过" + text + "。若还忍不了，就在句末输出[ACTION:SCENE_TAUNT_FIGHT]，用拳头教训他。";
			}
			if (flag4)
			{
				return "若" + text + "挑衅你，可在句末输出[ACTION:SCENE_TAUNT_WARN]警告他；若忍无可忍，可直接输出[ACTION:SCENE_TAUNT_FIGHT]。一旦开打，场上己方士兵和领主会来帮你砍他。";
			}
			return "若" + text + "挑衅你，可在句末输出[ACTION:SCENE_TAUNT_WARN]警告他；若忍无可忍，可直接输出[ACTION:SCENE_TAUNT_FIGHT]，用拳头教训他。";
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
			if (IsMeetingTauntContext(targetHero))
			{
				return LordEncounterBehavior.BuildMeetingTauntRuntimeInstructionForExternal(targetHero);
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
			dictionary["warnTag"] = "MEETING_TAUNT_WARN";
			dictionary["fightTag"] = "MEETING_TAUNT_BATTLE";
			dictionary["fightEffectText"] = "这会把当前会面立刻升级为战斗，并按玩家攻击了你方军队来处理后果。";
		}
		else if (isSceneLord)
		{
			dictionary["tauntContext"] = "非会面场景中的领主互动";
			dictionary["warnTag"] = "SCENE_TAUNT_WARN";
			dictionary["fightTag"] = "SCENE_TAUNT_FIGHT";
			dictionary["fightEffectText"] = "这会把当前场景立刻升级为持械冲突；该领主和场上士兵会拿武器围攻玩家；并且会按玩家公开敌对该领主所属势力来处理，必要时会先强制让玩家脱离原势力再宣战。";
		}
		else
		{
			dictionary["tauntContext"] = "普通NPC的场景互动";
			dictionary["warnTag"] = "SCENE_TAUNT_WARN";
			dictionary["fightTag"] = "SCENE_TAUNT_FIGHT";
			dictionary["fightEffectText"] = "这会把当前场景立刻升级为冲突。";
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
	private static readonly FieldInfo PlayerSideOldTeamDataField = AccessTools.Field(typeof(MissionFightHandler), "_playerSideAgentsOldTeamData");

	private static readonly FieldInfo OpponentSideOldTeamDataField = AccessTools.Field(typeof(MissionFightHandler), "_opponentSideAgentsOldTeamData");

	private static readonly FieldInfo OpponentSideAgentsField = AccessTools.Field(typeof(MissionFightHandler), "_opponentSideAgents");

	private static readonly FieldInfo FinishTimerField = AccessTools.Field(typeof(MissionFightHandler), "_finishTimer");

	private const float SceneTauntNativeFightAutoEndDelaySeconds = 3600f;

	private const float ArmedBystanderReactionRefreshIntervalSeconds = 0.5f;

	private const float ArmedBystanderReactionRadiusMeters = 20f;

	private const string FallbackSoldierWeaponId = "iron_spatha_sword_t2";

	private const float SceneTauntCrimeCapBeforeWar = 59f;

	private const float SceneTauntInitialArmedCrimeAmount = 35f;

	private const float SceneTauntPerKnockdownCrimeAmount = 20f;

	private const int SceneTauntPerKnockdownTrustPenalty = 20;

	private MissionFightHandler _fightHandler;

	private readonly HashSet<int> _playerAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _opponentAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _guardAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _blockedAiWeaponAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _armedBystanderWatcherIndices = new HashSet<int>();

	private readonly HashSet<int> _armedEscalationBehaviorFactRolledAgentIndices = new HashSet<int>();

	private readonly HashSet<int> _penalizedArmedKnockdownAgentIndices = new HashSet<int>();

	private readonly Dictionary<int, MissionEquipment> _cachedUnarmedConflictEquipment = new Dictionary<int, MissionEquipment>();

	private readonly Dictionary<Hero, bool> _sceneNotableRecentHitNonLethal = new Dictionary<Hero, bool>();

	private readonly HashSet<Hero> _sceneNotableDeferredBattleDeathCandidates = new HashSet<Hero>();

	private bool _conflictActive;

	private bool _armedConflict;

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
		TryActivateSettlementArmedCarryover();
		TryResolveCompletedUnarmedConflictBeforeEscalation();
		TryCommitPendingImmediateUnarmedFightEnd();
		TryCommitPendingPlayerUnarmedPrep();
		TryCommitPendingPlayerRearmAfterArmedConflictEnd();
		TryCommitPendingActiveUnarmedTargetFlee();
		TryMaintainMainAgentArmedPresence();
		TryMaintainArmedBystanderReactions();
		TryAppendNearbyArmedEscalationBehaviorFacts();
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
			Logger.Log("SceneTaunt", $"[AttackTiming] release_triggered time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} weapon={IsAgentUsingRealWeapon(Agent.Main)} conflict={_conflictActive} armed={_armedConflict}");
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
			Logger.Log("SceneTaunt", $"[AttackTiming] facing_attack_scan time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} nearbyCount={(nearbyNPCAgents != null ? nearbyNPCAgents.Count : 0)} target={(facingAgent?.Name?.ToString() ?? "null")} targetIndex={(facingAgent != null ? facingAgent.Index : -1)}");
			if (facingAgent == null || !facingAgent.IsActive())
			{
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
			if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = -1f;
			foreach (Agent agent in Mission.Current.Agents)
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
			if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return null;
			}
			Vec3 position = Agent.Main.Position;
			Vec3 lookDirection = Agent.Main.LookDirection;
			Agent result = null;
			float num = float.MaxValue;
			foreach (Agent agent in Mission.Current.Agents)
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
			TryAddFacingAgentToUnarmedConflict(facingAgent, "player_attack_release_targeting_existing_unarmed_conflict");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Handling facing attack during unarmed conflict failed: " + ex.Message);
		}
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main)
		{
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(affectedAgent.Index, affectorAgent == Agent.Main ? "scene_taunt_agent_hit" : "scene_agent_hit_any_source");
		}
		if (affectorAgent != Agent.Main || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
		{
			return;
		}
		Logger.Log("SceneTaunt", $"[AttackTiming] on_agent_hit time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} target={affectedAgent.Name} targetIndex={affectedAgent.Index} weapon={IsMissionWeaponRealWeapon(attackerWeapon)} conflict={_conflictActive} armed={_armedConflict}");
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
		RememberSceneNotableHitLethality(affectedAgent, attackerWeapon, in blow, damagedHp);
		if (affectedAgent != null && affectedAgent.IsHuman && affectedAgent != Agent.Main && damagedHp > 0f)
		{
			ShoutBehavior.InterruptAgentSpeechForCombatExternal(affectedAgent.Index, affectorAgent == Agent.Main ? "scene_taunt_score_hit" : "scene_score_hit_any_source");
		}
		if (damagedHp <= 0f || affectorAgent != Agent.Main || affectedAgent == null || !affectedAgent.IsHuman || affectedAgent == Agent.Main)
		{
			return;
		}
		Logger.Log("SceneTaunt", $"[AttackTiming] on_score_hit time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} target={affectedAgent.Name} targetIndex={affectedAgent.Index} weapon={IsWeaponComponentRealWeapon(attackerWeapon)} damage={damagedHp:0.##} blocked={isBlocked} conflict={_conflictActive} armed={_armedConflict}");
		if (!_conflictActive)
		{
			TryStartConflictFromPhysicalAttack(affectedAgent, IsWeaponComponentRealWeapon(attackerWeapon), "player_physical_score_hit");
			return;
		}
		if (!_armedConflict && IsWeaponComponentRealWeapon(attackerWeapon))
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
			TryQueuePendingPlayerBattleDeathOutcome(affectedAgent, affectorAgent, agentState);
			TryApplyNativeAlleyNpcKnockdownConsequences(affectedAgent, affectorAgent, agentState);
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
		ClearRuntimeState();
	}

	internal bool CanStartConflict(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		_fightHandler = _fightHandler ?? Mission.Current?.GetMissionBehavior<MissionFightHandler>();
		if (_conflictActive || Mission.Current == null || _fightHandler == null || Settlement.CurrentSettlement == null)
		{
			return false;
		}
		if (_fightHandler.IsThereActiveFight())
		{
			return false;
		}
		Agent agent = ResolveTargetAgent(targetHero, targetCharacter, targetAgentIndex);
		return agent != null && agent.IsHuman && agent.IsActive();
	}

	internal bool TryStartConflict(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex, string targetKey, bool fromVerbalTaunt = false, bool playerUsedWeaponOverride = false)
	{
		try
		{
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
			if (flag4)
			{
				return TryStartNativeCriminalConflict(agent, fromVerbalTaunt ? "scene_taunt_verbal_criminal_conflict" : "scene_taunt_physical_criminal_conflict");
			}
			List<Agent> list = CollectPlayerSideAgents();
			List<Agent> list2 = CollectOpponentSideAgents(agent);
			List<Agent> list3 = flag4 ? new List<Agent>() : CollectGuardAgents(list, list2);
			if (flag)
			{
				foreach (Agent item in list3)
				{
					AddUniqueAgent(list2, item);
				}
			}
			_conflictActive = true;
			_armedConflict = false;
			_armedConflictOccurredThisConflict = false;
			_armedDefeatOutcomeHandled = false;
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
			}
			foreach (Agent item4 in list3)
			{
				_guardAgentIndices.Add(item4.Index);
			}
			_fightHandler.StartCustomFight(list, list2, dropWeapons: false, isItemUseDisabled: false, OnConflictFinished, float.Epsilon);
			ApplyBaseConsequences(targetCharacter, (flag || flag2 || flag3) ? SceneTauntInitialArmedCrimeAmount : 5f);
			bool flag5 = SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement() && _armedCarryoverHandledInThisMission;
			if (flag3)
			{
				ApplyLordSceneFightConsequences(targetHero);
			}
			if (flag3)
			{
				EscalateToArmedConflict("taunted_lord_scene", flag5);
			}
			else if (flag2)
			{
				EscalateToArmedConflict("taunted_soldier", flag5);
			}
			else if (flag)
			{
				EscalateToArmedConflict("player_already_wielding", flag5);
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
			Logger.Log("SceneTaunt", $"[AttackTiming] try_start_conflict_from_physical_attack time={Mission.Current?.CurrentTime:0.###} location={(CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant()} settlement={Settlement.CurrentSettlement?.StringId} reason={reason} target={(targetAgent?.Name?.ToString() ?? "null")} targetIndex={(targetAgent != null ? targetAgent.Index : -1)} playerUsedWeapon={playerUsedWeapon} conflict={_conflictActive} armed={_armedConflict}");
			if (_conflictActive || targetAgent == null || !targetAgent.IsHuman || !targetAgent.IsActive())
			{
				return false;
			}
			CharacterObject characterObject = targetAgent.Character as CharacterObject;
			Hero hero = characterObject?.HeroObject;
			if (!IsEligiblePhysicalAttackTarget(hero, characterObject))
			{
				return false;
			}
			if (IsSettlementCriminalConflictTarget(hero, characterObject))
			{
				if (ShouldSuppressDuplicateNativeCriminalConflict(targetAgent))
				{
					Logger.Log("SceneTaunt", $"Skipped duplicate native criminal conflict redirect. Reason={reason}, Target={targetAgent.Name}, AgentIndex={targetAgent.Index}");
					return true;
				}
				try
				{
					Campaign.Current?.ConversationManager?.EndConversation();
				}
				catch
				{
				}
				bool startedNativeCriminalConflict = TryStartNativeCriminalConflict(targetAgent, reason + "_native_alley");
				if (startedNativeCriminalConflict)
				{
					RememberNativeCriminalConflictTarget(targetAgent);
					Logger.Log("SceneTaunt", $"Physical attack bypassed custom scene conflict and redirected to native criminal conflict. Reason={reason}, Target={targetAgent.Name}, UsedWeapon={playerUsedWeapon}");
				}
				return startedNativeCriminalConflict;
			}
			try
			{
				Campaign.Current?.ConversationManager?.EndConversation();
			}
			catch
			{
			}
			string sceneTauntTargetKey = SceneTauntBehavior.BuildSceneTauntTargetKey(hero, characterObject, targetAgent.Index);
			bool flag = TryStartConflict(hero, characterObject, targetAgent.Index, sceneTauntTargetKey, fromVerbalTaunt: false, playerUsedWeaponOverride: playerUsedWeapon);
			Logger.Log("SceneTaunt", $"[AttackTiming] try_start_conflict_result time={Mission.Current?.CurrentTime:0.###} reason={reason} target={(targetAgent?.Name?.ToString() ?? "null")} started={flag} conflict={_conflictActive} armed={_armedConflict}");
			if (!flag)
			{
				return false;
			}
			if (IsSettlementCriminalConflictTarget(hero, characterObject))
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
		if (targetAgent == null || targetAgent.Index < 0 || Mission.Current == null)
		{
			return false;
		}
		return _lastNativeCriminalConflictTargetAgentIndex == targetAgent.Index && Mission.Current.CurrentTime - _lastNativeCriminalConflictMissionTime <= 0.45f;
	}

	private void RememberNativeCriminalConflictTarget(Agent targetAgent)
	{
		if (targetAgent == null || targetAgent.Index < 0 || Mission.Current == null)
		{
			return;
		}
		_lastNativeCriminalConflictTargetAgentIndex = targetAgent.Index;
		_lastNativeCriminalConflictMissionTime = Mission.Current.CurrentTime;
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
			if (_opponentAgentIndices.Contains(targetAgent.Index))
			{
				if (_armedBystanderWatcherIndices.Contains(targetAgent.Index))
				{
					ReleaseArmedBystanderWatcher(targetAgent);
					Logger.Log("SceneTaunt", $"Released frozen armed bystander into active combat. Reason={reason}, Target={targetAgent.Name}");
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
			Logger.Log("SceneTaunt", $"Added facing civilian to armed scene conflict. Reason={reason}, Target={targetAgent.Name}, Opponents={_opponentAgentIndices.Count}");
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
			if (!IsEligiblePhysicalAttackTarget(hero, characterObject) || SceneTauntBehavior.IsChildSceneProtectedTarget(characterObject))
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
		return "[AFEF NPC行为补充] "+text + "一拳打到了你的身上，你也开始拿拳头反击。";
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
		return "[AFEF NPC行为补充] ，" + text + "一刀看向了你，而你现在也拔出了" + text2 + "与" + text + "肉搏。";
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
			if (primaryWieldedItemIndex != EquipmentIndex.None && IsMissionWeaponRealWeapon(agent.Equipment[primaryWieldedItemIndex]))
			{
				string text = agent.Equipment[primaryWieldedItemIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text.Trim();
				}
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if (offhandWieldedItemIndex != EquipmentIndex.None && IsMissionWeaponRealWeapon(agent.Equipment[offhandWieldedItemIndex]))
			{
				string text2 = agent.Equipment[offhandWieldedItemIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2.Trim();
				}
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
			string factText;
			if (flag3)
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

	private void TryAppendPlayerBehaviorFactForArmedEscalation(string reason)
	{
		try
		{
			if (_activeTargetAgentIndex < 0)
			{
				return;
			}
			if (_openedFromVerbalTaunt || string.Equals(reason, "taunted_lord_scene", StringComparison.Ordinal) || string.Equals(reason, "taunted_soldier", StringComparison.Ordinal))
			{
				TryAppendNpcBehaviorFactForVerbalArmedEscalation();
				return;
			}
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
			string factText = BuildDirectArmedImmediateReactionFactText(agent);
			ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, _activeTargetAgentIndex, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Appending player behavior fact for armed escalation failed: " + ex.Message);
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
			return text + "在定居点内殴打了当局人员，你和其他守卫开始向他发动攻击";
		}
		return text + "在定居点内拿武器乱砍人，你和其他守卫开始向他发动攻击";
	}

	private void TryAppendGuardBehaviorFactsForArmedEscalation()
	{
		try
		{
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || _guardAgentIndices.Count == 0)
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
		if (main == null || string.IsNullOrWhiteSpace(factText))
		{
			return;
		}
		foreach (int item in _guardAgentIndices)
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault(a => a != null && a.Index == item && a.IsActive());
			if (agent == null || !IsAgentWithinArmedBystanderReactionRadius(agent, main) || !_armedEscalationBehaviorFactRolledAgentIndices.Add(item))
			{
				continue;
			}
			if (MBRandom.RandomFloat <= 0.3334f)
			{
				ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, item, persistHeroPrivateHistory: true, suppressStare: true);
			}
		}
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_guardAgentIndices);
		foreach (Agent agent2 in Mission.Current?.Agents ?? Enumerable.Empty<Agent>())
		{
			if (agent2 == null || !agent2.IsActive() || !agent2.IsHuman || hashSet.Contains(agent2.Index))
			{
				continue;
			}
			if (!IsAgentWithinArmedBystanderReactionRadius(agent2, main) || !IsAgentCarryingRealWeapon(agent2))
			{
				continue;
			}
			if (SceneTauntBehavior.IsChildSceneProtectedTarget(agent2.Character as CharacterObject))
			{
				continue;
			}
			if (!_armedEscalationBehaviorFactRolledAgentIndices.Add(agent2.Index))
			{
				continue;
			}
			if (MBRandom.RandomFloat <= 0.5f)
			{
				ShoutBehavior.TriggerImmediateSceneBehaviorReactionForExternal(factText, agent2.Index, persistHeroPrivateHistory: true, suppressStare: true);
			}
		}
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
			if (!_conflictActive || (!_armedConflict && !_armedConflictOccurredThisConflict) || effectedAgent == null || !effectedAgent.IsMainAgent)
			{
				return false;
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
		return IsAuthorityPhysicalAttackTarget(targetHero, targetCharacter) || IsSettlementCriminalConflictTarget(targetHero, targetCharacter) || SceneTauntBehavior.IsSceneNotableTauntTarget(targetHero) || SceneTauntBehavior.IsEligibleSceneTauntCharacter(targetCharacter);
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
			InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：{ownerHero.Name} 的个人信任 -1，私人关系 -1。", new Color(1f, 0.72f, 0.2f)));
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
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				if (agent == Agent.Main)
				{
					AddUniqueAgent(list, agent);
					continue;
				}
				try
				{
					LocationCharacter locationCharacter = LocationComplex.Current?.FindCharacter(agent);
					AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter?.GetAccompanyingCharacter(locationCharacter);
					if (accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart)
					{
						AddUniqueAgent(list, agent);
					}
				}
				catch
				{
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

	private List<Agent> CollectEscortedFollowers(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		if (targetAgent == null)
		{
			return list;
		}
		try
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || agent == targetAgent || !agent.IsHuman || !agent.IsActive())
				{
					continue;
				}
				if (EscortAgentBehavior.CheckIfAgentIsEscortedBy(agent, targetAgent))
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
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index))
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
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index))
				{
					continue;
				}
				CharacterObject characterObject = agent.Character as CharacterObject;
				Hero hero = characterObject?.HeroObject;
				if (!IsCarryoverAuthorityOpponent(hero, characterObject))
				{
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

	private void TryActivateSettlementArmedCarryover()
	{
		if (_conflictActive || _armedCarryoverSceneInitialized || _armedCarryoverHandledInThisMission || !SceneTauntBehavior.HasArmedCarryoverForCurrentSettlement())
		{
			return;
		}
		if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || Settlement.CurrentSettlement == null || _fightHandler == null)
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
		float currentTime = Mission.Current.CurrentTime;
		if (_lastArmedCarryoverAttemptAtMissionTime >= 0f && currentTime - _lastArmedCarryoverAttemptAtMissionTime < 0.25f)
		{
			return;
		}
		_lastArmedCarryoverAttemptAtMissionTime = currentTime;
		List<Agent> list = CollectPlayerSideAgents();
		List<Agent> list2 = CollectAuthorityCarryoverOpponentAgents(list, out var guardAgents);
		if (list2.Count == 0)
		{
			if (!_armedCarryoverNoAuthoritySceneNotified && !SceneTauntBehavior.HasShownCarryoverNoAuthorityAlertForCurrentLocationExternal())
			{
				AlarmNearbyBystanders();
				InformationManager.DisplayMessage(new InformationMessage("持械冲突的警报蔓延到了这个场景，周围的人立刻紧张起来。", new Color(1f, 0.45f, 0.2f)));
				_armedCarryoverNoAuthoritySceneNotified = true;
				_armedCarryoverHandledInThisMission = true;
				SceneTauntBehavior.MarkCarryoverNoAuthorityAlertShownForCurrentLocationExternal();
				Logger.Log("SceneTaunt", $"Armed carryover reached scene without authority opponents. Settlement={Settlement.CurrentSettlement?.Name}, Source={SceneTauntBehavior.GetArmedCarryoverSourceForCurrentSettlement()}");
			}
			return;
		}
		try
		{
			_conflictActive = true;
			_armedConflict = true;
			_armedConflictOccurredThisConflict = true;
			_armedDefeatOutcomeHandled = false;
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
			}
			foreach (Agent guardAgent in guardAgents)
			{
				_guardAgentIndices.Add(guardAgent.Index);
			}
			_fightHandler.StartCustomFight(list, list2, dropWeapons: false, isItemUseDisabled: false, OnConflictFinished, float.Epsilon);
			foreach (Agent agent in EnumerateConflictAgents(includeGuards: true))
			{
				if (agent == null || !agent.IsActive())
				{
					continue;
				}
				TryRestoreWeaponsAfterUnarmedConflict(agent);
				TryAlarmAgent(agent);
				if (agent != Agent.Main)
				{
					TryArmAgent(agent);
				}
			}
			ForceAllNonPlayerSceneAgentsMortal();
			AlarmNearbyBystanders();
			_armedCarryoverSceneInitialized = true;
			_armedCarryoverHandledInThisMission = true;
			InformationManager.DisplayMessage(new InformationMessage("你的持械冲突已经蔓延到这个场景，守卫和当局人员立刻开始围堵你。", new Color(1f, 0.35f, 0.2f)));
			Logger.Log("SceneTaunt", $"Activated armed settlement carryover in scene. Settlement={Settlement.CurrentSettlement?.Name}, Opponents={list2.Count}, Guards={guardAgents.Count}, Source={SceneTauntBehavior.GetArmedCarryoverSourceForCurrentSettlement()}");
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
			if (indices == null || indices.Count == 0 || Mission.Current?.Agents == null)
			{
				return true;
			}
			foreach (int index in indices)
			{
				Agent agent = Mission.Current.Agents.FirstOrDefault(a => a != null && a.Index == index);
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
			if (!_pendingPlayerUnarmedPrep || Mission.Current == null || _armedConflict)
			{
				if (_armedConflict)
				{
					ClearPendingPlayerUnarmedPrep();
				}
				return;
			}
			if (Mission.Current.CurrentTime < _pendingPlayerUnarmedPrepAtMissionTime)
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
			if (!_armedConflict || _activeTargetAgentIndex < 0 || Mission.Current == null)
			{
				return;
			}
			Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == _activeTargetAgentIndex);
			if (agent == null || !agent.IsActive() || !ShouldFleeWhenArmedVictim(agent))
			{
				return;
			}
			_pendingActiveUnarmedTargetFlee = true;
			_pendingActiveUnarmedTargetFleeAgentIndex = agent.Index;
			_pendingActiveUnarmedTargetFleeAtMissionTime = Mission.Current.CurrentTime + 0.12f;
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
			if (!_pendingActiveUnarmedTargetFlee || Mission.Current == null)
			{
				return;
			}
			if (Mission.Current.CurrentTime < _pendingActiveUnarmedTargetFleeAtMissionTime)
			{
				return;
			}
			Agent agent = Mission.Current.Agents?.FirstOrDefault(a => a != null && a.Index == _pendingActiveUnarmedTargetFleeAgentIndex);
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

	private void TryCommitPendingPlayerRearmAfterArmedConflictEnd()
	{
		try
		{
			if (!_pendingPlayerRearmAfterArmedConflictEnd || Mission.Current == null)
			{
				return;
			}
			if (Mission.Current.CurrentTime < _pendingPlayerRearmAfterArmedConflictEndAtMissionTime)
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
			PartyBase partyBase = null;
			try
			{
				partyBase = targetHero?.PartyBelongedTo?.Party;
			}
			catch
			{
				partyBase = null;
			}
			if (partyBase == null)
			{
				partyBase = Settlement.CurrentSettlement?.Party;
			}
			LordEncounterBehavior.ApplyHostileEscalationDiplomaticConsequences(partyBase, targetHero, "scene_taunt_lord_scene", "SceneTaunt");
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Applying lord scene fight consequences failed: " + ex.Message);
		}
	}

	private void EscalateToArmedConflict(string reason, bool suppressAnnouncement = false)
	{
		if (!_conflictActive || _armedConflict)
		{
			return;
		}
		ClearMissionFightHandlerPendingFinishTimer();
		_armedConflict = true;
		_armedConflictOccurredThisConflict = true;
		_armedCarryoverHandledInThisMission = true;
		SceneTauntBehavior.MarkArmedCarryoverForCurrentSettlement(reason);
		_blockedAiWeaponAgentIndices.Clear();
		foreach (int guardAgentIndex in _guardAgentIndices.ToList())
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == guardAgentIndex);
			if (agent != null && agent.IsActive())
			{
				AddAgentToFightSide(agent, isPlayerSide: false);
			}
		}
		foreach (Agent agent2 in EnumerateConflictAgents(includeGuards: true))
		{
			if (agent2 == null || !agent2.IsActive())
			{
				continue;
			}
			TryRestoreWeaponsAfterUnarmedConflict(agent2);
			TryAlarmAgent(agent2);
			TryArmAgent(agent2);
		}
		TryConvertUnarmedCivilianOpponentsToFleeingBystanders();
		QueuePendingActiveUnarmedTargetFleeIfNeeded();
		ForceAllNonPlayerSceneAgentsMortal();
		EnsureCrimeRatingAtLeast(SceneTauntInitialArmedCrimeAmount);
		AlarmNearbyBystanders();
		TryAppendPlayerBehaviorFactForArmedEscalation(reason);
		TryAppendGuardBehaviorFactsForArmedEscalation();
		_openedAsUnarmedBrawl = false;
		if (!suppressAnnouncement)
		{
			InformationManager.DisplayMessage(new InformationMessage("持械冲突爆发，守卫开始敌视你和你的同伴。", new Color(1f, 0.35f, 0.2f)));
		}
		Logger.Log("SceneTaunt", $"Escalated scene conflict to armed combat. Reason={reason}, Target={_activeTargetName}, Guards={_guardAgentIndices.Count}");
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
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || _playerAgentIndices.Contains(agent.Index))
				{
					continue;
				}
				TryForceAgentMortal(agent);
			}
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
			ReleaseArmedBystanderWatcher(agent);
			Team team = agent.Team;
			_fightHandler.AddAgentToSide(agent, isPlayerSide);
			FixMissionFightHandlerStoredTeam(agent, isPlayerSide, team);
			if (isPlayerSide)
			{
				_playerAgentIndices.Add(agent.Index);
			}
			else
			{
				_opponentAgentIndices.Add(agent.Index);
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
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_opponentAgentIndices);
		if (includeGuards)
		{
			hashSet.UnionWith(_guardAgentIndices);
		}
		foreach (int item in hashSet)
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent x) => x != null && x.Index == item);
			if (agent != null)
			{
				yield return agent;
			}
		}
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
					InformationManager.DisplayMessage(new InformationMessage($"{currentSettlement.Name} 的{GetMerchantTrustLabel(kind)}信任 -10。", new Color(1f, 0.7f, 0.2f)));
				}
				else
				{
					RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(currentSettlement, -10, "scene_taunt_brawl");
					InformationManager.DisplayMessage(new InformationMessage($"{currentSettlement.Name} 的公共信任 -10。", new Color(1f, 0.7f, 0.2f)));
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
			if (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
			{
				return;
			}
			if (_playerAgentIndices.Contains(affectedAgent.Index) || !_penalizedArmedKnockdownAgentIndices.Add(affectedAgent.Index))
			{
				return;
			}
			bool flag = affectorAgent == Agent.Main || (affectorAgent != null && _playerAgentIndices.Contains(affectorAgent.Index));
			if (!flag)
			{
				return;
			}
			CharacterObject characterObject = affectedAgent.Character as CharacterObject;
			ApplyPerNpcKnockdownConsequences(affectedAgent, characterObject, affectedAgent.Name?.ToString());
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
					InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：{currentSettlement.Name} 的{GetMerchantTrustLabel(kind)}信任 -{SceneTauntPerKnockdownTrustPenalty}。", new Color(1f, 0.7f, 0.2f)));
				}
				else
				{
					RewardSystemBehavior.Instance.AdjustSettlementLocalPublicTrustForExternal(currentSettlement, -SceneTauntPerKnockdownTrustPenalty, "scene_taunt_armed_knockdown");
					InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：{currentSettlement.Name} 的公共信任 -{SceneTauntPerKnockdownTrustPenalty}。", new Color(1f, 0.7f, 0.2f)));
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
				InformationManager.DisplayMessage(new InformationMessage($"击倒 {text}：累计犯罪度 +{SceneTauntPerKnockdownCrimeAmount:0.#}。超出 59 的部分会在离开定居点后再结算。", new Color(1f, 0.45f, 0.2f)));
			}
		}
		catch (Exception ex2)
		{
			Logger.Log("SceneTaunt", "Applying per-NPC knockdown crime consequence failed: " + ex2.Message);
		}
	}

	private void AlarmNearbyBystanders()
	{
		HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
		hashSet.UnionWith(_opponentAgentIndices);
		hashSet.UnionWith(_guardAgentIndices);
		Agent main = Agent.Main;
		try
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index) || !IsAgentWithinArmedBystanderReactionRadius(agent, main))
				{
					continue;
				}
				TryAlarmAgent(agent);
				if (!TryJoinArmedBystanderToConflict(agent))
				{
					TryForceUnarmedBystanderToFlee(agent);
				}
			}
		}
		catch
		{
		}
	}

	private void TryMaintainArmedBystanderReactions()
	{
		try
		{
			if (!_conflictActive || !_armedConflict || Mission.Current?.Agents == null)
			{
				return;
			}
			float currentTime = Mission.Current.CurrentTime;
			if (_lastArmedBystanderReactionRefreshAtMissionTime >= 0f && currentTime - _lastArmedBystanderReactionRefreshAtMissionTime < ArmedBystanderReactionRefreshIntervalSeconds)
			{
				return;
			}
			_lastArmedBystanderReactionRefreshAtMissionTime = currentTime;
			HashSet<int> hashSet = new HashSet<int>(_playerAgentIndices);
			hashSet.UnionWith(_opponentAgentIndices);
			hashSet.UnionWith(_guardAgentIndices);
			Agent main = Agent.Main;
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent == null || !agent.IsHuman || !agent.IsActive() || hashSet.Contains(agent.Index) || !IsAgentWithinArmedBystanderReactionRadius(agent, main))
				{
					continue;
				}
				if (!TryJoinArmedBystanderToConflict(agent))
				{
					TryForceUnarmedBystanderToFlee(agent);
				}
			}
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
			return !ShouldForceArmedCivilianBystanderToFlee(agent);
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
			if (primaryWieldedItemIndex != EquipmentIndex.None && IsMissionWeaponRealWeapon(agent.Equipment[primaryWieldedItemIndex]))
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
			return offhandWieldedItemIndex != EquipmentIndex.None && IsMissionWeaponRealWeapon(agent.Equipment[offhandWieldedItemIndex]);
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
		_armedEscalationBehaviorFactRolledAgentIndices.Clear();
		RestoreAllCachedWeapons();
		_conflictActive = false;
		_armedConflict = false;
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

	private void RememberSceneNotableHitLethality(Agent affectedAgent, WeaponComponentData attackerWeapon, in Blow blow, float damagedHp)
	{
		try
		{
			if (!_conflictActive || damagedHp <= 0f || affectedAgent == null || !affectedAgent.IsHuman)
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
			return hero != null && _conflictActive && _sceneNotableRecentHitNonLethal.TryGetValue(hero, out bool flag) && flag;
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
			return hero != null && _conflictActive && _sceneNotableDeferredBattleDeathCandidates.Contains(hero);
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
			if (!_conflictActive || affectedAgent == null || !affectedAgent.IsMainAgent)
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
		SceneTauntMissionBehavior missionBehavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
		if (missionBehavior != null && missionBehavior.ShouldBlockSceneExit())
		{
			canPlayerLeave = false;
			InformationManager.DisplayMessage(new InformationMessage("这场冲突还没结束，不能离开场景。", Color.FromUint(4294901760u)));
		}
		return null;
	}

	public override void OnMissionTick(float dt)
	{
		SceneTauntMissionBehavior missionBehavior = Mission.Current?.GetMissionBehavior<SceneTauntMissionBehavior>();
		if (missionBehavior == null)
		{
			_pendingDefeatCaptivityAtMissionTime = -1f;
			return;
		}
		if (missionBehavior.ShouldCommitPlayerBattleDeathAfterMission())
		{
			if (_pendingDefeatCaptivityAtMissionTime < 0f)
			{
				_pendingDefeatCaptivityAtMissionTime = Mission.Current.CurrentTime + 0.2f;
				return;
			}
			if (Mission.Current.CurrentTime < _pendingDefeatCaptivityAtMissionTime)
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
			_pendingDefeatCaptivityAtMissionTime = Mission.Current.CurrentTime + 0.5f;
			return;
		}
		if (Mission.Current.CurrentTime < _pendingDefeatCaptivityAtMissionTime)
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
				InformationManager.DisplayMessage(new InformationMessage($"你的累计犯罪度已达 {SceneTauntBehavior.ForcedExecutionCrimeThreshold:0}，你将被处决。", Color.FromUint(4294901760u)));
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
			bool flag = missionBehavior.WasLastArmedDefeatCriminalConflict();
			if (flag && currentSettlement != null && currentSettlement.IsTown)
			{
				SceneTauntBehavior.ClearArmedCarryoverForExternal("scene_taunt_defeat_criminal_target_flow");
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
				Logger.Log("SceneTaunt", $"Player was defeated after criminal-target armed conflict and redirected to criminal judgment flow. Settlement={currentSettlement?.Name}, Captor={party.Name}");
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
			Type type2 = AccessTools.TypeByName("SandBox.Missions.MissionLogics.LeaveMissionLogic");
			MethodInfo method3 = AccessTools.Method(type2, "OnEndMissionRequest");
			if (type2 != null && method3 != null && method2 != null)
			{
				harmony.Patch(method3, prefix: new HarmonyMethod(method2));
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
			__result = new InquiryData("无法离开", "这场冲突还没结束，不能离开场景。", isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", "确定", null, null);
			return false;
		}
		catch
		{
			return true;
		}
	}
}
