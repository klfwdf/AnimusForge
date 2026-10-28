using System;

namespace TaleWorlds.Network;

public class PostBoxId : Attribute
{
	public string Id { get; private set; }

	public PostBoxId(string id)
	{
		Id = id;
	}
}
