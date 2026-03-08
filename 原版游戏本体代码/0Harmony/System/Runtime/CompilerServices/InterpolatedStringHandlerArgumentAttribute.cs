using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004B9 RID: 1209
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	internal sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
	{
		// Token: 0x06001B29 RID: 6953 RVA: 0x0005840F File Offset: 0x0005660F
		public InterpolatedStringHandlerArgumentAttribute(string argument)
		{
			this.Arguments = new string[] { argument };
		}

		// Token: 0x06001B2A RID: 6954 RVA: 0x00058427 File Offset: 0x00056627
		public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
		{
			this.Arguments = arguments;
		}

		// Token: 0x170005E2 RID: 1506
		// (get) Token: 0x06001B2B RID: 6955 RVA: 0x00058436 File Offset: 0x00056636
		public string[] Arguments { get; }
	}
}
