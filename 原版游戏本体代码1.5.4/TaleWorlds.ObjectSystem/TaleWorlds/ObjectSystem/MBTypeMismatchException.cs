namespace TaleWorlds.ObjectSystem;

public class MBTypeMismatchException : ObjectSystemException
{
	internal MBTypeMismatchException(string exceptionString)
		: base("Type Does not match with the expected one. " + exceptionString)
	{
	}
}
