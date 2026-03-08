using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Deployment.Internal.Isolation.Manifest;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Security.Principal;
using System.Security.Util;
using System.Text;
using System.Threading;

namespace System
{
	// Token: 0x02000097 RID: 151
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_AppDomain))]
	[ComVisible(true)]
	public sealed class AppDomain : MarshalByRefObject, _AppDomain, IEvidenceFactory
	{
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000786 RID: 1926 RVA: 0x0001A224 File Offset: 0x00018424
		// (remove) Token: 0x06000787 RID: 1927 RVA: 0x0001A25C File Offset: 0x0001845C
		[method: SecurityCritical]
		public event AssemblyLoadEventHandler AssemblyLoad;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000788 RID: 1928 RVA: 0x0001A294 File Offset: 0x00018494
		// (remove) Token: 0x06000789 RID: 1929 RVA: 0x0001A2E0 File Offset: 0x000184E0
		public event ResolveEventHandler TypeResolve
		{
			[SecurityCritical]
			add
			{
				lock (this)
				{
					this._TypeResolve = (ResolveEventHandler)Delegate.Combine(this._TypeResolve, value);
				}
			}
			[SecurityCritical]
			remove
			{
				lock (this)
				{
					this._TypeResolve = (ResolveEventHandler)Delegate.Remove(this._TypeResolve, value);
				}
			}
		}

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x0600078A RID: 1930 RVA: 0x0001A32C File Offset: 0x0001852C
		// (remove) Token: 0x0600078B RID: 1931 RVA: 0x0001A378 File Offset: 0x00018578
		public event ResolveEventHandler ResourceResolve
		{
			[SecurityCritical]
			add
			{
				lock (this)
				{
					this._ResourceResolve = (ResolveEventHandler)Delegate.Combine(this._ResourceResolve, value);
				}
			}
			[SecurityCritical]
			remove
			{
				lock (this)
				{
					this._ResourceResolve = (ResolveEventHandler)Delegate.Remove(this._ResourceResolve, value);
				}
			}
		}

		// Token: 0x14000005 RID: 5
		// (add) Token: 0x0600078C RID: 1932 RVA: 0x0001A3C4 File Offset: 0x000185C4
		// (remove) Token: 0x0600078D RID: 1933 RVA: 0x0001A410 File Offset: 0x00018610
		public event ResolveEventHandler AssemblyResolve
		{
			[SecurityCritical]
			add
			{
				lock (this)
				{
					this._AssemblyResolve = (ResolveEventHandler)Delegate.Combine(this._AssemblyResolve, value);
				}
			}
			[SecurityCritical]
			remove
			{
				lock (this)
				{
					this._AssemblyResolve = (ResolveEventHandler)Delegate.Remove(this._AssemblyResolve, value);
				}
			}
		}

		// Token: 0x14000006 RID: 6
		// (add) Token: 0x0600078E RID: 1934 RVA: 0x0001A45C File Offset: 0x0001865C
		// (remove) Token: 0x0600078F RID: 1935 RVA: 0x0001A494 File Offset: 0x00018694
		[method: SecurityCritical]
		public event ResolveEventHandler ReflectionOnlyAssemblyResolve;

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x06000790 RID: 1936 RVA: 0x0001A4C9 File Offset: 0x000186C9
		private static AppDomain.APPX_FLAGS Flags
		{
			[SecuritySafeCritical]
			get
			{
				if (AppDomain.s_flags == (AppDomain.APPX_FLAGS)0)
				{
					AppDomain.s_flags = AppDomain.nGetAppXFlags();
				}
				return AppDomain.s_flags;
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x06000791 RID: 1937 RVA: 0x0001A4E1 File Offset: 0x000186E1
		internal static bool ProfileAPICheck
		{
			[SecuritySafeCritical]
			get
			{
				return (AppDomain.Flags & AppDomain.APPX_FLAGS.APPX_FLAGS_API_CHECK) > (AppDomain.APPX_FLAGS)0;
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x06000792 RID: 1938 RVA: 0x0001A4EE File Offset: 0x000186EE
		internal static bool IsAppXNGen
		{
			[SecuritySafeCritical]
			get
			{
				return (AppDomain.Flags & AppDomain.APPX_FLAGS.APPX_FLAGS_APPX_NGEN) > (AppDomain.APPX_FLAGS)0;
			}
		}

		// Token: 0x06000793 RID: 1939
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DisableFusionUpdatesFromADManager(AppDomainHandle domain);

		// Token: 0x06000794 RID: 1940
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern AppDomain.APPX_FLAGS nGetAppXFlags();

		// Token: 0x06000795 RID: 1941
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetAppDomainManagerType(AppDomainHandle domain, StringHandleOnStack retAssembly, StringHandleOnStack retType);

		// Token: 0x06000796 RID: 1942
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SetAppDomainManagerType(AppDomainHandle domain, string assembly, string type);

		// Token: 0x06000797 RID: 1943
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nSetHostSecurityManagerFlags(HostSecurityManagerOptions flags);

		// Token: 0x06000798 RID: 1944
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SetSecurityHomogeneousFlag(AppDomainHandle domain, [MarshalAs(UnmanagedType.Bool)] bool runtimeSuppliedHomogenousGrantSet);

		// Token: 0x06000799 RID: 1945
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SetLegacyCasPolicyEnabled(AppDomainHandle domain);

		// Token: 0x0600079A RID: 1946 RVA: 0x0001A4FA File Offset: 0x000186FA
		[SecurityCritical]
		private void SetLegacyCasPolicyEnabled()
		{
			AppDomain.SetLegacyCasPolicyEnabled(this.GetNativeHandle());
		}

		// Token: 0x0600079B RID: 1947 RVA: 0x0001A507 File Offset: 0x00018707
		internal AppDomainHandle GetNativeHandle()
		{
			if (this._pDomain.IsNull())
			{
				throw new InvalidOperationException(Environment.GetResourceString("Argument_InvalidHandle"));
			}
			return new AppDomainHandle(this._pDomain);
		}

		// Token: 0x0600079C RID: 1948 RVA: 0x0001A534 File Offset: 0x00018734
		[SecuritySafeCritical]
		private void CreateAppDomainManager()
		{
			AppDomainSetup fusionStore = this.FusionStore;
			string text;
			string text2;
			this.GetAppDomainManagerType(out text, out text2);
			if (text != null && text2 != null)
			{
				try
				{
					new PermissionSet(PermissionState.Unrestricted).Assert();
					this._domainManager = this.CreateInstanceAndUnwrap(text, text2) as AppDomainManager;
					CodeAccessPermission.RevertAssert();
				}
				catch (FileNotFoundException inner)
				{
					throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), inner);
				}
				catch (SecurityException inner2)
				{
					throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), inner2);
				}
				catch (TypeLoadException inner3)
				{
					throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), inner3);
				}
				if (this._domainManager == null)
				{
					throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"));
				}
				this.FusionStore.AppDomainManagerAssembly = text;
				this.FusionStore.AppDomainManagerType = text2;
				bool flag = this._domainManager.GetType() != typeof(AppDomainManager) && !this.DisableFusionUpdatesFromADManager();
				AppDomainSetup oldInfo = null;
				if (flag)
				{
					oldInfo = new AppDomainSetup(this.FusionStore, true);
				}
				this._domainManager.InitializeNewDomain(this.FusionStore);
				if (flag)
				{
					this.SetupFusionStore(this._FusionStore, oldInfo);
				}
				AppDomainManagerInitializationOptions initializationFlags = this._domainManager.InitializationFlags;
				if ((initializationFlags & AppDomainManagerInitializationOptions.RegisterWithHost) == AppDomainManagerInitializationOptions.RegisterWithHost)
				{
					this._domainManager.RegisterWithHost();
				}
			}
			this.InitializeCompatibilityFlags();
		}

		// Token: 0x0600079D RID: 1949 RVA: 0x0001A69C File Offset: 0x0001889C
		private void InitializeCompatibilityFlags()
		{
			AppDomainSetup fusionStore = this.FusionStore;
			if (fusionStore.GetCompatibilityFlags() != null)
			{
				this._compatFlags = new Dictionary<string, object>(fusionStore.GetCompatibilityFlags(), StringComparer.OrdinalIgnoreCase);
			}
			this._compatFlagsInitialized = true;
			CompatibilitySwitches.InitializeSwitches();
		}

		// Token: 0x0600079E RID: 1950 RVA: 0x0001A6DC File Offset: 0x000188DC
		[SecuritySafeCritical]
		internal string GetTargetFrameworkName()
		{
			string text = this._FusionStore.TargetFrameworkName;
			if (text == null && this.IsDefaultAppDomain() && !this._FusionStore.CheckedForTargetFrameworkName)
			{
				Assembly entryAssembly = Assembly.GetEntryAssembly();
				if (entryAssembly != null)
				{
					TargetFrameworkAttribute[] array = (TargetFrameworkAttribute[])entryAssembly.GetCustomAttributes(typeof(TargetFrameworkAttribute));
					if (array != null && array.Length != 0)
					{
						text = array[0].FrameworkName;
						this._FusionStore.TargetFrameworkName = text;
					}
				}
				this._FusionStore.CheckedForTargetFrameworkName = true;
			}
			return text;
		}

		// Token: 0x0600079F RID: 1951 RVA: 0x0001A75B File Offset: 0x0001895B
		[SecuritySafeCritical]
		private void SetTargetFrameworkName(string targetFrameworkName)
		{
			if (!this._FusionStore.CheckedForTargetFrameworkName)
			{
				this._FusionStore.TargetFrameworkName = targetFrameworkName;
				this._FusionStore.CheckedForTargetFrameworkName = true;
			}
		}

		// Token: 0x060007A0 RID: 1952 RVA: 0x0001A782 File Offset: 0x00018982
		[SecuritySafeCritical]
		internal bool DisableFusionUpdatesFromADManager()
		{
			return AppDomain.DisableFusionUpdatesFromADManager(this.GetNativeHandle());
		}

		// Token: 0x060007A1 RID: 1953 RVA: 0x0001A78F File Offset: 0x0001898F
		[SecuritySafeCritical]
		internal static bool IsAppXModel()
		{
			return (AppDomain.Flags & AppDomain.APPX_FLAGS.APPX_FLAGS_APPX_MODEL) > (AppDomain.APPX_FLAGS)0;
		}

		// Token: 0x060007A2 RID: 1954 RVA: 0x0001A79B File Offset: 0x0001899B
		[SecuritySafeCritical]
		internal static bool IsAppXDesignMode()
		{
			return (AppDomain.Flags & AppDomain.APPX_FLAGS.APPX_FLAGS_APPX_MASK) == (AppDomain.APPX_FLAGS.APPX_FLAGS_APPX_MODEL | AppDomain.APPX_FLAGS.APPX_FLAGS_APPX_DESIGN_MODE);
		}

		// Token: 0x060007A3 RID: 1955 RVA: 0x0001A7A8 File Offset: 0x000189A8
		[SecuritySafeCritical]
		internal static void CheckLoadFromSupported()
		{
			if (AppDomain.IsAppXModel())
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "Assembly.LoadFrom" }));
			}
		}

		// Token: 0x060007A4 RID: 1956 RVA: 0x0001A7CF File Offset: 0x000189CF
		[SecuritySafeCritical]
		internal static void CheckLoadFileSupported()
		{
			if (AppDomain.IsAppXModel())
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "Assembly.LoadFile" }));
			}
		}

		// Token: 0x060007A5 RID: 1957 RVA: 0x0001A7F6 File Offset: 0x000189F6
		[SecuritySafeCritical]
		internal static void CheckReflectionOnlyLoadSupported()
		{
			if (AppDomain.IsAppXModel())
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "Assembly.ReflectionOnlyLoad" }));
			}
		}

		// Token: 0x060007A6 RID: 1958 RVA: 0x0001A820 File Offset: 0x00018A20
		[SecuritySafeCritical]
		internal static void CheckLoadWithPartialNameSupported(StackCrawlMark stackMark)
		{
			if (AppDomain.IsAppXModel())
			{
				RuntimeAssembly executingAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
				if (!(executingAssembly != null) || !executingAssembly.IsFrameworkAssembly())
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "Assembly.LoadWithPartialName" }));
				}
			}
		}

		// Token: 0x060007A7 RID: 1959 RVA: 0x0001A870 File Offset: 0x00018A70
		[SecuritySafeCritical]
		internal static void CheckDefinePInvokeSupported()
		{
			if (AppDomain.IsAppXModel())
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "DefinePInvokeMethod" }));
			}
		}

		// Token: 0x060007A8 RID: 1960 RVA: 0x0001A897 File Offset: 0x00018A97
		[SecuritySafeCritical]
		internal static void CheckLoadByteArraySupported()
		{
			if (AppDomain.IsAppXModel())
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "Assembly.Load(byte[], ...)" }));
			}
		}

		// Token: 0x060007A9 RID: 1961 RVA: 0x0001A8BE File Offset: 0x00018ABE
		[SecuritySafeCritical]
		internal static void CheckCreateDomainSupported()
		{
			if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", new object[] { "AppDomain.CreateDomain" }));
			}
		}

		// Token: 0x060007AA RID: 1962 RVA: 0x0001A8EC File Offset: 0x00018AEC
		[SecuritySafeCritical]
		internal void GetAppDomainManagerType(out string assembly, out string type)
		{
			string text = null;
			string text2 = null;
			AppDomain.GetAppDomainManagerType(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref text), JitHelpers.GetStringHandleOnStack(ref text2));
			assembly = text;
			type = text2;
		}

		// Token: 0x060007AB RID: 1963 RVA: 0x0001A91C File Offset: 0x00018B1C
		[SecuritySafeCritical]
		private void SetAppDomainManagerType(string assembly, string type)
		{
			AppDomain.SetAppDomainManagerType(this.GetNativeHandle(), assembly, type);
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x060007AC RID: 1964 RVA: 0x0001A92B File Offset: 0x00018B2B
		// (set) Token: 0x060007AD RID: 1965 RVA: 0x0001A934 File Offset: 0x00018B34
		internal string[] PartialTrustVisibleAssemblies
		{
			get
			{
				return this._aptcaVisibleAssemblies;
			}
			[SecuritySafeCritical]
			set
			{
				this._aptcaVisibleAssemblies = value;
				string canonicalConditionalAptcaList = null;
				if (value != null)
				{
					StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
					for (int i = 0; i < value.Length; i++)
					{
						if (value[i] != null)
						{
							stringBuilder.Append(value[i].ToUpperInvariant());
							if (i != value.Length - 1)
							{
								stringBuilder.Append(';');
							}
						}
					}
					canonicalConditionalAptcaList = StringBuilderCache.GetStringAndRelease(stringBuilder);
				}
				this.SetCanonicalConditionalAptcaList(canonicalConditionalAptcaList);
			}
		}

		// Token: 0x060007AE RID: 1966
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SetCanonicalConditionalAptcaList(AppDomainHandle appDomain, string canonicalList);

		// Token: 0x060007AF RID: 1967 RVA: 0x0001A996 File Offset: 0x00018B96
		[SecurityCritical]
		private void SetCanonicalConditionalAptcaList(string canonicalList)
		{
			AppDomain.SetCanonicalConditionalAptcaList(this.GetNativeHandle(), canonicalList);
		}

		// Token: 0x060007B0 RID: 1968 RVA: 0x0001A9A4 File Offset: 0x00018BA4
		private void SetupDefaultClickOnceDomain(string fullName, string[] manifestPaths, string[] activationData)
		{
			this.FusionStore.ActivationArguments = new ActivationArguments(fullName, manifestPaths, activationData);
		}

		// Token: 0x060007B1 RID: 1969 RVA: 0x0001A9BC File Offset: 0x00018BBC
		[SecurityCritical]
		private void InitializeDomainSecurity(Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, bool generateDefaultEvidence, IntPtr parentSecurityDescriptor, bool publishAppDomain)
		{
			AppDomainSetup fusionStore = this.FusionStore;
			if (CompatibilitySwitches.IsNetFx40LegacySecurityPolicy)
			{
				this.SetLegacyCasPolicyEnabled();
			}
			if (fusionStore.ActivationArguments != null)
			{
				ActivationContext activationContext = null;
				ApplicationIdentity applicationIdentity = null;
				CmsUtils.CreateActivationContext(fusionStore.ActivationArguments.ApplicationFullName, fusionStore.ActivationArguments.ApplicationManifestPaths, fusionStore.ActivationArguments.UseFusionActivationContext, out applicationIdentity, out activationContext);
				string[] activationData = fusionStore.ActivationArguments.ActivationData;
				providedSecurityInfo = CmsUtils.MergeApplicationEvidence(providedSecurityInfo, applicationIdentity, activationContext, activationData, fusionStore.ApplicationTrust);
				this.SetupApplicationHelper(providedSecurityInfo, creatorsSecurityInfo, applicationIdentity, activationContext, activationData);
			}
			else
			{
				bool runtimeSuppliedHomogenousGrantSet = false;
				ApplicationTrust applicationTrust = fusionStore.ApplicationTrust;
				if (applicationTrust == null && !this.IsLegacyCasPolicyEnabled)
				{
					this._IsFastFullTrustDomain = true;
					runtimeSuppliedHomogenousGrantSet = true;
				}
				if (applicationTrust != null)
				{
					this.SetupDomainSecurityForHomogeneousDomain(applicationTrust, runtimeSuppliedHomogenousGrantSet);
				}
				else if (this._IsFastFullTrustDomain)
				{
					AppDomain.SetSecurityHomogeneousFlag(this.GetNativeHandle(), runtimeSuppliedHomogenousGrantSet);
				}
			}
			Evidence evidence = ((providedSecurityInfo != null) ? providedSecurityInfo : creatorsSecurityInfo);
			if (evidence == null && generateDefaultEvidence)
			{
				evidence = new Evidence(new AppDomainEvidenceFactory(this));
			}
			if (this._domainManager != null)
			{
				HostSecurityManager hostSecurityManager = this._domainManager.HostSecurityManager;
				if (hostSecurityManager != null)
				{
					AppDomain.nSetHostSecurityManagerFlags(hostSecurityManager.Flags);
					if ((hostSecurityManager.Flags & HostSecurityManagerOptions.HostAppDomainEvidence) == HostSecurityManagerOptions.HostAppDomainEvidence)
					{
						evidence = hostSecurityManager.ProvideAppDomainEvidence(evidence);
						if (evidence != null && evidence.Target == null)
						{
							evidence.Target = new AppDomainEvidenceFactory(this);
						}
					}
				}
			}
			this._SecurityIdentity = evidence;
			this.SetupDomainSecurity(evidence, parentSecurityDescriptor, publishAppDomain);
			if (this._domainManager != null)
			{
				this.RunDomainManagerPostInitialization(this._domainManager);
			}
			AppDomain.InitState.RecordEndOfEarlyAppDomainInit();
		}

		// Token: 0x060007B2 RID: 1970 RVA: 0x0001AB24 File Offset: 0x00018D24
		[SecurityCritical]
		private void RunDomainManagerPostInitialization(AppDomainManager domainManager)
		{
			HostExecutionContextManager hostExecutionContextManager = domainManager.HostExecutionContextManager;
			if (this.IsLegacyCasPolicyEnabled)
			{
				HostSecurityManager hostSecurityManager = domainManager.HostSecurityManager;
				if (hostSecurityManager != null && (hostSecurityManager.Flags & HostSecurityManagerOptions.HostPolicyLevel) == HostSecurityManagerOptions.HostPolicyLevel)
				{
					PolicyLevel domainPolicy = hostSecurityManager.DomainPolicy;
					if (domainPolicy != null)
					{
						this.SetAppDomainPolicy(domainPolicy);
					}
				}
			}
		}

		// Token: 0x060007B3 RID: 1971 RVA: 0x0001AB68 File Offset: 0x00018D68
		[SecurityCritical]
		private void SetupApplicationHelper(Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, ApplicationIdentity appIdentity, ActivationContext activationContext, string[] activationData)
		{
			HostSecurityManager hostSecurityManager = AppDomain.CurrentDomain.HostSecurityManager;
			ApplicationTrust applicationTrust = hostSecurityManager.DetermineApplicationTrust(providedSecurityInfo, creatorsSecurityInfo, new TrustManagerContext());
			if (applicationTrust == null || !applicationTrust.IsApplicationTrustedToRun)
			{
				throw new PolicyException(Environment.GetResourceString("Policy_NoExecutionPermission"), -2146233320, null);
			}
			if (activationContext != null)
			{
				this.SetupDomainForApplication(activationContext, activationData);
			}
			this.SetupDomainSecurityForApplication(appIdentity, applicationTrust);
		}

		// Token: 0x060007B4 RID: 1972 RVA: 0x0001ABC8 File Offset: 0x00018DC8
		[SecurityCritical]
		private void SetupDomainForApplication(ActivationContext activationContext, string[] activationData)
		{
			if (this.IsDefaultAppDomain())
			{
				AppDomainSetup fusionStore = this.FusionStore;
				fusionStore.ActivationArguments = new ActivationArguments(activationContext, activationData);
				string entryPointFullPath = CmsUtils.GetEntryPointFullPath(activationContext);
				if (!string.IsNullOrEmpty(entryPointFullPath))
				{
					fusionStore.SetupDefaults(entryPointFullPath, false);
				}
				else
				{
					fusionStore.ApplicationBase = activationContext.ApplicationDirectory;
				}
				this.SetupFusionStore(fusionStore, null);
			}
			activationContext.PrepareForExecution();
			activationContext.SetApplicationState(ActivationContext.ApplicationState.Starting);
			activationContext.SetApplicationState(ActivationContext.ApplicationState.Running);
			IPermission permission = null;
			string dataDirectory = activationContext.DataDirectory;
			if (dataDirectory != null && dataDirectory.Length > 0)
			{
				permission = new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dataDirectory);
			}
			this.SetData("DataDirectory", dataDirectory, permission);
			this._activationContext = activationContext;
		}

		// Token: 0x060007B5 RID: 1973 RVA: 0x0001AC65 File Offset: 0x00018E65
		[SecurityCritical]
		private void SetupDomainSecurityForApplication(ApplicationIdentity appIdentity, ApplicationTrust appTrust)
		{
			this._applicationIdentity = appIdentity;
			this.SetupDomainSecurityForHomogeneousDomain(appTrust, false);
		}

		// Token: 0x060007B6 RID: 1974 RVA: 0x0001AC76 File Offset: 0x00018E76
		[SecurityCritical]
		private void SetupDomainSecurityForHomogeneousDomain(ApplicationTrust appTrust, bool runtimeSuppliedHomogenousGrantSet)
		{
			if (runtimeSuppliedHomogenousGrantSet)
			{
				this._FusionStore.ApplicationTrust = null;
			}
			this._applicationTrust = appTrust;
			AppDomain.SetSecurityHomogeneousFlag(this.GetNativeHandle(), runtimeSuppliedHomogenousGrantSet);
		}

		// Token: 0x060007B7 RID: 1975 RVA: 0x0001AC9C File Offset: 0x00018E9C
		[SecuritySafeCritical]
		private int ActivateApplication()
		{
			ObjectHandle objectHandle = Activator.CreateInstance(AppDomain.CurrentDomain.ActivationContext);
			return (int)objectHandle.Unwrap();
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x060007B8 RID: 1976 RVA: 0x0001ACC4 File Offset: 0x00018EC4
		public AppDomainManager DomainManager
		{
			[SecurityCritical]
			get
			{
				return this._domainManager;
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x060007B9 RID: 1977 RVA: 0x0001ACCC File Offset: 0x00018ECC
		internal HostSecurityManager HostSecurityManager
		{
			[SecurityCritical]
			get
			{
				HostSecurityManager hostSecurityManager = null;
				AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
				if (domainManager != null)
				{
					hostSecurityManager = domainManager.HostSecurityManager;
				}
				if (hostSecurityManager == null)
				{
					hostSecurityManager = new HostSecurityManager();
				}
				return hostSecurityManager;
			}
		}

		// Token: 0x060007BA RID: 1978 RVA: 0x0001ACFA File Offset: 0x00018EFA
		private Assembly ResolveAssemblyForIntrospection(object sender, ResolveEventArgs args)
		{
			return Assembly.ReflectionOnlyLoad(this.ApplyPolicy(args.Name));
		}

		// Token: 0x060007BB RID: 1979 RVA: 0x0001AD10 File Offset: 0x00018F10
		[SecuritySafeCritical]
		private void EnableResolveAssembliesForIntrospection(string verifiedFileDirectory)
		{
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += this.ResolveAssemblyForIntrospection;
			string[] packageGraphFilePaths = null;
			if (verifiedFileDirectory != null)
			{
				packageGraphFilePaths = new string[] { verifiedFileDirectory };
			}
			AppDomain.NamespaceResolverForIntrospection @object = new AppDomain.NamespaceResolverForIntrospection(packageGraphFilePaths);
			WindowsRuntimeMetadata.ReflectionOnlyNamespaceResolve += @object.ResolveNamespace;
		}

		// Token: 0x060007BC RID: 1980 RVA: 0x0001AD5C File Offset: 0x00018F5C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007BD RID: 1981 RVA: 0x0001AD7C File Offset: 0x00018F7C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackCrawlMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007BE RID: 1982 RVA: 0x0001AD9C File Offset: 0x00018F9C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes, SecurityContextSource securityContextSource)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackCrawlMark, assemblyAttributes, securityContextSource);
		}

		// Token: 0x060007BF RID: 1983 RVA: 0x0001ADC0 File Offset: 0x00018FC0
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, null, null, null, null, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C0 RID: 1984 RVA: 0x0001ADE0 File Offset: 0x00018FE0
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.  See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, Evidence evidence)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, null, evidence, null, null, null, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C1 RID: 1985 RVA: 0x0001AE00 File Offset: 0x00019000
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.  See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, null, null, requiredPermissions, optionalPermissions, refusedPermissions, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C2 RID: 1986 RVA: 0x0001AE24 File Offset: 0x00019024
		[SecuritySafeCritical]
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of DefineDynamicAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkId=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, evidence, null, null, null, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C3 RID: 1987 RVA: 0x0001AE48 File Offset: 0x00019048
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, null, requiredPermissions, optionalPermissions, refusedPermissions, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C4 RID: 1988 RVA: 0x0001AE6C File Offset: 0x0001906C
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, null, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C5 RID: 1989 RVA: 0x0001AE90 File Offset: 0x00019090
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.  Please see http://go.microsoft.com/fwlink/?LinkId=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C6 RID: 1990 RVA: 0x0001AEB4 File Offset: 0x000190B4
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, bool isSynchronized)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackCrawlMark, null, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C7 RID: 1991 RVA: 0x0001AED8 File Offset: 0x000190D8
		[SecuritySafeCritical]
		[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, bool isSynchronized, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackCrawlMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C8 RID: 1992 RVA: 0x0001AF00 File Offset: 0x00019100
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, bool isSynchronized, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalDefineDynamicAssembly(name, access, dir, null, null, null, null, ref stackCrawlMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007C9 RID: 1993 RVA: 0x0001AF24 File Offset: 0x00019124
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private AssemblyBuilder InternalDefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, ref StackCrawlMark stackMark, IEnumerable<CustomAttributeBuilder> assemblyAttributes, SecurityContextSource securityContextSource)
		{
			return AssemblyBuilder.InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, assemblyAttributes, securityContextSource);
		}

		// Token: 0x060007CA RID: 1994
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern string nApplyPolicy(AssemblyName an);

		// Token: 0x060007CB RID: 1995 RVA: 0x0001AF48 File Offset: 0x00019148
		[ComVisible(false)]
		public string ApplyPolicy(string assemblyName)
		{
			AssemblyName assemblyName2 = new AssemblyName(assemblyName);
			byte[] array = assemblyName2.GetPublicKeyToken();
			if (array == null)
			{
				array = assemblyName2.GetPublicKey();
			}
			if (array == null || array.Length == 0)
			{
				return assemblyName;
			}
			return this.nApplyPolicy(assemblyName2);
		}

		// Token: 0x060007CC RID: 1996 RVA: 0x0001AF7D File Offset: 0x0001917D
		public ObjectHandle CreateInstance(string assemblyName, string typeName)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			return Activator.CreateInstance(assemblyName, typeName);
		}

		// Token: 0x060007CD RID: 1997 RVA: 0x0001AF9D File Offset: 0x0001919D
		[SecurityCritical]
		internal ObjectHandle InternalCreateInstanceWithNoSecurity(string assemblyName, string typeName)
		{
			PermissionSet.s_fullTrust.Assert();
			return this.CreateInstance(assemblyName, typeName);
		}

		// Token: 0x060007CE RID: 1998 RVA: 0x0001AFB1 File Offset: 0x000191B1
		public ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			return Activator.CreateInstanceFrom(assemblyFile, typeName);
		}

		// Token: 0x060007CF RID: 1999 RVA: 0x0001AFC3 File Offset: 0x000191C3
		[SecurityCritical]
		internal ObjectHandle InternalCreateInstanceFromWithNoSecurity(string assemblyName, string typeName)
		{
			PermissionSet.s_fullTrust.Assert();
			return this.CreateInstanceFrom(assemblyName, typeName);
		}

		// Token: 0x060007D0 RID: 2000 RVA: 0x0001AFD7 File Offset: 0x000191D7
		public ObjectHandle CreateComInstanceFrom(string assemblyName, string typeName)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			return Activator.CreateComInstanceFrom(assemblyName, typeName);
		}

		// Token: 0x060007D1 RID: 2001 RVA: 0x0001AFE9 File Offset: 0x000191E9
		public ObjectHandle CreateComInstanceFrom(string assemblyFile, string typeName, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			return Activator.CreateComInstanceFrom(assemblyFile, typeName, hashValue, hashAlgorithm);
		}

		// Token: 0x060007D2 RID: 2002 RVA: 0x0001AFFE File Offset: 0x000191FE
		public ObjectHandle CreateInstance(string assemblyName, string typeName, object[] activationAttributes)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			return Activator.CreateInstance(assemblyName, typeName, activationAttributes);
		}

		// Token: 0x060007D3 RID: 2003 RVA: 0x0001B01F File Offset: 0x0001921F
		public ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, object[] activationAttributes)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			return Activator.CreateInstanceFrom(assemblyFile, typeName, activationAttributes);
		}

		// Token: 0x060007D4 RID: 2004 RVA: 0x0001B034 File Offset: 0x00019234
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstance which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			if (securityAttributes != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			return Activator.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
		}

		// Token: 0x060007D5 RID: 2005 RVA: 0x0001B088 File Offset: 0x00019288
		public ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			return Activator.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
		}

		// Token: 0x060007D6 RID: 2006 RVA: 0x0001B0B4 File Offset: 0x000192B4
		[SecurityCritical]
		internal ObjectHandle InternalCreateInstanceWithNoSecurity(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
		{
			PermissionSet.s_fullTrust.Assert();
			return this.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
		}

		// Token: 0x060007D7 RID: 2007 RVA: 0x0001B0E0 File Offset: 0x000192E0
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			if (securityAttributes != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			return Activator.CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
		}

		// Token: 0x060007D8 RID: 2008 RVA: 0x0001B126 File Offset: 0x00019326
		public ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
		{
			if (this == null)
			{
				throw new NullReferenceException();
			}
			return Activator.CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
		}

		// Token: 0x060007D9 RID: 2009 RVA: 0x0001B144 File Offset: 0x00019344
		[SecurityCritical]
		internal ObjectHandle InternalCreateInstanceFromWithNoSecurity(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
		{
			PermissionSet.s_fullTrust.Assert();
			return this.CreateInstanceFrom(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
		}

		// Token: 0x060007DA RID: 2010 RVA: 0x0001B170 File Offset: 0x00019370
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(AssemblyName assemblyRef)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackCrawlMark, true, false, false);
		}

		// Token: 0x060007DB RID: 2011 RVA: 0x0001B18C File Offset: 0x0001938C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(string assemblyString)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackCrawlMark, false);
		}

		// Token: 0x060007DC RID: 2012 RVA: 0x0001B1A8 File Offset: 0x000193A8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(byte[] rawAssembly)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, null, null, ref stackCrawlMark, false, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007DD RID: 2013 RVA: 0x0001B1C4 File Offset: 0x000193C4
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackCrawlMark, false, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007DE RID: 2014 RVA: 0x0001B1E0 File Offset: 0x000193E0
		[SecuritySafeCritical]
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkId=155570 for more information.")]
		[SecurityPermission(SecurityAction.Demand, ControlEvidence = true)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, Evidence securityEvidence)
		{
			if (securityEvidence != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, securityEvidence, ref stackCrawlMark, false, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x060007DF RID: 2015 RVA: 0x0001B218 File Offset: 0x00019418
		[SecuritySafeCritical]
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(AssemblyName assemblyRef, Evidence assemblySecurity)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackCrawlMark, true, false, false);
		}

		// Token: 0x060007E0 RID: 2016 RVA: 0x0001B234 File Offset: 0x00019434
		[SecuritySafeCritical]
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Assembly Load(string assemblyString, Evidence assemblySecurity)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackCrawlMark, false);
		}

		// Token: 0x060007E1 RID: 2017 RVA: 0x0001B24D File Offset: 0x0001944D
		public int ExecuteAssembly(string assemblyFile)
		{
			return this.ExecuteAssembly(assemblyFile, null);
		}

		// Token: 0x060007E2 RID: 2018 RVA: 0x0001B257 File Offset: 0x00019457
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public int ExecuteAssembly(string assemblyFile, Evidence assemblySecurity)
		{
			return this.ExecuteAssembly(assemblyFile, assemblySecurity, null);
		}

		// Token: 0x060007E3 RID: 2019 RVA: 0x0001B264 File Offset: 0x00019464
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public int ExecuteAssembly(string assemblyFile, Evidence assemblySecurity, string[] args)
		{
			if (assemblySecurity != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, assemblySecurity);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x0001B2AC File Offset: 0x000194AC
		public int ExecuteAssembly(string assemblyFile, string[] args)
		{
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x0001B2D8 File Offset: 0x000194D8
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssembly which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public int ExecuteAssembly(string assemblyFile, Evidence assemblySecurity, string[] args, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
		{
			if (assemblySecurity != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, assemblySecurity, hashValue, hashAlgorithm);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007E6 RID: 2022 RVA: 0x0001B324 File Offset: 0x00019524
		public int ExecuteAssembly(string assemblyFile, string[] args, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
		{
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, hashValue, hashAlgorithm);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007E7 RID: 2023 RVA: 0x0001B353 File Offset: 0x00019553
		public int ExecuteAssemblyByName(string assemblyName)
		{
			return this.ExecuteAssemblyByName(assemblyName, null);
		}

		// Token: 0x060007E8 RID: 2024 RVA: 0x0001B35D File Offset: 0x0001955D
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssemblyByName which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public int ExecuteAssemblyByName(string assemblyName, Evidence assemblySecurity)
		{
			return this.ExecuteAssemblyByName(assemblyName, assemblySecurity, null);
		}

		// Token: 0x060007E9 RID: 2025 RVA: 0x0001B368 File Offset: 0x00019568
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssemblyByName which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public int ExecuteAssemblyByName(string assemblyName, Evidence assemblySecurity, params string[] args)
		{
			if (assemblySecurity != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName, assemblySecurity);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007EA RID: 2026 RVA: 0x0001B3B0 File Offset: 0x000195B0
		public int ExecuteAssemblyByName(string assemblyName, params string[] args)
		{
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007EB RID: 2027 RVA: 0x0001B3DC File Offset: 0x000195DC
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of ExecuteAssemblyByName which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public int ExecuteAssemblyByName(AssemblyName assemblyName, Evidence assemblySecurity, params string[] args)
		{
			if (assemblySecurity != null && !this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName, assemblySecurity);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x060007EC RID: 2028 RVA: 0x0001B424 File Offset: 0x00019624
		public int ExecuteAssemblyByName(AssemblyName assemblyName, params string[] args)
		{
			RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName);
			if (args == null)
			{
				args = new string[0];
			}
			return this.nExecuteAssembly(assembly, args);
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x060007ED RID: 2029 RVA: 0x0001B450 File Offset: 0x00019650
		public static AppDomain CurrentDomain
		{
			get
			{
				return Thread.GetDomain();
			}
		}

		// Token: 0x170000CC RID: 204
		// (get) Token: 0x060007EE RID: 2030 RVA: 0x0001B457 File Offset: 0x00019657
		public Evidence Evidence
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, ControlEvidence = true)]
			get
			{
				return this.EvidenceNoDemand;
			}
		}

		// Token: 0x170000CD RID: 205
		// (get) Token: 0x060007EF RID: 2031 RVA: 0x0001B45F File Offset: 0x0001965F
		internal Evidence EvidenceNoDemand
		{
			[SecurityCritical]
			get
			{
				if (this._SecurityIdentity != null)
				{
					return this._SecurityIdentity.Clone();
				}
				if (!this.IsDefaultAppDomain() && this.nIsDefaultAppDomainForEvidence())
				{
					return AppDomain.GetDefaultDomain().Evidence;
				}
				return new Evidence(new AppDomainEvidenceFactory(this));
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x060007F0 RID: 2032 RVA: 0x0001B49B File Offset: 0x0001969B
		internal Evidence InternalEvidence
		{
			get
			{
				return this._SecurityIdentity;
			}
		}

		// Token: 0x060007F1 RID: 2033 RVA: 0x0001B4A3 File Offset: 0x000196A3
		internal EvidenceBase GetHostEvidence(Type type)
		{
			if (this._SecurityIdentity != null)
			{
				return this._SecurityIdentity.GetHostEvidence(type);
			}
			return new Evidence(new AppDomainEvidenceFactory(this)).GetHostEvidence(type);
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x060007F2 RID: 2034 RVA: 0x0001B4CB File Offset: 0x000196CB
		public string FriendlyName
		{
			[SecuritySafeCritical]
			get
			{
				return this.nGetFriendlyName();
			}
		}

		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x060007F3 RID: 2035 RVA: 0x0001B4D3 File Offset: 0x000196D3
		public string BaseDirectory
		{
			get
			{
				return this.FusionStore.ApplicationBase;
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x060007F4 RID: 2036 RVA: 0x0001B4E0 File Offset: 0x000196E0
		public string RelativeSearchPath
		{
			get
			{
				return this.FusionStore.PrivateBinPath;
			}
		}

		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x060007F5 RID: 2037 RVA: 0x0001B4F0 File Offset: 0x000196F0
		public bool ShadowCopyFiles
		{
			get
			{
				string shadowCopyFiles = this.FusionStore.ShadowCopyFiles;
				return shadowCopyFiles != null && string.Compare(shadowCopyFiles, "true", StringComparison.OrdinalIgnoreCase) == 0;
			}
		}

		// Token: 0x060007F6 RID: 2038 RVA: 0x0001B520 File Offset: 0x00019720
		[SecuritySafeCritical]
		public override string ToString()
		{
			StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
			string text = this.nGetFriendlyName();
			if (text != null)
			{
				stringBuilder.Append(Environment.GetResourceString("Loader_Name") + text);
				stringBuilder.Append(Environment.NewLine);
			}
			if (this._Policies == null || this._Policies.Length == 0)
			{
				stringBuilder.Append(Environment.GetResourceString("Loader_NoContextPolicies") + Environment.NewLine);
			}
			else
			{
				stringBuilder.Append(Environment.GetResourceString("Loader_ContextPolicies") + Environment.NewLine);
				for (int i = 0; i < this._Policies.Length; i++)
				{
					stringBuilder.Append(this._Policies[i]);
					stringBuilder.Append(Environment.NewLine);
				}
			}
			return StringBuilderCache.GetStringAndRelease(stringBuilder);
		}

		// Token: 0x060007F7 RID: 2039 RVA: 0x0001B5DF File Offset: 0x000197DF
		public Assembly[] GetAssemblies()
		{
			return this.nGetAssemblies(false);
		}

		// Token: 0x060007F8 RID: 2040 RVA: 0x0001B5E8 File Offset: 0x000197E8
		public Assembly[] ReflectionOnlyGetAssemblies()
		{
			return this.nGetAssemblies(true);
		}

		// Token: 0x060007F9 RID: 2041
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern Assembly[] nGetAssemblies(bool forIntrospection);

		// Token: 0x060007FA RID: 2042
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern bool IsUnloadingForcedFinalize();

		// Token: 0x060007FB RID: 2043
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern bool IsFinalizingForUnload();

		// Token: 0x060007FC RID: 2044
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void PublishAnonymouslyHostedDynamicMethodsAssembly(RuntimeAssembly assemblyHandle);

		// Token: 0x060007FD RID: 2045 RVA: 0x0001B5F4 File Offset: 0x000197F4
		[SecurityCritical]
		[Obsolete("AppDomain.AppendPrivatePath has been deprecated. Please investigate the use of AppDomainSetup.PrivateBinPath instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void AppendPrivatePath(string path)
		{
			if (path == null || path.Length == 0)
			{
				return;
			}
			string text = this.FusionStore.Value[5];
			StringBuilder stringBuilder = StringBuilderCache.Acquire(16);
			if (text != null && text.Length > 0)
			{
				stringBuilder.Append(text);
				if (text[text.Length - 1] != Path.PathSeparator && path[0] != Path.PathSeparator)
				{
					stringBuilder.Append(Path.PathSeparator);
				}
			}
			stringBuilder.Append(path);
			string stringAndRelease = StringBuilderCache.GetStringAndRelease(stringBuilder);
			this.InternalSetPrivateBinPath(stringAndRelease);
		}

		// Token: 0x060007FE RID: 2046 RVA: 0x0001B67C File Offset: 0x0001987C
		[SecurityCritical]
		[Obsolete("AppDomain.ClearPrivatePath has been deprecated. Please investigate the use of AppDomainSetup.PrivateBinPath instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void ClearPrivatePath()
		{
			this.InternalSetPrivateBinPath(string.Empty);
		}

		// Token: 0x060007FF RID: 2047 RVA: 0x0001B689 File Offset: 0x00019889
		[SecurityCritical]
		[Obsolete("AppDomain.ClearShadowCopyPath has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyDirectories instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void ClearShadowCopyPath()
		{
			this.InternalSetShadowCopyPath(string.Empty);
		}

		// Token: 0x06000800 RID: 2048 RVA: 0x0001B696 File Offset: 0x00019896
		[SecurityCritical]
		[Obsolete("AppDomain.SetCachePath has been deprecated. Please investigate the use of AppDomainSetup.CachePath instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void SetCachePath(string path)
		{
			this.InternalSetCachePath(path);
		}

		// Token: 0x06000801 RID: 2049 RVA: 0x0001B69F File Offset: 0x0001989F
		[SecurityCritical]
		public void SetData(string name, object data)
		{
			this.SetDataHelper(name, data, null);
		}

		// Token: 0x06000802 RID: 2050 RVA: 0x0001B6AA File Offset: 0x000198AA
		[SecurityCritical]
		public void SetData(string name, object data, IPermission permission)
		{
			this.SetDataHelper(name, data, permission);
		}

		// Token: 0x06000803 RID: 2051 RVA: 0x0001B6B8 File Offset: 0x000198B8
		[SecurityCritical]
		private void SetDataHelper(string name, object data, IPermission permission)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Equals("TargetFrameworkName"))
			{
				this._FusionStore.TargetFrameworkName = (string)data;
				return;
			}
			if (name.Equals("IgnoreSystemPolicy"))
			{
				lock (this)
				{
					if (!this._HasSetPolicy)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData"));
					}
				}
				new PermissionSet(PermissionState.Unrestricted).Demand();
			}
			int num = AppDomainSetup.Locate(name);
			if (num == -1)
			{
				object syncRoot = ((ICollection)this.LocalStore).SyncRoot;
				lock (syncRoot)
				{
					this.LocalStore[name] = new object[] { data, permission };
					return;
				}
			}
			if (permission != null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData"));
			}
			switch (num)
			{
			case 2:
				this.FusionStore.DynamicBase = (string)data;
				return;
			case 3:
				this.FusionStore.DeveloperPath = (string)data;
				return;
			case 7:
				this.FusionStore.ShadowCopyDirectories = (string)data;
				return;
			case 11:
				if (data != null)
				{
					this.FusionStore.DisallowPublisherPolicy = true;
					return;
				}
				this.FusionStore.DisallowPublisherPolicy = false;
				return;
			case 12:
				if (data != null)
				{
					this.FusionStore.DisallowCodeDownload = true;
					return;
				}
				this.FusionStore.DisallowCodeDownload = false;
				return;
			case 13:
				if (data != null)
				{
					this.FusionStore.DisallowBindingRedirects = true;
					return;
				}
				this.FusionStore.DisallowBindingRedirects = false;
				return;
			case 14:
				if (data != null)
				{
					this.FusionStore.DisallowApplicationBaseProbing = true;
					return;
				}
				this.FusionStore.DisallowApplicationBaseProbing = false;
				return;
			case 15:
				this.FusionStore.SetConfigurationBytes((byte[])data);
				return;
			}
			this.FusionStore.Value[num] = (string)data;
		}

		// Token: 0x06000804 RID: 2052 RVA: 0x0001B8CC File Offset: 0x00019ACC
		[SecuritySafeCritical]
		public object GetData(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			int num = AppDomainSetup.Locate(name);
			if (num == -1)
			{
				if (name.Equals(AppDomainSetup.LoaderOptimizationKey))
				{
					return this.FusionStore.LoaderOptimization;
				}
				object syncRoot = ((ICollection)this.LocalStore).SyncRoot;
				object[] array;
				lock (syncRoot)
				{
					this.LocalStore.TryGetValue(name, out array);
				}
				if (array == null)
				{
					return null;
				}
				if (array[1] != null)
				{
					IPermission permission = (IPermission)array[1];
					permission.Demand();
				}
				return array[0];
			}
			else
			{
				switch (num)
				{
				case 0:
					return this.FusionStore.ApplicationBase;
				case 1:
					return this.FusionStore.ConfigurationFile;
				case 2:
					return this.FusionStore.DynamicBase;
				case 3:
					return this.FusionStore.DeveloperPath;
				case 4:
					return this.FusionStore.ApplicationName;
				case 5:
					return this.FusionStore.PrivateBinPath;
				case 6:
					return this.FusionStore.PrivateBinPathProbe;
				case 7:
					return this.FusionStore.ShadowCopyDirectories;
				case 8:
					return this.FusionStore.ShadowCopyFiles;
				case 9:
					return this.FusionStore.CachePath;
				case 10:
					return this.FusionStore.LicenseFile;
				case 11:
					return this.FusionStore.DisallowPublisherPolicy;
				case 12:
					return this.FusionStore.DisallowCodeDownload;
				case 13:
					return this.FusionStore.DisallowBindingRedirects;
				case 14:
					return this.FusionStore.DisallowApplicationBaseProbing;
				case 15:
					return this.FusionStore.GetConfigurationBytes();
				default:
					return null;
				}
			}
		}

		// Token: 0x06000805 RID: 2053 RVA: 0x0001BA90 File Offset: 0x00019C90
		public bool? IsCompatibilitySwitchSet(string value)
		{
			bool? result;
			if (!this._compatFlagsInitialized)
			{
				result = null;
			}
			else
			{
				result = new bool?(this._compatFlags != null && this._compatFlags.ContainsKey(value));
			}
			return result;
		}

		// Token: 0x06000806 RID: 2054
		[Obsolete("AppDomain.GetCurrentThreadId has been deprecated because it does not provide a stable Id when managed threads are running on fibers (aka lightweight threads). To get a stable identifier for a managed thread, use the ManagedThreadId property on Thread.  http://go.microsoft.com/fwlink/?linkid=14202", false)]
		[DllImport("kernel32.dll")]
		public static extern int GetCurrentThreadId();

		// Token: 0x06000807 RID: 2055 RVA: 0x0001BAD0 File Offset: 0x00019CD0
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.MayFail)]
		[SecurityPermission(SecurityAction.Demand, ControlAppDomain = true)]
		public static void Unload(AppDomain domain)
		{
			if (domain == null)
			{
				throw new ArgumentNullException("domain");
			}
			try
			{
				int idForUnload = AppDomain.GetIdForUnload(domain);
				if (idForUnload == 0)
				{
					throw new CannotUnloadAppDomainException();
				}
				AppDomain.nUnload(idForUnload);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		// Token: 0x06000808 RID: 2056 RVA: 0x0001BB18 File Offset: 0x00019D18
		[SecurityCritical]
		[Obsolete("AppDomain policy levels are obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public void SetAppDomainPolicy(PolicyLevel domainPolicy)
		{
			if (domainPolicy == null)
			{
				throw new ArgumentNullException("domainPolicy");
			}
			if (!this.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyExplicit"));
			}
			lock (this)
			{
				if (this._HasSetPolicy)
				{
					throw new PolicyException(Environment.GetResourceString("Policy_PolicyAlreadySet"));
				}
				this._HasSetPolicy = true;
				this.nChangeSecurityPolicy();
			}
			SecurityManager.PolicyManager.AddLevel(domainPolicy);
		}

		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x06000809 RID: 2057 RVA: 0x0001BBA4 File Offset: 0x00019DA4
		public ActivationContext ActivationContext
		{
			[SecurityCritical]
			get
			{
				return this._activationContext;
			}
		}

		// Token: 0x170000D4 RID: 212
		// (get) Token: 0x0600080A RID: 2058 RVA: 0x0001BBAC File Offset: 0x00019DAC
		public ApplicationIdentity ApplicationIdentity
		{
			[SecurityCritical]
			get
			{
				return this._applicationIdentity;
			}
		}

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x0600080B RID: 2059 RVA: 0x0001BBB4 File Offset: 0x00019DB4
		public ApplicationTrust ApplicationTrust
		{
			[SecurityCritical]
			get
			{
				if (this._applicationTrust == null && this._IsFastFullTrustDomain)
				{
					this._applicationTrust = new ApplicationTrust(new PermissionSet(PermissionState.Unrestricted));
				}
				return this._applicationTrust;
			}
		}

		// Token: 0x0600080C RID: 2060 RVA: 0x0001BBE0 File Offset: 0x00019DE0
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public void SetThreadPrincipal(IPrincipal principal)
		{
			if (principal == null)
			{
				throw new ArgumentNullException("principal");
			}
			lock (this)
			{
				if (this._DefaultPrincipal != null)
				{
					throw new PolicyException(Environment.GetResourceString("Policy_PrincipalTwice"));
				}
				this._DefaultPrincipal = principal;
			}
		}

		// Token: 0x0600080D RID: 2061 RVA: 0x0001BC44 File Offset: 0x00019E44
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public void SetPrincipalPolicy(PrincipalPolicy policy)
		{
			this._PrincipalPolicy = policy;
		}

		// Token: 0x0600080E RID: 2062 RVA: 0x0001BC4D File Offset: 0x00019E4D
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Token: 0x0600080F RID: 2063 RVA: 0x0001BC50 File Offset: 0x00019E50
		public void DoCallBack(CrossAppDomainDelegate callBackDelegate)
		{
			if (callBackDelegate == null)
			{
				throw new ArgumentNullException("callBackDelegate");
			}
			callBackDelegate();
		}

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x06000810 RID: 2064 RVA: 0x0001BC68 File Offset: 0x00019E68
		public string DynamicDirectory
		{
			[SecuritySafeCritical]
			get
			{
				string dynamicDir = this.GetDynamicDir();
				if (dynamicDir != null)
				{
					FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, dynamicDir, false, true);
				}
				return dynamicDir;
			}
		}

		// Token: 0x06000811 RID: 2065 RVA: 0x0001BC89 File Offset: 0x00019E89
		public static AppDomain CreateDomain(string friendlyName, Evidence securityInfo)
		{
			return AppDomain.CreateDomain(friendlyName, securityInfo, null);
		}

		// Token: 0x06000812 RID: 2066 RVA: 0x0001BC94 File Offset: 0x00019E94
		public static AppDomain CreateDomain(string friendlyName, Evidence securityInfo, string appBasePath, string appRelativeSearchPath, bool shadowCopyFiles)
		{
			AppDomainSetup appDomainSetup = new AppDomainSetup();
			appDomainSetup.ApplicationBase = appBasePath;
			appDomainSetup.PrivateBinPath = appRelativeSearchPath;
			if (shadowCopyFiles)
			{
				appDomainSetup.ShadowCopyFiles = "true";
			}
			return AppDomain.CreateDomain(friendlyName, securityInfo, appDomainSetup);
		}

		// Token: 0x06000813 RID: 2067
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern string GetDynamicDir();

		// Token: 0x06000814 RID: 2068 RVA: 0x0001BCCC File Offset: 0x00019ECC
		public static AppDomain CreateDomain(string friendlyName)
		{
			return AppDomain.CreateDomain(friendlyName, null, null);
		}

		// Token: 0x06000815 RID: 2069 RVA: 0x0001BCD6 File Offset: 0x00019ED6
		[SecurityCritical]
		private static byte[] MarshalObject(object o)
		{
			CodeAccessPermission.Assert(true);
			return AppDomain.Serialize(o);
		}

		// Token: 0x06000816 RID: 2070 RVA: 0x0001BCE4 File Offset: 0x00019EE4
		[SecurityCritical]
		private static byte[] MarshalObjects(object o1, object o2, out byte[] blob2)
		{
			CodeAccessPermission.Assert(true);
			byte[] result = AppDomain.Serialize(o1);
			blob2 = AppDomain.Serialize(o2);
			return result;
		}

		// Token: 0x06000817 RID: 2071 RVA: 0x0001BD07 File Offset: 0x00019F07
		[SecurityCritical]
		private static object UnmarshalObject(byte[] blob)
		{
			CodeAccessPermission.Assert(true);
			return AppDomain.Deserialize(blob);
		}

		// Token: 0x06000818 RID: 2072 RVA: 0x0001BD18 File Offset: 0x00019F18
		[SecurityCritical]
		private static object UnmarshalObjects(byte[] blob1, byte[] blob2, out object o2)
		{
			CodeAccessPermission.Assert(true);
			object result = AppDomain.Deserialize(blob1);
			o2 = AppDomain.Deserialize(blob2);
			return result;
		}

		// Token: 0x06000819 RID: 2073 RVA: 0x0001BD3C File Offset: 0x00019F3C
		[SecurityCritical]
		private static byte[] Serialize(object o)
		{
			if (o == null)
			{
				return null;
			}
			if (o is ISecurityEncodable)
			{
				SecurityElement securityElement = ((ISecurityEncodable)o).ToXml();
				MemoryStream memoryStream = new MemoryStream(4096);
				memoryStream.WriteByte(0);
				StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
				securityElement.ToWriter(streamWriter);
				streamWriter.Flush();
				return memoryStream.ToArray();
			}
			MemoryStream memoryStream2 = new MemoryStream();
			memoryStream2.WriteByte(1);
			CrossAppDomainSerializer.SerializeObject(o, memoryStream2);
			return memoryStream2.ToArray();
		}

		// Token: 0x0600081A RID: 2074 RVA: 0x0001BDB0 File Offset: 0x00019FB0
		[SecurityCritical]
		private static object Deserialize(byte[] blob)
		{
			if (blob == null)
			{
				return null;
			}
			if (blob[0] != 0)
			{
				object result = null;
				using (MemoryStream memoryStream = new MemoryStream(blob, 1, blob.Length - 1))
				{
					result = CrossAppDomainSerializer.DeserializeObject(memoryStream);
				}
				return result;
			}
			Parser parser = new Parser(blob, Tokenizer.ByteTokenEncoding.UTF8Tokens, 1);
			SecurityElement topElement = parser.GetTopElement();
			if (topElement.Tag.Equals("IPermission") || topElement.Tag.Equals("Permission"))
			{
				IPermission permission = XMLUtil.CreatePermission(topElement, PermissionState.None, false);
				if (permission == null)
				{
					return null;
				}
				permission.FromXml(topElement);
				return permission;
			}
			else
			{
				if (topElement.Tag.Equals("PermissionSet"))
				{
					PermissionSet permissionSet = new PermissionSet();
					permissionSet.FromXml(topElement, false, false);
					return permissionSet;
				}
				if (topElement.Tag.Equals("PermissionToken"))
				{
					PermissionToken permissionToken = new PermissionToken();
					permissionToken.FromXml(topElement);
					return permissionToken;
				}
				return null;
			}
		}

		// Token: 0x0600081B RID: 2075 RVA: 0x0001BE98 File Offset: 0x0001A098
		[SecurityCritical]
		internal static void Pause()
		{
			AppDomainPauseManager.Instance.Pausing();
			AppDomainPauseManager.Instance.Paused();
		}

		// Token: 0x0600081C RID: 2076 RVA: 0x0001BEAE File Offset: 0x0001A0AE
		[SecurityCritical]
		internal static void Resume()
		{
			if (AppDomainPauseManager.IsPaused)
			{
				AppDomainPauseManager.Instance.Resuming();
				AppDomainPauseManager.Instance.Resumed();
			}
		}

		// Token: 0x0600081D RID: 2077 RVA: 0x0001BECB File Offset: 0x0001A0CB
		private AppDomain()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_Constructor"));
		}

		// Token: 0x0600081E RID: 2078
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern int _nExecuteAssembly(RuntimeAssembly assembly, string[] args);

		// Token: 0x0600081F RID: 2079 RVA: 0x0001BEE2 File Offset: 0x0001A0E2
		internal int nExecuteAssembly(RuntimeAssembly assembly, string[] args)
		{
			return this._nExecuteAssembly(assembly, args);
		}

		// Token: 0x06000820 RID: 2080 RVA: 0x0001BEEC File Offset: 0x0001A0EC
		internal void CreateRemotingData()
		{
			lock (this)
			{
				if (this._RemotingData == null)
				{
					this._RemotingData = new DomainSpecificRemotingData();
				}
			}
		}

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x06000821 RID: 2081 RVA: 0x0001BF34 File Offset: 0x0001A134
		internal DomainSpecificRemotingData RemotingData
		{
			get
			{
				if (this._RemotingData == null)
				{
					this.CreateRemotingData();
				}
				return this._RemotingData;
			}
		}

		// Token: 0x06000822 RID: 2082
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern string nGetFriendlyName();

		// Token: 0x06000823 RID: 2083
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool nIsDefaultAppDomainForEvidence();

		// Token: 0x14000007 RID: 7
		// (add) Token: 0x06000824 RID: 2084 RVA: 0x0001BF4C File Offset: 0x0001A14C
		// (remove) Token: 0x06000825 RID: 2085 RVA: 0x0001BFA4 File Offset: 0x0001A1A4
		public event EventHandler ProcessExit
		{
			[SecuritySafeCritical]
			add
			{
				if (value != null)
				{
					RuntimeHelpers.PrepareContractedDelegate(value);
					lock (this)
					{
						this._processExit = (EventHandler)Delegate.Combine(this._processExit, value);
					}
				}
			}
			remove
			{
				lock (this)
				{
					this._processExit = (EventHandler)Delegate.Remove(this._processExit, value);
				}
			}
		}

		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000826 RID: 2086 RVA: 0x0001BFF0 File Offset: 0x0001A1F0
		// (remove) Token: 0x06000827 RID: 2087 RVA: 0x0001C048 File Offset: 0x0001A248
		public event EventHandler DomainUnload
		{
			[SecuritySafeCritical]
			add
			{
				if (value != null)
				{
					RuntimeHelpers.PrepareContractedDelegate(value);
					lock (this)
					{
						this._domainUnload = (EventHandler)Delegate.Combine(this._domainUnload, value);
					}
				}
			}
			remove
			{
				lock (this)
				{
					this._domainUnload = (EventHandler)Delegate.Remove(this._domainUnload, value);
				}
			}
		}

		// Token: 0x14000009 RID: 9
		// (add) Token: 0x06000828 RID: 2088 RVA: 0x0001C094 File Offset: 0x0001A294
		// (remove) Token: 0x06000829 RID: 2089 RVA: 0x0001C0EC File Offset: 0x0001A2EC
		public event UnhandledExceptionEventHandler UnhandledException
		{
			[SecurityCritical]
			add
			{
				if (value != null)
				{
					RuntimeHelpers.PrepareContractedDelegate(value);
					lock (this)
					{
						this._unhandledException = (UnhandledExceptionEventHandler)Delegate.Combine(this._unhandledException, value);
					}
				}
			}
			[SecurityCritical]
			remove
			{
				lock (this)
				{
					this._unhandledException = (UnhandledExceptionEventHandler)Delegate.Remove(this._unhandledException, value);
				}
			}
		}

		// Token: 0x1400000A RID: 10
		// (add) Token: 0x0600082A RID: 2090 RVA: 0x0001C138 File Offset: 0x0001A338
		// (remove) Token: 0x0600082B RID: 2091 RVA: 0x0001C190 File Offset: 0x0001A390
		public event EventHandler<FirstChanceExceptionEventArgs> FirstChanceException
		{
			[SecurityCritical]
			add
			{
				if (value != null)
				{
					RuntimeHelpers.PrepareContractedDelegate(value);
					lock (this)
					{
						this._firstChanceException = (EventHandler<FirstChanceExceptionEventArgs>)Delegate.Combine(this._firstChanceException, value);
					}
				}
			}
			[SecurityCritical]
			remove
			{
				lock (this)
				{
					this._firstChanceException = (EventHandler<FirstChanceExceptionEventArgs>)Delegate.Remove(this._firstChanceException, value);
				}
			}
		}

		// Token: 0x0600082C RID: 2092 RVA: 0x0001C1DC File Offset: 0x0001A3DC
		private void OnAssemblyLoadEvent(RuntimeAssembly LoadedAssembly)
		{
			AssemblyLoadEventHandler assemblyLoad = this.AssemblyLoad;
			if (assemblyLoad != null)
			{
				AssemblyLoadEventArgs args = new AssemblyLoadEventArgs(LoadedAssembly);
				assemblyLoad(this, args);
			}
		}

		// Token: 0x0600082D RID: 2093 RVA: 0x0001C204 File Offset: 0x0001A404
		[SecurityCritical]
		private RuntimeAssembly OnResourceResolveEvent(RuntimeAssembly assembly, string resourceName)
		{
			ResolveEventHandler resourceResolve = this._ResourceResolve;
			if (resourceResolve == null)
			{
				return null;
			}
			Delegate[] invocationList = resourceResolve.GetInvocationList();
			int num = invocationList.Length;
			for (int i = 0; i < num; i++)
			{
				Assembly asm = ((ResolveEventHandler)invocationList[i])(this, new ResolveEventArgs(resourceName, assembly));
				RuntimeAssembly runtimeAssembly = AppDomain.GetRuntimeAssembly(asm);
				if (runtimeAssembly != null)
				{
					return runtimeAssembly;
				}
			}
			return null;
		}

		// Token: 0x0600082E RID: 2094 RVA: 0x0001C264 File Offset: 0x0001A464
		[SecurityCritical]
		private RuntimeAssembly OnTypeResolveEvent(RuntimeAssembly assembly, string typeName)
		{
			ResolveEventHandler typeResolve = this._TypeResolve;
			if (typeResolve == null)
			{
				return null;
			}
			Delegate[] invocationList = typeResolve.GetInvocationList();
			int num = invocationList.Length;
			for (int i = 0; i < num; i++)
			{
				Assembly asm = ((ResolveEventHandler)invocationList[i])(this, new ResolveEventArgs(typeName, assembly));
				RuntimeAssembly runtimeAssembly = AppDomain.GetRuntimeAssembly(asm);
				if (runtimeAssembly != null)
				{
					return runtimeAssembly;
				}
			}
			return null;
		}

		// Token: 0x0600082F RID: 2095 RVA: 0x0001C2C4 File Offset: 0x0001A4C4
		[SecurityCritical]
		private RuntimeAssembly OnAssemblyResolveEvent(RuntimeAssembly assembly, string assemblyFullName)
		{
			ResolveEventHandler assemblyResolve = this._AssemblyResolve;
			if (assemblyResolve == null)
			{
				return null;
			}
			Delegate[] invocationList = assemblyResolve.GetInvocationList();
			int num = invocationList.Length;
			for (int i = 0; i < num; i++)
			{
				Assembly asm = ((ResolveEventHandler)invocationList[i])(this, new ResolveEventArgs(assemblyFullName, assembly));
				RuntimeAssembly runtimeAssembly = AppDomain.GetRuntimeAssembly(asm);
				if (runtimeAssembly != null)
				{
					return runtimeAssembly;
				}
			}
			return null;
		}

		// Token: 0x06000830 RID: 2096 RVA: 0x0001C324 File Offset: 0x0001A524
		private RuntimeAssembly OnReflectionOnlyAssemblyResolveEvent(RuntimeAssembly assembly, string assemblyFullName)
		{
			ResolveEventHandler reflectionOnlyAssemblyResolve = this.ReflectionOnlyAssemblyResolve;
			if (reflectionOnlyAssemblyResolve != null)
			{
				Delegate[] invocationList = reflectionOnlyAssemblyResolve.GetInvocationList();
				int num = invocationList.Length;
				for (int i = 0; i < num; i++)
				{
					Assembly asm = ((ResolveEventHandler)invocationList[i])(this, new ResolveEventArgs(assemblyFullName, assembly));
					RuntimeAssembly runtimeAssembly = AppDomain.GetRuntimeAssembly(asm);
					if (runtimeAssembly != null)
					{
						return runtimeAssembly;
					}
				}
			}
			return null;
		}

		// Token: 0x06000831 RID: 2097 RVA: 0x0001C380 File Offset: 0x0001A580
		private RuntimeAssembly[] OnReflectionOnlyNamespaceResolveEvent(RuntimeAssembly assembly, string namespaceName)
		{
			return WindowsRuntimeMetadata.OnReflectionOnlyNamespaceResolveEvent(this, assembly, namespaceName);
		}

		// Token: 0x06000832 RID: 2098 RVA: 0x0001C38A File Offset: 0x0001A58A
		private string[] OnDesignerNamespaceResolveEvent(string namespaceName)
		{
			return WindowsRuntimeMetadata.OnDesignerNamespaceResolveEvent(this, namespaceName);
		}

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x06000833 RID: 2099 RVA: 0x0001C393 File Offset: 0x0001A593
		internal AppDomainSetup FusionStore
		{
			get
			{
				return this._FusionStore;
			}
		}

		// Token: 0x06000834 RID: 2100 RVA: 0x0001C39C File Offset: 0x0001A59C
		internal static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
		{
			if (asm == null)
			{
				return null;
			}
			RuntimeAssembly runtimeAssembly = asm as RuntimeAssembly;
			if (runtimeAssembly != null)
			{
				return runtimeAssembly;
			}
			AssemblyBuilder assemblyBuilder = asm as AssemblyBuilder;
			if (assemblyBuilder != null)
			{
				return assemblyBuilder.InternalAssembly;
			}
			return null;
		}

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x06000835 RID: 2101 RVA: 0x0001C3DE File Offset: 0x0001A5DE
		private Dictionary<string, object[]> LocalStore
		{
			get
			{
				if (this._LocalStore != null)
				{
					return this._LocalStore;
				}
				this._LocalStore = new Dictionary<string, object[]>();
				return this._LocalStore;
			}
		}

		// Token: 0x06000836 RID: 2102 RVA: 0x0001C400 File Offset: 0x0001A600
		private void TurnOnBindingRedirects()
		{
			this._FusionStore.DisallowBindingRedirects = false;
		}

		// Token: 0x06000837 RID: 2103 RVA: 0x0001C40E File Offset: 0x0001A60E
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal static int GetIdForUnload(AppDomain domain)
		{
			if (RemotingServices.IsTransparentProxy(domain))
			{
				return RemotingServices.GetServerDomainIdForProxy(domain);
			}
			return domain.Id;
		}

		// Token: 0x06000838 RID: 2104
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsDomainIdValid(int id);

		// Token: 0x06000839 RID: 2105
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern AppDomain GetDefaultDomain();

		// Token: 0x0600083A RID: 2106 RVA: 0x0001C428 File Offset: 0x0001A628
		internal IPrincipal GetThreadPrincipal()
		{
			IPrincipal result;
			if (this._DefaultPrincipal == null)
			{
				switch (this._PrincipalPolicy)
				{
				case PrincipalPolicy.UnauthenticatedPrincipal:
					result = new GenericPrincipal(new GenericIdentity("", ""), new string[] { "" });
					break;
				case PrincipalPolicy.NoPrincipal:
					result = null;
					break;
				case PrincipalPolicy.WindowsPrincipal:
					result = new WindowsPrincipal(WindowsIdentity.GetCurrent());
					break;
				default:
					result = null;
					break;
				}
			}
			else
			{
				result = this._DefaultPrincipal;
			}
			return result;
		}

		// Token: 0x0600083B RID: 2107 RVA: 0x0001C49C File Offset: 0x0001A69C
		[SecurityCritical]
		internal void CreateDefaultContext()
		{
			lock (this)
			{
				if (this._DefaultContext == null)
				{
					this._DefaultContext = Context.CreateDefaultContext();
				}
			}
		}

		// Token: 0x0600083C RID: 2108 RVA: 0x0001C4E4 File Offset: 0x0001A6E4
		[SecurityCritical]
		internal Context GetDefaultContext()
		{
			if (this._DefaultContext == null)
			{
				this.CreateDefaultContext();
			}
			return this._DefaultContext;
		}

		// Token: 0x0600083D RID: 2109 RVA: 0x0001C4FC File Offset: 0x0001A6FC
		[SecuritySafeCritical]
		internal static void CheckDomainCreationEvidence(AppDomainSetup creationDomainSetup, Evidence creationEvidence)
		{
			if (creationEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled && (creationDomainSetup == null || creationDomainSetup.ApplicationTrust == null))
			{
				Zone hostEvidence = AppDomain.CurrentDomain.EvidenceNoDemand.GetHostEvidence<Zone>();
				SecurityZone securityZone = ((hostEvidence != null) ? hostEvidence.SecurityZone : SecurityZone.MyComputer);
				Zone hostEvidence2 = creationEvidence.GetHostEvidence<Zone>();
				if (hostEvidence2 != null && hostEvidence2.SecurityZone != securityZone && hostEvidence2.SecurityZone != SecurityZone.MyComputer)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
				}
			}
		}

		// Token: 0x0600083E RID: 2110 RVA: 0x0001C56B File Offset: 0x0001A76B
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, ControlAppDomain = true)]
		public static AppDomain CreateDomain(string friendlyName, Evidence securityInfo, AppDomainSetup info)
		{
			return AppDomain.InternalCreateDomain(friendlyName, securityInfo, info);
		}

		// Token: 0x0600083F RID: 2111 RVA: 0x0001C578 File Offset: 0x0001A778
		[SecurityCritical]
		internal static AppDomain InternalCreateDomain(string friendlyName, Evidence securityInfo, AppDomainSetup info)
		{
			if (friendlyName == null)
			{
				throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_String"));
			}
			AppDomain.CheckCreateDomainSupported();
			if (info == null)
			{
				info = new AppDomainSetup();
			}
			if (info.TargetFrameworkName == null)
			{
				info.TargetFrameworkName = AppDomain.CurrentDomain.GetTargetFrameworkName();
			}
			AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
			if (domainManager != null)
			{
				return domainManager.CreateDomain(friendlyName, securityInfo, info);
			}
			if (securityInfo != null)
			{
				new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
				AppDomain.CheckDomainCreationEvidence(info, securityInfo);
			}
			return AppDomain.nCreateDomain(friendlyName, info, securityInfo, (securityInfo == null) ? AppDomain.CurrentDomain.InternalEvidence : null, AppDomain.CurrentDomain.GetSecurityDescriptor());
		}

		// Token: 0x06000840 RID: 2112 RVA: 0x0001C610 File Offset: 0x0001A810
		public static AppDomain CreateDomain(string friendlyName, Evidence securityInfo, AppDomainSetup info, PermissionSet grantSet, params StrongName[] fullTrustAssemblies)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			if (info.ApplicationBase == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AppDomainSandboxAPINeedsExplicitAppBase"));
			}
			if (fullTrustAssemblies == null)
			{
				fullTrustAssemblies = new StrongName[0];
			}
			info.ApplicationTrust = new ApplicationTrust(grantSet, fullTrustAssemblies);
			return AppDomain.CreateDomain(friendlyName, securityInfo, info);
		}

		// Token: 0x06000841 RID: 2113 RVA: 0x0001C668 File Offset: 0x0001A868
		public static AppDomain CreateDomain(string friendlyName, Evidence securityInfo, string appBasePath, string appRelativeSearchPath, bool shadowCopyFiles, AppDomainInitializer adInit, string[] adInitArgs)
		{
			AppDomainSetup appDomainSetup = new AppDomainSetup();
			appDomainSetup.ApplicationBase = appBasePath;
			appDomainSetup.PrivateBinPath = appRelativeSearchPath;
			appDomainSetup.AppDomainInitializer = adInit;
			appDomainSetup.AppDomainInitializerArguments = adInitArgs;
			if (shadowCopyFiles)
			{
				appDomainSetup.ShadowCopyFiles = "true";
			}
			return AppDomain.CreateDomain(friendlyName, securityInfo, appDomainSetup);
		}

		// Token: 0x06000842 RID: 2114 RVA: 0x0001C6B0 File Offset: 0x0001A8B0
		[SecurityCritical]
		private void SetupFusionStore(AppDomainSetup info, AppDomainSetup oldInfo)
		{
			this._FusionStore = info;
			if (oldInfo == null)
			{
				if (info.Value[0] == null || info.Value[1] == null)
				{
					AppDomain defaultDomain = AppDomain.GetDefaultDomain();
					if (this == defaultDomain)
					{
						info.SetupDefaults(RuntimeEnvironment.GetModuleFileName(), true);
					}
					else
					{
						if (info.Value[1] == null)
						{
							info.ConfigurationFile = defaultDomain.FusionStore.Value[1];
						}
						if (info.Value[0] == null)
						{
							info.ApplicationBase = defaultDomain.FusionStore.Value[0];
						}
						if (info.Value[4] == null)
						{
							info.ApplicationName = defaultDomain.FusionStore.Value[4];
						}
					}
				}
				if (info.Value[5] == null)
				{
					info.PrivateBinPath = Environment.nativeGetEnvironmentVariable(AppDomainSetup.PrivateBinPathEnvironmentVariable);
				}
				if (info.DeveloperPath == null)
				{
					info.DeveloperPath = RuntimeEnvironment.GetDeveloperPath();
				}
			}
			IntPtr fusionContext = this.GetFusionContext();
			info.SetupFusionContext(fusionContext, oldInfo);
			if (info.LoaderOptimization != LoaderOptimization.NotSpecified || (oldInfo != null && info.LoaderOptimization != oldInfo.LoaderOptimization))
			{
				this.UpdateLoaderOptimization(info.LoaderOptimization);
			}
		}

		// Token: 0x06000843 RID: 2115 RVA: 0x0001C7B0 File Offset: 0x0001A9B0
		private static void RunInitializer(AppDomainSetup setup)
		{
			if (setup.AppDomainInitializer != null)
			{
				string[] args = null;
				if (setup.AppDomainInitializerArguments != null)
				{
					args = (string[])setup.AppDomainInitializerArguments.Clone();
				}
				setup.AppDomainInitializer(args);
			}
		}

		// Token: 0x06000844 RID: 2116 RVA: 0x0001C7EC File Offset: 0x0001A9EC
		[SecurityCritical]
		private static object PrepareDataForSetup(string friendlyName, AppDomainSetup setup, Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, IntPtr parentSecurityDescriptor, string sandboxName, string[] propertyNames, string[] propertyValues)
		{
			byte[] array = null;
			bool flag = false;
			AppDomain.EvidenceCollection evidenceCollection = null;
			if (providedSecurityInfo != null || creatorsSecurityInfo != null)
			{
				HostSecurityManager hostSecurityManager = ((AppDomain.CurrentDomain.DomainManager != null) ? AppDomain.CurrentDomain.DomainManager.HostSecurityManager : null);
				if (hostSecurityManager == null || !(hostSecurityManager.GetType() != typeof(HostSecurityManager)) || (hostSecurityManager.Flags & HostSecurityManagerOptions.HostAppDomainEvidence) != HostSecurityManagerOptions.HostAppDomainEvidence)
				{
					if (providedSecurityInfo != null && providedSecurityInfo.IsUnmodified && providedSecurityInfo.Target != null && providedSecurityInfo.Target is AppDomainEvidenceFactory)
					{
						providedSecurityInfo = null;
						flag = true;
					}
					if (creatorsSecurityInfo != null && creatorsSecurityInfo.IsUnmodified && creatorsSecurityInfo.Target != null && creatorsSecurityInfo.Target is AppDomainEvidenceFactory)
					{
						creatorsSecurityInfo = null;
						flag = true;
					}
				}
			}
			if (providedSecurityInfo != null || creatorsSecurityInfo != null)
			{
				evidenceCollection = new AppDomain.EvidenceCollection();
				evidenceCollection.ProvidedSecurityInfo = providedSecurityInfo;
				evidenceCollection.CreatorsSecurityInfo = creatorsSecurityInfo;
			}
			if (evidenceCollection != null)
			{
				array = CrossAppDomainSerializer.SerializeObject(evidenceCollection).GetBuffer();
			}
			AppDomainInitializerInfo appDomainInitializerInfo = null;
			if (setup != null && setup.AppDomainInitializer != null)
			{
				appDomainInitializerInfo = new AppDomainInitializerInfo(setup.AppDomainInitializer);
			}
			AppDomainSetup appDomainSetup = new AppDomainSetup(setup, false);
			return new object[] { friendlyName, appDomainSetup, parentSecurityDescriptor, flag, array, appDomainInitializerInfo, sandboxName, propertyNames, propertyValues };
		}

		// Token: 0x06000845 RID: 2117 RVA: 0x0001C928 File Offset: 0x0001AB28
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static object Setup(object arg)
		{
			object[] array = (object[])arg;
			string friendlyName = (string)array[0];
			AppDomainSetup appDomainSetup = (AppDomainSetup)array[1];
			IntPtr parentSecurityDescriptor = (IntPtr)array[2];
			bool generateDefaultEvidence = (bool)array[3];
			byte[] array2 = (byte[])array[4];
			AppDomainInitializerInfo appDomainInitializerInfo = (AppDomainInitializerInfo)array[5];
			string text = (string)array[6];
			string[] array3 = (string[])array[7];
			string[] array4 = (string[])array[8];
			Evidence evidence = null;
			Evidence creatorsSecurityInfo = null;
			AppDomain currentDomain = AppDomain.CurrentDomain;
			AppDomainSetup appDomainSetup2 = new AppDomainSetup(appDomainSetup, false);
			if (array3 != null && array4 != null)
			{
				for (int i = 0; i < array3.Length; i++)
				{
					if (array3[i] == "APPBASE")
					{
						if (array4[i] == null)
						{
							throw new ArgumentNullException("APPBASE");
						}
						if (Path.IsRelative(array4[i]))
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"));
						}
						appDomainSetup2.ApplicationBase = AppDomain.NormalizePath(array4[i], true);
					}
					else if (array3[i] == "LOCATION_URI" && evidence == null)
					{
						evidence = new Evidence();
						evidence.AddHostEvidence<Url>(new Url(array4[i]));
						currentDomain.SetDataHelper(array3[i], array4[i], null);
					}
					else if (array3[i] == "LOADER_OPTIMIZATION")
					{
						if (array4[i] == null)
						{
							throw new ArgumentNullException("LOADER_OPTIMIZATION");
						}
						string a = array4[i];
						if (!(a == "SingleDomain"))
						{
							if (!(a == "MultiDomain"))
							{
								if (!(a == "MultiDomainHost"))
								{
									if (!(a == "NotSpecified"))
									{
										throw new ArgumentException(Environment.GetResourceString("Argument_UnrecognizedLoaderOptimization"), "LOADER_OPTIMIZATION");
									}
									appDomainSetup2.LoaderOptimization = LoaderOptimization.NotSpecified;
								}
								else
								{
									appDomainSetup2.LoaderOptimization = LoaderOptimization.MultiDomainHost;
								}
							}
							else
							{
								appDomainSetup2.LoaderOptimization = LoaderOptimization.MultiDomain;
							}
						}
						else
						{
							appDomainSetup2.LoaderOptimization = LoaderOptimization.SingleDomain;
						}
					}
				}
			}
			AppDomainSortingSetupInfo appDomainSortingSetupInfo = appDomainSetup2._AppDomainSortingSetupInfo;
			if (appDomainSortingSetupInfo != null && (appDomainSortingSetupInfo._pfnIsNLSDefinedString == IntPtr.Zero || appDomainSortingSetupInfo._pfnCompareStringEx == IntPtr.Zero || appDomainSortingSetupInfo._pfnLCMapStringEx == IntPtr.Zero || appDomainSortingSetupInfo._pfnFindNLSStringEx == IntPtr.Zero || appDomainSortingSetupInfo._pfnCompareStringOrdinal == IntPtr.Zero || appDomainSortingSetupInfo._pfnGetNLSVersionEx == IntPtr.Zero) && (!(appDomainSortingSetupInfo._pfnIsNLSDefinedString == IntPtr.Zero) || !(appDomainSortingSetupInfo._pfnCompareStringEx == IntPtr.Zero) || !(appDomainSortingSetupInfo._pfnLCMapStringEx == IntPtr.Zero) || !(appDomainSortingSetupInfo._pfnFindNLSStringEx == IntPtr.Zero) || !(appDomainSortingSetupInfo._pfnCompareStringOrdinal == IntPtr.Zero) || !(appDomainSortingSetupInfo._pfnGetNLSVersionEx == IntPtr.Zero)))
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentException_NotAllCustomSortingFuncsDefined"));
			}
			currentDomain.SetupFusionStore(appDomainSetup2, null);
			AppDomainSetup fusionStore = currentDomain.FusionStore;
			if (array2 != null)
			{
				AppDomain.EvidenceCollection evidenceCollection = (AppDomain.EvidenceCollection)CrossAppDomainSerializer.DeserializeObject(new MemoryStream(array2));
				evidence = evidenceCollection.ProvidedSecurityInfo;
				creatorsSecurityInfo = evidenceCollection.CreatorsSecurityInfo;
			}
			currentDomain.nSetupFriendlyName(friendlyName);
			if (appDomainSetup != null && appDomainSetup.SandboxInterop)
			{
				currentDomain.nSetDisableInterfaceCache();
			}
			if (fusionStore.AppDomainManagerAssembly != null && fusionStore.AppDomainManagerType != null)
			{
				currentDomain.SetAppDomainManagerType(fusionStore.AppDomainManagerAssembly, fusionStore.AppDomainManagerType);
			}
			currentDomain.PartialTrustVisibleAssemblies = fusionStore.PartialTrustVisibleAssemblies;
			currentDomain.CreateAppDomainManager();
			currentDomain.InitializeDomainSecurity(evidence, creatorsSecurityInfo, generateDefaultEvidence, parentSecurityDescriptor, true);
			if (appDomainInitializerInfo != null)
			{
				fusionStore.AppDomainInitializer = appDomainInitializerInfo.Unwrap();
			}
			AppDomain.RunInitializer(fusionStore);
			ObjectHandle obj = null;
			if (fusionStore.ActivationArguments != null && fusionStore.ActivationArguments.ActivateInstance)
			{
				obj = Activator.CreateInstance(currentDomain.ActivationContext);
			}
			return RemotingServices.MarshalInternal(obj, null, null);
		}

		// Token: 0x06000846 RID: 2118 RVA: 0x0001CD00 File Offset: 0x0001AF00
		[SecuritySafeCritical]
		internal static string NormalizePath(string path, bool fullCheck)
		{
			return Path.LegacyNormalizePath(path, fullCheck, 260, true);
		}

		// Token: 0x06000847 RID: 2119 RVA: 0x0001CD10 File Offset: 0x0001AF10
		[SecuritySafeCritical]
		[PermissionSet(SecurityAction.Assert, Unrestricted = true)]
		private bool IsAssemblyOnAptcaVisibleList(RuntimeAssembly assembly)
		{
			if (this._aptcaVisibleAssemblies == null)
			{
				return false;
			}
			AssemblyName name = assembly.GetName();
			string text = name.GetNameWithPublicKey();
			text = text.ToUpperInvariant();
			int num = Array.BinarySearch<string>(this._aptcaVisibleAssemblies, text, StringComparer.OrdinalIgnoreCase);
			return num >= 0;
		}

		// Token: 0x06000848 RID: 2120 RVA: 0x0001CD58 File Offset: 0x0001AF58
		[SecurityCritical]
		private unsafe bool IsAssemblyOnAptcaVisibleListRaw(char* namePtr, int nameLen, byte* keyTokenPtr, int keyTokenLen)
		{
			if (this._aptcaVisibleAssemblies == null)
			{
				return false;
			}
			string name = new string(namePtr, 0, nameLen);
			byte[] array = new byte[keyTokenLen];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = keyTokenPtr[i];
			}
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = name;
			assemblyName.SetPublicKeyToken(array);
			bool result;
			try
			{
				int num = Array.BinarySearch(this._aptcaVisibleAssemblies, assemblyName, new AppDomain.CAPTCASearcher());
				result = num >= 0;
			}
			catch (InvalidOperationException)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000849 RID: 2121 RVA: 0x0001CDE0 File Offset: 0x0001AFE0
		[SecurityCritical]
		private void SetupDomain(bool allowRedirects, string path, string configFile, string[] propertyNames, string[] propertyValues)
		{
			lock (this)
			{
				if (this._FusionStore == null)
				{
					AppDomainSetup appDomainSetup = new AppDomainSetup();
					appDomainSetup.SetupDefaults(RuntimeEnvironment.GetModuleFileName(), true);
					if (path != null)
					{
						appDomainSetup.Value[0] = path;
					}
					if (configFile != null)
					{
						appDomainSetup.Value[1] = configFile;
					}
					if (!allowRedirects)
					{
						appDomainSetup.DisallowBindingRedirects = true;
					}
					if (propertyNames != null)
					{
						for (int i = 0; i < propertyNames.Length; i++)
						{
							if (string.Equals(propertyNames[i], "PARTIAL_TRUST_VISIBLE_ASSEMBLIES", StringComparison.Ordinal) && propertyValues[i] != null)
							{
								if (propertyValues[i].Length > 0)
								{
									appDomainSetup.PartialTrustVisibleAssemblies = propertyValues[i].Split(new char[] { ';' });
								}
								else
								{
									appDomainSetup.PartialTrustVisibleAssemblies = new string[0];
								}
							}
						}
					}
					this.PartialTrustVisibleAssemblies = appDomainSetup.PartialTrustVisibleAssemblies;
					this.SetupFusionStore(appDomainSetup, null);
				}
			}
		}

		// Token: 0x0600084A RID: 2122 RVA: 0x0001CEC8 File Offset: 0x0001B0C8
		[SecurityCritical]
		private void SetupLoaderOptimization(LoaderOptimization policy)
		{
			if (policy != LoaderOptimization.NotSpecified)
			{
				this.FusionStore.LoaderOptimization = policy;
				this.UpdateLoaderOptimization(this.FusionStore.LoaderOptimization);
			}
		}

		// Token: 0x0600084B RID: 2123
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetFusionContext();

		// Token: 0x0600084C RID: 2124
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetSecurityDescriptor();

		// Token: 0x0600084D RID: 2125
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern AppDomain nCreateDomain(string friendlyName, AppDomainSetup setup, Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, IntPtr parentSecurityDescriptor);

		// Token: 0x0600084E RID: 2126
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern ObjRef nCreateInstance(string friendlyName, AppDomainSetup setup, Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, IntPtr parentSecurityDescriptor);

		// Token: 0x0600084F RID: 2127 RVA: 0x0001CEEC File Offset: 0x0001B0EC
		[SecurityCritical]
		private void SetupDomainSecurity(Evidence appDomainEvidence, IntPtr creatorsSecurityDescriptor, bool publishAppDomain)
		{
			Evidence evidence = appDomainEvidence;
			AppDomain.SetupDomainSecurity(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<Evidence>(ref evidence), creatorsSecurityDescriptor, publishAppDomain);
		}

		// Token: 0x06000850 RID: 2128
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SetupDomainSecurity(AppDomainHandle appDomain, ObjectHandleOnStack appDomainEvidence, IntPtr creatorsSecurityDescriptor, [MarshalAs(UnmanagedType.Bool)] bool publishAppDomain);

		// Token: 0x06000851 RID: 2129
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void nSetupFriendlyName(string friendlyName);

		// Token: 0x06000852 RID: 2130
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void nSetDisableInterfaceCache();

		// Token: 0x06000853 RID: 2131
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern void UpdateLoaderOptimization(LoaderOptimization optimization);

		// Token: 0x06000854 RID: 2132 RVA: 0x0001CF0F File Offset: 0x0001B10F
		[SecurityCritical]
		[Obsolete("AppDomain.SetShadowCopyPath has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyDirectories instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void SetShadowCopyPath(string path)
		{
			this.InternalSetShadowCopyPath(path);
		}

		// Token: 0x06000855 RID: 2133 RVA: 0x0001CF18 File Offset: 0x0001B118
		[SecurityCritical]
		[Obsolete("AppDomain.SetShadowCopyFiles has been deprecated. Please investigate the use of AppDomainSetup.ShadowCopyFiles instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void SetShadowCopyFiles()
		{
			this.InternalSetShadowCopyFiles();
		}

		// Token: 0x06000856 RID: 2134 RVA: 0x0001CF20 File Offset: 0x0001B120
		[SecurityCritical]
		[Obsolete("AppDomain.SetDynamicBase has been deprecated. Please investigate the use of AppDomainSetup.DynamicBase instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void SetDynamicBase(string path)
		{
			this.InternalSetDynamicBase(path);
		}

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x06000857 RID: 2135 RVA: 0x0001CF29 File Offset: 0x0001B129
		public AppDomainSetup SetupInformation
		{
			get
			{
				return new AppDomainSetup(this.FusionStore, true);
			}
		}

		// Token: 0x06000858 RID: 2136 RVA: 0x0001CF38 File Offset: 0x0001B138
		[SecurityCritical]
		internal void InternalSetShadowCopyPath(string path)
		{
			if (path != null)
			{
				IntPtr fusionContext = this.GetFusionContext();
				AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.ShadowCopyDirectoriesKey, path);
			}
			this.FusionStore.ShadowCopyDirectories = path;
		}

		// Token: 0x06000859 RID: 2137 RVA: 0x0001CF68 File Offset: 0x0001B168
		[SecurityCritical]
		internal void InternalSetShadowCopyFiles()
		{
			IntPtr fusionContext = this.GetFusionContext();
			AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.ShadowCopyFilesKey, "true");
			this.FusionStore.ShadowCopyFiles = "true";
		}

		// Token: 0x0600085A RID: 2138 RVA: 0x0001CF9C File Offset: 0x0001B19C
		[SecurityCritical]
		internal void InternalSetCachePath(string path)
		{
			this.FusionStore.CachePath = path;
			if (this.FusionStore.Value[9] != null)
			{
				IntPtr fusionContext = this.GetFusionContext();
				AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.CachePathKey, this.FusionStore.Value[9]);
			}
		}

		// Token: 0x0600085B RID: 2139 RVA: 0x0001CFE8 File Offset: 0x0001B1E8
		[SecurityCritical]
		internal void InternalSetPrivateBinPath(string path)
		{
			IntPtr fusionContext = this.GetFusionContext();
			AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.PrivateBinPathKey, path);
			this.FusionStore.PrivateBinPath = path;
		}

		// Token: 0x0600085C RID: 2140 RVA: 0x0001D014 File Offset: 0x0001B214
		[SecurityCritical]
		internal void InternalSetDynamicBase(string path)
		{
			this.FusionStore.DynamicBase = path;
			if (this.FusionStore.Value[2] != null)
			{
				IntPtr fusionContext = this.GetFusionContext();
				AppDomainSetup.UpdateContextProperty(fusionContext, AppDomainSetup.DynamicBaseKey, this.FusionStore.Value[2]);
			}
		}

		// Token: 0x0600085D RID: 2141
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern string IsStringInterned(string str);

		// Token: 0x0600085E RID: 2142
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern string GetOrInternString(string str);

		// Token: 0x0600085F RID: 2143
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetGrantSet(AppDomainHandle domain, ObjectHandleOnStack retGrantSet);

		// Token: 0x06000860 RID: 2144
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetIsLegacyCasPolicyEnabled(AppDomainHandle domain);

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x06000861 RID: 2145 RVA: 0x0001D05C File Offset: 0x0001B25C
		public PermissionSet PermissionSet
		{
			[SecurityCritical]
			get
			{
				PermissionSet permissionSet = null;
				AppDomain.GetGrantSet(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet));
				if (permissionSet != null)
				{
					return permissionSet.Copy();
				}
				return new PermissionSet(PermissionState.Unrestricted);
			}
		}

		// Token: 0x170000DC RID: 220
		// (get) Token: 0x06000862 RID: 2146 RVA: 0x0001D090 File Offset: 0x0001B290
		public bool IsFullyTrusted
		{
			[SecuritySafeCritical]
			get
			{
				PermissionSet permissionSet = null;
				AppDomain.GetGrantSet(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet));
				return permissionSet == null || permissionSet.IsUnrestricted();
			}
		}

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x06000863 RID: 2147 RVA: 0x0001D0BC File Offset: 0x0001B2BC
		public bool IsHomogenous
		{
			get
			{
				return this._IsFastFullTrustDomain || this._applicationTrust != null;
			}
		}

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x06000864 RID: 2148 RVA: 0x0001D0D1 File Offset: 0x0001B2D1
		internal bool IsLegacyCasPolicyEnabled
		{
			[SecuritySafeCritical]
			get
			{
				return AppDomain.GetIsLegacyCasPolicyEnabled(this.GetNativeHandle());
			}
		}

		// Token: 0x06000865 RID: 2149 RVA: 0x0001D0E0 File Offset: 0x0001B2E0
		[SecuritySafeCritical]
		internal PermissionSet GetHomogenousGrantSet(Evidence evidence)
		{
			if (this._IsFastFullTrustDomain)
			{
				return new PermissionSet(PermissionState.Unrestricted);
			}
			if (evidence.GetDelayEvaluatedHostEvidence<StrongName>() != null)
			{
				foreach (StrongName strongName in this.ApplicationTrust.FullTrustAssemblies)
				{
					StrongNameMembershipCondition strongNameMembershipCondition = new StrongNameMembershipCondition(strongName.PublicKey, strongName.Name, strongName.Version);
					object obj = null;
					if (((IReportMatchMembershipCondition)strongNameMembershipCondition).Check(evidence, out obj))
					{
						IDelayEvaluatedEvidence delayEvaluatedEvidence = obj as IDelayEvaluatedEvidence;
						if (obj != null)
						{
							delayEvaluatedEvidence.MarkUsed();
						}
						return new PermissionSet(PermissionState.Unrestricted);
					}
				}
			}
			return this.ApplicationTrust.DefaultGrantSet.PermissionSet.Copy();
		}

		// Token: 0x06000866 RID: 2150
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void nChangeSecurityPolicy();

		// Token: 0x06000867 RID: 2151
		[SecurityCritical]
		[ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.MayFail)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void nUnload(int domainInternal);

		// Token: 0x06000868 RID: 2152 RVA: 0x0001D19C File Offset: 0x0001B39C
		public object CreateInstanceAndUnwrap(string assemblyName, string typeName)
		{
			ObjectHandle objectHandle = this.CreateInstance(assemblyName, typeName);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x06000869 RID: 2153 RVA: 0x0001D1C0 File Offset: 0x0001B3C0
		public object CreateInstanceAndUnwrap(string assemblyName, string typeName, object[] activationAttributes)
		{
			ObjectHandle objectHandle = this.CreateInstance(assemblyName, typeName, activationAttributes);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x0600086A RID: 2154 RVA: 0x0001D1E4 File Offset: 0x0001B3E4
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceAndUnwrap which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
		{
			ObjectHandle objectHandle = this.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x0600086B RID: 2155 RVA: 0x0001D214 File Offset: 0x0001B414
		public object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
		{
			ObjectHandle objectHandle = this.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x0600086C RID: 2156 RVA: 0x0001D240 File Offset: 0x0001B440
		public object CreateInstanceFromAndUnwrap(string assemblyName, string typeName)
		{
			ObjectHandle objectHandle = this.CreateInstanceFrom(assemblyName, typeName);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x0600086D RID: 2157 RVA: 0x0001D264 File Offset: 0x0001B464
		public object CreateInstanceFromAndUnwrap(string assemblyName, string typeName, object[] activationAttributes)
		{
			ObjectHandle objectHandle = this.CreateInstanceFrom(assemblyName, typeName, activationAttributes);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x0600086E RID: 2158 RVA: 0x0001D288 File Offset: 0x0001B488
		[Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceFromAndUnwrap which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		public object CreateInstanceFromAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes)
		{
			ObjectHandle objectHandle = this.CreateInstanceFrom(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x0600086F RID: 2159 RVA: 0x0001D2B8 File Offset: 0x0001B4B8
		public object CreateInstanceFromAndUnwrap(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
		{
			ObjectHandle objectHandle = this.CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
			if (objectHandle == null)
			{
				return null;
			}
			return objectHandle.Unwrap();
		}

		// Token: 0x170000DF RID: 223
		// (get) Token: 0x06000870 RID: 2160 RVA: 0x0001D2E4 File Offset: 0x0001B4E4
		public int Id
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.GetId();
			}
		}

		// Token: 0x06000871 RID: 2161
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern int GetId();

		// Token: 0x06000872 RID: 2162 RVA: 0x0001D2EC File Offset: 0x0001B4EC
		public bool IsDefaultAppDomain()
		{
			return this.GetId() == 1;
		}

		// Token: 0x06000873 RID: 2163 RVA: 0x0001D2FC File Offset: 0x0001B4FC
		private static AppDomainSetup InternalCreateDomainSetup(string imageLocation)
		{
			int num = imageLocation.LastIndexOf('\\');
			AppDomainSetup appDomainSetup = new AppDomainSetup();
			appDomainSetup.ApplicationBase = imageLocation.Substring(0, num + 1);
			StringBuilder stringBuilder = new StringBuilder(imageLocation.Substring(num + 1));
			stringBuilder.Append(AppDomainSetup.ConfigurationExtension);
			appDomainSetup.ConfigurationFile = stringBuilder.ToString();
			return appDomainSetup;
		}

		// Token: 0x06000874 RID: 2164 RVA: 0x0001D350 File Offset: 0x0001B550
		private static AppDomain InternalCreateDomain(string imageLocation)
		{
			AppDomainSetup info = AppDomain.InternalCreateDomainSetup(imageLocation);
			return AppDomain.CreateDomain("Validator", null, info);
		}

		// Token: 0x06000875 RID: 2165
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nEnableMonitoring();

		// Token: 0x06000876 RID: 2166
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool nMonitoringIsEnabled();

		// Token: 0x06000877 RID: 2167
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern long nGetTotalProcessorTime();

		// Token: 0x06000878 RID: 2168
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern long nGetTotalAllocatedMemorySize();

		// Token: 0x06000879 RID: 2169
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern long nGetLastSurvivedMemorySize();

		// Token: 0x0600087A RID: 2170
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern long nGetLastSurvivedProcessMemorySize();

		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x0600087B RID: 2171 RVA: 0x0001D370 File Offset: 0x0001B570
		// (set) Token: 0x0600087C RID: 2172 RVA: 0x0001D377 File Offset: 0x0001B577
		public static bool MonitoringIsEnabled
		{
			[SecurityCritical]
			get
			{
				return AppDomain.nMonitoringIsEnabled();
			}
			[SecurityCritical]
			set
			{
				if (!value)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_MustBeTrue"));
				}
				AppDomain.nEnableMonitoring();
			}
		}

		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x0600087D RID: 2173 RVA: 0x0001D394 File Offset: 0x0001B594
		public TimeSpan MonitoringTotalProcessorTime
		{
			[SecurityCritical]
			get
			{
				long num = this.nGetTotalProcessorTime();
				if (num == -1L)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
				}
				return new TimeSpan(num);
			}
		}

		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x0600087E RID: 2174 RVA: 0x0001D3C4 File Offset: 0x0001B5C4
		public long MonitoringTotalAllocatedMemorySize
		{
			[SecurityCritical]
			get
			{
				long num = this.nGetTotalAllocatedMemorySize();
				if (num == -1L)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
				}
				return num;
			}
		}

		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x0600087F RID: 2175 RVA: 0x0001D3F0 File Offset: 0x0001B5F0
		public long MonitoringSurvivedMemorySize
		{
			[SecurityCritical]
			get
			{
				long num = this.nGetLastSurvivedMemorySize();
				if (num == -1L)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
				}
				return num;
			}
		}

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000880 RID: 2176 RVA: 0x0001D41C File Offset: 0x0001B61C
		public static long MonitoringSurvivedProcessMemorySize
		{
			[SecurityCritical]
			get
			{
				long num = AppDomain.nGetLastSurvivedProcessMemorySize();
				if (num == -1L)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
				}
				return num;
			}
		}

		// Token: 0x06000881 RID: 2177 RVA: 0x0001D445 File Offset: 0x0001B645
		[SecurityCritical]
		private void InternalSetDomainContext(string imageLocation)
		{
			this.SetupFusionStore(AppDomain.InternalCreateDomainSetup(imageLocation), null);
		}

		// Token: 0x06000882 RID: 2178 RVA: 0x0001D454 File Offset: 0x0001B654
		[__DynamicallyInvokable]
		public new Type GetType()
		{
			return base.GetType();
		}

		// Token: 0x06000883 RID: 2179 RVA: 0x0001D45C File Offset: 0x0001B65C
		void _AppDomain.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000884 RID: 2180 RVA: 0x0001D463 File Offset: 0x0001B663
		void _AppDomain.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000885 RID: 2181 RVA: 0x0001D46A File Offset: 0x0001B66A
		void _AppDomain.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000886 RID: 2182 RVA: 0x0001D471 File Offset: 0x0001B671
		void _AppDomain.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000887 RID: 2183 RVA: 0x0001D478 File Offset: 0x0001B678
		internal static bool IsStillInEarlyInit()
		{
			return !AppDomain.InitState.EarlyInitIsComplete();
		}

		// Token: 0x0400037E RID: 894
		[SecurityCritical]
		private AppDomainManager _domainManager;

		// Token: 0x0400037F RID: 895
		private Dictionary<string, object[]> _LocalStore;

		// Token: 0x04000380 RID: 896
		private AppDomainSetup _FusionStore;

		// Token: 0x04000381 RID: 897
		private Evidence _SecurityIdentity;

		// Token: 0x04000382 RID: 898
		private object[] _Policies;

		// Token: 0x04000384 RID: 900
		[SecurityCritical]
		private ResolveEventHandler _TypeResolve;

		// Token: 0x04000385 RID: 901
		[SecurityCritical]
		private ResolveEventHandler _ResourceResolve;

		// Token: 0x04000386 RID: 902
		[SecurityCritical]
		private ResolveEventHandler _AssemblyResolve;

		// Token: 0x04000388 RID: 904
		private Context _DefaultContext;

		// Token: 0x04000389 RID: 905
		private ActivationContext _activationContext;

		// Token: 0x0400038A RID: 906
		private ApplicationIdentity _applicationIdentity;

		// Token: 0x0400038B RID: 907
		private ApplicationTrust _applicationTrust;

		// Token: 0x0400038C RID: 908
		private IPrincipal _DefaultPrincipal;

		// Token: 0x0400038D RID: 909
		private DomainSpecificRemotingData _RemotingData;

		// Token: 0x0400038E RID: 910
		private EventHandler _processExit;

		// Token: 0x0400038F RID: 911
		private EventHandler _domainUnload;

		// Token: 0x04000390 RID: 912
		private UnhandledExceptionEventHandler _unhandledException;

		// Token: 0x04000391 RID: 913
		private string[] _aptcaVisibleAssemblies;

		// Token: 0x04000392 RID: 914
		private Dictionary<string, object> _compatFlags;

		// Token: 0x04000393 RID: 915
		private EventHandler<FirstChanceExceptionEventArgs> _firstChanceException;

		// Token: 0x04000394 RID: 916
		private IntPtr _pDomain;

		// Token: 0x04000395 RID: 917
		private PrincipalPolicy _PrincipalPolicy;

		// Token: 0x04000396 RID: 918
		private bool _HasSetPolicy;

		// Token: 0x04000397 RID: 919
		private bool _IsFastFullTrustDomain;

		// Token: 0x04000398 RID: 920
		private bool _compatFlagsInitialized;

		// Token: 0x04000399 RID: 921
		internal const string TargetFrameworkNameAppCompatSetting = "TargetFrameworkName";

		// Token: 0x0400039A RID: 922
		private static AppDomain.APPX_FLAGS s_flags;

		// Token: 0x0400039B RID: 923
		internal const int DefaultADID = 1;

		// Token: 0x02000ACB RID: 2763
		[Flags]
		private enum APPX_FLAGS
		{
			// Token: 0x040030EF RID: 12527
			APPX_FLAGS_INITIALIZED = 1,
			// Token: 0x040030F0 RID: 12528
			APPX_FLAGS_APPX_MODEL = 2,
			// Token: 0x040030F1 RID: 12529
			APPX_FLAGS_APPX_DESIGN_MODE = 4,
			// Token: 0x040030F2 RID: 12530
			APPX_FLAGS_APPX_NGEN = 8,
			// Token: 0x040030F3 RID: 12531
			APPX_FLAGS_APPX_MASK = 14,
			// Token: 0x040030F4 RID: 12532
			APPX_FLAGS_API_CHECK = 16
		}

		// Token: 0x02000ACC RID: 2764
		private class NamespaceResolverForIntrospection
		{
			// Token: 0x060069D0 RID: 27088 RVA: 0x0016CD84 File Offset: 0x0016AF84
			public NamespaceResolverForIntrospection(IEnumerable<string> packageGraphFilePaths)
			{
				this._packageGraphFilePaths = packageGraphFilePaths;
			}

			// Token: 0x060069D1 RID: 27089 RVA: 0x0016CD94 File Offset: 0x0016AF94
			[SecurityCritical]
			public void ResolveNamespace(object sender, NamespaceResolveEventArgs args)
			{
				IEnumerable<string> enumerable = WindowsRuntimeMetadata.ResolveNamespace(args.NamespaceName, null, this._packageGraphFilePaths);
				foreach (string assemblyFile in enumerable)
				{
					args.ResolvedAssemblies.Add(Assembly.ReflectionOnlyLoadFrom(assemblyFile));
				}
			}

			// Token: 0x040030F5 RID: 12533
			private IEnumerable<string> _packageGraphFilePaths;
		}

		// Token: 0x02000ACD RID: 2765
		[Serializable]
		private class EvidenceCollection
		{
			// Token: 0x040030F6 RID: 12534
			public Evidence ProvidedSecurityInfo;

			// Token: 0x040030F7 RID: 12535
			public Evidence CreatorsSecurityInfo;
		}

		// Token: 0x02000ACE RID: 2766
		private class CAPTCASearcher : IComparer
		{
			// Token: 0x060069D3 RID: 27091 RVA: 0x0016CE04 File Offset: 0x0016B004
			int IComparer.Compare(object lhs, object rhs)
			{
				AssemblyName assemblyName = new AssemblyName((string)lhs);
				AssemblyName assemblyName2 = (AssemblyName)rhs;
				int num = string.Compare(assemblyName.Name, assemblyName2.Name, StringComparison.OrdinalIgnoreCase);
				if (num != 0)
				{
					return num;
				}
				byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
				byte[] publicKeyToken2 = assemblyName2.GetPublicKeyToken();
				if (publicKeyToken == null)
				{
					return -1;
				}
				if (publicKeyToken2 == null)
				{
					return 1;
				}
				if (publicKeyToken.Length < publicKeyToken2.Length)
				{
					return -1;
				}
				if (publicKeyToken.Length > publicKeyToken2.Length)
				{
					return 1;
				}
				for (int i = 0; i < publicKeyToken.Length; i++)
				{
					byte b = publicKeyToken[i];
					byte b2 = publicKeyToken2[i];
					if (b < b2)
					{
						return -1;
					}
					if (b > b2)
					{
						return 1;
					}
				}
				return 0;
			}
		}

		// Token: 0x02000ACF RID: 2767
		private static class InitState
		{
			// Token: 0x060069D5 RID: 27093 RVA: 0x0016CEA4 File Offset: 0x0016B0A4
			internal static bool EarlyInitIsComplete()
			{
				return AppDomain.InitState.s_EarlyInitIsComplete;
			}

			// Token: 0x060069D6 RID: 27094 RVA: 0x0016CEAB File Offset: 0x0016B0AB
			internal static void RecordEndOfEarlyAppDomainInit()
			{
				AppDomain.InitState.s_EarlyInitIsComplete = true;
			}

			// Token: 0x040030F8 RID: 12536
			private static bool s_EarlyInitIsComplete;
		}
	}
}
