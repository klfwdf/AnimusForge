using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.ModuleManager;

internal static class ModuleInfoExtendedExtensions
{
	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesAllDistinct(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		return module.DependenciesAll().DistinctBy((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesAll(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata dependentModuleMetadata in module.DependentModuleMetadatas)
		{
			yield return dependentModuleMetadata;
		}
		foreach (Bannerlord.ModuleManager.DependentModule dependentModule in module.DependentModules)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = dependentModule.Id,
				LoadType = Bannerlord.ModuleManager.LoadType.LoadBeforeThis,
				IsOptional = dependentModule.IsOptional,
				Version = dependentModule.Version
			};
		}
		foreach (Bannerlord.ModuleManager.DependentModule modulesToLoadAfterThi in module.ModulesToLoadAfterThis)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = modulesToLoadAfterThi.Id,
				LoadType = Bannerlord.ModuleManager.LoadType.LoadAfterThis,
				IsOptional = modulesToLoadAfterThi.IsOptional,
				Version = modulesToLoadAfterThi.Version
			};
		}
		foreach (Bannerlord.ModuleManager.DependentModule incompatibleModule in module.IncompatibleModules)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = incompatibleModule.Id,
				IsIncompatible = true,
				IsOptional = incompatibleModule.IsOptional,
				Version = incompatibleModule.Version
			};
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesToLoadDistinct(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		return module.DependenciesToLoad().DistinctBy((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesToLoad(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module.DependentModuleMetadatas.Where((Bannerlord.ModuleManager.DependentModuleMetadata x) => !x.IsIncompatible))
		{
			yield return item;
		}
		foreach (Bannerlord.ModuleManager.DependentModule dependentModule in module.DependentModules)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = dependentModule.Id,
				LoadType = Bannerlord.ModuleManager.LoadType.LoadBeforeThis,
				IsOptional = dependentModule.IsOptional,
				Version = dependentModule.Version
			};
		}
		foreach (Bannerlord.ModuleManager.DependentModule modulesToLoadAfterThi in module.ModulesToLoadAfterThis)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = modulesToLoadAfterThi.Id,
				LoadType = Bannerlord.ModuleManager.LoadType.LoadAfterThis,
				IsOptional = modulesToLoadAfterThi.IsOptional,
				Version = modulesToLoadAfterThi.Version
			};
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesLoadBeforeThisDistinct(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		return module.DependenciesLoadBeforeThis().DistinctBy((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesLoadBeforeThis(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module.DependentModuleMetadatas.Where((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.LoadType == Bannerlord.ModuleManager.LoadType.LoadBeforeThis))
		{
			yield return item;
		}
		foreach (Bannerlord.ModuleManager.DependentModule dependentModule in module.DependentModules)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = dependentModule.Id,
				LoadType = Bannerlord.ModuleManager.LoadType.LoadBeforeThis,
				IsOptional = dependentModule.IsOptional,
				Version = dependentModule.Version
			};
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesLoadAfterThisDistinct(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		return module.DependenciesLoadAfterThis().DistinctBy((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesLoadAfterThis(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module.DependentModuleMetadatas.Where((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.LoadType == Bannerlord.ModuleManager.LoadType.LoadAfterThis))
		{
			yield return item;
		}
		foreach (Bannerlord.ModuleManager.DependentModule modulesToLoadAfterThi in module.ModulesToLoadAfterThis)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = modulesToLoadAfterThi.Id,
				LoadType = Bannerlord.ModuleManager.LoadType.LoadAfterThis,
				IsOptional = modulesToLoadAfterThi.IsOptional,
				Version = modulesToLoadAfterThi.Version
			};
		}
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesIncompatiblesDistinct(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		return module.DependenciesIncompatibles().DistinctBy((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<Bannerlord.ModuleManager.DependentModuleMetadata> DependenciesIncompatibles(this Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata item in module.DependentModuleMetadatas.Where((Bannerlord.ModuleManager.DependentModuleMetadata x) => x.IsIncompatible))
		{
			yield return item;
		}
		foreach (Bannerlord.ModuleManager.DependentModule incompatibleModule in module.IncompatibleModules)
		{
			yield return new Bannerlord.ModuleManager.DependentModuleMetadata
			{
				Id = incompatibleModule.Id,
				IsIncompatible = true,
				IsOptional = incompatibleModule.IsOptional,
				Version = incompatibleModule.Version
			};
		}
	}
}
