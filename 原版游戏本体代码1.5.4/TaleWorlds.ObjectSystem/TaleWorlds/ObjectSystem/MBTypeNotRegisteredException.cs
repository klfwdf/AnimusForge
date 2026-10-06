namespace TaleWorlds.ObjectSystem;

public class MBTypeNotRegisteredException : ObjectSystemException
{
	internal MBTypeNotRegisteredException()
		: base("Type Not Registered")
	{
	}
}
