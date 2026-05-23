using System;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000906 RID: 2310
	[Guid("FFCC1B5D-ECB8-38DD-9B01-3DC8ABC2AA5F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(MethodInfo))]
	[ComVisible(true)]
	public interface _MethodInfo
	{
		// Token: 0x06005F31 RID: 24369
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005F32 RID: 24370
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005F33 RID: 24371
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005F34 RID: 24372
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x06005F35 RID: 24373
		string ToString();

		// Token: 0x06005F36 RID: 24374
		bool Equals(object other);

		// Token: 0x06005F37 RID: 24375
		int GetHashCode();

		// Token: 0x06005F38 RID: 24376
		Type GetType();

		// Token: 0x17001080 RID: 4224
		// (get) Token: 0x06005F39 RID: 24377
		MemberTypes MemberType { get; }

		// Token: 0x17001081 RID: 4225
		// (get) Token: 0x06005F3A RID: 24378
		string Name { get; }

		// Token: 0x17001082 RID: 4226
		// (get) Token: 0x06005F3B RID: 24379
		Type DeclaringType { get; }

		// Token: 0x17001083 RID: 4227
		// (get) Token: 0x06005F3C RID: 24380
		Type ReflectedType { get; }

		// Token: 0x06005F3D RID: 24381
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06005F3E RID: 24382
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06005F3F RID: 24383
		bool IsDefined(Type attributeType, bool inherit);

		// Token: 0x06005F40 RID: 24384
		ParameterInfo[] GetParameters();

		// Token: 0x06005F41 RID: 24385
		MethodImplAttributes GetMethodImplementationFlags();

		// Token: 0x17001084 RID: 4228
		// (get) Token: 0x06005F42 RID: 24386
		RuntimeMethodHandle MethodHandle { get; }

		// Token: 0x17001085 RID: 4229
		// (get) Token: 0x06005F43 RID: 24387
		MethodAttributes Attributes { get; }

		// Token: 0x17001086 RID: 4230
		// (get) Token: 0x06005F44 RID: 24388
		CallingConventions CallingConvention { get; }

		// Token: 0x06005F45 RID: 24389
		object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture);

		// Token: 0x17001087 RID: 4231
		// (get) Token: 0x06005F46 RID: 24390
		bool IsPublic { get; }

		// Token: 0x17001088 RID: 4232
		// (get) Token: 0x06005F47 RID: 24391
		bool IsPrivate { get; }

		// Token: 0x17001089 RID: 4233
		// (get) Token: 0x06005F48 RID: 24392
		bool IsFamily { get; }

		// Token: 0x1700108A RID: 4234
		// (get) Token: 0x06005F49 RID: 24393
		bool IsAssembly { get; }

		// Token: 0x1700108B RID: 4235
		// (get) Token: 0x06005F4A RID: 24394
		bool IsFamilyAndAssembly { get; }

		// Token: 0x1700108C RID: 4236
		// (get) Token: 0x06005F4B RID: 24395
		bool IsFamilyOrAssembly { get; }

		// Token: 0x1700108D RID: 4237
		// (get) Token: 0x06005F4C RID: 24396
		bool IsStatic { get; }

		// Token: 0x1700108E RID: 4238
		// (get) Token: 0x06005F4D RID: 24397
		bool IsFinal { get; }

		// Token: 0x1700108F RID: 4239
		// (get) Token: 0x06005F4E RID: 24398
		bool IsVirtual { get; }

		// Token: 0x17001090 RID: 4240
		// (get) Token: 0x06005F4F RID: 24399
		bool IsHideBySig { get; }

		// Token: 0x17001091 RID: 4241
		// (get) Token: 0x06005F50 RID: 24400
		bool IsAbstract { get; }

		// Token: 0x17001092 RID: 4242
		// (get) Token: 0x06005F51 RID: 24401
		bool IsSpecialName { get; }

		// Token: 0x17001093 RID: 4243
		// (get) Token: 0x06005F52 RID: 24402
		bool IsConstructor { get; }

		// Token: 0x06005F53 RID: 24403
		object Invoke(object obj, object[] parameters);

		// Token: 0x17001094 RID: 4244
		// (get) Token: 0x06005F54 RID: 24404
		Type ReturnType { get; }

		// Token: 0x17001095 RID: 4245
		// (get) Token: 0x06005F55 RID: 24405
		ICustomAttributeProvider ReturnTypeCustomAttributes { get; }

		// Token: 0x06005F56 RID: 24406
		MethodInfo GetBaseDefinition();
	}
}
