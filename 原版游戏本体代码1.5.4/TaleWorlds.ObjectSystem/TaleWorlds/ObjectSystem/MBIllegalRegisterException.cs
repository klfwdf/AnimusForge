namespace TaleWorlds.ObjectSystem;

public class MBIllegalRegisterException : ObjectSystemException
{
	internal MBIllegalRegisterException()
		: base("A registered Object exists with same name.")
	{
	}
}
