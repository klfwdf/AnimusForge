using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core
{
	// Token: 0x02000074 RID: 116
	public abstract class GameState : MBObjectBase
	{
		// Token: 0x170002BD RID: 701
		// (get) Token: 0x060007EF RID: 2031 RVA: 0x0001A333 File Offset: 0x00018533
		public GameState Predecessor
		{
			get
			{
				return this.GameStateManager.FindPredecessor(this);
			}
		}

		// Token: 0x170002BE RID: 702
		// (get) Token: 0x060007F0 RID: 2032 RVA: 0x0001A341 File Offset: 0x00018541
		public bool IsActive
		{
			get
			{
				return this.GameStateManager != null && this.GameStateManager.ActiveState == this;
			}
		}

		// Token: 0x170002BF RID: 703
		// (get) Token: 0x060007F1 RID: 2033 RVA: 0x0001A35B File Offset: 0x0001855B
		public IReadOnlyCollection<IGameStateListener> Listeners
		{
			get
			{
				return this._listeners.AsReadOnly();
			}
		}

		// Token: 0x170002C0 RID: 704
		// (get) Token: 0x060007F2 RID: 2034 RVA: 0x0001A368 File Offset: 0x00018568
		// (set) Token: 0x060007F3 RID: 2035 RVA: 0x0001A370 File Offset: 0x00018570
		public GameStateManager GameStateManager { get; internal set; }

		// Token: 0x170002C1 RID: 705
		// (get) Token: 0x060007F4 RID: 2036 RVA: 0x0001A379 File Offset: 0x00018579
		public virtual bool IsMusicMenuState
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170002C2 RID: 706
		// (get) Token: 0x060007F5 RID: 2037 RVA: 0x0001A37C File Offset: 0x0001857C
		public virtual bool IsMenuState
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060007F6 RID: 2038 RVA: 0x0001A37F File Offset: 0x0001857F
		protected GameState()
		{
			this._listeners = new List<IGameStateListener>();
		}

		// Token: 0x060007F7 RID: 2039 RVA: 0x0001A392 File Offset: 0x00018592
		public bool RegisterListener(IGameStateListener listener)
		{
			if (listener == null)
			{
				Debug.FailedAssert("Can not register null listener to game state.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\GameState.cs", "RegisterListener", 47);
			}
			if (this._listeners.Contains(listener))
			{
				return false;
			}
			this._listeners.Add(listener);
			return true;
		}

		// Token: 0x060007F8 RID: 2040 RVA: 0x0001A3CA File Offset: 0x000185CA
		public bool UnregisterListener(IGameStateListener listener)
		{
			return this._listeners.Remove(listener);
		}

		// Token: 0x060007F9 RID: 2041 RVA: 0x0001A3D8 File Offset: 0x000185D8
		public T GetListenerOfType<T>()
		{
			using (List<IGameStateListener>.Enumerator enumerator = this._listeners.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					IGameStateListener gameStateListener;
					if ((gameStateListener = enumerator.Current) is T)
					{
						return (T)((object)gameStateListener);
					}
				}
			}
			return default(T);
		}

		// Token: 0x060007FA RID: 2042 RVA: 0x0001A444 File Offset: 0x00018644
		internal void HandleInitialize()
		{
			this.OnInitialize();
			foreach (IGameStateListener gameStateListener in this._listeners)
			{
				gameStateListener.OnInitialize();
			}
		}

		// Token: 0x060007FB RID: 2043 RVA: 0x0001A49C File Offset: 0x0001869C
		protected virtual void OnInitialize()
		{
		}

		// Token: 0x060007FC RID: 2044 RVA: 0x0001A4A0 File Offset: 0x000186A0
		internal void HandleFinalize()
		{
			this.OnFinalize();
			foreach (IGameStateListener gameStateListener in this._listeners)
			{
				gameStateListener.OnFinalize();
			}
			this._listeners = null;
			this.GameStateManager = null;
		}

		// Token: 0x060007FD RID: 2045 RVA: 0x0001A504 File Offset: 0x00018704
		protected virtual void OnFinalize()
		{
		}

		// Token: 0x060007FE RID: 2046 RVA: 0x0001A508 File Offset: 0x00018708
		internal void HandleActivate()
		{
			GameState.NumberOfListenerActivations = 0;
			if (this.IsActive)
			{
				this.OnActivate();
				if (this.IsActive && this._listeners.Count != 0 && GameState.NumberOfListenerActivations == 0)
				{
					foreach (IGameStateListener gameStateListener in this._listeners)
					{
						gameStateListener.OnActivate();
					}
					GameState.NumberOfListenerActivations++;
				}
				if (!string.IsNullOrEmpty(GameStateManager.StateActivateCommand))
				{
					bool flag;
					CommandLineFunctionality.CallFunction(GameStateManager.StateActivateCommand, "", out flag);
				}
				Debug.ReportMemoryBookmark("GameState Activated: " + base.GetType().Name);
			}
		}

		// Token: 0x170002C3 RID: 707
		// (get) Token: 0x060007FF RID: 2047 RVA: 0x0001A5D0 File Offset: 0x000187D0
		// (set) Token: 0x06000800 RID: 2048 RVA: 0x0001A5D8 File Offset: 0x000187D8
		public bool Activated { get; private set; }

		// Token: 0x06000801 RID: 2049 RVA: 0x0001A5E1 File Offset: 0x000187E1
		protected virtual void OnActivate()
		{
			this.Activated = true;
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x0001A5EC File Offset: 0x000187EC
		internal void HandleDeactivate()
		{
			this.OnDeactivate();
			foreach (IGameStateListener gameStateListener in this._listeners)
			{
				gameStateListener.OnDeactivate();
			}
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x0001A644 File Offset: 0x00018844
		protected virtual void OnDeactivate()
		{
			this.Activated = false;
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x0001A64D File Offset: 0x0001884D
		protected internal virtual void OnTick(float dt)
		{
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x0001A64F File Offset: 0x0001884F
		protected internal virtual void OnIdleTick(float dt)
		{
		}

		// Token: 0x0400040D RID: 1037
		public int Level;

		// Token: 0x0400040E RID: 1038
		private List<IGameStateListener> _listeners;

		// Token: 0x0400040F RID: 1039
		public static int NumberOfListenerActivations;
	}
}
