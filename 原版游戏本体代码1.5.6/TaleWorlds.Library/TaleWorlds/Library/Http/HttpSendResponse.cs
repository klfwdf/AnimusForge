namespace TaleWorlds.Library.Http;

public struct HttpSendResponse
{
	public int StatusCode;

	public string Body;

	public string Error;

	public bool IsSuccess
	{
		get
		{
			if (StatusCode >= 200)
			{
				return StatusCode < 300;
			}
			return false;
		}
	}

	public static HttpSendResponse Failed(string error)
	{
		return new HttpSendResponse
		{
			StatusCode = 0,
			Body = "",
			Error = error
		};
	}
}
