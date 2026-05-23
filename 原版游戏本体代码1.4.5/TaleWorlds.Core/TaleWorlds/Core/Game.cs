using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.SaveSystem.Save;

namespace TaleWorlds.Core;

[SaveableRootClass(5000)]
public sealed class Game : IGameStateManagerOwner
{
	public enum State
	{
		Running,
		Destroying,
		Destroyed
	}

	public Action<float> AfterTick;

	private EntitySystem<GameHandler> _gameEntitySystem;

	private Monster _defaultMonster;

	private Dictionary<Type, GameModelsManager> _gameModelManagers;

	private static Game _current;

	[SaveableField(11)]
	private int _nextUniqueTroopSeed = 1;

	private IReadOnlyDictionary<string, Equipment> _defaultEquipments;

	private Tuple<SaveOutput, Action<SaveResult>> _currentActiveSaveData;

	public State CurrentState { get; private set; }

	public IMonsterMissionDataCreator MonsterMissionDataCreator { get; set; }

	public Monster DefaultMonster => _defaultMonster ?? (_defaultMonster = ObjectManager.GetFirstObject<Monster>());

	[SaveableProperty(3)]
	public GameType GameType { get; private set; }

	public DefaultSiegeEngineTypes DefaultSiegeEngineTypes { get; private set; }

	public MBObjectManager ObjectManager { get; private set; }

	[SaveableProperty(8)]
	public BasicCharacterObject PlayerTroop { get; set; }

	[SaveableProperty(12)]
	internal MBFastRandom RandomGenerator { get; private set; }

	public BasicGameModels BasicModels { get; private set; }

	public GameManagerBase GameManager { get; private set; }

	public GameTextManager GameTextManager { get; private set; }

	public GameStateManager GameStateManager { get; private set; }

	public bool CheatMode => GameManager.CheatMode;

	public bool IsDevelopmentMode => GameManager.IsDevelopmentMode;

	public bool IsEditModeOn => GameManager.IsEditModeOn;

	public UnitSpawnPrioritizations UnitSpawnPrioritization => GameManager.UnitSpawnPrioritization;

	public float ApplicationTime => GameManager.ApplicationTime;

	public static Game Current
	{
		get
		{
			return _current;
		}
		internal set
		{
			_current = value;
			Game.OnGameCreated?.Invoke();
		}
	}

	public IBannerVisualCreator BannerVisualCreator { get; set; }

	public int NextUniqueTroopSeed => _nextUniqueTroopSeed++;

	public DefaultCharacterAttributes DefaultCharacterAttributes { get; private set; }

	public DefaultSkills DefaultSkills { get; private set; }

	public DefaultBannerEffects DefaultBannerEffects { get; private set; }

	public DefaultItemCategories DefaultItemCategories { get; private set; }

	public EventManager EventManager { get; private set; }

	public static event Action OnGameCreated;

	public event Action<ItemObject> OnItemDeserializedEvent;

	public T AddGameModelsManager<T>(IEnumerable<GameModel> inputComponents) where T : GameModelsManager
	{
		T val = (T)Activator.CreateInstance(typeof(T), inputComponents);
		_gameModelManagers.Add(typeof(T), val);
		return val;
	}

	public IBannerVisual CreateBannerVisual(Banner banner)
	{
		return BannerVisualCreator?.CreateBannerVisual(banner);
	}

	public Equipment GetDefaultEquipmentWithName(string equipmentName)
	{
		if (!_defaultEquipments.ContainsKey(equipmentName))
		{
			Debug.FailedAssert("Equipment with name \"" + equipmentName + "\" could not be found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Game.cs", "GetDefaultEquipmentWithName", 125);
			return null;
		}
		return _defaultEquipments[equipmentName].Clone();
	}

	public void SetDefaultEquipments(IReadOnlyDictionary<string, Equipment> defaultEquipments)
	{
		if (_defaultEquipments == null)
		{
			_defaultEquipments = defaultEquipments;
		}
	}

	public static Game CreateGame(GameType gameType, GameManagerBase gameManager, int seed)
	{
		Game game = CreateGame(gameType, gameManager);
		game.RandomGenerator = new MBFastRandom((uint)seed);
		return game;
	}

	private Game(GameType gameType, GameManagerBase gameManager, MBObjectManager objectManager)
	{
		GameType = gameType;
		Current = this;
		GameType.CurrentGame = this;
		GameManager = gameManager;
		GameManager.Game = this;
		EventManager = new EventManager();
		ObjectManager = objectManager;
		RandomGenerator = new MBFastRandom();
		InitializeParameters();
	}

