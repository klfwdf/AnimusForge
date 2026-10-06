using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Authentication;

public class MPAuthenticationVM : ViewModel
{
	private readonly LobbyState _lobbyState;

	private bool? _hasPrivilege;

	private readonly TextObject _idleTitle = new TextObject("{=g1lgiwn1}Not Logged In");

	private readonly TextObject _idleMessage = new TextObject("{=saZ1OvPt}You can press the login button to establish connection");

	private readonly TextObject _noAccessMessage = new TextObject("{=9P0VL49j}You don't have access to multiplayer.");

	private readonly TextObject _loggingInTitle = new TextObject("{=iNqucBor}Logging In");

	private readonly TextObject _loggingInMessage = new TextObject("{=U4dzbzNb}Please wait while you are being connected to the server");

	private static readonly TextObject CantLogoutLoggingInTextObject = new TextObject("{=E0q43haK}Please wait until you are logged in.");

	private static readonly TextObject CantLogoutSearchingForMatchTextObject = new TextObject("{=DyeaObj5}Please cancel game search request before logging out.");

	private bool _isEnabled;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private bool _isLoginRequestActive;

	private bool _canTryLogin;

	private string _titleText;

	private string _messageText;

	private string _exitText;

	private string _loginText;

	private string _communityGamesText;

