namespace TaleWorlds.Core;

public class MBNullParameterException : MBException
{
	public MBNullParameterException(string parameterName)
		: base("The parameter cannot be null : " + parameterName)
	{
	}
}
