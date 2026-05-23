using System;
using System.Deployment.Internal.Isolation.Manifest;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006AE RID: 1710
	internal class Store
	{
		// Token: 0x17000CAB RID: 3243
		// (get) Token: 0x06004FE2 RID: 20450 RVA: 0x0011CDD0 File Offset: 0x0011AFD0
		public IStore InternalStore
		{
			get
			{
				return this._pStore;
			}
		}

		// Token: 0x06004FE3 RID: 20451 RVA: 0x0011CDD8 File Offset: 0x0011AFD8
		public Store(IStore pStore)
		{
			if (pStore == null)
			{
				throw new ArgumentNullException("pStore");
			}
			this._pStore = pStore;
		}

		// Token: 0x06004FE4 RID: 20452 RVA: 0x0011CDF8 File Offset: 0x0011AFF8
		[SecuritySafeCritical]
		public uint[] Transact(StoreTransactionOperation[] operations)
		{
			if (operations == null || operations.Length == 0)
			{
				throw new ArgumentException("operations");
			}
			uint[] array = new uint[operations.Length];
			int[] rgResults = new int[operations.Length];
			this._pStore.Transact(new IntPtr(operations.Length), operations, array, rgResults);
			return array;
		}

		// Token: 0x06004FE5 RID: 20453 RVA: 0x0011CE40 File Offset: 0x0011B040
		[SecuritySafeCritical]
		public IDefinitionIdentity BindReferenceToAssemblyIdentity(uint Flags, IReferenceIdentity ReferenceIdentity, uint cDeploymentsToIgnore, IDefinitionIdentity[] DefinitionIdentity_DeploymentsToIgnore)
		{
			Guid iid_IDefinitionIdentity = IsolationInterop.IID_IDefinitionIdentity;
			object obj = this._pStore.BindReferenceToAssembly(Flags, ReferenceIdentity, cDeploymentsToIgnore, DefinitionIdentity_DeploymentsToIgnore, ref iid_IDefinitionIdentity);
			return (IDefinitionIdentity)obj;
		}

		// Token: 0x06004FE6 RID: 20454 RVA: 0x0011CE6C File Offset: 0x0011B06C
		[SecuritySafeCritical]
		public void CalculateDelimiterOfDeploymentsBasedOnQuota(uint dwFlags, uint cDeployments, IDefinitionAppId[] rgpIDefinitionAppId_Deployments, ref StoreApplicationReference InstallerReference, ulong ulonglongQuota, ref uint Delimiter, ref ulong SizeSharedWithExternalDeployment, ref ulong SizeConsumedByInputDeploymentArray)
		{
			IntPtr zero = IntPtr.Zero;
			this._pStore.CalculateDelimiterOfDeploymentsBasedOnQuota(dwFlags, new IntPtr((long)((ulong)cDeployments)), rgpIDefinitionAppId_Deployments, ref InstallerReference, ulonglongQuota, ref zero, ref SizeSharedWithExternalDeployment, ref SizeConsumedByInputDeploymentArray);
			Delimiter = (uint)zero.ToInt64();
		}

		// Token: 0x06004FE7 RID: 20455 RVA: 0x0011CEA8 File Offset: 0x0011B0A8
		[SecuritySafeCritical]
		public ICMS BindReferenceToAssemblyManifest(uint Flags, IReferenceIdentity ReferenceIdentity, uint cDeploymentsToIgnore, IDefinitionIdentity[] DefinitionIdentity_DeploymentsToIgnore)
		{
			Guid iid_ICMS = IsolationInterop.IID_ICMS;
			object obj = this._pStore.BindReferenceToAssembly(Flags, ReferenceIdentity, cDeploymentsToIgnore, DefinitionIdentity_DeploymentsToIgnore, ref iid_ICMS);
			return (ICMS)obj;
		}

		// Token: 0x06004FE8 RID: 20456 RVA: 0x0011CED4 File Offset: 0x0011B0D4
		[SecuritySafeCritical]
		public ICMS GetAssemblyManifest(uint Flags, IDefinitionIdentity DefinitionIdentity)
		{
			Guid iid_ICMS = IsolationInterop.IID_ICMS;
			object assemblyInformation = this._pStore.GetAssemblyInformation(Flags, DefinitionIdentity, ref iid_ICMS);
			return (ICMS)assemblyInformation;
		}

		// Token: 0x06004FE9 RID: 20457 RVA: 0x0011CF00 File Offset: 0x0011B100
		[SecuritySafeCritical]
		public IDefinitionIdentity GetAssemblyIdentity(uint Flags, IDefinitionIdentity DefinitionIdentity)
		{
			Guid iid_IDefinitionIdentity = IsolationInterop.IID_IDefinitionIdentity;
			object assemblyInformation = this._pStore.GetAssemblyInformation(Flags, DefinitionIdentity, ref iid_IDefinitionIdentity);
			return (IDefinitionIdentity)assemblyInformation;
		}

		// Token: 0x06004FEA RID: 20458 RVA: 0x0011CF29 File Offset: 0x0011B129
		public StoreAssemblyEnumeration EnumAssemblies(Store.EnumAssembliesFlags Flags)
		{
			return this.EnumAssemblies(Flags, null);
		}

		// Token: 0x06004FEB RID: 20459 RVA: 0x0011CF34 File Offset: 0x0011B134
		[SecuritySafeCritical]
		public StoreAssemblyEnumeration EnumAssemblies(Store.EnumAssembliesFlags Flags, IReferenceIdentity refToMatch)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_ASSEMBLY));
			object obj = this._pStore.EnumAssemblies((uint)Flags, refToMatch, ref guidOfType);
			return new StoreAssemblyEnumeration((IEnumSTORE_ASSEMBLY)obj);
		}

		// Token: 0x06004FEC RID: 20460 RVA: 0x0011CF6C File Offset: 0x0011B16C
		[SecuritySafeCritical]
		public StoreAssemblyFileEnumeration EnumFiles(Store.EnumAssemblyFilesFlags Flags, IDefinitionIdentity Assembly)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_ASSEMBLY_FILE));
			object obj = this._pStore.EnumFiles((uint)Flags, Assembly, ref guidOfType);
			return new StoreAssemblyFileEnumeration((IEnumSTORE_ASSEMBLY_FILE)obj);
		}

		// Token: 0x06004FED RID: 20461 RVA: 0x0011CFA4 File Offset: 0x0011B1A4
		[SecuritySafeCritical]
		public StoreAssemblyFileEnumeration EnumPrivateFiles(Store.EnumApplicationPrivateFiles Flags, IDefinitionAppId Application, IDefinitionIdentity Assembly)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_ASSEMBLY_FILE));
			object obj = this._pStore.EnumPrivateFiles((uint)Flags, Application, Assembly, ref guidOfType);
			return new StoreAssemblyFileEnumeration((IEnumSTORE_ASSEMBLY_FILE)obj);
		}

		// Token: 0x06004FEE RID: 20462 RVA: 0x0011CFE0 File Offset: 0x0011B1E0
		[SecuritySafeCritical]
		public IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE EnumInstallationReferences(Store.EnumAssemblyInstallReferenceFlags Flags, IDefinitionIdentity Assembly)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE));
			object obj = this._pStore.EnumInstallationReferences((uint)Flags, Assembly, ref guidOfType);
			return (IEnumSTORE_ASSEMBLY_INSTALLATION_REFERENCE)obj;
		}

		// Token: 0x06004FEF RID: 20463 RVA: 0x0011D014 File Offset: 0x0011B214
		[SecuritySafeCritical]
		public Store.IPathLock LockAssemblyPath(IDefinitionIdentity asm)
		{
			IntPtr c;
			string path = this._pStore.LockAssemblyPath(0U, asm, out c);
			return new Store.AssemblyPathLock(this._pStore, c, path);
		}

		// Token: 0x06004FF0 RID: 20464 RVA: 0x0011D040 File Offset: 0x0011B240
		[SecuritySafeCritical]
		public Store.IPathLock LockApplicationPath(IDefinitionAppId app)
		{
			IntPtr c;
			string path = this._pStore.LockApplicationPath(0U, app, out c);
			return new Store.ApplicationPathLock(this._pStore, c, path);
		}

		// Token: 0x06004FF1 RID: 20465 RVA: 0x0011D06C File Offset: 0x0011B26C
		[SecuritySafeCritical]
		public ulong QueryChangeID(IDefinitionIdentity asm)
		{
			return this._pStore.QueryChangeID(asm);
		}

		// Token: 0x06004FF2 RID: 20466 RVA: 0x0011D088 File Offset: 0x0011B288
		[SecuritySafeCritical]
		public StoreCategoryEnumeration EnumCategories(Store.EnumCategoriesFlags Flags, IReferenceIdentity CategoryMatch)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_CATEGORY));
			object obj = this._pStore.EnumCategories((uint)Flags, CategoryMatch, ref guidOfType);
			return new StoreCategoryEnumeration((IEnumSTORE_CATEGORY)obj);
		}

		// Token: 0x06004FF3 RID: 20467 RVA: 0x0011D0C0 File Offset: 0x0011B2C0
		public StoreSubcategoryEnumeration EnumSubcategories(Store.EnumSubcategoriesFlags Flags, IDefinitionIdentity CategoryMatch)
		{
			return this.EnumSubcategories(Flags, CategoryMatch, null);
		}

		// Token: 0x06004FF4 RID: 20468 RVA: 0x0011D0CC File Offset: 0x0011B2CC
		[SecuritySafeCritical]
		public StoreSubcategoryEnumeration EnumSubcategories(Store.EnumSubcategoriesFlags Flags, IDefinitionIdentity Category, string SearchPattern)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_CATEGORY_SUBCATEGORY));
			object obj = this._pStore.EnumSubcategories((uint)Flags, Category, SearchPattern, ref guidOfType);
			return new StoreSubcategoryEnumeration((IEnumSTORE_CATEGORY_SUBCATEGORY)obj);
		}

		// Token: 0x06004FF5 RID: 20469 RVA: 0x0011D108 File Offset: 0x0011B308
		[SecuritySafeCritical]
		public StoreCategoryInstanceEnumeration EnumCategoryInstances(Store.EnumCategoryInstancesFlags Flags, IDefinitionIdentity Category, string SubCat)
		{
			Guid guidOfType = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_CATEGORY_INSTANCE));
			object obj = this._pStore.EnumCategoryInstances((uint)Flags, Category, SubCat, ref guidOfType);
			return new StoreCategoryInstanceEnumeration((IEnumSTORE_CATEGORY_INSTANCE)obj);
		}

		// Token: 0x06004FF6 RID: 20470 RVA: 0x0011D144 File Offset: 0x0011B344
		[SecurityCritical]
		public byte[] GetDeploymentProperty(Store.GetPackagePropertyFlags Flags, IDefinitionAppId Deployment, StoreApplicationReference Reference, Guid PropertySet, string PropertyName)
		{
			BLOB blob = default(BLOB);
			byte[] array = null;
			try
			{
				this._pStore.GetDeploymentProperty((uint)Flags, Deployment, ref Reference, ref PropertySet, PropertyName, out blob);
				array = new byte[blob.Size];
				Marshal.Copy(blob.BlobData, array, 0, (int)blob.Size);
			}
			finally
			{
				blob.Dispose();
			}
			return array;
		}

		// Token: 0x06004FF7 RID: 20471 RVA: 0x0011D1AC File Offset: 0x0011B3AC
		[SecuritySafeCritical]
		public StoreDeploymentMetadataEnumeration EnumInstallerDeployments(Guid InstallerId, string InstallerName, string InstallerMetadata, IReferenceAppId DeploymentFilter)
		{
			StoreApplicationReference storeApplicationReference = new StoreApplicationReference(InstallerId, InstallerName, InstallerMetadata);
			object obj = this._pStore.EnumInstallerDeploymentMetadata(0U, ref storeApplicationReference, DeploymentFilter, ref IsolationInterop.IID_IEnumSTORE_DEPLOYMENT_METADATA);
			return new StoreDeploymentMetadataEnumeration((IEnumSTORE_DEPLOYMENT_METADATA)obj);
		}

		// Token: 0x06004FF8 RID: 20472 RVA: 0x0011D1E8 File Offset: 0x0011B3E8
		[SecuritySafeCritical]
		public StoreDeploymentMetadataPropertyEnumeration EnumInstallerDeploymentProperties(Guid InstallerId, string InstallerName, string InstallerMetadata, IDefinitionAppId Deployment)
		{
			StoreApplicationReference storeApplicationReference = new StoreApplicationReference(InstallerId, InstallerName, InstallerMetadata);
			object obj = this._pStore.EnumInstallerDeploymentMetadataProperties(0U, ref storeApplicationReference, Deployment, ref IsolationInterop.IID_IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY);
			return new StoreDeploymentMetadataPropertyEnumeration((IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY)obj);
		}

		// Token: 0x04002268 RID: 8808
		private IStore _pStore;

		// Token: 0x02000C55 RID: 3157
		[Flags]
		public enum EnumAssembliesFlags
		{
			// Token: 0x04003795 RID: 14229
			Nothing = 0,
			// Token: 0x04003796 RID: 14230
			VisibleOnly = 1,
			// Token: 0x04003797 RID: 14231
			MatchServicing = 2,
			// Token: 0x04003798 RID: 14232
			ForceLibrarySemantics = 4
		}

		// Token: 0x02000C56 RID: 3158
		[Flags]
		public enum EnumAssemblyFilesFlags
		{
			// Token: 0x0400379A RID: 14234
			Nothing = 0,
			// Token: 0x0400379B RID: 14235
			IncludeInstalled = 1,
			// Token: 0x0400379C RID: 14236
			IncludeMissing = 2
		}

		// Token: 0x02000C57 RID: 3159
		[Flags]
		public enum EnumApplicationPrivateFiles
		{
			// Token: 0x0400379E RID: 14238
			Nothing = 0,
			// Token: 0x0400379F RID: 14239
			IncludeInstalled = 1,
			// Token: 0x040037A0 RID: 14240
			IncludeMissing = 2
		}

		// Token: 0x02000C58 RID: 3160
		[Flags]
		public enum EnumAssemblyInstallReferenceFlags
		{
			// Token: 0x040037A2 RID: 14242
			Nothing = 0
		}

		// Token: 0x02000C59 RID: 3161
		public interface IPathLock : IDisposable
		{
			// Token: 0x1700134F RID: 4943
			// (get) Token: 0x0600706D RID: 28781
			string Path { get; }
		}

		// Token: 0x02000C5A RID: 3162
		private class AssemblyPathLock : Store.IPathLock, IDisposable
		{
			// Token: 0x0600706E RID: 28782 RVA: 0x00183406 File Offset: 0x00181606
			public AssemblyPathLock(IStore s, IntPtr c, string path)
			{
				this._pSourceStore = s;
				this._pLockCookie = c;
				this._path = path;
			}

			// Token: 0x0600706F RID: 28783 RVA: 0x0018342E File Offset: 0x0018162E
			[SecuritySafeCritical]
			private void Dispose(bool fDisposing)
			{
				if (fDisposing)
				{
					GC.SuppressFinalize(this);
				}
				if (this._pLockCookie != IntPtr.Zero)
				{
					this._pSourceStore.ReleaseAssemblyPath(this._pLockCookie);
					this._pLockCookie = IntPtr.Zero;
				}
			}

			// Token: 0x06007070 RID: 28784 RVA: 0x00183468 File Offset: 0x00181668
			~AssemblyPathLock()
			{
				this.Dispose(false);
			}

			// Token: 0x06007071 RID: 28785 RVA: 0x00183498 File Offset: 0x00181698
			void IDisposable.Dispose()
			{
				this.Dispose(true);
			}

			// Token: 0x17001350 RID: 4944
			// (get) Token: 0x06007072 RID: 28786 RVA: 0x001834A1 File Offset: 0x001816A1
			public string Path
			{
				get
				{
					return this._path;
				}
			}

			// Token: 0x040037A3 RID: 14243
			private IStore _pSourceStore;

			// Token: 0x040037A4 RID: 14244
			private IntPtr _pLockCookie = IntPtr.Zero;

			// Token: 0x040037A5 RID: 14245
			private string _path;
		}

		// Token: 0x02000C5B RID: 3163
		private class ApplicationPathLock : Store.IPathLock, IDisposable
		{
			// Token: 0x06007073 RID: 28787 RVA: 0x001834A9 File Offset: 0x001816A9
			public ApplicationPathLock(IStore s, IntPtr c, string path)
			{
				this._pSourceStore = s;
				this._pLockCookie = c;
				this._path = path;
			}

			// Token: 0x06007074 RID: 28788 RVA: 0x001834D1 File Offset: 0x001816D1
			[SecuritySafeCritical]
			private void Dispose(bool fDisposing)
			{
				if (fDisposing)
				{
					GC.SuppressFinalize(this);
				}
				if (this._pLockCookie != IntPtr.Zero)
				{
					this._pSourceStore.ReleaseApplicationPath(this._pLockCookie);
					this._pLockCookie = IntPtr.Zero;
				}
			}

			// Token: 0x06007075 RID: 28789 RVA: 0x0018350C File Offset: 0x0018170C
			~ApplicationPathLock()
			{
				this.Dispose(false);
			}

			// Token: 0x06007076 RID: 28790 RVA: 0x0018353C File Offset: 0x0018173C
			void IDisposable.Dispose()
			{
				this.Dispose(true);
			}

			// Token: 0x17001351 RID: 4945
			// (get) Token: 0x06007077 RID: 28791 RVA: 0x00183545 File Offset: 0x00181745
			public string Path
			{
				get
				{
					return this._path;
				}
			}

			// Token: 0x040037A6 RID: 14246
			private IStore _pSourceStore;

			// Token: 0x040037A7 RID: 14247
			private IntPtr _pLockCookie = IntPtr.Zero;

			// Token: 0x040037A8 RID: 14248
			private string _path;
		}

		// Token: 0x02000C5C RID: 3164
		[Flags]
		public enum EnumCategoriesFlags
		{
			// Token: 0x040037AA RID: 14250
			Nothing = 0
		}

		// Token: 0x02000C5D RID: 3165
		[Flags]
		public enum EnumSubcategoriesFlags
		{
			// Token: 0x040037AC RID: 14252
			Nothing = 0
		}

		// Token: 0x02000C5E RID: 3166
		[Flags]
		public enum EnumCategoryInstancesFlags
		{
			// Token: 0x040037AE RID: 14254
			Nothing = 0
		}

		// Token: 0x02000C5F RID: 3167
		[Flags]
		public enum GetPackagePropertyFlags
		{
			// Token: 0x040037B0 RID: 14256
			Nothing = 0
		}
	}
}
