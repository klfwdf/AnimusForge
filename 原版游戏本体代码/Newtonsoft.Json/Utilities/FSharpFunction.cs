using System;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x0200005A RID: 90
	[NullableContext(2)]
	[Nullable(0)]
	internal class FSharpFunction
	{
		// Token: 0x06000529 RID: 1321 RVA: 0x000165C9 File Offset: 0x000147C9
		public FSharpFunction(object instance, [Nullable(new byte[] { 1, 2, 1 })] MethodCall<object, object> invoker)
		{
			this._instance = instance;
			this._invoker = invoker;
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x000165DF File Offset: 0x000147DF
		[NullableContext(1)]
		public object Invoke(params object[] args)
		{
			return this._invoker(this._instance, args);
		}

		// Token: 0x040001E6 RID: 486
		private readonly object _instance;

		// Token: 0x040001E7 RID: 487
		[Nullable(new byte[] { 1, 2, 1 })]
		private readonly MethodCall<object, object> _invoker;
	}
}
