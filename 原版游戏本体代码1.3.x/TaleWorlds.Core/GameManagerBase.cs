using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x0200006C RID: 108
	public abstract class GameManagerBase
	{
		// Token: 0x170002B3 RID: 691
		// (get) Token: 0x060007A8 RID: 1960 RVA: 0x00019DE9 File Offset: 0x00017FE9
		// (set) Token: 0x060007A9 RID: 1961 RVA: 0x00019DF0 File Offset: 0x00017FF0
		public static GameManagerBase Current { get; private set; }

		// Token: 0x170002B4 RID: 692
		// (get) Token: 0x060007AA RID: 1962 RVA: 0x00019DF8 File Offset: 0x00017FF8
		// (set) Token: 0x060007AB RID: 1963 RVA: 0x00019E00 File Offset: 0x00018000
		public Game Game
		{
			get
			{
				return this._game;
			}
			internal set
			{
				if (value == null)
				{
					this._game = null;
					this._initialized = false;
					return;
				}
				this._game = value;
				this.Initialize();
			}
		}

		// Token: 0x060007AC RID: 1964 RVA: 0x00019E21 File Offset: 0x00018021
		public void Initialize()
		{
			if (!this._initialized)
			{
				this._initialized = true;
			}
		}

		// Token: 0x060007AD RID: 1965 RVA: 0x00019E32 File Offset: 0x00018032
		protected GameManagerBase()
		{
			GameManagerBase.Current = this;
			this._entitySystem = new EntitySystem<GameManagerComponent>();
			this._stepNo = GameManagerLoadingSteps.PreInitializeZerothStep;
		}

		// Token: 0x170002B5 RID: 693
		// (get) Token: 0x060007AE RID: 1966 RVA: 0x00019E52 File Offset: 0x00018052
		public IEnumerable<GameManagerComponent> Components
		{
			get
			{
				return this._entitySystem.Components;
			}
		}

		// Token: 0x060007AF RID: 1967 RVA: 0x00019E5F File Offset: 0x0001805F
		public GameManagerComponent AddComponent(Type componentType)
		{
			GameManagerComponent gameManagerComponent = this._entitySystem.AddComponent(componentType);
			gameManagerComponent.GameManager = this;
			return gameManagerComponent;
		}

		// Token: 0x060007B0 RID: 1968 RVA: 0x00019E74 File Offset: 0x00018074
		public T AddComponent<T>() where T : GameManagerComponent, new()
		{
			return (T)((object)this.AddComponent(typeof(T)));
		}

		// Token: 0x060007B1 RID: 1969 RVA: 0x00019E8B File Offset: 0x0001808B
		public GameManagerComponent GetComponent(Type componentType)
		{
			return this._entitySystem.GetComponent(componentType);
		}

		// Token: 0x060007B2 RID: 1970 RVA: 0x00019E99 File Offset: 0x00018099
		public T GetComponent<T>() where T : GameManagerComponent
		{
			return this._entitySystem.GetComponent<T>();
		}

		// Token: 0x060007B3 RID: 1971 RVA: 0x00019EA6 File Offset: 0x000180A6
		public IEnumerable<T> GetComponents<T>() where T : GameManagerComponent
		{
			return this._entitySystem.GetComponents<T>();
		}

		// Token: 0x060007B4 RID: 1972 RVA: 0x00019EB4 File Offset: 0x000180B4
		public void RemoveComponent<T>() where T : GameManagerComponent
		{
			T component = this._entitySystem.GetComponent<T>();
			this.RemoveComponent(component);
		}

		// Token: 0x060007B5 RID: 1973 RVA: 0x00019ED9 File Offset: 0x000180D9
		public void RemoveComponent(GameManagerComponent component)
		{
			this._entitySystem.RemoveComponent(component);
		}

		// Token: 0x060007B6 RID: 1974 RVA: 0x00019EE8 File Offset: 0x000180E8
		public void OnTick(float dt)
		{
			foreach (GameManagerComponent gameManagerComponent in this._entitySystem.Components)
			{
				gameManagerComponent.OnTick();
			}
			if (this.Game != null)
			{
				this.Game.OnTick(dt);
			}
		}

		// Token: 0x060007B7 RID: 1975 RVA: 0x00019F54 File Offset: 0x00018154
		public void OnGameNetworkBegin()
		{
			foreach (GameManagerComponent gameManagerComponent in this._entitySystem.Components)
			{
				gameManagerComponent.OnGameNetworkBegin();
			}
			if (this.Game != null)
			{
				this.Game.OnGameNetworkBegin();
			}
		}

		// Token: 0x060007B8 RID: 1976 RVA: 0x00019FBC File Offset: 0x000181BC
		public void OnGameNetworkEnd()
		{
			foreach (GameManagerComponent gameManagerComponent in this._entitySystem.Components)
			{
				gameManagerComponent.OnGameNetworkEnd();
			}
			if (this.Game != null)
			{
				this.Game.OnGameNetworkEnd();
			}
		}

		// Token: 0x060007B9 RID: 1977 RVA: 0x0001A024 File Offset: 0x00018224
		public void OnPlayerConnect(VirtualPlayer peer)
		{
			foreach (GameManagerComponent gameManagerComponent in this._entitySystem.Components)
			{
				gameManagerComponent.OnEarlyPlayerConnect(peer);
			}
			if (this.Game != null)
			{
				this.Game.OnEarlyPlayerConnect(peer);
			}
			foreach (GameManagerComponent gameManagerComponent2 in this._entitySystem.Components)
			{
				gameManagerComponent2.OnPlayerConnect(peer);
			}
			if (this.Game != null)
			{
				this.Game.OnPlayerConnect(peer);
			}
		}

		// Token: 0x060007BA RID: 1978 RVA: 0x0001A0E8 File Offset: 0x000182E8
		public void OnPlayerDisconnect(VirtualPlayer peer)
		{
			foreach (GameManagerComponent gameManagerComponent in this._entitySystem.Components)
			{
				gameManagerComponent.OnPlayerDisconnect(peer);
			}
			if (this.Game != null)
			{
				this.Game.OnPlayerDisconnect(peer);
			}
		}

		// Token: 0x060007BB RID: 1979 RVA: 0x0001A154 File Offset: 0x00018354
		public virtual void OnGameEnd(Game game)
		{
			GameManagerBase.Current = null;
			this.Game = null;
		}

		// Token: 0x060007BC RID: 1980 RVA: 0x0001A163 File Offset: 0x00018363
		protected virtual void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
		{
			nextStep = GameManagerLoadingSteps.None;
		}

		// Token: 0x060007BD RID: 1981 RVA: 0x0001A168 File Offset: 0x00018368
		public bool DoLoadingForGameManager()
		{
			bool result = false;
			GameManagerLoadingSteps gameManagerLoadingSteps = GameManagerLoadingSteps.None;
			switch (this._stepNo)
			{
			case GameManagerLoadingSteps.PreInitializeZerothStep:
				this.DoLoadingForGameManager(GameManagerLoadingSteps.PreInitializeZerothStep, out gameManagerLoadingSteps);
				if (gameManagerLoadingSteps == GameManagerLoadingSteps.FirstInitializeFirstStep)
				{
					this._stepNo++;
				}
				break;
			case GameManagerLoadingSteps.FirstInitializeFirstStep:
				this.DoLoadingForGameManager(GameManagerLoadingSteps.FirstInitializeFirstStep, out gameManagerLoadingSteps);
				if (gameManagerLoadingSteps == GameManagerLoadingSteps.WaitSecondStep)
				{
					this._stepNo++;
				}
				break;
			case GameManagerLoadingSteps.WaitSecondStep:
				this.DoLoadingForGameManager(GameManagerLoadingSteps.WaitSecondStep, out gameManagerLoadingSteps);
				if (gameManagerLoadingSteps == GameManagerLoadingSteps.SecondInitializeThirdState)
				{
					this._stepNo++;
				}
				break;
			case GameManagerLoadingSteps.SecondInitializeThirdState:
				this.DoLoadingForGameManager(GameManagerLoadingSteps.SecondInitializeThirdState, out gameManagerLoadingSteps);
				if (gameManagerLoadingSteps == GameManagerLoadingSteps.PostInitializeFourthState)
				{
					this._stepNo++;
				}
				break;
			case GameManagerLoadingSteps.PostInitializeFourthState:
				this.DoLoadingForGameManager(GameManagerLoadingSteps.PostInitializeFourthState, out gameManagerLoadingSteps);
				if (gameManagerLoadingSteps == GameManagerLoadingSteps.FinishLoadingFifthStep)
				{
					this._stepNo++;
				}
				break;
			case GameManagerLoadingSteps.FinishLoadingFifthStep:
				this.DoLoadingForGameManager(GameManagerLoadingSteps.FinishLoadingFifthStep, out gameManagerLoadingSteps);
				if (gameManagerLoadingSteps == GameManagerLoadingSteps.None)
				{
					this._stepNo++;
					result = true;
				}
				break;
			case GameManagerLoadingSteps.LoadingIsOver:
				result = true;
				break;
			}
			return result;
		}

		// Token: 0x060007BE RID: 1982 RVA: 0x0001A266 File Offset: 0x00018466
		public virtual void OnLoadFinished()
		{
		}

		// Token: 0x060007BF RID: 1983 RVA: 0x0001A268 File Offset: 0x00018468
		public virtual void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
		}

		// Token: 0x060007C0 RID: 1984
		public abstract void OnGameStart(Game game, IGameStarter gameStarter);

		// Token: 0x060007C1 RID: 1985
		public abstract void BeginGameStart(Game game);

		// Token: 0x060007C2 RID: 1986
		public abstract void OnNewCampaignStart(Game game, object starterObject);

		// Token: 0x060007C3 RID: 1987
		public abstract void OnAfterCampaignStart(Game game);

		// Token: 0x060007C4 RID: 1988
		public abstract void RegisterSubModuleObjects(bool isSavedCampaign);

		// Token: 0x060007C5 RID: 1989
		public abstract void AfterRegisterSubModuleObjects(bool isSavedCampaign);

		// Token: 0x060007C6 RID: 1990
		public abstract void OnGameInitializationFinished(Game game);

		// Token: 0x060007C7 RID: 1991
		public abstract void OnNewGameCreated(Game game, object initializerObject);

		// Token: 0x060007C8 RID: 1992
		public abstract void OnGameLoaded(Game game, object initializerObject);

		// Token: 0x060007C9 RID: 1993
		public abstract void OnAfterGameLoaded(Game game);

		// Token: 0x060007CA RID: 1994
		public abstract void OnAfterGameInitializationFinished(Game game, object initializerObject);

		// Token: 0x060007CB RID: 1995
		public abstract void RegisterSubModuleTypes();

		// Token: 0x060007CC RID: 1996 RVA: 0x0001A26A File Offset: 0x0001846A
		public virtual void InitializeSubModuleGameObjects(Game game)
		{
		}

		// Token: 0x170002B6 RID: 694
		// (get) Token: 0x060007CD RID: 1997
		public abstract float ApplicationTime { get; }

		// Token: 0x170002B7 RID: 695
		// (get) Token: 0x060007CE RID: 1998
		public abstract bool CheatMode { get; }

		// Token: 0x170002B8 RID: 696
		// (get) Token: 0x060007CF RID: 1999
		public abstract bool IsDevelopmentMode { get; }

		// Token: 0x170002B9 RID: 697
		// (get) Token: 0x060007D0 RID: 2000
		public abstract bool IsEditModeOn { get; }

		// Token: 0x170002BA RID: 698
		// (get) Token: 0x060007D1 RID: 2001
		public abstract UnitSpawnPrioritizations UnitSpawnPrioritization { get; }

		// Token: 0x04000406 RID: 1030
		private EntitySystem<GameManagerComponent> _entitySystem;

		// Token: 0x04000407 RID: 1031
		private GameManagerLoadingSteps _stepNo;

		// Token: 0x04000409 RID: 1033
		private Game _game;

		// Token: 0x0400040A RID: 1034
		private bool _initialized;
	}
}
