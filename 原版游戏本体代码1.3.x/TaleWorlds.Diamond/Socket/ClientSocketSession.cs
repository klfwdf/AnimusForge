using System;
using System.Threading.Tasks;
using TaleWorlds.Network;

namespace TaleWorlds.Diamond.Socket
{
	// Token: 0x02000032 RID: 50
	public abstract class ClientSocketSession : ClientsideSession, IClientSession
	{
		// Token: 0x0600010E RID: 270 RVA: 0x00003D0D File Offset: 0x00001F0D
		protected ClientSocketSession(IClient client, string address, int port)
		{
			this._client = client;
			this._address = address;
			this._port = port;
			base.AddMessageHandler<SocketMessage>(new MessageContractHandlerDelegate<SocketMessage>(this.HandleSocketMessage));
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00003D3C File Offset: 0x00001F3C
		private void HandleSocketMessage(SocketMessage socketMessage)
		{
			Message message = socketMessage.Message;
			this._client.HandleMessage(message);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00003D5C File Offset: 0x00001F5C
		protected override void OnConnected()
		{
			base.OnConnected();
			this._client.OnConnected();
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00003D6F File Offset: 0x00001F6F
		protected override void OnCantConnect()
		{
			base.OnCantConnect();
			this._client.OnCantConnect();
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00003D82 File Offset: 0x00001F82
		protected override void OnDisconnected()
		{
			base.OnDisconnected();
			this._client.OnDisconnected();
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00003D95 File Offset: 0x00001F95
		void IClientSession.Connect()
		{
			this.Connect(this._address, this._port, true);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00003DAA File Offset: 0x00001FAA
		void IClientSession.Disconnect()
		{
			base.SendDisconnectMessage();
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00003DB2 File Offset: 0x00001FB2
		Task<TReturn> IClientSession.CallFunction<TReturn>(Message message)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00003DB9 File Offset: 0x00001FB9
		void IClientSession.SendMessage(Message message)
		{
			base.SendMessage(new SocketMessage(message));
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00003DC7 File Offset: 0x00001FC7
		Task<LoginResult> IClientSession.Login(LoginMessage message)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00003DCE File Offset: 0x00001FCE
		Task<bool> IClientSession.CheckConnection()
		{
			return Task.FromResult<bool>(true);
		}

		// Token: 0x04000056 RID: 86
		private string _address;

		// Token: 0x04000057 RID: 87
		private int _port;

		// Token: 0x04000058 RID: 88
		private IClient _client;
	}
}
