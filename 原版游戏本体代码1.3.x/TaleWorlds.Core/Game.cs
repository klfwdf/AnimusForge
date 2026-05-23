using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;
using TaleWorlds.SaveSystem.Save;

namespace TaleWorlds.Core
{
	// Token: 0x02000068 RID: 104
	[SaveableRootClass(5000)]
	public sealed class Game : IGameStateManagerOwner
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000744 RID: 1860 RVA: 0x00019098 File Offset: 0x00017298
		// (remove) Token: 0x06000745 RID: 1861 RVA: 0x000190CC File Offset: 0x000172CC
		public static event Action OnGameCreated;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000746 RID: 1862 RVA: 0x00019100 File Offset: 0x00017300
		// (remove) Token: 0x06000747 RID: 1863 RVA: 0x00019138 File Offset: 0x00017338
		public event Action<ItemObject> OnItemDeserializedEvent;

		// Token: 0x1700029A RID: 666
		// (get) Token: 0x06000748 RID: 1864 RVA: 0x0001916D File Offset: 0x0001736D
		// (set) Token: 0x06000749 RID: 1865 RVA: 0x00019175 File Offset: 0x00017375
		public Game.State CurrentState { get; private set; }

		// Token: 0x1700029B RID: 667
		// (get) Token: 0x0600074A RID: 1866 RVA: 0x0001917E File Offset: 0x0001737E
		// (set) Token: 0x0600074B RID: 1867 RVA: 0x00019186 File Offset: 0x00017386
		public IMonsterMissionDataCreator MonsterMissionDataCreator { get; set; }

		// Token: 0x1700029C RID: 668
		// (get) Token: 0x0600074C RID: 1868 RVA: 0x00019190 File Offset: 0x00017390
		public Monster DefaultMonster
		{
			get
			{
				Monster result;
				if ((result = this._defaultMonster) == null)
				{
					result = (this._defaultMonster = this.ObjectManager.GetFirstObject<Monster>());
				}
				return result;
			}
		}

		// Token: 0x1700029D RID: 669
		// (get) Token: 0x0600074D RID: 1869 RVA: 0x000191BB File Offset: 0x000173BB
		// (set) Token: 0x0600074E RID: 1870 RVA: 0x000191C3 File Offset: 0x000173C3
		[SaveableProperty(3)]
		public GameType GameType { get; private set; }

		// Token: 0x1700029E RID: 670
		// (get) Token: 0x0600074F RID: 1871 RVA: 0x000191CC File Offset: 0x000173CC
		// (set) Token: 0x06000750 RID: 1872 RVA: 0x000191D4 File Offset: 0x000173D4
		public DefaultSiegeEngineTypes DefaultSiegeEngineTypes { get; private set; }

		// Token: 0x1700029F RID: 671
		// (get) Token: 0x06000751 RID: 1873 RVA: 0x000191DD File Offset: 0x000173DD
		// (set) Token: 0x06000752 RID: 1874 RVA: 0x000191E5 File Offset: 0x000173E5
		public MBObjectManager ObjectManager { get; private set; }

		// Token: 0x170002A0 RID: 672
		// (get) Token: 0x06000753 RID: 1875 RVA: 0x000191EE File Offset: 0x000173EE
		// (set) Token: 0x06000754 RID: 1876 RVA: 0x000191F6 File Offset: 0x000173F6
		[SaveableProperty(8)]
		public BasicCharacterObject PlayerTroop { get; set; }

		// Token: 0x170002A1 RID: 673
		// (get) Token: 0x06000755 RID: 1877 RVA: 0x000191FF File Offset: 0x000173FF
		// (set) Token: 0x06000756 RID: 1878 RVA: 0x00019207 File Offset: 0x00017407
		[SaveableProperty(12)]
		internal MBFastRandom RandomGenerator { get; private set; }

		// Token: 0x170002A2 RID: 674
		// (get) Token: 0x06000757 RID: 1879 RVA: 0x00019210 File Offset: 0x00017410
		// (set) Token: 0x06000758 RID: 1880 RVA: 0x00019218 File Offset: 0x00017418
		public BasicGameModels BasicModels { get; private set; }

		// Token: 0x06000759 RID: 1881 RVA: 0x00019224 File Offset: 0x00017424
		public T AddGameModelsManager<T>(IEnumerable<GameModel> inputComponents) where T : GameModelsManager
		{
			T t = (T)((object)Activator.CreateInstance(typeof(T), new object[] { inputComponents }));
			this._gameModelManagers.Add(typeof(T), t);
			return t;
		}

