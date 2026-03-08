using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace System
{
	// Token: 0x0200014D RID: 333
	[CLSCompliant(false)]
	[ComVisible(true)]
	[NonVersionable]
	public struct TypedReference
	{
		// Token: 0x060014C7 RID: 5319 RVA: 0x0003D9DC File Offset: 0x0003BBDC
		[SecurityCritical]
		[CLSCompliant(false)]
		public unsafe static TypedReference MakeTypedReference(object target, FieldInfo[] flds)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target");
			}
			if (flds == null)
			{
				throw new ArgumentNullException("flds");
			}
			if (flds.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ArrayZeroError"));
			}
			IntPtr[] array = new IntPtr[flds.Length];
			RuntimeType runtimeType = (RuntimeType)target.GetType();
			for (int i = 0; i < flds.Length; i++)
			{
				RuntimeFieldInfo runtimeFieldInfo = flds[i] as RuntimeFieldInfo;
				if (runtimeFieldInfo == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"));
				}
				if (runtimeFieldInfo.IsInitOnly || runtimeFieldInfo.IsStatic)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_TypedReferenceInvalidField"));
				}
				if (runtimeType != runtimeFieldInfo.GetDeclaringTypeInternal() && !runtimeType.IsSubclassOf(runtimeFieldInfo.GetDeclaringTypeInternal()))
				{
					throw new MissingMemberException(Environment.GetResourceString("MissingMemberTypeRef"));
				}
				RuntimeType runtimeType2 = (RuntimeType)runtimeFieldInfo.FieldType;
				if (runtimeType2.IsPrimitive)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_TypeRefPrimitve"));
				}
				if (i < flds.Length - 1 && !runtimeType2.IsValueType)
				{
					throw new MissingMemberException(Environment.GetResourceString("MissingMemberNestErr"));
				}
				array[i] = runtimeFieldInfo.FieldHandle.Value;
				runtimeType = runtimeType2;
			}
			TypedReference result = default(TypedReference);
			TypedReference.InternalMakeTypedReference((void*)(&result), target, array, runtimeType);
			return result;
		}

		// Token: 0x060014C8 RID: 5320
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private unsafe static extern void InternalMakeTypedReference(void* result, object target, IntPtr[] flds, RuntimeType lastFieldType);

		// Token: 0x060014C9 RID: 5321 RVA: 0x0003DB28 File Offset: 0x0003BD28
		public override int GetHashCode()
		{
			if (this.Type == IntPtr.Zero)
			{
				return 0;
			}
			return __reftype(this).GetHashCode();
		}

		// Token: 0x060014CA RID: 5322 RVA: 0x0003DB50 File Offset: 0x0003BD50
		public override bool Equals(object o)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_NYI"));
		}

		// Token: 0x060014CB RID: 5323 RVA: 0x0003DB61 File Offset: 0x0003BD61
		[SecuritySafeCritical]
		public unsafe static object ToObject(TypedReference value)
		{
			return TypedReference.InternalToObject((void*)(&value));
		}

		// Token: 0x060014CC RID: 5324
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern object InternalToObject(void* value);

		// Token: 0x1700025B RID: 603
		// (get) Token: 0x060014CD RID: 5325 RVA: 0x0003DB6B File Offset: 0x0003BD6B
		internal bool IsNull
		{
			get
			{
				return this.Value.IsNull() && this.Type.IsNull();
			}
		}

		// Token: 0x060014CE RID: 5326 RVA: 0x0003DB87 File Offset: 0x0003BD87
		public static Type GetTargetType(TypedReference value)
		{
			return __reftype(value);
		}

		// Token: 0x060014CF RID: 5327 RVA: 0x0003DB91 File Offset: 0x0003BD91
		public static RuntimeTypeHandle TargetTypeToken(TypedReference value)
		{
			return __reftype(value).TypeHandle;
		}

		// Token: 0x060014D0 RID: 5328 RVA: 0x0003DBA0 File Offset: 0x0003BDA0
		[SecuritySafeCritical]
		[CLSCompliant(false)]
		public unsafe static void SetTypedReference(TypedReference target, object value)
		{
			TypedReference.InternalSetTypedReference((void*)(&target), value);
		}

		// Token: 0x060014D1 RID: 5329
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal unsafe static extern void InternalSetTypedReference(void* target, object value);

		// Token: 0x040006EA RID: 1770
		private IntPtr Value;

		// Token: 0x040006EB RID: 1771
		private IntPtr Type;
	}
}
