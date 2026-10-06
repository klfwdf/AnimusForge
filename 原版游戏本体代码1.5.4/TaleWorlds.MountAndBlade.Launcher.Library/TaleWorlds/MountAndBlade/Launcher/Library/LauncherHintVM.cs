using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherHintVM : ViewModel
{
	private string _text;

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

	public LauncherHintVM(string text)
	{
		Text = text;
	}

	public void ExecuteBeginHint()
	{
		if (!string.IsNullOrEmpty(Text))
		{
			LauncherUI.AddHintInformation(Text);
		}
	}

	public void ExecuteEndHint()
	{
		LauncherUI.HideHintInformation();
	}
}