		// Token: 0x170002A3 RID: 675
		// (get) Token: 0x0600075A RID: 1882 RVA: 0x0001926C File Offset: 0x0001746C
		// (set) Token: 0x0600075B RID: 1883 RVA: 0x00019274 File Offset: 0x00017474
		public GameManagerBase GameManager { get; private set; }

		// Token: 0x170002A4 RID: 676
		// (get) Token: 0x0600075C RID: 1884 RVA: 0x0001927D File Offset: 0x0001747D
		// (set) Token: 0x0600075D RID: 1885 RVA: 0x00019285 File Offset: 0x00017485
		public GameTextManager GameTextManager { get; private set; }

		// Token: 0x170002A5 RID: 677
		// (get) Token: 0x0600075E RID: 1886 RVA: 0x0001928E File Offset: 0x0001748E
		// (set) Token: 0x0600075F RID: 1887 RVA: 0x00019296 File Offset: 0x00017496
		public GameStateManager GameStateManager { get; private set; }

		// Token: 0x170002A6 RID: 678
		// (get) Token: 0x06000760 RID: 1888 RVA: 0x0001929F File Offset: 0x0001749F
		public bool CheatMode
		{
			get
			{
				return this.GameManager.CheatMode;
			}
		}

		// Token: 0x170002A7 RID: 679
		// (get) Token: 0x06000761 RID: 1889 RVA: 0x000192AC File Offset: 0x000174AC
		public bool IsDevelopmentMode
		{
			get
			{
				return this.GameManager.IsDevelopmentMode;
			}
		}

		// Token: 0x170002A8 RID: 680
		// (get) Token: 0x06000762 RID: 1890 RVA: 0x000192B9 File Offset: 0x000174B9
		public bool IsEditModeOn
		{
			get
			{
				return this.GameManager.IsEditModeOn;
			}
		}

		// Token: 0x170002A9 RID: 681
		// (get) Token: 0x06000763 RID: 1891 RVA: 0x000192C6 File Offset: 0x000174C6
		public UnitSpawnPrioritizations UnitSpawnPrioritization
		{
			get
			{
				return this.GameManager.UnitSpawnPrioritization;
			}
		}

		// Token: 0x170002AA RID: 682
		// (get) Token: 0x06000764 RID: 1892 RVA: 0x000192D3 File Offset: 0x000174D3
		public float ApplicationTime
		{
			get
			{
				return this.GameManager.ApplicationTime;
			}
		}

		// Token: 0x170002AB RID: 683
		// (get) Token: 0x06000765 RID: 1893 RVA: 0x000192E0 File Offset: 0x000174E0
		// (set) Token: 0x06000766 RID: 1894 RVA: 0x000192E7 File Offset: 0x000174E7
		public static Game Current
		{
			get
			{
				return Game._current;
			}
			internal set
			{
				Game._current = value;
				Action onGameCreated = Game.OnGameCreated;
				if (onGameCreated == null)
				{
					return;
				}
				onGameCreated();
			}
		}

		// Token: 0x170002AC RID: 684
		// (get) Token: 0x06000767 RID: 1895 RVA: 0x000192FE File Offset: 0x000174FE
		// (set) Token: 0x06000768 RID: 1896 RVA: 0x00019306 File Offset: 0x00017506
		public IBannerVisualCreator BannerVisualCreator { get; set; }

		// Token: 0x06000769 RID: 1897 RVA: 0x0001930F File Offset: 0x0001750F
		public IBannerVisual CreateBannerVisual(Banner banner)
		{
			if (this.BannerVisualCreator == null)
			{
				return null;
			}
			return this.BannerVisualCreator.CreateBannerVisual(banner);
		}

		// Token: 0x170002AD RID: 685
		// (get) Token: 0x0600076A RID: 1898 RVA: 0x00019328 File Offset: 0x00017528
		public int NextUniqueTroopSeed
		{
			get
			{
				int nextUniqueTroopSeed = this._nextUniqueTroopSeed;
				this._nextUniqueTroopSeed = nextUniqueTroopSeed + 1;
				return nextUniqueTroopSeed;
			}
		}

		// Token: 0x170002AE RID: 686
		// (get) Token: 0x0600076B RID: 1899 RVA: 0x00019346 File Offset: 0x00017546
		// (set) Token: 0x0600076C RID: 1900 RVA: 0x0001934E File Offset: 0x0001754E
		public DefaultCharacterAttributes DefaultCharacterAttributes { get; private set; }

