using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
	// Token: 0x02000619 RID: 1561
	[CLSCompliant(false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class Pointer : ISerializable
	{
		// Token: 0x0600484D RID: 18509 RVA: 0x00106CF1 File Offset: 0x00104EF1
		private Pointer()
		{
		}

		// Token: 0x0600484E RID: 18510 RVA: 0x00106CFC File Offset: 0x00104EFC
		[SecurityCritical]
		private Pointer(SerializationInfo info, StreamingContext context)
		{
			this._ptr = ((IntPtr)info.GetValue("_ptr", typeof(IntPtr))).ToPointer();
			this._ptrType = (RuntimeType)info.GetValue("_ptrType", typeof(RuntimeType));
		}

		// Token: 0x0600484F RID: 18511 RVA: 0x00106D58 File Offset: 0x00104F58
		[SecurityCritical]
		public unsafe static object Box(void* ptr, Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (!type.IsPointer)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");
			}
			RuntimeType runtimeType = type as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");
			}
			return new Pointer
			{
				_ptr = ptr,
				_ptrType = runtimeType
			};
		}

		// Token: 0x06004850 RID: 18512 RVA: 0x00106DD0 File Offset: 0x00104FD0
		[SecurityCritical]
		public unsafe static void* Unbox(object ptr)
		{
			if (!(ptr is Pointer))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "ptr");
			}
			return ((Pointer)ptr)._ptr;
		}

		// Token: 0x06004851 RID: 18513 RVA: 0x00106DFA File Offset: 0x00104FFA
		internal RuntimeType GetPointerType()
		{
			return this._ptrType;
		}

		// Token: 0x06004852 RID: 18514 RVA: 0x00106E02 File Offset: 0x00105002
		[SecurityCritical]
		internal object GetPointerValue()
		{
			return (IntPtr)this._ptr;
		}

		// Token: 0x06004853 RID: 18515 RVA: 0x00106E14 File Offset: 0x00105014
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("_ptr", new IntPtr(this._ptr));
			info.AddValue("_ptrType", this._ptrType);
		}

		// Token: 0x04001E01 RID: 7681
		[SecurityCritical]
		private unsafe void* _ptr;

		// Token: 0x04001E02 RID: 7682
		private RuntimeType _ptrType;
	}
}
