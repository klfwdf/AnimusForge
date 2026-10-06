namespace TaleWorlds.Core;

public class MBNotNullParameterException : MBException
{
	public MBNotNullParameterException(string parameterName)
		: base("The parameter must be null : " + parameterName)
	{
	}
}
