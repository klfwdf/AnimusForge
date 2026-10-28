namespace TaleWorlds.Core.ImageIdentifiers;

public class EmptyImageIdentifier : ImageIdentifier
{
	public EmptyImageIdentifier()
	{
		base.Id = string.Empty;
		base.AdditionalArgs = string.Empty;
		base.TextureProviderName = string.Empty;
	}
}