	public static Game CreateGame(GameType gameType, GameManagerBase gameManager)
	{
		MBObjectManager objectManager = MBObjectManager.Init();
		RegisterTypes(gameType, objectManager, gameManager);
		return new Game(gameType, gameManager, objectManager);
	}

	public static Game LoadSaveGame(LoadResult loadResult, GameManagerBase gameManager)
	{
		MBSaveLoad.OnStartGame(loadResult);
		MBObjectManager objectManager = MBObjectManager.Init();
		Game obj = (Game)loadResult.Root;
		RegisterTypes(obj.GameType, objectManager, gameManager);
		loadResult.InitializeObjects();
		MBObjectManager.Instance.ReInitialize();
		loadResult.AfterInitializeObjects();
		GC.Collect();
		obj.ObjectManager = objectManager;
		obj.BeginLoading(gameManager);
		return obj;
	}

	[LoadInitializationCallback]
	private void OnLoad()
	{
		if (RandomGenerator == null)
		{
			RandomGenerator = new MBFastRandom();
		}
	}

	private void BeginLoading(GameManagerBase gameManager)
	{
		Current = this;
		GameType.CurrentGame = this;
		GameManager = gameManager;
		GameManager.Game = this;
		EventManager = new EventManager();
		InitializeParameters();
	}

