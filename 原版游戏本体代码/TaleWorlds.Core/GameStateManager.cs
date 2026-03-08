using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Core
{
	// Token: 0x02000075 RID: 117
	public class GameStateManager
	{
		// Token: 0x170002C4 RID: 708
		// (get) Token: 0x06000806 RID: 2054 RVA: 0x0001A651 File Offset: 0x00018851
		// (set) Token: 0x06000807 RID: 2055 RVA: 0x0001A658 File Offset: 0x00018858
		public static GameStateManager Current
		{
			get
			{
				return GameStateManager._current;
			}
			set
			{
				GameStateManager current = GameStateManager._current;
				if (current != null)
				{
					current.CleanStates(0);
				}
				GameStateManager._current = value;
			}
		}

		// Token: 0x170002C5 RID: 709
		// (get) Token: 0x06000808 RID: 2056 RVA: 0x0001A671 File Offset: 0x00018871
		public IReadOnlyCollection<IGameStateManagerListener> Listeners
		{
			get
			{
				return this._listeners.AsReadOnly();
			}
		}

		// Token: 0x170002C6 RID: 710
		// (get) Token: 0x06000809 RID: 2057 RVA: 0x0001A67E File Offset: 0x0001887E
		// (set) Token: 0x0600080A RID: 2058 RVA: 0x0001A686 File Offset: 0x00018886
		public GameStateManager.GameStateManagerType CurrentType { get; private set; }

		// Token: 0x170002C7 RID: 711
		// (get) Token: 0x0600080B RID: 2059 RVA: 0x0001A68F File Offset: 0x0001888F
		// (set) Token: 0x0600080C RID: 2060 RVA: 0x0001A697 File Offset: 0x00018897
		public IGameStateManagerOwner Owner { get; private set; }

		// Token: 0x170002C8 RID: 712
		// (get) Token: 0x0600080D RID: 2061 RVA: 0x0001A6A0 File Offset: 0x000188A0
		public IEnumerable<GameState> GameStates
		{
			get
			{
				return this._gameStates.AsReadOnly();
			}
		}

		// Token: 0x170002C9 RID: 713
		// (get) Token: 0x0600080E RID: 2062 RVA: 0x0001A6AD File Offset: 0x000188AD
		public bool ActiveStateDisabledByUser
		{
			get
			{
				return this._activeStateDisableRequests.Count > 0;
			}
		}

		// Token: 0x170002CA RID: 714
		// (get) Token: 0x0600080F RID: 2063 RVA: 0x0001A6BD File Offset: 0x000188BD
		public GameState ActiveState
		{
			get
			{
				if (this._gameStates.Count <= 0)
				{
					return null;
				}
				return this._gameStates[this._gameStates.Count - 1];
			}
		}

		// Token: 0x06000810 RID: 2064 RVA: 0x0001A6E8 File Offset: 0x000188E8
		public GameStateManager(IGameStateManagerOwner owner, GameStateManager.GameStateManagerType gameStateManagerType)
		{
			this.Owner = owner;
			this.CurrentType = gameStateManagerType;
			this._gameStateJobs = new Queue<GameStateManager.GameStateJob>();
			this._gameStates = new List<GameState>();
			this._listeners = new List<IGameStateManagerListener>();
			this._activeStateDisableRequests = new List<WeakReference>();
		}

		// Token: 0x06000811 RID: 2065 RVA: 0x0001A738 File Offset: 0x00018938
		internal GameState FindPredecessor(GameState gameState)
		{
			GameState result = null;
			int num = this._gameStates.IndexOf(gameState);
			if (num > 0)
			{
				result = this._gameStates[num - 1];
			}
			return result;
		}

		// Token: 0x06000812 RID: 2066 RVA: 0x0001A768 File Offset: 0x00018968
		public bool RegisterListener(IGameStateManagerListener listener)
		{
			if (this._listeners.Contains(listener))
			{
				return false;
			}
			this._listeners.Add(listener);
			return true;
		}

		// Token: 0x06000813 RID: 2067 RVA: 0x0001A787 File Offset: 0x00018987
		public bool UnregisterListener(IGameStateManagerListener listener)
		{
			return this._listeners.Remove(listener);
		}

		// Token: 0x06000814 RID: 2068 RVA: 0x0001A798 File Offset: 0x00018998
		public T GetListenerOfType<T>()
		{
			using (List<IGameStateManagerListener>.Enumerator enumerator = this._listeners.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					IGameStateManagerListener gameStateManagerListener;
					if ((gameStateManagerListener = enumerator.Current) is T)
					{
						return (T)((object)gameStateManagerListener);
					}
				}
			}
			return default(T);
		}

		// Token: 0x06000815 RID: 2069 RVA: 0x0001A804 File Offset: 0x00018A04
		public void RegisterActiveStateDisableRequest(object requestingInstance)
		{
			if (!this._activeStateDisableRequests.Contains(requestingInstance))
			{
				this._activeStateDisableRequests.Add(new WeakReference(requestingInstance));
			}
		}

		// Token: 0x06000816 RID: 2070 RVA: 0x0001A828 File Offset: 0x00018A28
		public void UnregisterActiveStateDisableRequest(object requestingInstance)
		{
			for (int i = 0; i < this._activeStateDisableRequests.Count; i++)
			{
				WeakReference weakReference = this._activeStateDisableRequests[i];
				if (((weakReference != null) ? weakReference.Target : null) == requestingInstance)
				{
					this._activeStateDisableRequests.RemoveAt(i);
					return;
				}
			}
		}

		// Token: 0x06000817 RID: 2071 RVA: 0x0001A874 File Offset: 0x00018A74
		public void OnSavedGameLoadFinished()
		{
			foreach (IGameStateManagerListener gameStateManagerListener in this._listeners)
			{
				gameStateManagerListener.OnSavedGameLoadFinished();
			}
		}

		// Token: 0x06000818 RID: 2072 RVA: 0x0001A8C4 File Offset: 0x00018AC4
		public T LastOrDefault<T>() where T : GameState
		{
			return this._gameStates.LastOrDefault((GameState g) => g is T) as T;
		}

		// Token: 0x06000819 RID: 2073 RVA: 0x0001A8FC File Offset: 0x00018AFC
		public T CreateState<T>() where T : GameState, new()
		{
			T t = Activator.CreateInstance<T>();
			this.HandleCreateState(t);
			return t;
		}

		// Token: 0x0600081A RID: 2074 RVA: 0x0001A91C File Offset: 0x00018B1C
		public T CreateState<T>(params object[] parameters) where T : GameState, new()
		{
			GameState gameState = (GameState)Activator.CreateInstance(typeof(T), parameters);
			this.HandleCreateState(gameState);
			return (T)((object)gameState);
		}

		// Token: 0x0600081B RID: 2075 RVA: 0x0001A94C File Offset: 0x00018B4C
		private void HandleCreateState(GameState state)
		{
			state.GameStateManager = this;
			foreach (IGameStateManagerListener gameStateManagerListener in this._listeners)
			{
				gameStateManagerListener.OnCreateState(state);
			}
		}

		// Token: 0x0600081C RID: 2076 RVA: 0x0001A9A4 File Offset: 0x00018BA4
		public void OnTick(float dt)
		{
			this.CleanRequests();
			if (this.ActiveState != null)
			{
				if (this.ActiveStateDisabledByUser)
				{
					this.ActiveState.OnIdleTick(dt);
					return;
				}
				this.ActiveState.OnTick(dt);
			}
		}

		// Token: 0x0600081D RID: 2077 RVA: 0x0001A9D8 File Offset: 0x00018BD8
		private void CleanRequests()
		{
			for (int i = this._activeStateDisableRequests.Count - 1; i >= 0; i--)
			{
				WeakReference weakReference = this._activeStateDisableRequests[i];
				if (weakReference == null || !weakReference.IsAlive)
				{
					this._activeStateDisableRequests.RemoveAt(i);
				}
			}
		}

		// Token: 0x0600081E RID: 2078 RVA: 0x0001AA28 File Offset: 0x00018C28
		public void PushState(GameState gameState, int level = 0)
		{
			GameStateManager.GameStateJob item = new GameStateManager.GameStateJob(GameStateManager.GameStateJob.JobType.Push, gameState, level);
			this._gameStateJobs.Enqueue(item);
			this.DoGameStateJobs();
		}

		// Token: 0x0600081F RID: 2079 RVA: 0x0001AA54 File Offset: 0x00018C54
		public void PopState(int level = 0)
		{
			GameStateManager.GameStateJob item = new GameStateManager.GameStateJob(GameStateManager.GameStateJob.JobType.Pop, null, level);
			this._gameStateJobs.Enqueue(item);
			this.DoGameStateJobs();
		}

		// Token: 0x06000820 RID: 2080 RVA: 0x0001AA80 File Offset: 0x00018C80
		public void CleanAndPushState(GameState gameState, int level = 0)
		{
			GameStateManager.GameStateJob item = new GameStateManager.GameStateJob(GameStateManager.GameStateJob.JobType.CleanAndPushState, gameState, level);
			this._gameStateJobs.Enqueue(item);
			this.DoGameStateJobs();
		}

		// Token: 0x06000821 RID: 2081 RVA: 0x0001AAAC File Offset: 0x00018CAC
		public void CleanStates(int level = 0)
		{
			GameStateManager.GameStateJob item = new GameStateManager.GameStateJob(GameStateManager.GameStateJob.JobType.CleanStates, null, level);
			this._gameStateJobs.Enqueue(item);
			this.DoGameStateJobs();
		}

		// Token: 0x06000822 RID: 2082 RVA: 0x0001AAD8 File Offset: 0x00018CD8
		private void OnPushState(GameState gameState)
		{
			GameState activeState = this.ActiveState;
			bool isTopGameState = this._gameStates.Count == 0;
			int num = this._gameStates.FindLastIndex((GameState state) => state.Level <= gameState.Level);
			if (num == -1)
			{
				this._gameStates.Add(gameState);
			}
			else
			{
				this._gameStates.Insert(num + 1, gameState);
			}
			GameState activeState2 = this.ActiveState;
			if (activeState2 != activeState)
			{
				if (activeState != null && activeState.Activated)
				{
					activeState.HandleDeactivate();
				}
				foreach (IGameStateManagerListener gameStateManagerListener in this._listeners)
				{
					gameStateManagerListener.OnPushState(activeState2, isTopGameState);
				}
				activeState2.HandleInitialize();
				activeState2.HandleActivate();
				this.Owner.OnStateChanged(activeState);
			}
			Common.MemoryCleanupGC(false);
		}

		// Token: 0x06000823 RID: 2083 RVA: 0x0001ABD0 File Offset: 0x00018DD0
		private void OnPopState(int level)
		{
			GameState activeState = this.ActiveState;
			int index = this._gameStates.FindLastIndex((GameState state) => state.Level == level);
			GameState gameState = this._gameStates[index];
			gameState.HandleDeactivate();
			gameState.HandleFinalize();
			this._gameStates.RemoveAt(index);
			GameState activeState2 = this.ActiveState;
			foreach (IGameStateManagerListener gameStateManagerListener in this._listeners)
			{
				gameStateManagerListener.OnPopState(gameState);
			}
			if (activeState2 != activeState)
			{
				if (activeState2 != null)
				{
					activeState2.HandleActivate();
				}
				else if (this._gameStateJobs.Count == 0 || (this._gameStateJobs.Peek().Job != GameStateManager.GameStateJob.JobType.Push && this._gameStateJobs.Peek().Job != GameStateManager.GameStateJob.JobType.CleanAndPushState))
				{
					this.Owner.OnStateStackEmpty();
				}
				this.Owner.OnStateChanged(gameState);
			}
			Common.MemoryCleanupGC(false);
		}

		// Token: 0x06000824 RID: 2084 RVA: 0x0001ACE0 File Offset: 0x00018EE0
		private void OnCleanAndPushState(GameState gameState)
		{
			int num = -1;
			for (int i = 0; i < this._gameStates.Count; i++)
			{
				if (this._gameStates[i].Level >= gameState.Level)
				{
					num = i - 1;
					break;
				}
			}
			GameState activeState = this.ActiveState;
			for (int j = this._gameStates.Count - 1; j > num; j--)
			{
				GameState gameState2 = this._gameStates[j];
				if (gameState2.Activated)
				{
					gameState2.HandleDeactivate();
				}
				gameState2.HandleFinalize();
				this._gameStates.RemoveAt(j);
			}
			this.OnPushState(gameState);
			this.Owner.OnStateChanged(activeState);
		}

		// Token: 0x06000825 RID: 2085 RVA: 0x0001AD88 File Offset: 0x00018F88
		private void OnCleanStates(int popLevel)
		{
			int num = -1;
			for (int i = 0; i < this._gameStates.Count; i++)
			{
				if (this._gameStates[i].Level >= popLevel)
				{
					num = i - 1;
					break;
				}
			}
			GameState activeState = this.ActiveState;
			for (int j = this._gameStates.Count - 1; j > num; j--)
			{
				GameState gameState = this._gameStates[j];
				if (gameState.Activated)
				{
					gameState.HandleDeactivate();
				}
				gameState.HandleFinalize();
				this._gameStates.RemoveAt(j);
			}
			foreach (IGameStateManagerListener gameStateManagerListener in this._listeners)
			{
				gameStateManagerListener.OnCleanStates();
			}
			GameState activeState2 = this.ActiveState;
			if (activeState != activeState2)
			{
				if (activeState2 != null)
				{
					activeState2.HandleActivate();
				}
				else if (this._gameStateJobs.Count == 0 || (this._gameStateJobs.Peek().Job != GameStateManager.GameStateJob.JobType.Push && this._gameStateJobs.Peek().Job != GameStateManager.GameStateJob.JobType.CleanAndPushState))
				{
					this.Owner.OnStateStackEmpty();
				}
				this.Owner.OnStateChanged(activeState);
			}
		}

		// Token: 0x06000826 RID: 2086 RVA: 0x0001AEC4 File Offset: 0x000190C4
		private void DoGameStateJobs()
		{
			while (this._gameStateJobs.Count > 0)
			{
				GameStateManager.GameStateJob gameStateJob = this._gameStateJobs.Dequeue();
				switch (gameStateJob.Job)
				{
				case GameStateManager.GameStateJob.JobType.Push:
					this.OnPushState(gameStateJob.GameState);
					break;
				case GameStateManager.GameStateJob.JobType.Pop:
					this.OnPopState(gameStateJob.PopLevel);
					break;
				case GameStateManager.GameStateJob.JobType.CleanAndPushState:
					this.OnCleanAndPushState(gameStateJob.GameState);
					break;
				case GameStateManager.GameStateJob.JobType.CleanStates:
					this.OnCleanStates(gameStateJob.PopLevel);
					break;
				}
			}
		}

		// Token: 0x04000412 RID: 1042
		private static GameStateManager _current;

		// Token: 0x04000413 RID: 1043
		public static string StateActivateCommand;

		// Token: 0x04000416 RID: 1046
		private readonly List<GameState> _gameStates;

		// Token: 0x04000417 RID: 1047
		private readonly List<IGameStateManagerListener> _listeners;

		// Token: 0x04000418 RID: 1048
		private readonly List<WeakReference> _activeStateDisableRequests;

		// Token: 0x04000419 RID: 1049
		private readonly Queue<GameStateManager.GameStateJob> _gameStateJobs;

		// Token: 0x02000114 RID: 276
		public enum GameStateManagerType
		{
			// Token: 0x0400079C RID: 1948
			Game,
			// Token: 0x0400079D RID: 1949
			Global
		}

		// Token: 0x02000115 RID: 277
		private struct GameStateJob
		{
			// Token: 0x06000BE4 RID: 3044 RVA: 0x00025FBD File Offset: 0x000241BD
			public GameStateJob(GameStateManager.GameStateJob.JobType job, GameState gameState, int popLevel)
			{
				this.Job = job;
				this.GameState = gameState;
				this.PopLevel = popLevel;
			}

			// Token: 0x0400079E RID: 1950
			public readonly GameStateManager.GameStateJob.JobType Job;

			// Token: 0x0400079F RID: 1951
			public readonly GameState GameState;

			// Token: 0x040007A0 RID: 1952
			public readonly int PopLevel;

			// Token: 0x02000141 RID: 321
			public enum JobType
			{
				// Token: 0x04000837 RID: 2103
				None,
				// Token: 0x04000838 RID: 2104
				Push,
				// Token: 0x04000839 RID: 2105
				Pop,
				// Token: 0x0400083A RID: 2106
				CleanAndPushState,
				// Token: 0x0400083B RID: 2107
				CleanStates
			}
		}
	}
}
