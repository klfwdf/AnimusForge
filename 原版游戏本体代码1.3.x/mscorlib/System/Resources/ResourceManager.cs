using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Resources
{
	// Token: 0x02000397 RID: 919
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class ResourceManager
	{
		// Token: 0x06002D1B RID: 11547 RVA: 0x000AA3F4 File Offset: 0x000A85F4
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Init()
		{
			this.m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
		}

		// Token: 0x06002D1C RID: 11548 RVA: 0x000AA408 File Offset: 0x000A8608
		protected ResourceManager()
		{
			this.Init();
			this._lastUsedResourceCache = new ResourceManager.CultureNameResourceSetPair();
			ResourceManager.ResourceManagerMediator mediator = new ResourceManager.ResourceManagerMediator(this);
			this.resourceGroveler = new ManifestBasedResourceGroveler(mediator);
		}

		// Token: 0x06002D1D RID: 11549 RVA: 0x000AA440 File Offset: 0x000A8640
		private ResourceManager(string baseName, string resourceDir, Type usingResourceSet)
		{
			if (baseName == null)
			{
				throw new ArgumentNullException("baseName");
			}
			if (resourceDir == null)
			{
				throw new ArgumentNullException("resourceDir");
			}
			this.BaseNameField = baseName;
			this.moduleDir = resourceDir;
			this._userResourceSet = usingResourceSet;
			this.ResourceSets = new Hashtable();
			this._resourceSets = new Dictionary<string, ResourceSet>();
			this._lastUsedResourceCache = new ResourceManager.CultureNameResourceSetPair();
			this.UseManifest = false;
			ResourceManager.ResourceManagerMediator mediator = new ResourceManager.ResourceManagerMediator(this);
			this.resourceGroveler = new FileBasedResourceGroveler(mediator);
			if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled())
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				string resourceFileName = this.GetResourceFileName(invariantCulture);
				if (this.resourceGroveler.HasNeutralResources(invariantCulture, resourceFileName))
				{
					FrameworkEventSource.Log.ResourceManagerNeutralResourcesFound(this.BaseNameField, this.MainAssembly, resourceFileName);
					return;
				}
				FrameworkEventSource.Log.ResourceManagerNeutralResourcesNotFound(this.BaseNameField, this.MainAssembly, resourceFileName);
			}
		}

		// Token: 0x06002D1E RID: 11550 RVA: 0x000AA520 File Offset: 0x000A8720
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ResourceManager(string baseName, Assembly assembly)
		{
			if (baseName == null)
			{
				throw new ArgumentNullException("baseName");
			}
			if (null == assembly)
			{
				throw new ArgumentNullException("assembly");
			}
			if (!(assembly is RuntimeAssembly))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
			}
			this.MainAssembly = assembly;
			this.BaseNameField = baseName;
			this.SetAppXConfiguration();
			this.CommonAssemblyInit();
			this.m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
			if (assembly == typeof(object).Assembly && this.m_callingAssembly != assembly)
			{
				this.m_callingAssembly = null;
			}
		}

		// Token: 0x06002D1F RID: 11551 RVA: 0x000AA5C4 File Offset: 0x000A87C4
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ResourceManager(string baseName, Assembly assembly, Type usingResourceSet)
		{
			if (baseName == null)
			{
				throw new ArgumentNullException("baseName");
			}
			if (null == assembly)
			{
				throw new ArgumentNullException("assembly");
			}
			if (!(assembly is RuntimeAssembly))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
			}
			this.MainAssembly = assembly;
			this.BaseNameField = baseName;
			if (usingResourceSet != null && usingResourceSet != ResourceManager._minResourceSet && !usingResourceSet.IsSubclassOf(ResourceManager._minResourceSet))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ResMgrNotResSet"), "usingResourceSet");
			}
			this._userResourceSet = usingResourceSet;
			this.CommonAssemblyInit();
			this.m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
			if (assembly == typeof(object).Assembly && this.m_callingAssembly != assembly)
			{
				this.m_callingAssembly = null;
			}
		}

		// Token: 0x06002D20 RID: 11552 RVA: 0x000AA6A0 File Offset: 0x000A88A0
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ResourceManager(Type resourceSource)
		{
			if (null == resourceSource)
			{
				throw new ArgumentNullException("resourceSource");
			}
			if (!(resourceSource is RuntimeType))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			this._locationInfo = resourceSource;
			this.MainAssembly = this._locationInfo.Assembly;
			this.BaseNameField = resourceSource.Name;
			this.SetAppXConfiguration();
			this.CommonAssemblyInit();
			this.m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
			if (this.MainAssembly == typeof(object).Assembly && this.m_callingAssembly != this.MainAssembly)
			{
				this.m_callingAssembly = null;
			}
		}

		// Token: 0x06002D21 RID: 11553 RVA: 0x000AA755 File Offset: 0x000A8955
		[OnDeserializing]
		private void OnDeserializing(StreamingContext ctx)
		{
			this._resourceSets = null;
			this.resourceGroveler = null;
			this._lastUsedResourceCache = null;
		}

		// Token: 0x06002D22 RID: 11554 RVA: 0x000AA76C File Offset: 0x000A896C
		[SecuritySafeCritical]
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			this._resourceSets = new Dictionary<string, ResourceSet>();
			this._lastUsedResourceCache = new ResourceManager.CultureNameResourceSetPair();
			ResourceManager.ResourceManagerMediator mediator = new ResourceManager.ResourceManagerMediator(this);
			if (this.UseManifest)
			{
				this.resourceGroveler = new ManifestBasedResourceGroveler(mediator);
			}
			else
			{
				this.resourceGroveler = new FileBasedResourceGroveler(mediator);
			}
			if (this.m_callingAssembly == null)
			{
				this.m_callingAssembly = (RuntimeAssembly)this._callingAssembly;
			}
			if (this.UseManifest && this._neutralResourcesCulture == null)
			{
				this._neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(this.MainAssembly, ref this._fallbackLoc);
			}
		}

		// Token: 0x06002D23 RID: 11555 RVA: 0x000AA7FE File Offset: 0x000A89FE
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this._callingAssembly = this.m_callingAssembly;
			this.UseSatelliteAssem = this.UseManifest;
			this.ResourceSets = new Hashtable();
		}

		// Token: 0x06002D24 RID: 11556 RVA: 0x000AA824 File Offset: 0x000A8A24
		[SecuritySafeCritical]
		private void CommonAssemblyInit()
		{
			if (!this._bUsingModernResourceManagement)
			{
				this.UseManifest = true;
				this._resourceSets = new Dictionary<string, ResourceSet>();
				this._lastUsedResourceCache = new ResourceManager.CultureNameResourceSetPair();
				this._fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;
				ResourceManager.ResourceManagerMediator mediator = new ResourceManager.ResourceManagerMediator(this);
				this.resourceGroveler = new ManifestBasedResourceGroveler(mediator);
			}
			this._neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(this.MainAssembly, ref this._fallbackLoc);
			if (!this._bUsingModernResourceManagement)
			{
				if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled())
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					string resourceFileName = this.GetResourceFileName(invariantCulture);
					if (this.resourceGroveler.HasNeutralResources(invariantCulture, resourceFileName))
					{
						FrameworkEventSource.Log.ResourceManagerNeutralResourcesFound(this.BaseNameField, this.MainAssembly, resourceFileName);
					}
					else
					{
						string resName = resourceFileName;
						if (this._locationInfo != null && this._locationInfo.Namespace != null)
						{
							resName = this._locationInfo.Namespace + Type.Delimiter.ToString() + resourceFileName;
						}
						FrameworkEventSource.Log.ResourceManagerNeutralResourcesNotFound(this.BaseNameField, this.MainAssembly, resName);
					}
				}
				this.ResourceSets = new Hashtable();
			}
		}

		// Token: 0x170005E9 RID: 1513
		// (get) Token: 0x06002D25 RID: 11557 RVA: 0x000AA940 File Offset: 0x000A8B40
		public virtual string BaseName
		{
			get
			{
				return this.BaseNameField;
			}
		}

		// Token: 0x170005EA RID: 1514
		// (get) Token: 0x06002D26 RID: 11558 RVA: 0x000AA948 File Offset: 0x000A8B48
		// (set) Token: 0x06002D27 RID: 11559 RVA: 0x000AA950 File Offset: 0x000A8B50
		public virtual bool IgnoreCase
		{
			get
			{
				return this._ignoreCase;
			}
			set
			{
				this._ignoreCase = value;
			}
		}

		// Token: 0x170005EB RID: 1515
		// (get) Token: 0x06002D28 RID: 11560 RVA: 0x000AA959 File Offset: 0x000A8B59
		public virtual Type ResourceSetType
		{
			get
			{
				if (!(this._userResourceSet == null))
				{
					return this._userResourceSet;
				}
				return typeof(RuntimeResourceSet);
			}
		}

		// Token: 0x170005EC RID: 1516
		// (get) Token: 0x06002D29 RID: 11561 RVA: 0x000AA97A File Offset: 0x000A8B7A
		// (set) Token: 0x06002D2A RID: 11562 RVA: 0x000AA982 File Offset: 0x000A8B82
		protected UltimateResourceFallbackLocation FallbackLocation
		{
			get
			{
				return this._fallbackLoc;
			}
			set
			{
				this._fallbackLoc = value;
			}
		}

		// Token: 0x06002D2B RID: 11563 RVA: 0x000AA98C File Offset: 0x000A8B8C
		public virtual void ReleaseAllResources()
		{
			if (FrameworkEventSource.IsInitialized)
			{
				FrameworkEventSource.Log.ResourceManagerReleasingResources(this.BaseNameField, this.MainAssembly);
			}
			Dictionary<string, ResourceSet> resourceSets = this._resourceSets;
			this._resourceSets = new Dictionary<string, ResourceSet>();
			this._lastUsedResourceCache = new ResourceManager.CultureNameResourceSetPair();
			Dictionary<string, ResourceSet> obj = resourceSets;
			lock (obj)
			{
				IDictionaryEnumerator dictionaryEnumerator = resourceSets.GetEnumerator();
				IDictionaryEnumerator dictionaryEnumerator2 = null;
				if (this.ResourceSets != null)
				{
					dictionaryEnumerator2 = this.ResourceSets.GetEnumerator();
				}
				this.ResourceSets = new Hashtable();
				while (dictionaryEnumerator.MoveNext())
				{
					((ResourceSet)dictionaryEnumerator.Value).Close();
				}
				if (dictionaryEnumerator2 != null)
				{
					while (dictionaryEnumerator2.MoveNext())
					{
						((ResourceSet)dictionaryEnumerator2.Value).Close();
					}
				}
			}
		}

		// Token: 0x06002D2C RID: 11564 RVA: 0x000AAA64 File Offset: 0x000A8C64
		public static ResourceManager CreateFileBasedResourceManager(string baseName, string resourceDir, Type usingResourceSet)
		{
			return new ResourceManager(baseName, resourceDir, usingResourceSet);
		}

		// Token: 0x06002D2D RID: 11565 RVA: 0x000AAA70 File Offset: 0x000A8C70
		protected virtual string GetResourceFileName(CultureInfo culture)
		{
			StringBuilder stringBuilder = new StringBuilder(255);
			stringBuilder.Append(this.BaseNameField);
			if (!culture.HasInvariantCultureName)
			{
				CultureInfo.VerifyCultureName(culture.Name, true);
				stringBuilder.Append('.');
				stringBuilder.Append(culture.Name);
			}
			stringBuilder.Append(".resources");
			return stringBuilder.ToString();
		}

		// Token: 0x06002D2E RID: 11566 RVA: 0x000AAAD4 File Offset: 0x000A8CD4
		internal ResourceSet GetFirstResourceSet(CultureInfo culture)
		{
			if (this._neutralResourcesCulture != null && culture.Name == this._neutralResourcesCulture.Name)
			{
				culture = CultureInfo.InvariantCulture;
			}
			if (this._lastUsedResourceCache != null)
			{
				ResourceManager.CultureNameResourceSetPair lastUsedResourceCache = this._lastUsedResourceCache;
				lock (lastUsedResourceCache)
				{
					if (culture.Name == this._lastUsedResourceCache.lastCultureName)
					{
						return this._lastUsedResourceCache.lastResourceSet;
					}
				}
			}
			Dictionary<string, ResourceSet> resourceSets = this._resourceSets;
			ResourceSet resourceSet = null;
			if (resourceSets != null)
			{
				Dictionary<string, ResourceSet> obj = resourceSets;
				lock (obj)
				{
					resourceSets.TryGetValue(culture.Name, out resourceSet);
				}
			}
			if (resourceSet != null)
			{
				if (this._lastUsedResourceCache != null)
				{
					ResourceManager.CultureNameResourceSetPair lastUsedResourceCache2 = this._lastUsedResourceCache;
					lock (lastUsedResourceCache2)
					{
						this._lastUsedResourceCache.lastCultureName = culture.Name;
						this._lastUsedResourceCache.lastResourceSet = resourceSet;
					}
				}
				return resourceSet;
			}
			return null;
		}

		// Token: 0x06002D2F RID: 11567 RVA: 0x000AAC08 File Offset: 0x000A8E08
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public virtual ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
		{
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			Dictionary<string, ResourceSet> resourceSets = this._resourceSets;
			if (resourceSets != null)
			{
				Dictionary<string, ResourceSet> obj = resourceSets;
				lock (obj)
				{
					ResourceSet result;
					if (resourceSets.TryGetValue(culture.Name, out result))
					{
						return result;
					}
				}
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			if (this.UseManifest && culture.HasInvariantCultureName)
			{
				string resourceFileName = this.GetResourceFileName(culture);
				RuntimeAssembly runtimeAssembly = (RuntimeAssembly)this.MainAssembly;
				Stream manifestResourceStream = runtimeAssembly.GetManifestResourceStream(this._locationInfo, resourceFileName, this.m_callingAssembly == this.MainAssembly, ref stackCrawlMark);
				if (createIfNotExists && manifestResourceStream != null)
				{
					ResourceSet result = ((ManifestBasedResourceGroveler)this.resourceGroveler).CreateResourceSet(manifestResourceStream, this.MainAssembly);
					ResourceManager.AddResourceSet(resourceSets, culture.Name, ref result);
					return result;
				}
			}
			return this.InternalGetResourceSet(culture, createIfNotExists, tryParents);
		}

		// Token: 0x06002D30 RID: 11568 RVA: 0x000AACF8 File Offset: 0x000A8EF8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		protected virtual ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalGetResourceSet(culture, createIfNotExists, tryParents, ref stackCrawlMark);
		}

		// Token: 0x06002D31 RID: 11569 RVA: 0x000AAD14 File Offset: 0x000A8F14
		[SecurityCritical]
		private ResourceSet InternalGetResourceSet(CultureInfo requestedCulture, bool createIfNotExists, bool tryParents, ref StackCrawlMark stackMark)
		{
			Dictionary<string, ResourceSet> resourceSets = this._resourceSets;
			ResourceSet resourceSet = null;
			CultureInfo cultureInfo = null;
			Dictionary<string, ResourceSet> obj = resourceSets;
			lock (obj)
			{
				if (resourceSets.TryGetValue(requestedCulture.Name, out resourceSet))
				{
					if (FrameworkEventSource.IsInitialized)
					{
						FrameworkEventSource.Log.ResourceManagerFoundResourceSetInCache(this.BaseNameField, this.MainAssembly, requestedCulture.Name);
					}
					return resourceSet;
				}
			}
			ResourceFallbackManager resourceFallbackManager = new ResourceFallbackManager(requestedCulture, this._neutralResourcesCulture, tryParents);
			foreach (CultureInfo cultureInfo2 in resourceFallbackManager)
			{
				if (FrameworkEventSource.IsInitialized)
				{
					FrameworkEventSource.Log.ResourceManagerLookingForResourceSet(this.BaseNameField, this.MainAssembly, cultureInfo2.Name);
				}
				Dictionary<string, ResourceSet> obj2 = resourceSets;
				lock (obj2)
				{
					if (resourceSets.TryGetValue(cultureInfo2.Name, out resourceSet))
					{
						if (FrameworkEventSource.IsInitialized)
						{
							FrameworkEventSource.Log.ResourceManagerFoundResourceSetInCache(this.BaseNameField, this.MainAssembly, cultureInfo2.Name);
						}
						if (requestedCulture != cultureInfo2)
						{
							cultureInfo = cultureInfo2;
						}
						break;
					}
				}
				resourceSet = this.resourceGroveler.GrovelForResourceSet(cultureInfo2, resourceSets, tryParents, createIfNotExists, ref stackMark);
				if (resourceSet != null)
				{
					cultureInfo = cultureInfo2;
					break;
				}
			}
			if (resourceSet != null && cultureInfo != null)
			{
				foreach (CultureInfo cultureInfo3 in resourceFallbackManager)
				{
					ResourceManager.AddResourceSet(resourceSets, cultureInfo3.Name, ref resourceSet);
					if (cultureInfo3 == cultureInfo)
					{
						break;
					}
				}
			}
			return resourceSet;
		}

		// Token: 0x06002D32 RID: 11570 RVA: 0x000AAEDC File Offset: 0x000A90DC
		private static void AddResourceSet(Dictionary<string, ResourceSet> localResourceSets, string cultureName, ref ResourceSet rs)
		{
			lock (localResourceSets)
			{
				ResourceSet resourceSet;
				if (localResourceSets.TryGetValue(cultureName, out resourceSet))
				{
					if (resourceSet != rs)
					{
						if (!localResourceSets.ContainsValue(rs))
						{
							rs.Dispose();
						}
						rs = resourceSet;
					}
				}
				else
				{
					localResourceSets.Add(cultureName, rs);
				}
			}
		}

		// Token: 0x06002D33 RID: 11571 RVA: 0x000AAF40 File Offset: 0x000A9140
		protected static Version GetSatelliteContractVersion(Assembly a)
		{
			if (a == null)
			{
				throw new ArgumentNullException("a", Environment.GetResourceString("ArgumentNull_Assembly"));
			}
			string text = null;
			if (a.ReflectionOnly)
			{
				foreach (CustomAttributeData customAttributeData in CustomAttributeData.GetCustomAttributes(a))
				{
					if (customAttributeData.Constructor.DeclaringType == typeof(SatelliteContractVersionAttribute))
					{
						text = (string)customAttributeData.ConstructorArguments[0].Value;
						break;
					}
				}
				if (text == null)
				{
					return null;
				}
			}
			else
			{
				object[] customAttributes = a.GetCustomAttributes(typeof(SatelliteContractVersionAttribute), false);
				if (customAttributes.Length == 0)
				{
					return null;
				}
				text = ((SatelliteContractVersionAttribute)customAttributes[0]).Version;
			}
			Version result;
			try
			{
				result = new Version(text);
			}
			catch (ArgumentOutOfRangeException innerException)
			{
				if (a == typeof(object).Assembly)
				{
					return null;
				}
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidSatelliteContract_Asm_Ver", new object[]
				{
					a.ToString(),
					text
				}), innerException);
			}
			return result;
		}

		// Token: 0x06002D34 RID: 11572 RVA: 0x000AB074 File Offset: 0x000A9274
		[SecuritySafeCritical]
		protected static CultureInfo GetNeutralResourcesLanguage(Assembly a)
		{
			UltimateResourceFallbackLocation ultimateResourceFallbackLocation = UltimateResourceFallbackLocation.MainAssembly;
			return ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(a, ref ultimateResourceFallbackLocation);
		}

		// Token: 0x06002D35 RID: 11573 RVA: 0x000AB090 File Offset: 0x000A9290
		internal static bool CompareNames(string asmTypeName1, string typeName2, AssemblyName asmName2)
		{
			int num = asmTypeName1.IndexOf(',');
			if (((num == -1) ? asmTypeName1.Length : num) != typeName2.Length)
			{
				return false;
			}
			if (string.Compare(asmTypeName1, 0, typeName2, 0, typeName2.Length, StringComparison.Ordinal) != 0)
			{
				return false;
			}
			if (num == -1)
			{
				return true;
			}
			while (char.IsWhiteSpace(asmTypeName1[++num]))
			{
			}
			AssemblyName assemblyName = new AssemblyName(asmTypeName1.Substring(num));
			if (string.Compare(assemblyName.Name, asmName2.Name, StringComparison.OrdinalIgnoreCase) != 0)
			{
				return false;
			}
			if (string.Compare(assemblyName.Name, "mscorlib", StringComparison.OrdinalIgnoreCase) == 0)
			{
				return true;
			}
			if (assemblyName.CultureInfo != null && asmName2.CultureInfo != null && assemblyName.CultureInfo.LCID != asmName2.CultureInfo.LCID)
			{
				return false;
			}
			byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
			byte[] publicKeyToken2 = asmName2.GetPublicKeyToken();
			if (publicKeyToken != null && publicKeyToken2 != null)
			{
				if (publicKeyToken.Length != publicKeyToken2.Length)
				{
					return false;
				}
				for (int i = 0; i < publicKeyToken.Length; i++)
				{
					if (publicKeyToken[i] != publicKeyToken2[i])
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06002D36 RID: 11574 RVA: 0x000AB188 File Offset: 0x000A9388
		[SecuritySafeCritical]
		private string GetStringFromPRI(string stringName, string startingCulture, string neutralResourcesCulture)
		{
			if (stringName.Length == 0)
			{
				return null;
			}
			return this._WinRTResourceManager.GetString(stringName, string.IsNullOrEmpty(startingCulture) ? null : startingCulture, string.IsNullOrEmpty(neutralResourcesCulture) ? null : neutralResourcesCulture);
		}

		// Token: 0x06002D37 RID: 11575 RVA: 0x000AB1C8 File Offset: 0x000A93C8
		[SecurityCritical]
		internal static WindowsRuntimeResourceManagerBase GetWinRTResourceManager()
		{
			Type type = Type.GetType("System.Resources.WindowsRuntimeResourceManager, System.Runtime.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", true);
			return (WindowsRuntimeResourceManagerBase)Activator.CreateInstance(type, true);
		}

		// Token: 0x06002D38 RID: 11576 RVA: 0x000AB1F0 File Offset: 0x000A93F0
		[SecuritySafeCritical]
		private bool ShouldUseSatelliteAssemblyResourceLookupUnderAppX(RuntimeAssembly resourcesAssembly)
		{
			return resourcesAssembly.IsFrameworkAssembly();
		}

		// Token: 0x06002D39 RID: 11577 RVA: 0x000AB208 File Offset: 0x000A9408
		[SecuritySafeCritical]
		private void SetAppXConfiguration()
		{
			bool flag = false;
			RuntimeAssembly runtimeAssembly = (RuntimeAssembly)this.MainAssembly;
			if (runtimeAssembly == null)
			{
				runtimeAssembly = this.m_callingAssembly;
			}
			if (runtimeAssembly != null && runtimeAssembly != typeof(object).Assembly && AppDomain.IsAppXModel() && !AppDomain.IsAppXNGen)
			{
				ResourceManager.s_IsAppXModel = true;
				string text = ((this._locationInfo == null) ? this.BaseNameField : this._locationInfo.FullName);
				if (text == null)
				{
					text = string.Empty;
				}
				WindowsRuntimeResourceManagerBase windowsRuntimeResourceManagerBase = null;
				bool flag2 = false;
				if (AppDomain.IsAppXDesignMode())
				{
					windowsRuntimeResourceManagerBase = ResourceManager.GetWinRTResourceManager();
					try
					{
						PRIExceptionInfo priexceptionInfo;
						flag2 = windowsRuntimeResourceManagerBase.Initialize(runtimeAssembly.Location, text, out priexceptionInfo);
						flag = !flag2;
					}
					catch (Exception ex)
					{
						flag = true;
						if (ex.IsTransient)
						{
							throw;
						}
					}
				}
				if (!flag)
				{
					this._bUsingModernResourceManagement = !this.ShouldUseSatelliteAssemblyResourceLookupUnderAppX(runtimeAssembly);
					if (this._bUsingModernResourceManagement)
					{
						if (windowsRuntimeResourceManagerBase != null && flag2)
						{
							this._WinRTResourceManager = windowsRuntimeResourceManagerBase;
							this._PRIonAppXInitialized = true;
							return;
						}
						this._WinRTResourceManager = ResourceManager.GetWinRTResourceManager();
						try
						{
							this._PRIonAppXInitialized = this._WinRTResourceManager.Initialize(runtimeAssembly.Location, text, out this._PRIExceptionInfo);
						}
						catch (FileNotFoundException)
						{
						}
						catch (Exception ex2)
						{
							if (ex2.HResult != -2147009761)
							{
								throw;
							}
						}
					}
				}
			}
		}

		// Token: 0x06002D3A RID: 11578 RVA: 0x000AB37C File Offset: 0x000A957C
		[__DynamicallyInvokable]
		public virtual string GetString(string name)
		{
			return this.GetString(name, null);
		}

		// Token: 0x06002D3B RID: 11579 RVA: 0x000AB388 File Offset: 0x000A9588
		[__DynamicallyInvokable]
		public virtual string GetString(string name, CultureInfo culture)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (ResourceManager.s_IsAppXModel && culture == CultureInfo.CurrentUICulture)
			{
				culture = null;
			}
			if (!this._bUsingModernResourceManagement)
			{
				if (culture == null)
				{
					culture = Thread.CurrentThread.GetCurrentUICultureNoAppX();
				}
				if (FrameworkEventSource.IsInitialized)
				{
					FrameworkEventSource.Log.ResourceManagerLookupStarted(this.BaseNameField, this.MainAssembly, culture.Name);
				}
				ResourceSet resourceSet = this.GetFirstResourceSet(culture);
				if (resourceSet != null)
				{
					string @string = resourceSet.GetString(name, this._ignoreCase);
					if (@string != null)
					{
						return @string;
					}
				}
				ResourceFallbackManager resourceFallbackManager = new ResourceFallbackManager(culture, this._neutralResourcesCulture, true);
				foreach (CultureInfo cultureInfo in resourceFallbackManager)
				{
					ResourceSet resourceSet2 = this.InternalGetResourceSet(cultureInfo, true, true);
					if (resourceSet2 == null)
					{
						break;
					}
					if (resourceSet2 != resourceSet)
					{
						string string2 = resourceSet2.GetString(name, this._ignoreCase);
						if (string2 != null)
						{
							if (this._lastUsedResourceCache != null)
							{
								ResourceManager.CultureNameResourceSetPair lastUsedResourceCache = this._lastUsedResourceCache;
								lock (lastUsedResourceCache)
								{
									this._lastUsedResourceCache.lastCultureName = cultureInfo.Name;
									this._lastUsedResourceCache.lastResourceSet = resourceSet2;
								}
							}
							return string2;
						}
						resourceSet = resourceSet2;
					}
				}
				if (FrameworkEventSource.IsInitialized)
				{
					FrameworkEventSource.Log.ResourceManagerLookupFailed(this.BaseNameField, this.MainAssembly, culture.Name);
				}
				return null;
			}
			if (this._PRIonAppXInitialized)
			{
				return this.GetStringFromPRI(name, (culture == null) ? null : culture.Name, this._neutralResourcesCulture.Name);
			}
			if (this._PRIExceptionInfo != null && this._PRIExceptionInfo._PackageSimpleName != null && this._PRIExceptionInfo._ResWFile != null)
			{
				throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_ResWFileNotLoaded", new object[]
				{
					this._PRIExceptionInfo._ResWFile,
					this._PRIExceptionInfo._PackageSimpleName
				}));
			}
			throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_NoPRIresources"));
		}

		// Token: 0x06002D3C RID: 11580 RVA: 0x000AB59C File Offset: 0x000A979C
		public virtual object GetObject(string name)
		{
			return this.GetObject(name, null, true);
		}

		// Token: 0x06002D3D RID: 11581 RVA: 0x000AB5A7 File Offset: 0x000A97A7
		public virtual object GetObject(string name, CultureInfo culture)
		{
			return this.GetObject(name, culture, true);
		}

		// Token: 0x06002D3E RID: 11582 RVA: 0x000AB5B4 File Offset: 0x000A97B4
		private object GetObject(string name, CultureInfo culture, bool wrapUnmanagedMemStream)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (ResourceManager.s_IsAppXModel && culture == CultureInfo.CurrentUICulture)
			{
				culture = null;
			}
			if (culture == null)
			{
				culture = Thread.CurrentThread.GetCurrentUICultureNoAppX();
			}
			if (FrameworkEventSource.IsInitialized)
			{
				FrameworkEventSource.Log.ResourceManagerLookupStarted(this.BaseNameField, this.MainAssembly, culture.Name);
			}
			ResourceSet resourceSet = this.GetFirstResourceSet(culture);
			if (resourceSet != null)
			{
				object @object = resourceSet.GetObject(name, this._ignoreCase);
				if (@object != null)
				{
					UnmanagedMemoryStream unmanagedMemoryStream = @object as UnmanagedMemoryStream;
					if (unmanagedMemoryStream != null && wrapUnmanagedMemStream)
					{
						return new UnmanagedMemoryStreamWrapper(unmanagedMemoryStream);
					}
					return @object;
				}
			}
			ResourceFallbackManager resourceFallbackManager = new ResourceFallbackManager(culture, this._neutralResourcesCulture, true);
			foreach (CultureInfo cultureInfo in resourceFallbackManager)
			{
				ResourceSet resourceSet2 = this.InternalGetResourceSet(cultureInfo, true, true);
				if (resourceSet2 == null)
				{
					break;
				}
				if (resourceSet2 != resourceSet)
				{
					object object2 = resourceSet2.GetObject(name, this._ignoreCase);
					if (object2 != null)
					{
						if (this._lastUsedResourceCache != null)
						{
							ResourceManager.CultureNameResourceSetPair lastUsedResourceCache = this._lastUsedResourceCache;
							lock (lastUsedResourceCache)
							{
								this._lastUsedResourceCache.lastCultureName = cultureInfo.Name;
								this._lastUsedResourceCache.lastResourceSet = resourceSet2;
							}
						}
						UnmanagedMemoryStream unmanagedMemoryStream2 = object2 as UnmanagedMemoryStream;
						if (unmanagedMemoryStream2 != null && wrapUnmanagedMemStream)
						{
							return new UnmanagedMemoryStreamWrapper(unmanagedMemoryStream2);
						}
						return object2;
					}
					else
					{
						resourceSet = resourceSet2;
					}
				}
			}
			if (FrameworkEventSource.IsInitialized)
			{
				FrameworkEventSource.Log.ResourceManagerLookupFailed(this.BaseNameField, this.MainAssembly, culture.Name);
			}
			return null;
		}

		// Token: 0x06002D3F RID: 11583 RVA: 0x000AB76C File Offset: 0x000A996C
		[ComVisible(false)]
		public UnmanagedMemoryStream GetStream(string name)
		{
			return this.GetStream(name, null);
		}

		// Token: 0x06002D40 RID: 11584 RVA: 0x000AB778 File Offset: 0x000A9978
		[ComVisible(false)]
		public UnmanagedMemoryStream GetStream(string name, CultureInfo culture)
		{
			object @object = this.GetObject(name, culture, false);
			UnmanagedMemoryStream unmanagedMemoryStream = @object as UnmanagedMemoryStream;
			if (unmanagedMemoryStream == null && @object != null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotStream_Name", new object[] { name }));
			}
			return unmanagedMemoryStream;
		}

		// Token: 0x06002D41 RID: 11585 RVA: 0x000AB7B8 File Offset: 0x000A99B8
		[SecurityCritical]
		private bool TryLookingForSatellite(CultureInfo lookForCulture)
		{
			if (!ResourceManager._checkedConfigFile)
			{
				lock (this)
				{
					if (!ResourceManager._checkedConfigFile)
					{
						ResourceManager._checkedConfigFile = true;
						ResourceManager._installedSatelliteInfo = this.GetSatelliteAssembliesFromConfig();
					}
				}
			}
			if (ResourceManager._installedSatelliteInfo == null)
			{
				return true;
			}
			string[] array = (string[])ResourceManager._installedSatelliteInfo[this.MainAssembly.FullName];
			if (array == null)
			{
				return true;
			}
			int num = Array.IndexOf<string>(array, lookForCulture.Name);
			if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled())
			{
				if (num < 0)
				{
					FrameworkEventSource.Log.ResourceManagerCultureNotFoundInConfigFile(this.BaseNameField, this.MainAssembly, lookForCulture.Name);
				}
				else
				{
					FrameworkEventSource.Log.ResourceManagerCultureFoundInConfigFile(this.BaseNameField, this.MainAssembly, lookForCulture.Name);
				}
			}
			return num >= 0;
		}

		// Token: 0x06002D42 RID: 11586 RVA: 0x000AB8A8 File Offset: 0x000A9AA8
		[SecurityCritical]
		private Hashtable GetSatelliteAssembliesFromConfig()
		{
			string configurationFileInternal = AppDomain.CurrentDomain.FusionStore.ConfigurationFileInternal;
			if (configurationFileInternal == null)
			{
				return null;
			}
			if (configurationFileInternal.Length >= 2 && (configurationFileInternal[1] == Path.VolumeSeparatorChar || (configurationFileInternal[0] == Path.DirectorySeparatorChar && configurationFileInternal[1] == Path.DirectorySeparatorChar)) && !File.InternalExists(configurationFileInternal))
			{
				return null;
			}
			ConfigTreeParser configTreeParser = new ConfigTreeParser();
			string configPath = "/configuration/satelliteassemblies";
			ConfigNode configNode = null;
			try
			{
				configNode = configTreeParser.Parse(configurationFileInternal, configPath, true);
			}
			catch (Exception)
			{
			}
			if (configNode == null)
			{
				return null;
			}
			Hashtable hashtable = new Hashtable(StringComparer.OrdinalIgnoreCase);
			foreach (ConfigNode configNode2 in configNode.Children)
			{
				if (!string.Equals(configNode2.Name, "assembly"))
				{
					throw new ApplicationException(Environment.GetResourceString("XMLSyntax_InvalidSyntaxSatAssemTag", new object[]
					{
						Path.GetFileName(configurationFileInternal),
						configNode2.Name
					}));
				}
				if (configNode2.Attributes.Count == 0)
				{
					throw new ApplicationException(Environment.GetResourceString("XMLSyntax_InvalidSyntaxSatAssemTagNoAttr", new object[] { Path.GetFileName(configurationFileInternal) }));
				}
				DictionaryEntry dictionaryEntry = configNode2.Attributes[0];
				string text = (string)dictionaryEntry.Value;
				if (!object.Equals(dictionaryEntry.Key, "name") || string.IsNullOrEmpty(text) || configNode2.Attributes.Count > 1)
				{
					throw new ApplicationException(Environment.GetResourceString("XMLSyntax_InvalidSyntaxSatAssemTagBadAttr", new object[]
					{
						Path.GetFileName(configurationFileInternal),
						dictionaryEntry.Key,
						dictionaryEntry.Value
					}));
				}
				ArrayList arrayList = new ArrayList(5);
				foreach (ConfigNode configNode3 in configNode2.Children)
				{
					if (configNode3.Value != null)
					{
						arrayList.Add(configNode3.Value);
					}
				}
				string[] array = new string[arrayList.Count];
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = (string)arrayList[i];
					array[i] = text2;
					if (FrameworkEventSource.IsInitialized)
					{
						FrameworkEventSource.Log.ResourceManagerAddingCultureFromConfigFile(this.BaseNameField, this.MainAssembly, text2);
					}
				}
				hashtable.Add(text, array);
			}
			return hashtable;
		}

		// Token: 0x04001236 RID: 4662
		protected string BaseNameField;

		// Token: 0x04001237 RID: 4663
		[Obsolete("call InternalGetResourceSet instead")]
		protected Hashtable ResourceSets;

		// Token: 0x04001238 RID: 4664
		[NonSerialized]
		private Dictionary<string, ResourceSet> _resourceSets;

		// Token: 0x04001239 RID: 4665
		private string moduleDir;

		// Token: 0x0400123A RID: 4666
		protected Assembly MainAssembly;

		// Token: 0x0400123B RID: 4667
		private Type _locationInfo;

		// Token: 0x0400123C RID: 4668
		private Type _userResourceSet;

		// Token: 0x0400123D RID: 4669
		private CultureInfo _neutralResourcesCulture;

		// Token: 0x0400123E RID: 4670
		[NonSerialized]
		private ResourceManager.CultureNameResourceSetPair _lastUsedResourceCache;

		// Token: 0x0400123F RID: 4671
		private bool _ignoreCase;

		// Token: 0x04001240 RID: 4672
		private bool UseManifest;

		// Token: 0x04001241 RID: 4673
		[OptionalField(VersionAdded = 1)]
		private bool UseSatelliteAssem;

		// Token: 0x04001242 RID: 4674
		private static volatile Hashtable _installedSatelliteInfo;

		// Token: 0x04001243 RID: 4675
		private static volatile bool _checkedConfigFile;

		// Token: 0x04001244 RID: 4676
		[OptionalField]
		private UltimateResourceFallbackLocation _fallbackLoc;

		// Token: 0x04001245 RID: 4677
		[OptionalField]
		private Version _satelliteContractVersion;

		// Token: 0x04001246 RID: 4678
		[OptionalField]
		private bool _lookedForSatelliteContractVersion;

		// Token: 0x04001247 RID: 4679
		[OptionalField(VersionAdded = 1)]
		private Assembly _callingAssembly;

		// Token: 0x04001248 RID: 4680
		[OptionalField(VersionAdded = 4)]
		private RuntimeAssembly m_callingAssembly;

		// Token: 0x04001249 RID: 4681
		[NonSerialized]
		private IResourceGroveler resourceGroveler;

		// Token: 0x0400124A RID: 4682
		public static readonly int MagicNumber = -1091581234;

		// Token: 0x0400124B RID: 4683
		public static readonly int HeaderVersionNumber = 1;

		// Token: 0x0400124C RID: 4684
		private static readonly Type _minResourceSet = typeof(ResourceSet);

		// Token: 0x0400124D RID: 4685
		internal static readonly string ResReaderTypeName = typeof(ResourceReader).FullName;

		// Token: 0x0400124E RID: 4686
		internal static readonly string ResSetTypeName = typeof(RuntimeResourceSet).FullName;

		// Token: 0x0400124F RID: 4687
		internal static readonly string MscorlibName = typeof(ResourceReader).Assembly.FullName;

		// Token: 0x04001250 RID: 4688
		internal const string ResFileExtension = ".resources";

		// Token: 0x04001251 RID: 4689
		internal const int ResFileExtensionLength = 10;

		// Token: 0x04001252 RID: 4690
		internal static readonly int DEBUG = 0;

		// Token: 0x04001253 RID: 4691
		private static volatile bool s_IsAppXModel;

		// Token: 0x04001254 RID: 4692
		[NonSerialized]
		private bool _bUsingModernResourceManagement;

		// Token: 0x04001255 RID: 4693
		[SecurityCritical]
		[NonSerialized]
		private WindowsRuntimeResourceManagerBase _WinRTResourceManager;

		// Token: 0x04001256 RID: 4694
		[NonSerialized]
		private bool _PRIonAppXInitialized;

		// Token: 0x04001257 RID: 4695
		[NonSerialized]
		private PRIExceptionInfo _PRIExceptionInfo;

		// Token: 0x02000B65 RID: 2917
		internal class CultureNameResourceSetPair
		{
			// Token: 0x04003449 RID: 13385
			public string lastCultureName;

			// Token: 0x0400344A RID: 13386
			public ResourceSet lastResourceSet;
		}

		// Token: 0x02000B66 RID: 2918
		internal class ResourceManagerMediator
		{
			// Token: 0x06006C09 RID: 27657 RVA: 0x00175FAB File Offset: 0x001741AB
			internal ResourceManagerMediator(ResourceManager rm)
			{
				if (rm == null)
				{
					throw new ArgumentNullException("rm");
				}
				this._rm = rm;
			}

			// Token: 0x1700123D RID: 4669
			// (get) Token: 0x06006C0A RID: 27658 RVA: 0x00175FC8 File Offset: 0x001741C8
			internal string ModuleDir
			{
				get
				{
					return this._rm.moduleDir;
				}
			}

			// Token: 0x1700123E RID: 4670
			// (get) Token: 0x06006C0B RID: 27659 RVA: 0x00175FD5 File Offset: 0x001741D5
			internal Type LocationInfo
			{
				get
				{
					return this._rm._locationInfo;
				}
			}

			// Token: 0x1700123F RID: 4671
			// (get) Token: 0x06006C0C RID: 27660 RVA: 0x00175FE2 File Offset: 0x001741E2
			internal Type UserResourceSet
			{
				get
				{
					return this._rm._userResourceSet;
				}
			}

			// Token: 0x17001240 RID: 4672
			// (get) Token: 0x06006C0D RID: 27661 RVA: 0x00175FEF File Offset: 0x001741EF
			internal string BaseNameField
			{
				get
				{
					return this._rm.BaseNameField;
				}
			}

			// Token: 0x17001241 RID: 4673
			// (get) Token: 0x06006C0E RID: 27662 RVA: 0x00175FFC File Offset: 0x001741FC
			// (set) Token: 0x06006C0F RID: 27663 RVA: 0x00176009 File Offset: 0x00174209
			internal CultureInfo NeutralResourcesCulture
			{
				get
				{
					return this._rm._neutralResourcesCulture;
				}
				set
				{
					this._rm._neutralResourcesCulture = value;
				}
			}

			// Token: 0x06006C10 RID: 27664 RVA: 0x00176017 File Offset: 0x00174217
			internal string GetResourceFileName(CultureInfo culture)
			{
				return this._rm.GetResourceFileName(culture);
			}

			// Token: 0x17001242 RID: 4674
			// (get) Token: 0x06006C11 RID: 27665 RVA: 0x00176025 File Offset: 0x00174225
			// (set) Token: 0x06006C12 RID: 27666 RVA: 0x00176032 File Offset: 0x00174232
			internal bool LookedForSatelliteContractVersion
			{
				get
				{
					return this._rm._lookedForSatelliteContractVersion;
				}
				set
				{
					this._rm._lookedForSatelliteContractVersion = value;
				}
			}

			// Token: 0x17001243 RID: 4675
			// (get) Token: 0x06006C13 RID: 27667 RVA: 0x00176040 File Offset: 0x00174240
			// (set) Token: 0x06006C14 RID: 27668 RVA: 0x0017604D File Offset: 0x0017424D
			internal Version SatelliteContractVersion
			{
				get
				{
					return this._rm._satelliteContractVersion;
				}
				set
				{
					this._rm._satelliteContractVersion = value;
				}
			}

			// Token: 0x06006C15 RID: 27669 RVA: 0x0017605B File Offset: 0x0017425B
			internal Version ObtainSatelliteContractVersion(Assembly a)
			{
				return ResourceManager.GetSatelliteContractVersion(a);
			}

			// Token: 0x17001244 RID: 4676
			// (get) Token: 0x06006C16 RID: 27670 RVA: 0x00176063 File Offset: 0x00174263
			// (set) Token: 0x06006C17 RID: 27671 RVA: 0x00176070 File Offset: 0x00174270
			internal UltimateResourceFallbackLocation FallbackLoc
			{
				get
				{
					return this._rm.FallbackLocation;
				}
				set
				{
					this._rm._fallbackLoc = value;
				}
			}

			// Token: 0x17001245 RID: 4677
			// (get) Token: 0x06006C18 RID: 27672 RVA: 0x0017607E File Offset: 0x0017427E
			internal RuntimeAssembly CallingAssembly
			{
				get
				{
					return this._rm.m_callingAssembly;
				}
			}

			// Token: 0x17001246 RID: 4678
			// (get) Token: 0x06006C19 RID: 27673 RVA: 0x0017608B File Offset: 0x0017428B
			internal RuntimeAssembly MainAssembly
			{
				get
				{
					return (RuntimeAssembly)this._rm.MainAssembly;
				}
			}

			// Token: 0x17001247 RID: 4679
			// (get) Token: 0x06006C1A RID: 27674 RVA: 0x0017609D File Offset: 0x0017429D
			internal string BaseName
			{
				get
				{
					return this._rm.BaseName;
				}
			}

			// Token: 0x06006C1B RID: 27675 RVA: 0x001760AA File Offset: 0x001742AA
			[SecurityCritical]
			internal bool TryLookingForSatellite(CultureInfo lookForCulture)
			{
				return this._rm.TryLookingForSatellite(lookForCulture);
			}

			// Token: 0x0400344B RID: 13387
			private ResourceManager _rm;
		}
	}
}
