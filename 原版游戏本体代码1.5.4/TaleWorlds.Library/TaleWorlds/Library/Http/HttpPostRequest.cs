using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TaleWorlds.Library.Http;

public class HttpPostRequest : IHttpRequestTask
{
	private HttpClient _httpClient;

	private readonly string _address;

	private string _postData;

	private Version _versionToUse;

	private CancellationToken _cancellationToken;

	public HttpRequestTaskState State { get; private set; }

	public bool Successful { get; private set; }

	public string ResponseData { get; private set; }

	public Exception Exception { get; private set; }

	public HttpPostRequest(HttpClient httpClient, string address, string postData, CancellationToken cancellationToken)
		: this(httpClient, address, postData, new Version("1.1"), cancellationToken)
	{
	}

	public HttpPostRequest(HttpClient httpClient, string address, string postData, Version version, CancellationToken cancellationToken)
	{
		_httpClient = httpClient;
		_postData = postData;
		_address = address;
		State = HttpRequestTaskState.NotStarted;
		ResponseData = "";
		_versionToUse = version;
		_cancellationToken = cancellationToken;
	}

	private void SetFinishedAsSuccessful(string responseData)
	{
		Successful = true;
		ResponseData = responseData;
		State = HttpRequestTaskState.Finished;
	}

	private void SetFinishedAsUnsuccessful(Exception e)
	{
		Successful = false;
		Exception = e;
		State = HttpRequestTaskState.Finished;
	}

	public void Start()
	{
		DoTask();
	}

	private static Exception GetRootCause(Exception e)
	{
		while (e.InnerException != null)
		{
			e = e.InnerException;
		}
		return e;
	}

	private async void DoTask()
	{
		State = HttpRequestTaskState.Working;
		try
		{
			Debug.Print("Http Post Request to " + _address);
			using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _address);
			requestMessage.Version = _versionToUse;
			requestMessage.Headers.Add("Accept", "application/json");
			requestMessage.Headers.Add("UserAgent", "TaleWorlds Client");
			requestMessage.Content = new StringContent(_postData, Encoding.Unicode, "application/json");
			using HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, _cancellationToken);
			_ = response.IsSuccessStatusCode;
			response.EnsureSuccessStatusCode();
			Debug.Print("Protocol version used for post request to " + _address + " is: " + response.Version);
			using HttpContent content = response.Content;
			SetFinishedAsSuccessful(await content.ReadAsStringAsync());
		}
		catch (Exception ex)
		{
			bool num = ex is OperationCanceledException || _cancellationToken.IsCancellationRequested;
			Exception rootCause = GetRootCause(ex);
			bool flag = ex is HttpRequestException && (rootCause is SocketException || rootCause is IOException);
			if (num || flag)
			{
				Debug.Print("Http post request to " + _address + " aborted (shutdown or target unavailable): " + ex.Message + " [" + rootCause.GetType().Name + "]");
			}
			SetFinishedAsUnsuccessful(ex);
		}
	}
}
