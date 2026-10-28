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
		foreach (ModuleIssue item in ValidateModule(modules, targetModule, visitedModules, isSelected, (ModuleInfoExtended x) => ValidateModule(modules, x, isSelected).Count() == 0))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssue item in ValidateModule(modules, targetModule, visitedModules, isSelected, isValid))
		{
			yield return item;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		foreach (ModuleIssue item in ValidateModuleCommonData(targetModule))
		{
			yield return item;
		}
		foreach (ModuleIssue item2 in ValidateModuleDependenciesDeclarations(modules, targetModule))
		{
			yield return item2;
		}
		foreach (ModuleIssue item3 in ValidateModuleDependencies(modules, targetModule, visitedModules, isSelected, isValid))
		{
			yield return item3;
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModuleCommonData(ModuleInfoExtended module)
	{
		if (string.IsNullOrWhiteSpace(module.Id))
		{
			yield return new ModuleIssue(module, "UNKNOWN", ModuleIssueType.MissingModuleId)
			{
				Reason = "Module Id is missing for '" + module.Name + "'"
			};
		}
		if (string.IsNullOrWhiteSpace(module.Name))
		{
			yield return new ModuleIssue(module, module.Id, ModuleIssueType.MissingModuleName)
			{
				Reason = "Module Name is missing in '" + module.Id + "'"
			};
		}
		foreach (DependentModule item in module.DependentModules.Where((DependentModule x) => (object)x != null))
		{
			if ((object)item == null)
			{
				yield return new ModuleIssue(module, "UNKNOWN", ModuleIssueType.DependencyIsNull)
				{
					Reason = "Found a null dependency in '" + module.Id + "'"
				};
				break;
			}
			if (string.IsNullOrWhiteSpace(item.Id))
			{
				yield return new ModuleIssue(module, "UNKNOWN", ModuleIssueType.DependencyMissingModuleId)
				{
					Reason = "Module Id is missing for one if the dependencies of '" + module.Id + "'"
				};
			}
		}
		foreach (DependentModuleMetadata item2 in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => (object)x != null))
		{
			if ((object)item2 == null)
			{
				yield return new ModuleIssue(module, "UNKNOWN", ModuleIssueType.DependencyIsNull)
				{
					Reason = "Found a null dependency in '" + module.Id + "'"
				};
				break;
			}
			if (string.IsNullOrWhiteSpace(item2.Id))
			{
				yield return new ModuleIssue(module, "UNKNOWN", ModuleIssueType.DependencyMissingModuleId)
				{
					Reason = "Module Id is missing for one if the dependencies of '" + module.Id + "'"
				};
			}
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModuleDependenciesDeclarations(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
	{
		foreach (string item in (from x in targetModule.DependenciesToLoadDistinct()
			select x.Id).Intersect(from x in targetModule.DependenciesIncompatiblesDistinct()
			select x.Id))
		{
			yield return new ModuleIssue(targetModule, item, ModuleIssueType.DependencyConflictDependentAndIncompatible)
			{
				Reason = "Module '" + item + "' is both depended upon and marked as incompatible"
			};
		}
		foreach (DependentModuleMetadata item2 in from x in targetModule.DependentModuleMetadatas
			where (object)x != null
			where x.IsIncompatible && x.LoadType != LoadType.None
			select x)
		{
			yield return new ModuleIssue(targetModule, item2.Id, ModuleIssueType.DependencyConflictDependentAndIncompatible)
			{
				Reason = "Module '" + item2.Id + "' is both depended upon and marked as incompatible"
			};
		}
		foreach (DependentModuleMetadata module in targetModule.DependenciesLoadBeforeThisDistinct())
		{
			DependentModuleMetadata dependentModuleMetadata = targetModule.DependenciesLoadAfterThisDistinct().FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, module.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata != null)
			{
				yield return new ModuleIssue(targetModule, dependentModuleMetadata.Id, ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter)
				{
					Reason = "Module '" + dependentModuleMetadata.Id + "' is both depended upon as LoadBefore and LoadAfter"
				};
			}
		}
		foreach (DependentModuleMetadata module2 in from x in targetModule.DependenciesToLoadDistinct()
			where x.LoadType != LoadType.None
			select x)
		{
			ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, module2.Id, StringComparison.Ordinal));
			DependentModuleMetadata dependentModuleMetadata2 = moduleInfoExtended?.DependenciesToLoadDistinct().Where((DependentModuleMetadata x) => x.LoadType != LoadType.None).FirstOrDefault((DependentModuleMetadata x) => string.Equals(x.Id, targetModule.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata2 != null && dependentModuleMetadata2.LoadType == module2.LoadType)
			{
				yield return new ModuleIssue(targetModule, dependentModuleMetadata2.Id, ModuleIssueType.DependencyConflictCircular)
				{
					Reason = "Circular dependencies. '" + targetModule.Id + "' and '" + moduleInfoExtended.Id + "' depend on each other"
				};
			}
		}
	}

	public static IEnumerable<ModuleIssue> ValidateModuleDependencies(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
	{
		foreach (DependentModuleMetadata metadata in targetModule.DependenciesToLoadDistinct())
		{
			if (!metadata.IsOptional && !modules.Any((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
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
				yield break;
			}
		}
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
					yield return new ModuleIssue(targetModule, dependency.Id, ModuleIssueType.DependencyMissingDependencies)
					{
						Reason = "'" + dependency.Id + "' is missing it's dependencies!"
					};
				}
				else if (!isValid(moduleInfoExtended))
				{
					yield return new ModuleIssue(targetModule, dependency.Id, ModuleIssueType.DependencyValidationError)
					{
						Reason = "'" + dependency.Id + "' has unresolved issues!"
					};
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
				yield return new ModuleIssue(targetModule, moduleInfoExtended2.Id, ModuleIssueType.VersionMismatchLessThanOrEqual)
				{
					Reason = $"'{moduleInfoExtended2.Id}' wrong version <= {metadata2.Version}",
					SourceVersion = new ApplicationVersionRange(metadata2.Version, metadata2.Version)
				};
			}
			else if (metadata2.VersionRange != ApplicationVersionRange.Empty && !metadata2.IsOptional)
			{
				if (ApplicationVersionComparer.CompareStandard(metadata2.VersionRange.Min, moduleInfoExtended2.Version) > 0)
				{
					yield return new ModuleIssue(targetModule, moduleInfoExtended2.Id, ModuleIssueType.VersionMismatchLessThan)
					{
						Reason = $"'{moduleInfoExtended2?.Id}' wrong version < [{metadata2.VersionRange}]",
						SourceVersion = metadata2.VersionRange
					};
				}
				else if (ApplicationVersionComparer.CompareStandard(metadata2.VersionRange.Max, moduleInfoExtended2.Version) < 0)
				{
					yield return new ModuleIssue(targetModule, moduleInfoExtended2.Id, ModuleIssueType.VersionMismatchGreaterThan)
					{
						Reason = $"'{moduleInfoExtended2.Id}' wrong version > [{metadata2.VersionRange}]",
						SourceVersion = metadata2.VersionRange
					};
				}
			}
		}
		foreach (DependentModuleMetadata metadata3 in targetModule.DependenciesIncompatiblesDistinct())
		{
			ModuleInfoExtended moduleInfoExtended3 = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata3.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended3 != null && isSelected(moduleInfoExtended3) && isSelected(moduleInfoExtended3))
			{
				yield return new ModuleIssue(targetModule, moduleInfoExtended3.Id, ModuleIssueType.Incompatible)
				{
					Reason = "'" + moduleInfoExtended3.Id + "' is incompatible with this module"
				};
			}
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
		foreach (ModuleIssue item in ValidateModuleCommonData(targetModule))
		{
			yield return item;
		}
		HashSet<ModuleInfoExtended> visitedModules = new HashSet<ModuleInfoExtended>();
		foreach (ModuleIssue item2 in ValidateLoadOrder(modules, targetModule, visitedModules))
		{
			yield return item2;
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
			int metadataIdx = modules.IndexOf((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
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
					Reason = "'" + metadata.Id + "' should be loaded before '" + targetModule.Id + "'"
				};
			}
			if (metadata.LoadType == LoadType.LoadAfterThis && metadataIdx < targetModuleIdx)
			{
				yield return new ModuleIssue(targetModule, metadata.Id, ModuleIssueType.DependencyNotLoadedAfterThis)
				{
					Reason = "'" + metadata.Id + "' should be loaded after '" + targetModule.Id + "'"
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
