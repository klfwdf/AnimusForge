using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.Rest;

internal class ClientRestSessionTask
{
	private static readonly int RequestRetryTimeout = 1000;

	private readonly Type[] RetryableExceptions = new Type[5]
	{
		typeof(HttpRequestException),
		typeof(TaskCanceledException),
		typeof(IOException),
		typeof(SocketException),
		typeof(InvalidOperationException)
	};

	public bool _willTryAgain;

	private string _requestAddress;

	private string _postData;

	private string _messageName;

	private int _maxIterationCount = 5;

	private int _currentIterationCount;

	private Stopwatch _sw;

	private TaskCompletionSource<bool> _taskCompletionSource;

	private IHttpDriver _networkClient;

	private bool _resultExamined;

	public RestRequestMessage RestRequestMessage { get; private set; }

	public bool Finished { get; private set; }

	public bool Successful { get; private set; }

	public IHttpRequestTask Request { get; private set; }

	public RestResponse RestResponse { get; private set; }

	public bool IsCompletelyFinished
	{
		get
		{
			if (_willTryAgain)
			{
				return false;
			}
			if (!_resultExamined)
			{
				return false;
			}
			return Request.State == HttpRequestTaskState.Finished;
		}
	}

	public ClientRestSessionTask(RestRequestMessage restRequestMessage)
	{
		RestRequestMessage = restRequestMessage;
		_taskCompletionSource = new TaskCompletionSource<bool>();
		_sw = new Stopwatch();
		_messageName = RestRequestMessage.TypeName;
	}

	public void SetRequestData(byte[] userCertificate, string address, IHttpDriver networkClient)
	{
		RestRequestMessage.UserCertificate = userCertificate;
		_requestAddress = address;
		_postData = RestRequestMessage.SerializeAsJson();
		_networkClient = networkClient;
		CreateAndSetRequest();
	}

	private void DetermineNextTry()
	{
		if (_sw.ElapsedMilliseconds >= RequestRetryTimeout)
		{
			_willTryAgain = false;
			TaleWorlds.Library.Debug.Print("Retrying http post request, iteration count: " + _currentIterationCount);
			CreateAndSetRequest();
		}
	}

	private static string GetCode(WebException webException)
	{
		if (webException.Response != null && webException.Response is HttpWebResponse)
		{
			return ((HttpWebResponse)webException.Response).StatusCode.ToString();
		}
		return "NoCode";
	}

	private void ExamineResult()
	{
		if (!Request.Successful)
		{
			bool flag = false;
			if (Request.Exception != null && RetryableExceptions.Any((Type e) => e == Request.Exception.GetType()))
			{
				TaleWorlds.Library.Debug.Print(string.Concat("Http Post Request with message(", RestRequestMessage, ")  failed. Retrying: (", Request.Exception?.GetType(), ") ", Request.Exception));
				flag = true;
			}
			else
			{
				TaleWorlds.Library.Debug.Print(string.Concat("Http Post Request with message(", RestRequestMessage, ")  failed. Exception: (", Request.Exception?.GetType(), ") ", Request.Exception));
			}
			if (Request != null && Request.Exception != null)
			{
				PrintExceptions(Request.Exception);
			}
			if (flag)
			{
				if (_currentIterationCount < _maxIterationCount)
				{
					_sw.Restart();
					_willTryAgain = true;
					_currentIterationCount++;
					TaleWorlds.Library.Debug.Print(string.Concat("Http post request(", RestRequestMessage, ")  will try again, iteration count: ", _currentIterationCount));
				}
				else
				{
					_willTryAgain = false;
					TaleWorlds.Library.Debug.Print(string.Concat("Passed max retry count for http post request(", RestRequestMessage, ") "));
				}
			}
			else
			{
				TaleWorlds.Library.Debug.Print(string.Concat("Http post request(", RestRequestMessage, ")  will not try again due to exception type!"));
				_willTryAgain = false;
			}
		}
		else if (_currentIterationCount > 0)
		{
			TaleWorlds.Library.Debug.Print(string.Concat("Http post request(", RestRequestMessage, ") is successful with iteration count: ", _currentIterationCount));
		}
		_resultExamined = true;
	}

	public void Tick()
	{
		switch (Request.State)
		{
		case HttpRequestTaskState.NotStarted:
			Request.Start();
			break;
		case HttpRequestTaskState.Finished:
			if (!_resultExamined)
			{
				ExamineResult();
			}
			else
			{
				DetermineNextTry();
			}
			break;
		case HttpRequestTaskState.Working:
			break;
		}
	}

	public async Task WaitUntilFinished()
	{
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::WaitUntilFinished::" + _messageName);
		await _taskCompletionSource.Task;
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::WaitUntilFinished::" + _messageName + " done");
	}

	public void SetFinishedAsSuccessful(RestResponse restResponse)
	{
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsSuccessful::" + _messageName);
		RestResponse = restResponse;
		Successful = true;
		Finished = true;
		_taskCompletionSource.SetResult(result: true);
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsSuccessful::" + _messageName + " done");
	}

	public void SetFinishedAsFailed()
	{
		SetFinishedAsFailed(null);
	}

	public void SetFinishedAsFailed(RestResponse restResponse)
	{
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsFailed::" + _messageName);
		RestResponse = restResponse;
		Successful = false;
		Finished = true;
		_taskCompletionSource.SetResult(result: true);
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsFailed:: " + _messageName + " done");
	}

	private void CreateAndSetRequest()
	{
		bool flag = RestRequestMessage is RestObjectRequestMessage restObjectRequestMessage && restObjectRequestMessage.MessageType == MessageType.Login;
		string address = _requestAddress + (flag ? "/Data/Login" : "/Data/ProcessMessage");
		Request = _networkClient.CreateHttpPostRequestTask(address, _postData, withUserToken: true);
		_resultExamined = false;
	}

	private void PrintExceptions(Exception e)
	{
		if (e != null)
		{
			Exception ex = e;
			int num = 0;
			while (ex != null)
			{
				TaleWorlds.Library.Debug.Print("Exception #" + num + ": " + ex.Message + " ||| StackTrace: " + ex.InnerException);
				ex = ex.InnerException;
				num++;
			}
		}
	}
}
