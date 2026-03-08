using System;
using System.Text;
using Steamworks;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.Steam
{
	// Token: 0x02000002 RID: 2
	public class SteamLoginAccessProvider : ILoginAccessProvider
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
		{
			if (SteamAPI.Init() && Packsize.Test())
			{
				this._appId = SteamUtils.GetAppID().m_AppId;
				this._steamId = SteamUser.GetSteamID().m_SteamID;
				this._steamUserName = SteamFriends.GetPersonaName();
				this._initParams = initParams;
			}
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000002 RID: 2 RVA: 0x00002095 File Offset: 0x00000295
		private int AppId
		{
			get
			{
				return Convert.ToInt32(this._appId);
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020A2 File Offset: 0x000002A2
		string ILoginAccessProvider.GetUserName()
		{
			return this._steamUserName;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020AA File Offset: 0x000002AA
		PlayerId ILoginAccessProvider.GetPlayerId()
		{
			return new PlayerId(2, 0UL, this._steamId);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020BC File Offset: 0x000002BC
		AccessObjectResult ILoginAccessProvider.CreateAccessObject()
		{
			if (!SteamAPI.IsSteamRunning())
			{
				return AccessObjectResult.CreateFailed(new TextObject("{=uunRVBPN}Steam is not running.", null));
			}
			byte[] array = new byte[1024];
			uint num;
			if (SteamUser.GetAuthSessionTicket(array, 1024, out num) == HAuthTicket.Invalid)
			{
				return AccessObjectResult.CreateFailed(new TextObject("{=XSU8Bbbb}Invalid Steam session.", null));
			}
			StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
			foreach (byte b in array)
			{
				stringBuilder.AppendFormat("{0:x2}", b);
			}
			string externalAccessToken = stringBuilder.ToString();
			return AccessObjectResult.CreateSuccess(new SteamAccessObject(this._steamUserName, externalAccessToken, this.AppId));
		}

		// Token: 0x04000001 RID: 1
		private string _steamUserName;

		// Token: 0x04000002 RID: 2
		private ulong _steamId;

		// Token: 0x04000003 RID: 3
		private PlatformInitParams _initParams;

		// Token: 0x04000004 RID: 4
		private uint _appId;
	}
}
