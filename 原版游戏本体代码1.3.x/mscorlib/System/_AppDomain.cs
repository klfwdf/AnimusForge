using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Policy;
using System.Security.Principal;

namespace System
{
	// Token: 0x0200009D RID: 157
	[Guid("05F696DC-2B29-3663-AD8B-C4389CF2A713")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[ComVisible(true)]
	public interface _AppDomain
	{
		// Token: 0x060008A7 RID: 2215
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x060008A8 RID: 2216
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x060008A9 RID: 2217
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x060008AA RID: 2218
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x060008AB RID: 2219
		string ToString();

		// Token: 0x060008AC RID: 2220
		bool Equals(object other);

		// Token: 0x060008AD RID: 2221
		int GetHashCode();

		// Token: 0x060008AE RID: 2222
		Type GetType();

		// Token: 0x060008AF RID: 2223
		[SecurityCritical]
		object InitializeLifetimeService();

		// Token: 0x060008B0 RID: 2224
		[SecurityCritical]
		object GetLifetimeService();

		// Token: 0x170000EE RID: 238
		// (get) Token: 0x060008B1 RID: 2225
		Evidence Evidence { get; }

		// Token: 0x1400000B RID: 11
		// (add) Token: 0x060008B2 RID: 2226
		// (remove) Token: 0x060008B3 RID: 2227
		event EventHandler DomainUnload;

		// Token: 0x1400000C RID: 12
		// (add) Token: 0x060008B4 RID: 2228
		// (remove) Token: 0x060008B5 RID: 2229
		event AssemblyLoadEventHandler AssemblyLoad;

		// Token: 0x1400000D RID: 13
		// (add) Token: 0x060008B6 RID: 2230
		// (remove) Token: 0x060008B7 RID: 2231
		event EventHandler ProcessExit;

		// Token: 0x1400000E RID: 14
		// (add) Token: 0x060008B8 RID: 2232
		// (remove) Token: 0x060008B9 RID: 2233
		event ResolveEventHandler TypeResolve;

		// Token: 0x1400000F RID: 15
		// (add) Token: 0x060008BA RID: 2234
		// (remove) Token: 0x060008BB RID: 2235
		event ResolveEventHandler ResourceResolve;

		// Token: 0x14000010 RID: 16
		// (add) Token: 0x060008BC RID: 2236
		// (remove) Token: 0x060008BD RID: 2237
		event ResolveEventHandler AssemblyResolve;

		// Token: 0x14000011 RID: 17
		// (add) Token: 0x060008BE RID: 2238
		// (remove) Token: 0x060008BF RID: 2239
		event UnhandledExceptionEventHandler UnhandledException;

		// Token: 0x060008C0 RID: 2240
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access);

		// Token: 0x060008C1 RID: 2241
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir);

		// Token: 0x060008C2 RID: 2242
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, Evidence evidence);

		// Token: 0x060008C3 RID: 2243
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions);

		// Token: 0x060008C4 RID: 2244
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence);

		// Token: 0x060008C5 RID: 2245
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions);

		// Token: 0x060008C6 RID: 2246
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions);

		// Token: 0x060008C7 RID: 2247
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions);

		// Token: 0x060008C8 RID: 2248
		AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, string dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, bool isSynchronized);

		// Token: 0x060008C9 RID: 2249
		ObjectHandle CreateInstance(string assemblyName, string typeName);

		// Token: 0x060008CA RID: 2250
		ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName);

		// Token: 0x060008CB RID: 2251
		ObjectHandle CreateInstance(string assemblyName, string typeName, object[] activationAttributes);

		// Token: 0x060008CC RID: 2252
		ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, object[] activationAttributes);

		// Token: 0x060008CD RID: 2253
		ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes);

		// Token: 0x060008CE RID: 2254
		ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, Evidence securityAttributes);

		// Token: 0x060008CF RID: 2255
		Assembly Load(AssemblyName assemblyRef);

		// Token: 0x060008D0 RID: 2256
		Assembly Load(string assemblyString);

		// Token: 0x060008D1 RID: 2257
		Assembly Load(byte[] rawAssembly);

		// Token: 0x060008D2 RID: 2258
		Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore);

		// Token: 0x060008D3 RID: 2259
		Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, Evidence securityEvidence);

		// Token: 0x060008D4 RID: 2260
		Assembly Load(AssemblyName assemblyRef, Evidence assemblySecurity);

		// Token: 0x060008D5 RID: 2261
		Assembly Load(string assemblyString, Evidence assemblySecurity);

		// Token: 0x060008D6 RID: 2262
		int ExecuteAssembly(string assemblyFile, Evidence assemblySecurity);

		// Token: 0x060008D7 RID: 2263
		int ExecuteAssembly(string assemblyFile);

		// Token: 0x060008D8 RID: 2264
		int ExecuteAssembly(string assemblyFile, Evidence assemblySecurity, string[] args);

		// Token: 0x170000EF RID: 239
		// (get) Token: 0x060008D9 RID: 2265
		string FriendlyName { get; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x060008DA RID: 2266
		string BaseDirectory { get; }

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x060008DB RID: 2267
		string RelativeSearchPath { get; }

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x060008DC RID: 2268
		bool ShadowCopyFiles { get; }

		// Token: 0x060008DD RID: 2269
		Assembly[] GetAssemblies();

		// Token: 0x060008DE RID: 2270
		[SecurityCritical]
		void AppendPrivatePath(string path);

		// Token: 0x060008DF RID: 2271
		[SecurityCritical]
		void ClearPrivatePath();

		// Token: 0x060008E0 RID: 2272
		[SecurityCritical]
		void SetShadowCopyPath(string s);

		// Token: 0x060008E1 RID: 2273
		[SecurityCritical]
		void ClearShadowCopyPath();

		// Token: 0x060008E2 RID: 2274
		[SecurityCritical]
		void SetCachePath(string s);

		// Token: 0x060008E3 RID: 2275
		[SecurityCritical]
		void SetData(string name, object data);

		// Token: 0x060008E4 RID: 2276
		object GetData(string name);

		// Token: 0x060008E5 RID: 2277
		[SecurityCritical]
		void SetAppDomainPolicy(PolicyLevel domainPolicy);

		// Token: 0x060008E6 RID: 2278
		void SetThreadPrincipal(IPrincipal principal);

		// Token: 0x060008E7 RID: 2279
		void SetPrincipalPolicy(PrincipalPolicy policy);

		// Token: 0x060008E8 RID: 2280
		void DoCallBack(CrossAppDomainDelegate theDelegate);

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x060008E9 RID: 2281
		string DynamicDirectory { get; }
	}
}
