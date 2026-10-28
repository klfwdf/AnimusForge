using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public class PlayerAvatarImageIdentifierVM : ImageIdentifierVM
{
	public PlayerAvatarImageIdentifierVM(PlayerId playerId, int forcedAvatarIndex)
	{
		base.ImageIdentifier = new PlayerAvatarImageIdentifier(playerId, forcedAvatarIndex);
	}
}
