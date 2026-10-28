using System.Collections.Generic;

namespace TaleWorlds.ModuleManager;

public interface IPlatformModuleExtension
{
	void Initialize(List<string> args);

	void Destroy();

	string[] GetModulePaths();

	void SetLauncherMode(bool isLauncherModeActive);

	bool CheckEntitlement(string title);
}
