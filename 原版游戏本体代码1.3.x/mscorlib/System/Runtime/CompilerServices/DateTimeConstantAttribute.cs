using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008AF RID: 2223
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DateTimeConstantAttribute : CustomConstantAttribute
	{
		// Token: 0x06005D9C RID: 23964 RVA: 0x00149560 File Offset: 0x00147760
		[__DynamicallyInvokable]
		public DateTimeConstantAttribute(long ticks)
		{
			this.date = new DateTime(ticks);
		}

		// Token: 0x17001011 RID: 4113
		// (get) Token: 0x06005D9D RID: 23965 RVA: 0x00149574 File Offset: 0x00147774
		[__DynamicallyInvokable]
		public override object Value
		{
			[__DynamicallyInvokable]
			get
			{
				return this.date;
			}
		}

		// Token: 0x06005D9E RID: 23966 RVA: 0x00149584 File Offset: 0x00147784
		internal static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
		{
			foreach (CustomAttributeNamedArgument customAttributeNamedArgument in attr.NamedArguments)
			{
				if (customAttributeNamedArgument.MemberInfo.Name.Equals("Value"))
				{
					return new DateTime((long)customAttributeNamedArgument.TypedValue.Value);
				}
			}
			return new DateTime((long)attr.ConstructorArguments[0].Value);
		}

		// Token: 0x04002A12 RID: 10770
		private DateTime date;
	}
}
