using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace AnimusForge;

using static AnimusForge.ShoutBehavior;


internal sealed class SceneMovementController
{
    private readonly SceneMovementPorts _ports;
    private readonly object _launchGate = new object();
    private long _generation;
    private Mission _mission;
    internal SceneMovementController(SceneMovementPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }
	internal sealed class PendingSceneSummonReturnAfterSpeech
	{
		public int AgentIndex;

		public SceneSummonConversationSession Session;

		public bool ReturnOnlySpeaker;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	internal sealed class PendingSceneFollowCommand
	{
		public int AgentIndex;

		public bool StartFollow;

		public bool WaitForPlaybackFinished;

		public float ExecuteAtMissionTime = -1f;
	}

	internal sealed class PendingSceneGuideReturnAfterSpeech
	{
		public int AgentIndex;

		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;

		public bool WaitForPlaybackFinished;

		public bool PostPlaybackDelayArmed;

		public bool SpeechPlaybackScheduled;

		public float ExecuteAtMissionTime = -1f;
	}

	internal sealed class SceneGuideArrivalHold
	{
		public int AgentIndex;

		public Vec3 AnchorPosition;

		public float ExpiresAtMissionTime;
	}

	internal enum SceneGuideStage
	{
		Guiding
	}

	internal sealed class ActiveSceneGuideRequest
	{
		public int GuideAgentIndex = -1;

		public string GuideName;

		public string TargetName;

		public int TargetPromptId = -1;

		public LocationCharacter GuideLocationCharacter;

		public LocationCharacter TargetLocationCharacter;

		public Location CurrentLocation;

		public Location OriginalGuideLocation;

		public Vec3? OriginalGuidePosition;

		public Location TargetSourceLocation;

		public Passage TargetDoorPassage;

		public int DoorProxyAgentIndex = -1;

		public SceneGuideStage Stage = SceneGuideStage.Guiding;

		public float NextStageMissionTime;

		public bool EscortStarted;

		public bool ArrivalTriggered;

        public bool Cancelled;

		public bool ArrivalReachedDoor;
	}

	internal sealed class ActiveSceneSummonRequest
	{
		public int BatchId = -1;

		public int SpeakerAgentIndex = -1;

		public string SpeakerName;

		public int TargetPromptId;

		public string TargetName;

		public LocationCharacter SpeakerLocationCharacter;

		public LocationCharacter TargetLocationCharacter;

		public Location CurrentLocation;

		public Location OriginalSpeakerLocation;

		public Location OriginalTargetLocation;

		public Location TargetSourceLocation;

		public Location PassageHopLocation;

		public Vec3? OriginalSpeakerPosition;

		public Vec3? OriginalTargetPosition;

		public Passage MessengerDoorPassage;

		public int DoorProxyAgentIndex = -1;

		public float NextStageMissionTime;

		public float ArrivalSpeechDeadlineMissionTime;

		public string PreGeneratedArrivalSpeech;

		public bool ArrivalSpeechConsumed;

		public SceneSummonStage Stage;

		public SceneSummonStage PendingLaunchStage;

		public string LaunchAnnouncement;

		public bool KeepMessengerWithTarget = true;

		public bool BatchContinuationStarted;
	}

	internal sealed class SceneSummonConversationParticipant
	{
		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;
	}

	internal sealed class SceneSummonConversationSession
	{
		public int BatchId = -1;

		public int SpeakerAgentIndex = -1;

		public string SpeakerName;

		public LocationCharacter SpeakerLocationCharacter;

		public Location OriginalSpeakerLocation;

		public Vec3? OriginalSpeakerPosition;

		public bool KeepSpeakerNearby = true;

		public List<SceneSummonConversationParticipant> Participants = new List<SceneSummonConversationParticipant>();
	}

	internal sealed class SceneSummonBatchState
	{
		public int BatchId = -1;

		public int SpeakerAgentIndex = -1;

		public string SpeakerName;

		public LocationCharacter SpeakerLocationCharacter;

		public Location OriginalSpeakerLocation;

		public Vec3? OriginalSpeakerPosition;

		public Queue<SceneSummonPromptTarget> PendingTargets = new Queue<SceneSummonPromptTarget>();

		public bool CompletionFactRecorded;
	}

	internal sealed class SceneReturnJob
	{
		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location CurrentLocation;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;

		public Passage ExitPassage;

		public int DoorProxyAgentIndex = -1;
	}

	internal sealed class SceneFollowReturnState
	{
		public string DisplayName;

		public LocationCharacter LocationCharacter;

		public Location OriginalLocation;

		public Vec3? OriginalPosition;
	}

	internal sealed class SceneGhostWalkState
	{
		public Vec3 LastPosition;

		public Vec3 LastTarget;

		public float LastDistanceSquared;

		public float LastProgressMissionTime;

		public float LastCheckMissionTime;

		public float LastStepMissionTime;
	}

	internal enum SceneSummonStage
	{
		PendingLaunch,
		MessengerToTarget,
		MessengerToDoor,
		WaitingForTarget,
		TargetToPlayer
	}

	private const float SCENE_FOLLOW_MAX_IDLE_DISTANCE = 0f;

	private const float SCENE_COMMAND_FOLLOWER_CACHE_REFRESH_SECONDS = 0.2f;

	private const float SCENE_SUMMON_ESCORT_REPOSITION_DISTANCE_SQ = 25f;

	private const float SCENE_GUIDE_PLAYER_REQUIRED_DISTANCE_SQ = 100f;

	private const float SCENE_GUIDE_TARGET_REACHED_DISTANCE_SQ = 36f;

	private const float SCENE_GUIDE_PLAYER_LOST_TIMEOUT = 45f;

	private const float SCENE_GUIDE_RETURN_EXTRA_DELAY_SECONDS = 2f;

	private const float SCENE_GUIDE_ARRIVAL_HOLD_FAILSAFE_SECONDS = 90f;

	private static readonly FieldInfo FollowAgentBehaviorIdleDistanceField = typeof(FollowAgentBehavior).GetField("_idleDistance", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly Regex SceneSummonActionTagRegex = new Regex("\\[(?:ASS|ACTION:SCENE_SUMMON):([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneGuideActionTagRegex = new Regex("\\[(?:GUI|ACTION:SCENE_GUIDE):([^\\]\\r\\n]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneFollowStartTagRegex = new Regex("\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneFollowStopTagRegex = new Regex("\\[(?:STP|ACTION:SCENE_STOP_FOLLOW)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SceneEndChatActionTagRegex = new Regex("\\[END\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly FieldInfo PrisonBreakPrisonerAgentField = typeof(PrisonBreakMissionController).GetField("_prisonerAgent", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo PrisonBreakPrisonerFollowingField = typeof(PrisonBreakMissionController).GetField("_isPrisonerFollowing", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo PrisonBreakSwitchPrisonerFollowingStateMethod = typeof(PrisonBreakMissionController).GetMethod("SwitchPrisonerFollowingState", BindingFlags.Instance | BindingFlags.NonPublic);

	private const int SCENE_SUMMON_PROMPT_TARGET_LIMIT = 12;

	private const int SCENE_GUIDE_PROMPT_TARGET_LIMIT = 14;

	private const float SCENE_SUMMON_MESSENGER_DOOR_DISTANCE_SQ = 2.25f;

	private const float SCENE_SUMMON_MESSENGER_TARGET_DISTANCE_SQ = 100f;

	private const float SCENE_SUMMON_RELAY_DISTANCE_SQ = 9f;

	private const float SCENE_SUMMON_TARGET_ARRIVAL_DISTANCE_SQ = 9f;

	private const float SCENE_SUMMON_DELAY_SECONDS = 2.25f;

	private const float SCENE_GHOST_STUCK_CHECK_INTERVAL = 0.45f;

	private const float SCENE_GHOST_STUCK_SECONDS = 1.8f;

	private const float SCENE_GHOST_MIN_PROGRESS_DISTANCE_SQ = 0.0225f;

	private const float SCENE_GHOST_TARGET_CHANGE_DISTANCE_SQ = 2.25f;

	private const float SCENE_GHOST_MIN_TARGET_DISTANCE_SQ = 9f;

	private const float SCENE_GHOST_PLAYER_TELEPORT_DISABLE_DISTANCE_SQ = 100f;

	private const float SCENE_GHOST_STEP_COOLDOWN = 0.55f;

	private const float SCENE_GHOST_STEP_DISTANCE = 1.35f;

	private const float SCENE_GHOST_MAX_STEP_DISTANCE = 2.75f;

	private readonly List<ActiveSceneSummonRequest> _activeSceneSummonRequests = new List<ActiveSceneSummonRequest>();

	private readonly List<ActiveSceneGuideRequest> _activeSceneGuideRequests = new List<ActiveSceneGuideRequest>();

	private readonly List<SceneSummonConversationSession> _activeSceneSummonConversationSessions = new List<SceneSummonConversationSession>();

	private readonly Dictionary<int, SceneSummonBatchState> _activeSceneSummonBatches = new Dictionary<int, SceneSummonBatchState>();

	private readonly List<SceneReturnJob> _activeSceneReturnJobs = new List<SceneReturnJob>();

	private readonly HashSet<int> _sceneSummonScriptedAgentIndices = new HashSet<int>();

	private readonly Dictionary<int, Queue<ActiveSceneSummonRequest>> _pendingSceneSummonLaunchQueues = new Dictionary<int, Queue<ActiveSceneSummonRequest>>();

	private readonly Dictionary<int, Queue<ActiveSceneGuideRequest>> _pendingSceneGuideLaunchQueues = new Dictionary<int, Queue<ActiveSceneGuideRequest>>();

	private readonly Dictionary<int, PendingSceneSummonReturnAfterSpeech> _pendingSceneSummonReturnsAfterSpeech = new Dictionary<int, PendingSceneSummonReturnAfterSpeech>();

	private readonly Dictionary<int, PendingSceneFollowCommand> _pendingSceneFollowCommands = new Dictionary<int, PendingSceneFollowCommand>();

	private readonly Dictionary<int, PendingSceneGuideReturnAfterSpeech> _pendingSceneGuideReturnsAfterSpeech = new Dictionary<int, PendingSceneGuideReturnAfterSpeech>();

	private readonly Dictionary<int, SceneGuideArrivalHold> _sceneGuideArrivalHolds = new Dictionary<int, SceneGuideArrivalHold>();

	private readonly Dictionary<int, SceneFollowReturnState> _sceneFollowReturnStates = new Dictionary<int, SceneFollowReturnState>();

	private readonly HashSet<int> _transientSceneFollowAgentIndices = new HashSet<int>();

	private readonly List<Agent> _sceneCommandFollowerCache = new List<Agent>();

	private Mission _sceneCommandFollowerCacheMission;

	private float _nextSceneCommandFollowerCacheRefreshApplicationTime;

	private readonly Dictionary<int, SceneGhostWalkState> _sceneGhostWalkStates = new Dictionary<int, SceneGhostWalkState>();

	private int _nextSceneSummonBatchId = 1;

	internal static string GetSceneSummonTargetDisplayName(NpcDataPacket npc)
	{
		if (npc == null)
		{
			return "";
		}
		return (npc.IsHero ? GetSceneNpcIdentityNameForPrompt(npc) : GetSceneNpcGivenNameForPrompt(npc)).Trim();
	}

	internal static string GetSceneSummonLocationCode(Location location)
	{
		string text = (location?.StringId ?? "").Trim().ToLowerInvariant();
		return text switch
		{
			"center" => "城镇街道",
			"village_center" => "村庄中心",
			"tavern" => "酒馆",
			"lordshall" => "领主大厅",
			"prison" => "地牢",
			"arena" => "竞技场",
			"alley" => "小巷",
			"port" => "港口",
			_ => GetSceneLocationDisplayName(location),
		};
	}

	internal static List<Location> FindSceneLocationPath(Location fromLocation, Location toLocation)
	{
		if (fromLocation == null || toLocation == null)
		{
			return null;
		}
		if (fromLocation == toLocation)
		{
			return new List<Location> { fromLocation };
		}
		Queue<Location> queue = new Queue<Location>();
		Dictionary<Location, Location> dictionary = new Dictionary<Location, Location>();
		HashSet<Location> hashSet = new HashSet<Location>();
		hashSet.Add(fromLocation);
		queue.Enqueue(fromLocation);
		while (queue.Count > 0)
		{
			Location location = queue.Dequeue();
			IEnumerable<Location> enumerable = location.LocationsOfPassages ?? Enumerable.Empty<Location>();
			foreach (Location item in enumerable)
			{
				if (item == null || !hashSet.Add(item))
				{
					continue;
				}
				dictionary[item] = location;
				if (item == toLocation)
				{
					List<Location> list = new List<Location> { toLocation };
					Location location2 = toLocation;
					while (dictionary.TryGetValue(location2, out var value))
					{
						list.Add(value);
						if (value == fromLocation)
						{
							break;
						}
						location2 = value;
					}
					list.Reverse();
					return list;
				}
				queue.Enqueue(item);
			}
		}
		return null;
	}

	internal List<SceneSummonPromptTarget> BuildSceneSummonPromptTargets(IEnumerable<NpcDataPacket> presentNpcs, Dictionary<int, Hero> resolvedHeroes = null)
	{
		List<SceneSummonPromptTarget> list = new List<SceneSummonPromptTarget>();
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		if (locationComplex == null || location == null)
		{
			return list;
		}
		HashSet<LocationCharacter> hashSet = new HashSet<LocationCharacter>();
		void tryAdd(LocationCharacter locationCharacter, string displayName)
		{
			if (locationCharacter == null || hashSet.Contains(locationCharacter) || locationCharacter.Character == null || locationCharacter.Character == CharacterObject.PlayerCharacter)
			{
				return;
			}
			Location locationOfCharacter = locationComplex.GetLocationOfCharacter(locationCharacter);
			if (locationOfCharacter == null)
			{
				return;
			}
			List<Location> sceneLocationPath = FindSceneLocationPath(location, locationOfCharacter);
			if (sceneLocationPath == null || sceneLocationPath.Count == 0)
			{
				return;
			}
			string text = (displayName ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = (locationCharacter.Character.Name?.ToString() ?? "").Trim();
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			hashSet.Add(locationCharacter);
			list.Add(new SceneSummonPromptTarget
			{
				DisplayName = text,
				LocationCode = GetSceneSummonLocationCode(locationOfCharacter),
				LocationCharacter = locationCharacter,
				SourceLocation = locationOfCharacter
			});
		}
		if (presentNpcs != null)
		{
			foreach (NpcDataPacket presentNpc in presentNpcs)
			{
				if (presentNpc == null)
				{
					continue;
				}
				LocationCharacter locationCharacter2 = null;
				if (presentNpc.IsHero)
				{
					Hero hero = null;
					if (resolvedHeroes != null)
					{
						resolvedHeroes.TryGetValue(presentNpc.AgentIndex, out hero);
					}
					hero ??= _ports.ResolveHeroFromAgentIndex(presentNpc.AgentIndex);
					if (hero != null)
					{
						locationCharacter2 = locationComplex.GetLocationCharacterOfHero(hero);
					}
				}
				else
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == presentNpc.AgentIndex);
					if (agent != null)
					{
						locationCharacter2 = locationComplex.FindCharacter(agent);
					}
				}
				tryAdd(locationCharacter2, GetSceneSummonTargetDisplayName(presentNpc));
				if (list.Count >= SCENE_SUMMON_PROMPT_TARGET_LIMIT)
				{
					break;
				}
			}
		}
		if (list.Count < SCENE_SUMMON_PROMPT_TARGET_LIMIT)
		{
			IEnumerable<IGrouping<Location, LocationCharacter>> enumerable2 = from locationCharacter3 in locationComplex.GetListOfCharacters()
				where locationCharacter3 != null && locationCharacter3.Character != null && locationCharacter3.Character.IsHero && locationCharacter3.Character.HeroObject != null && locationCharacter3.Character.HeroObject != Hero.MainHero
				group locationCharacter3 by locationComplex.GetLocationOfCharacter(locationCharacter3) into grouped
				orderby (FindSceneLocationPath(location, grouped.Key)?.Count ?? int.MaxValue), (grouped.Key?.StringId ?? ""), (grouped.FirstOrDefault()?.Character?.Name?.ToString() ?? "")
				select grouped;
			foreach (IGrouping<Location, LocationCharacter> item in enumerable2)
			{
				foreach (LocationCharacter item2 in item.OrderBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture))
				{
					tryAdd(item2, item2.Character?.HeroObject?.Name?.ToString());
					if (list.Count >= SCENE_SUMMON_PROMPT_TARGET_LIMIT)
					{
						break;
					}
				}
				if (list.Count >= SCENE_SUMMON_PROMPT_TARGET_LIMIT)
				{
					break;
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].PromptId = i + 1;
		}
		return list;
	}

	internal static List<SceneSummonPromptTarget> CloneSceneSummonPromptTargets(List<SceneSummonPromptTarget> source)
	{
		if (source == null || source.Count == 0)
		{
			return null;
		}
		List<SceneSummonPromptTarget> list = new List<SceneSummonPromptTarget>(source.Count);
		foreach (SceneSummonPromptTarget item in source)
		{
			if (item == null)
			{
				continue;
			}
			list.Add(new SceneSummonPromptTarget
			{
				PromptId = item.PromptId,
				DisplayName = item.DisplayName,
				LocationCode = item.LocationCode,
				LocationCharacter = item.LocationCharacter,
				SourceLocation = item.SourceLocation
			});
		}
		return list;
	}

	internal static string GetSceneGuideCategoryDisplayName(Occupation occupation)
	{
		return occupation switch
		{
			Occupation.Armorer => "盔甲匠",
			Occupation.Weaponsmith => "武器匠",
			Occupation.Blacksmith => "铁匠",
			Occupation.HorseTrader => "马匹贩子",
			Occupation.Merchant => "商贩",
			Occupation.GoodsTrader => "商贩",
			Occupation.ArenaMaster => "竞技场老板",
			Occupation.Tavernkeeper => "酒馆老板",
			Occupation.RansomBroker => "赎金经纪人",
			Occupation.TavernGameHost => "棋局主持",
			Occupation.TavernWench => "酒馆女侍",
			Occupation.Musician => "乐师",
			Occupation.Mercenary => "雇佣兵",
			Occupation.ShopWorker => "工坊工人",
			_ => ""
		};
	}

	internal static bool TryGetSceneGuideTargetLabel(LocationCharacter locationCharacter, out string displayName, out bool preferNearest)
	{
		displayName = "";
		preferNearest = false;
		Occupation occupation = Occupation.NotAssigned;
		try
		{
			if (locationCharacter?.Character is CharacterObject characterObject)
			{
				occupation = characterObject.Occupation;
			}
		}
		catch
		{
			occupation = Occupation.NotAssigned;
		}
		displayName = GetSceneGuideCategoryDisplayName(occupation);
		try
		{
			if (string.IsNullOrWhiteSpace(displayName) && locationCharacter?.Character is CharacterObject characterObject && Settlement.CurrentSettlement?.Culture?.Barber == characterObject)
			{
				displayName = "理发师";
			}
		}
		catch
		{
		}
		preferNearest = occupation == Occupation.Merchant || occupation == Occupation.GoodsTrader || occupation == Occupation.ShopWorker;
		return !string.IsNullOrWhiteSpace(displayName);
	}

	internal static bool IsSceneGuideTavernkeeper(LocationCharacter locationCharacter)
	{
		try
		{
			return locationCharacter?.Character is CharacterObject characterObject && characterObject.Occupation == Occupation.Tavernkeeper;
		}
		catch
		{
			return false;
		}
	}

	internal static LocationCharacter CreateSceneGuideFallbackTavernkeeper(CultureObject culture)
	{
		if (culture?.Tavernkeeper == null)
		{
			return null;
		}
		try
		{
			Type type = AppDomain.CurrentDomain.GetAssemblies().Select((Assembly a) => a?.GetType("SandBox.CampaignBehaviors.TavernEmployeesCampaignBehavior", throwOnError: false)).FirstOrDefault((Type t) => t != null);
			MethodInfo methodInfo = type?.GetMethod("CreateTavernkeeper", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
			{
				typeof(CultureObject),
				typeof(LocationCharacter.CharacterRelations)
			}, null);
			return methodInfo?.Invoke(null, new object[2]
			{
				culture,
				LocationCharacter.CharacterRelations.Neutral
			}) as LocationCharacter;
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("SceneGuide", "fallback_tavernkeeper_create_failed error=" + ex.Message);
			}
			catch
			{
			}
			return null;
		}
	}

	internal static void EnsureSceneGuideTavernkeeperTarget(LocationComplex locationComplex, Location currentLocation, Dictionary<string, List<LocationCharacter>> targetsByLabel)
	{
		if (locationComplex == null || currentLocation == null || targetsByLabel == null)
		{
			return;
		}
		try
		{
			Location tavernLocation = locationComplex.GetLocationWithId("tavern");
			if (tavernLocation == null || FindSceneLocationPath(currentLocation, tavernLocation) == null)
			{
				return;
			}
			if (targetsByLabel.TryGetValue("酒馆老板", out var existingTargets) && existingTargets != null && existingTargets.Any((LocationCharacter x) => x != null && locationComplex.GetLocationOfCharacter(x) == tavernLocation))
			{
				return;
			}
			LocationCharacter locationCharacter = tavernLocation.GetCharacterList().FirstOrDefault(IsSceneGuideTavernkeeper);
			if (locationCharacter == null)
			{
				Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.LocationEncounter?.Settlement;
				if (settlement == null || !settlement.IsTown)
				{
					return;
				}
				locationCharacter = CreateSceneGuideFallbackTavernkeeper(settlement.Culture);
				if (locationCharacter == null)
				{
					return;
				}
				tavernLocation.AddCharacter(locationCharacter);
				try
				{
					Logger.Log("SceneGuide", "fallback_tavernkeeper_added settlement=" + (settlement.StringId ?? "") + " location=tavern");
				}
				catch
				{
				}
			}
			if (!TryGetSceneGuideTargetLabel(locationCharacter, out var displayName, out var _) || !string.Equals(displayName, "酒馆老板", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			if (!targetsByLabel.TryGetValue(displayName, out var list))
			{
				list = new List<LocationCharacter>();
				targetsByLabel[displayName] = list;
			}
			if (!list.Contains(locationCharacter))
			{
				list.Add(locationCharacter);
			}
		}
		catch (Exception ex2)
		{
			try
			{
				Logger.Log("SceneGuide", "fallback_tavernkeeper_target_failed error=" + ex2.Message);
			}
			catch
			{
			}
		}
	}

	internal List<SceneGuidePromptTarget> BuildSceneGuidePromptTargets(Agent guideAgent = null, int firstPromptId = 1)
	{
		List<SceneGuidePromptTarget> list = new List<SceneGuidePromptTarget>();
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		if (locationComplex == null || location == null)
		{
			return list;
		}
		Dictionary<string, List<LocationCharacter>> dictionary = new Dictionary<string, List<LocationCharacter>>(StringComparer.OrdinalIgnoreCase);
		foreach (LocationCharacter item in locationComplex.GetListOfCharacters())
		{
			if (!TryGetSceneGuideTargetLabel(item, out var displayName, out var _))
			{
				continue;
			}
			if (string.Equals(displayName, "商贩", StringComparison.OrdinalIgnoreCase) && (item.Character?.IsHero ?? false))
			{
				continue;
			}
			Location locationOfCharacter = locationComplex.GetLocationOfCharacter(item);
			if (locationOfCharacter == null)
			{
				continue;
			}
			List<Location> sceneLocationPath = FindSceneLocationPath(location, locationOfCharacter);
			if (sceneLocationPath == null || sceneLocationPath.Count == 0)
			{
				continue;
			}
			if (!dictionary.TryGetValue(displayName, out var value))
			{
				value = new List<LocationCharacter>();
				dictionary[displayName] = value;
			}
			value.Add(item);
		}
		EnsureSceneGuideTavernkeeperTarget(locationComplex, location, dictionary);
		Vec3 vec = guideAgent?.Position ?? Agent.Main?.Position ?? Vec3.Zero;
		string[] array = new string[14] { "盔甲匠", "武器匠", "铁匠", "马匹贩子", "商贩", "工坊工人", "理发师", "竞技场老板", "酒馆老板", "赎金经纪人", "棋局主持", "酒馆女侍", "乐师", "雇佣兵" };
		foreach (string text in array)
		{
			if (!dictionary.TryGetValue(text, out var value2) || value2 == null || value2.Count == 0)
			{
				continue;
			}
			LocationCharacter locationCharacter = value2[0];
			if (string.Equals(text, "商贩", StringComparison.OrdinalIgnoreCase))
			{
				locationCharacter = value2.OrderBy(delegate(LocationCharacter x)
				{
					Agent agent = ResolveAgentForLocationCharacter(x);
					if (agent != null && agent.IsActive())
					{
						return agent.Position.DistanceSquared(vec);
					}
					return float.MaxValue;
				}).ThenBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture).FirstOrDefault();
			}
			else if (string.Equals(text, "酒馆老板", StringComparison.OrdinalIgnoreCase))
			{
				Location tavernLocation = locationComplex.GetLocationWithId("tavern");
				bool currentIsTavern = IsSceneLocation(location, "tavern");
				locationCharacter = value2.OrderBy(delegate(LocationCharacter x)
				{
					Location candidateLocation = locationComplex.GetLocationOfCharacter(x);
					if (currentIsTavern)
					{
						return candidateLocation == location ? 0 : 1;
					}
					return candidateLocation == tavernLocation ? 0 : 1;
				}).ThenBy(delegate(LocationCharacter x)
				{
					Agent agent = ResolveAgentForLocationCharacter(x);
					return agent != null && agent.IsActive() ? 0 : 1;
				}).ThenBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture).FirstOrDefault();
			}
			else
			{
				locationCharacter = value2.OrderBy((LocationCharacter x) => (x.Character?.Name?.ToString() ?? "").Trim(), StringComparer.CurrentCulture).FirstOrDefault();
			}
			if (locationCharacter == null)
			{
				continue;
			}
			Location locationOfCharacter2 = locationComplex.GetLocationOfCharacter(locationCharacter);
			if (locationOfCharacter2 == null)
			{
				continue;
			}
			list.Add(new SceneGuidePromptTarget
			{
				PromptId = Math.Max(1, firstPromptId) + list.Count,
				DisplayName = text,
				LocationCode = GetSceneSummonLocationCode(locationOfCharacter2),
				LocationCharacter = locationCharacter,
				SourceLocation = locationOfCharacter2
			});
			if (list.Count >= SCENE_GUIDE_PROMPT_TARGET_LIMIT)
			{
				break;
			}
		}
		return list;
	}

	internal static List<SceneGuidePromptTarget> CloneSceneGuidePromptTargets(List<SceneGuidePromptTarget> source)
	{
		if (source == null || source.Count == 0)
		{
			return null;
		}
		List<SceneGuidePromptTarget> list = new List<SceneGuidePromptTarget>(source.Count);
		foreach (SceneGuidePromptTarget item in source)
		{
			if (item == null)
			{
				continue;
			}
			list.Add(new SceneGuidePromptTarget
			{
				PromptId = item.PromptId,
				DisplayName = item.DisplayName,
				LocationCode = item.LocationCode,
				LocationCharacter = item.LocationCharacter,
				SourceLocation = item.SourceLocation
			});
		}
		return list;
	}

	internal static void AppendSceneSummonPromptSection(StringBuilder prompt, List<SceneSummonPromptTarget> targets)
	{
		if (prompt == null || targets == null || targets.Count == 0)
		{
			return;
		}
		prompt.AppendLine("【可召目标】：");
		foreach (SceneSummonPromptTarget target in targets)
		{
			if (target != null && target.PromptId > 0 && !string.IsNullOrWhiteSpace(target.DisplayName))
			{
				prompt.AppendLine(target.PromptId + " " + target.DisplayName.Trim() + " " + (target.LocationCode ?? "处"));
			}
		}
	}

	internal void EnqueuePendingSceneSummonLaunch(int agentIndex, ActiveSceneSummonRequest request)
	{
		if (agentIndex < 0 || request == null)
		{
			return;
		}
		lock (_launchGate)
		{
			if (!_pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<ActiveSceneSummonRequest>();
				_pendingSceneSummonLaunchQueues[agentIndex] = value;
			}
			value.Enqueue(request);
		}
	}

	internal void EnqueuePendingSceneGuideLaunch(int agentIndex, ActiveSceneGuideRequest request)
	{
		if (agentIndex < 0 || request == null)
		{
			return;
		}
		lock (_launchGate)
		{
			if (!_pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out var value))
			{
				value = new Queue<ActiveSceneGuideRequest>();
				_pendingSceneGuideLaunchQueues[agentIndex] = value;
			}
			value.Enqueue(request);
		}
	}

	internal bool TryDequeuePendingSceneSummonLaunch(int agentIndex, out ActiveSceneSummonRequest request)
	{
		request = null;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_launchGate)
		{
			if (_pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out var value) && value.Count > 0)
			{
				request = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingSceneSummonLaunchQueues.Remove(agentIndex);
				}
				return request != null;
			}
		}
		return false;
	}

