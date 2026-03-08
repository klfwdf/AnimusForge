using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000936 RID: 2358
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class FieldOffsetAttribute : Attribute
	{
		// Token: 0x06006045 RID: 24645 RVA: 0x0014BED8 File Offset: 0x0014A0D8
		[SecurityCritical]
		internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
		{
			int offset;
			if (field.DeclaringType != null && field.GetRuntimeModule().MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out offset))
			{
				return new FieldOffsetAttribute(offset);
			}
			return null;
		}

		// Token: 0x06006046 RID: 24646 RVA: 0x0014BF23 File Offset: 0x0014A123
		[SecurityCritical]
		internal static bool IsDefined(RuntimeFieldInfo field)
		{
			return FieldOffsetAttribute.GetCustomAttribute(field) != null;
		}

		// Token: 0x06006047 RID: 24647 RVA: 0x0014BF2E File Offset: 0x0014A12E
		[__DynamicallyInvokable]
		public FieldOffsetAttribute(int offset)
		{
			this._val = offset;
		}

		// Token: 0x170010E4 RID: 4324
		// (get) Token: 0x06006048 RID: 24648 RVA: 0x0014BF3D File Offset: 0x0014A13D
		[__DynamicallyInvokable]
		public int Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this._val;
			}
		}

		// Token: 0x04002B21 RID: 11041
		internal int _val;
	}
}
