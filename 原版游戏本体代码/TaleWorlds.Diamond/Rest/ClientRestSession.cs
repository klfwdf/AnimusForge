using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x0200003B RID: 59
	public class ClientRestSession : IClientSession
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000138 RID: 312 RVA: 0x0000400E File Offset: 0x0000220E
		// (set) Token: 0x06000139 RID: 313 RVA: 0x00004016 File Offset: 0x00002216
		public bool IsConnected { get; private set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600013A RID: 314 RVA: 0x0000401F File Offset: 0x0000221F
		// (set) Token: 0x0600013B RID: 315 RVA: 0x00004027 File Offset: 0x00002227
		public IClient Client { get; private set; }

		// Token: 0x0600013C RID: 316 RVA: 0x00004030 File Offset: 0x00002230
		public ClientRestSession(IClient client, string address, IHttpDriver platformNetworkClient)
		{
			this.Client = client;
			this._sessionInitialized = false;
			this._platformNetworkClient = platformNetworkClient;
			this.ResetTimer();
			this._address = address;
			this._messageTaskQueue = new Queue<ClientRestSessionTask>();
			this._currentConnectionResultType = ClientRestSession.ConnectionResultType.None;
			this._restDataJsonConverter = new RestDataJsonConverter();
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00004082 File Offset: 0x00002282
		private void ResetTimer()
		{
			this._timer = new Stopwatch();
			this._timer.Start();
		}

		// Token: 0x0600013E RID: 318 RVA: 0x0000409C File Offset: 0x0000229C
		private void AssignRequestJob(ClientRestSessionTask requestMessageTask)
		{
			RestRequestMessage restRequestMessage = requestMessageTask.RestRequestMessage;
			bool flag = false;
			if (restRequestMessage is ConnectMessage)
			{
				if (!this.IsConnected)
				{
					flag = true;
				}
			}
			else if (restRequestMessage is DisconnectMessage)
			{
				if (this.IsConnected)
				{
					flag = true;
				}
			}
			else if (this.IsConnected)
			{
				flag = true;
			}
			if (flag)
			{
				this._currentMessageTask = requestMessageTask;
				this._currentMessageTask.SetRequestData(this._userCertificate, this._address, this._platformNetworkClient);
				restRequestMessage.SerializeAsJson();
				this._lastRequestOperationTime = this._timer.ElapsedMilliseconds;
				return;
			}
			Debug.Print("Setting new request message as failed because can't assign it", 0, Debug.DebugColor.White, 17592186044416UL);
			requestMessageTask.SetFinishedAsFailed();
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00004140 File Offset: 0x00002340
		private void RemoveRequestJob()
		{
			this._currentMessageTask = null;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x0000414C File Offset: 0x0000234C
		void IClientSession.Tick()
		{
			this.TryAssignJob();
			if (this._currentMessageTask != null)
			{
				this._currentMessageTask.Tick();
				if (this._currentMessageTask.IsCompletelyFinished)
				{
					if (this._currentMessageTask.Request.Successful)
					{
						if (this._currentMessageTask.RestRequestMessage is ConnectMessage)
						{
							this._currentConnectionResultType = ClientRestSession.ConnectionResultType.Connected;
							this._currentMessageTask.SetFinishedAsSuccessful(null);
						}
						else if (this._currentMessageTask.RestRequestMessage is DisconnectMessage)
						{
							this._currentConnectionResultType = ClientRestSession.ConnectionResultType.Disconnected;
							this._currentMessageTask.SetFinishedAsSuccessful(null);
						}
						else
						{
							string responseData = this._currentMessageTask.Request.ResponseData;
							if (!string.IsNullOrEmpty(responseData))
							{
								RestResponse restResponse = JsonConvert.DeserializeObject<RestResponse>(responseData, new JsonConverter[] { this._restDataJsonConverter });
								if (restResponse.Successful)
								{
									this._userCertificate = restResponse.UserCertificate;
									this._currentMessageTask.SetFinishedAsSuccessful(restResponse);
									while (restResponse.RemainingMessageCount > 0)
									{
										RestResponseMessage restResponseMessage = restResponse.TryDequeueMessage();
										this.HandleMessage(restResponseMessage.GetMessage());
									}
								}
								else
								{
									this._currentConnectionResultType = ClientRestSession.ConnectionResultType.Disconnected;
									Debug.Print("Setting current request message as failed because server returned unsuccessful response(" + restResponse.SuccessfulReason + ")", 0, Debug.DebugColor.White, 17592186044416UL);
									this._currentMessageTask.SetFinishedAsFailed(restResponse);
								}
							}
							else
							{
								this._currentConnectionResultType = ClientRestSession.ConnectionResultType.Disconnected;
								Debug.Print("Setting current request message as failed because server returned empty response", 0, Debug.DebugColor.White, 17592186044416UL);
								this._currentMessageTask.SetFinishedAsFailed();
							}
						}
					}
					else
					{
						if (this._currentMessageTask.RestRequestMessage is ConnectMessage)
						{
							this._currentConnectionResultType = ClientRestSession.ConnectionResultType.CantConnect;
						}
						else
						{
							this._currentConnectionResultType = ClientRestSession.ConnectionResultType.Disconnected;
						}
						Debug.Print("Setting current request message as failed because server request is failed", 0, Debug.DebugColor.White, 17592186044416UL);
						this._currentMessageTask.SetFinishedAsFailed();
					}
					this.RemoveRequestJob();
				}
				if (this._currentConnectionResultType != ClientRestSession.ConnectionResultType.None)
				{
					switch (this._currentConnectionResultType)
					{
					case ClientRestSession.ConnectionResultType.Connected:
						this.IsConnected = true;
						this.OnConnected();
						break;
					case ClientRestSession.ConnectionResultType.Disconnected:
						this.IsConnected = false;
						this.ClearMessageTaskQueueDueToDisconnect();
						this._sessionCredentials = null;
						this._sessionInitialized = false;
						this._userCertificate = null;
						this.ResetTimer();
						this.OnDisconnected();
						break;
					case ClientRestSession.ConnectionResultType.CantConnect:
						this._userCertificate = null;
						this.ResetTimer();
						this.OnCantConnect();
						break;
					}
					this._currentConnectionResultType = ClientRestSession.ConnectionResultType.None;
				}
			}
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00004390 File Offset: 0x00002590
		private void TryAssignJob()
		{
			if (this._currentMessageTask == null)
			{
				if (this._messageTaskQueue.Count > 0)
				{
					ClientRestSessionTask requestMessageTask = this._messageTaskQueue.Dequeue();
					this.AssignRequestJob(requestMessageTask);
					return;
				}
				if (this.IsConnected && this._sessionInitialized && this._timer.ElapsedMilliseconds - this._lastRequestOperationTime > (this.Client.IsInCriticalState ? ClientRestSession.CriticalStateCheckTime : this.Client.AliveCheckTimeInMiliSeconds) && this._userCertificate != null)
				{
					this.AssignRequestJob(new ClientRestSessionTask(new AliveMessage(this._sessionCredentials)));
				}
			}
		}

		// Token: 0x06000142 RID: 322 RVA: 0x0000442C File Offset: 0x0000262C
		private void ClearMessageTaskQueueDueToDisconnect()
		{
			foreach (ClientRestSessionTask clientRestSessionTask in this._messageTaskQueue)
			{
				clientRestSessionTask.SetFinishedAsFailed();
			}
			this._messageTaskQueue.Clear();
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00004488 File Offset: 0x00002688
		public void Connect()
		{
			this.ResetTimer();
			this.SendMessage(new ConnectMessage());
		}

		// Token: 0x06000144 RID: 324 RVA: 0x0000449B File Offset: 0x0000269B
		public void Disconnect()
		{
			this.SendMessage(new DisconnectMessage());
			this.ResetTimer();
		}

		// Token: 0x06000145 RID: 325 RVA: 0x000044AE File Offset: 0x000026AE
		private void SendMessage(RestRequestMessage message)
		{
			this._messageTaskQueue.Enqueue(new ClientRestSessionTask(message));
		}

		// Token: 0x06000146 RID: 326 RVA: 0x000044C4 File Offset: 0x000026C4
		async Task<LoginResult> IClientSession.Login(LoginMessage message)
		{
			ClientRestSessionTask clientRestSessionTask = new ClientRestSessionTask(new RestObjectRequestMessage(null, message, MessageType.Login));
			this._messageTaskQueue.Enqueue(clientRestSessionTask);
			await clientRestSessionTask.WaitUntilFinished();
			LoginResult result;
			if (!clientRestSessionTask.Successful && !clientRestSessionTask.Request.Successful)
			{
				result = new LoginResult(LoginErrorCode.LoginRequestFailed.ToString(), null);
			}
			else
			{
				RestFunctionResult functionResult = clientRestSessionTask.RestResponse.FunctionResult;
				LoginResult loginResult = null;
				if (functionResult != null)
				{
					loginResult = (LoginResult)functionResult.GetFunctionResult();
					if (clientRestSessionTask.Successful)
					{
						this._sessionCredentials = new SessionCredentials(loginResult.PeerId, loginResult.SessionKey);
						this._sessionInitialized = true;
					}
				}
				result = loginResult;
			}
			return result;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00004511 File Offset: 0x00002711
		void IClientSession.SendMessage(Message message)
		{
			this.SendMessage(new RestObjectRequestMessage(this._sessionCredentials, message, MessageType.Message));
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00004528 File Offset: 0x00002728
		async Task<TResult> IClientSession.CallFunction<TResult>(Message message)
		{
			ClientRestSessionTask clientRestSessionTask = new ClientRestSessionTask(new RestObjectRequestMessage(this._sessionCredentials, message, MessageType.Function));
			this._messageTaskQueue.Enqueue(clientRestSessionTask);
			await clientRestSessionTask.WaitUntilFinished();
			if (clientRestSessionTask.Successful)
			{
				return (TResult)((object)clientRestSessionTask.RestResponse.FunctionResult.GetFunctionResult());
			}
			throw new Exception("Could not call function with " + message.GetType().Name);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00004575 File Offset: 0x00002775
		private void HandleMessage(Message message)
		{
			this.Client.HandleMessage(message);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00004583 File Offset: 0x00002783
		private void OnConnected()
		{
			this.Client.OnConnected();
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00004590 File Offset: 0x00002790
		private void OnDisconnected()
		{
			this.Client.OnDisconnected();
		}

		// Token: 0x0600014C RID: 332 RVA: 0x0000459D File Offset: 0x0000279D
		private void OnCantConnect()
		{
			this.Client.OnCantConnect();
		}

		// Token: 0x0600014D RID: 333 RVA: 0x000045AC File Offset: 0x000027AC
		async Task<bool> IClientSession.CheckConnection()
		{
			bool result;
			try
			{
				string url = this._address + "/Data/Ping";
				await this._platformNetworkClient.HttpGetString(url, false);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0400005E RID: 94
		private static readonly long CriticalStateCheckTime = 1000L;

		// Token: 0x0400005F RID: 95
		private readonly Queue<ClientRestSessionTask> _messageTaskQueue;

		// Token: 0x04000060 RID: 96
		private readonly string _address;

		// Token: 0x04000061 RID: 97
		private byte[] _userCertificate;

		// Token: 0x04000062 RID: 98
		private ClientRestSessionTask _currentMessageTask;

		// Token: 0x04000064 RID: 100
		private ClientRestSession.ConnectionResultType _currentConnectionResultType;

		// Token: 0x04000065 RID: 101
		private Stopwatch _timer;

		// Token: 0x04000066 RID: 102
		private long _lastRequestOperationTime;

		// Token: 0x04000067 RID: 103
		private bool _sessionInitialized;

		// Token: 0x04000068 RID: 104
		private SessionCredentials _sessionCredentials;

		// Token: 0x0400006A RID: 106
		private RestDataJsonConverter _restDataJsonConverter;

		// Token: 0x0400006B RID: 107
		private IHttpDriver _platformNetworkClient;

		// Token: 0x02000056 RID: 86
		private enum ConnectionResultType
		{
			// Token: 0x040000D4 RID: 212
			None,
			// Token: 0x040000D5 RID: 213
			Connected,
			// Token: 0x040000D6 RID: 214
			Disconnected,
			// Token: 0x040000D7 RID: 215
			CantConnect
		}
	}
}
