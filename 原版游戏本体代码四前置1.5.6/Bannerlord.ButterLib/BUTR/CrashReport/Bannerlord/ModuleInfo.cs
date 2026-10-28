using System.Collections.Generic;
using System.Linq;
using BUTR.CrashReport.Models;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ModuleManager;

namespace BUTR.CrashReport.Bannerlord;

internal class ModuleInfo : IModuleInfo
{
	public ModuleInfoExtendedHelper InternalModuleInfo { get; }

	public string Id => InternalModuleInfo.Id;

	public string Version => InternalModuleInfo.Version.ToString();

	public string UpdateInfo => InternalModuleInfo.UpdateInfo;

	public IEnumerable<IModuleSubModuleInfo> SubModules => InternalModuleInfo.SubModules.Select((SubModuleInfoExtended x) => new ModuleSubModuleInfo(x));

	public ModuleInfo(ModuleInfoExtendedHelper internalModuleInfo)
	{
		InternalModuleInfo = internalModuleInfo;
	}
}
