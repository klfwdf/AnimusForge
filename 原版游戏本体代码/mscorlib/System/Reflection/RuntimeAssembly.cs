using System;
using System.Collections;
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
using System.Security.Util;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace System.Reflection
{
	// Token: 0x020005B3 RID: 1459
	[Serializable]
	internal class RuntimeAssembly : Assembly, ICustomQueryInterface
	{
		// Token: 0x060043CA RID: 17354 RVA: 0x000FAF94 File Offset: 0x000F9194
		[SecurityCritical]
		CustomQueryInterfaceResult ICustomQueryInterface.GetInterface([In] ref Guid iid, out IntPtr ppv)
		{
			if (iid == typeof(NativeMethods.IDispatch).GUID)
			{
				ppv = Marshal.GetComInterfaceForObject(this, typeof(_Assembly));
				return CustomQueryInterfaceResult.Handled;
			}
			ppv = IntPtr.Zero;
			return CustomQueryInterfaceResult.NotHandled;
		}

		// Token: 0x060043CB RID: 17355 RVA: 0x000FAFCE File Offset: 0x000F91CE
		internal RuntimeAssembly()
		{
			throw new NotSupportedException();
		}

		// Token: 0x1400001B RID: 27
		// (add) Token: 0x060043CC RID: 17356 RVA: 0x000FAFDC File Offset: 0x000F91DC
		// (remove) Token: 0x060043CD RID: 17357 RVA: 0x000FB014 File Offset: 0x000F9214
		[method: SecurityCritical]
		private event ModuleResolveEventHandler _ModuleResolve;

		// Token: 0x170009FC RID: 2556
		// (get) Token: 0x060043CE RID: 17358 RVA: 0x000FB04C File Offset: 0x000F924C
		internal int InvocableAttributeCtorToken
		{
			get
			{
				int num = (int)(this.Flags & RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_TOKEN_MASK);
				return num | 100663296;
			}
		}

		// Token: 0x170009FD RID: 2557
		// (get) Token: 0x060043CF RID: 17359 RVA: 0x000FB070 File Offset: 0x000F9270
		private RuntimeAssembly.ASSEMBLY_FLAGS Flags
		{
			[SecuritySafeCritical]
			get
			{
				if ((this.m_flags & RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED) == RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN)
				{
					RuntimeAssembly.ASSEMBLY_FLAGS assembly_FLAGS = RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN;
					if (RuntimeAssembly.IsFrameworkAssembly(this.GetName()))
					{
						assembly_FLAGS |= (RuntimeAssembly.ASSEMBLY_FLAGS)100663296U;
						foreach (string strB in RuntimeAssembly.s_unsafeFrameworkAssemblyNames)
						{
							if (string.Compare(this.GetSimpleName(), strB, StringComparison.OrdinalIgnoreCase) == 0)
							{
								assembly_FLAGS &= (RuntimeAssembly.ASSEMBLY_FLAGS)4227858431U;
								break;
							}
						}
						Type type = this.GetType("__DynamicallyInvokableAttribute", false);
						if (type != null)
						{
							ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
							int metadataToken = constructor.MetadataToken;
							assembly_FLAGS |= (RuntimeAssembly.ASSEMBLY_FLAGS)(metadataToken & 16777215);
						}
					}
					else if (this.IsDesignerBindingContext())
					{
						assembly_FLAGS = RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
					}
					this.m_flags = assembly_FLAGS | RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED;
				}
				return this.m_flags;
			}
		}

		// Token: 0x170009FE RID: 2558
		// (get) Token: 0x060043D0 RID: 17360 RVA: 0x000FB132 File Offset: 0x000F9332
		internal object SyncRoot
		{
			get
			{
				if (this.m_syncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref this.m_syncRoot, new object(), null);
				}
				return this.m_syncRoot;
			}
		}

		// Token: 0x1400001C RID: 28
		// (add) Token: 0x060043D1 RID: 17361 RVA: 0x000FB154 File Offset: 0x000F9354
		// (remove) Token: 0x060043D2 RID: 17362 RVA: 0x000FB15D File Offset: 0x000F935D
		public override event ModuleResolveEventHandler ModuleResolve
		{
			[SecurityCritical]
			add
			{
				this._ModuleResolve += value;
			}
			[SecurityCritical]
			remove
			{
				this._ModuleResolve -= value;
			}
		}

		// Token: 0x060043D3 RID: 17363
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetCodeBase(RuntimeAssembly assembly, bool copiedName, StringHandleOnStack retString);

		// Token: 0x060043D4 RID: 17364 RVA: 0x000FB168 File Offset: 0x000F9368
		[SecurityCritical]
		internal string GetCodeBase(bool copiedName)
		{
			string result = null;
			RuntimeAssembly.GetCodeBase(this.GetNativeHandle(), copiedName, JitHelpers.GetStringHandleOnStack(ref result));
			return result;
		}

		// Token: 0x170009FF RID: 2559
		// (get) Token: 0x060043D5 RID: 17365 RVA: 0x000FB18C File Offset: 0x000F938C
		public override string CodeBase
		{
			[SecuritySafeCritical]
			get
			{
				string codeBase = this.GetCodeBase(false);
				this.VerifyCodeBaseDiscovery(codeBase);
				return codeBase;
			}
		}

		// Token: 0x060043D6 RID: 17366 RVA: 0x000FB1A9 File Offset: 0x000F93A9
		internal RuntimeAssembly GetNativeHandle()
		{
			return this;
		}

		// Token: 0x060043D7 RID: 17367 RVA: 0x000FB1AC File Offset: 0x000F93AC
		[SecuritySafeCritical]
		public override AssemblyName GetName(bool copiedName)
		{
			AssemblyName assemblyName = new AssemblyName();
			string codeBase = this.GetCodeBase(copiedName);
			this.VerifyCodeBaseDiscovery(codeBase);
			assemblyName.Init(this.GetSimpleName(), this.GetPublicKey(), null, this.GetVersion(), this.GetLocale(), this.GetHashAlgorithm(), AssemblyVersionCompatibility.SameMachine, codeBase, this.GetFlags() | AssemblyNameFlags.PublicKey, null);
			Module manifestModule = this.ManifestModule;
			if (manifestModule != null && manifestModule.MDStreamVersion > 65536)
			{
				PortableExecutableKinds pek;
				ImageFileMachine ifm;
				this.ManifestModule.GetPEKind(out pek, out ifm);
				assemblyName.SetProcArchIndex(pek, ifm);
			}
			return assemblyName;
		}

		// Token: 0x060043D8 RID: 17368 RVA: 0x000FB238 File Offset: 0x000F9438
		[SecurityCritical]
		[PermissionSet(SecurityAction.Assert, Unrestricted = true)]
		private string GetNameForConditionalAptca()
		{
			AssemblyName name = this.GetName();
			return name.GetNameWithPublicKey();
		}

		// Token: 0x060043D9 RID: 17369
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetFullName(RuntimeAssembly assembly, StringHandleOnStack retString);

		// Token: 0x17000A00 RID: 2560
		// (get) Token: 0x060043DA RID: 17370 RVA: 0x000FB254 File Offset: 0x000F9454
		public override string FullName
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_fullname == null)
				{
					string value = null;
					RuntimeAssembly.GetFullName(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref value));
					Interlocked.CompareExchange<string>(ref this.m_fullname, value, null);
				}
				return this.m_fullname;
			}
		}

		// Token: 0x060043DB RID: 17371
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetEntryPoint(RuntimeAssembly assembly, ObjectHandleOnStack retMethod);

		// Token: 0x17000A01 RID: 2561
		// (get) Token: 0x060043DC RID: 17372 RVA: 0x000FB294 File Offset: 0x000F9494
		public override MethodInfo EntryPoint
		{
			[SecuritySafeCritical]
			get
			{
				IRuntimeMethodInfo runtimeMethodInfo = null;
				RuntimeAssembly.GetEntryPoint(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<IRuntimeMethodInfo>(ref runtimeMethodInfo));
				if (runtimeMethodInfo == null)
				{
					return null;
				}
				return (MethodInfo)RuntimeType.GetMethodBase(runtimeMethodInfo);
			}
		}

		// Token: 0x060043DD RID: 17373
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetType(RuntimeAssembly assembly, string name, bool throwOnError, bool ignoreCase, ObjectHandleOnStack type);

		// Token: 0x060043DE RID: 17374 RVA: 0x000FB2C8 File Offset: 0x000F94C8
		[SecuritySafeCritical]
		public override Type GetType(string name, bool throwOnError, bool ignoreCase)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			RuntimeType result = null;
			RuntimeAssembly.GetType(this.GetNativeHandle(), name, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack<RuntimeType>(ref result));
			return result;
		}

		// Token: 0x060043DF RID: 17375
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void GetForwardedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes);

		// Token: 0x060043E0 RID: 17376
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetExportedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes);

		// Token: 0x060043E1 RID: 17377 RVA: 0x000FB2FC File Offset: 0x000F94FC
		[SecuritySafeCritical]
		public override Type[] GetExportedTypes()
		{
			Type[] result = null;
			RuntimeAssembly.GetExportedTypes(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<Type[]>(ref result));
			return result;
		}

		// Token: 0x17000A02 RID: 2562
		// (get) Token: 0x060043E2 RID: 17378 RVA: 0x000FB320 File Offset: 0x000F9520
		public override IEnumerable<TypeInfo> DefinedTypes
		{
			[SecuritySafeCritical]
			get
			{
				List<RuntimeType> list = new List<RuntimeType>();
				RuntimeModule[] modulesInternal = this.GetModulesInternal(true, false);
				for (int i = 0; i < modulesInternal.Length; i++)
				{
					list.AddRange(modulesInternal[i].GetDefinedTypes());
				}
				return list.ToArray();
			}
		}

		// Token: 0x060043E3 RID: 17379 RVA: 0x000FB360 File Offset: 0x000F9560
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override Stream GetManifestResourceStream(Type type, string name)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.GetManifestResourceStream(type, name, false, ref stackCrawlMark);
		}

		// Token: 0x060043E4 RID: 17380 RVA: 0x000FB37C File Offset: 0x000F957C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override Stream GetManifestResourceStream(string name)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.GetManifestResourceStream(name, ref stackCrawlMark, false);
		}

		// Token: 0x060043E5 RID: 17381
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetEvidence(RuntimeAssembly assembly, ObjectHandleOnStack retEvidence);

		// Token: 0x060043E6 RID: 17382
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern SecurityRuleSet GetSecurityRuleSet(RuntimeAssembly assembly);

		// Token: 0x17000A03 RID: 2563
		// (get) Token: 0x060043E7 RID: 17383 RVA: 0x000FB398 File Offset: 0x000F9598
		public override Evidence Evidence
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, ControlEvidence = true)]
			get
			{
				Evidence evidenceNoDemand = this.EvidenceNoDemand;
				return evidenceNoDemand.Clone();
			}
		}

		// Token: 0x17000A04 RID: 2564
		// (get) Token: 0x060043E8 RID: 17384 RVA: 0x000FB3B4 File Offset: 0x000F95B4
		internal Evidence EvidenceNoDemand
		{
			[SecurityCritical]
			get
			{
				Evidence result = null;
				RuntimeAssembly.GetEvidence(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<Evidence>(ref result));
				return result;
			}
		}

		// Token: 0x17000A05 RID: 2565
		// (get) Token: 0x060043E9 RID: 17385 RVA: 0x000FB3D8 File Offset: 0x000F95D8
		public override PermissionSet PermissionSet
		{
			[SecurityCritical]
			get
			{
				PermissionSet permissionSet = null;
				PermissionSet permissionSet2 = null;
				this.GetGrantSet(out permissionSet, out permissionSet2);
				if (permissionSet != null)
				{
					return permissionSet.Copy();
				}
				return new PermissionSet(PermissionState.Unrestricted);
			}
		}

		// Token: 0x17000A06 RID: 2566
		// (get) Token: 0x060043EA RID: 17386 RVA: 0x000FB403 File Offset: 0x000F9603
		public override SecurityRuleSet SecurityRuleSet
		{
			[SecuritySafeCritical]
			get
			{
				return RuntimeAssembly.GetSecurityRuleSet(this.GetNativeHandle());
			}
		}

		// Token: 0x060043EB RID: 17387 RVA: 0x000FB410 File Offset: 0x000F9610
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			UnitySerializationHolder.GetUnitySerializationInfo(info, 6, this.FullName, this);
		}

		// Token: 0x17000A07 RID: 2567
		// (get) Token: 0x060043EC RID: 17388 RVA: 0x000FB42E File Offset: 0x000F962E
		public override Module ManifestModule
		{
			get
			{
				return RuntimeAssembly.GetManifestModule(this.GetNativeHandle());
			}
		}

		// Token: 0x060043ED RID: 17389 RVA: 0x000FB43B File Offset: 0x000F963B
		public override object[] GetCustomAttributes(bool inherit)
		{
			return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
		}

		// Token: 0x060043EE RID: 17390 RVA: 0x000FB454 File Offset: 0x000F9654
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.GetCustomAttributes(this, runtimeType);
		}

		// Token: 0x060043EF RID: 17391 RVA: 0x000FB4A8 File Offset: 0x000F96A8
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "caType");
			}
			return CustomAttribute.IsDefined(this, runtimeType);
		}

		// Token: 0x060043F0 RID: 17392 RVA: 0x000FB4FA File Offset: 0x000F96FA
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return CustomAttributeData.GetCustomAttributesInternal(this);
		}

		// Token: 0x060043F1 RID: 17393 RVA: 0x000FB504 File Offset: 0x000F9704
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static RuntimeAssembly InternalLoadFrom(string assemblyFile, Evidence securityEvidence, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm, bool forIntrospection, bool suppressSecurityChecks, ref StackCrawlMark stackMark)
		{
			if (assemblyFile == null)
			{
				throw new ArgumentNullException("assemblyFile");
			}
			if (securityEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
			}
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.CodeBase = assemblyFile;
			assemblyName.SetHashControl(hashValue, hashAlgorithm);
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyName, securityEvidence, null, ref stackMark, true, forIntrospection, suppressSecurityChecks);
		}

		// Token: 0x060043F2 RID: 17394 RVA: 0x000FB562 File Offset: 0x000F9762
		[SecurityCritical]
		internal static RuntimeAssembly InternalLoad(string assemblyString, Evidence assemblySecurity, ref StackCrawlMark stackMark, bool forIntrospection)
		{
			return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackMark, IntPtr.Zero, forIntrospection);
		}

		// Token: 0x060043F3 RID: 17395 RVA: 0x000FB574 File Offset: 0x000F9774
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static RuntimeAssembly InternalLoad(string assemblyString, Evidence assemblySecurity, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool forIntrospection)
		{
			RuntimeAssembly runtimeAssembly;
			AssemblyName assemblyRef = RuntimeAssembly.CreateAssemblyName(assemblyString, forIntrospection, out runtimeAssembly);
			if (runtimeAssembly != null)
			{
				return runtimeAssembly;
			}
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackMark, pPrivHostBinder, true, forIntrospection, false);
		}

		// Token: 0x060043F4 RID: 17396 RVA: 0x000FB5A8 File Offset: 0x000F97A8
		[SecurityCritical]
		internal static AssemblyName CreateAssemblyName(string assemblyString, bool forIntrospection, out RuntimeAssembly assemblyFromResolveEvent)
		{
			if (assemblyString == null)
			{
				throw new ArgumentNullException("assemblyString");
			}
			if (assemblyString.Length == 0 || assemblyString[0] == '\0')
			{
				throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));
			}
			if (forIntrospection)
			{
				AppDomain.CheckReflectionOnlyLoadSupported();
			}
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = assemblyString;
			assemblyName.nInit(out assemblyFromResolveEvent, forIntrospection, true);
			return assemblyName;
		}

		// Token: 0x060043F5 RID: 17397 RVA: 0x000FB603 File Offset: 0x000F9803
		[SecurityCritical]
		internal static RuntimeAssembly InternalLoadAssemblyName(AssemblyName assemblyRef, Evidence assemblySecurity, RuntimeAssembly reqAssembly, ref StackCrawlMark stackMark, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks)
		{
			return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, reqAssembly, ref stackMark, IntPtr.Zero, true, forIntrospection, suppressSecurityChecks);
		}

		// Token: 0x060043F6 RID: 17398 RVA: 0x000FB618 File Offset: 0x000F9818
		[SecurityCritical]
		internal static RuntimeAssembly InternalLoadAssemblyName(AssemblyName assemblyRef, Evidence assemblySecurity, RuntimeAssembly reqAssembly, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks)
		{
			if (assemblyRef == null)
			{
				throw new ArgumentNullException("assemblyRef");
			}
			if (assemblyRef.CodeBase != null)
			{
				AppDomain.CheckLoadFromSupported();
			}
			assemblyRef = (AssemblyName)assemblyRef.Clone();
			if (assemblySecurity != null)
			{
				if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
				}
				if (!suppressSecurityChecks)
				{
					new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
				}
			}
			string text = RuntimeAssembly.VerifyCodeBase(assemblyRef.CodeBase);
			if (text != null && !suppressSecurityChecks)
			{
				if (string.Compare(text, 0, "file:", 0, 5, StringComparison.OrdinalIgnoreCase) != 0)
				{
					IPermission permission = RuntimeAssembly.CreateWebPermission(assemblyRef.EscapedCodeBase);
					permission.Demand();
				}
				else
				{
					URLString urlstring = new URLString(text, true);
					new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, urlstring.GetFileName()).Demand();
				}
			}
			return RuntimeAssembly.nLoad(assemblyRef, text, assemblySecurity, reqAssembly, ref stackMark, pPrivHostBinder, throwOnFileNotFound, forIntrospection, suppressSecurityChecks);
		}

		// Token: 0x060043F7 RID: 17399 RVA: 0x000FB6E0 File Offset: 0x000F98E0
		[SecuritySafeCritical]
		internal bool IsFrameworkAssembly()
		{
			RuntimeAssembly.ASSEMBLY_FLAGS flags = this.Flags;
			return (flags & RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK) > RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN;
		}

		// Token: 0x060043F8 RID: 17400 RVA: 0x000FB700 File Offset: 0x000F9900
		internal bool IsSafeForReflection()
		{
			RuntimeAssembly.ASSEMBLY_FLAGS flags = this.Flags;
			return (flags & RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION) > RuntimeAssembly.ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN;
		}

		// Token: 0x060043F9 RID: 17401 RVA: 0x000FB71E File Offset: 0x000F991E
		[SecuritySafeCritical]
		private bool IsDesignerBindingContext()
		{
			return RuntimeAssembly.nIsDesignerBindingContext(this);
		}

		// Token: 0x060043FA RID: 17402
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern bool nIsDesignerBindingContext(RuntimeAssembly assembly);

		// Token: 0x060043FB RID: 17403
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern RuntimeAssembly _nLoad(AssemblyName fileName, string codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks);

		// Token: 0x060043FC RID: 17404
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsFrameworkAssembly(AssemblyName assemblyName);

		// Token: 0x060043FD RID: 17405
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IsNewPortableAssembly(AssemblyName assemblyName);

		// Token: 0x060043FE RID: 17406 RVA: 0x000FB728 File Offset: 0x000F9928
		[SecurityCritical]
		private static RuntimeAssembly nLoad(AssemblyName fileName, string codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks)
		{
			return RuntimeAssembly._nLoad(fileName, codeBase, assemblySecurity, locationHint, ref stackMark, pPrivHostBinder, throwOnFileNotFound, forIntrospection, suppressSecurityChecks);
		}

		// Token: 0x060043FF RID: 17407 RVA: 0x000FB748 File Offset: 0x000F9948
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static RuntimeAssembly LoadWithPartialNameHack(string partialName, bool cropPublicKey)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			AssemblyName assemblyName = new AssemblyName(partialName);
			if (!RuntimeAssembly.IsSimplyNamed(assemblyName))
			{
				if (cropPublicKey)
				{
					assemblyName.SetPublicKey(null);
					assemblyName.SetPublicKeyToken(null);
				}
				if (RuntimeAssembly.IsFrameworkAssembly(assemblyName) || !AppDomain.IsAppXModel())
				{
					AssemblyName assemblyName2 = RuntimeAssembly.EnumerateCache(assemblyName);
					if (assemblyName2 != null)
					{
						return RuntimeAssembly.InternalLoadAssemblyName(assemblyName2, null, null, ref stackCrawlMark, true, false, false);
					}
					return null;
				}
			}
			if (AppDomain.IsAppXModel())
			{
				assemblyName.Version = null;
				return RuntimeAssembly.nLoad(assemblyName, null, null, null, ref stackCrawlMark, IntPtr.Zero, false, false, false);
			}
			return null;
		}

		// Token: 0x06004400 RID: 17408 RVA: 0x000FB7C4 File Offset: 0x000F99C4
		[SecurityCritical]
		internal static RuntimeAssembly LoadWithPartialNameInternal(string partialName, Evidence securityEvidence, ref StackCrawlMark stackMark)
		{
			AssemblyName an = new AssemblyName(partialName);
			return RuntimeAssembly.LoadWithPartialNameInternal(an, securityEvidence, ref stackMark);
		}

		// Token: 0x06004401 RID: 17409 RVA: 0x000FB7E0 File Offset: 0x000F99E0
		[SecurityCritical]
		internal static RuntimeAssembly LoadWithPartialNameInternal(AssemblyName an, Evidence securityEvidence, ref StackCrawlMark stackMark)
		{
			if (securityEvidence != null)
			{
				if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
				{
					throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
				}
				new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
			}
			AppDomain.CheckLoadWithPartialNameSupported(stackMark);
			RuntimeAssembly result = null;
			try
			{
				result = RuntimeAssembly.nLoad(an, null, securityEvidence, null, ref stackMark, IntPtr.Zero, true, false, false);
			}
			catch (Exception ex)
			{
				if (ex.IsTransient)
				{
					throw ex;
				}
				if (RuntimeAssembly.IsUserError(ex))
				{
					throw;
				}
				if (RuntimeAssembly.IsFrameworkAssembly(an) || !AppDomain.IsAppXModel())
				{
					if (RuntimeAssembly.IsSimplyNamed(an))
					{
						return null;
					}
					AssemblyName assemblyName = RuntimeAssembly.EnumerateCache(an);
					if (assemblyName != null)
					{
						result = RuntimeAssembly.InternalLoadAssemblyName(assemblyName, securityEvidence, null, ref stackMark, true, false, false);
					}
				}
				else
				{
					an.Version = null;
					result = RuntimeAssembly.nLoad(an, null, securityEvidence, null, ref stackMark, IntPtr.Zero, false, false, false);
				}
			}
			return result;
		}

		// Token: 0x06004402 RID: 17410 RVA: 0x000FB8B0 File Offset: 0x000F9AB0
		[SecuritySafeCritical]
		private static bool IsUserError(Exception e)
		{
			return e.HResult == -2146234280;
		}

		// Token: 0x06004403 RID: 17411 RVA: 0x000FB8C0 File Offset: 0x000F9AC0
		private static bool IsSimplyNamed(AssemblyName partialName)
		{
			byte[] array = partialName.GetPublicKeyToken();
			if (array != null && array.Length == 0)
			{
				return true;
			}
			array = partialName.GetPublicKey();
			return array != null && array.Length == 0;
		}

		// Token: 0x06004404 RID: 17412 RVA: 0x000FB8F0 File Offset: 0x000F9AF0
		[SecurityCritical]
		private static AssemblyName EnumerateCache(AssemblyName partialName)
		{
			new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
			partialName.Version = null;
			ArrayList arrayList = new ArrayList();
			Fusion.ReadCache(arrayList, partialName.FullName, 2U);
			IEnumerator enumerator = arrayList.GetEnumerator();
			AssemblyName assemblyName = null;
			CultureInfo cultureInfo = partialName.CultureInfo;
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				AssemblyName assemblyName2 = new AssemblyName((string)obj);
				if (RuntimeAssembly.CulturesEqual(cultureInfo, assemblyName2.CultureInfo))
				{
					if (assemblyName == null)
					{
						assemblyName = assemblyName2;
					}
					else if (assemblyName2.Version > assemblyName.Version)
					{
						assemblyName = assemblyName2;
					}
				}
			}
			return assemblyName;
		}

		// Token: 0x06004405 RID: 17413 RVA: 0x000FB980 File Offset: 0x000F9B80
		private static bool CulturesEqual(CultureInfo refCI, CultureInfo defCI)
		{
			bool flag = defCI.Equals(CultureInfo.InvariantCulture);
			if (refCI == null || refCI.Equals(CultureInfo.InvariantCulture))
			{
				return flag;
			}
			return !flag && defCI.Equals(refCI);
		}

		// Token: 0x06004406 RID: 17414
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsReflectionOnly(RuntimeAssembly assembly);

		// Token: 0x17000A08 RID: 2568
		// (get) Token: 0x06004407 RID: 17415 RVA: 0x000FB9BA File Offset: 0x000F9BBA
		[ComVisible(false)]
		public override bool ReflectionOnly
		{
			[SecuritySafeCritical]
			get
			{
				return RuntimeAssembly.IsReflectionOnly(this.GetNativeHandle());
			}
		}

		// Token: 0x06004408 RID: 17416
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void LoadModule(RuntimeAssembly assembly, string moduleName, byte[] rawModule, int cbModule, byte[] rawSymbolStore, int cbSymbolStore, ObjectHandleOnStack retModule);

		// Token: 0x06004409 RID: 17417 RVA: 0x000FB9C8 File Offset: 0x000F9BC8
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, ControlEvidence = true)]
		public override Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
		{
			RuntimeModule result = null;
			RuntimeAssembly.LoadModule(this.GetNativeHandle(), moduleName, rawModule, (rawModule != null) ? rawModule.Length : 0, rawSymbolStore, (rawSymbolStore != null) ? rawSymbolStore.Length : 0, JitHelpers.GetObjectHandleOnStack<RuntimeModule>(ref result));
			return result;
		}

		// Token: 0x0600440A RID: 17418
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetModule(RuntimeAssembly assembly, string name, ObjectHandleOnStack retModule);

		// Token: 0x0600440B RID: 17419 RVA: 0x000FBA00 File Offset: 0x000F9C00
		[SecuritySafeCritical]
		public override Module GetModule(string name)
		{
			Module result = null;
			RuntimeAssembly.GetModule(this.GetNativeHandle(), name, JitHelpers.GetObjectHandleOnStack<Module>(ref result));
			return result;
		}

		// Token: 0x0600440C RID: 17420 RVA: 0x000FBA24 File Offset: 0x000F9C24
		[SecuritySafeCritical]
		public override FileStream GetFile(string name)
		{
			RuntimeModule runtimeModule = (RuntimeModule)this.GetModule(name);
			if (runtimeModule == null)
			{
				return null;
			}
			return new FileStream(runtimeModule.GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
		}

		// Token: 0x0600440D RID: 17421 RVA: 0x000FBA60 File Offset: 0x000F9C60
		[SecuritySafeCritical]
		public override FileStream[] GetFiles(bool getResourceModules)
		{
			Module[] modules = this.GetModules(getResourceModules);
			int num = modules.Length;
			FileStream[] array = new FileStream[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = new FileStream(((RuntimeModule)modules[i]).GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
			}
			return array;
		}

		// Token: 0x0600440E RID: 17422
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern string[] GetManifestResourceNames(RuntimeAssembly assembly);

		// Token: 0x0600440F RID: 17423 RVA: 0x000FBAAB File Offset: 0x000F9CAB
		[SecuritySafeCritical]
		public override string[] GetManifestResourceNames()
		{
			return RuntimeAssembly.GetManifestResourceNames(this.GetNativeHandle());
		}

		// Token: 0x06004410 RID: 17424
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetExecutingAssembly(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

		// Token: 0x06004411 RID: 17425 RVA: 0x000FBAB8 File Offset: 0x000F9CB8
		[SecurityCritical]
		internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
		{
			RuntimeAssembly result = null;
			RuntimeAssembly.GetExecutingAssembly(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack<RuntimeAssembly>(ref result));
			return result;
		}

		// Token: 0x06004412 RID: 17426
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

		// Token: 0x06004413 RID: 17427 RVA: 0x000FBADA File Offset: 0x000F9CDA
		[SecuritySafeCritical]
		public override AssemblyName[] GetReferencedAssemblies()
		{
			return RuntimeAssembly.GetReferencedAssemblies(this.GetNativeHandle());
		}

		// Token: 0x06004414 RID: 17428
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetManifestResourceInfo(RuntimeAssembly assembly, string resourceName, ObjectHandleOnStack assemblyRef, StringHandleOnStack retFileName, StackCrawlMarkHandle stackMark);

		// Token: 0x06004415 RID: 17429 RVA: 0x000FBAE8 File Offset: 0x000F9CE8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override ManifestResourceInfo GetManifestResourceInfo(string resourceName)
		{
			RuntimeAssembly containingAssembly = null;
			string containingFileName = null;
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			int manifestResourceInfo = RuntimeAssembly.GetManifestResourceInfo(this.GetNativeHandle(), resourceName, JitHelpers.GetObjectHandleOnStack<RuntimeAssembly>(ref containingAssembly), JitHelpers.GetStringHandleOnStack(ref containingFileName), JitHelpers.GetStackCrawlMarkHandle(ref stackCrawlMark));
			if (manifestResourceInfo == -1)
			{
				return null;
			}
			return new ManifestResourceInfo(containingAssembly, containingFileName, (ResourceLocation)manifestResourceInfo);
		}

		// Token: 0x06004416 RID: 17430
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetLocation(RuntimeAssembly assembly, StringHandleOnStack retString);

		// Token: 0x17000A09 RID: 2569
		// (get) Token: 0x06004417 RID: 17431 RVA: 0x000FBB2C File Offset: 0x000F9D2C
		public override string Location
		{
			[SecuritySafeCritical]
			get
			{
				string text = null;
				RuntimeAssembly.GetLocation(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref text));
				if (text != null)
				{
					new FileIOPermission(FileIOPermissionAccess.PathDiscovery, text).Demand();
				}
				return text;
			}
		}

		// Token: 0x06004418 RID: 17432
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetImageRuntimeVersion(RuntimeAssembly assembly, StringHandleOnStack retString);

		// Token: 0x17000A0A RID: 2570
		// (get) Token: 0x06004419 RID: 17433 RVA: 0x000FBB60 File Offset: 0x000F9D60
		[ComVisible(false)]
		public override string ImageRuntimeVersion
		{
			[SecuritySafeCritical]
			get
			{
				string result = null;
				RuntimeAssembly.GetImageRuntimeVersion(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref result));
				return result;
			}
		}

		// Token: 0x0600441A RID: 17434
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsGlobalAssemblyCache(RuntimeAssembly assembly);

		// Token: 0x17000A0B RID: 2571
		// (get) Token: 0x0600441B RID: 17435 RVA: 0x000FBB82 File Offset: 0x000F9D82
		public override bool GlobalAssemblyCache
		{
			[SecuritySafeCritical]
			get
			{
				return RuntimeAssembly.IsGlobalAssemblyCache(this.GetNativeHandle());
			}
		}

		// Token: 0x0600441C RID: 17436
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern long GetHostContext(RuntimeAssembly assembly);

		// Token: 0x17000A0C RID: 2572
		// (get) Token: 0x0600441D RID: 17437 RVA: 0x000FBB8F File Offset: 0x000F9D8F
		public override long HostContext
		{
			[SecuritySafeCritical]
			get
			{
				return RuntimeAssembly.GetHostContext(this.GetNativeHandle());
			}
		}

		// Token: 0x0600441E RID: 17438 RVA: 0x000FBB9C File Offset: 0x000F9D9C
		private static string VerifyCodeBase(string codebase)
		{
			if (codebase == null)
			{
				return null;
			}
			int length = codebase.Length;
			if (length == 0)
			{
				return null;
			}
			int num = codebase.IndexOf(':');
			if (num != -1 && num + 2 < length && (codebase[num + 1] == '/' || codebase[num + 1] == '\\') && (codebase[num + 2] == '/' || codebase[num + 2] == '\\'))
			{
				return codebase;
			}
			if (length > 2 && codebase[0] == '\\' && codebase[1] == '\\')
			{
				return "file://" + codebase;
			}
			return "file:///" + Path.GetFullPathInternal(codebase);
		}

		// Token: 0x0600441F RID: 17439 RVA: 0x000FBC3C File Offset: 0x000F9E3C
		[SecurityCritical]
		internal Stream GetManifestResourceStream(Type type, string name, bool skipSecurityCheck, ref StackCrawlMark stackMark)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (type == null)
			{
				if (name == null)
				{
					throw new ArgumentNullException("type");
				}
			}
			else
			{
				string @namespace = type.Namespace;
				if (@namespace != null)
				{
					stringBuilder.Append(@namespace);
					if (name != null)
					{
						stringBuilder.Append(Type.Delimiter);
					}
				}
			}
			if (name != null)
			{
				stringBuilder.Append(name);
			}
			return this.GetManifestResourceStream(stringBuilder.ToString(), ref stackMark, skipSecurityCheck);
		}

		// Token: 0x17000A0D RID: 2573
		// (get) Token: 0x06004420 RID: 17440 RVA: 0x000FBCA1 File Offset: 0x000F9EA1
		internal bool IsStrongNameVerified
		{
			[SecurityCritical]
			get
			{
				return RuntimeAssembly.GetIsStrongNameVerified(this.GetNativeHandle());
			}
		}

		// Token: 0x06004421 RID: 17441
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern bool GetIsStrongNameVerified(RuntimeAssembly assembly);

		// Token: 0x06004422 RID: 17442
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private unsafe static extern byte* GetResource(RuntimeAssembly assembly, string resourceName, out ulong length, StackCrawlMarkHandle stackMark, bool skipSecurityCheck);

		// Token: 0x06004423 RID: 17443 RVA: 0x000FBCB0 File Offset: 0x000F9EB0
		[SecurityCritical]
		internal unsafe Stream GetManifestResourceStream(string name, ref StackCrawlMark stackMark, bool skipSecurityCheck)
		{
			ulong num = 0UL;
			byte* resource = RuntimeAssembly.GetResource(this.GetNativeHandle(), name, out num, JitHelpers.GetStackCrawlMarkHandle(ref stackMark), skipSecurityCheck);
			if (resource == null)
			{
				return null;
			}
			if (num > 9223372036854775807UL)
			{
				throw new NotImplementedException(Environment.GetResourceString("NotImplemented_ResourcesLongerThan2^63"));
			}
			return new UnmanagedMemoryStream(resource, (long)num, (long)num, FileAccess.Read, true);
		}

		// Token: 0x06004424 RID: 17444
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetVersion(RuntimeAssembly assembly, out int majVer, out int minVer, out int buildNum, out int revNum);

		// Token: 0x06004425 RID: 17445 RVA: 0x000FBD04 File Offset: 0x000F9F04
		[SecurityCritical]
		internal Version GetVersion()
		{
			int major;
			int minor;
			int build;
			int revision;
			RuntimeAssembly.GetVersion(this.GetNativeHandle(), out major, out minor, out build, out revision);
			return new Version(major, minor, build, revision);
		}

		// Token: 0x06004426 RID: 17446
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetLocale(RuntimeAssembly assembly, StringHandleOnStack retString);

		// Token: 0x06004427 RID: 17447 RVA: 0x000FBD30 File Offset: 0x000F9F30
		[SecurityCritical]
		internal CultureInfo GetLocale()
		{
			string text = null;
			RuntimeAssembly.GetLocale(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref text));
			if (text == null)
			{
				return CultureInfo.InvariantCulture;
			}
			return new CultureInfo(text);
		}

		// Token: 0x06004428 RID: 17448
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool FCallIsDynamic(RuntimeAssembly assembly);

		// Token: 0x17000A0E RID: 2574
		// (get) Token: 0x06004429 RID: 17449 RVA: 0x000FBD60 File Offset: 0x000F9F60
		public override bool IsDynamic
		{
			[SecuritySafeCritical]
			get
			{
				return RuntimeAssembly.FCallIsDynamic(this.GetNativeHandle());
			}
		}

		// Token: 0x0600442A RID: 17450 RVA: 0x000FBD70 File Offset: 0x000F9F70
		[SecurityCritical]
		private void VerifyCodeBaseDiscovery(string codeBase)
		{
			if (CodeAccessSecurityEngine.QuickCheckForAllDemands())
			{
				return;
			}
			if (codeBase != null && string.Compare(codeBase, 0, "file:", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
			{
				URLString urlstring = new URLString(codeBase, true);
				new FileIOPermission(FileIOPermissionAccess.PathDiscovery, urlstring.GetFileName()).Demand();
			}
		}

		// Token: 0x0600442B RID: 17451
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetSimpleName(RuntimeAssembly assembly, StringHandleOnStack retSimpleName);

		// Token: 0x0600442C RID: 17452 RVA: 0x000FBDB4 File Offset: 0x000F9FB4
		[SecuritySafeCritical]
		internal string GetSimpleName()
		{
			string result = null;
			RuntimeAssembly.GetSimpleName(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref result));
			return result;
		}

		// Token: 0x0600442D RID: 17453
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern AssemblyHashAlgorithm GetHashAlgorithm(RuntimeAssembly assembly);

		// Token: 0x0600442E RID: 17454 RVA: 0x000FBDD6 File Offset: 0x000F9FD6
		[SecurityCritical]
		private AssemblyHashAlgorithm GetHashAlgorithm()
		{
			return RuntimeAssembly.GetHashAlgorithm(this.GetNativeHandle());
		}

		// Token: 0x0600442F RID: 17455
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern AssemblyNameFlags GetFlags(RuntimeAssembly assembly);

		// Token: 0x06004430 RID: 17456 RVA: 0x000FBDE3 File Offset: 0x000F9FE3
		[SecurityCritical]
		private AssemblyNameFlags GetFlags()
		{
			return RuntimeAssembly.GetFlags(this.GetNativeHandle());
		}

		// Token: 0x06004431 RID: 17457
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetRawBytes(RuntimeAssembly assembly, ObjectHandleOnStack retRawBytes);

		// Token: 0x06004432 RID: 17458 RVA: 0x000FBDF0 File Offset: 0x000F9FF0
		[SecuritySafeCritical]
		internal byte[] GetRawBytes()
		{
			byte[] result = null;
			RuntimeAssembly.GetRawBytes(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<byte[]>(ref result));
			return result;
		}

		// Token: 0x06004433 RID: 17459
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetPublicKey(RuntimeAssembly assembly, ObjectHandleOnStack retPublicKey);

		// Token: 0x06004434 RID: 17460 RVA: 0x000FBE14 File Offset: 0x000FA014
		[SecurityCritical]
		internal byte[] GetPublicKey()
		{
			byte[] result = null;
			RuntimeAssembly.GetPublicKey(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<byte[]>(ref result));
			return result;
		}

		// Token: 0x06004435 RID: 17461
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetGrantSet(RuntimeAssembly assembly, ObjectHandleOnStack granted, ObjectHandleOnStack denied);

		// Token: 0x06004436 RID: 17462 RVA: 0x000FBE38 File Offset: 0x000FA038
		[SecurityCritical]
		internal void GetGrantSet(out PermissionSet newGrant, out PermissionSet newDenied)
		{
			PermissionSet permissionSet = null;
			PermissionSet permissionSet2 = null;
			RuntimeAssembly.GetGrantSet(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet2));
			newGrant = permissionSet;
			newDenied = permissionSet2;
		}

		// Token: 0x06004437 RID: 17463
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsAllSecurityCritical(RuntimeAssembly assembly);

		// Token: 0x06004438 RID: 17464 RVA: 0x000FBE68 File Offset: 0x000FA068
		[SecuritySafeCritical]
		internal bool IsAllSecurityCritical()
		{
			return RuntimeAssembly.IsAllSecurityCritical(this.GetNativeHandle());
		}

		// Token: 0x06004439 RID: 17465
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsAllSecuritySafeCritical(RuntimeAssembly assembly);

		// Token: 0x0600443A RID: 17466 RVA: 0x000FBE75 File Offset: 0x000FA075
		[SecuritySafeCritical]
		internal bool IsAllSecuritySafeCritical()
		{
			return RuntimeAssembly.IsAllSecuritySafeCritical(this.GetNativeHandle());
		}

		// Token: 0x0600443B RID: 17467
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsAllPublicAreaSecuritySafeCritical(RuntimeAssembly assembly);

		// Token: 0x0600443C RID: 17468 RVA: 0x000FBE82 File Offset: 0x000FA082
		[SecuritySafeCritical]
		internal bool IsAllPublicAreaSecuritySafeCritical()
		{
			return RuntimeAssembly.IsAllPublicAreaSecuritySafeCritical(this.GetNativeHandle());
		}

		// Token: 0x0600443D RID: 17469
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsAllSecurityTransparent(RuntimeAssembly assembly);

		// Token: 0x0600443E RID: 17470 RVA: 0x000FBE8F File Offset: 0x000FA08F
		[SecuritySafeCritical]
		internal bool IsAllSecurityTransparent()
		{
			return RuntimeAssembly.IsAllSecurityTransparent(this.GetNativeHandle());
		}

		// Token: 0x0600443F RID: 17471 RVA: 0x000FBE9C File Offset: 0x000FA09C
		[SecurityCritical]
		private static void DemandPermission(string codeBase, bool havePath, int demandFlag)
		{
			FileIOPermissionAccess access = FileIOPermissionAccess.PathDiscovery;
			switch (demandFlag)
			{
			case 1:
				access = FileIOPermissionAccess.Read;
				break;
			case 2:
				access = FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery;
				break;
			case 3:
			{
				IPermission permission = RuntimeAssembly.CreateWebPermission(AssemblyName.EscapeCodeBase(codeBase));
				permission.Demand();
				return;
			}
			}
			if (!havePath)
			{
				URLString urlstring = new URLString(codeBase, true);
				codeBase = urlstring.GetFileName();
			}
			codeBase = Path.GetFullPathInternal(codeBase);
			new FileIOPermission(access, codeBase).Demand();
		}

		// Token: 0x06004440 RID: 17472 RVA: 0x000FBF08 File Offset: 0x000FA108
		private static IPermission CreateWebPermission(string codeBase)
		{
			Assembly assembly = Assembly.Load("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Type type = assembly.GetType("System.Net.NetworkAccess", true);
			IPermission permission = null;
			if (type.IsEnum && type.IsVisible)
			{
				object[] array = new object[2];
				array[0] = (Enum)Enum.Parse(type, "Connect", true);
				if (array[0] != null)
				{
					array[1] = codeBase;
					type = assembly.GetType("System.Net.WebPermission", true);
					if (type.IsVisible)
					{
						permission = (IPermission)Activator.CreateInstance(type, array);
					}
				}
			}
			if (permission == null)
			{
				throw new InvalidOperationException();
			}
			return permission;
		}

		// Token: 0x06004441 RID: 17473 RVA: 0x000FBF90 File Offset: 0x000FA190
		[SecurityCritical]
		private RuntimeModule OnModuleResolveEvent(string moduleName)
		{
			ModuleResolveEventHandler moduleResolve = this._ModuleResolve;
			if (moduleResolve == null)
			{
				return null;
			}
			Delegate[] invocationList = moduleResolve.GetInvocationList();
			int num = invocationList.Length;
			for (int i = 0; i < num; i++)
			{
				RuntimeModule runtimeModule = (RuntimeModule)((ModuleResolveEventHandler)invocationList[i])(this, new ResolveEventArgs(moduleName, this));
				if (runtimeModule != null)
				{
					return runtimeModule;
				}
			}
			return null;
		}

		// Token: 0x06004442 RID: 17474 RVA: 0x000FBFEC File Offset: 0x000FA1EC
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override Assembly GetSatelliteAssembly(CultureInfo culture)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalGetSatelliteAssembly(culture, null, ref stackCrawlMark);
		}

		// Token: 0x06004443 RID: 17475 RVA: 0x000FC008 File Offset: 0x000FA208
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.InternalGetSatelliteAssembly(culture, version, ref stackCrawlMark);
		}

		// Token: 0x06004444 RID: 17476 RVA: 0x000FC024 File Offset: 0x000FA224
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal Assembly InternalGetSatelliteAssembly(CultureInfo culture, Version version, ref StackCrawlMark stackMark)
		{
			if (culture == null)
			{
				throw new ArgumentNullException("culture");
			}
			string name = this.GetSimpleName() + ".resources";
			return this.InternalGetSatelliteAssembly(name, culture, version, true, ref stackMark);
		}

		// Token: 0x06004445 RID: 17477
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UseRelativeBindForSatellites();

		// Token: 0x06004446 RID: 17478 RVA: 0x000FC05C File Offset: 0x000FA25C
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal RuntimeAssembly InternalGetSatelliteAssembly(string name, CultureInfo culture, Version version, bool throwOnFileNotFound, ref StackCrawlMark stackMark)
		{
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.SetPublicKey(this.GetPublicKey());
			assemblyName.Flags = this.GetFlags() | AssemblyNameFlags.PublicKey;
			if (version == null)
			{
				assemblyName.Version = this.GetVersion();
			}
			else
			{
				assemblyName.Version = version;
			}
			assemblyName.CultureInfo = culture;
			assemblyName.Name = name;
			RuntimeAssembly runtimeAssembly = null;
			bool flag = AppDomain.IsAppXDesignMode();
			bool flag2 = false;
			if (CodeAccessSecurityEngine.QuickCheckForAllDemands())
			{
				flag2 = this.IsFrameworkAssembly() || RuntimeAssembly.UseRelativeBindForSatellites();
			}
			if (flag || flag2)
			{
				if (this.GlobalAssemblyCache)
				{
					ArrayList arrayList = new ArrayList();
					bool flag3 = false;
					try
					{
						Fusion.ReadCache(arrayList, assemblyName.FullName, 2U);
					}
					catch (Exception ex)
					{
						if (ex.IsTransient)
						{
							throw;
						}
						if (!AppDomain.IsAppXModel())
						{
							flag3 = true;
						}
					}
					if (arrayList.Count > 0 || flag3)
					{
						runtimeAssembly = RuntimeAssembly.nLoad(assemblyName, null, null, this, ref stackMark, IntPtr.Zero, throwOnFileNotFound, false, false);
					}
				}
				else
				{
					string codeBase = this.CodeBase;
					if (codeBase != null && string.Compare(codeBase, 0, "file:", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
					{
						runtimeAssembly = this.InternalProbeForSatelliteAssemblyNextToParentAssembly(assemblyName, name, codeBase, culture, throwOnFileNotFound, flag, ref stackMark);
						if (runtimeAssembly != null && !RuntimeAssembly.IsSimplyNamed(assemblyName))
						{
							AssemblyName name2 = runtimeAssembly.GetName();
							if (!AssemblyName.ReferenceMatchesDefinitionInternal(assemblyName, name2, false))
							{
								runtimeAssembly = null;
							}
						}
					}
					else if (!flag)
					{
						runtimeAssembly = RuntimeAssembly.nLoad(assemblyName, null, null, this, ref stackMark, IntPtr.Zero, throwOnFileNotFound, false, false);
					}
				}
			}
			else
			{
				runtimeAssembly = RuntimeAssembly.nLoad(assemblyName, null, null, this, ref stackMark, IntPtr.Zero, throwOnFileNotFound, false, false);
			}
			if (runtimeAssembly == this || (runtimeAssembly == null && throwOnFileNotFound))
			{
				throw new FileNotFoundException(string.Format(culture, Environment.GetResourceString("IO.FileNotFound_FileName"), assemblyName.Name));
			}
			return runtimeAssembly;
		}

		// Token: 0x06004447 RID: 17479 RVA: 0x000FC210 File Offset: 0x000FA410
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private RuntimeAssembly InternalProbeForSatelliteAssemblyNextToParentAssembly(AssemblyName an, string name, string codeBase, CultureInfo culture, bool throwOnFileNotFound, bool useLoadFile, ref StackCrawlMark stackMark)
		{
			RuntimeAssembly runtimeAssembly = null;
			string text = null;
			if (useLoadFile)
			{
				text = this.Location;
			}
			FileNotFoundException ex = null;
			StringBuilder stringBuilder = new StringBuilder(useLoadFile ? text : codeBase, 0, useLoadFile ? (text.LastIndexOf('\\') + 1) : (codeBase.LastIndexOf('/') + 1), 260);
			stringBuilder.Append(an.CultureInfo.Name);
			stringBuilder.Append(useLoadFile ? '\\' : '/');
			stringBuilder.Append(name);
			stringBuilder.Append(".DLL");
			string text2 = stringBuilder.ToString();
			AssemblyName assemblyName = null;
			if (!useLoadFile)
			{
				assemblyName = new AssemblyName();
				assemblyName.CodeBase = text2;
			}
			try
			{
				try
				{
					runtimeAssembly = (useLoadFile ? RuntimeAssembly.nLoadFile(text2, null) : RuntimeAssembly.nLoad(assemblyName, text2, null, this, ref stackMark, IntPtr.Zero, throwOnFileNotFound, false, false));
				}
				catch (FileNotFoundException)
				{
					ex = new FileNotFoundException(string.Format(culture, Environment.GetResourceString("IO.FileNotFound_FileName"), text2), text2);
					runtimeAssembly = null;
				}
				if (runtimeAssembly == null)
				{
					stringBuilder.Remove(stringBuilder.Length - 4, 4);
					stringBuilder.Append(".EXE");
					text2 = stringBuilder.ToString();
					if (!useLoadFile)
					{
						assemblyName.CodeBase = text2;
					}
					try
					{
						runtimeAssembly = (useLoadFile ? RuntimeAssembly.nLoadFile(text2, null) : RuntimeAssembly.nLoad(assemblyName, text2, null, this, ref stackMark, IntPtr.Zero, false, false, false));
					}
					catch (FileNotFoundException)
					{
						runtimeAssembly = null;
					}
					if (runtimeAssembly == null && throwOnFileNotFound)
					{
						throw ex;
					}
				}
			}
			catch (DirectoryNotFoundException)
			{
				if (throwOnFileNotFound)
				{
					throw;
				}
				runtimeAssembly = null;
			}
			return runtimeAssembly;
		}

		// Token: 0x06004448 RID: 17480
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeAssembly nLoadFile(string path, Evidence evidence);

		// Token: 0x06004449 RID: 17481
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeAssembly nLoadImage(byte[] rawAssembly, byte[] rawSymbolStore, Evidence evidence, ref StackCrawlMark stackMark, bool fIntrospection, bool fSkipIntegrityCheck, SecurityContextSource securityContextSource);

		// Token: 0x0600444A RID: 17482
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetModules(RuntimeAssembly assembly, bool loadIfNotFound, bool getResourceModules, ObjectHandleOnStack retModuleHandles);

		// Token: 0x0600444B RID: 17483 RVA: 0x000FC3A4 File Offset: 0x000FA5A4
		[SecuritySafeCritical]
		private RuntimeModule[] GetModulesInternal(bool loadIfNotFound, bool getResourceModules)
		{
			RuntimeModule[] result = null;
			RuntimeAssembly.GetModules(this.GetNativeHandle(), loadIfNotFound, getResourceModules, JitHelpers.GetObjectHandleOnStack<RuntimeModule[]>(ref result));
			return result;
		}

		// Token: 0x0600444C RID: 17484 RVA: 0x000FC3C8 File Offset: 0x000FA5C8
		public override Module[] GetModules(bool getResourceModules)
		{
			return this.GetModulesInternal(true, getResourceModules);
		}

		// Token: 0x0600444D RID: 17485 RVA: 0x000FC3E0 File Offset: 0x000FA5E0
		public override Module[] GetLoadedModules(bool getResourceModules)
		{
			return this.GetModulesInternal(false, getResourceModules);
		}

		// Token: 0x0600444E RID: 17486
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);

		// Token: 0x0600444F RID: 17487
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool AptcaCheck(RuntimeAssembly targetAssembly, RuntimeAssembly sourceAssembly);

		// Token: 0x06004450 RID: 17488
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetToken(RuntimeAssembly assembly);

		// Token: 0x04001BF6 RID: 7158
		private const uint COR_E_LOADING_REFERENCE_ASSEMBLY = 2148733016U;

		// Token: 0x04001BF8 RID: 7160
		private string m_fullname;

		// Token: 0x04001BF9 RID: 7161
		private object m_syncRoot;

		// Token: 0x04001BFA RID: 7162
		private IntPtr m_assembly;

		// Token: 0x04001BFB RID: 7163
		private RuntimeAssembly.ASSEMBLY_FLAGS m_flags;

		// Token: 0x04001BFC RID: 7164
		private const string s_localFilePrefix = "file:";

		// Token: 0x04001BFD RID: 7165
		private static string[] s_unsafeFrameworkAssemblyNames = new string[] { "System.Reflection.Context", "Microsoft.VisualBasic" };

		// Token: 0x02000C38 RID: 3128
		private enum ASSEMBLY_FLAGS : uint
		{
			// Token: 0x04003731 RID: 14129
			ASSEMBLY_FLAGS_UNKNOWN,
			// Token: 0x04003732 RID: 14130
			ASSEMBLY_FLAGS_INITIALIZED = 16777216U,
			// Token: 0x04003733 RID: 14131
			ASSEMBLY_FLAGS_FRAMEWORK = 33554432U,
			// Token: 0x04003734 RID: 14132
			ASSEMBLY_FLAGS_SAFE_REFLECTION = 67108864U,
			// Token: 0x04003735 RID: 14133
			ASSEMBLY_FLAGS_TOKEN_MASK = 16777215U
		}
	}
}
