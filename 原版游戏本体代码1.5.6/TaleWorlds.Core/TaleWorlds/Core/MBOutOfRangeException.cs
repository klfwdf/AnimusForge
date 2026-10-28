namespace TaleWorlds.Core;

public class MBOutOfRangeException : MBException
{
	public MBOutOfRangeException(string parameterName)
		: base("The given value is out of range : " + parameterName)
	{
	}
}
