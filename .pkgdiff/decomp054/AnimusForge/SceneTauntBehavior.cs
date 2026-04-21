using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

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
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener((object)this, (Action<IMission>)OnMissionStarted);
		CampaignEvents.TickEvent.AddNonSerializedListener((object)this, (Action<float>)OnCampaignTick);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, (Action)OnDailyTick);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener((object)this, (Action<PartyBase, Hero>)OnHeroPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener((object)this, (Action<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>)OnHeroPrisonerReleased);
		CampaignEvents.CanHeroDieEvent.AddNonSerializedListener((object)this, (ReferenceAction<Hero, KillCharacterActionDetail, bool>)OnCanHeroDie);
		CampaignEvents.OnBeforeMainCharacterDiedEvent.AddNonSerializedListener((object)this, (Action<Hero, Hero, KillCharacterActionDetail, bool>)OnBeforeMainCharacterDied);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, (Action<MenuCallbackArgs>)OnGameMenuOpened);
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
		dataStore.SyncData<List<string>>("_sceneTauntWarnedTargets_v1", ref _warnedSceneTargetKeysStorage);
		dataStore.SyncData<bool>("_sceneTauntPendingTempWarPeace_v1", ref _pendingTemporaryDungeonWarPeace);
		dataStore.SyncData<string>("_sceneTauntPendingTempWarPlayerFactionId_v1", ref _pendingTemporaryDungeonWarPlayerFactionId);
		dataStore.SyncData<string>("_sceneTauntPendingTempWarEnemyFactionId_v1", ref _pendingTemporaryDungeonWarEnemyFactionId);
		dataStore.SyncData<Dictionary<string, float>>("_sceneTauntDeferredCrimeByFaction_v1", ref _pendingDeferredCrimeByFactionStorage);
		dataStore.SyncData<Dictionary<string, float>>("_sceneTauntCrimeRefillReserveByFaction_v1", ref _crimeRefillReserveByFactionStorage);
		dataStore.SyncData<Dictionary<string, float>>("_sceneTauntLastObservedNativeCrimeByFaction_v1", ref _lastObservedNativeCrimeByFactionStorage);
		dataStore.SyncData<Dictionary<string, int>>("_sceneTauntCriminalTrustRewardTenthBySettlement_v1", ref _criminalTrustRewardTenthBySettlementStorage);
		dataStore.SyncData<bool>("_sceneTauntArmedCarryoverActive_v1", ref _armedSettlementCarryoverActive);
		dataStore.SyncData<string>("_sceneTauntArmedCarryoverSettlementId_v1", ref _armedSettlementCarryoverSettlementId);
		dataStore.SyncData<string>("_sceneTauntArmedCarryoverSource_v1", ref _armedSettlementCarryoverSource);
		if (dataStore.IsSaving)
		{
			return;
		}
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
		_lastObservedNativeCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		if (_lastObservedNativeCrimeByFactionStorage != null)
		{
			foreach (KeyValuePair<string, float> item2 in _lastObservedNativeCrimeByFactionStorage)
			{
				string text2 = (item2.Key ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					_lastObservedNativeCrimeByFaction[text2] = MathF.Max(0f, item2.Value);
				}
			}
		}
		_criminalTrustRewardTenthBySettlement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (_criminalTrustRewardTenthBySettlementStorage != null)
		{
			foreach (KeyValuePair<string, int> item3 in _criminalTrustRewardTenthBySettlementStorage)
			{
				string text3 = (item3.Key ?? "").Trim();
				int num2 = Math.Max(0, item3.Value);
				if (!string.IsNullOrWhiteSpace(text3) && num2 > 0)
				{
					_criminalTrustRewardTenthBySettlement[text3] = num2;
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

	private void OnMissionStarted(IMission mission)
	{
		try
		{
			Mission val = (Mission)(object)((mission is Mission) ? mission : null);
			if (val != null)
			{
				if (val.GetMissionBehavior<SceneTauntMissionBehavior>() == null)
				{
					val.AddMissionBehavior((MissionBehavior)(object)new SceneTauntMissionBehavior());
				}
				if (val.GetMissionBehavior<SceneTauntConsequenceMissionLogic>() == null)
				{
					val.AddMissionBehavior((MissionBehavior)(object)new SceneTauntConsequenceMissionLogic());
				}
				if (val.GetMissionBehavior<SceneTauntPlayerDeathAgentStateDeciderLogic>() == null)
				{
					val.AddMissionBehavior((MissionBehavior)(object)new SceneTauntPlayerDeathAgentStateDeciderLogic());
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "OnMissionStarted failed: " + ex.Message);
		}
	}

	private void OnCampaignTick(float dt)
	{
		if (!TryCommitPendingMainHeroBattleDeath() && !TryCommitPendingForcedPlayerExecution())
		{
			TryForcePendingForcedPlayerExecutionMenuIfReady();
			if (!_pendingForcedPlayerExecution)
			{
				TryForcePendingLocalDungeonCaptivityMenuIfReady();
				TryClearExpiredArmedSettlementCarryover();
				TryCommitDeferredCrimeWhenBackOnWorldMap();
				TryCommitPendingSceneNotableBattleDeaths();
			}
		}
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
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		if (prisoner != Hero.MainHero || capturer == null || _pendingForcedPlayerExecution)
		{
			return;
		}
		try
		{
			object obj = capturer.MapFaction;
			if (obj == null)
			{
				Hero leaderHero = capturer.LeaderHero;
				obj = ((leaderHero != null) ? leaderHero.MapFaction : null);
			}
			IFaction val = (IFaction)obj;
			float effectiveCrimeRatingForExternal = GetEffectiveCrimeRatingForExternal(val);
			if (val != null && !(effectiveCrimeRatingForExternal < 90f))
			{
				Hero val2 = capturer.LeaderHero ?? val.Leader;
				QueuePendingForcedPlayerExecutionForExternal(val2, "", "scene_taunt_capture_execution_threshold");
				InformationManager.DisplayMessage(new InformationMessage($"{val.Name} 认定你的罪行已满，俘虏后将处决你。", Color.FromUint(4294901760u)));
				Logger.Log("SceneTaunt", $"Queued forced execution after capture. Captor={capturer.Name}, Faction={val.Name}, EffectiveCrime={effectiveCrimeRatingForExternal:0.##}, Executor={((val2 != null) ? val2.Name : null)}");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Capture-based forced execution check failed: " + ex.Message);
		}
	}

	private void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterActionDetail detail, bool showNotification)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
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
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (prisoner != Hero.MainHero || !_pendingTemporaryDungeonWarPeace)
		{
			return;
		}
		try
		{
			IFaction val = ResolveFactionById(_pendingTemporaryDungeonWarPlayerFactionId);
			IFaction val2 = ResolveFactionById(_pendingTemporaryDungeonWarEnemyFactionId);
			if (val != null && val2 != null && val != val2 && FactionManager.IsAtWarAgainstFaction(val, val2))
			{
				MakePeaceAction.Apply(val, val2);
				Logger.Log("SceneTaunt", $"Temporary dungeon war ended after player release. PlayerFaction={val.Name}, EnemyFaction={val2.Name}, Detail={detail}");
			}
			else
			{
				Logger.Log("SceneTaunt", $"Temporary dungeon war peace cleanup skipped. PlayerFaction={((val != null) ? val.Name : null)}, EnemyFaction={((val2 != null) ? val2.Name : null)}, Detail={detail}");
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

	private void OnCanHeroDie(Hero hero, KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (!result || hero == null || (int)causeOfDeath != 4)
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
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
				Logger.Log("SceneTaunt", $"Committed deferred scene notable battle death. Hero={key.Name}, Killer={((value != null) ? value.Name : null)}");
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
			if (Mission.Current == null)
			{
				Game current = Game.Current;
				object obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					GameStateManager gameStateManager = current.GameStateManager;
					obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
				}
				if (!(obj is MissionState) && Campaign.Current != null)
				{
					bool flag = false;
					try
					{
						ConversationManager conversationManager = Campaign.Current.ConversationManager;
						flag = ((conversationManager != null) ? conversationManager.OneToOneConversationAgent : null) != null;
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
						Game current2 = Game.Current;
						object obj3;
						if (current2 == null)
						{
							obj3 = null;
						}
						else
						{
							GameStateManager gameStateManager2 = current2.GameStateManager;
							obj3 = ((gameStateManager2 != null) ? gameStateManager2.ActiveState : null);
						}
						string text = obj3?.GetType()?.FullName ?? string.Empty;
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
					Hero val = ResolveHeroById(_pendingMainHeroBattleDeathKillerHeroId);
					ClearPendingMainHeroBattleDeath("committed");
					ClearPendingLocalDungeonCaptivityForExternal("scene_taunt_battle_death_committed");
					ClearArmedCarryoverForExternal("scene_taunt_battle_death_committed");
					KillCharacterAction.ApplyByBattle(Hero.MainHero, val, true);
					Logger.Log("SceneTaunt", $"Committed pending scene-taunt main hero battle death. Killer={((val != null) ? val.Name : null)}");
					return true;
				}
			}
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
			_pendingLocalDungeonCaptivityMenuAtTime = Time.ApplicationTime;
		}
		catch
		{
			_pendingLocalDungeonCaptivityMenuAtTime = 0f;
		}
		Logger.Log("SceneTaunt", string.Format("Marked pending local dungeon captivity menu. Reason={0}, Captor={1}", reason ?? "N/A", (captorParty != null) ? captorParty.Name : null));
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
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
			Campaign current2 = Campaign.Current;
			object obj3;
			if (current2 == null)
			{
				obj3 = null;
			}
			else
			{
				MenuContext currentMenuContext = current2.CurrentMenuContext;
				if (currentMenuContext == null)
				{
					obj3 = null;
				}
				else
				{
					GameMenu gameMenu = currentMenuContext.GameMenu;
					obj3 = ((gameMenu != null) ? gameMenu.StringId : null);
				}
			}
			text = (string)obj3;
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
				float applicationTime = Time.ApplicationTime;
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
			Campaign current3 = Campaign.Current;
			if (((current3 != null) ? current3.CurrentMenuContext : null) != null)
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
				Campaign current4 = Campaign.Current;
				object obj7;
				if (current4 == null)
				{
					obj7 = null;
				}
				else
				{
					MenuContext currentMenuContext2 = current4.CurrentMenuContext;
					if (currentMenuContext2 == null)
					{
						obj7 = null;
					}
					else
					{
						GameMenu gameMenu2 = currentMenuContext2.GameMenu;
						obj7 = ((gameMenu2 != null) ? gameMenu2.StringId : null);
					}
				}
				text2 = (string)obj7;
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
			object obj = Settlement.CurrentSettlement;
			if (obj == null)
			{
				MobileParty mainParty = MobileParty.MainParty;
				obj = ((mainParty != null) ? mainParty.CurrentSettlement : null);
			}
			return (Settlement)obj;
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
			Settlement activeSettlementSafe = GetActiveSettlementSafe();
			return (((activeSettlementSafe != null) ? ((MBObjectBase)activeSettlementSafe).StringId : null) ?? "").Trim();
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
			if (Mission.Current != null)
			{
				goto IL_0035;
			}
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
			{
				goto IL_0035;
			}
			ICampaignMission current2 = CampaignMission.Current;
			if (((current2 != null) ? current2.Location : null) != null)
			{
				return false;
			}
			Campaign current3 = Campaign.Current;
			if (((current3 == null) ? null : current3.GameMenuManager?.NextLocation) != null)
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
			goto end_IL_0001;
			IL_0035:
			return false;
			end_IL_0001:;
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
			if (Mission.Current == null)
			{
				Game current = Game.Current;
				object obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					GameStateManager gameStateManager = current.GameStateManager;
					obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
				}
				if (!(obj is MissionState))
				{
					Campaign current2 = Campaign.Current;
					if (((current2 != null) ? current2.CurrentMenuContext : null) != null)
					{
						Hero val = ResolveHeroById(_pendingForcedPlayerExecutionExecutorHeroId);
						ClearPendingForcedPlayerExecution("committed");
						KillCharacterAction.ApplyByExecution(Hero.MainHero, val, true, false);
						Logger.Log("SceneTaunt", $"Committed pending forced player execution. Executor={((val != null) ? val.Name : null)}");
						return true;
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Committing pending forced player execution failed: " + ex.Message);
			try
			{
				Hero val2 = ResolveHeroById(_pendingForcedPlayerExecutionExecutorHeroId);
				ClearPendingForcedPlayerExecution("fallback_murder");
				KillCharacterAction.ApplyByMurder(Hero.MainHero, val2, true);
				Logger.Log("SceneTaunt", $"Fallback player execution used murder path. Executor={((val2 != null) ? val2.Name : null)}");
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
			if (Mission.Current != null)
			{
				return;
			}
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameStateManager gameStateManager = current.GameStateManager;
				obj = ((gameStateManager != null) ? gameStateManager.ActiveState : null);
			}
			if (obj is MissionState)
			{
				return;
			}
			Campaign current2 = Campaign.Current;
			if (((current2 != null) ? current2.CurrentMenuContext : null) == null)
			{
				string text = (_pendingForcedPlayerExecutionMenuId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					GameMenu.ActivateGameMenu(text);
					Logger.Log("SceneTaunt", "Activated pending forced player execution menu. Menu=" + text);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Activating pending forced player execution menu failed: " + ex.Message);
		}
	}

	private void TryCommitDeferredCrimeWhenBackOnWorldMap()
	{
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Expected O, but got Unknown
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
				IFaction val = ResolveFactionById(text);
				if (val == null)
				{
					Logger.Log("SceneTaunt", $"Deferred scene crime dropped because faction could not be resolved. FactionId={text}, Amount={num:0.##}");
					_pendingDeferredCrimeByFaction.Remove(item.Key);
					continue;
				}
				try
				{
					float num2 = MathF.Max(0f, val.MainHeroCrimeRating);
					Campaign current2 = Campaign.Current;
					float? obj;
					if (current2 == null)
					{
						obj = null;
					}
					else
					{
						GameModels models = current2.Models;
						if (models == null)
						{
							obj = null;
						}
						else
						{
							CrimeModel crimeModel = models.CrimeModel;
							obj = ((crimeModel != null) ? new float?(crimeModel.GetMaxCrimeRating()) : ((float?)null));
						}
					}
					float num3 = obj ?? 100f;
					float num4 = MathF.Max(0f, num3 - num2);
					if (num4 <= 0f)
					{
						Logger.Log("SceneTaunt", $"Deferred scene-taunt crime pool not injected because native crime is already at max. Faction={val.Name}, NativeCrime={num2:0.##}, Pool={num:0.##}, Max={num3:0.##}");
						continue;
					}
					float num5 = MathF.Min(num, num4);
					if (!(num5 <= 0f))
					{
						float num6 = MathF.Max(0f, num - num5);
						if (num6 <= 0f)
						{
							_pendingDeferredCrimeByFaction.Remove(item.Key);
						}
						else
						{
							_pendingDeferredCrimeByFaction[text] = num6;
						}
						ChangeCrimeRatingAction.Apply(val, num5, true);
						InformationManager.DisplayMessage(new InformationMessage($"离开当前场景后，{val.Name} 的累计犯罪度 +{num5:0.#}。", new Color(1f, 0.45f, 0.2f, 1f)));
						Logger.Log("SceneTaunt", $"Injected scene-taunt crime pool into native crime. Faction={val.Name}, NativeBefore={num2:0.##}, Added={num5:0.##}, RemainingPool={num6:0.##}, NativeAfter={MathF.Max(0f, val.MainHeroCrimeRating):0.##}");
					}
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
		if (_armedSettlementCarryoverActive)
		{
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
	}

	private void MarkArmedSettlementCarryover(string settlementId, string source)
	{
		string text = (settlementId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			_armedSettlementCarryoverActive = true;
			_armedSettlementCarryoverSettlementId = text;
			_armedSettlementCarryoverSource = (source ?? "").Trim();
			Logger.Log("SceneTaunt", "Marked armed settlement carryover. SettlementId=" + text + ", Source=" + _armedSettlementCarryoverSource);
		}
	}

	private void ClearArmedSettlementCarryover(string reason)
	{
		if (_armedSettlementCarryoverActive || !string.IsNullOrWhiteSpace(_armedSettlementCarryoverSettlementId))
		{
			_armedSettlementCarryoverActive = false;
			_armedSettlementCarryoverSettlementId = "";
			_armedSettlementCarryoverSource = "";
			_armedCarryoverLastAlertSettlementId = "";
			_armedCarryoverLastAlertLocationId = "";
			Logger.Log("SceneTaunt", "Cleared armed settlement carryover. Reason=" + (reason ?? "N/A"));
		}
	}

	private static string GetCurrentCarryoverLocationIdSafe()
	{
		try
		{
			ICampaignMission current = CampaignMission.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				Location location = current.Location;
				obj = ((location != null) ? location.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			return ((string)obj).Trim().ToLowerInvariant();
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
		if (Instance != null)
		{
			string activeSettlementIdSafe = GetActiveSettlementIdSafe();
			if (!string.IsNullOrWhiteSpace(activeSettlementIdSafe))
			{
				Instance.MarkArmedSettlementCarryover(activeSettlementIdSafe, reason);
			}
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
		if (Instance != null && victim != null)
		{
			Instance._pendingSceneNotableBattleDeaths[victim] = killer;
			Logger.Log("SceneTaunt", string.Format("Marked pending deferred scene notable battle death. Hero={0}, Killer={1}, Reason={2}", victim.Name, (killer != null) ? killer.Name : null, reason ?? "N/A"));
		}
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
			if (!string.IsNullOrWhiteSpace(text) && !(num <= 0f))
			{
				if (Instance._pendingDeferredCrimeByFaction == null)
				{
					Instance._pendingDeferredCrimeByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
				}
				Instance._pendingDeferredCrimeByFaction.TryGetValue(text, out var value);
				Instance._pendingDeferredCrimeByFaction[text] = value + num;
				Logger.Log("SceneTaunt", string.Format("Queued deferred scene-taunt crime. Faction={0}, Added={1:0.##}, Pending={2:0.##}, Reason={3}", faction.Name, num, Instance._pendingDeferredCrimeByFaction[text], reason ?? "N/A"));
			}
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
			string text = (((faction != null) ? faction.StringId : null) ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || _pendingDeferredCrimeByFaction == null)
			{
				return 0f;
			}
			float value;
			return _pendingDeferredCrimeByFaction.TryGetValue(text, out value) ? MathF.Max(0f, value) : 0f;
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
			float num = MathF.Max(0f, (faction != null) ? faction.MainHeroCrimeRating : 0f);
			float pendingDeferredCrimeAmount = GetPendingDeferredCrimeAmount(faction);
			return MathF.Max(0f, num + pendingDeferredCrimeAmount);
		}
		catch
		{
			return MathF.Max(0f, (faction != null) ? faction.MainHeroCrimeRating : 0f);
		}
	}

	private void TryShowTrackedCrimeTotalMessage(IFaction faction)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		try
		{
			if (faction != null)
			{
				float trackedCrimeTotalAmount = GetTrackedCrimeTotalAmount(faction);
				if (!(trackedCrimeTotalAmount <= 0f))
				{
					InformationManager.DisplayMessage(new InformationMessage($"你在{faction.Name}积累了{trackedCrimeTotalAmount:0.#}犯罪度！", new Color(1f, 0.45f, 0.2f, 1f)));
				}
			}
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
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		try
		{
			if (settlement == null || RewardSystemBehavior.Instance == null || Instance == null)
			{
				return;
			}
			string text = (((MBObjectBase)settlement).StringId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text))
			{
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
				string text2 = (string.IsNullOrWhiteSpace(victimName) ? "匪类" : victimName);
				InformationManager.DisplayMessage(new InformationMessage($"击倒 {text2}：{settlement.Name} 的公共信任 +1.3。", new Color(0.45f, 1f, 0.45f, 1f)));
				Logger.Log("SceneTaunt", $"Rewarded settlement trust for criminal knockdown. Settlement={settlement.Name}, Victim={text2}, GrantedTenths=13, WholeApplied={num2}, CarryTenths={num3}");
			}
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
			string text = (((faction != null) ? faction.StringId : null) ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || _crimeRefillReserveByFaction == null)
			{
				return 0f;
			}
			float value;
			return _crimeRefillReserveByFaction.TryGetValue(text, out value) ? MathF.Max(0f, value) : 0f;
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
			string text = (((faction != null) ? faction.StringId : null) ?? "").Trim();
			float num = MathF.Max(0f, amount);
			if (!string.IsNullOrWhiteSpace(text) && !(num <= 0f))
			{
				if (_crimeRefillReserveByFaction == null)
				{
					_crimeRefillReserveByFaction = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
				}
				_crimeRefillReserveByFaction.TryGetValue(text, out var value);
				_crimeRefillReserveByFaction[text] = value + num;
				_lastObservedNativeCrimeByFaction[text] = MathF.Max(0f, faction.MainHeroCrimeRating);
				Logger.Log("SceneTaunt", string.Format("Added scene-taunt crime refill reserve. Faction={0}, Added={1:0.##}, Reserve={2:0.##}, Reason={3}", faction.Name, num, _crimeRefillReserveByFaction[text], reason ?? "N/A"));
			}
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
			IFaction val = ResolveFactionById(text);
			if (val == null)
			{
				_crimeRefillReserveByFaction.Remove(item.Key);
				continue;
			}
			try
			{
				float num2 = MathF.Max(0f, val.MainHeroCrimeRating);
				float value;
				float num3 = (_lastObservedNativeCrimeByFaction.TryGetValue(text, out value) ? MathF.Max(0f, value) : num2);
				if (num2 >= num3 - 0.01f)
				{
					_lastObservedNativeCrimeByFaction[text] = num2;
					continue;
				}
				Campaign current2 = Campaign.Current;
				float? obj;
				if (current2 == null)
				{
					obj = null;
				}
				else
				{
					GameModels models = current2.Models;
					if (models == null)
					{
						obj = null;
					}
					else
					{
						CrimeModel crimeModel = models.CrimeModel;
						obj = ((crimeModel != null) ? new float?(crimeModel.GetMaxCrimeRating()) : ((float?)null));
					}
				}
				float num4 = MathF.Max(0f, (obj ?? 100f) - num2);
				float num5 = MathF.Min(num, num4);
				if (num5 <= 0f)
				{
					_lastObservedNativeCrimeByFaction[text] = num2;
					continue;
				}
				ChangeCrimeRatingAction.Apply(val, num5, true);
				float num6 = MathF.Max(0f, num - num5);
				float num7 = MathF.Max(0f, val.MainHeroCrimeRating);
				_lastObservedNativeCrimeByFaction[text] = num7;
				if (num6 <= 0f)
				{
					_crimeRefillReserveByFaction.Remove(item.Key);
				}
				else
				{
					_crimeRefillReserveByFaction[text] = num6;
				}
				Logger.Log("SceneTaunt", $"Injected scene-taunt crime reserve into native crime after native decay. Faction={val.Name}, PreviousNative={num3:0.##}, CurrentNativeBeforeInject={num2:0.##}, Added={num5:0.##}, RemainingReserve={num6:0.##}, NativeAfterInject={num7:0.##}");
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
			float num = MathF.Max(0f, (faction != null) ? faction.MainHeroCrimeRating : 0f);
			float num2 = Instance?.GetPendingDeferredCrimeAmount(faction) ?? 0f;
			Campaign current = Campaign.Current;
			float? obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				GameModels models = current.Models;
				if (models == null)
				{
					obj = null;
				}
				else
				{
					CrimeModel crimeModel = models.CrimeModel;
					obj = ((crimeModel != null) ? new float?(crimeModel.GetMaxCrimeRating()) : ((float?)null));
				}
			}
			float num3 = obj ?? 100f;
			return MBMath.ClampFloat(num + num2, 0f, num3);
		}
		catch
		{
			return MathF.Max(0f, (faction != null) ? faction.MainHeroCrimeRating : 0f);
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
			Logger.Log("SceneTaunt", string.Format("Cleared deferred scene-taunt crime. Faction={0}, Amount={1:0.##}, Reason={2}", faction.Name, num, reason ?? "N/A"));
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
			if (Instance != null)
			{
				Instance._pendingForcedPlayerExecution = true;
				Instance._pendingForcedPlayerExecutionExecutorHeroId = (((executor != null) ? ((MBObjectBase)executor).StringId : null) ?? "").Trim();
				Instance._pendingForcedPlayerExecutionMenuId = (menuId ?? "").Trim();
				Logger.Log("SceneTaunt", string.Format("Marked pending forced player execution. Executor={0}, Menu={1}, Reason={2}", (executor != null) ? executor.Name : null, Instance._pendingForcedPlayerExecutionMenuId, reason ?? "N/A"));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Queueing pending forced player execution failed: " + ex.Message);
		}
	}

	internal static void QueuePendingMainHeroBattleDeathForExternal(Hero killer, string reason)
	{
		string text = (((killer != null) ? ((MBObjectBase)killer).StringId : null) ?? "").Trim();
		_pendingMainHeroBattleDeath = true;
		if (!string.IsNullOrWhiteSpace(text))
		{
			_pendingMainHeroBattleDeathKillerHeroId = text;
		}
		_pendingMainHeroBattleDeathRequestUtcTicks = DateTime.UtcNow.Ticks;
		Instance?.ClearPendingForcedPlayerExecution("scene_taunt_battle_death");
		Logger.Log("SceneTaunt", string.Format("Marked pending scene-taunt main hero battle death. Killer={0}, Reason={1}", (killer != null) ? killer.Name : null, reason ?? "N/A"));
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
			Campaign current = Campaign.Current;
			return (current == null) ? null : current.Factions?.FirstOrDefault((IFaction x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
			Game current = Game.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				MBObjectManager objectManager = current.ObjectManager;
				obj = ((objectManager != null) ? objectManager.GetObject<Hero>(text) : null);
			}
			Hero val = (Hero)obj;
			if (val != null)
			{
				return val;
			}
		}
		catch
		{
		}
		try
		{
			Hero val2 = ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Hero x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			if (val2 != null)
			{
				return val2;
			}
			return ((IEnumerable<Hero>)Hero.DeadOrDisabledHeroes).FirstOrDefault((Hero x) => x != null && string.Equals((((MBObjectBase)x).StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static void MarkPendingTemporaryDungeonWarPeace(IFaction playerFaction, IFaction enemyFaction, string reason)
	{
		if (Instance != null && playerFaction != null && enemyFaction != null && playerFaction != enemyFaction)
		{
			Instance._pendingTemporaryDungeonWarPeace = true;
			Instance._pendingTemporaryDungeonWarPlayerFactionId = (playerFaction.StringId ?? "").Trim();
			Instance._pendingTemporaryDungeonWarEnemyFactionId = (enemyFaction.StringId ?? "").Trim();
			Logger.Log("SceneTaunt", string.Format("Marked pending temporary dungeon war peace. Reason={0}, PlayerFaction={1}, EnemyFaction={2}", reason ?? "N/A", playerFaction.Name, enemyFaction.Name));
		}
	}

	private static void ClearPendingTemporaryDungeonWarPeace(string reason)
	{
		if (Instance != null)
		{
			Instance._pendingTemporaryDungeonWarPeace = false;
			Instance._pendingTemporaryDungeonWarPlayerFactionId = "";
			Instance._pendingTemporaryDungeonWarEnemyFactionId = "";
			Logger.Log("SceneTaunt", "Cleared pending temporary dungeon war peace. Reason=" + (reason ?? "N/A"));
		}
	}

	internal static void TryStartTemporaryDungeonWarForExternal(PartyBase captorParty, Hero targetHero, string reason)
	{
		try
		{
			PartyBase mainParty = PartyBase.MainParty;
			IFaction val = ((mainParty != null) ? mainParty.MapFaction : null);
			IFaction val2 = ((captorParty != null) ? captorParty.MapFaction : null) ?? ((targetHero != null) ? targetHero.MapFaction : null);
			bool flag = val != null && val2 != null && val != val2 && FactionManager.IsAtWarAgainstFaction(val, val2);
			LordEncounterBehavior.ApplyHostileEscalationDiplomaticConsequences(captorParty, targetHero, reason ?? "scene_taunt_dungeon_defeat", "SceneTaunt");
			PartyBase mainParty2 = PartyBase.MainParty;
			IFaction val3 = ((mainParty2 != null) ? mainParty2.MapFaction : null);
			if (!flag && val3 != null && val2 != null && val3 != val2 && FactionManager.IsAtWarAgainstFaction(val3, val2))
			{
				MarkPendingTemporaryDungeonWarPeace(val3, val2, reason ?? "scene_taunt_dungeon_defeat");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SceneTaunt", "Starting temporary dungeon war failed: " + ex.Message);
		}
	}

	internal static bool IsEligibleSceneTauntCharacter(CharacterObject targetCharacter)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		if (targetCharacter == null || ((BasicCharacterObject)targetCharacter).IsHero || IsChildSceneProtectedTarget(targetCharacter))
		{
			return false;
		}
		Occupation occupation = targetCharacter.Occupation;
		Occupation val = occupation;
		if ((int)val == 15 || (int)val == 21 || (int)val == 27)
		{
			return true;
		}
		if (IsSoldierSceneTauntTarget(targetCharacter))
		{
			return true;
		}
		Occupation occupation2 = targetCharacter.Occupation;
		Occupation val2 = occupation2;
		if ((int)val2 == 2 || (int)val2 == 5 || val2 - 23 <= 1)
		{
			return false;
		}
		return !((BasicCharacterObject)targetCharacter).IsSoldier;
	}

	internal static bool IsSoldierSceneTauntTarget(CharacterObject targetCharacter)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		return targetCharacter != null && !((BasicCharacterObject)targetCharacter).IsHero && (int)targetCharacter.Occupation == 7;
	}

	internal static bool IsSceneLordTauntTarget(Hero targetHero)
	{
		return targetHero != null && targetHero.IsLord && !IsMeetingTauntContext(targetHero);
	}

	internal static bool IsSceneNotableTauntTarget(Hero targetHero)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Invalid comparison between Unknown and I4
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
			Occupation occupation = targetHero.Occupation;
			Occupation val = occupation;
			if (val - 17 <= 5)
			{
				return true;
			}
			return false;
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
			int result;
			if (((BasicCharacterObject)targetCharacter).Age > 0f)
			{
				float age = ((BasicCharacterObject)targetCharacter).Age;
				Campaign current = Campaign.Current;
				int? obj;
				if (current == null)
				{
					obj = null;
				}
				else
				{
					GameModels models = current.Models;
					if (models == null)
					{
						obj = null;
					}
					else
					{
						AgeModel ageModel = models.AgeModel;
						obj = ((ageModel != null) ? new int?(ageModel.HeroComesOfAge) : ((int?)null));
					}
				}
				result = ((age < (float?)obj) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
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
			Hero targetHero2 = MeetingBattleRuntime.TargetHero;
			if (targetHero2 != null && targetHero2 != targetHero)
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
		return BuildSceneTauntRuntimeInstructionForExternal((targetCharacter != null) ? targetCharacter.HeroObject : null, targetCharacter, targetAgentIndex);
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
				return "【AFEF与匪徒冲突规则】你是匪徒/帮派成员，这一类冲突不走普通场景口角的警告-升级流程，也不要输出[ACTION:SCENE_TAUNT_WARN]。" + text + "若只是嘴硬、试探、骂你几句，你可以继续放狠话、威胁、索要买路钱或逼他滚开；只有当你明确决定和他立刻翻脸开打时，才在句末输出[ACTION:SCENE_TAUNT_FIGHT]。一旦输出，该冲突会进入原版的匪徒冲突流程。";
			}
			string targetKey = BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex);
			bool flag = HasSceneTauntWarning(targetKey);
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
		return TryProcessSceneTauntAction((targetCharacter != null) ? targetCharacter.HeroObject : null, targetCharacter, targetAgentIndex, ref content, out escalatedToFight);
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
			string targetKey = BuildSceneTauntTargetKey(targetHero, targetCharacter, targetAgentIndex);
			if (flag && IsSceneTauntApplicable(targetHero, targetCharacter, targetAgentIndex))
			{
				RememberSceneTauntWarning(targetKey);
			}
			if (flag2)
			{
				escalatedToFight = TryStartSceneTauntFight(targetHero, targetCharacter, targetAgentIndex, targetKey);
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
		return BuildSceneTauntTargetKey((targetCharacter != null) ? targetCharacter.HeroObject : null, targetCharacter, targetAgentIndex);
	}

	internal static string BuildSceneTauntTargetKey(Hero targetHero, CharacterObject targetCharacter, int targetAgentIndex)
	{
		try
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			string text = (((currentSettlement != null) ? ((MBObjectBase)currentSettlement).StringId : null) ?? "").Trim().ToLowerInvariant();
			ICampaignMission current = CampaignMission.Current;
			object obj;
			if (current == null)
			{
				obj = null;
			}
			else
			{
				Location location = current.Location;
				obj = ((location != null) ? location.StringId : null);
			}
			if (obj == null)
			{
				obj = "";
			}
			string text2 = ((string)obj).Trim().ToLowerInvariant();
			string text3 = (((targetHero != null) ? ((MBObjectBase)targetHero).StringId : null) ?? "").Trim().ToLowerInvariant();
			if (IsSceneLordTauntTarget(targetHero) && !string.IsNullOrWhiteSpace(text3))
			{
				return "scene_lord:" + text + ":" + text2 + ":" + text3;
			}
			string text4 = (((targetCharacter != null) ? ((MBObjectBase)targetCharacter).StringId : null) ?? "").Trim().ToLowerInvariant();
			string text5 = (((targetCharacter == null) ? null : ((object)((BasicCharacterObject)targetCharacter).Name)?.ToString()) ?? "").Trim().ToLowerInvariant();
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
				return "scene_troop:" + text + ":" + text2 + ":" + text4 + ":" + text5;
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
			return ((current != null) ? current.GetMissionBehavior<SceneTauntMissionBehavior>() : null)?.CanStartConflict(targetHero, targetCharacter, targetAgentIndex) ?? false;
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
			Mission current = Mission.Current;
			SceneTauntMissionBehavior sceneTauntMissionBehavior = ((current != null) ? current.GetMissionBehavior<SceneTauntMissionBehavior>() : null);
			if (sceneTauntMissionBehavior == null || !sceneTauntMissionBehavior.CanStartConflict(targetHero, targetCharacter, targetAgentIndex))
			{
				Logger.Log("SceneTaunt", "Fight tag ignored because current scene taunt context is not applicable.");
				return false;
			}
			try
			{
				Campaign current2 = Campaign.Current;
				if (current2 != null)
				{
					ConversationManager conversationManager = current2.ConversationManager;
					if (conversationManager != null)
					{
						conversationManager.EndConversation();
					}
				}
			}
			catch
			{
			}
			bool flag = sceneTauntMissionBehavior.TryStartConflict(targetHero, targetCharacter, targetAgentIndex, targetKey, fromVerbalTaunt: true);
			if (flag)
			{
				Logger.Log("SceneTaunt", $"Scene taunt fight started. Target={((targetHero != null) ? targetHero.Name : null) ?? ((targetCharacter != null) ? ((BasicCharacterObject)targetCharacter).Name : null)}, AgentIndex={targetAgentIndex}, Key={targetKey}");
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
		if (!string.IsNullOrWhiteSpace(text) && Instance != null)
		{
			if (Instance._warnedSceneTargetKeys == null)
			{
				Instance._warnedSceneTargetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}
			if (Instance._warnedSceneTargetKeys.Add(text))
			{
				Logger.Log("SceneTaunt", "Recorded warning state for target=" + text);
			}
		}
	}

	internal static string BuildFrightenedCivilianShoutExtraFactExternal(Agent targetAgent)
	{
		try
		{
			Mission current = Mission.Current;
			return ((current == null) ? null : current.GetMissionBehavior<SceneTauntMissionBehavior>()?.BuildFrightenedCivilianShoutExtraFact(targetAgent)) ?? "";
		}
		catch
		{
			return "";
		}
	}
}
