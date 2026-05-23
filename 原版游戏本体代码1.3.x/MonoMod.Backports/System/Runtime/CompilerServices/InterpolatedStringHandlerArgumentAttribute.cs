using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000044 RID: 68
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
	{
		// Token: 0x0600029E RID: 670 RVA: 0x0000CB5B File Offset: 0x0000AD5B
		public InterpolatedStringHandlerArgumentAttribute(string argument)
		{
			this.Arguments = new string[] { argument };
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0000CB73 File Offset: 0x0000AD73
		public InterpolatedStringHandlerArgumentAttribute(params string[] arguments)
		{
			this.Arguments = arguments;
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x060002A0 RID: 672 RVA: 0x0000CB82 File Offset: 0x0000AD82
		public string[] Arguments { get; }
	}
}
