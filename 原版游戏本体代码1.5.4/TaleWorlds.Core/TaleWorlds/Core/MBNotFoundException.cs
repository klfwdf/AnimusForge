namespace TaleWorlds.Core;

public class MBNotFoundException : MBException
{
	public MBNotFoundException(string exceptionString)
		: base(exceptionString)
	{
	}
}
