using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008AE RID: 2222
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class CustomConstantAttribute : Attribute
	{
		// Token: 0x17001010 RID: 4112
		// (get) Token: 0x06005D99 RID: 23961
		[__DynamicallyInvokable]
		public abstract object Value
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06005D9A RID: 23962 RVA: 0x001494E0 File Offset: 0x001476E0
		internal static object GetRawConstant(CustomAttributeData attr)
		{
			foreach (CustomAttributeNamedArgument customAttributeNamedArgument in attr.NamedArguments)
			{
				if (customAttributeNamedArgument.MemberInfo.Name.Equals("Value"))
				{
					return customAttributeNamedArgument.TypedValue.Value;
				}
			}
			return DBNull.Value;
		}

		// Token: 0x06005D9B RID: 23963 RVA: 0x00149558 File Offset: 0x00147758
		[__DynamicallyInvokable]
		protected CustomConstantAttribute()
		{
		}
	}
}
