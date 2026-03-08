using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TaleWorlds.Library.Http
{
	// Token: 0x020000AE RID: 174
	public class DotNetHttpDriver : IHttpDriver
	{
		// Token: 0x0600068A RID: 1674 RVA: 0x00016B5C File Offset: 0x00014D5C
		public DotNetHttpDriver()
		{
			ServicePointManager.DefaultConnectionLimit = 5;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			this._httpClient = new HttpClient();
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x00016B7F File Offset: 0x00014D7F
		IHttpRequestTask IHttpDriver.CreateHttpPostRequestTask(string address, string postData, bool withUserToken)
		{
			return new HttpPostRequest(this._httpClient, address, postData);
		}

		// Token: 0x0600068C RID: 1676 RVA: 0x00016B8E File Offset: 0x00014D8E
		IHttpRequestTask IHttpDriver.CreateHttpGetRequestTask(string address, bool withUserToken)
		{
			return new HttpGetRequest(this._httpClient, address);
		}

		// Token: 0x0600068D RID: 1677 RVA: 0x00016B9C File Offset: 0x00014D9C
		async Task<string> IHttpDriver.HttpGetString(string url, bool withUserToken)
		{
			HttpResponseMessage httpResponseMessage = await this._httpClient.GetAsync(url);
			HttpResponseMessage responseMessage = httpResponseMessage;
			string text = await responseMessage.Content.ReadAsStringAsync();
			if (!responseMessage.IsSuccessStatusCode)
			{
				throw new Exception(text);
			}
			return text;
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x00016BEC File Offset: 0x00014DEC
		async Task<string> IHttpDriver.HttpPostString(string url, string postData, string mediaType, bool withUserToken)
		{
			HttpResponseMessage httpResponseMessage = await this._httpClient.PostAsync(url, new StringContent(postData, Encoding.Unicode, mediaType));
			string result;
			using (HttpResponseMessage response = httpResponseMessage)
			{
				using (HttpContent content = response.Content)
				{
					result = await content.ReadAsStringAsync();
				}
			}
			return result;
		}

		// Token: 0x0600068F RID: 1679 RVA: 0x00016C4C File Offset: 0x00014E4C
		async Task<byte[]> IHttpDriver.HttpDownloadData(string url)
		{
			return await this._httpClient.GetByteArrayAsync(url);
		}

		// Token: 0x040001F6 RID: 502
		private HttpClient _httpClient;
	}
}
