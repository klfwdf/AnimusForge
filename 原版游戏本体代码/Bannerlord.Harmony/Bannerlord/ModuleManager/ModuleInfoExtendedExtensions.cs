using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000021 RID: 33
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModuleInfoExtendedExtensions
	{
		// Token: 0x06000198 RID: 408 RVA: 0x00008762 File Offset: 0x00006962
		public static IEnumerable<DependentModuleMetadata> DependenciesAllDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesAll().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x06000199 RID: 409 RVA: 0x0000878E File Offset: 0x0000698E
		public static IEnumerable<DependentModuleMetadata> DependenciesAll(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesAll>d__1 <DependenciesAll>d__ = new ModuleInfoExtendedExtensions.<DependenciesAll>d__1(-2);
			<DependenciesAll>d__.<>3__module = module;
			return <DependenciesAll>d__;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x0000879E File Offset: 0x0000699E
		public static IEnumerable<DependentModuleMetadata> DependenciesToLoadDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesToLoad().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x0600019B RID: 411 RVA: 0x000087CA File Offset: 0x000069CA
		public static IEnumerable<DependentModuleMetadata> DependenciesToLoad(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesToLoad>d__3 <DependenciesToLoad>d__ = new ModuleInfoExtendedExtensions.<DependenciesToLoad>d__3(-2);
			<DependenciesToLoad>d__.<>3__module = module;
			return <DependenciesToLoad>d__;
		}

		// Token: 0x0600019C RID: 412 RVA: 0x000087DA File Offset: 0x000069DA
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThisDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesLoadBeforeThis().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00008806 File Offset: 0x00006A06
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThis(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesLoadBeforeThis>d__5 <DependenciesLoadBeforeThis>d__ = new ModuleInfoExtendedExtensions.<DependenciesLoadBeforeThis>d__5(-2);
			<DependenciesLoadBeforeThis>d__.<>3__module = module;
			return <DependenciesLoadBeforeThis>d__;
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00008816 File Offset: 0x00006A16
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThisDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesLoadAfterThis().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00008842 File Offset: 0x00006A42
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThis(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesLoadAfterThis>d__7 <DependenciesLoadAfterThis>d__ = new ModuleInfoExtendedExtensions.<DependenciesLoadAfterThis>d__7(-2);
			<DependenciesLoadAfterThis>d__.<>3__module = module;
			return <DependenciesLoadAfterThis>d__;
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00008852 File Offset: 0x00006A52
		public static IEnumerable<DependentModuleMetadata> DependenciesIncompatiblesDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesIncompatibles().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x0000887E File Offset: 0x00006A7E
		public static IEnumerable<DependentModuleMetadata> DependenciesIncompatibles(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesIncompatibles>d__9 <DependenciesIncompatibles>d__ = new ModuleInfoExtendedExtensions.<DependenciesIncompatibles>d__9(-2);
			<DependenciesIncompatibles>d__.<>3__module = module;
			return <DependenciesIncompatibles>d__;
		}
	}
}
