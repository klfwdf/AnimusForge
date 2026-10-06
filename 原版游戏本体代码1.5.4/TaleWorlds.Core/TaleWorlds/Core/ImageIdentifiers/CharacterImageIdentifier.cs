namespace TaleWorlds.Core.ImageIdentifiers;

public class CharacterImageIdentifier : ImageIdentifier
{
	public CharacterImageIdentifier(CharacterCode characterCode)
	{
		base.Id = characterCode?.Code ?? "";
		base.AdditionalArgs = "";
		base.TextureProviderName = "CharacterImageTextureProvider";
	}
}