		// Token: 0x170002AF RID: 687
		// (get) Token: 0x0600076D RID: 1901 RVA: 0x00019357 File Offset: 0x00017557
		// (set) Token: 0x0600076E RID: 1902 RVA: 0x0001935F File Offset: 0x0001755F
		public DefaultSkills DefaultSkills { get; private set; }

		// Token: 0x170002B0 RID: 688
		// (get) Token: 0x0600076F RID: 1903 RVA: 0x00019368 File Offset: 0x00017568
		// (set) Token: 0x06000770 RID: 1904 RVA: 0x00019370 File Offset: 0x00017570
		public DefaultBannerEffects DefaultBannerEffects { get; private set; }

		// Token: 0x170002B1 RID: 689
		// (get) Token: 0x06000771 RID: 1905 RVA: 0x00019379 File Offset: 0x00017579
		// (set) Token: 0x06000772 RID: 1906 RVA: 0x00019381 File Offset: 0x00017581
		public DefaultItemCategories DefaultItemCategories { get; private set; }

		// Token: 0x170002B2 RID: 690
		// (get) Token: 0x06000773 RID: 1907 RVA: 0x0001938A File Offset: 0x0001758A
		// (set) Token: 0x06000774 RID: 1908 RVA: 0x00019392 File Offset: 0x00017592
		public EventManager EventManager { get; private set; }

		// Token: 0x06000775 RID: 1909 RVA: 0x0001939C File Offset: 0x0001759C
		public Equipment GetDefaultEquipmentWithName(string equipmentName)
		{
			if (!this._defaultEquipments.ContainsKey(equipmentName))
			{
				Debug.FailedAssert("Equipment with name \"" + equipmentName + "\" could not be found.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\Game.cs", "GetDefaultEquipmentWithName", 128);
				return null;
			}
			return this._defaultEquipments[equipmentName].Clone(false);
		}

		// Token: 0x06000776 RID: 1910 RVA: 0x000193EF File Offset: 0x000175EF
		public void SetDefaultEquipments(IReadOnlyDictionary<string, Equipment> defaultEquipments)
		{
			if (this._defaultEquipments == null)
			{
				this._defaultEquipments = defaultEquipments;
			}
		}

		// Token: 0x06000777 RID: 1911 RVA: 0x00019400 File Offset: 0x00017600
		public static Game CreateGame(GameType gameType, GameManagerBase gameManager, int seed)
		{
			Game game = Game.CreateGame(gameType, gameManager);
			game.RandomGenerator = new MBFastRandom((uint)seed);
			return game;
		}

		// Token: 0x06000778 RID: 1912 RVA: 0x00019418 File Offset: 0x00017618
		private Game(GameType gameType, GameManagerBase gameManager, MBObjectManager objectManager)
		{
			this.GameType = gameType;
			Game.Current = this;
			this.GameType.CurrentGame = this;
			this.GameManager = gameManager;
			this.GameManager.Game = this;
			this.EventManager = new EventManager();
			this.ObjectManager = objectManager;
			this.RandomGenerator = new MBFastRandom();
			this.InitializeParameters();
		}

		// Token: 0x06000779 RID: 1913 RVA: 0x00019484 File Offset: 0x00017684
		public static Game CreateGame(GameType gameType, GameManagerBase gameManager)
		{
			MBObjectManager objectManager = MBObjectManager.Init();
			Game.RegisterTypes(gameType, objectManager, gameManager);
			return new Game(gameType, gameManager, objectManager);
		}

		// Token: 0x0600077A RID: 1914 RVA: 0x000194A8 File Offset: 0x000176A8
		public static Game LoadSaveGame(LoadResult loadResult, GameManagerBase gameManager)
		{
			MBSaveLoad.OnStartGame(loadResult);
			MBObjectManager objectManager = MBObjectManager.Init();
			Game game = (Game)loadResult.Root;
			Game.RegisterTypes(game.GameType, objectManager, gameManager);
			loadResult.InitializeObjects();
			MBObjectManager.Instance.ReInitialize();
			loadResult.AfterInitializeObjects();
			GC.Collect();
			game.ObjectManager = objectManager;
			game.BeginLoading(gameManager);
			return game;
		}

		// Token: 0x0600077B RID: 1915 RVA: 0x00019502 File Offset: 0x00017702
		[LoadInitializationCallback]
		private void OnLoad()
		{
			if (this.RandomGenerator == null)
			{
				this.RandomGenerator = new MBFastRandom();
			}
		}

