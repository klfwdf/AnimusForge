using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Bannerlord.ModuleManager.Models.Issues;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000024 RID: 36
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModuleUtilities
	{
		// Token: 0x060001B6 RID: 438 RVA: 0x00008CF4 File Offset: 0x00006EF4
		public static bool AreDependenciesPresent(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
		{
			using (IEnumerator<DependentModuleMetadata> enumerator = module.DependenciesLoadBeforeThisDistinct().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator.Current;
					bool isOptional = metadata.IsOptional;
					if (!isOptional)
					{
						bool flag = modules.All((ModuleInfoExtended x) => !string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
						if (flag)
						{
							return false;
						}
					}
				}
			}
			using (IEnumerator<DependentModuleMetadata> enumerator2 = module.DependenciesIncompatiblesDistinct().GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DependentModuleMetadata metadata = enumerator2.Current;
					bool flag2 = modules.Any((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
					if (flag2)
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x00008DE8 File Offset: 0x00006FE8
		public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			return ModuleUtilities.GetDependencies(modules, module, visited, new ModuleSorterOptions
			{
				SkipOptionals = false,
				SkipExternalDependencies = false
			});
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x00008E20 File Offset: 0x00007020
		public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, ModuleSorterOptions options)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			return ModuleUtilities.GetDependencies(modules, module, visited, options);
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00008E44 File Offset: 0x00007044
		public static IEnumerable<ModuleInfoExtended> GetDependencies(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, HashSet<ModuleInfoExtended> visited, ModuleSorterOptions options)
		{
			List<ModuleInfoExtended> dependencies = new List<ModuleInfoExtended>();
			ModuleSorter.Visit<ModuleInfoExtended>(module, (ModuleInfoExtended x) => ModuleUtilities.GetDependenciesInternal(modules, x, options), delegate(ModuleInfoExtended moduleToAdd)
			{
				bool flag = moduleToAdd != module;
				if (flag)
				{
					dependencies.Add(moduleToAdd);
				}
			}, visited);
			return dependencies;
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00008EA7 File Offset: 0x000070A7
		private static IEnumerable<ModuleInfoExtended> GetDependenciesInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended module, ModuleSorterOptions options)
		{
			ModuleUtilities.<GetDependenciesInternal>d__4 <GetDependenciesInternal>d__ = new ModuleUtilities.<GetDependenciesInternal>d__4(-2);
			<GetDependenciesInternal>d__.<>3__modules = modules;
			<GetDependenciesInternal>d__.<>3__module = module;
			<GetDependenciesInternal>d__.<>3__options = options;
			return <GetDependenciesInternal>d__;
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00008EC8 File Offset: 0x000070C8
		public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			return from x in ModuleUtilities.ValidateModuleEx(modules, targetModule, visited, isSelected, (ModuleInfoExtended x) => !ModuleUtilities.ValidateModuleEx(modules, x, isSelected).Any<ModuleIssueV2>(), true)
				select x.ToLegacy();
		}

		// Token: 0x060001BC RID: 444 RVA: 0x00008F38 File Offset: 0x00007138
		public static IEnumerable<ModuleIssue> ValidateModule(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			return from x in ModuleUtilities.ValidateModuleEx(modules, targetModule, visited, isSelected, isValid, true)
				select x.ToLegacy();
		}

		// Token: 0x060001BD RID: 445 RVA: 0x00008F7F File Offset: 0x0000717F
		public static IEnumerable<ModuleIssue> ValidateModuleCommonData(ModuleInfoExtended module)
		{
			return from x in ModuleUtilities.ValidateModuleCommonDataEx(module)
				select x.ToLegacy();
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00008FAB File Offset: 0x000071AB
		public static IEnumerable<ModuleIssue> ValidateModuleDependenciesDeclarations(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
		{
			return from x in ModuleUtilities.ValidateModuleDependenciesDeclarationsEx(modules, targetModule)
				select x.ToLegacy();
		}

		// Token: 0x060001BF RID: 447 RVA: 0x00008FD8 File Offset: 0x000071D8
		public static IEnumerable<ModuleIssue> ValidateModuleDependencies(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid)
		{
			return from x in ModuleUtilities.ValidateModuleDependenciesEx(modules, targetModule, visitedModules, isSelected, isValid, true)
				select x.ToLegacy();
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x0000900A File Offset: 0x0000720A
		public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
		{
			ModuleUtilities.<ValidateLoadOrder>d__10 <ValidateLoadOrder>d__ = new ModuleUtilities.<ValidateLoadOrder>d__10(-2);
			<ValidateLoadOrder>d__.<>3__modules = modules;
			<ValidateLoadOrder>d__.<>3__targetModule = targetModule;
			return <ValidateLoadOrder>d__;
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00009021 File Offset: 0x00007221
		public static IEnumerable<ModuleIssue> ValidateLoadOrder(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules)
		{
			return from x in ModuleUtilities.ValidateLoadOrderEx(modules, targetModule, visitedModules)
				select x.ToLegacy();
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0000904F File Offset: 0x0000724F
		public static IEnumerable<ModuleIssueV2> ValidateModuleEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected)
		{
			ModuleUtilities.<ValidateModuleEx>d__12 <ValidateModuleEx>d__ = new ModuleUtilities.<ValidateModuleEx>d__12(-2);
			<ValidateModuleEx>d__.<>3__modules = modules;
			<ValidateModuleEx>d__.<>3__targetModule = targetModule;
			<ValidateModuleEx>d__.<>3__isSelected = isSelected;
			return <ValidateModuleEx>d__;
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x0000906D File Offset: 0x0000726D
		public static IEnumerable<ModuleIssueV2> ValidateModuleEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid, bool validateDependencies = true)
		{
			ModuleUtilities.<ValidateModuleEx>d__13 <ValidateModuleEx>d__ = new ModuleUtilities.<ValidateModuleEx>d__13(-2);
			<ValidateModuleEx>d__.<>3__modules = modules;
			<ValidateModuleEx>d__.<>3__targetModule = targetModule;
			<ValidateModuleEx>d__.<>3__isSelected = isSelected;
			<ValidateModuleEx>d__.<>3__isValid = isValid;
			<ValidateModuleEx>d__.<>3__validateDependencies = validateDependencies;
			return <ValidateModuleEx>d__;
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x0000909A File Offset: 0x0000729A
		public static IEnumerable<ModuleIssueV2> ValidateModuleEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid, bool validateDependencies = true)
		{
			ModuleUtilities.<ValidateModuleEx>d__14 <ValidateModuleEx>d__ = new ModuleUtilities.<ValidateModuleEx>d__14(-2);
			<ValidateModuleEx>d__.<>3__modules = modules;
			<ValidateModuleEx>d__.<>3__targetModule = targetModule;
			<ValidateModuleEx>d__.<>3__visitedModules = visitedModules;
			<ValidateModuleEx>d__.<>3__isSelected = isSelected;
			<ValidateModuleEx>d__.<>3__isValid = isValid;
			<ValidateModuleEx>d__.<>3__validateDependencies = validateDependencies;
			return <ValidateModuleEx>d__;
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x000090CF File Offset: 0x000072CF
		public static IEnumerable<ModuleIssueV2> ValidateModuleCommonDataEx(ModuleInfoExtended module)
		{
			ModuleUtilities.<ValidateModuleCommonDataEx>d__15 <ValidateModuleCommonDataEx>d__ = new ModuleUtilities.<ValidateModuleCommonDataEx>d__15(-2);
			<ValidateModuleCommonDataEx>d__.<>3__module = module;
			return <ValidateModuleCommonDataEx>d__;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x000090DF File Offset: 0x000072DF
		public static IEnumerable<ModuleIssueV2> ValidateModuleDependenciesDeclarationsEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
		{
			ModuleUtilities.<ValidateModuleDependenciesDeclarationsEx>d__16 <ValidateModuleDependenciesDeclarationsEx>d__ = new ModuleUtilities.<ValidateModuleDependenciesDeclarationsEx>d__16(-2);
			<ValidateModuleDependenciesDeclarationsEx>d__.<>3__modules = modules;
			<ValidateModuleDependenciesDeclarationsEx>d__.<>3__targetModule = targetModule;
			return <ValidateModuleDependenciesDeclarationsEx>d__;
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x000090F6 File Offset: 0x000072F6
		private static IEnumerable<ModuleIssueV2> ValidateModuleDependenciesEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> isSelected, Func<ModuleInfoExtended, bool> isValid, bool validateDependencies = true)
		{
			ModuleUtilities.<ValidateModuleDependenciesEx>d__17 <ValidateModuleDependenciesEx>d__ = new ModuleUtilities.<ValidateModuleDependenciesEx>d__17(-2);
			<ValidateModuleDependenciesEx>d__.<>3__modules = modules;
			<ValidateModuleDependenciesEx>d__.<>3__targetModule = targetModule;
			<ValidateModuleDependenciesEx>d__.<>3__visitedModules = visitedModules;
			<ValidateModuleDependenciesEx>d__.<>3__isSelected = isSelected;
			<ValidateModuleDependenciesEx>d__.<>3__isValid = isValid;
			<ValidateModuleDependenciesEx>d__.<>3__validateDependencies = validateDependencies;
			return <ValidateModuleDependenciesEx>d__;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x0000912B File Offset: 0x0000732B
		public static IEnumerable<ModuleIssueV2> ValidateLoadOrderEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule)
		{
			ModuleUtilities.<ValidateLoadOrderEx>d__18 <ValidateLoadOrderEx>d__ = new ModuleUtilities.<ValidateLoadOrderEx>d__18(-2);
			<ValidateLoadOrderEx>d__.<>3__modules = modules;
			<ValidateLoadOrderEx>d__.<>3__targetModule = targetModule;
			return <ValidateLoadOrderEx>d__;
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00009142 File Offset: 0x00007342
		public static IEnumerable<ModuleIssueV2> ValidateLoadOrderEx(IReadOnlyList<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules)
		{
			ModuleUtilities.<ValidateLoadOrderEx>d__19 <ValidateLoadOrderEx>d__ = new ModuleUtilities.<ValidateLoadOrderEx>d__19(-2);
			<ValidateLoadOrderEx>d__.<>3__modules = modules;
			<ValidateLoadOrderEx>d__.<>3__targetModule = targetModule;
			<ValidateLoadOrderEx>d__.<>3__visitedModules = visitedModules;
			return <ValidateLoadOrderEx>d__;
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00009160 File Offset: 0x00007360
		public static void EnableModule(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			ModuleUtilities.EnableModuleInternal(modules, targetModule, visited, getSelected, setSelected, getDisabled, setDisabled);
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00009184 File Offset: 0x00007384
		private static void EnableModuleInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			bool flag = !visitedModules.Add(targetModule);
			if (!flag)
			{
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
						bool flag2 = !getSelected(module) && dependencies.Any((ModuleInfoExtended d) => string.Equals(d.Id, module.Id, StringComparison.Ordinal));
						if (flag2)
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
						bool isOptional = metadata.IsOptional;
						if (!isOptional)
						{
							bool isIncompatible = metadata.IsIncompatible;
							if (!isIncompatible)
							{
								ModuleInfoExtended metadataModule = modules.FirstOrDefault((ModuleInfoExtended x) => string.Equals(x.Id, metadata.Id, StringComparison.Ordinal));
								bool flag3 = metadataModule == null;
								if (!flag3)
								{
									bool flag4 = !getSelected(metadataModule);
									if (flag4)
									{
										ModuleUtilities.EnableModuleInternal(modules, metadataModule, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
									}
								}
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
						bool flag5 = metadataModule2 == null;
						if (!flag5)
						{
							bool flag6 = getSelected(metadataModule2);
							if (flag6)
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
						bool flag7 = !string.Equals(metadata2.Id, targetModule.Id, StringComparison.Ordinal);
						if (!flag7)
						{
							bool flag8 = getSelected(module2);
							if (flag8)
							{
								ModuleUtilities.DisableModuleInternal(modules, module2, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
							}
							setDisabled(module2, getDisabled(module2) | !ModuleUtilities.AreDependenciesPresent(modules, module2));
						}
					}
				}
			}
		}

		// Token: 0x060001CC RID: 460 RVA: 0x00009498 File Offset: 0x00007698
		public static void DisableModule(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			HashSet<ModuleInfoExtended> visited = new HashSet<ModuleInfoExtended>();
			ModuleUtilities.DisableModuleInternal(modules, targetModule, visited, getSelected, setSelected, getDisabled, setDisabled);
		}

		// Token: 0x060001CD RID: 461 RVA: 0x000094BC File Offset: 0x000076BC
		private static void DisableModuleInternal(IReadOnlyCollection<ModuleInfoExtended> modules, ModuleInfoExtended targetModule, HashSet<ModuleInfoExtended> visitedModules, Func<ModuleInfoExtended, bool> getSelected, Action<ModuleInfoExtended, bool> setSelected, Func<ModuleInfoExtended, bool> getDisabled, Action<ModuleInfoExtended, bool> setDisabled)
		{
			bool flag = !visitedModules.Add(targetModule);
			if (!flag)
			{
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
					bool flag2;
					if (getSelected(module))
					{
						IEnumerable<ModuleInfoExtended> source = dependencies;
						Func<ModuleInfoExtended, bool> predicate;
						if ((predicate = <>9__0) == null)
						{
							predicate = (<>9__0 = (ModuleInfoExtended d) => string.Equals(d.Id, targetModule.Id, StringComparison.Ordinal));
						}
						flag2 = source.Any(predicate);
					}
					else
					{
						flag2 = false;
					}
					bool flag3 = flag2;
					if (flag3)
					{
						ModuleUtilities.DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
					}
					foreach (DependentModuleMetadata metadata3 in module.DependenciesLoadAfterThisDistinct())
					{
						bool flag4 = !string.Equals(metadata3.Id, targetModule.Id, StringComparison.Ordinal);
						if (!flag4)
						{
							bool isOptional = metadata3.IsOptional;
							if (!isOptional)
							{
								bool isIncompatible = metadata3.IsIncompatible;
								if (!isIncompatible)
								{
									bool flag5 = getSelected(module);
									if (flag5)
									{
										ModuleUtilities.DisableModuleInternal(modules, module, visitedModules, getSelected, setSelected, getDisabled, setDisabled);
									}
								}
							}
						}
					}
					foreach (DependentModuleMetadata metadata2 in module.DependenciesIncompatiblesDistinct())
					{
						bool flag6 = !string.Equals(metadata2.Id, targetModule.Id, StringComparison.Ordinal);
						if (!flag6)
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
						bool flag7 = metadataModule == null;
						if (!flag7)
						{
							setDisabled(metadataModule, false);
						}
					}
				}
			}
		}
	}
}
