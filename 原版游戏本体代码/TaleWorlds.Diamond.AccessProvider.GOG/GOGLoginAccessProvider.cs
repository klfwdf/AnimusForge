using System;
using System.Text;
using System.Threading;
using Galaxy.Api;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.GOG
{
	// Token: 0x02000002 RID: 2
	public class GOGLoginAccessProvider : ILoginAccessProvider
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
		{
			this._initParams = initParams;
			if (GalaxyInstance.User().IsLoggedOn())
			{
				IUser user = GalaxyInstance.User();
				IFriends friends = GalaxyInstance.Friends();
				this._gogId = user.GetGalaxyID().GetRealID();
				this._oldId = user.GetGalaxyID().ToUint64();
				this._gogUserName = friends.GetPersonaName();
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x000020A2 File Offset: 0x000002A2
		string ILoginAccessProvider.GetUserName()
		{
			return this._gogUserName;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020AA File Offset: 0x000002AA
		PlayerId ILoginAccessProvider.GetPlayerId()
		{
			return new PlayerId(8, 0UL, this._gogId);
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020BC File Offset: 0x000002BC
		AccessObjectResult ILoginAccessProvider.CreateAccessObject()
		{
			if (!GalaxyInstance.User().IsLoggedOn())
			{
				return AccessObjectResult.CreateFailed(new TextObject("{=hU361b7v}Not logged in on GOG.", null));
			}
			IUser user = GalaxyInstance.User();
			if (user.IsLoggedOn())
			{
				EncryptedAppTicketListener encryptedAppTicketListener = new EncryptedAppTicketListener();
				user.RequestEncryptedAppTicket(null, 0U, encryptedAppTicketListener);
				while (!encryptedAppTicketListener.GotResult)
				{
					GalaxyInstance.ProcessData();
					Thread.Sleep(5);
				}
				byte[] array = new byte[4096];
				uint num = 0U;
				user.GetEncryptedAppTicket(array, (uint)array.Length, ref num);
				byte[] array2 = new byte[num];
				Array.Copy(array, array2, (long)((ulong)num));
				string @string = Encoding.ASCII.GetString(array2);
				return AccessObjectResult.CreateSuccess(new GOGAccessObject(this._gogUserName, this._gogId, this._oldId, @string));
			}
			return AccessObjectResult.CreateFailed(new TextObject("{=hU361b7v}Not logged in on GOG.", null));
		}

		// Token: 0x04000001 RID: 1
		private string _gogUserName;

		// Token: 0x04000002 RID: 2
		private ulong _gogId;

		// Token: 0x04000003 RID: 3
		private ulong _oldId;

		// Token: 0x04000004 RID: 4
		private PlatformInitParams _initParams;
	}
}
