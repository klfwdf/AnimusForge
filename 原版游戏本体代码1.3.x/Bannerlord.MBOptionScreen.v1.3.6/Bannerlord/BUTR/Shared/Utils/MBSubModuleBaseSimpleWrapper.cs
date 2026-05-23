using System;
using System.Runtime.CompilerServices;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.BUTR.Shared.Utils
{
	// Token: 0x0200004B RID: 75
	[NullableContext(2)]
	[Nullable(0)]
	public class MBSubModuleBaseSimpleWrapper : MBSubModuleBase
	{
		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000227 RID: 551 RVA: 0x0000890D File Offset: 0x00006B0D
		private MBSubModuleBaseSimpleWrapper.OnSubModuleLoadDelegate OnSubModuleLoadInstance { get; }

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x06000228 RID: 552 RVA: 0x00008915 File Offset: 0x00006B15
		private MBSubModuleBaseSimpleWrapper.OnSubModuleUnloadedDelegate OnSubModuleUnloadedInstance { get; }

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x06000229 RID: 553 RVA: 0x0000891D File Offset: 0x00006B1D
		private MBSubModuleBaseSimpleWrapper.OnBeforeInitialModuleScreenSetAsRootDelegate OnBeforeInitialModuleScreenSetAsRootInstance { get; }

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x0600022A RID: 554 RVA: 0x00008925 File Offset: 0x00006B25
		private MBSubModuleBaseSimpleWrapper.OnGameStartDelegate OnGameStartInstance { get; }

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x0600022B RID: 555 RVA: 0x0000892D File Offset: 0x00006B2D
		private MBSubModuleBaseSimpleWrapper.OnApplicationTickDelegate OnApplicationTickInstance { get; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x0600022C RID: 556 RVA: 0x00008935 File Offset: 0x00006B35
		private MBSubModuleBaseSimpleWrapper.InitializeGameStarterDelegate InitializeGameStarterInstance { get; }

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x0600022D RID: 557 RVA: 0x0000893D File Offset: 0x00006B3D
		private MBSubModuleBaseSimpleWrapper.AfterRegisterSubModuleObjectsDelegate AfterRegisterSubModuleObjectsInstance { get; }

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x0600022E RID: 558 RVA: 0x00008945 File Offset: 0x00006B45
		private MBSubModuleBaseSimpleWrapper.AfterAsyncTickTickDelegate AfterAsyncTickTickInstance { get; }

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x0600022F RID: 559 RVA: 0x0000894D File Offset: 0x00006B4D
		[Nullable(1)]
		public MBSubModuleBase SubModule
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x06000230 RID: 560 RVA: 0x00008958 File Offset: 0x00006B58
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

		// Token: 0x06000231 RID: 561 RVA: 0x00008A12 File Offset: 0x00006C12
		protected override void OnSubModuleLoad()
		{
			MBSubModuleBaseSimpleWrapper.OnSubModuleLoadDelegate onSubModuleLoadInstance = this.OnSubModuleLoadInstance;
			if (onSubModuleLoadInstance == null)
			{
				return;
			}
			onSubModuleLoadInstance();
		}

		// Token: 0x06000232 RID: 562 RVA: 0x00008A24 File Offset: 0x00006C24
		protected override void OnSubModuleUnloaded()
		{
			MBSubModuleBaseSimpleWrapper.OnSubModuleUnloadedDelegate onSubModuleUnloadedInstance = this.OnSubModuleUnloadedInstance;
			if (onSubModuleUnloadedInstance == null)
			{
				return;
			}
			onSubModuleUnloadedInstance();
		}

		// Token: 0x06000233 RID: 563 RVA: 0x00008A36 File Offset: 0x00006C36
		protected override void OnApplicationTick(float dt)
		{
			MBSubModuleBaseSimpleWrapper.OnApplicationTickDelegate onApplicationTickInstance = this.OnApplicationTickInstance;
			if (onApplicationTickInstance == null)
			{
				return;
			}
			onApplicationTickInstance(dt);
		}

		// Token: 0x06000234 RID: 564 RVA: 0x00008A49 File Offset: 0x00006C49
		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
			MBSubModuleBaseSimpleWrapper.OnBeforeInitialModuleScreenSetAsRootDelegate onBeforeInitialModuleScreenSetAsRootInstance = this.OnBeforeInitialModuleScreenSetAsRootInstance;
			if (onBeforeInitialModuleScreenSetAsRootInstance == null)
			{
				return;
			}
			onBeforeInitialModuleScreenSetAsRootInstance();
		}

		// Token: 0x06000235 RID: 565 RVA: 0x00008A5B File Offset: 0x00006C5B
		[NullableContext(1)]
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			MBSubModuleBaseSimpleWrapper.OnGameStartDelegate onGameStartInstance = this.OnGameStartInstance;
			if (onGameStartInstance == null)
			{
				return;
			}
			onGameStartInstance(game, gameStarterObject);
		}

		// Token: 0x06000236 RID: 566 RVA: 0x00008A6F File Offset: 0x00006C6F
		[NullableContext(1)]
		protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
			MBSubModuleBaseSimpleWrapper.InitializeGameStarterDelegate initializeGameStarterInstance = this.InitializeGameStarterInstance;
			if (initializeGameStarterInstance == null)
			{
				return;
			}
			initializeGameStarterInstance(game, starterObject);
		}

		// Token: 0x06000237 RID: 567 RVA: 0x00008A83 File Offset: 0x00006C83
		protected override void AfterAsyncTickTick(float dt)
		{
			MBSubModuleBaseSimpleWrapper.AfterAsyncTickTickDelegate afterAsyncTickTickInstance = this.AfterAsyncTickTickInstance;
			if (afterAsyncTickTickInstance == null)
			{
				return;
			}
			afterAsyncTickTickInstance(dt);
		}

		// Token: 0x06000238 RID: 568 RVA: 0x00008A96 File Offset: 0x00006C96
		[NullableContext(1)]
		public override bool DoLoading(Game game)
		{
			return this.SubModule.DoLoading(game);
		}

		// Token: 0x06000239 RID: 569 RVA: 0x00008AA4 File Offset: 0x00006CA4
		[NullableContext(1)]
		public override void OnGameLoaded(Game game, object initializerObject)
		{
			this.SubModule.OnGameLoaded(game, initializerObject);
		}

		// Token: 0x0600023A RID: 570 RVA: 0x00008AB3 File Offset: 0x00006CB3
		[NullableContext(1)]
		public override void OnCampaignStart(Game game, object starterObject)
		{
			this.SubModule.OnCampaignStart(game, starterObject);
		}

		// Token: 0x0600023B RID: 571 RVA: 0x00008AC2 File Offset: 0x00006CC2
		[NullableContext(1)]
		public override void BeginGameStart(Game game)
		{
			this.SubModule.BeginGameStart(game);
		}

		// Token: 0x0600023C RID: 572 RVA: 0x00008AD0 File Offset: 0x00006CD0
		[NullableContext(1)]
		public override void OnGameEnd(Game game)
		{
			this.SubModule.OnGameEnd(game);
		}

		// Token: 0x0600023D RID: 573 RVA: 0x00008ADE File Offset: 0x00006CDE
		[NullableContext(1)]
		public override void OnGameInitializationFinished(Game game)
		{
			this.SubModule.OnGameInitializationFinished(game);
		}

		// Token: 0x0600023E RID: 574 RVA: 0x00008AEC File Offset: 0x00006CEC
		[NullableContext(1)]
		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			this.SubModule.OnBeforeMissionBehaviorInitialize(mission);
		}

		// Token: 0x0600023F RID: 575 RVA: 0x00008AFA File Offset: 0x00006CFA
		[NullableContext(1)]
		public override void OnMissionBehaviorInitialize(Mission mission)
		{
			this.SubModule.OnMissionBehaviorInitialize(mission);
		}

		// Token: 0x06000240 RID: 576 RVA: 0x00008B08 File Offset: 0x00006D08
		[NullableContext(1)]
		public override void OnMultiplayerGameStart(Game game, object starterObject)
		{
			this.SubModule.OnMultiplayerGameStart(game, starterObject);
		}

		// Token: 0x06000241 RID: 577 RVA: 0x00008B17 File Offset: 0x00006D17
		[NullableContext(1)]
		public override void OnNewGameCreated(Game game, object initializerObject)
		{
			this.SubModule.OnNewGameCreated(game, initializerObject);
		}

		// Token: 0x06000242 RID: 578 RVA: 0x00008B26 File Offset: 0x00006D26
		public override void RegisterSubModuleObjects(bool isSavedCampaign)
		{
			this.SubModule.RegisterSubModuleObjects(isSavedCampaign);
		}

		// Token: 0x06000243 RID: 579 RVA: 0x00008B34 File Offset: 0x00006D34
		[NullableContext(1)]
		public override void OnAfterGameInitializationFinished(Game game, object starterObject)
		{
			this.SubModule.OnAfterGameInitializationFinished(game, starterObject);
		}

		// Token: 0x06000244 RID: 580 RVA: 0x00008B43 File Offset: 0x00006D43
		public override void AfterRegisterSubModuleObjects(bool isSavedCampaign)
		{
			MBSubModuleBaseSimpleWrapper.AfterRegisterSubModuleObjectsDelegate afterRegisterSubModuleObjectsInstance = this.AfterRegisterSubModuleObjectsInstance;
			if (afterRegisterSubModuleObjectsInstance == null)
			{
				return;
			}
			afterRegisterSubModuleObjectsInstance(isSavedCampaign);
		}

		// Token: 0x06000245 RID: 581 RVA: 0x00008B56 File Offset: 0x00006D56
		public override void OnConfigChanged()
		{
			this.SubModule.OnConfigChanged();
		}

		// Token: 0x06000246 RID: 582 RVA: 0x00008B63 File Offset: 0x00006D63
		public override void OnInitialState()
		{
			this.SubModule.OnInitialState();
		}

		// Token: 0x020000AD RID: 173
		// (Invoke) Token: 0x0600055C RID: 1372
		[NullableContext(0)]
		private delegate void OnSubModuleLoadDelegate();

		// Token: 0x020000AE RID: 174
		// (Invoke) Token: 0x06000560 RID: 1376
		[NullableContext(0)]
		private delegate void OnSubModuleUnloadedDelegate();

		// Token: 0x020000AF RID: 175
		// (Invoke) Token: 0x06000564 RID: 1380
		[NullableContext(0)]
		private delegate void OnBeforeInitialModuleScreenSetAsRootDelegate();

		// Token: 0x020000B0 RID: 176
		// (Invoke) Token: 0x06000568 RID: 1384
		[NullableContext(0)]
		private delegate void OnGameStartDelegate(Game game, IGameStarter gameStarterObject);

		// Token: 0x020000B1 RID: 177
		// (Invoke) Token: 0x0600056C RID: 1388
		[NullableContext(0)]
		private delegate void OnApplicationTickDelegate(float dt);

		// Token: 0x020000B2 RID: 178
		// (Invoke) Token: 0x06000570 RID: 1392
		[NullableContext(0)]
		private delegate void InitializeGameStarterDelegate(Game game, IGameStarter starterObject);

		// Token: 0x020000B3 RID: 179
		// (Invoke) Token: 0x06000574 RID: 1396
		[NullableContext(0)]
		private delegate void AfterRegisterSubModuleObjectsDelegate(bool isSavedCampaign);

		// Token: 0x020000B4 RID: 180
		// (Invoke) Token: 0x06000578 RID: 1400
		[NullableContext(0)]
		private delegate void AfterAsyncTickTickDelegate(float dt);
	}
}
