using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

public class MBCallback : ManagedFromNativeCallback
{
	public MBCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
		: base(conditionals, isMultiThreadCallable)
	{
	}
}
