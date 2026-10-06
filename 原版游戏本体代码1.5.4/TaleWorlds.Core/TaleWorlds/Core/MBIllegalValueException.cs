namespace TaleWorlds.Core;

public class MBIllegalValueException : MBException
{
	public MBIllegalValueException(string exceptionString)
		: base(exceptionString)
	{
	}
}
