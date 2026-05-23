using System;
using System.Threading.Tasks;

namespace TaleWorlds.Library.Http
{
	// Token: 0x020000B3 RID: 179
	public interface IHttpDriver
	{
		// Token: 0x060006B3 RID: 1715
		Task<string> HttpGetString(string url, bool withUserToken);

		// Token: 0x060006B4 RID: 1716
		Task<string> HttpPostString(string url, string postData, string mediaType, bool withUserToken);

		// Token: 0x060006B5 RID: 1717
		Task<byte[]> HttpDownloadData(string url);

		// Token: 0x060006B6 RID: 1718
		IHttpRequestTask CreateHttpPostRequestTask(string address, string postData, bool withUserToken);

		// Token: 0x060006B7 RID: 1719
		IHttpRequestTask CreateHttpGetRequestTask(string address, bool withUserToken);
	}
}
