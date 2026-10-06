using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Authentication;

public class MPAuthenticationDebugVM : ViewModel
{
	private bool _isEnabled;

	private bool _isLoginRequestActive;

	private string _titleText;

	private string _usernameText;

	private string _passwordText;

	private string _username;

	private string _password;

	private string _loginText;

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
	public string UsernameText
	{
		get
		{
			return _usernameText;
		}
		set
		{
			if (value != _usernameText)
			{
				_usernameText = value;
				OnPropertyChangedWithValue(value, "UsernameText");
			}
		}
	}

	[DataSourceProperty]
	public string PasswordText
	{
		get
		{
			return _passwordText;
		}
		set
		{
			if (value != _passwordText)
			{
				_passwordText = value;
				OnPropertyChangedWithValue(value, "PasswordText");
			}
		}
	}

	[DataSourceProperty]
	public string Username
	{
		get
		{
			return _username;
		}
		set
		{
			if (value != _username)
			{
				_username = value;
				OnPropertyChangedWithValue(value, "Username");
			}
		}
	}

	[DataSourceProperty]
	public string Password
	{
		get
		{
			return _password;
		}
		set
		{
			if (value != _password)
			{
				_password = value;
				OnPropertyChangedWithValue(value, "Password");
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

	public MPAuthenticationDebugVM()
	{
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = new TextObject("{=!}For Debug Purposes").ToString();
		UsernameText = new TextObject("{=!}Username:").ToString();
		PasswordText = new TextObject("{=!}Password:").ToString();
		LoginText = new TextObject("{=!}Login").ToString();
	}

	private async void ExecuteLogin()
	{
		LobbyState obj = Game.Current.GameStateManager.ActiveState as LobbyState;
		IsLoginRequestActive = true;
		await obj.TryLogin(Username, Password);
		IsLoginRequestActive = false;
	}
}
