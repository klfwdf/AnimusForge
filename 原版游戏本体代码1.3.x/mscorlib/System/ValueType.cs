using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
	// Token: 0x02000159 RID: 345
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class ValueType
	{
		// Token: 0x06001570 RID: 5488 RVA: 0x0003ECDC File Offset: 0x0003CEDC
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			RuntimeType runtimeType = (RuntimeType)base.GetType();
			RuntimeType left = (RuntimeType)obj.GetType();
			if (left != runtimeType)
			{
				return false;
			}
			if (ValueType.CanCompareBits(this))
			{
				return ValueType.FastEqualsCheck(this, obj);
			}
			FieldInfo[] fields = runtimeType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < fields.Length; i++)
			{
				object obj2 = ((RtFieldInfo)fields[i]).UnsafeGetValue(this);
				object obj3 = ((RtFieldInfo)fields[i]).UnsafeGetValue(obj);
				if (obj2 == null)
				{
					if (obj3 != null)
					{
						return false;
					}
				}
				else if (!obj2.Equals(obj3))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06001571 RID: 5489
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool CanCompareBits(object obj);

		// Token: 0x06001572 RID: 5490
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool FastEqualsCheck(object a, object b);

		// Token: 0x06001573 RID: 5491
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public override extern int GetHashCode();

		// Token: 0x06001574 RID: 5492
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetHashCodeOfPtr(IntPtr ptr);

		// Token: 0x06001575 RID: 5493 RVA: 0x0003ED79 File Offset: 0x0003CF79
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return base.GetType().ToString();
		}

		// Token: 0x06001576 RID: 5494 RVA: 0x0003ED86 File Offset: 0x0003CF86
		[__DynamicallyInvokable]
		protected ValueType()
		{
		}
	}
}
