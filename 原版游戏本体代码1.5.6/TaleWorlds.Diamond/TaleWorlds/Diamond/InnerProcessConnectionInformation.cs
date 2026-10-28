namespace TaleWorlds.Diamond;

public sealed class InnerProcessConnectionInformation : IConnectionInformation
{
	string IConnectionInformation.GetAddress(bool isIpv6Compatible)
	{
		return "InnerProcess";
	}

	string IConnectionInformation.GetCountry()
	{
		return "TR";
	}
}
