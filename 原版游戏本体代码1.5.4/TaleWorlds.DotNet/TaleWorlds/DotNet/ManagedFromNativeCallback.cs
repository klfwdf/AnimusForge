using System;

namespace TaleWorlds.DotNet;

public class ManagedFromNativeCallback : Attribute
{
	public bool IsMultiThreadCallable;

	public string[] Conditionals { get; private set; }

	public ManagedFromNativeCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
	{
		Conditionals = conditionals;
		IsMultiThreadCallable = isMultiThreadCallable;
	}
}
