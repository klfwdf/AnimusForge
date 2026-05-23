using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.Rest;

public class ClientRestSession : IClientSession
{
	private enum ConnectionResultType
	{
		None,
		Connected,
		Disconnected,
		CantConnect
	}

	private static readonly long CriticalStateCheckTime = 1000L;

	private readonly Queue<ClientRestSessionTask> _messageTaskQueue;

	private readonly string _address;

	private byte[] _userCertificate;

	private ClientRestSessionTask _currentMessageTask;

	private ConnectionResultType _currentConnectionResultType;

	private Stopwatch _timer;

	private long _lastRequestOperationTime;

	private bool _sessionInitialized;

	private SessionCredentials _sessionCredentials;

	private RestDataJsonConverter _restDataJsonConverter;

	private IHttpDriver _platformNetworkClient;

	public bool IsConnected { get; private set; }

	public IClient Client { get; private set; }

	public ClientRestSession(IClient client, string address, IHttpDriver platformNetworkClient)
	{
		Client = client;
		_sessionInitialized = false;
		_platformNetworkClient = platformNetworkClient;
		ResetTimer();
		_address = address;
		_messageTaskQueue = new Queue<ClientRestSessionTask>();
		_currentConnectionResultType = ConnectionResultType.None;
		_restDataJsonConverter = new RestDataJsonConverter();
	}

	private void ResetTimer()
	{
		_timer = new Stopwatch();
		_timer.Start();
	}

	private void AssignRequestJob(ClientRestSessionTask requestMessageTask)
	{
		RestRequestMessage restRequestMessage = requestMessageTask.RestRequestMessage;
		bool flag = false;
		if (restRequestMessage is ConnectMessage)
		{
			if (!IsConnected)
			{
				flag = true;
			}
		}
		else if (restRequestMessage is DisconnectMessage)
		{
			if (IsConnected)
			{
				flag = true;
			}
		}
		else if (IsConnected)
		{
			flag = true;
		}
		if (flag)
		{
			_currentMessageTask = requestMessageTask;
			_currentMessageTask.SetRequestData(_userCertificate, _address, _platformNetworkClient);
			restRequestMessage.SerializeAsJson();
			_lastRequestOperationTime = _timer.ElapsedMilliseconds;
		}
		else
		{
			TaleWorlds.Library.Debug.Print("Setting new request message as failed because can't assign it");
			requestMessageTask.SetFinishedAsFailed();
		}
	}

	private void RemoveRequestJob()
	{
		_currentMessageTask = null;
	}

	void IClientSession.Tick()
	{
		TryAssignJob();
		if (_currentMessageTask == null)
		{
			return;
		}
		_currentMessageTask.Tick();
		if (_currentMessageTask.IsCompletelyFinished)
		{
			if (_currentMessageTask.Request.Successful)
			{
				if (_currentMessageTask.RestRequestMessage is ConnectMessage)
				{
					_currentConnectionResultType = ConnectionResultType.Connected;
					_currentMessageTask.SetFinishedAsSuccessful(null);
				}
				else if (_currentMessageTask.RestRequestMessage is DisconnectMessage)
				{
					_currentConnectionResultType = ConnectionResultType.Disconnected;
					_currentMessageTask.SetFinishedAsSuccessful(null);
				}
				else
				{
					string responseData = _currentMessageTask.Request.ResponseData;
					if (!string.IsNullOrEmpty(responseData))
					{
						RestResponse restResponse = JsonConvert.DeserializeObject<RestResponse>(responseData, new JsonConverter[1] { _restDataJsonConverter });
						if (restResponse.Successful)
						{
							_userCertificate = restResponse.UserCertificate;
							_currentMessageTask.SetFinishedAsSuccessful(restResponse);
							while (restResponse.RemainingMessageCount > 0)
							{
								RestResponseMessage restResponseMessage = restResponse.TryDequeueMessage();
								HandleMessage(restResponseMessage.GetMessage());
							}
						}
						else
						{
							_currentConnectionResultType = ConnectionResultType.Disconnected;
							TaleWorlds.Library.Debug.Print("Setting current request message as failed because server returned unsuccessful response(" + restResponse.SuccessfulReason + ")");
							_currentMessageTask.SetFinishedAsFailed(restResponse);
						}
					}
					else
					{
						_currentConnectionResultType = ConnectionResultType.Disconnected;
						TaleWorlds.Library.Debug.Print("Setting current request message as failed because server returned empty response");
						_currentMessageTask.SetFinishedAsFailed();
					}
				}
			}
			else
			{
				if (_currentMessageTask.RestRequestMessage is ConnectMessage)
				{
					_currentConnectionResultType = ConnectionResultType.CantConnect;
				}
				else
				{
					_currentConnectionResultType = ConnectionResultType.Disconnected;
				}
				TaleWorlds.Library.Debug.Print("Setting current request message as failed because server request is failed");
				_currentMessageTask.SetFinishedAsFailed();
			}
			RemoveRequestJob();
		}
		if (_currentConnectionResultType != ConnectionResultType.None)
		{
			switch (_currentConnectionResultType)
			{
			case ConnectionResultType.Connected:
				IsConnected = true;
				OnConnected();
				break;
			case ConnectionResultType.Disconnected:
				IsConnected = false;
				ClearMessageTaskQueueDueToDisconnect();
				_sessionCredentials = null;
				_sessionInitialized = false;
				_userCertificate = null;
				ResetTimer();
				OnDisconnected();
				break;
			case ConnectionResultType.CantConnect:
				_userCertificate = null;
				ResetTimer();
				OnCantConnect();
				break;
			}
			_currentConnectionResultType = ConnectionResultType.None;
		}
	}

