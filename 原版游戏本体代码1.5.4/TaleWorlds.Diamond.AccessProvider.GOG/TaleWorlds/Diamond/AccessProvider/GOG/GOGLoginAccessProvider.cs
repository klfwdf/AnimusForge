using System;
using System.Text;
using System.Threading;
using Galaxy.Api;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Diamond.AccessProvider.GOG;

public class GOGLoginAccessProvider : ILoginAccessProvider
{
	private string _gogUserName;

	private ulong _gogId;

	private ulong _oldId;

	private PlatformInitParams _initParams;

	void ILoginAccessProvider.Initialize(string preferredUserName, PlatformInitParams initParams)
	{
		_initParams = initParams;
		if (GalaxyInstance.User().IsLoggedOn())
		{
			IUser user = GalaxyInstance.User();
			IFriends friends = GalaxyInstance.Friends();
			_gogId = user.GetGalaxyID().GetRealID();
			_oldId = user.GetGalaxyID().ToUint64();
			_gogUserName = friends.GetPersonaName();
		}
	}

	string ILoginAccessProvider.GetUserName()
	{
		return _gogUserName;
	}

	PlayerId ILoginAccessProvider.GetPlayerId()
	{
		return new PlayerId(8, 0uL, _gogId);
	}

	AccessObjectResult ILoginAccessProvider.CreateAccessObject()
	{
		if (!GalaxyInstance.User().IsLoggedOn())
		{
			return AccessObjectResult.CreateFailed(new TextObject("{=hU361b7v}Not logged in on GOG."));
		}
		IUser user = GalaxyInstance.User();
		if (user.IsLoggedOn())
		{
			EncryptedAppTicketListener encryptedAppTicketListener = new EncryptedAppTicketListener();
			user.RequestEncryptedAppTicket(null, 0u, encryptedAppTicketListener);
			while (!encryptedAppTicketListener.GotResult)
			{
				GalaxyInstance.ProcessData();
				Thread.Sleep(5);
			}
			byte[] array = new byte[4096];
			uint currentEncryptedAppTicketSize = 0u;
			user.GetEncryptedAppTicket(array, (uint)array.Length, ref currentEncryptedAppTicketSize);
			byte[] array2 = new byte[currentEncryptedAppTicketSize];
			Array.Copy(array, array2, currentEncryptedAppTicketSize);
			string ticket = Encoding.ASCII.GetString(array2);
			return AccessObjectResult.CreateSuccess(new GOGAccessObject(_gogUserName, _gogId, _oldId, ticket));
		}
		return AccessObjectResult.CreateFailed(new TextObject("{=hU361b7v}Not logged in on GOG."));
	}
}
