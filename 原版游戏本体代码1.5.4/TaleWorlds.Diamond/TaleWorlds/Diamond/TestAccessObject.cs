using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
public class TestAccessObject : AccessObject
{
	[JsonProperty]
	public string UserName { get; private set; }

	[JsonProperty]
	public string Password { get; private set; }

	public TestAccessObject()
	{
	}

	public TestAccessObject(string userName, string password)
	{
		base.Type = "Test";
		UserName = userName;
		Password = password;
	}
}
