using System;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x02000009 RID: 9
	[NullableContext(2)]
	[Nullable(0)]
	public class MBSubModuleBaseSimpleWrapper : MBSubModuleBase
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600001E RID: 30 RVA: 0x000032C5 File Offset: 0x000014C5
		private MBSubModuleBaseSimpleWrapper.OnSubModuleLoadDelegate OnSubModuleLoadInstance { get; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600001F RID: 31 RVA: 0x000032CD File Offset: 0x000014CD
		private MBSubModuleBaseSimpleWrapper.OnSubModuleUnloadedDelegate OnSubModuleUnloadedInstance { get; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000020 RID: 32 RVA: 0x000032D5 File Offset: 0x000014D5
		private MBSubModuleBaseSimpleWrapper.OnBeforeInitialModuleScreenSetAsRootDelegate OnBeforeInitialModuleScreenSetAsRootInstance { get; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000021 RID: 33 RVA: 0x000032DD File Offset: 0x000014DD
		private MBSubModuleBaseSimpleWrapper.OnGameStartDelegate OnGameStartInstance { get; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000022 RID: 34 RVA: 0x000032E5 File Offset: 0x000014E5
		private MBSubModuleBaseSimpleWrapper.OnApplicationTickDelegate OnApplicationTickInstance { get; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000023 RID: 35 RVA: 0x000032ED File Offset: 0x000014ED
		private MBSubModuleBaseSimpleWrapper.InitializeGameStarterDelegate InitializeGameStarterInstance { get; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000032F5 File Offset: 0x000014F5
		private MBSubModuleBaseSimpleWrapper.AfterRegisterSubModuleObjectsDelegate AfterRegisterSubModuleObjectsInstance { get; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000025 RID: 37 RVA: 0x000032FD File Offset: 0x000014FD
		private MBSubModuleBaseSimpleWrapper.AfterAsyncTickTickDelegate AfterAsyncTickTickInstance { get; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00003305 File Offset: 0x00001505
		[Nullable(1)]
		public MBSubModuleBase SubModule
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00003310 File Offset: 0x00001510
		[NullableContext(1)]
		public MBSubModuleBaseSimpleWrapper(MBSubModuleBase subModule)
		{
			this.SubModule = subModule;
			this.OnSubModuleLoadInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.OnSubModuleLoadDelegate, MBSubModuleBase>(subModule, "OnSubModuleLoad", null, null, true);
			this.OnSubModuleUnloadedInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.OnSubModuleUnloadedDelegate, MBSubModuleBase>(subModule, "OnSubModuleUnloaded", null, null, true);
			this.OnBeforeInitialModuleScreenSetAsRootInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.OnBeforeInitialModuleScreenSetAsRootDelegate, MBSubModuleBase>(subModule, "OnBeforeInitialModuleScreenSetAsRoot", null, null, true);
			this.OnGameStartInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.OnGameStartDelegate, MBSubModuleBase>(subModule, "OnGameStart", null, null, true);
			this.OnApplicationTickInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.OnApplicationTickDelegate, MBSubModuleBase>(subModule, "OnApplicationTick", null, null, true);
			this.InitializeGameStarterInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.InitializeGameStarterDelegate, MBSubModuleBase>(subModule, "InitializeGameStarter", null, null, true);
			this.AfterRegisterSubModuleObjectsInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.AfterRegisterSubModuleObjectsDelegate, MBSubModuleBase>(subModule, "AfterRegisterSubModuleObjects", null, null, true);
			this.AfterAsyncTickTickInstance = AccessTools2.GetDelegate<MBSubModuleBaseSimpleWrapper.AfterAsyncTickTickDelegate, MBSubModuleBase>(subModule, "AfterAsyncTickTick", null, null, true);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000033CC File Offset: 0x000015CC
		protected override void OnSubModuleLoad()
		{
			MBSubModuleBaseSimpleWrapper.OnSubModuleLoadDelegate onSubModuleLoadInstance = this.OnSubModuleLoadInstance;
			if (onSubModuleLoadInstance != null)
			{
				onSubModuleLoadInstance();
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000033E0 File Offset: 0x000015E0
		protected override void OnSubModuleUnloaded()
		{
			MBSubModuleBaseSimpleWrapper.OnSubModuleUnloadedDelegate onSubModuleUnloadedInstance = this.OnSubModuleUnloadedInstance;
			if (onSubModuleUnloadedInstance != null)
			{
				onSubModuleUnloadedInstance();
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000033F4 File Offset: 0x000015F4
		protected override void OnApplicationTick(float dt)
		{
			MBSubModuleBaseSimpleWrapper.OnApplicationTickDelegate onApplicationTickInstance = this.OnApplicationTickInstance;
			if (onApplicationTickInstance != null)
			{
				onApplicationTickInstance(dt);
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003409 File Offset: 0x00001609
		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			MBSubModuleBaseSimpleWrapper.OnBeforeInitialModuleScreenSetAsRootDelegate onBeforeInitialModuleScreenSetAsRootInstance = this.OnBeforeInitialModuleScreenSetAsRootInstance;
			if (onBeforeInitialModuleScreenSetAsRootInstance != null)
			{
				onBeforeInitialModuleScreenSetAsRootInstance();
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000341D File Offset: 0x0000161D
		[NullableContext(1)]
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			MBSubModuleBaseSimpleWrapper.OnGameStartDelegate onGameStartInstance = this.OnGameStartInstance;
			if (onGameStartInstance != null)
			{
				onGameStartInstance(game, gameStarterObject);
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003433 File Offset: 0x00001633
		[NullableContext(1)]
		protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
			MBSubModuleBaseSimpleWrapper.InitializeGameStarterDelegate initializeGameStarterInstance = this.InitializeGameStarterInstance;
			if (initializeGameStarterInstance != null)
			{
				initializeGameStarterInstance(game, starterObject);
			}
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00003449 File Offset: 0x00001649
		protected override void AfterAsyncTickTick(float dt)
		{
			MBSubModuleBaseSimpleWrapper.AfterAsyncTickTickDelegate afterAsyncTickTickInstance = this.AfterAsyncTickTickInstance;
			if (afterAsyncTickTickInstance != null)
			{
				afterAsyncTickTickInstance(dt);
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x0000345E File Offset: 0x0000165E
		[NullableContext(1)]
		public override bool DoLoading(Game game)
		{
			return this.SubModule.DoLoading(game);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x0000346C File Offset: 0x0000166C
		[NullableContext(1)]
		public override void OnGameLoaded(Game game, object initializerObject)
		{
			this.SubModule.OnGameLoaded(game, initializerObject);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x0000347C File Offset: 0x0000167C
		[NullableContext(1)]
		public override void OnCampaignStart(Game game, object starterObject)
		{
			this.SubModule.OnCampaignStart(game, starterObject);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x0000348C File Offset: 0x0000168C
		[NullableContext(1)]
		public override void BeginGameStart(Game game)
		{
			this.SubModule.BeginGameStart(game);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x0000349B File Offset: 0x0000169B
		[NullableContext(1)]
		public override void OnGameEnd(Game game)
		{
			this.SubModule.OnGameEnd(game);
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000034AA File Offset: 0x000016AA
		[NullableContext(1)]
		public override void OnGameInitializationFinished(Game game)
		{
			this.SubModule.OnGameInitializationFinished(game);
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000034B9 File Offset: 0x000016B9
		[NullableContext(1)]
		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			this.SubModule.OnBeforeMissionBehaviorInitialize(mission);
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000034C8 File Offset: 0x000016C8
		[NullableContext(1)]
		public override void OnMissionBehaviorInitialize(Mission mission)
		{
			this.SubModule.OnMissionBehaviorInitialize(mission);
		}

		// Token: 0x06000037 RID: 55 RVA: 0x000034D7 File Offset: 0x000016D7
		[NullableContext(1)]
		public override void OnMultiplayerGameStart(Game game, object starterObject)
		{
			this.SubModule.OnMultiplayerGameStart(game, starterObject);
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000034E7 File Offset: 0x000016E7
		[NullableContext(1)]
		public override void OnNewGameCreated(Game game, object initializerObject)
		{
			this.SubModule.OnNewGameCreated(game, initializerObject);
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000034F7 File Offset: 0x000016F7
		public override void RegisterSubModuleObjects(bool isSavedCampaign)
		{
			this.SubModule.RegisterSubModuleObjects(isSavedCampaign);
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003506 File Offset: 0x00001706
		[NullableContext(1)]
		public override void OnAfterGameInitializationFinished(Game game, object starterObject)
		{
			this.SubModule.OnAfterGameInitializationFinished(game, starterObject);
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00003516 File Offset: 0x00001716
		public override void AfterRegisterSubModuleObjects(bool isSavedCampaign)
		{
			MBSubModuleBaseSimpleWrapper.AfterRegisterSubModuleObjectsDelegate afterRegisterSubModuleObjectsInstance = this.AfterRegisterSubModuleObjectsInstance;
			if (afterRegisterSubModuleObjectsInstance != null)
			{
				afterRegisterSubModuleObjectsInstance(isSavedCampaign);
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x0000352B File Offset: 0x0000172B
		public override void OnConfigChanged()
		{
			this.SubModule.OnConfigChanged();
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003539 File Offset: 0x00001739
		public override void OnInitialState()
		{
			this.SubModule.OnInitialState();
		}

		// Token: 0x02000048 RID: 72
		// (Invoke) Token: 0x0600041A RID: 1050
		[NullableContext(0)]
		private delegate void OnSubModuleLoadDelegate();

		// Token: 0x02000049 RID: 73
		// (Invoke) Token: 0x0600041E RID: 1054
		[NullableContext(0)]
		private delegate void OnSubModuleUnloadedDelegate();

		// Token: 0x0200004A RID: 74
		// (Invoke) Token: 0x06000422 RID: 1058
		[NullableContext(0)]
		private delegate void OnBeforeInitialModuleScreenSetAsRootDelegate();

		// Token: 0x0200004B RID: 75
		// (Invoke) Token: 0x06000426 RID: 1062
		[NullableContext(0)]
		private delegate void OnGameStartDelegate(Game game, IGameStarter gameStarterObject);

		// Token: 0x0200004C RID: 76
		// (Invoke) Token: 0x0600042A RID: 1066
		[NullableContext(0)]
		private delegate void OnApplicationTickDelegate(float dt);

		// Token: 0x0200004D RID: 77
		// (Invoke) Token: 0x0600042E RID: 1070
		[NullableContext(0)]
		private delegate void InitializeGameStarterDelegate(Game game, IGameStarter starterObject);

		// Token: 0x0200004E RID: 78
		// (Invoke) Token: 0x06000432 RID: 1074
		[NullableContext(0)]
		private delegate void AfterRegisterSubModuleObjectsDelegate(bool isSavedCampaign);

		// Token: 0x0200004F RID: 79
		// (Invoke) Token: 0x06000436 RID: 1078
		[NullableContext(0)]
		private delegate void AfterAsyncTickTickDelegate(float dt);
	}
}
