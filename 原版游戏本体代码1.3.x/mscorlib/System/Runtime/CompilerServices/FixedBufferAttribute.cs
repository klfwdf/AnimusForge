using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008B7 RID: 2231
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	[__DynamicallyInvokable]
	public sealed class FixedBufferAttribute : Attribute
	{
		// Token: 0x06005DAA RID: 23978 RVA: 0x00149862 File Offset: 0x00147A62
		[__DynamicallyInvokable]
		public FixedBufferAttribute(Type elementType, int length)
		{
			this.elementType = elementType;
			this.length = length;
		}

		// Token: 0x17001014 RID: 4116
		// (get) Token: 0x06005DAB RID: 23979 RVA: 0x00149878 File Offset: 0x00147A78
		[__DynamicallyInvokable]
		public Type ElementType
		{
			[__DynamicallyInvokable]
			get
			{
				return this.elementType;
			}
		}

		// Token: 0x17001015 RID: 4117
		// (get) Token: 0x06005DAC RID: 23980 RVA: 0x00149880 File Offset: 0x00147A80
		[__DynamicallyInvokable]
		public int Length
		{
			[__DynamicallyInvokable]
			get
			{
				return this.length;
			}
		}

		// Token: 0x04002A17 RID: 10775
		private Type elementType;

		// Token: 0x04002A18 RID: 10776
		private int length;
	}
}
