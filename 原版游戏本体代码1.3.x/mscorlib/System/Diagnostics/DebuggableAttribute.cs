using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003EB RID: 1003
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	public sealed class DebuggableAttribute : Attribute
	{
		// Token: 0x0600330C RID: 13068 RVA: 0x000C4CB0 File Offset: 0x000C2EB0
		public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled)
		{
			this.m_debuggingModes = DebuggableAttribute.DebuggingModes.None;
			if (isJITTrackingEnabled)
			{
				this.m_debuggingModes |= DebuggableAttribute.DebuggingModes.Default;
			}
			if (isJITOptimizerDisabled)
			{
				this.m_debuggingModes |= DebuggableAttribute.DebuggingModes.DisableOptimizations;
			}
		}

		// Token: 0x0600330D RID: 13069 RVA: 0x000C4CE5 File Offset: 0x000C2EE5
		[__DynamicallyInvokable]
		public DebuggableAttribute(DebuggableAttribute.DebuggingModes modes)
		{
			this.m_debuggingModes = modes;
		}

		// Token: 0x17000772 RID: 1906
		// (get) Token: 0x0600330E RID: 13070 RVA: 0x000C4CF4 File Offset: 0x000C2EF4
		public bool IsJITTrackingEnabled
		{
			get
			{
				return (this.m_debuggingModes & DebuggableAttribute.DebuggingModes.Default) > DebuggableAttribute.DebuggingModes.None;
			}
		}

		// Token: 0x17000773 RID: 1907
		// (get) Token: 0x0600330F RID: 13071 RVA: 0x000C4D01 File Offset: 0x000C2F01
		public bool IsJITOptimizerDisabled
		{
			get
			{
				return (this.m_debuggingModes & DebuggableAttribute.DebuggingModes.DisableOptimizations) > DebuggableAttribute.DebuggingModes.None;
			}
		}

		// Token: 0x17000774 RID: 1908
		// (get) Token: 0x06003310 RID: 13072 RVA: 0x000C4D12 File Offset: 0x000C2F12
		public DebuggableAttribute.DebuggingModes DebuggingFlags
		{
			get
			{
				return this.m_debuggingModes;
			}
		}

		// Token: 0x040016A3 RID: 5795
		private DebuggableAttribute.DebuggingModes m_debuggingModes;

		// Token: 0x02000B82 RID: 2946
		[Flags]
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public enum DebuggingModes
		{
			// Token: 0x040034DB RID: 13531
			[__DynamicallyInvokable]
			None = 0,
			// Token: 0x040034DC RID: 13532
			[__DynamicallyInvokable]
			Default = 1,
			// Token: 0x040034DD RID: 13533
			[__DynamicallyInvokable]
			DisableOptimizations = 256,
			// Token: 0x040034DE RID: 13534
			[__DynamicallyInvokable]
			IgnoreSymbolStoreSequencePoints = 2,
			// Token: 0x040034DF RID: 13535
			[__DynamicallyInvokable]
			EnableEditAndContinue = 4
		}
	}
}
