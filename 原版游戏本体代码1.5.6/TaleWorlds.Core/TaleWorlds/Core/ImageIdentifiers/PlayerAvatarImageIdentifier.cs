using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core.ImageIdentifiers;

public class PlayerAvatarImageIdentifier : ImageIdentifier
{
	public PlayerAvatarImageIdentifier(PlayerId playerId, int forcedAvatarIndex)
	{
		base.Id = playerId.ToString();
		base.AdditionalArgs = $"{forcedAvatarIndex}";
		base.TextureProviderName = "PlayerAvatarImageTextureProvider";
	}
}
