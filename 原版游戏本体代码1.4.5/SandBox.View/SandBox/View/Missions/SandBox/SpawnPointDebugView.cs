using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.AreaMarkers;
using SandBox.Objects.Usables;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;

namespace SandBox.View.Missions.SandBox;

public class SpawnPointDebugView : ScriptComponentBehavior
{
	private enum CategoryId
	{
		NPC,
		Animal,
		Chair,
		Passage,
		OutOfMissionBound,
		SemivalidChair
	}

	private struct InvalidPosition
	{
		public Vec3 position;

		public GameEntity entity;

		public bool isDisabledNavMesh;

		public bool doNotShowWarning;
	}

	private const string BattleSetName = "sp_battle_set";

	private const string CenterConversationPoint = "center_conversation_point";

	private const float AgentRadius = 0.3f;

	public static bool ActivateDebugUI;

	public bool ActivateDebugUIEditor;

	private readonly bool _separatorNeeded = true;

	private readonly bool _onSameLineNeeded = true;

	private bool _townCenterRadioButton;

	private bool _tavernRadioButton;

	private bool _arenaRadioButton;

	private bool _villageRadioButton;

	private bool _lordshallRadioButton;

	private bool _castleRadioButton;

	private bool _basicInformationTab;

	private bool _entityInformationTab;

	private bool _navigationMeshCheckTab;

	private bool _inaccessiblePositionCheckTab;

	private bool _relatedEntityWindow;

	private string _relatedPrefabTag;

	private bool _workshopAndAlleyConflictWindow;

	private string _problematicAreaMarkerWarningText;

	private int _cameraFocusIndex;

	private bool _showNPCs;

	private bool _showChairs;

	private bool _showAnimals;

	private bool _showSemiValidPoints;

	private bool _showPassagePoints;

	private bool _showOutOfBoundPoints;

	private bool _showPassagesList;

	private bool _showAnimalsList;

	private bool _showNPCsList;

	private bool _showDontUseList;

	private bool _showOthersList;

	private string _sceneName;

	private SpawnPointUnits.SceneType _sceneType;

	private readonly bool _normalButton;

	private int _currentTownsfolkCount;

	private Vec3 _redColor = new Vec3(200f, 0f, 0f, 255f);

	private Vec3 _greenColor = new Vec3(0f, 200f, 0f, 255f);

	private Vec3 _blueColor = new Vec3(0f, 180f, 180f, 255f);

	private Vec3 _yellowColor = new Vec3(200f, 200f, 0f, 255f);

	private Vec3 _purbleColor = new Vec3(255f, 0f, 255f, 255f);

	private uint _npcDebugLineColor = 4294901760u;

	private uint _chairDebugLineColor = 4278255360u;

	private uint _animalDebugLineColor = 4279356620u;

	private uint _semivalidChairDebugLineColor = 4294963200u;

	private uint _passageDebugLineColor = 4288217241u;

	private uint _missionBoundDebugLineColor = uint.MaxValue;

	private int _totalInvalidPoints;

	private int _currentInvalidPoints;

	private int _disabledFaceId;

	private int _particularfaceID;

	private Dictionary<CategoryId, List<InvalidPosition>> _invalidSpawnPointsDictionary = new Dictionary<CategoryId, List<InvalidPosition>>();

	private string allPrefabsWithParticularTag;

	private IList<SpawnPointUnits> _spUnitsList = new List<SpawnPointUnits>();

	private List<GameEntity> _allGameEntitiesWithAnimationScript = new List<GameEntity>();

	private List<GameEntity> _inaccessibleEntitiesList = new List<GameEntity>();

	private List<GameEntity> _closeEntitiesToInaccessible = new List<GameEntity>();

	private GameEntity _selectedEntity;

	private GameEntity _closeEntity;

	private PathFaceRecord _startPositionNavMesh;

