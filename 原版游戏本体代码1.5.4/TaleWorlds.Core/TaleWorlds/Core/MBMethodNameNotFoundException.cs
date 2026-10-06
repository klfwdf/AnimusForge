namespace TaleWorlds.Core;

public class MBMethodNameNotFoundException : MBException
{
	public MBMethodNameNotFoundException(string methodName)
		: base("Unable to find method " + methodName)
	{
	}
}
