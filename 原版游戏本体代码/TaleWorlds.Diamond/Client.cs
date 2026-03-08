using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.Library;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000005 RID: 5
	public abstract class Client<T> : DiamondClientApplicationObject, IClient where T : Client<T>
	{
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000E RID: 14 RVA: 0x00002428 File Offset: 0x00000628
		// (set) Token: 0x0600000F RID: 15 RVA: 0x00002430 File Offset: 0x00000630
		public bool IsInCriticalState { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000010 RID: 16 RVA: 0x00002439 File Offset: 0x00000639
		public virtual long AliveCheckTimeInMiliSeconds
		{
			get
			{
				return 2000L;
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002441 File Offset: 0x00000641
		protected Client(DiamondClientApplication diamondClientApplication, IClientSessionProvider<T> sessionProvider, bool autoReconnect)
			: base(diamondClientApplication)
		{
			this._clientSession = sessionProvider.CreateSession((T)((object)this));
			this._messageHandlers = new Dictionary<Type, Delegate>();
			this._autoReconnect = autoReconnect;
			if (autoReconnect)
			{
				this.Reset();
				this._connectionState = Client<T>.ConnectionState.ReadyToConnect;
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002480 File Offset: 0x00000680
		public void Update()
		{
			this._clientSession.Tick();
			if (this._connectionState == Client<T>.ConnectionState.SleepingToConnectAgain)
			{
				if (this._timer.ElapsedMilliseconds > 5000L)
				{
					this._connectionState = Client<T>.ConnectionState.ReadyToConnect;
					this._timer.Stop();
					this._timer = null;
				}
			}
			else if (this._connectionState == Client<T>.ConnectionState.ReadyToConnect)
			{
				this._connectionState = Client<T>.ConnectionState.Connecting;
				this._clientSession.Connect();
			}
			else
			{
				Client<T>.ConnectionState connectionState = this._connectionState;
			}
			this.OnTick();
		}

		// Token: 0x06000013 RID: 19
		protected abstract void OnTick();

		// Token: 0x06000014 RID: 20 RVA: 0x000024FB File Offset: 0x000006FB
		protected void SendMessage(Message message)
		{
			this._clientSession.SendMessage(message);
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000015 RID: 21 RVA: 0x00002509 File Offset: 0x00000709
		// (set) Token: 0x06000016 RID: 22 RVA: 0x00002511 File Offset: 0x00000711
		public ILoginAccessProvider AccessProvider { get; protected set; }

		// Token: 0x06000017 RID: 23 RVA: 0x0000251C File Offset: 0x0000071C
		protected async Task<LoginResult> Login(LoginMessage message)
		{
			Debug.Print("Logging in", 0, Debug.DebugColor.White, 17592186044416UL);
			return await this._clientSession.Login(message);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x0000256C File Offset: 0x0000076C
		protected async Task<TResult> CallFunction<TResult>(Message message) where TResult : FunctionResult
		{
			return await this._clientSession.CallFunction<TResult>(message);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000025B9 File Offset: 0x000007B9
		protected void AddMessageHandler<TMessage>(ClientMessageHandler<TMessage> messageHandler) where TMessage : Message
		{
			this._messageHandlers.Add(typeof(TMessage), messageHandler);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000025D1 File Offset: 0x000007D1
		public void HandleMessage(Message message)
		{
			this._messageHandlers[message.GetType()].DynamicInvokeWithLog(new object[] { message });
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000025F4 File Offset: 0x000007F4
		public virtual void OnConnected()
		{
			this._connectionState = Client<T>.ConnectionState.Connected;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000025FD File Offset: 0x000007FD
		public virtual void OnCantConnect()
		{
			if (this._autoReconnect)
			{
				this.Reset();
				return;
			}
			this._connectionState = Client<T>.ConnectionState.Idle;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002615 File Offset: 0x00000815
		public virtual void OnDisconnected()
		{
			if (this._autoReconnect)
			{
				this.Reset();
				return;
			}
			this._connectionState = Client<T>.ConnectionState.Idle;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000262D File Offset: 0x0000082D
		protected void BeginConnect()
		{
			this._connectionState = Client<T>.ConnectionState.ReadyToConnect;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002636 File Offset: 0x00000836
		protected void BeginDisconnect()
		{
			this._clientSession.Disconnect();
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002643 File Offset: 0x00000843
		protected void SetAliveCheckTime(long time)
		{
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002648 File Offset: 0x00000848
		private void Reset()
		{
			this._connectionState = Client<T>.ConnectionState.SleepingToConnectAgain;
			this._timer = new Stopwatch();
			this._timer.Start();
			Debug.Print("Waiting " + 5000L + " milliseconds for another connection attempt", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x0000269D File Offset: 0x0000089D
		public Task<bool> CheckConnection()
		{
			return this._clientSession.CheckConnection();
		}

		// Token: 0x04000002 RID: 2
		private IClientSession _clientSession;

		// Token: 0x04000003 RID: 3
		private Dictionary<Type, Delegate> _messageHandlers;

		// Token: 0x04000004 RID: 4
		private Client<T>.ConnectionState _connectionState;

		// Token: 0x04000005 RID: 5
		private Stopwatch _timer;

		// Token: 0x04000006 RID: 6
		private const long ReconnectTime = 5000L;

		// Token: 0x04000007 RID: 7
		private bool _autoReconnect;

		// Token: 0x02000049 RID: 73
		private enum ConnectionState
		{
			// Token: 0x040000A1 RID: 161
			Idle,
			// Token: 0x040000A2 RID: 162
			ReadyToConnect,
			// Token: 0x040000A3 RID: 163
			Connecting,
			// Token: 0x040000A4 RID: 164
			Connected,
			// Token: 0x040000A5 RID: 165
			SleepingToConnectAgain
		}
	}
}
