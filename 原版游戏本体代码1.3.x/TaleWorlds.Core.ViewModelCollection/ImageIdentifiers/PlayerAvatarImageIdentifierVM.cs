using System;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers
{
	// Token: 0x02000023 RID: 35
	public class PlayerAvatarImageIdentifierVM : ImageIdentifierVM
	{
		// Token: 0x060001AB RID: 427 RVA: 0x00005B94 File Offset: 0x00003D94
		public PlayerAvatarImageIdentifierVM(PlayerId playerId, int forcedAvatarIndex)
		{
			base.ImageIdentifier = new PlayerAvatarImageIdentifier(playerId, forcedAvatarIndex);
		}
	}
}
