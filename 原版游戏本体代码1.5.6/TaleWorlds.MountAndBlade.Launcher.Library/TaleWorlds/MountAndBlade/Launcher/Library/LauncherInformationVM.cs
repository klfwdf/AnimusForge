using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherInformationVM : ViewModel
{
	private bool _isEnabled;

	private string _text;

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
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
			}
		}
	}

	public LauncherInformationVM()
	{
		LauncherUI.OnAddHintInformation += ExecuteEnableHint;
		LauncherUI.OnHideHintInformation += ExecuteDisableHint;
	}

	private void ExecuteEnableHint(string text)
	{
		IsEnabled = true;
		Text = text;
	}

	private void ExecuteDisableHint()
	{
		IsEnabled = false;
	}
}
