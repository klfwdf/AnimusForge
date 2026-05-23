using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x02000138 RID: 312
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public struct RuntimeFieldHandle : ISerializable
	{
		// Token: 0x06001288 RID: 4744 RVA: 0x0003785C File Offset: 0x00035A5C
		internal RuntimeFieldHandle GetNativeHandle()
		{
			IRuntimeFieldInfo ptr = this.m_ptr;
			if (ptr == null)
			{
				throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
			}
			return new RuntimeFieldHandle(ptr);
		}

		// Token: 0x06001289 RID: 4745 RVA: 0x0003788A File Offset: 0x00035A8A
		internal RuntimeFieldHandle(IRuntimeFieldInfo fieldInfo)
		{
			this.m_ptr = fieldInfo;
		}

		// Token: 0x0600128A RID: 4746 RVA: 0x00037893 File Offset: 0x00035A93
		internal IRuntimeFieldInfo GetRuntimeFieldInfo()
		{
			return this.m_ptr;
		}

		// Token: 0x170001F9 RID: 505
		// (get) Token: 0x0600128B RID: 4747 RVA: 0x0003789C File Offset: 0x00035A9C
		public IntPtr Value
		{
			[SecurityCritical]
			get
			{
				if (this.m_ptr == null)
				{
					return IntPtr.Zero;
				}
				return this.m_ptr.Value.Value;
			}
		}

		// Token: 0x0600128C RID: 4748 RVA: 0x000378CA File Offset: 0x00035ACA
		internal bool IsNullHandle()
		{
			return this.m_ptr == null;
		}

		// Token: 0x0600128D RID: 4749 RVA: 0x000378D5 File Offset: 0x00035AD5
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return ValueType.GetHashCodeOfPtr(this.Value);
		}

		// Token: 0x0600128E RID: 4750 RVA: 0x000378E4 File Offset: 0x00035AE4
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is RuntimeFieldHandle && ((RuntimeFieldHandle)obj).Value == this.Value;
		}

		// Token: 0x0600128F RID: 4751 RVA: 0x00037914 File Offset: 0x00035B14
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public bool Equals(RuntimeFieldHandle handle)
		{
			return handle.Value == this.Value;
		}

		// Token: 0x06001290 RID: 4752 RVA: 0x00037928 File Offset: 0x00035B28
		[__DynamicallyInvokable]
		public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right)
		{
			return left.Equals(right);
		}

		// Token: 0x06001291 RID: 4753 RVA: 0x00037932 File Offset: 0x00035B32
		[__DynamicallyInvokable]
		public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right)
		{
			return !left.Equals(right);
		}

		// Token: 0x06001292 RID: 4754
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string GetName(RtFieldInfo field);

		// Token: 0x06001293 RID: 4755
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void* _GetUtf8Name(RuntimeFieldHandleInternal field);

		// Token: 0x06001294 RID: 4756 RVA: 0x0003793F File Offset: 0x00035B3F
		[SecuritySafeCritical]
		internal static Utf8String GetUtf8Name(RuntimeFieldHandleInternal field)
		{
			return new Utf8String(RuntimeFieldHandle._GetUtf8Name(field));
		}

		// Token: 0x06001295 RID: 4757
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool MatchesNameHash(RuntimeFieldHandleInternal handle, uint hash);

		// Token: 0x06001296 RID: 4758
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern FieldAttributes GetAttributes(RuntimeFieldHandleInternal field);

		// Token: 0x06001297 RID: 4759
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeType GetApproxDeclaringType(RuntimeFieldHandleInternal field);

		// Token: 0x06001298 RID: 4760 RVA: 0x0003794C File Offset: 0x00035B4C
		[SecurityCritical]
		internal static RuntimeType GetApproxDeclaringType(IRuntimeFieldInfo field)
		{
			RuntimeType approxDeclaringType = RuntimeFieldHandle.GetApproxDeclaringType(field.Value);
			GC.KeepAlive(field);
			return approxDeclaringType;
		}

		// Token: 0x06001299 RID: 4761
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetToken(RtFieldInfo field);

		// Token: 0x0600129A RID: 4762
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern object GetValue(RtFieldInfo field, object instance, RuntimeType fieldType, RuntimeType declaringType, ref bool domainInitialized);

		// Token: 0x0600129B RID: 4763
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern object GetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType contextType);

		// Token: 0x0600129C RID: 4764
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void SetValue(RtFieldInfo field, object obj, object value, RuntimeType fieldType, FieldAttributes fieldAttr, RuntimeType declaringType, ref bool domainInitialized);

		// Token: 0x0600129D RID: 4765
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern void SetValueDirect(RtFieldInfo field, RuntimeType fieldType, void* pTypedRef, object value, RuntimeType contextType);

		// Token: 0x0600129E RID: 4766
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeFieldHandleInternal GetStaticFieldForGenericType(RuntimeFieldHandleInternal field, RuntimeType declaringType);

		// Token: 0x0600129F RID: 4767
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool AcquiresContextFromThis(RuntimeFieldHandleInternal field);

		// Token: 0x060012A0 RID: 4768
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsSecurityCritical(RuntimeFieldHandle fieldHandle);

		// Token: 0x060012A1 RID: 4769 RVA: 0x0003796C File Offset: 0x00035B6C
		[SecuritySafeCritical]
		internal bool IsSecurityCritical()
		{
			return RuntimeFieldHandle.IsSecurityCritical(this.GetNativeHandle());
		}

		// Token: 0x060012A2 RID: 4770
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsSecuritySafeCritical(RuntimeFieldHandle fieldHandle);

		// Token: 0x060012A3 RID: 4771 RVA: 0x00037979 File Offset: 0x00035B79
		[SecuritySafeCritical]
		internal bool IsSecuritySafeCritical()
		{
			return RuntimeFieldHandle.IsSecuritySafeCritical(this.GetNativeHandle());
		}

		// Token: 0x060012A4 RID: 4772
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsSecurityTransparent(RuntimeFieldHandle fieldHandle);

		// Token: 0x060012A5 RID: 4773 RVA: 0x00037986 File Offset: 0x00035B86
		[SecuritySafeCritical]
		internal bool IsSecurityTransparent()
		{
			return RuntimeFieldHandle.IsSecurityTransparent(this.GetNativeHandle());
		}

		// Token: 0x060012A6 RID: 4774
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void CheckAttributeAccess(RuntimeFieldHandle fieldHandle, RuntimeModule decoratedTarget);

		// Token: 0x060012A7 RID: 4775 RVA: 0x00037994 File Offset: 0x00035B94
		[SecurityCritical]
		private RuntimeFieldHandle(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			FieldInfo fieldInfo = (RuntimeFieldInfo)info.GetValue("FieldObj", typeof(RuntimeFieldInfo));
			if (fieldInfo == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
			}
			this.m_ptr = fieldInfo.FieldHandle.m_ptr;
			if (this.m_ptr == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
			}
		}

		// Token: 0x060012A8 RID: 4776 RVA: 0x00037A10 File Offset: 0x00035C10
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			if (this.m_ptr == null)
			{
				throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState"));
			}
			RuntimeFieldInfo value = (RuntimeFieldInfo)RuntimeType.GetFieldInfo(this.GetRuntimeFieldInfo());
			info.AddValue("FieldObj", value, typeof(RuntimeFieldInfo));
		}

		// Token: 0x04000674 RID: 1652
		private IRuntimeFieldInfo m_ptr;
	}
}