	private void SaveAux(MetaData metaData, string saveName, ISaveDriver driver, Action<SaveResult> onSaveCompleted)
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnBeforeSave();
		}
		SaveOutput saveOutput = SaveManager.Save(this, metaData, saveName, driver);
		if (!saveOutput.IsContinuing)
		{
			OnSaveCompleted(saveOutput, onSaveCompleted);
		}
		else
		{
			_currentActiveSaveData = new Tuple<SaveOutput, Action<SaveResult>>(saveOutput, onSaveCompleted);
		}
	}

	private void OnSaveCompleted(SaveOutput finishedOutput, Action<SaveResult> onSaveCompleted)
	{
		finishedOutput.PrintStatus();
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnAfterSave();
		}
		Common.MemoryCleanupGC();
		onSaveCompleted?.Invoke(finishedOutput.Result);
	}

	public void Save(MetaData metaData, string saveName, ISaveDriver driver, Action<SaveResult> onSaveCompleted)
	{
		using (new PerformanceTestBlock("Save Process"))
		{
			SaveAux(metaData, saveName, driver, onSaveCompleted);
		}
	}

	private void InitializeParameters()
	{
		ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_core_parameters"));
		GameType.InitializeParameters();
	}

	void IGameStateManagerOwner.OnStateStackEmpty()
	{
		Destroy();
	}

	public void Destroy()
	{
		CurrentState = State.Destroying;
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnGameEnd();
		}
		GameManager.OnGameEnd(this);
		GameType.OnDestroy();
		ObjectManager.Destroy();
		EventManager.Clear();
		EventManager = null;
		GameStateManager.Current = null;
		GameStateManager = null;
		Current = null;
		CurrentState = State.Destroyed;
		_currentActiveSaveData = null;
		Common.MemoryCleanupGC();
	}

	public void CreateGameManager()
	{
		GameStateManager = new GameStateManager(this, GameStateManager.GameStateManagerType.Game);
	}

	public void OnStateChanged(GameState oldState)
	{
		GameType.OnStateChanged(oldState);
	}

	public T AddGameHandler<T>() where T : GameHandler, new()
	{
		return _gameEntitySystem.AddComponent<T>();
	}

	public T GetGameHandler<T>() where T : GameHandler
	{
		return _gameEntitySystem.GetComponent<T>();
	}

	public void RemoveGameHandler<T>() where T : GameHandler
	{
		_gameEntitySystem.RemoveComponent<T>();
	}

	public void Initialize()
	{
		if (_gameEntitySystem == null)
		{
			_gameEntitySystem = new EntitySystem<GameHandler>();
		}
		GameTextManager = new GameTextManager();
		GameTextManager.LoadGameTexts();
		_gameModelManagers = new Dictionary<Type, GameModelsManager>();
		GameTexts.Initialize(GameTextManager);
		GameType.OnInitialize();
	}

	public static void RegisterTypes(GameType gameType, MBObjectManager objectManager, GameManagerBase gameManager)
	{
		gameType?.BeforeRegisterTypes(objectManager);
		objectManager.RegisterType<Monster>("Monster", "Monsters", 2u);
		objectManager.RegisterType<SkeletonScale>("Scale", "Scales", 3u);
		objectManager.RegisterType<ItemObject>("Item", "Items", 4u);
		objectManager.RegisterType<ItemModifier>("ItemModifier", "ItemModifiers", 6u);
		objectManager.RegisterType<ItemModifierGroup>("ItemModifierGroup", "ItemModifierGroups", 7u);
		objectManager.RegisterType<CharacterAttribute>("CharacterAttribute", "CharacterAttributes", 8u);
		objectManager.RegisterType<SkillObject>("Skill", "Skills", 9u);
		objectManager.RegisterType<ItemCategory>("ItemCategory", "ItemCategories", 10u);
		objectManager.RegisterType<CraftingPiece>("CraftingPiece", "CraftingPieces", 11u);
		objectManager.RegisterType<CraftingTemplate>("CraftingTemplate", "CraftingTemplates", 12u);
		objectManager.RegisterType<SiegeEngineType>("SiegeEngineType", "SiegeEngineTypes", 13u);
		objectManager.RegisterType<WeaponDescription>("WeaponDescription", "WeaponDescriptions", 14u);
		objectManager.RegisterType<MBBodyProperty>("BodyProperty", "BodyProperties", 50u);
		objectManager.RegisterType<MBEquipmentRoster>("EquipmentRoster", "EquipmentRosters", 51u);
		objectManager.RegisterType<MBCharacterSkills>("SkillSet", "SkillSets", 52u);
		objectManager.RegisterType<BannerEffect>("BannerEffect", "BannerEffects", 53u);
		gameType?.OnRegisterTypes(objectManager);
		gameManager?.RegisterSubModuleTypes();
	}

	public void SetBasicModels(IEnumerable<GameModel> models)
	{
		BasicModels = AddGameModelsManager<BasicGameModels>(models);
	}

	internal void OnTick(float dt)
	{
		if (GameStateManager.Current == GameStateManager)
		{
			GameStateManager.OnTick(dt);
			if (_gameEntitySystem != null)
			{
				foreach (GameHandler component in _gameEntitySystem.Components)
				{
					try
					{
						component.OnTick(dt);
					}
					catch (Exception ex)
					{
						Debug.Print("Exception on gameHandler tick: " + ex);
					}
				}
			}
		}
		AfterTick?.Invoke(dt);
		Tuple<SaveOutput, Action<SaveResult>> currentActiveSaveData = _currentActiveSaveData;
		if (currentActiveSaveData != null && !currentActiveSaveData.Item1.IsContinuing)
		{
			OnSaveCompleted(_currentActiveSaveData.Item1, _currentActiveSaveData.Item2);
			_currentActiveSaveData = null;
		}
	}

	internal void OnGameNetworkBegin()
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnGameNetworkBegin();
		}
	}

	internal void OnGameNetworkEnd()
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnGameNetworkEnd();
		}
	}

	internal void OnEarlyPlayerConnect(VirtualPlayer peer)
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnEarlyPlayerConnect(peer);
		}
	}

	internal void OnPlayerConnect(VirtualPlayer peer)
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnPlayerConnect(peer);
		}
	}

	internal void OnPlayerDisconnect(VirtualPlayer peer)
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnPlayerDisconnect(peer);
		}
	}

	public void OnGameStart()
	{
		foreach (GameHandler component in _gameEntitySystem.Components)
		{
			component.OnGameStart();
		}
	}

	public bool DoLoading()
	{
		return GameType.DoLoadingForGameType();
	}

	public void OnMissionIsStarting(string missionName, MissionInitializerRecord rec)
	{
		GameType.OnMissionIsStarting(missionName, rec);
	}

	public void OnFinalize()
	{
		CurrentState = State.Destroying;
		GameStateManager.Current.CleanStates();
	}

	public void InitializeDefaultGameObjects()
	{
		DefaultCharacterAttributes = new DefaultCharacterAttributes();
		DefaultSkills = new DefaultSkills();
		DefaultBannerEffects = new DefaultBannerEffects();
		DefaultItemCategories = new DefaultItemCategories();
		DefaultSiegeEngineTypes = new DefaultSiegeEngineTypes();
		GameManager.InitializeSubModuleGameObjects(Current);
	}

	public void LoadBasicFiles()
	{
		ObjectManager.LoadXML("Monsters");
		ObjectManager.LoadXML("SkeletonScales");
		ObjectManager.LoadXML("ItemModifiers");
		ObjectManager.LoadXML("ItemModifierGroups");
		ObjectManager.LoadXML("CraftingPieces");
		ObjectManager.LoadXML("WeaponDescriptions");
		ObjectManager.LoadXML("CraftingTemplates");
		ObjectManager.LoadXML("BodyProperties");
		ObjectManager.LoadXML("SkillSets");
	}

	public void ItemObjectDeserialized(ItemObject itemObject)
	{
		this.OnItemDeserializedEvent?.Invoke(itemObject);
	}
}
