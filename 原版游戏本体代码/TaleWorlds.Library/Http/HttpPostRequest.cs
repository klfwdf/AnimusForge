using System;
using System.Net.Http;
using System.Text;

namespace TaleWorlds.Library.Http
{
	// Token: 0x020000B1 RID: 177
	public class HttpPostRequest : IHttpRequestTask
	{
		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x060006A5 RID: 1701 RVA: 0x00016E59 File Offset: 0x00015059
		// (set) Token: 0x060006A6 RID: 1702 RVA: 0x00016E61 File Offset: 0x00015061
		public HttpRequestTaskState State { get; private set; }

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x060006A7 RID: 1703 RVA: 0x00016E6A File Offset: 0x0001506A
		// (set) Token: 0x060006A8 RID: 1704 RVA: 0x00016E72 File Offset: 0x00015072
		public bool Successful { get; private set; }

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x060006A9 RID: 1705 RVA: 0x00016E7B File Offset: 0x0001507B
		// (set) Token: 0x060006AA RID: 1706 RVA: 0x00016E83 File Offset: 0x00015083
		public string ResponseData { get; private set; }

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x060006AB RID: 1707 RVA: 0x00016E8C File Offset: 0x0001508C
		// (set) Token: 0x060006AC RID: 1708 RVA: 0x00016E94 File Offset: 0x00015094
		public Exception Exception { get; private set; }

		// Token: 0x060006AD RID: 1709 RVA: 0x00016E9D File Offset: 0x0001509D
		public HttpPostRequest(HttpClient httpClient, string address, string postData)
			: this(httpClient, address, postData, new Version("1.1"))
		{
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x00016EB2 File Offset: 0x000150B2
		public HttpPostRequest(HttpClient httpClient, string address, string postData, Version version)
		{
			this._httpClient = httpClient;
			this._postData = postData;
			this._address = address;
			this.State = HttpRequestTaskState.NotStarted;
			this.ResponseData = "";
			this._versionToUse = version;
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x00016EE9 File Offset: 0x000150E9
		private void SetFinishedAsSuccessful(string responseData)
		{
			this.Successful = true;
			this.ResponseData = responseData;
			this.State = HttpRequestTaskState.Finished;
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x00016F00 File Offset: 0x00015100
		private void SetFinishedAsUnsuccessful(Exception e)
		{
			this.Successful = false;
			this.Exception = e;
			this.State = HttpRequestTaskState.Finished;
		}

		// Token: 0x060006B1 RID: 1713 RVA: 0x00016F17 File Offset: 0x00015117
		public void Start()
		{
			this.DoTask();
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x00016F20 File Offset: 0x00015120
		private async void DoTask()
		{
			this.State = HttpRequestTaskState.Working;
			try
			{
				Debug.Print("Http Post Request to " + this._address, 0, Debug.DebugColor.White, 17592186044416UL);
				using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, this._address))
				{
					requestMessage.Version = this._versionToUse;
					requestMessage.Headers.Add("Accept", "application/json");
					requestMessage.Headers.Add("UserAgent", "TaleWorlds Client");
					requestMessage.Content = new StringContent(this._postData, Encoding.Unicode, "application/json");
					HttpResponseMessage httpResponseMessage = await this._httpClient.SendAsync(requestMessage);
					using (HttpResponseMessage response = httpResponseMessage)
					{
						bool isSuccessStatusCode = response.IsSuccessStatusCode;
						response.EnsureSuccessStatusCode();
						Debug.Print(string.Concat(new object[] { "Protocol version used for post request to ", this._address, " is: ", response.Version }), 0, Debug.DebugColor.White, 17592186044416UL);
						using (HttpContent content = response.Content)
						{
							this.SetFinishedAsSuccessful(await content.ReadAsStringAsync());
						}
						HttpContent content = null;
					}
					HttpResponseMessage response = null;
				}
				HttpRequestMessage requestMessage = null;
			}
			catch (Exception finishedAsUnsuccessful)
			{
				this.SetFinishedAsUnsuccessful(finishedAsUnsuccessful);
			}
		}

		// Token: 0x04000202 RID: 514
		private HttpClient _httpClient;

		// Token: 0x04000203 RID: 515
		private readonly string _address;

		// Token: 0x04000204 RID: 516
		private string _postData;

		// Token: 0x04000209 RID: 521
		private Version _versionToUse;
	}
}
