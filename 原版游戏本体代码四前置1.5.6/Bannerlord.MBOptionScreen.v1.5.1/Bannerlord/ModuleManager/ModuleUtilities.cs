using System;
using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.ModuleManager;

internal static class ModuleUtilities
{
	public static bool AreDependenciesPresent(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata in module.DependenciesLoadBeforeThisDistinct())
		{
			if (!metadata.IsOptional && modules.All((Bannerlord.ModuleManager.ModuleInfoExtended x) => !string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
			{
				return false;
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata2 in module.DependenciesIncompatiblesDistinct())
		{
			if (modules.Any((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata2.Id, StringComparison.Ordinal)))
			{
				return false;
			}
		}
		return true;
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleInfoExtended> GetDependencies(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visited = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		return GetDependencies(modules, module, visited, new Bannerlord.ModuleManager.ModuleSorterOptions
		{
			SkipOptionals = false,
			SkipExternalDependencies = false
		});
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleInfoExtended> GetDependencies(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended module, Bannerlord.ModuleManager.ModuleSorterOptions options)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visited = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		return GetDependencies(modules, module, visited, options);
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleInfoExtended> GetDependencies(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended module, HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visited, Bannerlord.ModuleManager.ModuleSorterOptions options)
	{
		List<Bannerlord.ModuleManager.ModuleInfoExtended> dependencies = new List<Bannerlord.ModuleManager.ModuleInfoExtended>();
		Bannerlord.ModuleManager.ModuleSorter.Visit(module, (Bannerlord.ModuleManager.ModuleInfoExtended x) => GetDependenciesInternal(modules, x, options), delegate(Bannerlord.ModuleManager.ModuleInfoExtended moduleToAdd)
		{
			if (moduleToAdd != module)
			{
				dependencies.Add(moduleToAdd);
			}
		}, visited);
		return dependencies;
	}

	private static IEnumerable<Bannerlord.ModuleManager.ModuleInfoExtended> GetDependenciesInternal(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended module, Bannerlord.ModuleManager.ModuleSorterOptions options)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata dependentModuleMetadata in module.DependenciesLoadBeforeThisDistinct())
		{
			if (!dependentModuleMetadata.IsOptional || !options.SkipOptionals)
			{
				Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, dependentModuleMetadata.Id, StringComparison.Ordinal));
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
		foreach (Bannerlord.ModuleManager.ModuleInfoExtended moduleInfo in modules)
		{
			foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in moduleInfo.DependenciesLoadAfterThisDistinct())
			{
				if ((!item.IsOptional || !options.SkipOptionals) && string.Equals(item.Id, module.Id, StringComparison.Ordinal))
				{
					yield return moduleInfo;
				}
			}
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateModule(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isSelected)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		foreach (Bannerlord.ModuleManager.ModuleIssue item in ValidateModule(modules, targetModule, visitedModules, isSelected, (Bannerlord.ModuleManager.ModuleInfoExtended x) => ValidateModule(modules, x, isSelected).Count() == 0))
		{
			yield return item;
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateModule(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isValid)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		foreach (Bannerlord.ModuleManager.ModuleIssue item in ValidateModule(modules, targetModule, visitedModules, isSelected, isValid))
		{
			yield return item;
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateModule(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isValid)
	{
		foreach (Bannerlord.ModuleManager.ModuleIssue item in ValidateModuleDependenciesDeclarations(modules, targetModule))
		{
			yield return item;
		}
		foreach (Bannerlord.ModuleManager.ModuleIssue item2 in ValidateModuleDependencies(modules, targetModule, visitedModules, isSelected, isValid))
		{
			yield return item2;
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateModuleDependenciesDeclarations(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule)
	{
		foreach (string item in (from x in targetModule.DependenciesToLoadDistinct()
			select x.Id).Intersect(from x in targetModule.DependenciesIncompatiblesDistinct()
			select x.Id))
		{
			yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, item, Bannerlord.ModuleManager.ModuleIssueType.DependencyConflictDependentAndIncompatible)
			{
				Reason = "Module '" + item + "' is both depended upon and marked as incompatible"
			};
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata item2 in targetModule.DependentModuleMetadatas.Where((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.IsIncompatible && x.LoadType != Bannerlord.ModuleManager.LoadType.None))
		{
			yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, item2.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyConflictDependentAndIncompatible)
			{
				Reason = "Module '" + item2.Id + "' is both depended upon and marked as incompatible"
			};
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata module in targetModule.DependenciesLoadBeforeThisDistinct())
		{
			Bannerlord.ModuleManager.DependentModuleMetadata dependentModuleMetadata = targetModule.DependenciesLoadAfterThisDistinct().FirstOrDefault((Bannerlord.ModuleManager.DependentModuleMetadata x) => string.Equals(x.Id, module.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata != null)
			{
				yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, dependentModuleMetadata.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter)
				{
					Reason = "Module '" + dependentModuleMetadata.Id + "' is both depended upon as LoadBefore and LoadAfter"
				};
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata module2 in from x in targetModule.DependenciesToLoadDistinct()
			where x.LoadType != Bannerlord.ModuleManager.LoadType.None
			select x)
		{
			Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, module2.Id, StringComparison.Ordinal));
			Bannerlord.ModuleManager.DependentModuleMetadata dependentModuleMetadata2 = moduleInfoExtended?.DependenciesToLoadDistinct().Where((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.LoadType != Bannerlord.ModuleManager.LoadType.None).FirstOrDefault((Bannerlord.ModuleManager.DependentModuleMetadata x) => string.Equals(x.Id, targetModule.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata2 != null && dependentModuleMetadata2.LoadType == module2.LoadType)
			{
				yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, dependentModuleMetadata2.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyConflictCircular)
				{
					Reason = "Circular dependencies. '" + targetModule.Id + "' and '" + moduleInfoExtended.Id + "' depend on each other"
				};
			}
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateModuleDependencies(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> isValid)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata in targetModule.DependenciesToLoadDistinct())
		{
			if (!metadata.IsOptional && !modules.Any((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
			{
				if (metadata.Version != Bannerlord.ModuleManager.ApplicationVersion.Empty)
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.MissingDependencies)
					{
						Reason = $"Missing '{metadata.Id}' {metadata.Version}",
						SourceVersion = new Bannerlord.ModuleManager.ApplicationVersionRange(metadata.Version, metadata.Version)
					};
				}
				else if (metadata.VersionRange != Bannerlord.ModuleManager.ApplicationVersionRange.Empty)
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.MissingDependencies)
					{
						Reason = $"Missing '{metadata.Id}' {metadata.VersionRange}",
						SourceVersion = metadata.VersionRange
					};
				}
				else
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.MissingDependencies)
					{
						Reason = "Missing '" + metadata.Id + "'"
					};
				}
				yield break;
			}
		}
		Bannerlord.ModuleManager.ModuleSorterOptions options = new Bannerlord.ModuleManager.ModuleSorterOptions
		{
			SkipOptionals = true,
			SkipExternalDependencies = true
		};
		Bannerlord.ModuleManager.ModuleInfoExtended[] array = GetDependencies(modules, targetModule, visitedModules, options).ToArray();
		Bannerlord.ModuleManager.ModuleInfoExtended[] array2 = array;
		foreach (Bannerlord.ModuleManager.ModuleInfoExtended dependency in array2)
		{
			Bannerlord.ModuleManager.DependentModuleMetadata dependentModuleMetadata = targetModule.DependenciesAllDistinct().FirstOrDefault((Bannerlord.ModuleManager.DependentModuleMetadata x) => string.Equals(x.Id, dependency.Id, StringComparison.Ordinal));
			if ((object)dependentModuleMetadata != null && (object)dependentModuleMetadata != null && dependentModuleMetadata.LoadType == Bannerlord.ModuleManager.LoadType.LoadBeforeThis && !dependentModuleMetadata.IsOptional && !dependentModuleMetadata.IsIncompatible)
			{
				Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, dependency.Id, StringComparison.Ordinal));
				if ((object)moduleInfoExtended == null)
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, dependency.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyMissingDependencies)
					{
						Reason = "'" + dependency.Id + "' is missing it's dependencies!"
					};
				}
				else if (!isValid(moduleInfoExtended))
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, dependency.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyValidationError)
					{
						Reason = "'" + dependency.Id + "' has unresolved issues!"
					};
				}
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata2 in targetModule.DependenciesToLoadDistinct())
		{
			if (metadata2.Version == Bannerlord.ModuleManager.ApplicationVersion.Empty && metadata2.VersionRange == Bannerlord.ModuleManager.ApplicationVersionRange.Empty)
			{
				continue;
			}
			Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended2 = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata2.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended2 == null)
			{
				continue;
			}
			if (metadata2.Version != Bannerlord.ModuleManager.ApplicationVersion.Empty && !metadata2.IsOptional && Bannerlord.ModuleManager.ApplicationVersionComparer.CompareStandard(metadata2.Version, moduleInfoExtended2.Version) > 0)
			{
				yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, moduleInfoExtended2.Id, Bannerlord.ModuleManager.ModuleIssueType.VersionMismatchLessThanOrEqual)
				{
					Reason = $"'{moduleInfoExtended2.Id}' wrong version <= {metadata2.Version}",
					SourceVersion = new Bannerlord.ModuleManager.ApplicationVersionRange(metadata2.Version, metadata2.Version)
				};
			}
			else if (metadata2.VersionRange != Bannerlord.ModuleManager.ApplicationVersionRange.Empty && !metadata2.IsOptional)
			{
				if (Bannerlord.ModuleManager.ApplicationVersionComparer.CompareStandard(metadata2.VersionRange.Min, moduleInfoExtended2.Version) > 0)
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, moduleInfoExtended2.Id, Bannerlord.ModuleManager.ModuleIssueType.VersionMismatchLessThan)
					{
						Reason = $"'{moduleInfoExtended2?.Id}' wrong version < [{metadata2.VersionRange}]",
						SourceVersion = metadata2.VersionRange
					};
				}
				else if (Bannerlord.ModuleManager.ApplicationVersionComparer.CompareStandard(metadata2.VersionRange.Max, moduleInfoExtended2.Version) < 0)
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, moduleInfoExtended2.Id, Bannerlord.ModuleManager.ModuleIssueType.VersionMismatchGreaterThan)
					{
						Reason = $"'{moduleInfoExtended2.Id}' wrong version > [{metadata2.VersionRange}]",
						SourceVersion = metadata2.VersionRange
					};
				}
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata3 in targetModule.DependenciesIncompatiblesDistinct())
		{
			Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended3 = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata3.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended3 != null && isSelected(moduleInfoExtended3) && isSelected(moduleInfoExtended3))
			{
				yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, moduleInfoExtended3.Id, Bannerlord.ModuleManager.ModuleIssueType.Incompatible)
				{
					Reason = "'" + moduleInfoExtended3.Id + "' is incompatible with this module"
				};
			}
		}
		foreach (Bannerlord.ModuleManager.ModuleInfoExtended module in modules)
		{
			if (string.Equals(module.Id, targetModule.Id, StringComparison.Ordinal) || !isSelected(module))
			{
				continue;
			}
			foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module.DependenciesIncompatiblesDistinct())
			{
				if (string.Equals(item.Id, targetModule.Id, StringComparison.Ordinal) && isSelected(module))
				{
					yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, module.Id, Bannerlord.ModuleManager.ModuleIssueType.Incompatible)
					{
						Reason = "'" + module.Id + "' is incompatible with this module"
					};
				}
			}
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateLoadOrder(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		foreach (Bannerlord.ModuleManager.ModuleIssue item in ValidateLoadOrder(modules, targetModule, visitedModules))
		{
			yield return item;
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.ModuleIssue> ValidateLoadOrder(IReadOnlyList<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules)
	{
		int targetModuleIdx = modules.IndexOf(targetModule);
		if (targetModuleIdx == -1)
		{
			yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, targetModule.Id, Bannerlord.ModuleManager.ModuleIssueType.Missing)
			{
				Reason = $"Missing '{targetModule.Id}' {targetModule.Version} in modules list",
				SourceVersion = new Bannerlord.ModuleManager.ApplicationVersionRange(targetModule.Version, targetModule.Version)
			};
			yield break;
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata in targetModule.DependenciesToLoad().DistinctBy((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.Id))
		{
			int metadataIdx = modules.IndexOf((Bannerlord.ModuleManager.ModuleInfoExtended x) => x.Id == metadata.Id);
			if (metadataIdx == -1)
			{
				if (!metadata.IsOptional)
				{
					if (metadata.Version != Bannerlord.ModuleManager.ApplicationVersion.Empty)
					{
						yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.MissingDependencies)
						{
							Reason = $"Missing '{metadata.Id}' {metadata.Version}",
							SourceVersion = new Bannerlord.ModuleManager.ApplicationVersionRange(metadata.Version, metadata.Version)
						};
					}
					else if (metadata.VersionRange != Bannerlord.ModuleManager.ApplicationVersionRange.Empty)
					{
						yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.MissingDependencies)
						{
							Reason = $"Missing '{metadata.Id}' {metadata.VersionRange}",
							SourceVersion = metadata.VersionRange
						};
					}
					else
					{
						yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.MissingDependencies)
						{
							Reason = "Missing '" + metadata.Id + "'"
						};
					}
				}
				continue;
			}
			if (metadata.LoadType == Bannerlord.ModuleManager.LoadType.LoadBeforeThis && metadataIdx > targetModuleIdx)
			{
				yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyNotLoadedBeforeThis)
				{
					Reason = "'" + targetModule.Id + "' should be loaded before '" + metadata.Id + "'"
				};
			}
			if (metadata.LoadType == Bannerlord.ModuleManager.LoadType.LoadAfterThis && metadataIdx < targetModuleIdx)
			{
				yield return new Bannerlord.ModuleManager.ModuleIssue(targetModule, metadata.Id, Bannerlord.ModuleManager.ModuleIssueType.DependencyNotLoadedAfterThis)
				{
					Reason = "'" + targetModule.Id + "' should be loaded after '" + metadata.Id + "'"
				};
			}
		}
	}

	public static void EnableModule(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getSelected, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getDisabled, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setDisabled)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		EnableModuleInternal(modules, targetModule, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
	}

	private static void EnableModuleInternal(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getSelected, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getDisabled, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setDisabled)
	{
		if (visitedModules.Contains(targetModule))
		{
			return;
		}
		visitedModules.Add(targetModule);
		setSelected(targetModule, arg2: true);
		Bannerlord.ModuleManager.ModuleSorterOptions options = new Bannerlord.ModuleManager.ModuleSorterOptions
		{
			SkipOptionals = true,
			SkipExternalDependencies = true
		};
		Bannerlord.ModuleManager.ModuleInfoExtended[] source = GetDependencies(modules, targetModule, options).ToArray();
		foreach (Bannerlord.ModuleManager.ModuleInfoExtended module in modules)
		{
			if (!getSelected(module) && source.Any((Bannerlord.ModuleManager.ModuleInfoExtended d) => string.Equals(d.Id, module.Id, StringComparison.Ordinal)))
			{
				EnableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata in targetModule.DependenciesLoadAfterThisDistinct())
		{
			if (!metadata.IsOptional && !metadata.IsIncompatible)
			{
				Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
				if ((object)moduleInfoExtended != null && !getSelected(moduleInfoExtended))
				{
					EnableModuleInternal(modules, moduleInfoExtended, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
				}
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata2 in targetModule.DependenciesIncompatiblesDistinct())
		{
			Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended2 = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata2.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended2 != null)
			{
				if (getSelected(moduleInfoExtended2))
				{
					DisableModuleInternal(modules, moduleInfoExtended2, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
				}
				setDisabled(moduleInfoExtended2, arg2: true);
			}
		}
		foreach (Bannerlord.ModuleManager.ModuleInfoExtended module2 in modules)
		{
			foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module2.DependenciesIncompatiblesDistinct())
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

	public static void DisableModule(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getSelected, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getDisabled, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setDisabled)
	{
		HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules = new HashSet<Bannerlord.ModuleManager.ModuleInfoExtended>();
		DisableModuleInternal(modules, targetModule, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
	}

	private static void DisableModuleInternal(IReadOnlyCollection<Bannerlord.ModuleManager.ModuleInfoExtended> modules, Bannerlord.ModuleManager.ModuleInfoExtended targetModule, HashSet<Bannerlord.ModuleManager.ModuleInfoExtended> visitedModules, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getSelected, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setSelected, Func<Bannerlord.ModuleManager.ModuleInfoExtended, bool> getDisabled, Action<Bannerlord.ModuleManager.ModuleInfoExtended, bool> setDisabled)
	{
		if (visitedModules.Contains(targetModule))
		{
			return;
		}
		visitedModules.Add(targetModule);
		setSelected(targetModule, arg2: false);
		Bannerlord.ModuleManager.ModuleSorterOptions options = new Bannerlord.ModuleManager.ModuleSorterOptions
		{
			SkipOptionals = true,
			SkipExternalDependencies = true
		};
		foreach (Bannerlord.ModuleManager.ModuleInfoExtended module in modules)
		{
			IEnumerable<Bannerlord.ModuleManager.ModuleInfoExtended> dependencies = GetDependencies(modules, module, options);
			if (getSelected(module) && dependencies.Any((Bannerlord.ModuleManager.ModuleInfoExtended d) => string.Equals(d.Id, targetModule.Id, StringComparison.Ordinal)))
			{
				DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
			}
			foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module.DependenciesLoadAfterThisDistinct())
			{
				if (string.Equals(item.Id, targetModule.Id, StringComparison.Ordinal) && !item.IsOptional && !item.IsIncompatible && getSelected(module))
				{
					DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
				}
			}
			foreach (Bannerlord.ModuleManager.DependentModuleMetadata item2 in module.DependenciesIncompatiblesDistinct())
			{
				if (string.Equals(item2.Id, targetModule.Id, StringComparison.Ordinal))
				{
					setDisabled(module, getDisabled(module) & !AreDependenciesPresent(modules, module));
				}
			}
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata metadata in targetModule.DependenciesIncompatiblesDistinct())
		{
			Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = modules.FirstOrDefault((Bannerlord.ModuleManager.ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
			if ((object)moduleInfoExtended != null)
			{
				setDisabled(moduleInfoExtended, arg2: false);
			}
		}
	}
}
