namespace TaleWorlds.ObjectSystem;

public class MBOutOfRangeException : ObjectSystemException
{
	internal MBOutOfRangeException(string parameterName)
		: base("The given value is out of range : " + parameterName)
	{
	}
}
