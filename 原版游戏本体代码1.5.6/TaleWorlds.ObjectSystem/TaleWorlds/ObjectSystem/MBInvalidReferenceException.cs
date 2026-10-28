namespace TaleWorlds.ObjectSystem;

public class MBInvalidReferenceException : ObjectSystemException
{
	internal MBInvalidReferenceException(string exceptionString)
		: base("Reference structure is not valid. " + exceptionString)
	{
	}
}
