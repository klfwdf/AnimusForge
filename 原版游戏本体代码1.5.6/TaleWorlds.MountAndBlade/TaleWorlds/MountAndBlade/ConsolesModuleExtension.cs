using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade;

public class ConsolesModuleExtension : IPlatformModuleExtension
{
	private List<string> _modulePaths;

	public ConsolesModuleExtension()
	{
		_modulePaths = new List<string>();
	}

	public void Initialize(List<string> args)
	{
		string platformModulePaths = Utilities.GetPlatformModulePaths();
		Debug.Print("ConsolesModuleExtension::Initialize::" + platformModulePaths + "\n");
		if (platformModulePaths.Length > 0)
		{
			_modulePaths = new List<string>(platformModulePaths.Split(new char[1] { '$' }));
		}
		else
		{
			_modulePaths = new List<string>();
		}
	}

	public string[] GetModulePaths()
	{
		return _modulePaths.ToArray();
	}

	public void Destroy()
	{
	}

	public void SetLauncherMode(bool isLauncherModeActive)
	{
	}

	public bool CheckEntitlement(string title)
	{
		return true;
	}
}
