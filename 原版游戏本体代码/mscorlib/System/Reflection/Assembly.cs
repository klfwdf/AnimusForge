using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;

namespace System.Reflection
{
	// Token: 0x020005B1 RID: 1457
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_Assembly))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
	[Serializable]
	public abstract class Assembly : _Assembly, IEvidenceFactory, ICustomAttributeProvider, ISerializable
	{
		// Token: 0x06004374 RID: 17268 RVA: 0x000FA859 File Offset: 0x000F8A59
		public static string CreateQualifiedName(string assemblyName, string typeName)
		{
			return typeName + ", " + assemblyName;
		}

		// Token: 0x06004375 RID: 17269 RVA: 0x000FA868 File Offset: 0x000F8A68
		public static Assembly GetAssembly(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			Module module = type.Module;
			if (module == null)
			{
				return null;
			}
			return module.Assembly;
		}

		// Token: 0x06004376 RID: 17270 RVA: 0x000FA8A1 File Offset: 0x000F8AA1
		[__DynamicallyInvokable]
		public static bool operator ==(Assembly left, Assembly right)
		{
			return left == right || (left != null && right != null && !(left is RuntimeAssembly) && !(right is RuntimeAssembly) && left.Equals(right));
		}

		// Token: 0x06004377 RID: 17271 RVA: 0x000FA8C8 File Offset: 0x000F8AC8
		[__DynamicallyInvokable]
		public static bool operator !=(Assembly left, Assembly right)
		{
			return !(left == right);
		}

		// Token: 0x06004378 RID: 17272 RVA: 0x000FA8D4 File Offset: 0x000F8AD4
		[__DynamicallyInvokable]
		public override bool Equals(object o)
		{
			return base.Equals(o);
		}

		// Token: 0x06004379 RID: 17273 RVA: 0x000FA8DD File Offset: 0x000F8ADD
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600437A RID: 17274 RVA: 0x000FA8E8 File Offset: 0x000F8AE8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly LoadFrom(string assemblyFile)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadFrom(assemblyFile, null, null, AssemblyHashAlgorithm.None, false, false, ref stackCrawlMark);
		}

		// Token: 0x0600437B RID: 17275 RVA: 0x000FA904 File Offset: 0x000F8B04
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly ReflectionOnlyLoadFrom(string assemblyFile)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadFrom(assemblyFile, null, null, AssemblyHashAlgorithm.None, true, false, ref stackCrawlMark);
		}

		// Token: 0x0600437C RID: 17276 RVA: 0x000FA920 File Offset: 0x000F8B20
		[SecuritySafeCritical]
		[Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly LoadFrom(string assemblyFile, Evidence securityEvidence)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadFrom(assemblyFile, securityEvidence, null, AssemblyHashAlgorithm.None, false, false, ref stackCrawlMark);
		}

		// Token: 0x0600437D RID: 17277 RVA: 0x000FA93C File Offset: 0x000F8B3C
		[SecuritySafeCritical]
		[Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly LoadFrom(string assemblyFile, Evidence securityEvidence, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadFrom(assemblyFile, securityEvidence, hashValue, hashAlgorithm, false, false, ref stackCrawlMark);
		}

		// Token: 0x0600437E RID: 17278 RVA: 0x000FA958 File Offset: 0x000F8B58
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly LoadFrom(string assemblyFile, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadFrom(assemblyFile, null, hashValue, hashAlgorithm, false, false, ref stackCrawlMark);
		}

		// Token: 0x0600437F RID: 17279 RVA: 0x000FA974 File Offset: 0x000F8B74
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly UnsafeLoadFrom(string assemblyFile)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadFrom(assemblyFile, null, null, AssemblyHashAlgorithm.None, false, true, ref stackCrawlMark);
		}

		// Token: 0x06004380 RID: 17280 RVA: 0x000FA990 File Offset: 0x000F8B90
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(string assemblyString)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackCrawlMark, false);
		}

		// Token: 0x06004381 RID: 17281 RVA: 0x000FA9AC File Offset: 0x000F8BAC
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static Type GetType_Compat(string assemblyString, string typeName)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			RuntimeAssembly runtimeAssembly;
			AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(assemblyString, false, out runtimeAssembly);
			if (runtimeAssembly == null)
			{
				if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
				{
					return Type.GetType(typeName + ", " + assemblyString, true, false);
				}
				runtimeAssembly = RuntimeAssembly.InternalLoadAssemblyName(assemblyName, null, null, ref stackCrawlMark, true, false, false);
			}
			return runtimeAssembly.GetType(typeName, true, false);
		}

		// Token: 0x06004382 RID: 17282 RVA: 0x000FAA04 File Offset: 0x000F8C04
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly ReflectionOnlyLoad(string assemblyString)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackCrawlMark, true);
		}

		// Token: 0x06004383 RID: 17283 RVA: 0x000FAA20 File Offset: 0x000F8C20
		[SecuritySafeCritical]
		[Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(string assemblyString, Evidence assemblySecurity)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackCrawlMark, false);
		}

		// Token: 0x06004384 RID: 17284 RVA: 0x000FAA3C File Offset: 0x000F8C3C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(AssemblyName assemblyRef)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackCrawlMark, true, false, false);
		}

		// Token: 0x06004385 RID: 17285 RVA: 0x000FAA58 File Offset: 0x000F8C58
		[SecuritySafeCritical]
		[Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(AssemblyName assemblyRef, Evidence assemblySecurity)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackCrawlMark, true, false, false);
		}

		// Token: 0x06004386 RID: 17286 RVA: 0x000FAA74 File Offset: 0x000F8C74
		[SecuritySafeCritical]
		[Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly LoadWithPartialName(string partialName)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.LoadWithPartialNameInternal(partialName, null, ref stackCrawlMark);
		}

		// Token: 0x06004387 RID: 17287 RVA: 0x000FAA8C File Offset: 0x000F8C8C
		[SecuritySafeCritical]
		[Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly LoadWithPartialName(string partialName, Evidence securityEvidence)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.LoadWithPartialNameInternal(partialName, securityEvidence, ref stackCrawlMark);
		}

		// Token: 0x06004388 RID: 17288 RVA: 0x000FAAA4 File Offset: 0x000F8CA4
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(byte[] rawAssembly)
		{
			AppDomain.CheckLoadByteArraySupported();
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, null, null, ref stackCrawlMark, false, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x06004389 RID: 17289 RVA: 0x000FAAC8 File Offset: 0x000F8CC8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly ReflectionOnlyLoad(byte[] rawAssembly)
		{
			AppDomain.CheckReflectionOnlyLoadSupported();
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, null, null, ref stackCrawlMark, true, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x0600438A RID: 17290 RVA: 0x000FAAEC File Offset: 0x000F8CEC
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore)
		{
			AppDomain.CheckLoadByteArraySupported();
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackCrawlMark, false, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x0600438B RID: 17291 RVA: 0x000FAB10 File Offset: 0x000F8D10
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, SecurityContextSource securityContextSource)
		{
			AppDomain.CheckLoadByteArraySupported();
			if (securityContextSource < SecurityContextSource.CurrentAppDomain || securityContextSource > SecurityContextSource.CurrentAssembly)
			{
				throw new ArgumentOutOfRangeException("securityContextSource");
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackCrawlMark, false, false, securityContextSource);
		}

		// Token: 0x0600438C RID: 17292 RVA: 0x000FAB44 File Offset: 0x000F8D44
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static Assembly LoadImageSkipIntegrityCheck(byte[] rawAssembly, byte[] rawSymbolStore, SecurityContextSource securityContextSource)
		{
			AppDomain.CheckLoadByteArraySupported();
			if (securityContextSource < SecurityContextSource.CurrentAppDomain || securityContextSource > SecurityContextSource.CurrentAssembly)
			{
				throw new ArgumentOutOfRangeException("securityContextSource");
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackCrawlMark, false, true, securityContextSource);
		}

		// Token: 0x0600438D RID: 17293 RVA: 0x000FAB78 File Offset: 0x000F8D78
		[SecuritySafeCritical]
		[Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlEvidence)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, Evidence securityEvidence)
		{
			AppDomain.CheckLoadByteArraySupported();
			if (securityEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
			{
				Zone hostEvidence = securityEvidence.GetHostEvidence<Zone>();
				if (hostEvidence == null || hostEvidence.SecurityZone != SecurityZone.MyComputer)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
				}
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, securityEvidence, ref stackCrawlMark, false, false, SecurityContextSource.CurrentAssembly);
		}

		// Token: 0x0600438E RID: 17294 RVA: 0x000FABCA File Offset: 0x000F8DCA
		[SecuritySafeCritical]
		public static Assembly LoadFile(string path)
		{
			AppDomain.CheckLoadFileSupported();
			new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, path).Demand();
			return RuntimeAssembly.nLoadFile(path, null);
		}

		// Token: 0x0600438F RID: 17295 RVA: 0x000FABE5 File Offset: 0x000F8DE5
		[SecuritySafeCritical]
		[Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFile which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlEvidence)]
		public static Assembly LoadFile(string path, Evidence securityEvidence)
		{
			AppDomain.CheckLoadFileSupported();
			if (securityEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, path).Demand();
			return RuntimeAssembly.nLoadFile(path, securityEvidence);
		}

		// Token: 0x06004390 RID: 17296 RVA: 0x000FAC20 File Offset: 0x000F8E20
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly GetExecutingAssembly()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeAssembly.GetExecutingAssembly(ref stackCrawlMark);
		}

		// Token: 0x06004391 RID: 17297 RVA: 0x000FAC38 File Offset: 0x000F8E38
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Assembly GetCallingAssembly()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCallersCaller;
			return RuntimeAssembly.GetExecutingAssembly(ref stackCrawlMark);
		}

		// Token: 0x06004392 RID: 17298 RVA: 0x000FAC50 File Offset: 0x000F8E50
		[SecuritySafeCritical]
		public static Assembly GetEntryAssembly()
		{
			AppDomainManager appDomainManager = AppDomain.CurrentDomain.DomainManager;
			if (appDomainManager == null)
			{
				appDomainManager = new AppDomainManager();
			}
			return appDomainManager.EntryAssembly;
		}

		// Token: 0x1400001A RID: 26
		// (add) Token: 0x06004393 RID: 17299 RVA: 0x000FAC77 File Offset: 0x000F8E77
		// (remove) Token: 0x06004394 RID: 17300 RVA: 0x000FAC7E File Offset: 0x000F8E7E
		public virtual event ModuleResolveEventHandler ModuleResolve
		{
			[SecurityCritical]
			add
			{
				throw new NotImplementedException();
			}
			[SecurityCritical]
			remove
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009E9 RID: 2537
		// (get) Token: 0x06004395 RID: 17301 RVA: 0x000FAC85 File Offset: 0x000F8E85
		public virtual string CodeBase
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009EA RID: 2538
		// (get) Token: 0x06004396 RID: 17302 RVA: 0x000FAC8C File Offset: 0x000F8E8C
		public virtual string EscapedCodeBase
		{
			[SecuritySafeCritical]
			get
			{
				return AssemblyName.EscapeCodeBase(this.CodeBase);
			}
		}

		// Token: 0x06004397 RID: 17303 RVA: 0x000FAC99 File Offset: 0x000F8E99
		[__DynamicallyInvokable]
		public virtual AssemblyName GetName()
		{
			return this.GetName(false);
		}

		// Token: 0x06004398 RID: 17304 RVA: 0x000FACA2 File Offset: 0x000F8EA2
		public virtual AssemblyName GetName(bool copiedName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x170009EB RID: 2539
		// (get) Token: 0x06004399 RID: 17305 RVA: 0x000FACA9 File Offset: 0x000F8EA9
		[__DynamicallyInvokable]
		public virtual string FullName
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009EC RID: 2540
		// (get) Token: 0x0600439A RID: 17306 RVA: 0x000FACB0 File Offset: 0x000F8EB0
		[__DynamicallyInvokable]
		public virtual MethodInfo EntryPoint
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x0600439B RID: 17307 RVA: 0x000FACB7 File Offset: 0x000F8EB7
		Type _Assembly.GetType()
		{
			return base.GetType();
		}

		// Token: 0x0600439C RID: 17308 RVA: 0x000FACBF File Offset: 0x000F8EBF
		[__DynamicallyInvokable]
		public virtual Type GetType(string name)
		{
			return this.GetType(name, false, false);
		}

		// Token: 0x0600439D RID: 17309 RVA: 0x000FACCA File Offset: 0x000F8ECA
		[__DynamicallyInvokable]
		public virtual Type GetType(string name, bool throwOnError)
		{
			return this.GetType(name, throwOnError, false);
		}

		// Token: 0x0600439E RID: 17310 RVA: 0x000FACD5 File Offset: 0x000F8ED5
		[__DynamicallyInvokable]
		public virtual Type GetType(string name, bool throwOnError, bool ignoreCase)
		{
			throw new NotImplementedException();
		}

		// Token: 0x170009ED RID: 2541
		// (get) Token: 0x0600439F RID: 17311 RVA: 0x000FACDC File Offset: 0x000F8EDC
		[__DynamicallyInvokable]
		public virtual IEnumerable<Type> ExportedTypes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetExportedTypes();
			}
		}

		// Token: 0x060043A0 RID: 17312 RVA: 0x000FACE4 File Offset: 0x000F8EE4
		[__DynamicallyInvokable]
		public virtual Type[] GetExportedTypes()
		{
			throw new NotImplementedException();
		}

		// Token: 0x170009EE RID: 2542
		// (get) Token: 0x060043A1 RID: 17313 RVA: 0x000FACEC File Offset: 0x000F8EEC
		[__DynamicallyInvokable]
		public virtual IEnumerable<TypeInfo> DefinedTypes
		{
			[__DynamicallyInvokable]
			get
			{
				Type[] types = this.GetTypes();
				TypeInfo[] array = new TypeInfo[types.Length];
				for (int i = 0; i < types.Length; i++)
				{
					TypeInfo typeInfo = types[i].GetTypeInfo();
					if (typeInfo == null)
					{
						throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoTypeInfo", new object[] { types[i].FullName }));
					}
					array[i] = typeInfo;
				}
				return array;
			}
		}

		// Token: 0x060043A2 RID: 17314 RVA: 0x000FAD50 File Offset: 0x000F8F50
		[__DynamicallyInvokable]
		public virtual Type[] GetTypes()
		{
			Module[] modules = this.GetModules(false);
			int num = modules.Length;
			int num2 = 0;
			Type[][] array = new Type[num][];
			for (int i = 0; i < num; i++)
			{
				array[i] = modules[i].GetTypes();
				num2 += array[i].Length;
			}
			int num3 = 0;
			Type[] array2 = new Type[num2];
			for (int j = 0; j < num; j++)
			{
				int num4 = array[j].Length;
				Array.Copy(array[j], 0, array2, num3, num4);
				num3 += num4;
			}
			return array2;
		}

		// Token: 0x060043A3 RID: 17315 RVA: 0x000FADD4 File Offset: 0x000F8FD4
		[__DynamicallyInvokable]
		public virtual Stream GetManifestResourceStream(Type type, string name)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043A4 RID: 17316 RVA: 0x000FADDB File Offset: 0x000F8FDB
		[__DynamicallyInvokable]
		public virtual Stream GetManifestResourceStream(string name)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043A5 RID: 17317 RVA: 0x000FADE2 File Offset: 0x000F8FE2
		public virtual Assembly GetSatelliteAssembly(CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043A6 RID: 17318 RVA: 0x000FADE9 File Offset: 0x000F8FE9
		public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
		{
			throw new NotImplementedException();
		}

		// Token: 0x170009EF RID: 2543
		// (get) Token: 0x060043A7 RID: 17319 RVA: 0x000FADF0 File Offset: 0x000F8FF0
		public virtual Evidence Evidence
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009F0 RID: 2544
		// (get) Token: 0x060043A8 RID: 17320 RVA: 0x000FADF7 File Offset: 0x000F8FF7
		public virtual PermissionSet PermissionSet
		{
			[SecurityCritical]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009F1 RID: 2545
		// (get) Token: 0x060043A9 RID: 17321 RVA: 0x000FADFE File Offset: 0x000F8FFE
		public bool IsFullyTrusted
		{
			[SecuritySafeCritical]
			get
			{
				return this.PermissionSet.IsUnrestricted();
			}
		}

		// Token: 0x170009F2 RID: 2546
		// (get) Token: 0x060043AA RID: 17322 RVA: 0x000FAE0B File Offset: 0x000F900B
		public virtual SecurityRuleSet SecurityRuleSet
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x060043AB RID: 17323 RVA: 0x000FAE12 File Offset: 0x000F9012
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}

		// Token: 0x170009F3 RID: 2547
		// (get) Token: 0x060043AC RID: 17324 RVA: 0x000FAE1C File Offset: 0x000F901C
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public virtual Module ManifestModule
		{
			[__DynamicallyInvokable]
			get
			{
				RuntimeAssembly runtimeAssembly = this as RuntimeAssembly;
				if (runtimeAssembly != null)
				{
					return runtimeAssembly.ManifestModule;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009F4 RID: 2548
		// (get) Token: 0x060043AD RID: 17325 RVA: 0x000FAE45 File Offset: 0x000F9045
		[__DynamicallyInvokable]
		public virtual IEnumerable<CustomAttributeData> CustomAttributes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetCustomAttributesData();
			}
		}

		// Token: 0x060043AE RID: 17326 RVA: 0x000FAE4D File Offset: 0x000F904D
		[__DynamicallyInvokable]
		public virtual object[] GetCustomAttributes(bool inherit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043AF RID: 17327 RVA: 0x000FAE54 File Offset: 0x000F9054
		[__DynamicallyInvokable]
		public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043B0 RID: 17328 RVA: 0x000FAE5B File Offset: 0x000F905B
		[__DynamicallyInvokable]
		public virtual bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043B1 RID: 17329 RVA: 0x000FAE62 File Offset: 0x000F9062
		public virtual IList<CustomAttributeData> GetCustomAttributesData()
		{
			throw new NotImplementedException();
		}

		// Token: 0x170009F5 RID: 2549
		// (get) Token: 0x060043B2 RID: 17330 RVA: 0x000FAE69 File Offset: 0x000F9069
		[ComVisible(false)]
		public virtual bool ReflectionOnly
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x060043B3 RID: 17331 RVA: 0x000FAE70 File Offset: 0x000F9070
		public Module LoadModule(string moduleName, byte[] rawModule)
		{
			return this.LoadModule(moduleName, rawModule, null);
		}

		// Token: 0x060043B4 RID: 17332 RVA: 0x000FAE7B File Offset: 0x000F907B
		public virtual Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043B5 RID: 17333 RVA: 0x000FAE82 File Offset: 0x000F9082
		public object CreateInstance(string typeName)
		{
			return this.CreateInstance(typeName, false, BindingFlags.Instance | BindingFlags.Public, null, null, null, null);
		}

		// Token: 0x060043B6 RID: 17334 RVA: 0x000FAE92 File Offset: 0x000F9092
		public object CreateInstance(string typeName, bool ignoreCase)
		{
			return this.CreateInstance(typeName, ignoreCase, BindingFlags.Instance | BindingFlags.Public, null, null, null, null);
		}

		// Token: 0x060043B7 RID: 17335 RVA: 0x000FAEA4 File Offset: 0x000F90A4
		public virtual object CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
		{
			Type type = this.GetType(typeName, false, ignoreCase);
			if (type == null)
			{
				return null;
			}
			return Activator.CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
		}

		// Token: 0x170009F6 RID: 2550
		// (get) Token: 0x060043B8 RID: 17336 RVA: 0x000FAED5 File Offset: 0x000F90D5
		[__DynamicallyInvokable]
		public virtual IEnumerable<Module> Modules
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetLoadedModules(true);
			}
		}

		// Token: 0x060043B9 RID: 17337 RVA: 0x000FAEDE File Offset: 0x000F90DE
		public Module[] GetLoadedModules()
		{
			return this.GetLoadedModules(false);
		}

		// Token: 0x060043BA RID: 17338 RVA: 0x000FAEE7 File Offset: 0x000F90E7
		public virtual Module[] GetLoadedModules(bool getResourceModules)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043BB RID: 17339 RVA: 0x000FAEEE File Offset: 0x000F90EE
		[__DynamicallyInvokable]
		public Module[] GetModules()
		{
			return this.GetModules(false);
		}

		// Token: 0x060043BC RID: 17340 RVA: 0x000FAEF7 File Offset: 0x000F90F7
		public virtual Module[] GetModules(bool getResourceModules)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043BD RID: 17341 RVA: 0x000FAEFE File Offset: 0x000F90FE
		public virtual Module GetModule(string name)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043BE RID: 17342 RVA: 0x000FAF05 File Offset: 0x000F9105
		public virtual FileStream GetFile(string name)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043BF RID: 17343 RVA: 0x000FAF0C File Offset: 0x000F910C
		public virtual FileStream[] GetFiles()
		{
			return this.GetFiles(false);
		}

		// Token: 0x060043C0 RID: 17344 RVA: 0x000FAF15 File Offset: 0x000F9115
		public virtual FileStream[] GetFiles(bool getResourceModules)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043C1 RID: 17345 RVA: 0x000FAF1C File Offset: 0x000F911C
		[__DynamicallyInvokable]
		public virtual string[] GetManifestResourceNames()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043C2 RID: 17346 RVA: 0x000FAF23 File Offset: 0x000F9123
		public virtual AssemblyName[] GetReferencedAssemblies()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043C3 RID: 17347 RVA: 0x000FAF2A File Offset: 0x000F912A
		[__DynamicallyInvokable]
		public virtual ManifestResourceInfo GetManifestResourceInfo(string resourceName)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060043C4 RID: 17348 RVA: 0x000FAF34 File Offset: 0x000F9134
		[__DynamicallyInvokable]
		public override string ToString()
		{
			string fullName = this.FullName;
			if (fullName == null)
			{
				return base.ToString();
			}
			return fullName;
		}

		// Token: 0x170009F7 RID: 2551
		// (get) Token: 0x060043C5 RID: 17349 RVA: 0x000FAF53 File Offset: 0x000F9153
		public virtual string Location
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009F8 RID: 2552
		// (get) Token: 0x060043C6 RID: 17350 RVA: 0x000FAF5A File Offset: 0x000F915A
		[ComVisible(false)]
		public virtual string ImageRuntimeVersion
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009F9 RID: 2553
		// (get) Token: 0x060043C7 RID: 17351 RVA: 0x000FAF61 File Offset: 0x000F9161
		public virtual bool GlobalAssemblyCache
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009FA RID: 2554
		// (get) Token: 0x060043C8 RID: 17352 RVA: 0x000FAF68 File Offset: 0x000F9168
		[ComVisible(false)]
		public virtual long HostContext
		{
			get
			{
				RuntimeAssembly runtimeAssembly = this as RuntimeAssembly;
				if (runtimeAssembly != null)
				{
					return runtimeAssembly.HostContext;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x170009FB RID: 2555
		// (get) Token: 0x060043C9 RID: 17353 RVA: 0x000FAF91 File Offset: 0x000F9191
		[__DynamicallyInvokable]
		public virtual bool IsDynamic
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}
	}
}