	private void TryAssignJob()
	{
		if (_currentMessageTask == null)
		{
			if (_messageTaskQueue.Count > 0)
			{
				ClientRestSessionTask requestMessageTask = _messageTaskQueue.Dequeue();
				AssignRequestJob(requestMessageTask);
			}
			else if (IsConnected && _sessionInitialized && _timer.ElapsedMilliseconds - _lastRequestOperationTime > (Client.IsInCriticalState ? CriticalStateCheckTime : Client.AliveCheckTimeInMiliSeconds) && _userCertificate != null)
			{
				AssignRequestJob(new ClientRestSessionTask(new AliveMessage(_sessionCredentials)));
			}
		}
	}

	private void ClearMessageTaskQueueDueToDisconnect()
	{
		foreach (ClientRestSessionTask item in _messageTaskQueue)
		{
			item.SetFinishedAsFailed();
		}
		_messageTaskQueue.Clear();
	}

	public void Connect()
	{
		ResetTimer();
		SendMessage(new ConnectMessage());
	}

	public void Disconnect()
	{
		SendMessage(new DisconnectMessage());
		ResetTimer();
	}

	private void SendMessage(RestRequestMessage message)
	{
		_messageTaskQueue.Enqueue(new ClientRestSessionTask(message));
	}

	async Task<LoginResult> IClientSession.Login(LoginMessage message)
	{
		ClientRestSessionTask clientRestSessionTask = new ClientRestSessionTask(new RestObjectRequestMessage(null, message, MessageType.Login));
		_messageTaskQueue.Enqueue(clientRestSessionTask);
		await clientRestSessionTask.WaitUntilFinished();
		if (!clientRestSessionTask.Successful && !clientRestSessionTask.Request.Successful)
		{
			return new LoginResult(LoginErrorCode.LoginRequestFailed.ToString());
		}
		RestFunctionResult functionResult = clientRestSessionTask.RestResponse.FunctionResult;
		LoginResult loginResult = null;
		if (functionResult != null)
		{
			loginResult = (LoginResult)functionResult.GetFunctionResult();
			if (clientRestSessionTask.Successful)
			{
				_sessionCredentials = new SessionCredentials(loginResult.PeerId, loginResult.SessionKey);
				_sessionInitialized = true;
			}
		}
		return loginResult;
	}

	void IClientSession.SendMessage(Message message)
	{
		SendMessage(new RestObjectRequestMessage(_sessionCredentials, message, MessageType.Message));
	}

	async Task<TResult> IClientSession.CallFunction<TResult>(Message message)
	{
		ClientRestSessionTask clientRestSessionTask = new ClientRestSessionTask(new RestObjectRequestMessage(_sessionCredentials, message, MessageType.Function));
		_messageTaskQueue.Enqueue(clientRestSessionTask);
		await clientRestSessionTask.WaitUntilFinished();
		if (clientRestSessionTask.Successful)
		{
			return (TResult)clientRestSessionTask.RestResponse.FunctionResult.GetFunctionResult();
		}
		throw new Exception("Could not call function with " + message.GetType().Name);
	}

	private void HandleMessage(Message message)
	{
		Client.HandleMessage(message);
	}

	private void OnConnected()
	{
		Client.OnConnected();
	}

	private void OnDisconnected()
	{
		Client.OnDisconnected();
	}

	private void OnCantConnect()
	{
		Client.OnCantConnect();
	}

	async Task<bool> IClientSession.CheckConnection()
	{
		try
		{
			string url = _address + "/Data/Ping";
			await _platformNetworkClient.HttpGetString(url, withUserToken: false);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