	internal bool TryDequeuePendingSceneGuideLaunch(int agentIndex, out ActiveSceneGuideRequest request)
	{
		request = null;
		if (agentIndex < 0)
		{
			return false;
		}
		lock (_launchGate)
		{
			if (_pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out var value) && value.Count > 0)
			{
				request = value.Dequeue();
				if (value.Count == 0)
				{
					_pendingSceneGuideLaunchQueues.Remove(agentIndex);
				}
				return request != null;
			}
		}
		return false;
	}

	internal void FlushPendingSceneSummonLaunches(int agentIndex, float additionalDelaySeconds = 0f)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		while (TryDequeuePendingSceneSummonLaunch(agentIndex, out var request))
		{
			request.NextStageMissionTime = mission.CurrentTime + Math.Max(0f, additionalDelaySeconds);
			LogSceneSummonState("pending_launch_tts_finished", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "extraDelay=" + additionalDelaySeconds.ToString("F2"), force: true);
		}
	}

	internal void FlushPendingSceneGuideLaunches(int agentIndex, float additionalDelaySeconds = 0f)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		while (TryDequeuePendingSceneGuideLaunch(agentIndex, out var request))
		{
			request.NextStageMissionTime = mission.CurrentTime + Math.Max(0f, additionalDelaySeconds);
		}
	}

	internal void CancelSceneSummonBatch(int batchId)
	{
		if (batchId < 0)
		{
			return;
		}
		_activeSceneSummonBatches.Remove(batchId);
		foreach (ActiveSceneSummonRequest item in _activeSceneSummonRequests.Where((ActiveSceneSummonRequest x) => x != null && x.BatchId == batchId).ToList())
		{
			CleanupSceneSummonDoorProxyAgent(item);
		}
		_activeSceneSummonRequests.RemoveAll((ActiveSceneSummonRequest x) => x != null && x.BatchId == batchId);
		lock (_launchGate)
		{
			List<int> list = _pendingSceneSummonLaunchQueues.Keys.ToList();
			foreach (int item in list)
			{
				if (!_pendingSceneSummonLaunchQueues.TryGetValue(item, out var value) || value == null || value.Count == 0)
				{
					continue;
				}
				Queue<ActiveSceneSummonRequest> queue = new Queue<ActiveSceneSummonRequest>(value.Where((ActiveSceneSummonRequest x) => x != null && x.BatchId != batchId));
				if (queue.Count == 0)
				{
					_pendingSceneSummonLaunchQueues.Remove(item);
				}
				else
				{
					_pendingSceneSummonLaunchQueues[item] = queue;
				}
			}
		}
	}

	internal void TryAdvanceSceneSummonBatch(ActiveSceneSummonRequest request, Agent speakerAgent)
	{
		if (request == null || request.BatchContinuationStarted || request.BatchId < 0 || request.KeepMessengerWithTarget)
		{
			return;
		}
		request.BatchContinuationStarted = true;
		if (!_activeSceneSummonBatches.TryGetValue(request.BatchId, out var value) || value == null || value.PendingTargets.Count == 0)
		{
			return;
		}
		if (TryStartNextSceneSummonBatchRequest(value, speakerAgent, isInitialRequest: false, out var preparedRequest))
		{
			LogSceneSummonState("batch_followup_queued", preparedRequest, speakerAgent, ResolveAgentForLocationCharacter(preparedRequest.TargetLocationCharacter), "remaining=" + value.PendingTargets.Count, force: true);
			return;
		}
		CancelSceneSummonBatch(request.BatchId);
	}

	internal static string BuildSceneSummonBatchTargetSummary(SceneSummonBatchState batch, SceneSummonPromptTarget currentTarget)
	{
		List<string> list = new List<string>();
		if (currentTarget != null && !string.IsNullOrWhiteSpace(currentTarget.DisplayName))
		{
			list.Add(currentTarget.DisplayName.Trim());
		}
		if (batch != null)
		{
			foreach (SceneSummonPromptTarget pendingTarget in batch.PendingTargets)
			{
				string text = pendingTarget?.DisplayName;
				if (!string.IsNullOrWhiteSpace(text))
				{
					text = text.Trim();
					if (!list.Contains(text))
					{
						list.Add(text);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return "那些人";
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		return string.Join("、", list);
	}

	internal string BuildSceneSummonArrivedDisplayNames(int batchId, string fallbackTargetName)
	{
		if (batchId >= 0)
		{
			SceneSummonConversationSession sceneSummonConversationSession = _activeSceneSummonConversationSessions.FirstOrDefault((SceneSummonConversationSession x) => x != null && x.BatchId == batchId);
			if (sceneSummonConversationSession != null)
			{
				List<string> list = sceneSummonConversationSession.Participants.Where((SceneSummonConversationParticipant x) => x != null && !string.IsNullOrWhiteSpace(x.DisplayName)).Select((SceneSummonConversationParticipant x) => x.DisplayName.Trim()).Distinct().ToList();
				if (list.Count == 1)
				{
					return list[0];
				}
				if (list.Count > 1)
				{
					return string.Join("、", list);
				}
			}
		}
		return fallbackTargetName ?? "那个人";
	}

	internal void TryRecordSceneSummonBatchCompletionFact(ActiveSceneSummonRequest request)
	{
		if (request == null || request.BatchId < 0 || request.SpeakerAgentIndex < 0)
		{
			return;
		}
		if (!_activeSceneSummonBatches.TryGetValue(request.BatchId, out var value) || value == null || value.CompletionFactRecorded)
		{
			return;
		}
		if (value.PendingTargets.Count > 0)
		{
			return;
		}
		int num = _activeSceneSummonRequests.Count((ActiveSceneSummonRequest x) => x != null && x.BatchId == request.BatchId);
		if (num > 1)
		{
			return;
		}
		string text = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		string text2 = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = "此人";
		}
		_ports.AppendTargetedSceneNpcFact("你已将" + text2 + "要求召集的" + text + "都带到了" + text2 + "身边。", request.SpeakerAgentIndex, persistHeroPrivateHistory: true);
		value.CompletionFactRecorded = true;
	}

	internal string BuildSceneSummonClosurePromptInstruction(IEnumerable<NpcDataPacket> participants)
	{
		if (participants == null)
		{
			return "";
		}
		foreach (NpcDataPacket participant in participants)
		{
			if (participant == null || participant.AgentIndex < 0)
			{
				continue;
			}
			if (TryGetSceneSummonConversationSessionForAgentIndex(participant.AgentIndex) != null)
			{
				return "【当前是传唤后的会面】若你明确同意之后跟随此人，系统会记录你开始跟随；若你明确表示这次会面到此结束，系统会结束会面；若此人改让你去叫【带路与传唤NPC清单】中的人，系统会记录传唤；若此人改让你带路去找【带路与传唤NPC清单】中的目标，系统会记录带路。正文只自然说话，不要自己写标签。";
			}
		}
		return "";
	}

	internal string BuildSceneFollowControlPromptInstruction(NpcDataPacket speaker)
	{
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && speaker != null && a.Index == speaker.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return "";
		}
		if (IsPrisonBreakRescuePrisonerAgent(agent))
		{
			return IsPrisonBreakPrisonerFollowing(agent)
				? "【当前正按原版营救跟随玩家】若此人明确让你停下且你同意，系统会记录停止营救跟随。正文只自然说话，不要自己写标签。"
				: "【营救跟随】若此人明确让你跟随一起逃离且你同意，系统会记录原版营救跟随。正文只自然说话，不要自己写标签。";
		}
		if (TryGetSceneSummonConversationSessionForAgentIndex(agent.Index) != null)
		{
			return "【当前是传唤后的会面】若玩家明确要求你在当前场景跟随、陪同或保护玩家，且你在正文明确同意，系统会记录开始跟随；若玩家明确表示会面结束或要求你回去，系统会记录停止跟随并恢复岗位。若玩家改让你去叫【带路与传唤NPC清单】中的人，系统会记录传唤；若玩家改让你带路去找【带路与传唤NPC清单】中的目标，系统会记录带路。正文只自然说话，不要自己写标签。";
		}
		return IsAgentFollowingPlayerBySceneCommand(agent)
			? "【当前正跟随玩家】若玩家明确要求你停止跟随、退下或回到岗位，且你在正文明确同意，系统会记录停止跟随；若玩家改让你去叫【带路与传唤NPC清单】中的人，系统会记录传唤；若玩家改让你带路去找【带路与传唤NPC清单】中的目标，系统会记录带路。正文只自然说话，不要自己写标签。"
			: "【场景跟随】若玩家明确要求你在当前场景跟随、陪同、保护或随行，且你在正文明确无条件同意，系统会记录开始跟随；若你当前已经跟随，玩家明确要求停止跟随、退下或回到岗位，且你明确同意，系统会记录停止跟随。这里的跟随只适用于当前场景，不是大地图队伍命令。正文只自然说话，不要自己写标签。";
	}

	internal static Agent ResolveAgentForLocationCharacter(LocationCharacter locationCharacter)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (locationCharacter == null || agents == null)
		{
			return null;
		}
		IAgentOriginBase agentOrigin = locationCharacter.AgentOrigin;
		if (agentOrigin != null)
		{
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Origin == agentOrigin);
			if (agent != null)
			{
				return agent;
			}
		}
		CharacterObject character = locationCharacter.Character;
		if (character == null)
		{
			return null;
		}
		return agents.FirstOrDefault((Agent a) => a != null && a.Character == character);
	}

	internal static Passage FindCurrentScenePassageToLocation(Location targetLocation)
	{
		if (targetLocation == null)
		{
			return null;
		}
		MissionAgentHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionAgentHandler>();
		IEnumerable<UsableMachine> enumerable = Enumerable.Empty<UsableMachine>();
		if (missionBehavior?.TownPassageProps != null)
		{
			enumerable = enumerable.Concat(missionBehavior.TownPassageProps);
		}
		if (missionBehavior?.DisabledPassages != null)
		{
			enumerable = enumerable.Concat(missionBehavior.DisabledPassages);
		}
		return enumerable.OfType<Passage>().FirstOrDefault((Passage x) => x != null && x.ToLocation == targetLocation);
	}

	internal Agent ResolveSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (request == null || request.DoorProxyAgentIndex < 0 || agents == null)
		{
			return null;
		}
		Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == request.DoorProxyAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			request.DoorProxyAgentIndex = -1;
			return null;
		}
		return agent;
	}

	internal Agent EnsureSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request, Passage passage, Agent speakerAgent)
	{
		Agent agent = ResolveSceneSummonDoorProxyAgent(request);
		if (agent != null)
		{
			return agent;
		}
		Mission mission = Mission.Current;
		if (request == null || passage == null || mission?.Scene == null)
		{
			return null;
		}
		CharacterObject characterObject = speakerAgent?.Character as CharacterObject;
		if (characterObject == null)
		{
			characterObject = request.SpeakerLocationCharacter?.Character as CharacterObject;
		}
		if (characterObject == null)
		{
			return null;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		passageWaitingPosition.z = mission.Scene.GetGroundHeightAtPosition(passageWaitingPosition, BodyFlags.CommonCollisionExcludeFlags);
		Vec3 vec3 = passageWaitingPosition;
		vec3.z -= 25f;
		Vec2 vec = Vec2.Forward;
		try
		{
			Vec2 vec2 = ((Agent.Main != null && Agent.Main.IsActive()) ? (Agent.Main.Position - passageWaitingPosition).AsVec2 : Vec2.Zero);
			if (vec2.LengthSquared > 0.001f)
			{
				vec = vec2.Normalized();
			}
			else if (speakerAgent != null)
			{
				vec = speakerAgent.LookDirection.AsVec2;
			}
		}
		catch
		{
		}
		Equipment equipment = null;
		try
		{
			equipment = speakerAgent?.SpawnEquipment?.Clone(false);
		}
		catch
		{
		}
		if (equipment == null)
		{
			try
			{
				equipment = (mission.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment)?.Clone(false);
			}
			catch
			{
			}
		}
		Team team = null;
		if (IsUsableTeam(speakerAgent?.Team))
		{
			team = speakerAgent.Team;
		}
		else if (IsUsableTeam(mission.PlayerTeam))
		{
			team = mission.PlayerTeam;
		}
		else if (IsUsableTeam(mission.DefenderTeam))
		{
			team = mission.DefenderTeam;
		}
		else if (IsUsableTeam(mission.AttackerTeam))
		{
			team = mission.AttackerTeam;
		}
		if (!IsUsableTeam(team))
		{
			return null;
		}
		try
		{
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).InitialPosition(in passageWaitingPosition)
				.InitialDirection(in vec)
				.NoHorses(true)
				.Controller(AgentControllerType.AI);
			if (equipment != null)
			{
				agentBuildData = agentBuildData.Equipment(equipment);
			}
			Agent agent2 = mission.SpawnAgent(agentBuildData, false);
			if (agent2 == null)
			{
				return null;
			}
			request.DoorProxyAgentIndex = agent2.Index;
			try
			{
				agent2.AgentVisuals?.SetVisible(false);
			}
			catch
			{
			}
			try
			{
				agent2.AgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
			try
			{
				agent2.TeleportToPosition(vec3);
			}
			catch
			{
			}
			try
			{
				agent2.SetMaximumSpeedLimit(0f, isMultiplier: false);
			}
			catch
			{
			}
			try
			{
				agent2.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent2.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
			LogSceneSummonState("cross_scene_proxy_spawned", request, speakerAgent, agent2, "waitPos=" + FormatSceneSummonPosition(passageWaitingPosition) + " hiddenPos=" + FormatSceneSummonPosition(vec3), force: true);
			return agent2;
		}
		catch
		{
			request.DoorProxyAgentIndex = -1;
			return null;
		}
	}

	internal void CleanupSceneSummonDoorProxyAgent(ActiveSceneSummonRequest request)
	{
		if (request == null)
		{
			return;
		}
		Agent agent = ResolveSceneSummonDoorProxyAgent(request);
		request.DoorProxyAgentIndex = -1;
		if (agent == null)
		{
			return;
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
	}

	internal Agent ResolveSceneGuideDoorProxyAgent(ActiveSceneGuideRequest request)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (request == null || request.DoorProxyAgentIndex < 0 || agents == null)
		{
			return null;
		}
		Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == request.DoorProxyAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			request.DoorProxyAgentIndex = -1;
			return null;
		}
		return agent;
	}

	internal Agent EnsureSceneGuideDoorProxyAgent(ActiveSceneGuideRequest request, Passage passage, Agent guideAgent)
	{
		Agent agent = ResolveSceneGuideDoorProxyAgent(request);
		if (agent != null)
		{
			return agent;
		}
		Mission mission = Mission.Current;
		if (request == null || passage == null || mission?.Scene == null)
		{
			return null;
		}
		CharacterObject characterObject = guideAgent?.Character as CharacterObject;
		if (characterObject == null)
		{
			characterObject = request.GuideLocationCharacter?.Character as CharacterObject;
		}
		if (characterObject == null)
		{
			return null;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		passageWaitingPosition.z = mission.Scene.GetGroundHeightAtPosition(passageWaitingPosition, BodyFlags.CommonCollisionExcludeFlags);
		Vec3 vec3 = passageWaitingPosition;
		vec3.z -= 25f;
		Vec2 vec = Vec2.Forward;
		try
		{
			Vec2 vec2 = ((Agent.Main != null && Agent.Main.IsActive()) ? (Agent.Main.Position - passageWaitingPosition).AsVec2 : Vec2.Zero);
			if (vec2.LengthSquared > 0.001f)
			{
				vec = vec2.Normalized();
			}
			else if (guideAgent != null)
			{
				vec = guideAgent.LookDirection.AsVec2;
			}
		}
		catch
		{
		}
		Equipment equipment = null;
		try
		{
			equipment = guideAgent?.SpawnEquipment?.Clone(false);
		}
		catch
		{
		}
		if (equipment == null)
		{
			try
			{
				equipment = (mission.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment)?.Clone(false);
			}
			catch
			{
			}
		}
		Team team = null;
		if (IsUsableTeam(guideAgent?.Team))
		{
			team = guideAgent.Team;
		}
		else if (IsUsableTeam(mission.PlayerTeam))
		{
			team = mission.PlayerTeam;
		}
		else if (IsUsableTeam(mission.DefenderTeam))
		{
			team = mission.DefenderTeam;
		}
		else if (IsUsableTeam(mission.AttackerTeam))
		{
			team = mission.AttackerTeam;
		}
		if (!IsUsableTeam(team))
		{
			return null;
		}
		try
		{
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).InitialPosition(in passageWaitingPosition)
				.InitialDirection(in vec)
				.NoHorses(true)
				.Controller(AgentControllerType.AI);
			if (equipment != null)
			{
				agentBuildData = agentBuildData.Equipment(equipment);
			}
			Agent agent2 = mission.SpawnAgent(agentBuildData, false);
			if (agent2 == null)
			{
				return null;
			}
			request.DoorProxyAgentIndex = agent2.Index;
			try
			{
				agent2.AgentVisuals?.SetVisible(false);
			}
			catch
			{
			}
			try
			{
				agent2.AgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
			try
			{
				agent2.TeleportToPosition(vec3);
			}
			catch
			{
			}
			try
			{
				agent2.SetMaximumSpeedLimit(0f, isMultiplier: false);
			}
			catch
			{
			}
			try
			{
				agent2.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent2.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
			Logger.Log("SceneGuide", "door_proxy_spawned guide=" + (request.GuideName ?? "") + " target=" + (request.TargetName ?? "") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition) + " hiddenPos=" + FormatSceneSummonPosition(vec3));
			return agent2;
		}
		catch
		{
			request.DoorProxyAgentIndex = -1;
			return null;
		}
	}

	internal void CleanupSceneGuideDoorProxyAgent(ActiveSceneGuideRequest request)
	{
		if (request == null)
		{
			return;
		}
		Agent agent = ResolveSceneGuideDoorProxyAgent(request);
		request.DoorProxyAgentIndex = -1;
		if (agent == null)
		{
			return;
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
	}

	internal Agent ResolveSceneReturnDoorProxyAgent(SceneReturnJob job)
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (job == null || job.DoorProxyAgentIndex < 0 || agents == null)
		{
			return null;
		}
		Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == job.DoorProxyAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			job.DoorProxyAgentIndex = -1;
			return null;
		}
		return agent;
	}

	internal Agent EnsureSceneReturnDoorProxyAgent(SceneReturnJob job, Passage passage, Agent movingAgent)
	{
		Agent agent = ResolveSceneReturnDoorProxyAgent(job);
		if (agent != null)
		{
			return agent;
		}
		Mission mission = Mission.Current;
		if (job == null || passage == null || mission?.Scene == null)
		{
			return null;
		}
		CharacterObject characterObject = movingAgent?.Character as CharacterObject;
		if (characterObject == null)
		{
			characterObject = job.LocationCharacter?.Character as CharacterObject;
		}
		if (characterObject == null)
		{
			return null;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		passageWaitingPosition.z = mission.Scene.GetGroundHeightAtPosition(passageWaitingPosition, BodyFlags.CommonCollisionExcludeFlags);
		Vec3 vec3 = passageWaitingPosition;
		vec3.z -= 25f;
		Vec2 vec = Vec2.Forward;
		try
		{
			Vec2 vec2 = ((Agent.Main != null && Agent.Main.IsActive()) ? (Agent.Main.Position - passageWaitingPosition).AsVec2 : Vec2.Zero);
			if (vec2.LengthSquared > 0.001f)
			{
				vec = vec2.Normalized();
			}
			else if (movingAgent != null)
			{
				vec = movingAgent.LookDirection.AsVec2;
			}
		}
		catch
		{
		}
		Equipment equipment = null;
		try
		{
			equipment = movingAgent?.SpawnEquipment?.Clone(false);
		}
		catch
		{
		}
		if (equipment == null)
		{
			try
			{
				equipment = (mission.DoesMissionRequireCivilianEquipment ? characterObject.FirstCivilianEquipment : characterObject.FirstBattleEquipment)?.Clone(false);
			}
			catch
			{
			}
		}
		Team team = null;
		if (IsUsableTeam(movingAgent?.Team))
		{
			team = movingAgent.Team;
		}
		else if (IsUsableTeam(mission.PlayerTeam))
		{
			team = mission.PlayerTeam;
		}
		else if (IsUsableTeam(mission.DefenderTeam))
		{
			team = mission.DefenderTeam;
		}
		else if (IsUsableTeam(mission.AttackerTeam))
		{
			team = mission.AttackerTeam;
		}
		if (!IsUsableTeam(team))
		{
			return null;
		}
		try
		{
			AgentBuildData agentBuildData = new AgentBuildData(characterObject).Team(team).InitialPosition(in passageWaitingPosition)
				.InitialDirection(in vec)
				.NoHorses(true)
				.Controller(AgentControllerType.AI);
			if (equipment != null)
			{
				agentBuildData = agentBuildData.Equipment(equipment);
			}
			Agent agent2 = mission.SpawnAgent(agentBuildData, false);
			if (agent2 == null)
			{
				return null;
			}
			job.DoorProxyAgentIndex = agent2.Index;
			try
			{
				agent2.AgentVisuals?.SetVisible(false);
			}
			catch
			{
			}
			try
			{
				agent2.AgentVisuals?.GetEntity()?.SetVisibilityExcludeParents(false);
			}
			catch
			{
			}
			try
			{
				agent2.TeleportToPosition(vec3);
			}
			catch
			{
			}
			try
			{
				agent2.SetMaximumSpeedLimit(0f, isMultiplier: false);
			}
			catch
			{
			}
			try
			{
				agent2.SetIsAIPaused(isPaused: true);
			}
			catch
			{
			}
			try
			{
				agent2.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
			catch
			{
			}
			return agent2;
		}
		catch
		{
			job.DoorProxyAgentIndex = -1;
			return null;
		}
	}

	internal void CleanupSceneReturnDoorProxyAgent(SceneReturnJob job)
	{
		if (job == null)
		{
			return;
		}
		Agent agent = ResolveSceneReturnDoorProxyAgent(job);
		job.DoorProxyAgentIndex = -1;
		if (agent == null)
		{
			return;
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
	}

	internal List<NpcDataPacket> BuildSceneSummonArrivalContextSnapshot(LocationCharacter targetLocationCharacter, Agent speakerAgent)
	{
		List<NpcDataPacket> list = (ShoutUtils.GetNearbyNPCAgents() ?? new List<Agent>()).Select((Agent a) => ShoutUtils.ExtractNpcData(a)).Where((NpcDataPacket d) => d != null).ToList();
		if (speakerAgent != null && !list.Any((NpcDataPacket x) => x != null && x.AgentIndex == speakerAgent.Index))
		{
			NpcDataPacket item = ShoutUtils.ExtractNpcData(speakerAgent);
			if (item != null)
			{
				list.Add(item);
			}
		}
		NpcDataPacket sceneNpcDataFromLocationCharacter = _ports.BuildSceneNpcDataFromLocationCharacter(targetLocationCharacter);
		if (sceneNpcDataFromLocationCharacter != null)
		{
			if (sceneNpcDataFromLocationCharacter.AgentIndex >= 0)
			{
				if (!list.Any((NpcDataPacket x) => x != null && x.AgentIndex == sceneNpcDataFromLocationCharacter.AgentIndex))
				{
					list.Add(sceneNpcDataFromLocationCharacter);
				}
			}
			else if (!list.Any((NpcDataPacket x) => x != null && string.Equals((x.Name ?? "").Trim(), (sceneNpcDataFromLocationCharacter.Name ?? "").Trim(), StringComparison.OrdinalIgnoreCase) && x.IsHero == sceneNpcDataFromLocationCharacter.IsHero))
			{
				list.Add(sceneNpcDataFromLocationCharacter);
			}
		}
		ApplySceneLocalDisambiguatedNames(list);
		return list;
	}

	internal void PrimeSceneSummonArrivalSpeechAsync(ActiveSceneSummonRequest request)
	{
		if (request == null || request.TargetLocationCharacter?.Character == null)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		List<NpcDataPacket> sceneSummonArrivalContextSnapshot = BuildSceneSummonArrivalContextSnapshot(request.TargetLocationCharacter, agent);
		NpcDataPacket sceneNpcDataFromLocationCharacter = _ports.BuildSceneNpcDataFromLocationCharacter(request.TargetLocationCharacter);
		if (sceneNpcDataFromLocationCharacter == null)
		{
			return;
		}
		Hero hero = request.TargetLocationCharacter.Character.HeroObject;
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		string extraFactLine = request.SpeakerName + "将你带到了" + text + "面前，" + text + "找你有些事。";
		string singleReplyUserContent = "请只根据【当前场景公共对话与互动】和上面的【AFEF玩家行为补充】，以你的身份主动开口问" + text + "找你有什么事。控制在 18-40 字之间，只输出你说出口的话。";
        Mission sourceMission = Mission.Current;
        long generation = _generation;
        bool IsCurrent() => generation == Volatile.Read(ref _generation) && ReferenceEquals(sourceMission, Mission.Current) && _activeSceneSummonRequests.Contains(request);
		_ = Task.Run(async delegate
		{
			try
			{
				string text2 = await SceneCompactReactionRuntime.GenerateAsync(await _ports.CaptureCompactSceneReactionInputAsync(sceneNpcDataFromLocationCharacter, hero, sceneSummonArrivalContextSnapshot, extraFactLine, singleReplyUserContent, IsCurrent).ConfigureAwait(false)).ConfigureAwait(false);
				if (generation == Volatile.Read(ref _generation)) request.PreGeneratedArrivalSpeech = text2;
			}
			catch (Exception ex)
			{
				Logger.Log("ShoutBehavior", "[ERROR] PrimeSceneSummonArrivalSpeechAsync failed: " + ex.Message);
				request.PreGeneratedArrivalSpeech = "";
			}
		});
	}

	internal void PlaySceneSummonArrivalSpeechIfReady(ActiveSceneSummonRequest request, Agent speakerAgent)
	{
		if (request == null || request.ArrivalSpeechConsumed)
		{
			return;
		}
		request.ArrivalSpeechConsumed = true;
		string text = (request.PreGeneratedArrivalSpeech ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		NpcDataPacket sceneNpcDataFromLocationCharacter = _ports.BuildSceneNpcDataFromLocationCharacter(request.TargetLocationCharacter);
		if (sceneNpcDataFromLocationCharacter == null)
		{
			return;
		}
		List<NpcDataPacket> sceneSummonArrivalContextSnapshot = BuildSceneSummonArrivalContextSnapshot(request.TargetLocationCharacter, speakerAgent);
		if (!sceneSummonArrivalContextSnapshot.Any((NpcDataPacket x) => x != null && x.AgentIndex >= 0 && x.AgentIndex == sceneNpcDataFromLocationCharacter.AgentIndex))
		{
			sceneSummonArrivalContextSnapshot.Add(sceneNpcDataFromLocationCharacter);
			ApplySceneLocalDisambiguatedNames(sceneSummonArrivalContextSnapshot);
		}
		_ports.EnqueueSpeechLine(sceneNpcDataFromLocationCharacter, text, sceneSummonArrivalContextSnapshot, skipHistory: false, suppressStare: false);
	}

	internal SceneSummonConversationSession TryGetSceneSummonConversationSessionForAgentIndex(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return null;
		}
		for (int i = 0; i < _activeSceneSummonConversationSessions.Count; i++)
		{
			SceneSummonConversationSession sceneSummonConversationSession = _activeSceneSummonConversationSessions[i];
			if (sceneSummonConversationSession == null)
			{
				continue;
			}
			Agent agent = ResolveAgentForLocationCharacter(sceneSummonConversationSession.SpeakerLocationCharacter);
			if (agent != null && agent.Index == agentIndex)
			{
				return sceneSummonConversationSession;
			}
			for (int j = 0; j < sceneSummonConversationSession.Participants.Count; j++)
			{
				SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants[j];
				if (sceneSummonConversationParticipant == null)
				{
					continue;
				}
				Agent agent2 = ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter);
				if (agent2 != null && agent2.Index == agentIndex)
				{
					return sceneSummonConversationSession;
				}
			}
		}
		return null;
	}

	internal void RegisterSceneSummonConversationSession(ActiveSceneSummonRequest request, Agent speakerAgent, Agent targetAgent)
	{
		if (request == null || request.SpeakerLocationCharacter == null || request.TargetLocationCharacter == null)
		{
			return;
		}
		SceneSummonConversationSession sceneSummonConversationSession = null;
		if (request.BatchId >= 0)
		{
			sceneSummonConversationSession = _activeSceneSummonConversationSessions.FirstOrDefault((SceneSummonConversationSession x) => x != null && x.BatchId == request.BatchId);
		}
		if (sceneSummonConversationSession == null)
		{
			_activeSceneSummonConversationSessions.RemoveAll(delegate(SceneSummonConversationSession x)
			{
				if (x == null)
				{
					return true;
				}
				if (x.SpeakerLocationCharacter == request.SpeakerLocationCharacter)
				{
					return true;
				}
				return x.Participants.Any((SceneSummonConversationParticipant p) => p != null && p.LocationCharacter == request.TargetLocationCharacter);
			});
			sceneSummonConversationSession = new SceneSummonConversationSession
			{
				BatchId = request.BatchId,
				SpeakerAgentIndex = request.SpeakerAgentIndex,
				SpeakerName = request.SpeakerName,
				SpeakerLocationCharacter = request.SpeakerLocationCharacter,
				OriginalSpeakerLocation = request.TargetSourceLocation == request.CurrentLocation ? request.CurrentLocation : request.OriginalSpeakerLocation ?? request.CurrentLocation,
				OriginalSpeakerPosition = request.OriginalSpeakerPosition ?? ((speakerAgent != null && speakerAgent.IsActive()) ? new Vec3?(speakerAgent.Position) : null),
				KeepSpeakerNearby = request.KeepMessengerWithTarget
			};
			_activeSceneSummonConversationSessions.Add(sceneSummonConversationSession);
		}
		if (!sceneSummonConversationSession.Participants.Any((SceneSummonConversationParticipant x) => x != null && x.LocationCharacter == request.TargetLocationCharacter))
		{
			sceneSummonConversationSession.Participants.Add(new SceneSummonConversationParticipant
			{
				DisplayName = request.TargetName,
				LocationCharacter = request.TargetLocationCharacter,
				OriginalLocation = request.OriginalTargetLocation ?? request.TargetSourceLocation ?? request.CurrentLocation,
				OriginalPosition = request.OriginalTargetPosition ?? ((targetAgent != null && targetAgent.IsActive()) ? new Vec3?(targetAgent.Position) : null)
			});
		}
		RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
		LogSceneSummonState("conversation_session_registered", request, speakerAgent, targetAgent, null, force: true);
	}

	internal void RefreshSceneSummonConversationInteractions(SceneSummonConversationSession session)
	{
		if (session == null)
		{
			return;
		}
		List<NpcDataPacket> list = new List<NpcDataPacket>();
		NpcDataPacket sceneNpcDataFromLocationCharacter = _ports.BuildSceneNpcDataFromLocationCharacter(session.SpeakerLocationCharacter);
		if (sceneNpcDataFromLocationCharacter != null && sceneNpcDataFromLocationCharacter.AgentIndex >= 0)
		{
			list.Add(sceneNpcDataFromLocationCharacter);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			NpcDataPacket sceneNpcDataFromLocationCharacter2 = _ports.BuildSceneNpcDataFromLocationCharacter(session.Participants[i]?.LocationCharacter);
			if (sceneNpcDataFromLocationCharacter2 != null && sceneNpcDataFromLocationCharacter2.AgentIndex >= 0 && !list.Any((NpcDataPacket x) => x != null && x.AgentIndex == sceneNpcDataFromLocationCharacter2.AgentIndex))
			{
				list.Add(sceneNpcDataFromLocationCharacter2);
			}
		}
		int num = Math.Max(1, list.Count);
		for (int j = 0; j < list.Count; j++)
		{
			_ports.TrackPlayerInteraction(list[j], num, ACTIVE_INTERACTION_IDLE_TIMEOUT);
		}
	}

	internal void RemoveAgentFromSceneSummonConversationForFollow(Agent agent)
	{
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agent.Index);
		if (sceneSummonConversationSession == null)
		{
			return;
		}
		ClearSceneSummonConversationInteractionTimers(sceneSummonConversationSession);
		bool wasSpeaker = IsSceneSummonConversationSpeaker(sceneSummonConversationSession, agent);
		bool flag = false;
		for (int num = sceneSummonConversationSession.Participants.Count - 1; num >= 0; num--)
		{
			SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants[num];
			if (sceneSummonConversationParticipant != null && sceneSummonConversationParticipant.LocationCharacter != null && ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter) == agent)
			{
				sceneSummonConversationSession.Participants.RemoveAt(num);
				flag = true;
			}
		}
		if (wasSpeaker)
		{
			sceneSummonConversationSession.SpeakerAgentIndex = -1;
			sceneSummonConversationSession.SpeakerLocationCharacter = null;
			sceneSummonConversationSession.KeepSpeakerNearby = false;
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		Agent agent2 = ResolveAgentForLocationCharacter(sceneSummonConversationSession.SpeakerLocationCharacter);
		if (sceneSummonConversationSession.Participants.Count == 0)
		{
			_activeSceneSummonConversationSessions.Remove(sceneSummonConversationSession);
			if (!wasSpeaker && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent)
			{
				StopSceneSummonFollowPlayer(agent2);
				_sceneFollowReturnStates.Remove(agent2.Index);
				QueueSceneReturnJob(sceneSummonConversationSession.SpeakerName, sceneSummonConversationSession.SpeakerLocationCharacter, sceneSummonConversationSession.OriginalSpeakerLocation, sceneSummonConversationSession.OriginalSpeakerPosition);
			}
			if (agent2 != null)
			{
				_ports.CancelInteractionTimeoutArm(agent2.Index);
				_ports.RemoveInteractionSession(agent2.Index);
			}
		}
		else
		{
			CancelSceneSummonBatch(sceneSummonConversationSession.BatchId);
			RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
		}
		_ports.CancelInteractionTimeoutArm(agent.Index);
		_ports.RemoveInteractionSession(agent.Index);
	}

	internal void ClearSceneSummonConversationInteractionTimers(SceneSummonConversationSession session)
	{
		if (session == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		Agent agent = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		if (agent != null)
		{
			hashSet.Add(agent.Index);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			Agent agent2 = ResolveAgentForLocationCharacter(session.Participants[i]?.LocationCharacter);
			if (agent2 != null)
			{
				hashSet.Add(agent2.Index);
			}
		}
		foreach (int item in hashSet)
		{
			_ports.CancelInteractionTimeoutArm(item);
			_ports.RemoveInteractionSession(item);
		}
	}

	internal bool TryDetachSceneSummonConversationAgentForRelay(Agent agent, out Location originalLocation, out Vec3? originalPosition)
	{
		originalLocation = CampaignMission.Current?.Location;
		originalPosition = (agent != null && agent.IsActive()) ? new Vec3?(agent.Position) : null;
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return false;
		}
		SceneSummonConversationSession session = TryGetSceneSummonConversationSessionForAgentIndex(agent.Index);
		if (session == null)
		{
			return false;
		}
		LocationComplex locationComplex = LocationComplex.Current;
		LocationCharacter relayLocationCharacter = locationComplex?.FindCharacter(agent);
		if (relayLocationCharacter == null)
		{
			return false;
		}
		ClearSceneSummonConversationInteractionTimers(session);
		bool detached = false;
		for (int num = session.Participants.Count - 1; num >= 0; num--)
		{
			SceneSummonConversationParticipant participant = session.Participants[num];
			if (participant == null || participant.LocationCharacter == null)
			{
				continue;
			}
			Agent participantAgent = ResolveAgentForLocationCharacter(participant.LocationCharacter);
			if (participant.LocationCharacter != relayLocationCharacter && (participantAgent == null || participantAgent.Index != agent.Index))
			{
				continue;
			}
			originalLocation = participant.OriginalLocation ?? originalLocation;
			originalPosition = participant.OriginalPosition ?? originalPosition;
			session.Participants.RemoveAt(num);
			detached = true;
		}
		if (!detached && session.SpeakerLocationCharacter == relayLocationCharacter)
		{
			originalLocation = session.OriginalSpeakerLocation ?? originalLocation;
			originalPosition = session.OriginalSpeakerPosition ?? originalPosition;
			session.SpeakerAgentIndex = -1;
			session.SpeakerLocationCharacter = null;
			session.KeepSpeakerNearby = false;
			detached = true;
		}
		if (!detached)
		{
			RefreshSceneSummonConversationInteractions(session);
			return false;
		}
		CancelSceneSummonBatch(session.BatchId);
		CancelSceneReturnJob(relayLocationCharacter);
		_pendingSceneSummonReturnsAfterSpeech.Remove(agent.Index);
		_pendingSceneFollowCommands.Remove(agent.Index);
		_ports.CancelInteractionTimeoutArm(agent.Index);
		_ports.RemoveInteractionSession(agent.Index);
		_sceneFollowReturnStates.Remove(agent.Index);
		StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
		_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
		_ports.ReleaseSceneConversationAttention(new List<int> { agent.Index }, fullyRestoreAutonomy: true);
		if (session.SpeakerLocationCharacter == null && session.Participants.Count == 0)
		{
			_activeSceneSummonConversationSessions.Remove(session);
		}
		else if (session.Participants.Count == 0 && session.SpeakerLocationCharacter != null)
		{
			Agent oldSpeaker = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
			if (CanAgentParticipateInSceneSpeech(oldSpeaker) && oldSpeaker.Index != agent.Index)
			{
				StopSceneSummonFollowPlayer(oldSpeaker);
				_sceneFollowReturnStates.Remove(oldSpeaker.Index);
				QueueSceneReturnJob(session.SpeakerName, session.SpeakerLocationCharacter, session.OriginalSpeakerLocation, session.OriginalSpeakerPosition);
				_ports.CancelInteractionTimeoutArm(oldSpeaker.Index);
				_ports.RemoveInteractionSession(oldSpeaker.Index);
			}
			_activeSceneSummonConversationSessions.Remove(session);
		}
		else
		{
			RefreshSceneSummonConversationInteractions(session);
		}
		try
		{
			Logger.Log("SceneSummon", "relay_detached agent=" + agent.Index + " name=" + (agent.Name ?? "") + " originalLoc=" + (originalLocation?.StringId ?? "") + " originalPos=" + (originalPosition.HasValue ? FormatSceneSummonPosition(originalPosition.Value) : ""));
		}
		catch
		{
		}
		return true;
	}

	internal void BeginSceneSummonConversationReturn(SceneSummonConversationSession session)
	{
		if (session == null || CampaignMission.Current?.Location == null)
		{
			return;
		}
		string text = string.Join("、", session.Participants.Where((SceneSummonConversationParticipant x) => x != null && !string.IsNullOrWhiteSpace(x.DisplayName)).Select((SceneSummonConversationParticipant x) => x.DisplayName).Distinct());
		_activeSceneSummonConversationSessions.Remove(session);
		CancelSceneSummonBatch(session.BatchId);
		Agent agent = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		StopSceneSummonFollowPlayer(agent);
		if (agent != null)
		{
			_sceneFollowReturnStates.Remove(agent.Index);
		}
		QueueSceneReturnJob(session.SpeakerName, session.SpeakerLocationCharacter, session.OriginalSpeakerLocation, session.OriginalSpeakerPosition);
		for (int i = 0; i < session.Participants.Count; i++)
		{
			SceneSummonConversationParticipant sceneSummonConversationParticipant = session.Participants[i];
			if (sceneSummonConversationParticipant != null)
			{
				Agent agent2 = ResolveAgentForLocationCharacter(sceneSummonConversationParticipant.LocationCharacter);
				StopSceneSummonFollowPlayer(agent2);
				if (agent2 != null)
				{
					_sceneFollowReturnStates.Remove(agent2.Index);
				}
				QueueSceneReturnJob(sceneSummonConversationParticipant.DisplayName, sceneSummonConversationParticipant.LocationCharacter, sceneSummonConversationParticipant.OriginalLocation, sceneSummonConversationParticipant.OriginalPosition);
			}
		}
		List<int> list = new List<int>();
		if (agent != null)
		{
			list.Add(agent.Index);
		}
		for (int j = 0; j < session.Participants.Count; j++)
		{
			Agent agent2 = ResolveAgentForLocationCharacter(session.Participants[j]?.LocationCharacter);
			if (agent2 != null && !list.Contains(agent2.Index))
			{
				list.Add(agent2.Index);
			}
		}
		if (list.Count > 0)
		{
			_ports.ClearPendingSceneConversationAttentionRelease();
			_ports.RemoveSceneMovementSuppressionAgents(list);
			_ports.ReleaseSceneConversationAttention(list, fullyRestoreAutonomy: true);
			foreach (int item in list)
			{
				_pendingSceneFollowCommands.Remove(item);
				_pendingSceneSummonReturnsAfterSpeech.Remove(item);
				_ports.CancelInteractionTimeoutArm(item);
				_ports.RemoveInteractionSession(item);
			}
		}
	}

	internal bool IsSceneSummonConversationSpeaker(SceneSummonConversationSession session, Agent agent)
	{
		if (session == null || agent == null || !agent.IsActive())
		{
			return false;
		}
		Agent agent2 = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		return agent2 != null && agent2.Index == agent.Index;
	}

	internal void BeginSceneSummonSpeakerReturn(SceneSummonConversationSession session)
	{
		if (session == null || CampaignMission.Current?.Location == null)
		{
			return;
		}
		CancelSceneSummonBatch(session.BatchId);
		Agent agent = ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter);
		if (agent != null)
		{
			_pendingSceneFollowCommands.Remove(agent.Index);
			_pendingSceneSummonReturnsAfterSpeech.Remove(agent.Index);
			_ports.CancelInteractionTimeoutArm(agent.Index);
			_ports.RemoveInteractionSession(agent.Index);
			StopSceneSummonFollowPlayer(agent);
			_sceneFollowReturnStates.Remove(agent.Index);
			_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
			_ports.ReleaseSceneConversationAttention(new List<int> { agent.Index }, fullyRestoreAutonomy: true);
		}
		QueueSceneReturnJob(session.SpeakerName, session.SpeakerLocationCharacter, session.OriginalSpeakerLocation, session.OriginalSpeakerPosition);
		session.SpeakerAgentIndex = -1;
		session.SpeakerLocationCharacter = null;
		session.KeepSpeakerNearby = false;
		if (session.Participants.Count == 0)
		{
			_activeSceneSummonConversationSessions.Remove(session);
			return;
		}
		ClearSceneSummonConversationInteractionTimers(session);
		RefreshSceneSummonConversationInteractions(session);
	}

	internal void QueueSceneReturnJob(string displayName, LocationCharacter locationCharacter, Location originalLocation, Vec3? originalPosition)
	{
		if (locationCharacter == null)
		{
			return;
		}
		Location currentLocation = CampaignMission.Current?.Location;
		if (currentLocation == null)
		{
			return;
		}
		CancelSceneReturnJob(locationCharacter);
		_activeSceneReturnJobs.Add(new SceneReturnJob
		{
			DisplayName = displayName,
			LocationCharacter = locationCharacter,
			CurrentLocation = currentLocation,
			OriginalLocation = originalLocation ?? currentLocation,
			OriginalPosition = originalPosition,
			ExitPassage = null
		});
	}

	internal void CancelSceneReturnJob(LocationCharacter locationCharacter)
	{
		if (locationCharacter == null)
		{
			return;
		}
		foreach (SceneReturnJob item in _activeSceneReturnJobs.Where((SceneReturnJob x) => x != null && x.LocationCharacter == locationCharacter).ToList())
		{
			CleanupSceneReturnDoorProxyAgent(item);
		}
		_activeSceneReturnJobs.RemoveAll((SceneReturnJob x) => x == null || x.LocationCharacter == locationCharacter);
	}

	internal void CancelSceneTemporaryCommandStateForFollow(Agent agent)
	{
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		LocationCharacter locationCharacter = null;
		try
		{
			locationCharacter = LocationComplex.Current?.FindCharacter(agent);
		}
		catch
		{
			locationCharacter = null;
		}
		if (locationCharacter != null)
		{
			CancelSceneReturnJob(locationCharacter);
		}
		CancelSceneGuideActionForAgent(agent.Index);
		RemoveAgentFromSceneSummonConversationForFollow(agent);
		CancelSceneSummonActionForAgent(agent.Index, locationCharacter);
		lock (_launchGate)
		{
			_pendingSceneGuideLaunchQueues.Remove(agent.Index);
		}
		_pendingSceneGuideReturnsAfterSpeech.Remove(agent.Index);
		_ports.CancelAutonomyRestore(agent.Index);
		_ports.CancelInteractionTimeoutArm(agent.Index);
		_ports.RemoveInteractionSession(agent.Index);
		_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
	}

	internal void PrepareAgentForSceneSummonMovement(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_ports.DetachStareAgent(agent.Index);
		_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
		_ports.ReleaseAgentFromSceneConversationLocks(agent);
		_ports.RestoreAgentAutonomy(agent);
	}

	internal void TrackSceneSummonScriptedAgent(Agent agent)
	{
		if (agent != null && agent.IsActive() && agent.Index >= 0)
		{
			_sceneSummonScriptedAgentIndices.Add(agent.Index);
		}
	}

	internal void ClearSceneSummonScriptedBehavior(Agent agent)
	{
		if (agent == null || agent.Index < 0 || !_sceneSummonScriptedAgentIndices.Contains(agent.Index))
		{
			return;
		}
		_sceneSummonScriptedAgentIndices.Remove(agent.Index);
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			DailyBehaviorGroup behaviorGroup = component?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup?.ScriptedBehavior is ScriptBehavior)
			{
				behaviorGroup.DisableScriptedBehavior();
			}
		}
		catch
		{
		}
	}

	internal void ReleaseAgentForSceneSummonAction(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_ports.DetachStareAgent(agent.Index);
		_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
		_ports.RemoveAttentionRelease(agent.Index);
		_ports.CancelInteractionTimeoutArm(agent.Index);
		_ports.RemoveInteractionSession(agent.Index);
		try
		{
			if (_ports.ReleaseUseConversationSlot(agent.Index) && agent.CurrentlyUsedGameObject != null)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationEnd();
			}
		}
		catch
		{
		}
		try
		{
			agent.SetLookAgent(null);
		}
		catch
		{
		}
		try
		{
			agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
		}
		catch
		{
		}
		if (!ShouldPreserveMeetingSceneAutonomy())
		{
			try
			{
				agent.DisableScriptedMovement();
			}
			catch
			{
			}
		}
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
	}

	internal bool NavigateAgentToScenePassage(Agent agent, Passage passage)
	{
		if (agent == null || !agent.IsActive() || passage == null)
		{
			return false;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		return NavigateAgentToWorldPosition(agent, passageWaitingPosition, 0.9f, doNotRun: false);
	}

	internal Vec3 GetPassageWaitingPosition(Passage passage)
	{
		Vec3 result;
		var scene = Mission.Current?.Scene;
		if (scene == null || passage == null)
		{
			return Vec3.Zero;
		}
		try
		{
			if (passage.PilotStandingPoint != null && passage.PilotStandingPoint.GameEntity != null)
			{
				MatrixFrame globalFrame = passage.PilotStandingPoint.GameEntity.GetGlobalFrame();
				result = globalFrame.origin;
				Vec3 vec = globalFrame.rotation.f;
				if (vec.LengthSquared > 0.0001f)
				{
					vec.Normalize();
					result -= vec * 0.35f;
				}
				result.z = scene.GetGroundHeightAtPosition(result, BodyFlags.CommonCollisionExcludeFlags);
				return result;
			}
		}
		catch
		{
		}
		try
		{
			result = passage.GameEntity.GlobalPosition;
			result.z = scene.GetGroundHeightAtPosition(result, BodyFlags.CommonCollisionExcludeFlags);
			return result;
		}
		catch
		{
			return Vec3.Zero;
		}
	}

	internal bool NavigateAgentToWorldPosition(Agent agent, Vec3 targetPosition, float rangeThreshold = 0.8f, bool doNotRun = false)
	{
		var scene = Mission.Current?.Scene;
		if (agent == null || !agent.IsActive() || scene == null)
		{
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(agent);
			Vec2 vec = (targetPosition - agent.Position).AsVec2;
			float rotationInRadians = agent.LookDirection.AsVec2.RotationInRadians;
			if (vec.LengthSquared > 0.0001f)
			{
				rotationInRadians = vec.RotationInRadians;
			}
			targetPosition.z = scene.GetGroundHeightAtPosition(targetPosition, BodyFlags.CommonCollisionExcludeFlags);
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			if (agentNavigator == null)
			{
				return false;
			}
			WorldPosition worldPosition = new WorldPosition(scene, targetPosition);
			WorldFrame targetWorldFrame = new WorldFrame(Mat3.CreateMat3WithForward(Vec3.Forward), worldPosition);
			try
			{
				Vec2 vec2 = new Vec2((float)Math.Cos(rotationInRadians), (float)Math.Sin(rotationInRadians));
				targetWorldFrame = new WorldFrame(Mat3.CreateMat3WithForward(vec2.ToVec3()), worldPosition);
			}
			catch
			{
			}
			ScriptBehavior.AddWorldFrameTarget(agent, targetWorldFrame);
			TrackSceneSummonScriptedAgent(agent);
			try
			{
				WorldPosition origin = targetWorldFrame.Origin;
				agent.SetScriptedPosition(ref origin, addHumanLikeDelay: false, doNotRun ? Agent.AIScriptedFrameFlags.DoNotRun : Agent.AIScriptedFrameFlags.NeverSlowDown);
			}
			catch
			{
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal Dictionary<int, Vec3> BuildSceneGhostMovementTargets(List<Agent> sceneCommandFollowers)
	{
		Dictionary<int, Vec3> targets = new Dictionary<int, Vec3>();
		Mission mission = Mission.Current;
		Agent main = Agent.Main;
		if (mission == null || main == null || !main.IsActive())
		{
			return targets;
		}
		foreach (Agent agent in sceneCommandFollowers ?? Enumerable.Empty<Agent>())
		{
			if (CanAgentUseSceneGhostMovement(agent))
			{
				TryAddSceneGhostMovementTarget(targets, agent, main.Position);
			}
		}
		foreach (ActiveSceneGuideRequest request in _activeSceneGuideRequests)
		{
			if (request == null || request.ArrivalTriggered)
			{
				continue;
			}
			Agent agent2 = mission.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
			if (CanAgentUseSceneGhostMovement(agent2) && TryGetSceneGuideGhostTarget(request, out var targetPosition))
			{
				TryAddSceneGhostMovementTarget(targets, agent2, targetPosition);
			}
		}
		foreach (ActiveSceneSummonRequest request2 in _activeSceneSummonRequests)
		{
			TryCollectSceneSummonGhostMovementTarget(targets, request2);
		}
		foreach (SceneSummonConversationSession session in _activeSceneSummonConversationSessions)
		{
			TryCollectSceneSummonConversationGhostMovementTargets(targets, session);
		}
		foreach (SceneReturnJob job in _activeSceneReturnJobs)
		{
			TryCollectSceneReturnGhostMovementTarget(targets, job);
		}
		return targets;
	}

	internal bool CanAgentUseSceneGhostMovement(Agent agent)
	{
		return CanAgentParticipateInSceneSpeech(agent) && agent != Agent.Main && !IsAgentHostileToMainAgent(agent);
	}

	internal static bool IsSceneGhostTeleportDisabledNearPlayer(Agent agent)
	{
		Agent main = Agent.Main;
		return agent != null && main != null && main.IsActive() && agent.Position.AsVec2.DistanceSquared(main.Position.AsVec2) <= SCENE_GHOST_PLAYER_TELEPORT_DISABLE_DISTANCE_SQ;
	}

	internal void TryAddSceneGhostMovementTarget(Dictionary<int, Vec3> targets, Agent agent, Vec3 targetPosition)
	{
		if (targets == null || !CanAgentUseSceneGhostMovement(agent))
		{
			return;
		}
		if (targetPosition.LengthSquared < 0.0001f)
		{
			return;
		}
		if (!targets.TryGetValue(agent.Index, out var oldTarget) || agent.Position.DistanceSquared(targetPosition) < agent.Position.DistanceSquared(oldTarget))
		{
			targets[agent.Index] = targetPosition;
		}
	}

	internal bool TryGetSceneGuideGhostTarget(ActiveSceneGuideRequest request, out Vec3 targetPosition)
	{
		targetPosition = Vec3.Zero;
		Location currentLocation = CampaignMission.Current?.Location;
		if (request == null || currentLocation == null || request.TargetSourceLocation == null)
		{
			return false;
		}
		if (request.TargetSourceLocation == currentLocation)
		{
			Agent agent = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return false;
			}
			targetPosition = agent.Position;
			return true;
		}
		Passage passage = request.TargetDoorPassage;
		if (passage == null)
		{
			List<Location> sceneLocationPath = FindSceneLocationPath(currentLocation, request.TargetSourceLocation);
			if (sceneLocationPath == null || sceneLocationPath.Count < 2)
			{
				return false;
			}
			passage = FindCurrentScenePassageToLocation(sceneLocationPath[1]);
		}
		if (passage == null)
		{
			return false;
		}
		targetPosition = GetPassageWaitingPosition(passage);
		return true;
	}

	internal void TryCollectSceneSummonGhostMovementTarget(Dictionary<int, Vec3> targets, ActiveSceneSummonRequest request)
	{
		if (targets == null || request == null || CampaignMission.Current?.Location != request.CurrentLocation)
		{
			return;
		}
		Agent speakerAgent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		switch (request.Stage)
		{
		case SceneSummonStage.MessengerToTarget:
		{
			Agent targetAgent = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
			if (CanAgentParticipateInSceneSpeech(targetAgent))
			{
				TryAddSceneGhostMovementTarget(targets, speakerAgent, targetAgent.Position);
			}
			break;
		}
		case SceneSummonStage.MessengerToDoor:
		{
			Passage passage = request.MessengerDoorPassage ?? FindCurrentScenePassageToLocation(request.PassageHopLocation);
			if (passage != null)
			{
				TryAddSceneGhostMovementTarget(targets, speakerAgent, GetPassageWaitingPosition(passage));
			}
			break;
		}
		case SceneSummonStage.TargetToPlayer:
		{
			Agent targetAgent2 = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
			if (Agent.Main != null && Agent.Main.IsActive())
			{
				TryAddSceneGhostMovementTarget(targets, targetAgent2, Agent.Main.Position);
			}
			break;
		}
		}
	}

	internal void TryCollectSceneSummonConversationGhostMovementTargets(Dictionary<int, Vec3> targets, SceneSummonConversationSession session)
	{
		if (targets == null || session == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return;
		}
		Vec3 targetPosition = Agent.Main.Position;
		if (session.KeepSpeakerNearby)
		{
			TryAddSceneGhostMovementTarget(targets, ResolveAgentForLocationCharacter(session.SpeakerLocationCharacter), targetPosition);
		}
		for (int i = 0; i < session.Participants.Count; i++)
		{
			TryAddSceneGhostMovementTarget(targets, ResolveAgentForLocationCharacter(session.Participants[i]?.LocationCharacter), targetPosition);
		}
	}

	internal void TryCollectSceneReturnGhostMovementTarget(Dictionary<int, Vec3> targets, SceneReturnJob job)
	{
		Location currentLocation = CampaignMission.Current?.Location;
		if (targets == null || job == null || currentLocation == null || job.CurrentLocation != currentLocation)
		{
			return;
		}
		Agent agent = ResolveAgentForLocationCharacter(job.LocationCharacter);
		if (!CanAgentUseSceneGhostMovement(agent))
		{
			return;
		}
		Location originalLocation = job.OriginalLocation ?? currentLocation;
		if (originalLocation == currentLocation)
		{
			if (job.OriginalPosition.HasValue)
			{
				TryAddSceneGhostMovementTarget(targets, agent, job.OriginalPosition.Value);
			}
			return;
		}
		List<Location> sceneLocationPath = FindSceneLocationPath(currentLocation, originalLocation);
		if (sceneLocationPath == null || sceneLocationPath.Count < 2)
		{
			return;
		}
		Passage passage = job.ExitPassage ?? FindCurrentScenePassageToLocation(sceneLocationPath[1]);
		if (passage != null)
		{
			TryAddSceneGhostMovementTarget(targets, agent, GetPassageWaitingPosition(passage));
		}
	}

	internal void ApplySceneGhostMovementIfStuck(Agent agent, Vec3 targetPosition)
	{
		Mission mission = Mission.Current;
		if (!CanAgentUseSceneGhostMovement(agent) || mission?.Scene == null)
		{
			return;
		}
		if (IsSceneGhostTeleportDisabledNearPlayer(agent))
		{
			_sceneGhostWalkStates.Remove(agent.Index);
			return;
		}
		float currentTime = mission.CurrentTime;
		float distanceSquared = agent.Position.DistanceSquared(targetPosition);
		if (distanceSquared < SCENE_GHOST_MIN_TARGET_DISTANCE_SQ)
		{
			_sceneGhostWalkStates.Remove(agent.Index);
			return;
		}
		if (!_sceneGhostWalkStates.TryGetValue(agent.Index, out var state) || state == null)
		{
			_sceneGhostWalkStates[agent.Index] = new SceneGhostWalkState
			{
				LastPosition = agent.Position,
				LastTarget = targetPosition,
				LastDistanceSquared = distanceSquared,
				LastProgressMissionTime = currentTime,
				LastCheckMissionTime = currentTime,
				LastStepMissionTime = -1000f
			};
			return;
		}
		if (state.LastTarget.DistanceSquared(targetPosition) > SCENE_GHOST_TARGET_CHANGE_DISTANCE_SQ)
		{
			state.LastPosition = agent.Position;
			state.LastTarget = targetPosition;
			state.LastDistanceSquared = distanceSquared;
			state.LastProgressMissionTime = currentTime;
			state.LastCheckMissionTime = currentTime;
			return;
		}
		if (currentTime - state.LastCheckMissionTime < SCENE_GHOST_STUCK_CHECK_INTERVAL)
		{
			return;
		}
		float movedSquared = agent.Position.DistanceSquared(state.LastPosition);
		bool madeProgress = movedSquared >= SCENE_GHOST_MIN_PROGRESS_DISTANCE_SQ || distanceSquared < state.LastDistanceSquared - SCENE_GHOST_MIN_PROGRESS_DISTANCE_SQ;
		state.LastCheckMissionTime = currentTime;
		if (madeProgress)
		{
			state.LastPosition = agent.Position;
			state.LastDistanceSquared = distanceSquared;
			state.LastProgressMissionTime = currentTime;
			return;
		}
		if (currentTime - state.LastProgressMissionTime < SCENE_GHOST_STUCK_SECONDS || currentTime - state.LastStepMissionTime < SCENE_GHOST_STEP_COOLDOWN)
		{
			return;
		}
		if (TrySceneGhostStep(agent, targetPosition))
		{
			state.LastStepMissionTime = currentTime;
			state.LastPosition = agent.Position;
			state.LastDistanceSquared = agent.Position.DistanceSquared(targetPosition);
			state.LastProgressMissionTime = currentTime;
			try
			{
				Logger.Log("SceneGhostMove", "step agent=" + agent.Index + " name=" + (agent.Name ?? "") + " target=" + FormatSceneSummonPosition(targetPosition));
			}
			catch
			{
			}
		}
	}

	internal bool TrySceneGhostStep(Agent agent, Vec3 targetPosition)
	{
		var scene = Mission.Current?.Scene;
		if (!CanAgentUseSceneGhostMovement(agent) || IsSceneGhostTeleportDisabledNearPlayer(agent) || scene == null)
		{
			return false;
		}
		try
		{
			Vec2 vec = targetPosition.AsVec2 - agent.Position.AsVec2;
			float length = vec.Normalize();
			if (length <= 0.05f)
			{
				return false;
			}
			float stepDistance = Math.Min(SCENE_GHOST_MAX_STEP_DISTANCE, Math.Max(SCENE_GHOST_STEP_DISTANCE, length * 0.2f));
			if (stepDistance > length)
			{
				stepDistance = length;
			}
			Vec3 position = agent.Position + vec.ToVec3() * stepDistance;
			position.z = scene.GetGroundHeightAtPosition(position, BodyFlags.CommonCollisionExcludeFlags);
			agent.TeleportToPosition(position);
			try
			{
				agent.SetLookToPointOfInterest(targetPosition);
			}
			catch
			{
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static void BuildSceneSummonStandPositions(Agent main, out Vec3 primaryPosition, out Vec3 secondaryPosition)
	{
		Vec3 position = main.Position;
		Vec2 vec = main.LookDirection.AsVec2;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		else
		{
			vec.Normalize();
		}
		Vec2 vec2 = new Vec2(-vec.y, vec.x);
		primaryPosition = position + (vec.ToVec3() * 1.85f) - (vec2.ToVec3() * 0.85f);
		secondaryPosition = position + (vec.ToVec3() * 1.55f) + (vec2.ToVec3() * 0.85f);
	}

	internal static Vec3 BuildSceneSummonEscortPosition(Agent main, int slotIndex, int slotCount)
	{
		Vec3 position = main.Position;
		Vec2 vec = main.LookDirection.AsVec2;
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		else
		{
			vec.Normalize();
		}
		Vec2 vec2 = new Vec2(-vec.y, vec.x);
		int num = Math.Max(1, slotCount);
		float num2 = (num == 1) ? 0f : ((float)slotIndex - (float)(num - 1) * 0.5f);
		float num3 = 1.7f + 0.25f * Math.Min(slotIndex, 3);
		float num4 = 0.85f * num2;
		return position + (vec.ToVec3() * num3) + (vec2.ToVec3() * num4);
	}

	internal static string GetSceneSummonLocationDebugName(Location location)
	{
		string text = (location?.StringId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (location?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "null" : text;
	}

	internal static string GetSceneSummonPassageDebugName(Passage passage)
	{
		if (passage == null)
		{
			return "null";
		}
		string text = "";
		try
		{
			text = (passage.GameEntity.Name ?? "").Trim();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = passage.GetType().Name;
		}
		return text + "->" + GetSceneSummonLocationDebugName(passage.ToLocation);
	}

	internal static string FormatSceneSummonPosition(Vec3? position)
	{
		if (!position.HasValue)
		{
			return "null";
		}
		Vec3 value = position.Value;
		return value.x.ToString("F2") + "," + value.y.ToString("F2") + "," + value.z.ToString("F2");
	}

	internal string GetSceneSummonAgentDebugState(Agent agent)
	{
		if (agent == null)
		{
			return "null";
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			UsableMachine targetUsableMachine = component?.AgentNavigator?.TargetUsableMachine;
			string text = (targetUsableMachine?.GetType().Name ?? "none").Trim();
			if (targetUsableMachine != null)
			{
				try
				{
					string text2 = (targetUsableMachine.GameEntity.Name ?? "").Trim();
					if (!string.IsNullOrWhiteSpace(text2))
					{
						text = text2;
					}
				}
				catch
				{
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "none";
			}
			return "idx=" + agent.Index + " pos=" + FormatSceneSummonPosition(agent.Position) + " using=" + agent.IsUsingGameObject + " curUse=" + (agent.CurrentlyUsedGameObject?.GetType().Name ?? "none") + " nav=" + text;
		}
		catch (Exception ex)
		{
			return "idx=" + agent.Index + " debug_err=" + ex.GetType().Name;
		}
	}

	internal void LogSceneSummonState(string phase, ActiveSceneSummonRequest request, Agent speakerAgent = null, Agent targetAgent = null, string extra = null, bool force = false)
	{
		return;
	}

	internal static SceneSummonPromptTarget ResolveSceneSummonPromptTargetByToken(
		IEnumerable<SceneSummonPromptTarget> summonTargets,
		IEnumerable<SceneGuidePromptTarget> guideTargets,
		string token)
    {
        var selected=ConversationActionPostprocessOwner.ResolvePostprocessSummonTargetByToken(CapturePostprocessSummonTargets(summonTargets), CapturePostprocessGuideTargets(guideTargets), token);
        if (selected == null) return null;
        var direct=(summonTargets ?? Enumerable.Empty<SceneSummonPromptTarget>()).FirstOrDefault(x => x != null && x.PromptId == selected.PromptId && x.DisplayName == selected.DisplayName);
        if (direct != null) return direct;
        var guide=(guideTargets ?? Enumerable.Empty<SceneGuidePromptTarget>()).FirstOrDefault(x => x != null && x.PromptId == selected.PromptId && x.DisplayName == selected.DisplayName);
        return guide == null ? null : new SceneSummonPromptTarget { PromptId=guide.PromptId,DisplayName=guide.DisplayName,LocationCode=guide.LocationCode,LocationCharacter=guide.LocationCharacter,SourceLocation=guide.SourceLocation };
    }

	internal bool TryTriggerSceneSummonAction(NpcDataPacket npc, Agent agent, List<SceneSummonPromptTarget> summonTargets, List<SceneGuidePromptTarget> guideTargets, ref string content, out ActiveSceneSummonRequest preparedRequest)
	{
		preparedRequest = null;
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		MatchCollection matchCollection = SceneSummonActionTagRegex.Matches(content);
		if (matchCollection == null || matchCollection.Count == 0)
		{
			return false;
		}
		if (IsPrisonBreakRescueMissionActive())
		{
			content = SceneSummonActionTagRegex.Replace(content, "").Trim();
			Logger.Log("SceneFollow", "prison_break_unsupported_scene_summon_ignored agent=" + (agent?.Index ?? -1));
			return false;
		}
		content = SceneSummonActionTagRegex.Replace(content, "").Trim();
		HashSet<LocationCharacter> hashSet = new HashSet<LocationCharacter>();
		List<SceneSummonPromptTarget> list = new List<SceneSummonPromptTarget>();
		foreach (Match item2 in matchCollection)
		{
			if (item2 == null || !item2.Success)
			{
				continue;
			}
			List<string> list2 = ParseSceneMechanismTargetTokens(item2.Groups[1].Value);
			if (list2 == null || list2.Count == 0)
			{
				continue;
			}
			foreach (string item in list2)
			{
				SceneSummonPromptTarget sceneSummonPromptTarget = ResolveSceneSummonPromptTargetByToken(summonTargets, guideTargets, item);
				if (sceneSummonPromptTarget != null && sceneSummonPromptTarget.LocationCharacter != null && hashSet.Add(sceneSummonPromptTarget.LocationCharacter))
				{
					list.Add(sceneSummonPromptTarget);
				}
			}
		}
		if (list == null || list.Count == 0)
		{
			Logger.Log(
				"SceneSummon",
				"tag_target_unresolved agent=" + (agent?.Index ?? npc?.AgentIndex ?? -1)
				+ " summonCandidates=" + (summonTargets?.Count ?? 0)
				+ " guideCandidates=" + (guideTargets?.Count ?? 0));
			return false;
		}
		Logger.Log(
			"SceneSummon",
			"tag_targets_resolved agent=" + (agent?.Index ?? npc?.AgentIndex ?? -1)
			+ " targets=" + string.Join(",", list.Select((SceneSummonPromptTarget x) => x?.DisplayName ?? ""))
			+ " summonCandidates=" + (summonTargets?.Count ?? 0)
			+ " guideCandidates=" + (guideTargets?.Count ?? 0));
		return StartSceneSummonBatchAction(npc, agent, list, out preparedRequest);
	}

	internal bool StartSceneSummonBatchAction(NpcDataPacket npc, Agent agent, List<SceneSummonPromptTarget> targets, out ActiveSceneSummonRequest preparedRequest)
	{
        EnsureCurrentMission();
		preparedRequest = null;
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		LocationCharacter locationCharacter = locationComplex?.FindCharacter(agent);
		if (npc == null || agent == null || !agent.IsActive() || locationComplex == null || location == null || locationCharacter == null || targets == null || targets.Count == 0)
		{
			return false;
		}
		Location originalSpeakerLocation = location;
		Vec3? originalSpeakerPosition = agent.Position;
		bool flag = IsAgentFollowingPlayerBySceneCommand(agent);
		if (_sceneFollowReturnStates.TryGetValue(agent.Index, out var value) && value != null)
		{
			originalSpeakerLocation = value.OriginalLocation ?? originalSpeakerLocation;
			originalSpeakerPosition = value.OriginalPosition ?? originalSpeakerPosition;
			_pendingSceneFollowCommands.Remove(agent.Index);
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
			_sceneFollowReturnStates.Remove(agent.Index);
		}
		else if (flag)
		{
			_pendingSceneFollowCommands.Remove(agent.Index);
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
		}
		if (TryDetachSceneSummonConversationAgentForRelay(agent, out var relayOriginalLocation, out var relayOriginalPosition))
		{
			originalSpeakerLocation = relayOriginalLocation ?? originalSpeakerLocation;
			originalSpeakerPosition = relayOriginalPosition ?? originalSpeakerPosition;
		}
		else
		{
			SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(npc.AgentIndex);
			if (sceneSummonConversationSession != null)
			{
				BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
		}
		CancelSceneReturnJob(locationCharacter);
		CancelSceneGuideActionForAgent(npc.AgentIndex);
		CancelSceneSummonActionForAgent(npc.AgentIndex, locationCharacter);
		SceneSummonBatchState sceneSummonBatchState = new SceneSummonBatchState
		{
			BatchId = _nextSceneSummonBatchId++,
			SpeakerAgentIndex = npc.AgentIndex,
			SpeakerName = (string.IsNullOrWhiteSpace(npc.Name) ? (agent.Name?.ToString() ?? "有人") : npc.Name),
			SpeakerLocationCharacter = locationCharacter,
			OriginalSpeakerLocation = originalSpeakerLocation,
			OriginalSpeakerPosition = originalSpeakerPosition
		};
		foreach (SceneSummonPromptTarget target in targets)
		{
			if (target != null)
			{
				sceneSummonBatchState.PendingTargets.Enqueue(target);
			}
		}
		if (sceneSummonBatchState.PendingTargets.Count == 0)
		{
			return false;
		}
		_activeSceneSummonBatches[sceneSummonBatchState.BatchId] = sceneSummonBatchState;
		if (!TryStartNextSceneSummonBatchRequest(sceneSummonBatchState, agent, isInitialRequest: true, out preparedRequest))
		{
			_activeSceneSummonBatches.Remove(sceneSummonBatchState.BatchId);
			return false;
		}
		return true;
	}

	internal bool TryTriggerSceneGuideAction(NpcDataPacket npc, Agent agent, List<SceneGuidePromptTarget> guideTargets, List<SceneSummonPromptTarget> summonTargets, ref string content, out ActiveSceneGuideRequest preparedRequest)
	{
		preparedRequest = null;
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		Match match = SceneGuideActionTagRegex.Match(content);
		if (!match.Success)
		{
			return false;
		}
		if (IsPrisonBreakRescueMissionActive())
		{
			content = SceneGuideActionTagRegex.Replace(content, "").Trim();
			Logger.Log("SceneFollow", "prison_break_unsupported_scene_guide_ignored agent=" + (agent?.Index ?? -1));
			return false;
		}
		content = SceneGuideActionTagRegex.Replace(content, "").Trim();
		string text = NormalizeSceneMechanismTargetToken(match.Groups[1].Value);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		SceneGuidePromptTarget sceneGuidePromptTarget = ResolveSceneMechanismTargetByToken(guideTargets, text, (SceneGuidePromptTarget x) => x.PromptId, (SceneGuidePromptTarget x) => x.DisplayName);
		if (sceneGuidePromptTarget != null)
		{
			return StartSceneGuideAction(npc, agent, sceneGuidePromptTarget, out preparedRequest);
		}
		SceneSummonPromptTarget sceneSummonPromptTarget = ResolveSceneMechanismTargetByToken(summonTargets, text, (SceneSummonPromptTarget x) => x.PromptId, (SceneSummonPromptTarget x) => x.DisplayName);
		if (sceneSummonPromptTarget == null)
		{
			return false;
		}
		SceneGuidePromptTarget target = new SceneGuidePromptTarget
		{
			PromptId = sceneSummonPromptTarget.PromptId,
			DisplayName = sceneSummonPromptTarget.DisplayName,
			LocationCode = sceneSummonPromptTarget.LocationCode,
			LocationCharacter = sceneSummonPromptTarget.LocationCharacter,
			SourceLocation = sceneSummonPromptTarget.SourceLocation
		};
		return StartSceneGuideAction(npc, agent, target, out preparedRequest);
	}

	internal bool StartSceneGuideAction(NpcDataPacket npc, Agent agent, SceneGuidePromptTarget target, out ActiveSceneGuideRequest preparedRequest)
	{
        EnsureCurrentMission();
		preparedRequest = null;
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		LocationCharacter locationCharacter = locationComplex?.FindCharacter(agent);
		if (npc == null || agent == null || !agent.IsActive() || target == null || target.LocationCharacter == null || locationComplex == null || location == null || locationCharacter == null)
		{
			return false;
		}
		Location originalGuideLocation = location;
		Vec3? originalGuidePosition = agent.Position;
		bool flag = IsAgentFollowingPlayerBySceneCommand(agent);
		if (_sceneFollowReturnStates.TryGetValue(agent.Index, out var value) && value != null)
		{
			originalGuideLocation = value.OriginalLocation ?? originalGuideLocation;
			originalGuidePosition = value.OriginalPosition ?? originalGuidePosition;
			_pendingSceneFollowCommands.Remove(agent.Index);
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
			_sceneFollowReturnStates.Remove(agent.Index);
		}
		else if (flag)
		{
			_pendingSceneFollowCommands.Remove(agent.Index);
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
		}
		if (TryDetachSceneSummonConversationAgentForRelay(agent, out var relayOriginalLocation, out var relayOriginalPosition))
		{
			originalGuideLocation = relayOriginalLocation ?? originalGuideLocation;
			originalGuidePosition = relayOriginalPosition ?? originalGuidePosition;
		}
		else
		{
			SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(npc.AgentIndex);
			if (sceneSummonConversationSession != null)
			{
				BeginSceneSummonConversationReturn(sceneSummonConversationSession);
			}
		}
		CancelSceneReturnJob(locationCharacter);
		CancelSceneGuideActionForAgent(npc.AgentIndex);
		CancelSceneSummonActionForAgent(npc.AgentIndex, locationCharacter);
		preparedRequest = new ActiveSceneGuideRequest
		{
			GuideAgentIndex = npc.AgentIndex,
			GuideName = (string.IsNullOrWhiteSpace(npc.Name) ? (agent.Name?.ToString() ?? "有人") : npc.Name),
			TargetName = target.DisplayName,
			TargetPromptId = target.PromptId,
			GuideLocationCharacter = locationCharacter,
			TargetLocationCharacter = target.LocationCharacter,
			CurrentLocation = location,
			OriginalGuideLocation = originalGuideLocation,
			OriginalGuidePosition = originalGuidePosition,
			TargetSourceLocation = target.SourceLocation,
			NextStageMissionTime = Mission.Current?.CurrentTime ?? 0f
		};
		_activeSceneGuideRequests.Add(preparedRequest);
		return true;
	}

	internal void CancelSceneGuideActionForAgent(int guideAgentIndex)
	{
		if (guideAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == guideAgentIndex);
		if (agent != null)
		{
			StopSceneGuideEscort(agent);
		}
		foreach (ActiveSceneGuideRequest item in _activeSceneGuideRequests.Where((ActiveSceneGuideRequest x) => x == null || x.GuideAgentIndex == guideAgentIndex).ToList())
		{
			if (item != null) item.Cancelled = true;
			CleanupSceneGuideDoorProxyAgent(item);
		}
		_activeSceneGuideRequests.RemoveAll((ActiveSceneGuideRequest x) => x == null || x.GuideAgentIndex == guideAgentIndex);
		lock (_launchGate)
		{
			_pendingSceneGuideLaunchQueues.Remove(guideAgentIndex);
		}
		_pendingSceneGuideReturnsAfterSpeech.Remove(guideAgentIndex);
		ReleaseSceneGuideArrivalHold(guideAgentIndex, restoreAutonomy: true);
	}

	internal void CancelSceneSummonActionForAgent(int agentIndex, LocationCharacter locationCharacter = null)
	{
		if (agentIndex < 0)
		{
			return;
		}
		foreach (ActiveSceneSummonRequest item in _activeSceneSummonRequests.Where((ActiveSceneSummonRequest x) => x == null || x.SpeakerAgentIndex == agentIndex || (locationCharacter != null && x.TargetLocationCharacter == locationCharacter)).ToList())
		{
			CleanupSceneSummonDoorProxyAgent(item);
		}
		_activeSceneSummonRequests.RemoveAll((ActiveSceneSummonRequest x) => x == null || x.SpeakerAgentIndex == agentIndex || (locationCharacter != null && x.TargetLocationCharacter == locationCharacter));
		foreach (int item2 in _activeSceneSummonBatches.Values.Where((SceneSummonBatchState x) => x != null && x.SpeakerAgentIndex == agentIndex).Select((SceneSummonBatchState x) => x.BatchId).ToList())
		{
			CancelSceneSummonBatch(item2);
		}
		lock (_launchGate)
		{
			_pendingSceneSummonLaunchQueues.Remove(agentIndex);
		}
		_pendingSceneSummonReturnsAfterSpeech.Remove(agentIndex);
	}

	internal static EscortAgentBehavior GetSceneGuideEscortBehavior(Agent agent)
	{
		try
		{
			AgentNavigator agentNavigator = agent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			InterruptingBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			return behaviorGroup?.GetBehavior<EscortAgentBehavior>();
		}
		catch
		{
			return null;
		}
	}

	internal static bool TryStartEscortBehavior(Agent ownerAgent, UsableMachine targetMachine, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached = null)
	{
		try
		{
			AgentNavigator agentNavigator = ownerAgent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			InterruptingBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			if (behaviorGroup == null || ownerAgent == null || Agent.Main == null || !Agent.Main.IsActive() || targetMachine == null)
			{
				return false;
			}
			EscortAgentBehavior escortAgentBehavior = behaviorGroup.GetBehavior<EscortAgentBehavior>() ?? behaviorGroup.AddBehavior<EscortAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<EscortAgentBehavior>();
			escortAgentBehavior.Initialize(Agent.Main, targetMachine, onTargetReached);
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryStartEscortBehavior(Agent ownerAgent, Agent targetAgent, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached = null)
	{
		try
		{
			AgentNavigator agentNavigator = ownerAgent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			InterruptingBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			if (behaviorGroup == null || ownerAgent == null || Agent.Main == null || !Agent.Main.IsActive() || targetAgent == null || !targetAgent.IsActive())
			{
				return false;
			}
			EscortAgentBehavior escortAgentBehavior = behaviorGroup.GetBehavior<EscortAgentBehavior>() ?? behaviorGroup.AddBehavior<EscortAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<EscortAgentBehavior>();
			escortAgentBehavior.Initialize(Agent.Main, targetAgent, onTargetReached);
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryStartEscortBehavior(Agent ownerAgent, Vec3 targetPosition, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached = null)
	{
		try
		{
			AgentNavigator agentNavigator = ownerAgent?.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
			InterruptingBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			if (behaviorGroup == null || ownerAgent == null || Agent.Main == null || !Agent.Main.IsActive())
			{
				return false;
			}
			var scene = Mission.Current?.Scene;
			if (scene != null)
			{
				targetPosition.z = scene.GetGroundHeightAtPosition(targetPosition, BodyFlags.CommonCollisionExcludeFlags);
			}
			EscortAgentBehavior escortAgentBehavior = behaviorGroup.GetBehavior<EscortAgentBehavior>() ?? behaviorGroup.AddBehavior<EscortAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<EscortAgentBehavior>();
			escortAgentBehavior.Initialize(Agent.Main, targetPosition, onTargetReached);
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal EscortAgentBehavior.OnTargetReachedDelegate BuildSceneGuideArrivalCallback(ActiveSceneGuideRequest request, bool reachedDoor)
	{
        long generation = _generation;
        Mission mission = Mission.Current;
		return delegate
        {
            if (generation != _generation || !ReferenceEquals(mission, Mission.Current) || request == null || request.Cancelled) return false;
			try
			{
				CompleteSceneGuideArrival(request, reachedDoor, "escort_callback");
			}
			catch (Exception ex)
			{
				try
				{
					Logger.Log("SceneGuide", "arrival_callback_failed guide=" + (request?.GuideName ?? "") + " target=" + (request?.TargetName ?? "") + " error=" + ex.Message);
				}
				catch
				{
				}
			}
			return false;
		};
	}

	internal bool CompleteSceneGuideArrival(ActiveSceneGuideRequest request, bool reachedDoor, string reason)
	{
		if (request == null || request.Cancelled || request.GuideAgentIndex < 0 || request.ArrivalTriggered)
		{
			return false;
		}
		request.ArrivalTriggered = true;
		request.ArrivalReachedDoor = reachedDoor;
		request.NextStageMissionTime = float.MaxValue;
		CleanupSceneGuideDoorProxyAgent(request);
		try
		{
			Logger.Log("SceneGuide", "arrival_complete reason=" + (reason ?? "") + " guide=" + (request.GuideName ?? "") + " target=" + (request.TargetName ?? "") + " reachedDoor=" + reachedDoor);
		}
		catch
		{
		}
		TriggerSceneGuideArrivalAndReturn(request, reachedDoor);
		return true;
	}

	internal bool TryStartSceneGuideEscort(ActiveSceneGuideRequest request, Agent guideAgent)
	{
		if (request == null || !CanAgentParticipateInSceneSpeech(guideAgent) || Agent.Main == null || !Agent.Main.IsActive())
		{
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(guideAgent);
			Location currentLocation = CampaignMission.Current?.Location;
			if (currentLocation == null || request.TargetSourceLocation == null)
			{
				return false;
			}
			if (request.TargetSourceLocation == currentLocation)
			{
				Agent agent = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
				if (!CanAgentParticipateInSceneSpeech(agent))
				{
					return false;
				}
				// 带路必须绑定实时目标 Agent，严禁改回 Vec3 targetPosition。
				// 原版 EscortAgentBehavior 对固定坐标目标不会稳定执行带路，容易只记录死坐标或直接停止。
				bool started = TryStartEscortBehavior(guideAgent, agent, BuildSceneGuideArrivalCallback(request, reachedDoor: false));
				try
				{
					Logger.Log("SceneGuide", "escort_started_same_scene_position guide=" + (request.GuideName ?? "") + " target=" + (request.TargetName ?? "") + " ok=" + started + " pos=" + FormatSceneSummonPosition(agent.Position));
				}
				catch
				{
				}
				return started;
			}
			List<Location> sceneLocationPath = FindSceneLocationPath(currentLocation, request.TargetSourceLocation);
			if (sceneLocationPath == null || sceneLocationPath.Count < 2)
			{
				return false;
			}
			Passage currentScenePassageToLocation = request.TargetDoorPassage ?? FindCurrentScenePassageToLocation(sceneLocationPath[1]);
			if (currentScenePassageToLocation == null)
			{
				return false;
			}
			request.TargetDoorPassage = currentScenePassageToLocation;
			Agent agent2 = EnsureSceneGuideDoorProxyAgent(request, currentScenePassageToLocation, guideAgent);
			if (agent2 != null && agent2.IsActive())
			{
				bool started2 = TryStartEscortBehavior(guideAgent, agent2, BuildSceneGuideArrivalCallback(request, reachedDoor: true));
				try
				{
					Logger.Log("SceneGuide", "escort_started_door_proxy guide=" + (request.GuideName ?? "") + " target=" + (request.TargetName ?? "") + " ok=" + started2);
				}
				catch
				{
				}
				return started2;
			}
			bool flag = NavigateAgentToScenePassage(guideAgent, currentScenePassageToLocation);
			try
			{
				Logger.Log("SceneGuide", "guide_started_passage_navigation guide=" + (request.GuideName ?? "") + " target=" + (request.TargetName ?? "") + " ok=" + flag);
			}
			catch
			{
			}
			return flag;
		}
		catch
		{
			return false;
		}
	}

	internal bool IsSceneGuideDoorReached(ActiveSceneGuideRequest request, Agent guideAgent, Agent main)
	{
		if (request == null || !CanAgentParticipateInSceneSpeech(guideAgent) || main == null || !main.IsActive() || request.TargetDoorPassage == null)
		{
			return false;
		}
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(request.TargetDoorPassage);
		bool flag = guideAgent.Position.DistanceSquared(passageWaitingPosition) <= SCENE_GUIDE_TARGET_REACHED_DISTANCE_SQ;
		bool flag2 = main.Position.DistanceSquared(passageWaitingPosition) <= SCENE_GUIDE_TARGET_REACHED_DISTANCE_SQ;
		return flag && flag2;
	}

	internal static void StopSceneGuideEscort(Agent agent)
	{
		try
		{
			if (agent != null && agent.IsActive())
			{
				EscortAgentBehavior.RemoveEscortBehaviorOfAgent(agent);
			}
		}
		catch
		{
		}
		ClearSceneGuideEscortMovement(agent);
	}

	internal static void ClearSceneGuideEscortMovement(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			(component?.AgentNavigator ?? component?.CreateAgentNavigator())?.ClearTarget();
		}
		catch
		{
		}
		try
		{
			agent.ClearTargetFrame();
		}
		catch
		{
		}
		try
		{
			agent.DisableScriptedMovement();
		}
		catch
		{
		}
	}

	internal void HoldSceneGuideAgentForArrivalSpeech(Agent agent)
	{
		Mission mission = Mission.Current;
		if (!CanAgentParticipateInSceneSpeech(agent) || mission == null)
		{
			return;
		}
		StopSceneGuideEscort(agent);
		SceneGuideArrivalHold hold = new SceneGuideArrivalHold
		{
			AgentIndex = agent.Index,
			AnchorPosition = agent.Position,
			ExpiresAtMissionTime = mission.CurrentTime + SCENE_GUIDE_ARRIVAL_HOLD_FAILSAFE_SECONDS
		};
		_sceneGuideArrivalHolds[agent.Index] = hold;
		try
		{
			Logger.Log("SceneGuide", "arrival_hold_start agent=" + agent.Index + " pos=" + FormatSceneSummonPosition(hold.AnchorPosition));
		}
		catch
		{
		}
		ApplySceneGuideArrivalPlayerMessageLock(agent);
		ApplySceneGuideArrivalHold(agent, hold);
	}

	internal void ApplySceneGuideArrivalPlayerMessageLock(Agent agent)
	{
		if (!CanAgentParticipateInSceneSpeech(agent) || Mission.Current == null)
		{
			return;
		}
		NpcDataPacket npcDataPacket = ShoutUtils.ExtractNpcData(agent);
		if (npcDataPacket != null)
		{
			_ports.TrackPlayerInteraction(npcDataPacket, 1, ACTIVE_INTERACTION_IDLE_TIMEOUT);
		}
		_ports.TryInterruptAgentSceneUseForStare(agent);
		try
		{
			Logger.Log("SceneGuide", "arrival_player_message_lock agent=" + agent.Index + " softHold=true");
		}
		catch
		{
		}
	}

	internal void ApplySceneGuideArrivalHold(Agent agent, SceneGuideArrivalHold hold)
	{
		if (!CanAgentParticipateInSceneSpeech(agent) || hold == null || Mission.Current == null)
		{
			return;
		}
		try
		{
			EscortAgentBehavior.RemoveEscortBehaviorOfAgent(agent);
		}
		catch
		{
		}
		ClearSceneGuideEscortMovement(agent);
		try
		{
			agent.SetLookAgent(null);
			Vec2 zero = Vec2.Zero;
			agent.SetMovementDirection(in zero);
			agent.MovementInputVector = Vec2.Zero;
			agent.MovementFlags = Agent.MovementControlFlag.None;
			agent.SetTargetPosition(agent.Position.AsVec2);
			agent.SetMaximumSpeedLimit(0f, isMultiplier: false);
		}
		catch
		{
		}
		try
		{
			// Arrival hold must pause vanilla daily AI, but must not write a scripted target frame.
			agent.SetIsAIPaused(isPaused: true);
		}
		catch
		{
		}
	}

	internal void ClearSceneGuideArrivalHoldMotion(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		_ports.ClearAgentSceneConversationFocus(agent);
		try
		{
			agent.SetIsAIPaused(isPaused: false);
		}
		catch
		{
		}
		if (!ShouldPreserveMeetingSceneAutonomy())
		{
			ClearSceneGuideEscortMovement(agent);
		}
	}

	internal void ReleaseSceneGuideArrivalHold(int agentIndex, bool restoreAutonomy)
	{
		if (agentIndex < 0)
		{
			return;
		}
		bool flag = _sceneGuideArrivalHolds.ContainsKey(agentIndex);
		_sceneGuideArrivalHolds.Remove(agentIndex);
		_ports.RemoveInteractionSession(agentIndex);
		_ports.CancelInteractionTimeoutArm(agentIndex);
		_ports.DetachStareAgent(agentIndex);
		try
		{
			Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex && a.IsActive());
			if (_ports.ReleaseUseConversationSlot(agentIndex) && agent?.CurrentlyUsedGameObject != null)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationEnd();
			}
			ClearSceneGuideArrivalHoldMotion(agent);
			if (restoreAutonomy && CanAgentParticipateInSceneSpeech(agent))
			{
				_ports.RestoreAgentAutonomy(agent);
			}
		}
		catch
		{
		}
		_ports.ClearEmptyStareDeadline();
		_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agentIndex });
		if (flag)
		{
			try
			{
				Logger.Log("SceneGuide", "arrival_hold_release agent=" + agentIndex + " restoreAutonomy=" + restoreAutonomy);
			}
			catch
			{
			}
		}
	}

	internal void UpdateSceneGuideArrivalHolds()
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (agents == null || _sceneGuideArrivalHolds.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, SceneGuideArrivalHold> sceneGuideArrivalHold in _sceneGuideArrivalHolds)
		{
			SceneGuideArrivalHold value = sceneGuideArrivalHold.Value;
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == sceneGuideArrivalHold.Key && a.IsActive());
			if (value == null || !CanAgentParticipateInSceneSpeech(agent) || currentTime >= value.ExpiresAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(sceneGuideArrivalHold.Key);
				continue;
			}
			ApplySceneGuideArrivalHold(agent, value);
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			if (_pendingSceneGuideReturnsAfterSpeech.TryGetValue(item, out var value2) && value2 != null)
			{
				_pendingSceneGuideReturnsAfterSpeech.Remove(item);
				ReleaseSceneGuideArrivalHold(item, restoreAutonomy: false);
				if (value2.LocationCharacter != null)
				{
					QueueSceneReturnJob(value2.DisplayName, value2.LocationCharacter, value2.OriginalLocation, value2.OriginalPosition);
				}
			}
			else
			{
				ReleaseSceneGuideArrivalHold(item, restoreAutonomy: true);
			}
		}
	}

	internal bool TryStartNextSceneSummonBatchRequest(SceneSummonBatchState batch, Agent fallbackSpeakerAgent, bool isInitialRequest, out ActiveSceneSummonRequest preparedRequest)
	{
		preparedRequest = null;
		if (batch == null || batch.PendingTargets.Count == 0)
		{
			return false;
		}
		Agent agent = ResolveAgentForLocationCharacter(batch.SpeakerLocationCharacter) ?? fallbackSpeakerAgent;
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return false;
		}
		while (batch.PendingTargets.Count > 0)
		{
			SceneSummonPromptTarget target = batch.PendingTargets.Dequeue();
			bool keepMessengerWithTarget = batch.PendingTargets.Count == 0;
			preparedRequest = BuildSceneSummonRequest(batch, agent, target, keepMessengerWithTarget, isInitialRequest);
			if (preparedRequest == null)
			{
				continue;
			}
			LocationCharacter preparedTargetLocationCharacter = preparedRequest.TargetLocationCharacter;
			foreach (ActiveSceneSummonRequest item in _activeSceneSummonRequests.Where((ActiveSceneSummonRequest x) => x == null || (x.BatchId == batch.BatchId && x.TargetLocationCharacter == preparedTargetLocationCharacter)).ToList())
			{
				CleanupSceneSummonDoorProxyAgent(item);
			}
			_activeSceneSummonRequests.RemoveAll((ActiveSceneSummonRequest x) => x == null || (x.BatchId == batch.BatchId && x.TargetLocationCharacter == preparedTargetLocationCharacter));
			_activeSceneSummonRequests.Add(preparedRequest);
			PrimeSceneSummonArrivalSpeechAsync(preparedRequest);
			return true;
		}
		return false;
	}

	internal ActiveSceneSummonRequest BuildSceneSummonRequest(SceneSummonBatchState batch, Agent speakerAgent, SceneSummonPromptTarget target, bool keepMessengerWithTarget, bool isInitialRequest)
	{
		Mission mission = Mission.Current;
		LocationComplex locationComplex = LocationComplex.Current;
		Location location = CampaignMission.Current?.Location;
		if (batch == null || speakerAgent == null || !speakerAgent.IsActive() || target == null || target.LocationCharacter == null || locationComplex == null || location == null || mission == null)
		{
			return null;
		}
		Location locationOfCharacter = locationComplex.GetLocationOfCharacter(target.LocationCharacter) ?? target.SourceLocation;
		if (locationOfCharacter == null || target.LocationCharacter.Character == null || target.LocationCharacter.Character == CharacterObject.PlayerCharacter || target.LocationCharacter.Character == speakerAgent.Character)
		{
			return null;
		}
		List<Location> sceneLocationPath = FindSceneLocationPath(location, locationOfCharacter);
		if (sceneLocationPath == null || sceneLocationPath.Count == 0)
		{
			return null;
		}
		string text = string.IsNullOrWhiteSpace(target.DisplayName) ? (target.LocationCharacter.Character.Name?.ToString() ?? "那个人") : target.DisplayName;
		string text2 = BuildSceneSummonBatchTargetSummary(batch, target);
		LocationCharacter locationCharacter = batch.SpeakerLocationCharacter ?? locationComplex.FindCharacter(speakerAgent);
		if (locationCharacter == null)
		{
			return null;
		}
		if (locationOfCharacter == location)
		{
			Agent agent = ResolveAgentForLocationCharacter(target.LocationCharacter);
			if (!CanAgentParticipateInSceneSpeech(agent))
			{
				return null;
			}
			ActiveSceneSummonRequest activeSceneSummonRequest = new ActiveSceneSummonRequest
			{
				BatchId = batch.BatchId,
				SpeakerAgentIndex = batch.SpeakerAgentIndex,
				SpeakerName = batch.SpeakerName,
				TargetPromptId = target.PromptId,
				TargetName = text,
				SpeakerLocationCharacter = locationCharacter,
				TargetLocationCharacter = target.LocationCharacter,
				CurrentLocation = location,
				OriginalSpeakerLocation = batch.OriginalSpeakerLocation ?? location,
				OriginalTargetLocation = locationOfCharacter,
				TargetSourceLocation = locationOfCharacter,
				PassageHopLocation = location,
				OriginalSpeakerPosition = batch.OriginalSpeakerPosition ?? speakerAgent.Position,
				OriginalTargetPosition = agent.Position,
				NextStageMissionTime = mission.CurrentTime,
				ArrivalSpeechDeadlineMissionTime = mission.CurrentTime + 6f,
				Stage = SceneSummonStage.PendingLaunch,
				PendingLaunchStage = SceneSummonStage.MessengerToTarget,
				LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去叫" + text2 + "了。") : null),
				KeepMessengerWithTarget = keepMessengerWithTarget
			};
			LogSceneSummonState(isInitialRequest ? "start_same_scene" : "start_same_scene_followup", activeSceneSummonRequest, speakerAgent, agent, "pathLen=1 keepMessenger=" + keepMessengerWithTarget, force: true);
			if (!isInitialRequest)
			{
				activeSceneSummonRequest.NextStageMissionTime = mission.CurrentTime + 0.25f;
			}
			return activeSceneSummonRequest;
		}
		Location location2 = sceneLocationPath[1];
		Passage currentScenePassageToLocation = FindCurrentScenePassageToLocation(location2);
		if (currentScenePassageToLocation == null)
		{
			return null;
		}
		ActiveSceneSummonRequest activeSceneSummonRequest2 = new ActiveSceneSummonRequest
		{
			BatchId = batch.BatchId,
			SpeakerAgentIndex = batch.SpeakerAgentIndex,
			SpeakerName = batch.SpeakerName,
			TargetPromptId = target.PromptId,
			TargetName = text,
			SpeakerLocationCharacter = locationCharacter,
			TargetLocationCharacter = target.LocationCharacter,
			CurrentLocation = location,
			OriginalSpeakerLocation = batch.OriginalSpeakerLocation ?? location,
			OriginalTargetLocation = locationOfCharacter,
			TargetSourceLocation = locationOfCharacter,
			PassageHopLocation = location2,
			OriginalSpeakerPosition = batch.OriginalSpeakerPosition ?? speakerAgent.Position,
			OriginalTargetPosition = null,
			MessengerDoorPassage = currentScenePassageToLocation,
			NextStageMissionTime = mission.CurrentTime + (isInitialRequest ? SCENE_SUMMON_DELAY_SECONDS : 0.25f),
			ArrivalSpeechDeadlineMissionTime = mission.CurrentTime + 8f,
			Stage = SceneSummonStage.PendingLaunch,
			PendingLaunchStage = SceneSummonStage.MessengerToDoor,
			LaunchAnnouncement = (isInitialRequest ? (batch.SpeakerName + " 去帮你叫" + text2 + "了。") : null),
			KeepMessengerWithTarget = keepMessengerWithTarget
		};
		LogSceneSummonState(isInitialRequest ? "start_cross_scene" : "start_cross_scene_followup", activeSceneSummonRequest2, speakerAgent, null, "pathLen=" + sceneLocationPath.Count + " keepMessenger=" + keepMessengerWithTarget, force: true);
		return activeSceneSummonRequest2;
	}

	internal void UpdateActiveSceneSummonRequests()
	{
		Mission mission = Mission.Current;
		if (_activeSceneSummonRequests.Count == 0 || mission == null)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		for (int num = _activeSceneSummonRequests.Count - 1; num >= 0; num--)
		{
			ActiveSceneSummonRequest activeSceneSummonRequest = _activeSceneSummonRequests[num];
			if (!IsSceneSummonRequestStillValid(activeSceneSummonRequest))
			{
				LogSceneSummonState("request_invalidated", activeSceneSummonRequest, ResolveAgentForLocationCharacter(activeSceneSummonRequest.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(activeSceneSummonRequest.TargetLocationCharacter), "campaignLocationChangedOrTargetMissing", force: true);
				CleanupSceneSummonDoorProxyAgent(activeSceneSummonRequest);
				if (activeSceneSummonRequest.BatchId >= 0)
				{
					CancelSceneSummonBatch(activeSceneSummonRequest.BatchId);
				}
				else
				{
					_activeSceneSummonRequests.RemoveAt(num);
				}
				continue;
			}
			bool flag = activeSceneSummonRequest.Stage switch
			{
				SceneSummonStage.PendingLaunch => TickSceneSummonPendingLaunchStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.MessengerToTarget => TickSceneSummonMessengerToTargetStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.MessengerToDoor => TickSceneSummonMessengerStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.WaitingForTarget => TickSceneSummonWaitStage(activeSceneSummonRequest, currentTime),
				SceneSummonStage.TargetToPlayer => TickSceneSummonTargetStage(activeSceneSummonRequest, currentTime),
				_ => true,
			};
			if (flag)
			{
				CleanupSceneSummonDoorProxyAgent(activeSceneSummonRequest);
				_activeSceneSummonRequests.RemoveAt(num);
			}
		}
	}

	internal static bool IsSceneSummonRequestStillValid(ActiveSceneSummonRequest request)
	{
		return request != null && request.TargetLocationCharacter != null && CampaignMission.Current?.Location == request.CurrentLocation;
	}

	internal void SchedulePreparedSceneSummonLaunch(ActiveSceneSummonRequest request, SceneSpeechPlaybackInfo playbackInfo, string spokenText)
	{
		Mission mission = Mission.Current;
		if (request == null || mission == null)
		{
			return;
		}
		if (playbackInfo != null && playbackInfo.TtsEnabled && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished && request.SpeakerAgentIndex >= 0)
		{
			request.NextStageMissionTime = float.MaxValue;
			EnqueuePendingSceneSummonLaunch(request.SpeakerAgentIndex, request);
			LogSceneSummonState("pending_launch_wait_tts_finish", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "speakerAgentIndex=" + request.SpeakerAgentIndex, force: true);
			return;
		}
		float num = EstimateBubbleTypingDurationSeconds(spokenText ?? "");
		if (playbackInfo != null && playbackInfo.VisualDurationSeconds > num)
		{
			num = playbackInfo.VisualDurationSeconds;
		}
		if (playbackInfo != null && playbackInfo.TtsEnabled && playbackInfo.TtsAccepted)
		{
			num = Math.Max(num, EstimateBubbleTypingDurationSeconds(spokenText ?? ""));
		}
		float num2 = Math.Max(0.15f, num);
		request.NextStageMissionTime = mission.CurrentTime + num2;
		LogSceneSummonState("pending_launch_scheduled", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "delay=" + num2.ToString("F2") + " ttsAccepted=" + ((playbackInfo != null) ? playbackInfo.TtsAccepted.ToString() : "False") + " waitForFinish=" + ((playbackInfo != null) ? playbackInfo.WaitForPlaybackFinished.ToString() : "False") + " speechLen=" + ((spokenText ?? "").Length), force: true);
	}

	internal void SchedulePreparedSceneGuideLaunch(ActiveSceneGuideRequest request, SceneSpeechPlaybackInfo playbackInfo, string spokenText)
	{
		Mission mission = Mission.Current;
		if (request == null || mission == null)
		{
			return;
		}
		if (playbackInfo != null && playbackInfo.TtsEnabled && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished && request.GuideAgentIndex >= 0)
		{
			request.NextStageMissionTime = float.MaxValue;
			EnqueuePendingSceneGuideLaunch(request.GuideAgentIndex, request);
			return;
		}
		float num = EstimateBubbleTypingDurationSeconds(spokenText ?? "");
		if (playbackInfo != null && playbackInfo.VisualDurationSeconds > num)
		{
			num = playbackInfo.VisualDurationSeconds;
		}
		request.NextStageMissionTime = mission.CurrentTime + Math.Max(0.15f, num);
	}

	internal bool TickSceneGuideRequest(ActiveSceneGuideRequest request, float currentTime)
	{
		if (request == null)
		{
			return false;
		}
		if (request.ArrivalTriggered)
		{
			return true;
		}
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
		Agent main = Agent.Main;
		if (!CanAgentParticipateInSceneSpeech(agent) || main == null || !main.IsActive())
		{
			CleanupSceneGuideDoorProxyAgent(request);
			return true;
		}
		EscortAgentBehavior sceneGuideEscortBehavior = GetSceneGuideEscortBehavior(agent);
		Location currentLocation = CampaignMission.Current?.Location;
		if (currentLocation == null || request.TargetSourceLocation == null)
		{
			CleanupSceneGuideDoorProxyAgent(request);
			return true;
		}
		bool flag = request.TargetSourceLocation != currentLocation;
		if (!request.EscortStarted || sceneGuideEscortBehavior == null || (flag && request.TargetDoorPassage == null))
		{
			if (!TryStartSceneGuideEscort(request, agent))
			{
				CleanupSceneGuideDoorProxyAgent(request);
				return true;
			}
			request.EscortStarted = true;
			sceneGuideEscortBehavior = GetSceneGuideEscortBehavior(agent);
		}
		if (flag && IsSceneGuideDoorReached(request, agent, main))
		{
			CompleteSceneGuideArrival(request, reachedDoor: true, "door_poll");
			return true;
		}
		if (sceneGuideEscortBehavior != null && sceneGuideEscortBehavior.IsEscortFinished())
		{
			CompleteSceneGuideArrival(request, reachedDoor: flag, "escort_poll");
			return true;
		}
		return false;
	}

	internal void TriggerSceneGuideArrivalAndReturn(ActiveSceneGuideRequest request, bool reachedDoor)
	{
		if (request == null || request.GuideAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
		HoldSceneGuideAgentForArrivalSpeech(agent);
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "此人";
		}
		string factText = reachedDoor ? ("[AFEF NPC行为补充] 你已把" + text + "带到了前往人物" + request.TargetName + "所在之处的门口。") : ("[AFEF NPC行为补充] 你已把" + text + "带到了" + request.TargetName + "身边。");
		ScheduleSceneGuideReturnAfterNextSpeech(request);
		_ports.TriggerImmediateSceneBehaviorReaction(factText, request.GuideAgentIndex, persistHeroPrivateHistory: true, suppressStare: true, postSpeechLeaveSeconds: 0.5f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false);
	}

	internal void TriggerSceneGuideTimeoutAndReturn(ActiveSceneGuideRequest request)
	{
		if (request == null || request.GuideAgentIndex < 0)
		{
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.GuideAgentIndex);
		HoldSceneGuideAgentForArrivalSpeech(agent);
		string text = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "此人";
		}
		string factText = "[AFEF NPC行为补充] " + text + "长时间没有跟上你，你决定不再继续带路，先回去忙自己的事。";
		ScheduleSceneGuideReturnAfterNextSpeech(request);
		_ports.TriggerImmediateSceneBehaviorReaction(factText, request.GuideAgentIndex, persistHeroPrivateHistory: true, suppressStare: true, postSpeechLeaveSeconds: 0.5f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false);
	}

	internal void ScheduleSceneGuideReturnAfterNextSpeech(ActiveSceneGuideRequest request)
	{
		if (request == null || request.GuideAgentIndex < 0 || request.GuideLocationCharacter == null)
		{
			return;
		}
		_pendingSceneGuideReturnsAfterSpeech[request.GuideAgentIndex] = new PendingSceneGuideReturnAfterSpeech
		{
			AgentIndex = request.GuideAgentIndex,
			DisplayName = request.GuideName,
			LocationCharacter = request.GuideLocationCharacter,
			OriginalLocation = request.OriginalGuideLocation,
			OriginalPosition = request.OriginalGuidePosition
		};
		try
		{
			Logger.Log("SceneGuide", "return_after_next_speech_pending guide=" + (request.GuideName ?? "") + " target=" + (request.TargetName ?? ""));
		}
		catch
		{
		}
	}

	internal bool TickSceneSummonPendingLaunchStage(ActiveSceneSummonRequest request, float currentTime)
	{
		if (request == null)
		{
			return true;
		}
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		request.Stage = request.PendingLaunchStage;
		if (!string.IsNullOrWhiteSpace(request.LaunchAnnouncement))
		{
			AnimusForgeQuickInfo.Show(request.LaunchAnnouncement, request.SpeakerLocationCharacter?.Character);
		}
		LogSceneSummonState("pending_launch_begin", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "launchStage=" + request.PendingLaunchStage, force: true);
		return false;
	}

	internal bool TickSceneSummonMessengerToTargetStage(ActiveSceneSummonRequest request, float currentTime)
	{
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		Agent agent2 = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
		if (!CanAgentParticipateInSceneSpeech(agent) || !CanAgentParticipateInSceneSpeech(agent2))
		{
			LogSceneSummonState("same_scene_abort", request, agent, agent2, "speakerOrTargetInactive", force: true);
			return true;
		}
		if (agent.Position.DistanceSquared(agent2.Position) <= SCENE_SUMMON_MESSENGER_TARGET_DISTANCE_SQ)
		{
			try
			{
				agent.SetLookAgent(agent2);
			}
			catch
			{
			}
			request.Stage = SceneSummonStage.TargetToPlayer;
			request.NextStageMissionTime = currentTime + 0.25f;
			TryAdvanceSceneSummonBatch(request, agent);
			LogSceneSummonState("same_scene_reached_target", request, agent, agent2, null, force: true);
			return false;
		}
		try
		{
			PrepareAgentForSceneSummonMovement(agent);
			ScriptBehavior.AddAgentTarget(agent, agent2);
			TrackSceneSummonScriptedAgent(agent);
		}
		catch
		{
		}
		LogSceneSummonState("same_scene_moving_to_target", request, agent, agent2, "targetDistanceSq=" + agent.Position.DistanceSquared(agent2.Position).ToString("F2"));
		return false;
	}

	internal bool TickSceneSummonMessengerStage(ActiveSceneSummonRequest request, float currentTime)
	{
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == request.SpeakerAgentIndex);
		if (agent == null || !agent.IsActive())
		{
			CleanupSceneSummonDoorProxyAgent(request);
			request.Stage = SceneSummonStage.WaitingForTarget;
			request.NextStageMissionTime = currentTime + SCENE_SUMMON_DELAY_SECONDS;
			LogSceneSummonState("cross_scene_speaker_missing", request, agent, null, "fallback_to_wait", force: true);
			return false;
		}
		Passage messengerDoorPassage = request.MessengerDoorPassage ?? FindCurrentScenePassageToLocation(request.PassageHopLocation);
		if (messengerDoorPassage == null)
		{
			CleanupSceneSummonDoorProxyAgent(request);
			LogSceneSummonState("cross_scene_no_passage", request, agent, null, null, force: true);
			return true;
		}
		request.MessengerDoorPassage = messengerDoorPassage;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(messengerDoorPassage);
		Agent agent2 = EnsureSceneSummonDoorProxyAgent(request, messengerDoorPassage, agent);
		float num = passageWaitingPosition.AsVec2.DistanceSquared(agent.Position.AsVec2);
		if (num <= SCENE_SUMMON_RELAY_DISTANCE_SQ)
		{
			CleanupSceneSummonDoorProxyAgent(request);
			request.Stage = SceneSummonStage.WaitingForTarget;
			request.NextStageMissionTime = currentTime + SCENE_SUMMON_DELAY_SECONDS;
			TryAdvanceSceneSummonBatch(request, agent);
			LogSceneSummonState("cross_scene_reached_door", request, agent, null, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
			return false;
		}
		if (agent2 != null && agent2.IsActive())
		{
			try
			{
				PrepareAgentForSceneSummonMovement(agent);
				ScriptBehavior.AddAgentTarget(agent, agent2);
				TrackSceneSummonScriptedAgent(agent);
			}
			catch
			{
			}
			LogSceneSummonState("cross_scene_reissue_proxy_target", request, agent, agent2, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
		}
		else
		{
			LogSceneSummonState("cross_scene_reissue_passage_target", request, agent, null, "distanceSq2D=" + num.ToString("F2") + " waitPos=" + FormatSceneSummonPosition(passageWaitingPosition), force: true);
			NavigateAgentToScenePassage(agent, messengerDoorPassage);
		}
		return false;
	}

	internal bool TickSceneSummonWaitStage(ActiveSceneSummonRequest request, float currentTime)
	{
		CleanupSceneSummonDoorProxyAgent(request);
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		LocationComplex locationComplex = LocationComplex.Current;
		if (locationComplex == null || request.PassageHopLocation == null)
		{
			LogSceneSummonState("wait_stage_abort", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), "locationComplexOrHopMissing", force: true);
			return true;
		}
		BringLocationCharacterIntoCurrentScene(request.TargetLocationCharacter, request.PassageHopLocation, request.CurrentLocation);
		request.Stage = SceneSummonStage.TargetToPlayer;
		request.NextStageMissionTime = currentTime + 0.35f;
		LogSceneSummonState("wait_stage_spawn_back", request, ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter), ResolveAgentForLocationCharacter(request.TargetLocationCharacter), null, force: true);
		return false;
	}

	internal void BringLocationCharacterIntoCurrentScene(LocationCharacter locationCharacter, Location visibleFromLocation, Location currentLocation)
	{
		LocationComplex locationComplex = LocationComplex.Current;
		MissionAgentHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionAgentHandler>();
		if (locationCharacter == null || locationComplex == null || currentLocation == null || visibleFromLocation == null || missionBehavior == null)
		{
			return;
		}
		Agent agent = ResolveAgentForLocationCharacter(locationCharacter);
		Location locationOfCharacter = locationComplex.GetLocationOfCharacter(locationCharacter);
		if (locationOfCharacter != visibleFromLocation)
		{
			locationComplex.ChangeLocation(locationCharacter, locationOfCharacter, visibleFromLocation);
			locationOfCharacter = visibleFromLocation;
		}
		if (locationOfCharacter != currentLocation)
		{
			locationComplex.ChangeLocation(locationCharacter, locationOfCharacter, currentLocation);
		}
		if (agent == null || !agent.IsActive())
		{
			try
			{
				missionBehavior.SpawnEnteringLocationCharacter(locationCharacter, visibleFromLocation);
			}
			catch
			{
			}
		}
	}

	internal bool TryConsumeSceneEndChatActionTag(NpcDataPacket npc, Agent agent, ref string content, out SceneSummonConversationSession session)
	{
		session = null;
		if (string.IsNullOrWhiteSpace(content) || npc == null)
		{
			return false;
		}
		bool flag = SceneEndChatActionTagRegex.IsMatch(content);
		bool flag2 = SceneFollowStopTagRegex.IsMatch(content);
		if (!flag && !flag2)
		{
			return false;
		}
		if (flag)
		{
			content = SceneEndChatActionTagRegex.Replace(content, "").Trim();
		}
		if (flag2)
		{
			content = SceneFollowStopTagRegex.Replace(content, "").Trim();
		}
		session = TryGetSceneSummonConversationSessionForAgentIndex((agent != null) ? agent.Index : npc.AgentIndex);
		return session != null || IsAgentFollowingPlayerBySceneCommand(agent);
	}

	internal bool TryExecuteDeferredSceneFollowTagsDirectly(NpcDataPacket npc, string tags)
	{
		if (npc == null || string.IsNullOrWhiteSpace(tags))
		{
			return false;
		}
		bool hasStart = SceneFollowStartTagRegex.IsMatch(tags);
		bool hasStop = SceneFollowStopTagRegex.IsMatch(tags);
		if (!hasStart && !hasStop)
		{
			return false;
		}
		string otherTags = Regex.Replace(tags ?? "", "\\[(?:FOL|ACTION:SCENE_FOLLOW_PLAYER|STP|ACTION:SCENE_STOP_FOLLOW|ACTION:MOOD:[^\\]\\r\\n]*)\\]", "", RegexOptions.IgnoreCase).Trim();
		if (HasNonMoodDeferredSceneActionTag(otherTags))
		{
			return false;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		if (IsPrisonBreakRescueMissionActive())
		{
			Agent commandAgent = ResolvePrisonBreakSceneFollowCommandAgent(agent);
			if (commandAgent != null && commandAgent != agent)
			{
				Logger.Log("SceneFollow", "prison_break_deferred_follow_redirect requested=" + (agent?.Index ?? npc.AgentIndex) + " prisoner=" + commandAgent.Index);
				agent = commandAgent;
				npc = ShoutUtils.ExtractNpcData(commandAgent) ?? npc;
			}
		}
		if (!CanAgentParticipateInSceneSpeech(agent) || agent == Agent.Main)
		{
			Logger.Log("SceneFollow", "deferred_follow_skip agent=" + (npc?.AgentIndex ?? -1) + " reason=agent_unavailable tags=" + ((tags ?? "").Replace("\r", "\\r").Replace("\n", "\\n")));
			return true;
		}
		string content = tags;
		if (hasStop)
		{
			if (TryConsumeSceneFollowStopTag(npc, agent, ref content))
			{
				StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
				ReturnAgentAfterStoppingSceneFollow(agent);
				Logger.Log("SceneFollow", "deferred_stop_executed agent=" + agent.Index + " name=" + (npc.Name ?? ""));
			}
			else
			{
				Logger.Log("SceneFollow", "deferred_stop_noop agent=" + agent.Index + " following=" + IsAgentFollowingPlayerBySceneCommand(agent));
			}
			return true;
		}
		if (TryConsumeSceneFollowStartTag(npc, agent, ref content))
		{
			RememberSceneFollowReturnState(agent, overwriteExisting: true);
			RemoveAgentFromSceneSummonConversationForFollow(agent);
			StartSceneSummonFollowPlayer(agent);
			Logger.Log("SceneFollow", "deferred_start_executed agent=" + agent.Index + " name=" + (npc.Name ?? ""));
		}
		else
		{
			Logger.Log("SceneFollow", "deferred_start_noop agent=" + agent.Index + " following=" + IsAgentFollowingPlayerBySceneCommand(agent));
		}
		return true;
	}

	internal bool TryForceSceneFollowPlayerInternal(int targetAgentIndex, bool transient, string reason)
	{
        EnsureCurrentMission();
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (targetAgentIndex < 0 || agents == null)
		{
			return false;
		}
		Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent) || agent == Agent.Main)
		{
			Logger.Log("SceneFollow", "external_force_start_skip agent=" + targetAgentIndex + " reason=" + (reason ?? "") + " cause=agent_unavailable");
			return false;
		}
		string content = "[ACTION:SCENE_FOLLOW_PLAYER]";
		NpcDataPacket npc = new NpcDataPacket
		{
			AgentIndex = agent.Index,
			Name = agent.Name ?? "NPC",
			IsHero = (agent.Character as CharacterObject)?.IsHero == true
		};
		if (!TryConsumeSceneFollowStartTag(npc, agent, ref content))
		{
			Logger.Log("SceneFollow", "external_force_start_noop agent=" + agent.Index + " reason=" + (reason ?? "") + " following=" + IsAgentFollowingPlayerBySceneCommand(agent));
			return false;
		}
		if (transient)
		{
			_transientSceneFollowAgentIndices.Add(agent.Index);
		}
		RememberSceneFollowReturnState(agent, overwriteExisting: true);
		RemoveAgentFromSceneSummonConversationForFollow(agent);
		StartSceneSummonFollowPlayer(agent);
		Logger.Log("SceneFollow", "external_force_start_executed agent=" + agent.Index + " name=" + (agent.Name ?? "") + " transient=" + transient + " reason=" + (reason ?? ""));
		return true;
	}

	internal bool TryForceStopSceneFollowInternal(int targetAgentIndex, string reason)
	{
        EnsureCurrentMission();
		Mission mission = Mission.Current;
		Agent agent = mission?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex);
		if (agent == null || !agent.IsActive() || !IsAgentFollowingPlayerBySceneCommand(agent))
		{
			return false;
		}
		StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
		ReturnAgentAfterStoppingSceneFollow(agent);
		Logger.Log("SceneFollow", "external_force_stop_executed agent=" + agent.Index + " name=" + (agent.Name ?? "") + " reason=" + (reason ?? ""));
		return true;
	}

	internal bool ShouldReturnOnlySceneSummonSpeaker(SceneSummonConversationSession session, Agent agent)
	{
		return session != null && agent != null && IsSceneSummonConversationSpeaker(session, agent) && session.Participants.Count > 0;
	}

	internal bool TryConsumeSceneFollowStartTag(NpcDataPacket npc, Agent agent, ref string content)
	{
		if (string.IsNullOrWhiteSpace(content) || npc == null)
		{
			return false;
		}
		if (!SceneFollowStartTagRegex.IsMatch(content))
		{
			return false;
		}
		content = SceneFollowStartTagRegex.Replace(content, "").Trim();
		if (IsPrisonBreakRescueMissionActive())
		{
			return IsPrisonBreakRescuePrisonerAgent(agent);
		}
		return CanAgentParticipateInSceneSpeech(agent) && agent != Agent.Main;
	}

	internal bool TryConsumeSceneFollowStopTag(NpcDataPacket npc, Agent agent, ref string content)
	{
		if (string.IsNullOrWhiteSpace(content) || npc == null)
		{
			return false;
		}
		bool flag = SceneFollowStopTagRegex.IsMatch(content);
		bool flag2 = SceneEndChatActionTagRegex.IsMatch(content);
		if (!flag && !flag2)
		{
			return false;
		}
		if (flag)
		{
			content = SceneFollowStopTagRegex.Replace(content, "").Trim();
		}
		if (flag2)
		{
			content = SceneEndChatActionTagRegex.Replace(content, "").Trim();
		}
		if (IsPrisonBreakRescueMissionActive())
		{
			return flag && IsPrisonBreakRescuePrisonerAgent(agent) && IsPrisonBreakPrisonerFollowing(agent);
		}
		return CanAgentParticipateInSceneSpeech(agent) && agent != Agent.Main && IsAgentFollowingPlayerBySceneCommand(agent);
	}

	internal void RefreshSceneSummonConversationForSpeaker(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return;
		}
		SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agentIndex);
		if (sceneSummonConversationSession != null)
		{
			RefreshSceneSummonConversationInteractions(sceneSummonConversationSession);
		}
	}

	internal void UpdateActiveSceneGuideRequests()
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (_activeSceneGuideRequests.Count == 0 || mission == null)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		for (int num = _activeSceneGuideRequests.Count - 1; num >= 0; num--)
		{
			ActiveSceneGuideRequest activeSceneGuideRequest = _activeSceneGuideRequests[num];
			if (activeSceneGuideRequest == null || CampaignMission.Current?.Location != activeSceneGuideRequest.CurrentLocation)
			{
				if (activeSceneGuideRequest != null)
				{
					ReleaseSceneGuideArrivalHold(activeSceneGuideRequest.GuideAgentIndex, restoreAutonomy: false);
				}
				StopSceneGuideEscort(agents?.FirstOrDefault((Agent a) => a != null && a.Index == activeSceneGuideRequest?.GuideAgentIndex));
				CleanupSceneGuideDoorProxyAgent(activeSceneGuideRequest);
				_activeSceneGuideRequests.RemoveAt(num);
				continue;
			}
			if (activeSceneGuideRequest.ArrivalTriggered)
			{
				Agent agent = agents?.FirstOrDefault((Agent a) => a != null && a.Index == activeSceneGuideRequest.GuideAgentIndex);
				StopSceneGuideEscort(agent);
				if (_sceneGuideArrivalHolds.TryGetValue(activeSceneGuideRequest.GuideAgentIndex, out var value))
				{
					ApplySceneGuideArrivalHold(agent, value);
				}
				CleanupSceneGuideDoorProxyAgent(activeSceneGuideRequest);
				_activeSceneGuideRequests.RemoveAt(num);
				continue;
			}
			if (TickSceneGuideRequest(activeSceneGuideRequest, currentTime))
			{
				Agent agent = agents?.FirstOrDefault((Agent a) => a != null && a.Index == activeSceneGuideRequest?.GuideAgentIndex);
				StopSceneGuideEscort(agent);
				if (activeSceneGuideRequest != null && _sceneGuideArrivalHolds.TryGetValue(activeSceneGuideRequest.GuideAgentIndex, out var value))
				{
					ApplySceneGuideArrivalHold(agent, value);
				}
				CleanupSceneGuideDoorProxyAgent(activeSceneGuideRequest);
				_activeSceneGuideRequests.RemoveAt(num);
			}
		}
	}

	internal bool IsAgentBusyWithSceneSummonErrand(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return false;
		}
		if (_activeSceneSummonRequests.Any((ActiveSceneSummonRequest x) => x != null && x.SpeakerAgentIndex == agentIndex))
		{
			return true;
		}
		if (_activeSceneSummonBatches.Values.Any((SceneSummonBatchState x) => x != null && x.SpeakerAgentIndex == agentIndex && x.PendingTargets.Count > 0))
		{
			return true;
		}
		lock (_launchGate)
		{
			return _pendingSceneSummonLaunchQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0;
		}
	}

	internal bool IsAgentBusyWithSceneGuideErrand(int agentIndex)
	{
		if (agentIndex < 0)
		{
			return false;
		}
		if (_activeSceneGuideRequests.Any((ActiveSceneGuideRequest x) => x != null && x.GuideAgentIndex == agentIndex))
		{
			return true;
		}
		lock (_launchGate)
		{
			return _pendingSceneGuideLaunchQueues.TryGetValue(agentIndex, out var value) && value != null && value.Count > 0;
		}
	}

	internal void UpdateSceneSummonConversationEscortMovement()
	{
		Agent main = Agent.Main;
		if (main == null || !main.IsActive() || _activeSceneSummonConversationSessions.Count == 0)
		{
			return;
		}
		foreach (SceneSummonConversationSession item in _activeSceneSummonConversationSessions)
		{
			if (item == null)
			{
				continue;
			}
			List<Agent> list = new List<Agent>();
			for (int i = 0; i < item.Participants.Count; i++)
			{
				Agent agent = ResolveAgentForLocationCharacter(item.Participants[i]?.LocationCharacter);
				if (CanAgentParticipateInSceneSpeech(agent) && agent != main)
				{
					list.Add(agent);
				}
			}
			if (item.KeepSpeakerNearby)
			{
				Agent agent2 = ResolveAgentForLocationCharacter(item.SpeakerLocationCharacter);
				if (CanAgentParticipateInSceneSpeech(agent2) && agent2 != main && !list.Contains(agent2))
				{
					list.Add(agent2);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].Position.DistanceSquared(main.Position) <= SCENE_SUMMON_ESCORT_REPOSITION_DISTANCE_SQ)
				{
					continue;
				}
				Vec3 targetPosition = BuildSceneSummonEscortPosition(main, j, list.Count);
				TryGuideSummonParticipantToPosition(list[j], targetPosition);
			}
		}
	}

	internal static void AppendSceneGuidePromptSection(StringBuilder prompt, List<SceneGuidePromptTarget> targets)
	{
		if (prompt == null || targets == null || targets.Count == 0)
		{
			return;
		}
		prompt.AppendLine("【可带路目标】：");
		foreach (SceneGuidePromptTarget target in targets)
		{
			if (target != null && target.PromptId > 0)
			{
				prompt.AppendLine(target.PromptId + " " + target.DisplayName.Trim() + " " + (target.LocationCode ?? "处"));
			}
		}
	}

	internal static PrisonBreakMissionController GetPrisonBreakMissionController()
	{
		try
		{
			return Mission.Current?.GetMissionBehavior<PrisonBreakMissionController>();
		}
		catch
		{
			return null;
		}
	}

	internal static bool IsPrisonBreakRescueMissionActive()
	{
		return GetPrisonBreakMissionController() != null;
	}

	internal static bool TryGetPrisonBreakRescuePrisonerAgent(out Agent prisonerAgent)
	{
		prisonerAgent = GetPrisonBreakPrisonerAgent();
		return CanAgentParticipateInSceneSpeech(prisonerAgent) && prisonerAgent != Agent.Main;
	}

	internal static Agent ResolvePrisonBreakSceneFollowCommandAgent(Agent requestedAgent)
	{
		if (!IsPrisonBreakRescueMissionActive())
		{
			return requestedAgent;
		}
		if (IsPrisonBreakRescuePrisonerAgent(requestedAgent))
		{
			return requestedAgent;
		}
		return TryGetPrisonBreakRescuePrisonerAgent(out var prisonerAgent) ? prisonerAgent : requestedAgent;
	}

	internal static Agent GetPrisonBreakPrisonerAgent(PrisonBreakMissionController controller = null)
	{
		try
		{
			controller = controller ?? GetPrisonBreakMissionController();
			return (controller != null && PrisonBreakPrisonerAgentField != null) ? PrisonBreakPrisonerAgentField.GetValue(controller) as Agent : null;
		}
		catch
		{
			return null;
		}
	}

	internal static bool IsPrisonBreakRescuePrisonerAgent(Agent agent)
	{
		try
		{
			PrisonBreakMissionController controller = GetPrisonBreakMissionController();
			Agent prisonerAgent = GetPrisonBreakPrisonerAgent(controller);
			return CanAgentParticipateInSceneSpeech(agent) && prisonerAgent != null && prisonerAgent == agent;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsPrisonBreakPrisonerFollowing(Agent agent)
	{
		try
		{
			PrisonBreakMissionController controller = GetPrisonBreakMissionController();
			if (controller == null || GetPrisonBreakPrisonerAgent(controller) != agent || PrisonBreakPrisonerFollowingField == null)
			{
				return false;
			}
			return PrisonBreakPrisonerFollowingField.GetValue(controller) is bool value && value;
		}
		catch
		{
			return false;
		}
	}

	internal void ClearAnimusForgeSceneFollowStateForPrisonBreak(Agent agent)
	{
		if (agent == null || agent.Index < 0)
		{
			return;
		}
		_transientSceneFollowAgentIndices.Remove(agent.Index);
		_sceneFollowReturnStates.Remove(agent.Index);
		_pendingSceneFollowCommands.Remove(agent.Index);
		_ports.CancelAutonomyRestore(agent.Index);
		_pendingSceneSummonReturnsAfterSpeech.Remove(agent.Index);
		_pendingSceneGuideReturnsAfterSpeech.Remove(agent.Index);
		_ports.CancelInteractionTimeoutArm(agent.Index);
		_ports.RemoveInteractionSession(agent.Index);
		TrySetSceneFollowPersistence(agent, isFollowing: false);
	}

	internal bool TryApplyPrisonBreakSceneFollowCommand(Agent agent, bool startFollow, string reason)
	{
		try
		{
			PrisonBreakMissionController controller = GetPrisonBreakMissionController();
			if (controller == null)
			{
				return false;
			}
			Agent prisonerAgent = GetPrisonBreakPrisonerAgent(controller);
			if (!CanAgentParticipateInSceneSpeech(agent) || prisonerAgent == null || prisonerAgent != agent || PrisonBreakSwitchPrisonerFollowingStateMethod == null)
			{
				Logger.Log("SceneFollow", "prison_break_follow_skip agent=" + (agent?.Index ?? -1) + " reason=" + (reason ?? "") + " cause=not_rescue_prisoner");
				return true;
			}
			ClearAnimusForgeSceneFollowStateForPrisonBreak(agent);
			bool currentlyFollowing = IsPrisonBreakPrisonerFollowing(agent);
			if (startFollow)
			{
				if (!currentlyFollowing)
				{
					PrisonBreakSwitchPrisonerFollowingStateMethod.Invoke(controller, new object[] { true });
				}
			}
			else if (currentlyFollowing)
			{
				PrisonBreakSwitchPrisonerFollowingStateMethod.Invoke(controller, new object[] { false });
			}
			Logger.Log("SceneFollow", "prison_break_follow_" + (startFollow ? "start" : "stop") + " agent=" + agent.Index + " name=" + (agent.Name ?? "") + " wasFollowing=" + currentlyFollowing + " nowFollowing=" + IsPrisonBreakPrisonerFollowing(agent) + " reason=" + (reason ?? ""));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SceneFollow", "prison_break_follow_failed agent=" + (agent?.Index ?? -1) + " start=" + startFollow + " reason=" + (reason ?? "") + " error=" + ex.Message);
			return true;
		}
	}

	internal void UpdateActiveSceneReturnJobs()
	{
		if (_activeSceneReturnJobs.Count == 0 || CampaignMission.Current?.Location == null)
		{
			return;
		}
		for (int num = _activeSceneReturnJobs.Count - 1; num >= 0; num--)
		{
			SceneReturnJob sceneReturnJob = _activeSceneReturnJobs[num];
			if (sceneReturnJob == null || sceneReturnJob.LocationCharacter == null || sceneReturnJob.CurrentLocation != CampaignMission.Current.Location)
			{
				CleanupSceneReturnDoorProxyAgent(sceneReturnJob);
				_activeSceneReturnJobs.RemoveAt(num);
				continue;
			}
			if (TickSceneReturnJob(sceneReturnJob))
			{
				CleanupSceneReturnDoorProxyAgent(sceneReturnJob);
				_activeSceneReturnJobs.RemoveAt(num);
			}
		}
	}

	internal bool TickSceneReturnJob(SceneReturnJob job)
	{
		LocationComplex locationComplex = LocationComplex.Current;
		Location currentLocation = CampaignMission.Current?.Location;
		if (job == null || locationComplex == null || currentLocation == null)
		{
			return true;
		}
		Location originalLocation = job.OriginalLocation ?? currentLocation;
		Agent agent = ResolveAgentForLocationCharacter(job.LocationCharacter);
		if (originalLocation == currentLocation)
		{
			if (!job.OriginalPosition.HasValue || !CanAgentParticipateInSceneSpeech(agent))
			{
				if (agent != null)
				{
					_ports.RestoreAgentAutonomy(agent);
				}
				return true;
			}
			ApplySceneReturnWalkPacing(agent);
			NavigateAgentToWorldPosition(agent, job.OriginalPosition.Value, 0.6f, doNotRun: true);
			if (agent.Position.DistanceSquared(job.OriginalPosition.Value) > SCENE_SUMMON_TARGET_ARRIVAL_DISTANCE_SQ)
			{
				return false;
			}
			_ports.RestoreAgentAutonomy(agent);
			return true;
		}
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return true;
		}
		List<Location> sceneLocationPath = FindSceneLocationPath(currentLocation, originalLocation);
		if (sceneLocationPath == null || sceneLocationPath.Count < 2)
		{
			return true;
		}
		Location location = sceneLocationPath[1];
		Passage passage = job.ExitPassage ?? FindCurrentScenePassageToLocation(location);
		if (passage == null)
		{
			return true;
		}
		job.ExitPassage = passage;
		Vec3 passageWaitingPosition = GetPassageWaitingPosition(passage);
		Agent agent2 = EnsureSceneReturnDoorProxyAgent(job, passage, agent);
		if (passageWaitingPosition.AsVec2.DistanceSquared(agent.Position.AsVec2) > SCENE_SUMMON_MESSENGER_DOOR_DISTANCE_SQ)
		{
			ApplySceneReturnWalkPacing(agent);
			NavigateAgentToWorldPosition(agent, passageWaitingPosition, 0.9f, doNotRun: true);
			return false;
		}
		CleanupSceneReturnDoorProxyAgent(job);
		locationComplex.ChangeLocation(job.LocationCharacter, currentLocation, location);
		if (originalLocation != location)
		{
			locationComplex.ChangeLocation(job.LocationCharacter, location, originalLocation);
		}
		try
		{
			agent.FadeOut(false, true);
		}
		catch
		{
		}
		return true;
	}

	internal void ApplySceneReturnWalkPacing(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		try
		{
			agent.SetMaximumSpeedLimit(1.55f, isMultiplier: false);
		}
		catch
		{
		}
	}

	internal bool TryGuideSummonParticipantToPosition(Agent agent, Vec3 targetPosition)
	{
		var scene = Mission.Current?.Scene;
		if (!CanAgentParticipateInSceneSpeech(agent) || scene == null)
		{
			return false;
		}
		targetPosition.z = scene.GetGroundHeightAtPosition(targetPosition, BodyFlags.CommonCollisionExcludeFlags);
		NavigateAgentToWorldPosition(agent, targetPosition, 0.75f, doNotRun: false);
		return agent.Position.DistanceSquared(targetPosition) <= SCENE_SUMMON_TARGET_ARRIVAL_DISTANCE_SQ;
	}

	internal void StartSceneSummonFollowPlayer(Agent agent)
	{
		if (!CanAgentParticipateInSceneSpeech(agent) || Agent.Main == null || !Agent.Main.IsActive() || agent == Agent.Main)
		{
			return;
		}
		if (IsPrisonBreakRescueMissionActive())
		{
			TryApplyPrisonBreakSceneFollowCommand(agent, startFollow: true, "scene_follow_start");
			InvalidateSceneCommandFollowerCache();
			return;
		}
		try
		{
			CancelSceneTemporaryCommandStateForFollow(agent);
			_ports.CancelAutonomyRestore(agent.Index);
			_pendingSceneSummonReturnsAfterSpeech.Remove(agent.Index);
			_pendingSceneGuideReturnsAfterSpeech.Remove(agent.Index);
			ReleaseSceneGuideArrivalHold(agent.Index, restoreAutonomy: false);
			ClearSceneSummonScriptedBehavior(agent);
			_ports.DetachStareAgent(agent.Index);
			_ports.ClearEmptyStareDeadline();
			_ports.RemoveSceneMovementSuppressionAgents(new int[1] { agent.Index });
			_ports.RemoveAttentionRelease(agent.Index);
			_ports.ReleaseAgentFromSceneConversationLocks(agent);
			RememberSceneFollowReturnState(agent, overwriteExisting: false);
			bool transient = _transientSceneFollowAgentIndices.Contains(agent.Index);
			bool persisted = transient ? false : TrySetSceneFollowPersistence(agent, isFollowing: true);
			_ports.RemoveInteractionSession(agent.Index);
			_ports.CancelInteractionTimeoutArm(agent.Index);
			bool behaviorEnabled = TryEnableVanillaSceneFollowBehavior(agent, Agent.Main);
			InvalidateSceneCommandFollowerCache();
			Logger.Log("SceneFollow", "start agent=" + agent.Index + " name=" + (agent.Name ?? "") + " transient=" + transient + " persisted=" + persisted + " behaviorEnabled=" + behaviorEnabled);
		}
		catch
		{
		}
	}

	internal void StopSceneSummonFollowPlayer(Agent agent, bool restoreDailyBehaviors = true, bool invalidateFollowerCache = true)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		if (IsPrisonBreakRescueMissionActive())
		{
			TryApplyPrisonBreakSceneFollowCommand(agent, startFollow: false, "scene_follow_stop");
			if (invalidateFollowerCache)
			{
				InvalidateSceneCommandFollowerCache();
			}
			return;
		}
		try
		{
			bool wasTransient = _transientSceneFollowAgentIndices.Remove(agent.Index);
			if (wasTransient)
			{
				_sceneFollowReturnStates.Remove(agent.Index);
			}
			bool persisted = TrySetSceneFollowPersistence(agent, isFollowing: false);
			TryDisableVanillaSceneFollowBehavior(agent, restoreDailyBehaviors);
			if (invalidateFollowerCache)
			{
				InvalidateSceneCommandFollowerCache();
			}
			Logger.Log("SceneFollow", "stop agent=" + agent.Index + " name=" + (agent.Name ?? "") + " transient=" + wasTransient + " persisted=" + persisted + " restoreDaily=" + restoreDailyBehaviors);
		}
		catch
		{
		}
	}

	internal void RememberSceneFollowReturnState(Agent agent, bool overwriteExisting = true)
	{
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			return;
		}
		if (IsPrisonBreakRescuePrisonerAgent(agent))
		{
			return;
		}
		if (!overwriteExisting && _sceneFollowReturnStates.ContainsKey(agent.Index))
		{
			return;
		}
		SceneFollowReturnState sceneFollowReturnState = BuildSceneFollowReturnState(agent);
		if (sceneFollowReturnState != null)
		{
			_sceneFollowReturnStates[agent.Index] = sceneFollowReturnState;
		}
	}

	internal SceneFollowReturnState BuildSceneFollowReturnState(Agent agent)
	{
		try
		{
			LocationComplex locationComplex = LocationComplex.Current;
			Location currentLocation = CampaignMission.Current?.Location;
			if (!CanAgentParticipateInSceneSpeech(agent) || locationComplex == null || currentLocation == null)
			{
				return null;
			}
			LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
			if (locationCharacter == null)
			{
				return null;
			}
			SceneSummonConversationSession sceneSummonConversationSession = TryGetSceneSummonConversationSessionForAgentIndex(agent.Index);
			if (sceneSummonConversationSession != null)
			{
				SceneSummonConversationParticipant sceneSummonConversationParticipant = sceneSummonConversationSession.Participants.FirstOrDefault((SceneSummonConversationParticipant p) => p != null && p.LocationCharacter == locationCharacter);
				if (sceneSummonConversationParticipant != null)
				{
					return new SceneFollowReturnState
					{
						DisplayName = sceneSummonConversationParticipant.DisplayName,
						LocationCharacter = locationCharacter,
						OriginalLocation = sceneSummonConversationParticipant.OriginalLocation ?? currentLocation,
						OriginalPosition = sceneSummonConversationParticipant.OriginalPosition
					};
				}
				if (sceneSummonConversationSession.SpeakerLocationCharacter == locationCharacter)
				{
					return new SceneFollowReturnState
					{
						DisplayName = sceneSummonConversationSession.SpeakerName,
						LocationCharacter = locationCharacter,
						OriginalLocation = sceneSummonConversationSession.OriginalSpeakerLocation ?? currentLocation,
						OriginalPosition = sceneSummonConversationSession.OriginalSpeakerPosition
					};
				}
			}
			return new SceneFollowReturnState
			{
				DisplayName = agent.Name?.ToString() ?? "NPC",
				LocationCharacter = locationCharacter,
				OriginalLocation = currentLocation,
				OriginalPosition = agent.Position
			};
		}
		catch
		{
			return null;
		}
	}

	internal void ReturnAgentAfterStoppingSceneFollow(Agent agent)
	{
		if (agent == null)
		{
			return;
		}
		if (IsPrisonBreakRescuePrisonerAgent(agent))
		{
			return;
		}
		if (_sceneFollowReturnStates.TryGetValue(agent.Index, out var value) && value != null && value.LocationCharacter != null)
		{
			_sceneFollowReturnStates.Remove(agent.Index);
			QueueSceneReturnJob(value.DisplayName, value.LocationCharacter, value.OriginalLocation, value.OriginalPosition);
			try
			{
				agent.ClearTargetFrame();
				ClearSceneSummonScriptedBehavior(agent);
			}
			catch
			{
			}
			return;
		}
		_ports.RestoreAgentAutonomy(agent);
	}

	internal static bool TryEnableVanillaSceneFollowBehavior(Agent agent, Agent target)
	{
		try
		{
			if (agent == null || target == null || !agent.IsActive() || !target.IsActive())
			{
				return false;
			}
			CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator ?? component?.CreateAgentNavigator();
			DailyBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>() ?? agentNavigator?.AddBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup == null)
			{
				return false;
			}
			behaviorGroup.IsActive = true;
			FollowAgentBehavior followAgentBehavior = behaviorGroup.GetBehavior<FollowAgentBehavior>() ?? behaviorGroup.AddBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
			followAgentBehavior.IsActive = true;
			followAgentBehavior.SetTargetAgent(target);
			ScriptBehavior behavior = behaviorGroup.GetBehavior<ScriptBehavior>();
			if (behavior != null)
			{
				behavior.IsActive = false;
			}
			WalkingBehavior behavior2 = behaviorGroup.GetBehavior<WalkingBehavior>();
			if (behavior2 != null)
			{
				behavior2.IsActive = false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal static void TryDisableVanillaSceneFollowBehavior(Agent agent, bool restoreDailyBehaviors = true)
	{
		try
		{
			CampaignAgentComponent component = agent?.GetComponent<CampaignAgentComponent>();
			AgentNavigator agentNavigator = component?.AgentNavigator;
			DailyBehaviorGroup behaviorGroup = agentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup == null)
			{
				return;
			}
			behaviorGroup.RemoveBehavior<FollowAgentBehavior>();
			if (restoreDailyBehaviors)
			{
				ScriptBehavior behavior = behaviorGroup.GetBehavior<ScriptBehavior>();
				if (behavior != null)
				{
					behavior.IsActive = true;
				}
				WalkingBehavior behavior2 = behaviorGroup.GetBehavior<WalkingBehavior>() ?? behaviorGroup.AddBehavior<WalkingBehavior>();
				behavior2.IsActive = true;
			}
		}
		catch
		{
		}
	}

	internal static bool TrySetSceneFollowPersistence(Agent agent, bool isFollowing)
	{
		try
		{
			LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
			LocationComplex locationComplex = LocationComplex.Current;
			if (agent == null || locationEncounter == null || locationComplex == null)
			{
				return false;
			}
			if (isFollowing && IsPrisonBreakRescuePrisonerAgent(agent))
			{
				return false;
			}
			LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
			if (locationCharacter == null)
			{
				return false;
			}
			if (isFollowing)
			{
				AccompanyingCharacter accompanyingCharacter = locationEncounter.GetAccompanyingCharacter(locationCharacter);
				if (accompanyingCharacter == null || !accompanyingCharacter.IsFollowingPlayerAtMissionStart)
				{
					locationEncounter.RemoveAccompanyingCharacter(locationCharacter);
					locationEncounter.AddAccompanyingCharacter(locationCharacter, isFollowing: true);
				}
			}
			else
			{
				locationEncounter.RemoveAccompanyingCharacter(locationCharacter);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	internal List<Agent> GetSceneCommandFollowerSnapshot(Mission mission)
	{
		float applicationTime = GetApplicationTimeSafe();
		if (ReferenceEquals(_sceneCommandFollowerCacheMission, mission)
			&& applicationTime < _nextSceneCommandFollowerCacheRefreshApplicationTime)
		{
			return _sceneCommandFollowerCache;
		}
		using PerfProbe.ScopeToken perfScope = PerfProbe.Scope("Mission.Shout.RefreshSceneCommandFollowerCache");
		_sceneCommandFollowerCacheMission = mission;
		_nextSceneCommandFollowerCacheRefreshApplicationTime = applicationTime + SCENE_COMMAND_FOLLOWER_CACHE_REFRESH_SECONDS;
		_sceneCommandFollowerCache.Clear();
		var agents = mission?.Agents;
		if (agents == null)
		{
			return _sceneCommandFollowerCache;
		}
		foreach (Agent agent in agents)
		{
			if (IsAgentFollowingPlayerBySceneCommand(agent))
			{
				_sceneCommandFollowerCache.Add(agent);
			}
		}
		return _sceneCommandFollowerCache;
	}

	internal void InvalidateSceneCommandFollowerCache()
	{
		_sceneCommandFollowerCache.Clear();
		_sceneCommandFollowerCacheMission = null;
		_nextSceneCommandFollowerCacheRefreshApplicationTime = 0f;
	}

	internal bool IsAgentFollowingPlayerBySceneCommand(Agent agent)
	{
		try
		{
			if (!CanAgentParticipateInSceneSpeech(agent) || agent == Agent.Main)
			{
				return false;
			}
			if (IsPrisonBreakRescuePrisonerAgent(agent))
			{
				return IsPrisonBreakPrisonerFollowing(agent);
			}
			if (_transientSceneFollowAgentIndices.Contains(agent.Index))
			{
				return true;
			}
			LocationEncounter locationEncounter = PlayerEncounter.LocationEncounter;
			LocationComplex locationComplex = LocationComplex.Current;
			if (locationEncounter == null || locationComplex == null)
			{
				return false;
			}
			LocationCharacter locationCharacter = locationComplex.FindCharacter(agent);
			AccompanyingCharacter accompanyingCharacter = ((locationCharacter != null) ? locationEncounter.GetAccompanyingCharacter(locationCharacter) : null);
			return accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart;
		}
		catch
		{
			return false;
		}
	}

	internal bool TickSceneSummonTargetStage(ActiveSceneSummonRequest request, float currentTime)
	{
		if (currentTime < request.NextStageMissionTime)
		{
			return false;
		}
		Agent agent = ResolveAgentForLocationCharacter(request.TargetLocationCharacter);
		Agent agent2 = ResolveAgentForLocationCharacter(request.SpeakerLocationCharacter);
		Agent main = Agent.Main;
		if (!CanAgentParticipateInSceneSpeech(agent) || main == null || !main.IsActive())
		{
			LogSceneSummonState("target_to_player_wait_abort", request, agent2, agent, "targetOrPlayerUnavailable", force: currentTime >= request.NextStageMissionTime + 6f);
			return currentTime >= request.NextStageMissionTime + 6f;
		}
		BuildSceneSummonStandPositions(main, out var primaryPosition, out var secondaryPosition);
		bool flag = TryGuideSummonParticipantToPosition(agent, primaryPosition);
		bool flag2 = true;
		if (request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent)
		{
			flag2 = TryGuideSummonParticipantToPosition(agent2, secondaryPosition);
		}
		if (!flag)
		{
			LogSceneSummonState("target_to_player_moving", request, agent2, agent, "targetArrived=" + flag + " messengerArrived=" + flag2 + " targetStand=" + FormatSceneSummonPosition(primaryPosition) + " messengerStand=" + FormatSceneSummonPosition(secondaryPosition));
			return false;
		}
		if (!request.ArrivalSpeechConsumed && string.IsNullOrWhiteSpace(request.PreGeneratedArrivalSpeech) && currentTime < request.ArrivalSpeechDeadlineMissionTime)
		{
			LogSceneSummonState("target_to_player_waiting_speech", request, agent2, agent, "deadline=" + request.ArrivalSpeechDeadlineMissionTime.ToString("F2"));
			return false;
		}
		_ports.ForceAgentFacePlayer(agent);
		bool flag3 = request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent && flag2;
		if (request.KeepMessengerWithTarget && CanAgentParticipateInSceneSpeech(agent2) && agent2 != agent && !flag2)
		{
			_ports.RestoreAgentAutonomy(agent2);
		}
		if (flag3)
		{
			_ports.ForceAgentFacePlayer(agent2);
			PlaySceneSummonArrivalSpeechIfReady(request, agent2);
			RegisterSceneSummonConversationSession(request, agent2, agent);
			TryRecordSceneSummonBatchCompletionFact(request);
			LogSceneSummonState("target_to_player_arrived_pair", request, agent2, agent, null, force: true);
			string text = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
			AnimusForgeQuickInfo.Show(request.SpeakerName + " 带着" + text + "过来了。", request.SpeakerLocationCharacter?.Character);
		}
		else
		{
			PlaySceneSummonArrivalSpeechIfReady(request, agent2 ?? agent);
			RegisterSceneSummonConversationSession(request, agent2, agent);
			TryRecordSceneSummonBatchCompletionFact(request);
			LogSceneSummonState("target_to_player_arrived_single", request, agent2, agent, null, force: true);
			string text2 = BuildSceneSummonArrivedDisplayNames(request.BatchId, request.TargetName);
			AnimusForgeQuickInfo.Show(text2 + " 被叫过来了。", request.TargetLocationCharacter?.Character);
		}
		return true;
	}

	internal void SuppressOrdinarySceneFollowupsForBattleSpeech(
		IEnumerable<NpcDataPacket> participants)
	{
		int[] agentIndices = (participants ?? Enumerable.Empty<NpcDataPacket>())
			.Where(npc => npc != null && npc.AgentIndex >= 0)
			.Select(npc => npc.AgentIndex)
			.Distinct()
			.ToArray();
		foreach (int agentIndex in agentIndices)
		{
			_ports.CancelInteractionTimeoutArm(agentIndex);
			_ports.RemoveInteractionSession(agentIndex);
		}
		_ports.RemoveSceneMovementSuppressionAgents(agentIndices);
		Logger.Log(
			"ShoutBehavior",
			"[BattleSpeech] suppressed ordinary idle/relay follow-ups participants=" +
			agentIndices.Length);
	}

	internal void ScheduleSceneSummonReturnAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null || !_pendingSceneSummonReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (mission.CurrentTime + num);
	}

	internal void FlushSceneSummonReturnAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneSummonReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneSummonReturnsAfterSpeech.Remove(agentIndex);
		if (value.Session != null && _activeSceneSummonConversationSessions.Contains(value.Session))
		{
			if (value.ReturnOnlySpeaker)
			{
				BeginSceneSummonSpeakerReturn(value.Session);
			}
			else
			{
				BeginSceneSummonConversationReturn(value.Session);
			}
		}
	}

	internal void ScheduleSceneFollowCommandAfterSpeech(int agentIndex, bool startFollow, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		_pendingSceneFollowCommands[agentIndex] = new PendingSceneFollowCommand
		{
			AgentIndex = agentIndex,
			StartFollow = startFollow,
			WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished,
			ExecuteAtMissionTime = ((playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished) ? (-1f) : (mission.CurrentTime + num))
		};
		Logger.Log(
			"SceneFollow",
			"scheduled agent=" + agentIndex
			+ " action=" + (startFollow ? "start" : "stop")
			+ " waitPlayback=" + (playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished)
			+ " executeAt=" + ((playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished) ? "playback" : (mission.CurrentTime + num).ToString("F2")));
	}

	internal void FlushSceneFollowCommandAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneFollowCommands.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		_pendingSceneFollowCommands.Remove(agentIndex);
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == agentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			Logger.Log("SceneFollow", "flush_skip agent=" + agentIndex + " reason=agent_unavailable action=" + (value.StartFollow ? "start" : "stop"));
			return;
		}
		if (value.StartFollow)
		{
			RememberSceneFollowReturnState(agent, overwriteExisting: true);
			RemoveAgentFromSceneSummonConversationForFollow(agent);
			StartSceneSummonFollowPlayer(agent);
		}
		else
		{
			StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false);
			ReturnAgentAfterStoppingSceneFollow(agent);
		}
		Logger.Log("SceneFollow", "flushed agent=" + agentIndex + " action=" + (value.StartFollow ? "start" : "stop") + " following=" + IsAgentFollowingPlayerBySceneCommand(agent));
	}

	internal void ScheduleSceneGuideReturnAfterSpeech(int agentIndex, SceneSpeechPlaybackInfo playbackInfo)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null || !_pendingSceneGuideReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		float num = Math.Max(0.25f, playbackInfo?.VisualDurationSeconds ?? 0f);
		value.SpeechPlaybackScheduled = true;
		value.WaitForPlaybackFinished = playbackInfo != null && playbackInfo.TtsAccepted && playbackInfo.WaitForPlaybackFinished;
		value.PostPlaybackDelayArmed = false;
		value.ExecuteAtMissionTime = value.WaitForPlaybackFinished ? (-1f) : (mission.CurrentTime + num + SCENE_GUIDE_RETURN_EXTRA_DELAY_SECONDS);
		try
		{
			Logger.Log("SceneGuide", "return_after_speech_scheduled agent=" + agentIndex + " waitPlayback=" + value.WaitForPlaybackFinished + " delay=" + (num + SCENE_GUIDE_RETURN_EXTRA_DELAY_SECONDS).ToString("F2"));
		}
		catch
		{
		}
		if (_sceneGuideArrivalHolds.TryGetValue(agentIndex, out var value2) && value2 != null)
		{
			value2.ExpiresAtMissionTime = Math.Max(value2.ExpiresAtMissionTime, mission.CurrentTime + Math.Max(SCENE_GUIDE_ARRIVAL_HOLD_FAILSAFE_SECONDS, num + SCENE_GUIDE_RETURN_EXTRA_DELAY_SECONDS + 15f));
		}
	}

	internal void FlushSceneGuideReturnAfterSpeech(int agentIndex)
	{
		if (agentIndex < 0 || !_pendingSceneGuideReturnsAfterSpeech.TryGetValue(agentIndex, out var value) || value == null)
		{
			return;
		}
		if (!value.SpeechPlaybackScheduled)
		{
			return;
		}
		Mission mission = Mission.Current;
		if (mission != null)
		{
			float currentTime = mission.CurrentTime;
			if (value.WaitForPlaybackFinished && !value.PostPlaybackDelayArmed)
			{
				value.WaitForPlaybackFinished = false;
				value.PostPlaybackDelayArmed = true;
				value.ExecuteAtMissionTime = currentTime + SCENE_GUIDE_RETURN_EXTRA_DELAY_SECONDS;
				try
				{
					Logger.Log("SceneGuide", "return_after_speech_delay_armed agent=" + agentIndex + " delay=" + SCENE_GUIDE_RETURN_EXTRA_DELAY_SECONDS.ToString("F2"));
				}
				catch
				{
				}
				return;
			}
			if (!value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime < value.ExecuteAtMissionTime)
			{
				return;
			}
			if (!value.WaitForPlaybackFinished && value.ExecuteAtMissionTime < 0f)
			{
				return;
			}
		}
		_pendingSceneGuideReturnsAfterSpeech.Remove(agentIndex);
		ReleaseSceneGuideArrivalHold(agentIndex, restoreAutonomy: false);
		if (value.LocationCharacter != null)
		{
			try
			{
				Logger.Log("SceneGuide", "return_job_queued agent=" + agentIndex + " name=" + (value.DisplayName ?? ""));
			}
			catch
			{
			}
			QueueSceneReturnJob(value.DisplayName, value.LocationCharacter, value.OriginalLocation, value.OriginalPosition);
		}
	}

	internal void UpdatePendingSceneSummonReturnsAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingSceneSummonReturnsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneSummonReturnAfterSpeech> pendingSceneSummonReturnsAfterSpeech in _pendingSceneSummonReturnsAfterSpeech)
		{
			PendingSceneSummonReturnAfterSpeech value = pendingSceneSummonReturnsAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneSummonReturnsAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneSummonReturnAfterSpeech(item);
		}
	}

	internal void UpdatePendingSceneGuideReturnsAfterSpeech()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingSceneGuideReturnsAfterSpeech.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneGuideReturnAfterSpeech> pendingSceneGuideReturnsAfterSpeech in _pendingSceneGuideReturnsAfterSpeech)
		{
			PendingSceneGuideReturnAfterSpeech value = pendingSceneGuideReturnsAfterSpeech.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneGuideReturnsAfterSpeech.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneGuideReturnAfterSpeech(item);
		}
	}

	internal void UpdatePendingSceneFollowCommands()
	{
		Mission mission = Mission.Current;
		if (mission == null || _pendingSceneFollowCommands.Count == 0)
		{
			return;
		}
		float currentTime = mission.CurrentTime;
		List<int> list = null;
		foreach (KeyValuePair<int, PendingSceneFollowCommand> pendingSceneFollowCommand in _pendingSceneFollowCommands)
		{
			PendingSceneFollowCommand value = pendingSceneFollowCommand.Value;
			if (value != null && !value.WaitForPlaybackFinished && value.ExecuteAtMissionTime >= 0f && currentTime >= value.ExecuteAtMissionTime)
			{
				if (list == null)
				{
					list = new List<int>();
				}
				list.Add(pendingSceneFollowCommand.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (int item in list)
		{
			FlushSceneFollowCommandAfterSpeech(item);
		}
	}

	internal void UpdateSceneFollowSpacing()
	{
		Mission mission = Mission.Current;
		if (mission?.Agents == null || Agent.Main == null || !Agent.Main.IsActive() || FollowAgentBehaviorIdleDistanceField == null)
		{
			return;
		}
		foreach (Agent agent in GetSceneCommandFollowerSnapshot(mission))
		{
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			try
			{
				FollowAgentBehavior followAgentBehavior = agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>()?.GetBehavior<FollowAgentBehavior>();
				if (followAgentBehavior != null)
				{
					FollowAgentBehaviorIdleDistanceField.SetValue(followAgentBehavior, SCENE_FOLLOW_MAX_IDLE_DISTANCE);
				}
			}
			catch
			{
			}
		}
	}

	internal void UpdateSceneFollowHostilityState()
	{
		Mission mission = Mission.Current;
		if (mission?.Agents == null)
		{
			return;
		}
		bool cacheInvalidated = false;
		foreach (Agent agent in GetSceneCommandFollowerSnapshot(mission))
		{
			if (!IsAgentHostileToMainAgent(agent))
			{
				continue;
			}
			try
			{
				_pendingSceneFollowCommands.Remove(agent.Index);
				_ports.CancelInteractionTimeoutArm(agent.Index);
				_ports.RemoveInteractionSession(agent.Index);
				_sceneFollowReturnStates.Remove(agent.Index);
				StopSceneSummonFollowPlayer(agent, restoreDailyBehaviors: false, invalidateFollowerCache: false);
				cacheInvalidated = true;
				RefreshHostileCombatAgentAutonomy(agent);
			}
			catch
			{
			}
		}
		if (cacheInvalidated)
		{
			InvalidateSceneCommandFollowerCache();
		}
	}
	internal void UpdateSceneCommandGhostMovement()
	{
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (mission?.Scene == null || agents == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			_sceneGhostWalkStates.Clear();
			return;
		}
		List<Agent> sceneCommandFollowers = GetSceneCommandFollowerSnapshot(mission);
		if (sceneCommandFollowers.Count == 0
			&& _activeSceneGuideRequests.Count == 0
			&& _activeSceneSummonRequests.Count == 0
			&& _activeSceneSummonConversationSessions.Count == 0
			&& _activeSceneReturnJobs.Count == 0)
		{
			_sceneGhostWalkStates.Clear();
			return;
		}
		Dictionary<int, Vec3> targets = BuildSceneGhostMovementTargets(sceneCommandFollowers);
		if (targets.Count == 0)
		{
			_sceneGhostWalkStates.Clear();
			return;
		}
		HashSet<int> activeAgentIndices = new HashSet<int>();
		foreach (KeyValuePair<int, Vec3> target in targets)
		{
			activeAgentIndices.Add(target.Key);
			Agent agent = agents.FirstOrDefault((Agent a) => a != null && a.Index == target.Key);
			ApplySceneGhostMovementIfStuck(agent, target.Value);
		}
		foreach (int agentIndex in _sceneGhostWalkStates.Keys.Where((int x) => !activeAgentIndices.Contains(x)).ToList())
		{
			_sceneGhostWalkStates.Remove(agentIndex);
		}
	}


    internal static bool HasSceneFollowCommandTag(string tags) => SceneFollowStartTagRegex.IsMatch(tags) || SceneFollowStopTagRegex.IsMatch(tags);

    internal void ClearTransientFollowers() => _transientSceneFollowAgentIndices.Clear();
    internal void ClearPendingSummonLaunches() { lock (_launchGate) _pendingSceneSummonLaunchQueues.Clear(); }
    internal bool HasGuideArrivalHold(int agentIndex) => _sceneGuideArrivalHolds.ContainsKey(agentIndex);
    internal bool HasPendingGuideReturn(int agentIndex) => _pendingSceneGuideReturnsAfterSpeech.ContainsKey(agentIndex);
    internal void CancelPendingSummonReturn(int agentIndex) => _pendingSceneSummonReturnsAfterSpeech.Remove(agentIndex);
    internal void SetPendingSummonReturn(int agentIndex, SceneSummonConversationSession session, bool returnOnlySpeaker)
    {
        EnsureCurrentMission();
        _pendingSceneSummonReturnsAfterSpeech[agentIndex] = new PendingSceneSummonReturnAfterSpeech
        { AgentIndex = agentIndex, Session = session, ReturnOnlySpeaker = returnOnlySpeaker };
    }

    private void EnsureCurrentMission()
    {
        Mission current = Mission.Current;
        if (ReferenceEquals(current, _mission)) return;
        Reset();
        _mission = current;
    }

    // Split only at the established host's non-movement speech-effect boundary;
    // both tick phases share this controller, mission generation and state.
    internal void TickScheduledReturns()
    {
        EnsureCurrentMission();
        UpdatePendingSceneSummonReturnsAfterSpeech();
        UpdatePendingSceneGuideReturnsAfterSpeech();
        UpdateSceneGuideArrivalHolds();
    }
    internal void TickActiveCommands()
    {
        EnsureCurrentMission();
        UpdatePendingSceneFollowCommands();
        UpdateSceneSummonConversationEscortMovement();
        UpdateSceneFollowHostilityState();
        UpdateSceneFollowSpacing();
        UpdateActiveSceneSummonRequests();
        UpdateActiveSceneGuideRequests();
        UpdateActiveSceneReturnJobs();
        UpdateSceneCommandGhostMovement();
    }

    internal void Reset()
    {
        Interlocked.Increment(ref _generation); // Retire callbacks before cleaning any game effect.
        foreach (ActiveSceneGuideRequest request in _activeSceneGuideRequests)
            if (request != null) request.Cancelled = true;
        // AgentIndex is scoped to its captured mission. Never clean a proxy by
        // looking up the same numeric index in a replacement mission.
        if (ReferenceEquals(_mission, Mission.Current))
        {
            foreach (ActiveSceneSummonRequest request in _activeSceneSummonRequests) CleanupSceneSummonDoorProxyAgent(request);
            foreach (ActiveSceneGuideRequest request in _activeSceneGuideRequests) CleanupSceneGuideDoorProxyAgent(request);
            foreach (SceneReturnJob job in _activeSceneReturnJobs) CleanupSceneReturnDoorProxyAgent(job);
        }
        _activeSceneSummonRequests.Clear();
        _activeSceneGuideRequests.Clear();
        _activeSceneSummonConversationSessions.Clear();
        _activeSceneSummonBatches.Clear();
        _activeSceneReturnJobs.Clear();
        _sceneSummonScriptedAgentIndices.Clear();
        lock (_launchGate)
        {
            _pendingSceneSummonLaunchQueues.Clear();
            _pendingSceneGuideLaunchQueues.Clear();
        }
        _pendingSceneSummonReturnsAfterSpeech.Clear();
        _pendingSceneFollowCommands.Clear();
        _pendingSceneGuideReturnsAfterSpeech.Clear();
        _sceneGuideArrivalHolds.Clear();
        _sceneFollowReturnStates.Clear();
        _transientSceneFollowAgentIndices.Clear();
        _sceneGhostWalkStates.Clear();
        InvalidateSceneCommandFollowerCache();
        _nextSceneSummonBatchId = 1;
        _mission = null;
    }

}
