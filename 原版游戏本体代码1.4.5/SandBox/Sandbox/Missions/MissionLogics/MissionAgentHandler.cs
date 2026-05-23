using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using SandBox.Objects;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Objects;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics;

public class MissionAgentHandler : MissionLogic
{
	private const float PassageUsageDeltaTime = 30f;

	private static readonly uint[] _tournamentTeamColors = new uint[11]
	{
		4294110933u, 4290269521u, 4291535494u, 4286151096u, 4290286497u, 4291600739u, 4291868275u, 4287285710u, 4283204487u, 4287282028u,
		4290300789u
	};

	private static readonly uint[] _villagerClothColors = new uint[35]
	{
		4292860590u, 4291351206u, 4289117081u, 4288460959u, 4287541416u, 4288922566u, 4292654718u, 4289243320u, 4290286483u, 4290288531u,
		4290156159u, 4291136871u, 4289233774u, 4291205980u, 4291735684u, 4292722283u, 4293119406u, 4293911751u, 4294110933u, 4291535494u,
		4289955192u, 4289631650u, 4292133587u, 4288785593u, 4286288275u, 4286222496u, 4287601851u, 4286622134u, 4285898909u, 4285638289u,
		4289830302u, 4287593853u, 4289957781u, 4287071646u, 4284445583u
	};

	private static int _disabledFaceId = -1;

	private static int _disabledFaceIdForAnimals = 1;

	private readonly Dictionary<string, List<UsableMachine>> _usablePoints;

	private readonly Dictionary<string, List<UsableMachine>> _pairedUsablePoints;

	private readonly HashSet<UsableMachine> _usedSpawnPoints;

	private List<UsableMachine> _disabledPassages;

	private readonly List<(LocationCharacter, MatrixFrame, GameEntity, bool, bool, Timer)> _spawnTimers = new List<(LocationCharacter, MatrixFrame, GameEntity, bool, bool, Timer)>();

	private float _passageUsageTime;

	public List<UsableMachine> TownPassageProps
	{
		get
		{
			_usablePoints.TryGetValue("npc_passage", out var value);
			return value;
		}
	}

	public List<UsableMachine> DisabledPassages => _disabledPassages;

	public List<UsableMachine> UsablePoints
	{
		get
		{
			List<UsableMachine> list = new List<UsableMachine>();
			foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
			{
				list.AddRange(usablePoint.Value);
			}
			foreach (KeyValuePair<string, List<UsableMachine>> pairedUsablePoint in _pairedUsablePoints)
			{
				list.AddRange(pairedUsablePoint.Value);
			}
			return list;
		}
	}

	public bool HasPassages()
	{
		if (_usablePoints.TryGetValue("npc_passage", out var value))
		{
			return value.Count > 0;
		}
		return false;
	}

	public MissionAgentHandler()
	{
		_usablePoints = new Dictionary<string, List<UsableMachine>>();
		_pairedUsablePoints = new Dictionary<string, List<UsableMachine>>();
		_usedSpawnPoints = new HashSet<UsableMachine>();
		_disabledPassages = new List<UsableMachine>();
	}

	public override void EarlyStart()
	{
		_passageUsageTime = base.Mission.CurrentTime + 30f;
		GetAllProps();
		MapWeatherModel.WeatherEvent weatherEventInPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEventInPosition(Settlement.CurrentSettlement.Position.ToVec2());
		if (weatherEventInPosition != MapWeatherModel.WeatherEvent.HeavyRain && weatherEventInPosition != MapWeatherModel.WeatherEvent.Blizzard)
		{
			InitializePairedUsableObjects();
		}
		base.Mission.SetReportStuckAgentsMode(value: true);
	}

	public override void OnRenderingStarted()
	{
	}

	public override void OnMissionTick(float dt)
	{
		float currentTime = base.Mission.CurrentTime;
		if (currentTime > _passageUsageTime)
		{
			_passageUsageTime = currentTime + 30f;
			if (PlayerEncounter.LocationEncounter != null && LocationComplex.Current != null)
			{
				LocationComplex.Current.AgentPassageUsageTick();
			}
		}
		for (int num = _spawnTimers.Count - 1; num >= 0; num--)
		{
			if (_spawnTimers[num].Item6.Check(currentTime))
			{
				SpawnWanderingAgentWithInitialFrame(_spawnTimers[num].Item1, _spawnTimers[num].Item2, _spawnTimers[num].Item3.WeakEntity, _spawnTimers[num].Item4, _spawnTimers[num].Item5);
				_spawnTimers.RemoveAt(num);
			}
		}
	}

