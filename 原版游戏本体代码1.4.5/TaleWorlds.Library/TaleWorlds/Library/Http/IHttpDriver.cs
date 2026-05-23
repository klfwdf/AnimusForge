using System.Threading.Tasks;

namespace TaleWorlds.Library.Http;

public interface IHttpDriver
{
	Task<string> HttpGetString(string url, bool withUserToken);

	Task<string> HttpPostString(string url, string postData, string mediaType, bool withUserToken);

	Task<byte[]> HttpDownloadData(string url);

	IHttpRequestTask CreateHttpPostRequestTask(string address, string postData, bool withUserToken);

	IHttpRequestTask CreateHttpGetRequestTask(string address, bool withUserToken);
}
