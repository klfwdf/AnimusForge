using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core.ImageIdentifiers
{
	// Token: 0x020000E9 RID: 233
	public class PlayerAvatarImageIdentifier : ImageIdentifier
	{
		// Token: 0x06000B8A RID: 2954 RVA: 0x0002512B File Offset: 0x0002332B
		public PlayerAvatarImageIdentifier(PlayerId playerId, int forcedAvatarIndex)
		{
			base.Id = playerId.ToString();
			base.AdditionalArgs = string.Format("{0}", forcedAvatarIndex);
			base.TextureProviderName = "PlayerAvatarImageTextureProvider";
		}
	}
}
