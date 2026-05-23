using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009E0 RID: 2528
	[__DynamicallyInvokable]
	public struct EventRegistrationToken
	{
		// Token: 0x06006470 RID: 25712 RVA: 0x0015670F File Offset: 0x0015490F
		internal EventRegistrationToken(ulong value)
		{
			this.m_value = value;
		}

		// Token: 0x17001151 RID: 4433
		// (get) Token: 0x06006471 RID: 25713 RVA: 0x00156718 File Offset: 0x00154918
		internal ulong Value
		{
			get
			{
				return this.m_value;
			}
		}

		// Token: 0x06006472 RID: 25714 RVA: 0x00156720 File Offset: 0x00154920
		[__DynamicallyInvokable]
		public static bool operator ==(EventRegistrationToken left, EventRegistrationToken right)
		{
			return left.Equals(right);
		}

		// Token: 0x06006473 RID: 25715 RVA: 0x00156735 File Offset: 0x00154935
		[__DynamicallyInvokable]
		public static bool operator !=(EventRegistrationToken left, EventRegistrationToken right)
		{
			return !left.Equals(right);
		}

		// Token: 0x06006474 RID: 25716 RVA: 0x00156750 File Offset: 0x00154950
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is EventRegistrationToken && ((EventRegistrationToken)obj).Value == this.Value;
		}

		// Token: 0x06006475 RID: 25717 RVA: 0x0015677D File Offset: 0x0015497D
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.m_value.GetHashCode();
		}

		// Token: 0x04002CF0 RID: 11504
		internal ulong m_value;
	}
}
