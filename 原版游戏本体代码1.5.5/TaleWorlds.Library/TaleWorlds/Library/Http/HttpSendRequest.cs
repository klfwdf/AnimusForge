using System.Collections.Generic;
using System.Text;

namespace TaleWorlds.Library.Http;

public class HttpSendRequest
{
	public string Method { get; set; }

	public string Url { get; set; }

	public string Body { get; set; }

	public string ContentType { get; set; }

	public Dictionary<string, string> Headers { get; set; }

	public int TimeoutMilliseconds { get; set; }

	public HttpSendRequest()
	{
		Method = "POST";
		ContentType = "application/json";
		Headers = new Dictionary<string, string>();
		TimeoutMilliseconds = 15000;
	}

	public byte[] GetBodyBytes()
	{
		if (Body != null)
		{
			return Encoding.UTF8.GetBytes(Body);
		}
		return null;
	}
}
