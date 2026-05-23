using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000066 RID: 102
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModuleUtilities
	{
		// Token: 0x060003BE RID: 958 RVA: 0x0000DA20 File Offset: 0x0000BC20
		public static bool AreDependenciesPresent(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
		{
			using (IEnumerator<DependentModuleMetadata> enumerator = module.DependenciesLoadBeforeThisDistinct().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator.Current;
					if (!metadata.IsOptional && modules.All((ModuleInfoExtended x) => !string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
					{
						return false;
					}
				}
			}
			using (IEnumerator<DependentModuleMetadata> enumerator2 = module.DependenciesIncompatiblesDistinct().GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator2.Current;
					if (modules.Any((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal)))
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x060003BF RID: 959 RVA: 0x0000DAF4 File Offset: 0x0000BCF4
		public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			return ModuleUtilities.GetDependencies(modules, module, visited, new ModuleSorterOptions
			{
				SkipOptionals = false,
				SkipExternalDependencies = false
			});
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x0000DB24 File Offset: 0x0000BD24
		public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, ModuleSorterOptions options)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			return ModuleUtilities.GetDependencies(modules, module, visited, options);
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x0000DB40 File Offset: 0x0000BD40
		public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, HashSet<ModuleInfoExtended> visited, ModuleSorterOptions options)
		{
			List<ModuleInfoExtended> dependencies = new List<ModuleInfoExtended>();
			ModuleSorter.Visit<ModuleInfoExtended>(module, (ModuleInfoExtended x) => ModuleUtilities.GetDependenciesInternal(modules, x, options), delegate(ModuleInfoExtended moduleToAdd)
			{
				if (moduleToAdd != module)
				{
					dependencies.Add(moduleToAdd);
				}
			}, visited);
			return dependencies;
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x0000DB9D File Offset: 0x0000BD9D
		private static IEnumerable<ModuleInfoExtended> GetDependenciesInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, ModuleSorterOptions options)
		{
			ModuleUtilities.<GetDependenciesInternal>d__4 <GetDependenciesInternal>d__ = new ModuleUtilities.<GetDependenciesInternal>d__4(-2);
			<GetDependenciesInternal>d__.<>3__modules = modules;
			<GetDependenciesInternal>d__.<>3__module = module;
			<GetDependenciesInternal>d__.<>3__options = options;
			return <GetDependenciesInternal>d__;
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x0000DBBB File Offset: 0x0000BDBB
		public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected)
		{
			ModuleUtilities.<ValidateModule>d__5 <ValidateModule>d__ = new ModuleUtilities.<ValidateModule>d__5(-2);
			<ValidateModule>d__.<>3__modules = modules;
			<ValidateModule>d__.<>3__targetModule = targetModule;
			<ValidateModule>d__.<>3__isSelected = isSelected;
			return <ValidateModule>d__;
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x0000DBD9 File Offset: 0x0000BDD9
		public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
		{
			ModuleUtilities.<ValidateModule>d__6 <ValidateModule>d__ = new ModuleUtilities.<ValidateModule>d__6(-2);
			<ValidateModule>d__.<>3__modules = modules;
			<ValidateModule>d__.<>3__targetModule = targetModule;
			<ValidateModule>d__.<>3__isSelected = isSelected;
			<ValidateModule>d__.<>3__isValid = isValid;
			return <ValidateModule>d__;
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x0000DBFE File Offset: 0x0000BDFE
		public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
		{
			ModuleUtilities.<ValidateModule>d__7 <ValidateModule>d__ = new ModuleUtilities.<ValidateModule>d__7(-2);
			<ValidateModule>d__.<>3__modules = modules;
			<ValidateModule>d__.<>3__targetModule = targetModule;
			<ValidateModule>d__.<>3__visitedModules = visitedModules;
			<ValidateModule>d__.<>3__isSelected = isSelected;
			<ValidateModule>d__.<>3__isValid = isValid;
			return <ValidateModule>d__;
		}

		// Token: 0x060003C6 RID: 966 RVA: 0x0000DC2B File Offset: 0x0000BE2B
		public static IEnumerable<ModuleIssue> ValidateModuleDependenciesDeclarations(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
		{
			ModuleUtilities.<ValidateModuleDependenciesDeclarations>d__8 <ValidateModuleDependenciesDeclarations>d__ = new ModuleUtilities.<ValidateModuleDependenciesDeclarations>d__8(-2);
			<ValidateModuleDependenciesDeclarations>d__.<>3__modules = modules;
			<ValidateModuleDependenciesDeclarations>d__.<>3__targetModule = targetModule;
			return <ValidateModuleDependenciesDeclarations>d__;
		}

		// Token: 0x060003C7 RID: 967 RVA: 0x0000DC42 File Offset: 0x0000BE42
		public static IEnumerable<ModuleIssue> ValidateModuleDependencies(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
		{
			ModuleUtilities.<ValidateModuleDependencies>d__9 <ValidateModuleDependencies>d__ = new ModuleUtilities.<ValidateModuleDependencies>d__9(-2);
			<ValidateModuleDependencies>d__.<>3__modules = modules;
			<ValidateModuleDependencies>d__.<>3__targetModule = targetModule;
			<ValidateModuleDependencies>d__.<>3__visitedModules = visitedModules;
			<ValidateModuleDependencies>d__.<>3__isSelected = isSelected;
			<ValidateModuleDependencies>d__.<>3__isValid = isValid;
			return <ValidateModuleDependencies>d__;
		}

		// Token: 0x060003C8 RID: 968 RVA: 0x0000DC6F File Offset: 0x0000BE6F
		public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
		{
			ModuleUtilities.<ValidateLoadOrder>d__10 <ValidateLoadOrder>d__ = new ModuleUtilities.<ValidateLoadOrder>d__10(-2);
			<ValidateLoadOrder>d__.<>3__modules = modules;
			<ValidateLoadOrder>d__.<>3__targetModule = targetModule;
			return <ValidateLoadOrder>d__;
		}

		// Token: 0x060003C9 RID: 969 RVA: 0x0000DC86 File Offset: 0x0000BE86
		public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules)
		{
			ModuleUtilities.<ValidateLoadOrder>d__11 <ValidateLoadOrder>d__ = new ModuleUtilities.<ValidateLoadOrder>d__11(-2);
			<ValidateLoadOrder>d__.<>3__modules = modules;
			<ValidateLoadOrder>d__.<>3__targetModule = targetModule;
			return <ValidateLoadOrder>d__;
		}

		// Token: 0x060003CA RID: 970 RVA: 0x0000DCA0 File Offset: 0x0000BEA0
		public static void EnableModule(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			ModuleUtilities.EnableModuleInternal(modules, targetModule, visited, getSelected, setSelected, getDisabled, setDisabled);
		}

		// Token: 0x060003CB RID: 971 RVA: 0x0000DCC4 File Offset: 0x0000BEC4
		private static void EnableModuleInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			if (visitedModules.Contains(targetModule))
			{
				return;
			}
			visitedModules.Add(targetModule);
			setSelected(targetModule, true);
			ModuleSorterOptions opt = new ModuleSorterOptions
			{
				SkipOptionals = true,
				SkipExternalDependencies = true
			};
			ModuleInfoExtended[] dependencies = ModuleUtilities.GetDependencies(modules, targetModule, opt).ToArray<ModuleInfoExtended>();
			using (IEnumerator<ModuleInfoExtended> enumerator = modules.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ModuleInfoExtended module = enumerator.Current;
					if (!getSelected(module) && dependencies.Any((ModuleInfoExtended d) => string.Equals(d.Id, module.Id, StringComparison.Ordinal)))
					{
						ModuleUtilities.EnableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
					}
				}
			}
			using (IEnumerator<DependentModuleMetadata> enumerator2 = targetModule.DependenciesLoadAfterThisDistinct().GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator2.Current;
					if (!metadata.IsOptional && !metadata.IsIncompatible)
					{
						ModuleInfoExtended metadataModule = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
						if (metadataModule != null && !getSelected(metadataModule))
						{
							ModuleUtilities.EnableModuleInternal(modules, metadataModule, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
						}
					}
				}
			}
			using (IEnumerator<DependentModuleMetadata> enumerator3 = targetModule.DependenciesIncompatiblesDistinct().GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator3.Current;
					ModuleInfoExtended metadataModule2 = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
					if (metadataModule2 != null)
					{
						if (getSelected(metadataModule2))
						{
							ModuleUtilities.DisableModuleInternal(modules, metadataModule2, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
						}
						setDisabled(metadataModule2, true);
					}
				}
			}
			foreach (ModuleInfoExtended module2 in modules)
			{
				foreach (DependentModuleMetadata metadata2 in module2.DependenciesIncompatiblesDistinct())
				{
					if (string.Equals(metadata2.Id, targetModule.Id, StringComparison.Ordinal))
					{
						if (getSelected(module2))
						{
							ModuleUtilities.DisableModuleInternal(modules, module2, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
						}
						setDisabled(module2, getDisabled(module2) | !ModuleUtilities.AreDependenciesPresent(modules, module2));
					}
				}
			}
		}

		// Token: 0x060003CC RID: 972 RVA: 0x0000DF64 File Offset: 0x0000C164
		public static void DisableModule(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			ModuleUtilities.DisableModuleInternal(modules, targetModule, visited, getSelected, setSelected, getDisabled, setDisabled);
		}

		// Token: 0x060003CD RID: 973 RVA: 0x0000DF88 File Offset: 0x0000C188
		private static void DisableModuleInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			if (visitedModules.Contains(targetModule))
			{
				return;
			}
			visitedModules.Add(targetModule);
			setSelected(targetModule, false);
			ModuleSorterOptions opt = new ModuleSorterOptions
			{
				SkipOptionals = true,
				SkipExternalDependencies = true
			};
			Func<ModuleInfoExtended, bool> <>9__0;
			foreach (ModuleInfoExtended module in modules)
			{
				IEnumerable<ModuleInfoExtended> dependencies = ModuleUtilities.GetDependencies(modules, module, opt);
				if (getSelected(module))
				{
					IEnumerable<ModuleInfoExtended> source = dependencies;
					Func<ModuleInfoExtended, bool> predicate;
					if ((predicate = <>9__0) == null)
					{
						predicate = (<>9__0 = (ModuleInfoExtended d) => string.Equals(d.Id, targetModule.Id, StringComparison.Ordinal));
					}
					if (source.Any(predicate))
					{
						ModuleUtilities.DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
					}
				}
				foreach (DependentModuleMetadata metadata3 in module.DependenciesLoadAfterThisDistinct())
				{
					if (string.Equals(metadata3.Id, targetModule.Id, StringComparison.Ordinal) && !metadata3.IsOptional && !metadata3.IsIncompatible && getSelected(module))
					{
						ModuleUtilities.DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
					}
				}
				foreach (DependentModuleMetadata metadata2 in module.DependenciesIncompatiblesDistinct())
				{
					if (string.Equals(metadata2.Id, targetModule.Id, StringComparison.Ordinal))
					{
						setDisabled(module, getDisabled(module) & !ModuleUtilities.AreDependenciesPresent(modules, module));
					}
				}
			}
			using (IEnumerator<DependentModuleMetadata> enumerator4 = targetModule.DependenciesIncompatiblesDistinct().GetEnumerator())
			{
				while (enumerator4.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator4.Current;
					ModuleInfoExtended metadataModule = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
					if (metadataModule != null)
					{
						setDisabled(metadataModule, false);
					}
				}
			}
		}
	}
}
