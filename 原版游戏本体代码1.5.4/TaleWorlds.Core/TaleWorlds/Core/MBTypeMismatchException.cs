namespace TaleWorlds.Core;

public class MBTypeMismatchException : MBException
{
	public MBTypeMismatchException(string exceptionString)
		: base("Type Does not match with the expected one. " + exceptionString)
	{
	}
}
