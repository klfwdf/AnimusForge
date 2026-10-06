using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TaleWorlds.PlatformService;

public class PlatformServices
{
	private static IPlatformServices _platformServices;

	public static Action<bool> OnConnectionStateChanged;

	public static Action<bool> OnMultiplayerGameStateChanged;

	public static Action<bool, bool> OnLobbyClientStateChanged;

	public static IPlatformServices Instance => _platformServices;

	public static IPlatformInvitationServices InvitationServices => _platformServices as IPlatformInvitationServices;

	public static Action<SessionInvitationType> OnSessionInvitationAccepted { get; set; }

	public static Action OnPlatformRequestedMultiplayer { get; set; }

	public static bool IsPlatformRequestedMultiplayer { get; private set; }

	public static SessionInvitationType SessionInvitationType { get; private set; }

	public static bool IsPlatformRequestedContinueGame { get; private set; }

	public static string ProviderName => _platformServices.ProviderName;

	public static string UserId => _platformServices.UserId;

	static PlatformServices()
	{
		SessionInvitationType = SessionInvitationType.None;
		IsPlatformRequestedMultiplayer = false;
		_platformServices = new NullPlatformServices();
	}

	public static void Setup(IPlatformServices platformServices)
	{
		_platformServices = platformServices;
	}

	public static bool Initialize(IFriendListService[] additionalFriendListServices)
	{
		return _platformServices.Initialize(additionalFriendListServices);
	}

	public static void Terminate()
	{
		_platformServices.Terminate();
	}

	public static void ConnectionStateChanged(bool isAuthenticated)
	{
		OnConnectionStateChanged?.Invoke(isAuthenticated);
	}

	public static void MultiplayerGameStateChanged(bool isPlaying)
	{
		OnMultiplayerGameStateChanged?.Invoke(isPlaying);
	}

	public static void LobbyClientStateChanged(bool atLobby, bool isPartyLeaderOrSolo)
	{
		OnLobbyClientStateChanged?.Invoke(atLobby, isPartyLeaderOrSolo);
	}

	public static void FireOnSessionInvitationAccepted(SessionInvitationType sessionInvitationType)
	{
		SessionInvitationType = sessionInvitationType;
		if (OnSessionInvitationAccepted == null)
		{
			return;
		}
		Delegate[] invocationList = OnSessionInvitationAccepted.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			if (invocationList[i] is Action<SessionInvitationType> action)
			{
				action(sessionInvitationType);
			}
		}
	}

	public static void FireOnPlatformRequestedMultiplayer()
	{
		IsPlatformRequestedMultiplayer = true;
		if (OnPlatformRequestedMultiplayer == null)
		{
			return;
		}
		Delegate[] invocationList = OnPlatformRequestedMultiplayer.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			if (invocationList[i] is Action action)
			{
				action();
			}
		}
	}

	public static void OnSessionInvitationHandled()
	{
		SessionInvitationType = SessionInvitationType.None;
	}

	public static void OnPlatformMultiplayerRequestHandled()
	{
		IsPlatformRequestedMultiplayer = false;
	}

	public static void SetIsPlatformRequestedContinueGame(bool isRequested)
	{
		IsPlatformRequestedContinueGame = true;
	}

	public static async Task<string> FilterString(string content, string defaultContent)
	{
		if (!(await Instance.VerifyString(content)))
		{
			return defaultContent;
		}
		return content;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("trigger_invitation", "platform_services")]
	public static string TriggerInvitation(List<string> strings)
	{
		if (strings.Count == 0 || !Enum.TryParse<SessionInvitationType>(strings[0], out var result))
		{
			result = SessionInvitationType.Multiplayer;
		}
		FireOnSessionInvitationAccepted(result);
		return "Triggered invitation with " + result;
	}
}
