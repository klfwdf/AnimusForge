using Bannerlord.UIExtenderEx.Settings;

namespace Bannerlord.UIExtenderEx;

public class UIExtenderExSettings
{
	private readonly ISettingsProvider _provider;

	public static UIExtenderExSettings Instance { get; } = new UIExtenderExSettings();

	public bool DumpXML
	{
		get
		{
			return _provider.DumpXML;
		}
		set
		{
			_provider.DumpXML = value;
		}
	}

	private UIExtenderExSettings()
	{
		_provider = new SettingsSubModuleTags();
	}
}
