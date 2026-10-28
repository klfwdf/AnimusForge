using System;
using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.ModuleManager;

internal static class ModuleUtilities
{
	public static bool AreDependenciesPresent(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata metadata in module.DependenciesLoadBeforeThisDistinct())
		{
			if (metadata.IsOptional || !modules.All((ModuleInfoExtended x) => !string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
			{
				continue;
			}
			return false;
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
				ModuleInfoExtended moduleInfo = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, dependentModuleMetadata.Id, StringComparison.Ordinal));
				if ((dependentModuleMetadata.IsOptional || (object)moduleInfo != null) && (object)moduleInfo != null)
				{
					yield return moduleInfo;
				}
			}
		}
		if (options.SkipExternalDependencies)
		{
			yield break;
		}
		foreach (ModuleInfoExtended moduleInfo2 in modules)
		{
			foreach (DependentModuleMetadata dependentModuleMetadata2 in moduleInfo2.DependenciesLoadAfterThisDistinct())
			{
				if ((!dependentModuleMetadata2.IsOptional || !options.SkipOptionals) && string.Equals(dependentModuleMetadata2.Id, module.Id, StringComparison.Ordinal))
				{
					yield return moduleInfo2;
				}
			}
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected)
	{
		foreach (ModuleIssue item in ValidateModule(visitedModules: new HashSet<ModuleInfoExtended>(), modules: modules, targetModule: targetModule, isSelected: isSelected, isValid: (ModuleInfoExtended x) => ValidateModule(modules, x, isSelected).Count() == 0))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssue item in ValidateModule(modules, targetModule, visited, isSelected, isValid))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		foreach (ModuleIssue item in ValidateModuleDependenciesDeclarations(modules, targetModule))
		{
			yield return item;
		}
		foreach (ModuleIssue item2 in ValidateModuleDependencies(modules, targetModule, visitedModules, isSelected, isValid))
		{
			yield return item2;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModuleDependenciesDeclarations(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		foreach (string moduleId in (from x in targetModule.DependenciesToLoadDistinct()
			select x.Id).Intersect(from x in targetModule.DependenciesIncompatiblesDistinct()
			select x.Id))
		{
			yield return new ModuleIssue(targetModule, moduleId, ModuleIssueType.DependencyConflictDependentAndIncompatible)
			{
				Reason = "Module '" + moduleId + "' is both depended upon and marked as incompatible"
			};
		}
		foreach (DependentModuleMetadata dependency in targetModule.DependentModuleMetadatas.Where((DependentModuleMetadata x) => x.IsIncompatible && x.LoadType != LoadType.None))
		{
			yield return new ModuleIssue(targetModule, dependency.Id, ModuleIssueType.DependencyConflictDependentAndIncompatible)
			{
				Reason = "Module '" + dependency.Id + "' is both depended upon and marked as incompatible"
			};
		}
		foreach (DependentModuleMetadata module in targetModule.DependenciesLoadBeforeThisDistinct())
		{
			DependentModuleMetadata metadata = targetModule.DependenciesLoadAfterThisDistinct().FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, module.Id, StringComparison.Ordinal));
			if ((object)metadata != null)
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter)
				{
					Reason = "Module '" + metadata.Id + "' is both depended upon as LoadBefore and LoadAfter"
				};
			}
		}
		foreach (DependentModuleMetadata module2 in from x in targetModule.DependenciesToLoadDistinct()
			where x.LoadType != LoadType.None
			select x)
		{
			ModuleInfoExtended moduleInfo = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, module2.Id, StringComparison.Ordinal));
			DependentModuleMetadata metadata2 = moduleInfo?.DependenciesToLoadDistinct().Where((DependentModuleMetadata x) => x.LoadType != LoadType.None).FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, targetModule.Id, StringComparison.Ordinal));
			if ((object)metadata2 != null && metadata2.LoadType == module2.LoadType)
			{
				yield return new ModuleIssue(targetModule, metadata2.Id, ModuleIssueType.DependencyConflictCircular)
				{
					Reason = "Circular dependencies. '" + targetModule.Id + "' and '" + moduleInfo.Id + "' depend on each other"
				};
			}
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModuleDependencies(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesToLoadDistinct())
		{
			if (metadata.IsOptional || modules.Any((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
			{
				continue;
			}
			if (metadata.Version != ApplicationVersion.Empty)
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.MissingDependencies)
				{
					Reason = $"Missing '{metadata.Id}' {metadata.Version}",
					SourceVersion = new ApplicationVersionRange(metadata.Version, metadata.Version)
				};
			}
			else if (metadata.VersionRange != ApplicationVersionRange.Empty)
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.MissingDependencies)
				{
					Reason = $"Missing '{metadata.Id}' {metadata.VersionRange}",
					SourceVersion = metadata.VersionRange
				};
			}
			else
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.MissingDependencies)
				{
					Reason = "Missing '" + metadata.Id + "'"
				};
			}
			yield break;
		}
		ModuleSorterOptions opts = new ModuleSorterOptions
		{
			SkipOptionals = true,
			SkipExternalDependencies = true
		};
		ModuleInfoExtended[] dependencies = GetDependencies(modules, targetModule, visitedModules, opts).ToArray();
		ModuleInfoExtended[] array = dependencies;
		foreach (ModuleInfoExtended dependency in array)
		{
			DependentModuleMetadata metadata2 = targetModule.DependenciesAllDistinct().FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, dependency.Id, StringComparison.Ordinal));
			if ((object)metadata2 != null && (object)metadata2 != null && metadata2.LoadType == LoadType.LoadBeforeThis && !metadata2.IsOptional && !metadata2.IsIncompatible)
			{
				ModuleInfoExtended depencencyModuleInfo = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, dependency.Id, StringComparison.Ordinal));
				if ((object)depencencyModuleInfo == null)
				{
					yield return new ModuleIssue(targetModule, dependency.Id, ModuleIssueType.DependencyMissingDependencies)
					{
						Reason = "'" + dependency.Id + "' is missing it's dependencies!"
					};
				}
				else if (!isValid(depencencyModuleInfo))
				{
					yield return new ModuleIssue(targetModule, dependency.Id, ModuleIssueType.DependencyValidationError)
					{
						Reason = "'" + dependency.Id + "' has unresolved issues!"
					};
				}
			}
		}
		foreach (DependentModuleMetadata metadata3 in targetModule.DependenciesToLoadDistinct())
		{
			if (metadata3.Version == ApplicationVersion.Empty && metadata3.VersionRange == ApplicationVersionRange.Empty)
			{
				continue;
			}
			ModuleInfoExtended metadataModule = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata3.Id, StringComparison.Ordinal));
			if ((object)metadataModule == null)
			{
				continue;
			}
			if (metadata3.Version != ApplicationVersion.Empty && !metadata3.IsOptional && ApplicationVersionComparer.CompareStandard(metadata3.Version, metadataModule.Version) > 0)
			{
				yield return new ModuleIssue(targetModule, metadataModule.Id, ModuleIssueType.VersionMismatchLessThanOrEqual)
				{
					Reason = $"'{metadataModule.Id}' wrong version <= {metadata3.Version}",
					SourceVersion = new ApplicationVersionRange(metadata3.Version, metadata3.Version)
				};
			}
			else if (metadata3.VersionRange != ApplicationVersionRange.Empty && !metadata3.IsOptional)
			{
				if (ApplicationVersionComparer.CompareStandard(metadata3.VersionRange.Min, metadataModule.Version) > 0)
				{
					yield return new ModuleIssue(targetModule, metadataModule.Id, ModuleIssueType.VersionMismatchLessThan)
					{
						Reason = $"'{metadataModule?.Id}' wrong version < [{metadata3.VersionRange}]",
						SourceVersion = metadata3.VersionRange
					};
				}
				else if (ApplicationVersionComparer.CompareStandard(metadata3.VersionRange.Max, metadataModule.Version) < 0)
				{
					yield return new ModuleIssue(targetModule, metadataModule.Id, ModuleIssueType.VersionMismatchGreaterThan)
					{
						Reason = $"'{metadataModule.Id}' wrong version > [{metadata3.VersionRange}]",
						SourceVersion = metadata3.VersionRange
					};
				}
			}
		}
		foreach (DependentModuleMetadata metadata4 in targetModule.DependenciesIncompatiblesDistinct())
		{
			ModuleInfoExtended metadataModule2 = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata4.Id, StringComparison.Ordinal));
			if ((object)metadataModule2 != null && isSelected(metadataModule2) && isSelected(metadataModule2))
			{
				yield return new ModuleIssue(targetModule, metadataModule2.Id, ModuleIssueType.Incompatible)
				{
					Reason = "'" + metadataModule2.Id + "' is incompatible with this module"
				};
			}
		}
		foreach (ModuleInfoExtended module in modules)
		{
			if (string.Equals(module.Id, targetModule.Id, StringComparison.Ordinal) || !isSelected(module))
			{
				continue;
			}
			foreach (DependentModuleMetadata metadata5 in module.DependenciesIncompatiblesDistinct())
			{
				if (string.Equals(metadata5.Id, targetModule.Id, StringComparison.Ordinal) && isSelected(module))
				{
					yield return new ModuleIssue(targetModule, module.Id, ModuleIssueType.Incompatible)
					{
						Reason = "'" + module.Id + "' is incompatible with this module"
					};
				}
			}
		}
	}

	public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssue item in ValidateLoadOrder(modules, targetModule, visited))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules)
	{
		int targetModuleIdx = modules.IndexOf(targetModule);
		if (targetModuleIdx == -1)
		{
			yield return new ModuleIssue(targetModule, targetModule.Id, ModuleIssueType.Missing)
			{
				Reason = $"Missing '{targetModule.Id}' {targetModule.Version} in modules list",
				SourceVersion = new ApplicationVersionRange(targetModule.Version, targetModule.Version)
			};
			yield break;
		}
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesToLoad().DistinctBy((DependentModuleMetadata x) => x.Id))
		{
			int metadataIdx = modules.IndexOf((ModuleInfoExtended x) => x.Id == metadata.Id);
			if (metadataIdx == -1)
			{
				if (!metadata.IsOptional)
				{
					if (metadata.Version != ApplicationVersion.Empty)
					{
						yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.MissingDependencies)
						{
							Reason = $"Missing '{metadata.Id}' {metadata.Version}",
							SourceVersion = new ApplicationVersionRange(metadata.Version, metadata.Version)
						};
					}
					else if (metadata.VersionRange != ApplicationVersionRange.Empty)
					{
						yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.MissingDependencies)
						{
							Reason = $"Missing '{metadata.Id}' {metadata.VersionRange}",
							SourceVersion = metadata.VersionRange
						};
					}
					else
					{
						yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.MissingDependencies)
						{
							Reason = "Missing '" + metadata.Id + "'"
						};
					}
				}
				continue;
			}
			if (metadata.LoadType == LoadType.LoadBeforeThis && metadataIdx > targetModuleIdx)
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.DependencyNotLoadedBeforeThis)
				{
					Reason = "'" + targetModule.Id + "' should be loaded before '" + metadata.Id + "'"
				};
			}
			if (metadata.LoadType == LoadType.LoadAfterThis && metadataIdx < targetModuleIdx)
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.DependencyNotLoadedAfterThis)
				{
					Reason = "'" + targetModule.Id + "' should be loaded after '" + metadata.Id + "'"
				};
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
		if (visitedModules.Contains(targetModule))
		{
			return;
		}
		visitedModules.Add(targetModule);
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
		if (visitedModules.Contains(targetModule))
		{
			return;
		}
		visitedModules.Add(targetModule);
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
