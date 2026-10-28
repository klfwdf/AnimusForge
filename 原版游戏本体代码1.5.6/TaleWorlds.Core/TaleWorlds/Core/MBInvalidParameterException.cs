namespace TaleWorlds.Core;

public class MBInvalidParameterException : MBException
{
	public MBInvalidParameterException(string parameterName)
		: base("The parameter must be valid : " + parameterName)
	{
	}
}
