using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TaleWorlds.Library.Http;

public class DotNetHttpDriver : IHttpDriver
{
	private HttpClient _httpClient;

	public DotNetHttpDriver()
	{
		ServicePointManager.DefaultConnectionLimit = 5;
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		_httpClient = new HttpClient();
	}

	IHttpRequestTask IHttpDriver.CreateHttpPostRequestTask(string address, string postData, bool withUserToken)
	{
		return new HttpPostRequest(_httpClient, address, postData);
	}

	IHttpRequestTask IHttpDriver.CreateHttpGetRequestTask(string address, bool withUserToken)
	{
		return new HttpGetRequest(_httpClient, address);
	}

	async Task<string> IHttpDriver.HttpGetString(string url, bool withUserToken)
	{
		HttpResponseMessage responseMessage = await _httpClient.GetAsync(url);
		string text = await responseMessage.Content.ReadAsStringAsync();
		if (!responseMessage.IsSuccessStatusCode)
		{
			throw new Exception(text);
		}
		return text;
	}

	async Task<string> IHttpDriver.HttpPostString(string url, string postData, string mediaType, bool withUserToken)
	{
		using HttpResponseMessage response = await _httpClient.PostAsync(url, new StringContent(postData, Encoding.Unicode, mediaType));
		using HttpContent content = response.Content;
		return await content.ReadAsStringAsync();
	}

	async Task<byte[]> IHttpDriver.HttpDownloadData(string url)
	{
		return await _httpClient.GetByteArrayAsync(url);
	}
}
