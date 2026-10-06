namespace TaleWorlds.DotNet;

public class LibraryCallback : ManagedFromNativeCallback
{
	public LibraryCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
		: base(conditionals, isMultiThreadCallable)
	{
	}
}
