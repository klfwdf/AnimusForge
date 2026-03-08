using System;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000908 RID: 2312
	[Guid("8A7C1442-A9FB-366B-80D8-4939FFA6DBE0")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(FieldInfo))]
	[ComVisible(true)]
	public interface _FieldInfo
	{
		// Token: 0x06005F7C RID: 24444
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005F7D RID: 24445
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005F7E RID: 24446
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005F7F RID: 24447
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x06005F80 RID: 24448
		string ToString();

		// Token: 0x06005F81 RID: 24449
		bool Equals(object other);

		// Token: 0x06005F82 RID: 24450
		int GetHashCode();

		// Token: 0x06005F83 RID: 24451
		Type GetType();

		// Token: 0x170010AA RID: 4266
		// (get) Token: 0x06005F84 RID: 24452
		MemberTypes MemberType { get; }

		// Token: 0x170010AB RID: 4267
		// (get) Token: 0x06005F85 RID: 24453
		string Name { get; }

		// Token: 0x170010AC RID: 4268
		// (get) Token: 0x06005F86 RID: 24454
		Type DeclaringType { get; }

		// Token: 0x170010AD RID: 4269
		// (get) Token: 0x06005F87 RID: 24455
		Type ReflectedType { get; }

		// Token: 0x06005F88 RID: 24456
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06005F89 RID: 24457
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06005F8A RID: 24458
		bool IsDefined(Type attributeType, bool inherit);

		// Token: 0x170010AE RID: 4270
		// (get) Token: 0x06005F8B RID: 24459
		Type FieldType { get; }

		// Token: 0x06005F8C RID: 24460
		object GetValue(object obj);

		// Token: 0x06005F8D RID: 24461
		object GetValueDirect(TypedReference obj);

		// Token: 0x06005F8E RID: 24462
		void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);

		// Token: 0x06005F8F RID: 24463
		void SetValueDirect(TypedReference obj, object value);

		// Token: 0x170010AF RID: 4271
		// (get) Token: 0x06005F90 RID: 24464
		RuntimeFieldHandle FieldHandle { get; }

		// Token: 0x170010B0 RID: 4272
		// (get) Token: 0x06005F91 RID: 24465
		FieldAttributes Attributes { get; }

		// Token: 0x06005F92 RID: 24466
		void SetValue(object obj, object value);

		// Token: 0x170010B1 RID: 4273
		// (get) Token: 0x06005F93 RID: 24467
		bool IsPublic { get; }

		// Token: 0x170010B2 RID: 4274
		// (get) Token: 0x06005F94 RID: 24468
		bool IsPrivate { get; }

		// Token: 0x170010B3 RID: 4275
		// (get) Token: 0x06005F95 RID: 24469
		bool IsFamily { get; }

		// Token: 0x170010B4 RID: 4276
		// (get) Token: 0x06005F96 RID: 24470
		bool IsAssembly { get; }

		// Token: 0x170010B5 RID: 4277
		// (get) Token: 0x06005F97 RID: 24471
		bool IsFamilyAndAssembly { get; }

		// Token: 0x170010B6 RID: 4278
		// (get) Token: 0x06005F98 RID: 24472
		bool IsFamilyOrAssembly { get; }

		// Token: 0x170010B7 RID: 4279
		// (get) Token: 0x06005F99 RID: 24473
		bool IsStatic { get; }

		// Token: 0x170010B8 RID: 4280
		// (get) Token: 0x06005F9A RID: 24474
		bool IsInitOnly { get; }

		// Token: 0x170010B9 RID: 4281
		// (get) Token: 0x06005F9B RID: 24475
		bool IsLiteral { get; }

		// Token: 0x170010BA RID: 4282
		// (get) Token: 0x06005F9C RID: 24476
		bool IsNotSerialized { get; }

		// Token: 0x170010BB RID: 4283
		// (get) Token: 0x06005F9D RID: 24477
		bool IsSpecialName { get; }

		// Token: 0x170010BC RID: 4284
		// (get) Token: 0x06005F9E RID: 24478
		bool IsPinvokeImpl { get; }
	}
}
