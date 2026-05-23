using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B1 RID: 1713
	[Guid("a5c62f6d-5e3e-4cd9-b345-6b281d7a1d1e")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IStore
	{
		// Token: 0x06004FF9 RID: 20473
		[SecurityCritical]
		void Transact([In] IntPtr cOperation, [MarshalAs(UnmanagedType.LPArray)] [In] StoreTransactionOperation[] rgOperations, [MarshalAs(UnmanagedType.LPArray)] [Out] uint[] rgDispositions, [MarshalAs(UnmanagedType.LPArray)] [Out] int[] rgResults);

		// Token: 0x06004FFA RID: 20474
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object BindReferenceToAssembly([In] uint Flags, [In] IReferenceIdentity ReferenceIdentity, [In] uint cDeploymentsToIgnore, [MarshalAs(UnmanagedType.LPArray)] [In] IDefinitionIdentity[] DefinitionIdentity_DeploymentsToIgnore, [In] ref Guid riid);

		// Token: 0x06004FFB RID: 20475
		[SecurityCritical]
		void CalculateDelimiterOfDeploymentsBasedOnQuota([In] uint dwFlags, [In] IntPtr cDeployments, [MarshalAs(UnmanagedType.LPArray)] [In] IDefinitionAppId[] rgpIDefinitionAppId_Deployments, [In] ref StoreApplicationReference InstallerReference, [In] ulong ulonglongQuota, [In] [Out] ref IntPtr Delimiter, [In] [Out] ref ulong SizeSharedWithExternalDeployment, [In] [Out] ref ulong SizeConsumedByInputDeploymentArray);

		// Token: 0x06004FFC RID: 20476
		[SecurityCritical]
		IntPtr BindDefinitions([In] uint Flags, [MarshalAs(UnmanagedType.SysInt)] [In] IntPtr Count, [MarshalAs(UnmanagedType.LPArray)] [In] IDefinitionIdentity[] DefsToBind, [In] uint DeploymentsToIgnore, [MarshalAs(UnmanagedType.LPArray)] [In] IDefinitionIdentity[] DefsToIgnore);

		// Token: 0x06004FFD RID: 20477
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetAssemblyInformation([In] uint Flags, [In] IDefinitionIdentity DefinitionIdentity, [In] ref Guid riid);

		// Token: 0x06004FFE RID: 20478
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumAssemblies([In] uint Flags, [In] IReferenceIdentity ReferenceIdentity_ToMatch, [In] ref Guid riid);

		// Token: 0x06004FFF RID: 20479
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumFiles([In] uint Flags, [In] IDefinitionIdentity DefinitionIdentity, [In] ref Guid riid);

		// Token: 0x06005000 RID: 20480
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumInstallationReferences([In] uint Flags, [In] IDefinitionIdentity DefinitionIdentity, [In] ref Guid riid);

		// Token: 0x06005001 RID: 20481
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string LockAssemblyPath([In] uint Flags, [In] IDefinitionIdentity DefinitionIdentity, out IntPtr Cookie);

		// Token: 0x06005002 RID: 20482
		[SecurityCritical]
		void ReleaseAssemblyPath([In] IntPtr Cookie);

		// Token: 0x06005003 RID: 20483
		[SecurityCritical]
		ulong QueryChangeID([In] IDefinitionIdentity DefinitionIdentity);

		// Token: 0x06005004 RID: 20484
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumCategories([In] uint Flags, [In] IReferenceIdentity ReferenceIdentity_ToMatch, [In] ref Guid riid);

		// Token: 0x06005005 RID: 20485
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumSubcategories([In] uint Flags, [In] IDefinitionIdentity CategoryId, [MarshalAs(UnmanagedType.LPWStr)] [In] string SubcategoryPathPattern, [In] ref Guid riid);

		// Token: 0x06005006 RID: 20486
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumCategoryInstances([In] uint Flags, [In] IDefinitionIdentity CategoryId, [MarshalAs(UnmanagedType.LPWStr)] [In] string SubcategoryPath, [In] ref Guid riid);

		// Token: 0x06005007 RID: 20487
		[SecurityCritical]
		void GetDeploymentProperty([In] uint Flags, [In] IDefinitionAppId DeploymentInPackage, [In] ref StoreApplicationReference Reference, [In] ref Guid PropertySet, [MarshalAs(UnmanagedType.LPWStr)] [In] string pcwszPropertyName, out BLOB blob);

		// Token: 0x06005008 RID: 20488
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string LockApplicationPath([In] uint Flags, [In] IDefinitionAppId ApId, out IntPtr Cookie);

		// Token: 0x06005009 RID: 20489
		[SecurityCritical]
		void ReleaseApplicationPath([In] IntPtr Cookie);

		// Token: 0x0600500A RID: 20490
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumPrivateFiles([In] uint Flags, [In] IDefinitionAppId Application, [In] IDefinitionIdentity DefinitionIdentity, [In] ref Guid riid);

		// Token: 0x0600500B RID: 20491
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumInstallerDeploymentMetadata([In] uint Flags, [In] ref StoreApplicationReference Reference, [In] IReferenceAppId Filter, [In] ref Guid riid);

		// Token: 0x0600500C RID: 20492
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object EnumInstallerDeploymentMetadataProperties([In] uint Flags, [In] ref StoreApplicationReference Reference, [In] IDefinitionAppId Filter, [In] ref Guid riid);
	}
}
