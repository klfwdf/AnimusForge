using TaleWorlds.DotNet;

namespace TaleWorlds.Engine;

public class EngineCallback : ManagedFromNativeCallback
{
	public EngineCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
		: base(conditionals, isMultiThreadCallable)
	{
	}
}
