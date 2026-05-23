using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008EA RID: 2282
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	[__DynamicallyInvokable]
	[Serializable]
	public class StateMachineAttribute : Attribute
	{
		// Token: 0x17001025 RID: 4133
		// (get) Token: 0x06005E04 RID: 24068 RVA: 0x0014A66D File Offset: 0x0014886D
		// (set) Token: 0x06005E05 RID: 24069 RVA: 0x0014A675 File Offset: 0x00148875
		[__DynamicallyInvokable]
		public Type StateMachineType
		{
			[__DynamicallyInvokable]
			get;
			private set; }

		// Token: 0x06005E06 RID: 24070 RVA: 0x0014A67E File Offset: 0x0014887E
		[__DynamicallyInvokable]
		public StateMachineAttribute(Type stateMachineType)
		{
			this.StateMachineType = stateMachineType;
		}
	}
}
