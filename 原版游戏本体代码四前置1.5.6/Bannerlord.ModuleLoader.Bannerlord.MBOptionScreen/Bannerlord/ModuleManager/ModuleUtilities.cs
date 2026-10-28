using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ModuleManager.Models.Issues;

namespace Bannerlord.ModuleManager;

internal static class ModuleUtilities
{
	public static bool AreDependenciesPresent(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata metadata in module.DependenciesLoadBeforeThisDistinct())
		{
			if (!metadata.IsOptional && modules.All((ModuleInfoExtended x) => !string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
			{
				return false;
			}
		}
		foreach (DependentModuleMetadata metadata2 in module.DependenciesIncompatiblesDistinct())
		{
			if (modules.Any((ModuleInfoExtended x) => string.Equals(x.Id, metadata2.Id, StringComparison.Ordinal)))
			{
				return false;
			}
		}
		return true;
	}

	public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
	{
		HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
		return GetDependencies(modules, module, visited, new ModuleSorterOptions
		{
			SkipOptionals = false,
			SkipExternalDependencies = false
		});
	}

	public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, ModuleSorterOptions options)
	{
		HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
		return GetDependencies(modules, module, visited, options);
	}

	public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, HashSet<ModuleInfoExtended> visited, ModuleSorterOptions options)
	{
		List<ModuleInfoExtended> dependencies = new List<ModuleInfoExtended>();
		ModuleSorter.Visit(module, (ModuleInfoExtended x) => GetDependenciesInternal(modules, x, options), delegate(ModuleInfoExtended moduleToAdd)
		{
			if (moduleToAdd != module)
			{
				dependencies.Add(moduleToAdd);
			}
		}, visited);
		return dependencies;
	}

	private static IEnumerable<ModuleInfoExtended> GetDependenciesInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, ModuleSorterOptions options)
	{
		foreach (DependentModuleMetadata dependentModuleMetadata in module.DependenciesLoadBeforeThisDistinct())
		{
			if (!dependentModuleMetadata.IsOptional || !options.SkipOptionals)
			{
				ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, dependentModuleMetadata.Id, StringComparison.Ordinal));
				if ((dependentModuleMetadata.IsOptional || (object)moduleInfoExtended != null) && (object)moduleInfoExtended != null)
				{
					yield return moduleInfoExtended;
				}
			}
		}
		if (options.SkipExternalDependencies)
		{
			yield break;
		}
		foreach (ModuleInfoExtended moduleInfo in modules)
		{
			foreach (DependentModuleMetadata item in moduleInfo.DependenciesLoadAfterThisDistinct())
			{
				if ((!item.IsOptional || !options.SkipOptionals) && string.Equals(item.Id, module.Id, StringComparison.Ordinal))
				{
					yield return moduleInfo;
				}
			}
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		return from x in ValidateModuleEx(modules, targetModule, visitedModules, isSelected, (ModuleInfoExtended x) => !ValidateModuleEx(modules, x, isSelected).Any())
			select x.ToLegacy();
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		return from x in ValidateModuleEx(modules, targetModule, visitedModules, isSelected, isValid)
			select x.ToLegacy();
	}

	public static IEnumerable<ModuleIssue> ValidateModuleCommonData(ModuleInfoExtended module)
	{
		return from x in ValidateModuleCommonDataEx(module)
			select x.ToLegacy();
	}

	public static IEnumerable<ModuleIssue> ValidateModuleDependenciesDeclarations(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		return from x in ValidateModuleDependenciesDeclarationsEx(modules, targetModule)
			select x.ToLegacy();
	}

	public static IEnumerable<ModuleIssue> ValidateModuleDependencies(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		return from x in ValidateModuleDependenciesEx(modules, targetModule, visitedModules, isSelected, isValid)
			select x.ToLegacy();
	}

	public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		foreach (ModuleIssue item in ValidateModuleCommonData(targetModule))
		{
			yield return item;
		}
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssue item2 in from x in ValidateLoadOrderEx(modules, targetModule, visitedModules)
			select x.ToLegacy())
		{
			yield return item2;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules)
	{
		return from x in ValidateLoadOrderEx(modules, targetModule, visitedModules)
			select x.ToLegacy();
	}

	public static IEnumerable<ModuleIssueV2> ValidateModuleEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssueV2 item in ValidateModuleEx(modules, targetModule, visitedModules, isSelected, (ModuleInfoExtended x) => !ValidateModuleEx(modules, x, isSelected).Any()))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssueV2> ValidateModuleEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid, bool validateDependencies = true)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssueV2 item in ValidateModuleEx(modules, targetModule, visitedModules, isSelected, isValid, validateDependencies))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssueV2> ValidateModuleEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid, bool validateDependencies = true)
	{
		foreach (ModuleIssueV2 item in ValidateModuleCommonDataEx(targetModule))
		{
			yield return item;
		}
		foreach (ModuleIssueV2 item2 in ValidateModuleDependenciesDeclarationsEx(modules, targetModule))
		{
			yield return item2;
		}
		foreach (ModuleIssueV2 item3 in ValidateModuleDependenciesEx(modules, targetModule, visitedModules, isSelected, isValid, validateDependencies))
		{
			yield return item3;
		}
	}

	public static IEnumerable<ModuleIssueV2> ValidateModuleCommonDataEx(ModuleInfoExtended module)
	{
		if (string.IsNullOrWhiteSpace(module.Id))
		{
			yield return new ModuleMissingIdIssue(module);
		}
		if (string.IsNullOrWhiteSpace(module.Name))
		{
			yield return new ModuleMissingNameIssue(module);
		}
		foreach (DependentModule item in module.DependentModules.Where((DependentModule x) => (object)x != null))
		{
			if ((object)item == null)
			{
				yield return new ModuleDependencyNullIssue(module);
				break;
			}
			if (string.IsNullOrWhiteSpace(item.Id))
			{
				yield return new ModuleDependencyMissingIdIssue(module);
			}
		}
		foreach (DependentModuleMetadata item2 in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => (object)x != null))
		{
			if ((object)item2 == null)
			{
				yield return new ModuleDependencyNullIssue(module);
				break;
			}
			if (string.IsNullOrWhiteSpace(item2.Id))
			{
				yield return new ModuleDependencyMissingIdIssue(module);
			}
		}
	}

	public static IEnumerable<ModuleIssueV2> ValidateModuleDependenciesDeclarationsEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		foreach (string item in (from x in targetModule.DependenciesToLoadDistinct()
			select x.Id).Intersect(from x in targetModule.DependenciesIncompatiblesDistinct()
			select x.Id))
		{
			yield return new ModuleDependencyConflictDependentAndIncompatibleIssue(targetModule, item);
		}
		foreach (DependentModuleMetadata item2 in from x in targetModule.DependentModuleMetadatas
			where (object)x != null
			where x.IsIncompatible && x.LoadType != LoadType.None
			select x)
		{
			yield return new ModuleDependencyConflictDependentAndIncompatibleIssue(targetModule, item2.Id);
		}
		foreach (DependentModuleMetadata module in targetModule.DependenciesLoadBeforeThisDistinct())
		{
			DependentModuleMetadata dependentModuleMetadata = targetModule.DependenciesLoadAfterThisDistinct().FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, module.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata != null)
			{
				yield return new ModuleDependencyConflictLoadBeforeAndAfterIssue(targetModule, dependentModuleMetadata);
			}
		}
		foreach (DependentModuleMetadata module2 in from x in targetModule.DependenciesToLoadDistinct()
			where x.LoadType != LoadType.None
			select x)
		{
			ModuleInfoExtended moduleInfo = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, module2.Id, StringComparison.Ordinal));
			DependentModuleMetadata dependentModuleMetadata2 = moduleInfo?.DependenciesToLoadDistinct().Where((DependentModuleMetadata x) => x.LoadType != LoadType.None).FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, targetModule.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata2 != null && dependentModuleMetadata2.LoadType == module2.LoadType)
			{
				ModuleInfoExtended circularDependency = modules.First((ModuleInfoExtended x) => moduleInfo.Id == x.Id);
				yield return new ModuleDependencyConflictCircularIssue(targetModule, circularDependency);
			}
		}
	}

	private static IEnumerable<ModuleIssueV2> ValidateModuleDependenciesEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid, bool validateDependencies = true)
	{
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesToLoadDistinct())
		{
			if (!string.IsNullOrWhiteSpace(metadata.Id) && !metadata.IsOptional && !modules.Any((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
			{
				string id = metadata.Id;
				if ((id == "BLSE.LoadingInterceptor" || id == "BLSE.AssemblyResolver") ? true : false)
				{
					yield return new ModuleMissingBLSEDependencyIssue(targetModule, metadata);
				}
				else if (metadata.Version != ApplicationVersion.Empty)
				{
					yield return new ModuleMissingExactVersionDependencyIssue(targetModule, metadata);
				}
				else if (metadata.VersionRange != ApplicationVersionRange.Empty)
				{
					yield return new ModuleMissingVersionRangeDependencyIssue(targetModule, metadata);
				}
				else
				{
					yield return new ModuleMissingUnversionedDependencyIssue(targetModule, metadata);
				}
				yield break;
			}
		}
		if (validateDependencies)
		{
			ModuleSorterOptions options = new ModuleSorterOptions
			{
				SkipOptionals = true,
				SkipExternalDependencies = true
			};
			ModuleInfoExtended[] array = GetDependencies(modules, targetModule, visitedModules, options).ToArray();
			ModuleInfoExtended[] array2 = array;
			foreach (ModuleInfoExtended dependency in array2)
			{
				DependentModuleMetadata dependentModuleMetadata = targetModule.DependenciesAllDistinct().FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, dependency.Id, StringComparison.Ordinal));
				if ((object)dependentModuleMetadata != null && (object)dependentModuleMetadata != null && dependentModuleMetadata.LoadType == LoadType.LoadBeforeThis && !dependentModuleMetadata.IsOptional && !dependentModuleMetadata.IsIncompatible)
				{
					ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, dependency.Id, StringComparison.Ordinal));
					if ((object)moduleInfoExtended == null)
					{
						yield return new ModuleDependencyMissingDependenciesIssue(targetModule, dependency.Id);
					}
					else if (!isValid(moduleInfoExtended))
					{
						yield return new ModuleDependencyValidationIssue(targetModule, dependency.Id);
					}
				}
			}
		}
		foreach (DependentModuleMetadata metadata2 in targetModule.DependenciesToLoadDistinct())
		{
			if (metadata2.Version == ApplicationVersion.Empty && metadata2.VersionRange == ApplicationVersionRange.Empty)
			{
				continue;
			}
			ModuleInfoExtended moduleInfoExtended2 = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata2.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended2 == null)
			{
				continue;
			}
			if (metadata2.Version != ApplicationVersion.Empty && !metadata2.IsOptional && ApplicationVersionComparer.CompareStandard(metadata2.Version, moduleInfoExtended2.Version) > 0)
			{
				yield return new ModuleVersionTooLowIssue(targetModule, moduleInfoExtended2, metadata2.Version);
			}
			else if (metadata2.VersionRange != ApplicationVersionRange.Empty && !metadata2.IsOptional)
			{
				if (ApplicationVersionComparer.CompareStandard(metadata2.VersionRange.Min, moduleInfoExtended2.Version) > 0)
				{
					yield return new ModuleVersionMismatchLessThanRangeIssue(targetModule, moduleInfoExtended2, metadata2.VersionRange);
				}
				else if (ApplicationVersionComparer.CompareStandard(metadata2.VersionRange.Max, moduleInfoExtended2.Version) < 0)
				{
					yield return new ModuleVersionMismatchGreaterThanRangeIssue(targetModule, moduleInfoExtended2, metadata2.VersionRange);
				}
			}
		}
		foreach (DependentModuleMetadata metadata3 in targetModule.DependenciesIncompatiblesDistinct())
		{
			ModuleInfoExtended moduleInfoExtended3 = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata3.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended3 != null && isSelected(moduleInfoExtended3) && isSelected(moduleInfoExtended3))
			{
				yield return new ModuleIncompatibleIssue(targetModule, moduleInfoExtended3);
			}
		}
		if (!validateDependencies)
		{
			yield break;
		}
		foreach (ModuleInfoExtended module in modules)
		{
			if (string.Equals(module.Id, targetModule.Id, StringComparison.Ordinal) || !isSelected(module))
			{
				continue;
			}
			foreach (DependentModuleMetadata item in module.DependenciesIncompatiblesDistinct())
			{
				if (string.Equals(item.Id, targetModule.Id, StringComparison.Ordinal) && isSelected(module))
				{
					yield return new ModuleIncompatibleIssue(targetModule, module);
				}
			}
		}
	}

	public static IEnumerable<ModuleIssueV2> ValidateLoadOrderEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		foreach (ModuleIssueV2 item in ValidateModuleCommonDataEx(targetModule))
		{
			yield return item;
		}
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssueV2 item2 in ValidateLoadOrderEx(modules, targetModule, visitedModules))
		{
			yield return item2;
		}
	}

	public static IEnumerable<ModuleIssueV2> ValidateLoadOrderEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules)
	{
		int targetModuleIdx = modules.IndexOf(targetModule);
		if (targetModuleIdx == -1)
		{
			yield return new ModuleMissingIssue(targetModule, new ApplicationVersionRange(targetModule.Version, targetModule.Version));
			yield break;
		}
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesToLoad().DistinctBy((DependentModuleMetadata x) => x.Id))
		{
			int metadataIdx = modules.IndexOf((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
			if (metadataIdx == -1)
			{
				string id = metadata.Id;
				if ((id == "BLSE.LoadingInterceptor" || id == "BLSE.AssemblyResolver") ? true : false)
				{
					yield return new ModuleMissingBLSEDependencyIssue(targetModule, metadata);
				}
				else if (!string.IsNullOrWhiteSpace(metadata.Id) && !metadata.IsOptional)
				{
					if (metadata.Version != ApplicationVersion.Empty)
					{
						yield return new ModuleMissingExactVersionDependencyIssue(targetModule, metadata);
					}
					else if (metadata.VersionRange != ApplicationVersionRange.Empty)
					{
						yield return new ModuleMissingVersionRangeDependencyIssue(targetModule, metadata);
					}
					else
					{
						yield return new ModuleMissingUnversionedDependencyIssue(targetModule, metadata);
					}
				}
			}
			else
			{
				if (metadata.LoadType == LoadType.LoadBeforeThis && metadataIdx > targetModuleIdx)
				{
					yield return new ModuleDependencyNotLoadedBeforeIssue(targetModule, metadata);
				}
				if (metadata.LoadType == LoadType.LoadAfterThis && metadataIdx < targetModuleIdx)
				{
					yield return new ModuleDependencyNotLoadedAfterIssue(targetModule, metadata);
				}
			}
		}
	}

	public static void EnableModule(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		EnableModuleInternal(modules, targetModule, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
	}

	private static void EnableModuleInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
	{
		if (!visitedModules.Add(targetModule))
		{
			return;
		}
		setSelected(targetModule, arg2: true);
		ModuleSorterOptions options = new ModuleSorterOptions
		{
			SkipOptionals = true,
			SkipExternalDependencies = true
		};
		ModuleInfoExtended[] source = GetDependencies(modules, targetModule, options).ToArray();
		foreach (ModuleInfoExtended module in modules)
		{
			if (!getSelected(module) && source.Any((ModuleInfoExtended d) => string.Equals(d.Id, module.Id, StringComparison.Ordinal)))
			{
				EnableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
			}
		}
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesLoadAfterThisDistinct())
		{
			if (!metadata.IsOptional && !metadata.IsIncompatible)
			{
				ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
				if ((object)moduleInfoExtended != null && !getSelected(moduleInfoExtended))
				{
					EnableModuleInternal(modules, moduleInfoExtended, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
				}
			}
		}
		foreach (DependentModuleMetadata metadata2 in targetModule.DependenciesIncompatiblesDistinct())
		{
			ModuleInfoExtended moduleInfoExtended2 = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata2.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended2 != null)
			{
				if (getSelected(moduleInfoExtended2))
				{
					DisableModuleInternal(modules, moduleInfoExtended2, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
				}
				setDisabled(moduleInfoExtended2, arg2: true);
			}
		}
		foreach (ModuleInfoExtended module2 in modules)
		{
			foreach (DependentModuleMetadata item in module2.DependenciesIncompatiblesDistinct())
			{
				if (string.Equals(item.Id, targetModule.Id, StringComparison.Ordinal))
				{
					if (getSelected(module2))
					{
						DisableModuleInternal(modules, module2, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
					}
					setDisabled(module2, getDisabled(module2) | !AreDependenciesPresent(modules, module2));
				}
			}
		}
	}

	public static void DisableModule(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		DisableModuleInternal(modules, targetModule, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
	}

	private static void DisableModuleInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
	{
		if (!visitedModules.Add(targetModule))
		{
			return;
		}
		setSelected(targetModule, arg2: false);
		ModuleSorterOptions options = new ModuleSorterOptions
		{
			SkipOptionals = true,
			SkipExternalDependencies = true
		};
		foreach (ModuleInfoExtended module in modules)
		{
			IEnumerable<ModuleInfoExtended> dependencies = GetDependencies(modules, module, options);
			if (getSelected(module) && dependencies.Any((ModuleInfoExtended d) => string.Equals(d.Id, targetModule.Id, StringComparison.Ordinal)))
			{
				DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
			}
			foreach (DependentModuleMetadata item in module.DependenciesLoadAfterThisDistinct())
			{
				if (string.Equals(item.Id, targetModule.Id, StringComparison.Ordinal) && !item.IsOptional && !item.IsIncompatible && getSelected(module))
				{
					DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
				}
			}
			foreach (DependentModuleMetadata item2 in module.DependenciesIncompatiblesDistinct())
			{
				if (string.Equals(item2.Id, targetModule.Id, StringComparison.Ordinal))
				{
					setDisabled(module, getDisabled(module) & !AreDependenciesPresent(modules, module));
				}
			}
		}
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesIncompatiblesDistinct())
		{
			ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended != null)
			{
				setDisabled(moduleInfoExtended, arg2: false);
			}
		}
	}
}
