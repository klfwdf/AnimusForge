using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000904 RID: 2308
	[Guid("f7102fa9-cabb-3a74-a6da-b4567ef1b079")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[TypeLibImportClass(typeof(MemberInfo))]
	[CLSCompliant(false)]
	[ComVisible(true)]
	public interface _MemberInfo
	{
		// Token: 0x06005EFF RID: 24319
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005F00 RID: 24320
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005F01 RID: 24321
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005F02 RID: 24322
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x06005F03 RID: 24323
		string ToString();

		// Token: 0x06005F04 RID: 24324
		bool Equals(object other);

		// Token: 0x06005F05 RID: 24325
		int GetHashCode();

		// Token: 0x06005F06 RID: 24326
		Type GetType();

		// Token: 0x17001068 RID: 4200
		// (get) Token: 0x06005F07 RID: 24327
		MemberTypes MemberType { get; }

		// Token: 0x17001069 RID: 4201
		// (get) Token: 0x06005F08 RID: 24328
		string Name { get; }

		// Token: 0x1700106A RID: 4202
		// (get) Token: 0x06005F09 RID: 24329
		Type DeclaringType { get; }

		// Token: 0x1700106B RID: 4203
		// (get) Token: 0x06005F0A RID: 24330
		Type ReflectedType { get; }

		// Token: 0x06005F0B RID: 24331
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06005F0C RID: 24332
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06005F0D RID: 24333
		bool IsDefined(Type attributeType, bool inherit);
	}
}
