using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200001F RID: 31
	public static class PlayerIdExtensions
	{
		// Token: 0x060000A4 RID: 164 RVA: 0x0000324F File Offset: 0x0000144F
		public static PeerId ConvertToPeerId(this PlayerId playerId)
		{
			return new PeerId(playerId.ToByteArray());
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x0000325D File Offset: 0x0000145D
		public static PlayerId ConvertToPlayerId(this PeerId peerId)
		{
			return new PlayerId(peerId.ToByteArray());
		}
	}
}
