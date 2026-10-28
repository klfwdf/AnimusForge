namespace TaleWorlds.ObjectSystem;

public class MBTooManyRegisteredTypesException : ObjectSystemException
{
	internal MBTooManyRegisteredTypesException()
		: base("Too Many Registered Types")
	{
	}
}
