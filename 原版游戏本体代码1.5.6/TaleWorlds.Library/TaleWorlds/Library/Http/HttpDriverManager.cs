using System.Collections.Concurrent;

namespace TaleWorlds.Library.Http;

public static class HttpDriverManager
{
	private static ConcurrentDictionary<string, IHttpDriver> _httpDrivers;

	private static string _defaultHttpDriver;

	static HttpDriverManager()
	{
		_httpDrivers = new ConcurrentDictionary<string, IHttpDriver>();
	}

	public static void AddHttpDriver(string name, IHttpDriver driver)
	{
		if (_httpDrivers.Count == 0)
		{
			_defaultHttpDriver = name;
		}
		_httpDrivers[name] = driver;
	}

	public static void SetDefault(string name)
	{
		if (GetHttpDriver(name) != null)
		{
			_defaultHttpDriver = name;
		}
	}

	public static IHttpDriver GetHttpDriver(string name)
	{
		_httpDrivers.TryGetValue(name, out var value);
		if (value == null)
		{
			Debug.Print("HTTP driver not found:" + (name ?? "not set"));
		}
		return value;
	}

	public static IHttpDriver GetDefaultHttpDriver()
	{
		if (_defaultHttpDriver == null)
		{
			AddHttpDriver("DotNet", new DotNetHttpDriver());
		}
		return GetHttpDriver(_defaultHttpDriver);
	}
}
