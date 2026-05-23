using System;
using System.Net;
using System.Net.Http;

namespace TaleWorlds.Library.Http
{
	// Token: 0x020000B0 RID: 176
	public class HttpGetRequest : IHttpRequestTask
	{
		// Token: 0x170000BD RID: 189
		// (get) Token: 0x06000695 RID: 1685 RVA: 0x00016D3E File Offset: 0x00014F3E
		// (set) Token: 0x06000696 RID: 1686 RVA: 0x00016D46 File Offset: 0x00014F46
		public HttpRequestTaskState State { get; private set; }

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x06000697 RID: 1687 RVA: 0x00016D4F File Offset: 0x00014F4F
		// (set) Token: 0x06000698 RID: 1688 RVA: 0x00016D57 File Offset: 0x00014F57
		public bool Successful { get; private set; }

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x06000699 RID: 1689 RVA: 0x00016D60 File Offset: 0x00014F60
		// (set) Token: 0x0600069A RID: 1690 RVA: 0x00016D68 File Offset: 0x00014F68
		public string ResponseData { get; private set; }

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x0600069B RID: 1691 RVA: 0x00016D71 File Offset: 0x00014F71
		// (set) Token: 0x0600069C RID: 1692 RVA: 0x00016D79 File Offset: 0x00014F79
		public HttpStatusCode ResponseStatusCode { get; private set; }

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x0600069D RID: 1693 RVA: 0x00016D82 File Offset: 0x00014F82
		// (set) Token: 0x0600069E RID: 1694 RVA: 0x00016D8A File Offset: 0x00014F8A
		public Exception Exception { get; private set; }

		// Token: 0x0600069F RID: 1695 RVA: 0x00016D93 File Offset: 0x00014F93
		public HttpGetRequest(HttpClient httpClient, string address)
			: this(httpClient, address, new Version("1.1"))
		{
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x00016DA7 File Offset: 0x00014FA7
		public HttpGetRequest(HttpClient httpClient, string address, Version version)
		{
			this._versionToUse = version;
			this._address = address;
			this._httpClient = httpClient;
			this.State = HttpRequestTaskState.NotStarted;
			this.ResponseData = "";
			this.ResponseStatusCode = HttpStatusCode.OK;
		}

		// Token: 0x060006A1 RID: 1697 RVA: 0x00016DE1 File Offset: 0x00014FE1
		private void SetFinishedAsSuccessful(string responseData, HttpStatusCode statusCode)
		{
			this.Successful = true;
			this.ResponseData = responseData;
			this.ResponseStatusCode = statusCode;
			this.State = HttpRequestTaskState.Finished;
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x00016DFF File Offset: 0x00014FFF
		private void SetFinishedAsUnsuccessful(Exception e)
		{
			this.Successful = false;
			this.Exception = e;
			this.State = HttpRequestTaskState.Finished;
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x00016E16 File Offset: 0x00015016
		public void Start()
		{
			this.DoTask();
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x00016E20 File Offset: 0x00015020
		private async void DoTask()
		{
			this.State = HttpRequestTaskState.Working;
			try
			{
				using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, this._address))
				{
					requestMessage.Version = this._versionToUse;
					requestMessage.Headers.Add("Accept", "application/json");
					requestMessage.Headers.Add("UserAgent", "TaleWorlds Client");
					HttpResponseMessage httpResponseMessage = await this._httpClient.SendAsync(requestMessage);
					using (HttpResponseMessage response = httpResponseMessage)
					{
						bool isSuccessStatusCode = response.IsSuccessStatusCode;
						response.EnsureSuccessStatusCode();
						Debug.Print(string.Concat(new object[] { "Protocol version used for get request to ", this._address, " is: ", response.Version }), 0, Debug.DebugColor.White, 17592186044416UL);
						using (HttpContent content = response.Content)
						{
							this.SetFinishedAsSuccessful(await content.ReadAsStringAsync(), response.StatusCode);
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

		// Token: 0x040001F9 RID: 505
		private const int BufferSize = 1024;

		// Token: 0x040001FA RID: 506
		private HttpClient _httpClient;

		// Token: 0x040001FB RID: 507
		private readonly string _address;

		// Token: 0x04000201 RID: 513
		private Version _versionToUse;
	}
}
