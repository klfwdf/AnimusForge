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
		foreach (DependentModuleMetadata item in module.DependentModuleMetadatas.Where((DependentModuleMetadata x) => (object)x != null))
		{
			yield return item;
		}
		foreach (DependentModule item2 in module.DependentModules.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item2.Id,
				LoadType = LoadType.LoadBeforeThis,
				IsOptional = item2.IsOptional,
				Version = item2.Version
			};
		}
		foreach (DependentModule item3 in module.ModulesToLoadAfterThis.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item3.Id,
				LoadType = LoadType.LoadAfterThis,
				IsOptional = item3.IsOptional,
				Version = item3.Version
			};
		}
		foreach (DependentModule item4 in module.IncompatibleModules.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item4.Id,
				IsIncompatible = true,
				IsOptional = item4.IsOptional,
				Version = item4.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesToLoadDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesToLoad().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesToLoad(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in from x in module.DependentModuleMetadatas
			where (object)x != null
			where !x.IsIncompatible
			select x)
		{
			yield return item;
		}
		foreach (DependentModule item2 in module.DependentModules.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item2.Id,
				LoadType = LoadType.LoadBeforeThis,
				IsOptional = item2.IsOptional,
				Version = item2.Version
			};
		}
		foreach (DependentModule item3 in module.ModulesToLoadAfterThis.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item3.Id,
				LoadType = LoadType.LoadAfterThis,
				IsOptional = item3.IsOptional,
				Version = item3.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThisDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesLoadBeforeThis().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThis(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in from x in module.DependentModuleMetadatas
			where (object)x != null
			where x.LoadType == LoadType.LoadBeforeThis
			select x)
		{
			yield return item;
		}
		foreach (DependentModule item2 in module.DependentModules.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item2.Id,
				LoadType = LoadType.LoadBeforeThis,
				IsOptional = item2.IsOptional,
				Version = item2.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThisDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesLoadAfterThis().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThis(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in from x in module.DependentModuleMetadatas
			where (object)x != null
			where x.LoadType == LoadType.LoadAfterThis
			select x)
		{
			yield return item;
		}
		foreach (DependentModule item2 in module.ModulesToLoadAfterThis.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item2.Id,
				LoadType = LoadType.LoadAfterThis,
				IsOptional = item2.IsOptional,
				Version = item2.Version
			};
		}
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesIncompatiblesDistinct(this ModuleInfoExtended module)
	{
		return module.DependenciesIncompatibles().DistinctBy((DependentModuleMetadata x) => x.Id);
	}

	public static IEnumerable<DependentModuleMetadata> DependenciesIncompatibles(this ModuleInfoExtended module)
	{
		foreach (DependentModuleMetadata item in from x in module.DependentModuleMetadatas
			where (object)x != null
			where x.IsIncompatible
			select x)
		{
			yield return item;
		}
		foreach (DependentModule item2 in module.IncompatibleModules.Where((DependentModule x) => (object)x != null))
		{
			yield return new DependentModuleMetadata
			{
				Id = item2.Id,
				IsIncompatible = true,
				IsOptional = item2.IsOptional,
				Version = item2.Version
			};
		}
	}
}
