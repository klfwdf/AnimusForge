using System;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000905 RID: 2309
	[Guid("6240837A-707F-3181-8E98-A36AE086766B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(MethodBase))]
	[ComVisible(true)]
	public interface _MethodBase
	{
		// Token: 0x06005F0E RID: 24334
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005F0F RID: 24335
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005F10 RID: 24336
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005F11 RID: 24337
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x06005F12 RID: 24338
		string ToString();

		// Token: 0x06005F13 RID: 24339
		bool Equals(object other);

		// Token: 0x06005F14 RID: 24340
		int GetHashCode();

		// Token: 0x06005F15 RID: 24341
		Type GetType();

		// Token: 0x1700106C RID: 4204
		// (get) Token: 0x06005F16 RID: 24342
		MemberTypes MemberType { get; }

		// Token: 0x1700106D RID: 4205
		// (get) Token: 0x06005F17 RID: 24343
		string Name { get; }

		// Token: 0x1700106E RID: 4206
		// (get) Token: 0x06005F18 RID: 24344
		Type DeclaringType { get; }

		// Token: 0x1700106F RID: 4207
		// (get) Token: 0x06005F19 RID: 24345
		Type ReflectedType { get; }

		// Token: 0x06005F1A RID: 24346
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06005F1B RID: 24347
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06005F1C RID: 24348
		bool IsDefined(Type attributeType, bool inherit);

		// Token: 0x06005F1D RID: 24349
		ParameterInfo[] GetParameters();

		// Token: 0x06005F1E RID: 24350
		MethodImplAttributes GetMethodImplementationFlags();

		// Token: 0x17001070 RID: 4208
		// (get) Token: 0x06005F1F RID: 24351
		RuntimeMethodHandle MethodHandle { get; }

		// Token: 0x17001071 RID: 4209
		// (get) Token: 0x06005F20 RID: 24352
		MethodAttributes Attributes { get; }

		// Token: 0x17001072 RID: 4210
		// (get) Token: 0x06005F21 RID: 24353
		CallingConventions CallingConvention { get; }

		// Token: 0x06005F22 RID: 24354
		object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture);

		// Token: 0x17001073 RID: 4211
		// (get) Token: 0x06005F23 RID: 24355
		bool IsPublic { get; }

		// Token: 0x17001074 RID: 4212
		// (get) Token: 0x06005F24 RID: 24356
		bool IsPrivate { get; }

		// Token: 0x17001075 RID: 4213
		// (get) Token: 0x06005F25 RID: 24357
		bool IsFamily { get; }

		// Token: 0x17001076 RID: 4214
		// (get) Token: 0x06005F26 RID: 24358
		bool IsAssembly { get; }

		// Token: 0x17001077 RID: 4215
		// (get) Token: 0x06005F27 RID: 24359
		bool IsFamilyAndAssembly { get; }

		// Token: 0x17001078 RID: 4216
		// (get) Token: 0x06005F28 RID: 24360
		bool IsFamilyOrAssembly { get; }

		// Token: 0x17001079 RID: 4217
		// (get) Token: 0x06005F29 RID: 24361
		bool IsStatic { get; }

		// Token: 0x1700107A RID: 4218
		// (get) Token: 0x06005F2A RID: 24362
		bool IsFinal { get; }

		// Token: 0x1700107B RID: 4219
		// (get) Token: 0x06005F2B RID: 24363
		bool IsVirtual { get; }

		// Token: 0x1700107C RID: 4220
		// (get) Token: 0x06005F2C RID: 24364
		bool IsHideBySig { get; }

		// Token: 0x1700107D RID: 4221
		// (get) Token: 0x06005F2D RID: 24365
		bool IsAbstract { get; }

		// Token: 0x1700107E RID: 4222
		// (get) Token: 0x06005F2E RID: 24366
		bool IsSpecialName { get; }

		// Token: 0x1700107F RID: 4223
		// (get) Token: 0x06005F2F RID: 24367
		bool IsConstructor { get; }

		// Token: 0x06005F30 RID: 24368
		object Invoke(object obj, object[] parameters);
	}
}