	private PathFaceRecord _targetPositionNavMesh;

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		DetermineSceneType();
		AddSpawnPointsToList(alreadyInitialized: false);
	}

	protected override void OnInit()
	{
		base.OnInit();
		DetermineSceneType();
		AddSpawnPointsToList(alreadyInitialized: false);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		if (ActivateDebugUI || (MBEditor.IsEditModeOn && ActivateDebugUIEditor))
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected override void OnTick(float dt)
	{
		ToolMainFunction();
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		ToolMainFunction();
	}

	private void ToolMainFunction()
	{
		if (ActivateDebugUI || (MBEditor.IsEditModeOn && ActivateDebugUIEditor))
		{
			StartImGUIWindow("Mission Spawn Point Debugger");
			if (Mission.Current != null)
			{
				ImGUITextArea("- Do Not Hide The Mouse Cursor When Debug Window Is Intersecting With The Center Of The Screen!! -", _separatorNeeded, !_onSameLineNeeded);
			}
			if (ImGUIButton("Scene Basic Information Tab", _normalButton))
			{
				ChangeTab(basicInformationTab: true, entityInformationTab: false, navigationMeshCheckTab: false, navigationMeshCanWalkCheckTab: false);
			}
			LeaveSpaceBetweenTabs();
			if (ImGUIButton("Scene Entity Check Tab", _normalButton))
			{
				ChangeTab(basicInformationTab: false, entityInformationTab: true, navigationMeshCheckTab: false, navigationMeshCanWalkCheckTab: false);
			}
			LeaveSpaceBetweenTabs();
			if (ImGUIButton("Navigation Mesh Check Tab", _normalButton))
			{
				ChangeTab(basicInformationTab: false, entityInformationTab: false, navigationMeshCheckTab: true, navigationMeshCanWalkCheckTab: false);
			}
			if (ImGUIButton("Navigation Mesh Can Walkable Check Tab", _normalButton))
			{
				ChangeTab(basicInformationTab: false, entityInformationTab: false, navigationMeshCheckTab: false, navigationMeshCanWalkCheckTab: true);
			}
			if (_entityInformationTab)
			{
				ShowEntityInformationTab();
			}
			if (_basicInformationTab)
			{
				ShowBasicInformationTab();
			}
			if (_navigationMeshCheckTab)
			{
				ShowNavigationCheckTab();
			}
			if (_inaccessiblePositionCheckTab)
			{
				CheckInaccessiblePoint();
			}
			if (_relatedEntityWindow)
			{
				ShowRelatedEntity();
			}
			if (_workshopAndAlleyConflictWindow)
			{
				ShowWorkshopAndAlleyConflictWindow();
			}
			ImGUITextArea("If there are more than one 'SpawnPointDebugView' in the scene, please remove them.", _separatorNeeded, !_onSameLineNeeded);
			ImGUITextArea("If you have any questions about this tool feel free to ask Campaign team.", _separatorNeeded, !_onSameLineNeeded);
			EndImGUIWindow();
		}
	}

	private void ShowWorkshopAndAlleyConflictWindow()
	{
		StartImGUIWindow("Mission Spawn Point Debugger / Warning Window");
		ImGUITextArea(_problematicAreaMarkerWarningText, !_separatorNeeded, !_onSameLineNeeded);
		if (ImGUIButton("Close Tab", _normalButton))
		{
			_workshopAndAlleyConflictWindow = false;
			_problematicAreaMarkerWarningText = "";
		}
		EndImGUIWindow();
	}

	private void ShowRelatedEntity()
	{
		StartImGUIWindow("Mission Spawn Point Debugger / Entity Window");
		if (ImGUIButton("Close Tab", _normalButton))
		{
			_relatedEntityWindow = false;
		}
		ImGUITextArea("Please expand the window!", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Prefabs with '" + _relatedPrefabTag + "' tags are listed.", _separatorNeeded, !_onSameLineNeeded);
		FindAllPrefabsWithSelectedTag();
		EndImGUIWindow();
	}

	private void ShowBasicInformationTab()
	{
		ImGUITextArea("Tool tried to detect the scene type. If scene type is not correct or not determined", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("please select the scene type from toggle buttons below.", _separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea(string.Concat("Scene Type: ", _sceneType, " "), !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Scene Name: " + _sceneName + " ", !_separatorNeeded, !_onSameLineNeeded);
		HandleRadioButtons();
	}

	private void HandleRadioButtons()
	{
		if (ImGUIButton("Town Center", _townCenterRadioButton))
		{
			_sceneType = SpawnPointUnits.SceneType.Center;
			_townCenterRadioButton = false;
			_tavernRadioButton = false;
			_villageRadioButton = false;
			_arenaRadioButton = false;
			_lordshallRadioButton = false;
			_castleRadioButton = false;
			AddSpawnPointsToList(alreadyInitialized: true);
		}
		if (ImGUIButton("Tavern", _tavernRadioButton))
		{
			_sceneType = SpawnPointUnits.SceneType.Tavern;
			_tavernRadioButton = false;
			_townCenterRadioButton = false;
			_villageRadioButton = false;
			_arenaRadioButton = false;
			_lordshallRadioButton = false;
			_castleRadioButton = false;
			AddSpawnPointsToList(alreadyInitialized: true);
		}
		if (ImGUIButton("Village", _villageRadioButton))
		{
			_sceneType = SpawnPointUnits.SceneType.VillageCenter;
			_villageRadioButton = false;
			_townCenterRadioButton = false;
			_tavernRadioButton = false;
			_arenaRadioButton = false;
			_lordshallRadioButton = false;
			AddSpawnPointsToList(alreadyInitialized: true);
		}
		if (ImGUIButton("Arena", _arenaRadioButton))
		{
			_sceneType = SpawnPointUnits.SceneType.Arena;
			_arenaRadioButton = false;
			_townCenterRadioButton = false;
			_tavernRadioButton = false;
			_villageRadioButton = false;
			_lordshallRadioButton = false;
			_castleRadioButton = false;
			AddSpawnPointsToList(alreadyInitialized: true);
		}
		if (ImGUIButton("Lords Hall", _lordshallRadioButton))
		{
			_sceneType = SpawnPointUnits.SceneType.LordsHall;
			_lordshallRadioButton = false;
			_townCenterRadioButton = false;
			_tavernRadioButton = false;
			_villageRadioButton = false;
			_arenaRadioButton = false;
			_castleRadioButton = false;
			AddSpawnPointsToList(alreadyInitialized: true);
		}
		if (ImGUIButton("Castle", _castleRadioButton))
		{
			_sceneType = SpawnPointUnits.SceneType.Castle;
			_castleRadioButton = false;
			_lordshallRadioButton = false;
			_townCenterRadioButton = false;
			_tavernRadioButton = false;
			_villageRadioButton = false;
			_arenaRadioButton = false;
			AddSpawnPointsToList(alreadyInitialized: true);
		}
	}

	private void ChangeTab(bool basicInformationTab, bool entityInformationTab, bool navigationMeshCheckTab, bool navigationMeshCanWalkCheckTab)
	{
		_basicInformationTab = basicInformationTab;
		_entityInformationTab = entityInformationTab;
		_navigationMeshCheckTab = navigationMeshCheckTab;
		_inaccessiblePositionCheckTab = navigationMeshCanWalkCheckTab;
	}

	private void DetermineSceneType()
	{
		_sceneName = base.Scene.GetName();
		if (_sceneName.Contains("tavern"))
		{
			_sceneType = SpawnPointUnits.SceneType.Tavern;
		}
		else if (_sceneName.Contains("lords_hall") || (_sceneName.Contains("interior") && (_sceneName.Contains("lords_hall") || _sceneName.Contains("castle") || _sceneName.Contains("keep"))))
		{
			_sceneType = SpawnPointUnits.SceneType.LordsHall;
		}
		else if (_sceneName.Contains("village"))
		{
			_sceneType = SpawnPointUnits.SceneType.VillageCenter;
		}
		else if (_sceneName.Contains("town") || _sceneName.Contains("city"))
		{
			_sceneType = SpawnPointUnits.SceneType.Center;
		}
		else if (_sceneName.Contains("shipyard"))
		{
			_sceneType = SpawnPointUnits.SceneType.Shipyard;
		}
		else if (_sceneName.Contains("dungeon"))
		{
			_sceneType = SpawnPointUnits.SceneType.Dungeon;
		}
		else if (_sceneName.Contains("hippodrome") || _sceneName.Contains("arena"))
		{
			_sceneType = SpawnPointUnits.SceneType.Arena;
		}
		else if (_sceneName.Contains("castle") || _sceneName.Contains("siege"))
		{
			_sceneType = SpawnPointUnits.SceneType.Castle;
		}
		else if (_sceneName.Contains("interior"))
		{
			_sceneType = SpawnPointUnits.SceneType.EmptyShop;
		}
		else
		{
			_sceneType = SpawnPointUnits.SceneType.NotDetermined;
		}
	}

	private void AddSpawnPointsToList(bool alreadyInitialized)
	{
		_spUnitsList.Clear();
		if (_sceneType == SpawnPointUnits.SceneType.Center)
		{
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_player_outside", SpawnPointUnits.SceneType.Center, "npc", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("alley_1_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("alley_2_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("alley_3_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("center_conversation_point", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_1", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_2", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_3", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("workshop_area_1_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("workshop_area_2_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("workshop_area_3_population", SpawnPointUnits.SceneType.Center, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_workshop_1", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_workshop_2", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_workshop_3", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("workshop_1_notable_parent", SpawnPointUnits.SceneType.Center, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("workshop_2_notable_parent", SpawnPointUnits.SceneType.Center, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("workshop_3_notable_parent", SpawnPointUnits.SceneType.Center, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("navigation_mesh_deactivator", SpawnPointUnits.SceneType.Center, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("alley_marker", SpawnPointUnits.SceneType.Center, 3, 3));
			_spUnitsList.Add(new SpawnPointUnits("workshop_area_marker", SpawnPointUnits.SceneType.Center, 3, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_outside_near_town_main_gate", SpawnPointUnits.SceneType.Center, "npc", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_dancer", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_cleaner", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("npc_beggar", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_notable_artisan", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_notable_gangleader", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_notable_merchant", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_notable_preacher", SpawnPointUnits.SceneType.Center, "npc", 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_merchant", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
			_spUnitsList.Add(new SpawnPointUnits("sp_armorer", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
			_spUnitsList.Add(new SpawnPointUnits("sp_blacksmith", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
			_spUnitsList.Add(new SpawnPointUnits("sp_weaponsmith", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
			_spUnitsList.Add(new SpawnPointUnits("sp_horse_merchant", SpawnPointUnits.SceneType.Center, "npc", 1, 25));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.Center, "npc", 2, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_castle", SpawnPointUnits.SceneType.Center, "npc", 1, 2));
			_spUnitsList.Add(new SpawnPointUnits("sp_prison_guard", SpawnPointUnits.SceneType.Center, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_patrol", SpawnPointUnits.SceneType.Center, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_unarmed", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_tavern_wench", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_tavernkeeper", SpawnPointUnits.SceneType.Center, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_barber", SpawnPointUnits.SceneType.Center, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation", SpawnPointUnits.SceneType.Center, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_tavern", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_arena", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_lordshall", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_prison", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_house_1", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_house_2", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_house_3", SpawnPointUnits.SceneType.Center, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("desert_war_horse", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("steppe_charger", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("war_horse", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("charger", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("desert_horse", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("hunter", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_sheep", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_cow", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_hog", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_goose", SpawnPointUnits.SceneType.Center, "animal", 0, int.MaxValue));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.Shipyard)
		{
			_spUnitsList.Add(new SpawnPointUnits("navigation_mesh_deactivator", SpawnPointUnits.SceneType.Shipyard, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("sp_shipwright", SpawnPointUnits.SceneType.Shipyard, "npc", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("shipyard_worker", SpawnPointUnits.SceneType.Shipyard, "npc", 1, 1));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.Tavern)
		{
			_spUnitsList.Add(new SpawnPointUnits("musician", SpawnPointUnits.SceneType.Tavern, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_tavern_wench", SpawnPointUnits.SceneType.Tavern, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_tavernkeeper", SpawnPointUnits.SceneType.Tavern, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_mercenary", SpawnPointUnits.SceneType.Tavern, "npc", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("gambler_npc", SpawnPointUnits.SceneType.Tavern, "npc", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.Tavern, "passage", 1, 1));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.VillageCenter)
		{
			_spUnitsList.Add(new SpawnPointUnits("sp_notable", SpawnPointUnits.SceneType.VillageCenter, "notable", 6, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_notable_rural_notable", SpawnPointUnits.SceneType.VillageCenter, "npc", 6, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("alley_1_population", SpawnPointUnits.SceneType.VillageCenter, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("alley_2_population", SpawnPointUnits.SceneType.VillageCenter, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("alley_3_population", SpawnPointUnits.SceneType.VillageCenter, 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("center_conversation_point", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_1", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_2", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation_alley_3", SpawnPointUnits.SceneType.VillageCenter, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("alley_marker", SpawnPointUnits.SceneType.VillageCenter, 3, 3));
			_spUnitsList.Add(new SpawnPointUnits("battle_set", SpawnPointUnits.SceneType.VillageCenter, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("navigation_mesh_deactivator", SpawnPointUnits.SceneType.VillageCenter, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("desert_war_horse", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("steppe_charger", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("war_horse", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("charger", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("desert_horse", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("hunter", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_sheep", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_cow", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_hog", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_goose", SpawnPointUnits.SceneType.VillageCenter, "animal", 0, int.MaxValue));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.Arena)
		{
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_tournamentmaster", SpawnPointUnits.SceneType.Arena, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_near_arena_master", SpawnPointUnits.SceneType.Arena, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("tournament_archery", SpawnPointUnits.SceneType.Arena, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("tournament_fight", SpawnPointUnits.SceneType.Arena, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("tournament_jousting", SpawnPointUnits.SceneType.Arena, 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.Arena, "passage", 1, 1));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.LordsHall)
		{
			_spUnitsList.Add(new SpawnPointUnits("battle_set", SpawnPointUnits.SceneType.LordsHall, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.LordsHall, "npc", 2, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_notable", SpawnPointUnits.SceneType.LordsHall, "npc", 10, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_king", SpawnPointUnits.SceneType.LordsHall, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_throne", SpawnPointUnits.SceneType.LordsHall, "npc", 1, 2));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.LordsHall, "passage", 1, 1));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.Castle)
		{
			_spUnitsList.Add(new SpawnPointUnits("sp_prison_guard", SpawnPointUnits.SceneType.Castle, "npc", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.Castle, "npc", 2, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_castle", SpawnPointUnits.SceneType.Castle, "npc", 1, 2));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_patrol", SpawnPointUnits.SceneType.Castle, "npc", 1, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_unarmed", SpawnPointUnits.SceneType.Castle, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_lordshall", SpawnPointUnits.SceneType.Castle, "passage", 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("sp_player_conversation", SpawnPointUnits.SceneType.Castle, 1, int.MaxValue));
		}
		else if (_sceneType == SpawnPointUnits.SceneType.Dungeon)
		{
			_spUnitsList.Add(new SpawnPointUnits("sp_guard", SpawnPointUnits.SceneType.Dungeon, "npc", 2, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_patrol", SpawnPointUnits.SceneType.Dungeon, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_guard_unarmed", SpawnPointUnits.SceneType.Dungeon, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("npc_passage_center", SpawnPointUnits.SceneType.Castle, "passage", 1, 1));
		}
		if (!alreadyInitialized)
		{
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_player", SpawnPointUnits.SceneType.All, 1, 1));
			_spUnitsList.Add(new SpawnPointUnits("npc_common", SpawnPointUnits.SceneType.All, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("npc_common_limited", SpawnPointUnits.SceneType.All, "npc", 0, int.MaxValue));
			_spUnitsList.Add(new SpawnPointUnits("sp_npc", SpawnPointUnits.SceneType.All, "DONTUSE", 0, 0));
			_spUnitsList.Add(new SpawnPointUnits("spawnpoint_elder", SpawnPointUnits.SceneType.VillageCenter, "DONTUSE", 0, 0));
		}
		_invalidSpawnPointsDictionary.Clear();
		_invalidSpawnPointsDictionary.Add(CategoryId.NPC, new List<InvalidPosition>());
		_invalidSpawnPointsDictionary.Add(CategoryId.Animal, new List<InvalidPosition>());
		_invalidSpawnPointsDictionary.Add(CategoryId.Chair, new List<InvalidPosition>());
		_invalidSpawnPointsDictionary.Add(CategoryId.Passage, new List<InvalidPosition>());
		_invalidSpawnPointsDictionary.Add(CategoryId.SemivalidChair, new List<InvalidPosition>());
	}

	private List<List<string>> GetLevelCombinationsToCheck()
	{
		base.GameEntity.Scene.GetName();
		bool num = base.GameEntity.Scene.GetUpgradeLevelMaskOfLevelName("siege") != 0;
		bool flag = base.GameEntity.Scene.GetUpgradeLevelMaskOfLevelName("naval_raid") != 0;
		List<List<string>> list = new List<List<string>>();
		if (num)
		{
			list.Add(new List<string>());
			list[0].Add("level_1");
			list[0].Add("civilian");
			list.Add(new List<string>());
			list[1].Add("level_2");
			list[1].Add("civilian");
			list.Add(new List<string>());
			list[2].Add("level_3");
			list[2].Add("civilian");
		}
		else if (flag)
		{
			list.Add(new List<string>());
			list[0].Add("land_raid");
		}
		else
		{
			list.Add(new List<string>());
			list[0].Add("base");
		}
		return list;
	}

	protected override void OnSceneSave(string saveFolder)
	{
		base.OnSceneSave(saveFolder);
		OnCheckForProblems();
	}

	protected override bool OnCheckForProblems()
	{
		base.OnCheckForProblems();
		GetDisableFaceID();
		bool flag = false;
		if (_sceneType == SpawnPointUnits.SceneType.NotDetermined)
		{
			flag = true;
			MBEditor.AddEntityWarning(base.GameEntity, "Scene type could not be determined");
		}
		uint upgradeLevelMask = base.GameEntity.Scene.GetUpgradeLevelMask();
		foreach (List<string> item in GetLevelCombinationsToCheck())
		{
			string text = "";
			for (int i = 0; i < item.Count - 1; i++)
			{
				text = text + item[i] + "|";
			}
			text += item[item.Count - 1];
			base.GameEntity.Scene.SetUpgradeLevelVisibility(item);
			CountEntities();
			CheckInaccesiblePositions();
			if (_inaccessibleEntitiesList.Any())
			{
				foreach (GameEntity inaccessibleEntities in _inaccessibleEntitiesList)
				{
					string msg = ("There is an inaccessible position: " + inaccessibleEntities.GlobalPosition.ToString()) ?? "";
					MBEditor.AddEditorWarning("Level: " + string.Join(" - ", item));
					MBEditor.AddEntityWarning(inaccessibleEntities.WeakEntity, msg);
				}
			}
			foreach (SpawnPointUnits spUnits in _spUnitsList)
			{
				if (spUnits.Place == SpawnPointUnits.SceneType.All || _sceneType == spUnits.Place)
				{
					bool flag2 = spUnits.CurrentCount <= spUnits.MaxCount && spUnits.CurrentCount >= spUnits.MinCount;
					flag = flag || !flag2;
					if (!flag2)
					{
						string text2 = "Spawnpoint (" + spUnits.SpName + ") has some issues. ";
						MBEditor.AddEntityWarning(msg: (spUnits.MaxCount >= spUnits.CurrentCount) ? (text2 + "It is placed too less. Placed count(" + spUnits.CurrentCount + "). Min count(" + spUnits.MinCount + "). Level: " + text) : (text2 + "It is placed too much. Placed count(" + spUnits.CurrentCount + "). Max count(" + spUnits.MaxCount + "). Level: " + text), entityId: base.GameEntity);
					}
				}
			}
			if (!string.IsNullOrEmpty(_problematicAreaMarkerWarningText))
			{
				MBEditor.AddEntityWarning(base.GameEntity, _problematicAreaMarkerWarningText);
			}
		}
		base.GameEntity.Scene.SetUpgradeLevelVisibility(upgradeLevelMask);
		CheckForNavigationMesh();
		foreach (List<InvalidPosition> value in _invalidSpawnPointsDictionary.Values)
		{
			foreach (InvalidPosition item2 in value)
			{
				if (!item2.doNotShowWarning)
				{
					string text3 = "";
					MBEditor.AddEntityWarning(msg: (!item2.isDisabledNavMesh) ? ("Special entity with name (" + item2.entity.Name + ") has no navigation mesh below. Position " + item2.position.x + " , " + item2.position.y + " , " + item2.position.z + ".") : ("Special entity with name (" + item2.entity.Name + ") has a navigation mesh below which is deactivated by the deactivater script. Position " + item2.position.x + " , " + item2.position.y + " , " + item2.position.z + "."), entityId: item2.entity.WeakEntity);
					flag = true;
				}
			}
		}
		return flag;
	}

	private void ShowEntityInformationTab()
	{
		ImGUITextArea("This tab calculates the spawnpoint counts and warns you if", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("counts are not in the given criteria.", _separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Click 'Count Entities' button to calculate and toggle categories.", _separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("You can use the list button to list all the prefabs with tag.", _separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Current Townsfolk count: " + _currentTownsfolkCount, _separatorNeeded, !_onSameLineNeeded);
		ImGUICheckBox("NPCs ", ref _showNPCsList, !_separatorNeeded, _onSameLineNeeded);
		ImGUICheckBox("Animals ", ref _showAnimalsList, !_separatorNeeded, _onSameLineNeeded);
		ImGUICheckBox("Passages ", ref _showPassagesList, !_separatorNeeded, _onSameLineNeeded);
		ImGUICheckBox("Others ", ref _showOthersList, !_separatorNeeded, _onSameLineNeeded);
		ImGUICheckBox("DONT USE ", ref _showDontUseList, _separatorNeeded, !_onSameLineNeeded);
		WriteTableHeaders();
		foreach (SpawnPointUnits spUnits in _spUnitsList)
		{
			if (spUnits.Place == SpawnPointUnits.SceneType.All)
			{
				if (spUnits.CurrentCount > spUnits.MaxCount || spUnits.CurrentCount < spUnits.MinCount)
				{
					WriteLineOfTableDebug(spUnits, _redColor, spUnits.Type);
				}
				else
				{
					WriteLineOfTableDebug(spUnits, _greenColor, spUnits.Type);
				}
			}
			else if (_sceneType == spUnits.Place)
			{
				if (spUnits.CurrentCount > spUnits.MaxCount || spUnits.CurrentCount < spUnits.MinCount)
				{
					WriteLineOfTableDebug(spUnits, _redColor, spUnits.Type);
				}
				else
				{
					WriteLineOfTableDebug(spUnits, _greenColor, spUnits.Type);
				}
			}
		}
		if (ImGUIButton("COUNT ENTITIES", _normalButton))
		{
			CountEntities();
		}
	}

	private void CalculateSpawnedAgentCount(SpawnPointUnits spUnit)
	{
		if (spUnit.SpName == "npc_common")
		{
			spUnit.SpawnedAgentCount = (int)((float)spUnit.CurrentCount * 0.2f + 0.15f);
		}
		else if (spUnit.SpName == "npc_common_limited")
		{
			spUnit.SpawnedAgentCount = (int)((float)spUnit.CurrentCount * 0.15f + 0.1f);
		}
		else if (spUnit.SpName == "npc_beggar")
		{
			spUnit.SpawnedAgentCount = (int)((float)spUnit.CurrentCount * 0.33f);
		}
		else if (spUnit.SpName == "spawnpoint_cleaner" || spUnit.SpName == "npc_dancer" || spUnit.SpName == "sp_guard_patrol" || spUnit.SpName == "sp_guard")
		{
			spUnit.SpawnedAgentCount = spUnit.CurrentCount;
		}
		else if (spUnit.CurrentCount != 0)
		{
			spUnit.SpawnedAgentCount = 1;
		}
		_currentTownsfolkCount += spUnit.SpawnedAgentCount;
	}

	private void CountEntities()
	{
		GetDisableFaceID();
		_currentTownsfolkCount = 0;
		foreach (SpawnPointUnits item in _spUnitsList.ToList())
		{
			List<GameEntity> list = base.Scene.FindEntitiesWithTag(item.SpName).ToList();
			int num = 0;
			foreach (GameEntity item2 in list)
			{
				if (item2.GetEditModeLevelVisibility())
				{
					num++;
				}
			}
			item.CurrentCount = num;
			CalculateSpawnedAgentCount(item);
			CountPassages(item);
			foreach (GameEntity item3 in list)
			{
				if (item3.IsGhostObject())
				{
					item.CurrentCount--;
				}
			}
			if (item.SpName == "alley_marker")
			{
				CheckForCommonAreas(list, item);
			}
			else if (item.SpName == "workshop_area_marker")
			{
				CheckForWorkshops(list, item);
			}
			else
			{
				if (!(item.SpName == "center_conversation_point"))
				{
					continue;
				}
				List<GameEntity> list2 = base.Scene.FindEntitiesWithTag("sp_player_conversation").ToList();
				List<GameEntity> list3 = base.Scene.FindEntitiesWithTag("alley_marker").ToList();
				foreach (GameEntity item4 in list2)
				{
					bool flag = false;
					foreach (GameEntity item5 in list3)
					{
						if (item5.GetFirstScriptOfType<CommonAreaMarker>().IsPositionInRange(item4.GlobalPosition))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						SpawnPointUnits spawnPointUnits = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "center_conversation_point" && x.Place == _sceneType);
						if (spawnPointUnits != null)
						{
							spawnPointUnits.CurrentCount++;
						}
					}
				}
			}
		}
		IEnumerable<CommonAreaMarker> enumerable = from x in base.Scene.FindEntitiesWithTag("alley_marker")
			select x.GetFirstScriptOfType<CommonAreaMarker>();
		IEnumerable<WorkshopAreaMarker> enumerable2 = from x in base.Scene.FindEntitiesWithTag("workshop_area_marker")
			select x.GetFirstScriptOfType<WorkshopAreaMarker>();
		foreach (CommonAreaMarker item6 in enumerable)
		{
			foreach (WorkshopAreaMarker item7 in enumerable2)
			{
				if (item6.GetPosition().Distance(item7.GetPosition()) < item6.AreaRadius + item7.AreaRadius)
				{
					_workshopAndAlleyConflictWindow = true;
					_problematicAreaMarkerWarningText = string.Concat("The areas of Alley Marker at position:\n", item6.GetPosition(), "\nand Workshop Marker at position:\n", item7.GetPosition(), "\nintersects! \nPlease move one of them or\ndecrease their radius accordingly!");
					break;
				}
			}
		}
	}

	private void CheckForCommonAreas(IEnumerable<GameEntity> allGameEntitiesWithGivenTag, SpawnPointUnits spUnit)
	{
		foreach (GameEntity item in allGameEntitiesWithGivenTag)
		{
			CommonAreaMarker alleyMarker = item.GetFirstScriptOfType<CommonAreaMarker>();
			if (alleyMarker == null || item.IsGhostObject())
			{
				continue;
			}
			float areaRadius = alleyMarker.AreaRadius;
			List<GameEntity> list = base.Scene.FindEntitiesWithTag("npc_common").ToList();
			foreach (GameEntity item2 in list.ToList())
			{
				float num = areaRadius * areaRadius;
				if (item2.HasScriptOfType<Passage>() || !item2.IsVisibleIncludeParents() || item2.GlobalPosition.DistanceSquared(item.GlobalPosition) > num)
				{
					list.Remove(item2);
				}
			}
			List<GameEntity> list2 = base.Scene.FindEntitiesWithTag("sp_player_conversation").ToList();
			int num2 = 0;
			foreach (GameEntity item3 in list2)
			{
				if (alleyMarker.IsPositionInRange(item3.GlobalPosition))
				{
					SpawnPointUnits spawnPointUnits = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "sp_player_conversation_alley_" + alleyMarker.AreaIndex && x.Place == _sceneType);
					if (spawnPointUnits != null)
					{
						num2 = (spawnPointUnits.CurrentCount = num2 + 1);
					}
				}
			}
			if (alleyMarker.AreaIndex == 1)
			{
				SpawnPointUnits spawnPointUnits2 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "alley_1_population" && x.Place == _sceneType);
				if (spawnPointUnits2 != null)
				{
					spawnPointUnits2.CurrentCount = FindValidSpawnPointCountOfUsableMachine(list);
				}
			}
			else if (alleyMarker.AreaIndex == 2)
			{
				SpawnPointUnits spawnPointUnits3 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "alley_2_population" && x.Place == _sceneType);
				if (spawnPointUnits3 != null)
				{
					spawnPointUnits3.CurrentCount = FindValidSpawnPointCountOfUsableMachine(list);
				}
			}
			else if (alleyMarker.AreaIndex == 3)
			{
				SpawnPointUnits spawnPointUnits4 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "alley_3_population" && x.Place == _sceneType);
				if (spawnPointUnits4 != null)
				{
					spawnPointUnits4.CurrentCount = FindValidSpawnPointCountOfUsableMachine(list);
				}
			}
		}
	}

	private void CheckForWorkshops(IEnumerable<GameEntity> allGameEntitiesWithGivenTag, SpawnPointUnits spUnit)
	{
		foreach (GameEntity item in allGameEntitiesWithGivenTag)
		{
			WorkshopAreaMarker workshopAreaMarker = item.GetFirstScriptOfType<WorkshopAreaMarker>();
			if (workshopAreaMarker == null || item.IsGhostObject())
			{
				continue;
			}
			float areaRadius = workshopAreaMarker.AreaRadius;
			List<GameEntity> entities = new List<GameEntity>();
			base.Scene.GetEntities(ref entities);
			float num = areaRadius * areaRadius;
			foreach (GameEntity item2 in entities.ToList())
			{
				if (!item2.HasScriptOfType<UsableMachine>() || item2.HasScriptOfType<Passage>() || item2.GlobalPosition.DistanceSquared(item.GlobalPosition) > num)
				{
					entities.Remove(item2);
				}
			}
			foreach (GameEntity item3 in base.Scene.FindEntitiesWithTag("sp_notables_parent").ToList())
			{
				if (!(item3.GlobalPosition.DistanceSquared(item.GlobalPosition) < num))
				{
					continue;
				}
				if (workshopAreaMarker.AreaIndex == 1)
				{
					SpawnPointUnits spawnPointUnits = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_1_notable_parent" && x.Place == _sceneType);
					if (spawnPointUnits != null)
					{
						spawnPointUnits.CurrentCount = 1;
					}
				}
				else if (workshopAreaMarker.AreaIndex == 2)
				{
					SpawnPointUnits spawnPointUnits2 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_2_notable_parent" && x.Place == _sceneType);
					if (spawnPointUnits2 != null)
					{
						spawnPointUnits2.CurrentCount = 1;
					}
				}
				else if (workshopAreaMarker.AreaIndex == 3)
				{
					SpawnPointUnits spawnPointUnits3 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_3_notable_parent" && x.Place == _sceneType);
					if (spawnPointUnits3 != null)
					{
						spawnPointUnits3.CurrentCount = 1;
					}
				}
			}
			List<GameEntity> list = base.Scene.FindEntitiesWithTag("sp_player_conversation").ToList();
			int num2 = 0;
			foreach (GameEntity item4 in list)
			{
				if (workshopAreaMarker.IsPositionInRange(item4.GlobalPosition))
				{
					SpawnPointUnits spawnPointUnits4 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "sp_player_conversation_workshop_" + workshopAreaMarker.AreaIndex && x.Place == _sceneType);
					if (spawnPointUnits4 != null)
					{
						num2 = (spawnPointUnits4.CurrentCount = num2 + 1);
					}
				}
			}
			if (workshopAreaMarker.AreaIndex == 1)
			{
				SpawnPointUnits spawnPointUnits5 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_area_1_population" && x.Place == _sceneType);
				if (spawnPointUnits5 != null)
				{
					spawnPointUnits5.CurrentCount += FindValidSpawnPointCountOfUsableMachine(entities);
				}
			}
			else if (workshopAreaMarker.AreaIndex == 2)
			{
				SpawnPointUnits spawnPointUnits6 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_area_2_population" && x.Place == _sceneType);
				if (spawnPointUnits6 != null)
				{
					spawnPointUnits6.CurrentCount += FindValidSpawnPointCountOfUsableMachine(entities);
				}
			}
			else if (workshopAreaMarker.AreaIndex == 3)
			{
				SpawnPointUnits spawnPointUnits7 = _spUnitsList.FirstOrDefault((SpawnPointUnits x) => x.SpName == "workshop_area_3_population" && x.Place == _sceneType);
				if (spawnPointUnits7 != null)
				{
					spawnPointUnits7.CurrentCount += FindValidSpawnPointCountOfUsableMachine(entities);
				}
			}
		}
	}

	private int FindValidSpawnPointCountOfUsableMachine(List<GameEntity> gameEntities)
	{
		int num = 0;
		foreach (GameEntity gameEntity in gameEntities)
		{
			UsableMachine firstScriptOfType = gameEntity.GetFirstScriptOfType<UsableMachine>();
			if (firstScriptOfType != null)
			{
				num += MissionAgentHandler.GetPointCountOfUsableMachine(firstScriptOfType, checkForUnusedOnes: false);
			}
		}
		return num;
	}

	private void CountPassages(SpawnPointUnits spUnit)
	{
		if (!spUnit.SpName.Contains("npc_passage"))
		{
			return;
		}
		foreach (GameEntity item in base.Scene.FindEntitiesWithTag("npc_passage"))
		{
			foreach (GameEntity child in item.GetChildren())
			{
				PassageUsePoint firstScriptOfType = child.GetFirstScriptOfType<PassageUsePoint>();
				if (firstScriptOfType != null && !child.IsGhostObject() && child.GetEditModeLevelVisibility() && (DetectWhichPassage(firstScriptOfType, spUnit.SpName, "tavern") || DetectWhichPassage(firstScriptOfType, spUnit.SpName, "arena") || DetectWhichPassage(firstScriptOfType, spUnit.SpName, "prison") || DetectWhichPassage(firstScriptOfType, spUnit.SpName, "lordshall") || DetectWhichPassage(firstScriptOfType, spUnit.SpName, "house_1") || DetectWhichPassage(firstScriptOfType, spUnit.SpName, "house_2") || DetectWhichPassage(firstScriptOfType, spUnit.SpName, "house_3")))
				{
					spUnit.CurrentCount++;
				}
			}
		}
	}

	private void CalculateCurrentInvalidPointsCount()
	{
		_currentInvalidPoints = 0;
		if (_showAnimals)
		{
			_currentInvalidPoints += GetCategoryCount(CategoryId.Animal);
		}
		if (_showChairs)
		{
			_currentInvalidPoints += GetCategoryCount(CategoryId.Chair);
		}
		if (_showNPCs)
		{
			_currentInvalidPoints += GetCategoryCount(CategoryId.NPC);
		}
		if (_showSemiValidPoints)
		{
			_currentInvalidPoints += GetCategoryCount(CategoryId.SemivalidChair);
		}
		if (_showPassagePoints)
		{
			_currentInvalidPoints += GetCategoryCount(CategoryId.Passage);
		}
		if (_showOutOfBoundPoints)
		{
			_currentInvalidPoints += GetCategoryCount(CategoryId.OutOfMissionBound);
		}
	}

	private bool DetectWhichPassage(PassageUsePoint passageUsePoint, string spName, string locationName)
	{
		string toLocationId = passageUsePoint.ToLocationId;
		if (_sceneType != SpawnPointUnits.SceneType.Center && _sceneType != SpawnPointUnits.SceneType.Castle)
		{
			locationName = "center";
		}
		if (toLocationId == locationName)
		{
			return spName == "npc_passage_" + locationName;
		}
		return false;
	}

	private void ShowNavigationCheckTab()
	{
		WriteNavigationMeshTabTexts();
		ToggleButtons();
		CalculateCurrentInvalidPointsCount();
		if (ImGUIButton("CHECK", _normalButton))
		{
			CheckForNavigationMesh();
		}
	}

	private void CheckForNavigationMesh()
	{
		ClearAllLists();
		GetDisableFaceID();
		CountEntities();
		foreach (SpawnPointUnits spUnits in _spUnitsList)
		{
			if (!(spUnits.SpName == "alley_marker") && !(spUnits.SpName == "navigation_mesh_deactivator"))
			{
				CheckIfPassage(spUnits);
				CheckIfChairOrAnimal(spUnits);
			}
		}
		RemoveDuplicateValuesInLists();
	}

	private void CheckNavigationMeshForParticularEntity(GameEntity gameEntity, CategoryId categoryId)
	{
		if (gameEntity.Name == "workshop_1" || gameEntity.Name == "workshop_2" || gameEntity.Name == "workshop_3")
		{
			return;
		}
		Vec3 position = gameEntity.GetGlobalFrame().origin;
		if (gameEntity.HasScriptOfType<NavigationMeshDeactivator>() || !MBEditor.IsEditModeOn || !gameEntity.GetEditModeLevelVisibility() || !gameEntity.HasScriptOfType<StandingPoint>())
		{
			return;
		}
		if (Mission.Current != null && !Mission.Current.IsPositionInsideBoundaries(position.AsVec2))
		{
			AddPositionToInvalidList(categoryId, position, gameEntity, isDisabledNavMesh: false);
		}
		_particularfaceID = -1;
		if (base.Scene.GetNavigationMeshForPosition(in position, out _particularfaceID, 1.5f, excludeDynamicNavigationMeshes: false) != UIntPtr.Zero)
		{
			if (!gameEntity.Name.Contains("player") && _particularfaceID == _disabledFaceId && (_sceneType == SpawnPointUnits.SceneType.Center || _sceneType == SpawnPointUnits.SceneType.VillageCenter) && categoryId != CategoryId.Chair && categoryId != CategoryId.Animal)
			{
				if (!(gameEntity.Parent != null) || !(gameEntity.Parent.Name == "sp_battle_set"))
				{
					AddPositionToInvalidList(categoryId, position, gameEntity, isDisabledNavMesh: true);
				}
			}
			else if (gameEntity.Parent != null)
			{
				CheckSemiValidsOfChair(gameEntity);
			}
		}
		else if (categoryId == CategoryId.Chair && gameEntity.Parent != null)
		{
			CheckSemiValidsOfChair(gameEntity);
		}
		else
		{
			AddPositionToInvalidList(categoryId, position, gameEntity, isDisabledNavMesh: false);
		}
	}

	private void CheckSemiValidsOfChair(GameEntity gameEntity)
	{
		AnimationPoint firstScriptOfType = gameEntity.GetFirstScriptOfType<AnimationPoint>();
		if (firstScriptOfType == null)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		List<AnimationPoint> alternatives = firstScriptOfType.GetAlternatives();
		if (alternatives != null && !alternatives.IsEmpty())
		{
			foreach (AnimationPoint item in alternatives)
			{
				Vec3 position = item.GameEntity.GetGlobalFrame().origin;
				if ((!(base.Scene.GetNavigationMeshForPosition(in position, out _particularfaceID, 1.5f, excludeDynamicNavigationMeshes: false) == UIntPtr.Zero) || item.GameEntity.IsGhostObject()) && _particularfaceID != _disabledFaceId)
				{
					flag = true;
					if (item == firstScriptOfType)
					{
						flag2 = true;
					}
				}
			}
			if (!flag2)
			{
				if (flag)
				{
					Vec3 origin = firstScriptOfType.GameEntity.GetGlobalFrame().origin;
					AddPositionToInvalidList(CategoryId.SemivalidChair, origin, gameEntity, isDisabledNavMesh: false, doNotShowWarning: true);
				}
				else
				{
					Vec3 origin2 = firstScriptOfType.GameEntity.GetGlobalFrame().origin;
					AddPositionToInvalidList(CategoryId.Chair, origin2, gameEntity, isDisabledNavMesh: false);
				}
			}
		}
		else
		{
			Vec3 position2 = firstScriptOfType.GameEntity.GetGlobalFrame().origin;
			if (base.Scene.GetNavigationMeshForPosition(in position2) == UIntPtr.Zero && !firstScriptOfType.GameEntity.IsGhostObject())
			{
				AddPositionToInvalidList(CategoryId.Chair, position2, gameEntity, isDisabledNavMesh: false);
			}
		}
	}

	private void CheckIfChairOrAnimal(SpawnPointUnits spUnit)
	{
		foreach (GameEntity item in base.Scene.FindEntitiesWithTag(spUnit.SpName))
		{
			IEnumerable<GameEntity> children = item.GetChildren();
			if (children.Count() != 0)
			{
				foreach (GameEntity item2 in children)
				{
					if (item2.Name.Contains("chair") && !item2.IsGhostObject())
					{
						CheckNavigationMeshForParticularEntity(item2, CategoryId.Chair);
					}
					else if (!item2.IsGhostObject() && !item2.IsGhostObject())
					{
						CheckNavigationMeshForParticularEntity(item2, CategoryId.NPC);
					}
				}
			}
			else if (spUnit.Type == "animal" && !item.IsGhostObject())
			{
				CheckNavigationMeshForParticularEntity(item, CategoryId.Animal);
			}
			else if (!item.IsGhostObject())
			{
				CheckNavigationMeshForParticularEntity(item, CategoryId.NPC);
			}
		}
	}

	private void CheckIfPassage(SpawnPointUnits spUnit)
	{
		if (!spUnit.SpName.Contains("passage"))
		{
			return;
		}
		foreach (GameEntity item in base.Scene.FindEntitiesWithTag("npc_passage"))
		{
			foreach (GameEntity child in item.GetChildren())
			{
				if (child.Name.Contains("passage") && !child.IsGhostObject())
				{
					CheckNavigationMeshForParticularEntity(child, CategoryId.Passage);
					break;
				}
			}
		}
	}

	private void RemoveDuplicateValuesInLists()
	{
		_invalidSpawnPointsDictionary = _invalidSpawnPointsDictionary.ToDictionary((KeyValuePair<CategoryId, List<InvalidPosition>> c) => c.Key, (KeyValuePair<CategoryId, List<InvalidPosition>> c) => c.Value.Distinct().ToList());
		if (!_invalidSpawnPointsDictionary.ContainsKey(CategoryId.SemivalidChair))
		{
			return;
		}
		foreach (InvalidPosition item in _invalidSpawnPointsDictionary[CategoryId.SemivalidChair])
		{
			if (_invalidSpawnPointsDictionary[CategoryId.Chair].Contains(item))
			{
				_invalidSpawnPointsDictionary[CategoryId.Chair].Remove(item);
			}
		}
	}

	private void AddPositionToInvalidList(CategoryId categoryId, Vec3 globalPosition, GameEntity entity, bool isDisabledNavMesh, bool doNotShowWarning = false)
	{
		if (!entity.IsGhostObject() && entity.IsVisibleIncludeParents() && _invalidSpawnPointsDictionary.ContainsKey(categoryId))
		{
			InvalidPosition item = default(InvalidPosition);
			item.position = globalPosition;
			item.entity = entity;
			item.isDisabledNavMesh = isDisabledNavMesh;
			item.doNotShowWarning = doNotShowWarning;
			if (_invalidSpawnPointsDictionary[categoryId].All((InvalidPosition x) => x.position != globalPosition))
			{
				_invalidSpawnPointsDictionary[categoryId].Add(item);
			}
		}
	}

	private void ToggleButtons()
	{
		if (_showNPCs)
		{
			DrawDebugLinesForInvalidSpawnPoints(CategoryId.NPC, _npcDebugLineColor);
		}
		if (_showChairs)
		{
			DrawDebugLinesForInvalidSpawnPoints(CategoryId.Chair, _chairDebugLineColor);
		}
		if (_showAnimals)
		{
			DrawDebugLinesForInvalidSpawnPoints(CategoryId.Animal, _animalDebugLineColor);
		}
		if (_showSemiValidPoints)
		{
			DrawDebugLinesForInvalidSpawnPoints(CategoryId.SemivalidChair, _semivalidChairDebugLineColor);
		}
		if (_showPassagePoints)
		{
			DrawDebugLinesForInvalidSpawnPoints(CategoryId.Passage, _passageDebugLineColor);
		}
		if (_showOutOfBoundPoints)
		{
			DrawDebugLinesForInvalidSpawnPoints(CategoryId.OutOfMissionBound, _missionBoundDebugLineColor);
		}
	}

	private void CheckInaccessiblePoint()
	{
		WriteInaccessiblePointTexts();
		if (ImGUIButton("Check Inaccessible Points", _normalButton))
		{
			CheckInaccesiblePositions();
		}
	}

	private void CheckInaccesiblePositions()
	{
		_allGameEntitiesWithAnimationScript.Clear();
		_inaccessibleEntitiesList.Clear();
		_closeEntitiesToInaccessible.Clear();
		_selectedEntity = null;
		GetDisableFaceID();
		int[] excludedFaceIds = new int[1] { _disabledFaceId };
		base.Scene.GetAllEntitiesWithScriptComponent<AnimationPoint>(ref _allGameEntitiesWithAnimationScript);
		for (int num = _allGameEntitiesWithAnimationScript.Count - 1; num >= 0; num--)
		{
			GameEntity gameEntity = _allGameEntitiesWithAnimationScript[num];
			bool flag = false;
			while (gameEntity != null)
			{
				if (gameEntity.HasTag("static_npc"))
				{
					flag = true;
					break;
				}
				gameEntity = gameEntity.Parent;
			}
			if (flag)
			{
				_allGameEntitiesWithAnimationScript.RemoveAt(num);
			}
		}
		for (int i = 0; i < _allGameEntitiesWithAnimationScript.Count; i++)
		{
			base.Scene.GetNavMeshFaceIndex(ref _startPositionNavMesh, _allGameEntitiesWithAnimationScript[i].GlobalPosition, checkIfDisabled: true);
			if (_startPositionNavMesh.FaceGroupIndex != _disabledFaceId && _startPositionNavMesh.FaceGroupIndex >= 0)
			{
				_selectedEntity = _allGameEntitiesWithAnimationScript[i];
				if (_selectedEntity.IsVisibleIncludeParents())
				{
					break;
				}
			}
		}
		if (_selectedEntity != null)
		{
			for (int j = 0; j < _allGameEntitiesWithAnimationScript.Count; j++)
			{
				GameEntity gameEntity2 = _allGameEntitiesWithAnimationScript[j];
				base.Scene.GetNavMeshFaceIndex(ref _targetPositionNavMesh, gameEntity2.GlobalPosition, checkIfDisabled: true);
				if (_targetPositionNavMesh.FaceGroupIndex != _disabledFaceId && _targetPositionNavMesh.FaceGroupIndex >= 0 && gameEntity2.IsVisibleIncludeParents())
				{
					NavigationPath path = new NavigationPath();
					if (!base.Scene.GetPathBetweenAIFaces(_startPositionNavMesh.FaceIndex, _targetPositionNavMesh.FaceIndex, _selectedEntity.GlobalPosition.AsVec2, gameEntity2.GlobalPosition.AsVec2, 0.3f, path, excludedFaceIds, 1f))
					{
						_inaccessibleEntitiesList.Add(_allGameEntitiesWithAnimationScript[j]);
					}
				}
			}
			if (!_inaccessibleEntitiesList.Any())
			{
				return;
			}
			MBEditor.AddEditorWarning("Scene has inaccessible point!");
			{
				foreach (GameEntity inaccessibleEntities in _inaccessibleEntitiesList)
				{
					float num2 = float.MaxValue;
					for (int k = 0; k < _allGameEntitiesWithAnimationScript.Count; k++)
					{
						GameEntity gameEntity3 = _allGameEntitiesWithAnimationScript[k];
						base.Scene.GetNavMeshFaceIndex(ref _startPositionNavMesh, gameEntity3.GlobalPosition, checkIfDisabled: true);
						if (_startPositionNavMesh.FaceGroupIndex != _disabledFaceId && _startPositionNavMesh.FaceGroupIndex >= 0 && gameEntity3.IsVisibleIncludeParents())
						{
							float num3 = inaccessibleEntities.GlobalPosition.DistanceSquared(gameEntity3.GlobalPosition);
							if (!_inaccessibleEntitiesList.Contains(gameEntity3) && num3 < num2)
							{
								num2 = num3;
								_closeEntity = gameEntity3;
							}
						}
					}
					if (!_closeEntitiesToInaccessible.Contains(_closeEntity))
					{
						_closeEntitiesToInaccessible.Add(_closeEntity);
					}
				}
				return;
			}
		}
		MBEditor.AddEditorWarning("You have selected multiple levels or there is no valid entity in the scene!");
	}

	private void FindAllPrefabsWithSelectedTag()
	{
		if (allPrefabsWithParticularTag != null)
		{
			string[] array = allPrefabsWithParticularTag.Split(new char[1] { '/' });
			for (int i = 0; i < array.Length; i++)
			{
				ImGUITextArea(array[i], !_separatorNeeded, !_onSameLineNeeded);
			}
		}
	}

	private void FocusCameraToMisplacedObjects(CategoryId CategoryId)
	{
		_invalidSpawnPointsDictionary.TryGetValue(CategoryId, out var value);
		if (value.Count == 0 || _cameraFocusIndex < 0 || _cameraFocusIndex >= value.Count)
		{
			_cameraFocusIndex = 0;
			return;
		}
		MBEditor.ZoomToPosition(value[_cameraFocusIndex].position);
		_cameraFocusIndex = ((_cameraFocusIndex >= value.Count - 1) ? (_cameraFocusIndex = 0) : (++_cameraFocusIndex));
	}

	private void FocusCameraToInaccessiblePosition()
	{
		if (_inaccessibleEntitiesList.Count == 0 || _cameraFocusIndex < 0 || _cameraFocusIndex >= _inaccessibleEntitiesList.Count)
		{
			_cameraFocusIndex = 0;
			MBEditor.ZoomToPosition(_inaccessibleEntitiesList[_cameraFocusIndex].GlobalPosition);
		}
		else
		{
			_cameraFocusIndex = ((_cameraFocusIndex >= _inaccessibleEntitiesList.Count - 1) ? (_cameraFocusIndex = 0) : (++_cameraFocusIndex));
			MBEditor.ZoomToPosition(_inaccessibleEntitiesList[_cameraFocusIndex].GlobalPosition);
		}
	}

	private int GetCategoryCount(CategoryId CategoryId)
	{
		int result = 0;
		if (_invalidSpawnPointsDictionary.ContainsKey(CategoryId))
		{
			result = _invalidSpawnPointsDictionary[CategoryId].Count;
		}
		return result;
	}

	private void GetDisableFaceID()
	{
		foreach (SpawnPointUnits item in _spUnitsList.ToList())
		{
			List<GameEntity> list = base.Scene.FindEntitiesWithTag(item.SpName).ToList();
			if (!(item.SpName == "navigation_mesh_deactivator"))
			{
				continue;
			}
			foreach (GameEntity item2 in list)
			{
				NavigationMeshDeactivator firstScriptOfType = item2.GetFirstScriptOfType<NavigationMeshDeactivator>();
				if (firstScriptOfType != null && firstScriptOfType.GameEntity.GetEditModeLevelVisibility())
				{
					_disabledFaceId = firstScriptOfType.DisableFaceWithId;
					break;
				}
			}
		}
	}

	private void ClearAllLists()
	{
		foreach (KeyValuePair<CategoryId, List<InvalidPosition>> item in _invalidSpawnPointsDictionary)
		{
			item.Value.Clear();
		}
	}

	private bool ImGUIButton(string buttonText, bool smallButton)
	{
		if (smallButton)
		{
			return Imgui.SmallButton(buttonText);
		}
		return Imgui.Button(buttonText);
	}

	private void LeaveSpaceBetweenTabs()
	{
		OnSameLine();
		ImGUITextArea(" ", !_separatorNeeded, _onSameLineNeeded);
	}

	private void EndImGUIWindow()
	{
		Imgui.End();
		Imgui.EndMainThreadScope();
	}

	private void StartImGUIWindow(string str)
	{
		Imgui.BeginMainThreadScope();
		Imgui.Begin(str);
	}

	private void ImGUITextArea(string text, bool separatorNeeded, bool onSameLine)
	{
		Imgui.Text(text);
		ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
	}

	private void ImGUICheckBox(string text, ref bool is_checked, bool separatorNeeded, bool onSameLine)
	{
		Imgui.Checkbox(text, ref is_checked);
		ImGUISeparatorSameLineHandler(separatorNeeded, onSameLine);
	}

	private void ImguiSameLine(float positionX, float spacingWidth)
	{
		Imgui.SameLine(positionX, spacingWidth);
	}

	private void ImGUISeparatorSameLineHandler(bool separatorNeeded, bool onSameLine)
	{
		if (separatorNeeded)
		{
			Separator();
		}
		if (onSameLine)
		{
			OnSameLine();
		}
	}

	private void OnSameLine()
	{
		Imgui.SameLine();
	}

	private void Separator()
	{
		Imgui.Separator();
	}

	private void WriteLineOfTableDebug(SpawnPointUnits spUnit, Vec3 Color, string type)
	{
		if ((type == "animal" && _showAnimalsList) || (type == "npc" && _showNPCsList) || (type == "passage" && _showPassagesList) || (type == "DONTUSE" && _showDontUseList) || (type == "other" && _showOthersList))
		{
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref Color);
			ImguiSameLine(0f, 0f);
			ImGUITextArea(spUnit.SpName, !_separatorNeeded, _onSameLineNeeded);
			ImguiSameLine(305f, 10f);
			ImGUITextArea(spUnit.MinCount.ToString(), !_separatorNeeded, _onSameLineNeeded);
			ImguiSameLine(345f, 10f);
			ImGUITextArea((spUnit.MaxCount == int.MaxValue) ? "-" : spUnit.MaxCount.ToString(), !_separatorNeeded, _onSameLineNeeded);
			ImguiSameLine(405f, 10f);
			ImGUITextArea(spUnit.CurrentCount.ToString(), !_separatorNeeded, _onSameLineNeeded);
			ImguiSameLine(500f, 10f);
			ImGUITextArea(spUnit.SpawnedAgentCount.ToString(), !_separatorNeeded, _onSameLineNeeded);
			Imgui.PopStyleColor();
			ImguiSameLine(575f, 10f);
			if (ImGUIButton(spUnit.SpName, _normalButton))
			{
				_relatedEntityWindow = true;
				_relatedPrefabTag = spUnit.SpName;
				allPrefabsWithParticularTag = MBEditor.GetAllPrefabsAndChildWithTag(_relatedPrefabTag);
			}
			ImGUITextArea(" ", !_separatorNeeded, !_onSameLineNeeded);
		}
	}

	private void WriteNavigationMeshTabTexts()
	{
		ImGUITextArea("This tool will mark the spawn points which are not on the navigation mesh", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("or on the navigation mesh that will be deactivated by 'Navigation Mesh Deactivator'", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Deactivation Face Id: " + _disabledFaceId, !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Click 'CHECK' button to calculate.", _separatorNeeded, !_onSameLineNeeded);
		Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _redColor);
		ImGUICheckBox("Show NPCs ", ref _showNPCs, !_separatorNeeded, _onSameLineNeeded);
		ImGUITextArea("(" + GetCategoryCount(CategoryId.NPC) + ")", !_separatorNeeded, !_onSameLineNeeded);
		if (_showNPCs)
		{
			if (ImGUIButton("<Previous NPC", _normalButton))
			{
				_cameraFocusIndex -= 2;
				FocusCameraToMisplacedObjects(CategoryId.NPC);
			}
			ImguiSameLine(120f, 20f);
			if (ImGUIButton("Next NPC>", _normalButton))
			{
				FocusCameraToMisplacedObjects(CategoryId.NPC);
			}
			ImGUITextArea(_cameraFocusIndex + 1 + " (" + GetCategoryCount(CategoryId.NPC) + ")", _separatorNeeded, !_onSameLineNeeded);
		}
		Imgui.PopStyleColor();
		Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _blueColor);
		ImGUICheckBox("Show Animals ", ref _showAnimals, !_separatorNeeded, _onSameLineNeeded);
		ImGUITextArea("(" + GetCategoryCount(CategoryId.Animal) + ")", !_separatorNeeded, !_onSameLineNeeded);
		if (_showAnimals)
		{
			if (ImGUIButton("<Previous Animal", _normalButton))
			{
				_cameraFocusIndex -= 2;
				FocusCameraToMisplacedObjects(CategoryId.Animal);
			}
			ImguiSameLine(120f, 20f);
			if (ImGUIButton("Next Animal>", _normalButton))
			{
				FocusCameraToMisplacedObjects(CategoryId.Animal);
			}
			ImGUITextArea(_cameraFocusIndex + 1 + " (" + GetCategoryCount(CategoryId.Animal) + ")", !_separatorNeeded, !_onSameLineNeeded);
		}
		Imgui.PopStyleColor();
		Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _purbleColor);
		ImGUICheckBox("Show Passages ", ref _showPassagePoints, !_separatorNeeded, _onSameLineNeeded);
		ImGUITextArea("(" + GetCategoryCount(CategoryId.Passage) + ")", !_separatorNeeded, !_onSameLineNeeded);
		if (_showPassagePoints)
		{
			if (ImGUIButton("<Previous Passage", _normalButton))
			{
				_cameraFocusIndex -= 2;
				FocusCameraToMisplacedObjects(CategoryId.Passage);
			}
			ImguiSameLine(120f, 20f);
			if (ImGUIButton("Next Passage>", _normalButton))
			{
				FocusCameraToMisplacedObjects(CategoryId.Passage);
			}
			ImGUITextArea(_cameraFocusIndex + 1 + " (" + GetCategoryCount(CategoryId.Passage) + ")", !_separatorNeeded, !_onSameLineNeeded);
		}
		Imgui.PopStyleColor();
		Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _greenColor);
		ImGUICheckBox("Show Chairs ", ref _showChairs, !_separatorNeeded, _onSameLineNeeded);
		ImGUITextArea("(" + GetCategoryCount(CategoryId.Chair) + ")", !_separatorNeeded, !_onSameLineNeeded);
		if (_showChairs)
		{
			if (ImGUIButton("<Previous Chair", _normalButton))
			{
				_cameraFocusIndex -= 2;
				FocusCameraToMisplacedObjects(CategoryId.Chair);
			}
			ImguiSameLine(120f, 20f);
			if (ImGUIButton("Next Chair>", _normalButton))
			{
				FocusCameraToMisplacedObjects(CategoryId.Chair);
			}
			ImGUITextArea(_cameraFocusIndex + 1 + " (" + GetCategoryCount(CategoryId.Chair) + ")", !_separatorNeeded, !_onSameLineNeeded);
		}
		Imgui.PopStyleColor();
		Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _yellowColor);
		ImGUICheckBox("Show semi-valid Chairs* ", ref _showSemiValidPoints, !_separatorNeeded, _onSameLineNeeded);
		ImGUITextArea("(" + GetCategoryCount(CategoryId.SemivalidChair) + ")", !_separatorNeeded, !_onSameLineNeeded);
		if (_showSemiValidPoints)
		{
			if (ImGUIButton("<Previous S-Chair", _normalButton))
			{
				_cameraFocusIndex -= 2;
				FocusCameraToMisplacedObjects(CategoryId.SemivalidChair);
			}
			ImguiSameLine(120f, 20f);
			if (ImGUIButton("Next S-Chair>", _normalButton))
			{
				FocusCameraToMisplacedObjects(CategoryId.SemivalidChair);
			}
			ImGUITextArea(_cameraFocusIndex + 1 + " (" + GetCategoryCount(CategoryId.SemivalidChair) + ")", !_separatorNeeded, !_onSameLineNeeded);
		}
		Imgui.PopStyleColor();
		ImGUICheckBox("Show out of Mission Bound Points**", ref _showOutOfBoundPoints, !_separatorNeeded, _onSameLineNeeded);
		ImGUITextArea(" (" + GetCategoryCount(CategoryId.OutOfMissionBound) + ")", !_separatorNeeded, !_onSameLineNeeded);
		_totalInvalidPoints = GetCategoryCount(CategoryId.NPC) + GetCategoryCount(CategoryId.Chair) + GetCategoryCount(CategoryId.Animal) + GetCategoryCount(CategoryId.SemivalidChair) + GetCategoryCount(CategoryId.Passage) + GetCategoryCount(CategoryId.OutOfMissionBound);
		ImGUITextArea("(" + _currentInvalidPoints + " / " + _totalInvalidPoints + " ) are being shown.", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("Found " + _totalInvalidPoints + " invalid spawnpoints.", _separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("* Points that have at least one valid point as alternative", _separatorNeeded, !_onSameLineNeeded);
		if (Mission.Current == null)
		{
			ImGUITextArea("** Mission bound checking feature is not working in editor. Open mission to check it.", _separatorNeeded, !_onSameLineNeeded);
		}
	}

	private void WriteInaccessiblePointTexts()
	{
		ImGUITextArea("This tool will mark the spawn points as inaccessible because ", !_separatorNeeded, !_onSameLineNeeded);
		ImGUITextArea("the navmeshes leading to these points have incorrect IDs.", !_separatorNeeded, !_onSameLineNeeded);
		if (_inaccessibleEntitiesList.Any())
		{
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _redColor);
			ImGUITextArea("Inaccessible Point Count:", !_separatorNeeded, _onSameLineNeeded);
			ImGUITextArea(_cameraFocusIndex + 1 + " (" + _inaccessibleEntitiesList.Count + ")", !_separatorNeeded, !_onSameLineNeeded);
			ImGUITextArea("Red lines mark the inaccessible point.", !_separatorNeeded, !_onSameLineNeeded);
			Imgui.PopStyleColor();
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _yellowColor);
			ImGUITextArea("Yellow lines mark the closest point to the inaccessible point.", !_separatorNeeded, !_onSameLineNeeded);
			Imgui.PopStyleColor();
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _redColor);
			if (ImGUIButton("<Previous Position", _normalButton))
			{
				_cameraFocusIndex -= 2;
				FocusCameraToInaccessiblePosition();
			}
			ImguiSameLine(120f, 40f);
			if (ImGUIButton("Next Position>", _normalButton))
			{
				FocusCameraToInaccessiblePosition();
			}
			Imgui.PopStyleColor();
			DrawDebugLineForInaccesiblePositions();
		}
		else
		{
			Imgui.PushStyleColor(Imgui.ColorStyle.Text, ref _greenColor);
			ImGUITextArea("Inaccessible Point Count: ", !_separatorNeeded, _onSameLineNeeded);
			ImGUITextArea(_inaccessibleEntitiesList.Count.ToString(), !_separatorNeeded, !_onSameLineNeeded);
			Imgui.PopStyleColor();
		}
	}

	private void DrawDebugLineForInaccesiblePositions()
	{
		for (int i = 0; i < _closeEntitiesToInaccessible.Count; i++)
		{
		}
		for (int j = 0; j < _inaccessibleEntitiesList.Count; j++)
		{
		}
	}

	private void DrawDebugLinesForInvalidSpawnPoints(CategoryId CategoryId, uint color)
	{
		if (!_invalidSpawnPointsDictionary.ContainsKey(CategoryId))
		{
			return;
		}
		foreach (InvalidPosition item in _invalidSpawnPointsDictionary[CategoryId])
		{
			_ = item;
		}
	}

	private void WriteTableHeaders()
	{
		ImguiSameLine(0f, 0f);
		ImGUITextArea("Tag Name", !_separatorNeeded, _onSameLineNeeded);
		ImguiSameLine(295f, 10f);
		ImGUITextArea("Min", !_separatorNeeded, _onSameLineNeeded);
		ImguiSameLine(340f, 10f);
		ImGUITextArea("Max", !_separatorNeeded, _onSameLineNeeded);
		ImguiSameLine(390f, 10f);
		ImGUITextArea("Current", !_separatorNeeded, _onSameLineNeeded);
		ImguiSameLine(465f, 10f);
		ImGUITextArea("Agent Count", !_separatorNeeded, _onSameLineNeeded);
		ImguiSameLine(575f, 10f);
		ImGUITextArea("List all prefabs with tag:", _separatorNeeded, !_onSameLineNeeded);
	}
}
