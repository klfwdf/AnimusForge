using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bannerlord.ModuleManager
{
	// Token: 0x02000063 RID: 99
	[NullableContext(1)]
	[Nullable(0)]
	internal static class ModuleInfoExtendedExtensions
	{
		// Token: 0x060003A0 RID: 928 RVA: 0x0000D4C6 File Offset: 0x0000B6C6
		public static IEnumerable<DependentModuleMetadata> DependenciesAllDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesAll().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x0000D4F2 File Offset: 0x0000B6F2
		public static IEnumerable<DependentModuleMetadata> DependenciesAll(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesAll>d__1 <DependenciesAll>d__ = new ModuleInfoExtendedExtensions.<DependenciesAll>d__1(-2);
			<DependenciesAll>d__.<>3__module = module;
			return <DependenciesAll>d__;
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x0000D502 File Offset: 0x0000B702
		public static IEnumerable<DependentModuleMetadata> DependenciesToLoadDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesToLoad().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x0000D52E File Offset: 0x0000B72E
		public static IEnumerable<DependentModuleMetadata> DependenciesToLoad(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesToLoad>d__3 <DependenciesToLoad>d__ = new ModuleInfoExtendedExtensions.<DependenciesToLoad>d__3(-2);
			<DependenciesToLoad>d__.<>3__module = module;
			return <DependenciesToLoad>d__;
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x0000D53E File Offset: 0x0000B73E
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThisDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesLoadBeforeThis().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x0000D56A File Offset: 0x0000B76A
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadBeforeThis(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesLoadBeforeThis>d__5 <DependenciesLoadBeforeThis>d__ = new ModuleInfoExtendedExtensions.<DependenciesLoadBeforeThis>d__5(-2);
			<DependenciesLoadBeforeThis>d__.<>3__module = module;
			return <DependenciesLoadBeforeThis>d__;
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x0000D57A File Offset: 0x0000B77A
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThisDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesLoadAfterThis().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x0000D5A6 File Offset: 0x0000B7A6
		public static IEnumerable<DependentModuleMetadata> DependenciesLoadAfterThis(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesLoadAfterThis>d__7 <DependenciesLoadAfterThis>d__ = new ModuleInfoExtendedExtensions.<DependenciesLoadAfterThis>d__7(-2);
			<DependenciesLoadAfterThis>d__.<>3__module = module;
			return <DependenciesLoadAfterThis>d__;
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x0000D5B6 File Offset: 0x0000B7B6
		public static IEnumerable<DependentModuleMetadata> DependenciesIncompatiblesDistinct(this ModuleInfoExtended module)
		{
			return module.DependenciesIncompatibles().DistinctBy((DependentModuleMetadata x) => x.Id);
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x0000D5E2 File Offset: 0x0000B7E2
		public static IEnumerable<DependentModuleMetadata> DependenciesIncompatibles(this ModuleInfoExtended module)
		{
			ModuleInfoExtendedExtensions.<DependenciesIncompatibles>d__9 <DependenciesIncompatibles>d__ = new ModuleInfoExtendedExtensions.<DependenciesIncompatibles>d__9(-2);
			<DependenciesIncompatibles>d__.<>3__module = module;
			return <DependenciesIncompatibles>d__;
		}
	}
}