	protected override void OnEndMission()
	{
		_usablePoints.Clear();
		_pairedUsablePoints.Clear();
		_disabledPassages.Clear();
		_usedSpawnPoints.Clear();
	}

	public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
	{
		if (atStart || (base.Mission.Mode != MissionMode.Battle && oldMissionMode != MissionMode.Battle))
		{
			return;
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman && !agent.IsPlayerControlled)
			{
				agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, agent.CurrentWatchState != Agent.WatchState.Alarmed);
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			agent.GetComponent<CampaignAgentComponent>()?.OnAgentRemoved(affectedAgent);
		}
	}

	private void InitializePairedUsableObjects()
	{
		Dictionary<string, List<UsableMachine>> dictionary = new Dictionary<string, List<UsableMachine>>();
		foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
		{
			foreach (UsableMachine item in usablePoint.Value)
			{
				foreach (StandingPoint standingPoint in item.StandingPoints)
				{
					if (!(standingPoint is AnimationPoint animationPoint) || !(animationPoint.PairEntity != null))
					{
						continue;
					}
					if (_pairedUsablePoints.ContainsKey(usablePoint.Key))
					{
						if (!_pairedUsablePoints[usablePoint.Key].Contains(item))
						{
							_pairedUsablePoints[usablePoint.Key].Add(item);
						}
					}
					else
					{
						_pairedUsablePoints.Add(usablePoint.Key, new List<UsableMachine> { item });
					}
					if (dictionary.ContainsKey(usablePoint.Key))
					{
						dictionary[usablePoint.Key].Add(item);
						continue;
					}
					dictionary.Add(usablePoint.Key, new List<UsableMachine> { item });
				}
			}
		}
		foreach (KeyValuePair<string, List<UsableMachine>> item2 in dictionary)
		{
			foreach (KeyValuePair<string, List<UsableMachine>> usablePoint2 in _usablePoints)
			{
				foreach (UsableMachine item3 in dictionary[item2.Key])
				{
					usablePoint2.Value.Remove(item3);
				}
			}
		}
	}

	private void GetAllProps()
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("navigation_mesh_deactivator");
		if (gameEntity != null)
		{
			NavigationMeshDeactivator firstScriptOfType = gameEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();
			_disabledFaceId = firstScriptOfType.DisableFaceWithId;
			_disabledFaceIdForAnimals = firstScriptOfType.DisableFaceWithIdForAnimals;
		}
		_usablePoints.Clear();
		foreach (UsableMachine item in base.Mission.MissionObjects.FindAllWithType<UsableMachine>())
		{
			string[] tags = item.GameEntity.Tags;
			foreach (string text in tags)
			{
				if (!_usablePoints.ContainsKey(text))
				{
					_usablePoints.Add(text, new List<UsableMachine>());
				}
				if (text != "sp_guard" || !item.GameEntity.HasTag("sp_guard_with_spear"))
				{
					_usablePoints[text].Add(item);
				}
			}
		}
		if (Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsTown || Settlement.CurrentSettlement.IsVillage))
		{
			foreach (AreaMarker item2 in base.Mission.ActiveMissionObjects.FindAllWithType<AreaMarker>().ToList())
			{
				string tag = item2.Tag;
				List<UsableMachine> usableMachinesInRange = item2.GetUsableMachinesInRange(item2.Tag.Contains("workshop") ? "unaffected_by_area" : null);
				if (!_usablePoints.ContainsKey(tag))
				{
					_usablePoints.Add(tag, new List<UsableMachine>());
				}
				foreach (UsableMachine item3 in usableMachinesInRange)
				{
					foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
					{
						if (usablePoint.Value.Contains(item3))
						{
							usablePoint.Value.Remove(item3);
						}
					}
					if (item3.GameEntity.HasTag("hold_tag_always"))
					{
						string text2 = item3.GameEntity.Tags[0] + "_" + item2.Tag;
						item3.GameEntity.AddTag(text2);
						if (!_usablePoints.ContainsKey(text2))
						{
							_usablePoints.Add(text2, new List<UsableMachine>());
							_usablePoints[text2].Add(item3);
						}
						else
						{
							_usablePoints[text2].Add(item3);
						}
						continue;
					}
					foreach (UsableMachine item4 in usableMachinesInRange)
					{
						if (!item4.GameEntity.HasTag(tag))
						{
							item4.GameEntity.AddTag(tag);
						}
					}
				}
				if (_usablePoints.ContainsKey(tag))
				{
					usableMachinesInRange.RemoveAll((UsableMachine x) => _usablePoints[tag].Contains(x));
					if (usableMachinesInRange.Count > 0)
					{
						_usablePoints[tag].AddRange(usableMachinesInRange);
					}
				}
				foreach (UsableMachine item5 in item2.GetUsableMachinesWithTagInRange("unaffected_by_area"))
				{
					string key = item5.GameEntity.Tags[0];
					foreach (KeyValuePair<string, List<UsableMachine>> usablePoint2 in _usablePoints)
					{
						if (usablePoint2.Value.Contains(item5))
						{
							usablePoint2.Value.Remove(item5);
						}
					}
					if (_usablePoints.ContainsKey(key))
					{
						_usablePoints[key].Add(item5);
						continue;
					}
					_usablePoints.Add(key, new List<UsableMachine>());
					_usablePoints[key].Add(item5);
				}
			}
		}
		List<GameEntity> entities = new List<GameEntity>();
		base.Mission.Scene.GetAllEntitiesWithScriptComponent<DynamicPatrolAreaParent>(ref entities);
		foreach (GameEntity item6 in entities)
		{
			foreach (GameEntity child in item6.GetChildren())
			{
				PatrolPoint firstScriptOfType2 = child.GetChild(0).GetFirstScriptOfType<PatrolPoint>();
				if (firstScriptOfType2 != null && !firstScriptOfType2.IsDisabled && !string.IsNullOrEmpty(firstScriptOfType2.SpawnGroupTag))
				{
					if (_usablePoints.ContainsKey(firstScriptOfType2.SpawnGroupTag))
					{
						_usablePoints[firstScriptOfType2.SpawnGroupTag].Add(firstScriptOfType2.GameEntity.Parent.GetFirstScriptOfType<UsablePlace>());
						continue;
					}
					_usablePoints.Add(firstScriptOfType2.SpawnGroupTag, new List<UsableMachine>());
					_usablePoints[firstScriptOfType2.SpawnGroupTag].Add(firstScriptOfType2.GameEntity.Parent.GetFirstScriptOfType<UsablePlace>());
				}
			}
		}
		DisableUnavailableWaypoints();
		RemoveDeactivatedUsablePlacesFromList();
	}

	[Conditional("DEBUG")]
	public void DetectMissingEntities()
	{
		if (CampaignMission.Current.Location == null || Utilities.CommandLineArgumentExists("CampaignGameplayTest"))
		{
			return;
		}
		IEnumerable<LocationCharacter> characterList = CampaignMission.Current.Location.GetCharacterList();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (LocationCharacter item in characterList)
		{
			if (item.SpecialTargetTag != null)
			{
				if (dictionary.ContainsKey(item.SpecialTargetTag))
				{
					dictionary[item.SpecialTargetTag]++;
				}
				else
				{
					dictionary.Add(item.SpecialTargetTag, 1);
				}
			}
		}
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			string key = item2.Key;
			int value = item2.Value;
			int num = 0;
			if (_usablePoints.TryGetValue(key, out var value2))
			{
				num += value2.Count;
				foreach (UsableMachine item3 in value2)
				{
					num += GetPointCountOfUsableMachine(item3, checkForUnusedOnes: false);
				}
			}
			if (_pairedUsablePoints.TryGetValue(key, out var value3))
			{
				num += value3.Count;
				foreach (UsableMachine item4 in value3)
				{
					num += GetPointCountOfUsableMachine(item4, checkForUnusedOnes: false);
				}
			}
			if (num < value)
			{
				_ = "Trying to spawn " + value + " npc with \"" + key + "\" but there are " + num + " suitable spawn points in scene " + base.Mission.SceneName;
				if (TestCommonBase.BaseInstance != null)
				{
					_ = TestCommonBase.BaseInstance.IsTestEnabled;
				}
			}
		}
	}

	private void RemoveDeactivatedUsablePlacesFromList()
	{
		Dictionary<string, List<UsableMachine>> dictionary = new Dictionary<string, List<UsableMachine>>();
		foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
		{
			foreach (UsableMachine item in usablePoint.Value)
			{
				if (item.IsDeactivated)
				{
					if (dictionary.ContainsKey(usablePoint.Key))
					{
						dictionary[usablePoint.Key].Add(item);
						continue;
					}
					dictionary.Add(usablePoint.Key, new List<UsableMachine>());
					dictionary[usablePoint.Key].Add(item);
				}
			}
		}
		foreach (KeyValuePair<string, List<UsableMachine>> item2 in dictionary)
		{
			foreach (UsableMachine item3 in item2.Value)
			{
				_usablePoints[item2.Key].Remove(item3);
			}
		}
	}

	public Dictionary<string, int> FindUnusedUsablePointCount()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
		{
			int num = 0;
			foreach (UsableMachine item in usablePoint.Value)
			{
				if (!_usedSpawnPoints.Contains(item))
				{
					num += GetPointCountOfUsableMachine(item, checkForUnusedOnes: true);
				}
			}
			if (num > 0)
			{
				dictionary.Add(usablePoint.Key, num);
			}
		}
		foreach (KeyValuePair<string, List<UsableMachine>> pairedUsablePoint in _pairedUsablePoints)
		{
			int num2 = 0;
			foreach (UsableMachine item2 in pairedUsablePoint.Value)
			{
				if (!_usedSpawnPoints.Contains(item2))
				{
					num2 += GetPointCountOfUsableMachine(item2, checkForUnusedOnes: true);
				}
			}
			if (num2 > 0)
			{
				if (!dictionary.ContainsKey(pairedUsablePoint.Key))
				{
					dictionary.Add(pairedUsablePoint.Key, num2);
				}
				else
				{
					dictionary[pairedUsablePoint.Key] += num2;
				}
			}
		}
		return dictionary;
	}

	private void DisableUnavailableWaypoints()
	{
		bool isNight = Campaign.Current.IsNight;
		string text = "";
		int num = 0;
		foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
		{
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			for (int i = 0; i < usablePoint.Value.Count; i++)
			{
				UsableMachine usableMachine = usablePoint.Value[i];
				if (!Mission.Current.IsPositionInsideBoundaries(usableMachine.GameEntity.GlobalPosition.AsVec2))
				{
					foreach (StandingPoint standingPoint in usableMachine.StandingPoints)
					{
						standingPoint.IsDeactivated = true;
						num++;
					}
				}
				if (usableMachine is Chair)
				{
					foreach (StandingPoint standingPoint2 in usableMachine.StandingPoints)
					{
						Vec3 origin = standingPoint2.GameEntity.GetGlobalFrame().origin;
						PathFaceRecord record = PathFaceRecord.NullFaceRecord;
						base.Mission.Scene.GetNavMeshFaceIndex(ref record, origin, checkIfDisabled: true);
						if (!record.IsValid() || (_disabledFaceId != -1 && record.FaceGroupIndex == _disabledFaceId))
						{
							standingPoint2.IsDeactivated = true;
							num2++;
						}
					}
				}
				else if (usableMachine is Passage)
				{
					Passage passage = usableMachine as Passage;
					if (passage.ToLocation != null && passage.ToLocation.CanPlayerSee())
					{
						continue;
					}
					foreach (StandingPoint standingPoint3 in passage.StandingPoints)
					{
						standingPoint3.IsDeactivated = true;
					}
					passage.Disable();
					_disabledPassages.Add(usableMachine);
					_ = passage.ToLocation;
					usablePoint.Value.RemoveAt(i);
					i--;
					num3++;
				}
				else
				{
					if (!(usableMachine is UsablePlace))
					{
						continue;
					}
					foreach (StandingPoint standingPoint4 in usableMachine.StandingPoints)
					{
						Vec3 origin2 = standingPoint4.GameEntity.GetGlobalFrame().origin;
						PathFaceRecord record2 = PathFaceRecord.NullFaceRecord;
						base.Mission.Scene.GetNavMeshFaceIndex(ref record2, origin2, checkIfDisabled: true);
						if (!record2.IsValid() || (_disabledFaceId != -1 && record2.FaceGroupIndex == _disabledFaceId) || (isNight && usableMachine.GameEntity.HasTag("disable_at_night")) || (!isNight && usableMachine.GameEntity.HasTag("enable_at_night")))
						{
							standingPoint4.IsDeactivated = true;
							num4++;
						}
					}
				}
			}
			if (num4 + num2 + num3 > 0)
			{
				text = text + "_____________________________________________\n\"" + usablePoint.Key + "\" :\n";
				if (num4 > 0)
				{
					text = text + "Disabled standing point : " + num4 + "\n";
				}
				if (num2 > 0)
				{
					text = text + "Disabled chair use point : " + num2 + "\n";
				}
				if (num3 > 0)
				{
					text = text + "Disabled passage info : " + num3 + "\n";
				}
			}
		}
	}

	public void SpawnLocationCharacters(string overridenTagValue = null)
	{
		CampaignEventDispatcher.Instance.LocationCharactersAreReadyToSpawn(FindUnusedUsablePointCount());
		foreach (LocationCharacter character in CampaignMission.Current.Location.GetCharacterList())
		{
			if (!IsAlreadySpawned(character.AgentOrigin))
			{
				if (!string.IsNullOrEmpty(overridenTagValue))
				{
					character.SpecialTargetTag = overridenTagValue;
				}
				SpawnDefaultLocationCharacter(character)?.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, isExcluded: true);
			}
		}
		List<Passage> list = new List<Passage>();
		if (TownPassageProps != null)
		{
			foreach (UsableMachine townPassageProp in TownPassageProps)
			{
				if (townPassageProp is Passage passage && !townPassageProp.IsDeactivated)
				{
					passage.Deactivate();
					list.Add(passage);
				}
			}
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			SimulateAgent(agent);
		}
		foreach (Passage item in list)
		{
			item.Activate();
		}
		CampaignEventDispatcher.Instance.LocationCharactersSimulated();
	}

	private bool IsAlreadySpawned(IAgentOriginBase agentOrigin)
	{
		if (Mission.Current != null)
		{
			return Mission.Current.Agents.Any((Agent x) => x.Origin == agentOrigin);
		}
		return false;
	}

	public Agent SpawnDefaultLocationCharacter(LocationCharacter locationCharacter, bool simulateAgentAfterSpawn = false)
	{
		Agent agent = SpawnWanderingAgent(locationCharacter);
		if (agent != null)
		{
			if (simulateAgentAfterSpawn)
			{
				SimulateAgent(agent);
			}
			if (locationCharacter.IsVisualTracked)
			{
				Mission.Current.GetMissionBehavior<VisualTrackerMissionBehavior>()?.RegisterLocalOnlyObject(agent);
			}
		}
		return agent;
	}

	public void SimulateAgent(Agent agent)
	{
		if (!agent.IsHuman)
		{
			return;
		}
		AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().AgentNavigator;
		int num = MBRandom.RandomInt(35, 50);
		agent.PreloadForRendering();
		for (int i = 0; i < num; i++)
		{
			agentNavigator?.Tick(0.1f, isSimulation: true);
			if (agent.IsUsingGameObject)
			{
				agent.CurrentlyUsedGameObject.SimulateTick(0.1f);
			}
		}
	}

	private void GetFrameForFollowingAgent(Agent followedAgent, out MatrixFrame frame)
	{
		frame = followedAgent.Frame;
		frame.origin += -(frame.rotation.f * 1.5f);
	}

	public void FadeoutExitingLocationCharacter(LocationCharacter locationCharacter)
	{
		if (base.Mission.CurrentState == Mission.State.EndingNextFrame || base.Mission.CurrentState == Mission.State.Over)
		{
			return;
		}
		foreach (Agent agent in Mission.Current.Agents)
		{
			if ((CharacterObject)agent.Character == locationCharacter.Character)
			{
				agent.FadeOut(hideInstantly: false, hideMount: true);
				break;
			}
		}
	}

	public void SpawnEnteringLocationCharacter(LocationCharacter locationCharacter, Location fromLocation)
	{
		if (fromLocation != null)
		{
			bool flag = false;
			{
				foreach (UsableMachine townPassageProp in TownPassageProps)
				{
					Passage passage = townPassageProp as Passage;
					if (passage.ToLocation == fromLocation)
					{
						MatrixFrame globalFrame = passage.PilotStandingPoint.GameEntity.GetGlobalFrame();
						globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
						globalFrame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(globalFrame.origin);
						Vec3 f = globalFrame.rotation.f;
						f.Normalize();
						globalFrame.origin -= 0.3f * f;
						globalFrame.rotation.RotateAboutUp(System.MathF.PI);
						bool hasTorch = townPassageProp.GameEntity.HasTag("torch");
						Agent agent = SpawnWanderingAgentWithInitialFrame(locationCharacter, globalFrame, passage.PilotStandingPoint.GameEntity, noHorses: true, hasTorch);
						agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, isExcluded: true);
						base.Mission.MakeSound(MiscSoundContainer.SoundCodeMovementFoleyDoorClose, globalFrame.origin, soundCanBePredicted: true, isReliable: false, -1, -1);
						agent.FadeIn();
						flag = true;
						break;
					}
				}
				return;
			}
		}
		SpawnDefaultLocationCharacter(locationCharacter, simulateAgentAfterSpawn: true);
	}

	private void SetUsablePlaceUsed(string spawnTag, GameEntity gameEntity)
	{
		foreach (UsableMachine item in GetAllUsablePointsWithTag(spawnTag))
		{
			if (!_usedSpawnPoints.Contains(item) && item.GameEntity == gameEntity)
			{
				_usedSpawnPoints.Add(item);
			}
		}
	}

	private bool GetInitialFrameForSpawnTag(string spawnTag, ref WeakGameEntity spawnedOnGameEntity, ref MatrixFrame frame)
	{
		List<UsableMachine> allUsablePointsWithTag = GetAllUsablePointsWithTag(spawnTag);
		if (allUsablePointsWithTag.Count > 0)
		{
			foreach (UsableMachine item in allUsablePointsWithTag)
			{
				if (!_usedSpawnPoints.Contains(item) && GetSpawnFrameFromUsableMachine(item, out var frame2))
				{
					frame = frame2;
					spawnedOnGameEntity = item.GameEntity;
					_usedSpawnPoints.Add(item);
					return true;
				}
			}
		}
		return false;
	}

	public bool HasUsablePointWithTag(string tag)
	{
		if (!_usablePoints.ContainsKey(tag))
		{
			return _pairedUsablePoints.ContainsKey(tag);
		}
		return true;
	}

	public IEnumerable<string> GetAllSpawnTags()
	{
		return _usablePoints.Keys.ToList().Concat(_pairedUsablePoints.Keys.ToList());
	}

	public List<UsableMachine> GetAllUsablePointsWithTag(string tag)
	{
		List<UsableMachine> list = new List<UsableMachine>();
		List<UsableMachine> value = new List<UsableMachine>();
		if (_usablePoints.TryGetValue(tag, out value))
		{
			list.AddRange(value);
		}
		List<UsableMachine> value2 = new List<UsableMachine>();
		if (_pairedUsablePoints.TryGetValue(tag, out value2))
		{
			list.AddRange(value2);
		}
		return list;
	}

	public Agent SpawnWanderingAgent(LocationCharacter locationCharacter)
	{
		WeakGameEntity spawnedOnGameEntity = WeakGameEntity.Invalid;
		bool flag = false;
		MatrixFrame frame = MatrixFrame.Identity;
		if (locationCharacter.SpecialTargetTag != null)
		{
			flag = GetInitialFrameForSpawnTag(locationCharacter.SpecialTargetTag, ref spawnedOnGameEntity, ref frame);
		}
		if (!locationCharacter.ForceSpawnInSpecialTargetTag)
		{
			if (!flag)
			{
				flag = GetInitialFrameForSpawnTag("npc_common_limited", ref spawnedOnGameEntity, ref frame);
			}
			if (!flag)
			{
				flag = GetInitialFrameForSpawnTag("npc_common", ref spawnedOnGameEntity, ref frame);
			}
			if (!flag && _usablePoints.Count > 0)
			{
				foreach (KeyValuePair<string, List<UsableMachine>> usablePoint in _usablePoints)
				{
					if (usablePoint.Value.Count <= 0)
					{
						continue;
					}
					foreach (UsableMachine item in usablePoint.Value)
					{
						if (GetSpawnFrameFromUsableMachine(item, out var frame2))
						{
							frame = frame2;
							flag = true;
							spawnedOnGameEntity = item.GameEntity;
							break;
						}
					}
				}
			}
			if (!flag && _pairedUsablePoints.Count > 0)
			{
				foreach (KeyValuePair<string, List<UsableMachine>> pairedUsablePoint in _pairedUsablePoints)
				{
					if (pairedUsablePoint.Value.Count <= 0)
					{
						continue;
					}
					foreach (UsableMachine item2 in pairedUsablePoint.Value)
					{
						if (GetSpawnFrameFromUsableMachine(item2, out var frame3))
						{
							frame = frame3;
							flag = true;
							spawnedOnGameEntity = item2.GameEntity;
							break;
						}
					}
				}
			}
		}
		if (flag)
		{
			frame.rotation.f.z = 0f;
			frame.rotation.f.Normalize();
			frame.rotation.u = Vec3.Up;
			frame.rotation.s = Vec3.CrossProduct(frame.rotation.f, frame.rotation.u);
			frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			bool hasTorch = spawnedOnGameEntity.HasTag("torch") && !Campaign.Current.IsDay;
			Agent agent = SpawnWanderingAgentWithInitialFrame(locationCharacter, frame, spawnedOnGameEntity, noHorses: true, hasTorch);
			agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, isExcluded: true);
			return agent;
		}
		return null;
	}

	private bool GetSpawnFrameFromUsableMachine(UsableMachine usableMachine, out MatrixFrame frame)
	{
		frame = MatrixFrame.Identity;
		StandingPoint randomElementWithPredicate = usableMachine.StandingPoints.GetRandomElementWithPredicate((StandingPoint x) => !x.HasUser && !x.IsDeactivated && !x.IsDisabled);
		if (randomElementWithPredicate != null)
		{
			frame = randomElementWithPredicate.GameEntity.GetGlobalFrame();
			return true;
		}
		return false;
	}

	public void SpawnWanderingAgentWithDelay(LocationCharacter locationCharacter, MatrixFrame matrixFrame, GameEntity spawnEntity, bool noHorses = true, bool hasTorch = false, float delay = 3f)
	{
		if (delay > 0f)
		{
			_spawnTimers.Add((locationCharacter, matrixFrame, spawnEntity, noHorses, hasTorch, new Timer(base.Mission.CurrentTime, delay, autoReset: false)));
		}
		else
		{
			TaleWorlds.Library.Debug.FailedAssert("delay > 0", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\MissionAgentHandler.cs", "SpawnWanderingAgentWithDelay", 1035);
		}
	}

	public Agent SpawnWanderingAgentWithInitialFrame(LocationCharacter locationCharacter, MatrixFrame spawnPointFrame, WeakGameEntity spawnEntity, bool noHorses = true, bool hasTorch = false)
	{
		Team team = Team.Invalid;
		switch (locationCharacter.CharacterRelation)
		{
		case LocationCharacter.CharacterRelations.Neutral:
			team = Team.Invalid;
			break;
		case LocationCharacter.CharacterRelations.Friendly:
			team = base.Mission.PlayerAllyTeam;
			break;
		case LocationCharacter.CharacterRelations.Enemy:
			team = base.Mission.PlayerEnemyTeam;
			break;
		}
		spawnPointFrame.origin.z = base.Mission.Scene.GetGroundHeightAtPosition(spawnPointFrame.origin);
		(uint, uint) agentSettlementColors = GetAgentSettlementColors(locationCharacter);
		AgentBuildData agentBuildData = locationCharacter.GetAgentBuildData().Team(team).InitialPosition(in spawnPointFrame.origin)
			.InitialDirection(spawnPointFrame.rotation.f.AsVec2.Normalized())
			.ClothingColor1(agentSettlementColors.Item1)
			.ClothingColor2(agentSettlementColors.Item2)
			.CivilianEquipment(locationCharacter.UseCivilianEquipment)
			.NoHorses(noHorses)
			.Banner(locationCharacter.Character?.HeroObject?.Clan?.Banner);
		if (hasTorch)
		{
			Equipment equipment = locationCharacter.Character.Equipment.Clone();
			equipment[EquipmentIndex.ExtraWeaponSlot] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("torch"));
			agentBuildData = agentBuildData.Equipment(equipment);
		}
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, isExcluded: true);
		if (hasTorch)
		{
			agent.SpawnEquipment.GetInitialWeaponIndicesToEquip(out var _, out var offHandWeaponIndex, out var _);
			if (offHandWeaponIndex != EquipmentIndex.None)
			{
				agent.TryToWieldWeaponInSlot(offHandWeaponIndex, Agent.WeaponWieldActionType.InstantAfterPickUp, isWieldedOnSpawn: true);
			}
		}
		AnimationSystemData animationSystemData = agentBuildData.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSet(locationCharacter.ActionSetCode), locationCharacter.Character.GetStepSize(), hasClippingPlane: false);
		agent.SetActionSet(ref animationSystemData);
		agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator(locationCharacter);
		locationCharacter.AddBehaviors(agent);
		locationCharacter.AfterAgentCreated?.Invoke(agent);
		Game.Current.EventManager.TriggerEvent(new LocationCharacterAgentSpawnedMissionEvent(locationCharacter, agent, spawnEntity));
		return agent;
	}

	public static uint GetRandomTournamentTeamColor(int teamIndex)
	{
		return _tournamentTeamColors[teamIndex % _tournamentTeamColors.Length];
	}

	public static (uint color1, uint color2) GetAgentSettlementColors(LocationCharacter locationCharacter)
	{
		CharacterObject character = locationCharacter.Character;
		if (character.IsHero)
		{
			if (character.HeroObject.Clan == CharacterObject.PlayerCharacter.HeroObject.Clan)
			{
				return (color1: Clan.PlayerClan.MapFaction.Color, color2: Clan.PlayerClan.MapFaction.Color2);
			}
			if (!character.HeroObject.IsNotable)
			{
				return (color1: locationCharacter.AgentData.AgentClothingColor1, color2: locationCharacter.AgentData.AgentClothingColor2);
			}
			return CharacterHelper.GetDeterministicColorsForCharacter(character, character.HeroObject.PartyBelongedTo?.Party);
		}
		if (character.IsSoldier)
		{
			return (color1: Settlement.CurrentSettlement.MapFaction.Color, color2: Settlement.CurrentSettlement.MapFaction.Color2);
		}
		return (color1: _villagerClothColors[MBRandom.RandomInt(_villagerClothColors.Length)], color2: _villagerClothColors[MBRandom.RandomInt(_villagerClothColors.Length)]);
	}

	public UsableMachine FindUnusedPointWithTagForAgent(Agent agent, string tag)
	{
		UsableMachine usableMachine = FindUnusedPointForAgent(agent, _pairedUsablePoints, tag);
		if (usableMachine == null || usableMachine.StandingPoints.Any((StandingPoint x) => x.HasUser && x.UserAgent == agent))
		{
			usableMachine = FindUnusedPointForAgent(agent, _usablePoints, tag);
		}
		return usableMachine;
	}

	public List<UsableMachine> FindUnusedPoints(string tag)
	{
		if (_usablePoints.TryGetValue(tag, out var value))
		{
			return value;
		}
		return null;
	}

	private UsableMachine FindUnusedPointForAgent(Agent agent, Dictionary<string, List<UsableMachine>> usableMachinesList, string primaryTag)
	{
		if (usableMachinesList.TryGetValue(primaryTag, out var value) && value.Count > 0)
		{
			int num = MBRandom.RandomInt(0, value.Count);
			for (int i = 0; i < value.Count; i++)
			{
				UsableMachine usableMachine = value[(num + i) % value.Count];
				if (!usableMachine.IsDisabled && !usableMachine.IsDestroyed && usableMachine.IsStandingPointAvailableForAgent(agent))
				{
					return usableMachine;
				}
			}
		}
		return null;
	}

	public List<UsableMachine> FindAllUnusedPoints(Agent agent, string primaryTag)
	{
		List<UsableMachine> list = new List<UsableMachine>();
		List<UsableMachine> list2 = new List<UsableMachine>();
		_usablePoints.TryGetValue(primaryTag, out var value);
		_pairedUsablePoints.TryGetValue(primaryTag, out var value2);
		value2 = value2?.Distinct().ToList();
		if (value != null && value.Count > 0)
		{
			list.AddRange(value);
		}
		if (value2 != null && value2.Count > 0)
		{
			list.AddRange(value2);
		}
		if (list.Count > 0)
		{
			foreach (UsableMachine item in list)
			{
				if (item.StandingPoints.Exists((StandingPoint sp) => (sp.IsInstantUse || (!sp.HasUser && !sp.HasAIMovingTo)) && !sp.IsDisabledForAgent(agent)))
				{
					list2.Add(item);
				}
			}
		}
		return list2;
	}

	public void TeleportTargetAgentNearReferenceAgent(Agent referenceAgent, Agent teleportAgent, bool teleportFollowers, bool teleportOpposite)
	{
		Vec3 vec = referenceAgent.Position + referenceAgent.LookDirection.NormalizedCopy() * 4f;
		Vec3 position;
		if (teleportOpposite)
		{
			position = vec;
			position.z = base.Mission.Scene.GetGroundHeightAtPosition(position);
		}
		else
		{
			position = Mission.Current.GetRandomPositionAroundPoint(referenceAgent.Position, 2f, 4f, nearFirst: true);
			position.z = base.Mission.Scene.GetGroundHeightAtPosition(position);
		}
		teleportAgent.LookDirection = new Vec3(new WorldFrame(referenceAgent.Frame.rotation, new WorldPosition(base.Mission.Scene, referenceAgent.Frame.origin)).Origin.AsVec2 - position.AsVec2).NormalizedCopy();
		teleportAgent.TeleportToPosition(position);
		if (!teleportFollowers || teleportAgent.Controller != AgentControllerType.Player)
		{
			return;
		}
		foreach (Agent agent in base.Mission.Agents)
		{
			LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(agent.Origin);
			AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter);
			if (agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null && accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart)
			{
				GetFrameForFollowingAgent(teleportAgent, out var frame);
				agent.TeleportToPosition(frame.origin);
			}
		}
	}

	public static int GetPointCountOfUsableMachine(UsableMachine usableMachine, bool checkForUnusedOnes)
	{
		int num = 0;
		List<AnimationPoint> list = new List<AnimationPoint>();
		foreach (StandingPoint standingPoint in usableMachine.StandingPoints)
		{
			if (standingPoint.IsDeactivated || standingPoint.IsDisabled || standingPoint.IsInstantUse || (checkForUnusedOnes && (standingPoint.HasUser || standingPoint.HasAIMovingTo)))
			{
				continue;
			}
			if (standingPoint is AnimationPoint { IsActive: not false } animationPoint)
			{
				List<AnimationPoint> alternatives = animationPoint.GetAlternatives();
				if (alternatives.Count == 0)
				{
					num++;
				}
				else if (!list.Contains(animationPoint) && (!checkForUnusedOnes || !alternatives.Any((AnimationPoint x) => x.HasUser && x.HasAIMovingTo)))
				{
					list.AddRange(alternatives);
					num++;
				}
			}
			else
			{
				num++;
			}
		}
		return num;
	}
}
