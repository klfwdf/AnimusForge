using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.GDK
{
	// Token: 0x02000002 RID: 2
	public class GDKLoginAccessProvider : ILoginAccessProvider
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		public GDKLoginAccessProvider(string gamerTag, ulong xuid, string token, PlayerId playerId, TextObject initializationFailReason)
		{
			this._gamerTag = gamerTag;
			this._playerId = playerId;
			this._initializationFailReason = initializationFailReason;
			this._token = token;
			this._xuid = xuid;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002075 File Offset: 0x00000275
		void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
		{
			this._initParams = initParams;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x0000207E File Offset: 0x0000027E
		string ILoginAccessProvider.GetUserName()
		{
			return this._gamerTag;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002086 File Offset: 0x00000286
		PlayerId ILoginAccessProvider.GetPlayerId()
		{
			return this._playerId;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002090 File Offset: 0x00000290
		AccessObjectResult ILoginAccessProvider.CreateAccessObject()
		{
			if (this._initializationFailReason != null)
			{
				return AccessObjectResult.CreateFailed(this._initializationFailReason);
			}
			if (this._playerId == PlayerId.Empty)
			{
				return AccessObjectResult.CreateFailed(new TextObject("{=leU2EiDo}Could not initialize Xbox Network player on lobby server", null));
			}
			return AccessObjectResult.CreateSuccess(new GDKAccessObject
			{
				Id = this._xuid.ToString(),
				Token = this._token
			});
		}

		// Token: 0x04000001 RID: 1
		private PlatformInitParams _initParams;

		// Token: 0x04000002 RID: 2
		private string _gamerTag;

		// Token: 0x04000003 RID: 3
		private ulong _xuid;

		// Token: 0x04000004 RID: 4
		private PlayerId _playerId;

		// Token: 0x04000005 RID: 5
		private TextObject _initializationFailReason;

		// Token: 0x04000006 RID: 6
		private string _token;
	}
}
