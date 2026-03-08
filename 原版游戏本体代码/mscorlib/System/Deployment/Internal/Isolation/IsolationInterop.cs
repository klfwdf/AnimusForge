using System;
using System.Deployment.Internal.Isolation.Manifest;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006B4 RID: 1716
	internal static class IsolationInterop
	{
		// Token: 0x0600501D RID: 20509 RVA: 0x0011D6D6 File Offset: 0x0011B8D6
		[SecuritySafeCritical]
		public static Store GetUserStore()
		{
			return new Store(IsolationInterop.GetUserStore(0U, IntPtr.Zero, ref IsolationInterop.IID_IStore) as IStore);
		}

		// Token: 0x17000CAD RID: 3245
		// (get) Token: 0x0600501E RID: 20510 RVA: 0x0011D6F4 File Offset: 0x0011B8F4
		public static IIdentityAuthority IdentityAuthority
		{
			[SecuritySafeCritical]
			get
			{
				if (IsolationInterop._idAuth == null)
				{
					object synchObject = IsolationInterop._synchObject;
					lock (synchObject)
					{
						if (IsolationInterop._idAuth == null)
						{
							IsolationInterop._idAuth = IsolationInterop.GetIdentityAuthority();
						}
					}
				}
				return IsolationInterop._idAuth;
			}
		}

		// Token: 0x17000CAE RID: 3246
		// (get) Token: 0x0600501F RID: 20511 RVA: 0x0011D754 File Offset: 0x0011B954
		public static IAppIdAuthority AppIdAuthority
		{
			[SecuritySafeCritical]
			get
			{
				if (IsolationInterop._appIdAuth == null)
				{
					object synchObject = IsolationInterop._synchObject;
					lock (synchObject)
					{
						if (IsolationInterop._appIdAuth == null)
						{
							IsolationInterop._appIdAuth = IsolationInterop.GetAppIdAuthority();
						}
					}
				}
				return IsolationInterop._appIdAuth;
			}
		}

		// Token: 0x06005020 RID: 20512 RVA: 0x0011D7B4 File Offset: 0x0011B9B4
		[SecuritySafeCritical]
		internal static IActContext CreateActContext(IDefinitionAppId AppId)
		{
			IsolationInterop.CreateActContextParameters createActContextParameters;
			createActContextParameters.Size = (uint)Marshal.SizeOf(typeof(IsolationInterop.CreateActContextParameters));
			createActContextParameters.Flags = 16U;
			createActContextParameters.CustomStoreList = IntPtr.Zero;
			createActContextParameters.CultureFallbackList = IntPtr.Zero;
			createActContextParameters.ProcessorArchitectureList = IntPtr.Zero;
			createActContextParameters.Source = IntPtr.Zero;
			createActContextParameters.ProcArch = 0;
			IsolationInterop.CreateActContextParametersSource createActContextParametersSource;
			createActContextParametersSource.Size = (uint)Marshal.SizeOf(typeof(IsolationInterop.CreateActContextParametersSource));
			createActContextParametersSource.Flags = 0U;
			createActContextParametersSource.SourceType = 1U;
			createActContextParametersSource.Data = IntPtr.Zero;
			IsolationInterop.CreateActContextParametersSourceDefinitionAppid createActContextParametersSourceDefinitionAppid;
			createActContextParametersSourceDefinitionAppid.Size = (uint)Marshal.SizeOf(typeof(IsolationInterop.CreateActContextParametersSourceDefinitionAppid));
			createActContextParametersSourceDefinitionAppid.Flags = 0U;
			createActContextParametersSourceDefinitionAppid.AppId = AppId;
			IActContext result;
			try
			{
				createActContextParametersSource.Data = createActContextParametersSourceDefinitionAppid.ToIntPtr();
				createActContextParameters.Source = createActContextParametersSource.ToIntPtr();
				result = IsolationInterop.CreateActContext(ref createActContextParameters) as IActContext;
			}
			finally
			{
				if (createActContextParametersSource.Data != IntPtr.Zero)
				{
					IsolationInterop.CreateActContextParametersSourceDefinitionAppid.Destroy(createActContextParametersSource.Data);
					createActContextParametersSource.Data = IntPtr.Zero;
				}
				if (createActContextParameters.Source != IntPtr.Zero)
				{
					IsolationInterop.CreateActContextParametersSource.Destroy(createActContextParameters.Source);
					createActContextParameters.Source = IntPtr.Zero;
				}
			}
			return result;
		}

		// Token: 0x06005021 RID: 20513
		[DllImport("clr.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		internal static extern object CreateActContext(ref IsolationInterop.CreateActContextParameters Params);

		// Token: 0x06005022 RID: 20514
		[SecurityCritical]
		[DllImport("clr.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		internal static extern object CreateCMSFromXml([In] byte[] buffer, [In] uint bufferSize, [In] IManifestParseErrorCallback Callback, [In] ref Guid riid);

		// Token: 0x06005023 RID: 20515
		[SecurityCritical]
		[DllImport("clr.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		internal static extern object ParseManifest([MarshalAs(UnmanagedType.LPWStr)] [In] string pszManifestPath, [In] IManifestParseErrorCallback pIManifestParseErrorCallback, [In] ref Guid riid);

		// Token: 0x06005024 RID: 20516
		[SecurityCritical]
		[DllImport("clr.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		private static extern object GetUserStore([In] uint Flags, [In] IntPtr hToken, [In] ref Guid riid);

		// Token: 0x06005025 RID: 20517
		[SecurityCritical]
		[DllImport("clr.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		private static extern IIdentityAuthority GetIdentityAuthority();

		// Token: 0x06005026 RID: 20518
		[SecurityCritical]
		[DllImport("clr.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		private static extern IAppIdAuthority GetAppIdAuthority();

		// Token: 0x06005027 RID: 20519 RVA: 0x0011D900 File Offset: 0x0011BB00
		internal static Guid GetGuidOfType(Type type)
		{
			GuidAttribute guidAttribute = (GuidAttribute)Attribute.GetCustomAttribute(type, typeof(GuidAttribute), false);
			return new Guid(guidAttribute.Value);
		}

		// Token: 0x04002274 RID: 8820
		private static object _synchObject = new object();

		// Token: 0x04002275 RID: 8821
		private static volatile IIdentityAuthority _idAuth = null;

		// Token: 0x04002276 RID: 8822
		private static volatile IAppIdAuthority _appIdAuth = null;

		// Token: 0x04002277 RID: 8823
		public const string IsolationDllName = "clr.dll";

		// Token: 0x04002278 RID: 8824
		public static Guid IID_ICMS = IsolationInterop.GetGuidOfType(typeof(ICMS));

		// Token: 0x04002279 RID: 8825
		public static Guid IID_IDefinitionIdentity = IsolationInterop.GetGuidOfType(typeof(IDefinitionIdentity));

		// Token: 0x0400227A RID: 8826
		public static Guid IID_IManifestInformation = IsolationInterop.GetGuidOfType(typeof(IManifestInformation));

		// Token: 0x0400227B RID: 8827
		public static Guid IID_IEnumSTORE_ASSEMBLY = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_ASSEMBLY));

		// Token: 0x0400227C RID: 8828
		public static Guid IID_IEnumSTORE_ASSEMBLY_FILE = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_ASSEMBLY_FILE));

		// Token: 0x0400227D RID: 8829
		public static Guid IID_IEnumSTORE_CATEGORY = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_CATEGORY));

		// Token: 0x0400227E RID: 8830
		public static Guid IID_IEnumSTORE_CATEGORY_INSTANCE = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_CATEGORY_INSTANCE));

		// Token: 0x0400227F RID: 8831
		public static Guid IID_IEnumSTORE_DEPLOYMENT_METADATA = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_DEPLOYMENT_METADATA));

		// Token: 0x04002280 RID: 8832
		public static Guid IID_IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY = IsolationInterop.GetGuidOfType(typeof(IEnumSTORE_DEPLOYMENT_METADATA_PROPERTY));

		// Token: 0x04002281 RID: 8833
		public static Guid IID_IStore = IsolationInterop.GetGuidOfType(typeof(IStore));

		// Token: 0x04002282 RID: 8834
		public static Guid GUID_SXS_INSTALL_REFERENCE_SCHEME_OPAQUESTRING = new Guid("2ec93463-b0c3-45e1-8364-327e96aea856");

		// Token: 0x04002283 RID: 8835
		public static Guid SXS_INSTALL_REFERENCE_SCHEME_SXS_STRONGNAME_SIGNED_PRIVATE_ASSEMBLY = new Guid("3ab20ac0-67e8-4512-8385-a487e35df3da");

		// Token: 0x02000C60 RID: 3168
		internal struct CreateActContextParameters
		{
			// Token: 0x040037B1 RID: 14257
			[MarshalAs(UnmanagedType.U4)]
			public uint Size;

			// Token: 0x040037B2 RID: 14258
			[MarshalAs(UnmanagedType.U4)]
			public uint Flags;

			// Token: 0x040037B3 RID: 14259
			[MarshalAs(UnmanagedType.SysInt)]
			public IntPtr CustomStoreList;

			// Token: 0x040037B4 RID: 14260
			[MarshalAs(UnmanagedType.SysInt)]
			public IntPtr CultureFallbackList;

			// Token: 0x040037B5 RID: 14261
			[MarshalAs(UnmanagedType.SysInt)]
			public IntPtr ProcessorArchitectureList;

			// Token: 0x040037B6 RID: 14262
			[MarshalAs(UnmanagedType.SysInt)]
			public IntPtr Source;

			// Token: 0x040037B7 RID: 14263
			[MarshalAs(UnmanagedType.U2)]
			public ushort ProcArch;

			// Token: 0x02000D11 RID: 3345
			[Flags]
			public enum CreateFlags
			{
				// Token: 0x04003962 RID: 14690
				Nothing = 0,
				// Token: 0x04003963 RID: 14691
				StoreListValid = 1,
				// Token: 0x04003964 RID: 14692
				CultureListValid = 2,
				// Token: 0x04003965 RID: 14693
				ProcessorFallbackListValid = 4,
				// Token: 0x04003966 RID: 14694
				ProcessorValid = 8,
				// Token: 0x04003967 RID: 14695
				SourceValid = 16,
				// Token: 0x04003968 RID: 14696
				IgnoreVisibility = 32
			}
		}

		// Token: 0x02000C61 RID: 3169
		internal struct CreateActContextParametersSource
		{
			// Token: 0x06007078 RID: 28792 RVA: 0x00183550 File Offset: 0x00181750
			[SecurityCritical]
			public IntPtr ToIntPtr()
			{
				IntPtr intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<IsolationInterop.CreateActContextParametersSource>(this));
				Marshal.StructureToPtr<IsolationInterop.CreateActContextParametersSource>(this, intPtr, false);
				return intPtr;
			}

			// Token: 0x06007079 RID: 28793 RVA: 0x0018357C File Offset: 0x0018177C
			[SecurityCritical]
			public static void Destroy(IntPtr p)
			{
				Marshal.DestroyStructure(p, typeof(IsolationInterop.CreateActContextParametersSource));
				Marshal.FreeCoTaskMem(p);
			}

			// Token: 0x040037B8 RID: 14264
			[MarshalAs(UnmanagedType.U4)]
			public uint Size;

			// Token: 0x040037B9 RID: 14265
			[MarshalAs(UnmanagedType.U4)]
			public uint Flags;

			// Token: 0x040037BA RID: 14266
			[MarshalAs(UnmanagedType.U4)]
			public uint SourceType;

			// Token: 0x040037BB RID: 14267
			[MarshalAs(UnmanagedType.SysInt)]
			public IntPtr Data;

			// Token: 0x02000D12 RID: 3346
			[Flags]
			public enum SourceFlags
			{
				// Token: 0x0400396A RID: 14698
				Definition = 1,
				// Token: 0x0400396B RID: 14699
				Reference = 2
			}
		}

		// Token: 0x02000C62 RID: 3170
		internal struct CreateActContextParametersSourceDefinitionAppid
		{
			// Token: 0x0600707A RID: 28794 RVA: 0x00183594 File Offset: 0x00181794
			[SecurityCritical]
			public IntPtr ToIntPtr()
			{
				IntPtr intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<IsolationInterop.CreateActContextParametersSourceDefinitionAppid>(this));
				Marshal.StructureToPtr<IsolationInterop.CreateActContextParametersSourceDefinitionAppid>(this, intPtr, false);
				return intPtr;
			}

			// Token: 0x0600707B RID: 28795 RVA: 0x001835C0 File Offset: 0x001817C0
			[SecurityCritical]
			public static void Destroy(IntPtr p)
			{
				Marshal.DestroyStructure(p, typeof(IsolationInterop.CreateActContextParametersSourceDefinitionAppid));
				Marshal.FreeCoTaskMem(p);
			}

			// Token: 0x040037BC RID: 14268
			[MarshalAs(UnmanagedType.U4)]
			public uint Size;

			// Token: 0x040037BD RID: 14269
			[MarshalAs(UnmanagedType.U4)]
			public uint Flags;

			// Token: 0x040037BE RID: 14270
			public IDefinitionAppId AppId;
		}
	}
}
