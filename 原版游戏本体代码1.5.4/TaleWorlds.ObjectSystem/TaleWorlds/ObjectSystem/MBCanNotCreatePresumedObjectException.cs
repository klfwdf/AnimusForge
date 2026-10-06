namespace TaleWorlds.ObjectSystem;

public class MBCanNotCreatePresumedObjectException : ObjectSystemException
{
	internal MBCanNotCreatePresumedObjectException()
		: base("Cannot Create Presumed Object")
	{
	}
}