	private MPAuthenticationDebugVM _authenticationDebug;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				if (IsEnabled)
				{
					UpdatePrivilegeInformation();
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsLoginRequestActive
	{
		get
		{
			return _isLoginRequestActive;
		}
		set
		{
			if (value != _isLoginRequestActive)
			{
				_isLoginRequestActive = value;
				OnPropertyChangedWithValue(value, "IsLoginRequestActive");
				UpdateCanTryLogin();
			}
		}
	}

	[DataSourceProperty]
	public bool CanTryLogin
	{
		get
		{
			return _canTryLogin;
		}
		set
		{
			if (value != _canTryLogin)
			{
				_canTryLogin = value;
				OnPropertyChangedWithValue(value, "CanTryLogin");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string MessageText
	{
		get
		{
			return _messageText;
		}
		set
		{
			if (value != _messageText)
			{
				_messageText = value;
				OnPropertyChangedWithValue(value, "MessageText");
			}
		}
	}

	[DataSourceProperty]
	public string ExitText
	{
		get
		{
			return _exitText;
		}
		set
		{
			if (value != _exitText)
			{
				_exitText = value;
				OnPropertyChangedWithValue(value, "ExitText");
			}
		}
	}

	[DataSourceProperty]
	public string LoginText
	{
		get
		{
			return _loginText;
		}
		set
		{
			if (value != _loginText)
			{
				_loginText = value;
				OnPropertyChangedWithValue(value, "LoginText");
			}
		}
	}

	[DataSourceProperty]
	public string CommunityGamesText
	{
		get
		{
			return _communityGamesText;
		}
		set
		{
			if (value != _communityGamesText)
			{
				_communityGamesText = value;
				OnPropertyChangedWithValue(value, "CommunityGamesText");
			}
		}
	}

	[DataSourceProperty]
	public MPAuthenticationDebugVM AuthenticationDebug
	{
		get
		{
			return _authenticationDebug;
		}
		set
		{
			if (value != _authenticationDebug)
			{
				_authenticationDebug = value;
				OnPropertyChangedWithValue(value, "AuthenticationDebug");
			}
		}
	}

	public MPAuthenticationVM(LobbyState lobbyState)
	{
		_lobbyState = lobbyState;
		_hasPrivilege = _lobbyState.HasMultiplayerPrivilege;
		LobbyState lobbyState2 = _lobbyState;
		lobbyState2.OnMultiplayerPrivilegeUpdated = (Action<bool>)Delegate.Combine(lobbyState2.OnMultiplayerPrivilegeUpdated, new Action<bool>(OnMultiplayerPrivilegeUpdated));
		InternetAvailabilityChecker.OnInternetConnectionAvailabilityChanged = (Action<bool>)Delegate.Combine(InternetAvailabilityChecker.OnInternetConnectionAvailabilityChanged, new Action<bool>(OnInternetConnectionAvailabilityChanged));
		AuthenticationDebug = new MPAuthenticationDebugVM();
		AuthenticationDebug.IsEnabled = IsDebugAuthenticationEnabled();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ExitText = new TextObject("{=exitMenuOption}Exit").ToString();
		LoginText = new TextObject("{=lugGPVOb}Login").ToString();
		TitleText = _idleTitle.ToString();
		MessageText = _idleMessage.ToString();
		CommunityGamesText = new TextObject("{=SIIgjILk}Community Games").ToString();
		AuthenticationDebug.RefreshValues();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey?.OnFinalize();
		CancelInputKey?.OnFinalize();
		LobbyState lobbyState = _lobbyState;
		lobbyState.OnMultiplayerPrivilegeUpdated = (Action<bool>)Delegate.Remove(lobbyState.OnMultiplayerPrivilegeUpdated, new Action<bool>(OnMultiplayerPrivilegeUpdated));
		InternetAvailabilityChecker.OnInternetConnectionAvailabilityChanged = (Action<bool>)Delegate.Remove(InternetAvailabilityChecker.OnInternetConnectionAvailabilityChanged, new Action<bool>(OnInternetConnectionAvailabilityChanged));
	}

	private bool IsDebugAuthenticationEnabled()
	{
		if (Utilities.CommandLineArgumentExists("/forceMultiplayerDebugLogin"))
		{
			return true;
		}
		if (MBCommon.IsDebugMode)
		{
			return Module.CurrentModule.StartupInfo.StartupType != GameStartupType.Multiplayer;
		}
		return false;
	}

	public void OnTick(float dt)
	{
		if (IsEnabled && _lobbyState != null)
		{
			LobbyClient.State currentState = NetworkMain.GameClient.CurrentState;
			if (_hasPrivilege.HasValue && !_hasPrivilege.Value)
			{
				TitleText = _idleTitle.ToString();
				MessageText = _noAccessMessage.ToString();
			}
			else if (_lobbyState.IsLoggingIn)
			{
				IsLoginRequestActive = true;
				TitleText = _loggingInTitle.ToString();
				MessageText = _loggingInMessage.ToString();
			}
			else
			{
				IsLoginRequestActive = false;
				TitleText = _idleTitle.ToString();
				MessageText = _idleMessage.ToString();
			}
		}
	}

	public void ExecuteExit()
	{
		LobbyClient.State currentState = NetworkMain.GameClient.CurrentState;
		if (currentState == LobbyClient.State.Idle)
		{
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_exit").ToString(), GameTexts.FindText("str_mp_exit_query").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				OnExit();
			}, null));
			return;
		}
		TextObject textObject = CantLogoutSearchingForMatchTextObject;
		if (currentState == LobbyClient.State.Working || currentState == LobbyClient.State.Connected || currentState == LobbyClient.State.SessionRequested)
		{
			textObject = CantLogoutLoggingInTextObject;
		}
		InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_exit").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), null, null, null));
	}

	private void OnExit()
	{
		LobbyState lobbyState = _lobbyState;
		if (lobbyState == null || !lobbyState.IsLoggingIn)
		{
			if (Module.CurrentModule.StartupInfo.StartupType == GameStartupType.Multiplayer)
			{
				MBInitialScreenBase.DoExitButtonAction();
			}
			else
			{
				Game.Current.GameStateManager.PopState();
			}
		}
	}

	public async void ExecuteLogin()
	{
		LobbyState lobbyState = _lobbyState;
		if (lobbyState == null || !lobbyState.IsLoggingIn)
		{
			try
			{
				await _lobbyState.TryLogin();
			}
			catch (Exception ex)
			{
				Debug.Print(ex.StackTrace ?? "");
			}
		}
	}

	private void OnMultiplayerPrivilegeUpdated(bool hasPrivilege)
	{
		_hasPrivilege = hasPrivilege;
		UpdateCanTryLogin();
	}

	private void OnInternetConnectionAvailabilityChanged(bool isInternetAvailable)
	{
		UpdatePrivilegeInformation();
	}

	private void UpdateCanTryLogin()
	{
		CanTryLogin = _hasPrivilege == true && !_lobbyState.IsLoggingIn;
	}

	private void UpdatePrivilegeInformation()
	{
		_lobbyState?.UpdateHasMultiplayerPrivilege();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
