using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x02000876 RID: 2166
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class AssertionFailedException : Exception
	{
		// Token: 0x06002C94 RID: 11412 RVA: 0x000935A1 File Offset: 0x000917A1
		public AssertionFailedException()
		{
			this.Message = "";
		}

		// Token: 0x06002C95 RID: 11413 RVA: 0x000935BF File Offset: 0x000917BF
		[NullableContext(2)]
		public AssertionFailedException(string message)
			: base("Assertion failed! " + message)
		{
			this.Message = message ?? "";
		}

		// Token: 0x06002C96 RID: 11414 RVA: 0x000935ED File Offset: 0x000917ED
		public AssertionFailedException([Nullable(2)] string message, Exception innerException)
			: base("Assertion failed! " + message, innerException)
		{
			this.Message = message ?? "";
		}

		// Token: 0x06002C97 RID: 11415 RVA: 0x0009361C File Offset: 0x0009181C
		public AssertionFailedException([Nullable(2)] string message, string expression)
			: base("Assertion failed! " + expression + " " + message)
		{
			this.Message = message ?? "";
			this.Expression = expression;
		}

		// Token: 0x1700082C RID: 2092
		// (get) Token: 0x06002C98 RID: 11416 RVA: 0x00093657 File Offset: 0x00091857
		public string Expression { get; } = "";

		// Token: 0x1700082D RID: 2093
		// (get) Token: 0x06002C99 RID: 11417 RVA: 0x0009365F File Offset: 0x0009185F
		public new string Message { get; }

		// Token: 0x04003A55 RID: 14933
		private const string AssertFailed = "Assertion failed! ";
	}
}
