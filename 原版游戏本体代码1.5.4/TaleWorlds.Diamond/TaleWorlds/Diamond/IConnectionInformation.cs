namespace TaleWorlds.Diamond;

public interface IConnectionInformation
{
	string GetAddress(bool isIpv6Compatible = false);

	string GetCountry();
}
