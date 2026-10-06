using System;

namespace TaleWorlds.Network;

public class Authorize : Attribute
{
	public string Users { get; set; }

	public string Roles { get; set; }
}
