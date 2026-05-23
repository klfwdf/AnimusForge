using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200090A RID: 2314
	[Guid("9DE59C64-D889-35A1-B897-587D74469E5B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[CLSCompliant(false)]
	[TypeLibImportClass(typeof(EventInfo))]
	[ComVisible(true)]
	public interface _EventInfo
	{
		// Token: 0x06005FBE RID: 24510
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005FBF RID: 24511
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005FC0 RID: 24512
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005FC1 RID: 24513
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x06005FC2 RID: 24514
		string ToString();

		// Token: 0x06005FC3 RID: 24515
		bool Equals(object other);

		// Token: 0x06005FC4 RID: 24516
		int GetHashCode();

		// Token: 0x06005FC5 RID: 24517
		Type GetType();

		// Token: 0x170010C6 RID: 4294
		// (get) Token: 0x06005FC6 RID: 24518
		MemberTypes MemberType { get; }

		// Token: 0x170010C7 RID: 4295
		// (get) Token: 0x06005FC7 RID: 24519
		string Name { get; }

		// Token: 0x170010C8 RID: 4296
		// (get) Token: 0x06005FC8 RID: 24520
		Type DeclaringType { get; }

		// Token: 0x170010C9 RID: 4297
		// (get) Token: 0x06005FC9 RID: 24521
		Type ReflectedType { get; }

		// Token: 0x06005FCA RID: 24522
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06005FCB RID: 24523
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06005FCC RID: 24524
		bool IsDefined(Type attributeType, bool inherit);

		// Token: 0x06005FCD RID: 24525
		MethodInfo GetAddMethod(bool nonPublic);

		// Token: 0x06005FCE RID: 24526
		MethodInfo GetRemoveMethod(bool nonPublic);

		// Token: 0x06005FCF RID: 24527
		MethodInfo GetRaiseMethod(bool nonPublic);

		// Token: 0x170010CA RID: 4298
		// (get) Token: 0x06005FD0 RID: 24528
		EventAttributes Attributes { get; }

		// Token: 0x06005FD1 RID: 24529
		MethodInfo GetAddMethod();

		// Token: 0x06005FD2 RID: 24530
		MethodInfo GetRemoveMethod();

		// Token: 0x06005FD3 RID: 24531
		MethodInfo GetRaiseMethod();

		// Token: 0x06005FD4 RID: 24532
		void AddEventHandler(object target, Delegate handler);

		// Token: 0x06005FD5 RID: 24533
		void RemoveEventHandler(object target, Delegate handler);

		// Token: 0x170010CB RID: 4299
		// (get) Token: 0x06005FD6 RID: 24534
		Type EventHandlerType { get; }

		// Token: 0x170010CC RID: 4300
		// (get) Token: 0x06005FD7 RID: 24535
		bool IsSpecialName { get; }

		// Token: 0x170010CD RID: 4301
		// (get) Token: 0x06005FD8 RID: 24536
		bool IsMulticast { get; }
	}
}
