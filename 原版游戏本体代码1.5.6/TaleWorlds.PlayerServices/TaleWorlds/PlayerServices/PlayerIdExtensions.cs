namespace TaleWorlds.PlayerServices;

public static class PlayerIdExtensions
{
	public static bool SupportsPlayerCard(this PlayerIdProvidedTypes type)
	{
		if (type != PlayerIdProvidedTypes.GDK)
		{
			return type == PlayerIdProvidedTypes.PS;
		}
		return true;
	}
}
