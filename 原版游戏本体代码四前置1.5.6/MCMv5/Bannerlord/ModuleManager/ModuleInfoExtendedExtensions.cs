using System.Collections.Generic;
using System.Linq;

namespace Bannerlord.ModuleManager;

internal static class ModuleInfoExtendedExtensions
{
	public static IEnumerable<DependentModuleMetadata> DependenciesAllDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesAll().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesAll(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata dependentModuleMetadata in module.DependentModuleMetadatas)
		{
			yield return dependentModuleMetadata;
		}
		foreach (DependentModule metadata in module.DependentModules)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata.Id,
				LoadType = LoadType.LoadBeforeThis,
				IsOptional = metadata.IsOptional,
				Version = metadata.Version
			};
		}
		foreach (DependentModule metadata2 in module.ModulesToLoadAfterThis)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata2.Id,
				LoadType = LoadType.LoadAfterThis,
				IsOptional = metadata2.IsOptional,
				Version = metadata2.Version
			};
		}
		foreach (DependentModule metadata3 in module.IncompatibleModules)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata3.Id,
				IsIncompatible = true,
				IsOptional = metadata3.IsOptional,
				Version = metadata3.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesToLoadDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesToLoad().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesToLoad(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => !x.IsIncompatible))
		{
			yield return item;
		}
		foreach (DependentModule metadata in module.DependentModules)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata.Id,
				LoadType = LoadType.LoadBeforeThis,
				IsOptional = metadata.IsOptional,
				Version = metadata.Version
			};
		}
		foreach (DependentModule metadata2 in module.ModulesToLoadAfterThis)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata2.Id,
				LoadType = LoadType.LoadAfterThis,
				IsOptional = metadata2.IsOptional,
				Version = metadata2.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThisDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesLoadBeforeThis().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThis(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => x.LoadType == LoadType.LoadBeforeThis))
		{
			yield return item;
		}
		foreach (DependentModule metadata in module.DependentModules)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata.Id,
				LoadType = LoadType.LoadBeforeThis,
				IsOptional = metadata.IsOptional,
				Version = metadata.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThisDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesLoadAfterThis().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThis(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => x.LoadType == LoadType.LoadAfterThis))
		{
			yield return item;
		}
		foreach (DependentModule metadata in module.ModulesToLoadAfterThis)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata.Id,
				LoadType = LoadType.LoadAfterThis,
				IsOptional = metadata.IsOptional,
				Version = metadata.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesIncompatiblesDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesIncompatibles().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesIncompatibles(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => x.IsIncompatible))
		{
			yield return item;
		}
		foreach (DependentModule metadata in module.IncompatibleModules)
		{
			yield return new DependentModuleMetadata
			{
				Id = metadata.Id,
				IsIncompatible = true,
				IsOptional = metadata.IsOptional,
				Version = metadata.Version
			};
		}
	}
}
