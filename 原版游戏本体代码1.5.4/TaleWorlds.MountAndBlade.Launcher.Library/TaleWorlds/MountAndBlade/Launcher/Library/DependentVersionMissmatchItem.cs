using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public struct DependentVersionMissmatchItem
{
	public string MissmatchedModuleId { get; private set; }

	public List<Tuple<DependedModule, ApplicationVersion>> MissmatchedDependencies { get; private set; }

	public DependentVersionMissmatchItem(string missmatchedModuleId, List<Tuple<DependedModule, ApplicationVersion>> missmatchedDependencies)
	{
		MissmatchedModuleId = missmatchedModuleId;
		MissmatchedDependencies = missmatchedDependencies;
	}
}
