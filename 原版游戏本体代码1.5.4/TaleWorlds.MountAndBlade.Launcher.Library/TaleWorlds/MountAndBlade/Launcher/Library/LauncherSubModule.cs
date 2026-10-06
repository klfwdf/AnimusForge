using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherSubModule : ViewModel
{
	public readonly SubModuleInfo Info;

	private string _name;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	public LauncherSubModule(SubModuleInfo subModuleInfo)
	{
		Info = subModuleInfo;
		Name = subModuleInfo.Name;
	}
}