		// Token: 0x0600077C RID: 1916 RVA: 0x00019517 File Offset: 0x00017717
		private void BeginLoading(GameManagerBase gameManager)
		{
			Game.Current = this;
			this.GameType.CurrentGame = this;
			this.GameManager = gameManager;
			this.GameManager.Game = this;
			this.EventManager = new EventManager();
			this.InitializeParameters();
		}

		// Token: 0x0600077D RID: 1917 RVA: 0x00019550 File Offset: 0x00017750
		private void SaveAux(MetaData metaData, string saveName, ISaveDriver driver, Action<SaveResult> onSaveCompleted)
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnBeforeSave();
			}
			SaveOutput saveOutput = SaveManager.Save(this, metaData, saveName, driver);
			if (!saveOutput.IsContinuing)
			{
				this.OnSaveCompleted(saveOutput, onSaveCompleted);
				return;
			}
			this._currentActiveSaveData = new Tuple<SaveOutput, Action<SaveResult>>(saveOutput, onSaveCompleted);
		}

		// Token: 0x0600077E RID: 1918 RVA: 0x000195D0 File Offset: 0x000177D0
		private void OnSaveCompleted(SaveOutput finishedOutput, Action<SaveResult> onSaveCompleted)
		{
			finishedOutput.PrintStatus();
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnAfterSave();
			}
			Common.MemoryCleanupGC(false);
			if (onSaveCompleted != null)
			{
				onSaveCompleted(finishedOutput.Result);
			}
		}

		// Token: 0x0600077F RID: 1919 RVA: 0x00019640 File Offset: 0x00017840
		public void Save(MetaData metaData, string saveName, ISaveDriver driver, Action<SaveResult> onSaveCompleted)
		{
			using (new PerformanceTestBlock("Save Process"))
			{
				this.SaveAux(metaData, saveName, driver, onSaveCompleted);
			}
		}

		// Token: 0x06000780 RID: 1920 RVA: 0x00019680 File Offset: 0x00017880
		private void InitializeParameters()
		{
			ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_core_parameters"));
			this.GameType.InitializeParameters();
		}

		// Token: 0x06000781 RID: 1921 RVA: 0x000196A6 File Offset: 0x000178A6
		void IGameStateManagerOwner.OnStateStackEmpty()
		{
			this.Destroy();
		}

		// Token: 0x06000782 RID: 1922 RVA: 0x000196B0 File Offset: 0x000178B0
		public void Destroy()
		{
			this.CurrentState = Game.State.Destroying;
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnGameEnd();
			}
			this.GameManager.OnGameEnd(this);
			this.GameType.OnDestroy();
			this.ObjectManager.Destroy();
			this.EventManager.Clear();
			this.EventManager = null;
			GameStateManager.Current = null;
			this.GameStateManager = null;
			Game.Current = null;
			this.CurrentState = Game.State.Destroyed;
			this._currentActiveSaveData = null;
			Common.MemoryCleanupGC(false);
		}

		// Token: 0x06000783 RID: 1923 RVA: 0x00019768 File Offset: 0x00017968
		public void CreateGameManager()
		{
			this.GameStateManager = new GameStateManager(this, GameStateManager.GameStateManagerType.Game);
		}

		// Token: 0x06000784 RID: 1924 RVA: 0x00019777 File Offset: 0x00017977
		public void OnStateChanged(GameState oldState)
		{
			this.GameType.OnStateChanged(oldState);
		}

		// Token: 0x06000785 RID: 1925 RVA: 0x00019785 File Offset: 0x00017985
		public T AddGameHandler<T>() where T : GameHandler, new()
		{
			return this._gameEntitySystem.AddComponent<T>();
		}

		// Token: 0x06000786 RID: 1926 RVA: 0x00019792 File Offset: 0x00017992
		public T GetGameHandler<T>() where T : GameHandler
		{
			return this._gameEntitySystem.GetComponent<T>();
		}

		// Token: 0x06000787 RID: 1927 RVA: 0x0001979F File Offset: 0x0001799F
		public void RemoveGameHandler<T>() where T : GameHandler
		{
			this._gameEntitySystem.RemoveComponent<T>();
		}

		// Token: 0x06000788 RID: 1928 RVA: 0x000197AC File Offset: 0x000179AC
		public void Initialize()
		{
			if (this._gameEntitySystem == null)
			{
				this._gameEntitySystem = new EntitySystem<GameHandler>();
			}
			this.GameTextManager = new GameTextManager();
			this.GameTextManager.LoadGameTexts();
			this._gameModelManagers = new Dictionary<Type, GameModelsManager>();
			GameTexts.Initialize(this.GameTextManager);
			this.GameType.OnInitialize();
		}

		// Token: 0x06000789 RID: 1929 RVA: 0x00019804 File Offset: 0x00017A04
		public static void RegisterTypes(GameType gameType, MBObjectManager objectManager, GameManagerBase gameManager)
		{
			if (gameType != null)
			{
				gameType.BeforeRegisterTypes(objectManager);
			}
			objectManager.RegisterType<Monster>("Monster", "Monsters", 2U, true, false);
			objectManager.RegisterType<SkeletonScale>("Scale", "Scales", 3U, true, false);
			objectManager.RegisterType<ItemObject>("Item", "Items", 4U, true, false);
			objectManager.RegisterType<ItemModifier>("ItemModifier", "ItemModifiers", 6U, true, false);
			objectManager.RegisterType<ItemModifierGroup>("ItemModifierGroup", "ItemModifierGroups", 7U, true, false);
			objectManager.RegisterType<CharacterAttribute>("CharacterAttribute", "CharacterAttributes", 8U, true, false);
			objectManager.RegisterType<SkillObject>("Skill", "Skills", 9U, true, false);
			objectManager.RegisterType<ItemCategory>("ItemCategory", "ItemCategories", 10U, true, false);
			objectManager.RegisterType<CraftingPiece>("CraftingPiece", "CraftingPieces", 11U, true, false);
			objectManager.RegisterType<CraftingTemplate>("CraftingTemplate", "CraftingTemplates", 12U, true, false);
			objectManager.RegisterType<SiegeEngineType>("SiegeEngineType", "SiegeEngineTypes", 13U, true, false);
			objectManager.RegisterType<WeaponDescription>("WeaponDescription", "WeaponDescriptions", 14U, true, false);
			objectManager.RegisterType<MBBodyProperty>("BodyProperty", "BodyProperties", 50U, true, false);
			objectManager.RegisterType<MBEquipmentRoster>("EquipmentRoster", "EquipmentRosters", 51U, true, false);
			objectManager.RegisterType<MBCharacterSkills>("SkillSet", "SkillSets", 52U, true, false);
			objectManager.RegisterType<BannerEffect>("BannerEffect", "BannerEffects", 53U, true, false);
			if (gameType != null)
			{
				gameType.OnRegisterTypes(objectManager);
			}
			if (gameManager != null)
			{
				gameManager.RegisterSubModuleTypes();
			}
		}

		// Token: 0x0600078A RID: 1930 RVA: 0x00019968 File Offset: 0x00017B68
		public void SetBasicModels(IEnumerable<GameModel> models)
		{
			this.BasicModels = this.AddGameModelsManager<BasicGameModels>(models);
		}

		// Token: 0x0600078B RID: 1931 RVA: 0x00019978 File Offset: 0x00017B78
		internal void OnTick(float dt)
		{
			if (GameStateManager.Current == this.GameStateManager)
			{
				this.GameStateManager.OnTick(dt);
				if (this._gameEntitySystem != null)
				{
					foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
					{
						try
						{
							gameHandler.OnTick(dt);
						}
						catch (Exception arg)
						{
							Debug.Print("Exception on gameHandler tick: " + arg, 0, Debug.DebugColor.White, 17592186044416UL);
						}
					}
				}
			}
			Action<float> afterTick = this.AfterTick;
			if (afterTick != null)
			{
				afterTick(dt);
			}
			Tuple<SaveOutput, Action<SaveResult>> currentActiveSaveData = this._currentActiveSaveData;
			if (currentActiveSaveData != null && !currentActiveSaveData.Item1.IsContinuing)
			{
				this.OnSaveCompleted(this._currentActiveSaveData.Item1, this._currentActiveSaveData.Item2);
				this._currentActiveSaveData = null;
			}
		}

		// Token: 0x0600078C RID: 1932 RVA: 0x00019A70 File Offset: 0x00017C70
		internal void OnGameNetworkBegin()
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnGameNetworkBegin();
			}
		}

		// Token: 0x0600078D RID: 1933 RVA: 0x00019AC8 File Offset: 0x00017CC8
		internal void OnGameNetworkEnd()
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnGameNetworkEnd();
			}
		}

		// Token: 0x0600078E RID: 1934 RVA: 0x00019B20 File Offset: 0x00017D20
		internal void OnEarlyPlayerConnect(VirtualPlayer peer)
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnEarlyPlayerConnect(peer);
			}
		}

		// Token: 0x0600078F RID: 1935 RVA: 0x00019B78 File Offset: 0x00017D78
		internal void OnPlayerConnect(VirtualPlayer peer)
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnPlayerConnect(peer);
			}
		}

		// Token: 0x06000790 RID: 1936 RVA: 0x00019BD0 File Offset: 0x00017DD0
		internal void OnPlayerDisconnect(VirtualPlayer peer)
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnPlayerDisconnect(peer);
			}
		}

		// Token: 0x06000791 RID: 1937 RVA: 0x00019C28 File Offset: 0x00017E28
		public void OnGameStart()
		{
			foreach (GameHandler gameHandler in this._gameEntitySystem.Components)
			{
				gameHandler.OnGameStart();
			}
		}

		// Token: 0x06000792 RID: 1938 RVA: 0x00019C80 File Offset: 0x00017E80
		public bool DoLoading()
		{
			return this.GameType.DoLoadingForGameType();
		}

		// Token: 0x06000793 RID: 1939 RVA: 0x00019C8D File Offset: 0x00017E8D
		public void OnMissionIsStarting(string missionName, MissionInitializerRecord rec)
		{
			this.GameType.OnMissionIsStarting(missionName, rec);
		}

		// Token: 0x06000794 RID: 1940 RVA: 0x00019C9C File Offset: 0x00017E9C
		public void OnFinalize()
		{
			this.CurrentState = Game.State.Destroying;
			GameStateManager.Current.CleanStates(0);
		}

		// Token: 0x06000795 RID: 1941 RVA: 0x00019CB0 File Offset: 0x00017EB0
		public void InitializeDefaultGameObjects()
		{
			this.DefaultCharacterAttributes = new DefaultCharacterAttributes();
			this.DefaultSkills = new DefaultSkills();
			this.DefaultBannerEffects = new DefaultBannerEffects();
			this.DefaultItemCategories = new DefaultItemCategories();
			this.DefaultSiegeEngineTypes = new DefaultSiegeEngineTypes();
			this.GameManager.InitializeSubModuleGameObjects(Game.Current);
		}

		// Token: 0x06000796 RID: 1942 RVA: 0x00019D04 File Offset: 0x00017F04
		public void LoadBasicFiles()
		{
			this.ObjectManager.LoadXML("Monsters", false);
			this.ObjectManager.LoadXML("SkeletonScales", false);
			this.ObjectManager.LoadXML("ItemModifiers", false);
			this.ObjectManager.LoadXML("ItemModifierGroups", false);
			this.ObjectManager.LoadXML("CraftingPieces", false);
			this.ObjectManager.LoadXML("WeaponDescriptions", false);
			this.ObjectManager.LoadXML("CraftingTemplates", false);
			this.ObjectManager.LoadXML("BodyProperties", false);
			this.ObjectManager.LoadXML("SkillSets", false);
		}

		// Token: 0x06000797 RID: 1943 RVA: 0x00019DAA File Offset: 0x00017FAA
		public void ItemObjectDeserialized(ItemObject itemObject)
		{
			Action<ItemObject> onItemDeserializedEvent = this.OnItemDeserializedEvent;
			if (onItemDeserializedEvent == null)
			{
				return;
			}
			onItemDeserializedEvent(itemObject);
		}

		// Token: 0x040003E4 RID: 996
		public Action<float> AfterTick;

		// Token: 0x040003E7 RID: 999
		private EntitySystem<GameHandler> _gameEntitySystem;

		// Token: 0x040003E8 RID: 1000
		private Monster _defaultMonster;

		// Token: 0x040003EF RID: 1007
		private Dictionary<Type, GameModelsManager> _gameModelManagers;

		// Token: 0x040003F3 RID: 1011
		private static Game _current;

		// Token: 0x040003F5 RID: 1013
		[SaveableField(11)]
		private int _nextUniqueTroopSeed = 1;

		// Token: 0x040003FB RID: 1019
		private IReadOnlyDictionary<string, Equipment> _defaultEquipments;

		// Token: 0x040003FC RID: 1020
		private Tuple<SaveOutput, Action<SaveResult>> _currentActiveSaveData;

		// Token: 0x02000113 RID: 275
		public enum State
		{
			// Token: 0x04000798 RID: 1944
			Running,
			// Token: 0x04000799 RID: 1945
			Destroying,
			// Token: 0x0400079A RID: 1946
			Destroyed
		}
	}
}
