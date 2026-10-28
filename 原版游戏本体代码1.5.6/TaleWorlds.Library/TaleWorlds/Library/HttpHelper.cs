using System.Threading.Tasks;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Library;

public static class HttpHelper
{
	public static Task<string> DownloadStringTaskAsync(string url)
	{
		return HttpDriverManager.GetDefaultHttpDriver().HttpGetString(url, withUserToken: false);
	}

	public static Task<byte[]> DownloadDataTaskAsync(string url)
	{
		return HttpDriverManager.GetDefaultHttpDriver().HttpDownloadData(url);
	}

	public static Task<string> PostStringAsync(string url, string postData, string mediaType = "application/json")
	{
		return HttpDriverManager.GetDefaultHttpDriver().HttpPostString(url, postData, mediaType, withUserToken: false);
	}
}
