using System.Linq;
using BUTR.CrashReport.Models;
using Bannerlord.ModuleManager;

namespace BUTR.CrashReport.Bannerlord;

internal class ModuleSubModuleInfo : IModuleSubModuleInfo
{
	public SubModuleInfoExtended InternalSubModuleInfo { get; }

	public string AssemblyFile => InternalSubModuleInfo.DLLName;

	public string[] Dependencies => InternalSubModuleInfo.Assemblies.ToArray();

	public ModuleSubModuleInfo(SubModuleInfoExtended internalSubModuleInfo)
	{
		InternalSubModuleInfo = internalSubModuleInfo;
	}
}
