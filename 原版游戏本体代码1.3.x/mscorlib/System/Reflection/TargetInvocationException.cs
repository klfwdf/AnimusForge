using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
	// Token: 0x02000622 RID: 1570
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class TargetInvocationException : ApplicationException
	{
		// Token: 0x060048B7 RID: 18615 RVA: 0x001079C5 File Offset: 0x00105BC5
		private TargetInvocationException()
			: base(Environment.GetResourceString("Arg_TargetInvocationException"))
		{
			base.SetErrorCode(-2146232828);
		}

		// Token: 0x060048B8 RID: 18616 RVA: 0x001079E2 File Offset: 0x00105BE2
		private TargetInvocationException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232828);
		}

		// Token: 0x060048B9 RID: 18617 RVA: 0x001079F6 File Offset: 0x00105BF6
		[__DynamicallyInvokable]
		public TargetInvocationException(Exception inner)
			: base(Environment.GetResourceString("Arg_TargetInvocationException"), inner)
		{
			base.SetErrorCode(-2146232828);
		}

		// Token: 0x060048BA RID: 18618 RVA: 0x00107A14 File Offset: 0x00105C14
		[__DynamicallyInvokable]
		public TargetInvocationException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146232828);
		}

		// Token: 0x060048BB RID: 18619 RVA: 0x00107A29 File Offset: 0x00105C29
		internal TargetInvocationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
